using Unity.Collections;
using Unity.Mathematics;

public static class UnitFootprintUtility
{
    // Shared helpers for multi-cell units such as vehicles.
    public static int2 ClampSize(int2 size) => new(math.max(1, size.x), math.max(1, size.y));

    private static bool IsBlockedForFaction(in NativeBitArray blocked, in NativeArray<byte> friendlyPassFactionIds, int idx, byte factionId)
    {
        if (!blocked.IsCreated || !blocked.IsSet(idx))
            return false;

        if (friendlyPassFactionIds.IsCreated &&
            (uint)idx < (uint)friendlyPassFactionIds.Length &&
            friendlyPassFactionIds[idx] == factionId)
            return false;

        return true;
    }

    public static int2 GetMinCell(int2 centerCell, int2 size)
    {
        int2 clamped = ClampSize(size);
        return new int2(centerCell.x - ((clamped.x - 1) / 2), centerCell.y - ((clamped.y - 1) / 2));
    }

    public static bool CanPlace(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        int2 centerCell,
        int2 size,
        int2 currentCenterCell,
        byte factionId)
    {
        int2 clamped = ClampSize(size);
        int2 min = GetMinCell(centerCell, clamped);
        int2 max = min + clamped;

        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        int2 currentMin = GetMinCell(currentCenterCell, clamped);
        int2 currentMax = currentMin + clamped;

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int idx = row + x;
                if (walkable[idx].Value == 0)
                    return false;
                if (IsBlockedForFaction(blocked, friendlyPassFactionIds, idx, factionId))
                    return false;

                bool isCurrentFootprintCell =
                    x >= currentMin.x && x < currentMax.x &&
                    y >= currentMin.y && y < currentMax.y;
                if (!isCurrentFootprintCell && occupied.IsCreated && occupied.IsSet(idx))
                    return false;
            }
        }

        return true;
    }

    public static bool CanPlaceWithPadding(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        int2 centerCell,
        int2 size,
        int2 currentCenterCell,
        int occupiedPadding,
        byte factionId)
    {
        int padding = math.max(0, occupiedPadding);
        if (padding == 0)
            return CanPlace(grid, walkable, blocked, friendlyPassFactionIds, occupied, centerCell, size, currentCenterCell, factionId);

        int2 clamped = ClampSize(size);
        int2 min = GetMinCell(centerCell, clamped);
        int2 max = min + clamped;

        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        int2 currentMin = GetMinCell(currentCenterCell, clamped);
        int2 currentMax = currentMin + clamped;
        int2 paddedMin = min - new int2(padding, padding);
        int2 paddedMax = max + new int2(padding, padding);

        if (paddedMin.x < 0 || paddedMin.y < 0 || paddedMax.x > grid.Width || paddedMax.y > grid.Height)
            return false;

        for (int y = paddedMin.y; y < paddedMax.y; y++)
        {
            int row = y * grid.Width;
            for (int x = paddedMin.x; x < paddedMax.x; x++)
            {
                bool insideActualFootprint = x >= min.x && x < max.x && y >= min.y && y < max.y;
                if (insideActualFootprint)
                {
                    int idx = row + x;
                    if (walkable[idx].Value == 0)
                        return false;
                    if (IsBlockedForFaction(blocked, friendlyPassFactionIds, idx, factionId))
                        return false;
                }

                bool isCurrentFootprintCell =
                    x >= currentMin.x && x < currentMax.x &&
                    y >= currentMin.y && y < currentMax.y;
                if (!isCurrentFootprintCell && occupied.IsCreated && occupied.IsSet(row + x))
                    return false;
            }
        }

        return true;
    }

    public static bool ContainsCell(int2 centerCell, int2 size, int2 cell)
    {
        int2 clamped = ClampSize(size);
        int2 min = GetMinCell(centerCell, clamped);
        int2 max = min + clamped;
        return cell.x >= min.x && cell.x < max.x && cell.y >= min.y && cell.y < max.y;
    }

    public static bool ContainsCellWithPadding(int2 centerCell, int2 size, int2 cell, int padding)
    {
        int2 clamped = ClampSize(size);
        int2 min = GetMinCell(centerCell, clamped) - new int2(math.max(0, padding), math.max(0, padding));
        int2 max = min + clamped + new int2(math.max(0, padding), math.max(0, padding)) * 2;
        return cell.x >= min.x && cell.x < max.x && cell.y >= min.y && cell.y < max.y;
    }

    public static bool Overlaps(int2 centerA, int2 sizeA, int2 centerB, int2 sizeB)
    {
        int2 clampedA = ClampSize(sizeA);
        int2 clampedB = ClampSize(sizeB);

        int2 minA = GetMinCell(centerA, clampedA);
        int2 maxA = minA + clampedA;
        int2 minB = GetMinCell(centerB, clampedB);
        int2 maxB = minB + clampedB;

        return minA.x < maxB.x && maxA.x > minB.x &&
               minA.y < maxB.y && maxA.y > minB.y;
    }
}
