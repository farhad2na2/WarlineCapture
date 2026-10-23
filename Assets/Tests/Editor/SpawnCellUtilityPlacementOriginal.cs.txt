using Unity.Collections;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    public static class SpawnCellUtility
    {
        public static int2 FindSpawnCellNear(
            ref Random rng,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 center,
            int radiusCells,
            int2 footprintSize)
        {
            return TryFindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved, center, radiusCells, footprintSize, out int2 cell)
                ? cell
                : center;
        }

        public static bool TryFindSpawnCellNear(
            ref Random rng,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 center,
            int radiusCells,
            int2 footprintSize,
            out int2 cell)
        {
            center.x = math.clamp(center.x, 0, grid.Width - 1);
            center.y = math.clamp(center.y, 0, grid.Height - 1);
            radiusCells = math.max(0, radiusCells);
            cell = default;

            if (TryReserveCell(grid, walkable, blocked, occupied, ref reserved, center.x, center.y, footprintSize, out int2 exactCell))
            {
                cell = exactCell;
                return true;
            }

            const int randomTries = 128;
            for (int i = 0; i < randomTries; i++)
            {
                int x = center.x + rng.NextInt(-radiusCells, radiusCells + 1);
                int y = center.y + rng.NextInt(-radiusCells, radiusCells + 1);
                if (TryReserveCell(grid, walkable, blocked, occupied, ref reserved, x, y, footprintSize, out int2 randomCell))
                {
                    cell = randomCell;
                    return true;
                }
            }

            int maxRadius = math.max(8, radiusCells + 32);
            for (int r = 0; r <= maxRadius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (math.abs(dx) != r && math.abs(dy) != r)
                            continue;

                        int x = center.x + dx;
                        int y = center.y + dy;
                        if (TryReserveCell(grid, walkable, blocked, occupied, ref reserved, x, y, footprintSize, out int2 ringCell))
                        {
                            cell = ringCell;
                            return true;
                        }
                    }
                }
            }

            return TryFindAnyFreeCell(grid, walkable, blocked, occupied, ref reserved, footprintSize, out cell);
        }

        public static int2 FindSpawnCellNear(
            ref Random rng,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 center,
            int radiusCells)
        {
            return FindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved, center, radiusCells, new int2(1, 1));
        }

        private static bool TryFindAnyFreeCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 footprintSize,
            out int2 cell)
        {
            cell = default;
            int gridSize = grid.Width * grid.Height;
            for (int idx = 0; idx < gridSize; idx++)
            {
                int2 candidate = GridUtils.IndexToCell(idx, grid.Width);
                if (!IsCellFree(grid, walkable, blocked, occupied, reserved, candidate.x, candidate.y, footprintSize))
                    continue;

                ReserveFootprint(grid, ref reserved, candidate, footprintSize);
                cell = candidate;
                return true;
            }

            return false;
        }

        private static bool TryReserveCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int x,
            int y,
            int2 footprintSize,
            out int2 cell)
        {
            cell = default;
            if (!IsCellFree(grid, walkable, blocked, occupied, reserved, x, y, footprintSize))
                return false;

            cell = new int2(x, y);
            ReserveFootprint(grid, ref reserved, cell, footprintSize);
            return true;
        }

        private static bool IsCellFree(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            in NativeBitArray reserved,
            int x,
            int y,
            int2 footprintSize)
        {
            int2 center = new int2(x, y);
            int2 size = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(center, size);
            int2 max = min + size;
            if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
                return false;

            for (int yy = min.y; yy < max.y; yy++)
            {
                int row = yy * grid.Width;
                for (int xx = min.x; xx < max.x; xx++)
                {
                    int idx = row + xx;
                    if (walkable[idx].Value == 0)
                        return false;
                    if (blocked.IsSet(idx) || occupied.IsSet(idx) || reserved.IsSet(idx))
                        return false;
                }
            }

            return true;
        }

        private static void ReserveFootprint(in GridConfig grid, ref NativeBitArray reserved, int2 center, int2 footprintSize)
        {
            int2 size = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(center, size);
            int2 max = min + size;
            for (int y = min.y; y < max.y; y++)
            {
                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                    reserved.Set(row + x, true);
            }
        }
    }
}
