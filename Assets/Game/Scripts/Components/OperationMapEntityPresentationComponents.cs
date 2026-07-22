using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public struct OperationMapEntityPresentationRoot : IComponentData
    {
        public FixedString128Bytes OperationMapId;
        public byte Role;
        public int SchemaVersion;
        public FixedString128Bytes MigrationRecordSetHash;
    }

    public struct OperationMapEntityPresentationIdentity : IComponentData
    {
        public FixedString128Bytes OperationMapId;
        public FixedString128Bytes SourceGlobalObjectId;
        public byte Role;
        public int PlacementIndex;
    }

    public struct OperationMapBuildingIdentity : IComponentData
    {
        public FixedString128Bytes OperationMapId;
        public FixedString128Bytes StableId;
        public FixedString128Bytes SourceGlobalObjectId;
        public int PlacementIndex;
    }

    public struct OperationMapBuildingPresentation : IComponentData
    {
        public Entity IntactVisualRoot;
        public Entity DestroyedVisualRoot;
        public float IntactVisibleScale;
        public float DestroyedVisibleScale;
        public byte State;
    }

    public struct OperationMapBuildingAttachment : IComponentData
    {
        public Entity Building;
        public byte VisualState;
    }

    [InternalBufferCapacity(4)]
    public struct OperationMapBuildingProductionPrefab : IBufferElementData
    {
        public int ProductionIndex;
        public Entity Prefab;
        public FixedString64Bytes SourceKey;
    }
}
