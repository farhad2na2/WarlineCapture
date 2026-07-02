using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal struct UnitPathScratchWorkspace
    {
        public const int EpochsPerRequest = 128;

        public NativeArray<int> CameFrom;
        public NativeArray<int> GScore;
        public NativeArray<byte> Closed;
        public NativeArray<byte> InOpen;
        public NativeArray<int> Epoch;
        public NativeArray<long> Open; // binary heap of packed (fScore << 32 | cellIndex) entries
        public NativeArray<int> Path;

        private int _gridSize;
        private int _searchEpoch;

        public void Initialize()
        {
            _searchEpoch = 1;
        }

        public bool Ensure(int gridSize, out int scratchCells, out int threadSlots)
        {
            scratchCells = _gridSize;
            threadSlots = 1;
            if (_gridSize == gridSize && CameFrom.IsCreated)
                return false;

            Dispose();
            _gridSize = gridSize;
            int total = gridSize;
            scratchCells = gridSize;
            threadSlots = 1;

            CameFrom = new NativeArray<int>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            GScore = new NativeArray<int>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            Closed = new NativeArray<byte>(total, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            InOpen = new NativeArray<byte>(total, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            Epoch = new NativeArray<int>(total, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            Open = new NativeArray<long>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            Path = new NativeArray<int>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _searchEpoch = 1;
            return true;
        }

        public int ReserveEpochs(int requestCount)
        {
            int requestedEpochs = math.max(1, requestCount * EpochsPerRequest);
            if (_searchEpoch <= 0 || _searchEpoch > int.MaxValue - requestedEpochs)
            {
                if (Epoch.IsCreated)
                    Epoch.AsSpan().Clear();
                _searchEpoch = 1;
            }

            int epochBase = _searchEpoch;
            _searchEpoch += requestedEpochs;
            return epochBase;
        }

        public void Dispose()
        {
            if (CameFrom.IsCreated) CameFrom.Dispose();
            if (GScore.IsCreated) GScore.Dispose();
            if (Closed.IsCreated) Closed.Dispose();
            if (InOpen.IsCreated) InOpen.Dispose();
            if (Epoch.IsCreated) Epoch.Dispose();
            if (Open.IsCreated) Open.Dispose();
            if (Path.IsCreated) Path.Dispose();
            _gridSize = 0;
            _searchEpoch = 1;
        }
    }
}
