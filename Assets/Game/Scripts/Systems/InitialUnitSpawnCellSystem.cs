using Unity.Collections;
using Unity.Mathematics;

public readonly struct InitialUnitSpawnCellSystem
{
    public bool TryFindInitialUnitSpawnCell(
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        int2 center,
        int radiusCells,
        int2 footprintSize,
        bool isAirUnit,
        out int2 cell)
    {
        if (isAirUnit &&
            TryReserveInitialAirSpawnCell(grid, walkable, blocked, occupied, ref reserved, center, footprintSize, out cell))
        {
            return true;
        }

        return SpawnCellUtility.TryFindSpawnCellNear(
            ref rng,
            grid,
            walkable,
            blocked,
            occupied,
            ref reserved,
            center,
            radiusCells,
            footprintSize,
            out cell);
    }

    private static bool TryReserveInitialAirSpawnCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        int2 center,
        int2 footprintSize,
        out int2 cell)
    {
        cell = default;
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(center, size);
        int2 max = min + size;
        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int index = row + x;
                if (walkable[index].Value == 0 || occupied.IsSet(index))
                    return false;
                if (reserved.IsSet(index) && !blocked.IsSet(index))
                    return false;
            }
        }

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
                reserved.Set(row + x, true);
        }

        cell = center;
        return true;
    }
}
