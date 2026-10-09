using System;
using System.Collections.Generic;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeProductionCheckpointTests
    {
        [Test]
        public void PaidQueueRestoresRemainingUnitsReceiptsAndRelativeTiming()
        {
            using var source = new World("production-source");
            using var destination = new World("production-destination");
            var owner = source.EntityManager.CreateEntity();
            destination.EntityManager.CreateEntity();
            var newOwner = destination.EntityManager.CreateEntity();
            var prefab = new GameObject("checkpoint-test-unit");
            try
            {
                var building = new RuntimeBuildingEntity
                {
                    PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>
                    {
                        new()
                        {
                            Prefab = prefab, RemainingQuantity = 3, PaidQuantity = 4, RefundableMaterials = 75,
                            ReceiptOwner = owner, ReceiptId = 12, ReceiptAttempt = new FixedString64Bytes("attempt-5"),
                            ReservedProductionSlotIndex = -1, StartedAt = 90, ReadyAt = 110
                        }
                    }
                };
                var saved = SkirmishNativeProductionCheckpoint.Capture(building, 100, _ => "unit", _ => "session");
                saved = JsonUtility.FromJson<SkirmishNativeProductionCheckpoint.Snapshot>(JsonUtility.ToJson(saved));
                Assert.IsTrue(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 20,
                    key => key == "unit" ? prefab : null, key => key == "session" ? newOwner : Entity.Null, out var prepared), JsonUtility.ToJson(saved));
                Assert.AreEqual(10, prepared.Queue[0].StartedAt);
                Assert.AreEqual(30, prepared.Queue[0].ReadyAt);
                Assert.AreEqual(newOwner, prepared.Queue[0].ReceiptOwner);
                Assert.AreEqual(3, prepared.Queue[0].RemainingQuantity);
                Assert.AreEqual(4, prepared.Queue[0].PaidQuantity);
                Assert.AreEqual(75, prepared.Queue[0].RefundableMaterials);
                Assert.AreEqual(12, prepared.Queue[0].ReceiptId);
                var freshBuilding = new RuntimeBuildingEntity();
                var presentation = new BuildingProductionTransportPresentationSystemHelper();
                Assert.IsTrue(presentation.TryCommitCheckpointProduction(default, freshBuilding, prepared, 20));
                Assert.AreEqual(3, freshBuilding.PendingProductions[0].RemainingQuantity);
                Assert.IsFalse(presentation.TryCommitCheckpointProduction(default, freshBuilding, prepared, 20),
                    "A second restore must not overwrite or duplicate a paid queue.");
            }
            finally { UnityEngine.Object.DestroyImmediate(prefab); }
        }

        [Test]
        public void ActiveDropRemainsAttachedToItsPaidQueueAndRebasesDeliveryClocks()
        {
            using var world = new World(nameof(ActiveDropRemainsAttachedToItsPaidQueueAndRebasesDeliveryClocks));
            var owner = world.EntityManager.CreateEntity();
            var prefab = new GameObject("checkpoint-test-aircraft");
            var visual = new GameObject("checkpoint-test-delivery");
            try
            {
                var pending = new RuntimeBuildingEntity.PendingProduction
                {
                    Prefab = prefab, TransportPrefab = prefab, RemainingQuantity = 1, PaidQuantity = 2,
                    RefundableMaterials = 200, ReceiptId = 21, ReceiptOwner = owner,
                    ReceiptAttempt = new FixedString64Bytes("paid-air"), ReservedProductionSlotIndex = -1,
                    StartedAt = 80, ReadyAt = 95
                };
                var building = new RuntimeBuildingEntity
                {
                    PendingProductions = new List<RuntimeBuildingEntity.PendingProduction> { pending },
                    ActiveTransport = new RuntimeBuildingEntity.ActiveProductionTransport
                    {
                        Prefab = prefab, Instance = visual, Transform = visual.transform,
                        Phase = 1, PhaseStartedAt = 98, HoverEnteredAt = 99, NextDropReadyAt = 102,
                        NextClearDropSearchAt = 103, DeliveredUnitCount = 1,
                        ActiveDrop = new RuntimeBuildingEntity.PendingDropVisual
                        {
                            Production = pending, Prefab = prefab, StartedAt = 99, Duration = 2,
                            StartPosition = new Vector3(3, 20, 5), EndPosition = new Vector3(3, 0, 5)
                        }
                    }
                };
                var saved = SkirmishNativeProductionCheckpoint.Capture(building, 100, _ => "aircraft", _ => "owner");
                saved = JsonUtility.FromJson<SkirmishNativeProductionCheckpoint.Snapshot>(JsonUtility.ToJson(saved));
                Assert.IsTrue(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 10, _ => prefab, _ => owner, out var prepared));
                Assert.AreSame(prepared.Queue[0], prepared.Delivery.ActiveDrop.Production);
                Assert.AreEqual(9, prepared.Delivery.ActiveDrop.StartedAt);
                Assert.AreEqual(8, prepared.Delivery.PhaseStartedAt);
                Assert.AreEqual(12, prepared.Delivery.NextDropReadyAt);
                Assert.AreEqual(1, prepared.Delivery.DeliveredUnitCount);
                Assert.AreEqual(1, prepared.Queue[0].RemainingQuantity);
                // An invalid link cannot create a replacement queue or visual.
                saved.Delivery.ActiveDrop.QueueIndex = 2;
                Assert.IsFalse(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 10, _ => prefab, _ => owner, out _));
                Assert.AreEqual(1, building.PendingProductions.Count);
                Assert.AreSame(visual, building.ActiveTransport.Instance);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(visual);
            }
        }

        [Test]
        public void MissingPrefabOwnerAndNonfiniteClockFailBeforeCommit()
        {
            var prefab = new GameObject("checkpoint-test-rejection");
            try
            {
                var saved = new SkirmishNativeProductionCheckpoint.Snapshot
                {
                    Queue = new[]
                    {
                        new SkirmishNativeProductionCheckpoint.QueueItem
                        {
                            PrefabKey = "unit", TransportPrefabKey = "", ReceiptOwner = "owner", ReceiptAttempt = "attempt",
                            RemainingQuantity = 1, PaidQuantity = 1, ReceiptId = 1,
                            ReservedProductionSlotIndex = -1, StartedAt = -1, ReadyAt = 1
                        }
                    }
                };
                Assert.IsFalse(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 10, _ => null, _ => Entity.Null, out _));
                Assert.IsFalse(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 10, _ => prefab, _ => Entity.Null, out _));
                saved.Queue[0].ReadyAt = float.NaN;
                Assert.IsFalse(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 10, _ => prefab, _ => Entity.Null, out _));
            }
            finally { UnityEngine.Object.DestroyImmediate(prefab); }
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeProductionCheckpointTests();
            suite.PaidQueueRestoresRemainingUnitsReceiptsAndRelativeTiming();
            suite.ActiveDropRemainsAttachedToItsPaidQueueAndRebasesDeliveryClocks();
            suite.MissingPrefabOwnerAndNonfiniteClockFailBeforeCommit();
            suite.DeliveryVisualRestoresThroughTheNativePoolWithoutSpawningPaidUnits();
            suite.LoadingPublicationBindsOwnersAddedAfterProductionContextWasCached();
            suite.SessionReceiptQueuePreservesPaymentAuthorityWithoutALocalRefund();
            Debug.Log("[SkirmishNativeProductionCheckpointTests] result=Passed tests=6 nativeRecoveryComplete=0");
        }

        [Test]
        public void SessionReceiptQueuePreservesPaymentAuthorityWithoutALocalRefund()
        {
            using var world = new World(nameof(SessionReceiptQueuePreservesPaymentAuthorityWithoutALocalRefund));
            var session = world.EntityManager.CreateEntity();
            var prefab = new GameObject("checkpoint-session-receipt-unit");
            var presentation = new BuildingProductionTransportPresentationSystemHelper();
            try
            {
                var source = new RuntimeBuildingEntity
                {
                    PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>
                    {
                        new() { Prefab = prefab, ReceiptOwner = session, ReceiptId = 7,
                            ReceiptAttempt = new FixedString64Bytes("native-paid-attempt"),
                            RemainingQuantity = 1, PaidQuantity = 0, RefundableMaterials = 0,
                            ReservedProductionSlotIndex = -1, StartedAt = 8, ReadyAt = 18 }
                    }
                };
                var saved = SkirmishNativeProductionCheckpoint.Capture(source, 10, _ => "aa", _ => "$session");
                saved = JsonUtility.FromJson<SkirmishNativeProductionCheckpoint.Snapshot>(JsonUtility.ToJson(saved));
                Assert.IsTrue(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 30,
                    key => key == "aa" ? prefab : null, key => key == "$session" ? session : Entity.Null, out var prepared));
                var restored = new RuntimeBuildingEntity();
                Assert.IsTrue(presentation.TryCommitCheckpointProduction(default, restored, prepared, 30));
                var queue = restored.PendingProductions[0];
                Assert.AreEqual(session, queue.ReceiptOwner);
                Assert.AreEqual(7, queue.ReceiptId);
                Assert.AreEqual("native-paid-attempt", queue.ReceiptAttempt.ToString());
                Assert.AreEqual(1, queue.RemainingQuantity);
                Assert.AreEqual(0, queue.PaidQuantity);
                Assert.AreEqual(0, queue.RefundableMaterials, "Session receipts own the refund; recovery cannot add a second local refund.");
                Assert.AreEqual(38, queue.ReadyAt);
                Assert.IsFalse(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 30, _ => prefab, _ => Entity.Null, out _),
                    "Even a queue with zero local refund must resolve its authoritative receipt owner.");
                saved.Queue[0].ReceiptOwner = "";
                Assert.IsFalse(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 30, _ => prefab, _ => session, out _));
                saved.Queue[0].ReceiptOwner = "$session";
                saved.Queue[0].ReceiptAttempt = "";
                Assert.IsFalse(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 30, _ => prefab, _ => session, out _));
            }
            finally { presentation.Dispose(); UnityEngine.Object.DestroyImmediate(prefab); }
        }

        [Test]
        public void LoadingPublicationBindsOwnersAddedAfterProductionContextWasCached()
        {
            using var world = new World(nameof(LoadingPublicationBindsOwnersAddedAfterProductionContextWasCached));
            var em = world.EntityManager;
            var buildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>();
            var presentation = new BuildingProductionTransportPresentationSystemHelper();
            try
            {
                var production = new BuildingProductionUpdateCompositionSystemHelper.Context(buildings, null, presentation, default);
                var context = new BuildingProductionRuntimeTickCompositionSystemHelper.Context(buildings, null, null, null,
                    production, null, default, null, null, null, null, null, null, 1,
                    tryGetEntityManager: (out EntityManager manager) => { manager = em; return true; });
                var tick = new BuildingProductionRuntimeTickCompositionSystemHelper();
                tick.BindCheckpointOwners(context);
                Entity entity = em.CreateEntity();
                var building = new RuntimeBuildingEntity { Id = 7, CombatEntity = entity };
                var owner = new SkirmishNativeBuildingCheckpointRecord { Building = building };
                em.AddComponentObject(entity, owner);
                buildings.Add(7, building);
                Assert.IsNull(owner.TryRestoreProduction);
                tick.BindCheckpointOwners(context);
                Assert.IsNotNull(owner.TryRestoreProduction);
                Assert.IsNotNull(owner.RollbackProduction);
                Assert.AreEqual(0, building.PendingProductions?.Count ?? 0, "Binding cannot advance paid production.");
                var callback = owner.TryRestoreProduction;
                tick.BindCheckpointOwners(context);
                Assert.AreSame(callback, owner.TryRestoreProduction, "Repeated publication keeps the native owner callback.");
            }
            finally { presentation.Dispose(); }
        }

        [Test]
        public void DeliveryVisualRestoresThroughTheNativePoolWithoutSpawningPaidUnits()
        {
            var prefab = new GameObject("checkpoint-native-pool-prefab");
            var presentation = new BuildingProductionTransportPresentationSystemHelper();
            using var world = new World(nameof(DeliveryVisualRestoresThroughTheNativePoolWithoutSpawningPaidUnits));
            var owner = world.EntityManager.CreateEntity();
            try
            {
                var saved = new SkirmishNativeProductionCheckpoint.Snapshot
                {
                    HasDelivery = true,
                    Queue = new[]
                    {
                        new SkirmishNativeProductionCheckpoint.QueueItem
                        {
                            PrefabKey = "unit", TransportPrefabKey = "unit", ReceiptOwner = "session", ReceiptAttempt = "attempt",
                            RemainingQuantity = 1, PaidQuantity = 1, ReceiptId = 1, RefundableMaterials = 100,
                            ReservedProductionSlotIndex = -1, StartedAt = -5, ReadyAt = -1
                        }
                    },
                    Delivery = new SkirmishNativeProductionCheckpoint.Transport
                    {
                        PrefabKey = "unit", Position = new Vector3(30, 20, 40), Rotation = Quaternion.identity,
                        Mode = BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Plane,
                        Phase = 1, HasActiveDrop = true,
                        ActiveDrop = new SkirmishNativeProductionCheckpoint.Drop
                        {
                            QueueIndex = 0, PrefabKey = "unit", StartedAt = -1, Duration = 2,
                            StartPosition = new Vector3(30, 20, 40), EndPosition = new Vector3(30, 0, 40)
                        }
                    }
                };
                Assert.IsTrue(SkirmishNativeProductionCheckpoint.TryPrepare(saved, 10, _ => prefab, _ => owner, out var prepared));
                var building = new RuntimeBuildingEntity();
                Assert.IsTrue(presentation.TryCommitCheckpointProduction(default, building, prepared, 10));
                Assert.IsNotNull(building.ActiveTransport.Instance);
                Assert.IsNotNull(building.ActiveTransport.ActiveDrop.Visual);
                Assert.AreEqual(new Vector3(30, 10, 40), building.ActiveTransport.ActiveDrop.Visual.transform.position);
                Assert.AreSame(building.PendingProductions[0], building.ActiveTransport.ActiveDrop.Production);
                Assert.AreEqual(1, building.PendingProductions[0].RemainingQuantity);
                Assert.AreEqual(100, building.PendingProductions[0].RefundableMaterials);
                Assert.AreEqual(0, building.ProducedUnits.Count);
                var transportInstance = building.ActiveTransport.Instance;
                var dropInstance = building.ActiveTransport.ActiveDrop.Visual;
                presentation.RollbackCheckpointProduction(building, prepared);
                Assert.IsNull(building.ActiveTransport);
                Assert.AreEqual(0, building.PendingProductions.Count);
                Assert.IsFalse(transportInstance.activeSelf);
                Assert.IsFalse(dropInstance.activeSelf);
                Assert.IsTrue(presentation.TryCommitCheckpointProduction(default, building, prepared, 10));
                Assert.AreSame(transportInstance, building.ActiveTransport.Instance);
                Assert.AreSame(dropInstance, building.ActiveTransport.ActiveDrop.Visual);
                Assert.AreEqual(1, building.PendingProductions[0].RemainingQuantity);
            }
            finally
            {
                presentation.Dispose();
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }
    }
}
