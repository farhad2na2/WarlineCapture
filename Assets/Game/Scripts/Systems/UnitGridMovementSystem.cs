using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[WithNone(typeof(EngageTarget), typeof(UnitDeathAnimationComponent), typeof(UnitAirMovement))]
public partial struct UnitGridMoveJob : IJobEntity
{
    private const int VehicleOccupancyPaddingCells = 1;
    private const int SoftBlockerDisplacementSearchRadius = 4;
    private const float InfantryOccupiedRepathDelaySeconds = 0.35f;
    private const float InfantryGroupFinalStopDistanceCells = 1.25f;
    private const int InfantryMaxWaypointAdvancesPerFrame = 8;
    public float DeltaTime;
    public GridConfig Grid;
    public EntityCommandBuffer.ParallelWriter Ecb;
    [ReadOnly] public NativeArray<int2> Pool;
    [ReadOnly] public NativeArray<GridWalkable> Walkable;
    [ReadOnly] public NativeBitArray DynamicBlocked;
    [ReadOnly] public NativeArray<byte> FriendlyPassFactionIds;
    [ReadOnly] public NativeBitArray Occupied;
    [ReadOnly] public NativeArray<GridRoad> Roads;
    [ReadOnly] public NativeArray<GridRoadSidewalk> Sidewalks;
    [ReadOnly] public NativeArray<GridRoadDirt> DirtRoads;
    [ReadOnly] public NativeArray<Entity> LiveUnitEntities;
    [ReadOnly] public NativeArray<UnitGrid> LiveUnitGrids;
    [ReadOnly] public NativeArray<UnitFootprint> LiveUnitFootprints;
    [ReadOnly] public ComponentLookup<AutoWanderMoveTag> AutoWanderLookup;
    [ReadOnly] public ComponentLookup<UnitTarget> UnitTargetLookup;
    [ReadOnly] public ComponentLookup<UnitLongDistanceMove> LongDistanceMoveLookup;
    [ReadOnly] public ComponentLookup<ManualMoveGroupMemberTag> ManualMoveGroupLookup;
    [ReadOnly] public ComponentLookup<UnitTransportBoardingTarget> BoardingTargetLookup;

