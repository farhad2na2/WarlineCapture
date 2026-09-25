namespace Game.Skirmish.Contracts
{
    public enum SkirmishStrategyPriority : byte
    {
        None = 0,
        DefendHome = 1,
        Reserve = 2,
        Scout = 3,
        RecruitCounter = 4,
        AttackBase = 5,
        Hold = 6
    }

    public enum SkirmishAriaSkillKind : byte
    {
        None = 0,
        Inspect = 1,
        Recruit = 2,
        SelectGroup = 3,
        Attack = 4,
        Hold = 5,
        Scout = 6,
        RecognizeResult = 7,
        Handback = 8
    }

    public enum SkirmishAriaSkillPhase : byte
    {
        Observe = 0,
        Choose = 1,
        Act = 2,
        Verify = 3,
        Retry = 4,
        Alternative = 5,
        Handback = 6,
        Terminal = 7
    }

    public struct SkirmishPublicPerception
    {
        public int OwnInfantryLive;
        public int OwnGroundLive;
        public int OwnSupplyLive;
        public int OwnSupplyCap;
        public int OwnStartingSupply;
        public int OwnMaterials;
        public int OwnFuel;
        public int OwnAirLive;
        public int AirCap;
        public int VisibleHostileInfantry;
        public int VisibleHostileGround;
        public int VisibleHostileTanks;
        public int VisibleHostileAir;
        public bool HelipadPresent;
        public bool AirportPresent;
        public bool PlayerDesignatedAlive;
        public bool EnemyDesignatedAlive;
        public bool KnowsHostileMaterials;
        public int HostileMaterialsIfKnown;
        public bool Playing;
        public bool Finished;
    }

    public struct SkirmishStrategyScore
    {
        public SkirmishStrategyPriority Priority;
        public int ObjectiveGain;
        public int PreventLoss;
        public int CounterAdvantage;
        public int TravelPenalty;
        public int ExposurePenalty;
        public int Total;
        public string Field;
        public SkirmishRoleKind RecruitRole;
    }

    public struct SkirmishAriaPublicView
    {
        public bool Playing;
        public bool Finished;
        public bool PlayerDesignatedAlive;
        public bool EnemyDesignatedAlive;
        public bool RecruitControlAvailable;
        public bool AttackControlAvailable;
        public bool HoldControlAvailable;
        public bool GroupControlAvailable;
        public bool CanAffordRifle;
        public bool CanAffordRocketeer;
        public bool CanAffordTank;
        public bool CanAffordAntiAir;
        public bool CanAffordLogisticsTruck, LogisticsTruckCommitted, RifleRecruitPending, AirProfile;
        public bool CanBuildAirPad;
        public bool PadReady;
        public bool AirQueueOffered;
        public bool PadPresent, ReadinessEligible;
        public bool AirPadControlAvailable;
        public int VisibleHostileCombat;
        public int VisibleHostileAir;
        public int OwnInfantry;
        public int OwnMaterials;
        public int FailedAttempts;
        public SkirmishAriaSkillKind LastSkill;
        public bool LastSkillAccepted;
    }

    public struct SkirmishAriaSkillDecision
    {
        public bool Accepted;
        public SkirmishAriaSkillKind Skill;
        public SkirmishAriaSkillPhase Phase;
        public SkirmishReasonCode Reason;
        public string Field;
        public int RetryCount;
        public bool Handback;
    }
}
