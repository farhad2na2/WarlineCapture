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
        public FixedString128Bytes SourceGlobalObjectId;
        public int PlacementIndex;
        public OperationMapBuildingBlockerPolicy BlockerPolicy;
    }

    public struct OperationMapBuildingDestroyedComponent : IComponentData, IEnableableComponent
    {
    }
}