    public void Execute(
        [EntityIndexInQuery] int sortKey,
        Entity entity,
        ref LocalTransform transform,
        ref UnitGrid unitGrid,
        ref UnitPathFollow follow,
        ref UnitVehicleKinematics vehicleKinematics,
        in UnitMove move,
        in UnitPathRange range,
        in UnitFootprint footprint,
        in UnitMovementBehavior movementBehavior,
        in UnitVehicleMovement vehicleMovement,
        in Faction faction)
    {
        if (range.Length <= 0)
            return;

        if ((uint)follow.PathIndex >= (uint)range.Length)
            return;

        int poolIndex = range.Start + follow.PathIndex;
        if ((uint)poolIndex >= (uint)Pool.Length)
            return;

        int2 targetCell = Pool[poolIndex];
        int targetIndex = GridUtils.CellToIndex(targetCell, Grid.Width);
        int2 footprintSize = footprint.Size;
        bool isVehicle = UnitVehicleMovementUtility.IsVehicle(footprint, movementBehavior);
        bool groupedManualMove = !isVehicle && ManualMoveGroupLookup.HasComponent(entity);
        if (!isVehicle)
        {
            int2 worldCell = GridUtils.WorldToCell(Grid, transform.Position);
            if (GridUtils.InBounds(worldCell, Grid.Width, Grid.Height) && !worldCell.Equals(unitGrid.Cell))
                unitGrid.Cell = worldCell;
        }

        if (!targetCell.Equals(unitGrid.Cell))
        {
            bool targetOverlapsCurrentFootprint = UnitFootprintUtility.Overlaps(unitGrid.Cell, footprintSize, targetCell, footprintSize);
            bool targetBlockedByGrid = !targetOverlapsCurrentFootprint &&
                !CanOccupyMovementTarget(Grid, Walkable, DynamicBlocked, FriendlyPassFactionIds, targetCell, footprintSize, unitGrid.Cell, faction.Id);
            if (targetBlockedByGrid)
            {
                if (!isVehicle)
                {
                    vehicleKinematics.CurrentSpeed = 0f;
                    vehicleKinematics.StallSeconds = 0f;
                }

                RequestRepath(sortKey, entity);
                return;
            }

            bool targetBlockedByOccupant = Occupied.IsSet(targetIndex);
            if (targetBlockedByOccupant)
            {
                bool passableSelfOccupancy = IsOnlySelfOccupyingCell(entity, targetIndex);
                bool passableGroupMember = groupedManualMove && IsOnlyManualMoveGroupMemberAtCell(entity, targetIndex);
                bool passableVehicleSoftBlocker = isVehicle && IsVehicleSoftOccupiedTarget(targetIndex);
                bool passableBoardingTransport = !isVehicle && IsBoardingTransportOccupancyCell(entity, targetIndex);
                targetBlockedByOccupant = !passableSelfOccupancy && !passableGroupMember && !passableVehicleSoftBlocker && !passableBoardingTransport;
            }

            if (!targetOverlapsCurrentFootprint && targetBlockedByOccupant)
            {
                if (!isVehicle)
                {
                    vehicleKinematics.CurrentSpeed = 0f;
                    vehicleKinematics.StallSeconds += DeltaTime;
                    if (vehicleKinematics.StallSeconds < InfantryOccupiedRepathDelaySeconds)
                        return;

                    vehicleKinematics.StallSeconds = 0f;
                }

                RequestRepath(sortKey, entity);
                return;
            }
        }
        float3 targetPos = GridUtils.CellToWorldCenter(Grid, targetCell);
        targetPos.y = transform.Position.y;
        bool finalPathStep = follow.PathIndex >= (range.Length - 1);
        int2 finalCell = Pool[range.Start + range.Length - 1];
        float3 finalTargetPos = GridUtils.CellToWorldCenter(Grid, finalCell);
        finalTargetPos.y = transform.Position.y;
        float3 toFinalTarget = finalTargetPos - transform.Position;
        toFinalTarget.y = 0f;

        float3 toTarget = targetPos - transform.Position;
        float arriveDistance = isVehicle
            ? math.max(move.ArriveDistance, Grid.CellSize * (finalPathStep ? 1.35f : 0.9f))
            : move.ArriveDistance;
        float arriveDistSq = arriveDistance * arriveDistance;
        float finalStopDistance = isVehicle ? math.max(move.ArriveDistance, Grid.CellSize * 1.5f) : move.ArriveDistance;
        float finalStopDistSq = finalStopDistance * finalStopDistance;
        float groupedFinalStopDistance = math.max(move.ArriveDistance, Grid.CellSize * InfantryGroupFinalStopDistanceCells);
        float groupedFinalStopDistSq = groupedFinalStopDistance * groupedFinalStopDistance;

        if (groupedManualMove && finalPathStep && math.lengthsq(toFinalTarget) <= groupedFinalStopDistSq)
        {
            vehicleKinematics.CurrentSpeed = 0f;
            vehicleKinematics.StallSeconds = 0f;
            follow.PathIndex = range.Length;
            int2 settledCell = GridUtils.WorldToCell(Grid, transform.Position);
            if (GridUtils.InBounds(settledCell, Grid.Width, Grid.Height) && !settledCell.Equals(unitGrid.Cell))
                unitGrid.Cell = settledCell;
            return;
        }

        if (isVehicle && math.lengthsq(toFinalTarget) <= finalStopDistSq)
        {
            vehicleKinematics.CurrentSpeed = 0f;
            vehicleKinematics.StallSeconds = 0f;
            follow.PathIndex = range.Length;
            int2 settledCell = GridUtils.WorldToCell(Grid, transform.Position);
            if (GridUtils.InBounds(settledCell, Grid.Width, Grid.Height) && !settledCell.Equals(unitGrid.Cell))
                unitGrid.Cell = settledCell;
            return;
        }

        if (isVehicle && math.lengthsq(toTarget) <= arriveDistSq)
        {
            vehicleKinematics.StallSeconds = 0f;
            unitGrid.Cell = targetCell;
            follow.PathIndex++;
            return;
        }

        float3 dir = math.normalizesafe(toTarget);
        float speed = AutoWanderLookup.HasComponent(entity) ? move.WalkSpeed : move.Speed;
        int2 currentCell = GridUtils.WorldToCell(Grid, transform.Position);
        if (GridUtils.InBounds(currentCell, Grid.Width, Grid.Height))
        {
            int currentIndex = GridUtils.CellToIndex(currentCell, Grid.Width);
            bool onPreferredSurface = isVehicle
                ? DirtRoads[currentIndex].Value != 0
                : Sidewalks[currentIndex].Value != 0;
            if (onPreferredSurface)
                speed *= move.RoadSpeedMultiplier;
        }

        if (isVehicle)
        {
            bool canPlaceTargetCell = CanVehicleOccupyCell(entity, targetCell, footprintSize, unitGrid.Cell, faction.Id, sortKey);
            if (!canPlaceTargetCell)
            {
                vehicleKinematics.StallSeconds = 0f;
                RequestRepath(sortKey, entity);
                return;
            }

            float3 oldPosition = transform.Position;
            float turnThresholdRadians = math.radians(finalPathStep ? 18f : 10f);
            bool moved = UnitVehicleMovementUtility.MoveVehicle(
                ref transform,
                ref vehicleKinematics,
                vehicleMovement,
                dir,
                turnThresholdRadians,
                speed,
                DeltaTime,
                math.length(toTarget));

            if (!moved)
            {
                vehicleKinematics.StallSeconds += DeltaTime;
                if (vehicleKinematics.StallSeconds >= 0.35f)
                {
                    vehicleKinematics.StallSeconds = 0f;
                    RequestRepath(sortKey, entity);
                }
                return;
            }

            vehicleKinematics.StallSeconds = 0f;
            int2 movedCell = GridUtils.WorldToCell(Grid, transform.Position);
            if (!GridUtils.InBounds(movedCell, Grid.Width, Grid.Height))
            {
                transform.Position = oldPosition;
                vehicleKinematics.CurrentSpeed = 0f;
                vehicleKinematics.StallSeconds = 0f;
                RequestRepath(sortKey, entity);
                return;
            }

            // Intermediate world motion can briefly place the vehicle center on a cell that
            // would fail the same footprint test used for settled grid occupancy, even though
            // the next path node itself is valid. Only commit UnitGrid when that intermediate
            // cell is also valid; otherwise keep moving toward the validated target node.
            if (!movedCell.Equals(unitGrid.Cell) &&
                CanVehicleOccupyCell(entity, movedCell, footprintSize, unitGrid.Cell, faction.Id, sortKey))
            {
                unitGrid.Cell = movedCell;
            }

            float3 toCurrentTarget = targetPos - transform.Position;
            if (math.lengthsq(toCurrentTarget) <= arriveDistSq)
            {
                unitGrid.Cell = targetCell;
                follow.PathIndex = finalPathStep ? range.Length : (follow.PathIndex + 1);
            }
            return;
        }

        MoveInfantryAlongPath(
            ref transform,
            ref unitGrid,
            ref follow,
            ref vehicleKinematics,
            move,
            range,
            speed,
            groupedManualMove);
    }

