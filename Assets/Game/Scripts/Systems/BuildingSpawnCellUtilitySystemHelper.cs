using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingSpawnCellUtilitySystemHelper
{
    public int2 FindSpawnCellAdjacentToBuilding(
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        Vector2Int originCell,
        Vector2Int footprintCells,
        int2 fallbackCenter)
    {
        int maxRadius = math.max(grid.Width, grid.Height);
        for (int extraRadius = 1; extraRadius <= maxRadius; extraRadius++)
        {
            if (TryReservePerimeterCell(
                    ref rng,
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    ref reserved,
                    originCell,
                    footprintCells,
                    extraRadius,
                    out int2 cell))
            {
                return cell;
            }
        }

        return SpawnCellUtility.FindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved, fallbackCenter, math.max(footprintCells.x, footprintCells.y) + 4);
    }

    public bool TryReservePerimeterCell(
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        Vector2Int originCell,
        Vector2Int footprintCells,
        int extraRadius,
        out int2 cell)
    {
        cell = default;

        int minX = originCell.x - extraRadius;
        int minY = originCell.y - extraRadius;
        int maxX = originCell.x + footprintCells.x - 1 + extraRadius;
        int maxY = originCell.y + footprintCells.y - 1 + extraRadius;

        var candidates = new NativeList<int2>(Allocator.Temp);
        try
        {
            for (int x = minX; x <= maxX; x++)
            {
                TryAddPerimeterCandidate(grid, walkable, blocked, occupied, reserved, x, minY, ref candidates);
                if (maxY != minY)
                    TryAddPerimeterCandidate(grid, walkable, blocked, occupied, reserved, x, maxY, ref candidates);
            }

            for (int y = minY + 1; y < maxY; y++)
            {
                TryAddPerimeterCandidate(grid, walkable, blocked, occupied, reserved, minX, y, ref candidates);
                if (maxX != minX)
                    TryAddPerimeterCandidate(grid, walkable, blocked, occupied, reserved, maxX, y, ref candidates);
            }

            if (candidates.Length == 0)
                return false;

            int startIndex = rng.NextInt(candidates.Length);
            for (int offset = 0; offset < candidates.Length; offset++)
            {
                int2 candidate = candidates[(startIndex + offset) % candidates.Length];
                int index = GridUtils.CellToIndex(candidate, grid.Width);
                if (reserved.IsSet(index))
                    continue;

                reserved.Set(index, true);
                cell = candidate;
                return true;
            }

            return false;
        }
        finally
        {
            if (candidates.IsCreated)
                candidates.Dispose();
        }
    }

    public void TryAddPerimeterCandidate(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        in NativeBitArray reserved,
        int x,
        int y,
        ref NativeList<int2> candidates)
    {
        if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
            return;

        int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
        if (walkable[index].Value == 0 || blocked.IsSet(index) || occupied.IsSet(index) || reserved.IsSet(index))
            return;

        candidates.Add(new int2(x, y));
    }
}
