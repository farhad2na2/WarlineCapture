namespace Game.UI.Contracts
{
    public enum AriaPublicAirRole : byte { None,AttackHeliLight,AttackHeli,Fighter,Strike,Drone,TransportHeli,TransportPlane }
    public struct AriaOffensiveAirObservation
    {
        public bool Presented,Eligible,Busy,Returning,Paused,AcceptedContact,GoalFits;
        public UiEntityHandle Actor;
        public ulong World;
        public uint GroupId,SetupHash,Revision;
        public int RequestFrame,ContactTarget;
        public AriaPublicAirRole Role;
    }
    public struct AriaOffensiveAirPlan
    {
        public bool Active,Bound;
        public uint Epoch,GroupId,SetupHash,BaselineRevision;
        public ulong World,GoalKey;
        public UiEntityHandle Actor;
        public int Stage,IssueFrame,Target,NextSlot,NextPage,PagesVisited;
        public float Deadline,NextAt;
    }
    public struct AriaAirRecruitChoice
    {
        public AriaPublicAirRole Role;
        public bool CanQueue,CanQueueAtFullReadiness,Pending;
        public int Live;
        public AriaTouchTarget Target;
    }
    public struct AriaAirRecruitObservation
    {
        public bool Presented;
        public AriaAirRecruitChoice Light,Heavy,Fighter,Strike,Drone,TransportHeli,TransportPlane;
        public AriaAirRecruitChoice For(AriaPublicAirRole role)=>role switch
        {
            AriaPublicAirRole.AttackHeliLight=>Light,AriaPublicAirRole.AttackHeli=>Heavy,
            AriaPublicAirRole.Fighter=>Fighter,AriaPublicAirRole.Strike=>Strike,AriaPublicAirRole.Drone=>Drone,
            AriaPublicAirRole.TransportHeli=>TransportHeli,AriaPublicAirRole.TransportPlane=>TransportPlane,_=>default
        };
    }
}