    private void MoveInfantryAlongPath(
        ref LocalTransform transform,
        ref UnitGrid unitGrid,
        ref UnitPathFollow follow,
        ref UnitVehicleKinematics vehicleKinematics,
        in UnitMove move,
        in UnitPathRange range,
        float speed,
        bool groupedManualMove)
    {
        vehicleKinematics.CurrentSpeed = 0f;
        vehicleKinematics.StallSeconds = 0f;

        float remainingDistance = math.max(0f, speed * DeltaTime);
        float arriveDistance = move.ArriveDistance;
        float arriveDistSq = arriveDistance * arriveDistance;
        float groupedFinalStopDistance = math.max(move.ArriveDistance, Grid.CellSize * InfantryGroupFinalStopDistanceCells);
        float groupedFinalStopDistSq = groupedFinalStopDistance * groupedFinalStopDistance;

        for (int i = 0; i < InfantryMaxWaypointAdvancesPerFrame && remainingDistance > 0f; i++)
        {
            if ((uint)follow.PathIndex >= (uint)range.Length)
                return;

            int poolIndex = range.Start + follow.PathIndex;
            if ((uint)poolIndex >= (uint)Pool.Length)
                return;

            int2 targetCell = Pool[poolIndex];
            bool finalPathStep = follow.PathIndex >= (range.Length - 1);
            int finalPoolIndex = range.Start + range.Length - 1;
            if ((uint)finalPoolIndex >= (uint)Pool.Length)
                return;

            int2 finalCell = Pool[finalPoolIndex];
            float3 finalTargetPos = GridUtils.CellToWorldCenter(Grid, finalCell);
            finalTargetPos.y = transform.Position.y;
            float3 toFinalTarget = finalTargetPos - transform.Position;
            toFinalTarget.y = 0f;

            if (groupedManualMove && finalPathStep && math.lengthsq(toFinalTarget) <= groupedFinalStopDistSq)
            {
                follow.PathIndex = range.Length;
                int2 settledCell = GridUtils.WorldToCell(Grid, transform.Position);
                if (GridUtils.InBounds(settledCell, Grid.Width, Grid.Height) && !settledCell.Equals(unitGrid.Cell))
                    unitGrid.Cell = settledCell;
                return;
            }

            float3 targetPos = GridUtils.CellToWorldCenter(Grid, targetCell);
            targetPos.y = transform.Position.y;
            float3 toTarget = targetPos - transform.Position;
            toTarget.y = 0f;
            float distSq = math.lengthsq(toTarget);
            if (distSq <= arriveDistSq)
            {
                unitGrid.Cell = targetCell;
                follow.PathIndex++;
                continue;
            }

            float dist = math.sqrt(distSq);
            if (dist <= 0.0001f)
            {
                unitGrid.Cell = targetCell;
                follow.PathIndex++;
                continue;
            }

            float3 dir = toTarget / dist;
            if (remainingDistance >= dist)
            {
                transform.Position = targetPos;
                transform.Rotation = quaternion.LookRotationSafe(dir, math.up());
                remainingDistance -= dist;
                unitGrid.Cell = targetCell;
                follow.PathIndex++;
                continue;
            }

            transform.Position += dir * remainingDistance;
            transform.Rotation = quaternion.LookRotationSafe(dir, math.up());
            remainingDistance = 0f;
        }
    }

