// Shared path placement policy. Storage and entity queries are supplied explicitly by the host.
namespace Game.Runtime.Pathfinding
{
    public struct PlacementCell
    {
        public int x, y;
        public PlacementCell(int x, int y) { this.x=x; this.y=y; }
        public static PlacementCell operator +(PlacementCell a, PlacementCell b) => new PlacementCell(a.x+b.x,a.y+b.y);
        public static PlacementCell operator -(PlacementCell a, PlacementCell b) => new PlacementCell(a.x-b.x,a.y-b.y);
    }
    public struct PlacementIdentity
    {
        public int Index, Version;
        public PlacementIdentity(int index,int version) { Index=index; Version=version; }
        public static PlacementIdentity Null => default;
        public static bool operator ==(PlacementIdentity a,PlacementIdentity b) => a.Index==b.Index && a.Version==b.Version;
        public static bool operator !=(PlacementIdentity a,PlacementIdentity b) => !(a==b);
        public override bool Equals(object other) => other is PlacementIdentity identity && this==identity;
        public override int GetHashCode() => unchecked(Index*397 ^ Version);
    }
    public interface IPathPlacementContext
    {
        int Width { get; } int Height { get; }
        bool HasDynamicBlocked { get; } bool HasOccupied { get; } bool HasFriendlyPass { get; }
        int FriendlyPassLength { get; } int UnitCount { get; } int ManualGroupCount { get; }
        byte Walkable(int index); bool DynamicBlocked(int index); bool Occupied(int index); byte FriendlyPass(int index);
        PlacementIdentity UnitEntity(int index); PlacementCell UnitCell(int index); PlacementCell UnitSize(int index); byte ManualGroup(int index);
    }
    internal static class PlacementFootprint
    {
        internal static PlacementCell ClampSize(PlacementCell size) => new PlacementCell(System.Math.Max(1,size.x),System.Math.Max(1,size.y));
        internal static PlacementCell GetMinCell(PlacementCell center,PlacementCell size)
        { var s=ClampSize(size); return new PlacementCell(center.x-(s.x-1)/2,center.y-(s.y-1)/2); }
        internal static bool ContainsCell(PlacementCell center,PlacementCell size,PlacementCell cell)
        { var s=ClampSize(size); var min=GetMinCell(center,s); var max=min+s; return cell.x>=min.x&&cell.x<max.x&&cell.y>=min.y&&cell.y<max.y; }
        internal static bool Overlaps(PlacementCell a,PlacementCell sizeA,PlacementCell b,PlacementCell sizeB)
        { var sa=ClampSize(sizeA);var sb=ClampSize(sizeB);var minA=GetMinCell(a,sa);var maxA=minA+sa;var minB=GetMinCell(b,sb);var maxB=minB+sb;return minA.x<maxB.x&&maxA.x>minB.x&&minA.y<maxB.y&&maxA.y>minB.y; }
    }
}
namespace Game.Runtime.Pathfinding
{
    public static class PathPlacementKernel
    {
        public const int VehicleOccupancyPaddingCells = 1;

