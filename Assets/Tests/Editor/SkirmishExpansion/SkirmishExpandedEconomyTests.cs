using System;
using PaidHarness = Game.Editor.SkirmishS002AriaRunHarness;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishExpandedEconomyTests
    {
        private static Newtonsoft.Json.Linq.JArray paidDtoSamples;
        private static Newtonsoft.Json.Linq.JObject PaidDtoRoundTrip(Newtonsoft.Json.Linq.JObject value)
        {
            foreach (var scalar in System.Linq.Enumerable.OfType<Newtonsoft.Json.Linq.JValue>(value.Descendants()))
                if (scalar.Type == Newtonsoft.Json.Linq.JTokenType.Float)
                {
                    double number = Convert.ToDouble(scalar.Value, System.Globalization.CultureInfo.InvariantCulture);
                    Assert.IsFalse(double.IsNaN(number) || double.IsInfinity(number), "Finite fixture DTO numbers required");
                }
            string encoded = PaidHarness.SerializeProductionNativeRecord(value);
            var decoded = Newtonsoft.Json.Linq.JObject.Parse(encoded);
            Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(value, decoded), "Exact installed native JSON token roundtrip required");
            return decoded;
        }
        private static void PaidDtoSample(string family, string operation,
            Newtonsoft.Json.Linq.JObject before, Newtonsoft.Json.Linq.JObject after,
            Newtonsoft.Json.Linq.JObject decision, Newtonsoft.Json.Linq.JObject actor)
        {
            var sample = new Newtonsoft.Json.Linq.JObject {
                ["fixture_family"] = family, ["operation"] = operation,
                ["before"] = before, ["after"] = after, ["decision"] = decision, ["actor"] = actor,
                ["normalEnvelopeExercised"] = false, ["normalJournal"] = false, ["paid_resources"] = false };
            var decoded = PaidDtoRoundTrip(sample);
            Assert.AreEqual(Newtonsoft.Json.Linq.JTokenType.Object, decoded["before"]["bank"].Type);
            Assert.AreEqual(Newtonsoft.Json.Linq.JTokenType.Integer, decoded["after"]["bank"]["Current"].Type);
            if (paidDtoSamples != null) paidDtoSamples.Add(decoded);
        }
        private static void PaidProductionDtoRows(
            System.Collections.Generic.List<SkirmishProductionService.ProductionLedgerObservation> rows)
        {
            foreach (var row in rows)
            {
                var before = PaidHarness.PaidPoint(row.Before);
                var after = PaidHarness.PaidPoint(row.After);
                Assert.AreEqual(row.Before.Bank, before["bank"].ToObject(row.Before.Bank.GetType()));
                Assert.AreEqual(row.After.Bank, after["bank"].ToObject(row.After.Bank.GetType()));
                Assert.AreEqual(row.Before.Materials, (int)before["materials"]);
                Assert.AreEqual(row.After.Materials, (int)after["materials"]);
                Assert.AreEqual(row.After.HasReservation,
                    after["reservation"].Type != Newtonsoft.Json.Linq.JTokenType.Null);
                if (row.After.HasReservation)
                {
                    Assert.AreEqual(row.After.Reservation.ReservationId, (uint)after["reservation"]["id"]);
                    Assert.AreEqual(row.After.Reservation.Phase.ToString(), (string)after["reservation"]["phase"]);
                }
                var actor = PaidHarness.PaidProductionActorJson(row);
                Assert.AreEqual(row.HasActorOwner, actor != null);
                if (actor != null)
                {
                    Assert.AreEqual(row.Actor.Index, (int)actor["index"]);
                    Assert.AreEqual(row.Actor.Version, (int)actor["version"]);
                    Assert.AreEqual(row.ActorOwner.StableObjectId.ToString(), (string)actor["stable_id"]);
                    Assert.AreEqual(row.ActorOwner.CapacityReleased, (byte)actor["capacity_released"]);
                }
                var decision = PaidHarness.PaidProductionDecisionJson(row);
                Assert.AreEqual(row.HasDecision, decision != null);
                if (decision != null) Assert.AreEqual(row.Decision.MaterialsCost, (int)decision["materials"]);
                PaidDtoSample("production", row.Operation, before, after, decision, actor);
            }
        }
        [Test]
        public void PaidDtoDirectAndDeathUseRealCommittedPointsAndActorIdentity()
        {
            WithCommittedProductionLedger((em, session, rows) => {
                var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
                Assert.IsTrue(SkirmishProductionService.TryProduce(em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out var produced));
                Entity member = FindReservedUnit(em, produced.ReservationId);
                Assert.IsTrue(SkirmishProductionService.TryReleaseDeath(em, session, member));
                CollectionAssert.AreEqual(new[] { "DirectDebit", "DirectDispatch", "DirectMember", "DeathRelease" }, rows.ConvertAll(r => r.Operation));
                PaidProductionDtoRows(rows);
            });
        }
        [Test]
        public void PaidDtoQueuedDispatchSerializesRealNullAndLiveReservations()
        {
            WithCommittedProductionLedger((em, session, rows) => {
                var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
                Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyGround, out var queued));
                Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, queued.ReservationId, out _));
                Assert.IsTrue(SkirmishProductionService.TryDispatchQueued(em, session, queued.ReservationId, authored.ArmyGround, out _));
                Assert.IsFalse(rows[0].Before.HasReservation);
                Assert.AreEqual(SkirmishReservationPhase.Live, rows[2].After.Reservation.Phase);
                PaidProductionDtoRows(rows);
            });
        }
        [Test]
        public void PaidDtoResearchUsesRealStateDecisionAndNoProgressRows()
        {
            WithResearchLedger((em, session, rows) => {
                Assert.IsTrue(SkirmishResearchService.TryQueue(em, session, SkirmishResearchKind.InfantryWeapons, 1, out var queued));
                Assert.AreEqual(0, SkirmishResearchService.Tick(em, session, 1, false));
                Assert.AreEqual(2, rows.Count);
                float remaining = LedgerResearchItem(em, session, queued.ResearchId).RemainingSeconds;
                Assert.AreEqual(1, SkirmishResearchService.Tick(em, session, remaining, false));
                Assert.IsTrue(SkirmishResearchService.TryQueue(em, session, SkirmishResearchKind.VehicleProtection, 1, out var cancelled));
                Assert.IsTrue(SkirmishResearchService.TryCancel(em, session, cancelled.ResearchId, out _));
                CollectionAssert.AreEqual(new[] { "Queue", "Start", "Complete", "Queue", "Cancel" }, rows.ConvertAll(r => r.Operation));
                foreach (var row in rows)
                {
                    var before = PaidHarness.ResearchPointJson(row.Before);
                    var after = PaidHarness.ResearchPointJson(row.After);
                    Assert.AreEqual(row.After.Bank, after["bank"].ToObject(row.After.Bank.GetType()));
                    Assert.AreEqual(row.After.State, after["state"].ToObject(row.After.State.GetType()));
                    Assert.AreEqual(row.After.NextResearchId, (uint)after["next_research_id"]);
                    Assert.AreEqual(row.After.HasItem, after["research"].Type != Newtonsoft.Json.Linq.JTokenType.Null);
                    var decision = PaidHarness.PaidResearchDecisionJson(row);
                    Assert.AreEqual(row.HasDecision, decision != null);
                    if (decision != null) Assert.AreEqual(row.Decision, decision.ToObject(row.Decision.GetType()));
                    PaidDtoSample("research", row.Operation, before, after, decision, null);
                }
            });
        }
        [Test]
        public void PaidDtoEnemyConstructionUsesRealRequestRefundAndDeliveryPoints()
        {
            Assert.IsNull(SkirmishConstructionLedger.Observer);
            var rows = new System.Collections.Generic.List<SkirmishConstructionLedger.Observation>();
            Action<SkirmishConstructionLedger.Observation> sink = row => {
                var before = PaidHarness.ConstructionPoint(row.Before);
                var after = PaidHarness.ConstructionPoint(row.After);
                Assert.AreEqual(row.After.Bank, after["bank"].ToObject(row.After.Bank.GetType()));
                Assert.AreEqual(row.After.Economy, after["economy"].ToObject(row.After.Economy.GetType()));
                Assert.AreEqual(row.After.HasRequest, after["request"].Type != Newtonsoft.Json.Linq.JTokenType.Null);
                Assert.AreEqual(row.After.HasEnemyIntent, after["enemy_intent"].Type != Newtonsoft.Json.Linq.JTokenType.Null);
                var actor = PaidHarness.PaidConstructionActorJson(row);
                Assert.AreEqual(row.HasActorOwner, actor != null);
                if (actor != null) Assert.AreEqual(row.Actor.Index, (int)actor["index"]);
                PaidDtoSample("construction", row.Operation, before, after, null, actor);
                rows.Add(row);
            };
            long faults = SkirmishConstructionLedger.Faults;
            SkirmishConstructionLedger.OpenOwned(sink);
            try
            {
                using (var f = new SkirmishEnemyConstructionAdapterTests.Fixture())
                {
                    Assert.IsTrue(f.Request()); f.Failed();
                    SkirmishEnemyConstructionAdapter.Tick(f.Em, f.Session);
                    CollectionAssert.AreEqual(new[] { "EnemyRequest", "EnemyRefundDebt", "EnemyRefund" }, rows.ConvertAll(r => r.Operation));
                }
                rows.Clear();
                using (var f = new SkirmishEnemyConstructionAdapterTests.Fixture())
                {
                    Assert.IsTrue(f.Request()); f.Succeeded();
                    CollectionAssert.AreEqual(new[] { "EnemyRequest", "EnemyDelivered" }, rows.ConvertAll(r => r.Operation));
                }
                Assert.AreEqual(faults, SkirmishConstructionLedger.Faults, "Serializer assertion/sink exceptions must not be hidden as observation faults");
            }
            finally { Assert.IsTrue(SkirmishConstructionLedger.CloseOwned(sink), "Preserve unexpected foreign sink"); }
        }
        [Test]
        public void PaidDtoCompactSerializerRefusesNonfiniteFixtureRoundTrip()
        {
            var bad = new Newtonsoft.Json.Linq.JObject { ["fixture_nonfinite"] = double.NaN };
            Assert.Throws<AssertionException>(() => PaidDtoRoundTrip(bad));
        }
        public static void RunPaidNativeDtoConformanceControls()
        {
            Assert.IsNull(paidDtoSamples);
            string path = Environment.GetEnvironmentVariable("WARLINE_PAID_DTO_CONFORMANCE_OUTPUT");
            Assert.IsFalse(string.IsNullOrEmpty(path)); Assert.IsTrue(System.IO.Path.IsPathRooted(path));
            path = System.IO.Path.GetFullPath(path);
            string root = System.IO.Path.GetFullPath("Logs/SkirmishProduction") + System.IO.Path.DirectorySeparatorChar;
            Assert.IsTrue(path.StartsWith(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(System.IO.File.Exists(path)); Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)));
            for (string current = System.IO.Path.GetDirectoryName(path); !string.IsNullOrEmpty(current); current = System.IO.Path.GetDirectoryName(current))
                Assert.AreEqual(0, (int)(System.IO.File.GetAttributes(current) & System.IO.FileAttributes.ReparsePoint));
            var samples = new Newtonsoft.Json.Linq.JArray();
            var report = new Newtonsoft.Json.Linq.JObject {
                ["schema"] = 1, ["scope"] = "PaidNativeDtoConformanceFixture_NoNormalJournal",
                ["result"] = "Incomplete", ["samples"] = samples,
                ["normalEnvelopeExercised"] = false, ["normalJournal"] = false,
                ["compactRecordSerializerShared"] = true, ["paid_resources"] = false, ["qualified"] = false };
            paidDtoSamples = samples;
            try
            {
                var t = new SkirmishExpandedEconomyTests();
                t.PaidDtoDirectAndDeathUseRealCommittedPointsAndActorIdentity();
                t.PaidDtoQueuedDispatchSerializesRealNullAndLiveReservations();
                t.PaidDtoResearchUsesRealStateDecisionAndNoProgressRows();
                t.PaidDtoEnemyConstructionUsesRealRequestRefundAndDeliveryPoints();
                t.PaidDtoCompactSerializerRefusesNonfiniteFixtureRoundTrip();
                foreach (string family in new[] { "production", "research", "construction" })
                    Assert.IsTrue(System.Linq.Enumerable.Any(samples, x => (string)x["fixture_family"] == family));
                var counts = new Newtonsoft.Json.Linq.JObject();
                foreach (string family in new[] { "production", "research", "construction" })
                    counts[family] = System.Linq.Enumerable.Count(samples, x => (string)x["fixture_family"] == family);
                report["sampleCountsByFamily"] = counts;
                report["sampleCount"] = samples.Count; report["controls"] = 5; report["result"] = "CollectedFixtureOnly";
            }
            catch (Exception error) when (!(error is OutOfMemoryException))
            {
                report["result"] = "Failed"; report["exceptionType"] = error.GetType().FullName; throw;
            }
            finally
            {
                paidDtoSamples = null;
                using var stream = new System.IO.FileStream(path, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write, System.IO.FileShare.Read);
                using var writer = new System.IO.StreamWriter(stream, new System.Text.UTF8Encoding(false));
                writer.Write(PaidHarness.SerializeProductionNativeRecord(report));
            }
            Debug.Log("[PaidNativeDtoConformanceControls] result=Collected controls=5 families=3 normalEnvelope=0 normalJournal=0 paidResourcesQualified=0");
        }

        private sealed class PaidRegistryFixture : IDisposable
        {
            internal readonly Action<SkirmishProductionService.ProductionLedgerObservation> production = row => { };
            internal readonly Action<SkirmishResearchService.ResearchLedgerObservation> research = row => { };
            internal readonly Action<SkirmishConstructionLedger.Observation> construction = row => { };
            internal readonly Action<SkirmishProductionService.ProductionLedgerObservation> foreignProduction = row => { };
            internal readonly Action<SkirmishResearchService.ResearchLedgerObservation> foreignResearch = row => { };
            internal readonly Action<SkirmishConstructionLedger.Observation> foreignConstruction = row => { };
            internal readonly System.Collections.Generic.List<string> closed = new();
            internal int starts;
            internal PaidRegistryFixture() { AssertEmpty(); }
            internal void Install(int i, bool foreign = false)
            {
                if (!foreign) starts++;
                if (i == 0) SkirmishProductionService.ProductionLedgerObserver = foreign ? foreignProduction : production;
                if (i == 1) SkirmishResearchService.ResearchLedgerObserver = foreign ? foreignResearch : research;
                if (i == 2) SkirmishConstructionLedger.Observer = foreign ? foreignConstruction : construction;
            }
            internal void Close(int i)
            {
                closed.Add(i == 0 ? "P" : i == 1 ? "R" : "C");
                if (i == 0) PaidHarness.ReleasePaidProductionObserver(production);
                if (i == 1) PaidHarness.ReleasePaidResearchObserver(research);
                if (i == 2) SkirmishConstructionLedger.CloseOwned(construction);
            }
            internal static void AssertEmpty()
            {
                Assert.IsNull(SkirmishProductionService.ProductionLedgerObserver);
                Assert.IsNull(SkirmishResearchService.ResearchLedgerObserver);
                Assert.IsNull(SkirmishConstructionLedger.Observer);
            }
            internal void AssertForeign(int i)
            {
                if (i == 0) Assert.AreSame(foreignProduction, SkirmishProductionService.ProductionLedgerObserver);
                if (i == 1) Assert.AreSame(foreignResearch, SkirmishResearchService.ResearchLedgerObserver);
                if (i == 2) Assert.AreSame(foreignConstruction, SkirmishConstructionLedger.Observer);
            }
            public void Dispose()
            {
                PaidHarness.ReleasePaidProductionObserver(production);
                PaidHarness.ReleasePaidResearchObserver(research);
                SkirmishConstructionLedger.CloseOwned(construction);
                PaidHarness.ReleasePaidProductionObserver(foreignProduction);
                PaidHarness.ReleasePaidResearchObserver(foreignResearch);
                SkirmishConstructionLedger.CloseOwned(foreignConstruction);
            }
        }
        [Test]
        public void PaidGroupDisabledLeavesActualForeignRegistriesUntouched()
        {
            using var f = new PaidRegistryFixture();
            for (int i = 0; i < 3; i++) f.Install(i, true);
            int reservations = 0;
            PaidHarness.GuardPaidObserverReservation(false, true, () => reservations++);
            PaidHarness.StartPaidObserverGroup(false, () => f.Install(0), () => f.Install(1),
                () => f.Install(2), () => f.Close(2), () => f.Close(1), () => f.Close(0));
            Assert.AreEqual(1, reservations); Assert.AreEqual(0, f.starts);
            for (int i = 0; i < 3; i++) f.AssertForeign(i);
        }
        [Test]
        public void PaidGroupEachForeignRegistryRefusesBeforeReservation()
        {
            for (int i = 0; i < 3; i++)
            {
                using var f = new PaidRegistryFixture(); f.Install(i, true);
                int reservations = 0;
                Assert.Throws<InvalidOperationException>(() =>
                    PaidHarness.GuardPaidObserverReservation(true, false, () => reservations++));
                Assert.AreEqual(0, reservations); f.AssertForeign(i);
            }
        }
        [Test]
        public void PaidGroupLegacyRefusesBeforeReservation()
        {
            using var f = new PaidRegistryFixture(); int reservations = 0;
            Assert.Throws<System.IO.InvalidDataException>(() =>
                PaidHarness.GuardPaidObserverReservation(true, true, () => reservations++));
            Assert.AreEqual(0, reservations); PaidRegistryFixture.AssertEmpty();
        }
        [Test]
        public void PaidGroupStartupFaultAfterEachInstallClosesOwnedRegistriesInReverse()
        {
            for (int fault = 0; fault < 3; fault++)
            {
                using var f = new PaidRegistryFixture();
                Action<int> start = i => { f.Install(i); if (i == fault) throw new InvalidOperationException("injected-start"); };
                var error = Assert.Throws<InvalidOperationException>(() =>
                    PaidHarness.StartPaidObserverGroup(true, () => start(0), () => start(1), () => start(2),
                        () => f.Close(2), () => f.Close(1), () => f.Close(0)));
                Assert.AreEqual("injected-start", error.Message);
                Assert.AreEqual(fault + 1, f.starts);
                CollectionAssert.AreEqual(new[] { "C", "R", "P" }, f.closed);
                PaidRegistryFixture.AssertEmpty();
            }
        }
        [Test]
        public void PaidGroupStartupFaultPreservesForeignReplacement()
        {
            using var f = new PaidRegistryFixture();
            Assert.Throws<InvalidOperationException>(() =>
                PaidHarness.StartPaidObserverGroup(true, () => f.Install(0),
                    () => { f.Install(1); f.Install(0, true); throw new InvalidOperationException("injected-replaced"); },
                    () => f.Install(2), () => f.Close(2), () => f.Close(1), () => f.Close(0)));
            f.AssertForeign(0);
            Assert.IsNull(SkirmishResearchService.ResearchLedgerObserver);
            Assert.IsNull(SkirmishConstructionLedger.Observer);
        }
        [Test]
        public void PaidGroupClosePreservesAllForeignReplacements()
        {
            using var f = new PaidRegistryFixture();
            for (int i = 0; i < 3; i++) { f.Install(i); f.Install(i, true); }
            bool restored = false;
            PaidHarness.ClosePaidObserverGroup(() => f.Close(2), () => f.Close(1), () => f.Close(0), () => restored = true);
            Assert.IsTrue(restored);
            for (int i = 0; i < 3; i++) f.AssertForeign(i);
        }
        [Test]
        public void PaidGroupEachCloseFaultStillRunsRemainingCloseAndActualEcsCleanup()
        {
            for (int fault = 0; fault < 3; fault++)
            {
                using var f = new PaidRegistryFixture();
                using var world = new World("PaidCloseFailureControl");
                var em = world.EntityManager; var entity = em.CreateEntity();
                for (int i = 0; i < 3; i++) f.Install(i);
                Action<int> close = i => { f.Close(i); if (i == fault) throw new InvalidOperationException("injected-close"); };
                var error = Assert.Throws<InvalidOperationException>(() =>
                    PaidHarness.ClosePaidObserverGroup(() => close(2), () => close(1), () => close(0),
                        () => em.DestroyEntity(entity)));
                Assert.AreEqual("injected-close", error.Message);
                CollectionAssert.AreEqual(new[] { "C", "R", "P" }, f.closed);
                Assert.IsFalse(em.Exists(entity)); PaidRegistryFixture.AssertEmpty();
            }
        }
        [Test]
        public void PaidGroupNormalCloseRunsExactlyOnceInReverseThenCleanup()
        {
            using var f = new PaidRegistryFixture(); int cleanups = 0;
            PaidHarness.GuardPaidObserverReservation(true, false, () => { });
            PaidHarness.StartPaidObserverGroup(true, () => f.Install(0), () => f.Install(1), () => f.Install(2),
                () => f.Close(2), () => f.Close(1), () => f.Close(0));
            PaidHarness.ClosePaidObserverGroup(() => f.Close(2), () => f.Close(1), () => f.Close(0), () => cleanups++);
            CollectionAssert.AreEqual(new[] { "C", "R", "P" }, f.closed);
            Assert.AreEqual(3, f.starts); Assert.AreEqual(1, cleanups); PaidRegistryFixture.AssertEmpty();
        }
        public static void RunPaidObserverSharedLifecycleControls()
        {
            var t = new SkirmishExpandedEconomyTests();
            t.PaidGroupDisabledLeavesActualForeignRegistriesUntouched();
            t.PaidGroupEachForeignRegistryRefusesBeforeReservation();
            t.PaidGroupLegacyRefusesBeforeReservation();
            t.PaidGroupStartupFaultAfterEachInstallClosesOwnedRegistriesInReverse();
            t.PaidGroupStartupFaultPreservesForeignReplacement();
            t.PaidGroupClosePreservesAllForeignReplacements();
            t.PaidGroupEachCloseFaultStillRunsRemainingCloseAndActualEcsCleanup();
            t.PaidGroupNormalCloseRunsExactlyOnceInReverseThenCleanup();
            Debug.Log("[PaidObserverSharedLifecycleControls] result=Passed controls=14 realRegistries=3 ecsCleanupCases=3 normalJournal=0 paidResourcesQualified=0");
        }

        private static void WithResearchLedger(Action<EntityManager,Entity,
            System.Collections.Generic.List<SkirmishResearchService.ResearchLedgerObservation>> action)
        {
            Assert.IsNull(SkirmishResearchService.ResearchLedgerObserver,"Preserve a foreign research sink");
            using var world=new World("ResearchCommittedLedgerControls");var em=world.EntityManager;
            CompileAndSpawn(em,out var session,out var setup);SkirmishMaterialsService.Initialize(em,session,setup);
            var rows=new System.Collections.Generic.List<SkirmishResearchService.ResearchLedgerObservation>();
            Action<SkirmishResearchService.ResearchLedgerObservation> sink=rows.Add;
            SkirmishResearchService.ResearchLedgerObserver=sink;
            try{action(em,session,rows);}
            finally
            {
                bool owned=SkirmishResearchService.ResearchLedgerObserver==sink;
                if(owned)SkirmishResearchService.ResearchLedgerObserver=null;
                Assert.IsTrue(owned,"Do not erase a replaced research sink");
            }
        }
        private static SkirmishResearchQueueItem LedgerResearchItem(EntityManager em,Entity session,uint id)
        {
            foreach(var item in em.GetBuffer<SkirmishResearchQueueItem>(session))if(item.ResearchId==id)return item;
            Assert.Fail("Real queued research required");return default;
        }

        [Test]
        public void ResearchLedgerRealTickCompletesAndGrantsWithoutSecondDebit()
        {
            WithResearchLedger((em,session,rows)=>{
                int before=SkirmishMaterialsService.Read(em,session,1);
                Assert.IsTrue(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.InfantryWeapons,1,out var queued));
                Assert.AreEqual("Queue",rows[0].Operation);Assert.IsFalse(rows[0].Before.HasItem);
                Assert.AreEqual(before-queued.MaterialsCost,rows[0].After.Materials);
                Assert.AreEqual(queued.ResearchId,rows[0].After.Item.ResearchId);
                Assert.AreEqual(0,SkirmishResearchService.Tick(em,session,1,false));
                Assert.AreEqual("Start",rows[1].Operation);Assert.AreEqual(2,rows.Count,"No per-tick snapshots/rows");
                Assert.Less(LedgerResearchItem(em,session,queued.ResearchId).RemainingSeconds,rows[1].After.Item.RemainingSeconds);
                float remaining=LedgerResearchItem(em,session,queued.ResearchId).RemainingSeconds;
                Assert.AreEqual(1,SkirmishResearchService.Tick(em,session,remaining,false));
                Assert.AreEqual(3,rows.Count);Assert.AreEqual("Complete",rows[2].Operation);
                Assert.LessOrEqual(rows[2].Before.Item.RemainingSeconds,0,"Actual Tick wrote the elapsed state before completion");
                Assert.AreEqual(SkirmishResearchPhase.Completed,rows[2].After.Item.Phase);
                Assert.AreEqual(1,rows[2].After.State.InfantryWeapons);Assert.AreEqual(0,rows[2].After.Item.RemainingSeconds);
                Assert.AreEqual(before-queued.MaterialsCost,SkirmishMaterialsService.Read(em,session,1));
                foreach(var row in rows)if(row.Operation!="Queue")Assert.AreEqual(row.Before.Bank,row.After.Bank);
                Assert.IsFalse(SkirmishResearchService.TryComplete(em,session,queued.ResearchId,out _));Assert.AreEqual(3,rows.Count);
            });
        }

        [Test]
        public void ResearchLedgerQueuedCancellationUsesAuthoredFullRefundOnce()
        {
            WithResearchLedger((em,session,rows)=>{
                int before=SkirmishMaterialsService.Read(em,session,1);
                Assert.IsTrue(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.VehicleProtection,1,out var queued));
                int policy=SkirmishResearchCosts.RefundMaterials(queued.MaterialsCost,SkirmishResearchPhase.Queued);
                Assert.IsTrue(SkirmishResearchService.TryCancel(em,session,queued.ResearchId,out var cancelled));
                Assert.AreEqual("Cancel",rows[1].Operation);Assert.AreEqual(policy,cancelled.RefundedMaterials);
                Assert.AreEqual(before-queued.MaterialsCost+policy,SkirmishMaterialsService.Read(em,session,1));
                Assert.AreEqual(rows[0].After.Bank,rows[1].Before.Bank);
                Assert.AreEqual(SkirmishResearchPhase.Cancelled,rows[1].After.Item.Phase);
                Assert.IsFalse(SkirmishResearchService.TryCancel(em,session,queued.ResearchId,out _));Assert.AreEqual(2,rows.Count);
            });
        }

        [Test]
        public void ResearchLedgerResearchingCancellationUsesActualPartialPolicy()
        {
            WithResearchLedger((em,session,rows)=>{
                int before=SkirmishMaterialsService.Read(em,session,1);
                Assert.IsTrue(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.VehicleProtection,1,out var queued));
                Assert.IsTrue(SkirmishResearchService.TryStart(em,session,queued.ResearchId,out _));
                int policy=SkirmishResearchCosts.RefundMaterials(queued.MaterialsCost,SkirmishResearchPhase.Researching);
                Assert.IsTrue(SkirmishResearchService.TryCancel(em,session,queued.ResearchId,out var cancelled));
                Assert.AreEqual(policy,cancelled.RefundedMaterials);Assert.AreEqual(before-queued.MaterialsCost+policy,rows[2].After.Materials);
                Assert.AreEqual(rows[1].After.Bank,rows[2].Before.Bank);Assert.AreEqual("Cancel",rows[2].Operation);
                Assert.IsFalse(SkirmishResearchService.HasCompleted(em,session,SkirmishResearchKind.VehicleProtection));
            });
        }

        [Test]
        public void ResearchLedgerProducerLossNeverRefundsOrResurrects()
        {
            foreach(bool started in new[]{false,true})WithResearchLedger((em,session,rows)=>{
                Assert.IsTrue(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.InfantryWeapons,1,out var queued));
                if(started)Assert.IsTrue(SkirmishResearchService.TryStart(em,session,queued.ResearchId,out _));
                int paid=SkirmishMaterialsService.Read(em,session,1);
                var producer=LedgerResearchItem(em,session,queued.ResearchId).Producer;
                Assert.AreEqual(1,SkirmishResearchService.NotifyProducerDestroyed(em,session,producer,1));
                var last=rows[rows.Count-1];Assert.AreEqual("Lost",last.Operation);
                Assert.AreEqual(SkirmishResearchPhase.Lost,last.After.Item.Phase);Assert.AreEqual(last.Before.Bank,last.After.Bank);
                Assert.AreEqual(paid,SkirmishMaterialsService.Read(em,session,1));
                int count=rows.Count;Assert.AreEqual(0,SkirmishResearchService.NotifyProducerDestroyed(em,session,producer,1));
                Assert.IsFalse(SkirmishResearchService.TryCancel(em,session,queued.ResearchId,out _));
                Assert.IsFalse(SkirmishResearchService.TryComplete(em,session,queued.ResearchId,out _));Assert.AreEqual(count,rows.Count);
            });
        }

        [Test]
        public void ResearchLedgerBothFactionsHaveDistinctIdsBanksAndGrants()
        {
            WithResearchLedger((em,session,rows)=>{
                Assert.IsTrue(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.InfantryWeapons,1,out var player));
                Assert.IsTrue(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.InfantryWeapons,2,out var enemy));
                Assert.AreNotEqual(player.ResearchId,enemy.ResearchId);Assert.AreNotEqual(rows[0].After.BankEntity,rows[1].After.BankEntity);
                Assert.AreEqual(1,rows[0].After.State.FactionId);Assert.AreEqual(2,rows[1].After.State.FactionId);
                Assert.AreEqual(rows[1].Before.NextResearchId+1,rows[1].After.NextResearchId);
                Assert.IsTrue(SkirmishResearchService.TryComplete(em,session,enemy.ResearchId,out _));
                Assert.IsTrue(SkirmishResearchService.HasCompleted(em,session,SkirmishResearchKind.InfantryWeapons,2));
                Assert.IsFalse(SkirmishResearchService.HasCompleted(em,session,SkirmishResearchKind.InfantryWeapons,1));
                Assert.AreEqual(rows[2].Before.Bank,rows[2].After.Bank);
            });
        }

        [Test]
        public void ResearchLedgerPauseAndUnaffordableRequestHaveNoCommit()
        {
            WithResearchLedger((em,session,rows)=>{
                Assert.IsTrue(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.InfantryWeapons,1,out var queued));
                var item=LedgerResearchItem(em,session,queued.ResearchId);
                Assert.AreEqual(0,SkirmishResearchService.Tick(em,session,20,true));Assert.AreEqual(1,rows.Count);
                Assert.AreEqual(item,LedgerResearchItem(em,session,queued.ResearchId));
                SkirmishMaterialsService.Write(em,session,1,0);var bank=em.GetComponentData<FactionTacticalMaterialsComponent>(SkirmishMaterialsService.FindEconomy(em,1));
                Assert.IsFalse(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.VehicleProtection,1,out _));
                Assert.AreEqual(bank,em.GetComponentData<FactionTacticalMaterialsComponent>(SkirmishMaterialsService.FindEconomy(em,1)));Assert.AreEqual(1,rows.Count);
            });
        }

        [Test]
        public void ResearchLedgerOrdinarySinkFailureAndDefaultOffDoNotChangeDebits()
        {
            WithResearchLedger((em,session,rows)=>{
                var owned=SkirmishResearchService.ResearchLedgerObserver;long faults=SkirmishResearchService.ResearchLedgerObservationFaults;
                try
                {
                    SkirmishResearchService.ResearchLedgerObserver=_=>throw new InvalidOperationException("research observation failure");
                    int before=SkirmishMaterialsService.Read(em,session,1);
                    Assert.IsTrue(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.InfantryWeapons,1,out var first));
                    Assert.AreEqual(before-first.MaterialsCost,SkirmishMaterialsService.Read(em,session,1));
                    Assert.AreEqual(faults+1,SkirmishResearchService.ResearchLedgerObservationFaults);
                    SkirmishResearchService.ResearchLedgerObserver=null;before=SkirmishMaterialsService.Read(em,session,1);
                    Assert.IsTrue(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.VehicleProtection,1,out var second));
                    Assert.AreEqual(before-second.MaterialsCost,SkirmishMaterialsService.Read(em,session,1));
                    Assert.AreEqual(faults+1,SkirmishResearchService.ResearchLedgerObservationFaults);Assert.AreEqual(0,rows.Count);
                }
                finally{SkirmishResearchService.ResearchLedgerObserver=owned;}
            });
        }

        [Test]
        public void ResearchLedgerDuplicateBankRefusesObservationNotGameplay()
        {
            WithResearchLedger((em,session,rows)=>{
                var bank=SkirmishMaterialsService.FindEconomy(em,1);var duplicate=em.CreateEntity(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent));
                em.SetComponentData(duplicate,em.GetComponentData<FactionEconomy>(bank));
                em.SetComponentData(duplicate,em.GetComponentData<FactionTacticalMaterialsComponent>(bank));
                long faults=SkirmishResearchService.ResearchLedgerObservationFaults;
                Assert.IsTrue(SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.InfantryWeapons,1,out var queued));
                Assert.AreEqual(SkirmishResearchPhase.Queued,LedgerResearchItem(em,session,queued.ResearchId).Phase);
                Assert.AreEqual(faults+1,SkirmishResearchService.ResearchLedgerObservationFaults);Assert.AreEqual(0,rows.Count);
            });
        }

        public static void RunResearchCommittedLedgerFocusedControls()
        {
            var suite=new SkirmishExpandedEconomyTests();
            suite.ResearchLedgerRealTickCompletesAndGrantsWithoutSecondDebit();
            suite.ResearchLedgerQueuedCancellationUsesAuthoredFullRefundOnce();
            suite.ResearchLedgerResearchingCancellationUsesActualPartialPolicy();
            suite.ResearchLedgerProducerLossNeverRefundsOrResurrects();
            suite.ResearchLedgerBothFactionsHaveDistinctIdsBanksAndGrants();
            suite.ResearchLedgerPauseAndUnaffordableRequestHaveNoCommit();
            suite.ResearchLedgerOrdinarySinkFailureAndDefaultOffDoNotChangeDebits();
            suite.ResearchLedgerDuplicateBankRefusesObservationNotGameplay();
            Debug.Log("[ResearchCommittedLedgerControls] result=Passed ecsCases=8 paidResourcesQualified=0 missionReady=0");
        }


        private static void WithCommittedProductionLedger(Action<EntityManager,Entity,
            System.Collections.Generic.List<SkirmishProductionService.ProductionLedgerObservation>> action)
        {
            Assert.IsNull(SkirmishProductionService.ProductionLedgerObserver,"Do not replace a foreign sink");
            using var world=new World("ProductionCommittedOriginsControls");
            var em=world.EntityManager;
            CompileAndSpawn(em,out var session,out var setup);
            SkirmishMaterialsService.Initialize(em,session,setup);
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.Phase=SkirmishSessionPhase.Playing;em.SetComponentData(session,state);
            var rows=new System.Collections.Generic.List<SkirmishProductionService.ProductionLedgerObservation>();
            Action<SkirmishProductionService.ProductionLedgerObservation> sink=rows.Add;
            SkirmishProductionService.ProductionLedgerObserver=sink;
            try { action(em,session,rows); }
            finally
            {
                bool owned=SkirmishProductionService.ProductionLedgerObserver==sink;
                if(owned)SkirmishProductionService.ProductionLedgerObserver=null;
                Assert.IsTrue(owned,"Fixture must not erase a replaced observer");
            }
        }

        [Test]
        public void LedgerDirectDebitDispatchAndDeathReflectCommittedEcs()
        {
            WithCommittedProductionLedger((em,session,rows)=>{
                var authored=SkirmishExpansionCatalogFactory.CreateInMemory();
                int before=SkirmishMaterialsService.Read(em,session,1);
                var capacity=em.GetComponentData<SkirmishCapacityComponent>(session);
                Assert.IsTrue(SkirmishProductionService.TryProduce(em,session,SkirmishRoleIds.Tank,1,authored.ArmyGround,out var decision));
                Assert.AreEqual(3,rows.Count);Assert.AreEqual("DirectDebit",rows[0].Operation);Assert.AreEqual("DirectDispatch",rows[1].Operation);
                Assert.AreEqual("DirectMember",rows[2].Operation);Assert.AreEqual(0,rows[2].MemberOrdinal);
                Assert.AreEqual(before,rows[0].Before.Materials);
                Assert.AreEqual(before-decision.MaterialsCost,rows[0].After.Materials);
                Assert.AreEqual(SkirmishMaterialsService.Read(em,session,1),rows[1].After.Materials);
                Assert.AreEqual(rows[0].After.Bank,rows[1].Before.Bank);
                Assert.AreEqual(rows[1].Before.Bank,rows[1].After.Bank,"Dispatch cannot debit twice");
                Assert.AreEqual(SkirmishReservationPhase.Live,rows[1].After.Reservation.Phase);
                Entity member=FindReservedUnit(em,decision.ReservationId);Assert.AreNotEqual(Entity.Null,member);
                Assert.AreEqual(member,rows[2].Actor);
                AssertDirectMemberIdentities(em,session,decision.ReservationId,decision.MemberCount,rows);
                Assert.AreEqual(capacity.GroundLive+1,em.GetComponentData<SkirmishCapacityComponent>(session).GroundLive);
                Assert.IsTrue(SkirmishProductionService.TryReleaseDeath(em,session,member));
                Assert.AreEqual("DeathRelease",rows[3].Operation);Assert.IsTrue(rows[3].ActorOwner.CapacityReleased!=0);
                Assert.AreEqual(rows[3].Before.Bank,rows[3].After.Bank,"Death is not a refund");
                Assert.AreEqual(capacity.GroundLive,em.GetComponentData<SkirmishCapacityComponent>(session).GroundLive);
                Assert.IsFalse(SkirmishProductionService.TryReleaseDeath(em,session,member));Assert.AreEqual(4,rows.Count);
            });
        }

        [Test]
        public void LedgerQueuedDirectDispatchDoesNotDebitAgain()
        {
            WithCommittedProductionLedger((em,session,rows)=>{
                var authored=SkirmishExpansionCatalogFactory.CreateInMemory();
                Assert.IsTrue(SkirmishProductionService.TryQueue(em,session,SkirmishRoleIds.Rifle,1,authored.ArmyGround,out var queued));
                int paid=SkirmishMaterialsService.Read(em,session,1);
                Assert.IsTrue(SkirmishProductionService.TryStartQueued(em,session,queued.ReservationId,out _));
                Assert.IsTrue(SkirmishProductionService.TryDispatchQueued(em,session,queued.ReservationId,authored.ArmyGround,out _));
                Assert.AreEqual(3+queued.MemberCount,rows.Count);Assert.AreEqual("Queue",rows[0].Operation);Assert.AreEqual("StartQueued",rows[1].Operation);Assert.AreEqual("QueuedDirectDispatch",rows[2].Operation);
                Assert.AreEqual(rows[0].After.Bank,rows[1].After.Bank);Assert.AreEqual(rows[1].After.Bank,rows[2].After.Bank);
                Assert.AreEqual(paid,SkirmishMaterialsService.Read(em,session,1));
                Assert.AreEqual(SkirmishReservationPhase.Producing,rows[1].After.Reservation.Phase);
                Assert.AreEqual(SkirmishReservationPhase.Live,rows[2].After.Reservation.Phase);
                Assert.AreNotEqual(Entity.Null,FindReservedUnit(em,queued.ReservationId));
                Assert.IsFalse(SkirmishProductionService.TryDispatchQueued(em,session,queued.ReservationId,authored.ArmyGround,out _));Assert.AreEqual(3+queued.MemberCount,rows.Count);
                AssertDirectMemberIdentities(em,session,queued.ReservationId,queued.MemberCount,rows);
            });
        }

        [Test]
        public void LedgerNativeStartAndRealMemberAdoptionStayBound()
        {
            WithCommittedProductionLedger((em,session,rows)=>{
                var authored=SkirmishExpansionCatalogFactory.CreateInMemory();
                Assert.IsTrue(SkirmishProductionService.TryQueue(em,session,SkirmishRoleIds.Rifle,1,authored.ArmyGround,out var queued));
                Assert.IsTrue(SkirmishProductionService.BindNativeQueue(em,session,queued.ReservationId,42));
                var id=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                SkirmishProductionService.StartNativeQueue(em,session,queued.ReservationId,new FixedString64Bytes("foreign"));Assert.AreEqual(2,rows.Count);
                SkirmishProductionService.StartNativeQueue(em,session,queued.ReservationId,id);
                Assert.AreEqual("StartNativeQueue",rows[2].Operation);Assert.AreEqual(42,rows[2].After.Reservation.ProducerRuntimeId);
                var member=em.CreateEntity(typeof(Faction),typeof(UnitSourcePrefabKey),typeof(UnitHealth));
                em.SetComponentData(member,new Faction {Id=1});
                em.SetComponentData(member,new UnitSourcePrefabKey {Value=new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Rifle))});
                Assert.IsFalse(SkirmishProductionService.AdoptNativeMember(em,session,queued.ReservationId,99,member,id));Assert.AreEqual(3,rows.Count);
                Assert.IsTrue(SkirmishProductionService.AdoptNativeMember(em,session,queued.ReservationId,42,member,id));
                Assert.AreEqual("Deliver",rows[3].Operation);Assert.AreEqual(member,rows[3].Actor);
                Assert.AreEqual(queued.ReservationId,rows[3].ActorOwner.ReservationId);
                Assert.AreEqual(rows[3].Before.Bank,rows[3].After.Bank);
                Assert.AreEqual(1,rows[3].After.Reservation.DeliveredMembers);
            });
        }

        [Test]
        public void LedgerBlockedRealSpawnCapturesDebitThenOneRefund()
        {
            Assert.IsNull(SkirmishProductionService.ProductionLedgerObserver);
            var rows=new System.Collections.Generic.List<SkirmishProductionService.ProductionLedgerObservation>();
            Action<SkirmishProductionService.ProductionLedgerObservation> sink=rows.Add;
            SkirmishProductionService.ProductionLedgerObserver=sink;
            try
            {
                // Existing actual registry/grid refusal fixture checks real banks,
                // population, BlockedSpawn and refusal of a second refund.
                new SkirmishExpandedEconomyTests().BlockedRegistryDispatchRefundsOnceWithoutPromotingPopulation();
                Assert.AreEqual(2,rows.Count);Assert.AreEqual("DirectDebit",rows[0].Operation);Assert.AreEqual("FailedDispatchRefund",rows[1].Operation);
                Assert.AreEqual(rows[0].After.Bank,rows[1].Before.Bank);
                Assert.AreEqual(rows[0].Before.Materials,rows[1].After.Materials);
                Assert.AreEqual(rows[0].After.Reservation.ReservationId,rows[1].After.Reservation.ReservationId);
            }
            finally { if(SkirmishProductionService.ProductionLedgerObserver==sink)SkirmishProductionService.ProductionLedgerObserver=null; }
        }

        [Test]
        public void LedgerUnaffordableRequestHasNoCommittedOrigin()
        {
            WithCommittedProductionLedger((em,session,rows)=>{
                SkirmishMaterialsService.Write(em,session,1,0);
                var before=em.GetComponentData<FactionTacticalMaterialsComponent>(SkirmishMaterialsService.FindEconomy(em,1));
                var authored=SkirmishExpansionCatalogFactory.CreateInMemory();
                Assert.IsFalse(SkirmishProductionService.TryProduce(em,session,SkirmishRoleIds.Tank,1,authored.ArmyGround,out _));
                Assert.IsFalse(SkirmishProductionService.TryQueue(em,session,SkirmishRoleIds.Rifle,1,authored.ArmyGround,out _));
                Assert.AreEqual(0,rows.Count);Assert.AreEqual(before,em.GetComponentData<FactionTacticalMaterialsComponent>(SkirmishMaterialsService.FindEconomy(em,1)));
            });
        }

        [Test]
        public void LedgerOrdinarySinkFailureCannotChangeAcceptedCommit()
        {
            WithCommittedProductionLedger((em,session,rows)=>{
                var original=SkirmishProductionService.ProductionLedgerObserver;
                long faults=SkirmishProductionService.ProductionLedgerObservationFaults;
                SkirmishProductionService.ProductionLedgerObserver=_=>throw new InvalidOperationException("fixture observer failure");
                try
                {
                    int before=SkirmishMaterialsService.Read(em,session,1);var authored=SkirmishExpansionCatalogFactory.CreateInMemory();
                    Assert.IsTrue(SkirmishProductionService.TryQueue(em,session,SkirmishRoleIds.Rifle,1,authored.ArmyGround,out var queued));
                    Assert.AreEqual(before-queued.MaterialsCost,SkirmishMaterialsService.Read(em,session,1));
                    Assert.AreEqual(SkirmishReservationPhase.Reserved,ReservationPhase(em,session,queued.ReservationId));
                    Assert.AreEqual(faults+1,SkirmishProductionService.ProductionLedgerObservationFaults);
                }
                finally { SkirmishProductionService.ProductionLedgerObserver=original; }
            });
        }

        [Test]
        public void LedgerDefaultOffPreservesRealProduction()
        {
            WithCommittedProductionLedger((em,session,rows)=>{
                var original=SkirmishProductionService.ProductionLedgerObserver;
                long faults=SkirmishProductionService.ProductionLedgerObservationFaults;
                SkirmishProductionService.ProductionLedgerObserver=null;
                try
                {
                    int before=SkirmishMaterialsService.Read(em,session,1);var authored=SkirmishExpansionCatalogFactory.CreateInMemory();
                    Assert.IsTrue(SkirmishProductionService.TryProduce(em,session,SkirmishRoleIds.Tank,1,authored.ArmyGround,out var decision));
                    Assert.AreEqual(before-decision.MaterialsCost,SkirmishMaterialsService.Read(em,session,1));
                    Assert.AreEqual(0,rows.Count);Assert.AreEqual(faults,SkirmishProductionService.ProductionLedgerObservationFaults);
                }
                finally { SkirmishProductionService.ProductionLedgerObserver=original; }
            });
        }

        private static void AssertDirectMemberIdentities(EntityManager em,Entity session,uint reservation,int count,
            System.Collections.Generic.List<SkirmishProductionService.ProductionLedgerObservation> rows)
        {
            var expected=new System.Collections.Generic.HashSet<Entity>();
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            using(var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent)))
            using(var entities=query.ToEntityArray(Allocator.Temp))
                foreach(var entity in entities)
                {
                    var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                    if(owned.SessionId.Equals(native.SessionId)&&owned.ReservationId==reservation)expected.Add(entity);
                }
            Assert.AreEqual(count,expected.Count,"Compare real committed members, not expected DTOs");
            var actual=new System.Collections.Generic.HashSet<Entity>();int ordinal=0;
            foreach(var row in rows)
            {
                if(row.Operation!="DirectMember")continue;
                Assert.AreEqual(session,row.Session);Assert.IsTrue(row.HasActorOwner);
                Assert.AreEqual(ordinal++,row.MemberOrdinal);Assert.IsTrue(actual.Add(row.Actor),"Duplicate member receipt");
                Assert.IsTrue(expected.Contains(row.Actor));
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(row.Actor);
                Assert.AreEqual(owned,row.ActorOwner);Assert.IsTrue(owned.SessionId.Equals(native.SessionId));
                Assert.AreEqual(reservation,owned.ReservationId);Assert.AreEqual(1,owned.FactionId);
                Assert.AreEqual(0,owned.IsStructure);Assert.AreEqual(0,owned.CapacityReleased);
                Assert.IsFalse(owned.StableObjectId.IsEmpty);
                Assert.IsFalse(em.GetComponentData<UnitSourcePrefabKey>(row.Actor).Value.IsEmpty);
                Assert.AreEqual(row.Before.Bank,row.After.Bank,"A member receipt cannot imply a second debit");
                Assert.AreEqual(SkirmishReservationPhase.Live,row.After.Reservation.Phase);
                Assert.AreEqual(reservation,row.After.Reservation.ReservationId);
                Assert.AreEqual(count,row.After.Reservation.MemberCount);
                // Direct spawning does not update the native delivered counter.
                Assert.AreEqual(0,row.After.Reservation.DeliveredMembers,"Do not manufacture native delivery state");
            }
            Assert.AreEqual(count,ordinal);Assert.IsTrue(expected.SetEquals(actual));
        }

        [Test]
        public void LedgerDirectRifleIdentitySetMatchesActualReservationMembers()
        {
            WithCommittedProductionLedger((em,session,rows)=>{
                var authored=SkirmishExpansionCatalogFactory.CreateInMemory();
                Assert.IsTrue(SkirmishProductionService.TryProduce(em,session,SkirmishRoleIds.Rifle,1,authored.ArmyGround,out var decision));
                Assert.Greater(decision.MemberCount,1,"Exercise a real multi-member configured squad");
                Assert.AreEqual(2+decision.MemberCount,rows.Count);
                AssertDirectMemberIdentities(em,session,decision.ReservationId,decision.MemberCount,rows);
            });
        }

        public static void RunProductionCommittedOriginsFocusedControls()
        {
            var suite=new SkirmishExpandedEconomyTests();
            suite.LedgerDirectDebitDispatchAndDeathReflectCommittedEcs();
            suite.LedgerQueuedDirectDispatchDoesNotDebitAgain();
            suite.LedgerNativeStartAndRealMemberAdoptionStayBound();
            suite.LedgerBlockedRealSpawnCapturesDebitThenOneRefund();
            suite.LedgerUnaffordableRequestHasNoCommittedOrigin();
            suite.LedgerOrdinarySinkFailureCannotChangeAcceptedCommit();
            suite.LedgerDefaultOffPreservesRealProduction();
            suite.LedgerDirectRifleIdentitySetMatchesActualReservationMembers();
            Debug.Log("[ProductionCommittedOriginsControls] result=Passed ecsCases=8 directMemberIdentity=1 paidResourcesQualified=0 missionReady=0");
        }

        [Test]
        public void NativeDeliveryRejectsStaleAttemptPauseAndTerminalState()
        {
            using var world = new World(nameof(NativeDeliveryRejectsStaleAttemptPauseAndTerminalState));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            var nativeState = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            nativeState.Phase = SkirmishSessionPhase.Playing;
            em.SetComponentData(session, nativeState);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var key = new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Rifle));
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyGround, out var receipt));
            Assert.IsTrue(SkirmishProductionService.BindNativeQueue(em, session, receipt.ReservationId, 42));
            Assert.IsFalse(SkirmishProductionService.CanDeliverNative(em, session, receipt.ReservationId, 42, 1, key, new FixedString64Bytes("previous-attempt")));
            Assert.IsTrue(SkirmishProductionService.CanDeliverNative(em, session, receipt.ReservationId, 42, 1, key, state.SessionId));
            em.AddComponent<SkirmishObjectiveClockComponent>(session);
            var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            clock.Paused = 1; em.SetComponentData(session, clock);
            Assert.IsFalse(SkirmishProductionService.CanDeliverNative(em, session, receipt.ReservationId, 42, 1, key, state.SessionId));
            clock.Paused = 0; em.SetComponentData(session, clock);
            state.Phase = SkirmishSessionPhase.Finished; em.SetComponentData(session, state);
            Assert.IsFalse(SkirmishProductionService.CanDeliverNative(em, session, receipt.ReservationId, 42, 1, key, state.SessionId));
            Assert.AreEqual(SkirmishReservationPhase.Reserved, ReservationPhase(em, session, receipt.ReservationId));
        }

        [Test]
        public void NativeProducerLossReleasesOnlyItsOwnQueue()
        {
            using var world = new World(nameof(NativeProducerLossReleasesOnlyItsOwnQueue));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            var nativeState = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            nativeState.Phase = SkirmishSessionPhase.Playing;
            em.SetComponentData(session, nativeState);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyGround, out var first));
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyGround, out var second));
            Assert.IsTrue(SkirmishProductionService.BindNativeQueue(em, session, first.ReservationId, 42));
            Assert.IsTrue(SkirmishProductionService.BindNativeQueue(em, session, second.ReservationId, 43));
            SkirmishProductionService.StartNativeQueue(em, session, first.ReservationId, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
            Assert.AreEqual(1, SkirmishProductionService.NotifyProducerDestroyed(em, session, SkirmishProducerKind.Barracks, 1, 42));
            Assert.AreEqual(SkirmishReservationPhase.Lost, ReservationPhase(em, session, first.ReservationId));
            Assert.AreEqual(SkirmishReservationPhase.Reserved, ReservationPhase(em, session, second.ReservationId));
            Assert.AreEqual(4, em.GetComponentData<SkirmishCapacityComponent>(session).InfantryReserved);
            Assert.AreEqual(materials - 160, SkirmishMaterialsService.Read(em, session, 1));
            Assert.AreEqual(0, SkirmishProductionService.NotifyProducerDestroyed(em, session, SkirmishProducerKind.Barracks, 1, 42));
            Assert.AreEqual(1, SkirmishProductionService.NotifyProducerDestroyed(em, session, SkirmishProducerKind.Barracks, 1, 43));
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).InfantryReserved);
            Assert.AreEqual(materials - 80, SkirmishMaterialsService.Read(em, session, 1), "The unstarted queue is refunded once.");
        }

        [Test]
        public void NativePacketDeliveryKeepsOneGroupAndRefundsOnlyUndeliveredMembers()
        {
            using var world = new World(nameof(NativePacketDeliveryKeepsOneGroupAndRefundsOnlyUndeliveredMembers));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            var nativeState = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            nativeState.Phase = SkirmishSessionPhase.Playing;
            em.SetComponentData(session, nativeState);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            int materials = SkirmishMaterialsService.Read(em, session, 1);
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Rifle, 1,
                authored.ArmyGround, out var decision));
            Assert.IsTrue(SkirmishProductionService.BindNativeQueue(em, session, decision.ReservationId, 42));
            uint group = 0;
            Entity first = Entity.Null;
            for (int i = 0; i < 2; i++)
            {
                var member = em.CreateEntity(typeof(Faction), typeof(UnitSourcePrefabKey), typeof(UnitHealth));
                em.SetComponentData(member, new Faction { Id = 1 });
                em.SetComponentData(member, new UnitSourcePrefabKey {
                    Value = new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Rifle)) });
                Assert.IsFalse(SkirmishProductionService.AdoptNativeMember(em, session, decision.ReservationId, 99, member, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId));
                Assert.IsTrue(SkirmishProductionService.AdoptNativeMember(em, session, decision.ReservationId, 42, member, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId));
                Assert.IsFalse(SkirmishProductionService.AdoptNativeMember(em, session, decision.ReservationId, 42, member, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId));
                var receipt = em.GetBuffer<SkirmishProductionReservation>(session)[0];
                if (i == 0) { group = receipt.ProductionGroupId; first = member; }
                Assert.AreEqual(group, receipt.ProductionGroupId);
                Assert.Greater(em.GetComponentData<UnitHealth>(member).Current, 0);
                Assert.IsTrue(em.HasComponent<SkirmishSharedActorTag>(member));
            }
            var halfway = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(before.InfantryLive + 2, halfway.InfantryLive);
            Assert.AreEqual(2, halfway.InfantryReserved);
            Assert.IsTrue(SkirmishProductionService.TryCancel(em, session, decision.ReservationId, out var cancel));
            Assert.AreEqual(30, cancel.RefundedMaterials, "75% of the two unspawned recruits, never the two delivered recruits.");
            Assert.AreEqual(materials - 80 + 30, SkirmishMaterialsService.Read(em, session, 1));
            var after = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(before.InfantryLive + 2, after.InfantryLive);
            Assert.AreEqual(0, after.InfantryReserved);
            Assert.AreEqual(0, after.SupplyReserved);
            Assert.AreEqual(2, em.GetBuffer<SkirmishProductionReservation>(session)[0].RemainingMembers);
            Assert.IsFalse(SkirmishProductionService.TryCancel(em, session, decision.ReservationId, out _));
            Assert.IsTrue(SkirmishProductionService.TryReleaseDeath(em, session, first));
            Assert.AreEqual(before.InfantryLive + 1, em.GetComponentData<SkirmishCapacityComponent>(session).InfantryLive);
        }

        [Test]
        public void S002StartingStocksAndCapsSeedFromMatrix()
        {
            using var world = new World(nameof(S002StartingStocksAndCapsSeedFromMatrix));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out SkirmishResolvedSetup setup);
            var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
            Assert.AreEqual(900, stock.Materials);
            Assert.AreEqual(240, stock.Oil);
            Assert.AreEqual(700, stock.Fuel);
            var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(48, capacity.InfantryCap);
            Assert.AreEqual(8, capacity.GroundCap);
            Assert.AreEqual(128, capacity.SupplyCap);
            Assert.AreEqual(setup.PlayerInfantry, capacity.InfantryLive);
            Assert.AreEqual(setup.PlayerGround, capacity.GroundLive);
            Assert.AreEqual(0, capacity.InfantryReserved);
        }

        [Test]
        public void CapacityLedgerReservePromoteReleaseOnce()
        {
            var capacity = new SkirmishCapacitySnapshot
            {
                GroundCap = 8,
                SupplyCap = 128
            };
            Assert.IsTrue(SkirmishCapacityLedger.TryReserve(
                ref capacity, SkirmishPopulationCategory.Ground, 1, 6));
            Assert.AreEqual(1, capacity.GroundReserved);
            Assert.AreEqual(6, capacity.SupplyReserved);
            Assert.IsTrue(SkirmishCapacityLedger.TryReleaseReserved(
                ref capacity, SkirmishPopulationCategory.Ground, 1, 6));
            Assert.AreEqual(0, capacity.GroundReserved);
            Assert.IsTrue(SkirmishCapacityLedger.TryReserve(
                ref capacity, SkirmishPopulationCategory.Ground, 1, 6));
            Assert.IsFalse(SkirmishCapacityLedger.TryReserve(
                ref capacity, SkirmishPopulationCategory.Ground, 8, 48));
            SkirmishCapacityLedger.PromoteLive(ref capacity, SkirmishPopulationCategory.Ground, 1, 6);
            Assert.AreEqual(1, capacity.GroundLive);
            Assert.AreEqual(0, capacity.GroundReserved);
            Assert.IsTrue(SkirmishCapacityLedger.TryReleaseDeath(
                ref capacity, SkirmishPopulationCategory.Ground, 6));
            Assert.IsFalse(SkirmishCapacityLedger.TryReleaseDeath(
                ref capacity, SkirmishPopulationCategory.Ground, 6));
            Assert.AreEqual(0, capacity.GroundLive);
            Assert.AreEqual(0, capacity.SupplyLive);
        }

        [Test]
        public void GroundStagingProducesTankThenDeathReleasesOnce()
        {
            using var world = new World(nameof(GroundStagingProducesTankThenDeathReleasesOnce));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            int materials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision tank));
            Assert.AreEqual(360, tank.MaterialsCost);
            Assert.AreEqual(0, tank.FuelCost);
            Assert.AreEqual(6, tank.SupplyCost);
            Assert.AreEqual(1, tank.MemberCount);
            Assert.AreEqual(SkirmishProducerKind.GroundStaging, tank.Producer);
            Assert.AreEqual(materials - 360, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(700, em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel);
            var after = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(before.GroundLive + 1, after.GroundLive);
            Assert.AreEqual(before.SupplyLive + 6, after.SupplyLive);
            Assert.AreEqual(0, after.GroundReserved);
            Assert.AreEqual(SkirmishReservationPhase.Live, ReservationPhase(em, session, tank.ReservationId));

            Entity produced = FindReservedUnit(em, tank.ReservationId);
            Assert.AreNotEqual(Entity.Null, produced);
            Assert.AreEqual("Unit_Veh_Tank_USA", em.GetComponentData<UnitSourcePrefabKey>(produced).Value.ToString());
            var health = em.GetComponentData<UnitHealth>(produced);
            health.Current = 0;
            em.SetComponentData(produced, health);
            Assert.IsTrue(SkirmishProductionService.TryReleaseDeath(em, session, produced));
            Assert.IsFalse(SkirmishProductionService.TryReleaseDeath(em, session, produced));
            var released = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(before.GroundLive, released.GroundLive);
            Assert.AreEqual(before.SupplyLive, released.SupplyLive);
            Assert.AreEqual(SkirmishReservationPhase.Released, ReservationPhase(em, session, tank.ReservationId));
        }

        [Test]
        public void CapacityLifecycleReleasesDeadOwnedUnitsOnce()
        {
            using var world = new World(nameof(CapacityLifecycleReleasesDeadOwnedUnitsOnce));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision tank));
            Entity produced = FindReservedUnit(em, tank.ReservationId);
            var health = em.GetComponentData<UnitHealth>(produced);
            health.Current = 0;
            em.SetComponentData(produced, health);
            using var owned = em.CreateEntityQuery(
                ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),
                ComponentType.ReadOnly<SkirmishUnitRoleComponent>(),
                ComponentType.ReadOnly<UnitHealth>());
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            Assert.AreEqual(1, SkirmishCapacityLifecycleSystem.ReleaseDead(em, session, owned, sessionId));
            Assert.AreEqual(0, SkirmishCapacityLifecycleSystem.ReleaseDead(em, session, owned, sessionId));
            Assert.AreEqual(before.GroundLive, em.GetComponentData<SkirmishCapacityComponent>(session).GroundLive);
            Assert.AreEqual(SkirmishReservationPhase.Released, ReservationPhase(em, session, tank.ReservationId));
        }

        [Test]
        public void InfantrySquadCostsFourMembersAndMaterials()
        {
            using var world = new World(nameof(InfantrySquadCostsFourMembersAndMaterials));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyGround, out SkirmishProductionDecision rifle));
            Assert.AreEqual(4, rifle.MemberCount);
            Assert.AreEqual(80, rifle.MaterialsCost);
            Assert.AreEqual(before.InfantryLive + 4, em.GetComponentData<SkirmishCapacityComponent>(session).InfantryLive);
        }

        [Test]
        public void MissingMaterialsAndCapsRejectWithoutMutation()
        {
            using var world = new World(nameof(MissingMaterialsAndCapsRejectWithoutMutation));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
            stock.Materials = 10;
            em.SetComponentData(session, stock);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision poor));
            Assert.AreEqual(SkirmishReasonCode.InsufficientMaterials, poor.Reason);
            Assert.AreEqual(10, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            stock.Materials = 900;
            em.SetComponentData(session, stock);
            var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
            capacity.GroundLive = capacity.GroundCap;
            em.SetComponentData(session, capacity);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision full));
            Assert.AreEqual(SkirmishReasonCode.InsufficientCapacity, full.Reason);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
        }

        [Test]
        public void FuelGateRejectsWithoutStockMutation()
        {
            SkirmishRoleOverlay[] overlays = SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
            for (int i = 0; i < overlays.Length; i++)
            {
                if (overlays[i].RoleKind != SkirmishRoleKind.Tank)
                    continue;
                overlays[i].FuelCost = 40;
                break;
            }

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            SkirmishProductionDecision dry = SkirmishProductionEligibility.Evaluate(
                new SkirmishProductionRequest
                {
                    RoleId = SkirmishRoleIds.Tank,
                    RoleKind = SkirmishRoleKind.Tank,
                    SquadCount = 1,
                    BarracksPresent = true,
                    GroundStagingPresent = true,
                    EnforceStocks = true,
                    MaterialsAvailable = 900,
                    FuelAvailable = 0,
                    GroundCap = 8,
                    SupplyCap = 128
                },
                authored.ArmyGround,
                SkirmishReadinessStage.Established,
                overlays);
            Assert.IsFalse(dry.Accepted);
            Assert.AreEqual(SkirmishReasonCode.InsufficientFuel, dry.Reason);
            Assert.AreEqual(40, dry.FuelCost);
        }

        [Test]
        public void RecruitmentFuelCostStaysZeroOnS002Overlays()
        {
            SkirmishRoleOverlay[] overlays = SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
            for (int i = 0; i < overlays.Length; i++)
                Assert.AreEqual(0, overlays[i].FuelCost, overlays[i].RoleId);
        }

        [Test]
        public void PlayerAndEnemyProductionReceiptsNeverShareAnIdOrRefundTheOtherFaction()
        {
            using var world = new World(nameof(PlayerAndEnemyProductionReceiptsNeverShareAnIdOrRefundTheOtherFaction));
            var em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.GroundManeuver);
            int playerBefore = SkirmishMaterialsService.Read(em, session, 1);
            int enemyBefore = SkirmishMaterialsService.Read(em, session, 2);
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Tank, 1, army, 1, out var player));
            Assert.IsTrue(SkirmishProductionService.TryQueue(em, session, SkirmishRoleIds.Car, 1, army, 2, out var enemy));
            Assert.AreNotEqual(player.ReservationId, enemy.ReservationId);
            Assert.IsTrue(SkirmishProductionService.TryCancel(em, session, enemy.ReservationId, out _));
            Assert.AreEqual(playerBefore - player.MaterialsCost, SkirmishMaterialsService.Read(em, session, 1));
            Assert.AreEqual(enemyBefore, SkirmishMaterialsService.Read(em, session, 2));
            Assert.AreEqual(SkirmishReservationPhase.Reserved, ReservationPhase(em, session, player.ReservationId));
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).GroundReserved);
            Assert.AreEqual(0, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).GroundReserved);
        }

        [Test]
        public void MidProduceCancelRefundsReservedThenSeventyFivePercent()
        {
            using var world = new World(nameof(MidProduceCancelRefundsReservedThenSeventyFivePercent));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision queued));
            Assert.AreEqual(SkirmishReservationPhase.Reserved, ReservationPhase(em, session, queued.ReservationId));
            Assert.AreEqual(540, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).GroundReserved);
            Assert.IsTrue(SkirmishProductionService.TryCancel(
                em, session, queued.ReservationId, out SkirmishProductionDecision fullRefund));
            Assert.AreEqual(360, fullRefund.RefundedMaterials);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).GroundReserved);
            Assert.AreEqual(SkirmishReservationPhase.Cancelled, ReservationPhase(em, session, queued.ReservationId));

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision started));
            Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, started.ReservationId, out _));
            Assert.AreEqual(SkirmishReservationPhase.Producing, ReservationPhase(em, session, started.ReservationId));
            Assert.IsTrue(SkirmishProductionService.IsProducerLocked(em, session, SkirmishProducerKind.GroundStaging, 1));
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Car, 1, authored.ArmyGround, out SkirmishProductionDecision locked));
            Assert.AreEqual(SkirmishReasonCode.QueueLocked, locked.Reason);
            Assert.IsTrue(SkirmishProductionService.TryCancel(
                em, session, started.ReservationId, out SkirmishProductionDecision partial));
            Assert.AreEqual(270, partial.RefundedMaterials);
            Assert.AreEqual(810, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.IsFalse(SkirmishProductionService.IsProducerLocked(em, session, SkirmishProducerKind.GroundStaging, 1));
        }

        [Test]
        public void FailedDispatchAndProducerDestructionUseSpecifiedRefunds()
        {
            using var world = new World(nameof(FailedDispatchAndProducerDestructionUseSpecifiedRefunds));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision failed));
            Assert.IsTrue(SkirmishProductionService.TryRefundFailedDispatch(
                em, session, failed.ReservationId, out SkirmishProductionDecision spawnRefund));
            Assert.AreEqual(360, spawnRefund.RefundedMaterials);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision producing));
            Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, producing.ReservationId, out _));
            Assert.AreEqual(1, SkirmishProductionService.NotifyProducerDestroyed(
                em, session, SkirmishProducerKind.GroundStaging, 1));
            Assert.AreEqual(SkirmishReservationPhase.Lost, ReservationPhase(em, session, producing.ReservationId));
            Assert.AreEqual(540, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).GroundReserved);

            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision live));
            Assert.IsFalse(SkirmishProductionService.TryCancel(em, session, live.ReservationId, out SkirmishProductionDecision launched));
            Assert.AreEqual(SkirmishReasonCode.NotCancellable, launched.Reason);
        }

        [Test]
        public void FactionResearchChargesAndUpgradesOnlyItsOwner()
        {
            using var world = new World(nameof(FactionResearchChargesAndUpgradesOnlyItsOwner));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            var player = FirstRole(em, SkirmishRoleKind.Rifle, 1);
            var enemy = FirstRole(em, SkirmishRoleKind.Rifle, 2);
            int playerDamage = em.GetComponentData<SkirmishRoleOverlayComponent>(player).Damage;
            int enemyDamage = em.GetComponentData<SkirmishRoleOverlayComponent>(enemy).Damage;
            em.AddComponentData(player, new UnitAttack { Damage = playerDamage });
            em.AddComponentData(enemy, new UnitAttack { Damage = enemyDamage });
            int playerMaterials = SkirmishMaterialsService.Read(em, session, 1);
            int enemyMaterials = SkirmishMaterialsService.Read(em, session, 2);
            Assert.IsTrue(SkirmishResearchService.TryQueue(em, session, SkirmishResearchKind.InfantryWeapons, 1, out var p));
            Assert.IsTrue(SkirmishResearchService.TryQueue(em, session, SkirmishResearchKind.InfantryWeapons, 2, out var e));
            Assert.AreNotEqual(p.ResearchId, e.ResearchId);
            Assert.AreEqual(playerMaterials - p.MaterialsCost, SkirmishMaterialsService.Read(em, session, 1));
            Assert.AreEqual(enemyMaterials - e.MaterialsCost, SkirmishMaterialsService.Read(em, session, 2));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, e.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, e.ResearchId, out var completed));
            Assert.AreEqual(1, completed.LevelAfter);
            Assert.AreEqual(playerDamage, em.GetComponentData<UnitAttack>(player).Damage);
            Assert.AreEqual(SkirmishResearchCosts.Scale(enemyDamage, 110), em.GetComponentData<UnitAttack>(enemy).Damage);
            Assert.IsFalse(SkirmishResearchService.HasCompleted(em, session, SkirmishResearchKind.InfantryWeapons, 1));
            Assert.IsTrue(SkirmishResearchService.HasCompleted(em, session, SkirmishResearchKind.InfantryWeapons, 2));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, p.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, p.ResearchId, out _));
            Assert.AreEqual(SkirmishResearchCosts.Scale(playerDamage, 110), em.GetComponentData<UnitAttack>(player).Damage);
            Assert.IsFalse(SkirmishResearchService.TryComplete(em, session, e.ResearchId, out _));
            Assert.AreEqual(SkirmishResearchCosts.Scale(enemyDamage, 110), em.GetComponentData<UnitAttack>(enemy).Damage);
        }

        [Test]
        public void PaidEnemyReadinessDoesNotUnlockPlayerOrChangePlayerPrice()
        {
            using var world = new World(nameof(PaidEnemyReadinessDoesNotUnlockPlayerOrChangePlayerPrice));
            var em = world.EntityManager;
            CompileAndSpawnS003(em, out var session, out _);
            Assert.IsTrue(SkirmishResearchService.TryQueue(em, session, SkirmishResearchKind.Readiness, 2, out var e));
            Assert.AreEqual(SkirmishResearchCosts.ReadinessEstablishedMaterials, e.MaterialsCost);
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, e.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, e.ResearchId, out var completed));
            Assert.AreEqual(SkirmishReadinessStage.Established, completed.ReadinessAfter);
            Assert.IsTrue(SkirmishResearchService.TryReadState(em, session, 1, out var pState));
            Assert.IsTrue(SkirmishResearchService.TryReadState(em, session, 2, out var eState));
            Assert.AreEqual(SkirmishReadinessStage.Field, pState.Readiness);
            Assert.AreEqual(SkirmishReadinessStage.Established, eState.Readiness);
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.AirMobile);
            var enemyAir = SkirmishProductionService.EvaluateQueue(em, session, SkirmishRoleIds.AttackHeliLight, 1, army, 2);
            var playerAir = SkirmishProductionService.EvaluateQueue(em, session, SkirmishRoleIds.AttackHeliLight, 1, army, 1);
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, enemyAir.Reason);
            Assert.AreEqual(SkirmishReasonCode.MissingReadiness, playerAir.Reason);
            Assert.IsTrue(SkirmishResearchService.CanQueue(em, session, SkirmishResearchKind.Readiness, 1, out var p, out _));
            Assert.AreEqual(SkirmishResearchCosts.ReadinessEstablishedMaterials, p.MaterialsCost);
            Assert.IsFalse(SkirmishResearchService.CanQueue(em, session, SkirmishResearchKind.Readiness, 0, out var invalid, out _));
            Assert.AreEqual("faction", invalid.Field);
            Assert.AreEqual(450, SkirmishMaterialsService.Read(em, session, 1));
        }

        [Test]
        public void LegacyCheckpointWithoutEnemyResearchUsesCompiledOpeningStage()
        {
            using var world = new World(nameof(LegacyCheckpointWithoutEnemyResearchUsesCompiledOpeningStage));
            var em = world.EntityManager;
            CompileAndSpawnS003(em, out var session, out _);
            em.RemoveComponent<SkirmishEnemyResearchStateComponent>(session);
            var p = em.GetComponentData<SkirmishResearchStateComponent>(session);
            p.Readiness = SkirmishReadinessStage.FullArsenal; p.InfantryWeapons = 1;
            em.SetComponentData(session, p);
            Assert.IsTrue(SkirmishResearchService.TryReadState(em, session, 2, out var e));
            Assert.AreEqual(SkirmishReadinessStage.Field, e.Readiness);
            Assert.AreEqual(0, e.InfantryWeapons);
            Assert.IsFalse(em.HasComponent<SkirmishEnemyResearchStateComponent>(session), "Eligibility must remain read-only.");
        }

        [Test]
        public void CategoryResearchFromStagingAndHqAppliesOnce()
        {
            using var world = new World(nameof(CategoryResearchFromStagingAndHqAppliesOnce));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            var research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            Assert.AreEqual(SkirmishReadinessStage.Established, research.Readiness);
            Assert.AreEqual(0, research.VehicleProtection);
            Assert.AreEqual(0, research.InfantryWeapons);

            Entity tank = FirstRole(em, SkirmishRoleKind.Tank, 1);
            var health = em.GetComponentData<UnitHealth>(tank);
            health.Current = health.Max / 2;
            em.SetComponentData(tank, health);
            int half = health.Current;
            int baseMax = health.Max;
            Entity rifle = FirstRole(em, SkirmishRoleKind.Rifle, 1);
            int rifleDamage = em.GetComponentData<SkirmishRoleOverlayComponent>(rifle).Damage;

            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.VehicleProtection, 1, out SkirmishResearchDecision vehicle));
            Assert.AreEqual(200, vehicle.MaterialsCost);
            Assert.AreEqual(700, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, vehicle.ResearchId, out _));
            Assert.IsTrue(SkirmishProductionService.IsProducerLocked(em, session, SkirmishProducerKind.GroundStaging, 1));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, vehicle.ResearchId, out SkirmishResearchDecision done));
            Assert.AreEqual(1, done.LevelAfter);
            Assert.AreEqual(1, em.GetComponentData<SkirmishResearchStateComponent>(session).VehicleProtection);
            var upgraded = em.GetComponentData<UnitHealth>(tank);
            Assert.AreEqual(SkirmishResearchCosts.Scale(baseMax, 110), upgraded.Max);
            Assert.AreEqual((half * upgraded.Max + baseMax / 2) / baseMax, upgraded.Current);
            Assert.AreNotEqual(upgraded.Max, upgraded.Current);

            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.VehicleProtection, 1, out SkirmishResearchDecision again));
            Assert.AreEqual(SkirmishReasonCode.AlreadyCompleted, again.Reason);
            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.AircraftEfficiency, 1, out SkirmishResearchDecision air));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, air.Reason);

            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.InfantryWeapons, 1, out SkirmishResearchDecision infantry));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, infantry.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, infantry.ResearchId, out _));
            Assert.AreEqual(
                SkirmishResearchCosts.Scale(rifleDamage, 110),
                em.GetComponentData<SkirmishRoleOverlayComponent>(rifle).Damage);

            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.Readiness, 1, out SkirmishResearchDecision r3));
            Assert.AreEqual(480, r3.MaterialsCost);
            int afterReadinessPurchase = SkirmishMaterialsService.Read(em, session, 1);
            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.Readiness, 1, out SkirmishResearchDecision duplicateReadiness));
            Assert.AreEqual(SkirmishReasonCode.QueueLocked, duplicateReadiness.Reason);
            Assert.AreEqual(afterReadinessPurchase, SkirmishMaterialsService.Read(em, session, 1));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, r3.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, r3.ResearchId, out SkirmishResearchDecision arsenal));
            Assert.AreEqual(SkirmishReadinessStage.FullArsenal, arsenal.ReadinessAfter);
        }

        [Test]
        public void ReplacementHqCanResearchWithoutBecomingVictoryBase()
        {
            using var world = new World(nameof(ReplacementHqCanResearchWithoutBecomingVictoryBase));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            Entity designated = FirstDesignated(em, SkirmishObjectiveRoleKind.PlayerBase);
            Assert.AreNotEqual(Entity.Null, designated);
            var designatedHealth = em.GetComponentData<UnitHealth>(designated);
            designatedHealth.Current = 0;
            em.SetComponentData(designated, designatedHealth);
            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.InfantryWeapons, 1, out SkirmishResearchDecision missing));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, missing.Reason);

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            Entity replacement = SkirmishScenarioSpawnSystem.CreateStructure(
                em,
                sessionId,
                new SkirmishResolvedStructureEntry
                {
                    FactionId = 1,
                    StructureId = SkirmishStructureIds.BarracksReplacement,
                    ObjectiveRoleId = string.Empty,
                    DesignatedBase = false
                });
            em.AddComponentData(replacement, new UnitHealth { Current = 800, Max = 800 });
            Assert.IsFalse(em.HasComponent<SkirmishObjectiveRoleComponent>(replacement));
            Assert.AreEqual(0, em.GetComponentData<SkirmishStructureIdentityComponent>(replacement).DesignatedBase);
            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.InfantryWeapons, 1, out SkirmishResearchDecision fromReplacement));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, fromReplacement.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryCancel(em, session, fromReplacement.ResearchId, out SkirmishResearchDecision cancelled));
            Assert.AreEqual(150, cancelled.RefundedMaterials);

            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.InfantryWeapons, 1, out SkirmishResearchDecision researching));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, researching.ResearchId, out _));
            em.SetComponentData(replacement, new UnitHealth { Current = 0, Max = 800 });
            Assert.AreEqual(1, SkirmishResearchService.NotifyProducerDestroyed(
                em, session, SkirmishProducerKind.Barracks, 1));
            Assert.AreEqual(SkirmishResearchPhase.Lost, ResearchPhase(em, session, researching.ResearchId));
            Assert.AreEqual(0, em.GetComponentData<SkirmishResearchStateComponent>(session).InfantryWeapons);
        }

        [Test]
        public void S003AirMobileAaGatePadReadinessAndMissingPadRefund()
        {
            using var world = new World(nameof(S003AirMobileAaGatePadReadinessAndMissingPadRefund));
            EntityManager em = world.EntityManager;
            CompileAndSpawnS003(em, out Entity session, out _);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(350, em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel);
            Assert.AreEqual(SkirmishReadinessStage.Field, em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(2, em.GetComponentData<SkirmishCapacityComponent>(session).AirCap);

            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyAir, out SkirmishProductionDecision tank));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, tank.Reason);
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            Assert.IsFalse(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision early));
            Assert.AreEqual(SkirmishReasonCode.MissingReadiness, early.Reason);
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);

            var research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            research.Readiness = SkirmishReadinessStage.Established;
            em.SetComponentData(session, research);
            Assert.IsFalse(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision missingPad));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, missingPad.Reason);
            Assert.AreEqual("producer.helipad", missingPad.Field);
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            Entity pad = SkirmishScenarioSpawnSystem.CreateStructure(
                em,
                sessionId,
                new SkirmishResolvedStructureEntry
                {
                    FactionId = 1,
                    StructureId = SkirmishStructureIds.Helipad
                });
            em.AddComponentData(pad, new UnitHealth { Current = 400, Max = 400 });

            var capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            int airLive = capped.AirLive;
            capped.AirLive = capped.AirCap;
            em.SetComponentData(session, capped);
            Assert.IsFalse(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision full));
            Assert.AreEqual(SkirmishReasonCode.InsufficientCapacity, full.Reason);
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            capped.AirLive = airLive;
            em.SetComponentData(session, capped);

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision queued));
            Assert.AreEqual(420, queued.MaterialsCost);
            Assert.AreEqual(8, queued.SupplyCost);
            Assert.AreEqual(SkirmishProducerKind.Helipad, queued.Producer);
            Assert.AreEqual(30, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, queued.ReservationId, out _));
            Assert.AreEqual(SkirmishReservationPhase.Producing, ReservationPhase(em, session, queued.ReservationId));

            em.SetComponentData(pad, new UnitHealth { Current = 0, Max = 400 });
            Assert.IsFalse(SkirmishProductionService.TryDispatchQueued(
                em, session, queued.ReservationId, authored.ArmyAir, out SkirmishProductionDecision refunded));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, refunded.Reason);
            Assert.AreEqual("producer.helipad", refunded.Field);
            Assert.AreEqual(420, refunded.RefundedMaterials);
            Assert.AreEqual(450, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(350, em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyReserved);
            Assert.AreEqual(SkirmishReservationPhase.Cancelled, ReservationPhase(em, session, queued.ReservationId));
            Assert.AreEqual(Entity.Null, FindReservedUnit(em, queued.ReservationId));

            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.AntiAir, 1, authored.ArmyAir, out SkirmishProductionDecision aa));
            Assert.AreEqual(220, aa.MaterialsCost);
            Assert.AreEqual(6, aa.SupplyCost);
            Assert.AreEqual(SkirmishProducerKind.GroundStaging, aa.Producer);
            Assert.AreEqual(230, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            var after = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(before.GroundLive + 1, after.GroundLive);
            Assert.AreEqual(before.AirLive, after.AirLive);
            Assert.AreEqual(0, after.GroundReserved);
            Entity produced = FindReservedUnit(em, aa.ReservationId);
            Assert.AreEqual(
                "Unit_Veh_Missle_Launcher_Air",
                em.GetComponentData<UnitSourcePrefabKey>(produced).Value.ToString());
        }

        [Test]
        public void S004EstablishedGrantsDoNotDebitAndPadStartsLive()
        {
            using var world = new World(nameof(S004EstablishedGrantsDoNotDebitAndPadStartsLive));
            EntityManager em = world.EntityManager;
            CompileAndSpawnS004(em, out Entity session, out SkirmishResolvedSetup setup);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(240, em.GetComponentData<SkirmishEconomyStockComponent>(session).Oil);
            Assert.AreEqual(700, em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            var research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            Assert.AreEqual(SkirmishReadinessStage.Established, research.Readiness);
            Assert.AreEqual(0, research.InfantryWeapons);
            Assert.AreEqual(0, research.VehicleProtection);
            Assert.AreEqual(0, research.AircraftEfficiency);
            Assert.AreEqual(0u, research.NextResearchId);
            Assert.AreEqual(0, em.GetBuffer<SkirmishResearchQueueItem>(session).Length);
            var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.AreEqual(1, capacity.AirLive);
            Assert.AreEqual(setup.PlayerGround, capacity.GroundLive);
            Assert.AreEqual(42, capacity.SupplyLive);
            Assert.AreEqual(0, capacity.AirReserved);
            Entity pad = FindStructure(em, 1, SkirmishStructureIds.Helipad);
            Assert.AreNotEqual(Entity.Null, pad);
            Assert.AreEqual(500, em.GetComponentData<UnitHealth>(pad).Current);
            Assert.AreEqual(Entity.Null, FindStructure(em, 1, SkirmishStructureIds.Airport));
            Assert.AreNotEqual(Entity.Null, FindStructure(em, 2, SkirmishStructureIds.Helipad));

            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyAir, out SkirmishProductionDecision tank));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, tank.Reason);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.ApcHeavy, 1, authored.ArmyAir, out SkirmishProductionDecision heavy));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, heavy.Reason);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Siege, 1, authored.ArmyAir, out SkirmishProductionDecision siege));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, siege.Reason);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            research.Readiness = SkirmishReadinessStage.FullArsenal;
            em.SetComponentData(session, research);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Fighter, 1, authored.ArmyAir, out SkirmishProductionDecision jet));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, jet.Reason);
            Assert.AreEqual("producer.airport", jet.Field);
            research.Readiness = SkirmishReadinessStage.Established;
            em.SetComponentData(session, research);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision queued));
            Assert.AreEqual(420, queued.MaterialsCost);
            Assert.AreEqual(480, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, queued.ReservationId, out _));
            em.SetComponentData(pad, new UnitHealth { Current = 0, Max = 500 });
            Assert.IsFalse(SkirmishProductionService.TryDispatchQueued(
                em, session, queued.ReservationId, authored.ArmyAir, out SkirmishProductionDecision refunded));
            Assert.AreEqual(420, refunded.RefundedMaterials);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);

            Assert.IsFalse(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.TransportHeli, 1, authored.ArmyAir, out SkirmishProductionDecision blocked));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, blocked.Reason);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);

            var before = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.AntiAir, 1, authored.ArmyAir, out SkirmishProductionDecision aa));
            Assert.AreEqual(220, aa.MaterialsCost);
            Assert.AreEqual(680, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.AreEqual(before.GroundLive + 1, em.GetComponentData<SkirmishCapacityComponent>(session).GroundLive);
            Assert.AreEqual(before.AirLive, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
        }

        [Test]
        public void S004EstablishedGrantLedgerAirCapAndHelipadResearch()
        {
            using var world = new World(nameof(S004EstablishedGrantLedgerAirCapAndHelipadResearch));
            EntityManager em = world.EntityManager;
            CompileAndSpawnS004(em, out Entity session, out SkirmishResolvedSetup setup);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Entity grantTransport = FirstRole(em, SkirmishRoleKind.TransportHeli, 1);
            Entity grantAa = FirstRole(em, SkirmishRoleKind.AntiAir, 1);
            Assert.AreNotEqual(Entity.Null, grantTransport);
            Assert.AreNotEqual(Entity.Null, grantAa);
            Assert.AreEqual(0, em.GetBuffer<SkirmishProductionReservation>(session).Length);
            Assert.AreEqual(0u, em.GetComponentData<SkirmishAttemptOwnedComponent>(grantTransport).ReservationId);
            Assert.AreEqual(0u, em.GetComponentData<SkirmishAttemptOwnedComponent>(grantAa).ReservationId);
            Assert.AreEqual(2, setup.TacticalAirCapEach);
            Assert.AreEqual(2, em.GetComponentData<SkirmishCapacityComponent>(session).AirCap);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(42, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyLive);
            Assert.AreEqual(900, PlayerMaterials(em, session));
            Assert.AreEqual(700, PlayerFuel(em, session));
            Assert.AreEqual(240, em.GetComponentData<SkirmishEconomyStockComponent>(session).Oil);
            Assert.AreEqual(1, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).AirLive);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);

            var capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            int airLive = capped.AirLive;
            capped.AirLive = capped.AirCap;
            em.SetComponentData(session, capped);
            Assert.IsFalse(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision full));
            Assert.AreEqual(SkirmishReasonCode.InsufficientCapacity, full.Reason);
            Assert.AreEqual(900, PlayerMaterials(em, session));
            Assert.AreEqual(700, PlayerFuel(em, session));
            capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            capped.AirLive = airLive;
            em.SetComponentData(session, capped);

            Entity pad = FindStructure(em, 1, SkirmishStructureIds.Helipad);
            em.SetComponentData(pad, new UnitHealth { Current = 0, Max = 500 });
            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.AircraftEfficiency, 1, out SkirmishResearchDecision missingAir));
            Assert.AreEqual(SkirmishReasonCode.MissingProducer, missingAir.Reason);
            Assert.AreEqual("producer.air", missingAir.Field);
            Assert.AreEqual(0, em.GetComponentData<SkirmishResearchStateComponent>(session).AircraftEfficiency);
            Assert.AreEqual(900, PlayerMaterials(em, session));
            em.SetComponentData(pad, new UnitHealth { Current = 500, Max = 500 });

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.AttackHeli, 1, authored.ArmyAir, out SkirmishProductionDecision queued));
            Assert.AreEqual(420, queued.MaterialsCost);
            Assert.AreEqual(8, queued.SupplyCost);
            Assert.AreEqual(SkirmishProducerKind.Helipad, queued.Producer);
            Assert.AreEqual(480, PlayerMaterials(em, session));
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(8, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyReserved);
            Assert.IsTrue(SkirmishProductionService.TryStartQueued(em, session, queued.ReservationId, out _));
            Assert.AreEqual(SkirmishReservationPhase.Producing, ReservationPhase(em, session, queued.ReservationId));

            capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            capped.AirCap = 4;
            em.SetComponentData(session, capped);
            Assert.IsFalse(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.TransportHeli, 1, authored.ArmyAir, out SkirmishProductionDecision locked));
            Assert.AreEqual(SkirmishReasonCode.QueueLocked, locked.Reason);
            Assert.AreEqual("queue", locked.Field);
            Assert.AreEqual(480, PlayerMaterials(em, session));
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            capped = em.GetComponentData<SkirmishCapacityComponent>(session);
            capped.AirCap = 2;
            em.SetComponentData(session, capped);

            var beforeAa = em.GetComponentData<SkirmishCapacityComponent>(session);
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.AntiAir, 1, authored.ArmyAir, out SkirmishProductionDecision boughtAa));
            Assert.AreEqual(220, boughtAa.MaterialsCost);
            Assert.AreEqual(SkirmishProducerKind.GroundStaging, boughtAa.Producer);
            Assert.AreEqual(260, PlayerMaterials(em, session));
            Assert.AreEqual(beforeAa.GroundLive + 1, em.GetComponentData<SkirmishCapacityComponent>(session).GroundLive);
            Assert.AreEqual(beforeAa.AirLive, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(
                "Unit_Veh_Missle_Launcher_Air",
                em.GetComponentData<UnitSourcePrefabKey>(FindReservedUnit(em, boughtAa.ReservationId)).Value.ToString());

            Assert.AreEqual(1, SkirmishProductionService.NotifyProducerDestroyed(
                em, session, SkirmishProducerKind.Helipad, 1));
            Assert.AreEqual(SkirmishReservationPhase.Lost, ReservationPhase(em, session, queued.ReservationId));
            Assert.AreEqual(Entity.Null, FindReservedUnit(em, queued.ReservationId));
            Assert.AreEqual(260, PlayerMaterials(em, session));
            Assert.AreEqual(700, PlayerFuel(em, session));
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyReserved);
            Assert.AreEqual(48, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyLive);
            Assert.AreEqual(1, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);

            Assert.IsTrue(SkirmishProductionService.TryQueue(
                em, session, SkirmishRoleIds.TransportHeli, 1, authored.ArmyAir, out SkirmishProductionDecision reservedTransport));
            Assert.AreEqual(240, reservedTransport.MaterialsCost);
            Assert.AreEqual(20, PlayerMaterials(em, session));
            Assert.AreEqual(1, SkirmishProductionService.NotifyProducerDestroyed(
                em, session, SkirmishProducerKind.Helipad, 1));
            Assert.AreEqual(SkirmishReservationPhase.Cancelled, ReservationPhase(em, session, reservedTransport.ReservationId));
            Assert.AreEqual(260, PlayerMaterials(em, session));
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirReserved);
            Assert.AreEqual(48, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyLive);

            em.AddComponentData(grantTransport, new UnitFuelConsumption
            {
                GroundFuelPerCell = 10f,
                AirFuelPerCell = 4f,
                Enabled = 1
            });
            Assert.IsTrue(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.AircraftEfficiency, 1, out SkirmishResearchDecision airResearch));
            Assert.AreEqual(200, airResearch.MaterialsCost);
            Assert.AreEqual(SkirmishProducerKind.Helipad, ResearchProducer(em, session, airResearch.ResearchId));
            Assert.AreEqual(60, PlayerMaterials(em, session));
            Assert.IsTrue(SkirmishResearchService.TryStart(em, session, airResearch.ResearchId, out _));
            Assert.IsTrue(SkirmishResearchService.TryComplete(em, session, airResearch.ResearchId, out SkirmishResearchDecision completed));
            Assert.AreEqual(1, completed.LevelAfter);
            var research = em.GetComponentData<SkirmishResearchStateComponent>(session);
            Assert.AreEqual(1, research.AircraftEfficiency);
            Assert.AreEqual(SkirmishReadinessStage.Established, research.Readiness);
            Assert.AreEqual(0, research.InfantryWeapons);
            Assert.AreEqual(0, research.VehicleProtection);
            var stamped = em.GetComponentData<UnitFuelConsumption>(grantTransport);
            Assert.AreEqual(9f, stamped.GroundFuelPerCell, 0.001f);
            Assert.AreEqual(3.6f, stamped.AirFuelPerCell, 0.001f);
            Assert.IsFalse(SkirmishResearchService.TryQueue(
                em, session, SkirmishResearchKind.AircraftEfficiency, 1, out SkirmishResearchDecision again));
            Assert.AreEqual(SkirmishReasonCode.AlreadyCompleted, again.Reason);
            Assert.AreEqual(60, PlayerMaterials(em, session));
            Assert.AreEqual(700, PlayerFuel(em, session));

            Assert.IsTrue(SkirmishProductionService.TryReleaseDeath(em, session, grantTransport));
            Assert.IsFalse(SkirmishProductionService.TryReleaseDeath(em, session, grantTransport));
            Assert.AreEqual(0, em.GetComponentData<SkirmishCapacityComponent>(session).AirLive);
            Assert.AreEqual(40, em.GetComponentData<SkirmishCapacityComponent>(session).SupplyLive);
            Assert.AreEqual(60, PlayerMaterials(em, session));
            Assert.AreEqual(700, PlayerFuel(em, session));
            Assert.AreEqual(240, em.GetComponentData<SkirmishEconomyStockComponent>(session).Oil);
            Assert.AreEqual(900, em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials);
            Assert.AreEqual(1, em.GetComponentData<SkirmishEnemyCapacityComponent>(session).AirLive);
            Assert.AreEqual(0u, em.GetComponentData<SkirmishAttemptOwnedComponent>(grantAa).ReservationId);
        }

        [Test]
        public void BlockedRegistryDispatchRefundsOnceWithoutPromotingPopulation()
        {
            using var world = new World(nameof(BlockedRegistryDispatchRefundsOnceWithoutPromotingPopulation));
            var em = world.EntityManager;
            CompileAndSpawnS003(em, out Entity session, out var setup);
            SkirmishMaterialsService.Initialize(em, session, setup);
            var authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            string rifleKey = null;
            foreach (var force in setup.Forces)
                if (force.RoleKind == SkirmishRoleKind.Rifle) { rifleKey = force.RuntimePrefabKey; break; }
            Assert.IsNotEmpty(rifleKey);
            Entity prefab = em.CreateEntity(typeof(Prefab), typeof(UnitSourcePrefabKey), typeof(UnitFootprint));
            em.SetComponentData(prefab, new UnitSourcePrefabKey { Value = new FixedString64Bytes(rifleKey) });
            Entity registry = em.CreateEntity();
            em.AddBuffer<UnitPrefabRegistryEntry>(registry).Add(new UnitPrefabRegistryEntry { Prefab = prefab });
            var blocked = new NativeBitArray(64, Allocator.Temp, NativeArrayOptions.ClearMemory);
            var occupied = new NativeBitArray(64, Allocator.Temp, NativeArrayOptions.ClearMemory);
            try
            {
                Entity grid = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
                em.SetComponentData(grid, new GridConfig { Width = 8, Height = 8, CellSize = 1f });
                em.SetComponentData(grid, new DynamicBlockerComponent { GridSize = 64, Blocked = blocked });
                em.SetComponentData(grid, new DynamicOccupancyComponent { GridSize = 64, Occupied = occupied });
                var walkable = em.AddBuffer<GridWalkable>(grid);
                walkable.ResizeUninitialized(64);
                for (int i = 0; i < 64; i++) walkable[i] = new GridWalkable { Value = 0 };
                var before = em.GetComponentData<SkirmishCapacityComponent>(session);
                Assert.IsFalse(SkirmishProductionService.TryProduce(em, session, SkirmishRoleIds.Rifle, 1, authored.ArmyAir, out var decision));
                Assert.AreEqual(SkirmishReasonCode.BlockedSpawn, decision.Reason);
                Assert.AreEqual(450, SkirmishMaterialsService.Read(em, session, 1));
                var after = em.GetComponentData<SkirmishCapacityComponent>(session);
                Assert.AreEqual(before.InfantryLive, after.InfantryLive);
                Assert.AreEqual(0, after.InfantryReserved);
                Assert.IsFalse(SkirmishProductionService.TryRefundFailedDispatch(em, session, decision.ReservationId, out _));
                Assert.AreEqual(450, SkirmishMaterialsService.Read(em, session, 1));
            }
            finally { blocked.Dispose(); occupied.Dispose(); }
        }

        [Test]
        public void QueuedReadinessStartsAndCompletesThroughTickWithoutManualStart()
        {
            using var world = new World(nameof(QueuedReadinessStartsAndCompletesThroughTickWithoutManualStart));
            var em = world.EntityManager;
            CompileAndSpawnS003(em, out Entity session, out _);
            int before = SkirmishMaterialsService.Read(em, session, 1);
            Assert.IsTrue(SkirmishResearchService.TryQueue(em, session, SkirmishResearchKind.Readiness, 1, out var queued));
            Assert.AreEqual(before - 240, SkirmishMaterialsService.Read(em, session, 1));
            Assert.AreEqual(0, SkirmishResearchService.Tick(em, session, 100, true));
            Assert.AreEqual(SkirmishReadinessStage.Field, em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness);
            Assert.AreEqual(0, SkirmishResearchService.Tick(em, session, 44, false));
            Assert.AreEqual(SkirmishReadinessStage.Field, em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness);
            Assert.AreEqual(1, SkirmishResearchService.Tick(em, session, 1, false));
            Assert.AreEqual(SkirmishReadinessStage.Established, em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness);
            Assert.AreEqual(0, SkirmishResearchService.Tick(em, session, 100, false));
            Assert.AreEqual(before - 240, SkirmishMaterialsService.Read(em, session, 1));
        }

        public static void RunPaidCommittedObserverFocusedControls()
        {
            RunPaidObserverSharedLifecycleControls();
            RunPackagedEconomyFixtureBindingControls();
            RunProductionCommittedOriginsFocusedControls();
            RunResearchCommittedLedgerFocusedControls();
            global::BuildingPlacementConstructionTransactionTests.RunConstructionLedgerPlayerControls();
            SkirmishEnemyConstructionAdapterTests.RunConstructionLedgerEnemyControls();
            RunFocusedValidation();
            Debug.Log("[PaidCommittedObserverControls] result=Passed productionDirect=8 research=8 constructionPlayer=8 constructionEnemy=5 legacyEconomy=24 paidResourcesQualified=0 missionReady=0");
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedEconomyTests();
                suite.BlockedRegistryDispatchRefundsOnceWithoutPromotingPopulation();
                suite.S002StartingStocksAndCapsSeedFromMatrix();
                suite.CapacityLedgerReservePromoteReleaseOnce();
                suite.GroundStagingProducesTankThenDeathReleasesOnce();
                suite.CapacityLifecycleReleasesDeadOwnedUnitsOnce();
                suite.InfantrySquadCostsFourMembersAndMaterials();
                suite.MissingMaterialsAndCapsRejectWithoutMutation();
                suite.FuelGateRejectsWithoutStockMutation();
                suite.RecruitmentFuelCostStaysZeroOnS002Overlays();
                suite.PlayerAndEnemyProductionReceiptsNeverShareAnIdOrRefundTheOtherFaction();
                suite.NativePacketDeliveryKeepsOneGroupAndRefundsOnlyUndeliveredMembers();
                suite.NativeProducerLossReleasesOnlyItsOwnQueue();
                suite.NativeDeliveryRejectsStaleAttemptPauseAndTerminalState();
                suite.MidProduceCancelRefundsReservedThenSeventyFivePercent();
                suite.FailedDispatchAndProducerDestructionUseSpecifiedRefunds();
                suite.CategoryResearchFromStagingAndHqAppliesOnce();
                suite.FactionResearchChargesAndUpgradesOnlyItsOwner();
                suite.PaidEnemyReadinessDoesNotUnlockPlayerOrChangePlayerPrice();
                suite.LegacyCheckpointWithoutEnemyResearchUsesCompiledOpeningStage();
                suite.QueuedReadinessStartsAndCompletesThroughTickWithoutManualStart();
                suite.ReplacementHqCanResearchWithoutBecomingVictoryBase();
                suite.S003AirMobileAaGatePadReadinessAndMissingPadRefund();
                suite.S004EstablishedGrantsDoNotDebitAndPadStartsLive();
                suite.S004EstablishedGrantLedgerAirCapAndHelipadResearch();
                Debug.Log("[SkirmishExpandedEconomyTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedEconomyTests] result=Failed\n" + exception);
                throw;
            }
        }

        private static SkirmishScenarioDefinitionConfig RequirePackagedEconomyDefinition(
            string catalogId, SkirmishScenarioDefinitionConfig definition)
        {
            var packaged = Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
            Assert.IsNotNull(packaged, "Actual packaged economy catalog required");
            Assert.AreSame(packaged, SkirmishExpansionCatalogConfig.Load(), "Preserve foreign validation catalog ownership");
            Assert.IsTrue(packaged.TryGetDefinition(catalogId, out var current), "Actual packaged coded definition required");
            Assert.IsNotNull(definition, "Actual packaged economy definition required");
            Assert.AreSame(current, definition, "Actual packaged economy definition required; stale or detached copies refused");
            Assert.IsNotNull(definition.MapLayout, "Existing coded logical layout required");
            Assert.IsTrue(SkirmishLegacyLayoutBaselineContract.TryGetReviewedLayoutFingerprint(catalogId, out string reviewed));
            Assert.AreEqual(reviewed, SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout),
                "Current packaged layout must retain its existing reviewed coded binding");
            return definition;
        }
        private static SkirmishExpansionAuthoredSet PackagedEconomyFixture(string catalogId)
        {
            var catalog = Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
            Assert.IsNotNull(catalog, "Actual packaged economy catalog required");
            Assert.IsTrue(catalog.TryGetDefinition(catalogId, out var definition));
            RequirePackagedEconomyDefinition(catalogId, definition);
            var authored = catalog.ToAuthoredSet();
            Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, catalogId, out var selected));
            Assert.AreSame(definition, selected, "Compile the exact existing packaged definition");
            return authored;
        }
        [Test]
        public void PackagedEconomyFixturesRetainAllThreeExistingReviewedBindings()
        {
            foreach (string id in new[] { "S002", "S003", "S004" })
            {
                var authored = PackagedEconomyFixture(id);
                Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, id, out var definition));
                Assert.AreEqual(id, RequirePackagedEconomyDefinition(id, definition).CatalogId);
            }
        }
        [Test]
        public void PackagedEconomyFixtureRefusesMissingAndStaleDefinitionWithoutMutation()
        {
            var catalog = Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
            Assert.IsNotNull(catalog); Assert.IsTrue(catalog.TryGetDefinition("S002", out var current));
            string before = JsonUtility.ToJson(current);
            var missing = Assert.Throws<AssertionException>(() => RequirePackagedEconomyDefinition("S002", null));
            StringAssert.Contains("Actual packaged economy definition required", missing.Message);
            var stale = UnityEngine.Object.Instantiate(current);
            try
            {
                JsonUtility.FromJsonOverwrite("{\"contentVersion\":" + Math.Max(0, current.ContentVersion - 1) + "}", stale);
                Assert.AreNotEqual(current.ContentVersion, stale.ContentVersion);
                var error = Assert.Throws<AssertionException>(() => RequirePackagedEconomyDefinition("S002", stale));
                StringAssert.Contains("Actual packaged economy definition required", error.Message);
                Assert.AreEqual(before, JsonUtility.ToJson(current), "Do not mutate packaged assets to repair fixture data");
            }
            finally { UnityEngine.Object.DestroyImmediate(stale); }
        }
        public static void RunPackagedEconomyFixtureBindingControls()
        {
            var t = new SkirmishExpandedEconomyTests();
            t.PackagedEconomyFixturesRetainAllThreeExistingReviewedBindings();
            t.PackagedEconomyFixtureRefusesMissingAndStaleDefinitionWithoutMutation();
            Debug.Log("[PackagedEconomyFixtureBindingControls] result=Passed catalogs=3 refusals=2 normalMission=0");
        }

        private static void CompileAndSpawn(
            EntityManager em,
            out Entity session,
            out SkirmishResolvedSetup setup)
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = PackagedEconomyFixture("S002");
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S002",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                104731,
                authored,
                matrix,
                manifest,
                out setup,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(
                em,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                setup);
        }

        private static void CompileAndSpawnS003(
            EntityManager em,
            out Entity session,
            out SkirmishResolvedSetup setup)
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = PackagedEconomyFixture("S003");
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS003.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S003",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                SkirmishS003FirstVisit.SeedA,
                authored,
                matrix,
                manifest,
                out setup,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(
                em,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                setup);
        }

        private static void CompileAndSpawnS004(
            EntityManager em,
            out Entity session,
            out SkirmishResolvedSetup setup)
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = PackagedEconomyFixture("S004");
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS004.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S004",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                SkirmishS004FirstVisit.SeedA,
                authored,
                matrix,
                manifest,
                out setup,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(
                em,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                setup);
        }

        private static int PlayerMaterials(EntityManager em, Entity session) =>
            em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;

        private static int PlayerFuel(EntityManager em, Entity session) =>
            em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel;

        private static SkirmishProducerKind ResearchProducer(EntityManager em, Entity session, uint researchId)
        {
            DynamicBuffer<SkirmishResearchQueueItem> buffer = em.GetBuffer<SkirmishResearchQueueItem>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ResearchId == researchId)
                    return buffer[i].Producer;
            }

            return SkirmishProducerKind.None;
        }

        private static Entity FindStructure(EntityManager em, byte faction, string structureId)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishStructureIdentityComponent), typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId != faction)
                    continue;
                if (em.GetComponentData<SkirmishStructureIdentityComponent>(entities[i]).StructureId.ToString() == structureId)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static Entity FindReservedUnit(EntityManager em, uint reservationId)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).ReservationId == reservationId)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static SkirmishReservationPhase ReservationPhase(
            EntityManager em,
            Entity session,
            uint reservationId)
        {
            DynamicBuffer<SkirmishProductionReservation> buffer = em.GetBuffer<SkirmishProductionReservation>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ReservationId == reservationId)
                    return buffer[i].Phase;
            }

            return SkirmishReservationPhase.None;
        }

        private static SkirmishResearchPhase ResearchPhase(
            EntityManager em,
            Entity session,
            uint researchId)
        {
            DynamicBuffer<SkirmishResearchQueueItem> buffer = em.GetBuffer<SkirmishResearchQueueItem>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ResearchId == researchId)
                    return buffer[i].Phase;
            }

            return SkirmishResearchPhase.None;
        }

        private static Entity FirstRole(EntityManager em, SkirmishRoleKind role, byte faction)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId == faction &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(entities[i]).Role == role)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static Entity FirstDesignated(EntityManager em, SkirmishObjectiveRoleKind role)
        {
            using var query = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entities[i]).Role == role)
                    return entities[i];
            }

            return Entity.Null;
        }
    }
}
