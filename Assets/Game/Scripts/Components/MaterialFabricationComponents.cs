using Unity.Entities;

namespace Game.Components
{
    public enum MaterialFabricationOutputCapacityPolicyCode : byte
    {
        RequireFullCycleCapacity = 0
    }

    public enum MaterialFabricationStatusCode : byte
    {
        None = 0,
        Producing = 1,
        Blocked = 2,
        Disabled = 3
    }

    public enum MaterialFabricationBlockReasonCode : byte
    {
        None = 0,
        NoOilInput = 1,
        MaterialsCapacityFull = 2,
        NoOilRoute = 3,
        ProductionDisabled = 4,
        BuildingDisabled = 5
    }

    public enum MaterialFabricationEconomyEventKind : byte
    {
        CycleCompleted = 0,
        StatusChanged = 1
    }

    public enum MaterialFabricationRequestKind : byte
    {
        SetProductionEnabled = 0
    }

    public enum MaterialFabricationResultCode : byte
    {
        None = 0,
        Applied = 1,
        Unchanged = 2,
        OwnerMismatch = 3,
        InvalidRequest = 4
    }

    public struct MaterialFabricationComponent : IComponentData
    {
        public int RuntimeBuildingId;
        public byte OwnerFactionId;
        public byte ProductionEnabled;
        public MaterialFabricationOutputCapacityPolicyCode OutputCapacityPolicy;
        public float OilConsumedPerCycle;
        public int MaterialsOutputPerCycle;
        public float CycleDurationSeconds;
        public float CycleProgressSeconds;
        public MaterialFabricationStatusCode Status;
        public MaterialFabricationBlockReasonCode BlockReason;
        public uint Version;
    }

    public struct MaterialFabricationInputTag : IComponentData
    {
    }

    public struct MaterialFabricationCommandQueueComponent : IComponentData
    {
        public const int Capacity = 8;

        public int LastRequestId;
    }

    [InternalBufferCapacity(MaterialFabricationCommandQueueComponent.Capacity)]
    public struct MaterialFabricationRequestComponent : IBufferElementData
    {
        public int RequestId;
        public byte RequesterFactionId;
        public byte ProductionEnabled;
        public MaterialFabricationRequestKind Kind;
    }

    [InternalBufferCapacity(MaterialFabricationCommandQueueComponent.Capacity)]
    public struct MaterialFabricationResultComponent : IBufferElementData
    {
        public int RequestId;
        public byte Accepted;
        public byte ProductionEnabled;
        public MaterialFabricationResultCode Code;
        public uint FabricationVersion;
    }

    public struct MaterialFabricationEconomyEventQueueComponent : IComponentData
    {
        public const int Capacity = 16;

        public int LastEventId;
        public uint Version;
    }

    [InternalBufferCapacity(MaterialFabricationEconomyEventQueueComponent.Capacity)]
    public struct MaterialFabricationEconomyEventElement : IBufferElementData
    {
        public int EventId;
        public int RuntimeBuildingId;
        public byte FactionId;
        public MaterialFabricationEconomyEventKind EventKind;
        public MaterialFabricationStatusCode Status;
        public MaterialFabricationBlockReasonCode BlockReason;
        public int CompletedCycles;
        public float OilConsumedBarrels;
        public int MaterialsProduced;
    }
}
