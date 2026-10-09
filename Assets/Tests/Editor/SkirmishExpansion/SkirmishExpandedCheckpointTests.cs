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
            using var returns = em.CreateEntityQuery(typeof(SkirmishReturnRequest));
            Assert.AreEqual(1, returns.CalculateEntityCount());
            var pending = returns.GetSingletonEntity();
            var launch = em.GetComponentObject<SkirmishReplayLaunchRecord>(pending);
            Assert.AreNotEqual(sourceId, launch.Payload.SessionId);
            Assert.AreSame(setup, launch.Setup);
            Assert.AreEqual(setup.Seed, launch.Payload.Seed);
            Assert.AreEqual(setup.SizeId, launch.Payload.SizeId);
            Assert.AreEqual(setup.DifficultyId, launch.Payload.DifficultyId);
            Assert.AreEqual(sourceId, em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString(),
                "The old attempt must remain identifiable until match teardown completes.");
            Assert.AreEqual(0, em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).PlayRequested);
            Assert.IsTrue(em.HasComponent<SkirmishSharedSupplyInitialized>(session));
            Assert.IsTrue(em.HasComponent<SkirmishSharedBuildingsReady>(session));
            Assert.AreEqual(780, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials);
            Assert.IsFalse(SkirmishCheckpointCompositionSystemHelper.TryReplayLive(em, session, out _),
                "A native supply session must never take the ledger-only replay shortcut.");
            Assert.IsTrue(SkirmishCheckpointCompositionSystemHelper.TryConsumeExpandedReplay(
                em, session, SkirmishAction.Restart));
            Assert.AreEqual(1, returns.CalculateEntityCount(), "Repeated requests must not queue duplicate attempts.");
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

        [Test]
        public void FreshReplayResetsPersistentFactionBankAndEventHistory()
        {
            using var world = new World(nameof(FreshReplayResetsPersistentFactionBankAndEventHistory));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out var setup);
            var bank = em.CreateEntity(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent),
                typeof(MaterialFabricationEconomyEventQueueComponent));
            em.SetComponentData(bank, new FactionEconomy { FactionId = 1, Money = 1234 });
            em.SetComponentData(bank, new FactionTacticalMaterialsComponent
                { FactionId = 1, Current = 17, Capacity = 9999, LifetimeSpent = 883, LifetimeFabricated = 20, Version = 90 });
            em.SetComponentData(bank, new MaterialFabricationEconomyEventQueueComponent { LastEventId = 87, Version = 5 });
            em.AddBuffer<MaterialFabricationEconomyEventElement>(bank).Add(new MaterialFabricationEconomyEventElement
                { EventId = 87, RuntimeBuildingId = 3, MaterialsProduced = 20 });
            SkirmishMaterialsService.Initialize(em, session, setup);
            Assert.AreEqual(bank, SkirmishMaterialsService.FindEconomy(em, 1));
            var fresh = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            Assert.AreEqual(setup.MaterialsEach, fresh.Current);
            Assert.AreEqual(setup.MaterialsCapacityEach, fresh.Capacity);
            Assert.AreEqual(0, fresh.LifetimeSpent);
            Assert.AreEqual(0, fresh.LifetimeFabricated);
            Assert.AreEqual(1, fresh.Version);
            Assert.AreEqual(0, em.GetComponentData<FactionEconomy>(bank).Money);
            Assert.AreEqual(0, em.GetComponentData<MaterialFabricationEconomyEventQueueComponent>(bank).LastEventId);
            Assert.AreEqual(0, em.GetBuffer<MaterialFabricationEconomyEventElement>(bank).Length);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedCheckpointTests();
                suite.FreshReplayResetsPersistentFactionBankAndEventHistory();
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
                suite.FreshReplayPreservesEachRequestedMissionAndRejectsUnknownCatalog();
                suite.IncompatibleFreshRestoreDoesNotCreateSessionOrTouchExistingAttempt();
                Debug.Log("[SkirmishExpandedCheckpointTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedCheckpointTests] result=Failed\n" + exception);
                throw;
            }
        }

        [Test]
        public void FreshReplayPreservesEachRequestedMissionAndRejectsUnknownCatalog()
        {
            LoadAuthored(out var authored, out var matrix, out _);
            foreach (string catalogId in new[] { "S002", "S003", "S004", "S005" })
            {
                using var world = new World("replay-" + catalogId);
                Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, catalogId, out var definition));
                var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
                var request = new SkirmishReplayRequest
                {
                    SourceSessionId = "previous-" + catalogId,
                    CatalogId = catalogId,
                    DifficultyId = SkirmishDifficultyId.Regular,
                    SizeId = SkirmishSizeId.Standard,
                    Seed = 104734,
                    Mode = SkirmishReplayMode.FreshStart
                };
                Assert.IsTrue(SkirmishCheckpointCompositionSystemHelper.TryQueueFreshReplay(
                    world.EntityManager, request, authored, matrix, manifest, out var payload, out var setup, out var reason),
                    catalogId + ": " + reason);
                Assert.AreEqual(catalogId, payload.CatalogId);
                Assert.AreEqual(definition.DefinitionId, setup.DefinitionId);
                Assert.AreNotEqual(request.SourceSessionId, payload.SessionId);
            }
            using var invalidWorld = new World("replay-unknown");
            Assert.IsFalse(SkirmishCheckpointCompositionSystemHelper.TryQueueFreshReplay(
                invalidWorld.EntityManager,
                new SkirmishReplayRequest { CatalogId = "S999", Mode = SkirmishReplayMode.FreshStart },
                authored, matrix, new SkirmishContentManifest(), out _, out _, out _));
            using var sessions = invalidWorld.EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Assert.AreEqual(0, sessions.CalculateEntityCount());
        }

        [Test]
        public void IncompatibleFreshRestoreDoesNotCreateSessionOrTouchExistingAttempt()
        {
            using var source = new World("restore-preflight-source");
            CompileAndSpawn(source.EntityManager, out var sourceSession, out _);
            Assert.IsTrue(SkirmishCheckpointService.TryCapture(source.EntityManager, sourceSession, out var saved));
            LoadAuthored(out var authored, out var matrix, out var manifest);
            using var destination = new World("restore-preflight-destination");
            int before = destination.EntityManager.UniversalQuery.CalculateEntityCount();
            saved.Header.ContentVersion++;
            saved.Payload.ContentVersion++;
            Assert.IsFalse(SkirmishCheckpointCompositionSystemHelper.TryRestoreFreshSession(
                destination.EntityManager, saved, authored, matrix, manifest, out var session, out _));
            Assert.AreEqual(Entity.Null, session);
            Assert.AreEqual(before, destination.EntityManager.UniversalQuery.CalculateEntityCount());
            saved.Header.ContentVersion--;
            saved.Payload.ContentVersion--;
            var stock = source.EntityManager.GetComponentData<SkirmishEconomyStockComponent>(sourceSession);
            Assert.IsFalse(SkirmishCheckpointCompositionSystemHelper.TryRestoreFreshSession(
                source.EntityManager, saved, authored, matrix, manifest, out _, out _));
            Assert.AreEqual(stock.Materials, source.EntityManager.GetComponentData<SkirmishEconomyStockComponent>(sourceSession).Materials);
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
