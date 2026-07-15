using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingConstructionResourceTransactionSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new BuildingConstructionResourceTransactionSystemHelperTests();
            tests.Reservation_FinalizesOrRollsBackExactlyOnce();
            tests.Reset_ClearsSettledTransactionHighWaterMark();
            tests.WarmedReservations_DoNotAllocateManagedMemory();
            Debug.Log("[BuildingConstructionResourceTransactionValidation] result=Passed tests=3");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[BuildingConstructionResourceTransactionValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Reservation_FinalizesOrRollsBackExactlyOnce()
    {
        using var fixture = new Fixture(500, 80);

        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            fixture.Transactions.TryReserve(1, 120, 30));
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.DuplicateTransaction,
            fixture.Transactions.TryReserve(1, 120, 30));
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            fixture.Transactions.TryFinalize(1));
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.InvalidState,
            fixture.Transactions.TryFinalize(1));
        Assert.AreEqual(380, fixture.Resources.CurrentDollars);
        Assert.AreEqual(50, fixture.Resources.CurrentMaterials);

        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            fixture.Transactions.TryReserve(2, 80, 20));
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            fixture.Transactions.TryRollback(2));
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.InvalidState,
            fixture.Transactions.TryRollback(2));
        Assert.AreEqual(380, fixture.Resources.CurrentDollars);
        Assert.AreEqual(50, fixture.Resources.CurrentMaterials);
    }

    [Test]
    public void Reset_ClearsSettledTransactionHighWaterMark()
    {
        using var fixture = new Fixture(500, 80);
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            fixture.Transactions.TryReserve(1, 120, 30));
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            fixture.Transactions.TryFinalize(1));

        fixture.Transactions.Reset();

        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            fixture.Transactions.TryReserve(1, 120, 30));
    }

    [Test]
    public void WarmedReservations_DoNotAllocateManagedMemory()
    {
        using var fixture = new Fixture(1000, 100);

        bool allMutationsApplied = true;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int transactionId = 1; transactionId <= 512; transactionId++)
        {
            allMutationsApplied &= fixture.Transactions.TryReserve(transactionId, 1, 1) ==
                                   FactionConstructionResourceMutationResult.Applied;
            allMutationsApplied &= fixture.Transactions.TryRollback(transactionId) ==
                                   FactionConstructionResourceMutationResult.Applied;
        }

        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsTrue(allMutationsApplied);
        Assert.AreEqual(0L, allocatedBytes);
        Assert.AreEqual(1000, fixture.Resources.CurrentDollars);
        Assert.AreEqual(100, fixture.Resources.CurrentMaterials);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly World _world;

        public readonly RuntimeFactionResourceSystemHelper Resources;
        public readonly BuildingConstructionResourceTransactionSystemHelper Transactions;

        public Fixture(int credits, int materials)
        {
            _world = new World(nameof(BuildingConstructionResourceTransactionSystemHelperTests));
            Resources = new RuntimeFactionResourceSystemHelper();
            Resources.SetInitialDollars(credits);
            Resources.Configure(_world.EntityManager);
            using EntityQuery query = _world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FactionEconomy>());
            Entity factionResourceEntity = query.GetSingletonEntity();
            _world.EntityManager.SetComponentData(factionResourceEntity, new FactionTacticalMaterialsComponent
            {
                FactionId = FactionIdentity.PlayerFactionId,
                Current = materials,
                Capacity = Math.Max(materials, 1)
            });
            Transactions = new BuildingConstructionResourceTransactionSystemHelper(Resources);
        }

        public void Dispose()
        {
            _world.Dispose();
        }
    }
}
