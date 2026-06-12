using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateAfter(typeof(UnitGridMovementSystem))]
[UpdateAfter(typeof(UnitAirMovementSystem))]
public partial struct ThreatDetectionWarningSystem : ISystem
{
    private const byte PlayerFactionId = FactionIdentitySystem.PlayerFactionId;
    private const float FallbackThreatSpeed = 5f;

    private NativeParallelHashSet<Entity> _previousGroundThreats;
    private NativeParallelHashSet<Entity> _previousAirThreats;
    private EntityQuery _sensorQuery;
    private EntityQuery _targetQuery;
    private EntityQuery _gridQuery;
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
        state.RequireForUpdate<ThreatDetector>();
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
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
        {
            ClearPreviousThreats();
            return;
        }

        float cellSize = TryGetCellSize(_gridQuery);

        int targetCapacity = math.max(16, _targetQuery.CalculateEntityCount() * 2);
        using NativeParallelHashSet<Entity> currentGroundThreats = new(targetCapacity, Allocator.Temp);
        using NativeParallelHashSet<Entity> currentAirThreats = new(targetCapacity, Allocator.Temp);
        using NativeList<Entity> currentGroundThreatList = new(Allocator.Temp);
        using NativeList<Entity> currentAirThreatList = new(Allocator.Temp);

        bool hasNewGroundThreat = false;
        bool hasNewAirThreat = false;
        float bestGroundEtaSeconds = float.MaxValue;
        float bestAirEtaSeconds = float.MaxValue;

        UpdateTypeHandles(ref state);
        ThreatTargetLookups targetLookups = new()
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
        };

        using NativeArray<ArchetypeChunk> sensorChunks = _sensorQuery.ToArchetypeChunkArray(Allocator.Temp);
        using NativeArray<ArchetypeChunk> targetChunks = _targetQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int sensorChunkIndex = 0; sensorChunkIndex < sensorChunks.Length; sensorChunkIndex++)
        {
            ArchetypeChunk sensorChunk = sensorChunks[sensorChunkIndex];
            NativeArray<Entity> sensorEntities = sensorChunk.GetNativeArray(_entityType);
            NativeArray<ThreatDetector> sensorDetectors = sensorChunk.GetNativeArray(ref _detectorType);
            NativeArray<Faction> sensorFactions = sensorChunk.GetNativeArray(ref _factionType);
            NativeArray<UnitGrid> sensorGrids = sensorChunk.GetNativeArray(ref _gridType);
            NativeArray<UnitHealth> sensorHealths = sensorChunk.GetNativeArray(ref _healthType);

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

                for (int targetChunkIndex = 0; targetChunkIndex < targetChunks.Length; targetChunkIndex++)
                {
                    ArchetypeChunk targetChunk = targetChunks[targetChunkIndex];
                    NativeArray<Entity> targetEntities = targetChunk.GetNativeArray(_entityType);
                    NativeArray<Faction> targetFactions = targetChunk.GetNativeArray(ref _factionType);
                    NativeArray<UnitGrid> targetGrids = targetChunk.GetNativeArray(ref _gridType);
                    NativeArray<UnitHealth> targetHealths = targetChunk.GetNativeArray(ref _healthType);

                    for (int targetIndex = 0; targetIndex < targetEntities.Length; targetIndex++)
                    {
                        Entity target = targetEntities[targetIndex];
                        if (target == sensor || targetLookups.BuildingLookup.HasComponent(target))
                            continue;

                        Faction targetFaction = targetFactions[targetIndex];
                        if (targetFaction.Id == sensorFaction.Id)
                            continue;

                        UnitHealth targetHealth = targetHealths[targetIndex];
                        if (targetHealth.Current <= 0)
                            continue;

                        bool isAirTarget = targetLookups.AirLookup.HasComponent(target);
                        if ((detectsAir && !isAirTarget) || (detectsGround && isAirTarget))
                            continue;
                        if (detectsGround && !IsGroundVehicle(targetLookups, target))
                            continue;

                        int2 targetCell = targetGrids[targetIndex].Cell;
                        if (!IsMovingTowardCell(targetLookups, target, targetCell, sensorCell))
                            continue;

                        int cellDistance = ChebyshevDistance(sensorCell, targetCell);
                        if (cellDistance > detector.RadiusCells)
                            continue;

                        float etaSeconds = EstimateEtaSeconds(targetLookups, target, sensorCell, targetCell, cellSize);
                        if (isAirTarget)
                        {
                            if (currentAirThreats.Add(target))
                                currentAirThreatList.Add(target);
                            if (!_previousAirThreats.Contains(target))
                            {
                                hasNewAirThreat = true;
                                bestAirEtaSeconds = math.min(bestAirEtaSeconds, etaSeconds);
                            }
                        }
                        else
                        {
                            if (currentGroundThreats.Add(target))
                                currentGroundThreatList.Add(target);
                            if (!_previousGroundThreats.Contains(target))
                            {
                                hasNewGroundThreat = true;
                                bestGroundEtaSeconds = math.min(bestGroundEtaSeconds, etaSeconds);
                            }
                        }
                    }
                }
            }
        }

        ReplacePreviousThreats(_previousGroundThreats, currentGroundThreatList);
        ReplacePreviousThreats(_previousAirThreats, currentAirThreatList);

        if (hasNewGroundThreat && (!hasNewAirThreat || bestGroundEtaSeconds <= bestAirEtaSeconds))
        {
            ThreatWarningRuntimeState.RequestWarning(
                ThreatWarningType.Ground,
                bestGroundEtaSeconds == float.MaxValue ? 0f : bestGroundEtaSeconds,
                currentGroundThreatList.Length);
        }
        else if (hasNewAirThreat)
        {
            ThreatWarningRuntimeState.RequestWarning(
                ThreatWarningType.Air,
                bestAirEtaSeconds == float.MaxValue ? 0f : bestAirEtaSeconds,
                currentAirThreatList.Length);
        }
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

    private static float TryGetCellSize(EntityQuery gridQuery)
    {
        if (gridQuery.IsEmptyIgnoreFilter)
            return 1f;

        GridConfig grid = gridQuery.GetSingleton<GridConfig>();
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
