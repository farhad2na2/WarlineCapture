using Game.Components;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    public readonly struct UnitSpatialIndexQuery
    {
        private readonly UnitSpatialIndexState _state;
        private readonly NativeArray<UnitSpatialIndexEntry> _entries;
        private readonly NativeArray<UnitSpatialIndexBucketHead> _heads;

        public UnitSpatialIndexQuery(
            in UnitSpatialIndexState state,
            NativeArray<UnitSpatialIndexEntry> entries,
            NativeArray<UnitSpatialIndexBucketHead> heads)
        {
            _state = state;
            _entries = entries;
            _heads = heads;
        }

        public bool IsReady =>
            _state.Ready != 0 &&
            _state.Version != 0u &&
            _state.OverflowCount == 0 &&
            _state.GridWidth > 0 &&
            _state.GridHeight > 0 &&
            _state.BucketCountX > 0 &&
            _state.BucketCountY > 0 &&
            _state.BucketCount == _state.BucketCountX * _state.BucketCountY &&
            _state.BucketCount <= UnitSpatialIndexBuildSystem.BucketHeadCount &&
            _state.EntryCount >= 0 &&
            _state.EntryCount <= _entries.Length &&
            _entries.IsCreated &&
            _heads.IsCreated &&
            _heads.Length >= UnitSpatialIndexBuildSystem.BucketHeadCount;

        public NativeArray<UnitSpatialIndexEntry> Entries => _entries;

        public bool IsCurrent(double elapsedTime)
        {
            return IsReady && _state.BuiltAtElapsedTime == elapsedTime;
        }

        public bool MatchesGrid(in GridConfig grid)
        {
            return grid.Width == _state.GridWidth &&
                   grid.Height == _state.GridHeight &&
                   grid.CellSize > 0f;
        }

        public Enumerator QueryCells(int2 minInclusive, int2 maxInclusive)
        {
            if (!IsReady || _state.EntryCount <= 0)
                return default;

            int2 clampedMin = math.clamp(
                math.min(minInclusive, maxInclusive),
                int2.zero,
                new int2(_state.GridWidth - 1, _state.GridHeight - 1));
            int2 clampedMax = math.clamp(
                math.max(minInclusive, maxInclusive),
                int2.zero,
                new int2(_state.GridWidth - 1, _state.GridHeight - 1));
            int2 minBucket = clampedMin / UnitSpatialIndexBuildSystem.BucketSizeCells;
            int2 maxBucket = clampedMax / UnitSpatialIndexBuildSystem.BucketSizeCells;
            return new Enumerator(_state, _entries, _heads, minBucket, maxBucket);
        }

        public struct Enumerator
        {
            private UnitSpatialIndexState _state;
            private NativeArray<UnitSpatialIndexEntry> _entries;
            private NativeArray<UnitSpatialIndexBucketHead> _heads;
            private int2 _minBucket;
            private int2 _maxBucket;
            private int _bucketX;
            private int _bucketY;
            private int _nextEntryIndex;
            private int _remainingEntries;
            private byte _initialized;

            internal Enumerator(
                in UnitSpatialIndexState state,
                NativeArray<UnitSpatialIndexEntry> entries,
                NativeArray<UnitSpatialIndexBucketHead> heads,
                int2 minBucket,
                int2 maxBucket)
            {
                _state = state;
                _entries = entries;
                _heads = heads;
                _minBucket = minBucket;
                _maxBucket = maxBucket;
                _bucketX = minBucket.x;
                _bucketY = minBucket.y;
                _nextEntryIndex = UnitSpatialIndexBuilder.InvalidEntryIndex;
                _remainingEntries = math.min(state.EntryCount, entries.Length);
                _initialized = 0;
                CurrentEntryIndex = UnitSpatialIndexBuilder.InvalidEntryIndex;
            }

            public int CurrentEntryIndex { get; private set; }

            public bool MoveNext()
            {
                while (_remainingEntries > 0)
                {
                    if (_nextEntryIndex != UnitSpatialIndexBuilder.InvalidEntryIndex)
                    {
                        int entryIndex = _nextEntryIndex;
                        if ((uint)entryIndex >= (uint)_state.EntryCount ||
                            (uint)entryIndex >= (uint)_entries.Length)
                        {
                            _nextEntryIndex = UnitSpatialIndexBuilder.InvalidEntryIndex;
                            continue;
                        }

                        UnitSpatialIndexEntry entry = _entries[entryIndex];
                        _nextEntryIndex = entry.NextEntryIndex;
                        _remainingEntries--;
                        CurrentEntryIndex = entryIndex;
                        return true;
                    }

                    if (!MoveToNextBucket())
                        return false;
                }

                return false;
            }

            private bool MoveToNextBucket()
            {
                if (_initialized == 0)
                {
                    _initialized = 1;
                }
                else
                {
                    _bucketX++;
                    if (_bucketX > _maxBucket.x)
                    {
                        _bucketX = _minBucket.x;
                        _bucketY++;
                    }
                }

                while (_bucketY <= _maxBucket.y)
                {
                    int bucketIndex = _bucketY * _state.BucketCountX + _bucketX;
                    if ((uint)bucketIndex < (uint)_state.BucketCount &&
                        (uint)bucketIndex < (uint)_heads.Length)
                    {
                        _nextEntryIndex = _heads[bucketIndex].EntryIndex;
                        if (_nextEntryIndex != UnitSpatialIndexBuilder.InvalidEntryIndex)
                            return true;
                    }

                    _bucketX++;
                    if (_bucketX > _maxBucket.x)
                    {
                        _bucketX = _minBucket.x;
                        _bucketY++;
                    }
                }

                return false;
            }
        }
    }
}
