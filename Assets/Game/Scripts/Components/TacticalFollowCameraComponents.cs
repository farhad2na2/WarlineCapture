using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum TacticalFollowCameraRequestKind : byte
    {
        None = 0,
        ToggleFollowMode = 1,
        ExitFollowMode = 2,
        SetBaseTarget = 3,
        RefreshBaseTarget = 4,
        SetTemporaryMissileTarget = 5,
        ClearTemporaryTarget = 6,
        RestoreDefaultCamera = 7
    }

    public enum TacticalFollowCameraTargetKind : byte
    {
        None = 0,
        Unit = 1,
        UnitGroup = 2,
        Building = 3,
        GroundMissile = 4,
        AirMissile = 5,
        AttackImpact = 6
    }

    public enum TacticalFollowCameraPoseSource : byte
    {
        None = 0,
        BaseTarget = 1,
        TemporaryMissile = 2,
        RestoreDefault = 3
    }

    public enum TacticalFollowCameraFeedbackCode : byte
    {
        None = 0,
        EnteredFollowMode = 1,
        ExitedFollowMode = 2,
        TargetLost = 3
    }

    public struct TacticalFollowCameraRequestQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct TacticalFollowCameraRequestElement : IBufferElementData
    {
        public TacticalFollowCameraRequestKind Kind;
        public int RequestId;
        public Entity TargetEntity;
        public TacticalFollowCameraTargetKind TargetKind;
        public float3 WorldPosition;
        public byte HasTargetEntity;
        public byte HasWorldPosition;
    }

    public struct TacticalFollowCameraBaseTargetElement : IBufferElementData
    {
        public Entity Entity;
    }

    public struct TacticalFollowCameraModeComponent : IComponentData
    {
        public byte Enabled;
        public byte PanInputLocked;
        public byte HasBaseTarget;
        public TacticalFollowCameraTargetKind BaseTargetKind;
        public Entity BaseTargetEntity;
        public byte HasTemporaryTarget;
        public TacticalFollowCameraTargetKind TemporaryTargetKind;
        public Entity TemporaryTargetEntity;
        public int ModeEnteredFrame;
        public float TemporaryTargetStartedTime;
        public float ReturnHoldUntilTime;
        public byte RestorePoseValid;
        public float3 RestorePosition;
        public quaternion RestoreRotation;
        public float RestoreFieldOfView;
        public float RestoreOrthographicSize;
        public byte RestoreOrthographic;
    }

    public enum TacticalFollowAttackCinematicPhase : byte
    {
        None = 0,
        Launch = 1,
        MissilePath = 2,
        Impact = 3,
        Flyover = 4
    }

    public enum TacticalFollowAttackCinematicAttackKind : byte
    {
        None = 0,
        FollowedAirInstantHit = 1
    }

    public enum TacticalFollowAttackCinematicAbortReason : byte
    {
        None = 0,
        FollowModeExited = 1,
        TemporaryTargetCleared = 2,
        SourceLost = 3,
        TargetLost = 4,
        Completed = 5
    }

    public struct TacticalFollowAttackCinematicStateComponent : IComponentData
    {
        public byte Active;
        public TacticalFollowAttackCinematicAttackKind AttackKind;
        public TacticalFollowAttackCinematicPhase LastAppliedPhase;
        public float ElapsedUnscaledSeconds;
        public float RequestedStartTime;
        public Entity SourceEntity;
        public Entity TargetEntity;
        public float3 LaunchPosition;
        public float3 ImpactPosition;
        public float3 AttackDirection;
        public float ProjectileProgress;
        public float3 ProjectilePosition;
        public float3 ProjectileDirection;
        public byte LaunchEventTriggered;
        public byte ProjectileActive;
        public byte ImpactEventTriggered;
        public byte FlyoverEventTriggered;
        public byte Completed;
        public TacticalFollowAttackCinematicAbortReason AbortReason;
        public byte TimeScaleApplied;
        public float SavedTimeScale;
        public float LastEndedElapsedTime;
        public byte HasEnded;
    }

    public struct TacticalFollowCameraTargetComponent : IComponentData
    {
        public byte Valid;
        public TacticalFollowCameraTargetKind TargetKind;
        public Entity TargetEntity;
        public float3 Center;
        public float3 LookAt;
        public float3 ForwardHint;
        public float BoundsRadius;
        public float DesiredDistance;
        public float DesiredHeight;
    }

    public struct TacticalFollowCameraPoseComponent : IComponentData
    {
        public byte Valid;
        public TacticalFollowCameraPoseSource Source;
        public float3 DesiredPosition;
        public quaternion DesiredRotation;
        public float3 LookAt;
        public float FieldOfView;
        public float OrthographicSize;
        public byte Orthographic;
        public float PositionDampingSeconds;
        public float RotationDampingSeconds;
        public float MaxTransitionSpeed;
    }

    public struct TacticalFollowCameraUiReadModelComponent : IComponentData
    {
        public byte Visible;
        public byte Enabled;
        public byte Selected;
        public int ReasonCode;
        public int FeedbackCode;
        public int FeedbackSequence;
    }
}
