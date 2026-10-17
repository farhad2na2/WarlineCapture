namespace Game.UI.Contracts
{
    // Publicly presented own state only; never Entity handles or hidden contacts.
    public enum AriaObjectiveSupportAction : byte
    { None, Rifle, AntiAir, LogisticsTruck, Tanker, Readiness, Helipad, AttackAircraft, ReturnAircraft }
    public struct AriaObjectiveSupportObservation
    {
        public bool Evacuation;
        public bool Presented, CriticalPublicRisk, ResearchVisible, ResearchInProgress, CaptureMajority;
        public byte Perspective, Readiness;
        public int DismountedCombatGroups, ResearchMaterials;
        public uint ResearchId;
        public float SimulationSeconds;
    }
    public struct AriaObjectiveSupportPlan
    {
        public AriaPublicAirRole RecruitAirRole;
        public AriaObjectiveSupportAction Action;
        public AriaAircraftServicePlan Aircraft;
        public float NextAircraftCheck;
        public uint Epoch, ResearchId, BaselineRosterRevision;
        public int LastFrame, LastActions, LastTarget, RepeatedContacts, BaselineInfantry,
            BaselineTankers, BaselineAir, AirStage, ScrollTargetId;
        public byte BaselineReadiness;
        public float Deadline, NextDecisionAt, AirActiveSince, BestScrollDistance;
        public bool ObservedAcceptance, Closing;
    }
}
