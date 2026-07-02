using Unity.Entities;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
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
                    if (!FactionIdentity.IsAiControlledByDefault(economy.FactionId))
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
                    if (!FactionIdentity.IsAiControlledByDefault(plan.FactionId))
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
                    if (!FactionIdentity.IsAiControlledByDefault(plan.FactionId))
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
                    if (!FactionIdentity.IsAiControlledByDefault(plan.FactionId))
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
                    if (!FactionIdentity.IsAiControlledByDefault(setting.FactionId))
                        continue;

                    setting.Priority = (byte)TargetPriority;
                    em.SetComponentData(entity, setting);
                }
            }
        }

    }
}
