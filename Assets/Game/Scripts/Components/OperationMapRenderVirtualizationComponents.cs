using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Components
{
    [Flags]
    public enum OperationMapRenderEligibilityFlags : byte
    {
        None = 0,
        Eligible = 1 << 0,
        RequiresStateOwner = 1 << 1,
        AlwaysResidentException = 1 << 2
    }

    [Flags]
    public enum OperationMapRenderLodFlags : byte
    {
        None = 0,
        Lod0 = 1 << 0,
        Lod1 = 1 << 1,
        Lod2 = 1 << 2
    }

    [Flags]
    public enum OperationMapRenderShadowFlags : byte
    {
        None = 0,
        CastShadows = 1 << 0,
        ReceiveShadows = 1 << 1,
        StaticShadowCaster = 1 << 2
    }

    public enum OperationMapRenderPolicyBucket : byte
    {
        OpaqueShadowsOn = 0,
        OpaqueShadowsOff = 1,
        AlphaClippedShadowsOn = 2,
        AlphaClippedShadowsOff = 3,
        TransparentShadowsOff = 4,
        AlwaysResidentException = 5
    }

    public enum OperationMapRenderMotionVectorMode : byte
    {
        Camera = 0,
        Object = 1,
        ForceNoMotion = 2
    }

    public enum OperationMapRenderVisualState : byte
    {
        Any = 0,
        Intact = 1,
        Destroyed = 2
    }

    public enum OperationMapRenderRebuildReason : byte
    {
        None = 0,
        InitialView = 1,
        CameraEnvelopeChanged = 2,
        VisualStateChanged = 3,
        MapGenerationChanged = 4
    }

    public struct OperationMapRenderBoundsBlob
    {
        public float3 Center;
        public float3 Extents;
    }

    public struct OperationMapRenderIdentity128
    {
        public ulong Low;
        public ulong High;
    }

    public struct OperationMapRenderDatabaseBlob
    {
        public FixedString64Bytes OperationMapId;
        public FixedString128Bytes ContentHash;
        public int SchemaVersion;
        public float CellSize;
        public float3 GridOrigin;
        public int2 GridDimensions;
        public BlobArray<OperationMapRenderPrototypeBlob> Prototypes;
        public BlobArray<OperationMapRenderPrototypePartBlob> Parts;
        public BlobArray<OperationMapRenderPlacementBlob> Placements;
        public BlobArray<OperationMapRenderCellBlob> Cells;
        public BlobArray<int> CellPlacementIndices;
        public BlobArray<OperationMapRenderPoolBucketBlob> PoolBuckets;
    }

    public struct OperationMapRenderPrototypeBlob
    {
        public OperationMapRenderIdentity128 ContentIdentity;
        public int FirstPart;
        public int PartCount;
        public OperationMapRenderBoundsBlob CombinedLocalBounds;
        public DenseCityPresentationSemanticCategory SemanticCategory;
        public OperationMapRenderEligibilityFlags EligibilityFlags;
    }

    public struct OperationMapRenderPrototypePartBlob
    {
        public OperationMapRenderIdentity128 RendererPathHash;
        public int MeshArrayIndex;
        public int MaterialArrayIndex;
        public int SubMeshIndex;
        public float4x4 LocalToPlacement;
        public OperationMapRenderBoundsBlob LocalBounds;
        public float4 LinearBaseColor;
        public OperationMapRenderPolicyBucket PolicyBucket;
        public int PoolBucketIndex;
        public OperationMapRenderLodFlags LodFlags;
        public OperationMapRenderShadowFlags ShadowFlags;
    }

    public struct OperationMapRenderPlacementBlob
    {
        public OperationMapRenderIdentity128 StableIdentityHash;
        public int PrototypeIndex;
        public float4x4 WorldMatrix;
        public int CellIndex;
        public int StateOwnerIndex;
        public OperationMapRenderVisualState RequiredVisualState;
        public int Priority;
        public DenseCityPresentationSemanticCategory SemanticCategory;
    }

    public struct OperationMapRenderCellBlob
    {
        public int2 Coordinate;
        public OperationMapRenderBoundsBlob WorldBounds;
        public int FirstPlacementIndex;
        public int PlacementIndexCount;
    }

    public struct OperationMapRenderPoolBucketBlob
    {
        public OperationMapRenderPolicyBucket PolicyBucket;
        public int Layer;
        public uint RenderingLayerMask;
        public OperationMapRenderMotionVectorMode MotionVectorMode;
        public OperationMapRenderShadowFlags ShadowFlags;
        public int FirstSlot;
        public int Capacity;
        public int PeakRequiredCount;
        public int HeadroomCount;
        public OperationMapRenderIdentity128 ReportIdentity;
    }

    public struct OperationMapRenderDatabaseComponent : IComponentData
    {
        public BlobAssetReference<OperationMapRenderDatabaseBlob> Blob;
        public FixedString128Bytes ContentHash;
        public int SchemaVersion;
        public int MapGeneration;
    }

    public struct OperationMapRenderProxySlotComponent : IComponentData
    {
        public int SlotIndex;
        public int PoolBucketIndex;
        public int PlacementIndex;
        public int PartIndex;
        public int AssignmentGeneration;
    }

    [InternalBufferCapacity(0)]
    public struct OperationMapRenderSlotCommandComponent : IBufferElementData
    {
        public int SlotIndex;
        public int LogicalRowIndex;
        public int PlacementIndex;
        public int PartIndex;
        public int PoolBucketIndex;
        public int AssignmentGeneration;
        public byte Assigned;
    }

    public struct OperationMapRenderSlotCommandStateComponent : IComponentData
    {
        public uint Version;
    }

    public struct OperationMapRenderVirtualizationStateComponent : IComponentData
    {
        public byte Initialized;
        public byte InitialViewApplied;
        public int2 ActiveEnvelopeMin;
        public int2 ActiveEnvelopeMax;
        public OperationMapRenderIdentity128 CameraSignature;
        public int ActiveSlotCount;
        public int DirtyPlacementCount;
        public int OverflowCount;
        public int RebuildCount;
    }

    public struct OperationMapVirtualizedBuildingPresentationComponent : IComponentData
    {
        public int StateOwnerIndex;
    }

    [InternalBufferCapacity(16)]
    public struct OperationMapRenderStateChangeComponent : IBufferElementData
    {
        public int StateOwnerIndex;
        public OperationMapRenderVisualState VisualState;
        public uint ChangeVersion;
    }

    public struct OperationMapRenderStateChangeSequenceComponent : IComponentData
    {
        public uint LastPublishedVersion;
    }

    [InternalBufferCapacity(0)]
    public struct OperationMapRenderCanonicalStateComponent : IBufferElementData
    {
        public OperationMapRenderVisualState VisualState;
        public uint ChangeVersion;
    }

    public struct OperationMapRenderStateSyncStateComponent : IComponentData
    {
        public byte Initialized;
        public uint Revision;
        public uint LastAppliedChangeVersion;
        public int StateOwnerCount;
        public int DirtyPlacementCount;
        public int DirtyCellCount;
    }

    public struct OperationMapRenderVirtualizationMetricsComponent : IComponentData
    {
        public int LogicalPlacementCount;
        public int LogicalPartCount;
        public int ResidentExceptionCount;
        public int Capacity;
        public int EnabledSlotCount;
        public int DisabledSlotCount;
        public int RetainedCount;
        public int ReleasedCount;
        public int ReboundCount;
        public int ActiveCellCount;
        public int ActivePlacementCount;
        public int OverflowCount;
        public int HighestDeficit;
        public uint CommandVersion;
        public OperationMapRenderRebuildReason RebuildReason;
    }

    public struct OperationMapRenderPackedReadinessComponent : IComponentData
    {
        public byte ResidencyMode;
        public int EligibleSourceRowCount;
        public int ResidentSourceRowCount;
        public int ProxySlotCount;
        public int VirtualizedAcceptedBuildingIdentityCount;
        public int VirtualizedAcceptedRenderOnlyIdentityCount;
        public int VirtualizedGeneratedBuildingIdentityCount;
        public int VirtualizedGeneratedRenderOnlyIdentityCount;
    }

    public struct OperationMapRenderEligibleSourceComponent : IComponentData
    {
    }

    [InternalBufferCapacity(0)]
    public struct OperationMapRenderResidentSourceRowComponent : IBufferElementData
    {
        public Entity RenderEntity;
        public OperationMapRenderIdentity128 OwnerIdentity;
        public OperationMapRenderIdentity128 RendererPathIdentity;
    }

    [BakingType]
    [InternalBufferCapacity(1)]
    public struct OperationMapRenderSourceRowBakingComponent : IBufferElementData
    {
        public Entity RenderEntity;
        public OperationMapRenderIdentity128 OwnerIdentity;
        public OperationMapRenderIdentity128 RendererPathIdentity;
        public byte IsRenderOnlyOwner;
        public byte IsGeneratedOwner;
    }

    [BakingType]
    public struct OperationMapVirtualizedBuildingOwnerBakingComponent : IComponentData
    {
        public OperationMapRenderIdentity128 OwnerIdentity;
    }

    [BakingType]
    [InternalBufferCapacity(0)]
    public struct OperationMapRenderEligibleSourceRowBakingComponent : IBufferElementData
    {
        public OperationMapRenderIdentity128 OwnerIdentity;
        public OperationMapRenderIdentity128 RendererPathIdentity;
        public UnityObjectRef<Mesh> Mesh;
        public UnityObjectRef<Material> Material;
        public ushort SubMeshIndex;
        public int StateOwnerIndex;
        public OperationMapRenderVisualState RequiredVisualState;
        public byte RequiresStateOwner;
    }
}
