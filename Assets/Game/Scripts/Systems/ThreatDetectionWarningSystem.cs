using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    [UpdateAfter(typeof(UnitGridMovementSystem))]
    [UpdateAfter(typeof(UnitAirMovementSystem))]
    public partial struct ThreatDetectionWarningSystem : ISystem
    {
        private const byte PlayerFactionId = FactionIdentity.PlayerFactionId;
        private const float FallbackThreatSpeed = 5f;
        private const int CloseContactWarningRadiusCells = 12;
        private const double ScanIntervalSeconds = 0.2d;
        private const int ScanEveryFrameTargetThreshold = 32;

        private NativeParallelHashSet<Entity> _previousGroundThreats;
        private NativeParallelHashSet<Entity> _previousAirThreats;
        private double _nextScanTime;
        private EntityQuery _sensorQuery;
        private EntityQuery _targetQuery;
        private EntityQuery _gridQuery;
        private EntityQuery _warningStateQuery;
        private EntityTypeHandle _entityType;
        private ComponentTypeHandle<ThreatDetector> _detectorType;
        private ComponentTypeHandle<Faction> _factionType;
        private ComponentTypeHandle<UnitGrid> _gridType;
        private ComponentTypeHandle<UnitHealth> _healthType;
        private ComponentLookup<RuntimeBuildingCombatTag> _buildingLookup;
        private ComponentLookup<UnitAirMovement> _airLookup;
        private ComponentLookup<UnitMovementBehavior> _movementBehaviorLookup;
        private ComponentLookup<UnitTarget> _targetLookup;
        private ComponentLookup<UnitPathRequest> _pathRequestLookup;
        private ComponentLookup<UnitLongDistanceMove> _longDistanceMoveLookup;
        private ComponentLookup<EngageTarget> _engageTargetLookup;
        private ComponentLookup<BaseBreachOrder> _baseBreachOrderLookup;
        private ComponentLookup<UnitMove> _unitMoveLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
            _previousGroundThreats = new NativeParallelHashSet<Entity>(64, Allocator.Persistent);
            _previousAirThreats = new NativeParallelHashSet<Entity>(64, Allocator.Persistent);
            _sensorQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ThreatDetector>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitHealth>());
            _targetQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitHealth>());
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            _warningStateQuery = state.GetEntityQuery(ComponentType.ReadWrite<ThreatWarningRuntimeStateComponent>());
            ThreatWarningRuntimeState.EnsureSingleton(state.EntityManager, _warningStateQuery);
            _entityType = state.GetEntityTypeHandle();
            _detectorType = state.GetComponentTypeHandle<ThreatDetector>(true);
            _factionType = state.GetComponentTypeHandle<Faction>(true);
            _gridType = state.GetComponentTypeHandle<UnitGrid>(true);
            _healthType = state.GetComponentTypeHandle<UnitHealth>(true);
            _buildingLookup = state.GetComponentLookup<RuntimeBuildingCombatTag>(true);
            _airLookup = state.GetComponentLookup<UnitAirMovement>(true);
            _movementBehaviorLookup = state.GetComponentLookup<UnitMovementBehavior>(true);
            _targetLookup = state.GetComponentLookup<UnitTarget>(true);
            _pathRequestLookup = state.GetComponentLookup<UnitPathRequest>(true);
            _longDistanceMoveLookup = state.GetComponentLookup<UnitLongDistanceMove>(true);
            _engageTargetLookup = state.GetComponentLookup<EngageTarget>(true);
            _baseBreachOrderLookup = state.GetComponentLookup<BaseBreachOrder>(true);
            _unitMoveLookup = state.GetComponentLookup<UnitMove>(true);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_previousGroundThreats.IsCreated)
                _previousGroundThreats.Dispose();
            if (_previousAirThreats.IsCreated)
                _previousAirThreats.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.CompleteDependencyBeforeRO<RuntimeGameplayStateComponent>();
            RuntimeGameplayStateComponent gameplayState = SystemAPI.GetSingleton<RuntimeGameplayStateComponent>();
            if (gameplayState.PlayRequested == 0 || gameplayState.SimulationActive == 0)
            {
                _nextScanTime = 0d;
                ClearPreviousThreats();
                ThreatWarningRuntimeState.Reset(state.EntityManager, _warningStateQuery);
                return;
            }

            int targetCount = _targetQuery.CalculateEntityCount();
            if (ShouldSkipScan(targetCount, SystemAPI.Time.ElapsedTime))
                return;

            CompleteMainThreadReadDependencies(ref state);

            float cellSize = TryGetCellSize(state.EntityManager, _gridQuery);

            int targetCapacity = math.max(16, targetCount * 2);
            using NativeParallelHashSet<Entity> currentGroundThreats = new(targetCapacity, Allocator.TempJob);
            using NativeParallelHashSet<Entity> currentAirThreats = new(targetCapacity, Allocator.TempJob);
            using NativeList<Entity> currentGroundThreatList = new(Allocator.TempJob);
            using NativeList<Entity> currentAirThreatList = new(Allocator.TempJob);
            using NativeList<ThreatCandidate> threatCandidates = new(math.max(1, targetCount), Allocator.TempJob);
            using NativeArray<ThreatScanResult> result = new(1, Allocator.TempJob);

            UpdateTypeHandles(ref state);
            using NativeArray<ArchetypeChunk> sensorChunks = _sensorQuery.ToArchetypeChunkArray(Allocator.TempJob);
            using NativeArray<ArchetypeChunk> targetChunks = _targetQuery.ToArchetypeChunkArray(Allocator.TempJob);

            JobHandle threatScanHandle = new ThreatScanJob
            {
                SensorChunks = sensorChunks,
                TargetChunks = targetChunks,
                EntityType = _entityType,
                DetectorType = _detectorType,
                FactionType = _factionType,
                GridType = _gridType,
                HealthType = _healthType,
                TargetLookups = new ThreatTargetLookups
                {
                    BuildingLookup = _buildingLookup,
                    AirLookup = _airLookup,
                    MovementBehaviorLookup = _movementBehaviorLookup,
                    TargetLookup = _targetLookup,
                    PathRequestLookup = _pathRequestLookup,
                    LongDistanceMoveLookup = _longDistanceMoveLookup,
                    EngageTargetLookup = _engageTargetLookup,
                    BaseBreachOrderLookup = _baseBreachOrderLookup,
                    UnitMoveLookup = _unitMoveLookup
                },
                PreviousGroundThreats = _previousGroundThreats,
                PreviousAirThreats = _previousAirThreats,
                CurrentGroundThreats = currentGroundThreats,
                CurrentAirThreats = currentAirThreats,
                CurrentGroundThreatList = currentGroundThreatList,
                CurrentAirThreatList = currentAirThreatList,
                ThreatCandidates = threatCandidates,
                CellSize = cellSize,
                Result = result
            }.Schedule(state.Dependency);
            threatScanHandle.Complete();

            ReplacePreviousThreats(_previousGroundThreats, currentGroundThreatList);
            ReplacePreviousThreats(_previousAirThreats, currentAirThreatList);

            ThreatScanResult scan = result[0];
            if (scan.HasNewGroundThreat != 0 && (scan.HasNewAirThreat == 0 || scan.BestGroundEtaSeconds <= scan.BestAirEtaSeconds))
            {
                float etaSeconds = scan.BestGroundEtaSeconds == float.MaxValue ? 0f : scan.BestGroundEtaSeconds;
                int threatCount = currentGroundThreatList.Length;
                ThreatWarningRuntimeState.RequestWarning(
                    state.EntityManager,
                    _warningStateQuery,
                    ThreatWarningType.Ground,
                    etaSeconds,
                    threatCount);
                ThreatWarningAudioEventUtility.TryEmit(
                    state.EntityManager,
                    ThreatWarningType.Ground,
                    etaSeconds,
                    threatCount,
                    (float)SystemAPI.Time.ElapsedTime);
            }
            else if (scan.HasNewAirThreat != 0)
            {
                float etaSeconds = scan.BestAirEtaSeconds == float.MaxValue ? 0f : scan.BestAirEtaSeconds;
                int threatCount = currentAirThreatList.Length;
                ThreatWarningRuntimeState.RequestWarning(
                    state.EntityManager,
                    _warningStateQuery,
                    ThreatWarningType.Air,
                    etaSeconds,
                    threatCount);
                ThreatWarningAudioEventUtility.TryEmit(
                    state.EntityManager,
                    ThreatWarningType.Air,
                    etaSeconds,
                    threatCount,
                    (float)SystemAPI.Time.ElapsedTime);
            }
        }

        private bool ShouldSkipScan(int targetCount, double now)
        {
            if (targetCount <= ScanEveryFrameTargetThreshold)
            {
                _nextScanTime = now;
                return false;
            }

            if (now < _nextScanTime)
                return true;

            _nextScanTime = now + ScanIntervalSeconds;
            return false;
        }

        private static void CompleteMainThreadReadDependencies(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            em.CompleteDependencyBeforeRO<GridConfig>();
            em.CompleteDependencyBeforeRO<ThreatDetector>();
            em.CompleteDependencyBeforeRO<Faction>();
            em.CompleteDependencyBeforeRO<UnitGrid>();
            em.CompleteDependencyBeforeRO<UnitHealth>();
            em.CompleteDependencyBeforeRO<RuntimeBuildingCombatTag>();
            em.CompleteDependencyBeforeRO<UnitAirMovement>();
            em.CompleteDependencyBeforeRO<UnitMovementBehavior>();
            em.CompleteDependencyBeforeRO<UnitTarget>();
            em.CompleteDependencyBeforeRO<UnitPathRequest>();
            em.CompleteDependencyBeforeRO<UnitLongDistanceMove>();
            em.CompleteDependencyBeforeRO<EngageTarget>();
            em.CompleteDependencyBeforeRO<BaseBreachOrder>();
            em.CompleteDependencyBeforeRO<UnitMove>();
        }

        private void UpdateTypeHandles(ref SystemState state)
        {
            _entityType.Update(ref state);
            _detectorType.Update(ref state);
            _factionType.Update(ref state);
            _gridType.Update(ref state);
            _healthType.Update(ref state);
            _buildingLookup.Update(ref state);
            _airLookup.Update(ref state);
            _movementBehaviorLookup.Update(ref state);
            _targetLookup.Update(ref state);
            _pathRequestLookup.Update(ref state);
            _longDistanceMoveLookup.Update(ref state);
            _engageTargetLookup.Update(ref state);
            _baseBreachOrderLookup.Update(ref state);
            _unitMoveLookup.Update(ref state);
        }

        private void ClearPreviousThreats()
        {
            if (_previousGroundThreats.IsCreated)
                _previousGroundThreats.Clear();
            if (_previousAirThreats.IsCreated)
                _previousAirThreats.Clear();
        }

        private static void ReplacePreviousThreats(NativeParallelHashSet<Entity> previousThreats, NativeList<Entity> currentThreats)
        {
            previousThreats.Clear();
            for (int i = 0; i < currentThreats.Length; i++)
                previousThreats.Add(currentThreats[i]);
        }

        private static float TryGetCellSize(EntityManager em, EntityQuery gridQuery)
        {
            if (gridQuery.IsEmptyIgnoreFilter)
                return 1f;

            Entity gridEntity = gridQuery.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            return math.max(0.01f, grid.CellSize);
        }

        private static int ChebyshevDistance(int2 a, int2 b)
        {
            int2 delta = math.abs(a - b);
            return math.max(delta.x, delta.y);
        }

        private struct ThreatTargetLookups
        {
            [ReadOnly] public ComponentLookup<RuntimeBuildingCombatTag> BuildingLookup;
            [ReadOnly] public ComponentLookup<UnitAirMovement> AirLookup;
            [ReadOnly] public ComponentLookup<UnitMovementBehavior> MovementBehaviorLookup;
            [ReadOnly] public ComponentLookup<UnitTarget> TargetLookup;
            [ReadOnly] public ComponentLookup<UnitPathRequest> PathRequestLookup;
            [ReadOnly] public ComponentLookup<UnitLongDistanceMove> LongDistanceMoveLookup;
            [ReadOnly] public ComponentLookup<EngageTarget> EngageTargetLookup;
            [ReadOnly] public ComponentLookup<BaseBreachOrder> BaseBreachOrderLookup;
            [ReadOnly] public ComponentLookup<UnitMove> UnitMoveLookup;
        }

        private struct ThreatScanResult
        {
            public byte HasNewGroundThreat;
            public byte HasNewAirThreat;
            public float BestGroundEtaSeconds;
            public float BestAirEtaSeconds;
        }

        private struct ThreatCandidate
        {
            public Entity Entity;
            public int2 Cell;
            public byte IsAir;
        }

        [BurstCompile]
        private struct ThreatScanJob : IJob
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> SensorChunks;
            [ReadOnly] public NativeArray<ArchetypeChunk> TargetChunks;
            [ReadOnly] public EntityTypeHandle EntityType;
            [ReadOnly] public ComponentTypeHandle<ThreatDetector> DetectorType;
            [ReadOnly] public ComponentTypeHandle<Faction> FactionType;
            [ReadOnly] public ComponentTypeHandle<UnitGrid> GridType;
            [ReadOnly] public ComponentTypeHandle<UnitHealth> HealthType;
            [ReadOnly] public ThreatTargetLookups TargetLookups;
            [ReadOnly] public NativeParallelHashSet<Entity> PreviousGroundThreats;
            [ReadOnly] public NativeParallelHashSet<Entity> PreviousAirThreats;
            public NativeParallelHashSet<Entity> CurrentGroundThreats;
            public NativeParallelHashSet<Entity> CurrentAirThreats;
            public NativeList<Entity> CurrentGroundThreatList;
            public NativeList<Entity> CurrentAirThreatList;
            public NativeList<ThreatCandidate> ThreatCandidates;
            public float CellSize;
            public NativeArray<ThreatScanResult> Result;

            public void Execute()
            {
                ThreatScanResult scan = new()
                {
                    BestGroundEtaSeconds = float.MaxValue,
                    BestAirEtaSeconds = float.MaxValue
                };

                ComponentTypeHandle<ThreatDetector> detectorType = DetectorType;
                ComponentTypeHandle<Faction> factionType = FactionType;
                ComponentTypeHandle<UnitGrid> gridType = GridType;
                ComponentTypeHandle<UnitHealth> healthType = HealthType;

                BuildThreatCandidates(ref factionType, ref gridType, ref healthType);

                for (int sensorChunkIndex = 0; sensorChunkIndex < SensorChunks.Length; sensorChunkIndex++)
                {
                    ArchetypeChunk sensorChunk = SensorChunks[sensorChunkIndex];
                    NativeArray<Entity> sensorEntities = sensorChunk.GetNativeArray(EntityType);
                    NativeArray<ThreatDetector> sensorDetectors = sensorChunk.GetNativeArray(ref detectorType);
                    NativeArray<Faction> sensorFactions = sensorChunk.GetNativeArray(ref factionType);
                    NativeArray<UnitGrid> sensorGrids = sensorChunk.GetNativeArray(ref gridType);
                    NativeArray<UnitHealth> sensorHealths = sensorChunk.GetNativeArray(ref healthType);

                    for (int i = 0; i < sensorEntities.Length; i++)
                    {
                        Entity sensor = sensorEntities[i];
                        Faction sensorFaction = sensorFactions[i];
                        if (sensorFaction.Id != PlayerFactionId)
                            continue;

                        UnitHealth sensorHealth = sensorHealths[i];
                        if (sensorHealth.Current <= 0)
                            continue;

                        ThreatDetector detector = sensorDetectors[i];
                        if (detector.RadiusCells <= 0 || detector.Kind == (byte)ThreatDetectionKind.None)
                            continue;

                        int2 sensorCell = sensorGrids[i].Cell;
                        bool detectsAir = detector.Kind == (byte)ThreatDetectionKind.Air;
                        bool detectsGround = detector.Kind == (byte)ThreatDetectionKind.Ground;

                        ScanTargetsForSensor(
                            sensor,
                            sensorCell,
                            detector.RadiusCells,
                            detectsAir,
                            detectsGround,
                            requireApproach: true,
                            ref scan);
                    }
                }

                ScanCloseContactThreats(ref factionType, ref gridType, ref healthType, ref scan);
                Result[0] = scan;
            }

            private void ScanCloseContactThreats(
                ref ComponentTypeHandle<Faction> factionType,
                ref ComponentTypeHandle<UnitGrid> gridType,
                ref ComponentTypeHandle<UnitHealth> healthType,
                ref ThreatScanResult scan)
            {
                for (int sensorChunkIndex = 0; sensorChunkIndex < TargetChunks.Length; sensorChunkIndex++)
                {
                    ArchetypeChunk sensorChunk = TargetChunks[sensorChunkIndex];
                    NativeArray<Entity> sensorEntities = sensorChunk.GetNativeArray(EntityType);
                    NativeArray<Faction> sensorFactions = sensorChunk.GetNativeArray(ref factionType);
                    NativeArray<UnitGrid> sensorGrids = sensorChunk.GetNativeArray(ref gridType);
                    NativeArray<UnitHealth> sensorHealths = sensorChunk.GetNativeArray(ref healthType);

                    for (int i = 0; i < sensorEntities.Length; i++)
                    {
                        Entity sensor = sensorEntities[i];
                        Faction sensorFaction = sensorFactions[i];
                        if (sensorFaction.Id != PlayerFactionId)
                            continue;

                        UnitHealth sensorHealth = sensorHealths[i];
                        if (sensorHealth.Current <= 0)
                            continue;

                        ScanTargetsForSensor(
                            sensor,
                            sensorGrids[i].Cell,
                            CloseContactWarningRadiusCells,
                            detectsAir: true,
                            detectsGround: true,
                            requireApproach: false,
                            ref scan);
                    }
                }
            }

            private void BuildThreatCandidates(
                ref ComponentTypeHandle<Faction> factionType,
                ref ComponentTypeHandle<UnitGrid> gridType,
                ref ComponentTypeHandle<UnitHealth> healthType)
            {
                ThreatCandidates.Clear();

                for (int targetChunkIndex = 0; targetChunkIndex < TargetChunks.Length; targetChunkIndex++)
                {
                    ArchetypeChunk targetChunk = TargetChunks[targetChunkIndex];
                    NativeArray<Entity> targetEntities = targetChunk.GetNativeArray(EntityType);
                    NativeArray<Faction> targetFactions = targetChunk.GetNativeArray(ref factionType);
                    NativeArray<UnitGrid> targetGrids = targetChunk.GetNativeArray(ref gridType);
                    NativeArray<UnitHealth> targetHealths = targetChunk.GetNativeArray(ref healthType);

                    for (int targetIndex = 0; targetIndex < targetEntities.Length; targetIndex++)
                    {
                        Entity target = targetEntities[targetIndex];
                        if (targetFactions[targetIndex].Id == PlayerFactionId ||
                            targetHealths[targetIndex].Current <= 0 ||
                            TargetLookups.BuildingLookup.HasComponent(target))
                        {
                            continue;
                        }

                        bool isAirTarget = TargetLookups.AirLookup.HasComponent(target);
                        if (!isAirTarget && !IsGroundVehicle(TargetLookups, target))
                            continue;

                        ThreatCandidates.Add(new ThreatCandidate
                        {
                            Entity = target,
                            Cell = targetGrids[targetIndex].Cell,
                            IsAir = isAirTarget ? (byte)1 : (byte)0
                        });
                    }
                }
            }

            private void ScanTargetsForSensor(
                Entity sensor,
                int2 sensorCell,
                int detectorRadiusCells,
                bool detectsAir,
                bool detectsGround,
                bool requireApproach,
                ref ThreatScanResult scan)
            {
                for (int targetIndex = 0; targetIndex < ThreatCandidates.Length; targetIndex++)
                {
                    ThreatCandidate candidate = ThreatCandidates[targetIndex];
                    Entity target = candidate.Entity;
                    if (target == sensor)
                        continue;

                    bool isAirTarget = candidate.IsAir != 0;
                    if ((isAirTarget && !detectsAir) || (!isAirTarget && !detectsGround))
                        continue;

                    int2 targetCell = candidate.Cell;
                    int cellDistance = ChebyshevDistance(sensorCell, targetCell);
                    if (cellDistance > detectorRadiusCells)
                        continue;

                    bool movingTowardSensor = IsMovingTowardCell(TargetLookups, target, targetCell, sensorCell);
                    if (requireApproach && !movingTowardSensor)
                        continue;

                    float etaSeconds = movingTowardSensor
                        ? EstimateEtaSeconds(TargetLookups, target, sensorCell, targetCell, CellSize)
                        : 0f;
                    RegisterThreat(target, isAirTarget, etaSeconds, ref scan);
                }
            }

            private void RegisterThreat(Entity target, bool isAirTarget, float etaSeconds, ref ThreatScanResult scan)
            {
                if (isAirTarget)
                {
                    if (CurrentAirThreats.Add(target))
                        CurrentAirThreatList.Add(target);
                    if (PreviousAirThreats.Contains(target))
                        return;

                    scan.HasNewAirThreat = 1;
                    scan.BestAirEtaSeconds = math.min(scan.BestAirEtaSeconds, etaSeconds);
                    return;
                }

                if (CurrentGroundThreats.Add(target))
                    CurrentGroundThreatList.Add(target);
                if (PreviousGroundThreats.Contains(target))
                    return;

                scan.HasNewGroundThreat = 1;
                scan.BestGroundEtaSeconds = math.min(scan.BestGroundEtaSeconds, etaSeconds);
            }
        }

        private static bool IsGroundVehicle(ThreatTargetLookups lookups, Entity target)
        {
            if (!lookups.MovementBehaviorLookup.HasComponent(target))
                return false;

            return lookups.MovementBehaviorLookup[target].UsesVehicleMotion != 0;
        }

        private static bool IsMovingTowardCell(ThreatTargetLookups lookups, Entity target, int2 currentCell, int2 sensorCell)
        {
            int currentDistance = ChebyshevDistance(currentCell, sensorCell);
            bool hasGoal = false;
            int bestGoalDistance = int.MaxValue;

            if (lookups.TargetLookup.HasComponent(target))
            {
                hasGoal = true;
                bestGoalDistance = math.min(bestGoalDistance, ChebyshevDistance(lookups.TargetLookup[target].Cell, sensorCell));
            }
            if (lookups.PathRequestLookup.HasComponent(target))
            {
                hasGoal = true;
                bestGoalDistance = math.min(bestGoalDistance, ChebyshevDistance(lookups.PathRequestLookup[target].Goal, sensorCell));
            }
            if (lookups.LongDistanceMoveLookup.HasComponent(target))
            {
                hasGoal = true;
                bestGoalDistance = math.min(bestGoalDistance, ChebyshevDistance(lookups.LongDistanceMoveLookup[target].FinalGoal, sensorCell));
            }
            if (lookups.EngageTargetLookup.HasComponent(target))
            {
                hasGoal = true;
                bestGoalDistance = math.min(bestGoalDistance, ChebyshevDistance(lookups.EngageTargetLookup[target].Cell, sensorCell));
            }
            if (lookups.BaseBreachOrderLookup.HasComponent(target))
            {
                hasGoal = true;
                BaseBreachOrder order = lookups.BaseBreachOrderLookup[target];
                bestGoalDistance = math.min(bestGoalDistance, ChebyshevDistance(order.FinalCell, sensorCell));
            }

            return hasGoal && bestGoalDistance < currentDistance;
        }

        private static float EstimateEtaSeconds(ThreatTargetLookups lookups, Entity target, int2 sensorCell, int2 targetCell, float cellSize)
        {
            float distanceCells = math.distance(new float2(sensorCell.x, sensorCell.y), new float2(targetCell.x, targetCell.y));
            float speed = lookups.UnitMoveLookup.HasComponent(target)
                ? math.max(0.1f, lookups.UnitMoveLookup[target].Speed)
                : FallbackThreatSpeed;
            return distanceCells * cellSize / speed;
        }
    }
}
