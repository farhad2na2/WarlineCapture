using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    [UpdateAfter(typeof(UnitGridMovementSystem))]
    [UpdateAfter(typeof(UnitAirMovementSystem))]
    [UpdateBefore(typeof(BuildingDefenseAttackSystem))]
    [UpdateBefore(typeof(AITargetingSystem))]
    [UpdateBefore(typeof(ThreatDetectionWarningSystem))]
    [UpdateBefore(typeof(VisibleUnitSelectionCandidateSystem))]
    public partial struct UnitSpatialIndexBuildSystem : ISystem
    {
        public const int BucketSizeCells = 128;
        public const int BucketHeadCount = 256;
        public const int MaxEntries = 2048;
        public const double RefreshIntervalSeconds = 0.12d;

        private Entity _indexEntity;
        private EntityQuery _indexQuery;
        private double _nextRefreshTime;

        public void OnCreate(ref SystemState state)
        {
            _indexQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<UnitSpatialIndexState>(),
                ComponentType.ReadWrite<UnitSpatialIndexEntry>(),
                ComponentType.ReadWrite<UnitSpatialIndexBucketHead>());
            using (var ecb = new EntityCommandBuffer(Allocator.Temp))
            {
                Entity indexEntity = ecb.CreateEntity();
                ecb.AddComponent(indexEntity, new UnitSpatialIndexState());
                ecb.AddBuffer<UnitSpatialIndexEntry>(indexEntity);
                ecb.AddBuffer<UnitSpatialIndexBucketHead>(indexEntity);
                ecb.Playback(state.EntityManager);
            }

            _indexEntity = _indexQuery.GetSingletonEntity();
            DynamicBuffer<UnitSpatialIndexEntry> entries =
                state.EntityManager.GetBuffer<UnitSpatialIndexEntry>(_indexEntity);
            DynamicBuffer<UnitSpatialIndexBucketHead> heads =
                state.EntityManager.GetBuffer<UnitSpatialIndexBucketHead>(_indexEntity);
            entries.Capacity = MaxEntries;
            heads.ResizeUninitialized(BucketHeadCount);
            UnitSpatialIndexBuilder.ClearHeads(heads);
            state.RequireForUpdate<GridConfig>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_indexEntity != Entity.Null && state.EntityManager.Exists(_indexEntity))
            {
                using var ecb = new EntityCommandBuffer(Allocator.Temp);
                ecb.DestroyEntity(_indexEntity);
                ecb.Playback(state.EntityManager);
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double elapsedTime = SystemAPI.Time.ElapsedTime;
            if (elapsedTime < _nextRefreshTime)
                return;

            _nextRefreshTime = elapsedTime + RefreshIntervalSeconds;
            GridConfig grid = SystemAPI.GetSingleton<GridConfig>();
            DynamicBuffer<UnitSpatialIndexEntry> entries =
                state.EntityManager.GetBuffer<UnitSpatialIndexEntry>(_indexEntity);
            DynamicBuffer<UnitSpatialIndexBucketHead> heads =
                state.EntityManager.GetBuffer<UnitSpatialIndexBucketHead>(_indexEntity);
            UnitSpatialIndexState previousState =
                state.EntityManager.GetComponentData<UnitSpatialIndexState>(_indexEntity);

            entries.Clear();
            UnitSpatialIndexBuilder.ClearHeads(heads);
            bool layoutValid = UnitSpatialIndexBuilder.TryGetBucketLayout(
                grid.Width,
                grid.Height,
                out int bucketCountX,
                out int bucketCountY,
                out int bucketCount);
            int sourceOrder = 0;
            int overflowCount = 0;

            if (layoutValid)
            {
                foreach (var (unitGrid, entity) in
                         SystemAPI.Query<RefRO<UnitGrid>>().WithEntityAccess())
                {
                    if (!UnitSpatialIndexBuilder.TryInsert(
                            entries,
                            heads,
                            entity,
                            unitGrid.ValueRO.Cell,
                            sourceOrder,
                            grid.Width,
                            grid.Height,
                            bucketCountX))
                    {
                        overflowCount++;
                    }

                    sourceOrder++;
                }
            }

            var indexState = new UnitSpatialIndexState
            {
                Version = previousState.Version + 1u,
                BuiltAtElapsedTime = elapsedTime,
                EntryCount = entries.Length,
                OverflowCount = overflowCount,
                GridWidth = math.max(1, grid.Width),
                GridHeight = math.max(1, grid.Height),
                BucketCountX = bucketCountX,
                BucketCountY = bucketCountY,
                BucketCount = bucketCount,
                Ready = layoutValid ? (byte)1 : (byte)0
            };
            SystemAPI.GetSingletonRW<UnitSpatialIndexState>().ValueRW = indexState;
        }
    }

    internal static class UnitSpatialIndexBuilder
    {
        public const int InvalidEntryIndex = -1;

        public static bool TryGetBucketLayout(
            int gridWidth,
            int gridHeight,
            out int bucketCountX,
            out int bucketCountY,
            out int bucketCount)
        {
            if (gridWidth <= 0 || gridHeight <= 0)
            {
                bucketCountX = 0;
                bucketCountY = 0;
                bucketCount = 0;
                return false;
            }

            bucketCountX = (gridWidth + UnitSpatialIndexBuildSystem.BucketSizeCells - 1) /
                           UnitSpatialIndexBuildSystem.BucketSizeCells;
            bucketCountY = (gridHeight + UnitSpatialIndexBuildSystem.BucketSizeCells - 1) /
                           UnitSpatialIndexBuildSystem.BucketSizeCells;
            long requiredBucketCount = (long)bucketCountX * bucketCountY;
            bucketCount = requiredBucketCount <= int.MaxValue ? (int)requiredBucketCount : int.MaxValue;
            return requiredBucketCount <= UnitSpatialIndexBuildSystem.BucketHeadCount;
        }

        public static void ClearHeads(DynamicBuffer<UnitSpatialIndexBucketHead> heads)
        {
            int clearCount = math.min(UnitSpatialIndexBuildSystem.BucketHeadCount, heads.Length);
            for (int i = 0; i < clearCount; i++)
                heads[i] = new UnitSpatialIndexBucketHead { EntryIndex = InvalidEntryIndex };
        }

        public static bool TryInsert(
            DynamicBuffer<UnitSpatialIndexEntry> entries,
            DynamicBuffer<UnitSpatialIndexBucketHead> heads,
            Entity entity,
            int2 cell,
            int sourceOrder,
            int gridWidth,
            int gridHeight,
            int bucketCountX)
        {
            if (entries.Length >= entries.Capacity ||
                heads.Length < UnitSpatialIndexBuildSystem.BucketHeadCount ||
                bucketCountX <= 0)
            {
                return false;
            }

            int entryIndex = entries.Length;
            entries.Add(new UnitSpatialIndexEntry
            {
                Entity = entity,
                Cell = cell,
                SourceOrder = sourceOrder,
                NextEntryIndex = InvalidEntryIndex
            });
            return TryLinkEntry(
                entries,
                heads,
                entryIndex,
                gridWidth,
                gridHeight,
                bucketCountX);
        }

        public static bool TryLinkEntry(
            DynamicBuffer<UnitSpatialIndexEntry> entries,
            DynamicBuffer<UnitSpatialIndexBucketHead> heads,
            int entryIndex,
            int gridWidth,
            int gridHeight,
            int bucketCountX)
        {
            if ((uint)entryIndex >= (uint)entries.Length ||
                heads.Length < UnitSpatialIndexBuildSystem.BucketHeadCount ||
                bucketCountX <= 0)
            {
                return false;
            }

            UnitSpatialIndexEntry entry = entries[entryIndex];
            int bucketIndex = BucketIndex(entry.Cell, gridWidth, gridHeight, bucketCountX);
            if ((uint)bucketIndex >= (uint)UnitSpatialIndexBuildSystem.BucketHeadCount)
                return false;

            UnitSpatialIndexBucketHead head = heads[bucketIndex];
            entry.NextEntryIndex = head.EntryIndex;
            entries[entryIndex] = entry;
            head.EntryIndex = entryIndex;
            heads[bucketIndex] = head;
            return true;
        }

        private static int BucketIndex(
            int2 cell,
            int gridWidth,
            int gridHeight,
            int bucketCountX)
        {
            int2 clampedCell = math.clamp(
                cell,
                int2.zero,
                new int2(math.max(0, gridWidth - 1), math.max(0, gridHeight - 1)));
            int2 bucket = clampedCell / UnitSpatialIndexBuildSystem.BucketSizeCells;
            return bucket.y * bucketCountX + bucket.x;
        }
    }
}
