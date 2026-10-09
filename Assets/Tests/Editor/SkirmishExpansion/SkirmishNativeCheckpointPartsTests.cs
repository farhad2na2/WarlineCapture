using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeCheckpointPartsTests
    {
        public static void RunDeliveryTaxiCheckpointValidation()
        {
            try
            {
                var tests = new SkirmishNativeCheckpointPartsTests();
                tests.DeliveryTaxiRoundTripPreservesParkingAndQueuedPlayerDestination();
                tests.RestoredMovementReplansInsteadOfReusingWorldPathIndices();
                tests.FreshWorldRestoresTransformsHealthAndCargoWithNewEntityHandles();
                Debug.Log("[DeliveryTaxiCheckpointValidation] result=Passed cases=3");
                ValidationExit.Passed();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[DeliveryTaxiCheckpointValidation] result=Failed");
                ValidationExit.Failed();
            }
        }

        [Test]
        public void DeliveryTaxiRoundTripPreservesParkingAndQueuedPlayerDestination()
        {
            using var source = new World("delivery-taxi-checkpoint-source");
            var em = source.EntityManager;
            var jet = em.CreateEntity(typeof(UnitSpawnTransitTag), typeof(UnitAirComponent),
                typeof(UnitTarget), typeof(ManualMoveOrderTag), typeof(LocalTransform));
            var parking = new float3(837.5f, .82416415f, 709.5f);
            var midway = new float3(834f, parking.y, 688f);
            var queuedGoal = new int2(1402, 635);
            em.SetComponentData(jet, new UnitAirComponent
            {
                HomeInitialized = 1, UsesRunway = 1, HomePosition = parking,
                HomeCell = new int2(837, 709),
                RunwayTakeoffPosition = new float3(832.32f, parking.y, 767.67f),
                RunwayLandingPosition = new float3(832.34f, parking.y, 670.62f)
            });
            em.SetComponentData(jet, LocalTransform.FromPosition(midway));
            em.SetComponentData(jet, new UnitTarget { Cell = queuedGoal });
            var parts = SkirmishNativeCheckpointParts.Capture(em, jet, _ => null);
            parts = JsonUtility.FromJson<Record>(JsonUtility.ToJson(new Record { Parts = parts })).Parts;
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(parts, new HashSet<string>()));

            using var destination = new World("delivery-taxi-checkpoint-destination");
            var restored = destination.EntityManager;
            restored.CreateEntity();
            var newJet = restored.CreateEntity();
            SkirmishNativeCheckpointParts.Apply(restored, newJet, parts, _ => Entity.Null);
            Assert.IsTrue(restored.HasComponent<UnitSpawnTransitTag>(newJet),
                "Save/Continue must keep a mid-taxi aircraft in delivery transit until parking completes.");
            Assert.AreEqual(parking, restored.GetComponentData<UnitAirComponent>(newJet).HomePosition);
            Assert.AreEqual(midway, restored.GetComponentData<LocalTransform>(newJet).Position);
            Assert.AreEqual(queuedGoal, restored.GetComponentData<UnitTarget>(newJet).Cell);
            Assert.IsTrue(restored.HasComponent<ManualMoveOrderTag>(newJet));
        }

        [Test]
        public void FreshWorldRestoresTransformsHealthAndCargoWithNewEntityHandles()
        {
            using var source = new World("checkpoint-parts-source");
            var em = source.EntityManager;
            var truck = em.CreateEntity();
            var passenger = em.CreateEntity(typeof(LocalTransform), typeof(UnitHealth), typeof(UnitTransportPassenger));
            em.SetComponentData(passenger, LocalTransform.FromPosition(new float3(31, 2, 49)));
            em.SetComponentData(passenger, new UnitHealth { Current = 37, Max = 100 });
            em.SetComponentData(passenger, new UnitTransportPassenger { Transport = truck });
            em.AddBuffer<UnitTransportPassengerElement>(truck).Add(new UnitTransportPassengerElement { Passenger = passenger });
            string Id(Entity entity) => entity == truck ? "truck" : entity == passenger ? "passenger" : null;
            var truckParts = SkirmishNativeCheckpointParts.Capture(em, truck, Id);
            var passengerParts = SkirmishNativeCheckpointParts.Capture(em, passenger, Id);
            // Exercise the actual serialization boundary, not an in-memory struct copy.
            truckParts = JsonUtility.FromJson<Record>(JsonUtility.ToJson(new Record { Parts = truckParts })).Parts;
            passengerParts = JsonUtility.FromJson<Record>(JsonUtility.ToJson(new Record { Parts = passengerParts })).Parts;

            using var destination = new World("checkpoint-parts-destination");
            var restored = destination.EntityManager;
            restored.CreateEntity(); // Deliberately shift destination entity indices.
            var newTruck = restored.CreateEntity();
            var newPassenger = restored.CreateEntity(typeof(HoldPositionOrderTag));
            Entity Resolve(string id) => id == "truck" ? newTruck : id == "passenger" ? newPassenger : Entity.Null;
            var ids = new HashSet<string> { "truck", "passenger" };
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(truckParts, ids));
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(passengerParts, ids));
            SkirmishNativeCheckpointParts.Apply(restored, newTruck, truckParts, Resolve);
            SkirmishNativeCheckpointParts.Apply(restored, newPassenger, passengerParts, Resolve);
            Assert.AreNotEqual(truck, newTruck);
            Assert.AreEqual(newTruck, restored.GetComponentData<UnitTransportPassenger>(newPassenger).Transport);
            Assert.AreEqual(newPassenger, restored.GetBuffer<UnitTransportPassengerElement>(newTruck)[0].Passenger);
            Assert.AreEqual(new float3(31, 2, 49), restored.GetComponentData<LocalTransform>(newPassenger).Position);
            Assert.AreEqual(37, restored.GetComponentData<UnitHealth>(newPassenger).Current);
            Assert.IsFalse(restored.HasComponent<HoldPositionOrderTag>(newPassenger));
        }

        [Test]
        public void MissingReferenceRejectsBeforeHealthOrTagsChange()
        {
            using var world = new World(nameof(MissingReferenceRejectsBeforeHealthOrTagsChange));
            var em = world.EntityManager;
            var target = em.CreateEntity();
            var actor = em.CreateEntity(typeof(UnitHealth), typeof(UnitTransportPassenger));
            em.SetComponentData(actor, new UnitHealth { Current = 25, Max = 100 });
            em.SetComponentData(actor, new UnitTransportPassenger { Transport = target });
            var parts = SkirmishNativeCheckpointParts.Capture(em, actor, _ => "missing-truck");
            em.SetComponentData(actor, new UnitHealth { Current = 80, Max = 100 });
            em.AddComponent<HoldPositionOrderTag>(actor);
            Assert.Throws<ArgumentException>(() => SkirmishNativeCheckpointParts.Apply(em, actor, parts, _ => Entity.Null));
            Assert.AreEqual(80, em.GetComponentData<UnitHealth>(actor).Current);
            Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(actor));
        }

        [Test]
        public void CorruptLayoutsDuplicatePartsAndUnknownReferencesAreRejected()
        {
            using var world = new World(nameof(CorruptLayoutsDuplicatePartsAndUnknownReferencesAreRejected));
            var em = world.EntityManager;
            var entity = em.CreateEntity(typeof(UnitHealth));
            var parts = SkirmishNativeCheckpointParts.Capture(em, entity, _ => null);
            var ids = new HashSet<string>();
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(parts, ids));
            Assert.IsFalse(SkirmishNativeCheckpointParts.Validate(parts.Concat(parts).ToArray(), ids));
            parts[0].Layout ^= 1;
            Assert.IsFalse(SkirmishNativeCheckpointParts.Validate(parts, ids));
            parts[0].Layout ^= 1;
            parts[0].Data = "invalid base64!";
            Assert.IsFalse(SkirmishNativeCheckpointParts.Validate(parts, ids));
        }

        [Test]
        public void RestoredMovementReplansInsteadOfReusingWorldPathIndices()
        {
            using var world = new World(nameof(RestoredMovementReplansInsteadOfReusingWorldPathIndices));
            var em = world.EntityManager;
            var actor = em.CreateEntity(typeof(UnitTarget));
            em.SetComponentData(actor, new UnitTarget { Cell = new int2(21, 33) });
            var parts = SkirmishNativeCheckpointParts.Capture(em, actor, _ => null);
            em.AddComponentData(actor, new UnitPathFollow { PathIndex = 999 });
            em.AddComponentData(actor, new UnitPathRange { Start = 222, Length = 10 });
            SkirmishNativeCheckpointParts.Apply(em, actor, parts, _ => Entity.Null);
            Assert.IsFalse(em.HasComponent<UnitPathFollow>(actor));
            Assert.IsFalse(em.HasComponent<UnitPathRange>(actor));
            Assert.AreEqual(new int2(21, 33), em.GetComponentData<UnitPathRequest>(actor).Goal);
        }

        [Test]
        public void ExpiredMoveTargetCapturePreservesOrderWithoutMutatingPausedActor()
        {
            foreach (bool destroyed in new[] { false, true })
            {
                using var source = new World("expired-move-source");
                var em = source.EntityManager;
                var target = em.CreateEntity(typeof(UnitHealth));
                em.SetComponentData(target, new UnitHealth { Current = 0, Max = 100 });
                if (destroyed) em.DestroyEntity(target);
                var actor = em.CreateEntity(typeof(SkirmishMoveIntentComponent));
                var intent = new SkirmishMoveIntentComponent {
                    AttackTarget = target, Active = 1, Engaged = 1,
                    Order = Game.Skirmish.Contracts.SkirmishGroupOrderKind.Attack,
                    DestinationX = 45, DestinationZ = 20, Cooldown = .7f
                };
                em.SetComponentData(actor, intent);
                var saved = SkirmishNativeCheckpointParts.Capture(em, actor, _ => null);
                saved = JsonUtility.FromJson<Record>(JsonUtility.ToJson(new Record { Parts = saved })).Parts;
                Assert.AreEqual(target, em.GetComponentData<SkirmishMoveIntentComponent>(actor).AttackTarget);
                Assert.AreEqual(1, em.GetComponentData<SkirmishMoveIntentComponent>(actor).Engaged);
                Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(saved, new HashSet<string>()));
                using var destination = new World("expired-move-restored");
                var restored = destination.EntityManager;
                var newActor = restored.CreateEntity();
                SkirmishNativeCheckpointParts.Apply(restored, newActor, saved, _ => Entity.Null);
                var result = restored.GetComponentData<SkirmishMoveIntentComponent>(newActor);
                Assert.AreEqual(Entity.Null, result.AttackTarget);
                Assert.AreEqual(0, result.Engaged);
                Assert.AreEqual(intent.Active, result.Active);
                Assert.AreEqual(intent.Order, result.Order);
                Assert.AreEqual(intent.DestinationX, result.DestinationX);
                Assert.AreEqual(intent.DestinationZ, result.DestinationZ);
                Assert.AreEqual(intent.Cooldown, result.Cooldown);
            }
        }

        [Test]
        public void LivingMoveTargetsRequireAliasesAndKeepTheirEngagement()
        {
            using var world = new World("living-move-target");
            var em = world.EntityManager;
            var target = em.CreateEntity(typeof(UnitHealth));
            em.SetComponentData(target, new UnitHealth { Current = 37, Max = 100 });
            var actor = em.CreateEntity(typeof(SkirmishMoveIntentComponent));
            em.SetComponentData(actor, new SkirmishMoveIntentComponent { AttackTarget = target, Engaged = 1 });
            Assert.Throws<InvalidOperationException>(() => SkirmishNativeCheckpointParts.Capture(em, actor, _ => null));
            var saved = SkirmishNativeCheckpointParts.Capture(em, actor, e => e == target ? "live-target" : null);
            using var destination = new World("living-target-restored");
            var restored = destination.EntityManager;
            restored.CreateEntity();
            var newTarget = restored.CreateEntity();
            var newActor = restored.CreateEntity();
            SkirmishNativeCheckpointParts.Apply(restored, newActor, saved, id => id == "live-target" ? newTarget : Entity.Null);
            Assert.AreEqual(newTarget, restored.GetComponentData<SkirmishMoveIntentComponent>(newActor).AttackTarget);
            Assert.AreEqual(1, restored.GetComponentData<SkirmishMoveIntentComponent>(newActor).Engaged);
        }

        [Test]
        public void DestroyedPassengerCarrierIsNeverDiscardedAsAnOptionalCombatTarget()
        {
            using var world = new World("missing-passenger-carrier");
            var em = world.EntityManager;
            var carrier = em.CreateEntity();
            var actor = em.CreateEntity(typeof(UnitTransportPassenger));
            em.SetComponentData(actor, new UnitTransportPassenger { Transport = carrier });
            em.DestroyEntity(carrier);
            Assert.Throws<InvalidOperationException>(() => SkirmishNativeCheckpointParts.Capture(em, actor, _ => null));
            Assert.AreEqual(carrier, em.GetComponentData<UnitTransportPassenger>(actor).Transport);
        }

        [Serializable] private sealed class Record { public SkirmishNativeCheckpointParts.Part[] Parts; }

        [Test]
        public void NativeCheckpointPreservesSeparateFactionResearchWithoutChangingPlayerLayout()
        {
            using var source = new World("faction-research-source");
            var em = source.EntityManager;
            var session = em.CreateEntity(typeof(SkirmishResearchStateComponent), typeof(SkirmishEnemyResearchStateComponent));
            em.SetComponentData(session, new SkirmishResearchStateComponent { FactionId = 1,
                Readiness = Game.Skirmish.Contracts.SkirmishReadinessStage.FullArsenal, InfantryWeapons = 1, NextResearchId = 7 });
            em.SetComponentData(session, new SkirmishEnemyResearchStateComponent { State =
                new SkirmishResearchStateComponent { FactionId = 2,
                    Readiness = Game.Skirmish.Contracts.SkirmishReadinessStage.Established, VehicleProtection = 1 } });
            var saved = SkirmishNativeCheckpointParts.Capture(em, session, _ => null);
            saved = JsonUtility.FromJson<Record>(JsonUtility.ToJson(new Record { Parts = saved })).Parts;
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(saved, new HashSet<string>()));
            using var destination = new World("faction-research-destination");
            var restored = destination.EntityManager;
            var fresh = restored.CreateEntity();
            SkirmishNativeCheckpointParts.Apply(restored, fresh, saved, _ => Entity.Null);
            var p = restored.GetComponentData<SkirmishResearchStateComponent>(fresh);
            var e = restored.GetComponentData<SkirmishEnemyResearchStateComponent>(fresh).State;
            Assert.AreEqual(1, p.FactionId); Assert.AreEqual(2, e.FactionId);
            Assert.AreEqual(Game.Skirmish.Contracts.SkirmishReadinessStage.FullArsenal, p.Readiness);
            Assert.AreEqual(Game.Skirmish.Contracts.SkirmishReadinessStage.Established, e.Readiness);
            Assert.AreEqual(1, p.InfantryWeapons); Assert.AreEqual(0, e.InfantryWeapons);
            Assert.AreEqual(0, p.VehicleProtection); Assert.AreEqual(1, e.VehicleProtection);
            Assert.AreEqual(7u, p.NextResearchId);
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeCheckpointPartsTests();
            suite.FreshWorldRestoresTransformsHealthAndCargoWithNewEntityHandles();
            suite.MissingReferenceRejectsBeforeHealthOrTagsChange();
            suite.CorruptLayoutsDuplicatePartsAndUnknownReferencesAreRejected();
            suite.RestoredMovementReplansInsteadOfReusingWorldPathIndices();
            suite.RestoredClocksPreserveScanDropAndHaulProgressAcrossSeparateTimeOrigins();
            suite.ExpiredMoveTargetCapturePreservesOrderWithoutMutatingPausedActor();
            suite.LivingMoveTargetsRequireAliasesAndKeepTheirEngagement();
            suite.DestroyedPassengerCarrierIsNeverDiscardedAsAnOptionalCombatTarget();
            suite.NativeCheckpointPreservesSeparateFactionResearchWithoutChangingPlayerLayout();
            suite.DeliveryTaxiRoundTripPreservesParkingAndQueuedPlayerDestination();
            Debug.Log("[SkirmishNativeCheckpointPartsTests] result=Passed tests=10 nativeRecoveryComplete=0");
        }

        [Test]
        public void RestoredClocksPreserveScanDropAndHaulProgressAcrossSeparateTimeOrigins()
        {
            using var world = new World(nameof(RestoredClocksPreserveScanDropAndHaulProgressAcrossSeparateTimeOrigins));
            var em = world.EntityManager;
            var actor = em.CreateEntity(typeof(UnitScanOrder), typeof(UnitResourceHaulOrder), typeof(UnitTransportRopeDropComponent));
            em.SetComponentData(actor, new UnitScanOrder
            { HasStarted = 1, StartedFrame = 990, StartedTimeSeconds = 100, NextRevealTimeSeconds = 125, NextPatrolMoveTimeSeconds = 130, DurationSeconds = 30 });
            em.SetComponentData(actor, new UnitResourceHaulOrder { ActionEndsAt = 230 });
            em.SetComponentData(actor, new UnitTransportRopeDropComponent { StartedAt = 115, DurationSeconds = 10 });
            em.AddComponentData(actor, new UnitTransportRopeDisembarkRequest { NextDropAt = 125, DropCount = 2, TotalDropCount = 4 });
            em.AddComponentData(actor, new UnitTransportAirdropRequest { NextDropAt = 130, DroppedCount = 1, SoldierDropCount = 4 });
            var original = SkirmishNativeCheckpointParts.Capture(em, actor, _ => null);
            var rebased = SkirmishNativeCheckpointParts.RebaseClocks(original, -100, -190, -1000);
            Assert.AreEqual(100, em.GetComponentData<UnitScanOrder>(actor).StartedTimeSeconds, "Staging must not mutate the source actor.");
            SkirmishNativeCheckpointParts.Apply(em, actor, rebased, _ => Entity.Null);
            var scan = em.GetComponentData<UnitScanOrder>(actor);
            Assert.AreEqual(0, scan.StartedTimeSeconds);
            Assert.AreEqual(25, scan.NextRevealTimeSeconds);
            Assert.AreEqual(30, scan.DurationSeconds);
            Assert.AreEqual(-10, scan.StartedFrame);
            Assert.AreEqual(40, em.GetComponentData<UnitResourceHaulOrder>(actor).ActionEndsAt);
            Assert.AreEqual(15, em.GetComponentData<UnitTransportRopeDropComponent>(actor).StartedAt);
            Assert.AreEqual(25, em.GetComponentData<UnitTransportRopeDisembarkRequest>(actor).NextDropAt);
            Assert.AreEqual(2, em.GetComponentData<UnitTransportRopeDisembarkRequest>(actor).DropCount);
            Assert.AreEqual(30, em.GetComponentData<UnitTransportAirdropRequest>(actor).NextDropAt);
            Assert.AreEqual(1, em.GetComponentData<UnitTransportAirdropRequest>(actor).DroppedCount);
            Assert.Throws<ArgumentException>(() => SkirmishNativeCheckpointParts.RebaseClocks(original, float.NaN, 0, 0));
            Assert.AreEqual(40, em.GetComponentData<UnitResourceHaulOrder>(actor).ActionEndsAt);
        }
    }
}
