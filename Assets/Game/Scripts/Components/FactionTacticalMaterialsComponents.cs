using Unity.Entities;

namespace Game.Components
{
    public enum FactionTacticalMaterialsSourceKind : byte
    {
        Fabrication = 0,
        Import = 1,
        Reward = 2
    }

    public enum FactionTacticalMaterialsSpendKind : byte
    {
        Construction = 0,
        Repair = 1,
        Infrastructure = 2,
        Upgrade = 3,
        Export = 4
    }

    public enum FactionTacticalMaterialsMutationResult : byte
    {
        Applied = 0,
        InvalidAmount = 1,
        InvalidState = 2,
        CapacityExceeded = 3,
        InsufficientMaterials = 4
    }

    public enum FactionConstructionResourceMutationResult : byte
    {
        Applied = 0,
        InvalidCost = 1,
        InvalidState = 2,
        InsufficientCredits = 3,
        InsufficientMaterials = 4,
        InsufficientCreditsAndMaterials = 5,
        DuplicateTransaction = 6
    }

    public struct FactionTacticalMaterialsComponent : IComponentData
    {
        public byte FactionId;
        public int Current;
        public int Capacity;
        public int LifetimeFabricated;
        public int LifetimeImported;
        public int LifetimeRewarded;
        public int LifetimeExported;
        public int LifetimeSpent;
        public int LifetimeConstructionSpent;
        public int LifetimeRepairSpent;
        public int LifetimeInfrastructureSpent;
        public int LifetimeUpgradeSpent;
        public uint Version;
    }

    public struct FactionMaterialFabricationTelemetryComponent : IComponentData
    {
        public byte FactionId;
        public float ActiveSeconds;
        public float NoOilInputBlockedSeconds;
        public float MaterialsCapacityFullBlockedSeconds;
        public float NoOilRouteBlockedSeconds;
        public float ProductionDisabledSeconds;
        public float BuildingDisabledSeconds;
        public uint Version;
    }

    public struct FactionFuelLogisticsTelemetryComponent : IComponentData
    {
        public byte FactionId;
        public int TrayRouteAssignmentCount;
        public int TrayRouteReassignmentCount;
        public int TrayRouteFailureCount;
        public float OilDeliveredToRefineries;
        public float OilDeliveredToFabricationDepots;
        public uint Version;
    }
}
