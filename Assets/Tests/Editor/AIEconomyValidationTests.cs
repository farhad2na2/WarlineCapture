using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AIEconomyValidationTests
{
    public static void RunFocusedValidation()
    {
        var tests = new AIEconomyValidationTests();
        try
        {
            tests.SetUp();
            tests.SceneAIConfigAssets_MatchValidatedEconomyBudgets();
            AssertEmitsValidationLogForEnabledFactionEconomy(assertDiagnosticLog: false);
            AssertRequestsAndCompletesFactionResourceSale();
            Debug.Log("[AIEconomyFocusedValidation] result=Passed tests=3");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[AIEconomyFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
        finally
        {
            tests.TearDown();
        }
    }

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
        Assert.AreEqual(FactionIdentity.EnemyFactionId, enemy.FactionId);
        Assert.AreEqual(75000, enemy.StartingMoney);
        Assert.AreEqual(1.15f, enemy.IncomeMultiplier, 0.001f);

        Assert.IsTrue(playerAuto.Enabled);
        Assert.AreEqual(AIControllerRole.PlayerAuto, playerAuto.Role);
        Assert.AreEqual(FactionIdentity.PlayerFactionId, playerAuto.FactionId);
        Assert.IsTrue(playerAuto.AutoControlsPlayerFaction);
        Assert.AreEqual(300000, playerAuto.StartingMoney);
        Assert.AreEqual(1f, playerAuto.IncomeMultiplier, 0.001f);
    }

    [Test]
    public void AIEconomySystem_EmitsValidationLogForEnabledFactionEconomy()
    {
        AssertEmitsValidationLogForEnabledFactionEconomy(assertDiagnosticLog: true);
    }

    [Test]
    public void AIEconomySystem_RequestsAndCompletesFactionResourceSale()
    {
        AssertRequestsAndCompletesFactionResourceSale();
    }

    private static void AssertEmitsValidationLogForEnabledFactionEconomy(bool assertDiagnosticLog)
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

        if (assertDiagnosticLog)
        {
            LogAssert.Expect(
                LogType.Log,
                new Regex(@"\[AIEconomy\] faction=1 money=75000 oil=0 fuel=0 oilIncome=0\.0 fuelIncome=0\.0 soldOil=0 soldFuel=0 revenue=0"));
        }

        system.Update(world.Unmanaged);
        logFlushSystem.Update(world.Unmanaged);
        if (assertDiagnosticLog)
            LogAssert.NoUnexpectedReceived();

        FactionEconomy economy = em.GetComponentData<FactionEconomy>(economyEntity);
        Assert.AreEqual(1, economy.FactionId);
        Assert.AreEqual(75000, economy.Money);
    }

    private static void AssertRequestsAndCompletesFactionResourceSale()
    {
        using var world = new World("AIEconomyResourceSaleValidationTests");
        EntityManager em = world.EntityManager;
        Entity economyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
        em.SetComponentData(economyEntity, new FactionEconomy
        {
            FactionId = 2,
            Money = 1000,
            LastSellTime = -999f,
            LastLogTime = 999f
        });
        em.SetComponentData(economyEntity, new FactionEconomyPolicy
        {
            Enabled = 1,
            IncomeMultiplier = 1.5f,
            OilSellPrice = 100,
            FuelSellPrice = 200,
            SellIntervalSeconds = 1f
        });

        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingRuntimeFactionSummary> summaries = em.AddBuffer<BuildingRuntimeFactionSummary>(boundary);
        summaries.Add(new BuildingRuntimeFactionSummary
        {
            FactionId = 2,
            StoredOilBarrels = 3.8f,
            StoredFuelBarrels = 2.2f,
            OilBarrelsPerDay = 4f,
            FuelBarrelsPerDay = 6f
        });
        DynamicBuffer<BuildingFactionResourceSellRequest> requests = em.AddBuffer<BuildingFactionResourceSellRequest>(boundary);

        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        SystemHandle system = world.CreateSystem<AIEconomySystem>();

        system.Update(world.Unmanaged);
        requests = em.GetBuffer<BuildingFactionResourceSellRequest>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual((byte)2, requests[0].FactionId);
        Assert.AreEqual(3f, requests[0].RequestedOilBarrels, 0.001f);
        Assert.AreEqual(2f, requests[0].RequestedFuelBarrels, 0.001f);
        Assert.AreEqual(BuildingFactionResourceSellRequest.Pending, requests[0].Status);

        FactionEconomy economy = em.GetComponentData<FactionEconomy>(economyEntity);
        Assert.AreEqual(3.8f, economy.Oil, 0.001f);
        Assert.AreEqual(2.2f, economy.Fuel, 0.001f);
        Assert.AreEqual(6f, economy.OilIncomeRate, 0.001f);
        Assert.AreEqual(9f, economy.FuelIncomeRate, 0.001f);

        BuildingFactionResourceSellRequest completed = requests[0];
        completed.Status = BuildingFactionResourceSellRequest.Succeeded;
        completed.SoldOilBarrels = 3f;
        completed.SoldFuelBarrels = 2f;
        requests[0] = completed;

        system.Update(world.Unmanaged);
        requests = em.GetBuffer<BuildingFactionResourceSellRequest>(boundary);
        Assert.AreEqual(0, requests.Length);

        economy = em.GetComponentData<FactionEconomy>(economyEntity);
        Assert.AreEqual(1700, economy.Money);
    }

    private static AIControllerConfig LoadAIConfig(string path)
    {
        AIControllerConfig config = AssetDatabase.LoadAssetAtPath<AIControllerConfig>(path);
        Assert.NotNull(config, $"Missing AI config asset at {path}");
        return config;
    }
}