    private void RequestRepath(int sortKey, Entity entity)
    {
        bool hasLongDistanceMove = LongDistanceMoveLookup.HasComponent(entity);
        if (!hasLongDistanceMove && !UnitTargetLookup.HasComponent(entity))
            return;

        int2 goal = hasLongDistanceMove
            ? LongDistanceMoveLookup[entity].FinalGoal
            : UnitTargetLookup[entity].Cell;
        Ecb.RemoveComponent<UnitPathFollow>(sortKey, entity);
        Ecb.RemoveComponent<UnitPathRange>(sortKey, entity);
        Ecb.AddComponent(sortKey, entity, new UnitPathRequest { Goal = goal });
    }

    private bool IsVehicleSoftOccupiedTarget(int cellIndex)
    {
        return IsOnlySoftBlockerAtCell(cellIndex);
    }

    private bool IsBoardingTransportOccupancyCell(Entity passenger, int cellIndex)
    {
        if (!BoardingTargetLookup.HasComponent(passenger))
            return false;

        Entity transport = BoardingTargetLookup[passenger].Transport;
        if (transport == Entity.Null)
        {
            return false;
        }

        int2 cell = GridUtils.IndexToCell(cellIndex, Grid.Width);
        for (int i = 0; i < LiveUnitEntities.Length; i++)
        {
            if (LiveUnitEntities[i] != transport)
                continue;

            return UnitFootprintUtility.ContainsCell(
                LiveUnitGrids[i].Cell,
                LiveUnitFootprints[i].Size,
                cell);
        }

        return false;
    }

