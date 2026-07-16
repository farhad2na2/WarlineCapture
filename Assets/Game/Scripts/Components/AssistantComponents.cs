using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum AssistantGuidanceLevel : byte
    {
        FullGuidance = 0,
        HintsOnly = 1,
        Minimal = 2,
        Off = 3
    }

    public enum AssistantControlState : byte
    {
        Player = 0,
        Guided = 1,
        AssistantPreview = 2,
        AssistantTakeover = 3,
        PlayerOverridePending = 4
    }

    public enum AssistantRecommendationKind : byte
    {
        None = 0,
        Select = 1,
        Move = 2,
        Attack = 3,
        Build = 4,
        Produce = 5,
        CameraFocus = 6,
        Logistics = 7,
        DefensiveAlert = 8,
        Explain = 9,
        Stop = 10
    }

    public enum AssistantMessagePriority : byte
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    public enum AssistantNarrationMode : byte
    {
        Off = 0,
        CriticalOnly = 1,
        Important = 2,
        All = 3
    }

    public enum AssistantCommandIntentStatus : byte
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Completed = 3,
        Cancelled = 4,
        TimedOut = 5
    }

    public enum AssistantCommandIntentKind : byte
    {
        None = 0,
        ShowRecommendation = 1,
        SelectEntity = 2,
        MoveToWorldPosition = 3,
        AttackEntity = 4,
        FocusCamera = 5,
        StopAssistantControl = 6,
        CancelPreview = 7
    }

    public enum AssistantTargetKind : byte
    {
        None = 0,
        Entity = 1,
        Cell = 2,
        WorldPosition = 3,
        UiSurface = 4,
        Objective = 5
    }

    public enum AssistantGoalState : byte
    {
        Active = 0,
        Complete = 1,
        Warning = 2,
        Blocked = 3,
        Failed = 4
    }

    public enum AssistantThreatKind : byte
    {
        None = 0,
        FriendlyUnderAttack = 1,
        AirAttack = 2,
        GroundAttack = 3,
        BuildingDefenseAttack = 4,
        MissileAttack = 5
    }

    public enum AssistantTargetLockState : byte
    {
        None = 0,
        Candidate = 1,
        Preview = 2,
        Executable = 3,
        Executing = 4,
        Invalid = 5
    }

    public enum AssistantFactionRelation : byte
    {
        Unknown = 0,
        Friendly = 1,
        Hostile = 2,
        Neutral = 3,
        Protected = 4
    }

    public enum AssistantDownstreamCommandKind : byte
    {
        None = 0,
        Selection = 1,
        MoveOrder = 2,
        AttackOrder = 3,
        Camera = 4
    }

    public struct AssistantStateComponent : IComponentData
    {
        public AssistantGuidanceLevel GuidanceLevel;
        public AssistantControlState ControlState;
        public int ActiveRecommendationId;
        public uint SourceVersion;
        public uint PublishedVersion;
        public int LastEvaluationFrame;
        public byte PanelOpen;
        public byte HasActiveRecommendation;
        public byte UiDirty;
    }

    public struct AssistantControlOwnerComponent : IComponentData
    {
        public AssistantControlState State;
        public int ActiveIntentRequestId;
        public int ActiveRecommendationId;
        public int ActionCount;
        public int MaxActionCount;
        public int LastPlayerInputRequestId;
        public uint LastQueuedMoveOrderToken;
        public float StartedAt;
        public float TimeoutAt;
        public byte CancelRequested;
        public byte PlayerOverrideRequested;
    }

    public struct AssistantRecommendationReadModelComponent : IComponentData
    {
        public uint Version;
        public int RecommendationCount;
        public int TopRecommendationId;
        public AssistantMessagePriority TopPriority;
        public AssistantRecommendationKind TopKind;
        public byte UiDirty;
    }

    public struct AssistantRecommendationEvaluationStateComponent : IComponentData
    {
        public uint LastGoalVersion;
        public uint LastThreatVersion;
        public uint LastFocusedUnitVersion;
        public uint LastFuelVersion;
        public int LastRouteTransitionSequenceId;
        public AssistantControlState LastControlState;
        public byte Initialized;
    }

    public struct AssistantMessageReadModelComponent : IComponentData
    {
        public uint Version;
        public int VisibleCount;
        public int LastConsumedCommandResultVersion;
        public float NextAgeBoundaryAt;
    }

    public struct AssistantNarrationStateComponent : IComponentData
    {
        public uint Version;
        public int ActiveNarrationId;
        public int ActiveAudioPlaybackRequestId;
        public int LastSpokenMessageId;
        public float LastSpokenAt;
        public float LowPriorityCooldownUntil;
        public float LastPresentedAt;
        public AssistantNarrationMode Mode;
        public AudioPlaybackRequestStatus LastAudioStatus;
        public FixedString64Bytes LastAudioFailureReason;
        public byte IsSpeaking;
        public byte UiDirty;
    }

    public struct AssistantSettingsComponent : IComponentData
    {
        public AssistantGuidanceLevel GuidanceLevel;
        public AssistantNarrationMode NarrationMode;
        public byte AllowTakeover;
        public byte SubtitlesEnabled;
        public byte LargeTextEnabled;
        public byte HighContrastEnabled;
    }

    // The UI shell boundary already owns many components. Keep large ARIA rows outside its chunk.
    [InternalBufferCapacity(0)]
    public struct AssistantGoalReadModelElement : IBufferElementData
    {
        public int GoalId;
        public int SourceVersion;
        public FixedString64Bytes ObjectiveId;
        public FixedString64Bytes OperationMapAnchorId;
        public AssistantGoalState State;
        public AssistantMessagePriority Priority;
        public FixedString64Bytes Title;
        public FixedString128Bytes Body;
        public Entity TargetEntity;
        public int2 TargetCell;
        public float3 WorldPosition;
        public byte IsPrimary;
        public byte HasTargetCell;
        public byte HasWorldPosition;
    }

    [InternalBufferCapacity(0)]
    public struct AssistantRecommendationElement : IBufferElementData
    {
        public int RecommendationId;
        public int SourceVersion;
        public AssistantRecommendationKind Kind;
        public AssistantMessagePriority Priority;
        public AssistantTargetKind TargetKind;
        public float Score;
        public Entity SourceEntity;
        public Entity TargetEntity;
        public int2 TargetCell;
        public float3 WorldPosition;
        public FixedString64Bytes TargetId;
        public FixedString64Bytes Title;
        public FixedString128Bytes Reason;
        public FixedString64Bytes RejectionReason;
        public FixedString64Bytes ActionLabel;
        public byte HasTargetCell;
        public byte HasWorldPosition;
        public byte CanShow;
        public byte CanExecute;
        public byte CanTakeControl;
    }

    [InternalBufferCapacity(0)]
    public struct AssistantMessageElement : IBufferElementData
    {
        public int MessageId;
        public int SourceVersion;
        public AssistantMessagePriority Priority;
        public AssistantRecommendationKind RelatedKind;
        public FixedString64Bytes SuppressionKey;
        public FixedString128Bytes Text;
        public FixedString64Bytes AudioEventId;
        public float CreatedAt;
        public float ExpiresAt;
        public byte RequiresNarration;
        public byte Acknowledged;
    }

    [InternalBufferCapacity(0)]
    public struct AssistantNarrationRequestElement : IBufferElementData
    {
        public int RequestId;
        public int MessageId;
        public AssistantMessagePriority Priority;
        public AssistantCommandIntentStatus Status;
        public FixedString128Bytes Text;
        public FixedString64Bytes AudioEventId;
        public uint AudioEventHash;
        public int AudioPlaybackRequestId;
        public float RequestedAt;
        public byte InterruptsLowerPriority;
    }

    [InternalBufferCapacity(0)]
    public struct AssistantCommandIntentRequestElement : IBufferElementData
    {
        public int RequestId;
        public int Frame;
        public int RecommendationId;
        public int RecommendationSourceVersion;
        public AssistantCommandIntentKind Kind;
        public AssistantTargetKind TargetKind;
        public Entity SourceEntity;
        public Entity TargetEntity;
        public int2 TargetCell;
        public float3 WorldPosition;
        public FixedString64Bytes TargetId;
        public byte FromTakeover;
    }

    [InternalBufferCapacity(0)]
    public struct AssistantCommandIntentResultElement : IBufferElementData
    {
        public int RequestId;
        public int Frame;
        public int RecommendationId;
        public AssistantCommandIntentKind Kind;
        public AssistantCommandIntentStatus Status;
        public AssistantTargetKind TargetKind;
        public Entity SourceEntity;
        public Entity TargetEntity;
        public int2 TargetCell;
        public float3 WorldPosition;
        public int ReasonCode;
        public FixedString64Bytes Message;
    }

    [InternalBufferCapacity(0)]
    public struct AssistantPreviewHighlightElement : IBufferElementData
    {
        public int RequestId;
        public int Frame;
        public int RecommendationId;
        public AssistantTargetKind TargetKind;
        public Entity SourceEntity;
        public Entity TargetEntity;
        public int2 TargetCell;
        public float3 WorldPosition;
        public float Strength;
        public byte Active;
    }

    [InternalBufferCapacity(0)]
    public struct AssistantThreatReadModelElement : IBufferElementData
    {
        public int ThreatId;
        public int SourceEventId;
        public AssistantThreatKind Kind;
        public AssistantMessagePriority Priority;
        public Entity FriendlyTarget;
        public Entity HostileSource;
        public byte FriendlyFactionId;
        public byte HostileFactionId;
        public float3 FriendlyWorldPosition;
        public float3 HostileWorldPosition;
        public float Distance;
        public int Damage;
        public int FriendlyHealth;
        public int FriendlyMaxHealth;
        public float LastObservedAt;
        public float ExpiresAt;
        public FixedString64Bytes FriendlyName;
        public FixedString64Bytes HostileName;
        public FixedString128Bytes Reason;
    }

    public struct AssistantThreatReadModelStateComponent : IComponentData
    {
        public uint Version;
        public uint LastObservedQueueVersion;
        public int LastConsumedEventId;
        public float NextExpiryAt;
        public int VisibleCount;
    }

    public struct AssistantTargetLockReadModelComponent : IComponentData
    {
        public uint Version;
        public int RecommendationId;
        public int ThreatId;
        public AssistantTargetLockState State;
        public AssistantTargetKind TargetKind;
        public AssistantFactionRelation FactionRelation;
        public Entity SourceEntity;
        public Entity TargetEntity;
        public int2 TargetCell;
        public float3 WorldPosition;
        public float Distance;
        public int HealthCurrent;
        public int HealthMax;
        public byte Visible;
        public byte HasTargetCell;
        public byte HasWorldPosition;
        public byte HasDistance;
        public byte HasHealth;
        public FixedString64Bytes SourceName;
        public FixedString64Bytes TargetName;
        public FixedString128Bytes Reason;
    }

    [InternalBufferCapacity(0)]
    public struct AssistantCommandDispatchElement : IBufferElementData
    {
        public int AssistantRequestId;
        public int RecommendationId;
        public AssistantCommandIntentKind IntentKind;
        public AssistantDownstreamCommandKind DownstreamKind;
        public int DownstreamRequestId;
        public AssistantCommandIntentStatus Status;
        public int ReasonCode;
        public float RequestedAt;
    }
}
