using System;
using System.Collections.Generic;
using System.IO;
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
    public sealed class SkirmishMedalLedgerTests
    {
        private string _saveRoot;

        [SetUp]
        public void SetUp()
        {
            _saveRoot = Path.Combine(Path.GetTempPath(), "SkirmishMedalLedgerTests", Guid.NewGuid().ToString("N"));
            SkirmishMedalProfileCommit.TestSave = null;
            SkirmishMedalProfileCommit.AllowCommit = null;
        }

        [TearDown]
        public void TearDown()
        {
            SkirmishMedalProfileCommit.TestSave = null;
            SkirmishMedalProfileCommit.AllowCommit = null;
            if (!string.IsNullOrEmpty(_saveRoot) && Directory.Exists(_saveRoot))
                Directory.Delete(_saveRoot, true);
        }

        [Test]
        public void VictoryAwardsOneMedal()
        {
            var ledger = new SkirmishMedalLedger(null, Catalog());
            Assert.IsTrue(ledger.TryAward(Victory("S002", SkirmishDifficultyId.Regular, "session-a", 3), out SkirmishMedalAwardResult award));
            Assert.IsTrue(award.IsFirstAward);
            Assert.AreEqual("S002", award.ScenarioId);
            Assert.AreEqual(SkirmishDifficultyId.Regular, award.Difficulty);
            Assert.IsTrue(ledger.Has("S002", SkirmishDifficultyId.Regular));
            Assert.AreEqual(1, ledger.BattlesWon());
            SkirmishMedalDifficultyTotals totals = ledger.TotalsByDifficulty();
            Assert.AreEqual(1, totals.Regular);
            Assert.AreEqual(0, totals.Recruit);
        }

        [Test]
        public void DuplicateSessionAndLaterContentVersionDoNotReplaceTheMedal()
        {
            var ledger = new SkirmishMedalLedger(null, Catalog());
            var earned = new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);
            SkirmishResultReceipt first = Victory("S002", SkirmishDifficultyId.Veteran, "session-a", 3);
            Assert.IsTrue(ledger.TryAward(first, earned, out SkirmishMedalAwardResult firstAward));
            Assert.IsTrue(firstAward.IsFirstAward);
            SkirmishResultReceipt repeat = Victory("S002", SkirmishDifficultyId.Veteran, "session-a", 9);
            Assert.IsTrue(ledger.TryAward(repeat, earned.AddMinutes(5), out SkirmishMedalAwardResult repeatAward));
            Assert.IsFalse(repeatAward.IsFirstAward);
            SkirmishResultReceipt laterSession = Victory("S002", SkirmishDifficultyId.Veteran, "session-b", 11);
            Assert.IsTrue(ledger.TryAward(laterSession, earned.AddHours(1), out SkirmishMedalAwardResult laterAward));
            Assert.IsFalse(laterAward.IsFirstAward);
            SkirmishMedalSaveData[] rows = ledger.ToSaveData();
            Assert.AreEqual(1, rows.Length);
            Assert.AreEqual(3, rows[0].contentVersion);
            Assert.AreEqual("session-a", rows[0].sessionId);
            Assert.AreEqual(1, ledger.BattlesWon());
        }

        [Test]
        public void CustomLegacyDrawDefeatSurrenderAndTechnicalFailureAwardNothing()
        {
            var ledger = new SkirmishMedalLedger(null, Catalog());
            Assert.IsFalse(ledger.TryAward(Victory("S002", SkirmishDifficultyId.Regular, "custom", 1, custom: true), out _));
            Assert.IsFalse(ledger.TryAward(Victory("S002", SkirmishDifficultyId.Regular, "legacy", 1, legacy: true), out _));
            Assert.IsFalse(ledger.TryAward(Outcome("S002", SkirmishOutcomeKind.Draw, SkirmishEndReasonKind.TimeLimit), out _));
            Assert.IsFalse(ledger.TryAward(Outcome("S002", SkirmishOutcomeKind.Defeat, SkirmishEndReasonKind.MainBaseDestroyed), out _));
            Assert.IsFalse(ledger.TryAward(Outcome("S002", SkirmishOutcomeKind.TechnicalFailure, SkirmishEndReasonKind.StartupFailure), out _));
            Assert.IsFalse(ledger.TryAward(Outcome("S002", SkirmishOutcomeKind.Victory, SkirmishEndReasonKind.Surrender), out _));
            Assert.IsFalse(ledger.TryAward(Victory("S006", SkirmishDifficultyId.Regular, "planned", 1), out _));
            Assert.AreEqual(0, ledger.ToSaveData().Length);
        }

        [Test]
        public void CountsPerMapAndObjectiveFollowCatalogIdentity()
        {
            var ledger = new SkirmishMedalLedger(null, Catalog());
            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Recruit, "a", 1), out _));
            Assert.IsTrue(ledger.TryAward(Victory("S001", SkirmishDifficultyId.Commander, "b", 1), out _));
            Assert.IsTrue(ledger.TryAward(Victory("S025", SkirmishDifficultyId.Regular, "c", 1), out _));
            Assert.AreEqual(1, ledger.CountFor("DB"));
            Assert.AreEqual(1, ledger.CountFor("DB", "BA"));
            Assert.AreEqual(0, ledger.CountFor("DB", "FC"));
            Assert.AreEqual(1, ledger.CountFor("CC"));
            Assert.AreEqual(2, ledger.BattlesWon());
            SkirmishMedalDifficultyTotals totals = ledger.TotalsByDifficulty();
            Assert.AreEqual(1, totals.Recruit);
            Assert.AreEqual(1, totals.Regular);
            Assert.AreEqual(1, totals.Commander);
        }

        [Test]
        public void SaveRoundTripPreservesMedals()
        {
            var save = new SaveService(new JsonSaveRepository(_saveRoot));
            PlayerProfileSaveData profile = save.LoadProfile();
            profile.skirmishMedals = new[]
            {
                new SkirmishMedalSaveData
                {
                    catalogId = "S002",
                    difficulty = (byte)SkirmishDifficultyId.Regular,
                    contentVersion = 4,
                    firstEarnedUtc = "2026-10-14T00:00:00.0000000Z",
                    sessionId = "kept"
                }
            };
            save.SaveProfile(profile);
            PlayerProfileSaveData loaded = new SaveService(new JsonSaveRepository(_saveRoot)).LoadProfile();
            Assert.AreEqual(1, loaded.skirmishMedals.Length);
            Assert.AreEqual("S002", loaded.skirmishMedals[0].catalogId);
            Assert.AreEqual((byte)SkirmishDifficultyId.Regular, loaded.skirmishMedals[0].difficulty);
            Assert.AreEqual(4, loaded.skirmishMedals[0].contentVersion);
            Assert.AreEqual("kept", loaded.skirmishMedals[0].sessionId);
            var ledger = new SkirmishMedalLedger(loaded.skirmishMedals, Catalog());
            Assert.IsTrue(ledger.Has("s002", SkirmishDifficultyId.Regular));
        }

        [Test]
        public void MigrationFromPreviousVersionLeavesMedalsEmptyAndIgnoresLastResult()
        {
            SaveDataModel legacy = new SaveDataModel
            {
                saveVersion = 1,
                profile = new PlayerProfileSaveData
                {
                    commanderName = "Old",
                    skirmishMedals = null
                },
                quickGame = new QuickGameSaveData
                {
                    lastResult = new SkirmishResultSaveData
                    {
                        sessionId = "old-win",
                        outcome = "Victory",
                        scenarioIndex = 0
                    }
                }
            };

            SaveDataModel migrated = SaveMigration.Migrate(legacy);
            Assert.AreEqual(SaveMigration.CurrentVersion, migrated.saveVersion);
            Assert.NotNull(migrated.profile.skirmishMedals);
            Assert.AreEqual(0, migrated.profile.skirmishMedals.Length);
            Assert.AreEqual("Victory", migrated.quickGame.lastResult.outcome);
            Assert.AreEqual("Old", migrated.profile.commanderName);

            Directory.CreateDirectory(_saveRoot);
            File.WriteAllText(Path.Combine(_saveRoot, SaveService.ProfileFileName), "{\"commanderName\":\"Legacy\",\"commanderLevel\":2}");
            File.WriteAllText(Path.Combine(_saveRoot, SaveService.QuickGameFileName),
                "{\"lastResult\":{\"sessionId\":\"legacy-win\",\"outcome\":\"Victory\",\"scenarioIndex\":0}}");
            SaveDataModel loaded = new SaveService(new JsonSaveRepository(_saveRoot)).LoadProject();
            Assert.AreEqual(SaveMigration.CurrentVersion, loaded.saveVersion);
            Assert.NotNull(loaded.profile.skirmishMedals);
            Assert.AreEqual(0, loaded.profile.skirmishMedals.Length);
            Assert.AreEqual("Victory", loaded.quickGame.lastResult.outcome);
        }

        [Test]
        public void VictorySettlementPersistsTheFirstMedalAndExposesTheAward()
        {
            var save = new SaveService(new JsonSaveRepository(_saveRoot));
            SkirmishBattleCatalogConfig catalog = ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();
            catalog.Configure(Catalog().ToArray());
            SkirmishMedalProfileCommit.TestSave = save;
            using (SkirmishBattleCatalogConfig.UseEditorValidationCatalog(catalog))
            using (var world = new World(nameof(VictorySettlementPersistsTheFirstMedalAndExposesTheAward)))
            {
                Entity session = CreateSession(world.EntityManager, "session-award", "S002", SkirmishDifficultyId.Regular, 4,
                    SkirmishOutcomeKind.Victory, SkirmishEndReasonKind.MainBaseDestroyed, 0, 0);
                var journal = new SkirmishExpandedResultJournal();
                Assert.IsTrue(SkirmishResultSettlementService.TrySettleSession(
                    world.EntityManager, session, journal, out _, out _, out SkirmishMedalAwardResult award));
                Assert.IsTrue(award.IsFirstAward);
                Assert.AreEqual("S002", award.ScenarioId);
                Assert.AreEqual(SkirmishDifficultyId.Regular, award.Difficulty);
                Assert.IsTrue(SkirmishResultSettlementService.LastMedalAward.IsFirstAward);
                Assert.AreEqual(1, world.EntityManager.GetComponentData<SkirmishResultComponent>(session).SaveAcknowledged);
                Assert.AreEqual(1, save.LoadProfile().skirmishMedals.Length);
                Assert.AreEqual(4, save.LoadProfile().skirmishMedals[0].contentVersion);

                Assert.IsTrue(SkirmishResultSettlementService.TrySettleSession(
                    world.EntityManager, session, journal, out _, out _, out SkirmishMedalAwardResult repeat));
                Assert.IsFalse(repeat.IsFirstAward);
                Assert.AreEqual(1, save.LoadProfile().skirmishMedals.Length);
            }

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void FailedMedalCommitDoesNotAcknowledgeUntilTheRetryLands()
        {
            var save = new SaveService(new JsonSaveRepository(_saveRoot));
            SkirmishBattleCatalogConfig catalog = ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();
            catalog.Configure(Catalog().ToArray());
            SkirmishMedalProfileCommit.TestSave = save;
            int attempts = 0;
            SkirmishMedalProfileCommit.AllowCommit = () => ++attempts > 1;
            using (SkirmishBattleCatalogConfig.UseEditorValidationCatalog(catalog))
            using (var world = new World(nameof(FailedMedalCommitDoesNotAcknowledgeUntilTheRetryLands)))
            {
                Entity session = CreateSession(world.EntityManager, "session-retry", "S002", SkirmishDifficultyId.Recruit, 2,
                    SkirmishOutcomeKind.Victory, SkirmishEndReasonKind.MainBaseDestroyed, 0, 0);
                var journal = new SkirmishExpandedResultJournal();
                Assert.IsFalse(SkirmishResultSettlementService.TrySettleSession(
                    world.EntityManager, session, journal, out _, out _, out _));
                Assert.AreEqual(0, world.EntityManager.GetComponentData<SkirmishResultComponent>(session).SaveAcknowledged);
                Assert.AreEqual(0, save.LoadProfile().skirmishMedals.Length);

                SkirmishMedalProfileCommit.AllowCommit = null;
                Assert.IsTrue(SkirmishResultSettlementService.TrySettleSession(
                    world.EntityManager, session, journal, out _, out _, out SkirmishMedalAwardResult award));
                Assert.IsTrue(award.IsFirstAward);
                Assert.AreEqual(1, world.EntityManager.GetComponentData<SkirmishResultComponent>(session).SaveAcknowledged);
                Assert.AreEqual(1, save.LoadProfile().skirmishMedals.Length);
                Assert.AreEqual("session-retry", save.LoadProfile().skirmishMedals[0].sessionId);
            }

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        private static Entity CreateSession(
            EntityManager em,
            string sessionId,
            string catalogId,
            SkirmishDifficultyId difficulty,
            int contentVersion,
            SkirmishOutcomeKind outcome,
            SkirmishEndReasonKind reason,
            byte custom,
            byte legacy)
        {
            Entity session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResultComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent
            {
                SessionId = new FixedString64Bytes(sessionId),
                CatalogId = new FixedString64Bytes(catalogId),
                DefinitionId = new FixedString64Bytes("skirmish." + catalogId.ToLowerInvariant()),
                ContentVersion = contentVersion,
                DifficultyId = difficulty,
                SizeId = SkirmishSizeId.Standard,
                IsCustom = custom,
                IsLegacy = legacy
            });
            em.SetComponentData(session, new SkirmishResultComponent
            {
                Outcome = outcome,
                Reason = reason,
                Frozen = 1
            });
            return session;
        }

        private static List<SkirmishBattleCatalogEntry> Catalog() => new()
        {
            Entry("S001", "DB", "BA", "G", "F", true, 0),
            Entry("S002", "DB", "BA", "G", "E", true, 2),
            Entry("S006", "DB", "BA", "C", "E", false, -1),
            Entry("S025", "CC", "BA", "G", "F", true, 1)
        };

        private static SkirmishBattleCatalogEntry Entry(
            string id, string map, string objective, string army, string start, bool playable, int index) =>
            new()
            {
                ScenarioId = id,
                MapId = map,
                ObjectiveId = objective,
                ArmyProfile = army,
                StartProfile = start,
                TitleEnglish = id,
                Status = playable ? SkirmishBattleCatalogStatus.Playable : SkirmishBattleCatalogStatus.Planned,
                PlayableScenarioIndex = playable ? index : -1
            };

        private static SkirmishResultReceipt Victory(
            string catalogId, SkirmishDifficultyId difficulty, string sessionId, int contentVersion,
            bool custom = false, bool legacy = false) =>
            Outcome(catalogId, SkirmishOutcomeKind.Victory, SkirmishEndReasonKind.MainBaseDestroyed, difficulty, sessionId, contentVersion, custom, legacy);

        private static SkirmishResultReceipt Outcome(
            string catalogId,
            SkirmishOutcomeKind outcome,
            SkirmishEndReasonKind reason,
            SkirmishDifficultyId difficulty = SkirmishDifficultyId.Regular,
            string sessionId = "session",
            int contentVersion = 1,
            bool custom = false,
            bool legacy = false) =>
            new()
            {
                CatalogId = catalogId,
                SessionId = sessionId,
                DefinitionId = "skirmish." + catalogId.ToLowerInvariant(),
                DifficultyId = difficulty,
                SizeId = SkirmishSizeId.Standard,
                Outcome = outcome,
                Reason = reason,
                ContentVersion = contentVersion,
                IsCustom = custom ? (byte)1 : (byte)0,
                IsLegacy = legacy ? (byte)1 : (byte)0
            };
    }
}
