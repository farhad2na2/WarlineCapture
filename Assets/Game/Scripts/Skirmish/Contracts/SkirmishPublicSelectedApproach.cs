namespace Game.Skirmish.Contracts
{
    // Published own-force planning data. None of these fields establish enemy visibility.
    public struct SkirmishPublicApproachMember
    {
        public uint GroupId;
        public float X, Y, Z;
        public float AcquisitionRadius;
        public bool StructureCapable;
        // Current own order state only; never an all-member accepted-group proof.
        public bool HasOwnMoveGoal;
        public int OwnGoalX, OwnGoalZ;
    }

    public struct SkirmishPublicSelectedApproach
    {
        public bool Required;
        public bool ContextKnown;
        public bool PublicObjectiveContactVisible;
        public int SelectedCount, StructureCapableCount, RelevantCount;
        public int InOwnAcquisitionRangeCount;
        public bool LegalGoalAvailable;
        public float GoalX, GoalY, GoalZ;
        public bool LegalGoalWithinOwnAcquisitionRange;
        public string UnavailableReason;
        public long ProjectionTicks, GoalEvaluationTicks, AllocatedBytes, TimerFrequency;
    }
}
