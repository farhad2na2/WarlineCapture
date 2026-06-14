using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateBefore(typeof(StaticGridBlockerUpdateSystem))]
[UpdateBefore(typeof(DynamicOccupancyRebuildSystem))]
[UpdateBefore(typeof(UnitPathfindingSystem))]
public partial struct InitialUnitsSpawnSystem : ISystem
{
    private static readonly bool EnableInitialSpawnDiagnostics = false;
    private const int InitialSpawnBatchSize = 24;
    private const int InitialBlockerBatchSize = 24;
    private const int DiagnosticIntervalFrames = 120;
    private const int InitialBaseCoreRequestEntryIndex = -100;
    private const int MaxInitialBuildingCompletionWaitFrames = 300;

    private InitialUnitsSpawnQuerySystem.Context _queryContext;
    private InitialUnitsSpawnStartupGateSystem _startupGateSystem;
    private InitialFactionSpawnSnapshotSystem _factionSpawnSnapshotSystem;
    private InitialRespawnQueueProjectionSystem _respawnQueueProjectionSystem;
    private InitialFactionBaseRequestSystem _factionBaseRequestSystem;
    private InitialConfiguredBuildingRequestSystem _configuredBuildingRequestSystem;
    private InitialBuildingCompletionSystem _buildingCompletionSystem;
    private InitialAirPlatformSpawnSystem _airPlatformSpawnSystem;
    private InitialUnitSourceKeySystem _sourceKeySystem;
    private InitialUnitSpawnBatchSystem _unitSpawnBatchSystem;
    private InitialUnitSpawnApplySystem _unitSpawnApplySystem;
    private InitialUnitSpawnResetSystem _unitSpawnResetSystem;
    private InitialBlockerSpawnSystem _blockerSpawnSystem;
    private InitialSpawnStructuralApplySystem _structuralApplySystem;
    private InitialSpawnDiagnosticStateSystem _diagnosticStateSystem;
    private InitialSpawnDiagnosticLogSystem _diagnosticLogSystem;
    private InitialSpawnDuplicateCellDiagnosticSystem _duplicateCellDiagnosticSystem;
    private InitialSpawnFreezeDiagnosticSystem _freezeDiagnosticSystem;
    private MapSurfaceSpawnGroundingSystem _spawnGroundingSystem;
    private EntityTypeHandle _progressEntityType;

