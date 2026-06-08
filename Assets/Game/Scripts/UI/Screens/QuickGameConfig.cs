using UnityEngine;

public enum QuickGameEnemyType : byte
{
    Balanced = 0,
    Military = 1,
    Defensive = 2,
    Air = 3,
    Swarm = 4,
    Random = 5
}

public enum QuickGameWinCondition : byte
{
    DestroyAllEnemies = 0,
    SurviveDuration = 1,
    Sandbox = 2
}

public enum QuickGameStartingResources : byte
{
    Standard = 0,
    Low = 1,
    High = 2
}

public struct QuickGameConfig
{
    public QuickGameEnemyType EnemyType;
    public int EnemyCount;
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
    public QuickGameWinCondition WinCondition;
    public bool FogOfWar;
    public bool IntelReveal;
    public QuickGameStartingResources StartingResources;
    public int MapSeed;

    public static QuickGameConfig Defaults => new()
    {
        EnemyType = QuickGameEnemyType.Balanced,
        EnemyCount = 1,
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
        WinCondition = QuickGameWinCondition.DestroyAllEnemies,
        FogOfWar = false,
        IntelReveal = true,
        StartingResources = QuickGameStartingResources.Standard,
        MapSeed = 104729
    };

    public static QuickGameConfig FromRuntimeState()
    {
        return FromAISettingsSnapshot(AISettingsRuntimeState.CurrentSnapshot);
    }

    public static QuickGameConfig FromAISettingsSnapshot(AISettingsSnapshot snapshot)
    {
        QuickGameConfig config = Defaults;
        config.EnemyCount = Mathf.Clamp(snapshot.EnemyAICount, 1, 3);
        config.Difficulty = snapshot.Difficulty;
        config.StartingMoney = snapshot.StartingMoney;
        config.IncomeMultiplier = Mathf.Clamp(snapshot.IncomeMultiplier, 0.5f, 3f);
        config.BuildSpeed = snapshot.BuildSpeed;
        config.UnitProductionSpeed = snapshot.UnitProductionSpeed;
        config.AttackGroupSize = snapshot.AttackGroupSize;
        config.AttackFrequency = snapshot.AttackFrequency;
        config.Aggression = snapshot.Aggression;
        config.Expansion = snapshot.Expansion;
        config.TargetPriority = snapshot.TargetPriority;
        config.PlayerAutoAIEnabled = snapshot.PlayerAutoAIEnabled;
        return config;
    }

    public AISettingsSnapshot ToAISettingsSnapshot()
    {
        return new AISettingsSnapshot
        {
            Difficulty = Difficulty,
            StartingMoney = StartingMoney,
            IncomeMultiplier = Mathf.Clamp(IncomeMultiplier, 0.5f, 3f),
            BuildSpeed = BuildSpeed,
            UnitProductionSpeed = UnitProductionSpeed,
            AttackGroupSize = AttackGroupSize,
            AttackFrequency = AttackFrequency,
            Aggression = Aggression,
            Expansion = Expansion,
            TargetPriority = TargetPriority,
            PlayerAutoAIEnabled = PlayerAutoAIEnabled,
            EnemyAICount = Mathf.Clamp(EnemyCount, 1, 3)
        };
    }

    public void ApplyToRuntimeState()
    {
        AISettingsRuntimeState.ApplySnapshot(ToAISettingsSnapshot());
    }
}
