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

        EntityManager em = state.EntityManager;
        float cellSize = TryGetCellSize(em, _gridQuery);

        using NativeArray<Entity> sensors = _sensorQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<Entity> targets = _targetQuery.ToEntityArray(Allocator.Temp);
        using NativeParallelHashSet<Entity> currentGroundThreats = new(math.max(16, targets.Length * 2), Allocator.Temp);
        using NativeParallelHashSet<Entity> currentAirThreats = new(math.max(16, targets.Length * 2), Allocator.Temp);
        using NativeList<Entity> currentGroundThreatList = new(Allocator.Temp);
        using NativeList<Entity> currentAirThreatList = new(Allocator.Temp);

        bool hasNewGroundThreat = false;
        bool hasNewAirThreat = false;
        float bestGroundEtaSeconds = float.MaxValue;
        float bestAirEtaSeconds = float.MaxValue;

        for (int i = 0; i < sensors.Length; i++)
        {
            Entity sensor = sensors[i];
            if (!em.Exists(sensor))
                continue;

            Faction sensorFaction = em.GetComponentData<Faction>(sensor);
            if (sensorFaction.Id != PlayerFactionId)
                continue;

            UnitHealth sensorHealth = em.GetComponentData<UnitHealth>(sensor);
            if (sensorHealth.Current <= 0)
                continue;

            ThreatDetector detector = em.GetComponentData<ThreatDetector>(sensor);
            if (detector.RadiusCells <= 0 || detector.Kind == (byte)ThreatDetectionKind.None)
                continue;

            int2 sensorCell = em.GetComponentData<UnitGrid>(sensor).Cell;
            bool detectsAir = detector.Kind == (byte)ThreatDetectionKind.Air;
            bool detectsGround = detector.Kind == (byte)ThreatDetectionKind.Ground;

            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                Entity target = targets[targetIndex];
                if (!em.Exists(target) || target == sensor || em.HasComponent<RuntimeBuildingCombatTag>(target))
                    continue;

                Faction targetFaction = em.GetComponentData<Faction>(target);
                if (targetFaction.Id == sensorFaction.Id)
                    continue;

                UnitHealth targetHealth = em.GetComponentData<UnitHealth>(target);
                if (targetHealth.Current <= 0)
                    continue;

                bool isAirTarget = em.HasComponent<UnitAirMovement>(target);
                if ((detectsAir && !isAirTarget) || (detectsGround && isAirTarget))
                    continue;
                if (detectsGround && !IsGroundVehicle(em, target))
                    continue;

                int2 targetCell = em.GetComponentData<UnitGrid>(target).Cell;
                if (!IsMovingTowardCell(em, target, targetCell, sensorCell))
                    continue;

                int cellDistance = ChebyshevDistance(sensorCell, targetCell);
                if (cellDistance > detector.RadiusCells)
                    continue;

                float etaSeconds = EstimateEtaSeconds(em, target, sensorCell, targetCell, cellSize);
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
        using NativeArray<Entity> grids = gridQuery.ToEntityArray(Allocator.Temp);
        if (grids.Length == 0 || !em.Exists(grids[0]))
            return 1f;

        GridConfig grid = em.GetComponentData<GridConfig>(grids[0]);
        return math.max(0.01f, grid.CellSize);
    }

    private static int ChebyshevDistance(int2 a, int2 b)
    {
        int2 delta = math.abs(a - b);
        return math.max(delta.x, delta.y);
    }

    private static bool IsGroundVehicle(EntityManager em, Entity target)
    {
        if (!em.HasComponent<UnitMovementBehavior>(target))
            return false;

        return em.GetComponentData<UnitMovementBehavior>(target).UsesVehicleMotion != 0;
    }

    private static bool IsMovingTowardCell(EntityManager em, Entity target, int2 currentCell, int2 sensorCell)
    {
        int currentDistance = ChebyshevDistance(currentCell, sensorCell);
        bool hasGoal = false;
        int bestGoalDistance = int.MaxValue;

        if (em.HasComponent<UnitTarget>(target))
        {
            hasGoal = true;
            bestGoalDistance = math.min(bestGoalDistance, ChebyshevDistance(em.GetComponentData<UnitTarget>(target).Cell, sensorCell));
        }
        if (em.HasComponent<UnitPathRequest>(target))
        {
            hasGoal = true;
            bestGoalDistance = math.min(bestGoalDistance, ChebyshevDistance(em.GetComponentData<UnitPathRequest>(target).Goal, sensorCell));
        }
        if (em.HasComponent<UnitLongDistanceMove>(target))
        {
            hasGoal = true;
            bestGoalDistance = math.min(bestGoalDistance, ChebyshevDistance(em.GetComponentData<UnitLongDistanceMove>(target).FinalGoal, sensorCell));
        }
        if (em.HasComponent<EngageTarget>(target))
        {
            hasGoal = true;
            bestGoalDistance = math.min(bestGoalDistance, ChebyshevDistance(em.GetComponentData<EngageTarget>(target).Cell, sensorCell));
        }
        if (em.HasComponent<BaseBreachOrder>(target))
        {
            hasGoal = true;
            BaseBreachOrder order = em.GetComponentData<BaseBreachOrder>(target);
            bestGoalDistance = math.min(bestGoalDistance, ChebyshevDistance(order.FinalCell, sensorCell));
        }

        return hasGoal && bestGoalDistance < currentDistance;
    }

    private static float EstimateEtaSeconds(EntityManager em, Entity target, int2 sensorCell, int2 targetCell, float cellSize)
    {
        float distanceCells = math.distance(new float2(sensorCell.x, sensorCell.y), new float2(targetCell.x, targetCell.y));
        float speed = em.HasComponent<UnitMove>(target)
            ? math.max(0.1f, em.GetComponentData<UnitMove>(target).Speed)
            : FallbackThreatSpeed;
        return distanceCells * cellSize / speed;
    }
}
