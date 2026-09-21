using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishExpandedCatalogTests
    {
        [Test]
        public void RegularStandardResolvesS002BriefingAndHudKeys()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishExpandedCopyProjection.TryResolveLibraryBriefing(
                authored.DefinitionS002,
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                GameLocalization.EnglishLocaleCode,
                out SkirmishLibraryBriefing en));
            Assert.AreEqual("skirmish.s002.title", en.TitleKey);
            Assert.AreEqual("skirmish.s002.brief", en.BriefKey);
            Assert.AreEqual("Desert Base · Base Assault · Established", en.Title);
            Assert.IsFalse(en.Playable);

            Assert.IsTrue(SkirmishExpandedCopyProjection.TryResolveS002LibraryBriefing(
                GameLocalization.PersianLocaleCode,
                out SkirmishLibraryBriefing fa));
            Assert.AreEqual("پایگاه صحرا · حمله به پایگاه · پایگاه برقرار", fa.Title);
            Assert.IsTrue(fa.Brief.IndexOf("بزرگراه", StringComparison.Ordinal) >= 0);

            var setup = new SkirmishResolvedSetup { CatalogId = "S002" };
            Assert.IsTrue(SkirmishExpandedCopyProjection.TryResolveHud(
                setup,
                SkirmishOutcomeKind.Victory,
                SkirmishEndReasonKind.MainBaseDestroyed,
                GameLocalization.EnglishLocaleCode,
                out SkirmishHudCopy victory));
            Assert.AreEqual(SkirmishExpandedCopyProjection.ResultVictoryKey, victory.ResultKey);
            Assert.IsTrue(SkirmishExpandedCopyProjection.TryResolveHud(
                setup,
                SkirmishOutcomeKind.Defeat,
                SkirmishEndReasonKind.Surrender,
                GameLocalization.EnglishLocaleCode,
                out SkirmishHudCopy surrender));
            Assert.AreEqual(SkirmishExpandedCopyProjection.ResultSurrenderKey, surrender.ResultKey);
            Assert.AreEqual(10, SkirmishExpandedCopyProjection.RequiredS002Keys.Length);
        }

        [Test]
        public void WarAndCustomDoNotResolveFirstVisitBriefing()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsFalse(SkirmishExpandedCopyProjection.TryResolveLibraryBriefing(
                authored.DefinitionS002,
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.War,
                GameLocalization.EnglishLocaleCode,
                out _));
        }

        [Test]
        public void AssetExistenceDoesNotSetPlayable()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig row));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);

            var evidence = new SkirmishPublicationEvidence
            {
                CatalogId = "S002",
                DefinitionId = "skirmish.s002",
                AssetExists = true,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluate(
                row,
                authored.DefinitionS002,
                null,
                in evidence,
                out SkirmishPublicationStatus status,
                out List<SkirmishCompileReason> reasons));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, status);
            Assert.AreEqual(SkirmishReasonCode.MissingReadiness, reasons[0].Code);
        }

        [Test]
        public void HashMismatchAndWrongMatrixRejectPlayable()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig row);
            var setup = new SkirmishResolvedSetup
            {
                CatalogId = "S002",
                DefinitionId = "skirmish.s002",
                ContentHash = authored.DefinitionS002.ContentHash,
                SetupHash = 42,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            var mismatched = new SkirmishPublicationEvidence
            {
                CatalogId = "S002",
                DefinitionId = "skirmish.s002",
                ContentHash = "stale.hash",
                SetupHash = 99,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                ManualWin = true,
                AriaMatrix = true,
                EdgeFixtures = true,
                RecoveryEvidence = true,
                DeviceEvidence = true
            };
            Assert.IsFalse(SkirmishPublicationValidator.TryEvaluate(
                row, authored.DefinitionS002, setup, in mismatched, out _, out List<SkirmishCompileReason> hashReasons));
            Assert.AreEqual(SkirmishReasonCode.MatrixMismatch, hashReasons[0].Code);

            var war = mismatched;
            war.ContentHash = setup.ContentHash;
            war.SetupHash = setup.SetupHash;
            war.SizeId = SkirmishSizeId.War;
            Assert.IsFalse(SkirmishPublicationValidator.TryEvaluate(
                row, authored.DefinitionS002, setup, in war, out _, out List<SkirmishCompileReason> sizeReasons));
            Assert.AreEqual(SkirmishReasonCode.UnsupportedSize, sizeReasons[0].Code);
        }

        [Test]
        public void LegacyPrototypeCompatibilityIsVersionedAndDoesNotPublishS002()
        {
            var entries = new[]
            {
                new SkirmishBattleCatalogEntry { ScenarioId = "S001", Status = SkirmishBattleCatalogStatus.Planned, PlayableScenarioIndex = -1 },
                new SkirmishBattleCatalogEntry { ScenarioId = "S002", Status = SkirmishBattleCatalogStatus.Planned, PlayableScenarioIndex = -1 },
                new SkirmishBattleCatalogEntry { ScenarioId = "S025", Status = SkirmishBattleCatalogStatus.Planned, PlayableScenarioIndex = -1 },
                new SkirmishBattleCatalogEntry { ScenarioId = "S073", Status = SkirmishBattleCatalogStatus.Planned, PlayableScenarioIndex = -1 }
            };
            SkirmishPublicationValidator.ApplyLegacyPrototypeCompatibility(entries, 0);
            Assert.IsFalse(entries[0].IsPlayable);
            SkirmishPublicationValidator.ApplyLegacyPrototypeCompatibility(
                entries,
                SkirmishPublicationValidator.LegacyPrototypeCompatibilityVersion);
            Assert.IsTrue(entries[0].IsPlayable);
            Assert.AreEqual(0, entries[0].PlayableScenarioIndex);
            Assert.IsFalse(entries[1].IsPlayable);
            Assert.AreEqual("skirmish.s002.title", entries[1].TitleKey);
            Assert.AreEqual(-1, entries[1].PlayableScenarioIndex);
            Assert.IsTrue(entries[2].IsPlayable);
            Assert.IsTrue(entries[3].IsPlayable);
        }

        [Test]
        public void CompleteEvidenceWouldAllowPlayableButAuthoredRowStaysInProgress()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig row);
            var setup = new SkirmishResolvedSetup
            {
                CatalogId = "S002",
                DefinitionId = "skirmish.s002",
                ContentHash = authored.DefinitionS002.ContentHash,
                SetupHash = 7,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            var complete = new SkirmishPublicationEvidence
            {
                CatalogId = "S002",
                DefinitionId = "skirmish.s002",
                ContentHash = setup.ContentHash,
                SetupHash = 7,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                ManualWin = true,
                AriaMatrix = true,
                EdgeFixtures = true,
                RecoveryEvidence = true,
                DeviceEvidence = true
            };
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluate(
                row, authored.DefinitionS002, setup, in complete, out SkirmishPublicationStatus decision, out _));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, decision);
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedCatalogTests();
                suite.RegularStandardResolvesS002BriefingAndHudKeys();
                suite.WarAndCustomDoNotResolveFirstVisitBriefing();
                suite.AssetExistenceDoesNotSetPlayable();
                suite.HashMismatchAndWrongMatrixRejectPlayable();
                suite.LegacyPrototypeCompatibilityIsVersionedAndDoesNotPublishS002();
                suite.CompleteEvidenceWouldAllowPlayableButAuthoredRowStaysInProgress();
                Debug.Log("[SkirmishExpandedCatalogTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedCatalogTests] result=Failed\n" + exception);
                throw;
            }
        }
    }
}
