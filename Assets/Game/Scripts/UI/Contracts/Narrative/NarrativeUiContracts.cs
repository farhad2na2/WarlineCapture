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

    [Serializable]
    public struct NarrativeUiAction
    {
        public string SequenceId;
        public string StateId;
        public string LineId;
        public NarrativeUiActionKind Kind;
        public ulong TransitionToken;
    }

}
