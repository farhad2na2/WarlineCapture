using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal struct UnitPathGoalAssignment
{
    public const int InfantryGoalSearchRadius = 10;
    public const int VehicleGoalSearchRadius = 20;
    public const int InfantryAlternateGoalCandidates = 16;
    public const int VehicleAlternateGoalCandidates = 32;

    public int2 FindNearestFreeGoal(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        NativeArray<Entity> liveUnitEntities,
        NativeArray<UnitGrid> liveUnitGrids,
        NativeArray<UnitFootprint> liveUnitFootprints,
        NativeArray<int> reservedGoalEpochs,
        int reservedGoalGeneration,
        Entity movingEntity,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        MapSurfacePathfindingSnapshot.Context surfaceContext,
        int2 desiredGoal,
        int2 startCell,
        int2 footprintSize,
        byte factionId,
        int startIndex)
    {
        int2 bestCell = startCell;
        int bestDistanceSq = int.MaxValue;

        void ConsiderBest(int2 cell)
        {
            int dx = cell.x - desiredGoal.x;
            int dy = cell.y - desiredGoal.y;
            int distSq = (dx * dx) + (dy * dy);
            if (distSq < bestDistanceSq)
            {
                bestDistanceSq = distSq;
                bestCell = cell;
            }
        }

        if (GridUtils.InBounds(desiredGoal, grid.Width, grid.Height))
        {
            int desiredIndex = GridUtils.CellToIndex(desiredGoal, grid.Width);
            if (desiredIndex == startIndex)
                return desiredGoal;
            if (CanUseGoalCell(
                    grid,
                    walkable,
                    dynamicBlocked,
                    friendlyPassFactionIds,
                    occupied,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    reservedGoalEpochs,
                    reservedGoalGeneration,
                    movingEntity,
                    ignoredOccupancyEntity,
                    ignoredOccupancyCell,
                    ignoredOccupancySize,
                    surfaceContext,
                    desiredGoal,
                    footprintSize,
                    startCell,
                    factionId))
            {
                UnitPathReservedGoal.ReserveGoalFootprint(grid, reservedGoalEpochs, reservedGoalGeneration, desiredGoal, footprintSize);
                return desiredGoal;
            }
        }

        bool isVehicle = footprintSize.x > 1 || footprintSize.y > 1;
        int maxRadius = math.min(
            math.max(grid.Width, grid.Height),
            isVehicle ? VehicleGoalSearchRadius : InfantryGoalSearchRadius);
        uint seed = math.hash(new int3(desiredGoal.x, desiredGoal.y, startIndex));
        for (int r = 1; r <= maxRadius; r++)
        {
            int ringLen = 8 * r;
            int startStep = (int)(seed % (uint)ringLen);

            for (int step = 0; step < ringLen; step++)
            {
                int s = startStep + step;
                if (s >= ringLen) s -= ringLen;

                var offset = SquareRingOffset(r, s);
                var cell = desiredGoal + offset;
                if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                    continue;

                int idx = GridUtils.CellToIndex(cell, grid.Width);
                if (idx == startIndex)
                    continue;

                if (CanUseGoalCell(
                        grid,
                        walkable,
                        dynamicBlocked,
                        friendlyPassFactionIds,
                        occupied,
                        liveUnitEntities,
                        liveUnitGrids,
                        liveUnitFootprints,
                        reservedGoalEpochs,
                        reservedGoalGeneration,
                        movingEntity,
                        ignoredOccupancyEntity,
                        ignoredOccupancyCell,
                        ignoredOccupancySize,
                        surfaceContext,
                        cell,
                        footprintSize,
                        startCell,
                        factionId))
                {
                    ConsiderBest(cell);
                    UnitPathReservedGoal.ReserveGoalFootprint(grid, reservedGoalEpochs, reservedGoalGeneration, cell, footprintSize);
                    return bestCell;
                }
            }
        }

        if (bestDistanceSq != int.MaxValue)
        {
            UnitPathReservedGoal.ReserveGoalFootprint(grid, reservedGoalEpochs, reservedGoalGeneration, bestCell, footprintSize);
            return bestCell;
        }

        return startCell;
    }

    public static bool CanUseGoalCell(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        NativeArray<Entity> liveUnitEntities,
        NativeArray<UnitGrid> liveUnitGrids,
        NativeArray<UnitFootprint> liveUnitFootprints,
        NativeArray<int> reservedGoalEpochs,
        int reservedGoalGeneration,
        Entity movingEntity,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        MapSurfacePathfindingSnapshot.Context surfaceContext,
        int2 cell,
        int2 footprintSize,
        int2 startCell,
        byte factionId)
    {
        bool isVehicle = footprintSize.x > 1 || footprintSize.y > 1;
        if (!UnitPathPlacementValidation.CanPlaceForPathing(grid, walkable, dynamicBlocked, friendlyPassFactionIds, occupied, liveUnitEntities, liveUnitGrids, liveUnitFootprints, default, movingEntity, cell, footprintSize, startCell, isVehicle, false, factionId, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize))
            return false;

        MapSurfaceTraversalValidation surfaceValidation = new();
        if (!surfaceValidation.CanTraverseFootprint(surfaceContext.Surface, surfaceContext.HasSurfaceData, grid, cell, footprintSize, isVehicle))
            return false;

        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                if (reservedGoalEpochs[row + x] == reservedGoalGeneration)
                    return false;
            }
        }

        return true;
    }

    public static bool IsFree(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        int cellIndex,
        byte factionId) =>
        (uint)cellIndex < (uint)(grid.Width * grid.Height) &&
        walkable[cellIndex].Value != 0 &&
        (!dynamicBlocked.IsSet(cellIndex) ||
         ((uint)cellIndex < (uint)friendlyPassFactionIds.Length && friendlyPassFactionIds[cellIndex] == factionId)) &&
        !occupied.IsSet(cellIndex);

    public static int2 SquareRingOffset(int r, int step)
    {
        int topLen = (2 * r) + 1;
        if (step < topLen)
            return new int2(-r + step, r);

        step -= topLen;
        int rightLen = 2 * r;
        if (step < rightLen)
            return new int2(r, (r - 1) - step);

        step -= rightLen;
        int bottomLen = 2 * r;
        if (step < bottomLen)
            return new int2((r - 1) - step, -r);

        step -= bottomLen;
        return new int2(-r, (-r + 1) + step);
    }
}
