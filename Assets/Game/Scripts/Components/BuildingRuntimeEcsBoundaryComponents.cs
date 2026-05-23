using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct BuildingRuntimeBoundaryTag : IComponentData
{
}

public struct BuildingConfiguredSpawnableReadModel : IBufferElementData
{
    public FixedString128Bytes BuildingId;
    public FixedString128Bytes DisplayName;
    public int Price;
    public int2 FootprintCells;
    public byte CanRequest;
}

public struct BuildingConfiguredUnitReadModel : IBufferElementData
{
    public FixedString128Bytes UnitId;
    public FixedString128Bytes DisplayName;
    public int Price;
    public byte CanRequest;
    public byte IsVehicle;
}

public struct BuildingRuntimeFactionSummary : IBufferElementData
{
    public byte FactionId;
    public int BuildingCount;
    public float StoredOilBarrels;
    public float StoredFuelBarrels;
    public float OilBarrelsPerDay;
    public float FuelBarrelsPerDay;
}

public struct BuildingRuntimeOwnedBuildingSummary : IBufferElementData
{
    public byte FactionId;
    public FixedString128Bytes BuildingId;
    public int Count;
}

public struct BuildingRuntimeUnitProductionSummary : IBufferElementData
{
    public byte FactionId;
    public FixedString128Bytes UnitId;
    public int ProducedCount;
    public int QueuedCount;
}

public struct BuildingFactionUnitProductionRequest : IBufferElementData
{
    public const byte Pending = 0;
    public const byte Succeeded = 1;
    public const byte Failed = 2;
    public const byte MissingUnitConfig = 1;
    public const byte MissingProducerBuilding = 2;
    public const byte ProducerUnavailable = 3;

    public int RequestId;
    public byte FactionId;
    public FixedString128Bytes UnitId;
    public byte Status;
    public byte ResultCode;
    public FixedString128Bytes ProducerDisplayName;
    public FixedString128Bytes UnitDisplayName;
    public int Cost;
    public int QueueCount;
    public int ProducedCount;
}

public struct BuildingFactionResourceSellRequest : IBufferElementData
{
    public const byte Pending = 0;
    public const byte Succeeded = 1;
    public const byte Failed = 2;
    public const byte NoneSold = 1;

    public int RequestId;
    public byte FactionId;
    public float RequestedOilBarrels;
    public float RequestedFuelBarrels;
    public byte Status;
    public byte ResultCode;
    public float SoldOilBarrels;
    public float SoldFuelBarrels;
}

public struct BuildingRuntimeSpawnRequest : IBufferElementData
{
    public const byte Pending = 0;
    public const byte Succeeded = 1;
    public const byte Failed = 2;
    public const byte MissingConfig = 1;
    public const byte Blocked = 2;
    public const byte KindBuilding = 0;
    public const byte KindWallRun = 1;
    public const byte KindWallSegment = 2;

    public int RequestId;
    public byte RequestKind;
    public byte FactionId;
    public FixedString128Bytes BuildingId;
    public int2 PreferredOrigin;
    public int2 EndOrigin;
    public byte RotateVertical;
    public byte AllowExistingWallOverlap;
    public byte Status;
    public byte ResultCode;
    public Entity PlanEntity;
    public int EntryIndex;
    public int Cost;
    public FixedString128Bytes DisplayName;
    public int BuildingRuntimeId;
    public int2 ActualOrigin;
    public int2 ActualFootprint;
    public int SpawnedCount;
}
