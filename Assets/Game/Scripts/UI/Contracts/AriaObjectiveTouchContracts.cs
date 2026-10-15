namespace Game.UI.Contracts
{
    public enum AriaObjectiveAction : byte { None, Move, Hold, Unload, Inspect, Repair }
    // This compact value is the displayed frame's policy decision. PublicKey is a
    // presenter-owned collision-checked marker key, not an Entity or native handle.
    public struct AriaObjectiveTouchObservation
    {
        public bool Applicable, Supported, Presented, Finished, CameraSettled, Paused;
        public uint GroupId, NavigationGroupId, RosterRevision, PresentationEpoch;
        public ulong PublicKey;
        public AriaObjectiveAction Action;
        public AriaSkirmishIntent Intent;
        public int Reason;
        public bool ExclusiveGroupSelected, MoveMode;
        public AriaTouchTarget SelectGroup, ClearOtherSelection, Focus, Move, Hold, Unload, OpenUnload, Repair, OpenRepair, Ground;
        public bool GoalInView, GroundFitsPublicGoal;
        // Accepted normal command visibly shown for this exact group and goal.
        // A completed contact, changed selection or animation is not this receipt.
        public uint AcceptedOrderRevision, AcceptedGroupId, AcceptedNavigationGroupId;
        public int AcceptedContactTargetId, AcceptedFrame;
        public ulong AcceptedPublicKey;
        public AriaObjectiveAction AcceptedAction;
        public bool ProgressKnown,RepairWorkActive;
        public uint PublicProgressRevision;
    }
    public struct AriaObjectiveTouchPlan
    {
        public uint GroupId, NavigationGroupId, RosterRevision, PresentationEpoch, OrderRevisionAtIssue, ProgressRevision;
        public ulong PublicKey;
        public AriaObjectiveAction Action;
        public int Stage, LastContactActions, LastTarget, RejectedAttempts, LastFrame, IssueFrame;
        public float Deadline, ObserveUntil;
        public bool CommandPending;
        public int ScrollTargetId;
        public float BestScrollDistance;
    }
}
