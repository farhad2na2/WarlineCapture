using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
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
        public void S003PublicationStaysInProgressAndS002AssetStaysPlayable()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig memoryS002));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, memoryS002.Status);
            Assert.IsTrue(authored.Publication.TryGet("S003", out SkirmishPublicationRowConfig memoryS003));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, memoryS003.Status);

            var publication = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset");
            Assert.IsTrue(publication.TryGet("S002", out SkirmishPublicationRowConfig assetS002));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, assetS002.Status);
            Assert.IsTrue(publication.TryGet("S003", out SkirmishPublicationRowConfig assetS003));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, assetS003.Status);

            CompileS003ForPublication(out SkirmishResolvedSetup setup);
            var complete = new SkirmishPublicationEvidence
            {
                CatalogId = "S003",
                DefinitionId = "skirmish.s003",
                ContentHash = setup.ContentHash,
                SetupHash = setup.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                ManualWin = true,
                AriaMatrix = true,
                EdgeFixtures = true,
                RecoveryEvidence = true,
                DeviceEvidence = true,
                AssetExists = true
            };
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluate(
                assetS003,
                authored.DefinitionS003,
                setup,
                in complete,
                out SkirmishPublicationStatus status,
                out _));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, status);
            Assert.AreEqual(SkirmishPublicationStatus.Playable, assetS002.Status);
        }

        [Test]
        public void S004PublicationStaysInProgressAndPriorRowsStayPlayable()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(authored.Publication.TryGet("S004", out SkirmishPublicationRowConfig memory));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, memory.Status);
            var publication = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset");
            Assert.IsTrue(publication.TryGet("S002", out SkirmishPublicationRowConfig s002));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s002.Status);
            Assert.IsTrue(publication.TryGet("S003", out SkirmishPublicationRowConfig s003));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s003.Status);
            Assert.IsTrue(publication.TryGet("S004", out SkirmishPublicationRowConfig s004));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s004.Status);

            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS004.RequiredFeatureIds };
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(
                authored.DefinitionS004,
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                SkirmishS004FirstVisit.SeedA,
                manifest,
                matrix,
                out SkirmishResolvedSetup setup,
                out System.Collections.Generic.List<SkirmishCompileReason> reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            var complete = new SkirmishPublicationEvidence
            {
                CatalogId = "S004",
                DefinitionId = "skirmish.s004",
                ContentHash = setup.ContentHash,
                SetupHash = setup.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                ManualWin = true,
                AriaMatrix = true,
                EdgeFixtures = true,
                RecoveryEvidence = true,
                DeviceEvidence = true,
                AssetExists = true
            };
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluate(
                s004,
                authored.DefinitionS004,
                setup,
                in complete,
                out SkirmishPublicationStatus status,
                out _));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, status);
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s002.Status);
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s003.Status);
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s004.Status);
        }

        [Test]
        public void S005PublicationStaysInProgressAndPriorRowsStayPlayable()
        {
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(authored.Publication.TryGet("S005", out SkirmishPublicationRowConfig memory));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, memory.Status);
            Assert.IsTrue(authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig memoryS002));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, memoryS002.Status);
            var publication = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset");
            Assert.IsTrue(publication.TryGet("S002", out SkirmishPublicationRowConfig s002));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s002.Status);
            Assert.IsTrue(publication.TryGet("S003", out SkirmishPublicationRowConfig s003));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s003.Status);
            Assert.IsTrue(publication.TryGet("S004", out SkirmishPublicationRowConfig s004));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s004.Status);
            Assert.IsTrue(publication.TryGet("S005", out SkirmishPublicationRowConfig s005));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, s005.Status);

            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS005.RequiredFeatureIds };
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(
                authored.DefinitionS005,
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                SkirmishS005FirstVisit.SeedA,
                manifest,
                matrix,
                out SkirmishResolvedSetup setup,
                out System.Collections.Generic.List<SkirmishCompileReason> reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            var complete = new SkirmishPublicationEvidence
            {
                CatalogId = "S005",
                DefinitionId = "skirmish.s005",
                ContentHash = setup.ContentHash,
                SetupHash = setup.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                ManualWin = true,
                AriaMatrix = true,
                EdgeFixtures = true,
                RecoveryEvidence = true,
                DeviceEvidence = true,
                AssetExists = true
            };
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluate(
                s005,
                authored.DefinitionS005,
                setup,
                in complete,
                out SkirmishPublicationStatus status,
                out _));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, status);
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s002.Status);
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s003.Status);
            Assert.AreEqual(SkirmishPublicationStatus.Playable, s004.Status);
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, s005.Status);
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

        private static void CompileS003ForPublication(out SkirmishResolvedSetup setup)
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out System.Collections.Generic.List<SkirmishSetupMatrixRow> matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS003.RequiredFeatureIds };
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(
                authored.DefinitionS003,
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                SkirmishS003FirstVisit.SeedA,
                manifest,
                matrix,
                out setup,
                out System.Collections.Generic.List<SkirmishCompileReason> reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
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
                suite.S003PublicationStaysInProgressAndS002AssetStaysPlayable();
                suite.S004PublicationStaysInProgressAndPriorRowsStayPlayable();
                suite.S005PublicationStaysInProgressAndPriorRowsStayPlayable();
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
