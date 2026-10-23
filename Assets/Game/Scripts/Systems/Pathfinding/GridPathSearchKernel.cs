using System;

namespace Game.Runtime.Pathfinding
{
    public struct PathSearchCell
    {
        public int X, Z;
        public PathSearchCell(int x, int z) { X = x; Z = z; }
    }
    public struct PathSearchRequest
    {
        public int Width, Height, GridSize, StartX, StartZ, GoalX, GoalZ;
        public int SearchBoundsPadding, MaxExpansions, SearchEpoch, HeuristicWeight;
        public bool IsVehicle;
    }
    public struct PathSearchResult
    {
        public bool Found, SetExpansionCount;
        public int FailureCode, ExpansionCount;
    }

    // A request-scoped value adapter supplies authoritative placement/surface facts,
    // existing reusable scratch and ordered output. No allocations or Unity types.
    // Unknown/unauthored facts must be rejected by the caller, not inferred as free.
    public interface IPathSearchContext
    {
        bool IsWalkable(int cellIndex);
        bool IsSurfaceCellPathable(int x, int z);
        bool CanPlace(int x, int z, int currentX, int currentZ);
        bool IsSurfaceFootprintPathable(int x, int z);
        int TraversalCost(int cellIndex, bool diagonal);
        int Epoch(int index); void SetEpoch(int index, int value);
        int CameFrom(int index); void SetCameFrom(int index, int value);
        int GScore(int index); void SetGScore(int index, int value);
        byte Closed(int index); void SetClosed(int index, byte value);
        void SetInOpen(int index, byte value);
        long Open(int index); void SetOpen(int index, long value);
        int Path(int index); void SetPath(int index, int value);
        void WriteCell(int x, int z);
    }

    public static class GridPathSearchKernel
    {
        public const int FreeTraversalCost = 10, FreeDiagonalTraversalCost = 14;
        public const int PreferredSurfaceTraversalCost = 6, PreferredSurfaceDiagonalTraversalCost = 8;
        public const int AvoidedSurfaceTraversalCost = 18, AvoidedSurfaceDiagonalTraversalCost = 25;
        public const int InfantryMaxAStarExpansions = 450, InfantrySegmentedMaxAStarExpansions = 30000;
        public const int VehicleMaxAStarExpansions = 1600, VehicleManualMaxAStarExpansions = 12000;
        public const int InfantrySearchBoundsPaddingCells = 8, InfantrySegmentedSearchBoundsPaddingCells = 180, VehicleSearchBoundsPaddingCells = 12;

        public static int ResolveSearchPadding(bool isVehicle, bool cheapSegmentMode, bool hasModes, byte mode) =>
            hasModes && mode >= 2 ? InfantrySegmentedSearchBoundsPaddingCells : isVehicle ? VehicleSearchBoundsPaddingCells :
            cheapSegmentMode ? InfantrySegmentedSearchBoundsPaddingCells : InfantrySearchBoundsPaddingCells;
        public static int ResolveExpansionLimit(bool isVehicle, bool manualMove, bool cheapSegmentMode, bool hasModes, byte mode) =>
            isVehicle ? (manualMove || hasModes && mode == 3 ? VehicleManualMaxAStarExpansions : VehicleMaxAStarExpansions) :
            (cheapSegmentMode ? InfantrySegmentedMaxAStarExpansions : InfantryMaxAStarExpansions);
        public static bool InBounds(int x, int z, int width, int height) => (uint)x < (uint)width && (uint)z < (uint)height;

        // Start checks are performed once by the batch adapter before retries.
        public static int ValidateStart<T>(ref T context, in PathSearchRequest request) where T : struct, IPathSearchContext
        {
            if (!InBounds(request.StartX, request.StartZ, request.Width, request.Height)) return 1;
            if (!context.IsWalkable(request.StartZ * request.Width + request.StartX)) return 2;
            return context.IsSurfaceCellPathable(request.StartX, request.StartZ) ? 0 : 3;
        }

