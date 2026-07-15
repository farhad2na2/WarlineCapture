using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum OperationMapAnchorKind : byte
    {
        None = 0,
        Spawn = 1,
        Objective = 2,
        Deployment = 3,
        Build = 4,
        Civilian = 5,
        Hostile = 6,
        Base = 7,
        Resource = 8,
        Runway = 9,
        Helipad = 10,
        Lane = 11,
        Camera = 12,
        Minimap = 13,
        Debug = 14
    }

    public enum OperationMapLoadRequestKind : byte
    {
        None = 0,
        Load = 1,
        Unload = 2,
        Switch = 3,
        Retry = 4
    }

    public enum OperationMapLoadStatusKind : byte
    {
        None = 0,
        Resolving = 1,
        LoadingScene = 2,
        LoadingSubScene = 3,
        BindingMetadata = 4,
        PreloadingPresentation = 5,
        Ready = 6,
        Draining = 7,
        Unloading = 8,
        Failed = 9
    }

    public enum OperationMapLoadResultCode : byte
    {
        None = 0,
        Accepted = 1,
        IgnoredDuplicate = 2,
        InvalidRequest = 3,
        InvalidOperationMapId = 4,
        MissingDefinition = 5,
        MissingSourceContent = 6,
        StaleContent = 7,
        SourceLoadFailed = 8,
        SubSceneLoadFailed = 9,
        MetadataBindFailed = 10,
        PresentationPreloadFailed = 11,
        Interrupted = 12,
        SourceUnloadFailed = 13,
        TeardownFailed = 14,
        Busy = 15
    }

    [Flags]
    public enum OperationMapReadinessFlags : ushort
    {
        None = 0,
        SourceContent = 1 << 0,
        SubScene = 1 << 1,
        Metadata = 1 << 2,
        MapSurface = 1 << 3,
        AuthoredConversion = 1 << 4,
        PresentationManifest = 1 << 5,
        RequiredPresentationPreload = 1 << 6
    }

    public struct OperationMapRootComponent : IComponentData
    {
    }

    public struct OperationMapQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct OperationMapLoadStateComponent : IComponentData
    {
        public int ActiveRequestId;
        public int Generation;
        public float Progress01;
        public OperationMapLoadStatusKind Status;
        public OperationMapReadinessFlags Readiness;
        public byte IsBusy;
    }

    public struct ActiveOperationMapComponent : IComponentData
    {
        public FixedString64Bytes OperationMapId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes MissionId;
        public int SchemaVersion;
        public int ContentVersion;
        public int Generation;
    }

    public struct OperationMapBoundsComponent : IComponentData
    {
        public float3 WorldMin;
        public float3 WorldMax;
        public float3 PlayableMin;
        public float3 PlayableMax;
        public float3 CameraMin;
        public float3 CameraMax;
    }

    public struct OperationMapMetadataComponent : IComponentData
    {
        public BlobAssetReference<OperationMapBlob> Blob;
        public FixedString128Bytes MetadataHash;
        public int Generation;
    }

    public struct OperationMapReadinessComponent : IComponentData
    {
        public int Generation;
        public OperationMapReadinessFlags ReadyFlags;
        public OperationMapReadinessFlags RequiredFlags;
        public OperationMapReadinessFlags FailedFlags;
    }

    public struct OperationMapLoadRequestElement : IBufferElementData
    {
        public OperationMapLoadRequestKind Kind;
        public int RequestId;
        public FixedString64Bytes OperationMapId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes MissionId;
        public byte ActivateOnLoad;
    }

    public struct OperationMapLoadResultElement : IBufferElementData
    {
        public OperationMapLoadRequestKind Kind;
        public OperationMapLoadStatusKind Status;
        public OperationMapLoadResultCode ResultCode;
        public int RequestId;
        public int Generation;
        public float Progress01;
        public FixedString64Bytes OperationMapId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes MissionId;
        public FixedString128Bytes Message;
    }

    public struct OperationMapBlob
    {
        public FixedString64Bytes OperationMapId;
        public FixedString64Bytes PlanningCameraId;
        public FixedString64Bytes BattleCameraId;
        public FixedString128Bytes SourceIdentityHash;
        public FixedString128Bytes ContentHash;
        public FixedString128Bytes GeneratedMetadataHash;
        public int SchemaVersion;
        public int ContentVersion;
        public BlobArray<OperationMapAnchorBlob> Anchors;
        public BlobArray<OperationMapCameraBlob> Cameras;
        public OperationMapMinimapBlob Minimap;
    }

    public struct OperationMapAnchorBlob
    {
        public FixedString64Bytes Id;
        public OperationMapAnchorKind Kind;
        public float3 Position;
        public quaternion Rotation;
        public float Radius;
        public int FactionId;
        public int LaneIndex;
    }

    public struct OperationMapCameraBlob
    {
        public FixedString64Bytes Id;
        public float3 Position;
        public quaternion Rotation;
        public float FieldOfView;
        public float OrthographicSize;
        public byte IsOrthographic;
        public byte ClampToCameraBounds;
    }

    public struct OperationMapMinimapBlob
    {
        public FixedString64Bytes Id;
        public float3 ProjectionOrigin;
        public float2 ProjectionSize;
        public float OrientationDegrees;
    }
}
