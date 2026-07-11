using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Flags]
    public enum UnitSpatialIndexFlags : uint
    {
        None = 0,
        HasHealth = 1u << 0,
        HasLocalTransform = 1u << 1,
        HasLocalToWorld = 1u << 2,
        Air = 1u << 3,
        DebugTarget = 1u << 4,
        RuntimeBuilding = 1u << 5,
        StaticGridBlocker = 1u << 6,
        GroundVehicle = 1u << 7,
        CanAttack = 1u << 8,
        HasCombat = 1u << 9,
        ResourceHauler = 1u << 10,
        FuelOilSource = 1u << 11,
        FuelRefineryInput = 1u << 12,
        FuelRefineryOutput = 1u << 13,
        FuelStorage = 1u << 14,
        Selectable = 1u << 15,
        SelectionVehicle = 1u << 16,
        SpawnTransit = 1u << 17,
        HasSelectionHitbox = 1u << 18
    }

    public struct UnitSpatialIndexState : IComponentData
    {
        public uint Version;
        public int EntryCount;
        public int BucketReferenceCount;
        public int OverflowCount;
        public int GridWidth;
        public int GridHeight;
        public int BucketSizeCells;
        public int BucketCountX;
        public int BucketCountY;
        public int BucketCount;
        public byte Ready;
    }

    [InternalBufferCapacity(0)]
    public struct UnitSpatialIndexEntry : IBufferElementData
    {
        public Entity Entity;
        public int SourceOrder;
        public int2 Cell;
        public float3 Position;
        public float3 SelectionPosition;
        public int HealthCurrent;
        public int HealthMax;
        public byte FactionId;
        public UnitSpatialIndexFlags Flags;

        public bool Has(UnitSpatialIndexFlags flags)
        {
            return (Flags & flags) == flags;
        }
    }

    [InternalBufferCapacity(0)]
    public struct UnitSpatialIndexBucketRange : IBufferElementData
    {
        public int Start;
        public int Count;
        public int WriteCursor;
    }

    [InternalBufferCapacity(0)]
    public struct UnitSpatialIndexBucketEntry : IBufferElementData
    {
        public int EntryIndex;
    }
}
