using Unity.Collections;
using Game.Components;

/// <summary>
/// Snapshots all grid data read by <see cref="PathfindBatchJob"/> so the in-flight job
/// never holds references to live ECS buffers or shared containers. This detaches the
/// path batch from the ECS dependency chain: no main-thread system can be forced to
/// Complete() a long-running path job mid-frame, and grid writers can mutate live state
/// freely while a batch is still running. Capture only happens when a new batch is
/// scheduled, and a batch is only scheduled when no previous batch is pending.
/// </summary>

namespace Game.Runtime
{
    internal struct UnitPathGridSnapshot
    {
        public NativeArray<GridWalkable> Walkable;
        public NativeArray<GridRoad> Roads;
        public NativeArray<GridRoadSidewalk> Sidewalks;
        public NativeArray<GridRoadDirt> DirtRoads;
        public NativeBitArray DynamicBlocked;
        public NativeArray<byte> FriendlyPassFactionIds;
        public NativeBitArray Occupied;

        public void Capture(
            NativeArray<GridWalkable> walkable,
            NativeArray<GridRoad> roads,
            NativeArray<GridRoadSidewalk> sidewalks,
            NativeArray<GridRoadDirt> dirtRoads,
            NativeBitArray dynamicBlocked,
            NativeArray<byte> friendlyPassFactionIds,
            NativeBitArray occupied)
        {
            CopyArray(ref Walkable, walkable);
            CopyArray(ref Roads, roads);
            CopyArray(ref Sidewalks, sidewalks);
            CopyArray(ref DirtRoads, dirtRoads);
            CopyArray(ref FriendlyPassFactionIds, friendlyPassFactionIds);
            CopyBits(ref DynamicBlocked, dynamicBlocked);
            CopyBits(ref Occupied, occupied);
        }

        public void Dispose()
        {
            if (Walkable.IsCreated) Walkable.Dispose();
            if (Roads.IsCreated) Roads.Dispose();
            if (Sidewalks.IsCreated) Sidewalks.Dispose();
            if (DirtRoads.IsCreated) DirtRoads.Dispose();
            if (DynamicBlocked.IsCreated) DynamicBlocked.Dispose();
            if (FriendlyPassFactionIds.IsCreated) FriendlyPassFactionIds.Dispose();
            if (Occupied.IsCreated) Occupied.Dispose();
            this = default;
        }

        private static void CopyArray<T>(ref NativeArray<T> destination, NativeArray<T> source) where T : unmanaged
        {
            if (!source.IsCreated)
            {
                if (destination.IsCreated)
                {
                    destination.Dispose();
                    destination = default;
                }

                return;
            }

            if (!destination.IsCreated || destination.Length != source.Length)
            {
                if (destination.IsCreated)
                    destination.Dispose();
                destination = new NativeArray<T>(source.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            destination.CopyFrom(source);
        }

        private static void CopyBits(ref NativeBitArray destination, NativeBitArray source)
        {
            if (!source.IsCreated)
            {
                if (destination.IsCreated)
                {
                    destination.Dispose();
                    destination = default;
                }

                return;
            }

            if (!destination.IsCreated || destination.Length != source.Length)
            {
                if (destination.IsCreated)
                    destination.Dispose();
                destination = new NativeBitArray(source.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            NativeBitArray sourceBits = source;
            destination.Copy(0, ref sourceBits, 0, source.Length);
        }
    }
}