    private bool IsOnlyManualMoveGroupMemberAtCell(Entity entity, int cellIndex)
    {
        int2 cell = GridUtils.IndexToCell(cellIndex, Grid.Width);
        bool foundGroupMember = false;
        for (int i = 0; i < LiveUnitEntities.Length; i++)
        {
            Entity other = LiveUnitEntities[i];
            int2 otherSize = LiveUnitFootprints[i].Size;
            if (!UnitFootprintUtility.ContainsCell(LiveUnitGrids[i].Cell, otherSize, cell))
                continue;

            if (other != entity && !ManualMoveGroupLookup.HasComponent(other))
                return false;

            foundGroupMember = true;
        }

        return foundGroupMember;
    }

    private bool IsOnlySelfOccupyingCell(Entity entity, int cellIndex)
    {
        int2 cell = GridUtils.IndexToCell(cellIndex, Grid.Width);
        bool foundSelf = false;
        for (int i = 0; i < LiveUnitEntities.Length; i++)
        {
            int2 otherSize = LiveUnitFootprints[i].Size;
            if (!UnitFootprintUtility.ContainsCell(LiveUnitGrids[i].Cell, otherSize, cell))
                continue;

            if (LiveUnitEntities[i] != entity)
                return false;

            foundSelf = true;
        }

        return foundSelf;
    }

    private bool CanVehicleOccupyCell(Entity entity, int2 candidateCell, int2 candidateSize, int2 currentCell, byte factionId, int sortKey)
    {
        if (!CanVehiclePlaceWithSoftBlockers(candidateCell, candidateSize, currentCell, VehicleOccupancyPaddingCells, factionId))
            return false;

        return !OverlapsHardLiveUnitFootprints(entity, candidateCell, candidateSize, currentCell, sortKey);
    }

    private bool CanVehiclePlaceWithSoftBlockers(int2 centerCell, int2 size, int2 currentCenterCell, int occupiedPadding, byte factionId)
    {
        int padding = math.max(0, occupiedPadding);
        int2 clamped = UnitFootprintUtility.ClampSize(size);
        int2 min = UnitFootprintUtility.GetMinCell(centerCell, clamped);
        int2 max = min + clamped;

        if (min.x < 0 || min.y < 0 || max.x > Grid.Width || max.y > Grid.Height)
            return false;

        int2 currentMin = UnitFootprintUtility.GetMinCell(currentCenterCell, clamped);
        int2 currentMax = currentMin + clamped;
        int2 paddedMin = min - new int2(padding, padding);
        int2 paddedMax = max + new int2(padding, padding);
        int2 currentPaddedMin = currentMin - new int2(padding, padding);
        int2 currentPaddedMax = currentMax + new int2(padding, padding);

        if (paddedMin.x < 0 || paddedMin.y < 0 || paddedMax.x > Grid.Width || paddedMax.y > Grid.Height)
            return false;

        for (int y = paddedMin.y; y < paddedMax.y; y++)
        {
            int row = y * Grid.Width;
            for (int x = paddedMin.x; x < paddedMax.x; x++)
            {
                bool insideActualFootprint = x >= min.x && x < max.x && y >= min.y && y < max.y;
                int idx = row + x;
                if (insideActualFootprint)
                {
                    if (Walkable[idx].Value == 0)
                        return false;
                    if (IsBlockedForFaction(idx, factionId))
                        return false;
                }

                bool isCurrentClearanceCell =
                    x >= currentPaddedMin.x && x < currentPaddedMax.x &&
                    y >= currentPaddedMin.y && y < currentPaddedMax.y;
                if (!isCurrentClearanceCell && Occupied.IsCreated && Occupied.IsSet(idx) && !IsOnlySoftBlockerAtCell(idx))
                    return false;
            }
        }

        return true;
    }

