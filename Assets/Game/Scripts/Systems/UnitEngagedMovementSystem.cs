using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateAfter(typeof(UnitEngagementSystem))]
[UpdateBefore(typeof(UnitGridMovementSystem))]
public partial struct UnitEngagedMovementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<EngageTarget>();
        state.RequireForUpdate<GridRoad>();
        state.RequireForUpdate<GridRoadSidewalk>();
        state.RequireForUpdate<GridRoadDirt>();
        state.RequireForUpdate<DynamicBlockerComponent>();
        state.RequireForUpdate<UnitAttack>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        var grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
        var walkable = SystemAPI.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var blockerData = state.EntityManager.GetComponentData<DynamicBlockerComponent>(gridEntity);
        var roads = SystemAPI.GetBuffer<GridRoad>(gridEntity).AsNativeArray();
        var sidewalks = SystemAPI.GetBuffer<GridRoadSidewalk>(gridEntity).AsNativeArray();
        var dirtRoads = SystemAPI.GetBuffer<GridRoadDirt>(gridEntity).AsNativeArray();
        var footprintLookup = SystemAPI.GetComponentLookup<UnitFootprint>(true);
        var healthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
        var staticBlockerLookup = SystemAPI.GetComponentLookup<StaticGridBlocker>(true);
        var holdPositionLookup = SystemAPI.GetComponentLookup<HoldPositionOrderTag>(true);
        var debugFireTargetLookup = SystemAPI.GetComponentLookup<DebugFireTargetTag>(true);
        var scanOrderLookup = SystemAPI.GetComponentLookup<UnitScanOrder>(true);

        var handle = new EngagedMoveJob
        {
            Grid = grid,
            Walkable = walkable,
            DynamicBlocked = blockerData.Blocked,
            FriendlyPassFactionIds = blockerData.FriendlyPassFactionIds,
            Roads = roads,
            Sidewalks = sidewalks,
            DirtRoads = dirtRoads,
            DeltaTime = SystemAPI.Time.DeltaTime,
            FootprintLookup = footprintLookup,
            HealthLookup = healthLookup,
            StaticBlockerLookup = staticBlockerLookup,
            HoldPositionLookup = holdPositionLookup,
            DebugFireTargetLookup = debugFireTargetLookup,
            ScanOrderLookup = scanOrderLookup
        }.ScheduleParallel(state.Dependency);

        state.Dependency = handle;
    }

    [BurstCompile]
    [WithNone(typeof(StaticGridBlocker), typeof(UnitDeathAnimationComponent), typeof(UnitAirMovement))]
    private partial struct EngagedMoveJob : IJobEntity
    {
        public GridConfig Grid;
        public float DeltaTime;

        [ReadOnly]
        public Unity.Collections.NativeArray<GridWalkable> Walkable;
        [ReadOnly] public NativeBitArray DynamicBlocked;
        [ReadOnly] public Unity.Collections.NativeArray<byte> FriendlyPassFactionIds;
        [ReadOnly]
        public Unity.Collections.NativeArray<GridRoad> Roads;
        [ReadOnly] public Unity.Collections.NativeArray<GridRoadSidewalk> Sidewalks;
        [ReadOnly] public Unity.Collections.NativeArray<GridRoadDirt> DirtRoads;
        [ReadOnly] public ComponentLookup<UnitFootprint> FootprintLookup;
        [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
        [ReadOnly] public ComponentLookup<StaticGridBlocker> StaticBlockerLookup;
        [ReadOnly] public ComponentLookup<HoldPositionOrderTag> HoldPositionLookup;
        [ReadOnly] public ComponentLookup<DebugFireTargetTag> DebugFireTargetLookup;
        [ReadOnly] public ComponentLookup<UnitScanOrder> ScanOrderLookup;

        public void Execute(
            Entity entity,
            ref LocalTransform transform,
            ref UnitGrid selfGrid,
            ref EngageTarget engage,
            ref UnitVehicleKinematics vehicleKinematics,
            in UnitCombat combat,
            in UnitAttack attack,
            in UnitMove move,
            in UnitFootprint footprint,
            in UnitMovementBehavior movementBehavior,
            in UnitVehicleMovement vehicleMovement,
            in Faction faction)
        {
            float attackRange = math.max(0f, attack.Range);
            if (attackRange <= 0f)
                return;

            if (engage.Target == Entity.Null)
            {
                return;
            }

            if (DebugFireTargetLookup.HasComponent(engage.Target) &&
                DebugFireTargetLookup[engage.Target].Source == entity)
            {
                vehicleKinematics.CurrentSpeed = 0f;
                return;
            }

            if (StaticBlockerLookup.HasComponent(engage.Target) ||
                (HealthLookup.HasComponent(engage.Target) && HealthLookup[engage.Target].Current <= 0))
            {
                engage.Target = Entity.Null;
                engage.Cell = default;
                engage.Position = default;
                engage.IsCommanded = 0;
                vehicleKinematics.CurrentSpeed = 0f;
                return;
            }

            bool holdingPosition = HoldPositionLookup.HasComponent(entity);
            bool scanning = TryGetActiveScanOrder(entity, out UnitScanOrder scanOrder);

            // Keep UnitGrid in sync for engaged units so occupancy reflects actual positions (prevents "pushing").
            int2 selfWorldCell = GridUtils.WorldToCell(Grid, transform.Position);
            if (GridUtils.InBounds(selfWorldCell, Grid.Width, Grid.Height))
                selfGrid.Cell = selfWorldCell;

            float3 otherPos = engage.Position;
            float3 toOther = otherPos - transform.Position;
            toOther.y = 0f;
            float distSq = math.lengthsq(toOther);
            float dist = math.sqrt(distSq);
            float selfCombatRadius = GetCombatRadius(footprint.Size, Grid.CellSize);
            float targetCombatRadius = FootprintLookup.HasComponent(engage.Target)
                ? GetCombatRadius(FootprintLookup[engage.Target].Size, Grid.CellSize)
                : 0f;
            float effectiveAttackRange = attackRange + selfCombatRadius + targetCombatRadius;
            float chaseLeash = math.max(
                math.max(0f, combat.ChaseBreakDistance),
                math.max(0, combat.AggroRangeCells) * Grid.CellSize);

            if (scanning && !IsTargetInsideScanArea(engage.Position, scanOrder))
            {
                engage.Target = Entity.Null;
                engage.Cell = default;
                engage.Position = default;
                engage.IsCommanded = 0;
                vehicleKinematics.CurrentSpeed = 0f;
                return;
            }

            if (holdingPosition && distSq > effectiveAttackRange * effectiveAttackRange)
            {
                engage.Target = Entity.Null;
                engage.Cell = default;
                engage.Position = default;
                engage.IsCommanded = 0;
                vehicleKinematics.CurrentSpeed = 0f;
                return;
            }

            if (engage.IsCommanded == 0 && chaseLeash > 0f && dist > chaseLeash)
            {
                engage.Target = Entity.Null;
                engage.Cell = default;
                engage.Position = default;
                engage.IsCommanded = 0;
                return;
            }

            bool isVehicle = UnitVehicleMovementUtility.IsVehicle(footprint, movementBehavior);

            // If the enemy is in attack range, never move. Vehicles can attack in any direction.
            if (distSq <= effectiveAttackRange * effectiveAttackRange)
            {
                vehicleKinematics.CurrentSpeed = 0f;
                if (!isVehicle && math.lengthsq(toOther) > 1e-8f)
                {
                    float3 targetDir = math.normalizesafe(toOther);
                    transform.Rotation = quaternion.LookRotationSafe(targetDir, math.up());
                }
                return;
            }

            float3 desiredDirection = math.normalizesafe(toOther);
            if (!isVehicle && distSq > 1e-8f)
                transform.Rotation = quaternion.LookRotationSafe(desiredDirection, math.up());

            // Use world-space approach for engaged combat so units stop cleanly inside attack range instead
            // of oscillating between cell-center combat slots.
            float desiredWorldDistance = math.max(0.05f, effectiveAttackRange - 0.05f);
            if (desiredWorldDistance > 0f)
            {
                float remaining = dist - desiredWorldDistance;
                if (remaining <= 0f)
                    return;

                float directMoveSpeed = move.Speed;
                if (GridUtils.InBounds(selfGrid.Cell, Grid.Width, Grid.Height))
                {
                    int currentIndex = GridUtils.CellToIndex(selfGrid.Cell, Grid.Width);
                    bool onPreferredSurface = isVehicle
                        ? DirtRoads[currentIndex].Value != 0
                        : Sidewalks[currentIndex].Value != 0;
                    if (onPreferredSurface)
                        directMoveSpeed *= move.RoadSpeedMultiplier;
                }

                float moveDistance = math.min(remaining, directMoveSpeed * DeltaTime);
                if (isVehicle)
                {
                    float3 oldPosition = transform.Position;
                    bool moved = UnitVehicleMovementUtility.MoveVehicle(
                        ref transform,
                        ref vehicleKinematics,
                        vehicleMovement,
                        desiredDirection,
                        math.radians(8f),
                        directMoveSpeed,
                        DeltaTime,
                        remaining);
                    if (!moved)
                        return;

                    if (!CanMoveAlongSegment(oldPosition, transform.Position, footprint.Size, selfGrid.Cell, faction.Id))
                    {
                        transform.Position = oldPosition;
                        vehicleKinematics.CurrentSpeed = 0f;
                        return;
                    }
                }
                else
                {
                    vehicleKinematics.CurrentSpeed = 0f;
                    float3 nextPosition = transform.Position + desiredDirection * moveDistance;
                    if (!CanMoveAlongSegment(transform.Position, nextPosition, footprint.Size, selfGrid.Cell, faction.Id))
                        return;

                    transform.Position += desiredDirection * moveDistance;
                }

                int2 movedCell = GridUtils.WorldToCell(Grid, transform.Position);
                if (GridUtils.InBounds(movedCell, Grid.Width, Grid.Height))
                    selfGrid.Cell = movedCell;
                return;
            }
        }

        private static float GetCombatRadius(int2 footprintSize, float cellSize)
        {
            int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
            float halfWidth = math.max(0f, (clamped.x - 1) * 0.5f * cellSize);
            float halfDepth = math.max(0f, (clamped.y - 1) * 0.5f * cellSize);
            return math.max(halfWidth, halfDepth);
        }

        private bool TryGetActiveScanOrder(Entity entity, out UnitScanOrder scanOrder)
        {
            scanOrder = default;
            if (!ScanOrderLookup.HasComponent(entity))
                return false;

            scanOrder = ScanOrderLookup[entity];
            return scanOrder.HasStarted != 0 &&
                   scanOrder.RadiusCells > 0;
        }

        private bool IsTargetInsideScanArea(float3 targetPosition, in UnitScanOrder scanOrder)
        {
            int2 targetCell = GridUtils.WorldToCell(Grid, targetPosition);
            if (!GridUtils.InBounds(targetCell, Grid.Width, Grid.Height))
                return false;

            return ChebyshevDistance(targetCell, scanOrder.CenterCell) <= math.max(1, scanOrder.RadiusCells);
        }

        private static int ChebyshevDistance(int2 a, int2 b)
        {
            int2 delta = math.abs(a - b);
            return math.max(delta.x, delta.y);
        }

        private bool CanMoveAlongSegment(float3 startPosition, float3 endPosition, int2 footprintSize, int2 currentCell, byte factionId)
        {
            float2 delta = new float2(endPosition.x - startPosition.x, endPosition.z - startPosition.z);
            float distance = math.length(delta);
            float sampleDistance = math.max(0.05f, Grid.CellSize * 0.45f);
            int steps = math.max(1, (int)math.ceil(distance / sampleDistance));

            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                float3 samplePosition = math.lerp(startPosition, endPosition, t);
                int2 sampleCell = GridUtils.WorldToCell(Grid, samplePosition);
                if (!CanMoveIntoCell(sampleCell, footprintSize, currentCell, factionId))
                    return false;
            }

            return true;
        }

        private bool CanMoveIntoCell(int2 targetCell, int2 footprintSize, int2 currentCell, byte factionId)
        {
            if (targetCell.Equals(currentCell))
                return true;

            return UnitGridMoveJob.CanOccupyMovementTarget(
                Grid,
                Walkable,
                DynamicBlocked,
                FriendlyPassFactionIds,
                targetCell,
                footprintSize,
                currentCell,
                factionId);
        }
    }
}