    public void OnCreate(ref SystemState state)
    {
        _queryContext = new InitialUnitsSpawnQuerySystem().Create(ref state);
        _diagnosticLogSystem.EnsureQueue(state.EntityManager);
        _progressEntityType = state.GetEntityTypeHandle();
        state.RequireForUpdate(_queryContext.BuildingRuntimeBoundaryQuery);
        state.RequireForUpdate(_queryContext.GridContextQuery);
        state.RequireForUpdate(_queryContext.ActiveConfigQuery);
        state.RequireForUpdate<DynamicOccupancyComponent>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        double startTime = _freezeDiagnosticSystem.BeginFrame();
        int spawnedUnitsForLog = 0;
        int spawnedBlockersForLog = 0;
        bool completedForLog = false;
        var startupGate = _startupGateSystem.Evaluate(state.EntityManager, _queryContext);
        if (!startupGate.IsActionable)
            return;

        var queueEntity = _respawnQueueProjectionSystem.GetOrCreateQueue(ref state);
        var em = state.EntityManager;
        Entity boundaryEntity = startupGate.BoundaryEntity;

        InitializeInitialSpawnProgress(em, _queryContext);

        _progressEntityType.Update(ref state);
        using NativeArray<ArchetypeChunk> progressChunks = _queryContext.ProgressQuery.ToArchetypeChunkArray(Allocator.Temp);
        using var progressEntities = new NativeList<Entity>(_queryContext.ProgressQuery.CalculateEntityCount(), Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < progressChunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = progressChunks[chunkIndex].GetNativeArray(_progressEntityType);
            progressEntities.AddRange(entities);
        }

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

            bool completedInitialSpawn = false;
            NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns = _factionSpawnSnapshotSystem.Create(em, entity, Allocator.Temp);
            RespawnQueueComponent queueState = _respawnQueueProjectionSystem.ProjectInitialConfig(em, queueEntity, config, factionSpawns);

            InitialSpawnStructuralApplySystem.Context structuralContext = _structuralApplySystem.Create(Allocator.Temp);
            ref EntityCommandBuffer ecb = ref structuralContext.Ecb;

            if (progress.InitialBuildingsSpawned == 0)
            {
                bool allInitialBuildingsSpawned = false;
                if (boundaryEntity != Entity.Null)
                {
                    if (!TryGetInitialSpawnGridConfig(state.EntityManager, _queryContext.GridContextQuery, out GridConfig baseGrid))
                    {
                        allInitialBuildingsSpawned = false;
                    }
                    else if (progress.InitialBuildingRequestsIssued == 0)
                    {
                        int queuedInitialBuildingRequests = 0;
                        bool issuedInitialBuildingRequests = true;
                        int baseRequestCount = 0;
                        if (config.CreateFactionBases != 0 &&
                            !_factionBaseRequestSystem.Enqueue(state.EntityManager, boundaryEntity, entity, config, baseGrid, factionSpawns, InitialBaseCoreRequestEntryIndex, out baseRequestCount))
                        {
                            issuedInitialBuildingRequests = false;
                        }
                        else
                        {
                            queuedInitialBuildingRequests += baseRequestCount;
                        }

                        int configuredBuildingRequestCount = 0;
                        if (issuedInitialBuildingRequests &&
                            !_configuredBuildingRequestSystem.Enqueue(state.EntityManager, boundaryEntity, entity, factionSpawns, ref _diagnosticLogSystem, out configuredBuildingRequestCount))
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
                        allInitialBuildingsSpawned = _buildingCompletionSystem.Process(state.EntityManager, boundaryEntity, entity, baseGrid, InitialBaseCoreRequestEntryIndex);
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

            if (!TryCreateInitialSpawnGridContext(em, _queryContext.GridContextQuery, Allocator.Temp, out InitialSpawnGridContext gridContext))
            {
                _structuralApplySystem.PlaybackAndDispose(em, ref structuralContext);
                completedForLog = completedInitialSpawn;
                factionSpawns.Dispose();
                continue;
            }

            var grid = gridContext.Grid;
            ReserveStaticBlockerFootprints(em, ref gridContext.Reserved, grid);
            ReserveExistingUnitFootprints(em, ref gridContext.Reserved, grid);

            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity);
            DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> unitProgress = em.GetBuffer<InitialUnitsFactionUnitSpawnProgress>(entity);
            bool hasCustomGameSourceSpawns = em.HasBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity);
            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> customGameSourceSpawns = hasCustomGameSourceSpawns
                ? em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity)
                : default;
            int remainingBatch = InitialSpawnBatchSize;
            for (int unitIndex = 0; unitIndex < unitSpawns.Length && remainingBatch > 0; unitIndex++)
            {
                if (!_unitSpawnBatchSystem.TryCreateEntryBatch(unitSpawns, unitProgress, unitIndex, remainingBatch, out InitialUnitSpawnBatchSystem.EntryBatch batch))
                    continue;

                InitialUnitsFactionUnitSpawnEntry unitSpawn = batch.UnitSpawn;
                InitialUnitsFactionUnitSpawnProgress entryProgress = batch.EntryProgress;
                bool hasSourceKey = _sourceKeySystem.TryGetCustomGameUnitSourceKey(customGameSourceSpawns, hasCustomGameSourceSpawns, unitIndex, unitSpawn, out FixedString64Bytes sourceKey);
                if (_sourceKeySystem.TrySkipMissingPrefabUnit(em, unitSpawn, batch.HasPrefab, hasSourceKey, sourceKey, ref entryProgress, ref _diagnosticLogSystem))
                {
                    unitProgress[unitIndex] = entryProgress;
                    continue;
                }

                if (!_unitSpawnBatchSystem.TryCreateSpawnPlan(state.EntityManager, factionSpawns, batch, out InitialUnitSpawnBatchSystem.SpawnPlan spawnPlan))
                    continue;

                int spawnedThisEntry = 0;
                for (int i = 0; i < batch.ToSpawn; i++)
                {
                    int2 cell = default;
                    float3 pos = default;
                    bool foundPlatformSpawn = spawnPlan.IsAirUnit &&
                        _airPlatformSpawnSystem.TryGetInitialAirPlatformSpawn(
                            state.EntityManager,
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
                            gridContext.Walkable,
                            gridContext.DynamicBlocked,
                            gridContext.Occupied,
                            ref gridContext.Reserved,
                            spawnPlan.UnitSpawnCenter,
                            math.max(0, config.SpawnRadiusCells),
                            spawnPlan.FootprintSize,
                            spawnPlan.IsAirUnit,
                            out cell);
                    if (!foundSpawnCell)
                    {
                        if (EnableInitialSpawnDiagnostics)
                            _diagnosticLogSystem.EnqueueWarning(em, $"[InitialSpawn] no-free-cell faction={unitSpawn.FactionId} prefab={unitSpawn.Prefab} center={spawnPlan.UnitSpawnCenter} radius={config.SpawnRadiusCells} footprint={spawnPlan.FootprintSize}");
                        break;
                    }

                    byte faction = unitSpawn.FactionId;
                    if (!foundPlatformSpawn)
                    {
                        pos = GridUtils.CellToWorldCenter(grid, cell);
                        _spawnGroundingSystem.TryGroundCellCenter(em, grid, cell, ref pos, out _);
                    }
                    Entity instance = _unitSpawnApplySystem.InstantiateAndConfigureSpawnedUnit(
                        em,
                        ecb,
                        unitSpawn.Prefab,
                        batch.HasPrefab,
                        faction,
                        cell,
                        pos);
                    _unitSpawnResetSystem.ResetSpawnedUnitRuntimeState(em, ecb, instance, unitSpawn.Prefab, batch.HasPrefab, ref rng);
                    spawnedThisEntry++;
                    spawnedUnitsForLog++;
                }

                _unitSpawnBatchSystem.ApplySpawnedCount(unitProgress, batch, spawnedThisEntry, ref remainingBatch);
            }

            InitialBlockerSpawnSystem.Result blockerSpawnResult = _blockerSpawnSystem.SpawnBatch(
                ref rng,
                em,
                ecb,
                config,
                InitialBlockerBatchSize,
                progress.BlockersSpawned,
                grid,
                gridContext.Walkable,
                gridContext.DynamicBlocked,
                gridContext.Occupied,
                ref gridContext.Reserved,
                EnableInitialSpawnDiagnostics,
                ref _diagnosticLogSystem);
            spawnedBlockersForLog += blockerSpawnResult.SpawnedForLog;
            progress.BlockersSpawned += blockerSpawnResult.ProgressIncrement;

            _respawnQueueProjectionSystem.WriteRandomState(em, queueEntity, queueState, rng.state);

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

            bool allBlockersSpawned = progress.BlockersSpawned >= blockerSpawnResult.TargetCount;
            completedInitialSpawn = UpdateInitialSpawnCompletion(
                em,
                ecb,
                entity,
                config,
                ref progress,
                allUnitsSpawned,
                allBlockersSpawned,
                MaxInitialBuildingCompletionWaitFrames,
                ref _diagnosticLogSystem,
                out bool completionProgressChanged);
            if (completionProgressChanged)
                em.SetComponentData(entity, progress);

            _structuralApplySystem.PlaybackAndDispose(em, ref structuralContext);
            if (EnableInitialSpawnDiagnostics)
                _diagnosticStateSystem.LogSpawnState(ref state, completedInitialSpawn ? "completed" : "progress", DiagnosticIntervalFrames, ref _diagnosticLogSystem);
            if (EnableInitialSpawnDiagnostics && completedInitialSpawn)
                _duplicateCellDiagnosticSystem.LogInitialSpawnCellDuplicates(ref state, grid, ref _diagnosticLogSystem);
            completedForLog = completedInitialSpawn;
            factionSpawns.Dispose();
            gridContext.Dispose();
        }

