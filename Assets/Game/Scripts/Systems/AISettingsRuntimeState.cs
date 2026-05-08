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

public static class AISettingsRuntimeState
{
    public static AIDifficultySetting Difficulty = AIDifficultySetting.Normal;
    public static AIStartingMoneySetting StartingMoney = AIStartingMoneySetting.Normal;
    public static float IncomeMultiplier = 1f;
    public static AISpeedSetting BuildSpeed = AISpeedSetting.Normal;
    public static AISpeedSetting UnitProductionSpeed = AISpeedSetting.Normal;
    public static AIAttackGroupSizeSetting AttackGroupSize = AIAttackGroupSizeSetting.Normal;
    public static AIAttackFrequencySetting AttackFrequency = AIAttackFrequencySetting.Normal;
    public static AIAggressionSetting Aggression = AIAggressionSetting.Balanced;
    public static AIExpansionSetting Expansion = AIExpansionSetting.Normal;
    public static AITargetPriority TargetPriority = AITargetPriority.Balanced;
    public static bool PlayerAutoAIEnabled;
    public static int EnemyAICount = 1;

    public static void ResetDefaults()
    {
        Difficulty = AIDifficultySetting.Normal;
        StartingMoney = AIStartingMoneySetting.Normal;
        IncomeMultiplier = 1f;
        BuildSpeed = AISpeedSetting.Normal;
        UnitProductionSpeed = AISpeedSetting.Normal;
        AttackGroupSize = AIAttackGroupSizeSetting.Normal;
        AttackFrequency = AIAttackFrequencySetting.Normal;
        Aggression = AIAggressionSetting.Balanced;
        Expansion = AIExpansionSetting.Normal;
        TargetPriority = AITargetPriority.Balanced;
        PlayerAutoAIEnabled = false;
        EnemyAICount = 1;
    }

    public static bool IsEnemyAIIndexEnabled(int enemyIndex)
    {
        return enemyIndex >= 0 && enemyIndex < Mathf.Clamp(EnemyAICount, 1, 3);
    }

    public static bool ResolveEnabled(AIControllerConfig config)
    {
        if (config == null || !config.Enabled)
            return false;

        return config.Role != AIControllerRole.PlayerAuto || PlayerAutoAIEnabled;
    }

    public static int ApplyStartingMoney(int baseMoney, AIControllerRole role)
    {
        if (role != AIControllerRole.Enemy)
            return Mathf.Max(0, baseMoney);

        return Mathf.Max(0, Mathf.RoundToInt(baseMoney * DifficultyMoneyMultiplier() * StartingMoneyMultiplier()));
    }

    public static float ApplyIncomeMultiplier(float baseMultiplier, AIControllerRole role)
    {
        float multiplier = Mathf.Max(0f, baseMultiplier);
        if (role == AIControllerRole.Enemy)
            multiplier *= DifficultyIncomeMultiplier() * IncomeMultiplier;
        return Mathf.Max(0f, multiplier);
    }

    public static float ApplyBuildInterval(float baseSeconds, AIControllerRole role)
    {
        float interval = Mathf.Max(0.1f, baseSeconds);
        if (role == AIControllerRole.Enemy)
            interval *= DifficultyTimingMultiplier() * SpeedIntervalMultiplier(BuildSpeed) * ExpansionIntervalMultiplier();
        return Mathf.Max(0.1f, interval);
    }

    public static float ApplyProductionInterval(float baseSeconds, AIControllerRole role)
    {
        float interval = Mathf.Max(0.1f, baseSeconds);
        if (role == AIControllerRole.Enemy)
            interval *= DifficultyTimingMultiplier() * SpeedIntervalMultiplier(UnitProductionSpeed);
        return Mathf.Max(0.1f, interval);
    }

    public static bool ResolveBuildEnabled(AIControllerConfig config)
    {
        if (!ResolveEnabled(config))
            return false;

        return config.Role != AIControllerRole.Enemy || Expansion != AIExpansionSetting.Off;
    }

    public static int ApplyMaxSquadUnits(int baseMaxUnits, AIControllerRole role)
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

    public static int ApplyMinSquadUnits(int baseMinUnits, int maxUnits, AIControllerRole role)
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

    public static int ApplyMaxActiveSquads(int baseMaxActiveSquads, AIControllerRole role)
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
                if (economy.FactionId == 0)
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
                if (plan.FactionId == 0)
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
                if (plan.FactionId == 0)
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
                if (plan.FactionId == 0)
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
                if (setting.FactionId == 0)
                    continue;

                setting.Priority = (byte)TargetPriority;
                em.SetComponentData(entity, setting);
            }
        }
    }

    private static float DifficultyMoneyMultiplier()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => 0.75f,
            AIDifficultySetting.Hard => 1.25f,
            AIDifficultySetting.Brutal => 1.6f,
            _ => 1f
        };
    }

    private static float DifficultyIncomeMultiplier()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => 0.8f,
            AIDifficultySetting.Hard => 1.2f,
            AIDifficultySetting.Brutal => 1.5f,
            _ => 1f
        };
    }

    private static float DifficultyTimingMultiplier()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => 1.25f,
            AIDifficultySetting.Hard => 0.85f,
            AIDifficultySetting.Brutal => 0.65f,
            _ => 1f
        };
    }

    private static float StartingMoneyMultiplier()
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

    private static float ExpansionIntervalMultiplier()
    {
        return Expansion switch
        {
            AIExpansionSetting.Slow => 1.4f,
            AIExpansionSetting.Fast => 0.75f,
            _ => 1f
        };
    }

    private static int DifficultyMaxUnitDelta()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => -1,
            AIDifficultySetting.Hard => 2,
            AIDifficultySetting.Brutal => 4,
            _ => 0
        };
    }

    private static int DifficultyMinUnitDelta()
    {
        return Difficulty switch
        {
            AIDifficultySetting.Easy => -1,
            AIDifficultySetting.Hard => 1,
            AIDifficultySetting.Brutal => 2,
            _ => 0
        };
    }

    private static int DifficultyActiveSquadDelta()
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
