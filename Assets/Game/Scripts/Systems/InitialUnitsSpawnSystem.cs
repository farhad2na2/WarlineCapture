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

    private int _nextDiagnosticFrame;
    private EntityQuery _buildingPlacementRuntimeQuery;

    public void OnCreate(ref SystemState state)
    {
        _buildingPlacementRuntimeQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildingPlacementRuntimeComponent>());
        state.RequireForUpdate(_buildingPlacementRuntimeQuery);
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
        BuildingPlacementSystem buildingPlacementController = GetBuildingPlacement(ref state);

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
                    InitialBuildingsSpawned = 0
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

            if (progress.InitialBuildingsSpawned == 0 && buildingPlacementController != null)
            {
                buildingPlacementController.SetInitialResourceTotals(
                    config.InitialDollars,
                    config.InitialOil,
                    config.InitialFuel);
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
                bool allInitialBuildingsSpawned = true;
                if (useM01CompactRuntime)
                {
                    progress.InitialBuildingsSpawned = 1;
                    em.SetComponentData(entity, progress);
                }
                else if (buildingPlacementController != null)
                {
                    var baseGrid = SystemAPI.GetSingleton<GridConfig>();
                    if (config.CreateFactionBases != 0 &&
                        !TrySpawnInitialFactionBases(buildingPlacementController, config, baseGrid, factionSpawns))
                    {
                        allInitialBuildingsSpawned = false;
                    }

                    DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawnsBuffer = em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(entity);
                    var buildingSpawns = new NativeArray<InitialUnitsFactionBuildingSpawnEntry>(buildingSpawnsBuffer.Length, Allocator.Temp);
                    for (int buildingIndex = 0; buildingIndex < buildingSpawnsBuffer.Length; buildingIndex++)
                        buildingSpawns[buildingIndex] = buildingSpawnsBuffer[buildingIndex];

                    for (int buildingIndex = 0; buildingIndex < buildingSpawns.Length; buildingIndex++)
                    {
                        InitialUnitsFactionBuildingSpawnEntry building = buildingSpawns[buildingIndex];
                        if (building.Prefab == Entity.Null)
                            continue;

                        if (!TryGetFactionSpawnCell(factionSpawns, building.FactionId, out int2 factionSpawnCell))
                        {
                            allInitialBuildingsSpawned = false;
                            continue;
                        }

                        if (!buildingPlacementController.TryResolveConfiguredSpawnablePrefab(building.PrefabLookupKey.ToString(), out GameObject buildingPrefab))
                        {
                            allInitialBuildingsSpawned = false;
                            continue;
                        }

                        Vector2Int origin = new(factionSpawnCell.x + building.OriginOffset.x, factionSpawnCell.y + building.OriginOffset.y);
                        if (!buildingPlacementController.TrySpawnRuntimeBuilding(
                            buildingPrefab,
                            origin,
                            out _,
                            ownerFactionId: building.FactionId))
                        {
                            allInitialBuildingsSpawned = false;
                        }
                    }

                    buildingSpawns.Dispose();
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
            if (useM01CompactRuntime)
                ApplyM01CompactUnitRoster(unitSpawns, unitProgress);
            int remainingBatch = InitialSpawnBatchSize;
            for (int unitIndex = 0; unitIndex < unitSpawns.Length && remainingBatch > 0; unitIndex++)
            {
                InitialUnitsFactionUnitSpawnEntry unitSpawn = unitSpawns[unitIndex];
                InitialUnitsFactionUnitSpawnProgress entryProgress = unitProgress[unitIndex];
                int remaining = math.max(0, unitSpawn.Count - entryProgress.Spawned);
                int toSpawn = math.min(remainingBatch, remaining);
                if (toSpawn <= 0 || unitSpawn.Prefab == Entity.Null)
                    continue;

                if (!TryGetFactionSpawnCell(factionSpawns, unitSpawn.FactionId, out int2 factionSpawnCell))
                    continue;

                int2 unitSpawnCenter = factionSpawnCell + unitSpawn.SpawnOffset;
                int spawnedThisEntry = 0;
                for (int i = 0; i < toSpawn; i++)
                {
                    int2 footprintSize = em.HasComponent<UnitFootprint>(unitSpawn.Prefab)
                        ? em.GetComponentData<UnitFootprint>(unitSpawn.Prefab).Size
                        : new int2(1, 1);
                    bool isAirUnit = em.HasComponent<UnitAirMovement>(unitSpawn.Prefab);
                    int2 cell = default;
                    float3 pos = default;
                    bool foundPlatformSpawn = isAirUnit &&
                        TryGetInitialAirPlatformSpawn(
                            buildingPlacementController,
                            unitSpawn.FactionId,
                            unitSpawn.SpawnOffset,
                            grid,
                            out cell,
                            out pos);
                    bool foundSpawnCell = foundPlatformSpawn ||
                        (isAirUnit
                            ? TryReserveInitialAirSpawnCell(grid, walkable, dynamicBlocked, occupied, ref reserved, unitSpawnCenter, footprintSize, out cell)
                            : SpawnCellUtility.TryFindSpawnCellNear(ref rng, grid, walkable, dynamicBlocked, occupied, ref reserved, unitSpawnCenter, config.SpawnRadiusCells, footprintSize, out cell));
                    if (!foundSpawnCell)
                    {
                        Debug.LogWarning($"[InitialSpawn] no-free-cell faction={unitSpawn.FactionId} prefab={unitSpawn.Prefab} center={unitSpawnCenter} radius={config.SpawnRadiusCells} footprint={footprintSize}");
                        break;
                    }

                    var instance = ecb.Instantiate(unitSpawn.Prefab);
                    byte faction = unitSpawn.FactionId;
                    ecb.SetComponent(instance, new UnitGrid { Cell = cell });
                    if (!foundPlatformSpawn)
                        pos = GridUtils.CellToWorldCenter(grid, cell);
                    ecb.SetComponent(instance, LocalTransform.FromPosition(pos));
                    ecb.SetComponent(instance, new UnitPrevWorldPos { Value = pos });
                    ecb.SetComponent(instance, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
                    ecb.SetComponent(instance, new Faction { Id = faction });
                    ecb.SetComponent(instance, new UnitRespawnPrefab { Prefab = Entity.Null });
                    ecb.SetComponent(instance, new UnitAttackState { CooldownRemaining = 0f });
                    if (em.HasComponent<UnitIdleWanderState>(unitSpawn.Prefab))
                    {
                        ecb.SetComponent(instance, new UnitIdleWanderState
                        {
                            RandomState = rng.NextUInt(),
                            RetrySeconds = 0f,
                            CurrentIdleDelaySeconds = 0f
                        });
                    }
                    ecb.RemoveComponent<UnitPathFollow>(instance);
                    ecb.RemoveComponent<UnitPathRange>(instance);
                    ecb.RemoveComponent<EngageTarget>(instance);
                    ecb.RemoveComponent<UnitPathRequest>(instance);
                    ecb.RemoveComponent<UnitTarget>(instance);
                    ecb.RemoveComponent<AutoWanderMoveTag>(instance);
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

            if (allUnitsSpawned &&
                progress.BlockersSpawned >= blockerTargetCount)
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

    private BuildingPlacementSystem GetBuildingPlacement(ref SystemState state)
    {
        if (_buildingPlacementRuntimeQuery.IsEmptyIgnoreFilter)
            return null;

        Entity entity = _buildingPlacementRuntimeQuery.GetSingletonEntity();
        return state.EntityManager.GetComponentObject<BuildingPlacementRuntimeComponent>(entity).BuildingPlacement;
    }

    private static bool TrySpawnInitialFactionBases(
        BuildingPlacementSystem buildingPlacementController,
        InitialUnitsSpawnConfig config,
        GridConfig grid,
        NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns)
    {
        if (buildingPlacementController == null)
            return false;

        if (!TryResolveBasePrefab(buildingPlacementController, config.BaseWallPrefabLookupKey, "Wall_Dirt_Straight", out GameObject wallPrefab) &&
            !TryResolveBasePrefab(buildingPlacementController, config.BaseWallPrefabLookupKey, "Wall_Fence_Straight", out wallPrefab))
            return false;
        if (!TryResolveBasePrefab(buildingPlacementController, config.BaseGatePrefabLookupKey, "Building_Road_Barrier", out GameObject gatePrefab))
            return false;
        if (!TryResolveBasePrefab(buildingPlacementController, config.BaseCoreBuildingPrefabLookupKey, "Building_Ammunition_Depot", out _))
            return false;

        var placements = new List<InitialFactionBasePlacement>();
        InitialFactionBaseLayoutPlanner.BuildPlacements(
            config.BaseHalfWidthCells,
            config.BaseHalfHeightCells,
            placements);
        Vector2Int bottomGateFootprint = Vector2Int.one;
        Vector2Int sideGateFootprint = Vector2Int.one;
        if (buildingPlacementController.TryGetRuntimeBuildingPlacementFootprint(gatePrefab, false, out Vector2Int resolvedBottomGateFootprint))
            bottomGateFootprint = resolvedBottomGateFootprint;
        if (buildingPlacementController.TryGetRuntimeBuildingPlacementFootprint(gatePrefab, true, out Vector2Int resolvedSideGateFootprint))
            sideGateFootprint = resolvedSideGateFootprint;
        Vector2Int bottomWallFootprint = Vector2Int.one;
        Vector2Int sideWallFootprint = Vector2Int.one;
        if (buildingPlacementController.TryGetRuntimeWallSegmentFootprint(wallPrefab, false, out Vector2Int resolvedBottomWallFootprint))
            bottomWallFootprint = resolvedBottomWallFootprint;
        if (buildingPlacementController.TryGetRuntimeWallSegmentFootprint(wallPrefab, true, out Vector2Int resolvedSideWallFootprint))
            sideWallFootprint = resolvedSideWallFootprint;
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
            int spawnedForFaction = 0;
            int expectedGatesPerBase = CountPlannedPlacements(placements, "Building_Road_Barrier");

            if (buildingPlacementController.CountRuntimeBuildingsForFaction(factionSpawn.FactionId, "Building_Road_Barrier") < expectedGatesPerBase)
            {
                for (int wallRunIndex = 0; wallRunIndex < wallRuns.Count; wallRunIndex++)
                {
                    InitialFactionBaseWallRun run = wallRuns[wallRunIndex];
                    spawnedForFaction += buildingPlacementController.TrySpawnRuntimeWallRun(
                        wallPrefab,
                        anchor + run.StartOffset,
                        anchor + run.EndOffset,
                        factionSpawn.FactionId);
                }

                for (int flankIndex = 0; flankIndex < gateFlankWalls.Count; flankIndex++)
                {
                    InitialFactionBaseGateFlankWall flank = gateFlankWalls[flankIndex];
                    if (buildingPlacementController.TrySpawnRuntimeWallSegment(
                            wallPrefab,
                            anchor + flank.OriginOffset,
                            flank.RotateVertical,
                            factionSpawn.FactionId,
                            allowExistingWallOverlap: true))
                    {
                        spawnedForFaction++;
                    }
                    else
                    {
                        Debug.LogWarning($"[InitialBase] faction={factionSpawn.FactionId} kind=GateFlankWall result=PlacementFailed origin={anchor + flank.OriginOffset}");
                        allSpawned = false;
                    }
                }
            }

            for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
            {
                InitialFactionBasePlacement placement = placements[placementIndex];
                if (buildingPlacementController.CountRuntimeBuildingsForFaction(factionSpawn.FactionId, placement.PrefabKey) >= CountPlannedPlacements(placements, placement.PrefabKey))
                    continue;

                GameObject prefab = placement.Kind == InitialFactionBasePlacementKind.Gate
                    ? gatePrefab
                    : null;
                if (prefab == null && !TryResolveBasePrefab(buildingPlacementController, new FixedString128Bytes(placement.PrefabKey), placement.PrefabKey, out prefab))
                    prefab = null;

                if (prefab == null)
                {
                    allSpawned = false;
                    continue;
                }

                Vector2Int footprint = Vector2Int.one;
                if (!buildingPlacementController.TryGetRuntimeBuildingPlacementFootprint(prefab, placement.RotateVertical, out footprint))
                    footprint = Vector2Int.one;
                Vector2Int origin = InitialFactionBaseLayoutPlanner.ResolvePlacementOrigin(anchor, placement, footprint);
                if (!buildingPlacementController.TrySpawnRuntimeBuilding(
                        prefab,
                        origin,
                        out _,
                        out Vector2Int actualOrigin,
                        out Vector2Int actualFootprint,
                        ownerFactionId: factionSpawn.FactionId,
                        rotateVertical: placement.RotateVertical))
                {
                    Debug.LogWarning($"[InitialBase] faction={factionSpawn.FactionId} kind={placement.Kind} result=PlacementFailed origin={origin}");
                    allSpawned = false;
                }
                else
                {
                    if (factionSpawn.FactionId == 0 &&
                        placement.Kind == InitialFactionBasePlacementKind.CoreBuilding &&
                        !Chapter01M01PlayableRuntime.IsActiveMission())
                    {
                        Vector3 coreFocus = GetFootprintCenterWorld(actualOrigin, actualFootprint, grid);
                        InitialUnitsRuntimeState.InitialCameraFocusWorld = coreFocus;
                        InitialUnitsRuntimeState.InitialCameraFocusRequested = true;
                    }
                    spawnedForFaction++;
                }
            }

            LogInitialBaseCounts(buildingPlacementController, factionSpawn.FactionId, spawnedForFaction, factionSpawn.SpawnCell);
        }

        return allSpawned;
    }

    private static int CountPlannedPlacements(List<InitialFactionBasePlacement> placements, string prefabKey)
    {
        if (placements == null || string.IsNullOrWhiteSpace(prefabKey))
            return 0;

        int count = 0;
        for (int i = 0; i < placements.Count; i++)
        {
            if (placements[i].PrefabKey == prefabKey)
                count++;
        }

        return count;
    }

    private static void LogInitialBaseCounts(BuildingPlacementSystem buildingPlacementController, byte factionId, int spawnedForFaction, int2 center)
    {
        if (buildingPlacementController == null)
            return;

        Debug.Log(
            $"[InitialBase] faction={factionId} result=Spawned buildings={spawnedForFaction} center={center} " +
            $"roadBarrier={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_Road_Barrier")} " +
            $"guardTower={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_GuardTower")} " +
            $"guardTowerBig={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_GuardTower_Big")} " +
            $"oilPump={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_OilPump")} " +
            $"refinery={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_Refinery")} " +
            $"refineryBig={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_Refinery_Big")} " +
            $"satelliteDish={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_Satelite_Dish")} " +
            $"waterTank={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_WaterTank")} " +
            $"airport={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_Airport")} " +
            $"helipad={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_Helipad")} " +
            $"ammunitionDepot={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_Ammunition_Depot")} " +
            $"barrack={buildingPlacementController.CountRuntimeBuildingsForFaction(factionId, "Building_Barrack")}");
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

    private static bool TryGetInitialAirPlatformSpawn(
        BuildingPlacementSystem buildingPlacementController,
        byte factionId,
        int2 configuredSpawnOffset,
        GridConfig grid,
        out int2 cell,
        out float3 position)
    {
        cell = default;
        position = default;
        if (buildingPlacementController == null)
            return false;

        bool useHelipad = configuredSpawnOffset.y <= -45;
        string buildingId = useHelipad ? "Building_Helipad" : "Building_Airport";
        int slotIndex = ResolveInitialAirPlatformSlotIndex(configuredSpawnOffset, useHelipad);
        return buildingPlacementController.TryGetFactionProductionSpawnPoint(factionId, buildingId, slotIndex, grid, out cell, out position);
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

    private static bool TryResolveBasePrefab(
        BuildingPlacementSystem buildingPlacementController,
        FixedString128Bytes configuredKey,
        string fallbackKey,
        out GameObject prefab)
    {
        prefab = null;
        if (buildingPlacementController == null)
            return false;

        string key = configuredKey.ToString();
        if (!string.IsNullOrWhiteSpace(key) &&
            buildingPlacementController.TryResolveConfiguredSpawnablePrefab(key, out prefab))
            return true;

        return !string.IsNullOrWhiteSpace(fallbackKey) &&
               buildingPlacementController.TryResolveConfiguredSpawnablePrefab(fallbackKey, out prefab);
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