    private bool OverlapsHardLiveUnitFootprints(Entity entity, int2 candidateCell, int2 candidateSize, int2 currentCell, int sortKey)
    {
        for (int i = 0; i < LiveUnitEntities.Length; i++)
        {
            if (LiveUnitEntities[i] == entity)
                continue;

            int2 otherCell = LiveUnitGrids[i].Cell;
            int2 otherSize = LiveUnitFootprints[i].Size;

            if (UnitFootprintUtility.Overlaps(candidateCell, candidateSize, otherCell, otherSize) &&
                !UnitFootprintUtility.Overlaps(currentCell, candidateSize, otherCell, otherSize))
            {
                if (IsSoftBlocker(otherSize))
                {
                    RequestSoftBlockerMove(sortKey, LiveUnitEntities[i], otherCell, candidateCell);
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    private bool IsOnlySoftBlockerAtCell(int idx)
    {
        int2 cell = GridUtils.IndexToCell(idx, Grid.Width);
        bool foundSoft = false;
        for (int i = 0; i < LiveUnitEntities.Length; i++)
        {
            int2 otherSize = LiveUnitFootprints[i].Size;
            if (!UnitFootprintUtility.ContainsCell(LiveUnitGrids[i].Cell, otherSize, cell))
                continue;

            if (!IsSoftBlocker(otherSize))
                return false;

            foundSoft = true;
        }

        return foundSoft;
    }

    private static bool IsSoftBlocker(int2 size)
    {
        int2 clamped = UnitFootprintUtility.ClampSize(size);
        return clamped.x == 1 && clamped.y == 1;
    }

    public static bool CanOccupyMovementTarget(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray dynamicBlocked,
        in NativeArray<byte> friendlyPassFactionIds,
        int2 targetCell,
        int2 footprintSize,
        int2 currentCell,
        byte factionId)
    {
        return UnitFootprintUtility.CanPlace(
            grid,
            walkable,
            dynamicBlocked,
            friendlyPassFactionIds,
            default,
            targetCell,
            footprintSize,
            currentCell,
            factionId);
    }

    private bool IsBlockedForFaction(int idx, byte factionId)
    {
        if (!DynamicBlocked.IsCreated || !DynamicBlocked.IsSet(idx))
            return false;

        if (FriendlyPassFactionIds.IsCreated &&
            (uint)idx < (uint)FriendlyPassFactionIds.Length &&
            FriendlyPassFactionIds[idx] == factionId)
            return false;

        return true;
    }

    private void RequestSoftBlockerMove(int sortKey, Entity entity, int2 blockerCell, int2 vehicleCell)
    {
        int2 bestGoal = blockerCell;
        bool found = false;
        int2 away = blockerCell - vehicleCell;
        if (away.x == 0 && away.y == 0)
            away = new int2(1, 0);

        int bestScore = int.MinValue;
        for (int radius = 1; radius <= SoftBlockerDisplacementSearchRadius; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (math.abs(dx) != radius && math.abs(dy) != radius)
                        continue;

                    int2 candidate = blockerCell + new int2(dx, dy);
                    if (!GridUtils.InBounds(candidate, Grid.Width, Grid.Height))
                        continue;

                    int idx = GridUtils.CellToIndex(candidate, Grid.Width);
                    if (Walkable[idx].Value == 0 || IsBlockedForFaction(idx, 255) || Occupied.IsSet(idx))
                        continue;

                    int score = dx * away.x + dy * away.y;
                    if (found && score <= bestScore)
                        continue;

                    found = true;
                    bestScore = score;
                    bestGoal = candidate;
                }
            }
            if (found)
                break;
        }

        if (!found)
            return;

        Ecb.RemoveComponent<UnitPathFollow>(sortKey, entity);
        Ecb.RemoveComponent<UnitPathRange>(sortKey, entity);
        if (UnitTargetLookup.HasComponent(entity))
            Ecb.SetComponent(sortKey, entity, new UnitTarget { Cell = bestGoal });
        else
            Ecb.AddComponent(sortKey, entity, new UnitTarget { Cell = bestGoal });

        Ecb.AddComponent(sortKey, entity, new UnitPathRequest { Goal = bestGoal });
        Ecb.AddComponent<ManualMoveOrderTag>(sortKey, entity);
    }

}

[BurstCompile]
[WithNone(typeof(EngageTarget), typeof(UnitDeathAnimationComponent), typeof(UnitAirMovement))]
public partial struct UnitPathFollowCleanupJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    [ReadOnly] public ComponentLookup<UnitGrid> UnitGridLookup;
    [ReadOnly] public ComponentLookup<UnitLongDistanceMove> LongDistanceMoveLookup;
    [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveLookup;

    public void Execute([EntityIndexInQuery] int sortKey, Entity entity, ref UnitVehicleKinematics vehicleKinematics, in UnitPathFollow follow, in UnitPathRange range)
    {
        if (range.Length <= 0 || (uint)follow.PathIndex >= (uint)range.Length)
        {
            vehicleKinematics.CurrentSpeed = 0f;
            vehicleKinematics.StallSeconds = 0f;
            Ecb.RemoveComponent<UnitPathFollow>(sortKey, entity);
            Ecb.RemoveComponent<UnitPathRange>(sortKey, entity);
            Ecb.RemoveComponent<AutoWanderMoveTag>(sortKey, entity);

            if (LongDistanceMoveLookup.HasComponent(entity) && UnitGridLookup.HasComponent(entity))
            {
                int2 currentCell = UnitGridLookup[entity].Cell;
                int2 finalGoal = LongDistanceMoveLookup[entity].FinalGoal;
                if (!currentCell.Equals(finalGoal))
                {
                    Ecb.RemoveComponent<UnitTarget>(sortKey, entity);
                    Ecb.AddComponent(sortKey, entity, new UnitTarget { Cell = finalGoal });
                    Ecb.AddComponent(sortKey, entity, new UnitPathRequest { Goal = finalGoal });
                    if (!ManualMoveLookup.HasComponent(entity))
                        Ecb.AddComponent<ManualMoveOrderTag>(sortKey, entity);
                    return;
                }

                Ecb.RemoveComponent<UnitLongDistanceMove>(sortKey, entity);
            }

            Ecb.RemoveComponent<ManualMoveOrderTag>(sortKey, entity);
            Ecb.RemoveComponent<UnitTarget>(sortKey, entity);
        }
    }

}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(UnitPathfindingSystem))]
[UpdateAfter(typeof(DynamicOccupancyRebuildSystem))]
[UpdateAfter(typeof(UnitEngagedMovementSystem))]
public partial struct UnitGridMovementSystem : ISystem
{
    private const double FreezeLogThresholdSeconds = 0.05d;
    private static readonly bool EnableGridMovementFreezeLogs = false;
    private EntityQuery _gridQuery;
    private EntityQuery _ecbSingletonQuery;
    private EntityQuery _liveUnitsQuery;
    private EntityTypeHandle _liveEntityType;
    private ComponentTypeHandle<UnitGrid> _liveGridType;
    private ComponentTypeHandle<UnitFootprint> _liveFootprintType;

