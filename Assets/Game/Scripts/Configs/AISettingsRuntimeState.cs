using Unity.Entities;
using UnityEngine;

public enum AIDifficultySetting : byte
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Brutal = 3
}

public enum AIStartingMoneySetting : byte
{
    Low = 0,
    Normal = 1,
    High = 2
}

public enum AISpeedSetting : byte
{
    Slow = 0,
    Normal = 1,
    Fast = 2
}

public enum AIAttackGroupSizeSetting : byte
{
    Small = 0,
    Normal = 1,
    Large = 2
}

public enum AIAttackFrequencySetting : byte
{
    Rare = 0,
    Normal = 1,
    Frequent = 2
}

public enum AIAggressionSetting : byte
{
    Defensive = 0,
    Balanced = 1,
    Aggressive = 2
}

public enum AIExpansionSetting : byte
{
    Off = 0,
    Slow = 1,
    Normal = 2,
    Fast = 3
}

public enum AITargetPriority : byte
{
    Balanced = 0,
    Units = 1,
    Economy = 2,
    Production = 3
}

public struct AISettingsSnapshot
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

    public bool IsEnemyAIIndexEnabled(int enemyIndex)
    {
        return enemyIndex >= 0 && enemyIndex < Mathf.Clamp(EnemyAICount, 1, 3);
    }

    public bool ResolveEnabled(AIControllerConfig config)
    {
        if (config == null || !config.Enabled)
            return false;

        return config.Role != AIControllerRole.PlayerAuto || PlayerAutoAIEnabled;
    }

    public int ApplyStartingMoney(int baseMoney, AIControllerRole role)
    {
        if (role != AIControllerRole.Enemy)
            return Mathf.Max(0, baseMoney);

        return Mathf.Max(0, Mathf.RoundToInt(baseMoney * DifficultyMoneyMultiplier() * StartingMoneyMultiplier()));
    }

    public float ApplyIncomeMultiplier(float baseMultiplier, AIControllerRole role)
    {
        float multiplier = Mathf.Max(0f, baseMultiplier);
        if (role == AIControllerRole.Enemy)
            multiplier *= DifficultyIncomeMultiplier() * Mathf.Clamp(IncomeMultiplier, 0.5f, 3f);
        return Mathf.Max(0f, multiplier);
    }

    public float ApplyBuildInterval(float baseSeconds, AIControllerRole role)
    {
        float interval = Mathf.Max(0.1f, baseSeconds);
        if (role == AIControllerRole.Enemy)
            interval *= DifficultyTimingMultiplier() * SpeedIntervalMultiplier(BuildSpeed) * ExpansionIntervalMultiplier();
        return Mathf.Max(0.1f, interval);
    }

    public float ApplyProductionInterval(float baseSeconds, AIControllerRole role)
    {
        float interval = Mathf.Max(0.1f, baseSeconds);
        if (role == AIControllerRole.Enemy)
            interval *= DifficultyTimingMultiplier() * SpeedIntervalMultiplier(UnitProductionSpeed);
        return Mathf.Max(0.1f, interval);
    }

    public bool ResolveBuildEnabled(AIControllerConfig config)
    {
        if (!ResolveEnabled(config))
            return false;

        return config.Role != AIControllerRole.Enemy || Expansion != AIExpansionSetting.Off;
    }

    public int ApplyMaxSquadUnits(int baseMaxUnits, AIControllerRole role)
    {
        int units = Mathf.Max(1, baseMaxUnits);
        if (role != AIControllerRole.Enemy)
            return units;

        units += DifficultyMaxUnitDelta();
        units += AttackGroupSize switch
        {
            AIAttackGroupSizeSetting.Small => -2,
            AIAttackGroupSizeSetting.Large => 4,
            _ => 0
        };
        return Mathf.Clamp(units, 2, 24);
    }

    public int ApplyMinSquadUnits(int baseMinUnits, int maxUnits, AIControllerRole role)
    {
        int units = Mathf.Max(1, baseMinUnits);
        if (role == AIControllerRole.Enemy)
        {
            units += DifficultyMinUnitDelta();
            units += AttackGroupSize switch
            {
                AIAttackGroupSizeSetting.Small => -1,
                AIAttackGroupSizeSetting.Large => 1,
                _ => 0
            };
            units += Aggression switch
            {
                AIAggressionSetting.Defensive => 1,
                AIAggressionSetting.Aggressive => -1,
                _ => 0
            };
        }

        return Mathf.Clamp(units, 1, Mathf.Max(1, maxUnits));
    }

    public int ApplyMaxActiveSquads(int baseMaxActiveSquads, AIControllerRole role)
    {
        int squads = Mathf.Max(1, baseMaxActiveSquads);
        if (role == AIControllerRole.Enemy)
        {
            squads += DifficultyActiveSquadDelta();
            squads += AttackFrequency switch
            {
                AIAttackFrequencySetting.Rare => -1,
                AIAttackFrequencySetting.Frequent => 1,
                _ => 0
            };
            squads += Aggression == AIAggressionSetting.Aggressive ? 1 : 0;
        }

        return Mathf.Clamp(squads, 1, 8);
    }

    private float DifficultyMoneyMultiplier()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => 0.75f,
            AIDifficultySetting.Hard => 1.25f,
            AIDifficultySetting.Brutal => 1.6f,
            _ => 1f
        };
    }

    private float DifficultyIncomeMultiplier()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => 0.8f,
            AIDifficultySetting.Hard => 1.2f,
            AIDifficultySetting.Brutal => 1.5f,
            _ => 1f
        };
    }

    private float DifficultyTimingMultiplier()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => 1.25f,
            AIDifficultySetting.Hard => 0.85f,
            AIDifficultySetting.Brutal => 0.65f,
            _ => 1f
        };
    }

    private float StartingMoneyMultiplier()
    {
        return StartingMoney switch
        {
            AIStartingMoneySetting.Low => 0.75f,
            AIStartingMoneySetting.High => 1.5f,
            _ => 1f
        };
    }

    private static float SpeedIntervalMultiplier(AISpeedSetting speed)
    {
        return speed switch
        {
            AISpeedSetting.Slow => 1.35f,
            AISpeedSetting.Fast => 0.7f,
            _ => 1f
        };
    }

    private float ExpansionIntervalMultiplier()
    {
        return Expansion switch
        {
            AIExpansionSetting.Slow => 1.4f,
            AIExpansionSetting.Fast => 0.75f,
            _ => 1f
        };
    }

    private int DifficultyMaxUnitDelta()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => -1,
            AIDifficultySetting.Hard => 2,
            AIDifficultySetting.Brutal => 4,
            _ => 0
        };
    }

    private int DifficultyMinUnitDelta()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => -1,
            AIDifficultySetting.Hard => 1,
            AIDifficultySetting.Brutal => 2,
            _ => 0
        };
    }

    private int DifficultyActiveSquadDelta()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => -1,
            AIDifficultySetting.Hard => 1,
            AIDifficultySetting.Brutal => 2,
            _ => 0
        };
    }
}

