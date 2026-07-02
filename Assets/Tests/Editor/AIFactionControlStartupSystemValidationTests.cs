using Game.Components;
using Game.Configs;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;

public sealed class AIFactionControlStartupSystemValidationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.Initialize_ProjectsAiControlConfigsIntoFactionControlEntries());
            RunCase(test => test.Initialize_AddsDefaultPlayerAndEnemyEntriesWhenConfigListIsEmpty());
            RunCase(test => test.Initialize_ReusesExistingConfigEntityAndAddsMissingBuffer());
            UnityEngine.Debug.Log("[AIFactionControlStartupValidation] result=Passed tests=3");
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogError("[AIFactionControlStartupValidation] result=Failed");
            UnityEngine.Debug.LogException(exception);
            throw;
        }
    }

    private static void RunCase(System.Action<AIFactionControlStartupSystemValidationTests> testCase)
    {
        var tests = new AIFactionControlStartupSystemValidationTests();
        tests.SetUp();
        try
        {
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
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
    public void Initialize_ProjectsAiControlConfigsIntoFactionControlEntries()
    {
        AIControllerConfig enemy = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_Enemy_Config.asset");
        AIControllerConfig playerAuto = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_PlayerAuto_Config.asset");
        using var world = new World("AIFactionControlStartupSystemValidationTests");

        AIFactionControlStartupSystem system = new();
        AIFactionControlStartupSystem.Result result = system.Initialize(
            world.EntityManager,
            new[] { ToStartupEntry(enemy), ToStartupEntry(playerAuto) },
            AISettingsRuntimeState.CurrentSnapshot);

        DynamicBuffer<FactionControlEntry> entries = GetFactionControlEntries(world.EntityManager);
        Assert.IsTrue(result.HasPlayerAutoMode);
        Assert.IsTrue(result.PlayerAutoModeEnabled);
        Assert.IsTrue(AISettingsRuntimeState.PlayerAutoAIEnabled);
        Assert.IsTrue(ContainsFactionControl(entries, FactionIdentity.PlayerFactionId, true, true));
        Assert.IsTrue(ContainsFactionControl(entries, FactionIdentity.EnemyFactionId, true, false));
    }

    [Test]
    public void Initialize_AddsDefaultPlayerAndEnemyEntriesWhenConfigListIsEmpty()
    {
        using var world = new World("AIFactionControlStartupSystemEmptyConfigTests");

        AIFactionControlStartupSystem system = new();
        AIFactionControlStartupSystem.Result result = system.Initialize(
            world.EntityManager,
            System.Array.Empty<AIFactionControlStartupEntry>(),
            AISettingsRuntimeState.CurrentSnapshot);

        DynamicBuffer<FactionControlEntry> entries = GetFactionControlEntries(world.EntityManager);
        Assert.IsTrue(result.HasPlayerAutoMode);
        Assert.IsFalse(result.PlayerAutoModeEnabled);
        Assert.AreEqual(2, entries.Length);
        Assert.IsTrue(ContainsFactionControl(entries, FactionIdentity.PlayerFactionId, false, true));
        Assert.IsTrue(ContainsFactionControl(entries, FactionIdentity.EnemyFactionId, true, false));
    }

    [Test]
    public void Initialize_ReusesExistingConfigEntityAndAddsMissingBuffer()
    {
        AIControllerConfig enemy = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_Enemy_Config.asset");
        using var world = new World("AIFactionControlStartupSystemExistingEntityTests");
        EntityManager em = world.EntityManager;
        Entity configEntity = em.CreateEntity(typeof(FactionControlConfigTag));

        AIFactionControlStartupSystem system = new();
        system.Initialize(em, new[] { ToStartupEntry(enemy) }, AISettingsRuntimeState.CurrentSnapshot);

        Assert.IsTrue(em.HasBuffer<FactionControlEntry>(configEntity));
        DynamicBuffer<FactionControlEntry> entries = em.GetBuffer<FactionControlEntry>(configEntity);
        Assert.AreEqual(2, entries.Length);
        Assert.IsTrue(ContainsFactionControl(entries, FactionIdentity.EnemyFactionId, true, false));
        Assert.IsTrue(ContainsFactionControl(entries, FactionIdentity.PlayerFactionId, false, true));
    }

    private static AIControllerConfig LoadAIConfig(string path)
    {
        AIControllerConfig config = AssetDatabase.LoadAssetAtPath<AIControllerConfig>(path);
        Assert.NotNull(config, $"Missing AI config asset at {path}");
        return config;
    }

    private static AIFactionControlStartupEntry ToStartupEntry(AIControllerConfig config)
    {
        return new AIFactionControlStartupEntry(
            config.Enabled,
            config.Role,
            (byte)UnityEngine.Mathf.Clamp(config.FactionId, 0, byte.MaxValue));
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
                entry.IsPlayerFaction == (isPlayerFaction ? (byte)1 : (byte)0) &&
                entry.LastLogTime == -999f)
            {
                return true;
            }
        }

        return false;
    }
}
#endif
