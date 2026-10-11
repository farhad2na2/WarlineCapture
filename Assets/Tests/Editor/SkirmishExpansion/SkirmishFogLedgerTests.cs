using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishFogLedgerTests
    {
        private const string Attempt = "fog.ledger.actual-system";
        public static void RunFocusedValidation()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Ledger suite requires idle Editor.");
            var suite = new SkirmishFogLedgerTests();
            suite.DestroyedHiddenContactKeepsFrozenRecordUntilExpiry(1);
            suite.DestroyedHiddenContactKeepsFrozenRecordUntilExpiry(2);
            suite.NativeObservedDeathDoesNotResurrectContact();
            suite.NativeHiddenDeathPreservesLastObservedAliveContact();
            suite.StaleVisibleCannotRevealSameTickHiddenNativeDeath();
            suite.NativeScanEmissionSurvivesDestructionBeforeNextFogTick();
            suite.SessionCheckpointRetainsDestroyedContactAndRebasesRemainingAge();
            suite.ReusedAttemptCannotInheritPriorContactMemory();
            suite.NativeSessionCleanupRemovesLedger();
            Debug.Log("[SkirmishFogLedgerTests] result=Passed cases=9 scope=IsolatedActualSystemNotNormalMission");
        }

        [TestCase((byte)1)] [TestCase((byte)2)]
        public void DestroyedHiddenContactKeepsFrozenRecordUntilExpiry(byte observer)
        {
            using var world = new World(nameof(DestroyedHiddenContactKeepsFrozenRecordUntilExpiry));
            var em = world.EntityManager; Entity session = Session(em);
            Actor(em, "observer", observer, 0, 5);
            Entity target = Actor(em, "target", observer == 1 ? (byte)2 : (byte)1, 4, 0);
            Step(world, 1, .1f);
            var observed = Record(em, session, "target", observer);
            Move(em, target, 100); Step(world, 2, .1f);
            em.DestroyEntity(target);
            Step(world, 3, .1f);
            var absent = Record(em, session, "target", observer);
            Assert.AreEqual(SkirmishContactSight.LastSeen, absent.Sight);
            AssertFrozen(observed, absent);
            var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            clock.Paused = 1; em.SetComponentData(session, clock);
            Step(world, 3, .1f); Step(world, 3, 0);
            AssertFrozen(absent, Record(em, session, "target", observer));
            clock.Paused = 0; em.SetComponentData(session, clock);
            Step(world, 20.9, .1f); Assert.IsTrue(TryRecord(em, session, "target", observer, out _));
            Step(world, 21, .1f); Assert.IsFalse(TryRecord(em, session, "target", observer, out _));
            Assert.AreEqual(0, em.GetBuffer<SkirmishContactLedgerElement>(session).Length);
        }

        [Test]
        public void NativeObservedDeathDoesNotResurrectContact()
        {
            using var world = new World(nameof(NativeObservedDeathDoesNotResurrectContact));
            var em = world.EntityManager; Entity session = Session(em);
            Actor(em, "observer", 1, 0, 5); Entity target = Actor(em, "target", 2, 4, 0);
            Step(world, 1, .1f); Assert.IsTrue(TryRecord(em, session, "target", 1, out _));
            em.SetComponentData(target, new UnitHealth { Current = 0, Max = 100 });
            UnitDeathSystem.FinalizeDeath(em, Entity.Null, target, 1, 0);
            Assert.IsFalse(em.Exists(target));
            Assert.IsFalse(TryRecord(em, session, "target", 1, out _), "Native observed death removes memory before entity destruction.");
            Step(world, 2, .1f); Assert.IsFalse(TryRecord(em, session, "target", 1, out _));
        }

        [Test]
        public void NativeHiddenDeathPreservesLastObservedAliveContact()
        {
            using var world = new World(nameof(NativeHiddenDeathPreservesLastObservedAliveContact));
            var em = world.EntityManager; Entity session = Session(em);
            Actor(em, "observer", 1, 0, 5); Entity target = Actor(em, "target", 2, 4, 0);
            Step(world, 1, .1f); var before = Record(em, session, "target", 1);
            Move(em, target, 100); Step(world, 2, .1f);
            em.SetComponentData(target, new UnitHealth { Current = 0, Max = 100 });
            UnitDeathSystem.FinalizeDeath(em, Entity.Null, target, 2, 0);
            Step(world, 3, .1f);
            var after = Record(em, session, "target", 1); AssertFrozen(before, after);
            Assert.AreEqual(100, after.LastSeenHealth);
            Assert.AreEqual(SkirmishContactSight.LastSeen, after.Sight);
        }

        [Test]
        public void StaleVisibleCannotRevealSameTickHiddenNativeDeath()
        {
            using var world = new World(nameof(StaleVisibleCannotRevealSameTickHiddenNativeDeath));
            var em = world.EntityManager; Entity session = Session(em);
            Actor(em, "observer", 1, 0, 5); Entity target = Actor(em, "target", 2, 4, 0);
            Step(world, 1, .1f); var before = Record(em, session, "target", 1);
            Move(em, target, 100);
            em.SetComponentData(target, new UnitHealth { Current = 0, Max = 100 });
            // Exact native scheduling edge: no intervening shared-fog tick. The
            // old Visible sample cannot prove that this now-hidden death was seen.
            UnitDeathSystem.FinalizeDeath(em, Entity.Null, target, 1, 0);
            Assert.IsFalse(em.Exists(target));
            AssertFrozen(before, Record(em, session, "target", 1));
            Step(world, 2, .1f);
            var retained = Record(em, session, "target", 1);
            Assert.AreEqual(SkirmishContactSight.LastSeen, retained.Sight);
            AssertFrozen(before, retained);
        }

        [Test]
        public void NativeScanEmissionSurvivesDestructionBeforeNextFogTick()
        {
            using var world = new World(nameof(NativeScanEmissionSurvivesDestructionBeforeNextFogTick));
            var em = world.EntityManager; Entity session = Session(em);
            Entity observer = Actor(em, "scanner", 1, 0, 0), target = Actor(em, "target", 2, 20, 0);
            Step(world, 1, .1f); Assert.IsFalse(TryRecord(em, session, "target", 1, out _));
            ScanIntelCommandSystem.EnqueueScan(em, 901, 42, new int2(20, 0), new float3(20, 0, 0), observer, true, false, 4);
            world.GetOrCreateSystem<ScanIntelCommandSystem>().Update(world.Unmanaged); em.CompleteAllTrackedJobs();
            var actual = Record(em, session, "target", 1);
            Assert.AreEqual((byte)2, actual.ObservationKind);
            Assert.AreEqual(901, actual.ScanRequestId); Assert.AreEqual(42, actual.ObservationFrame);
            Assert.AreEqual(new FixedString64Bytes("scanner"), actual.ObservationSourceStableObjectId);
            em.DestroyEntity(target); Step(world, 2, .1f);
            AssertFrozen(actual, Record(em, session, "target", 1));
        }

        [Test]
        public void SessionCheckpointRetainsDestroyedContactAndRebasesRemainingAge()
        {
            using var world = new World(nameof(SessionCheckpointRetainsDestroyedContactAndRebasesRemainingAge));
            var em = world.EntityManager; Entity session = Session(em);
            Entity observer = Actor(em, "scanner", 1, 0, 0), target = Actor(em, "target", 2, 20, 0);
            Step(world, 1, .1f);
            ScanIntelCommandSystem.EnqueueScan(em, 902, 43, new int2(20, 0), new float3(20, 0, 0), observer, true, false, 4);
            world.GetOrCreateSystem<ScanIntelCommandSystem>().Update(world.Unmanaged); em.CompleteAllTrackedJobs();
            em.DestroyEntity(target); em.DestroyEntity(observer); Step(world, 2, .1f);
            var before = Record(em, session, "target", 1);
            var parts = SkirmishNativeCheckpointParts.Capture(em, session, entity => entity == session ? "session" : null);
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(parts, new HashSet<string> { "session" }));
            Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadBuffer<SkirmishContactLedgerElement>(parts, out var values));
            Assert.AreEqual(1, values.Length);
            var rebased = SkirmishNativeCheckpointParts.RebaseClocks(parts, 100, 0, 10);
            using var resumed = new World("ledger-resumed"); var fresh = resumed.EntityManager;
            Entity restoredSession = Session(fresh);
            SkirmishNativeCheckpointParts.Apply(fresh, restoredSession, rebased, id => id == "session" ? restoredSession : Entity.Null);
            var restored = Record(fresh, restoredSession, "target", 1);
            Assert.AreEqual(before.LastSeenPosition, restored.LastSeenPosition);
            Assert.AreEqual(before.LastSeenHealth, restored.LastSeenHealth);
            Assert.AreEqual(before.LastObservedTimeSeconds + 100, restored.LastObservedTimeSeconds);
            Assert.AreEqual(before.ObservationFrame + 10, restored.ObservationFrame);
            Step(resumed, 102, 0); Assert.AreEqual(restored.LastObservedTimeSeconds, Record(fresh, restoredSession, "target", 1).LastObservedTimeSeconds);
            Step(resumed, 120.9, .1f); Assert.IsTrue(TryRecord(fresh, restoredSession, "target", 1, out _));
            Step(resumed, 121, .1f); Assert.IsFalse(TryRecord(fresh, restoredSession, "target", 1, out _));
            var oldParts = Array.FindAll(parts, p => p.Type != typeof(SkirmishContactLedgerElement).FullName && p.Type != typeof(SkirmishContactLedgerStateComponent).FullName);
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(oldParts, new HashSet<string> { "session" }), "Older files can omit both optional records.");
        }

        [Test]
        public void ReusedAttemptCannotInheritPriorContactMemory()
        {
            using var world = new World(nameof(ReusedAttemptCannotInheritPriorContactMemory));
            var em = world.EntityManager; Entity session = Session(em);
            Actor(em, "observer", 1, 0, 5); Actor(em, "target", 2, 4, 0); Step(world, 1, .1f);
            Assert.IsTrue(TryRecord(em, session, "target", 1, out _));
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.SessionId = new FixedString64Bytes("replacement-attempt"); em.SetComponentData(session, state);
            Assert.IsFalse(TryRecord(em, session, "target", 1, out _));
            Step(world, 2, .1f); Assert.AreEqual(0, em.GetBuffer<SkirmishContactLedgerElement>(session).Length);
        }

        [Test]
        public void NativeSessionCleanupRemovesLedger()
        {
            using var world = new World(nameof(NativeSessionCleanupRemovesLedger));
            var em = world.EntityManager; Entity session = Session(em);
            Actor(em, "observer", 1, 0, 5); Actor(em, "target", 2, 4, 0); Step(world, 1, .1f);
            Assert.IsTrue(TryRecord(em, session, "target", 1, out _));
            em.AddComponent<SkirmishExpandedCleanupRequest>(session);
            world.GetOrCreateSystem<SkirmishSessionCleanupSystem>().Update(world.Unmanaged); em.CompleteAllTrackedJobs();
            Assert.IsFalse(em.HasBuffer<SkirmishContactLedgerElement>(session));
            Assert.IsFalse(em.HasComponent<SkirmishContactLedgerStateComponent>(session));
        }

        private static Entity Session(EntityManager em)
        {
            Entity grid = em.CreateEntity(typeof(GridConfig)); em.SetComponentData(grid, new GridConfig { Width = 200, Height = 100, CellSize = 1 });
            Entity session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishFogStateComponent), typeof(SkirmishObjectiveClockComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = new FixedString64Bytes(Attempt), Phase = SkirmishSessionPhase.Playing });
            em.SetComponentData(session, new SkirmishFogStateComponent { SharedFog = 1, LastSeenExpireSeconds = 20 }); return session;
        }
        private static Entity Actor(EntityManager em, string id, byte faction, float x, int radius)
        {
            Entity entity = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(Faction), typeof(UnitGrid), typeof(LocalTransform), typeof(UnitHealth), typeof(UnitCombat), typeof(UnitMove));
            em.SetComponentData(entity, new SkirmishAttemptOwnedComponent { SessionId = new FixedString64Bytes(Attempt), StableObjectId = new FixedString64Bytes(id), FactionId = faction });
            em.SetComponentData(entity, new Faction { Id = faction }); em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AggroRangeCells = radius });
            em.SetComponentData(entity, new UnitMove { WalkSpeed = 6, RoadSpeedMultiplier = 1, ArriveDistance = .1f }); Move(em, entity, x); return entity;
        }
        private static void Move(EntityManager em, Entity entity, float x)
        { em.SetComponentData(entity, LocalTransform.FromPosition(new float3(x, 0, 0))); em.SetComponentData(entity, new UnitGrid { Cell = new int2((int)x, 0) }); }
        private static void Step(World world, double elapsed, float delta)
        { world.SetTime(new TimeData(elapsed, delta)); world.GetOrCreateSystem<SkirmishSharedFogUpdateSystem>().Update(world.Unmanaged); world.EntityManager.CompleteAllTrackedJobs(); }
        private static bool TryRecord(EntityManager em, Entity session, string id, byte observer, out SkirmishContactLedgerElement value)
            => SkirmishFogService.TryGetLedger(em, session, new FixedString64Bytes(id), observer, out value);
        private static SkirmishContactLedgerElement Record(EntityManager em, Entity session, string id, byte observer)
        { Assert.IsTrue(TryRecord(em, session, id, observer, out var value), "Expected actual observed ledger contact."); return value; }
        private static void AssertFrozen(SkirmishContactLedgerElement expected, SkirmishContactLedgerElement actual)
        {
            Assert.AreEqual(expected.ContactStableObjectId, actual.ContactStableObjectId); Assert.AreEqual(expected.ContactFactionId, actual.ContactFactionId);
            Assert.AreEqual(expected.ObserverFactionId, actual.ObserverFactionId); Assert.AreEqual(expected.LastSeenPosition, actual.LastSeenPosition);
            Assert.AreEqual(expected.LastSeenCell, actual.LastSeenCell); Assert.AreEqual(expected.LastSeenHealth, actual.LastSeenHealth); Assert.AreEqual(expected.LastSeenMax, actual.LastSeenMax);
            Assert.AreEqual(expected.LastObservedTimeSeconds, actual.LastObservedTimeSeconds); Assert.AreEqual(expected.ObservationKind, actual.ObservationKind);
            Assert.AreEqual(expected.ScanRequestId, actual.ScanRequestId); Assert.AreEqual(expected.ObservationFrame, actual.ObservationFrame);
            Assert.AreEqual(expected.ObservationSourceStableObjectId, actual.ObservationSourceStableObjectId);
        }
    }
}
