using Unity.Collections;
using Unity.Mathematics;

internal struct UnitHierarchicalPathPlanner
{
    public const int SectorSizeCells = 32;
    public const int MaxExpandedSectors = 2048;

    private const int FreeTraversalCost = 10;
    private const int FreeDiagonalTraversalCost = 14;

    private static readonly int2[] SearchDirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    public bool TryFindWaypoint(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        ref UnitPathCoarseWorkspace coarseWorkspace,
        int2 start,
        int2 requestedGoal,
        float maxSegmentCells,
        byte factionId,
        out int2 waypoint)
    {
        waypoint = start;
        if (!GridUtils.InBounds(start, grid.Width, grid.Height) ||
            !GridUtils.InBounds(requestedGoal, grid.Width, grid.Height))
        {
            return false;
        }

        int2 startSector = CellToSector(start, coarseWorkspace);
        int2 goalSector = CellToSector(requestedGoal, coarseWorkspace);
        if (startSector.Equals(goalSector))
            return false;

        if (!TryFindRepresentativeCell(grid, walkable, dynamicBlocked, friendlyPassFactionIds, startSector, start, factionId, out _) ||
            !TryFindRepresentativeCell(grid, walkable, dynamicBlocked, friendlyPassFactionIds, goalSector, requestedGoal, factionId, out _))
        {
            return false;
        }

        int epoch = coarseWorkspace.ReserveSearchEpoch();
        int startIndex = coarseWorkspace.Index(startSector);
        int goalIndex = coarseWorkspace.Index(goalSector);
        int openCount = 0;
        int expanded = 0;

        coarseWorkspace.CameFrom[startIndex] = startIndex;
        coarseWorkspace.GScore[startIndex] = 0;
        coarseWorkspace.Epoch[startIndex] = epoch;
        coarseWorkspace.OpenEpoch[startIndex] = epoch;
        coarseWorkspace.Open[openCount++] = startIndex;

        while (openCount > 0 && expanded < MaxExpandedSectors)
        {
            int bestOpenSlot = 0;
            int bestIndex = coarseWorkspace.Open[0];
            int bestScore = coarseWorkspace.GScore[bestIndex] + HeuristicOctile(coarseWorkspace.ToSector(bestIndex), goalSector);
            for (int i = 1; i < openCount; i++)
            {
                int candidateIndex = coarseWorkspace.Open[i];
                int score = coarseWorkspace.GScore[candidateIndex] + HeuristicOctile(coarseWorkspace.ToSector(candidateIndex), goalSector);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestIndex = candidateIndex;
                bestOpenSlot = i;
            }

            openCount--;
            coarseWorkspace.Open[bestOpenSlot] = coarseWorkspace.Open[openCount];
            coarseWorkspace.OpenEpoch[bestIndex] = 0;
            if (coarseWorkspace.ClosedEpoch[bestIndex] == epoch)
                continue;

            coarseWorkspace.ClosedEpoch[bestIndex] = epoch;
            expanded++;

            if (bestIndex == goalIndex)
                return TryChooseWaypointFromCoarsePath(grid, walkable, dynamicBlocked, friendlyPassFactionIds, ref coarseWorkspace, start, requestedGoal, maxSegmentCells, factionId, startIndex, goalIndex, out waypoint);

            int2 currentSector = coarseWorkspace.ToSector(bestIndex);
            for (int i = 0; i < SearchDirs.Length; i++)
            {
                int2 nextSector = currentSector + SearchDirs[i];
                if (!coarseWorkspace.InBounds(nextSector))
                    continue;

                int nextIndex = coarseWorkspace.Index(nextSector);
                if (coarseWorkspace.ClosedEpoch[nextIndex] == epoch)
                    continue;

                if (!TryFindRepresentativeCell(grid, walkable, dynamicBlocked, friendlyPassFactionIds, nextSector, SectorCenterCell(grid, nextSector), factionId, out _))
                    continue;

                int stepCost = math.abs(SearchDirs[i].x) + math.abs(SearchDirs[i].y) == 2
                    ? FreeDiagonalTraversalCost
                    : FreeTraversalCost;
                int nextG = coarseWorkspace.GScore[bestIndex] + stepCost;
                if (coarseWorkspace.Epoch[nextIndex] == epoch && nextG >= coarseWorkspace.GScore[nextIndex])
                    continue;

                coarseWorkspace.CameFrom[nextIndex] = bestIndex;
                coarseWorkspace.GScore[nextIndex] = nextG;
                coarseWorkspace.Epoch[nextIndex] = epoch;
                if (coarseWorkspace.OpenEpoch[nextIndex] != epoch && openCount < coarseWorkspace.Open.Length)
                {
                    coarseWorkspace.Open[openCount++] = nextIndex;
                    coarseWorkspace.OpenEpoch[nextIndex] = epoch;
                }
            }
        }

        return false;
    }

