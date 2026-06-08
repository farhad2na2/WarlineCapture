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
    private InitialUnitsSpawnProgressSystem _progressSystem;
    private InitialFactionSpawnSnapshotSystem _factionSpawnSnapshotSystem;
    private InitialRespawnQueueProjectionSystem _respawnQueueProjectionSystem;
    private InitialSpawnResourceSystem _resourceSystem;
    private InitialFactionBaseRequestSystem _factionBaseRequestSystem;
    private InitialConfiguredBuildingRequestSystem _configuredBuildingRequestSystem;
    private InitialBuildingCompletionSystem _buildingCompletionSystem;
    private InitialSpawnCompletionSystem _completionSystem;
    private InitialSpawnGridContextSystem _gridContextSystem;
    private InitialSpawnReservationSystem _reservationSystem;
    private InitialUnitSpawnCellSystem _unitSpawnCellSystem;
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

    public void OnCreate(ref SystemState state)
    {
        _queryContext = new InitialUnitsSpawnQuerySystem().Create(ref state);
        _diagnosticLogSystem.EnsureQueue(state.EntityManager);
        state.RequireForUpdate(_queryContext.BuildingRuntimeBoundaryQuery);
        state.RequireForUpdate(_queryContext.GridContextQuery);
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<InitialUnitsSpawnConfig>();
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

        _progressSystem.InitializePending(em, _queryContext);

        using var progressEntities = _queryContext.ProgressQuery.ToEntityArray(Allocator.Temp);

        for (int configIndex = 0; configIndex < progressEntities.Length; configIndex++)
        {
            Entity entity = progressEntities[configIndex];
            InitialUnitsSpawnConfig config = em.GetComponentData<InitialUnitsSpawnConfig>(entity);
            InitialUnitsSpawnProgress progress = em.GetComponentData<InitialUnitsSpawnProgress>(entity);
            var rng = new Unity.Mathematics.Random(math.max(1u, progress.RandomState));

            if (progress.InitialResourcesApplied == 0)
            {
                _resourceSystem.ApplyInitialTotals(em, config);
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
                    if (!_gridContextSystem.TryGetGridConfig(state.EntityManager, _queryContext.GridContextQuery, out GridConfig baseGrid))
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

            if (!_gridContextSystem.TryCreate(em, _queryContext.GridContextQuery, Allocator.Temp, out InitialSpawnGridContextSystem.Context gridContext))
            {
                _structuralApplySystem.PlaybackAndDispose(em, ref structuralContext);
                completedForLog = completedInitialSpawn;
                factionSpawns.Dispose();
                continue;
            }

            var grid = gridContext.Grid;
            _reservationSystem.ReserveStaticBlockerFootprints(em, ref gridContext.Reserved, grid);
            _reservationSystem.ReserveExistingUnitFootprints(em, ref gridContext.Reserved, grid);

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
                        _unitSpawnCellSystem.TryFindInitialUnitSpawnCell(
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
            completedInitialSpawn = _completionSystem.Update(
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

}
