using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class FactionTacticalMaterialsUtilitySystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new FactionTacticalMaterialsUtilitySystemHelperTests();
            tests.Grant_UpdatesCanonicalAmountCounterAndVersion();
            tests.Grant_RejectsCapacityOverflowWithoutMutation();
            tests.Spend_UpdatesCanonicalAmountCountersAndVersion();
            tests.Spend_RecordsTypedReasonsAndConstructionRefund();
            tests.Spend_RejectsInsufficientMaterialsWithoutMutation();
            tests.RefundExport_ReversesOnlyRefundedReservation();
            tests.Mutations_RejectInvalidStateAndAmount();
            tests.Mutations_SaturateLifetimeCountersAndWrapVersion();
            tests.Mutations_DoNotAllocateManagedMemoryAfterWarmup();
            Debug.Log("[FactionTacticalMaterialsFocusedValidation] result=Passed tests=9");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[FactionTacticalMaterialsFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Grant_UpdatesCanonicalAmountCounterAndVersion()
    {
        FactionTacticalMaterialsComponent materials = CreateMaterials(current: 20, capacity: 100, version: 4u);

        FactionTacticalMaterialsMutationResult result =
            FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                ref materials,
                15,
                FactionTacticalMaterialsSourceKind.Fabrication);

        Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied, result);
        Assert.AreEqual(35, materials.Current);
        Assert.AreEqual(15, materials.LifetimeFabricated);
        Assert.AreEqual(0, materials.LifetimeImported);
        Assert.AreEqual(5u, materials.Version);
    }

    [Test]
    public void Grant_RejectsCapacityOverflowWithoutMutation()
    {
        FactionTacticalMaterialsComponent materials = CreateMaterials(current: 95, capacity: 100, version: 7u);

        FactionTacticalMaterialsMutationResult result =
            FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                ref materials,
                6,
                FactionTacticalMaterialsSourceKind.Import);

        Assert.AreEqual(FactionTacticalMaterialsMutationResult.CapacityExceeded, result);
        Assert.AreEqual(95, materials.Current);
        Assert.AreEqual(0, materials.LifetimeImported);
        Assert.AreEqual(7u, materials.Version);
    }

    [Test]
    public void Spend_UpdatesCanonicalAmountCountersAndVersion()
    {
        FactionTacticalMaterialsComponent materials = CreateMaterials(current: 70, capacity: 100, version: 2u);

        FactionTacticalMaterialsMutationResult result =
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref materials,
                12,
                FactionTacticalMaterialsSpendKind.Export);

        Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied, result);
        Assert.AreEqual(58, materials.Current);
        Assert.AreEqual(12, materials.LifetimeSpent);
        Assert.AreEqual(12, materials.LifetimeExported);
        Assert.AreEqual(3u, materials.Version);
    }

    [Test]
    public void Spend_RejectsInsufficientMaterialsWithoutMutation()
    {
        FactionTacticalMaterialsComponent materials = CreateMaterials(current: 4, capacity: 100, version: 9u);

        FactionTacticalMaterialsMutationResult result =
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref materials,
                5,
                FactionTacticalMaterialsSpendKind.Construction);

        Assert.AreEqual(FactionTacticalMaterialsMutationResult.InsufficientMaterials, result);
        Assert.AreEqual(4, materials.Current);
        Assert.AreEqual(0, materials.LifetimeSpent);
        Assert.AreEqual(9u, materials.Version);
    }

    [Test]
    public void Spend_RecordsTypedReasonsAndConstructionRefund()
    {
        FactionTacticalMaterialsComponent materials = CreateMaterials(current: 1000, capacity: 1000, version: 1u);

        Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref materials, 10, FactionTacticalMaterialsSpendKind.Construction));
        Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref materials, 20, FactionTacticalMaterialsSpendKind.Repair));
        Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref materials, 30, FactionTacticalMaterialsSpendKind.Infrastructure));
        Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref materials, 40, FactionTacticalMaterialsSpendKind.Upgrade));
        Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref materials, 50, FactionTacticalMaterialsSpendKind.Export));
        Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,
            FactionTacticalMaterialsUtilitySystemHelper.TryRefundConstruction(ref materials, 4));

        Assert.AreEqual(854, materials.Current);
        Assert.AreEqual(146, materials.LifetimeSpent);
        Assert.AreEqual(6, materials.LifetimeConstructionSpent);
        Assert.AreEqual(20, materials.LifetimeRepairSpent);
        Assert.AreEqual(30, materials.LifetimeInfrastructureSpent);
        Assert.AreEqual(40, materials.LifetimeUpgradeSpent);
        Assert.AreEqual(50, materials.LifetimeExported);
        Assert.AreEqual(7u, materials.Version);
    }

    [Test]
    public void RefundExport_ReversesOnlyRefundedReservation()
    {
        FactionTacticalMaterialsComponent materials = CreateMaterials(current: 80, capacity: 100, version: 2u);

        Assert.AreEqual(
            FactionTacticalMaterialsMutationResult.Applied,
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref materials,
                30,
                FactionTacticalMaterialsSpendKind.Export));
        Assert.AreEqual(
            FactionTacticalMaterialsMutationResult.Applied,
            FactionTacticalMaterialsUtilitySystemHelper.TryRefundExport(ref materials, 18));

        Assert.AreEqual(68, materials.Current);
        Assert.AreEqual(12, materials.LifetimeSpent);
        Assert.AreEqual(12, materials.LifetimeExported);
        Assert.AreEqual(4u, materials.Version);
    }

    [Test]
    public void Mutations_RejectInvalidStateAndAmount()
    {
        FactionTacticalMaterialsComponent invalid = CreateMaterials(current: 11, capacity: 10, version: 3u);
        Assert.AreEqual(
            FactionTacticalMaterialsMutationResult.InvalidState,
            FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                ref invalid,
                1,
                FactionTacticalMaterialsSourceKind.Reward));

        FactionTacticalMaterialsComponent valid = CreateMaterials(current: 5, capacity: 10, version: 3u);
        Assert.AreEqual(
            FactionTacticalMaterialsMutationResult.InvalidAmount,
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref valid,
                0,
                FactionTacticalMaterialsSpendKind.Repair));
        Assert.AreEqual(5, valid.Current);
        Assert.AreEqual(3u, valid.Version);
    }

    [Test]
    public void Mutations_SaturateLifetimeCountersAndWrapVersion()
    {
        FactionTacticalMaterialsComponent materials = CreateMaterials(current: 5, capacity: 20, version: uint.MaxValue);
        materials.LifetimeRewarded = int.MaxValue - 2;

        Assert.AreEqual(
            FactionTacticalMaterialsMutationResult.Applied,
            FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                ref materials,
                5,
                FactionTacticalMaterialsSourceKind.Reward));

        Assert.AreEqual(10, materials.Current);
        Assert.AreEqual(int.MaxValue, materials.LifetimeRewarded);
        Assert.AreEqual(1u, materials.Version);
    }

    [Test]
    public void Mutations_DoNotAllocateManagedMemoryAfterWarmup()
    {
        FactionTacticalMaterialsComponent materials = CreateMaterials(current: 1000, capacity: 2000, version: 1u);
        for (int i = 0; i < 32; i++)
        {
            FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                ref materials,
                1,
                FactionTacticalMaterialsSourceKind.Fabrication);
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref materials,
                1,
                FactionTacticalMaterialsSpendKind.Construction);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 512; i++)
        {
            FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                ref materials,
                1,
                FactionTacticalMaterialsSourceKind.Fabrication);
            FactionTacticalMaterialsUtilitySystemHelper.TrySpend(
                ref materials,
                1,
                FactionTacticalMaterialsSpendKind.Construction);
        }

        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0L, allocatedBytes);
        Assert.AreEqual(1000, materials.Current);
    }

    private static FactionTacticalMaterialsComponent CreateMaterials(int current, int capacity, uint version)
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
