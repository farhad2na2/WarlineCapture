using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AIEconomyValidationTests
{
    [SetUp]
    public void SetUp()
    {
        InitialUnitsRuntimeState.VerboseAILogs = true;
    }

    [TearDown]
    public void TearDown()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.VerboseAILogs = false;
    }

    [Test]
    public void SceneAIConfigAssets_MatchValidatedEconomyBudgets()
    {
        AIControllerConfig enemy = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_Enemy_Config.asset");
        AIControllerConfig playerAuto = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_PlayerAuto_Config.asset");

        Assert.IsTrue(enemy.Enabled);
        Assert.AreEqual(AIControllerRole.Enemy, enemy.Role);
        Assert.AreEqual(FactionIdentitySystem.EnemyFactionId, enemy.FactionId);
        Assert.AreEqual(75000, enemy.StartingMoney);
        Assert.AreEqual(1.15f, enemy.IncomeMultiplier, 0.001f);

        Assert.IsTrue(playerAuto.Enabled);
        Assert.AreEqual(AIControllerRole.PlayerAuto, playerAuto.Role);
        Assert.AreEqual(FactionIdentitySystem.PlayerFactionId, playerAuto.FactionId);
        Assert.IsTrue(playerAuto.AutoControlsPlayerFaction);
        Assert.AreEqual(300000, playerAuto.StartingMoney);
        Assert.AreEqual(1f, playerAuto.IncomeMultiplier, 0.001f);
    }

    [Test]
    public void AIEconomySystem_EmitsValidationLogForEnabledFactionEconomy()
    {
        using var world = new World("AIEconomyValidationTests");
        EntityManager em = world.EntityManager;
        Entity economyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
        em.SetComponentData(economyEntity, new FactionEconomy
        {
            FactionId = 1,
            Money = 75000,
            Oil = 0f,
            Fuel = 0f,
            OilIncomeRate = 0f,
            FuelIncomeRate = 0f,
            LastSellTime = 0f,
            LastLogTime = -999f
        });
        em.SetComponentData(economyEntity, new FactionEconomyPolicy
        {
            Enabled = 1,
            IncomeMultiplier = 1.15f,
            OilSellPrice = 150,
            FuelSellPrice = 220,
            SellIntervalSeconds = 8f
        });

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AIEconomySystem>();
        SystemHandle logFlushSystem = world.CreateSystem<AIDiagnosticLogFlushSystem>();

        LogAssert.Expect(
            LogType.Log,
            new Regex(@"\[AIEconomy\] faction=1 money=75000 oil=0 fuel=0 oilIncome=0\.0 fuelIncome=0\.0 soldOil=0 soldFuel=0 revenue=0"));

        system.Update(world.Unmanaged);
        logFlushSystem.Update(world.Unmanaged);
        LogAssert.NoUnexpectedReceived();

        FactionEconomy economy = em.GetComponentData<FactionEconomy>(economyEntity);
        Assert.AreEqual(1, economy.FactionId);
        Assert.AreEqual(75000, economy.Money);
    }

    private static AIControllerConfig LoadAIConfig(string path)
    {
        AIControllerConfig config = AssetDatabase.LoadAssetAtPath<AIControllerConfig>(path);
        Assert.NotNull(config, $"Missing AI config asset at {path}");
        return config;
    }
}
