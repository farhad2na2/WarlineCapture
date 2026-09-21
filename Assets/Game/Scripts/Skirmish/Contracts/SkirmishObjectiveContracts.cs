namespace Game.Skirmish.Contracts
{
    public enum SkirmishObjectiveKind : byte
    {
        None = 0,
        BaseAssault = 1,
        FrontlineControl = 2,
        Breakthrough = 3,
        ConvoyEscort = 4
    }

    public enum SkirmishObjectiveStateKind : byte
    {
        None = 0,
        Pending = 1,
        Active = 2,
        TerminalVictory = 3,
        TerminalDefeat = 4,
        TerminalDraw = 5,
        TechnicalFailure = 6
    }

    public enum SkirmishOutcomeKind : byte
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
        Draw = 3,
        TechnicalFailure = 4
    }

    public enum SkirmishEndReasonKind : byte
    {
        None = 0,
        MainBaseDestroyed = 1,
        BothBasesDestroyed = 2,
        TimeLimit = 3,
        Surrender = 4,
        TicketsDepleted = 5,
        TicketsAtDeadline = 6,
        BreakthroughEvacuated = 7,
        BreakthroughImpossible = 8,
        ConvoyDelivered = 9,
        ConvoyImpossible = 10,
        ObjectiveDeadline = 11,
        StartupFailure = 12,
        ContentFailure = 13,
        CheckpointFailure = 14
    }

    public enum SkirmishObjectiveRoleKind : byte
    {
        None = 0,
        PlayerBase = 1,
        EnemyBase = 2,
        CaptureZone = 3,
        BreakthroughDesignated = 4,
        ConvoyTruck = 5
    }

    public static class SkirmishObjectiveIds
    {
        public const string BasePlayer = "base.player";
        public const string BaseEnemy = "base.enemy";
        public const string Convoy1 = "convoy.1";
        public const string Convoy2 = "convoy.2";
        public const string Convoy3 = "convoy.3";

        public static bool TryParse(string value, out SkirmishObjectiveKind kind)
        {
            if (value == "BA")
            {
                kind = SkirmishObjectiveKind.BaseAssault;
                return true;
            }

            if (value == "FC")
            {
                kind = SkirmishObjectiveKind.FrontlineControl;
                return true;
            }

            if (value == "BT")
            {
                kind = SkirmishObjectiveKind.Breakthrough;
                return true;
            }

            if (value == "CE")
            {
                kind = SkirmishObjectiveKind.ConvoyEscort;
                return true;
            }

            kind = SkirmishObjectiveKind.None;
            return false;
        }

        public static string ToCode(SkirmishObjectiveKind kind)
        {
            switch (kind)
            {
                case SkirmishObjectiveKind.BaseAssault: return "BA";
                case SkirmishObjectiveKind.FrontlineControl: return "FC";
                case SkirmishObjectiveKind.Breakthrough: return "BT";
                case SkirmishObjectiveKind.ConvoyEscort: return "CE";
                default: return string.Empty;
            }
        }
    }
}