        public static PathSearchResult TrySearch<T>(ref T context, in PathSearchRequest request) where T : struct, IPathSearchContext
        {
            int width = request.Width, height = request.Height;
            int sx = request.StartX, sz = request.StartZ, gx = request.GoalX, gz = request.GoalZ;
            if (!InBounds(gx, gz, width, height)) return Fail(4);
            int startIndex = sz * width + sx, goalIndex = gz * width + gx;
            int minX = Math.Max(0, Math.Min(sx, gx) - request.SearchBoundsPadding);
            int maxX = Math.Min(width - 1, Math.Max(sx, gx) + request.SearchBoundsPadding);
            int minZ = Math.Max(0, Math.Min(sz, gz) - request.SearchBoundsPadding);
            int maxZ = Math.Min(height - 1, Math.Max(sz, gz) + request.SearchBoundsPadding);
            if (!context.IsWalkable(goalIndex)) return Fail(5);
            if (!context.IsSurfaceCellPathable(gx, gz)) return Fail(6);
            bool goalPlacementValid = context.CanPlace(gx, gz, sx, sz);
            bool goalFootprintValid = context.IsSurfaceFootprintPathable(gx, gz);
            if (goalIndex != startIndex)
            {
                if (!goalPlacementValid) return Fail(7);
                if (!goalFootprintValid) return Fail(8);
            }
            if (HasDirectPath(ref context, sx, sz, gx, gz))
            {
                context.WriteCell(sx, sz);
                if (sx != gx || sz != gz) context.WriteCell(gx, gz);
                return Success(0);
            }
            InitializeNode(ref context, startIndex, request.SearchEpoch);
            context.SetGScore(startIndex, 0);
            int heapCount = 0;
            HeapPush(ref context, ref heapCount, PackHeapEntry(unchecked(request.HeuristicWeight * HeuristicOctile(sx, sz, gx, gz)), startIndex));
            int expansions = 0;
            bool found = false;
            while (heapCount > 0)
            {
                int current = UnpackHeapIndex(HeapPop(ref context, ref heapCount));
                if (context.Closed(current) != 0) continue;
                expansions++;
                if (expansions > request.MaxExpansions) return Fail(9, expansions);
                if (current == goalIndex) { found = true; break; }
                context.SetClosed(current, 1);
                int cx = current % width, cz = current / width;
                for (int d = 0; d < 8; d++)
                {
                    var direction = Neighbor(d);
                    int nx = cx + direction.X, nz = cz + direction.Z;
                    if (!InBounds(nx, nz, width, height)) continue;
                    if (nx < minX || nx > maxX || nz < minZ || nz > maxZ) continue;
                    int next = nz * width + nx;
                    InitializeNode(ref context, next, request.SearchEpoch);
                    if (context.Closed(next) != 0) continue;
                    if (!context.IsWalkable(next)) continue;
                    if (!context.IsSurfaceCellPathable(nx, nz)) continue;
                    bool diagonal = nx != cx && nz != cz;
                    if (diagonal)
                    {
                        bool horizontal = context.CanPlace(nx, cz, cx, cz);
                        horizontal = horizontal && context.IsSurfaceFootprintPathable(nx, cz);
                        bool vertical = context.CanPlace(cx, nz, cx, cz);
                        vertical = vertical && context.IsSurfaceFootprintPathable(cx, nz);
                        if (!horizontal || !vertical) continue;
                    }
                    int cost = context.TraversalCost(next, diagonal);
                    bool canPlace = context.CanPlace(nx, nz, cx, cz);
                    if (!canPlace) continue;
                    if (!context.IsSurfaceFootprintPathable(nx, nz)) continue;
                    int tentative = unchecked(context.GScore(current) + cost);
                    if (tentative >= context.GScore(next)) continue;
                    context.SetCameFrom(next, current); context.SetGScore(next, tentative);
                    if (heapCount < request.GridSize)
                        HeapPush(ref context, ref heapCount, PackHeapEntry(unchecked(tentative + request.HeuristicWeight * HeuristicOctile(nx, nz, gx, gz)), next));
                }
            }
            if (!found) return Fail(10, expansions);
            int length = 0, at = goalIndex;
            while (at >= 0 && length < request.GridSize)
            {
                context.SetPath(length++, at);
                if (at == startIndex) break;
                at = context.CameFrom(at);
            }
            if (length == 0 || context.Path(length - 1) != startIndex) return Fail(11, expansions);
            for (int i = length - 1; i >= 0; i--)
            {
                int cell = context.Path(i); int x = cell % width, z = cell / width;
                if (!request.IsVehicle || i == length - 1 || i == 0) { context.WriteCell(x, z); continue; }
                int previous = context.Path(i + 1), next = context.Path(i - 1);
                int prevX = x - previous % width, prevZ = z - previous / width;
                int nextX = next % width - x, nextZ = next / width - z;
                if (prevX != nextX || prevZ != nextZ) context.WriteCell(x, z);
            }
            return Success(expansions);
        }

