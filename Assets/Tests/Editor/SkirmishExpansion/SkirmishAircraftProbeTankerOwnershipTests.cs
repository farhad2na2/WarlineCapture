using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.TestTools;
using Game.Components;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishAircraftProbeTankerOwnershipTests
    {
        private static Entity Session(EntityManager em, string id = "current-attempt")
        {
            var root = em.CreateEntity();
            em.AddComponentData(root, new SkirmishExpandedSessionComponent {
                SessionId = id, CatalogId = "S003", DefinitionId = "skirmish.s003",
                ContentVersion = 2, SetupHash = 1234, Seed = 104732,
                Phase = SkirmishSessionPhase.Playing });
            em.AddBuffer<SkirmishProductionReservation>(root);
            return root;
        }

        private static Entity Tanker(EntityManager em, string id = "current-attempt")
        {
            var actor = em.CreateEntity();
            em.AddComponentData(actor, new SkirmishAttemptOwnedComponent {
                SessionId = id, FactionId = 1, StableObjectId = "starting-tanker", ReservationId = 0 });
            em.AddComponentData(actor, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Tanker });
            em.AddComponentData(actor, new Faction { Id = 1 });
            em.AddComponentData(actor, new UnitHealth { Current = 100 });
            return actor;
        }

        private static SkirmishAircraftProbeTankerOwner Capture(World world, Entity root)
        {
            Assert.IsTrue(SkirmishAircraftProbeTankerOwner.TryCapture(world, root, out var owner));
            return owner;
        }

        private static SkirmishProductionReservation Pending(SkirmishReservationPhase phase) =>
            new SkirmishProductionReservation { ReservationId = 7, Role = SkirmishRoleKind.Tanker,
                FactionId = 1, MemberCount = 1, RemainingMembers = 1, MaterialsPaid = 140, Phase = phase };

        [Test]
        public void AcceptsCurrentStartingTankerWithoutPaidReservation()
        {
            using var world = new World("ProbeCurrentTanker");
            var em = world.EntityManager; var root = Session(em); Tanker(em);
            Assert.IsTrue(Capture(world, root).HasCommittedTanker(world));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)]
        public void RejectsForeignStaleOrNonLivingActor(int mutation)
        {
            using var world = new World("ProbeInvalidTanker");
            var em = world.EntityManager; var root = Session(em); var actor = Tanker(em);
            var owner = Capture(world, root);
            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            switch (mutation)
            {
                case 0: owned.SessionId = "old-attempt"; break;
                case 1: owned.FactionId = 2; break;
                case 2: owned.IsStructure = 1; break;
                case 3: owned.CapacityReleased = 1; break;
                case 4: em.SetComponentData(actor, new Faction { Id = 2 }); break;
                case 5: em.SetComponentData(actor, new UnitHealth { Current = 0 }); break;
                case 6: em.SetComponentData(actor, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.LogisticsTruck }); break;
                case 7: em.RemoveComponent<UnitHealth>(actor); break;
                case 8: em.RemoveComponent<SkirmishAttemptOwnedComponent>(actor); break;
                case 9: em.DestroyEntity(actor); break;
            }
            if (mutation != 8 && mutation != 9) em.SetComponentData(actor, owned);
            Assert.IsFalse(owner.HasCommittedTanker(world));
        }

        [TestCase(SkirmishReservationPhase.Reserved)]
        [TestCase(SkirmishReservationPhase.Producing)]
        public void AcceptsOnlyPendingReceiptOnExactCurrentRoot(SkirmishReservationPhase phase)
        {
            using var world = new World("ProbePendingTanker");
            var em = world.EntityManager; var root = Session(em); var other = Session(em, "other-attempt");
            var owner = Capture(world, root);
            em.GetBuffer<SkirmishProductionReservation>(other).Add(Pending(phase));
            Assert.IsFalse(owner.HasCommittedTanker(world), "Other root's pending tanker is not this attempt's supply");
            em.GetBuffer<SkirmishProductionReservation>(root).Add(Pending(phase));
            Assert.IsTrue(owner.HasCommittedTanker(world));
        }

        [Test]
        public void RejectsTerminalReceiptsAndEmptyDeliveredOrForeignOrders()
        {
            using var world = new World("ProbeInvalidReceipts");
            var em = world.EntityManager; var root = Session(em); var owner = Capture(world, root);
            var rows = em.GetBuffer<SkirmishProductionReservation>(root);
            foreach (SkirmishReservationPhase phase in Enum.GetValues(typeof(SkirmishReservationPhase)))
            {
                if (phase == SkirmishReservationPhase.Reserved || phase == SkirmishReservationPhase.Producing) continue;
                rows.Clear(); rows.Add(Pending(phase)); Assert.IsFalse(owner.HasCommittedTanker(world), phase.ToString());
            }
            for (int i = 0; i < 5; i++)
            {
                var row = Pending(SkirmishReservationPhase.Producing);
                if (i == 0) row.MemberCount = 0;
                if (i == 1) row.DeliveredMembers = 1;
                if (i == 2) row.DeliveredMembers = -1;
                if (i == 3) row.FactionId = 2;
                if (i == 4) row.Role = SkirmishRoleKind.LogisticsTruck;
                rows.Clear(); rows.Add(row); Assert.IsFalse(owner.HasCommittedTanker(world), "Invalid receipt " + i);
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)]
        public void RejectsReplacedIdentityOrEndedSessionWithoutRebinding(int mutation)
        {
            using var world = new World("ProbeStaleRoot");
            var em = world.EntityManager; var root = Session(em); Tanker(em);
            var owner = Capture(world, root); var current = em.GetComponentData<SkirmishExpandedSessionComponent>(root);
            switch (mutation)
            {
                case 0: current.SessionId = "new-attempt"; break;
                case 1: current.SetupHash++; break;
                case 2: current.Seed++; break;
                case 3: current.ContentVersion++; break;
                case 4: current.DefinitionId = "skirmish.s004"; break;
                case 5: current.CatalogId = "S004"; break;
                case 6: current.Phase = SkirmishSessionPhase.Finished; break;
                case 7: current.Phase = SkirmishSessionPhase.Failed; break;
                case 8: current.Phase = SkirmishSessionPhase.Cleaning; break;
                case 9: em.RemoveComponent<SkirmishExpandedSessionComponent>(root); break;
            }
            if (mutation != 9) em.SetComponentData(root, current);
            Assert.IsFalse(owner.Matches(world, root)); Assert.IsFalse(owner.HasCommittedTanker(world));
            var restored = new SkirmishExpandedSessionComponent { SessionId = "current-attempt", CatalogId = "S003",
                DefinitionId = "skirmish.s003", ContentVersion = 2, SetupHash = 1234, Seed = 104732,
                Phase = SkirmishSessionPhase.Playing };
            if (mutation == 9) em.AddComponentData(root, restored); else em.SetComponentData(root, restored);
            Assert.IsFalse(owner.HasCommittedTanker(world), "Observed invalidity cannot be undone by restoring old text/fields");
            var replacement = Session(em); Tanker(em);
            Assert.IsFalse(owner.Matches(world, replacement), "Do not adopt a later root with a reused text id");
            Assert.IsFalse(owner.HasCommittedTanker(world));
        }

        [Test]
        public void RejectsAnotherWorldWithReplayedEntityIndexVersionAndSessionText()
        {
            using var original = new World("ProbeOriginalWorld");
            using var foreign = new World("ProbeForeignWorld");
            var root = Session(original.EntityManager); var foreignRoot = Session(foreign.EntityManager);
            // Replay the identical handle into a different live World. Do not assume
            // a particular Entities allocator assigns equal indices to fresh Worlds.
            var alias = new Entity { Index = root.Index, Version = root.Version };
            Assert.AreEqual(root, alias);
            Assert.IsTrue(foreign.EntityManager.Exists(foreignRoot));
            Tanker(original.EntityManager); Tanker(foreign.EntityManager);
            var owner = Capture(original, root);
            Assert.IsTrue(owner.HasCommittedTanker(original));
            Assert.IsFalse(owner.Matches(foreign, alias)); Assert.IsFalse(owner.HasCommittedTanker(foreign));
            Assert.IsFalse(owner.HasCommittedTanker(null));
        }

        [Test]
        public void RejectsDestroyedRootAndDisposedWorldWithoutReadingDisposedManager()
        {
            var world = new World("ProbeDisposedWorld");
            try
            {
                var em = world.EntityManager; var root = Session(em); Tanker(em); var owner = Capture(world, root);
                em.DestroyEntity(root);
                var replacement = Session(em);
                Assert.AreNotEqual(root, replacement, "Recreated root cannot retain exact Entity identity");
                Assert.IsFalse(owner.Matches(world, replacement)); Assert.IsFalse(owner.HasCommittedTanker(world));
                world.Dispose();
                Assert.IsFalse(owner.Matches(world, root)); Assert.IsFalse(owner.HasCommittedTanker(world));
                Assert.IsFalse(SkirmishAircraftProbeTankerOwner.TryCapture(world, replacement, out _));
            }
            finally { if (world.IsCreated) world.Dispose(); }
        }

        [Test]
        public void DirectWorldDisposalRejectsWithoutPriorInvalidation()
        {
            var world = new World("ProbeDirectDispose");
            try
            {
                var root = Session(world.EntityManager); Tanker(world.EntityManager);
                var owner = Capture(world, root); world.Dispose();
                Assert.IsFalse(owner.HasCommittedTanker(world));
                Assert.IsFalse(owner.Matches(world, root));
            }
            finally { if (world.IsCreated) world.Dispose(); }
        }

        [Test]
        public void CaptureRequiresActualPlayingRootAndDoesNotAdoptLaterContext()
        {
            using var world = new World("ProbeCaptureBoundary"); var em = world.EntityManager; var root = Session(em);
            Assert.IsFalse(SkirmishAircraftProbeTankerOwner.TryCapture(null, root, out _));
            Assert.IsFalse(SkirmishAircraftProbeTankerOwner.TryCapture(world, Entity.Null, out _));
            var current = em.GetComponentData<SkirmishExpandedSessionComponent>(root);
            foreach (SkirmishSessionPhase phase in Enum.GetValues(typeof(SkirmishSessionPhase)))
            {
                if (phase == SkirmishSessionPhase.Playing) continue;
                current.Phase = phase; em.SetComponentData(root, current);
                Assert.IsFalse(SkirmishAircraftProbeTankerOwner.TryCapture(world, root, out _), phase.ToString());
            }
            current.Phase = SkirmishSessionPhase.Playing; current.SessionId = default; em.SetComponentData(root, current);
            Assert.IsFalse(SkirmishAircraftProbeTankerOwner.TryCapture(world, root, out _));
        }

        [Test]
        public void ObservationLeavesResourcesReservationsAndInputEvidenceUnchanged()
        {
            using var world = new World("ProbeReadOnlyTanker"); var em = world.EntityManager; var root = Session(em);
            em.AddComponentData(root, new SkirmishEconomyStockComponent { FactionId = 1, Materials = 450, Oil = 120, Fuel = 350 });
            var rows = em.GetBuffer<SkirmishProductionReservation>(root); rows.Add(Pending(SkirmishReservationPhase.Reserved));
            var owner = Capture(world, root); var defaultWorld = World.DefaultGameObjectInjectionWorld;
            float timeScale = Time.timeScale; int commands = AriaCommandEvidence.AcceptedCommands;
            int violations = AriaCommandEvidence.Violations;
            for (int i = 0; i < 10; i++) Assert.IsTrue(owner.HasCommittedTanker(world));
            var stock = em.GetComponentData<SkirmishEconomyStockComponent>(root);
            Assert.AreEqual(450, stock.Materials); Assert.AreEqual(120, stock.Oil); Assert.AreEqual(350, stock.Fuel);
            var receipt = em.GetBuffer<SkirmishProductionReservation>(root, true)[0];
            Assert.AreEqual(1, receipt.MemberCount); Assert.AreEqual(0, receipt.DeliveredMembers);
            Assert.AreEqual(140, receipt.MaterialsPaid); Assert.AreEqual(SkirmishReservationPhase.Reserved, receipt.Phase);
            Assert.AreSame(defaultWorld, World.DefaultGameObjectInjectionWorld); Assert.AreEqual(timeScale, Time.timeScale);
            Assert.AreEqual(commands, AriaCommandEvidence.AcceptedCommands); Assert.AreEqual(violations, AriaCommandEvidence.Violations);
        }

        private sealed class ParentDispatchFixture : IDisposable
        {
            internal const string Active = "Warline.S002.AriaRun";
            internal const string Artifact = Active + ".artifactId";
            internal const string Pending = "Warline.Skirmish.ValidationQuitPending";
            internal const string Code = "Warline.Skirmish.ValidationQuitCode";
            internal const string Delegated = "Warline.S003.AircraftProbe.ParentArtifact";
            internal static readonly string[] ParentMethods = { "Tick", "PollFinishSourceHash", "FinishQuitAfterPlayMode" };
            private static readonly Type Harness = typeof(SkirmishS002AriaRunHarness);
            private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
            private readonly Dictionary<string, object> saved = new Dictionary<string, object>();
            private readonly Delegate[] originalCallbacks;
            private readonly string originalArtifact, originalDelegated;
            private readonly bool originalActive, originalPending;
            private readonly int originalCode;
            private readonly EditorApplication.CallbackFunction unrelated;
            internal readonly string Owner = Guid.NewGuid().ToString("N");
            internal int UnrelatedDispatches;

            internal static Delegate[] OwnedCallbacks() =>
                (EditorApplication.update?.GetInvocationList() ?? Array.Empty<Delegate>())
                    .Where(d => d.Method.DeclaringType == Harness && ParentMethods.Contains(d.Method.Name)).ToArray();

            internal static object Call(string name, params object[] args)
            {
                var method = Harness.GetMethod(name, Flags);
                Assert.IsNotNull(method, name);
                try { return method.Invoke(null, args); }
                catch (TargetInvocationException error) { throw error.InnerException ?? error; }
            }

            internal static void Set(string name, object value) => Harness.GetField(name, Flags).SetValue(null, value);
            internal static object Get(string name) => Harness.GetField(name, Flags).GetValue(null);
            private static EditorApplication.CallbackFunction Callback(string name) =>
                (EditorApplication.CallbackFunction)Delegate.CreateDelegate(typeof(EditorApplication.CallbackFunction), Harness.GetMethod(name, Flags));

            internal ParentDispatchFixture()
            {
                Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode, "Idle callback fixture only");
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(Harness.TypeHandle);
                Assert.IsFalse(SessionState.GetBool(Active, false), "Never borrow an active harness for fixture controls");
                Assert.IsFalse(SessionState.GetBool(Pending, false), "Never borrow another run's pending quit callbacks");
                originalCallbacks = OwnedCallbacks();
                originalArtifact = SessionState.GetString(Artifact, ""); originalDelegated = SessionState.GetString(Delegated, "");
                originalActive = SessionState.GetBool(Active, false); originalPending = SessionState.GetBool(Pending, false);
                originalCode = SessionState.GetInt(Code, 2);
                foreach (string name in new[] { "airProbeTankerOwner", "airProbeTankerCaptureAttempted", "airProbeHarnessArtifactId",
                    "helipadProbeAircraftCycle", "helipadProbeStage", "helipadProbeTouch", "deadline", "stage" }) saved[name] = Get(name);
                foreach (string name in ParentMethods) { EditorApplication.update -= Callback(name); EditorApplication.update += Callback(name); }
                SessionState.SetString(Artifact, Owner); SessionState.SetBool(Active, true);
                SessionState.SetBool(Pending, true); SessionState.SetInt(Code, 0);
                SessionState.SetString(Delegated, Owner);
                Set("airProbeTankerOwner", null); Set("airProbeTankerCaptureAttempted", false);
                Set("airProbeHarnessArtifactId", Owner); Set("helipadProbeAircraftCycle", true);
                Set("helipadProbeStage", 1); Set("helipadProbeTouch", null);
                Set("deadline", EditorApplication.timeSinceStartup - 1d);
                unrelated = () => UnrelatedDispatches++;
                EditorApplication.update += unrelated;
                Assert.AreEqual(3, OwnedCallbacks().Length, "Install the actual parent callbacks, including an already-expired timeout");
            }

            internal void AssertRetiredAndDispatch()
            {
                Assert.AreEqual(0, OwnedCallbacks().Length, "No actual Tick/finish/quit callback may remain dispatchable");
                Assert.IsFalse(SessionState.GetBool(Active, false), "Static constructor cannot resume Tick after reload");
                Assert.IsFalse(SessionState.GetBool(Pending, false), "Static constructor cannot resume quit after reload");
                int commands = AriaCommandEvidence.AcceptedCommands, violations = AriaCommandEvidence.Violations;
                var world = World.DefaultGameObjectInjectionWorld; float scale = Time.timeScale;
                object finishing = Get("finishingEvidence"), hashTask = Get("finishSourceHash");
                // Dispatch only this fixture's remaining owned callback slice; never
                // invoke unrelated global Editor update handlers from a test.
                int dispatched = 0;
                foreach (var callback in OwnedCallbacks()) { callback.DynamicInvoke(); dispatched++; }
                Assert.AreEqual(0, dispatched, "Expired timeout, Finish and UI cleanup never execute after retirement");
                Assert.AreEqual(finishing, Get("finishingEvidence")); Assert.AreSame(hashTask, Get("finishSourceHash"));
                Assert.AreEqual(commands, AriaCommandEvidence.AcceptedCommands); Assert.AreEqual(violations, AriaCommandEvidence.Violations);
                Assert.AreSame(world, World.DefaultGameObjectInjectionWorld); Assert.AreEqual(scale, Time.timeScale);
                Assert.IsTrue((EditorApplication.update?.GetInvocationList() ?? Array.Empty<Delegate>()).Contains(unrelated));
                unrelated(); Assert.AreEqual(1, UnrelatedDispatches, "Unrelated callback remains installed and callable");
            }

            public void Dispose()
            {
                foreach (string name in ParentMethods) EditorApplication.update -= Callback(name);
                foreach (var callback in originalCallbacks) EditorApplication.update += (EditorApplication.CallbackFunction)callback;
                EditorApplication.update -= unrelated;
                foreach (var pair in saved) Set(pair.Key, pair.Value);
                SessionState.SetString(Artifact, originalArtifact); SessionState.SetString(Delegated, originalDelegated);
                SessionState.SetBool(Active, originalActive); SessionState.SetBool(Pending, originalPending); SessionState.SetInt(Code, originalCode);
            }
        }

        [Test]
        public void InitialCaptureFailureRetiresActualExpiredParentCallbacks()
        {
            using var fixture = new ParentDispatchFixture();
            string prior = Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_AIRCRAFT_CYCLE");
            try
            {
                Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_AIRCRAFT_CYCLE", "1");
                LogAssert.Expect(LogType.Error, "[SkirmishS003AircraftCycleProbe] result=Failed reason=tankerOwnerCaptureRejected");
                ParentDispatchFixture.Call("StartNativeHelipadProbe", Entity.Null);
                fixture.AssertRetiredAndDispatch();
            }
            finally { Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_AIRCRAFT_CYCLE", prior); }
        }

        [Test]
        public void LostWorldFrameRetiresActualExpiredParentCallbacksBeforeInput()
        {
            using var fixture = new ParentDispatchFixture();
            using var world = new World("ProbeLostDispatchOwner"); var root = Session(world.EntityManager);
            Assert.AreNotSame(world, World.DefaultGameObjectInjectionWorld);
            ParentDispatchFixture.Set("airProbeTankerOwner", Capture(world, root));
            ParentDispatchFixture.Set("airProbeTankerCaptureAttempted", true);
            LogAssert.Expect(LogType.Error, "[SkirmishS003AircraftCycleProbe] result=Failed reason=tankerOwnerContextChanged");
            ParentDispatchFixture.Call("TickHelipadPlayerFrame", root, fixture.Owner);
            fixture.AssertRetiredAndDispatch();
        }

        [Test]
        public void StalePlayerFrameCannotRetireReplacementHarnessOrInvalidateItsOwner()
        {
            using var fixture = new ParentDispatchFixture();
            using var world = new World("ProbeReplacementDispatchOwner"); var root = Session(world.EntityManager);
            var replacement = Capture(world, root); string later = Guid.NewGuid().ToString("N");
            ParentDispatchFixture.Set("airProbeTankerOwner", replacement);
            ParentDispatchFixture.Set("airProbeHarnessArtifactId", later);
            SessionState.SetString(ParentDispatchFixture.Artifact, later);
            ParentDispatchFixture.Call("TickHelipadPlayerFrame", root, fixture.Owner);
            Assert.AreEqual(3, ParentDispatchFixture.OwnedCallbacks().Length);
            Assert.IsTrue(SessionState.GetBool(ParentDispatchFixture.Active, false));
            Assert.IsTrue(SessionState.GetBool(ParentDispatchFixture.Pending, false));
            Assert.IsTrue(replacement.Matches(world, root), "Stale frame never reaches the later owner or its UI");
        }

        [TestCase(false)] [TestCase(true)]
        public void ReloadResumeGuardRetiresOnlyMatchingArtifact(bool laterLaunch)
        {
            using var fixture = new ParentDispatchFixture();
            if (laterLaunch) SessionState.SetString(ParentDispatchFixture.Artifact, Guid.NewGuid().ToString("N"));
            Assert.AreEqual(!laterLaunch, (bool)ParentDispatchFixture.Call("RetireAircraftProbeParentAfterReload"));
            if (!laterLaunch) fixture.AssertRetiredAndDispatch();
            else
            {
                Assert.AreEqual(3, ParentDispatchFixture.OwnedCallbacks().Length);
                Assert.IsTrue(SessionState.GetBool(ParentDispatchFixture.Active, false));
                Assert.IsTrue(SessionState.GetBool(ParentDispatchFixture.Pending, false));
            }
        }

        [Test]
        public void SuccessfulProbeHandoffSuspendsParentBeforeAnyDeadlineDispatch()
        {
            using var fixture = new ParentDispatchFixture();
            Assert.IsTrue((bool)ParentDispatchFixture.Call("SuspendAircraftProbeParent", fixture.Owner));
            Assert.AreEqual(0, ParentDispatchFixture.OwnedCallbacks().Length);
            Assert.IsTrue(SessionState.GetBool(ParentDispatchFixture.Active, false), "Keep owned input evidence enabled during the active probe");
            Assert.AreEqual(fixture.Owner, SessionState.GetString(ParentDispatchFixture.Delegated, ""));
        }

        private static IDisposable BeginDirectLogScope(out Type scopeType)
        {
            // Direct executeMethod does not install NUnit's log scope. Preserve
            // strict expected/error checks rather than ignoring failing messages.
            scopeType = typeof(LogAssert).Assembly.GetType("UnityEngine.TestTools.Logging.LogScope", true);
            bool hasScope = (bool)scopeType.GetMethod("HasCurrentLogScope").Invoke(null, null);
            return hasScope ? null : (IDisposable)Activator.CreateInstance(scopeType, true);
        }

        public static void RunFocusedValidation()
        {
            var logScope = BeginDirectLogScope(out var logScopeType);
            try
            {
                Assert.IsFalse(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode, "Idle synthetic-world controls only");
                var test = new SkirmishAircraftProbeTankerOwnershipTests();
                test.AcceptsCurrentStartingTankerWithoutPaidReservation();
                for (int i = 0; i < 10; i++) test.RejectsForeignStaleOrNonLivingActor(i);
                test.AcceptsOnlyPendingReceiptOnExactCurrentRoot(SkirmishReservationPhase.Reserved);
                test.AcceptsOnlyPendingReceiptOnExactCurrentRoot(SkirmishReservationPhase.Producing);
                test.RejectsTerminalReceiptsAndEmptyDeliveredOrForeignOrders();
                for (int i = 0; i < 10; i++) test.RejectsReplacedIdentityOrEndedSessionWithoutRebinding(i);
                test.RejectsAnotherWorldWithReplayedEntityIndexVersionAndSessionText();
                test.RejectsDestroyedRootAndDisposedWorldWithoutReadingDisposedManager();
                test.DirectWorldDisposalRejectsWithoutPriorInvalidation();
                test.CaptureRequiresActualPlayingRootAndDoesNotAdoptLaterContext();
                test.ObservationLeavesResourcesReservationsAndInputEvidenceUnchanged();
                test.InitialCaptureFailureRetiresActualExpiredParentCallbacks();
                test.LostWorldFrameRetiresActualExpiredParentCallbacksBeforeInput();
                test.StalePlayerFrameCannotRetireReplacementHarnessOrInvalidateItsOwner();
                test.ReloadResumeGuardRetiresOnlyMatchingArtifact(false);
                test.ReloadResumeGuardRetiresOnlyMatchingArtifact(true);
                test.SuccessfulProbeHandoffSuspendsParentBeforeAnyDeadlineDispatch();
                if (logScope != null)
                    logScopeType.GetMethod("EvaluateLogScope").Invoke(logScope, new object[] { true });
                Debug.Log("[SkirmishAircraftProbeTankerOwnership] result=Passed fixture=SyntheticWorldAndOwnedEditorDispatch normalMissionReady=0");
            }
            finally { logScope?.Dispose(); }
        }
    }
}
