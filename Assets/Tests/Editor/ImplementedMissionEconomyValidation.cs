using System;
using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Tactical.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public static class ImplementedMissionEconomyValidation
{
    public static void Run()
    {
        ValidateAssets();
        foreach (int wallet in new[] { 0, 1000000000 })
        {
            // The account balance deliberately never enters a tactical call.
            var account = new PlayerProfileSaveData { credits = wallet };
            using var world = new World("DefenseMaterialsTransactions");
            var resources = new RuntimeFactionResourceSystemHelper();
            resources.Configure(world.EntityManager);
            using var query = world.EntityManager.CreateEntityQuery(typeof(FactionEconomy));
            var owner = query.GetSingletonEntity();
            world.EntityManager.SetComponentData(owner, new FactionEconomy { FactionId = 1, MaterialsOnlyConstruction = 2 });
            world.EntityManager.SetComponentData(owner, new FactionTacticalMaterialsComponent { FactionId = 1, Current = 140, Capacity = 140, Version = 1 });
            var transaction = new BuildingConstructionResourceTransactionSystemHelper(resources);
            Assert.AreEqual(70, resources.VisibleConstructionMaterialsCost(22000, 50));
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, transaction.TryReserve(1, 22000, 50));
            Assert.AreEqual(70, resources.CurrentMaterials);
            Assert.AreEqual(FactionConstructionResourceMutationResult.DuplicateTransaction, transaction.TryReserve(1, 22000, 50));
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, transaction.TryRollback(1));
            Assert.AreEqual(FactionConstructionResourceMutationResult.InvalidState, transaction.TryRollback(1));
            Assert.AreEqual(140, resources.CurrentMaterials);
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, transaction.TryReserve(2, 22000, 50));
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, transaction.TryFinalize(2));
            Assert.AreEqual(FactionConstructionResourceMutationResult.InvalidState, transaction.TryRollback(2));
            Assert.AreEqual(FactionConstructionResourceMutationResult.InsufficientMaterials, transaction.TryReserve(3, 40000, 90));
            Assert.AreEqual(70, resources.CurrentMaterials);
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, transaction.TryReserve(3, 6000, 15));
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, transaction.TryFinalize(3));
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, resources.TrySpendConstructionResources(10000, 20));
            Assert.AreEqual(20, resources.CurrentMaterials);
            Assert.AreEqual(0, resources.CurrentDollars);
            Assert.AreEqual(FactionConstructionResourceMutationResult.InvalidCost, resources.TrySpendConstructionResources(22001, 50));
            Assert.AreEqual(20, resources.CurrentMaterials);
            Assert.AreEqual(wallet, account.credits);
        }
        var economy = new FactionEconomy { FactionId = 1, MaterialsOnlyConstruction = 1 };
        var materials = new FactionTacticalMaterialsComponent { FactionId = 1, Current = 120, Capacity = 120, Version = 1 };
        Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 40000, 90));
        Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 10000, 20));
        Assert.AreEqual(10, materials.Current);
        BuildingPlacementConstructionTransactionTests.RunFocusedValidation();
        if (ValidationExit.LastExitCode != 0) throw new InvalidOperationException("Placement transaction regression failed.");
        FactionConstructionResourceUtilitySystemHelperTests.RunFocusedValidation();
        if (ValidationExit.LastExitCode != 0) throw new InvalidOperationException("Faction transaction regression failed.");
        CH04M02SteelPushRulesValidation.Run();
        Debug.Log("[ImplementedMissionEconomy] result=Passed authoredPrices=4 walletIsolation=2 transaction=spend,shortage,retry,duplicate,refund,commit-failure M02=120-90-20=10");
    }

    private static void ValidateAssets()
    {
        string[] paths = { M02EstablishBaseConfigBuilder.ScenarioPath, M03RadarWarningConfigBuilder.ScenarioPath,
            CH04M01AirCorridorConfigBuilder.ScenarioPath, CH04M02SteelPushConfigBuilder.ScenarioPath };
        int[] budgets = { 120, 140, 140, 200 };
        for (int i = 0; i < paths.Length; i++)
        {
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(paths[i]);
            Assert.NotNull(scenario);
            Assert.IsTrue(scenario.TryValidate(out string error), error);
            Assert.AreEqual(0, scenario.MissionRuntime.StartingCredits);
            Assert.AreEqual(budgets[i], scenario.MissionRuntime.StartingMaterials);
        }
        int[] credits = { 40000, 22000, 6000, 10000 }, original = { 90, 50, 15, 20 }, authored = { 120, 70, 20, 30 };
        for (int i = 0; i < credits.Length; i++)
        {
            Assert.IsTrue(MissionConstructionCostPolicy.TryResolve(2, credits[i], original[i], out int money, out int cost));
            Assert.AreEqual(0, money); Assert.AreEqual(authored[i], cost);
        }
    }
}
