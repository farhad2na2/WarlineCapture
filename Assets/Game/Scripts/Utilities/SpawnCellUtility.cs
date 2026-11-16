using Unity.Collections;
using Unity.Mathematics;
using Game.Components;
using Game.Runtime.Spawning;

namespace Game.Runtime
{
    public static class SpawnCellUtility
    {
        internal struct NativeStorage : ISpawnCellStorage
        {
            public NativeArray<GridWalkable> Walkable;
            public NativeBitArray Blocked, Occupied, Reserved;
            public bool IsWalkable(int index) => Walkable[index].Value != 0;
            public bool IsBlocked(int index) => Blocked.IsSet(index);
            public bool IsOccupied(int index) => Occupied.IsSet(index);
            public bool IsReserved(int index) => Reserved.IsSet(index);
            public void SetReserved(int index, bool value) => Reserved.Set(index, value);
        }
        private struct NativeRandom : ISpawnCellRandom
        {
            public Random Value;
            public int NextInt(int minimum, int maximum) => Value.NextInt(minimum, maximum);
        }
        public static int2 FindSpawnCellNear(ref Random rng, in GridConfig grid,
            in NativeArray<GridWalkable> walkable, in NativeBitArray blocked, in NativeBitArray occupied,
            ref NativeBitArray reserved, int2 center, int radiusCells, int2 footprintSize)
            => TryFindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved,
                center, radiusCells, footprintSize, out int2 cell) ? cell : center;

        public static bool TryFindSpawnCellNear(ref Random rng, in GridConfig grid,
            in NativeArray<GridWalkable> walkable, in NativeBitArray blocked, in NativeBitArray occupied,
            ref NativeBitArray reserved, int2 center, int radiusCells, int2 footprintSize, out int2 cell)
            => TryFind(ref rng, grid, walkable, blocked, occupied, ref reserved,
                center, radiusCells, footprintSize, false, out cell);

        internal static bool TryFind(ref Random rng, in GridConfig grid,
            in NativeArray<GridWalkable> walkable, in NativeBitArray blocked, in NativeBitArray occupied,
            ref NativeBitArray reserved, int2 center, int radiusCells, int2 footprintSize, bool air, out int2 cell)
        {
            var storage = new NativeStorage { Walkable = walkable, Blocked = blocked, Occupied = occupied, Reserved = reserved };
            var random = new NativeRandom { Value = rng };
            bool found = SpawnCellSearchKernel.TryFindInitial(ref storage, ref random,
                grid.Width, grid.Height, center.x, center.y, radiusCells, footprintSize.x, footprintSize.y,
                air, out int x, out int z);
            rng = random.Value; reserved = storage.Reserved; cell = new int2(x, z); return found;
        }

        public static int2 FindSpawnCellNear(ref Random rng, in GridConfig grid,
            in NativeArray<GridWalkable> walkable, in NativeBitArray blocked, in NativeBitArray occupied,
            ref NativeBitArray reserved, int2 center, int radiusCells)
            => FindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved, center, radiusCells, new int2(1, 1));
    }
}
