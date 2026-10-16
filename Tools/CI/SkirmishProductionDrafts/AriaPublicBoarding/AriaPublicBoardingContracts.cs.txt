namespace Game.UI.Contracts
{
    public struct AriaPublicBoardingObservation
    {
        public bool Presented,Eligible,ExitOpen,BoardMode,Accepted,AllMounted,AlreadyMounted,CarrierSelected,CarrierInView,AtExit,MoveAccepted,ExistingMoveToExit;
        public uint GroupId,CarrierGroupId,SetupHash,MoveRevision;
        public UiEntityHandle Carrier;
        public ulong World;
        public int RequestId,Frame,ContactTarget,MoveFrame;
        public AriaTouchTarget Board,CarrierTarget,FocusCarrier,SelectCarrier;
    }
    public struct AriaPublicBoardingPlan
    {
        public bool Active;
        public uint Epoch,GroupId,CarrierGroupId,SetupHash,BaselineMoveRevision;
        public UiEntityHandle Carrier;
        public ulong World,Goal;
        public int Stage,IssueFrame,Target,RequestId;
        public float Deadline,NextAt,InspectUntil;
    }
}
