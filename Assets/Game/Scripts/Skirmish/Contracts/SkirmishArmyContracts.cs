namespace Game.Skirmish.Contracts
{
    public enum SkirmishGroupOrderKind : byte
    {
        None = 0,
        Move = 1,
        Attack = 2,
        Hold = 3
    }

    public enum SkirmishContactSight : byte
    {
        Unknown = 0,
        LastSeen = 1,
        Visible = 2
    }

    public static class SkirmishArmyPaging
    {
        public const int StandardPageSize = 4;
    }

    public struct SkirmishCommandDecision
    {
        public bool Accepted;
        public SkirmishReasonCode Reason;
        public string Field;
        public SkirmishGroupOrderKind Order;
        public int IssuedCount;
        public int ExcludedCount;
        public uint GroupId;
    }
}
