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
        public uint Version;
    }
}