        public static bool CanPlaceForPathing<T>(
            ref T context,
            PlacementIdentity movingEntity,
            PlacementCell cell,
            PlacementCell footprintSize,
            PlacementCell currentCell,
            bool isVehicle,
            bool manualMove,
            byte factionId,
            PlacementIdentity ignoredOccupancyEntity = default,
            PlacementCell ignoredOccupancyCell = default,
            PlacementCell ignoredOccupancySize = default) where T : struct, IPathPlacementContext
        {
            bool canPlace = isVehicle
                ? CanVehiclePlaceForPathing(ref context,
                    movingEntity,
                    cell,
                    footprintSize,
                    currentCell,
                    factionId,
                    ignoredOccupancyEntity,
                    ignoredOccupancyCell,
                    ignoredOccupancySize)
                : CanInfantryPlaceForPathing(ref context,
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

        private static bool CanInfantryPlaceForPathing<T>(
            ref T context,
            PlacementIdentity movingEntity,
            PlacementCell cell,
            PlacementCell footprintSize,
            PlacementCell currentCell,
            bool manualMove,
            byte factionId,
            PlacementIdentity ignoredOccupancyEntity,
            PlacementCell ignoredOccupancyCell,
            PlacementCell ignoredOccupancySize) where T : struct, IPathPlacementContext
        {
            PlacementCell clamped = PlacementFootprint.ClampSize(footprintSize);
            PlacementCell min = PlacementFootprint.GetMinCell(cell, clamped);
            PlacementCell max = min + clamped;

            if (min.x < 0 || min.y < 0 || max.x > context.Width || max.y > context.Height)
                return false;

            for (int y = min.y; y < max.y; y++)
            {
                int row = y * context.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    int idx = row + x;
                    if (context.Walkable(idx) == 0)
                        return false;

                    bool isCurrentFootprintCell = PlacementFootprint.ContainsCell(currentCell, clamped, new PlacementCell(x, y));
                    if (!isCurrentFootprintCell &&
                        IsBlockedForFaction(ref context, idx, factionId))
                        return false;

                    bool isIgnoredOccupancyCell =
                        ignoredOccupancyEntity != PlacementIdentity.Null &&
                        PlacementFootprint.ContainsCell(ignoredOccupancyCell, ignoredOccupancySize, new PlacementCell(x, y));
                    if (!isCurrentFootprintCell && context.HasOccupied && context.Occupied(idx) &&
                        !isIgnoredOccupancyCell &&
                        !ShouldIgnoreManualGroupOccupancy(ref context, manualMove, context.Width, idx, movingEntity))
                        return false;
                }
            }

            for (int i = 0; i < context.UnitCount; i++)
            {
                PlacementIdentity other = context.UnitEntity(i);
                if (other == movingEntity || other == ignoredOccupancyEntity)
                    continue;

                if (manualMove && i < context.ManualGroupCount && context.ManualGroup(i) != 0)
                    continue;

                PlacementCell otherCell = context.UnitCell(i);
                PlacementCell otherSize = context.UnitSize(i);
                if (PlacementFootprint.Overlaps(cell, footprintSize, otherCell, otherSize) &&
                    !PlacementFootprint.Overlaps(currentCell, footprintSize, otherCell, otherSize))
                    return false;
            }

            return true;
        }

        private static bool ShouldIgnoreManualGroupOccupancy<T>(
            ref T context,
            bool manualMove,
            int gridWidth,
            int idx,
            PlacementIdentity movingEntity) where T : struct, IPathPlacementContext
        {
            if (!manualMove)
                return false;

            PlacementCell cell = new PlacementCell(idx % gridWidth, idx / gridWidth);
            for (int i = 0; i < context.UnitCount; i++)
            {
                if (context.UnitEntity(i) == movingEntity)
                    continue;
                if (i >= context.ManualGroupCount || context.ManualGroup(i) == 0)
                    continue;

                PlacementCell otherSize = context.UnitSize(i);
                if (PlacementFootprint.ContainsCell(context.UnitCell(i), otherSize, cell))
                    return true;
            }

            return false;
        }

        private static bool CanVehiclePlaceForPathing<T>(
            ref T context,
            PlacementIdentity movingEntity,
            PlacementCell cell,
            PlacementCell footprintSize,
            PlacementCell currentCell,
            byte factionId,
            PlacementIdentity ignoredOccupancyEntity,
            PlacementCell ignoredOccupancyCell,
            PlacementCell ignoredOccupancySize) where T : struct, IPathPlacementContext
        {
            int padding = System.Math.Max(0, VehicleOccupancyPaddingCells);
            PlacementCell clamped = PlacementFootprint.ClampSize(footprintSize);
            PlacementCell min = PlacementFootprint.GetMinCell(cell, clamped);
            PlacementCell max = min + clamped;

            if (min.x < 0 || min.y < 0 || max.x > context.Width || max.y > context.Height)
                return false;

            PlacementCell currentMin = PlacementFootprint.GetMinCell(currentCell, clamped);
            PlacementCell currentMax = currentMin + clamped;
            PlacementCell paddedMin = min - new PlacementCell(padding, padding);
            PlacementCell paddedMax = max + new PlacementCell(padding, padding);
            PlacementCell currentPaddedMin = currentMin - new PlacementCell(padding, padding);
            PlacementCell currentPaddedMax = currentMax + new PlacementCell(padding, padding);

            if (paddedMin.x < 0 || paddedMin.y < 0 || paddedMax.x > context.Width || paddedMax.y > context.Height)
                return false;

            for (int y = paddedMin.y; y < paddedMax.y; y++)
            {
                int row = y * context.Width;
                for (int x = paddedMin.x; x < paddedMax.x; x++)
                {
                    bool insideActualFootprint = x >= min.x && x < max.x && y >= min.y && y < max.y;
                    int idx = row + x;
                    if (insideActualFootprint)
                    {
                        if (context.Walkable(idx) == 0)
                            return false;

                        bool isCurrentActualFootprintCell =
                            x >= currentMin.x && x < currentMax.x &&
                            y >= currentMin.y && y < currentMax.y;
                        if (!isCurrentActualFootprintCell &&
                            IsBlockedForFaction(ref context, idx, factionId))
                            return false;
                    }

                    bool isCurrentClearanceCell =
                        x >= currentPaddedMin.x && x < currentPaddedMax.x &&
                        y >= currentPaddedMin.y && y < currentPaddedMax.y;
                    bool isIgnoredOccupancyCell =
                        ignoredOccupancyEntity != PlacementIdentity.Null &&
                        PlacementFootprint.ContainsCell(ignoredOccupancyCell, ignoredOccupancySize, new PlacementCell(x, y));
                    if (!isCurrentClearanceCell && context.HasOccupied && context.Occupied(idx))
                    {
                        if (!isIgnoredOccupancyCell &&
                            !IsOnlySoftBlockerAtCell(ref context, context.Width, idx, movingEntity))
                            return false;
                    }
                }
            }

            for (int i = 0; i < context.UnitCount; i++)
            {
                PlacementIdentity other = context.UnitEntity(i);
                if (other == movingEntity || other == ignoredOccupancyEntity)
                    continue;

                PlacementCell otherCell = context.UnitCell(i);
                PlacementCell otherSize = context.UnitSize(i);
                if (PlacementFootprint.Overlaps(cell, footprintSize, otherCell, otherSize) &&
                    !PlacementFootprint.Overlaps(currentCell, footprintSize, otherCell, otherSize) &&
                    !IsSoftBlocker(otherSize))
                    return false;
            }

            return true;
        }

        private static bool IsOnlySoftBlockerAtCell<T>(
            ref T context,
            int gridWidth,
            int idx,
            PlacementIdentity movingEntity) where T : struct, IPathPlacementContext
        {
            PlacementCell cell = new PlacementCell(idx % gridWidth, idx / gridWidth);
            bool foundSoft = false;
            for (int i = 0; i < context.UnitCount; i++)
            {
                if (context.UnitEntity(i) == movingEntity)
                    continue;

                PlacementCell otherSize = context.UnitSize(i);
                if (!PlacementFootprint.ContainsCell(context.UnitCell(i), otherSize, cell))
                    continue;

                if (!IsSoftBlocker(otherSize))
                    return false;

                foundSoft = true;
            }

            return foundSoft;
        }

        private static bool IsBlockedForFaction<T>(
            ref T context,
            int idx,
            byte factionId) where T : struct, IPathPlacementContext
        {
            if (!context.HasDynamicBlocked || !context.DynamicBlocked(idx))
                return false;

            return !context.HasFriendlyPass ||
                (uint)idx >= (uint)context.FriendlyPassLength ||
                context.FriendlyPass(idx) != factionId;
        }

        private static bool IsSoftBlocker(PlacementCell size)
        {
            PlacementCell clamped = PlacementFootprint.ClampSize(size);
            return clamped.x == 1 && clamped.y == 1;
        }
    }
}
