using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum RtsCameraRequestKind : byte
    {
        ResetSession = 1,
        ResetCameraModeSession = 2,
        SetDragging = 3,
        ClearDragging = 4,
        SetWasPlayRequested = 5,
        SetWasBuildModeActive = 6,
        SetZoomTransitionActive = 7,
        SetFullscreenIsoTargets = 8,
        SetNormalIsoModeActive = 9,
        BeginZoomTransition = 10,
        CompleteZoomTransition = 11,
        ResetTransitionVelocities = 12,
        Pan = 13,
        PerspectiveZoom = 14,
        FullscreenIsoZoom = 15,
        UpdatePerspectiveMode = 16,
        UpdateFullscreenIsoMode = 17,
        ApplyPerspectiveModeInstant = 18,
        ApplyFullscreenIsoModeInstant = 19,
        MoveGroundCenterTo = 20,
        SetSmoothFocusTarget = 21,
        ClearSmoothFocusTarget = 22,
        UpdateSmoothFocus = 23,
        UpdateTacticalFollowPose = 24,
        SetMatchIntroZoomSettlePending = 25
    }

    public struct RtsCameraRequestQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct RtsCameraStateComponent : IComponentData
    {
        public byte IsDragging;
        public byte HasSmoothFocusTarget;
        public float3 SmoothFocusTarget;
        public byte WasPlayRequested;
        public byte WasBuildModeActive;
        public byte IsZoomTransitionActive;
        public byte MatchIntroZoomSettlePending;
        public float FullscreenIsoTargetHeight;
        public float FullscreenIsoTargetOrthographicSize;
        public byte NormalIsoModeActive;
    }

    public struct RtsCameraRequestElement : IBufferElementData
    {
        public RtsCameraRequestKind Kind;
        public int RequestId;
        public float2 ScreenDelta;
        public float3 WorldPosition;
        public float4 Rotation;
        public float Value;
        public float Value2;
        public float Value3;
        public float Value4;
        public float Value5;
        public float Value6;
        public byte Flag;
        public byte Flag2;
        public byte Flag3;
    }
}
