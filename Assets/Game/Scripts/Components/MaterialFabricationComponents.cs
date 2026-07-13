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
}
