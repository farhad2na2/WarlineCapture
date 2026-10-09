using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeCheckpointActorSpawnTests
    {
        [Test]
        public void RecoveryInstantiatesBakedLinkedPrefabAndPreservesUpgradedWeaponState()
        {
            using var source = new World("native-spawn-source");
            var original = CaptureActor(source.EntityManager);
            using var destination = new World("native-spawn-destination");
            var em = destination.EntityManager;
            Entity prefab = em.CreateEntity(typeof(Prefab), typeof(LocalTransform), typeof(UnitHealth), typeof(UnitAttack));
            em.AddComponentData(prefab, new UnitSourcePrefabKey { Value = new FixedString64Bytes("rifle") });
            em.SetComponentData(prefab, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(prefab, new UnitAttack { Damage = 10, Range = 5 });
            Entity child = em.CreateEntity(typeof(Prefab));
            em.AddBuffer<LinkedEntityGroup>(prefab).Add(new LinkedEntityGroup { Value = prefab });
            em.GetBuffer<LinkedEntityGroup>(prefab).Add(new LinkedEntityGroup { Value = child });
            Entity registry = em.CreateEntity();
            em.AddBuffer<UnitPrefabRegistryEntry>(registry).Add(new UnitPrefabRegistryEntry { Prefab = prefab });
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TryInstantiateCheckpointUnit(em, original, out var actor));
            Assert.IsTrue(em.HasComponent<Disabled>(actor));
            Assert.IsTrue(em.HasComponent<CampaignMissionCombatSuppressedTag>(actor));
            Assert.IsTrue(em.HasComponent<SkirmishSharedActorTag>(actor));
            var linked = em.GetBuffer<LinkedEntityGroup>(actor);
            Assert.AreEqual(2, linked.Length);
            Assert.AreEqual(actor, linked[0].Value);
            Assert.AreNotEqual(child, linked[1].Value);
            Assert.IsTrue(em.Exists(linked[1].Value));
            SkirmishNativeCheckpointParts.Apply(em, actor, original.Parts, _ => Entity.Null);
            Assert.AreEqual(27, em.GetComponentData<UnitHealth>(actor).Current);
            Assert.AreEqual(19, em.GetComponentData<UnitAttack>(actor).Damage);
            Assert.AreEqual(8, em.GetComponentData<UnitAttack>(actor).Range);
            Assert.AreEqual(new float3(15, 2, 30), em.GetComponentData<LocalTransform>(actor).Position);
            Assert.IsFalse(em.HasComponent<Disabled>(actor));
            Assert.IsFalse(SkirmishScenarioSpawnSystem.TryInstantiateCheckpointUnit(em, original, out _));
        }

        [Test]
        public void MissingBakedPrefabNeverCreatesFixtureRecoveryActor()
        {
            using var source = new World("native-spawn-missing-source");
            var saved = CaptureActor(source.EntityManager);
            using var destination = new World("native-spawn-missing-destination");
            var em = destination.EntityManager;
            Assert.IsFalse(SkirmishScenarioSpawnSystem.TryInstantiateCheckpointUnit(em, saved, out var actor));
            Assert.AreEqual(Entity.Null, actor);
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            Assert.AreEqual(0, query.CalculateEntityCount());
        }

        private static SkirmishNativeCheckpointSnapshotService.Actor CaptureActor(EntityManager em)
        {
            var snapshot = SkirmishNativeCheckpointCodecTests.Capture(em, out _, out var actor);
            em.AddComponentData(actor, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Rifle });
            em.AddComponentData(actor, LocalTransform.FromPosition(new float3(15, 2, 30)));
            em.AddComponentData(actor, new UnitAttack { Damage = 19, Range = 8 });
            em.SetComponentData(actor, new UnitHealth { Current = 27, Max = 100 });
            snapshot.Actors[0].Parts = SkirmishNativeCheckpointParts.Capture(em, actor, _ => null);
            return snapshot.Actors[0];
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeCheckpointActorSpawnTests();
            suite.RecoveryInstantiatesBakedLinkedPrefabAndPreservesUpgradedWeaponState();
            suite.MissingBakedPrefabNeverCreatesFixtureRecoveryActor();
            Debug.Log("[SkirmishNativeCheckpointActorSpawnTests] result=Passed tests=2 nativeRecoveredSessions=0");
        }
    }
}