public static class AISettingsRuntimeState
{
    private static AISettingsSnapshot current = AISettingsSnapshot.Defaults;

    public static AISettingsSnapshot CurrentSnapshot => current;
    public static AIDifficultySetting Difficulty
    {
        get => current.Difficulty;
        set => current.Difficulty = value;
    }

    public static AIStartingMoneySetting StartingMoney
    {
        get => current.StartingMoney;
        set => current.StartingMoney = value;
    }

    public static float IncomeMultiplier
    {
        get => current.IncomeMultiplier;
        set => current.IncomeMultiplier = Mathf.Clamp(value, 0.5f, 3f);
    }

    public static AISpeedSetting BuildSpeed
    {
        get => current.BuildSpeed;
        set => current.BuildSpeed = value;
    }

    public static AISpeedSetting UnitProductionSpeed
    {
        get => current.UnitProductionSpeed;
        set => current.UnitProductionSpeed = value;
    }

    public static AIAttackGroupSizeSetting AttackGroupSize
    {
        get => current.AttackGroupSize;
        set => current.AttackGroupSize = value;
    }

    public static AIAttackFrequencySetting AttackFrequency
    {
        get => current.AttackFrequency;
        set => current.AttackFrequency = value;
    }

    public static AIAggressionSetting Aggression
    {
        get => current.Aggression;
        set => current.Aggression = value;
    }

    public static AIExpansionSetting Expansion
    {
        get => current.Expansion;
        set => current.Expansion = value;
    }

    public static AITargetPriority TargetPriority
    {
        get => current.TargetPriority;
        set => current.TargetPriority = value;
    }

    public static bool PlayerAutoAIEnabled
    {
        get => current.PlayerAutoAIEnabled;
        set => current.PlayerAutoAIEnabled = value;
    }

    public static int EnemyAICount
    {
        get => current.EnemyAICount;
        set => current.EnemyAICount = Mathf.Clamp(value, 1, 3);
    }

