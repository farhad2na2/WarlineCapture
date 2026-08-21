using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class CampaignMissionGuidedStreetPathUtility
    {
        private const int MaximumRouteCells = 128;

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