    public bool TryChooseWaypointFromCoarsePath(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        ref UnitPathCoarseWorkspace coarseWorkspace,
        int2 start,
        int2 requestedGoal,
        float maxSegmentCells,
        byte factionId,
        int startIndex,
        int goalIndex,
        out int2 waypoint)
    {
        waypoint = start;
        int current = goalIndex;
        int chosen = -1;
        int guard = 0;

        while (current != startIndex && guard++ < coarseWorkspace.CameFrom.Length)
        {
            int2 sector = coarseWorkspace.ToSector(current);
            int2 preferredCell = current == goalIndex ? requestedGoal : SectorCenterCell(grid, sector);
            if (TryFindRepresentativeCell(grid, walkable, dynamicBlocked, friendlyPassFactionIds, sector, preferredCell, factionId, out int2 candidate))
            {
                float distance = math.distance(new float2(start.x, start.y), new float2(candidate.x, candidate.y));
                if (distance <= maxSegmentCells)
                {
                    chosen = current;
                    break;
                }
            }

            int previous = coarseWorkspace.CameFrom[current];
            if (previous == current)
                break;
            current = previous;
        }

        if (chosen < 0)
            return false;

        int2 chosenSector = coarseWorkspace.ToSector(chosen);
        int2 chosenPreferred = chosen == goalIndex ? requestedGoal : SectorCenterCell(grid, chosenSector);
        if (!TryFindRepresentativeCell(grid, walkable, dynamicBlocked, friendlyPassFactionIds, chosenSector, chosenPreferred, factionId, out waypoint))
            return false;

        return !waypoint.Equals(start);
    }

    private static int HeuristicOctile(int2 a, int2 b)
    {
        int dx = math.abs(a.x - b.x);
        int dy = math.abs(a.y - b.y);
        int diagonal = math.min(dx, dy);
        int straight = math.max(dx, dy) - diagonal;
        return (diagonal * FreeDiagonalTraversalCost) + (straight * FreeTraversalCost);
    }

    private static int2 CellToSector(int2 cell, in UnitPathCoarseWorkspace coarseWorkspace)
    {
        return new int2(
            math.clamp(cell.x / SectorSizeCells, 0, coarseWorkspace.Width - 1),
            math.clamp(cell.y / SectorSizeCells, 0, coarseWorkspace.Height - 1));
    }

    private static int2 SectorCenterCell(in GridConfig grid, int2 sector)
    {
        int minX = sector.x * SectorSizeCells;
        int minY = sector.y * SectorSizeCells;
        int maxX = math.min(minX + SectorSizeCells - 1, grid.Width - 1);
        int maxY = math.min(minY + SectorSizeCells - 1, grid.Height - 1);
        return new int2((minX + maxX) / 2, (minY + maxY) / 2);
    }

    public static bool TryFindRepresentativeCell(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        int2 sector,
        int2 preferredCell,
        byte factionId,
        out int2 representative)
    {
        representative = default;
        int minX = sector.x * SectorSizeCells;
        int minY = sector.y * SectorSizeCells;
        int maxX = math.min(minX + SectorSizeCells - 1, grid.Width - 1);
        int maxY = math.min(minY + SectorSizeCells - 1, grid.Height - 1);
        int2 clampedPreferred = new int2(
            math.clamp(preferredCell.x, minX, maxX),
            math.clamp(preferredCell.y, minY, maxY));

        if (IsCoarseCellPassable(grid, walkable, dynamicBlocked, friendlyPassFactionIds, clampedPreferred, factionId))
        {
            representative = clampedPreferred;
            return true;
        }

        for (int radius = 2; radius <= SectorSizeCells / 2; radius += 2)
        {
            int steps = radius * 8;
            for (int step = 0; step < steps; step += 2)
            {
                int2 candidate = clampedPreferred + SquareRingOffset(radius, step);
                if (candidate.x < minX || candidate.x > maxX || candidate.y < minY || candidate.y > maxY)
                    continue;

                if (!IsCoarseCellPassable(grid, walkable, dynamicBlocked, friendlyPassFactionIds, candidate, factionId))
                    continue;

                representative = candidate;
                return true;
            }
        }

        for (int y = minY; y <= maxY; y += 4)
        {
            for (int x = minX; x <= maxX; x += 4)
            {
                int2 candidate = new int2(x, y);
                if (!IsCoarseCellPassable(grid, walkable, dynamicBlocked, friendlyPassFactionIds, candidate, factionId))
                    continue;

                representative = candidate;
                return true;
            }
        }

        return false;
    }

    public static bool IsCoarseCellPassable(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        int2 cell,
        byte factionId)
    {
        if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
            return false;

        int index = GridUtils.CellToIndex(cell, grid.Width);
        if ((uint)index >= (uint)walkable.Length || walkable[index].Value == 0)
            return false;

        if (!dynamicBlocked.IsCreated || !dynamicBlocked.IsSet(index))
            return true;

        return friendlyPassFactionIds.IsCreated &&
               (uint)index < (uint)friendlyPassFactionIds.Length &&
               friendlyPassFactionIds[index] == factionId;
    }

    private static int2 SquareRingOffset(int r, int step)
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
