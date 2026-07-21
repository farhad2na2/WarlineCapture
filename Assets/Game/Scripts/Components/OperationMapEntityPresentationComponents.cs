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

    public struct OperationMapBuildingIdentity : IComponentData
    {
        public FixedString128Bytes OperationMapId;
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

    [InternalBufferCapacity(4)]
    public struct OperationMapBuildingProductionPrefab : IBufferElementData
    {
        public int ProductionIndex;
        public Entity Prefab;
        public FixedString64Bytes SourceKey;
    }
}
