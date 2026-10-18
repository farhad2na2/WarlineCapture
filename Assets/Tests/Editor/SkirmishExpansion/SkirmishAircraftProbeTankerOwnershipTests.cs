using System;
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

        public static void RunFocusedValidation()
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
            Debug.Log("[SkirmishAircraftProbeTankerOwnership] result=Passed fixture=SyntheticWorld normalMissionReady=0");
        }
    }
}
