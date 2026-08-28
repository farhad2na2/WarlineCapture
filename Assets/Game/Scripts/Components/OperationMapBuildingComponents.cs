using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public enum OperationMapBuildingBlockerPolicy : byte
    {
        Unknown = 0,
        RubbleRemainsBlocked = 1
    }

    public struct OperationMapBuildingComponent : IComponentData
    {
        public FixedString128Bytes OperationMapId;
        public FixedString128Bytes StableId;
        public FixedString128Bytes SourceGlobalObjectId;
        public int PlacementIndex;
        public OperationMapBuildingBlockerPolicy BlockerPolicy;
    }

    public struct OperationMapBuildingDestroyedComponent : IComponentData, IEnableableComponent
    {
    }

    public struct OperationMapBuildingProductionQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    [InternalBufferCapacity(4)]
    public struct OperationMapBuildingUnitProductionRequest : IBufferElementData
    {
        public const byte Pending = 0;

        public int RequestId;
        public int ProductionIndex;
        public Entity UnitPrefab;
        public FixedString64Bytes UnitSourceKey;
        public float QueuedAt;
        public float ReadyAt;
        public int RemainingQuantity;
        public byte Status;
    }
}
