using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct UnitSpatialIndexState : IComponentData
    {
        public uint Version;
        public double BuiltAtElapsedTime;
        public int EntryCount;
        public int OverflowCount;
        public int GridWidth;
        public int GridHeight;
        public int BucketCountX;
        public int BucketCountY;
        public int BucketCount;
        public byte Ready;
    }

    [InternalBufferCapacity(0)]
    public struct UnitSpatialIndexEntry : IBufferElementData
    {
        public Entity Entity;
        public int2 Cell;
        public int SourceOrder;
        public int NextEntryIndex;
    }

    [InternalBufferCapacity(0)]
    public struct UnitSpatialIndexBucketHead : IBufferElementData
    {
        public int EntryIndex;
    }
}