    public static void ApplySnapshot(AISettingsSnapshot snapshot)
    {
        current = snapshot;
        current.IncomeMultiplier = Mathf.Clamp(current.IncomeMultiplier, 0.5f, 3f);
        current.EnemyAICount = Mathf.Clamp(current.EnemyAICount, 1, 3);
    }

    public static void ResetDefaults()
    {
        current = AISettingsSnapshot.Defaults;
    }

    public static bool IsEnemyAIIndexEnabled(int enemyIndex)
    {
        return current.IsEnemyAIIndexEnabled(enemyIndex);
    }

    public static bool ResolveEnabled(AIControllerConfig config)
    {
        return current.ResolveEnabled(config);
    }

    public static int ApplyStartingMoney(int baseMoney, AIControllerRole role)
    {
        return current.ApplyStartingMoney(baseMoney, role);
    }

    public static float ApplyIncomeMultiplier(float baseMultiplier, AIControllerRole role)
    {
        return current.ApplyIncomeMultiplier(baseMultiplier, role);
    }

    public static float ApplyBuildInterval(float baseSeconds, AIControllerRole role)
    {
        return current.ApplyBuildInterval(baseSeconds, role);
    }

    public static float ApplyProductionInterval(float baseSeconds, AIControllerRole role)
    {
        return current.ApplyProductionInterval(baseSeconds, role);
    }

    public static bool ResolveBuildEnabled(AIControllerConfig config)
    {
        return current.ResolveBuildEnabled(config);
    }

    public static int ApplyMaxSquadUnits(int baseMaxUnits, AIControllerRole role)
    {
        return current.ApplyMaxSquadUnits(baseMaxUnits, role);
    }

    public static int ApplyMinSquadUnits(int baseMinUnits, int maxUnits, AIControllerRole role)
    {
        return current.ApplyMinSquadUnits(baseMinUnits, maxUnits, role);
    }

    public static int ApplyMaxActiveSquads(int baseMaxActiveSquads, AIControllerRole role)
    {
        return current.ApplyMaxActiveSquads(baseMaxActiveSquads, role);
    }

    public static void ApplyToWorld(World world)
    {
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;

        using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<FactionEconomy>(), ComponentType.ReadWrite<FactionEconomyPolicy>()))
        {
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                FactionEconomy economy = em.GetComponentData<FactionEconomy>(entity);
                if (!FactionIdentitySystem.IsAiControlledByDefault(economy.FactionId))
                    continue;

                FactionEconomyPolicy policy = em.GetComponentData<FactionEconomyPolicy>(entity);
                policy.IncomeMultiplier = ApplyIncomeMultiplier(1f, AIControllerRole.Enemy);
                em.SetComponentData(entity, policy);
            }
        }

        using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIBuildPlan>()))
        {
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(entity);
                if (!FactionIdentitySystem.IsAiControlledByDefault(plan.FactionId))
                    continue;

                plan.Enabled = Expansion == AIExpansionSetting.Off ? (byte)0 : (byte)1;
                plan.BuildIntervalSeconds = ApplyBuildInterval(8f, AIControllerRole.Enemy);
                plan.LastLogTime = -999f;
                em.SetComponentData(entity, plan);
            }
        }

        using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIProductionPlan>()))
        {
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AIProductionPlan plan = em.GetComponentData<AIProductionPlan>(entity);
                if (!FactionIdentitySystem.IsAiControlledByDefault(plan.FactionId))
                    continue;

                plan.UnitProductionIntervalSeconds = ApplyProductionInterval(6f, AIControllerRole.Enemy);
                plan.LastLogTime = -999f;
                em.SetComponentData(entity, plan);
            }
        }

        using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AISquadPlan>()))
        {
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AISquadPlan plan = em.GetComponentData<AISquadPlan>(entity);
                if (!FactionIdentitySystem.IsAiControlledByDefault(plan.FactionId))
                    continue;

                int maxUnits = ApplyMaxSquadUnits(8, AIControllerRole.Enemy);
                plan.MaxUnits = maxUnits;
                plan.MinUnits = ApplyMinSquadUnits(3, maxUnits, AIControllerRole.Enemy);
                plan.MaxActiveSquads = ApplyMaxActiveSquads(2, AIControllerRole.Enemy);
                plan.LastLogTime = -999f;
                em.SetComponentData(entity, plan);
            }
        }

        using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AITargetPrioritySetting>()))
        {
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AITargetPrioritySetting setting = em.GetComponentData<AITargetPrioritySetting>(entity);
                if (!FactionIdentitySystem.IsAiControlledByDefault(setting.FactionId))
                    continue;

                setting.Priority = (byte)TargetPriority;
                em.SetComponentData(entity, setting);
            }
        }
    }

}
