namespace Game.Configs
{
    public enum AIDifficultySetting : byte { Easy = 0, Normal = 1, Hard = 2, Brutal = 3 }
    public enum AIStartingMoneySetting : byte { Low = 0, Normal = 1, High = 2 }
    public enum AISpeedSetting : byte { Slow = 0, Normal = 1, Fast = 2 }
    public enum AIAttackGroupSizeSetting : byte { Small = 0, Normal = 1, Large = 2 }
    public enum AIAttackFrequencySetting : byte { Rare = 0, Normal = 1, Frequent = 2 }
    public enum AIAggressionSetting : byte { Defensive = 0, Balanced = 1, Aggressive = 2 }
    public enum AIExpansionSetting : byte { Off = 0, Slow = 1, Normal = 2, Fast = 3 }
    public enum AITargetPriority : byte { Balanced = 0, Units = 1, Economy = 2, Production = 3 }

    public partial struct AISettingsSnapshot
    {
        public AIDifficultySetting Difficulty;
        public AIStartingMoneySetting StartingMoney;
        public float IncomeMultiplier;
        public AISpeedSetting BuildSpeed;
        public AISpeedSetting UnitProductionSpeed;
        public AIAttackGroupSizeSetting AttackGroupSize;
        public AIAttackFrequencySetting AttackFrequency;
        public AIAggressionSetting Aggression;
        public AIExpansionSetting Expansion;
        public AITargetPriority TargetPriority;
        public bool PlayerAutoAIEnabled;
        public int EnemyAICount;

        public static AISettingsSnapshot Defaults => new()
        {
            Difficulty = AIDifficultySetting.Normal,
            StartingMoney = AIStartingMoneySetting.Normal,
            IncomeMultiplier = 1f,
            BuildSpeed = AISpeedSetting.Normal,
            UnitProductionSpeed = AISpeedSetting.Normal,
            AttackGroupSize = AIAttackGroupSizeSetting.Normal,
            AttackFrequency = AIAttackFrequencySetting.Normal,
            Aggression = AIAggressionSetting.Balanced,
            Expansion = AIExpansionSetting.Normal,
            TargetPriority = AITargetPriority.Balanced,
            PlayerAutoAIEnabled = false,
            EnemyAICount = 1
        };
    }
}
