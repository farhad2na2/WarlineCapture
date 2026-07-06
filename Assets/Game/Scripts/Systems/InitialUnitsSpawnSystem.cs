using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
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
        private static readonly bool EnableInitialSpawnFreezeLogs = false;
        private const double FreezeLogThresholdSeconds = 0.05d;

        private EntityQuery _buildingRuntimeBoundaryQuery;
        private EntityQuery _runtimeGameplayStateQuery;
        private EntityQuery _gridContextQuery;
        private EntityQuery _activeConfigQuery;
        private EntityQuery _pendingInitQuery;
        private EntityQuery _progressQuery;
        private InitialUnitSpawnApplySystem _unitSpawnApplySystem;
        private InitialUnitSpawnResetSystem _unitSpawnResetSystem;
        private InitialSpawnDiagnosticLogWriter _diagnosticLogWriter;
        private MapSurfaceSpawnGrounding _spawnGroundingSystem;
        private EntityTypeHandle _progressEntityType;
        private int _nextDiagnosticFrame;

        internal struct InitialSpawnDiagnosticLogWriter
        {
            private Entity _logQueueEntity;

            public void EnsureQueue(EntityManager em)
            {
                if (_logQueueEntity == Entity.Null || !em.Exists(_logQueueEntity))
                    _logQueueEntity = GetOrCreateLogQueue(em);
            }

            public void EnqueueLog(EntityManager em, string message)
            {
                Enqueue(em, message, InitialSpawnDiagnosticLogComponent.LogSeverity);
            }

            public void EnqueueWarning(EntityManager em, string message)
            {
                Enqueue(em, message, InitialSpawnDiagnosticLogComponent.WarningSeverity);
            }

            private void Enqueue(EntityManager em, string message, byte severity)
            {
                EnsureQueue(em);
                DynamicBuffer<InitialSpawnDiagnosticLogComponent> logs =
                    em.GetBuffer<InitialSpawnDiagnosticLogComponent>(_logQueueEntity);
                logs.Add(new InitialSpawnDiagnosticLogComponent
                {
                    Message = CreateFixedMessage(message),
                    Severity = severity
                });
            }

            private static Entity GetOrCreateLogQueue(EntityManager em)
            {
                EntityQuery query = em.CreateEntityQuery(
                    ComponentType.ReadOnly<InitialSpawnDiagnosticLogQueueComponent>(),
                    ComponentType.ReadWrite<InitialSpawnDiagnosticLogComponent>());
                try
                {
                    if (!query.IsEmptyIgnoreFilter)
                        return query.GetSingletonEntity();
                }
                finally
                {
                    query.Dispose();
                }

                Entity queueEntity = em.CreateEntity(typeof(InitialSpawnDiagnosticLogQueueComponent));
                em.SetName(queueEntity, "InitialSpawnDiagnosticLogQueue");
                em.AddBuffer<InitialSpawnDiagnosticLogComponent>(queueEntity);
                return queueEntity;
            }

            private static FixedString4096Bytes CreateFixedMessage(string message)
            {
                var fixedMessage = new FixedString4096Bytes();
                fixedMessage.Append(message);
                return fixedMessage;
            }
        }

        public void OnCreate(ref SystemState state)
        {
            _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                ComponentType.ReadOnly<BuildingConfiguredSpawnableReadModel>(),
                ComponentType.ReadOnly<BuildingFactionProductionSpawnPointReadModel>(),
                ComponentType.ReadWrite<BuildingRuntimeSpawnRequest>());

            _runtimeGameplayStateQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<RuntimeGameplayStateComponent>());

            _gridContextQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridWalkable>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>(),
                ComponentType.ReadOnly<DynamicOccupancyComponent>());

            _activeConfigQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<InitialUnitsSpawnConfig>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<InitialUnitsSpawnInitialized>()
                }
            });

            _pendingInitQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<InitialUnitsSpawnConfig>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<InitialUnitsSpawnInitialized>(),
                    ComponentType.ReadOnly<InitialUnitsSpawnProgress>()
                }
            });

            _progressQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
                    ComponentType.ReadOnly<InitialUnitsSpawnProgress>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<InitialUnitsSpawnInitialized>()
                }
            });

            _diagnosticLogWriter.EnsureQueue(state.EntityManager);
            _progressEntityType = state.GetEntityTypeHandle();
            state.RequireForUpdate(_buildingRuntimeBoundaryQuery);
            state.RequireForUpdate(_gridContextQuery);
            state.RequireForUpdate(_activeConfigQuery);
            state.RequireForUpdate<DynamicOccupancyComponent>();
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            double startTime = BeginInitialSpawnFrame();
            int spawnedUnitsForLog = 0;
            int spawnedBlockersForLog = 0;
            bool completedForLog = false;
            var startupGate = EvaluateInitialSpawnStartupGate(state.EntityManager, _runtimeGameplayStateQuery, _buildingRuntimeBoundaryQuery);
            if (!startupGate.IsActionable)
                return;

            var queueEntity = RespawnQueueUtility.GetOrCreateQueue(ref state);
            var em = state.EntityManager;
            Entity boundaryEntity = startupGate.BoundaryEntity;

            InitializeInitialSpawnProgress(em, _pendingInitQuery);

            _progressEntityType.Update(ref state);
            using NativeArray<ArchetypeChunk> progressChunks = _progressQuery.ToArchetypeChunkArray(Allocator.Temp);
            using var progressEntities = new NativeList<Entity>(_progressQuery.CalculateEntityCount(), Allocator.Temp);
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
                NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns = CreateInitialFactionSpawnSnapshot(em, entity, Allocator.Temp);
                RespawnQueueComponent queueState = ProjectInitialRespawnQueueConfig(em, queueEntity, config, factionSpawns);

                var ecb = new EntityCommandBuffer(Allocator.Temp);

                if (progress.InitialBuildingsSpawned == 0)
                {
                    bool allInitialBuildingsSpawned = false;
                    if (boundaryEntity != Entity.Null)
                    {
                        if (!TryGetInitialSpawnGridConfig(state.EntityManager, _gridContextQuery, out GridConfig baseGrid))
                        {
                            allInitialBuildingsSpawned = false;
                        }
                        else if (progress.InitialBuildingRequestsIssued == 0)
                        {
                            int queuedInitialBuildingRequests = 0;
                            bool issuedInitialBuildingRequests = true;
                            int baseRequestCount = 0;
                            if (config.CreateFactionBases != 0 &&
                                !EnqueueInitialFactionBaseRequests(state.EntityManager, boundaryEntity, entity, config, factionSpawns, baseGrid, InitialBaseCoreRequestEntryIndex, out baseRequestCount))
                            {
                                issuedInitialBuildingRequests = false;
                            }
                            else
                            {
                                queuedInitialBuildingRequests += baseRequestCount;
                            }

                            int configuredBuildingRequestCount = 0;
                            if (issuedInitialBuildingRequests &&
                                !EnqueueConfiguredInitialBuildingRequests(state.EntityManager, boundaryEntity, entity, factionSpawns, ref _diagnosticLogWriter, out configuredBuildingRequestCount))
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
                            allInitialBuildingsSpawned = ProcessInitialBuildingCompletion(
                                state.EntityManager,
                                boundaryEntity,
                                entity,
                                baseGrid,
                                InitialBaseCoreRequestEntryIndex,
                                ref _diagnosticLogWriter);
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

                if (!TryCreateInitialSpawnGridContext(em, _gridContextQuery, Allocator.Temp, out InitialSpawnGridContext gridContext))
                {
                    PlaybackAndDisposeInitialSpawnStructuralChanges(em, ref ecb);
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
                    if (!TryCreateInitialUnitSpawnEntryBatch(unitSpawns, unitProgress, unitIndex, remainingBatch, out InitialUnitSpawnEntryBatch batch))
                        continue;

                    InitialUnitsFactionUnitSpawnEntry unitSpawn = batch.UnitSpawn;
                    InitialUnitsFactionUnitSpawnProgress entryProgress = batch.EntryProgress;
                    bool hasSourceKey = TryGetCustomGameUnitSourceKey(customGameSourceSpawns, hasCustomGameSourceSpawns, unitIndex, unitSpawn, out FixedString64Bytes sourceKey);
                    if (TrySkipMissingPrefabUnit(em, unitSpawn, batch.HasPrefab, hasSourceKey, sourceKey, ref entryProgress, ref _diagnosticLogWriter))
                    {
                        unitProgress[unitIndex] = entryProgress;
                        continue;
                    }

                    if (!TryCreateInitialUnitSpawnPlan(state.EntityManager, factionSpawns, batch, out InitialUnitSpawnPlan spawnPlan))
                        continue;

                    int spawnedThisEntry = 0;
                    for (int i = 0; i < batch.ToSpawn; i++)
                    {
                        int2 cell = default;
                        float3 pos = default;
                        bool foundSpawnCell = TryFindInitialUnitSpawnCell(
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
                                _diagnosticLogWriter.EnqueueWarning(em, $"[InitialSpawn] no-free-cell faction={unitSpawn.FactionId} prefab={unitSpawn.Prefab} center={spawnPlan.UnitSpawnCenter} radius={config.SpawnRadiusCells} footprint={spawnPlan.FootprintSize}");
                            break;
                        }

                        byte faction = unitSpawn.FactionId;
                        pos = GridUtils.CellToWorldCenter(grid, cell);
                        _spawnGroundingSystem.TryGroundCellCenter(em, grid, cell, ref pos, out _);
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

                    ApplyInitialUnitSpawnedCount(unitProgress, batch, spawnedThisEntry, ref remainingBatch);
                }

                InitialBlockerSpawnResult blockerSpawnResult = SpawnInitialBlockerBatch(
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
                    ref _diagnosticLogWriter);
                spawnedBlockersForLog += blockerSpawnResult.SpawnedForLog;
                progress.BlockersSpawned += blockerSpawnResult.ProgressIncrement;

                WriteInitialRespawnQueueRandomState(em, queueEntity, queueState, rng.state);

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
                    ref _diagnosticLogWriter,
                    out bool completionProgressChanged);
                if (completionProgressChanged)
                    em.SetComponentData(entity, progress);

                PlaybackAndDisposeInitialSpawnStructuralChanges(em, ref ecb);
                if (EnableInitialSpawnDiagnostics)
                    LogInitialSpawnState(ref state, completedInitialSpawn ? "completed" : "progress", DiagnosticIntervalFrames, ref _diagnosticLogWriter);
                if (EnableInitialSpawnDiagnostics && completedInitialSpawn)
                    LogInitialSpawnCellDuplicates(ref state, grid, ref _diagnosticLogWriter);
                completedForLog = completedInitialSpawn;
                factionSpawns.Dispose();
                gridContext.Dispose();
            }

            LogInitialSpawnFreezeIfExceeded(
                state.EntityManager,
                startTime,
                spawnedUnitsForLog,
                spawnedBlockersForLog,
                completedForLog,
                ref _diagnosticLogWriter);
        }

        private readonly struct InitialSpawnStartupGateResult
        {
            public readonly bool IsActionable;
            public readonly Entity BoundaryEntity;

            private InitialSpawnStartupGateResult(bool isActionable, Entity boundaryEntity)
            {
                IsActionable = isActionable;
                BoundaryEntity = boundaryEntity;
            }

            public static InitialSpawnStartupGateResult NotActionable()
            {
                return new InitialSpawnStartupGateResult(false, Entity.Null);
            }

            public static InitialSpawnStartupGateResult Actionable(Entity boundaryEntity)
            {
                return new InitialSpawnStartupGateResult(true, boundaryEntity);
            }
        }

        private static InitialSpawnStartupGateResult EvaluateInitialSpawnStartupGate(
            EntityManager em,
            EntityQuery runtimeGameplayStateQuery,
            EntityQuery buildingRuntimeBoundaryQuery)
        {
            RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(
                runtimeGameplayStateQuery.GetSingletonEntity());
            if (runtimeState.PlayRequested == 0)
                return InitialSpawnStartupGateResult.NotActionable();

            Entity boundaryEntity = TryGetBuildingRuntimeStateEntity(em, buildingRuntimeBoundaryQuery, out Entity foundBoundaryEntity)
                ? foundBoundaryEntity
                : Entity.Null;

            return InitialSpawnStartupGateResult.Actionable(boundaryEntity);
        }

        private static bool TryGetBuildingRuntimeStateEntity(
            EntityManager em,
            EntityQuery buildingRuntimeBoundaryQuery,
            out Entity entity)
        {
            entity = Entity.Null;
            if (buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
                return false;

            entity = buildingRuntimeBoundaryQuery.GetSingletonEntity();
            return entity != Entity.Null && em.Exists(entity);
        }

        private static NativeArray<InitialUnitsFactionSpawnEntry> CreateInitialFactionSpawnSnapshot(
            EntityManager em,
            Entity configEntity,
            Allocator allocator)
        {
            DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawnsBuffer = em.GetBuffer<InitialUnitsFactionSpawnEntry>(configEntity);
            var factionSpawns = new NativeArray<InitialUnitsFactionSpawnEntry>(factionSpawnsBuffer.Length, allocator);
            for (int factionIndex = 0; factionIndex < factionSpawnsBuffer.Length; factionIndex++)
                factionSpawns[factionIndex] = factionSpawnsBuffer[factionIndex];

            return factionSpawns;
        }

        private static RespawnQueueComponent ProjectInitialRespawnQueueConfig(
            EntityManager em,
            Entity queueEntity,
            InitialUnitsSpawnConfig config,
            NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns)
        {
            RespawnQueueComponent queueState = em.GetComponentData<RespawnQueueComponent>(queueEntity);
            queueState.SpawnRadiusCells = math.max(0, config.SpawnRadiusCells);
            queueState.RespawnDelaySeconds = math.max(0.01f, config.RespawnDelaySeconds);

            DynamicBuffer<RespawnFactionSpawnPoint> respawnSpawnPoints = em.GetBuffer<RespawnFactionSpawnPoint>(queueEntity);
            respawnSpawnPoints.Clear();
            for (int factionIndex = 0; factionIndex < factionSpawns.Length; factionIndex++)
            {
                respawnSpawnPoints.Add(new RespawnFactionSpawnPoint
                {
                    FactionId = factionSpawns[factionIndex].FactionId,
                    SpawnCell = factionSpawns[factionIndex].SpawnCell
                });
            }

            return queueState;
        }

        private static void WriteInitialRespawnQueueRandomState(
            EntityManager em,
            Entity queueEntity,
            RespawnQueueComponent queueState,
            uint randomState)
        {
            queueState.RandomState = randomState;
            em.SetComponentData(queueEntity, queueState);
        }

        internal static bool TryGetInitialAirPlatformSpawn(
            EntityManager em,
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
                !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
                return false;

            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
                em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
            bool useHelipad = configuredSpawnOffset.y <= -45;
            string buildingId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(useHelipad ? "Building_Helipad" : "Building_Airport");
            int remainingSlotIndex = ResolveInitialAirPlatformSlotIndex(configuredSpawnOffset, useHelipad);
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

        internal readonly struct InitialBlockerSpawnResult
        {
            public readonly int TargetCount;
            public readonly int ProgressIncrement;
            public readonly int SpawnedForLog;

            public InitialBlockerSpawnResult(int targetCount, int progressIncrement, int spawnedForLog)
            {
                TargetCount = targetCount;
                ProgressIncrement = progressIncrement;
                SpawnedForLog = spawnedForLog;
            }
        }

        internal static InitialBlockerSpawnResult SpawnInitialBlockerBatch(
            ref Unity.Mathematics.Random rng,
            EntityManager em,
            EntityCommandBuffer ecb,
            InitialUnitsSpawnConfig config,
            int initialBlockerBatchSize,
            int blockersSpawned,
            GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray dynamicBlocked,
            NativeBitArray occupied,
            ref NativeBitArray reserved,
            bool enableDiagnostics,
            ref InitialSpawnDiagnosticLogWriter diagnosticLogWriter)
        {
            int blockerTargetCount = config.BlockerCount;
            int blockersToSpawn = math.min(initialBlockerBatchSize, math.max(0, blockerTargetCount - blockersSpawned));
            int spawnedForLog = 0;

            for (int i = 0; i < blockersToSpawn; i++)
            {
                if (config.BlockerPrefab == Entity.Null)
                    break;

                int2 center = new int2(grid.Width / 2, grid.Height / 2);
                int radius = math.max(0, config.SpawnRadiusCells) + 20;
                if (!SpawnCellUtility.TryFindSpawnCellNear(ref rng, grid, walkable, dynamicBlocked, occupied, ref reserved, center, radius, new int2(1, 1), out int2 cell))
                {
                    if (enableDiagnostics)
                        diagnosticLogWriter.EnqueueWarning(em, $"[InitialSpawn] no-free-blocker-cell center={center} radius={radius}");
                    break;
                }

                Entity instance = ecb.Instantiate(config.BlockerPrefab);
                ecb.SetComponent(instance, new UnitGrid { Cell = cell });
                ecb.SetComponent(instance, LocalTransform.FromPosition(GridUtils.CellToWorldCenter(grid, cell)));
                spawnedForLog++;
            }

            return new InitialBlockerSpawnResult(blockerTargetCount, blockersToSpawn, spawnedForLog);
        }

        private static double BeginInitialSpawnFrame()
        {
            return Time.realtimeSinceStartupAsDouble;
        }

        private static void PlaybackAndDisposeInitialSpawnStructuralChanges(EntityManager em, ref EntityCommandBuffer ecb)
        {
            ecb.Playback(em);
            ecb.Dispose();
        }

        private void LogInitialSpawnState(
            ref SystemState state,
            string reason,
            int diagnosticIntervalFrames,
            ref InitialSpawnDiagnosticLogWriter diagnosticLogWriter)
        {
            if (Time.frameCount < _nextDiagnosticFrame)
                return;

            _nextDiagnosticFrame = Time.frameCount + diagnosticIntervalFrames;
            EntityManager em = state.EntityManager;
            EntityQuery configQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            EntityQuery progressQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
                ComponentType.ReadOnly<InitialUnitsSpawnProgress>());
            EntityQuery initializedQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
                ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());
            EntityQuery unitGridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>());
            EntityQuery blockerDependencyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGridBlockerDependencyComponent>());
            byte depsReady = 1;
            string blockerDependencyStatus = "no-blocker-state";
            if (!blockerDependencyQuery.IsEmptyIgnoreFilter)
            {
                RuntimeGridBlockerDependencyComponent blockerState = em.GetComponentData<RuntimeGridBlockerDependencyComponent>(blockerDependencyQuery.GetSingletonEntity());
                depsReady = blockerState.ReadyForDependents;
                blockerDependencyStatus = $"ready={blockerState.ReadyForDependents} spawnOnStart={blockerState.SpawnOnStart} spawned={blockerState.Spawned} finalizing={blockerState.SpawnFinalizing} finalizeAfter={blockerState.FinalizeAfterFrames} pendingCity={blockerState.PendingCity} cityHasSpawned={blockerState.CityHasSpawned} cityGenerating={blockerState.CityGenerating}";
            }

            diagnosticLogWriter.EnqueueLog(em, $"[InitialSpawnState] frame={Time.frameCount} reason={reason} configs={configQuery.CalculateEntityCount()} progress={progressQuery.CalculateEntityCount()} initialized={initializedQuery.CalculateEntityCount()} unitGrid={unitGridQuery.CalculateEntityCount()} depsReady={depsReady} {blockerDependencyStatus}");

            configQuery.Dispose();
            progressQuery.Dispose();
            initializedQuery.Dispose();
            unitGridQuery.Dispose();
            blockerDependencyQuery.Dispose();
        }

        private static void LogInitialSpawnCellDuplicates(
            ref SystemState state,
            in GridConfig grid,
            ref InitialSpawnDiagnosticLogWriter diagnosticLogWriter)
        {
            using var query = state.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>());
            EntityManager em = state.EntityManager;
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            ComponentTypeHandle<UnitGrid> unitGridType = em.GetComponentTypeHandle<UnitGrid>(true);
            ComponentTypeHandle<UnitFootprint> footprintType = em.GetComponentTypeHandle<UnitFootprint>(true);
            ComponentLookup<StaticGridBlocker> staticGridBlockers = state.GetComponentLookup<StaticGridBlocker>(true);
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            int entityCount = query.CalculateEntityCount();
            using var occupiedCells = new NativeHashSet<int>(math.max(1024, entityCount * 32), Allocator.Temp);
            using var centers = new NativeHashSet<int>(math.max(1, entityCount), Allocator.Temp);
            int duplicateCells = 0;
            int duplicateCenters = 0;
            int occupiedFootprintCells = 0;
            int countedEntities = 0;
            string samples = string.Empty;
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<UnitGrid> unitGrids = chunk.GetNativeArray(ref unitGridType);
                NativeArray<UnitFootprint> footprints = chunk.GetNativeArray(ref footprintType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity unit = entities[i];
                    if (staticGridBlockers.HasComponent(unit))
                        continue;

                    countedEntities++;
                    int2 cell = unitGrids[i].Cell;
                    int2 size = footprints[i].Size;
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
            }

            diagnosticLogWriter.EnqueueLog(em, $"[InitialSpawnDiag] entities={countedEntities} occupiedCells={occupiedFootprintCells} duplicateCenters={duplicateCenters} duplicateFootprintCells={duplicateCells} samples={samples}");
        }

        private static void LogInitialSpawnFreezeIfExceeded(
            EntityManager em,
            double startTime,
            int spawnedUnitsForLog,
            int spawnedBlockersForLog,
            bool completedForLog,
            ref InitialSpawnDiagnosticLogWriter diagnosticLogWriter)
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
            if (!EnableInitialSpawnFreezeLogs || elapsed < FreezeLogThresholdSeconds)
                return;

            diagnosticLogWriter.EnqueueLog(em, $"[FreezeDetect:ECS] InitialUnitsSpawnSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms units={spawnedUnitsForLog} blockers={spawnedBlockersForLog} completed={(completedForLog ? 1 : 0)}");
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
                    if (!FactionIdentity.IsPlayerControlled(economy.FactionId))
                        continue;

                    economy.Money = math.max(0, config.InitialDollars);
                    economies[i] = economy;
                    return;
                }
            }

            Entity economyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
            em.SetComponentData(economyEntity, new FactionEconomy
            {
                FactionId = FactionIdentity.PlayerFactionId,
                Money = math.max(0, config.InitialDollars)
            });
            em.SetComponentData(economyEntity, new FactionEconomyPolicy
            {
                Enabled = 0,
                IncomeMultiplier = 1f
            });
        }

        internal static void InitializeInitialSpawnProgress(EntityManager em, EntityQuery pendingInitQuery)
        {
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = pendingInitQuery.ToArchetypeChunkArray(Allocator.Temp);
            using var initEntities = new NativeList<Entity>(pendingInitQuery.CalculateEntityCount(), Allocator.Temp);
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

        internal readonly struct InitialUnitSpawnEntryBatch
        {
            public readonly int UnitIndex;
            public readonly InitialUnitsFactionUnitSpawnEntry UnitSpawn;
            public readonly InitialUnitsFactionUnitSpawnProgress EntryProgress;
            public readonly int ToSpawn;
            public readonly bool HasPrefab;

            public InitialUnitSpawnEntryBatch(
                int unitIndex,
                InitialUnitsFactionUnitSpawnEntry unitSpawn,
                InitialUnitsFactionUnitSpawnProgress entryProgress,
                int toSpawn,
                bool hasPrefab)
            {
                UnitIndex = unitIndex;
                UnitSpawn = unitSpawn;
                EntryProgress = entryProgress;
                ToSpawn = toSpawn;
                HasPrefab = hasPrefab;
            }
        }

        private readonly struct InitialUnitSpawnPlan
        {
            public readonly int2 UnitSpawnCenter;
            public readonly int2 FootprintSize;
            public readonly bool IsAirUnit;

            public InitialUnitSpawnPlan(int2 unitSpawnCenter, int2 footprintSize, bool isAirUnit)
            {
                UnitSpawnCenter = unitSpawnCenter;
                FootprintSize = footprintSize;
                IsAirUnit = isAirUnit;
            }
        }

        internal static bool TryGetCustomGameUnitSourceKey(
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

        internal static bool TrySkipMissingPrefabUnit(
            EntityManager em,
            InitialUnitsFactionUnitSpawnEntry unitSpawn,
            bool hasPrefab,
            bool hasSourceKey,
            FixedString64Bytes sourceKey,
            ref InitialUnitsFactionUnitSpawnProgress entryProgress,
            ref InitialSpawnDiagnosticLogWriter diagnosticLogWriter)
        {
            if (hasPrefab)
                return false;

            if (hasSourceKey)
                diagnosticLogWriter.EnqueueWarning(em, $"[InitialSpawn] skipped source-key unit because no ECS prefab entity was resolved. sourceKey={sourceKey.ToString()} faction={unitSpawn.FactionId} count={unitSpawn.Count}");

            entryProgress.Spawned = unitSpawn.Count;
            return true;
        }

        internal static bool TryCreateInitialUnitSpawnEntryBatch(
            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns,
            DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> unitProgress,
            int unitIndex,
            int remainingBatch,
            out InitialUnitSpawnEntryBatch batch)
        {
            InitialUnitsFactionUnitSpawnEntry unitSpawn = unitSpawns[unitIndex];
            InitialUnitsFactionUnitSpawnProgress entryProgress = unitProgress[unitIndex];
            int remaining = math.max(0, unitSpawn.Count - entryProgress.Spawned);
            int toSpawn = math.min(remainingBatch, remaining);
            bool hasPrefab = unitSpawn.Prefab != Entity.Null;
            batch = new InitialUnitSpawnEntryBatch(unitIndex, unitSpawn, entryProgress, toSpawn, hasPrefab);
            return toSpawn > 0;
        }

        private static bool TryCreateInitialUnitSpawnPlan(
            EntityManager em,
            NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns,
            in InitialUnitSpawnEntryBatch batch,
            out InitialUnitSpawnPlan plan)
        {
            if (!TryGetFactionSpawnCell(factionSpawns, batch.UnitSpawn.FactionId, out int2 factionSpawnCell))
            {
                plan = default;
                return false;
            }

            int2 unitSpawnCenter = factionSpawnCell + batch.UnitSpawn.SpawnOffset;
            int2 footprintSize = batch.HasPrefab && em.HasComponent<UnitFootprint>(batch.UnitSpawn.Prefab)
                ? em.GetComponentData<UnitFootprint>(batch.UnitSpawn.Prefab).Size
                : new int2(1, 1);
            bool isAirUnit = batch.HasPrefab && em.HasComponent<UnitAirMovement>(batch.UnitSpawn.Prefab);
            plan = new InitialUnitSpawnPlan(unitSpawnCenter, footprintSize, isAirUnit);
            return true;
        }

        internal static void ApplyInitialUnitSpawnedCount(
            DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> unitProgress,
            in InitialUnitSpawnEntryBatch batch,
            int spawnedThisEntry,
            ref int remainingBatch)
        {
            InitialUnitsFactionUnitSpawnProgress entryProgress = batch.EntryProgress;
            entryProgress.Spawned += spawnedThisEntry;
            unitProgress[batch.UnitIndex] = entryProgress;
            remainingBatch -= spawnedThisEntry;
        }

        private static bool TryGetFactionSpawnCell(
            NativeArray<InitialUnitsFactionSpawnEntry> spawns,
            byte factionId,
            out int2 spawnCell)
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

        internal static bool EnqueueConfiguredInitialBuildingRequests(
            EntityManager em,
            Entity boundaryEntity,
            Entity configEntity,
            NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns,
            ref InitialSpawnDiagnosticLogWriter diagnosticLogWriter,
            out int requestCount)
        {
            requestCount = 0;
            if (boundaryEntity == Entity.Null)
                return false;

            DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawnsBuffer =
                em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity);
            for (int buildingIndex = 0; buildingIndex < buildingSpawnsBuffer.Length; buildingIndex++)
            {
                InitialUnitsFactionBuildingSpawnEntry building = buildingSpawnsBuffer[buildingIndex];
                string buildingId = building.PrefabLookupKey.ToString();
                if (string.IsNullOrWhiteSpace(buildingId))
                    continue;

                if (!TryGetFactionSpawnCell(factionSpawns, building.FactionId, out int2 factionSpawnCell))
                {
                    diagnosticLogWriter.EnqueueWarning(em, $"[InitialSpawn] skipping initial building entry with no faction spawn. faction={building.FactionId} buildingId={buildingId}");
                    continue;
                }

                int2 origin = factionSpawnCell + building.OriginOffset;
                EnqueueConfiguredInitialBuildingSpawnRequest(
                    em,
                    boundaryEntity,
                    configEntity,
                    building.FactionId,
                    buildingId,
                    origin);
                requestCount++;
            }

            return true;
        }

        private static void EnqueueConfiguredInitialBuildingSpawnRequest(
            EntityManager em,
            Entity boundaryEntity,
            Entity configEntity,
            byte factionId,
            string buildingId,
            int2 origin)
        {
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
            requests.Add(new BuildingRuntimeSpawnRequest
            {
                RequestId = requests.Length + 1,
                RequestKind = BuildingRuntimeSpawnRequest.KindBuilding,
                FactionId = factionId,
                HasOwnerFaction = 1,
                BuildingId = new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId)),
                PreferredOrigin = origin,
                EndOrigin = default,
                RotateVertical = 0,
                AllowExistingWallOverlap = 0,
                Status = BuildingRuntimeSpawnRequest.Pending,
                PlanEntity = configEntity,
                EntryIndex = 0
            });
        }

        internal static bool EnqueueInitialFactionBaseRequests(
            EntityManager em,
            Entity boundaryEntity,
            Entity configEntity,
            InitialUnitsSpawnConfig config,
            NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns,
            GridConfig grid,
            int initialBaseCoreRequestEntryIndex,
            out int requestCount)
        {
            requestCount = 0;
            if (boundaryEntity == Entity.Null)
                return false;

            if (!TryResolveInitialFactionBaseSpawnableId(em, boundaryEntity, config.BaseWallPrefabLookupKey, "Wall_Dirt_Straight", out string wallId, out BuildingConfiguredSpawnableReadModel wallModel) &&
                !TryResolveInitialFactionBaseSpawnableId(em, boundaryEntity, config.BaseWallPrefabLookupKey, "Wall_Fence_Straight", out wallId, out wallModel))
                return false;
            if (!TryResolveInitialFactionBaseSpawnableId(em, boundaryEntity, config.BaseGatePrefabLookupKey, "Building_Road_Barrier", out string gateId, out BuildingConfiguredSpawnableReadModel gateModel))
                return false;
            if (!TryResolveInitialFactionBaseSpawnableId(em, boundaryEntity, config.BaseCoreBuildingPrefabLookupKey, "Building_Ammunition_Depot", out _, out _))
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

                if (!TryResolveInitialFactionBaseSpawnableId(em, boundaryEntity, new FixedString128Bytes(placement.PrefabKey), placement.PrefabKey, out string resolvedId, out BuildingConfiguredSpawnableReadModel resolvedModel))
                {
                    if (placement.Kind == InitialFactionBasePlacementKind.CoreBuilding)
                        return false;

                    continue;
                }

                placementIds.Add(placement.PrefabKey, resolvedId);
                placementModels.Add(placement.PrefabKey, resolvedModel);
            }

            Vector2Int bottomGateFootprint = ToInitialFactionBaseFootprint(gateModel.FootprintCells, false);
            Vector2Int sideGateFootprint = ToInitialFactionBaseFootprint(gateModel.FootprintCells, true);
            Vector2Int bottomWallFootprint = ToInitialFactionBaseFootprint(wallModel.FootprintCells, false);
            Vector2Int sideWallFootprint = ToInitialFactionBaseFootprint(wallModel.FootprintCells, true);
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

            for (int factionIndex = 0; factionIndex < factionSpawns.Length; factionIndex++)
            {
                InitialUnitsFactionSpawnEntry factionSpawn = factionSpawns[factionIndex];
                Vector2Int anchor = new(factionSpawn.SpawnCell.x, factionSpawn.SpawnCell.y);
                for (int wallRunIndex = 0; wallRunIndex < wallRuns.Count; wallRunIndex++)
                {
                    InitialFactionBaseWallRun run = wallRuns[wallRunIndex];
                    requestCount += EnqueueInitialFactionBaseWallRunRequests(
                        em,
                        boundaryEntity,
                        configEntity,
                        factionSpawn.FactionId,
                        wallId,
                        new int2(anchor.x + run.StartOffset.x, anchor.y + run.StartOffset.y),
                        new int2(anchor.x + run.EndOffset.x, anchor.y + run.EndOffset.y),
                        bottomWallFootprint,
                        sideWallFootprint,
                        grid);
                }

                for (int flankIndex = 0; flankIndex < gateFlankWalls.Count; flankIndex++)
                {
                    InitialFactionBaseGateFlankWall flank = gateFlankWalls[flankIndex];
                    Vector2Int origin = anchor + flank.OriginOffset;
                    Vector2Int footprint = flank.RotateVertical ? sideWallFootprint : bottomWallFootprint;
                    if (!InitialFactionBaseLayoutPlanner.IsFootprintInsideGrid(origin, footprint, grid.Width, grid.Height))
                        continue;

                    EnqueueInitialFactionBaseBuildingSpawnRequest(
                        em,
                        boundaryEntity,
                        configEntity,
                        factionSpawn.FactionId,
                        wallId,
                        new int2(origin.x, origin.y),
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

                    Vector2Int footprint = ToInitialFactionBaseFootprint(model.FootprintCells, placement.RotateVertical);
                    Vector2Int origin = InitialFactionBaseLayoutPlanner.ResolvePlacementOrigin(anchor, placement, footprint);
                    if (!InitialFactionBaseLayoutPlanner.IsFootprintInsideGrid(origin, footprint, grid.Width, grid.Height))
                        continue;

                    EnqueueInitialFactionBaseBuildingSpawnRequest(
                        em,
                        boundaryEntity,
                        configEntity,
                        factionSpawn.FactionId,
                        buildingId,
                        new int2(origin.x, origin.y),
                        placement.RotateVertical,
                        BuildingRuntimeSpawnRequest.KindBuilding,
                        default,
                        false,
                        FactionIdentity.IsPlayerControlled(factionSpawn.FactionId) && placement.Kind == InitialFactionBasePlacementKind.CoreBuilding
                            ? initialBaseCoreRequestEntryIndex
                            : 0);
                    requestCount++;
                }
            }

            return true;
        }

        private static int EnqueueInitialFactionBaseWallRunRequests(
            EntityManager em,
            Entity boundaryEntity,
            Entity configEntity,
            byte factionId,
            string wallId,
            int2 startOrigin,
            int2 endOrigin,
            Vector2Int bottomWallFootprint,
            Vector2Int sideWallFootprint,
            GridConfig grid)
        {
            Vector2Int start = new(startOrigin.x, startOrigin.y);
            Vector2Int end = new(endOrigin.x, endOrigin.y);
            bool vertical = Mathf.Abs(end.y - start.y) > Mathf.Abs(end.x - start.x);
            if (vertical)
                end.x = start.x;
            else
                end.y = start.y;

            Vector2Int footprint = vertical ? sideWallFootprint : bottomWallFootprint;
            List<Vector2Int> origins = BuildingPlacementCommitCompositionSystemHelper.BuildWallRunOrigins(start, end, footprint, vertical);
            int enqueued = 0;
            for (int i = 0; i < origins.Count; i++)
            {
                Vector2Int origin = origins[i];
                if (!InitialFactionBaseLayoutPlanner.IsFootprintInsideGrid(origin, footprint, grid.Width, grid.Height))
                    continue;

                EnqueueInitialFactionBaseBuildingSpawnRequest(
                    em,
                    boundaryEntity,
                    configEntity,
                    factionId,
                    wallId,
                    new int2(origin.x, origin.y),
                    vertical,
                    BuildingRuntimeSpawnRequest.KindWallSegment);
                enqueued++;
            }

            return enqueued;
        }

        private static bool TryResolveInitialFactionBaseSpawnableId(
            EntityManager em,
            Entity boundaryEntity,
            FixedString128Bytes configuredKey,
            string fallbackKey,
            out string buildingId,
            out BuildingConfiguredSpawnableReadModel model)
        {
            model = default;
            buildingId = configuredKey.ToString();
            if (!string.IsNullOrWhiteSpace(buildingId) &&
                TryResolveInitialFactionBaseSpawnableReadModel(em, boundaryEntity, buildingId, out model))
            {
                return true;
            }

            buildingId = fallbackKey;
            return !string.IsNullOrWhiteSpace(buildingId) &&
                   TryResolveInitialFactionBaseSpawnableReadModel(em, boundaryEntity, buildingId, out model);
        }

        private static bool TryResolveInitialFactionBaseSpawnableReadModel(
            EntityManager em,
            Entity boundaryEntity,
            string buildingId,
            out BuildingConfiguredSpawnableReadModel model)
        {
            model = default;
            if (boundaryEntity == Entity.Null ||
                !em.HasBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity))
                return false;

            DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables =
                em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity, true);
            string normalized = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId);
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

        private static void EnqueueInitialFactionBaseBuildingSpawnRequest(
            EntityManager em,
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
                em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
            requests.Add(new BuildingRuntimeSpawnRequest
            {
                RequestId = requests.Length + 1,
                RequestKind = requestKind,
                FactionId = factionId,
                HasOwnerFaction = 1,
                BuildingId = new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId)),
                PreferredOrigin = origin,
                EndOrigin = endOrigin,
                RotateVertical = rotateVertical ? (byte)1 : (byte)0,
                AllowExistingWallOverlap = allowExistingWallOverlap ? (byte)1 : (byte)0,
                Status = BuildingRuntimeSpawnRequest.Pending,
                PlanEntity = configEntity,
                EntryIndex = entryIndex
            });
        }

        private static Vector2Int ToInitialFactionBaseFootprint(int2 footprint, bool rotateVertical)
        {
            int x = math.max(1, footprint.x);
            int y = math.max(1, footprint.y);
            return rotateVertical ? new Vector2Int(y, x) : new Vector2Int(x, y);
        }

        private static bool ProcessInitialBuildingCompletion(
            EntityManager em,
            Entity boundaryEntity,
            Entity configEntity,
            GridConfig grid,
            int initialBaseCoreRequestEntryIndex,
            ref InitialSpawnDiagnosticLogWriter diagnosticLogWriter)
        {
            if (boundaryEntity == Entity.Null ||
                !em.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
                return false;

            DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
            bool sawRequest = false;
            bool hasPending = false;
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

                if (request.Status == BuildingRuntimeSpawnRequest.Succeeded &&
                    FactionIdentity.IsPlayerControlled(request.FactionId) &&
                    request.EntryIndex == initialBaseCoreRequestEntryIndex)
                {
                    Vector3 coreFocus = GetInitialBuildingFootprintCenterWorld(
                        new Vector2Int(request.ActualOrigin.x, request.ActualOrigin.y),
                        new Vector2Int(request.ActualFootprint.x, request.ActualFootprint.y),
                        grid);
                    InitialUnitsRuntimeState.InitialCameraFocusWorld = coreFocus;
                    InitialUnitsRuntimeState.InitialCameraFocusRequested = true;
                }
                else if (request.Status == BuildingRuntimeSpawnRequest.Failed)
                {
                    diagnosticLogWriter.EnqueueWarning(
                        em,
                        $"[InitialSpawn] initial building request failed. faction={request.FactionId} buildingId={request.BuildingId.ToString()} result={DescribeRuntimeSpawnRequestResult(request.ResultCode)} origin=({request.PreferredOrigin.x},{request.PreferredOrigin.y})");
                }

                requests.RemoveAt(i);
            }

            if (hasPending)
                return false;

            return sawRequest;
        }

        private static string DescribeRuntimeSpawnRequestResult(byte resultCode)
        {
            return resultCode switch
            {
                BuildingRuntimeSpawnRequest.MissingConfig => "MissingConfig",
                BuildingRuntimeSpawnRequest.Blocked => "Blocked",
                _ => $"Unknown({resultCode})"
            };
        }

        private static Vector3 GetInitialBuildingFootprintCenterWorld(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
        {
            return new Vector3(
                grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
                grid.Origin.y,
                grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
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
            ref InitialSpawnDiagnosticLogWriter diagnosticLogWriter,
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
                int warningInterval = math.max(1, maxInitialBuildingCompletionWaitFrames);
                if (progress.InitialBuildingCompletionWaitFrames == 1 ||
                    progress.InitialBuildingCompletionWaitFrames % warningInterval == 0)
                {
                    diagnosticLogWriter.EnqueueWarning(
                        em,
                        $"[InitialSpawn] waiting initial building completion frames={progress.InitialBuildingCompletionWaitFrames} requiresFactionBases={(config.CreateFactionBases != 0 ? 1 : 0)} configuredBuildings={(em.HasBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity) ? em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(configEntity).Length : 0)}. The startup loading gate remains blocked until the building boundary confirms completion.");
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
}
