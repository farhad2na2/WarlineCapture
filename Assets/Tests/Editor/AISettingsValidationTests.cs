using NUnit.Framework;
using Unity.Entities;

public sealed class AISettingsValidationTests
{
    [TearDown]
    public void TearDown()
    {
        AISettingsRuntimeState.ResetDefaults();
    }

    [Test]
    public void AISettingsRuntimeState_AppliesDifficultyEconomyAndCadenceMultipliers()
    {
        AISettingsRuntimeState.Difficulty = AIDifficultySetting.Brutal;
        AISettingsRuntimeState.StartingMoney = AIStartingMoneySetting.High;
        AISettingsRuntimeState.IncomeMultiplier = 2f;
        AISettingsRuntimeState.BuildSpeed = AISpeedSetting.Fast;
        AISettingsRuntimeState.UnitProductionSpeed = AISpeedSetting.Fast;
        AISettingsRuntimeState.Expansion = AIExpansionSetting.Fast;

        Assert.AreEqual(120000, AISettingsRuntimeState.ApplyStartingMoney(50000, AIControllerRole.Enemy));
        Assert.AreEqual(3f, AISettingsRuntimeState.ApplyIncomeMultiplier(1f, AIControllerRole.Enemy), 0.001f);
        Assert.Less(AISettingsRuntimeState.ApplyBuildInterval(8f, AIControllerRole.Enemy), 4f);
        Assert.Less(AISettingsRuntimeState.ApplyProductionInterval(6f, AIControllerRole.Enemy), 3f);
    }

    [Test]
    public void AISettingsRuntimeState_ApplyToWorldUpdatesExistingEnemyPlans()
    {
        World world = new("AI Settings Validation");
        try
        {
            EntityManager em = world.EntityManager;
            Entity economy = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
            em.SetComponentData(economy, new FactionEconomy { FactionId = FactionIdentity.EnemyFactionId });
            em.SetComponentData(economy, new FactionEconomyPolicy { Enabled = 1, IncomeMultiplier = 1f });
            Entity buildPlan = em.CreateEntity(typeof(AIBuildPlan));
            em.SetComponentData(buildPlan, new AIBuildPlan { FactionId = FactionIdentity.EnemyFactionId, Enabled = 1, BuildIntervalSeconds = 8f });
            Entity productionPlan = em.CreateEntity(typeof(AIProductionPlan));
            em.SetComponentData(productionPlan, new AIProductionPlan { FactionId = FactionIdentity.EnemyFactionId, Enabled = 1, UnitProductionIntervalSeconds = 6f });
            Entity squadPlan = em.CreateEntity(typeof(AISquadPlan));
            em.SetComponentData(squadPlan, new AISquadPlan { FactionId = FactionIdentity.EnemyFactionId, Enabled = 1, MinUnits = 3, MaxUnits = 8, MaxActiveSquads = 2 });
            Entity targetPriority = em.CreateEntity(typeof(AITargetPrioritySetting));
            em.SetComponentData(targetPriority, new AITargetPrioritySetting { FactionId = FactionIdentity.EnemyFactionId, Priority = (byte)AITargetPriority.Balanced });

            AISettingsRuntimeState.Difficulty = AIDifficultySetting.Hard;
            AISettingsRuntimeState.IncomeMultiplier = 1.5f;
            AISettingsRuntimeState.BuildSpeed = AISpeedSetting.Fast;
            AISettingsRuntimeState.UnitProductionSpeed = AISpeedSetting.Fast;
            AISettingsRuntimeState.AttackGroupSize = AIAttackGroupSizeSetting.Large;
            AISettingsRuntimeState.AttackFrequency = AIAttackFrequencySetting.Frequent;
            AISettingsRuntimeState.Aggression = AIAggressionSetting.Aggressive;
            AISettingsRuntimeState.TargetPriority = AITargetPriority.Production;

            AISettingsRuntimeState.ApplyToWorld(world);

            Assert.Greater(em.GetComponentData<FactionEconomyPolicy>(economy).IncomeMultiplier, 1f);
            Assert.Less(em.GetComponentData<AIBuildPlan>(buildPlan).BuildIntervalSeconds, 8f);
            Assert.Less(em.GetComponentData<AIProductionPlan>(productionPlan).UnitProductionIntervalSeconds, 6f);
            Assert.Greater(em.GetComponentData<AISquadPlan>(squadPlan).MaxUnits, 8);
            Assert.Greater(em.GetComponentData<AISquadPlan>(squadPlan).MaxActiveSquads, 2);
            Assert.AreEqual((byte)AITargetPriority.Production, em.GetComponentData<AITargetPrioritySetting>(targetPriority).Priority);
        }
        finally
        {
            world.Dispose();
        }
    }
}
