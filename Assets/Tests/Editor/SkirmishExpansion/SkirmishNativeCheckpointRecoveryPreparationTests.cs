using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeCheckpointRecoveryPreparationTests
    {
        [Test]
        public void MissingSourceFootprintFailsBeforeAnyRestoredHealthChanges()
        {
            using var source = new World("native-preflight-source");
            var em = source.EntityManager;
            SkirmishNativeCheckpointCodecTests.Capture(em, out var setup, out var actor);
            Entity wall = Wall(em, 5);
            em.AddComponentData(actor, new EngageTarget { Target = wall });
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, sessions.GetSingletonEntity(),
                _ => "prefab", out var snapshot, out var failure), failure);
            string wire = SkirmishNativeCheckpointCodec.Encode(snapshot, setup);
            Assert.IsTrue(SkirmishNativeCheckpointCodec.TryDecode(wire, setup, out snapshot));

            using var destination = new World("native-preflight-destination");
            var fresh = destination.EntityManager;
            SkirmishNativeCheckpointCodecTests.Capture(fresh, out _, out var newActor);
            fresh.SetComponentData(newActor, new UnitHealth { Current = 99, Max = 100 });
            using var freshSessions = fresh.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity newSession = freshSessions.GetSingletonEntity();
            Assert.IsFalse(SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(fresh, newSession, snapshot, setup,
                _ => null, out var prepared, out failure));
            Assert.IsNull(prepared);
            StringAssert.Contains("Source map census differs", failure);
            Assert.AreEqual(99, fresh.GetComponentData<UnitHealth>(newActor).Current);

            Entity newWall = Wall(fresh, 105);
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(fresh, newSession, snapshot, setup,
                _ => null, out prepared, out failure), failure);
            Assert.AreEqual(99, fresh.GetComponentData<UnitHealth>(newActor).Current, "Preflight is read-only.");
            foreach (var pair in prepared.Parts)
                SkirmishNativeCheckpointParts.Apply(fresh, pair.Key, pair.Value, id => prepared.Identities[id]);
            Assert.AreEqual(37, fresh.GetComponentData<UnitHealth>(newActor).Current);
            Assert.AreEqual(newWall, fresh.GetComponentData<EngageTarget>(newActor).Target);
            Assert.AreEqual(105, fresh.GetComponentData<RuntimeBuildingCombatInfo>(newWall).RuntimeBuildingId);
        }

        [Test]
        public void FreshRosterMustMatchStableIdentityAndNativePrefab()
        {
            using var source = new World("native-preflight-identity-source");
            var snapshot = SkirmishNativeCheckpointCodecTests.Capture(source.EntityManager, out var setup, out _);
            using var destination = new World("native-preflight-identity-destination");
            var em = destination.EntityManager;
            SkirmishNativeCheckpointCodecTests.Capture(em, out _, out var actor);
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity session = sessions.GetSingletonEntity();
            em.SetComponentData(actor, new UnitSourcePrefabKey { Value = new FixedString64Bytes("wrong-prefab") });
            Assert.IsFalse(SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(em, session, snapshot, setup,
                _ => null, out _, out var failure));
            StringAssert.Contains("native prefab", failure);
            em.SetComponentData(actor, new UnitSourcePrefabKey { Value = new FixedString64Bytes("rifle") });
            Entity duplicate = em.CreateEntity(typeof(SkirmishSharedActorTag));
            em.AddComponentData(duplicate, em.GetComponentData<SkirmishAttemptOwnedComponent>(actor));
            Assert.IsFalse(SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(em, session, snapshot, setup,
                _ => null, out _, out failure));
            StringAssert.Contains("duplicated", failure);
            Assert.AreEqual(37, em.GetComponentData<UnitHealth>(actor).Current);
        }

        private static Entity Wall(EntityManager em, int runtimeId)
        {
            Entity entity = em.CreateEntity();
            em.AddComponentData(entity, new RuntimeBuildingCombatInfo
            { RuntimeBuildingId = runtimeId, OwnerFactionId = 2, OriginCell = new int2(10, 20) });
            em.AddComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes("wall") });
            em.AddComponentData(entity, new UnitHealth { Current = 17, Max = 100 });
            return entity;
        }

        [Test]
        public void SharedFactionBanksRestoreActualSpendingAndRejectChangedPhysicalMetadata()
        {
            using var source = new World("native-bank-source");
            var em = source.EntityManager;
            SkirmishNativeCheckpointCodecTests.Capture(em, out var setup, out _);
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity session = sessions.GetSingletonEntity();
            em.AddComponent<SkirmishSharedMaterialsInitialized>(session);
            Bank(em, 1, 190, 260);
            Bank(em, 2, 91, 359);
            Entity metadata = em.CreateEntity(typeof(OperationMapMetadataComponent));
            em.SetComponentData(metadata, new OperationMapMetadataComponent { MetadataHash = new FixedString128Bytes("physical-source-1") });
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "prefab", out var snapshot, out var failure), failure);
            string wire = SkirmishNativeCheckpointCodec.Encode(snapshot, setup);
            Assert.IsTrue(SkirmishNativeCheckpointCodec.TryDecode(wire, setup, out snapshot));
            Assert.AreEqual(2, snapshot.Banks.Length);

            using var destination = new World("native-bank-destination");
            var fresh = destination.EntityManager;
            SkirmishNativeCheckpointCodecTests.Capture(fresh, out _, out _);
            using var freshSessions = fresh.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity newSession = freshSessions.GetSingletonEntity();
            fresh.AddComponent<SkirmishSharedMaterialsInitialized>(newSession);
            Entity playerBank = Bank(fresh, 1, 450, 0);
            Entity enemyBank = Bank(fresh, 2, 450, 0);
            Entity newMetadata = fresh.CreateEntity(typeof(OperationMapMetadataComponent));
            fresh.SetComponentData(newMetadata, new OperationMapMetadataComponent { MetadataHash = new FixedString128Bytes("physical-source-2") });
            Assert.IsFalse(SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(fresh, newSession, snapshot, setup,
                _ => null, out _, out failure));
            StringAssert.Contains("physical map metadata", failure);
            Assert.AreEqual(450, fresh.GetComponentData<FactionTacticalMaterialsComponent>(playerBank).Current);
            fresh.SetComponentData(newMetadata, new OperationMapMetadataComponent { MetadataHash = new FixedString128Bytes("physical-source-1") });
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(fresh, newSession, snapshot, setup,
                _ => null, out var prepared, out failure), failure);
            foreach (var pair in prepared.Parts)
                SkirmishNativeCheckpointParts.Apply(fresh, pair.Key, pair.Value, id => prepared.Identities[id]);
            var player = fresh.GetComponentData<FactionTacticalMaterialsComponent>(playerBank);
            Assert.AreEqual(190, player.Current);
            Assert.AreEqual(260, player.LifetimeSpent);
            Assert.AreEqual(91, fresh.GetComponentData<FactionTacticalMaterialsComponent>(enemyBank).Current);
        }

        private static Entity Bank(EntityManager em, byte faction, int materials, int spent)
        {
            Entity bank = em.CreateEntity();
            em.AddComponentData(bank, new FactionEconomy { FactionId = faction, MaterialsOnlyConstruction = 1 });
            em.AddComponentData(bank, new FactionTacticalMaterialsComponent
            { FactionId = faction, Current = materials, Capacity = 800, LifetimeSpent = spent, Version = 10 });
            return bank;
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeCheckpointRecoveryPreparationTests();
            suite.MissingSourceFootprintFailsBeforeAnyRestoredHealthChanges();
            suite.FreshRosterMustMatchStableIdentityAndNativePrefab();
            suite.SharedFactionBanksRestoreActualSpendingAndRejectChangedPhysicalMetadata();
            Debug.Log("[SkirmishNativeCheckpointRecoveryPreparationTests] result=Passed tests=3 nativeRecoveredSessions=0");
        }
    }
}
