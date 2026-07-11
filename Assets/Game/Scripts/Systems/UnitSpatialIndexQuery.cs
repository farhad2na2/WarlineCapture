using Game.Components;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    public readonly struct UnitSpatialIndexQuery
    {
        private readonly UnitSpatialIndexState _state;
        private readonly NativeArray<UnitSpatialIndexEntry> _entries;
        private readonly NativeArray<UnitSpatialIndexBucketRange> _ranges;
        private readonly NativeArray<UnitSpatialIndexBucketEntry> _bucketEntries;

        public UnitSpatialIndexQuery(
            in UnitSpatialIndexState state,
            NativeArray<UnitSpatialIndexEntry> entries,
            NativeArray<UnitSpatialIndexBucketRange> ranges,
            NativeArray<UnitSpatialIndexBucketEntry> bucketEntries)
        {
            _state = state;
            _entries = entries;
            _ranges = ranges;
            _bucketEntries = bucketEntries;
        }

        public bool IsReady =>
            _state.Ready != 0 &&
            _state.BucketSizeCells > 0 &&
            _state.BucketCountX > 0 &&
            _state.BucketCountY > 0 &&
            _entries.IsCreated &&
            _ranges.IsCreated &&
            _bucketEntries.IsCreated;

        public NativeArray<UnitSpatialIndexEntry> Entries => _entries;

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
            int2 minBucket = clampedMin / _state.BucketSizeCells;
            int2 maxBucket = clampedMax / _state.BucketSizeCells;
            return new Enumerator(
                _state,
                _ranges,
                _bucketEntries,
                minBucket,
                maxBucket);
        }

        public struct Enumerator
        {
            private UnitSpatialIndexState _state;
            private NativeArray<UnitSpatialIndexBucketRange> _ranges;
            private NativeArray<UnitSpatialIndexBucketEntry> _bucketEntries;
            private int2 _minBucket;
            private int2 _maxBucket;
            private int _bucketX;
            private int _bucketY;
            private int _rangeOffset;
            private int _rangeCount;
            private int _rangeIndex;
            private byte _initialized;

            internal Enumerator(
                in UnitSpatialIndexState state,
                NativeArray<UnitSpatialIndexBucketRange> ranges,
                NativeArray<UnitSpatialIndexBucketEntry> bucketEntries,
                int2 minBucket,
                int2 maxBucket)
            {
                _state = state;
                _ranges = ranges;
                _bucketEntries = bucketEntries;
                _minBucket = minBucket;
                _maxBucket = maxBucket;
                _bucketX = minBucket.x;
                _bucketY = minBucket.y;
                _rangeOffset = 0;
                _rangeCount = 0;
                _rangeIndex = 0;
                _initialized = 0;
                CurrentEntryIndex = -1;
            }

            public int CurrentEntryIndex { get; private set; }

            public bool MoveNext()
            {
                while (true)
                {
                    if (_rangeIndex < _rangeCount)
                    {
                        int referenceIndex = _rangeOffset + _rangeIndex++;
                        if ((uint)referenceIndex >= (uint)_state.BucketReferenceCount ||
                            (uint)referenceIndex >= (uint)_bucketEntries.Length)
                        {
                            continue;
                        }

                        CurrentEntryIndex = _bucketEntries[referenceIndex].EntryIndex;
                        return true;
                    }

                    if (!MoveToNextBucket())
                        return false;
                }
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
                        (uint)bucketIndex < (uint)_ranges.Length)
                    {
                        UnitSpatialIndexBucketRange range = _ranges[bucketIndex];
                        _rangeOffset = range.Start;
                        _rangeCount = range.Count;
                        _rangeIndex = 0;
                        if (_rangeCount > 0)
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
