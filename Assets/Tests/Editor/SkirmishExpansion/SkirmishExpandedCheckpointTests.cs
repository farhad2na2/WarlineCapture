using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishExpandedCheckpointTests
    {
        [Test]
        public void CodecRoundTripsBaseAssaultSessionFacts()
        {
            using var world = new World(nameof(CodecRoundTripsBaseAssaultSessionFacts));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            MutatePauseAndSpend(em, session);
            Assert.IsTrue(SkirmishCheckpointService.TryCapture(em, session, out SkirmishCheckpointDocument document));
            Assert.AreEqual("S002", document.Header.CatalogId);
            Assert.AreEqual(1, document.Payload.Paused);
            Assert.AreEqual(780, document.Payload.PlayerMaterials);
            string json = SkirmishCheckpointCodec.Encode(document);
            Assert.IsFalse(string.IsNullOrEmpty(document.Header.Checksum));
            Assert.IsTrue(SkirmishCheckpointCodec.TryDecode(json, out SkirmishCheckpointDocument decoded, out _));
            Assert.AreEqual(document.Header.SetupHash, decoded.Header.SetupHash);
            Assert.AreEqual(document.Payload.PlayerMaterials, decoded.Payload.PlayerMaterials);
            Assert.AreEqual(document.Payload.Paused, decoded.Payload.Paused);
            Assert.AreEqual(document.Payload.Actors.Length, decoded.Payload.Actors.Length);
            string tampered = json.Replace("\"PlayerMaterials\":780", "\"PlayerMaterials\":0");
            Assert.IsFalse(SkirmishCheckpointCodec.TryDecode(tampered, out _, out SkirmishReasonCode reason));
            Assert.AreEqual(SkirmishReasonCode.IncompatibleSave, reason);
        }

        [Test]
        public void AtomicWriteAndFreshRestoreConservesPauseAndStocks()
        {
            using var source = new World(nameof(AtomicWriteAndFreshRestoreConservesPauseAndStocks) + "-src");
            EntityManager sourceEm = source.EntityManager;
            CompileAndSpawn(sourceEm, out Entity sourceSession, out SkirmishResolvedSetup setup);
            MutatePauseAndSpend(sourceEm, sourceSession);
            Assert.IsTrue(SkirmishCheckpointService.TryCapture(sourceEm, sourceSession, out SkirmishCheckpointDocument document));
            string path = Path.Combine(Path.GetTempPath(), "skirmish-s002-checkpoint-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                Assert.IsTrue(SkirmishCheckpointCodec.TryWriteAtomic(path, document, out _));
                Assert.IsTrue(File.Exists(path));
                Assert.IsTrue(SkirmishCheckpointCodec.TryRead(path, out SkirmishCheckpointDocument loaded, out _));

                using var restored = new World(nameof(AtomicWriteAndFreshRestoreConservesPauseAndStocks) + "-dst");
                LoadAuthored(out SkirmishExpansionAuthoredSet authored, out List<SkirmishSetupMatrixRow> matrix,
                    out SkirmishContentManifest manifest);
                Assert.IsTrue(SkirmishCheckpointCompositionSystemHelper.TryRestoreFreshSession(
                    restored.EntityManager,
                    loaded,
                    authored,
                    matrix,
                    manifest,
                    out Entity session,
                    out SkirmishReasonCode reason), reason.ToString());
                var clock = restored.EntityManager.GetComponentData<SkirmishObjectiveClockComponent>(session);
                Assert.AreEqual(1, clock.Paused);
                Assert.AreEqual(document.Payload.ElapsedSeconds, clock.ElapsedSeconds);
                var stock = restored.EntityManager.GetComponentData<SkirmishEconomyStockComponent>(session);
                Assert.AreEqual(780, stock.Materials);
                Assert.AreEqual(setup.SetupHash,
                    restored.EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(session).SetupHash);
                Assert.AreEqual(document.Header.SessionId,
                    restored.EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString());
                Assert.AreEqual(SkirmishCheckpointStatus.Restored,
                    restored.EntityManager.GetComponentData<SkirmishCheckpointRequestComponent>(session).Status);
                Assert.AreEqual(document.Payload.PlayerDesignatedAlive,
                    restored.EntityManager.GetComponentData<SkirmishBaseAssaultFactComponent>(session).PlayerDesignatedAlive);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
                if (File.Exists(path + ".bak"))
                    File.Delete(path + ".bak");
            }
        }

        [Test]
        public void IncompatibleContentVersionIsRejected()
        {
            using var world = new World(nameof(IncompatibleContentVersionIsRejected));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out SkirmishResolvedSetup setup);
            Assert.IsTrue(SkirmishCheckpointService.TryCapture(em, session, out SkirmishCheckpointDocument document));
            document.Header.ContentVersion = setup.ContentVersion + 9;
            document.Payload.ContentVersion = document.Header.ContentVersion;
            Assert.IsFalse(SkirmishCheckpointCodec.IsCompatible(document.Header, setup));
            Assert.IsFalse(SkirmishCheckpointService.TryApply(em, session, document, out SkirmishReasonCode reason));
            Assert.AreEqual(SkirmishReasonCode.IncompatibleSave, reason);
        }

        [Test]
        public void ResultSettlesOnceAndReplayOpensFreshSession()
        {
            using var world = new World(nameof(ResultSettlesOnceAndReplayOpensFreshSession));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out SkirmishResolvedSetup setup);
            var result = new SkirmishResultComponent
            {
                Outcome = SkirmishOutcomeKind.Victory,
                Reason = SkirmishEndReasonKind.MainBaseDestroyed,
                SetupHash = setup.SetupHash,
                SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                Frozen = 1
            };
            em.AddComponentData(session, result);
            var journal = new SkirmishExpandedResultJournal();
            Assert.IsTrue(SkirmishResultSettlementService.TrySettleSession(
                em, session, journal, out SkirmishResultReceipt first, out _));
            Assert.AreEqual(1, first.Settled);
            Assert.AreEqual(1, em.GetComponentData<SkirmishResultComponent>(session).SaveAcknowledged);
            Assert.IsTrue(SkirmishResultSettlementService.TrySettleSession(
                em, session, journal, out SkirmishResultReceipt again, out _));
            Assert.AreEqual(first.SessionId, again.SessionId);

            var conflict = new SkirmishResultReceipt
            {
                SessionId = first.SessionId,
                CatalogId = first.CatalogId,
                DefinitionId = first.DefinitionId,
                ContentVersion = first.ContentVersion,
                DifficultyId = first.DifficultyId,
                SizeId = first.SizeId,
                Outcome = SkirmishOutcomeKind.Defeat,
                Reason = SkirmishEndReasonKind.Surrender
            };
            Assert.IsFalse(journal.TrySettle(conflict, out _, out SkirmishReasonCode duplicate));
            Assert.AreEqual(SkirmishReasonCode.DuplicateId, duplicate);

            var custom = new SkirmishResultReceipt
            {
                SessionId = "custom-1",
                DefinitionId = first.DefinitionId,
                ContentVersion = first.ContentVersion,
                DifficultyId = first.DifficultyId,
                SizeId = first.SizeId,
                Outcome = SkirmishOutcomeKind.Victory,
                IsCustom = 1
            };
            Assert.IsFalse(journal.TrySettle(custom, out _, out SkirmishReasonCode blocked));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedCapability, blocked);

            LoadAuthored(out SkirmishExpansionAuthoredSet authored, out List<SkirmishSetupMatrixRow> matrix,
                out SkirmishContentManifest manifest);
            using var replayWorld = new World(nameof(ResultSettlesOnceAndReplayOpensFreshSession) + "-replay");
            var replay = new SkirmishReplayRequest
            {
                SourceSessionId = first.SessionId,
                CatalogId = "S002",
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                Seed = 104731,
                Mode = SkirmishReplayMode.FreshStart
            };
            Assert.IsTrue(SkirmishCheckpointCompositionSystemHelper.TryQueueFreshReplay(
                replayWorld.EntityManager,
                replay,
                authored,
                matrix,
                manifest,
                out SkirmishLaunchPayload payload,
                out SkirmishResolvedSetup replaySetup,
                out _));
            Assert.AreNotEqual(first.SessionId, payload.SessionId);
            Assert.AreEqual(setup.SetupHash, replaySetup.SetupHash);
            Assert.AreEqual(104731, payload.Seed);
            Assert.IsFalse(payload.IsLegacy);
            Entity replaySession = replayWorld.EntityManager
                .CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                replayWorld.EntityManager, replaySession, replaySetup, out _, out _));
            Assert.AreEqual(900,
                replayWorld.EntityManager.GetComponentData<SkirmishEconomyStockComponent>(replaySession).Materials);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedCheckpointTests();
                suite.CodecRoundTripsBaseAssaultSessionFacts();
                suite.AtomicWriteAndFreshRestoreConservesPauseAndStocks();
                suite.IncompatibleContentVersionIsRejected();
                suite.ResultSettlesOnceAndReplayOpensFreshSession();
                Debug.Log("[SkirmishExpandedCheckpointTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedCheckpointTests] result=Failed\n" + exception);
                throw;
            }
        }

        private static void MutatePauseAndSpend(EntityManager em, Entity session)
        {
            var clock = em.HasComponent<SkirmishObjectiveClockComponent>(session)
                ? em.GetComponentData<SkirmishObjectiveClockComponent>(session)
                : new SkirmishObjectiveClockComponent { DeadlineSeconds = 1080, Playing = 1 };
            clock.ElapsedSeconds = 42f;
            clock.Paused = 1;
            clock.Playing = 1;
            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
                em.SetComponentData(session, clock);
            else
                em.AddComponentData(session, clock);

            var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
            stock.Materials = 780;
            em.SetComponentData(session, stock);
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.TickClock = 17;
            em.SetComponentData(session, state);
        }

        private static void CompileAndSpawn(EntityManager em, out Entity session, out SkirmishResolvedSetup setup)
        {
            LoadAuthored(out SkirmishExpansionAuthoredSet authored, out List<SkirmishSetupMatrixRow> matrix,
                out SkirmishContentManifest manifest);
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
                em, session, setup, out SkirmishReasonCode reason, out _), reason.ToString());
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(
                em,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                setup);
            if (!em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                em.AddComponentData(session, new SkirmishObjectiveClockComponent
                {
                    DeadlineSeconds = setup.DeadlineSeconds,
                    Playing = 1
                });
            }
        }

        private static void LoadAuthored(
            out SkirmishExpansionAuthoredSet authored,
            out List<SkirmishSetupMatrixRow> matrix,
            out SkirmishContentManifest manifest)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out matrix, out string error), error);
            authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
        }
    }
}
