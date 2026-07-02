using Game.Components;
using Game.Configs;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class AIStartupSystemValidationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new AIStartupSystemValidationTests();
            tests.SetUp();
            try
            {
                tests.Initialize_ProjectsSceneAIConfigsIntoEcsStartupData();
                Debug.Log("[AIStartupSystemFocusedValidation] result=Passed tests=1");
            }
            finally
            {
                tests.TearDown();
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("[AIStartupSystemFocusedValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [SetUp]
    public void SetUp()
    {
        AISettingsRuntimeState.ResetDefaults();
        AISettingsRuntimeState.PlayerAutoAIEnabled = true;
        AISettingsRuntimeState.EnemyAICount = 1;
    }

    [TearDown]
    public void TearDown()
    {
        AISettingsRuntimeState.ResetDefaults();
    }

    [Test]
    public void Initialize_ProjectsSceneAIConfigsIntoEcsStartupData()
    {
        AIControllerConfig enemy = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_Enemy_Config.asset");
        AIControllerConfig playerAuto = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_PlayerAuto_Config.asset");
        AIPlanEntryStartupConfig planEntryConfig = LoadPlanEntryConfig();
        using var world = new World("AIStartupSystemValidationTests");
        AISettingsSnapshot aiSettings = AISettingsSnapshot.Defaults;
        aiSettings.PlayerAutoAIEnabled = true;
        aiSettings.EnemyAICount = 1;
        AISettingsRuntimeState.ResetDefaults();

        var system = new AIStartupSystem();
        AIStartupSystem.Result result = system.Initialize(
            world.EntityManager,
            new[] { enemy, playerAuto },
            planEntryConfig,
            TryResolveFactionSpawnCell,
            aiSettings);

        EntityManager em = world.EntityManager;
        Entity enemyEconomyEntity = GetEntityForFaction<FactionEconomy>(em, FactionIdentity.EnemyFactionId, economy => economy.FactionId);
        FactionEconomy enemyEconomy = GetComponentForFaction<FactionEconomy>(em, FactionIdentity.EnemyFactionId, economy => economy.FactionId);
        FactionEconomyPolicy enemyPolicy = em.GetComponentData<FactionEconomyPolicy>(enemyEconomyEntity);
        AIBuildPlan enemyBuildPlan = GetComponentForFaction<AIBuildPlan>(em, FactionIdentity.EnemyFactionId, plan => plan.FactionId);
        AIProductionPlan enemyProductionPlan = GetComponentForFaction<AIProductionPlan>(em, FactionIdentity.EnemyFactionId, plan => plan.FactionId);
        AISquadPlan enemySquadPlan = GetComponentForFaction<AISquadPlan>(em, FactionIdentity.EnemyFactionId, plan => plan.FactionId);
        AITargetPrioritySetting enemyTargetPriority = GetComponentForFaction<AITargetPrioritySetting>(em, FactionIdentity.EnemyFactionId, setting => setting.FactionId);

        Assert.IsTrue(result.HasPlayerAutoMode);
        Assert.IsTrue(result.PlayerAutoModeEnabled);
        Assert.AreEqual(75000, enemyEconomy.Money);
        Assert.AreEqual(1, enemyPolicy.Enabled);
        Assert.AreEqual(new int2(42, 54), enemyBuildPlan.BaseCenterCell);
        Assert.AreEqual(1, enemyBuildPlan.Enabled);
        Assert.AreEqual(1, enemyProductionPlan.Enabled);
        Assert.AreEqual(1, enemySquadPlan.Enabled);
        Assert.AreEqual((byte)AITargetPriority.Balanced, enemyTargetPriority.Priority);

        using EntityQuery buildEntryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<AIBuildPlanEntry>());
        using EntityQuery productionEntryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<AIProductionPlanEntry>());
        Assert.Greater(buildEntryQuery.CalculateEntityCount(), 0);
        Assert.Greater(productionEntryQuery.CalculateEntityCount(), 0);

        DynamicBuffer<FactionControlEntry> controlEntries = GetFactionControlEntries(em);
        Assert.IsTrue(ContainsFactionControl(controlEntries, FactionIdentity.PlayerFactionId, true, true));
        Assert.IsTrue(ContainsFactionControl(controlEntries, FactionIdentity.EnemyFactionId, true, false));
    }

    private static bool TryResolveFactionSpawnCell(byte factionId, out int2 spawnCell)
    {
        if (factionId == FactionIdentity.EnemyFactionId)
        {
            spawnCell = new int2(42, 54);
            return true;
        }

        spawnCell = default;
        return false;
    }

    private static AIControllerConfig LoadAIConfig(string path)
    {
        AIControllerConfig config = AssetDatabase.LoadAssetAtPath<AIControllerConfig>(path);
        Assert.NotNull(config, $"Missing AI config asset at {path}");
        return config;
    }

    private static AIPlanEntryStartupConfig LoadPlanEntryConfig()
    {
        const string path = "Assets/Game/Configs/Scene/Game_AI_PlanEntry_Startup_Config.asset";
        AIPlanEntryStartupConfig config = AssetDatabase.LoadAssetAtPath<AIPlanEntryStartupConfig>(path);
        Assert.NotNull(config, $"Missing AI plan entry startup config asset at {path}");
        return config;
    }

    private static T GetComponentForFaction<T>(EntityManager em, byte factionId, System.Func<T, byte> factionSelector)
        where T : unmanaged, IComponentData
    {
        Entity entity = GetEntityForFaction(em, factionId, factionSelector);
        return em.GetComponentData<T>(entity);
    }

    private static Entity GetEntityForFaction<T>(EntityManager em, byte factionId, System.Func<T, byte> factionSelector)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            T component = em.GetComponentData<T>(entities[i]);
            if (factionSelector(component) == factionId)
                return entities[i];
        }

        Assert.Fail($"Missing {typeof(T).Name} for faction {factionId}.");
        return Entity.Null;
    }

    private static DynamicBuffer<FactionControlEntry> GetFactionControlEntries(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<FactionControlConfigTag>(),
            ComponentType.ReadOnly<FactionControlEntry>());
        Entity entity = query.GetSingletonEntity();
        return em.GetBuffer<FactionControlEntry>(entity);
    }

    private static bool ContainsFactionControl(
        DynamicBuffer<FactionControlEntry> entries,
        byte factionId,
        bool aiControlled,
        bool isPlayerFaction)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            FactionControlEntry entry = entries[i];
            if (entry.FactionId == factionId &&
                entry.AIControlled == (aiControlled ? (byte)1 : (byte)0) &&
                entry.IsPlayerFaction == (isPlayerFaction ? (byte)1 : (byte)0))
            {
                return true;
            }
        }

        return false;
    }
}
#endif
