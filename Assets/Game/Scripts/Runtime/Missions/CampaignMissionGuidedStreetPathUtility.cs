using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class CampaignMissionGuidedStreetPathUtility
    {
        private const int MaximumRouteCells = 128;
        private const int PreferredFormationSlotCount = 4;

        internal static bool TryResolvePreferredFormationGoal(
            EntityManager entityManager,
            Entity gridEntity,
            in GridConfig grid,
            UnitMoveOrderSystem moveOrderSystem,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            HashSet<int> selectedCurrentCells,
            MapSurfacePathfindingSnapshot.Context surfaceContext,
            int2 start,
            int2 footprintSize,
            byte factionId,
            in CampaignMissionGuidedMoveRouteUtility.Context context,
            bool advancesAlongZ,
            HashSet<int> reservedGoalCells,
            out int2 resolvedGoal)
        {
            resolvedGoal = default;
            int outerOffset = math.max(2, context.TargetRadiusCells);
            int innerOffset = math.max(1, outerOffset - 2);
            int bestScore = int.MaxValue;
            using NativeList<int2> route = new(Allocator.Temp);
            for (int slotIndex = 0; slotIndex < PreferredFormationSlotCount; slotIndex++)
            {
                int lateralOffset = slotIndex switch
                {
                    0 => -outerOffset,
                    1 => -innerOffset,
                    2 => innerOffset,
                    _ => outerOffset
                };
                int2 candidate = context.TargetCell + (advancesAlongZ
                    ? new int2(lateralOffset, 0)
                    : new int2(0, lateralOffset));
                int score = (advancesAlongZ
                    ? math.abs(candidate.x - start.x)
                    : math.abs(candidate.y - start.y)) * 10 + slotIndex;
                if (score >= bestScore || !moveOrderSystem.CanReserveManualMoveGoal(
                        grid, walkable, blocked, friendlyPassFactionIds, occupied,
                        reservedGoalCells, selectedCurrentCells, candidate, footprintSize,
                        0, factionId, surfaceContext, false))
                    continue;

                route.Clear();
                if (!TryBuildDirect(
                        entityManager, gridEntity, grid, walkable, blocked,
                        friendlyPassFactionIds, start, candidate, footprintSize, factionId, route))
                    continue;
                bestScore = score;
                resolvedGoal = candidate;
            }

            if (bestScore == int.MaxValue)
                return false;
            moveOrderSystem.ReserveManualMoveGoalFootprint(
                grid, reservedGoalCells, resolvedGoal, footprintSize, 0);
            return true;
        }

        internal static bool HasRequiredBuffers(EntityManager entityManager, Entity gridEntity)
        {
            return entityManager.HasBuffer<GridRoad>(gridEntity) &&
                   entityManager.HasBuffer<GridRoadSidewalk>(gridEntity) &&
                   entityManager.HasBuffer<GridRoadDirt>(gridEntity);
        }

        internal static bool TryBuild(
            EntityManager entityManager,
            Entity gridEntity,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            int2 start,
            int2 goal,
            int2 footprintSize,
            byte factionId,
            NativeList<int2> route)
        {
            if (start.Equals(goal) || !HasRequiredBuffers(entityManager, gridEntity))
                return false;

            if (TryBuildDirect(
                    entityManager, gridEntity, grid, walkable, blocked, friendlyPassFactionIds,
                    start, goal, footprintSize, factionId, route))
                return true;

            route.Clear();
            if (TryBuildAxis(
                    entityManager, gridEntity, grid, walkable, blocked, friendlyPassFactionIds,
                    start, goal, footprintSize, factionId, zFirst: true, route))
                return true;

            route.Clear();
            return TryBuildAxis(
                entityManager, gridEntity, grid, walkable, blocked, friendlyPassFactionIds,
                start, goal, footprintSize, factionId, zFirst: false, route);
        }

        internal static bool TryBuildDirect(
            EntityManager entityManager,
            Entity gridEntity,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            int2 start,
            int2 goal,
            int2 footprintSize,
            byte factionId,
            NativeList<int2> route)
        {
            int2 cursor = start;
            int deltaX = math.abs(goal.x - start.x);
            int deltaY = -math.abs(goal.y - start.y);
            int stepX = math.select(-1, 1, start.x < goal.x);
            int stepY = math.select(-1, 1, start.y < goal.y);
            int error = deltaX + deltaY;
            while (!cursor.Equals(goal))
            {
                int twiceError = error * 2;
                if (twiceError >= deltaY)
                {
                    error += deltaY;
                    cursor.x += stepX;
                }
                if (twiceError <= deltaX)
                {
                    error += deltaX;
                    cursor.y += stepY;
                }
                if (!TryAppendValidatedCell(entityManager, gridEntity, grid, walkable, blocked,
                        friendlyPassFactionIds, start, goal, footprintSize, factionId, cursor, route))
                    return false;
            }
            return route.Length > 0;
        }

        private static bool TryBuildAxis(
            EntityManager entityManager,
            Entity gridEntity,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            int2 start,
            int2 goal,
            int2 footprintSize,
            byte factionId,
            bool zFirst,
            NativeList<int2> route)
        {
            int2 cursor = start;
            if (zFirst)
            {
                if (!TryAppendAxis(entityManager, gridEntity, grid, walkable, blocked,
                        friendlyPassFactionIds, start, goal, footprintSize, factionId,
                        xAxis: false, ref cursor, route) ||
                    !TryAppendAxis(entityManager, gridEntity, grid, walkable, blocked,
                        friendlyPassFactionIds, start, goal, footprintSize, factionId,
                        xAxis: true, ref cursor, route))
                    return false;
            }
            else if (!TryAppendAxis(entityManager, gridEntity, grid, walkable, blocked,
                         friendlyPassFactionIds, start, goal, footprintSize, factionId,
                         xAxis: true, ref cursor, route) ||
                     !TryAppendAxis(entityManager, gridEntity, grid, walkable, blocked,
                         friendlyPassFactionIds, start, goal, footprintSize, factionId,
                         xAxis: false, ref cursor, route))
            {
                return false;
            }
            return route.Length > 0;
        }

        private static bool TryAppendAxis(
            EntityManager entityManager,
            Entity gridEntity,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            int2 start,
            int2 goal,
            int2 footprintSize,
            byte factionId,
            bool xAxis,
            ref int2 cursor,
            NativeList<int2> route)
        {
            if (xAxis)
            {
                int step = math.select(-1, 1, goal.x > cursor.x);
                while (cursor.x != goal.x)
                {
                    cursor.x += step;
                    if (!TryAppendValidatedCell(entityManager, gridEntity, grid, walkable, blocked,
                            friendlyPassFactionIds, start, goal, footprintSize, factionId, cursor, route))
                        return false;
                }
                return true;
            }

            int zStep = math.select(-1, 1, goal.y > cursor.y);
            while (cursor.y != goal.y)
            {
                cursor.y += zStep;
                if (!TryAppendValidatedCell(entityManager, gridEntity, grid, walkable, blocked,
                        friendlyPassFactionIds, start, goal, footprintSize, factionId, cursor, route))
                    return false;
            }
            return true;
        }

        private static bool TryAppendValidatedCell(
            EntityManager entityManager,
            Entity gridEntity,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            int2 start,
            int2 goal,
            int2 footprintSize,
            byte factionId,
            int2 cell,
            NativeList<int2> route)
        {
            if (route.Length >= MaximumRouteCells ||
                !GridUtils.InBounds(cell, grid.Width, grid.Height))
                return false;

            // The final footprint is validated by the formation resolver. Intermediate
            // cells follow the visible authored avenue so sparse dense-city road metadata
            // cannot redirect the tutorial squad through surrounding houses.
            route.Add(cell);
            return true;
        }
    }
}
