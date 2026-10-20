using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingPlacementConstructionTransactionTests
{
    static void WithConstructionLedger(Action<System.Collections.Generic.List<SkirmishConstructionLedger.Observation>> body)
    {
        Assert.IsNull(SkirmishConstructionLedger.Observer);
        var rows=new System.Collections.Generic.List<SkirmishConstructionLedger.Observation>();
        Action<SkirmishConstructionLedger.Observation> sink=rows.Add;
        SkirmishConstructionLedger.Observer=sink;
        try{body(rows);}finally{if(SkirmishConstructionLedger.Observer==sink)SkirmishConstructionLedger.Observer=null;}
    }
    [Test] public void ConstructionLedgerSuccessTracksActualCommit()
    {
        WithConstructionLedger(rows=>{
            using var f=new Fixture(500,100,120,30);
            Assert.IsTrue(f.Confirm(41,true,out var failure));
            CollectionAssert.AreEqual(new[]{"PlayerReserve","PlayerPlacementRequest","PlayerPlacementOutcome","PlayerFinalize"},rows.ConvertAll(x=>x.Operation));
            Assert.AreEqual(4,rows.Count);
            foreach(var r in rows){Assert.AreEqual(41,r.TransactionId);Assert.AreSame(f.Transactions,r.TransactionOwner);}
            Assert.AreEqual(500,rows[0].Before.Economy.Money);
            Assert.AreEqual(f.Resources.CurrentMaterials,rows[3].After.Bank.Current);
            Assert.AreEqual(1,rows[2].CommittedCount);Assert.AreEqual(1,rows[2].ExpectedCount);
            Assert.IsFalse(rows[2].HasActorOwner); // Existing fixture supplies no native registration actor.
        });
    }
    [Test] public void ConstructionLedgerRollbackAndPartialKeepActualPolicy()
    {
        WithConstructionLedger(rows=>{
            using(var f=new Fixture(500,100,120,30)){
                Assert.IsFalse(f.Confirm(42,false,out var failure));
                Assert.AreEqual("PlayerRollback",rows[rows.Count-1].Operation);
                Assert.AreEqual(500,f.Resources.CurrentDollars);Assert.AreEqual(100,f.Resources.CurrentMaterials);
            }
            rows.Clear();
            using(var f=new Fixture(500,100,120,30)){
                Assert.IsFalse(f.Confirm(43,1,3,out var failure));
                Assert.AreEqual("PlayerFinalize",rows[rows.Count-1].Operation);
                Assert.AreEqual(380,f.Resources.CurrentDollars);Assert.AreEqual(70,f.Resources.CurrentMaterials);
                Assert.AreEqual(1,rows[2].CommittedCount);Assert.AreEqual(3,rows[2].ExpectedCount);
            }
        });
    }
    [Test] public void ConstructionLedgerRefusalsProduceNoCommitRows()
    {
        WithConstructionLedger(rows=>{
            CanonicalMaterialsShortage_DoesNotCommitOrMutateResources();
            InvalidPlacementAndPreviewCancel_SpendNothing();
            Assert.AreEqual(0,rows.Count);
        });
    }
    [Test] public void ConstructionLedgerObserverFailureKeepsDecision()
    {
        Assert.IsNull(SkirmishConstructionLedger.Observer);
        long faults=SkirmishConstructionLedger.Faults;
        Action<SkirmishConstructionLedger.Observation> sink=_=>throw new InvalidOperationException("injected observer failure");
        SkirmishConstructionLedger.Observer=sink;
        try{AffordablePlacement_SpendsCreditsAndMaterialsExactlyOnce();Assert.Greater(SkirmishConstructionLedger.Faults,faults);}
        finally{if(SkirmishConstructionLedger.Observer==sink)SkirmishConstructionLedger.Observer=null;}
        AffordablePlacement_SpendsCreditsAndMaterialsExactlyOnce();
    }
    [Test] public void ConstructionLedgerMaterialsOnlyUsesEffectiveCost()
    {
        WithConstructionLedger(rows=>{
            using var f=new Fixture(500,100,120,30);
            var point=f.Resources.CaptureConstructionLedger(f.Transactions);
            var economy=point.Manager.GetComponentData<FactionEconomy>(point.BankEntity);economy.MaterialsOnlyConstruction=1;point.Manager.SetComponentData(point.BankEntity,economy);
            Assert.IsTrue(f.Confirm(51,true,out var failure));Assert.AreEqual(500,f.Resources.CurrentDollars);Assert.AreEqual(70,f.Resources.CurrentMaterials);
            Assert.AreEqual(120,rows[0].CreditsQuoted);Assert.AreEqual(500,rows[0].Before.Economy.Money);Assert.AreEqual(500,rows[0].After.Economy.Money);
            Assert.AreEqual(30,rows[0].After.Bank.LifetimeConstructionSpent-rows[0].Before.Bank.LifetimeConstructionSpent);
        });
    }
    [Test] public void ConstructionLedgerDeferredRollbackRecoversExactDebt()
    {
        WithConstructionLedger(rows=>{
            using var f=new Fixture(500,100,120,30);
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,f.Transactions.TryReserve(52,120,30));
            var point=rows[0].After;var bank=point.Manager.GetComponentData<FactionTacticalMaterialsComponent>(point.BankEntity);
            Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,FactionTacticalMaterialsUtilitySystemHelper.TryGrant(ref bank,bank.Capacity-bank.Current,FactionTacticalMaterialsSourceKind.Fabrication));
            point.Manager.SetComponentData(point.BankEntity,bank);
            Assert.AreNotEqual(FactionConstructionResourceMutationResult.Applied,f.Transactions.TryRollback(52));
            Assert.AreEqual("PlayerRollbackDeferred",rows[1].Operation);Assert.AreEqual(380,f.Resources.CurrentDollars);Assert.AreEqual(100,f.Resources.CurrentMaterials);
            // Genuine unrelated committed spend creates refund headroom; it is not attributed to this transaction.
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,f.Resources.TrySpendConstructionResources(0,30));
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,f.Transactions.TryRollback(52));
            Assert.AreEqual("PlayerRollback",rows[2].Operation);Assert.AreEqual(500,f.Resources.CurrentDollars);Assert.AreEqual(100,f.Resources.CurrentMaterials);
            Assert.AreNotEqual(FactionConstructionResourceMutationResult.Applied,f.Transactions.TryRollback(52));Assert.AreEqual(3,rows.Count);
        });
    }
    [Test] public void ConstructionLedgerWrappedDelegateCannotGainAttribution()
    {
        WithConstructionLedger(rows=>{
            long faults=SkirmishConstructionLedger.Faults;using var f=new Fixture(500,100,120,30);f.WrapReservationForLedgerTest=true;
            Assert.IsTrue(f.Confirm(53,true,out var failure));Assert.AreEqual(380,f.Resources.CurrentDollars);Assert.AreEqual(70,f.Resources.CurrentMaterials);
            Assert.Greater(SkirmishConstructionLedger.Faults,faults);
            CollectionAssert.AreEqual(new[]{"PlayerReserve","PlayerFinalize"},rows.ConvertAll(x=>x.Operation));
        });
    }
    [Test] public void ConstructionLedgerOwnedClosePreservesForeignSink()
    {
        Assert.IsNull(SkirmishConstructionLedger.Observer);
        Action<SkirmishConstructionLedger.Observation> owned=_=>{},foreign=_=>{};
        try{
            SkirmishConstructionLedger.OpenOwned(owned);Assert.Throws<InvalidOperationException>(()=>SkirmishConstructionLedger.OpenOwned(foreign));Assert.AreSame(owned,SkirmishConstructionLedger.Observer);
            Assert.IsTrue(SkirmishConstructionLedger.CloseOwned(owned));Assert.IsNull(SkirmishConstructionLedger.Observer);
            SkirmishConstructionLedger.OpenOwned(owned);SkirmishConstructionLedger.Observer=foreign;
            Assert.IsFalse(SkirmishConstructionLedger.CloseOwned(owned));Assert.AreSame(foreign,SkirmishConstructionLedger.Observer);
        }
        finally{SkirmishConstructionLedger.CloseOwned(owned);SkirmishConstructionLedger.CloseOwned(foreign);}
    }
    public static void RunConstructionLedgerPlayerControls()
    {
        var t=new BuildingPlacementConstructionTransactionTests();
        t.ConstructionLedgerSuccessTracksActualCommit();t.ConstructionLedgerRollbackAndPartialKeepActualPolicy();
        t.ConstructionLedgerRefusalsProduceNoCommitRows();t.ConstructionLedgerObserverFailureKeepsDecision();
        t.ConstructionLedgerMaterialsOnlyUsesEffectiveCost();t.ConstructionLedgerDeferredRollbackRecoversExactDebt();
        t.ConstructionLedgerWrappedDelegateCannotGainAttribution();t.ConstructionLedgerOwnedClosePreservesForeignSink();
        Debug.Log("[ConstructionLedgerPlayerControls] result=Passed controls=8 nativePlacementMeasured=0 paidResourcesQualified=0");
    }

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
            tests.SnappedPreview_CommitsVisibleOriginAndRotationInsteadOfPointerSnapshot();
            Debug.Log("[BuildingPlacementConstructionTransactionValidation] result=Passed tests=7");
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

    [Test]
    public void SnappedPreview_CommitsVisibleOriginAndRotationInsteadOfPointerSnapshot()
    {
        using var fixture = new Fixture(500, 100, 120, 30);
        // The initial or last pointer cell can be on the median; road snapping moves
        // the visible, validated preview to the asphalt and rotates it across the lane.
        fixture.Placement.CommittedOriginCell = new Vector2Int(882, 427);
        fixture.Placement.OriginCell = new Vector2Int(882, 419);
        fixture.Placement.AutoRotateVertical = true;
        Assert.IsTrue(fixture.Confirm(1, committed: true, out _));
        Assert.AreEqual(new Vector2Int(882, 419), fixture.CommittedOrigin);
        Assert.IsTrue(fixture.CommittedRotation);
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
        public bool WrapReservationForLedgerTest;
        public int CommitCount { get; private set; }
        public BuildingPlacementLifecycleCompositionSystemHelper.PlacementState Placement => _lifecycle.ActivePlacement;
        public Vector2Int CommittedOrigin { get; private set; }
        public bool CommittedRotation { get; private set; }

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
            BuildingPlacementLifecycleCompositionSystemHelper.TryReserveCostDelegate reserve=Transactions.TryReserve;
            if(WrapReservationForLedgerTest)reserve=(id,credits,materials)=>Transactions.TryReserve(id,credits,materials);
            var context = new BuildingPlacementLifecycleCompositionSystemHelper.ConfirmContext(
                placement => placement.IsValid,
                reserve,
                Transactions.TryFinalize,
                Transactions.TryRollback,
                placement =>
                {
                    CommittedOrigin = placement.OriginCell;
                    CommittedRotation = placement.AutoRotateVertical;
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
