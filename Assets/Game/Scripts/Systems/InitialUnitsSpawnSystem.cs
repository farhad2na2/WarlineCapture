using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(StaticGridBlockerUpdateSystem))]
[UpdateBefore(typeof(DynamicOccupancyRebuildSystem))]
[UpdateBefore(typeof(UnitPathfindingSystem))]
public partial struct InitialUnitsSpawnSystem : ISystem
{
    private static readonly bool EnableInitialSpawnDiagnostics = false;
    private static readonly bool EnableInitialSpawnFreezeLogs = false;
    private const double FreezeLogThresholdSeconds = 0.05d;
    private const int InitialSpawnBatchSize = 24;
    private const int InitialBlockerBatchSize = 24;
    private const int DiagnosticIntervalFrames = 120;
    private const int InitialBaseCoreRequestEntryIndex = -100;
    private const int MaxInitialBuildingCompletionWaitFrames = 300;

    private int _nextDiagnosticFrame;
    private EntityQuery _buildingRuntimeBoundaryQuery;

    public void OnCreate(ref SystemState state)
    {
        _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingConfiguredSpawnableReadModel>(),
            ComponentType.ReadOnly<BuildingFactionProductionSpawnPointReadModel>(),
            ComponentType.ReadWrite<BuildingRuntimeSpawnRequest>());
        state.RequireForUpdate(_buildingRuntimeBoundaryQuery);
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<InitialUnitsSpawnConfig>();
        state.RequireForUpdate<DynamicOccupancyData>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        int spawnedUnitsForLog = 0;
        int spawnedBlockersForLog = 0;
        bool completedForLog = false;
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
            return;

        bool useM01CompactRuntime = Chapter01M01PlayableRuntime.IsActiveMission();
        var queueEntity = RespawnQueueUtils.GetOrCreateQueue(ref state);
        var em = state.EntityManager;
        Entity boundaryEntity = TryGetBuildingRuntimeBoundaryEntity(ref state, out Entity foundBoundaryEntity)
            ? foundBoundaryEntity
            : Entity.Null;