        _freezeDiagnosticSystem.LogIfExceeded(
            state.EntityManager,
            startTime,
            spawnedUnitsForLog,
            spawnedBlockersForLog,
            completedForLog,
            ref _diagnosticLogSystem);
    }

    internal static bool TryFindInitialUnitSpawnCell(
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

    internal static void ApplyInitialResourceTotals(EntityManager em, InitialUnitsSpawnConfig config)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<FactionEconomy>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<FactionEconomy> economyType = em.GetComponentTypeHandle<FactionEconomy>(false);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref economyType);
            for (int i = 0; i < entities.Length; i++)
            {
                FactionEconomy economy = economies[i];
                if (!FactionIdentitySystem.IsPlayerControlled(economy.FactionId))
                    continue;

                economy.Money = math.max(0, config.InitialDollars);
                economies[i] = economy;
                return;
            }
        }

        Entity economyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
        em.SetComponentData(economyEntity, new FactionEconomy
        {
            FactionId = FactionIdentitySystem.PlayerFactionId,
            Money = math.max(0, config.InitialDollars)
        });
        em.SetComponentData(economyEntity, new FactionEconomyPolicy
        {
            Enabled = 0,
            IncomeMultiplier = 1f
        });
    }

    internal static void InitializeInitialSpawnProgress(EntityManager em, InitialUnitsSpawnQuerySystem.Context queryContext)
    {
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = queryContext.PendingInitQuery.ToArchetypeChunkArray(Allocator.Temp);
        using var initEntities = new NativeList<Entity>(queryContext.PendingInitQuery.CalculateEntityCount(), Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            initEntities.AddRange(entities);
        }

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

    internal static bool UpdateInitialSpawnCompletion(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity configEntity,
        InitialUnitsSpawnConfig config,
        ref InitialUnitsSpawnProgress progress,
        bool allUnitsSpawned,
        bool allBlockersSpawned,
        int maxInitialBuildingCompletionWaitFrames,
        ref InitialSpawnDiagnosticLogSystem diagnosticLogSystem,
        out bool progressChanged)
    {
        progressChanged = false;
        bool canCompleteInitialSpawn = CanCompleteInitialSpawn(em, configEntity, config, progress);
        if (allUnitsSpawned &&
            allBlockersSpawned &&
            !canCompleteInitialSpawn)
        {
            progress.InitialBuildingCompletionWaitFrames++;
            progressChanged = true;
            if (progress.InitialBuildingCompletionWaitFrames >= maxInitialBuildingCompletionWaitFrames)
            {
                progress.InitialBuildingsSpawned = 1;
                canCompleteInitialSpawn = true;
                diagnosticLogSystem.EnqueueWarning(em, $"[InitialSpawn] fail-open initial building completion after {progress.InitialBuildingCompletionWaitFrames} frames. The startup loading gate will clear, but initial buildings may be missing or incomplete.");
            }
        }

        if (allUnitsSpawned &&
            canCompleteInitialSpawn &&
            allBlockersSpawned)
        {
            ecb.AddComponent<InitialUnitsSpawnInitialized>(configEntity);
            ecb.RemoveComponent<InitialUnitsSpawnProgress>(configEntity);
            ecb.RemoveComponent<InitialUnitsFactionUnitSpawnProgress>(configEntity);
            return true;
        }

        return false;
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

    private struct InitialSpawnGridContext : IDisposable
    {
        public readonly Entity GridEntity;
        public readonly GridConfig Grid;
        public readonly NativeArray<GridWalkable> Walkable;
        public readonly NativeBitArray DynamicBlocked;
        public readonly NativeBitArray Occupied;
        public NativeBitArray Reserved;

        public InitialSpawnGridContext(
            Entity gridEntity,
            GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray dynamicBlocked,
            NativeBitArray occupied,
            NativeBitArray reserved)
        {
            GridEntity = gridEntity;
            Grid = grid;
            Walkable = walkable;
            DynamicBlocked = dynamicBlocked;
            Occupied = occupied;
            Reserved = reserved;
        }

        public void Dispose()
        {
            if (Reserved.IsCreated)
                Reserved.Dispose();
        }
    }

    private static bool TryGetInitialSpawnGridConfig(EntityManager em, EntityQuery gridContextQuery, out GridConfig grid)
    {
        if (!TryGetInitialSpawnGridEntity(em, gridContextQuery, out Entity gridEntity))
        {
            grid = default;
            return false;
        }

        grid = em.GetComponentData<GridConfig>(gridEntity);
        return true;
    }

    private static bool TryCreateInitialSpawnGridContext(
        EntityManager em,
        EntityQuery gridContextQuery,
        Allocator allocator,
        out InitialSpawnGridContext context)
    {
        context = default;
        if (!TryGetInitialSpawnGridEntity(em, gridContextQuery, out Entity gridEntity))
            return false;

        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        NativeBitArray dynamicBlocked = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
        var reserved = new NativeBitArray(grid.Width * grid.Height, allocator);
        context = new InitialSpawnGridContext(gridEntity, grid, walkable, dynamicBlocked, occupied, reserved);
        return true;
    }

    private static bool TryGetInitialSpawnGridEntity(EntityManager em, EntityQuery gridContextQuery, out Entity gridEntity)
    {
        gridEntity = Entity.Null;
        int entityCount = gridContextQuery.CalculateEntityCount();
        if (entityCount <= 0)
            return false;

        if (entityCount == 1)
        {
            gridEntity = gridContextQuery.GetSingletonEntity();
            return gridEntity != Entity.Null;
        }

        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = gridContextQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> gridEntities = chunks[chunkIndex].GetNativeArray(entityType);
            if (gridEntities.Length <= 0)
                continue;

            gridEntity = gridEntities[0];
            return gridEntity != Entity.Null;
        }

        return false;
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

    private static void ReserveStaticBlockerFootprints(EntityManager em, ref NativeBitArray reserved, GridConfig grid)
    {
        using var blockerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<StaticGridBlocker>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<GridBlockerSize>());
        ComponentTypeHandle<UnitGrid> unitGridType = em.GetComponentTypeHandle<UnitGrid>(true);
        ComponentTypeHandle<GridBlockerSize> blockerSizeType = em.GetComponentTypeHandle<GridBlockerSize>(true);
        using NativeArray<ArchetypeChunk> chunks = blockerQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<UnitGrid> unitGrids = chunk.GetNativeArray(ref unitGridType);
            NativeArray<GridBlockerSize> blockerSizes = chunk.GetNativeArray(ref blockerSizeType);
            for (int i = 0; i < unitGrids.Length; i++)
            {
                int2 origin = unitGrids[i].Cell;
                int2 size = blockerSizes[i].Size;
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
    }

    private static void ReserveExistingUnitFootprints(EntityManager em, ref NativeBitArray reserved, GridConfig grid)
    {
        using var unitQuery = em.CreateEntityQuery(new EntityQueryDesc
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
        ComponentTypeHandle<UnitGrid> unitGridType = em.GetComponentTypeHandle<UnitGrid>(true);
        ComponentTypeHandle<UnitFootprint> footprintType = em.GetComponentTypeHandle<UnitFootprint>(true);
        using NativeArray<ArchetypeChunk> chunks = unitQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<UnitGrid> unitGrids = chunk.GetNativeArray(ref unitGridType);
            NativeArray<UnitFootprint> footprints = chunk.GetNativeArray(ref footprintType);
            for (int i = 0; i < unitGrids.Length; i++)
            {
                int2 center = unitGrids[i].Cell;
                int2 size = UnitFootprintUtility.ClampSize(footprints[i].Size);
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
}
