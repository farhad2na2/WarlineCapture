using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal struct UnitPathPlacementValidationSystem
{
    public const int VehicleOccupancyPaddingCells = 1;

    public static bool CanPlaceForPathing(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        NativeArray<Entity> liveUnitEntities,
        NativeArray<UnitGrid> liveUnitGrids,
        NativeArray<UnitFootprint> liveUnitFootprints,
        NativeArray<byte> liveUnitManualGroupMembers,
        Entity movingEntity,
        int2 cell,
        int2 footprintSize,
        int2 currentCell,
        bool isVehicle,
        bool manualMove,
        byte factionId,
        Entity ignoredOccupancyEntity = default,
        int2 ignoredOccupancyCell = default,
        int2 ignoredOccupancySize = default)
    {
        bool canPlace = isVehicle
            ? CanVehiclePlaceForPathing(
                grid,
                walkable,
                dynamicBlocked,
                friendlyPassFactionIds,
                occupied,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                movingEntity,
                cell,
                footprintSize,
                currentCell,
                factionId,
                ignoredOccupancyEntity,
                ignoredOccupancyCell,
                ignoredOccupancySize)
            : CanInfantryPlaceForPathing(
                grid,
                walkable,
                dynamicBlocked,
                friendlyPassFactionIds,
                occupied,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                liveUnitManualGroupMembers,
                movingEntity,
                cell,
                footprintSize,
                currentCell,
                manualMove,
                factionId,
                ignoredOccupancyEntity,
                ignoredOccupancyCell,
                ignoredOccupancySize);
        if (!canPlace)
            return false;

        if (!isVehicle)
            return true;

        return true;
    }

    private static bool CanInfantryPlaceForPathing(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        NativeArray<Entity> liveUnitEntities,
        NativeArray<UnitGrid> liveUnitGrids,
        NativeArray<UnitFootprint> liveUnitFootprints,
        NativeArray<byte> liveUnitManualGroupMembers,
        Entity movingEntity,
        int2 cell,
        int2 footprintSize,
        int2 currentCell,
        bool manualMove,
        byte factionId,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize)
    {
        int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
        int2 max = min + clamped;

        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int idx = row + x;
                if (walkable[idx].Value == 0)
                    return false;

                bool isCurrentFootprintCell = UnitFootprintUtility.ContainsCell(currentCell, clamped, new int2(x, y));
                if (!isCurrentFootprintCell &&
                    IsBlockedForFaction(dynamicBlocked, friendlyPassFactionIds, idx, factionId))
                    return false;

                bool isIgnoredOccupancyCell =
                    ignoredOccupancyEntity != Entity.Null &&
                    UnitFootprintUtility.ContainsCell(ignoredOccupancyCell, ignoredOccupancySize, new int2(x, y));
                if (!isCurrentFootprintCell && occupied.IsCreated && occupied.IsSet(idx) &&
                    !isIgnoredOccupancyCell &&
                    !ShouldIgnoreManualGroupOccupancy(manualMove, grid.Width, idx, liveUnitEntities, liveUnitGrids, liveUnitFootprints, liveUnitManualGroupMembers, movingEntity))
                    return false;
            }
        }

        for (int i = 0; i < liveUnitEntities.Length; i++)
        {
            Entity other = liveUnitEntities[i];
            if (other == movingEntity || other == ignoredOccupancyEntity)
                continue;

            if (manualMove && i < liveUnitManualGroupMembers.Length && liveUnitManualGroupMembers[i] != 0)
                continue;

            int2 otherCell = liveUnitGrids[i].Cell;
            int2 otherSize = liveUnitFootprints[i].Size;
            if (UnitFootprintUtility.Overlaps(cell, footprintSize, otherCell, otherSize) &&
                !UnitFootprintUtility.Overlaps(currentCell, footprintSize, otherCell, otherSize))
                return false;
        }

        return true;
    }

    private static bool ShouldIgnoreManualGroupOccupancy(
        bool manualMove,
        int gridWidth,
        int idx,
        NativeArray<Entity> liveUnitEntities,
        NativeArray<UnitGrid> liveUnitGrids,
        NativeArray<UnitFootprint> liveUnitFootprints,
        NativeArray<byte> liveUnitManualGroupMembers,
        Entity movingEntity)
    {
        if (!manualMove)
            return false;

        int2 cell = GridUtils.IndexToCell(idx, gridWidth);
        for (int i = 0; i < liveUnitEntities.Length; i++)
        {
            if (liveUnitEntities[i] == movingEntity)
                continue;
            if (i >= liveUnitManualGroupMembers.Length || liveUnitManualGroupMembers[i] == 0)
                continue;

            int2 otherSize = liveUnitFootprints[i].Size;
            if (UnitFootprintUtility.ContainsCell(liveUnitGrids[i].Cell, otherSize, cell))
                return true;
        }

        return false;
    }

    private static bool CanVehiclePlaceForPathing(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        NativeArray<Entity> liveUnitEntities,
        NativeArray<UnitGrid> liveUnitGrids,
        NativeArray<UnitFootprint> liveUnitFootprints,
        Entity movingEntity,
        int2 cell,
        int2 footprintSize,
        int2 currentCell,
        byte factionId,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize)
    {
        int padding = math.max(0, VehicleOccupancyPaddingCells);
        int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
        int2 max = min + clamped;

        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        int2 currentMin = UnitFootprintUtility.GetMinCell(currentCell, clamped);
        int2 currentMax = currentMin + clamped;
        int2 paddedMin = min - new int2(padding, padding);
        int2 paddedMax = max + new int2(padding, padding);
        int2 currentPaddedMin = currentMin - new int2(padding, padding);
        int2 currentPaddedMax = currentMax + new int2(padding, padding);

        if (paddedMin.x < 0 || paddedMin.y < 0 || paddedMax.x > grid.Width || paddedMax.y > grid.Height)
            return false;

        for (int y = paddedMin.y; y < paddedMax.y; y++)
        {
            int row = y * grid.Width;
            for (int x = paddedMin.x; x < paddedMax.x; x++)
            {
                bool insideActualFootprint = x >= min.x && x < max.x && y >= min.y && y < max.y;
                int idx = row + x;
                if (insideActualFootprint)
                {
                    if (walkable[idx].Value == 0)
                        return false;

                    bool isCurrentActualFootprintCell =
                        x >= currentMin.x && x < currentMax.x &&
                        y >= currentMin.y && y < currentMax.y;
                    if (!isCurrentActualFootprintCell &&
                        IsBlockedForFaction(dynamicBlocked, friendlyPassFactionIds, idx, factionId))
                        return false;
                }

                bool isCurrentClearanceCell =
                    x >= currentPaddedMin.x && x < currentPaddedMax.x &&
                    y >= currentPaddedMin.y && y < currentPaddedMax.y;
                bool isIgnoredOccupancyCell =
                    ignoredOccupancyEntity != Entity.Null &&
                    UnitFootprintUtility.ContainsCell(ignoredOccupancyCell, ignoredOccupancySize, new int2(x, y));
                if (!isCurrentClearanceCell && occupied.IsCreated && occupied.IsSet(idx))
                {
                    if (!isIgnoredOccupancyCell &&
                        !IsOnlySoftBlockerAtCell(grid.Width, idx, liveUnitEntities, liveUnitGrids, liveUnitFootprints, movingEntity))
                        return false;
                }
            }
        }

        for (int i = 0; i < liveUnitEntities.Length; i++)
        {
            Entity other = liveUnitEntities[i];
            if (other == movingEntity || other == ignoredOccupancyEntity)
                continue;

            int2 otherCell = liveUnitGrids[i].Cell;
            int2 otherSize = liveUnitFootprints[i].Size;
            if (UnitFootprintUtility.Overlaps(cell, footprintSize, otherCell, otherSize) &&
                !UnitFootprintUtility.Overlaps(currentCell, footprintSize, otherCell, otherSize) &&
                !IsSoftBlocker(otherSize))
                return false;
        }

        return true;
    }

    private static bool IsOnlySoftBlockerAtCell(
        int gridWidth,
        int idx,
        NativeArray<Entity> liveUnitEntities,
        NativeArray<UnitGrid> liveUnitGrids,
        NativeArray<UnitFootprint> liveUnitFootprints,
        Entity movingEntity)
    {
        int2 cell = GridUtils.IndexToCell(idx, gridWidth);
        bool foundSoft = false;
        for (int i = 0; i < liveUnitEntities.Length; i++)
        {
            if (liveUnitEntities[i] == movingEntity)
                continue;

            int2 otherSize = liveUnitFootprints[i].Size;
            if (!UnitFootprintUtility.ContainsCell(liveUnitGrids[i].Cell, otherSize, cell))
                continue;

            if (!IsSoftBlocker(otherSize))
                return false;

            foundSoft = true;
        }

        return foundSoft;
    }

    private static bool IsBlockedForFaction(
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        int idx,
        byte factionId)
    {
        if (!dynamicBlocked.IsCreated || !dynamicBlocked.IsSet(idx))
            return false;

        return !friendlyPassFactionIds.IsCreated ||
            (uint)idx >= (uint)friendlyPassFactionIds.Length ||
            friendlyPassFactionIds[idx] != factionId;
    }

    private static bool IsSoftBlocker(int2 size)
    {
        int2 clamped = UnitFootprintUtility.ClampSize(size);
        return clamped.x == 1 && clamped.y == 1;
    }
}
