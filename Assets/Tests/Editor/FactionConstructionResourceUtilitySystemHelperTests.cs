using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class FactionConstructionResourceUtilitySystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new FactionConstructionResourceUtilitySystemHelperTests();
            tests.Evaluate_ReturnsTypedCombinedAffordabilityFailures();
            tests.TrySpend_AppliesCreditsAndMaterialsAtomically();
            tests.TrySpend_RejectionDoesNotMutateEitherResource();
            tests.TrySpend_ZeroMaterialsCostPreservesMaterialsVersion();
            tests.TrySpend_RejectsMismatchedFactionWithoutMutation();
            tests.TryRollback_RestoresExactConstructionSpend();
            tests.TryRollback_RejectsInvalidStateWithoutMutation();
            tests.TrySpend_DoesNotAllocateManagedMemoryAfterWarmup();
            Debug.Log("[FactionConstructionResourceFocusedValidation] result=Passed tests=8");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[FactionConstructionResourceFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Evaluate_ReturnsTypedCombinedAffordabilityFailures()
    {
        FactionEconomy economy = Economy(credits: 10);
        FactionTacticalMaterialsComponent materials = Materials(current: 5, capacity: 20, version: 3u);

        Assert.AreEqual(
            FactionConstructionResourceMutationResult.InsufficientCredits,
            FactionConstructionResourceUtilitySystemHelper.Evaluate(economy, materials, 11, 5));
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.InsufficientMaterials,
            FactionConstructionResourceUtilitySystemHelper.Evaluate(economy, materials, 10, 6));
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.InsufficientCreditsAndMaterials,
            FactionConstructionResourceUtilitySystemHelper.Evaluate(economy, materials, 11, 6));
    }

    [Test]
    public void TrySpend_AppliesCreditsAndMaterialsAtomically()
    {
        FactionEconomy economy = Economy(credits: 100);
        FactionTacticalMaterialsComponent materials = Materials(current: 40, capacity: 80, version: 7u);

        FactionConstructionResourceMutationResult result =
            FactionConstructionResourceUtilitySystemHelper.TrySpend(
                ref economy,
                ref materials,
                25,
                12);

        Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, result);
        Assert.AreEqual(75, economy.Money);
        Assert.AreEqual(28, materials.Current);
        Assert.AreEqual(12, materials.LifetimeSpent);
        Assert.AreEqual(12, materials.LifetimeConstructionSpent);
        Assert.AreEqual(8u, materials.Version);
    }

    [Test]
    public void TrySpend_RejectionDoesNotMutateEitherResource()
    {
        FactionEconomy economy = Economy(credits: 10);
        FactionTacticalMaterialsComponent materials = Materials(current: 5, capacity: 20, version: 3u);

        FactionConstructionResourceMutationResult result =
            FactionConstructionResourceUtilitySystemHelper.TrySpend(
                ref economy,
                ref materials,
                11,
                6);

        Assert.AreEqual(FactionConstructionResourceMutationResult.InsufficientCreditsAndMaterials, result);
        Assert.AreEqual(10, economy.Money);
        Assert.AreEqual(5, materials.Current);
        Assert.AreEqual(0, materials.LifetimeSpent);
        Assert.AreEqual(0, materials.LifetimeConstructionSpent);
        Assert.AreEqual(3u, materials.Version);
    }

    [Test]
    public void TrySpend_ZeroMaterialsCostPreservesMaterialsVersion()
    {
        FactionEconomy economy = Economy(credits: 10);
        FactionTacticalMaterialsComponent materials = Materials(current: 5, capacity: 20, version: 3u);

        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            FactionConstructionResourceUtilitySystemHelper.TrySpend(
                ref economy,
                ref materials,
                4,
                0));
        Assert.AreEqual(6, economy.Money);
        Assert.AreEqual(5, materials.Current);
        Assert.AreEqual(3u, materials.Version);
    }

    [Test]
    public void TrySpend_RejectsMismatchedFactionWithoutMutation()
    {
        FactionEconomy economy = Economy(credits: 10);
        FactionTacticalMaterialsComponent materials = Materials(current: 5, capacity: 20, version: 3u);
        materials.FactionId = 2;

        Assert.AreEqual(
            FactionConstructionResourceMutationResult.InvalidState,
            FactionConstructionResourceUtilitySystemHelper.TrySpend(
                ref economy,
                ref materials,
                4,
                2));
        Assert.AreEqual(10, economy.Money);
        Assert.AreEqual(5, materials.Current);
        Assert.AreEqual(3u, materials.Version);
    }

    [Test]
    public void TryRollback_RestoresExactConstructionSpend()
    {
        FactionEconomy economy = Economy(credits: 500);
        FactionTacticalMaterialsComponent materials = Materials(current: 80, capacity: 100, version: 4u);
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            FactionConstructionResourceUtilitySystemHelper.TrySpend(
                ref economy,
                ref materials,
                120,
                30));

        FactionConstructionResourceMutationResult result =
            FactionConstructionResourceUtilitySystemHelper.TryRollback(
                ref economy,
                ref materials,
                120,
                30);

        Assert.AreEqual(FactionConstructionResourceMutationResult.Applied, result);
        Assert.AreEqual(500, economy.Money);
        Assert.AreEqual(80, materials.Current);
        Assert.AreEqual(0, materials.LifetimeSpent);
        Assert.AreEqual(6u, materials.Version);
    }

    [Test]
    public void TryRollback_RejectsInvalidStateWithoutMutation()
    {
        FactionEconomy economy = Economy(credits: int.MaxValue);
        FactionTacticalMaterialsComponent materials = Materials(current: 40, capacity: 100, version: 7u);
        materials.LifetimeSpent = 10;
        FactionEconomy originalEconomy = economy;
        FactionTacticalMaterialsComponent originalMaterials = materials;

        FactionConstructionResourceMutationResult result =
            FactionConstructionResourceUtilitySystemHelper.TryRollback(
                ref economy,
                ref materials,
                1,
                20);

        Assert.AreEqual(FactionConstructionResourceMutationResult.InvalidState, result);
        Assert.AreEqual(originalEconomy.Money, economy.Money);
        Assert.AreEqual(originalMaterials.Current, materials.Current);
        Assert.AreEqual(originalMaterials.LifetimeSpent, materials.LifetimeSpent);
        Assert.AreEqual(originalMaterials.Version, materials.Version);
    }

    [Test]
    public void TrySpend_DoesNotAllocateManagedMemoryAfterWarmup()
    {
        FactionEconomy economy = Economy(credits: 1024);
        FactionTacticalMaterialsComponent materials = Materials(current: 1024, capacity: 1024, version: 1u);
        for (int i = 0; i < 32; i++)
        {
            FactionConstructionResourceUtilitySystemHelper.Evaluate(economy, materials, 1, 1);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        bool allSpendsApplied = true;
        for (int i = 0; i < 512; i++)
        {
            allSpendsApplied &= FactionConstructionResourceUtilitySystemHelper.TrySpend(
                                    ref economy,
                                    ref materials,
                                    1,
                                    1) ==
                                FactionConstructionResourceMutationResult.Applied;
        }

        Assert.IsTrue(allSpendsApplied);
        Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.AreEqual(512, economy.Money);
        Assert.AreEqual(512, materials.Current);
    }

    private static FactionEconomy Economy(int credits)
    {
        return new FactionEconomy
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Money = credits
        };
    }

    private static FactionTacticalMaterialsComponent Materials(int current, int capacity, uint version)
    {
        return new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Current = current,
            Capacity = capacity,
            Version = version
        };
    }
}
