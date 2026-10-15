namespace Game.UI.Contracts
{
    public struct AriaPublicScanObservation
    {
        public bool InProgress;
        public bool Presented,SelectedDrone,ScanMode,ContactAccepted,CenterInPublicGoal,Completed;
        public UiEntityHandle Actor;
        public ulong World;
        public uint GroupId,SetupHash,Revision;
        public int RequestId,RequestFrame,ContactTarget;
        public AriaTouchTarget Scan;
    }
    public struct AriaPublicScanPlan
    {
        public bool Active,Bound;
        public uint Epoch,GroupId,SetupHash,BaselineRevision;
        public ulong GoalKey,World;
        public UiEntityHandle Actor;
        public int Stage,IssueFrame,Target,RequestId,NextPage,NextSlot,PagesVisited;
        public float Deadline,NextAt;
    }
}