        public static bool HasDirectPath<T>(ref T context, int sx, int sz, int gx, int gz) where T : struct, IPathSearchContext
        {
            int steps = Math.Max(Abs(unchecked(gx - sx)), Abs(unchecked(gz - sz)));
            if (steps <= 1) return true;
            int cx = sx, cz = sz;
            for (int step = 1; step <= steps; step++)
            {
                var next = DirectPathCell(sx, sz, gx, gz, step, steps);
                if (next.X == cx && next.Z == cz) continue;
                if (!context.CanPlace(next.X, next.Z, cx, cz)) return false;
                if (!context.IsSurfaceFootprintPathable(next.X, next.Z)) return false;
                cx = next.X; cz = next.Z;
            }
            return true;
        }
        public static PathSearchCell DirectPathCell(int sx, int sz, int gx, int gz, int step, int steps)
        {
            float t = (float)step / steps;
            return new PathSearchCell((int)Round(Lerp(sx, gx, t)), (int)Round(Lerp(sz, gz, t)));
        }
        private static float Lerp(float a, float b, float t) => a + t * (b - a);
        private static float Round(float value) => (float)Math.Round((float)value);
        public static int HeuristicOctile(int ax, int az, int bx, int bz)
        {
            int dx = Abs(unchecked(ax - bx)), dz = Abs(unchecked(az - bz));
            int diagonal = Math.Min(dx, dz), straight = Math.Max(dx, dz) - diagonal;
            return unchecked(diagonal * FreeDiagonalTraversalCost + straight * FreeTraversalCost);
        }
        public static PathSearchCell Neighbor(int index)
        {
            switch (index)
            {
                case 0: return new PathSearchCell(1, 0); case 1: return new PathSearchCell(-1, 0);
                case 2: return new PathSearchCell(0, 1); case 3: return new PathSearchCell(0, -1);
                case 4: return new PathSearchCell(1, 1); case 5: return new PathSearchCell(1, -1);
                case 6: return new PathSearchCell(-1, 1); default: return new PathSearchCell(-1, -1);
            }
        }
        public static PathSearchCell SquareRingOffset(int radius, int step)
        {
            int top = 2 * radius + 1;
            if (step < top) return new PathSearchCell(-radius + step, radius);
            step -= top; int right = 2 * radius;
            if (step < right) return new PathSearchCell(radius, radius - 1 - step);
            step -= right; int bottom = 2 * radius;
            if (step < bottom) return new PathSearchCell(radius - 1 - step, -radius);
            step -= bottom; return new PathSearchCell(-radius, -radius + 1 + step);
        }
        private static void InitializeNode<T>(ref T context, int index, int epoch) where T : struct, IPathSearchContext
        {
            if (context.Epoch(index) == epoch) return;
            context.SetEpoch(index, epoch); context.SetCameFrom(index, -1); context.SetGScore(index, int.MaxValue);
            context.SetClosed(index, 0); context.SetInOpen(index, 0);
        }
        public static long PackHeapEntry(int score, int index) => ((long)score << 32) | (uint)index;
        public static int UnpackHeapIndex(long entry) => (int)(entry & 0xFFFFFFFF);
        private static void HeapPush<T>(ref T context, ref int count, long entry) where T : struct, IPathSearchContext
        {
            int child = count++;
            while (child > 0)
            {
                int parent = (child - 1) >> 1; long existing = context.Open(parent);
                if (existing <= entry) break;
                context.SetOpen(child, existing); child = parent;
            }
            context.SetOpen(child, entry);
        }
        private static long HeapPop<T>(ref T context, ref int count) where T : struct, IPathSearchContext
        {
            long root = context.Open(0); long last = context.Open(--count); int parent = 0;
            while (true)
            {
                int left = (parent << 1) + 1; if (left >= count) break;
                int right = left + 1;
                int smallest = right < count && context.Open(right) < context.Open(left) ? right : left;
                if (context.Open(smallest) >= last) break;
                context.SetOpen(parent, context.Open(smallest)); parent = smallest;
            }
            context.SetOpen(parent, last); return root;
        }
        private static int Abs(int value) => value < 0 ? unchecked(-value) : value;
        private static PathSearchResult Fail(int code) => new PathSearchResult { FailureCode = code };
        private static PathSearchResult Fail(int code, int expansions) => new PathSearchResult { FailureCode = code, ExpansionCount = expansions, SetExpansionCount = true };
        private static PathSearchResult Success(int expansions) => new PathSearchResult { Found = true, ExpansionCount = expansions, SetExpansionCount = true };
    }
}
