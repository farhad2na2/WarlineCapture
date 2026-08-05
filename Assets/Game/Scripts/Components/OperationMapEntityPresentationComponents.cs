using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public enum DenseCityPresentationSemanticCategory : byte
    {
        Unknown = 0,
        GameplayBuildingIntact = 1,
        GameplayBuildingDestroyed = 2,
        BuildingAttachmentIntact = 3,
        BuildingAttachmentDestroyed = 4,
        Infrastructure = 5,
        Vegetation = 6,
        Prop = 7,
        Horizon = 8
    }

    [System.Flags]
    public enum DenseCityPresentationSemanticFlags : byte
    {
        None = 0,
        AllowsProtectedOverlap = 1
    }

    public struct OperationMapEntityPresentationRoot : IComponentData
    {
        public FixedString128Bytes OperationMapId;
        public byte Role;
        public int SchemaVersion;
        public FixedString128Bytes MigrationRecordSetHash;
    }

    public struct OperationMapEntityPresentationReadinessContract : IComponentData
    {
        public FixedString128Bytes OperationMapId;
        public FixedString128Bytes MigrationRecordSetHash;
        public int ExpectedPresentationRootCount;
        public int ExpectedGameplayBuildingCount;
        public int ExpectedGameplayVehicleCount;
        public int ExpectedRenderOnlyCount;
        public int ExpectedGeneratedIdentityCount;
        public byte RequiresStaticPresentationPreload;
    }

    public struct OperationMapEntityPresentationIdentity : IComponentData
    {
        public FixedString128Bytes OperationMapId;
        public FixedString128Bytes SourceGlobalObjectId;
        public byte Role;
        public int PlacementIndex;
    }

    public struct DenseCityPresentationIdentity : IComponentData
    {
        public FixedString128Bytes StableId;
        public byte Role;
        public byte Category;
        public byte Flags;
    }

    public struct OperationMapBuildingIdentity : IComponentData
    {
        public FixedString128Bytes OperationMapId;
        public FixedString128Bytes StableId;
        public FixedString128Bytes SourceGlobalObjectId;
        public int PlacementIndex;
    }

    public struct OperationMapAuthoredVehiclePresentation : IComponentData
    {
        public int PlacementIndex;
        public byte FactionId;
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