        var initQuery = SystemAPI.QueryBuilder()
            .WithAll<InitialUnitsSpawnConfig>()
            .WithNone<InitialUnitsSpawnInitialized, InitialUnitsSpawnProgress>()
            .Build();
        using (var initEntities = initQuery.ToEntityArray(Allocator.Temp))
        {
            for (int i = 0; i < initEntities.Length; i++)
            {
                Entity entity = initEntities[i];
                InitialUnitsSpawnConfig config = em.GetComponentData<InitialUnitsSpawnConfig>(entity);
                int unitSpawnCount = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity).Length;
                em.AddComponentData(entity, new InitialUnitsSpawnProgress
                {
                    RandomState = math.max(1u, config.RandomSeed),
                    BlockersSpawned = 0,
                    InitialResourcesApplied = 0,
                    InitialBuildingRequestsIssued = 0,
                    InitialBuildingsSpawned = 0,
                    InitialBuildingCompletionWaitFrames = 0
                });
                DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> progressBuffer = em.AddBuffer<InitialUnitsFactionUnitSpawnProgress>(entity);
                progressBuffer.ResizeUninitialized(unitSpawnCount);
                for (int unitIndex = 0; unitIndex < unitSpawnCount; unitIndex++)
                    progressBuffer[unitIndex] = new InitialUnitsFactionUnitSpawnProgress { Spawned = 0 };
            }
        }

        var progressQuery = SystemAPI.QueryBuilder()
            .WithAll<InitialUnitsSpawnConfig, InitialUnitsSpawnProgress>()
            .WithNone<InitialUnitsSpawnInitialized>()
            .Build();
        using var progressEntities = progressQuery.ToEntityArray(Allocator.Temp);

        for (int configIndex = 0; configIndex < progressEntities.Length; configIndex++)
        {
            Entity entity = progressEntities[configIndex];
            InitialUnitsSpawnConfig config = em.GetComponentData<InitialUnitsSpawnConfig>(entity);
            InitialUnitsSpawnProgress progress = em.GetComponentData<InitialUnitsSpawnProgress>(entity);
            var rng = new Unity.Mathematics.Random(math.max(1u, progress.RandomState));

            if (progress.InitialResourcesApplied == 0)
            {
                ApplyInitialResourceTotals(em, config);
                progress.InitialResourcesApplied = 1;
                em.SetComponentData(entity, progress);
            }

            var queueState = state.EntityManager.GetComponentData<RespawnQueueState>(queueEntity);
            queueState.SpawnRadiusCells = math.max(0, config.SpawnRadiusCells);
            queueState.RespawnDelaySeconds = math.max(0.01f, config.RespawnDelaySeconds);
            DynamicBuffer<RespawnFactionSpawnPoint> respawnSpawnPoints = state.EntityManager.GetBuffer<RespawnFactionSpawnPoint>(queueEntity);
            respawnSpawnPoints.Clear();
            bool completedInitialSpawn = false;
            DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawnsBuffer = em.GetBuffer<InitialUnitsFactionSpawnEntry>(entity);
            var factionSpawns = new NativeArray<InitialUnitsFactionSpawnEntry>(factionSpawnsBuffer.Length, Allocator.Temp);
            for (int factionIndex = 0; factionIndex < factionSpawnsBuffer.Length; factionIndex++)
            {
                factionSpawns[factionIndex] = factionSpawnsBuffer[factionIndex];
            }

            for (int factionIndex = 0; factionIndex < factionSpawns.Length; factionIndex++)
            {
                respawnSpawnPoints.Add(new RespawnFactionSpawnPoint
                {
                    FactionId = factionSpawns[factionIndex].FactionId,
                    SpawnCell = factionSpawns[factionIndex].SpawnCell
                });
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            if (progress.InitialBuildingsSpawned == 0)
            {
                bool allInitialBuildingsSpawned = false;
                if (useM01CompactRuntime)
                {
                    progress.InitialBuildingsSpawned = 1;
                    em.SetComponentData(entity, progress);
                }
                else if (boundaryEntity != Entity.Null)
                {
                    var baseGrid = SystemAPI.GetSingleton<GridConfig>();
                    if (progress.InitialBuildingRequestsIssued == 0)
                    {
                        int queuedInitialBuildingRequests = 0;
                        bool issuedInitialBuildingRequests = true;
                        int baseRequestCount = 0;
                        if (config.CreateFactionBases != 0 &&
                            !EnqueueInitialFactionBaseRequests(ref state, boundaryEntity, entity, config, baseGrid, factionSpawns, out baseRequestCount))
                        {
                            issuedInitialBuildingRequests = false;
                        }
                        else
                        {
                            queuedInitialBuildingRequests += baseRequestCount;
                        }

                        int configuredBuildingRequestCount = 0;
                        if (issuedInitialBuildingRequests &&
                            !TryEnqueueInitialBuildingSpawnEntries(ref state, boundaryEntity, entity, config, factionSpawns, out configuredBuildingRequestCount))
                        {
                            issuedInitialBuildingRequests = false;
                        }
                        else
                        {
                            queuedInitialBuildingRequests += configuredBuildingRequestCount;
                        }

                        if (issuedInitialBuildingRequests && queuedInitialBuildingRequests > 0)
                            progress.InitialBuildingRequestsIssued = 1;

                        allInitialBuildingsSpawned = issuedInitialBuildingRequests && queuedInitialBuildingRequests == 0;
                    }
                    else
                    {
                        allInitialBuildingsSpawned = ProcessCompletedInitialBuildingRequests(ref state, boundaryEntity, entity, baseGrid);
                    }
                }
                else
                {
                    allInitialBuildingsSpawned = false;
                }

                if (allInitialBuildingsSpawned)
                    progress.InitialBuildingsSpawned = 1;

                em.SetComponentData(entity, progress);
            }

            var grid = SystemAPI.GetSingleton<GridConfig>();
            var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
            var walkable = SystemAPI.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            var dynamicBlocked = SystemAPI.GetComponent<DynamicBlockerData>(gridEntity).Blocked;
            var occupied = SystemAPI.GetComponent<DynamicOccupancyData>(gridEntity).Occupied;
            var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
            ReserveStaticBlockerFootprints(ref state, ref reserved, grid);
            ReserveExistingUnitFootprints(ref state, ref reserved, grid);

            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity);
            DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> unitProgress = em.GetBuffer<InitialUnitsFactionUnitSpawnProgress>(entity);
            bool hasCustomGameSourceSpawns = em.HasBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity);
            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> customGameSourceSpawns = hasCustomGameSourceSpawns
                ? em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity)
                : default;
            if (useM01CompactRuntime)
                ApplyM01CompactUnitRoster(unitSpawns, unitProgress);
            int remainingBatch = InitialSpawnBatchSize;
            for (int unitIndex = 0; unitIndex < unitSpawns.Length && remainingBatch > 0; unitIndex++)
            {
                InitialUnitsFactionUnitSpawnEntry unitSpawn = unitSpawns[unitIndex];
                InitialUnitsFactionUnitSpawnProgress entryProgress = unitProgress[unitIndex];
                int remaining = math.max(0, unitSpawn.Count - entryProgress.Spawned);
                int toSpawn = math.min(remainingBatch, remaining);
                bool hasPrefab = unitSpawn.Prefab != Entity.Null;
                bool hasSourceKey = TryGetCustomGameUnitSourceKey(customGameSourceSpawns, hasCustomGameSourceSpawns, unitIndex, unitSpawn, out FixedString64Bytes sourceKey);
                if (toSpawn <= 0)
                    continue;
                if (!hasPrefab)
                {
                    if (hasSourceKey)
                        Debug.LogWarning($"[InitialSpawn] skipped source-key unit because no ECS prefab entity was resolved. sourceKey={sourceKey.ToString()} faction={unitSpawn.FactionId} count={unitSpawn.Count}");
                    entryProgress.Spawned = unitSpawn.Count;
                    unitProgress[unitIndex] = entryProgress;
                    continue;
                }

                if (!TryGetFactionSpawnCell(factionSpawns, unitSpawn.FactionId, out int2 factionSpawnCell))
                    continue;

                int2 unitSpawnCenter = factionSpawnCell + unitSpawn.SpawnOffset;
                int spawnedThisEntry = 0;
                for (int i = 0; i < toSpawn; i++)
                {
                    int2 footprintSize = hasPrefab && em.HasComponent<UnitFootprint>(unitSpawn.Prefab)
                        ? em.GetComponentData<UnitFootprint>(unitSpawn.Prefab).Size
                        : new int2(1, 1);
                    bool isAirUnit = hasPrefab && em.HasComponent<UnitAirMovement>(unitSpawn.Prefab);
                    int2 cell = default;
                    float3 pos = default;
                    bool foundPlatformSpawn = isAirUnit &&
                        TryGetInitialAirPlatformSpawn(
                            ref state,
                            boundaryEntity,
                            unitSpawn.FactionId,
                            unitSpawn.SpawnOffset,
                            grid,
                            out cell,
                            out pos);
                    bool foundSpawnCell = foundPlatformSpawn ||
                        TryFindInitialUnitSpawnCell(
                            ref rng,
                            grid,
                            walkable,
                            dynamicBlocked,
                            occupied,
                            ref reserved,
                            unitSpawnCenter,
                            math.max(0, config.SpawnRadiusCells),
                            footprintSize,
                            isAirUnit,
                            out cell);
                    if (!foundSpawnCell)
                    {
                        if (EnableInitialSpawnDiagnostics)
                            Debug.LogWarning($"[InitialSpawn] no-free-cell faction={unitSpawn.FactionId} prefab={unitSpawn.Prefab} center={unitSpawnCenter} radius={config.SpawnRadiusCells} footprint={footprintSize}");
                        break;
                    }

                    var instance = ecb.Instantiate(unitSpawn.Prefab);
                    byte faction = unitSpawn.FactionId;
                    if (!foundPlatformSpawn)
                        pos = GridUtils.CellToWorldCenter(grid, cell);
                    ConfigureSpawnedUnit(
                        em,
                        ecb,
                        instance,
                        unitSpawn.Prefab,
                        hasPrefab,
                        faction,
                        cell,
                        pos);
                    if (hasPrefab && em.HasComponent<UnitIdleWanderState>(unitSpawn.Prefab))
                    {
                        ecb.SetComponent(instance, new UnitIdleWanderState
                        {
                            RandomState = rng.NextUInt(),
                            RetrySeconds = 0f,
                            CurrentIdleDelaySeconds = 0f
                        });
                    }
                    if (hasPrefab)
                    {
                        ecb.RemoveComponent<UnitPathFollow>(instance);
                        ecb.RemoveComponent<UnitPathRange>(instance);
                        ecb.RemoveComponent<EngageTarget>(instance);
                        ecb.RemoveComponent<UnitPathRequest>(instance);
                        ecb.RemoveComponent<UnitTarget>(instance);
                        ecb.RemoveComponent<AutoWanderMoveTag>(instance);
                    }
                    spawnedThisEntry++;
                    spawnedUnitsForLog++;
                }

                entryProgress.Spawned += spawnedThisEntry;
                unitProgress[unitIndex] = entryProgress;
                remainingBatch -= spawnedThisEntry;
            }

            int blockerTargetCount = useM01CompactRuntime ? 0 : config.BlockerCount;
            int blockersToSpawn = math.min(InitialBlockerBatchSize, math.max(0, blockerTargetCount - progress.BlockersSpawned));

            for (int i = 0; i < blockersToSpawn; i++)
            {
                if (config.BlockerPrefab == Entity.Null)
                    break;

                if (!SpawnCellUtility.TryFindSpawnCellNear(ref rng, grid, walkable, dynamicBlocked, occupied, ref reserved, new int2(grid.Width / 2, grid.Height / 2), math.max(0, config.SpawnRadiusCells) + 20, new int2(1, 1), out int2 cell))
                {
                    if (EnableInitialSpawnDiagnostics)
                        Debug.LogWarning($"[InitialSpawn] no-free-blocker-cell center={new int2(grid.Width / 2, grid.Height / 2)} radius={math.max(0, config.SpawnRadiusCells) + 20}");
                    break;
                }

                var instance = ecb.Instantiate(config.BlockerPrefab);
                ecb.SetComponent(instance, new UnitGrid { Cell = cell });
                ecb.SetComponent(instance, LocalTransform.FromPosition(GridUtils.CellToWorldCenter(grid, cell)));
                spawnedBlockersForLog++;
            }
            progress.BlockersSpawned += blockersToSpawn;

            queueState.RandomState = rng.state;
            em.SetComponentData(queueEntity, queueState);

            progress.RandomState = math.max(1u, rng.state);
            em.SetComponentData(entity, progress);

            bool allUnitsSpawned = true;
            for (int unitIndex = 0; unitIndex < unitSpawns.Length; unitIndex++)
            {
                if (unitProgress[unitIndex].Spawned < unitSpawns[unitIndex].Count)
                {
                    allUnitsSpawned = false;
                    break;
                }
            }

            bool allBlockersSpawned = progress.BlockersSpawned >= blockerTargetCount;
            bool canCompleteInitialSpawn = CanCompleteInitialSpawn(em, entity, config, progress);
            if (allUnitsSpawned &&
                allBlockersSpawned &&
                !canCompleteInitialSpawn)
            {
                progress.InitialBuildingCompletionWaitFrames++;
                if (progress.InitialBuildingCompletionWaitFrames >= MaxInitialBuildingCompletionWaitFrames)
                {
                    progress.InitialBuildingsSpawned = 1;
                    canCompleteInitialSpawn = true;
                    Debug.LogWarning($"[InitialSpawn] fail-open initial building completion after {progress.InitialBuildingCompletionWaitFrames} frames. The startup loading gate will clear, but initial buildings may be missing or incomplete.");
                }

                em.SetComponentData(entity, progress);
            }

            if (allUnitsSpawned &&
                canCompleteInitialSpawn &&
                allBlockersSpawned)
            {
                completedInitialSpawn = true;
                ecb.AddComponent<InitialUnitsSpawnInitialized>(entity);
                ecb.RemoveComponent<InitialUnitsSpawnProgress>(entity);
                ecb.RemoveComponent<InitialUnitsFactionUnitSpawnProgress>(entity);
            }

            ecb.Playback(em);
            if (EnableInitialSpawnDiagnostics)
                LogSpawnState(ref state, completedInitialSpawn ? "completed" : "progress");
            if (EnableInitialSpawnDiagnostics && completedInitialSpawn)
                LogInitialSpawnCellDuplicates(ref state, grid);
            completedForLog = completedInitialSpawn;
            ecb.Dispose();
            factionSpawns.Dispose();
            reserved.Dispose();
        }

        double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
        if (EnableInitialSpawnFreezeLogs && elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect:ECS] InitialUnitsSpawnSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms units={spawnedUnitsForLog} blockers={spawnedBlockersForLog} completed={(completedForLog ? 1 : 0)}");
    }

    private static void ApplyM01CompactUnitRoster(
        DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns,
        DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> unitProgress)
    {
        bool playerAssigned = false;
        bool enemyAssigned = false;
        for (int i = 0; i < unitSpawns.Length; i++)
        {
            InitialUnitsFactionUnitSpawnEntry unit = unitSpawns[i];
            bool isPlayer = unit.FactionId == 0;
            bool isEnemy = unit.FactionId == 1;
            bool keep = unit.Prefab != Entity.Null &&
                ((isPlayer && !playerAssigned) || (isEnemy && !enemyAssigned));

            unit.Count = keep ? 1 : 0;
            unit.SpawnOffset = int2.zero;
            unitSpawns[i] = unit;

            if (!keep)
            {
                InitialUnitsFactionUnitSpawnProgress progress = unitProgress[i];
                progress.Spawned = 0;
                unitProgress[i] = progress;
                continue;
            }

            if (isPlayer)
                playerAssigned = true;
            if (isEnemy)
                enemyAssigned = true;
        }
    }

    private void LogSpawnState(ref SystemState state, string reason)
    {
        if (Time.frameCount < _nextDiagnosticFrame)
            return;

        _nextDiagnosticFrame = Time.frameCount + DiagnosticIntervalFrames;
        EntityManager em = state.EntityManager;
        EntityQuery configQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        EntityQuery progressQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnProgress>());
        EntityQuery initializedQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());
        EntityQuery unitGridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>());
        EntityQuery blockerDependencyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGridBlockerDependencyState>());
        byte depsReady = 1;
        string blockerDependencyStatus = "no-blocker-state";
        if (!blockerDependencyQuery.IsEmptyIgnoreFilter)
        {
            RuntimeGridBlockerDependencyState blockerState = em.GetComponentData<RuntimeGridBlockerDependencyState>(blockerDependencyQuery.GetSingletonEntity());
            depsReady = blockerState.ReadyForDependents;
            blockerDependencyStatus = $"ready={blockerState.ReadyForDependents} spawnOnStart={blockerState.SpawnOnStart} spawned={blockerState.Spawned} finalizing={blockerState.SpawnFinalizing} finalizeAfter={blockerState.FinalizeAfterFrames} pendingCity={blockerState.PendingCity} cityHasSpawned={blockerState.CityHasSpawned} cityGenerating={blockerState.CityGenerating}";
        }

        Debug.Log($"[InitialSpawnState] frame={Time.frameCount} reason={reason} configs={configQuery.CalculateEntityCount()} progress={progressQuery.CalculateEntityCount()} initialized={initializedQuery.CalculateEntityCount()} unitGrid={unitGridQuery.CalculateEntityCount()} depsReady={depsReady} {blockerDependencyStatus}");

        configQuery.Dispose();
        progressQuery.Dispose();
        initializedQuery.Dispose();
        unitGridQuery.Dispose();
        blockerDependencyQuery.Dispose();
    }

    private static bool TryGetFactionSpawnCell(NativeArray<InitialUnitsFactionSpawnEntry> spawns, byte factionId, out int2 spawnCell)
    {
        for (int i = 0; i < spawns.Length; i++)
        {
            if (spawns[i].FactionId != factionId)
                continue;

            spawnCell = spawns[i].SpawnCell;
            return true;
        }

        spawnCell = default;
        return false;
    }

    private static bool TryGetCustomGameUnitSourceKey(
        DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceSpawns,
        bool hasSourceSpawns,
        int unitIndex,
        InitialUnitsFactionUnitSpawnEntry unitSpawn,
        out FixedString64Bytes sourceKey)
    {
        sourceKey = default;
        if (!hasSourceSpawns || unitIndex < 0 || unitIndex >= sourceSpawns.Length)
            return false;

        CustomGameFactionUnitSourceSpawnEntry sourceSpawn = sourceSpawns[unitIndex];
        if (sourceSpawn.FactionId != unitSpawn.FactionId ||
            sourceSpawn.Count != unitSpawn.Count ||
            !math.all(sourceSpawn.SpawnOffset == unitSpawn.SpawnOffset) ||
            sourceSpawn.SourceKey.Length == 0)
        {
            return false;
        }

        sourceKey = sourceSpawn.SourceKey;
        return true;
    }

    private static void ConfigureSpawnedUnit(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity instance,
        Entity prefab,
        bool hasPrefab,
        byte faction,
        int2 cell,
        float3 pos)
    {
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new UnitGrid { Cell = cell });
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, LocalTransform.FromPosition(pos));
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new UnitPrevWorldPos { Value = pos });
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new Faction { Id = faction });
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new UnitRespawnPrefab { Prefab = Entity.Null });
        SetOrAddComponent(em, ecb, instance, prefab, hasPrefab, new UnitAttackState { CooldownRemaining = 0f });
    }

    private static void SetOrAddComponent<T>(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity instance,
        Entity prefab,
        bool hasPrefab,
        T component)
        where T : unmanaged, IComponentData
    {
        if (hasPrefab && em.HasComponent<T>(prefab))
            ecb.SetComponent(instance, component);
        else
            ecb.AddComponent(instance, component);
    }

    private bool TryGetBuildingRuntimeBoundaryEntity(ref SystemState state, out Entity entity)
    {
        entity = Entity.Null;
        if (_buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
        return entity != Entity.Null && state.EntityManager.Exists(entity);
    }

    private static void ApplyInitialResourceTotals(EntityManager em, InitialUnitsSpawnConfig config)
    {
        Entity economyEntity = Entity.Null;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<FactionEconomy>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            FactionEconomy economy = em.GetComponentData<FactionEconomy>(entities[i]);
            if (economy.FactionId != 0)
                continue;

            economyEntity = entities[i];
            economy.Money = math.max(0, config.InitialDollars);
            em.SetComponentData(economyEntity, economy);
            return;
        }

        economyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
        em.SetComponentData(economyEntity, new FactionEconomy
        {
            FactionId = 0,
            Money = math.max(0, config.InitialDollars)
        });
        em.SetComponentData(economyEntity, new FactionEconomyPolicy
        {
            Enabled = 0,
            IncomeMultiplier = 1f
        });
    }

    private static bool EnqueueInitialFactionBaseRequests(
        ref SystemState state,
        Entity boundaryEntity,
        Entity configEntity,
        InitialUnitsSpawnConfig config,
        GridConfig grid,
        NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns,
        out int requestCount)
    {
        requestCount = 0;
        if (boundaryEntity == Entity.Null)
            return false;

        if (!TryResolveSpawnableId(ref state, boundaryEntity, config.BaseWallPrefabLookupKey, "Wall_Dirt_Straight", out string wallId, out BuildingConfiguredSpawnableReadModel wallModel) &&
            !TryResolveSpawnableId(ref state, boundaryEntity, config.BaseWallPrefabLookupKey, "Wall_Fence_Straight", out wallId, out wallModel))
            return false;
        if (!TryResolveSpawnableId(ref state, boundaryEntity, config.BaseGatePrefabLookupKey, "Building_Road_Barrier", out string gateId, out BuildingConfiguredSpawnableReadModel gateModel))
            return false;
        if (!TryResolveSpawnableId(ref state, boundaryEntity, config.BaseCoreBuildingPrefabLookupKey, "Building_Ammunition_Depot", out _, out _))
            return false;

        var placements = new List<InitialFactionBasePlacement>();
        InitialFactionBaseLayoutPlanner.BuildPlacements(
            config.BaseHalfWidthCells,
            config.BaseHalfHeightCells,
            placements);
        var placementIds = new Dictionary<string, string>();
        var placementModels = new Dictionary<string, BuildingConfiguredSpawnableReadModel>();
        for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
        {
            InitialFactionBasePlacement placement = placements[placementIndex];
            if (placement.Kind == InitialFactionBasePlacementKind.Gate ||
                placementIds.ContainsKey(placement.PrefabKey))
            {
                continue;
            }

            if (!TryResolveSpawnableId(ref state, boundaryEntity, new FixedString128Bytes(placement.PrefabKey), placement.PrefabKey, out string resolvedId, out BuildingConfiguredSpawnableReadModel resolvedModel))
            {
                if (placement.Kind == InitialFactionBasePlacementKind.CoreBuilding)
                    return false;

                continue;
            }

            placementIds.Add(placement.PrefabKey, resolvedId);
            placementModels.Add(placement.PrefabKey, resolvedModel);
        }

        Vector2Int bottomGateFootprint = ToFootprint(gateModel.FootprintCells, false);
        Vector2Int sideGateFootprint = ToFootprint(gateModel.FootprintCells, true);
        Vector2Int bottomWallFootprint = ToFootprint(wallModel.FootprintCells, false);
        Vector2Int sideWallFootprint = ToFootprint(wallModel.FootprintCells, true);
        int gateHalfGap = InitialFactionBaseLayoutPlanner.CalculateGateHalfGap(bottomGateFootprint, sideGateFootprint, bottomWallFootprint, sideWallFootprint);
        var wallRuns = new List<InitialFactionBaseWallRun>();
        InitialFactionBaseLayoutPlanner.BuildWallRuns(config.BaseHalfWidthCells, config.BaseHalfHeightCells, gateHalfGap, wallRuns);
        var gateFlankWalls = new List<InitialFactionBaseGateFlankWall>();
        InitialFactionBaseLayoutPlanner.BuildGateFlankWalls(
            config.BaseHalfWidthCells,
            config.BaseHalfHeightCells,
            bottomGateFootprint,
            sideGateFootprint,
            bottomWallFootprint,
            sideWallFootprint,
            gateFlankWalls);

        bool allSpawned = true;
        for (int factionIndex = 0; factionIndex < factionSpawns.Length; factionIndex++)
        {
            InitialUnitsFactionSpawnEntry factionSpawn = factionSpawns[factionIndex];
            Vector2Int anchor = new(factionSpawn.SpawnCell.x, factionSpawn.SpawnCell.y);
            for (int wallRunIndex = 0; wallRunIndex < wallRuns.Count; wallRunIndex++)
            {
                InitialFactionBaseWallRun run = wallRuns[wallRunIndex];
                EnqueueInitialBuildingSpawnRequest(
                    ref state,
                    boundaryEntity,
                    configEntity,
                    factionSpawn.FactionId,
                    wallId,
                    new int2(anchor.x + run.StartOffset.x, anchor.y + run.StartOffset.y),
                    false,
                    BuildingRuntimeSpawnRequest.KindWallRun,
                    new int2(anchor.x + run.EndOffset.x, anchor.y + run.EndOffset.y));
                requestCount++;
            }

            for (int flankIndex = 0; flankIndex < gateFlankWalls.Count; flankIndex++)
            {
                InitialFactionBaseGateFlankWall flank = gateFlankWalls[flankIndex];
                EnqueueInitialBuildingSpawnRequest(
                    ref state,
                    boundaryEntity,
                    configEntity,
                    factionSpawn.FactionId,
                    wallId,
                    new int2(anchor.x + flank.OriginOffset.x, anchor.y + flank.OriginOffset.y),
                    flank.RotateVertical,
                    BuildingRuntimeSpawnRequest.KindWallSegment,
                    default,
                    allowExistingWallOverlap: true);
                requestCount++;
            }

            for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
            {
                InitialFactionBasePlacement placement = placements[placementIndex];
                string buildingId;
                BuildingConfiguredSpawnableReadModel model;
                if (placement.Kind == InitialFactionBasePlacementKind.Gate)
                {
                    buildingId = gateId;
                    model = gateModel;
                }
                else
                {
                    if (!placementIds.TryGetValue(placement.PrefabKey, out buildingId) ||
                        !placementModels.TryGetValue(placement.PrefabKey, out model))
                    {
                        continue;
                    }

                }

                Vector2Int footprint = ToFootprint(model.FootprintCells, placement.RotateVertical);
                Vector2Int origin = InitialFactionBaseLayoutPlanner.ResolvePlacementOrigin(anchor, placement, footprint);
                EnqueueInitialBuildingSpawnRequest(
                    ref state,
                    boundaryEntity,
                    configEntity,
                    factionSpawn.FactionId,
                    buildingId,
                    new int2(origin.x, origin.y),
                    placement.RotateVertical,
                    BuildingRuntimeSpawnRequest.KindBuilding,
                    default,
                    false,
                    factionSpawn.FactionId == 0 && placement.Kind == InitialFactionBasePlacementKind.CoreBuilding
                        ? InitialBaseCoreRequestEntryIndex
                        : 0);
                requestCount++;
            }
        }

        return allSpawned;
    }

    private static bool TryEnqueueInitialBuildingSpawnEntries(
        ref SystemState state,
        Entity boundaryEntity,
        Entity configEntity,
        InitialUnitsSpawnConfig config,
        NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns,
        out int requestCount)
    {
        requestCount = 0;
        if (boundaryEntity == Entity.Null)
            return false;

        DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawnsBuffer =
            state.EntityManager.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity);
        for (int buildingIndex = 0; buildingIndex < buildingSpawnsBuffer.Length; buildingIndex++)
        {
            InitialUnitsFactionBuildingSpawnEntry building = buildingSpawnsBuffer[buildingIndex];
            if (building.Prefab == Entity.Null)
                continue;

            if (!TryGetFactionSpawnCell(factionSpawns, building.FactionId, out int2 factionSpawnCell))
            {
                Debug.LogWarning($"[InitialSpawn] skipping initial building entry with no faction spawn. faction={building.FactionId} prefab={building.Prefab}");
                continue;
            }

            string buildingId = building.PrefabLookupKey.ToString();
            if (!TryResolveSpawnableReadModel(ref state, boundaryEntity, buildingId, out _))
            {
                Debug.LogWarning($"[InitialSpawn] skipping unresolved initial building entry. faction={building.FactionId} buildingId={buildingId}");
                continue;
            }

            int2 origin = factionSpawnCell + building.OriginOffset;
            EnqueueInitialBuildingSpawnRequest(
                ref state,
                boundaryEntity,
                configEntity,
                building.FactionId,
                buildingId,
                origin,
                false);
            requestCount++;
        }

        return true;
    }

    private static bool ProcessCompletedInitialBuildingRequests(
        ref SystemState state,
        Entity boundaryEntity,
        Entity configEntity,
        GridConfig grid)
    {
        if (boundaryEntity == Entity.Null ||
            !state.EntityManager.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
        {
            return false;
        }

        bool sawRequest = false;
        bool hasPending = false;
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
            state.EntityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
        for (int i = requests.Length - 1; i >= 0; i--)
        {
            BuildingRuntimeSpawnRequest request = requests[i];
            if (request.PlanEntity != configEntity)
                continue;

            sawRequest = true;
            if (request.Status == BuildingRuntimeSpawnRequest.Pending)
            {
                hasPending = true;
                continue;
            }

            if (request.Status == BuildingRuntimeSpawnRequest.Succeeded)
            {
                if (request.FactionId == 0 &&
                    request.EntryIndex == InitialBaseCoreRequestEntryIndex &&
                    !Chapter01M01PlayableRuntime.IsActiveMission())
                {
                    Vector3 coreFocus = GetFootprintCenterWorld(
                        new Vector2Int(request.ActualOrigin.x, request.ActualOrigin.y),
                        new Vector2Int(request.ActualFootprint.x, request.ActualFootprint.y),
                        grid);
                    InitialUnitsRuntimeState.InitialCameraFocusWorld = coreFocus;
                    InitialUnitsRuntimeState.InitialCameraFocusRequested = true;
                }
            }

            requests.RemoveAt(i);
        }

        if (hasPending)
            return false;

        return sawRequest;
    }

    private static bool CanCompleteInitialSpawn(
        EntityManager em,
        Entity configEntity,
        InitialUnitsSpawnConfig config,
        InitialUnitsSpawnProgress progress)
    {
        return progress.InitialBuildingsSpawned != 0 ||
               !RequiresInitialBuildingCompletion(em, configEntity, config);
    }

    private static bool RequiresInitialBuildingCompletion(EntityManager em, Entity configEntity, InitialUnitsSpawnConfig config)
    {
        if (config.CreateFactionBases != 0)
            return true;

        return em.HasBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity) &&
               em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity).Length > 0;
    }

    private static void EnqueueInitialBuildingSpawnRequest(
        ref SystemState state,
        Entity boundaryEntity,
        Entity configEntity,
        byte factionId,
        string buildingId,
        int2 origin,
        bool rotateVertical,
        byte requestKind = BuildingRuntimeSpawnRequest.KindBuilding,
        int2 endOrigin = default,
        bool allowExistingWallOverlap = false,
        int entryIndex = 0)
    {
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
            state.EntityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = requests.Length + 1,
            RequestKind = requestKind,
            FactionId = factionId,
            BuildingId = new FixedString128Bytes(BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId)),
            PreferredOrigin = origin,
            EndOrigin = endOrigin,
            RotateVertical = rotateVertical ? (byte)1 : (byte)0,
            AllowExistingWallOverlap = allowExistingWallOverlap ? (byte)1 : (byte)0,
            Status = BuildingRuntimeSpawnRequest.Pending,
            PlanEntity = configEntity,
            EntryIndex = entryIndex
        });
    }

    private static bool TryResolveSpawnableId(
        ref SystemState state,
        Entity boundaryEntity,
        FixedString128Bytes configuredKey,
        string fallbackKey,
        out string buildingId,
        out BuildingConfiguredSpawnableReadModel model)
    {
        model = default;
        buildingId = configuredKey.ToString();
        if (!string.IsNullOrWhiteSpace(buildingId) &&
            TryResolveSpawnableReadModel(ref state, boundaryEntity, buildingId, out model))
        {
            return true;
        }

        buildingId = fallbackKey;
        return !string.IsNullOrWhiteSpace(buildingId) &&
               TryResolveSpawnableReadModel(ref state, boundaryEntity, buildingId, out model);
    }

    private static bool TryResolveSpawnableReadModel(
        ref SystemState state,
        Entity boundaryEntity,
        string buildingId,
        out BuildingConfiguredSpawnableReadModel model)
    {
        model = default;
        if (boundaryEntity == Entity.Null ||
            !state.EntityManager.HasBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity))
        {
            return false;
        }

        string normalized = BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables =
            state.EntityManager.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity, true);
        for (int i = 0; i < spawnables.Length; i++)
        {
            BuildingConfiguredSpawnableReadModel candidate = spawnables[i];
            if (candidate.BuildingId.ToString() != normalized)
                continue;

            model = candidate;
            return true;
        }

        return false;
    }

    private static Vector2Int ToFootprint(int2 footprint, bool rotateVertical)
    {
        int x = math.max(1, footprint.x);
        int y = math.max(1, footprint.y);
        return rotateVertical ? new Vector2Int(y, x) : new Vector2Int(x, y);
    }

    private static Vector3 GetFootprintCenterWorld(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
    {
        return new Vector3(
            grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
            grid.Origin.y,
            grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
    }

    private static bool TryReserveInitialAirSpawnCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        int2 center,
        int2 footprintSize,
        out int2 cell)
    {
        cell = default;
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(center, size);
        int2 max = min + size;
        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int index = row + x;
                if (walkable[index].Value == 0 || occupied.IsSet(index))
                    return false;
                if (reserved.IsSet(index) && !blocked.IsSet(index))
                    return false;
            }
        }

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
                reserved.Set(row + x, true);
        }

        cell = center;
        return true;
    }

    private static bool TryFindInitialUnitSpawnCell(
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        int2 center,
        int radiusCells,
        int2 footprintSize,
        bool isAirUnit,
        out int2 cell)
    {
        if (isAirUnit &&
            TryReserveInitialAirSpawnCell(grid, walkable, blocked, occupied, ref reserved, center, footprintSize, out cell))
        {
            return true;
        }

        return SpawnCellUtility.TryFindSpawnCellNear(
            ref rng,
            grid,
            walkable,
            blocked,
            occupied,
            ref reserved,
            center,
            radiusCells,
            footprintSize,
            out cell);
    }

    private static bool TryGetInitialAirPlatformSpawn(
        ref SystemState state,
        Entity boundaryEntity,
        byte factionId,
        int2 configuredSpawnOffset,
        GridConfig grid,
        out int2 cell,
        out float3 position)
    {
        cell = default;
        position = default;
        if (boundaryEntity == Entity.Null ||
            !state.EntityManager.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
        {
            return false;
        }

        bool useHelipad = configuredSpawnOffset.y <= -45;
        string buildingId = BuildingDefinitionSystem.NormalizeSpawnableKey(useHelipad ? "Building_Helipad" : "Building_Airport");
        int remainingSlotIndex = ResolveInitialAirPlatformSlotIndex(configuredSpawnOffset, useHelipad);
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            state.EntityManager.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            BuildingFactionProductionSpawnPointReadModel spawnPoint = spawnPoints[i];
            if (spawnPoint.FactionId != factionId ||
                spawnPoint.BuildingId.ToString() != buildingId)
            {
                continue;
            }

            if (remainingSlotIndex > 0)
            {
                remainingSlotIndex--;
                continue;
            }

            if (!GridUtils.InBounds(spawnPoint.Cell, grid.Width, grid.Height))
                return false;

            cell = spawnPoint.Cell;
            position = spawnPoint.WorldPosition;
            return true;
        }

        return false;
    }

    private static int ResolveInitialAirPlatformSlotIndex(int2 configuredSpawnOffset, bool useHelipad)
    {
        int x = configuredSpawnOffset.x;
        if (useHelipad)
        {
            if (x < 80)
                return 0;
            if (x < 100)
                return 1;
            return 2;
        }

        if (x < 56)
            return 0;
        if (x < 70)
            return 1;
        return 2;
    }

    private static void LogInitialSpawnCellDuplicates(ref SystemState state, in GridConfig grid)
    {
        using var query = state.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        using var occupiedCells = new NativeHashSet<int>(math.max(1024, entities.Length * 32), Allocator.Temp);
        using var centers = new NativeHashSet<int>(math.max(1, entities.Length), Allocator.Temp);
        EntityManager em = state.EntityManager;
        int duplicateCells = 0;
        int duplicateCenters = 0;
        int occupiedFootprintCells = 0;
        int countedEntities = 0;
        string samples = string.Empty;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity unit = entities[i];
            if (!em.Exists(unit) || em.HasComponent<StaticGridBlocker>(unit))
                continue;

            countedEntities++;
            int2 cell = em.GetComponentData<UnitGrid>(unit).Cell;
            int2 size = em.GetComponentData<UnitFootprint>(unit).Size;
            int centerKey = (uint)cell.x < (uint)grid.Width && (uint)cell.y < (uint)grid.Height
                ? cell.y * grid.Width + cell.x
                : int.MinValue + countedEntities;
            if (!centers.Add(centerKey))
            {
                duplicateCenters++;
                if (samples.Length < 430)
                    samples += $" center={cell}";
            }

            int2 min = UnitFootprintUtility.GetMinCell(cell, UnitFootprintUtility.ClampSize(size));
            int2 max = min + UnitFootprintUtility.ClampSize(size);
            for (int y = min.y; y < max.y; y++)
            {
                if (y < 0 || y >= grid.Height)
                    continue;

                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    if (x < 0 || x >= grid.Width)
                        continue;

                    occupiedFootprintCells++;
                    if (!occupiedCells.Add(row + x))
                    {
                        duplicateCells++;
                        if (samples.Length < 430)
                            samples += $" footprint={new int2(x, y)}";
                    }
                }
            }
        }

        Debug.Log($"[InitialSpawnDiag] entities={countedEntities} occupiedCells={occupiedFootprintCells} duplicateCenters={duplicateCenters} duplicateFootprintCells={duplicateCells} samples={samples}");
    }

    private static void ReserveStaticBlockerFootprints(ref SystemState state, ref NativeBitArray reserved, GridConfig grid)
    {
        using var blockerQuery = state.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<StaticGridBlocker>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<GridBlockerSize>());
        using var blockers = blockerQuery.ToEntityArray(Allocator.Temp);
        EntityManager em = state.EntityManager;

        for (int i = 0; i < blockers.Length; i++)
        {
            Entity blocker = blockers[i];
            int2 origin = em.GetComponentData<UnitGrid>(blocker).Cell;
            int2 size = em.GetComponentData<GridBlockerSize>(blocker).Size;
            for (int y = origin.y; y < origin.y + size.y; y++)
            {
                if ((uint)y >= (uint)grid.Height)
                    continue;
                int row = y * grid.Width;
                for (int x = origin.x; x < origin.x + size.x; x++)
                {
                    if ((uint)x >= (uint)grid.Width)
                        continue;
                    reserved.Set(row + x, true);
                }
            }
        }
    }

    private static void ReserveExistingUnitFootprints(ref SystemState state, ref NativeBitArray reserved, GridConfig grid)
    {
        using var unitQuery = state.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        using var units = unitQuery.ToEntityArray(Allocator.Temp);
        EntityManager em = state.EntityManager;

        for (int i = 0; i < units.Length; i++)
        {
            Entity unit = units[i];
            if (!em.Exists(unit))
                continue;

            int2 center = em.GetComponentData<UnitGrid>(unit).Cell;
            int2 size = UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(unit).Size);
            int2 min = UnitFootprintUtility.GetMinCell(center, size);
            int2 max = min + size;
            for (int y = min.y; y < max.y; y++)
            {
                if ((uint)y >= (uint)grid.Height)
                    continue;

                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    if ((uint)x >= (uint)grid.Width)
                        continue;

                    reserved.Set(row + x, true);
                }
            }
        }
    }
}