    public void OnCreate(ref SystemState state)
    {
        _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
        _ecbSingletonQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<EndSimulationEntityCommandBufferSystem.Singleton>() },
            Options = EntityQueryOptions.IncludeSystems
        });
        state.RequireForUpdate(_gridQuery);
        state.RequireForUpdate<UnitPathFollow>();
        state.RequireForUpdate<UnitPathRange>();
        state.RequireForUpdate<PathPoolComponent>();
        state.RequireForUpdate<DynamicOccupancyComponent>();
        state.RequireForUpdate<GridRoad>();
        state.RequireForUpdate<GridRoadSidewalk>();
        state.RequireForUpdate<GridRoadDirt>();
        state.RequireForUpdate(_ecbSingletonQuery);

        _liveUnitsQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>()
            }
        });
        _liveEntityType = state.GetEntityTypeHandle();
        _liveGridType = state.GetComponentTypeHandle<UnitGrid>(true);
        _liveFootprintType = state.GetComponentTypeHandle<UnitFootprint>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        try
        {
            Entity gridEntity = _gridQuery.GetSingletonEntity();
            var grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
            var pool = state.EntityManager.GetComponentData<PathPoolComponent>(gridEntity);
            var poolArray = pool.Cells.AsArray();
            var walkable = SystemAPI.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            var dynamicBlocked = state.EntityManager.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
            var friendlyPassFactionIds = state.EntityManager.GetComponentData<DynamicBlockerComponent>(gridEntity).FriendlyPassFactionIds;
            var occupied = state.EntityManager.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
            var roads = SystemAPI.GetBuffer<GridRoad>(gridEntity).AsNativeArray();
            var sidewalks = SystemAPI.GetBuffer<GridRoadSidewalk>(gridEntity).AsNativeArray();
            var dirtRoads = SystemAPI.GetBuffer<GridRoadDirt>(gridEntity).AsNativeArray();
            int liveUnitCount = math.max(1, _liveUnitsQuery.CalculateEntityCount());
            var liveUnitEntities = new NativeList<Entity>(liveUnitCount, Allocator.TempJob);
            var liveUnitGrids = new NativeList<UnitGrid>(liveUnitCount, Allocator.TempJob);
            var liveUnitFootprints = new NativeList<UnitFootprint>(liveUnitCount, Allocator.TempJob);
            state.EntityManager.CompleteDependencyBeforeRO<UnitGrid>();
            state.EntityManager.CompleteDependencyBeforeRO<UnitFootprint>();
            _liveEntityType.Update(ref state);
            _liveGridType.Update(ref state);
            _liveFootprintType.Update(ref state);
            CollectLiveUnits(ref liveUnitEntities, ref liveUnitGrids, ref liveUnitFootprints);
            Entity ecbEntity = _ecbSingletonQuery.GetSingletonEntity();
            var ecbSystem = state.EntityManager.GetComponentData<EndSimulationEntityCommandBufferSystem.Singleton>(ecbEntity);
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var moveHandle = new UnitGridMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Grid = grid,
                Ecb = ecb,
                Pool = poolArray,
                Walkable = walkable,
                DynamicBlocked = dynamicBlocked,
                FriendlyPassFactionIds = friendlyPassFactionIds,
                Occupied = occupied,
                Roads = roads,
                Sidewalks = sidewalks,
                DirtRoads = dirtRoads,
                LiveUnitEntities = liveUnitEntities.AsArray(),
                LiveUnitGrids = liveUnitGrids.AsArray(),
                LiveUnitFootprints = liveUnitFootprints.AsArray(),
                AutoWanderLookup = SystemAPI.GetComponentLookup<AutoWanderMoveTag>(true),
                UnitTargetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true),
                LongDistanceMoveLookup = SystemAPI.GetComponentLookup<UnitLongDistanceMove>(true),
                ManualMoveGroupLookup = SystemAPI.GetComponentLookup<ManualMoveGroupMemberTag>(true),
                BoardingTargetLookup = SystemAPI.GetComponentLookup<UnitTransportBoardingTarget>(true)
            }.ScheduleParallel(state.Dependency);

            var cleanupHandle = new UnitPathFollowCleanupJob
            {
                Ecb = ecb,
                UnitGridLookup = SystemAPI.GetComponentLookup<UnitGrid>(true),
                LongDistanceMoveLookup = SystemAPI.GetComponentLookup<UnitLongDistanceMove>(true),
                ManualMoveLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true)
            }.ScheduleParallel(moveHandle);

            var disposeEntitiesHandle = liveUnitEntities.Dispose(cleanupHandle);
            var disposeGridsHandle = liveUnitGrids.Dispose(cleanupHandle);
            var disposeFootprintsHandle = liveUnitFootprints.Dispose(cleanupHandle);
            var disposeHandle = JobHandle.CombineDependencies(disposeEntitiesHandle, disposeGridsHandle, disposeFootprintsHandle);
            state.Dependency = JobHandle.CombineDependencies(cleanupHandle, disposeHandle);
        }
        finally
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
            if (EnableGridMovementFreezeLogs && elapsed >= FreezeLogThresholdSeconds)
                Debug.Log($"[FreezeDetect:ECS] UnitGridMovementSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms");
        }
    }

    private void CollectLiveUnits(
        ref NativeList<Entity> entities,
        ref NativeList<UnitGrid> grids,
        ref NativeList<UnitFootprint> footprints)
    {
        entities.Clear();
        grids.Clear();
        footprints.Clear();

        using NativeArray<ArchetypeChunk> chunks = _liveUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> chunkEntities = chunk.GetNativeArray(_liveEntityType);
            NativeArray<UnitGrid> chunkGrids = chunk.GetNativeArray(ref _liveGridType);
            NativeArray<UnitFootprint> chunkFootprints = chunk.GetNativeArray(ref _liveFootprintType);
            for (int i = 0; i < chunkEntities.Length; i++)
            {
                entities.Add(chunkEntities[i]);
                grids.Add(chunkGrids[i]);
                footprints.Add(chunkFootprints[i]);
            }
        }
    }
}
