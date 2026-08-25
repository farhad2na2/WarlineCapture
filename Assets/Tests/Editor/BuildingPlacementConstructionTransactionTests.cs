using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingPlacementConstructionTransactionTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new BuildingPlacementConstructionTransactionTests();
            tests.AffordablePlacement_SpendsCreditsAndMaterialsExactlyOnce();
            tests.CanonicalMaterialsShortage_DoesNotCommitOrMutateResources();
            tests.InvalidPlacementAndPreviewCancel_SpendNothing();
            tests.RegistrationFailure_RollsBackResourcesExactlyOnce();
            tests.PartialRegistration_RollsBackResourcesExactlyOnce();
            tests.DuplicateTransaction_DoesNotSpendOrCommitTwice();
            Debug.Log("[BuildingPlacementConstructionTransactionValidation] result=Passed tests=6");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[BuildingPlacementConstructionTransactionValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AffordablePlacement_SpendsCreditsAndMaterialsExactlyOnce()
    {
        using var fixture = new Fixture(credits: 500, materials: 100, creditsCost: 120, materialsCost: 30);

        Assert.IsTrue(fixture.Confirm(1, committed: true, out var failure));

        Assert.AreEqual(BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.None, failure);
        Assert.AreEqual(380, fixture.Resources.CurrentDollars);
        Assert.AreEqual(70, fixture.Resources.CurrentMaterials);
        Assert.AreEqual(1, fixture.CommitCount);
    }

    [Test]
    public void CanonicalMaterialsShortage_DoesNotCommitOrMutateResources()
    {
        using (var creditsShort = new Fixture(credits: 99, materials: 50, creditsCost: 100, materialsCost: 20))
        {
            Assert.IsFalse(creditsShort.Confirm(1, committed: true, out var failure));
            Assert.AreEqual(BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientCredits, failure);
            Assert.AreEqual(99, creditsShort.Resources.CurrentDollars);
            Assert.AreEqual(50, creditsShort.Resources.CurrentMaterials);
            Assert.AreEqual(0, creditsShort.CommitCount);
        }

        AssertShortage(credits: 150, materials: 19, expected: BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientMaterials);
        AssertShortage(credits: 99, materials: 19, expected: BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientCreditsAndMaterials);
    }

    [Test]
    public void InvalidPlacementAndPreviewCancel_SpendNothing()
    {
        using (var invalid = new Fixture(500, 100, 120, 30, placementValid: false))
        {
            Assert.IsFalse(invalid.Confirm(1, committed: true, out var failure));
            Assert.AreEqual(BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.BlockedPlacement, failure);
            Assert.AreEqual(500, invalid.Resources.CurrentDollars);
            Assert.AreEqual(100, invalid.Resources.CurrentMaterials);
            Assert.AreEqual(0, invalid.CommitCount);
        }

        using (var cancelled = new Fixture(500, 100, 120, 30))
        {
            cancelled.Cancel();
            Assert.AreEqual(500, cancelled.Resources.CurrentDollars);
            Assert.AreEqual(100, cancelled.Resources.CurrentMaterials);
            Assert.AreEqual(0, cancelled.CommitCount);
        }
    }

    [Test]
    public void RegistrationFailure_RollsBackResourcesExactlyOnce()
    {
        using var fixture = new Fixture(500, 100, 120, 30);

        Assert.IsFalse(fixture.Confirm(1, committed: false, out var failure));

        Assert.AreEqual(BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.RegistrationFailed, failure);
        Assert.AreEqual(500, fixture.Resources.CurrentDollars);
        Assert.AreEqual(100, fixture.Resources.CurrentMaterials);
        Assert.AreEqual(1, fixture.CommitCount);
    }

    [Test]
    public void PartialRegistration_RollsBackResourcesExactlyOnce()
    {
        using var fixture = new Fixture(500, 100, 120, 30);

        Assert.IsFalse(fixture.Confirm(1, committedCount: 0, expectedCount: 3, out var failure));

        Assert.AreEqual(BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.RegistrationFailed, failure);
        Assert.AreEqual(500, fixture.Resources.CurrentDollars);
        Assert.AreEqual(100, fixture.Resources.CurrentMaterials);
        Assert.AreEqual(1, fixture.CommitCount);
    }

    [Test]
    public void DuplicateTransaction_DoesNotSpendOrCommitTwice()
    {
        using var fixture = new Fixture(500, 100, 120, 30);
        Assert.IsTrue(fixture.Confirm(7, committed: true, out _));

        Assert.IsFalse(fixture.Confirm(7, committed: true, out var duplicateFailure));

        Assert.AreEqual(BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.DuplicateTransaction, duplicateFailure);
        Assert.AreEqual(380, fixture.Resources.CurrentDollars);
        Assert.AreEqual(70, fixture.Resources.CurrentMaterials);
        Assert.AreEqual(1, fixture.CommitCount);
    }

    private static void AssertShortage(
        int credits,
        int materials,
        BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason expected)
    {
        using var fixture = new Fixture(credits, materials, creditsCost: 100, materialsCost: 20);
        Assert.IsFalse(fixture.Confirm(1, committed: true, out var failure));
        Assert.AreEqual(expected, failure);
        Assert.AreEqual(credits, fixture.Resources.CurrentDollars);
        Assert.AreEqual(materials, fixture.Resources.CurrentMaterials);
        Assert.AreEqual(0, fixture.CommitCount);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly World _world;
        private readonly GameObject _prefab;
        private readonly GameObject _root;
        private readonly BuildingPlacementLifecycleCompositionSystemHelper _lifecycle;
        private readonly BuildingPlacementLifecycleCompositionSystemHelper.CancelContext _cancelContext;
        private readonly bool _placementValid;

        public readonly RuntimeFactionResourceSystemHelper Resources;
        public readonly BuildingConstructionResourceTransactionSystemHelper Transactions;
        public int CommitCount { get; private set; }

        public Fixture(
            int credits,
            int materials,
            int creditsCost,
            int materialsCost,
            bool placementValid = true)
        {
            _placementValid = placementValid;
            _world = new World(nameof(BuildingPlacementConstructionTransactionTests));
            Resources = new RuntimeFactionResourceSystemHelper();
            Resources.SetInitialDollars(credits);
            Resources.Configure(_world.EntityManager);
            Transactions = new BuildingConstructionResourceTransactionSystemHelper(Resources);
            Entity player = GetPlayerEconomyEntity(_world.EntityManager);
            _world.EntityManager.SetComponentData(player, new FactionTacticalMaterialsComponent
            {
                FactionId = FactionIdentity.PlayerFactionId,
                Current = materials,
                Capacity = Math.Max(materials, 1)
            });

            _prefab = new GameObject("ConstructionTransactionPrefab");
            _root = new GameObject("ConstructionTransactionRoot");
            _lifecycle = new BuildingPlacementLifecycleCompositionSystemHelper();
            _cancelContext = new BuildingPlacementLifecycleCompositionSystemHelper.CancelContext(
                null,
                null,
                UnityEngine.Object.DestroyImmediate);
            var runtimeState = new RuntimeGameplayStateSystem(_world.EntityManager);
            _lifecycle.Begin(
                new BuildingDefinition
                {
                    Prefab = _prefab,
                    FootprintCells = Vector2Int.one,
                    CreditsCost = creditsCost,
                    MaterialsCost = materialsCost
                },
                new BuildingPlacementLifecycleCompositionSystemHelper.BeginContext(
                    runtimeState,
                    null,
                    null,
                    _root.transform,
                    null,
                    UnityEngine.Object.DestroyImmediate,
                    _ => Vector2Int.zero,
                    null,
                    (placement, _, _) => placement.IsValid = _placementValid,
                    null,
                    null,
                    null));
        }

        public bool Confirm(
            int transactionId,
            bool committed,
            out BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason failure)
        {
            return Confirm(transactionId, committed ? 1 : 0, 1, out failure);
        }

        public bool Confirm(
            int transactionId,
            int committedCount,
            int expectedCount,
            out BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason failure)
        {
            var context = new BuildingPlacementLifecycleCompositionSystemHelper.ConfirmContext(
                placement => placement.IsValid,
                Transactions.TryReserve,
                Transactions.TryFinalize,
                Transactions.TryRollback,
                _ =>
                {
                    CommitCount++;
                    return new BuildingPlacementCommitCompositionSystemHelper.CommitOutcome(
                        null,
                        committedCount,
                        expectedCount);
                });
            return _lifecycle.Confirm(transactionId, context, out failure);
        }

        public void Cancel()
        {
            _lifecycle.Cancel(_cancelContext);
        }

        public void Dispose()
        {
            _lifecycle.Cancel(_cancelContext);
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_prefab);
            _world.Dispose();
        }

        private static Entity GetPlayerEconomyEntity(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
            return query.GetSingletonEntity();
        }
    }
}
