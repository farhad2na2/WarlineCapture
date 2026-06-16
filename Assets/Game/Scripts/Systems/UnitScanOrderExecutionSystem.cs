using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

[UpdateBefore(typeof(ScanIntelCommandSystem))]
public partial struct UnitScanOrderExecutionSystem : ISystem
{
    private const float RevealIntervalSeconds = 1f;
    private const float GroundPatrolMoveIntervalSeconds = 2.5f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitScanOrder>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);
        NativeList<PendingScanReveal> pendingReveals = new(16, Allocator.Temp);
        NativeList<PendingPatrolMove> pendingPatrolMoves = new(16, Allocator.Temp);
        ComponentLookup<Disabled> disabledLookup = SystemAPI.GetComponentLookup<Disabled>(true);
        ComponentLookup<UnitDeathAnimationComponent> deathAnimationLookup = SystemAPI.GetComponentLookup<UnitDeathAnimationComponent>(true);
        ComponentLookup<UnitHealth> healthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
        ComponentLookup<UnitAirComponent> airLookup = SystemAPI.GetComponentLookup<UnitAirComponent>(false);
        ComponentLookup<UnitAirMovement> airMovementLookup = SystemAPI.GetComponentLookup<UnitAirMovement>(true);
        ComponentLookup<UnitTarget> targetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true);
        ComponentLookup<UnitPathRequest> pathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true);
        ComponentLookup<ManualMoveOrderTag> manualMoveOrderLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true);
        ComponentLookup<EngageTarget> engageTargetLookup = SystemAPI.GetComponentLookup<EngageTarget>(true);
        bool hasGridConfig = SystemAPI.TryGetSingleton(out GridConfig gridConfig);
        try
        {
            float now = (float)SystemAPI.Time.ElapsedTime;
            foreach (var (scanOrder, unitGrid, entity) in SystemAPI
                         .Query<RefRW<UnitScanOrder>, RefRO<UnitGrid>>()
                         .WithEntityAccess())
            {
                if (IsDeadOrInvalidSource(entity, disabledLookup, deathAnimationLookup, healthLookup))
                {
                    ecb.RemoveComponent<UnitScanOrder>(entity);
                    continue;
                }

                ref UnitScanOrder order = ref scanOrder.ValueRW;
                int triggerRadius = math.max(1, order.RadiusCells);
                bool insideScanArea = ChebyshevDistance(unitGrid.ValueRO.Cell, order.CenterCell) <= triggerRadius;
                if (order.HasStarted == 0)
                {
                    if (!insideScanArea)
                        continue;

                    order.HasStarted = 1;
                    order.StartedTimeSeconds = now;
                    order.NextRevealTimeSeconds = now;
                }

                if (order.DurationSeconds > 0f &&
                    now - order.StartedTimeSeconds >= order.DurationSeconds)
                {
                    CompleteScanOrder(
                        ecb,
                        airLookup,
                        targetLookup,
                        pathRequestLookup,
                        manualMoveOrderLookup,
                        engageTargetLookup,
                        entity,
                        order);
                    continue;
                }

                if (!insideScanArea)
                    continue;

                if (now < order.NextRevealTimeSeconds)
                {
                    TryScheduleGroundPatrolMove(
                        entity,
                        unitGrid.ValueRO.Cell,
                        ref order,
                        now,
                        hasGridConfig,
                        gridConfig,
                        airMovementLookup,
                        targetLookup,
                        pendingPatrolMoves);
                    continue;
                }

                pendingReveals.Add(new PendingScanReveal
                {
                    RequestId = order.RequestId,
                    Frame = order.StartedFrame,
                    SourceEntity = entity,
                    CenterCell = order.CenterCell,
                    CenterWorld = order.CenterWorld,
                    RadiusCells = order.RadiusCells
                });
                order.NextRevealTimeSeconds = now + RevealIntervalSeconds;

                TryScheduleGroundPatrolMove(
                    entity,
                    unitGrid.ValueRO.Cell,
                    ref order,
                    now,
                    hasGridConfig,
                    gridConfig,
                    airMovementLookup,
                    targetLookup,
                    pendingPatrolMoves);
            }

            UnitMoveOrderSystem moveOrderSystem = new();
            for (int i = 0; i < pendingPatrolMoves.Length; i++)
            {
                PendingPatrolMove patrolMove = pendingPatrolMoves[i];
                moveOrderSystem.IssueImmediateMoveCommand(state.EntityManager, patrolMove.Entity, patrolMove.TargetCell);
            }

            ecb.Playback(state.EntityManager);

            for (int i = 0; i < pendingReveals.Length; i++)
            {
                PendingScanReveal reveal = pendingReveals[i];
                ScanIntelCommandSystem.EnqueueScan(
                    state.EntityManager,
                    reveal.RequestId,
                    reveal.Frame,
                    reveal.CenterCell,
                    reveal.CenterWorld,
                    reveal.SourceEntity,
                    true,
                    false,
                    reveal.RadiusCells);
            }
        }
        finally
        {
            if (pendingReveals.IsCreated)
                pendingReveals.Dispose();
            if (pendingPatrolMoves.IsCreated)
                pendingPatrolMoves.Dispose();
            ecb.Dispose();
        }
    }

    private static void TryScheduleGroundPatrolMove(
        Entity entity,
        int2 currentCell,
        ref UnitScanOrder order,
        float now,
        bool hasGridConfig,
        in GridConfig gridConfig,
        ComponentLookup<UnitAirMovement> airMovementLookup,
        ComponentLookup<UnitTarget> targetLookup,
        NativeList<PendingPatrolMove> pendingPatrolMoves)
    {
        if (airMovementLookup.HasComponent(entity) ||
            order.RadiusCells <= 1 ||
            now < order.NextPatrolMoveTimeSeconds ||
            HasUnreachedTarget(entity, currentCell, targetLookup))
        {
            return;
        }

        int2 targetCell = ResolveGroundPatrolCell(order.CenterCell, order.RadiusCells, order.PatrolWaypointIndex, hasGridConfig, gridConfig);
        if (math.all(targetCell == currentCell))
        {
            order.PatrolWaypointIndex = (order.PatrolWaypointIndex + 1) & 3;
            targetCell = ResolveGroundPatrolCell(order.CenterCell, order.RadiusCells, order.PatrolWaypointIndex, hasGridConfig, gridConfig);
            if (math.all(targetCell == currentCell))
                return;
        }

        pendingPatrolMoves.Add(new PendingPatrolMove
        {
            Entity = entity,
            TargetCell = targetCell
        });
        order.PatrolWaypointIndex = (order.PatrolWaypointIndex + 1) & 3;
        order.NextPatrolMoveTimeSeconds = now + GroundPatrolMoveIntervalSeconds;
    }

    private static bool HasUnreachedTarget(Entity entity, int2 currentCell, ComponentLookup<UnitTarget> targetLookup)
    {
        if (!targetLookup.HasComponent(entity))
            return false;

        return ChebyshevDistance(currentCell, targetLookup[entity].Cell) > 1;
    }

    private static int2 ResolveGroundPatrolCell(
        int2 centerCell,
        int radiusCells,
        int waypointIndex,
        bool hasGridConfig,
        in GridConfig gridConfig)
    {
        int extent = math.max(1, radiusCells - 1);
        int2 offset = (waypointIndex & 3) switch
        {
            0 => new int2(extent, 0),
            1 => new int2(0, extent),
            2 => new int2(-extent, 0),
            _ => new int2(0, -extent)
        };

        int2 targetCell = centerCell + offset;
        if (!hasGridConfig)
            return targetCell;

        return new int2(
            math.clamp(targetCell.x, 0, math.max(0, gridConfig.Width - 1)),
            math.clamp(targetCell.y, 0, math.max(0, gridConfig.Height - 1)));
    }

    private static void CompleteScanOrder(
        EntityCommandBuffer ecb,
        ComponentLookup<UnitAirComponent> airLookup,
        ComponentLookup<UnitTarget> targetLookup,
        ComponentLookup<UnitPathRequest> pathRequestLookup,
        ComponentLookup<ManualMoveOrderTag> manualMoveOrderLookup,
        ComponentLookup<EngageTarget> engageTargetLookup,
        Entity entity,
        in UnitScanOrder order)
    {
        if (order.ReturnHomeAfterCompletion != 0 && airLookup.HasComponent(entity))
        {
            UnitAirComponent airState = airLookup[entity];
            airState.ReturningHome = 1;
            airState.AttackRunActive = 0;
            airState.ReturnApproachInitialized = 0;
            if (airState.Airborne != 0)
                airState.TakeoffRolling = 0;
            airLookup[entity] = airState;

            RemoveIfPresent(targetLookup, ecb, entity);
            RemoveIfPresent(pathRequestLookup, ecb, entity);
            RemoveIfPresent(manualMoveOrderLookup, ecb, entity);
            RemoveIfPresent(engageTargetLookup, ecb, entity);
        }

        ecb.RemoveComponent<UnitScanOrder>(entity);
    }

    private static bool IsDeadOrInvalidSource(
        Entity entity,
        ComponentLookup<Disabled> disabledLookup,
        ComponentLookup<UnitDeathAnimationComponent> deathAnimationLookup,
        ComponentLookup<UnitHealth> healthLookup)
    {
        if (disabledLookup.HasComponent(entity) ||
            deathAnimationLookup.HasComponent(entity))
        {
            return true;
        }

        return healthLookup.HasComponent(entity) &&
               healthLookup[entity].Current <= 0;
    }

    private static int ChebyshevDistance(int2 a, int2 b)
    {
        int2 delta = math.abs(a - b);
        return math.max(delta.x, delta.y);
    }

    private static void RemoveIfPresent<T>(ComponentLookup<T> lookup, EntityCommandBuffer ecb, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (lookup.HasComponent(entity))
            ecb.RemoveComponent<T>(entity);
    }

    private struct PendingScanReveal
    {
        public int RequestId;
        public int Frame;
        public Entity SourceEntity;
        public int2 CenterCell;
        public float3 CenterWorld;
        public int RadiusCells;
    }

    private struct PendingPatrolMove
    {
        public Entity Entity;
        public int2 TargetCell;
    }
}
