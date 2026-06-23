using Unity.Burst;
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
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        NativeList<PendingScanReveal> pendingReveals = new(16, Allocator.TempJob);
        NativeList<PendingPatrolMove> pendingPatrolMoves = new(16, Allocator.TempJob);
        bool hasGridConfig = SystemAPI.TryGetSingleton(out GridConfig gridConfig);
        try
        {
            state.Dependency = new ExecuteScanOrdersJob
            {
                Ecb = ecb,
                PendingReveals = pendingReveals,
                PendingPatrolMoves = pendingPatrolMoves,
                DisabledLookup = SystemAPI.GetComponentLookup<Disabled>(true),
                DeathAnimationLookup = SystemAPI.GetComponentLookup<UnitDeathAnimationComponent>(true),
                HealthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true),
                AirLookup = SystemAPI.GetComponentLookup<UnitAirComponent>(false),
                AirMovementLookup = SystemAPI.GetComponentLookup<UnitAirMovement>(true),
                TargetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true),
                PathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true),
                ManualMoveOrderLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true),
                EngageTargetLookup = SystemAPI.GetComponentLookup<EngageTarget>(true),
                Now = (float)SystemAPI.Time.ElapsedTime,
                HasGridConfig = hasGridConfig ? (byte)1 : (byte)0,
                GridConfig = gridConfig
            }.Schedule(state.Dependency);
            state.Dependency.Complete();

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

    [BurstCompile]
    private partial struct ExecuteScanOrdersJob : IJobEntity
    {
        public EntityCommandBuffer Ecb;
        public NativeList<PendingScanReveal> PendingReveals;
        public NativeList<PendingPatrolMove> PendingPatrolMoves;
        [ReadOnly] public ComponentLookup<Disabled> DisabledLookup;
        [ReadOnly] public ComponentLookup<UnitDeathAnimationComponent> DeathAnimationLookup;
        [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
        public ComponentLookup<UnitAirComponent> AirLookup;
        [ReadOnly] public ComponentLookup<UnitAirMovement> AirMovementLookup;
        [ReadOnly] public ComponentLookup<UnitTarget> TargetLookup;
        [ReadOnly] public ComponentLookup<UnitPathRequest> PathRequestLookup;
        [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveOrderLookup;
        [ReadOnly] public ComponentLookup<EngageTarget> EngageTargetLookup;
        public float Now;
        public byte HasGridConfig;
        public GridConfig GridConfig;

        private void Execute(Entity entity, ref UnitScanOrder order, in UnitGrid unitGrid)
        {
            if (IsDeadOrInvalidSource(entity, DisabledLookup, DeathAnimationLookup, HealthLookup))
            {
                Ecb.RemoveComponent<UnitScanOrder>(entity);
                return;
            }

            int triggerRadius = math.max(1, order.RadiusCells);
            bool insideScanArea = ChebyshevDistance(unitGrid.Cell, order.CenterCell) <= triggerRadius;
            if (order.HasStarted == 0)
            {
                if (!insideScanArea)
                    return;

                order.HasStarted = 1;
                order.StartedTimeSeconds = Now;
                order.NextRevealTimeSeconds = Now;
            }

            if (order.DurationSeconds > 0f &&
                Now - order.StartedTimeSeconds >= order.DurationSeconds)
            {
                CompleteScanOrder(
                    Ecb,
                    AirLookup,
                    TargetLookup,
                    PathRequestLookup,
                    ManualMoveOrderLookup,
                    EngageTargetLookup,
                    entity,
                    order);
                return;
            }

            if (!insideScanArea)
                return;

            if (Now < order.NextRevealTimeSeconds)
            {
                TryScheduleGroundPatrolMove(
                    entity,
                    unitGrid.Cell,
                    ref order,
                    Now,
                    HasGridConfig != 0,
                    GridConfig,
                    AirMovementLookup,
                    TargetLookup,
                    PendingPatrolMoves);
                return;
            }

            PendingReveals.Add(new PendingScanReveal
            {
                RequestId = order.RequestId,
                Frame = order.StartedFrame,
                SourceEntity = entity,
                CenterCell = order.CenterCell,
                CenterWorld = order.CenterWorld,
                RadiusCells = order.RadiusCells
            });
            order.NextRevealTimeSeconds = Now + RevealIntervalSeconds;

            TryScheduleGroundPatrolMove(
                entity,
                unitGrid.Cell,
                ref order,
                Now,
                HasGridConfig != 0,
                GridConfig,
                AirMovementLookup,
                TargetLookup,
                PendingPatrolMoves);
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
