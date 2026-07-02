using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public enum SelectionHudFeedbackKind : byte
    {
        Selection,
        SquadSelection,
        ClearSelection,
        CommandMode,
        ClearCommandMode,
        CommandResult,
        WorldMarkersVisible
    }

    public struct SelectionHudFeedbackQueueComponent : IComponentData
    {
    }

    public struct SelectionHudFeedbackElement : IBufferElementData
    {
        public SelectionHudFeedbackKind Kind;
        public FixedString64Bytes Label;
        public FixedString64Bytes Status;
        public int Count;
        public int CommandMode;
        public byte CommandAccepted;
        public int ReasonCode;
        public FixedString64Bytes Message;
        public byte Visible;
    }
}
