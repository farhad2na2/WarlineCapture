// Data-only extraction of production spawn search and surface correction.
// Callers own snapshot lifetime, actual surface facts, and actor creation ordering.
namespace Game.Runtime.Spawning
{
    public interface ISpawnCellStorage
    {
        bool IsWalkable(int index);
        bool IsBlocked(int index);
        bool IsOccupied(int index);
        bool IsReserved(int index);
        void SetReserved(int index, bool value);
    }
    public interface ISpawnCellRandom { int NextInt(int minimum, int maximum); }
    public interface ISpawnSurfaceAdmission
    {
        bool CanTraverseFootprint(int x, int z, int sizeX, int sizeZ);
    }

    public static class SpawnCellSearchKernel
    {
        private static int Max(int a, int b) => a > b ? a : b;
        private static int Min(int a, int b) => a < b ? a : b;
        private static int Abs(int value) => value < 0 ? -value : value;

        public static bool TryFindInitial<TStorage, TRandom>(ref TStorage storage, ref TRandom random,
            int width, int height, int centerX, int centerZ, int radius, int sizeX, int sizeZ,
            bool isAirUnit, out int x, out int z)
            where TStorage : struct, ISpawnCellStorage where TRandom : struct, ISpawnCellRandom
        {
            // Air preference intentionally uses the original, unclamped center.
            if (isAirUnit && TryReserve(ref storage, width, height, centerX, centerZ, sizeX, sizeZ, true))
            { x = centerX; z = centerZ; return true; }
            return TryFindNear(ref storage, ref random, width, height, centerX, centerZ,
                radius, sizeX, sizeZ, out x, out z);
        }

        public static bool TryFindNear<TStorage, TRandom>(ref TStorage storage, ref TRandom random,
            int width, int height, int centerX, int centerZ, int radius, int sizeX, int sizeZ,
            out int x, out int z)
            where TStorage : struct, ISpawnCellStorage where TRandom : struct, ISpawnCellRandom
        {
            centerX = Max(0, Min(width - 1, centerX));
            centerZ = Max(0, Min(height - 1, centerZ));
            radius = Max(0, radius);
            x = z = 0;
            if (TryReserve(ref storage, width, height, centerX, centerZ, sizeX, sizeZ, false))
            { x = centerX; z = centerZ; return true; }
            for (int i = 0; i < 128; i++)
            {
                int candidateX = centerX + random.NextInt(-radius, radius + 1);
                int candidateZ = centerZ + random.NextInt(-radius, radius + 1);
                if (!TryReserve(ref storage, width, height, candidateX, candidateZ, sizeX, sizeZ, false)) continue;
                x = candidateX; z = candidateZ; return true;
            }
            int maximumRadius = Max(8, radius + 32);
            for (int r = 0; r <= maximumRadius; r++)
                for (int dz = -r; dz <= r; dz++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Abs(dx) != r && Abs(dz) != r) continue;
                        int candidateX = centerX + dx, candidateZ = centerZ + dz;
                        if (!TryReserve(ref storage, width, height, candidateX, candidateZ, sizeX, sizeZ, false)) continue;
                        x = candidateX; z = candidateZ; return true;
                    }
            int count = width * height;
            for (int index = 0; index < count; index++)
            {
                int candidateX = index % width, candidateZ = index / width;
                if (!TryReserve(ref storage, width, height, candidateX, candidateZ, sizeX, sizeZ, false)) continue;
                x = candidateX; z = candidateZ; return true;
            }
            return false;
        }

        private static bool TryReserve<TStorage>(ref TStorage storage, int width, int height,
            int x, int z, int sizeX, int sizeZ, bool air) where TStorage : struct, ISpawnCellStorage
        {
            sizeX = Max(1, sizeX); sizeZ = Max(1, sizeZ);
            int minX = x - (sizeX - 1) / 2, minZ = z - (sizeZ - 1) / 2;
            int maxX = minX + sizeX, maxZ = minZ + sizeZ;
            if (minX < 0 || minZ < 0 || maxX > width || maxZ > height) return false;
            for (int rowZ = minZ; rowZ < maxZ; rowZ++)
                for (int rowX = minX; rowX < maxX; rowX++)
                {
                    int index = rowZ * width + rowX;
                    if (air)
                    {
                        if (!storage.IsWalkable(index) || storage.IsOccupied(index)) return false;
                        if (storage.IsReserved(index) && !storage.IsBlocked(index)) return false;
                    }
                    else
                    {
                        if (!storage.IsWalkable(index)) return false;
                        if (storage.IsBlocked(index) || storage.IsOccupied(index) || storage.IsReserved(index)) return false;
                    }
                }
            for (int rowZ = minZ; rowZ < maxZ; rowZ++)
                for (int rowX = minX; rowX < maxX; rowX++) storage.SetReserved(rowZ * width + rowX, true);
            return true;
        }

