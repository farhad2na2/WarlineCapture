using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeCheckpointRecoveryCommitTests
    {
        private static SkirmishNativeCheckpointRecoveryPreparation.Prepared Prepare(EntityManager em, out Entity actor)
        {
            var saved = SkirmishNativeCheckpointCodecTests.Capture(em, out var setup, out actor);
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity session = sessions.GetSingletonEntity();
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.Phase = SkirmishSessionPhase.Spawning;
            em.SetComponentData(session, state);
            em.SetComponentData(actor, new UnitHealth { Current = 99, Max = 100 });
            em.AddComponent<CampaignMissionCombatSuppressedTag>(actor);
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(em, session, saved, setup,
                _ => null, out var prepared, out var failure), failure);
            return prepared;
        }

        [Test]
        public void RestoreCommitsOnceAndReleasesCombatSuppression()
        {
            using var world = new World(nameof(RestoreCommitsOnceAndReleasesCombatSuppression));
            var em = world.EntityManager;
            var prepared = Prepare(em, out var actor);
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryCommit.TryCommit(em, prepared, out var failure), failure);
            Assert.AreEqual(37, em.GetComponentData<UnitHealth>(actor).Current);
            Assert.IsFalse(em.HasComponent<CampaignMissionCombatSuppressedTag>(actor));
            Assert.IsTrue(prepared.Committed);
            Assert.IsFalse(SkirmishNativeCheckpointRecoveryCommit.TryCommit(em, prepared, out _));
        }

        [Test]
        public void ForeignWorldAndDisappearedActorFailWithoutChangingState()
        {
            using var world = new World("commit-original");
            var prepared = Prepare(world.EntityManager, out var actor);
            using var other = new World("commit-other");
            Prepare(other.EntityManager, out var otherActor);
            using var sessions = other.EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity originalSession = prepared.Session;
            prepared.Session = sessions.GetSingletonEntity();
            Assert.IsFalse(SkirmishNativeCheckpointRecoveryCommit.TryCommit(other.EntityManager, prepared, out _));
            Assert.AreEqual(99, other.EntityManager.GetComponentData<UnitHealth>(otherActor).Current);
            prepared.Session = originalSession;
            world.EntityManager.DestroyEntity(actor);
            Assert.IsFalse(SkirmishNativeCheckpointRecoveryCommit.TryCommit(world.EntityManager, prepared, out _));
            Assert.IsFalse(prepared.Committed);
        }

        [Test]
        public void LaterProductionFailureRollsBackEarlierOwnerBeforeActorsChange()
        {
            using var world = new World(nameof(LaterProductionFailureRollsBackEarlierOwnerBeforeActorsChange));
            var em = world.EntityManager;
            var prepared = Prepare(em, out var actor);
            bool firstCommitted = false;
            bool firstRolledBack = false;
            for (int i = 1; i <= 2; i++)
            {
                Entity entity = em.CreateEntity(typeof(RuntimeBuildingCombatInfo));
                em.SetComponentData(entity, new RuntimeBuildingCombatInfo { RuntimeBuildingId = i });
                var building = new RuntimeBuildingEntity { Id = i, CombatEntity = entity };
                bool succeeds = i == 1;
                em.AddComponentObject(entity, new SkirmishNativeBuildingCheckpointRecord
                {
                    Building = building,
                    TryRestoreProduction = _ => { if (succeeds) firstCommitted = true; return succeeds; },
                    RollbackProduction = _ => { firstCommitted = false; firstRolledBack = true; }
                });
                prepared.Production.Add(building, new SkirmishNativeProductionCheckpoint.Prepared { Slots = new Entity[0] });
            }
            Assert.IsFalse(SkirmishNativeCheckpointRecoveryCommit.TryCommit(em, prepared, out var failure));
            StringAssert.Contains("visual reconstruction failed", failure);
            Assert.IsTrue(firstRolledBack);
            Assert.IsFalse(firstCommitted);
            Assert.AreEqual(99, em.GetComponentData<UnitHealth>(actor).Current);
            Assert.IsTrue(em.HasComponent<CampaignMissionCombatSuppressedTag>(actor));
            Assert.IsFalse(prepared.Committed);
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeCheckpointRecoveryCommitTests();
            suite.RestoreCommitsOnceAndReleasesCombatSuppression();
            suite.ForeignWorldAndDisappearedActorFailWithoutChangingState();
            suite.LaterProductionFailureRollsBackEarlierOwnerBeforeActorsChange();
            Debug.Log("[SkirmishNativeCheckpointRecoveryCommitTests] result=Passed tests=3 nativeRecoveredSessions=0");
        }
    }
}
