using System;

namespace Game.UI.Contracts
{
    public enum NarrativeUiActionKind
    {
        CompleteText = 0,
        Continue = 1,
        Skip = 2,
        ConfirmDefaultAndSkip = 3,
        CancelSkip = 4,
        CommitCommanderIdentity = 5,
        CommitGuidance = 6,
        ReviewSeek = 7,
        JumpToDebrief = 8
    }

    public enum NarrativeInteractiveStateKind
    {
        None = 0,
        CommanderIdentity = 1,
        GuidanceChoice = 2,
        DefaultSkipConfirmation = 3,
        ReviewGameplayPlaceholder = 4
    }

    public enum NarrativeGuidanceMode
    {
        Full = 0,
        Contextual = 1,
        Minimal = 2
    }

    [Serializable]
    public struct NarrativeUiAction
    {
        public string SequenceId;
        public string StateId;
        public string LineId;
        public NarrativeUiActionKind Kind;
        public ulong TransitionToken;
    }

    [Serializable]
    public struct NarrativeCommanderIdentityData
    {
        public string Callsign;
        public string DisplayName;
    }

    [Serializable]
    public struct NarrativeCompletionPayload
    {
        public string PayloadId;
        public bool Watched;
        public bool Skipped;
        public string LastCompletedStateId;
        public string[] EvidenceIds;
        public string[] MissionContextFlags;
    }

    [Serializable]
    public struct NarrativeHandoffResult
    {
        public string DestinationId;
        public NarrativeCommanderIdentityData Commander;
        public NarrativeGuidanceMode Guidance;
        public NarrativeCompletionPayload Completion;
        public ulong TransitionToken;
    }
}
