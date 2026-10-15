namespace Game.UI.Contracts
{
    public struct AriaAircraftServiceObservation
    {
        public bool NativeTaskBusy;
        public bool Presented,Airborne,Returning,Rolling,HomeKnown,AtHome,Paused,AcceptedReturnContact;
        public UiEntityHandle Actor;
        public ulong World;
        public uint GroupId,SetupHash,ReturnRevision;
        public int PooledFactionFuel,RequestFrame,RequestId,ReturnContactTarget;
    }
    public struct AriaAircraftServicePlan
    {
        public UiEntityHandle Actor;
        public ulong World;
        public uint GroupId,SetupHash,ReturnRevision;
        public int Stage,IssueFrame,Target,Actions,NextPage,NextSlot,PagesVisited;
        public bool Bound;
    }
}
