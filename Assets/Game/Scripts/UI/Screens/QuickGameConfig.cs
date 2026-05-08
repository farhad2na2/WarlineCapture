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
        QuickGameConfig config = Defaults;
        config.EnemyCount = Mathf.Clamp(AISettingsRuntimeState.EnemyAICount, 1, 3);
        config.Difficulty = AISettingsRuntimeState.Difficulty;
        config.StartingMoney = AISettingsRuntimeState.StartingMoney;
        config.IncomeMultiplier = Mathf.Clamp(AISettingsRuntimeState.IncomeMultiplier, 0.5f, 3f);
        config.BuildSpeed = AISettingsRuntimeState.BuildSpeed;
        config.UnitProductionSpeed = AISettingsRuntimeState.UnitProductionSpeed;
        config.AttackGroupSize = AISettingsRuntimeState.AttackGroupSize;
        config.AttackFrequency = AISettingsRuntimeState.AttackFrequency;
        config.Aggression = AISettingsRuntimeState.Aggression;
        config.Expansion = AISettingsRuntimeState.Expansion;
        config.TargetPriority = AISettingsRuntimeState.TargetPriority;
        config.PlayerAutoAIEnabled = AISettingsRuntimeState.PlayerAutoAIEnabled;
        return config;
    }

    public void ApplyToRuntimeState()
    {
        AISettingsRuntimeState.Difficulty = Difficulty;
        AISettingsRuntimeState.StartingMoney = StartingMoney;
        AISettingsRuntimeState.IncomeMultiplier = Mathf.Clamp(IncomeMultiplier, 0.5f, 3f);
        AISettingsRuntimeState.BuildSpeed = BuildSpeed;
        AISettingsRuntimeState.UnitProductionSpeed = UnitProductionSpeed;
        AISettingsRuntimeState.AttackGroupSize = AttackGroupSize;
        AISettingsRuntimeState.AttackFrequency = AttackFrequency;
        AISettingsRuntimeState.Aggression = Aggression;
        AISettingsRuntimeState.Expansion = Expansion;
        AISettingsRuntimeState.TargetPriority = TargetPriority;
        AISettingsRuntimeState.PlayerAutoAIEnabled = PlayerAutoAIEnabled;
        AISettingsRuntimeState.EnemyAICount = Mathf.Clamp(EnemyCount, 1, 3);
    }
}
