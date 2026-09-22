namespace Game.Skirmish.Contracts
{
    public enum SkirmishResearchKind : byte
    {
        None = 0,
        Readiness = 1,
        InfantryWeapons = 2,
        VehicleProtection = 3,
        AircraftEfficiency = 4
    }

    public enum SkirmishResearchPhase : byte
    {
        None = 0,
        Queued = 1,
        Researching = 2,
        Completed = 3,
        Cancelled = 4,
        Lost = 5
    }

    public static class SkirmishResearchCosts
    {
        public const int CategoryMaterials = 200;
        public const float CategorySeconds = 45f;
        public const int ReadinessEstablishedMaterials = 240;
        public const float ReadinessEstablishedSeconds = 45f;
        public const int ReadinessFullArsenalMaterials = 480;
        public const float ReadinessFullArsenalSeconds = 60f;
        public const int CancelAfterStartPercent = 75;
        public const int InfantryDamagePercent = 110;
        public const int VehicleHealthPercent = 110;
        public const int AircraftFuelPercent = 90;

        public static int RefundMaterials(int paid, SkirmishResearchPhase phase)
        {
            if (paid <= 0)
                return 0;
            if (phase == SkirmishResearchPhase.Queued)
                return paid;
            if (phase == SkirmishResearchPhase.Researching)
                return paid * CancelAfterStartPercent / 100;
            return 0;
        }

        public static int RefundProduce(int paid, SkirmishReservationPhase phase)
        {
            if (paid <= 0)
                return 0;
            if (phase == SkirmishReservationPhase.Reserved)
                return paid;
            if (phase == SkirmishReservationPhase.Producing)
                return paid * CancelAfterStartPercent / 100;
            return 0;
        }

        public static int Scale(int value, int percent)
        {
            if (value <= 0)
                return 0;
            return (value * percent + 50) / 100;
        }
    }

    public struct SkirmishResearchDecision
    {
        public bool Accepted;
        public SkirmishReasonCode Reason;
        public string Field;
        public SkirmishResearchKind Kind;
        public SkirmishResearchPhase Phase;
        public int MaterialsCost;
        public int RefundedMaterials;
        public uint ResearchId;
        public SkirmishReadinessStage ReadinessAfter;
        public byte LevelAfter;
    }
}
