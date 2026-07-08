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
        Blocked = 3
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

    public struct AssistantNarrationStateComponent : IComponentData
    {
        public uint Version;
        public int ActiveNarrationId;
        public int LastSpokenMessageId;
        public float LastSpokenAt;
        public float LowPriorityCooldownUntil;
        public AssistantNarrationMode Mode;
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

    public struct AssistantGoalReadModelElement : IBufferElementData
    {
        public int GoalId;
        public int SourceVersion;
        public AssistantGoalState State;
        public AssistantMessagePriority Priority;
        public FixedString64Bytes Title;
        public FixedString128Bytes Body;
        public byte IsPrimary;
    }

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
        public FixedString64Bytes Title;
        public FixedString128Bytes Reason;
        public FixedString64Bytes ActionLabel;
        public byte CanShow;
        public byte CanExecute;
        public byte CanTakeControl;
    }

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

    public struct AssistantNarrationRequestElement : IBufferElementData
    {
        public int RequestId;
        public int MessageId;
        public AssistantMessagePriority Priority;
        public AssistantCommandIntentStatus Status;
        public FixedString128Bytes Text;
        public FixedString64Bytes AudioEventId;
        public float RequestedAt;
        public byte InterruptsLowerPriority;
    }

    public struct AssistantCommandIntentRequestElement : IBufferElementData
    {
        public int RequestId;
        public int Frame;
        public int RecommendationId;
        public AssistantCommandIntentKind Kind;
        public AssistantTargetKind TargetKind;
        public Entity SourceEntity;
        public Entity TargetEntity;
        public int2 TargetCell;
        public float3 WorldPosition;
        public FixedString64Bytes TargetId;
        public byte FromTakeover;
    }

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
}
