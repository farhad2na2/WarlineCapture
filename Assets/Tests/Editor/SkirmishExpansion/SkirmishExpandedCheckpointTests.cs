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
        public void IncompleteNativeCheckpointIsRejectedBeforeAnyMutation()
        {
            using var world = new World(nameof(IncompleteNativeCheckpointIsRejectedBeforeAnyMutation));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            Assert.IsTrue(SkirmishCheckpointService.TryCapture(em, session, out var fixture));
            var actor = em.CreateEntity(typeof(SkirmishSharedActorTag), typeof(SkirmishAttemptOwnedComponent));
            em.SetComponentData(actor, new SkirmishAttemptOwnedComponent
            { SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId });
            fixture.Payload.PlayerMaterials = 9999;
            int before = SkirmishMaterialsService.Read(em, session, 1);
            Assert.IsFalse(SkirmishCheckpointService.TryCapture(em, session, out _));
            Assert.IsFalse(SkirmishCheckpointService.TryApply(em, session, fixture, out var reason));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedCapability, reason);
            Assert.AreEqual(before, SkirmishMaterialsService.Read(em, session, 1));
            Assert.IsTrue(em.Exists(actor));
        }

        [Test]
        public void FirstCheckpointSystemRequestAttachesItsDocumentWithoutStructuralIterationFailure()
        {
            using var world = new World(nameof(FirstCheckpointSystemRequestAttachesItsDocumentWithoutStructuralIterationFailure));
            var em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            if (!em.HasComponent<SkirmishCheckpointRequestComponent>(session))
                em.AddComponent<SkirmishCheckpointRequestComponent>(session);
            em.SetComponentData(session, new SkirmishCheckpointRequestComponent { Status = SkirmishCheckpointStatus.Requested });
            Assert.IsFalse(em.HasComponent<SkirmishCheckpointDocumentRecord>(session));
            world.GetOrCreateSystem<SkirmishCheckpointSystem>().Update(world.Unmanaged);
            Assert.AreEqual(SkirmishCheckpointStatus.Written, em.GetComponentData<SkirmishCheckpointRequestComponent>(session).Status);
            Assert.IsNotNull(em.GetComponentObject<SkirmishCheckpointDocumentRecord>(session).Document);
        }

        [Test]
        public void SameSeedFreshAttemptsSettleDifferentOutcomesIndependently()
        {
            using var first = new World("same-seed-first");
            using var second = new World("same-seed-second");
            CompileAndSpawn(first.EntityManager, out var firstSession, out var firstSetup);
            CompileAndSpawn(second.EntityManager, out var secondSession, out var secondSetup);
            var firstState = first.EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(firstSession);
            var secondState = second.EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(secondSession);
            Assert.AreEqual(firstSetup.SetupHash, secondSetup.SetupHash);
            Assert.AreEqual(firstSetup.Seed, secondSetup.Seed);
            Assert.AreNotEqual(firstState.SessionId, secondState.SessionId);
            var journal = new SkirmishExpandedResultJournal();
            first.EntityManager.AddComponentData(firstSession, new SkirmishResultComponent
            { Outcome = SkirmishOutcomeKind.Victory, Reason = SkirmishEndReasonKind.MainBaseDestroyed, Frozen = 1 });
            second.EntityManager.AddComponentData(secondSession, new SkirmishResultComponent
            { Outcome = SkirmishOutcomeKind.Defeat, Reason = SkirmishEndReasonKind.MainBaseDestroyed, Frozen = 1 });
            Assert.IsTrue(SkirmishResultSettlementService.TrySettleSession(first.EntityManager, firstSession, journal, out _, out _));
            Assert.IsTrue(SkirmishResultSettlementService.TrySettleSession(second.EntityManager, secondSession, journal, out _, out _));
        }

        [Test]
        public void PhysicalSupplyCheckpointPreservesFractionsReservationsAndRejectsMissingStoresAtomically()
        {
            using var world = new World(nameof(PhysicalSupplyCheckpointPreservesFractionsReservationsAndRejectsMissingStoresAtomically));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            em.AddComponent<SkirmishSharedSupplyInitialized>(session);
            Entity Store(byte faction)
            {
                var entity = em.CreateEntity(typeof(BuildingResourceStorageComponent), typeof(RuntimeBuildingCombatInfo), typeof(UnitSourcePrefabKey));
                em.SetComponentData(entity, new RuntimeBuildingCombatInfo
                { OwnerFactionId = faction, RuntimeBuildingId = faction, OriginCell = new Unity.Mathematics.int2(faction * 10, 20) });
                em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new Unity.Collections.FixedString64Bytes("Building_Fuel_Bladder") });
                em.SetComponentData(entity, new BuildingResourceStorageComponent
                {
                    OwnerFactionId = faction, OilStorageCapacity = 500, FuelStorageCapacity = 500,
                    StoredOilBarrels = 120.25f, StoredFuelBarrels = 350.75f,
                    ReservedOilInboundBarrels = 3.25f, ReservedOilOutboundBarrels = 2.5f,
                    ReservedFuelInboundBarrels = 5.5f, ReservedFuelOutboundBarrels = 20f,
                    CivilianFuelReserveBarrels = 10f
                });
                return entity;
            }
            var playerStore = Store(1);
            var enemyStore = Store(2);
            Assert.IsTrue(SkirmishCheckpointService.TryCapture(em, session, out var captured));
            Assert.AreEqual(120, captured.Payload.PlayerOil);
            Assert.AreEqual(320, captured.Payload.PlayerFuel);
            Assert.AreEqual(2, captured.Payload.SupplyStores.Length);
            Assert.IsTrue(SkirmishCheckpointCodec.TryDecode(SkirmishCheckpointCodec.Encode(captured), out var restored, out _));
            var consumed = em.GetComponentData<BuildingResourceStorageComponent>(playerStore);
            consumed.StoredFuelBarrels = 100.5f;
            em.SetComponentData(playerStore, consumed);
            Assert.IsTrue(SkirmishCheckpointService.TryApply(em, session, restored, out _));
            Assert.IsTrue(SkirmishCheckpointSupplyService.Conserved(em, restored.Payload.SupplyStores));
            Assert.AreEqual(350.75f, em.GetComponentData<BuildingResourceStorageComponent>(playerStore).StoredFuelBarrels);
            Assert.AreEqual(5.5f, em.GetComponentData<BuildingResourceStorageComponent>(playerStore).ReservedFuelInboundBarrels);

            int before = SkirmishMaterialsService.Read(em, session, 1);
            restored.Payload.PlayerMaterials = 0;
            restored.Payload.SupplyStores[1].OriginX += 1;
            Assert.IsFalse(SkirmishCheckpointService.TryApply(em, session, restored, out var reason));
            Assert.AreEqual(SkirmishReasonCode.IncompatibleSave, reason);
            Assert.AreEqual(before, SkirmishMaterialsService.Read(em, session, 1));
            Assert.AreEqual(350.75f, em.GetComponentData<BuildingResourceStorageComponent>(enemyStore).StoredFuelBarrels);
        }

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

        [Test]
        public void LivePauseSettlesOnceAndReplayReseedsFreshSession()
        {
            using var world = new World(nameof(LivePauseSettlesOnceAndReplayReseedsFreshSession));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out SkirmishResolvedSetup setup);
            var playing = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            playing.Phase = SkirmishSessionPhase.Playing;
            em.SetComponentData(session, playing);
            var spent = em.GetComponentData<SkirmishEconomyStockComponent>(session);
            spent.Materials = 780;
            em.SetComponentData(session, spent);

            Assert.IsTrue(SkirmishExpandedSessionControlService.TryPause(
                em, session, out SkirmishCheckpointDocument paused));
            Assert.AreEqual(1, em.GetComponentData<SkirmishObjectiveClockComponent>(session).Paused);
            Assert.AreEqual(1, paused.Payload.Paused);
            Assert.AreEqual(780, paused.Payload.PlayerMaterials);
            Assert.IsTrue(SkirmishExpandedSessionControlService.TryResume(em, session));
            Assert.AreEqual(0, em.GetComponentData<SkirmishObjectiveClockComponent>(session).Paused);

            em.AddComponentData(session, new SkirmishResultComponent
            {
                Outcome = SkirmishOutcomeKind.Victory,
                Reason = SkirmishEndReasonKind.MainBaseDestroyed,
                SetupHash = setup.SetupHash,
                SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                Frozen = 1
            });
            // Keep this test's receipt isolated from other tests using the same
            // authored seed/session identity and the process-wide journal.
            var journal = new SkirmishExpandedResultJournal();
            Assert.IsTrue(SkirmishExpandedSessionControlService.TrySettle(em, session, journal));
            Assert.AreEqual(1, em.GetComponentData<SkirmishResultComponent>(session).SaveAcknowledged);
            Assert.IsTrue(SkirmishExpandedSessionControlService.TrySettle(em, session, journal));

            em.AddComponent<SkirmishSharedMaterialsInitialized>(session);
            em.AddComponent<SkirmishSharedSupplyInitialized>(session);
            em.AddComponent<SkirmishSharedBuildingsReady>(session);
            em.AddBuffer<SkirmishStartingBuildingRequest>(session);
            string sourceId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString();
            var gameplay = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
            em.SetComponentData(gameplay, new RuntimeGameplayStateComponent { SimulationActive = 0, PlayRequested = 0 });
            em.GetBuffer<SkirmishTrackedUnit>(session).Add(new SkirmishTrackedUnit { Entity = Entity.Null, FactionId = 1 });
            Assert.IsTrue(SkirmishCheckpointCompositionSystemHelper.TryConsumeExpandedReplay(
                em, session, SkirmishAction.Replay));
            Assert.AreEqual(0, em.CreateEntityQuery(typeof(SkirmishReturnRequest)).CalculateEntityCount());
            Entity replayed = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(em.HasComponent<SkirmishExpandedReplayStartPending>(replayed));
            Assert.AreEqual(1, em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).PlayRequested);
            Assert.AreEqual(0, em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive,
                "Replay must finish spawning before simulation resumes.");
            Assert.AreEqual(0, em.GetBuffer<SkirmishTrackedUnit>(replayed).Length,
                "Previous-attempt teardown must not count as new-attempt losses.");
            Assert.IsFalse(em.HasComponent<SkirmishSharedMaterialsInitialized>(replayed));
            Assert.IsFalse(em.HasComponent<SkirmishSharedSupplyInitialized>(replayed));
            Assert.IsFalse(em.HasComponent<SkirmishSharedBuildingsReady>(replayed));
            Assert.IsFalse(em.HasBuffer<SkirmishStartingBuildingRequest>(replayed));
            string replayId = em.GetComponentData<SkirmishExpandedSessionComponent>(replayed).SessionId.ToString();
            Assert.AreNotEqual(sourceId, replayId);
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, replayed, setup, out _, out _));
            Assert.AreEqual(900, em.GetComponentData<SkirmishEconomyStockComponent>(replayed).Materials);
            Assert.IsFalse(em.HasComponent<SkirmishResultComponent>(replayed));
        }

        [Test]
        public void CustomAndLegacySessionsAreNotExpandedCompletion()
        {
            using var world = new World(nameof(CustomAndLegacySessionsAreNotExpandedCompletion));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out SkirmishResolvedSetup setup);
            var custom = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            custom.IsCustom = 1;
            em.SetComponentData(session, custom);
            em.AddComponentData(session, new SkirmishResultComponent
            {
                Outcome = SkirmishOutcomeKind.Victory,
                Reason = SkirmishEndReasonKind.MainBaseDestroyed,
                SetupHash = setup.SetupHash,
                SessionId = custom.SessionId,
                Frozen = 1
            });
            var journal = new SkirmishExpandedResultJournal();
            Assert.IsFalse(SkirmishResultSettlementService.TrySettleSession(
                em, session, journal, out _, out SkirmishReasonCode customBlocked));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedCapability, customBlocked);
            Assert.AreEqual(0, em.GetComponentData<SkirmishResultComponent>(session).SaveAcknowledged);

            custom.IsCustom = 0;
            custom.IsLegacy = 1;
            em.SetComponentData(session, custom);
            Assert.IsFalse(SkirmishExpandedSessionControlService.IsExpanded(em, session));
            Assert.IsFalse(SkirmishExpandedSessionControlService.TryPause(em, session, out _));
            Assert.IsFalse(SkirmishCheckpointCompositionSystemHelper.TryConsumeExpandedReplay(
                em, session, SkirmishAction.Replay));
            Assert.IsFalse(SkirmishResultSettlementService.TrySettleSession(
                em, session, journal, out _, out SkirmishReasonCode legacyBlocked));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedCapability, legacyBlocked);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedCheckpointTests();
                suite.IncompleteNativeCheckpointIsRejectedBeforeAnyMutation();
                suite.FirstCheckpointSystemRequestAttachesItsDocumentWithoutStructuralIterationFailure();
                suite.SameSeedFreshAttemptsSettleDifferentOutcomesIndependently();
                suite.PhysicalSupplyCheckpointPreservesFractionsReservationsAndRejectsMissingStoresAtomically();
                suite.CodecRoundTripsBaseAssaultSessionFacts();
                suite.AtomicWriteAndFreshRestoreConservesPauseAndStocks();
                suite.IncompatibleContentVersionIsRejected();
                suite.ResultSettlesOnceAndReplayOpensFreshSession();
                suite.LivePauseSettlesOnceAndReplayReseedsFreshSession();
                suite.CustomAndLegacySessionsAreNotExpandedCompletion();
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
