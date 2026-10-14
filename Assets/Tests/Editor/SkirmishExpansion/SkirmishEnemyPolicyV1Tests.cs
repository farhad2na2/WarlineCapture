using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishEnemyPolicyV1Tests
    {
        private static SkirmishResolvedSetup Setup(SkirmishDifficultyId difficulty = SkirmishDifficultyId.Regular) =>
            new SkirmishResolvedSetup { CatalogId = "S005", DefinitionId = "skirmish.s005", ContentHash = "source-fixture",
                ObjectiveKind = SkirmishObjectiveKind.BaseAssault, Readiness = SkirmishReadinessStage.Field,
                DifficultyId = difficulty, SizeId = SkirmishSizeId.Standard, Seed = 104734 };

        [Test]
        public void IdentityIsLocaleIndependentAndDifficultyOrSeedChangesIt()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                var setup = Setup();
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup, null, out var en));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fa-IR");
                Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup, null, out var fa));
                Assert.AreEqual(en.Fingerprint, fa.Fingerprint); Assert.AreEqual(en.InitialRandomState, fa.InitialRandomState);
                setup.Seed++;
                Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup, null, out var other)); Assert.AreNotEqual(en.Fingerprint, other.Fingerprint);
                setup.Seed--; setup.DifficultyId = SkirmishDifficultyId.Commander;
                Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup, null, out other));
                Assert.AreEqual(.8f, other.ResponseMinSeconds); Assert.AreEqual(2f, other.ResponseMaxSeconds);
                Assert.AreEqual(30f, other.OpeningRestraintSeconds); Assert.AreNotEqual(en.Fingerprint, other.Fingerprint);
                setup.Readiness = SkirmishReadinessStage.Established;
                Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup, null, out other)); Assert.AreEqual(15f, other.OpeningRestraintSeconds);
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [Test]
        public void RoutesAreDeepCopiedAndAuthoredTransitChangesIdentity()
        {
            SkirmishMapLayoutConfig layout = null;
            try
            {
                // The existing authored builder owns logical routes on existing Campaign geometry.
                var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
                layout = authored.DefinitionS005.MapLayout;
                Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(Setup(), layout, out var policy));
                Assert.Greater(policy.Routes.Count, 0);
                var originalRoute = layout.Routes[0];
                string anchorId = originalRoute.WaypointAnchorIds[0];
                var first = policy.Routes.First(r => r.RouteId == originalRoute.RouteId).Waypoints[0];
                try
                {
                    layout.Routes[0].WaypointAnchorIds[0] = "missing-anchor";
                    Assert.AreEqual(first.X, policy.Routes.First(r => r.RouteId == originalRoute.RouteId).Waypoints[0].X);
                    Assert.IsFalse(SkirmishEnemyPolicyCompiler.TryCompile(Setup(), layout, out _));
                    layout.Routes[0].WaypointAnchorIds[0] = anchorId;
                    var changed = layout.Routes[0]; changed.GroundTransitSeconds += 1; layout.Routes[0] = changed;
                    Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(Setup(), layout, out var revised));
                    Assert.AreNotEqual(policy.Fingerprint, revised.Fingerprint);
                }
                finally { layout.Routes[0] = originalRoute; layout.Routes[0].WaypointAnchorIds[0] = anchorId; }
            }
            finally { if (layout != null) UnityEngine.Object.DestroyImmediate(layout); }
        }

        [Test]
        public void EnsureIsIdempotentAndPauseStopsTheElapsedPlannerClock()
        {
            using var world = new World(nameof(EnsureIsIdempotentAndPauseStopsTheElapsedPlannerClock));
            var em = world.EntityManager; var setup = Setup();
            Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup, null, out var policy)); setup.EnemyPolicy = policy;
            var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishObjectiveClockComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { Phase = SkirmishSessionPhase.Playing });
            em.SetComponentData(session, new SkirmishObjectiveClockComponent { ElapsedSeconds = 40, Playing = 1 });
            Assert.IsTrue(SkirmishEnemyPlannerService.EnsureSession(em, session, setup));
            var planner = em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);
            planner.NextDecisionAt = 55; planner.ResponseDueAt = 41.25f; planner.ResponsePending = 1; planner.DecisionCount = 9;
            em.SetComponentData(session, planner);
            Assert.IsTrue(SkirmishEnemyPlannerService.EnsureSession(em, session, setup));
            Assert.AreEqual(9, em.GetComponentData<SkirmishEnemyPlannerV1Component>(session).DecisionCount);
            var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session); clock.Paused = 1; em.SetComponentData(session, clock);
            Assert.IsFalse(SkirmishEnemyPlannerService.TryReadActiveClock(em, session, out _));
            Assert.AreEqual(41.25f, em.GetComponentData<SkirmishEnemyPlannerV1Component>(session).ResponseDueAt);
            clock.Paused = 0; em.SetComponentData(session, clock);
            Assert.IsTrue(SkirmishEnemyPlannerService.TryReadActiveClock(em, session, out float now)); Assert.AreEqual(40, now);
            Assert.AreEqual(55, em.GetComponentData<SkirmishEnemyPlannerV1Component>(session).NextDecisionAt);
        }

        private static SkirmishNativeCheckpointSnapshotService.Snapshot Native(EntityManager em,
            out SkirmishResolvedSetup setup, out Entity session)
        {
            SkirmishNativeCheckpointCodecTests.Capture(em, out setup, out _);
            setup.ObjectiveKind = SkirmishObjectiveKind.BaseAssault; setup.Readiness = SkirmishReadinessStage.Field;
            Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup, null, out var policy));
            setup.EnemyPolicy = policy; setup.ContentHash += "/enemy-policy-v1/" + policy.Fingerprint;
            setup.SetupHash = SkirmishSetupCompiler.ComputeHash(setup, setup.Seed);
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)); session = sessions.GetSingletonEntity();
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.SetupHash = setup.SetupHash; state.ObjectiveKind = SkirmishObjectiveKind.BaseAssault;
            state.Phase = SkirmishSessionPhase.Playing; state.SpawnComplete = 1;
            em.SetComponentData(session, state);
            em.AddComponentData(session, new SkirmishObjectiveClockComponent { ElapsedSeconds = 42, Playing = 1, Paused = 1 });
            em.AddBuffer<SkirmishArmyGroupRecord>(session);
            em.AddBuffer<SkirmishProductionReservation>(session);
            Assert.IsTrue(SkirmishEnemyPlannerService.EnsureSession(em, session, setup));
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "prefab", out var saved, out string failure), failure);
            return saved;
        }

        [Test]
        public void PendingPaidIntentAndResponseTimersSurviveNativeWireAndClockRebase()
        {
            using var world = new World(nameof(PendingPaidIntentAndResponseTimersSurviveNativeWireAndClockRebase)); var em = world.EntityManager;
            Native(em, out var setup, out var session);
            var planner = em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);
            planner.ResponsePending = 1; planner.ResponseDueAt = 43.5f; planner.NextDecisionAt = 47; planner.RandomState = 12345;
            em.SetComponentData(session, planner);
            em.GetBuffer<SkirmishProductionReservation>(session).Add(new SkirmishProductionReservation {
                ReservationId = 7, Role = SkirmishRoleKind.Rifle, FactionId = 2, MemberCount = 4, RemainingMembers = 4, Phase = SkirmishReservationPhase.Producing });
            em.SetComponentData(session, new SkirmishEnemyProductionIntentV1Component {
                Version = 1, State = SkirmishEnemyProductionIntentState.AcceptedWaiting, ActionKey = "enemy.rifle.1",
                RequestId = 1, NextRequestId = 2, ReservationId = 7, ExpectedMembers = 4, Role = SkirmishRoleKind.Rifle,
                LastRequestElapsed = 40, LastObservedReservationPhase = (byte)SkirmishReservationPhase.Producing });
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "prefab", out var saved, out _));
            string wire = SkirmishNativeCheckpointCodec.Encode(saved, setup); Assert.IsNotNull(wire);
            Assert.IsTrue(SkirmishNativeCheckpointCodec.TryDecode(wire, setup, out var restored));
            var parts = SkirmishNativeCheckpointParts.RebaseClocks(restored.SessionParts, 900, 900, 90);
            Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadComponent(parts, out SkirmishEnemyPlannerV1Component after));
            Assert.AreEqual(43.5f, after.ResponseDueAt); Assert.AreEqual(47, after.NextDecisionAt); Assert.AreEqual(12345, after.RandomState);
            Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadComponent(parts, out SkirmishEnemyProductionIntentV1Component intent));
            Assert.AreEqual(7, intent.ReservationId); Assert.AreEqual(SkirmishEnemyProductionIntentState.AcceptedWaiting, intent.State);
            var destination = em.CreateEntity();
            SkirmishNativeCheckpointParts.Apply(em, destination, parts, id => id == "$session" ? destination : Entity.Null);
            Assert.AreEqual(7, em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(destination).ReservationId);
            Assert.IsTrue(SkirmishEnemyPlannerService.EnsureSession(em, destination, setup));
            Assert.AreEqual(12345, em.GetComponentData<SkirmishEnemyPlannerV1Component>(destination).RandomState);
        }

        [Test]
        public void PolicylessOldSaveIsRejectedIntactButRecognizedOnlyWhenOtherwiseValid()
        {
            using var world = new World(nameof(PolicylessOldSaveIsRejectedIntactButRecognizedOnlyWhenOtherwiseValid)); var em = world.EntityManager;
            Native(em, out var setup, out var session);
            var policy = setup.EnemyPolicy;
            em.RemoveComponent<SkirmishEnemyPlannerV1Component>(session); em.RemoveComponent<SkirmishEnemyProductionIntentV1Component>(session);
            em.RemoveComponent<SkirmishEnemyGroupAssignmentV1>(session);
            // Represent a genuinely pre-policy snapshot, with no additive operation authority.
            em.RemoveComponent<SkirmishEnemyCapabilityStateV1Component>(session);
            em.RemoveComponent<SkirmishEnemyCapabilityOperationV1>(session);
            em.RemoveComponent<SkirmishEnemyTransportPlanV1>(session);
            em.RemoveComponent<SkirmishEnemyConstructionIntentV1Component>(session);
            setup.ContentHash = policy.SourceContentHash; setup.EnemyPolicy = null;
            setup.SetupHash = SkirmishSetupCompiler.ComputePolicylessHash(setup, setup.Seed);
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session); state.SetupHash = setup.SetupHash; em.SetComponentData(session, state);
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "prefab", out var old, out _));
            string directory = Path.Combine(Path.GetTempPath(), "warline-policy-old-" + Guid.NewGuid().ToString("N")); string path = Path.Combine(directory, "match.json");
            try
            {
                Assert.IsTrue(SkirmishNativeCheckpointCodec.TryWriteAtomic(path, old, setup)); string previous = File.ReadAllText(path);
                setup.EnemyPolicy = policy; setup.ContentHash += "/enemy-policy-v1/" + policy.Fingerprint;
                setup.SetupHash = SkirmishSetupCompiler.ComputeHash(setup, setup.Seed);
                Assert.IsFalse(SkirmishNativeCheckpointCodec.TryRead(path, setup, out _)); Assert.AreEqual(previous, File.ReadAllText(path));
                Assert.IsTrue(SkirmishNativeCheckpointCodec.ValidatePolicylessCompatibility(old, setup));
                old.SessionParts[0].Layout ^= 1;
                Assert.IsFalse(SkirmishNativeCheckpointCodec.ValidatePolicylessCompatibility(old, setup));
                Assert.AreEqual(previous, File.ReadAllText(path));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void NewPolicyRejectsMissingOrChangedPartsAndOldFixtureCodecBeforeMutation()
        {
            using var world = new World(nameof(NewPolicyRejectsMissingOrChangedPartsAndOldFixtureCodecBeforeMutation)); var em = world.EntityManager;
            var saved = Native(em, out var setup, out var session);
            Assert.IsNotNull(SkirmishNativeCheckpointCodec.Encode(saved, setup));
            saved.SessionParts = saved.SessionParts.Where(p => p.Type != typeof(SkirmishEnemyProductionIntentV1Component).FullName).ToArray();
            Assert.IsNull(SkirmishNativeCheckpointCodec.Encode(saved, setup));
            Assert.IsFalse(SkirmishCheckpointService.TryCapture(em, session, out _));
            var p = em.GetComponentData<SkirmishEnemyPlannerV1Component>(session); p.PolicyVersion++; em.SetComponentData(session, p);
            Assert.IsFalse(SkirmishEnemyPlannerService.EnsureSession(em, session, setup));
            Assert.AreEqual(setup.EnemyPolicy.Version + 1, em.GetComponentData<SkirmishEnemyPlannerV1Component>(session).PolicyVersion);
            // Legacy/null-policy native codec fixtures retain their exact original validity.
            using var legacy = new World("policyless-native-fixture");
            var old = SkirmishNativeCheckpointCodecTests.Capture(legacy.EntityManager, out var oldSetup, out _);
            Assert.IsNotNull(SkirmishNativeCheckpointCodec.Encode(old, oldSetup));
        }

        public static void RunFocusedValidation()
        {
            var tests = new SkirmishEnemyPolicyV1Tests();
            tests.IdentityIsLocaleIndependentAndDifficultyOrSeedChangesIt();
            tests.RoutesAreDeepCopiedAndAuthoredTransitChangesIdentity();
            tests.EnsureIsIdempotentAndPauseStopsTheElapsedPlannerClock();
            tests.PendingPaidIntentAndResponseTimersSurviveNativeWireAndClockRebase();
            tests.PolicylessOldSaveIsRejectedIntactButRecognizedOnlyWhenOtherwiseValid();
            tests.NewPolicyRejectsMissingOrChangedPartsAndOldFixtureCodecBeforeMutation();
            Debug.Log("[SkirmishEnemyPolicyV1Tests] result=Passed tests=6 scope=policy-native-codec-foundation nativePlayerWins=0");
        }
    }
}
