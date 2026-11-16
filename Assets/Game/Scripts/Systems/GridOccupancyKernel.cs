namespace Game.Runtime.Navigation
{
    public interface IGridOccupancyStorage
    {
        int GetCount(int index);
        void SetCount(int index, int count);
        void SetOccupied(int index, bool occupied);
    }
    public struct OccupancyBounds
    {
        public int MinX, MinZ, MaxX, MaxZ;
        public OccupancyBounds(int minX, int minZ, int maxX, int maxZ)
        { MinX = minX; MinZ = minZ; MaxX = maxX; MaxZ = maxZ; }
    }
    // Storage-independent production rectangle reduction. The host owns membership,
    // grid revisions, allocation and dirty-entity discovery; no geometry is generated.
    public static class GridOccupancyKernel
    {
        private struct Point
        {
            public int x, y;
            public Point(int a, int b) { x = a; y = b; }
            public bool Equals(Point other) => x == other.x && y == other.y;
        }
        public static OccupancyBounds Bounds(int width, int height, int cellX, int cellZ, int sizeX, int sizeZ)
        {
            sizeX = System.Math.Max(1, sizeX); sizeZ = System.Math.Max(1, sizeZ);
            int minX = unchecked(cellX - (sizeX - 1) / 2), minZ = unchecked(cellZ - (sizeZ - 1) / 2);
            return new OccupancyBounds(Clamp(minX, width), Clamp(minZ, height),
                Clamp(unchecked(minX + sizeX), width), Clamp(unchecked(minZ + sizeZ), height));
        }
        private static int Clamp(int value, int max) => System.Math.Max(0, System.Math.Min(max, value));
        public static void Add<T>(int width, ref T storage, OccupancyBounds bounds) where T : struct, IGridOccupancyStorage
        {
            for (int y = bounds.MinZ; y < bounds.MaxZ; y++)
            for (int x = bounds.MinX; x < bounds.MaxX; x++)
            {
                int index = unchecked(y * width + x);
                int next = unchecked(storage.GetCount(index) + 1);
                storage.SetCount(index, next);
                if (next == 1) storage.SetOccupied(index, true);
            }
        }
        public static void Remove<T>(int width, ref T storage, OccupancyBounds bounds) where T : struct, IGridOccupancyStorage
        {
            for (int y = bounds.MinZ; y < bounds.MaxZ; y++)
            for (int x = bounds.MinX; x < bounds.MaxX; x++)
            {
                int index = unchecked(y * width + x);
                int next = System.Math.Max(0, unchecked(storage.GetCount(index) - 1));
                storage.SetCount(index, next);
                if (next == 0) storage.SetOccupied(index, false);
            }
        }
        private static void AddRect<T>(int width, ref T storage, Point min, Point max) where T : struct, IGridOccupancyStorage =>
            Add(width, ref storage, new OccupancyBounds(min.x, min.y, max.x, max.y));
        private static void RemoveRect<T>(int width, ref T storage, Point min, Point max) where T : struct, IGridOccupancyStorage =>
            Remove(width, ref storage, new OccupancyBounds(min.x, min.y, max.x, max.y));
        public static void Update<T>(int width, ref T storage, OccupancyBounds previous, OccupancyBounds current) where T : struct, IGridOccupancyStorage
        {
            var previousMin = new Point(previous.MinX, previous.MinZ); var previousMax = new Point(previous.MaxX, previous.MaxZ);
            var currentMin = new Point(current.MinX, current.MinZ); var currentMax = new Point(current.MaxX, current.MaxZ);
            if (previousMin.Equals(currentMin) && previousMax.Equals(currentMax))
                return;

            Point overlapMin = new Point(System.Math.Max(previousMin.x, currentMin.x), System.Math.Max(previousMin.y, currentMin.y));
            Point overlapMax = new Point(System.Math.Min(previousMax.x, currentMax.x), System.Math.Min(previousMax.y, currentMax.y));
            bool hasOverlap = overlapMin.x < overlapMax.x && overlapMin.y < overlapMax.y;
            if (!hasOverlap)
            {
                RemoveRect(width, ref storage, previousMin, previousMax);
                AddRect(width, ref storage, currentMin, currentMax);
                return;
            }

            if (previousMin.x < overlapMin.x)
                RemoveRect(width, ref storage, new Point(previousMin.x, previousMin.y), new Point(overlapMin.x, previousMax.y));
            if (overlapMax.x < previousMax.x)
                RemoveRect(width, ref storage, new Point(overlapMax.x, previousMin.y), new Point(previousMax.x, previousMax.y));
            if (previousMin.y < overlapMin.y)
                RemoveRect(width, ref storage, new Point(overlapMin.x, previousMin.y), new Point(overlapMax.x, overlapMin.y));
            if (overlapMax.y < previousMax.y)
                RemoveRect(width, ref storage, new Point(overlapMin.x, overlapMax.y), new Point(overlapMax.x, previousMax.y));

            if (currentMin.x < overlapMin.x)
                AddRect(width, ref storage, new Point(currentMin.x, currentMin.y), new Point(overlapMin.x, currentMax.y));
            if (overlapMax.x < currentMax.x)
                AddRect(width, ref storage, new Point(overlapMax.x, currentMin.y), new Point(currentMax.x, currentMax.y));
            if (currentMin.y < overlapMin.y)
                AddRect(width, ref storage, new Point(overlapMin.x, currentMin.y), new Point(overlapMax.x, overlapMin.y));
            if (overlapMax.y < currentMax.y)
                AddRect(width, ref storage, new Point(overlapMin.x, overlapMax.y), new Point(overlapMax.x, currentMax.y));
        }
    }
}