        public static bool EnsureTraversable<TStorage, TSurface>(ref TStorage storage, ref TSurface surface,
            int width, int height, int sizeX, int sizeZ, bool hasMatch, int surfaceCount, ref int x, ref int z)
            where TStorage : struct, ISpawnCellStorage where TSurface : struct, ISpawnSurfaceAdmission
        {
            if (!hasMatch) return true;
            // An absent/ambiguous surface does NOT release the previous reservation.
            if (surfaceCount != 1) return false;
            if (surface.CanTraverseFootprint(x, z, sizeX, sizeZ)) return true;
            int minX = x - (Max(1, sizeX) - 1) / 2, minZ = z - (Max(1, sizeZ) - 1) / 2;
            // Original surface correction uses raw size loop bounds, unlike cell search.
            for (int dz = 0; dz < sizeZ; dz++) for (int dx = 0; dx < sizeX; dx++)
                storage.SetReserved((minZ + dz) * width + minX + dx, false);
            for (int radius = 1; radius <= 48; radius++)
                for (int dz = -radius; dz <= radius; dz++) for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Abs(dx) != radius && Abs(dz) != radius) continue;
                    int candidateX = x + dx, candidateZ = z + dz;
                    if (!surface.CanTraverseFootprint(candidateX, candidateZ, sizeX, sizeZ)) continue;
                    minX = candidateX - (Max(1, sizeX) - 1) / 2;
                    minZ = candidateZ - (Max(1, sizeZ) - 1) / 2;
                    bool free = true;
                    for (int rowZ = 0; rowZ < sizeZ && free; rowZ++) for (int rowX = 0; rowX < sizeX; rowX++)
                    {
                        int index = (minZ + rowZ) * width + minX + rowX;
                        if (!storage.IsWalkable(index) || storage.IsBlocked(index) || storage.IsOccupied(index) || storage.IsReserved(index))
                        { free = false; break; }
                    }
                    if (!free) continue;
                    for (int rowZ = 0; rowZ < sizeZ; rowZ++) for (int rowX = 0; rowX < sizeX; rowX++)
                        storage.SetReserved((minZ + rowZ) * width + minX + rowX, true);
                    x = candidateX; z = candidateZ; return true;
                }
            return false;
        }
    }

    public static class PlacementSeed
    {
        public static uint Begin(int missionSeed) => 2166136261u ^ unchecked((uint)missionSeed);
        public static uint AddByte(uint seed, byte value) => unchecked((seed ^ value) * 16777619u);
        // Exact installed Unity.Mathematics.math.hash(uint3), proved against native tests.
        public static uint Finish(uint seed, int x, int z)
        {
            uint hash = unchecked((uint)x * 3441847433u + (uint)z * 4052036147u + seed * 2011389559u + 2252224297u);
            return hash < 1u ? 1u : hash;
        }
    }

    // Exact Unity.Mathematics.Random scalar state transition/range mapping.
    // State is the live state (not a constructor seed); FromSeed advances once.
    public struct SpawnCellRandom : ISpawnCellRandom
    {
        public uint State;
        public static SpawnCellRandom FromSeed(uint seed)
        {
            if (seed == 0) throw new System.ArgumentException("Random seed must be nonzero.");
            var value = new SpawnCellRandom { State = seed }; value.NextState(); return value;
        }
        private uint NextState()
        {
            uint previous = State;
            State ^= State << 13; State ^= State >> 17; State ^= State << 5;
            return previous;
        }
        public int NextInt(int minimum, int maximum)
        {
            if (State == 0 || minimum > maximum) throw new System.ArgumentException("Valid random state and ordered bounds required.");
            uint range = unchecked((uint)(maximum - minimum));
            return unchecked((int)((NextState() * (ulong)range) >> 32) + minimum);
        }
    }
}
