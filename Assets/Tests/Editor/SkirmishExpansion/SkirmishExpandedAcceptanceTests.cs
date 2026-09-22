using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Editor;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishExpandedAcceptanceTests
    {
        [Test]
        public void CheckedInRunsCsvUsesMandatedHeader()
        {
            string root = ProjectRoot();
            Assert.IsTrue(
                SkirmishAcceptanceScaffold.TryReadRunsHeader(root, out string header, out string error),
                error);
            Assert.AreEqual(SkirmishAcceptanceScaffold.RequiredHeader, header);
            Assert.AreEqual(20, SkirmishAcceptanceScaffold.RequiredHeaderFields.Length);
            string[] lines = File.ReadAllLines(Path.Combine(root, SkirmishAcceptanceScaffold.RelativeRunsPath));
            Assert.AreEqual(1, lines.Length, "runs.csv must stay header-only until a real match is recorded.");
        }

        [Test]
        public void AcceptanceMarkdownListsRequiredMatrix()
        {
            string root = ProjectRoot();
            Assert.IsTrue(
                SkirmishAcceptanceScaffold.TryReadAcceptanceMarkdown(root, out string markdown, out string error),
                error);
            Assert.IsTrue(SkirmishAcceptanceScaffold.ListsRequiredMatrix(markdown, out string matrixError), matrixError);
            SkirmishAcceptanceMatrixSlot[] slots = SkirmishAcceptanceScaffold.RequiredFirstVisitSlots();
            Assert.AreEqual(28, slots.Length);
            Assert.AreEqual("S002-manual-rs-104731-en", slots[0].RunId);
            Assert.AreEqual("manual", slots[0].Kind);
            Assert.AreEqual("aria", slots[1].Kind);
            Assert.AreEqual(18, CountKind(slots, "aria"));
            Assert.AreEqual(5, CountKind(slots, "edge"));
            Assert.AreEqual(3, CountKind(slots, "recovery"));
            Assert.IsTrue(markdown.IndexOf(slots[0].RunId, StringComparison.Ordinal) >= 0);
            Assert.IsTrue(markdown.IndexOf("S002-aria-rs-155923-fa-3", StringComparison.Ordinal) >= 0);
        }

        [Test]
        public void RegularStandardCensusCapturesStableHashesWithoutPlayable()
        {
            LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishAcceptanceCensusCapture.TryCaptureRegularStandard(
                authored,
                matrix,
                out SkirmishAcceptanceCensus first,
                out List<SkirmishCompileReason> reasons),
                reasons == null || reasons.Count == 0 ? "census failed" : reasons[0].ToString());
            Assert.IsTrue(SkirmishAcceptanceCensusCapture.TryCaptureRegularStandard(
                authored,
                matrix,
                out SkirmishAcceptanceCensus second,
                out _));

            Assert.AreEqual("S002", first.CatalogId);
            Assert.AreEqual("skirmish.s002", first.DefinitionId);
            Assert.AreEqual(1, first.DefinitionVersion);
            Assert.AreEqual("Standard", first.Size);
            Assert.AreEqual("Regular", first.Difficulty);
            Assert.AreEqual(104731, first.Seed);
            Assert.AreEqual(20, first.PlayerInfantry);
            Assert.AreEqual(3, first.PlayerGround);
            Assert.AreEqual(23, first.PlayerCombat);
            Assert.AreEqual(900, first.MaterialsEach);
            Assert.AreEqual(1080, first.DeadlineSeconds);
            Assert.IsTrue(first.MeasuredLayoutBound);
            Assert.IsFalse(first.PlayableMarked);
            Assert.AreEqual(64, first.CodeHash.Length);
            Assert.AreEqual(64, first.ConfigHash.Length);
            Assert.AreEqual(first.CodeHash, second.CodeHash);
            Assert.AreEqual(first.ConfigHash, second.ConfigHash);
            Assert.AreEqual(first.SetupHash, second.SetupHash);
            Assert.AreEqual(SkirmishAcceptanceCensusCapture.ComputeCodeHash(), first.CodeHash);
            Assert.IsTrue(
                first.CodeHash.IndexOfAny(new[] { 'A', 'B', 'C', 'D', 'E', 'F' }) < 0,
                "code_hash must be lowercase hex.");

            string log = SkirmishAcceptanceCensusCapture.FormatLog(first);
            Assert.IsTrue(log.StartsWith("[SkirmishAcceptanceCensus] result=Passed", StringComparison.Ordinal));
            Assert.IsTrue(log.IndexOf("playable=0", StringComparison.Ordinal) >= 0);

            Assert.IsTrue(authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig row));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);
            var evidence = new SkirmishPublicationEvidence
            {
                CatalogId = first.CatalogId,
                DefinitionId = first.DefinitionId,
                ContentHash = first.ContentHash,
                SetupHash = first.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            var setup = new SkirmishResolvedSetup
            {
                CatalogId = first.CatalogId,
                DefinitionId = first.DefinitionId,
                ContentHash = first.ContentHash,
                SetupHash = first.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluate(
                row, authored.DefinitionS002, setup, in evidence, out SkirmishPublicationStatus status, out _));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, status);
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);
        }

        [Test]
        public void FirstVisitPayloadRejectsWarAndUnknownLocale()
        {
            Assert.IsFalse(SkirmishAriaAcceptancePayload.TryCreateFirstVisitS002(
                393243,
                GameLocalization.EnglishLocaleCode,
                out _,
                out string seedError));
            Assert.IsTrue(seedError.IndexOf("104731", StringComparison.Ordinal) >= 0);

            Assert.IsTrue(SkirmishAriaAcceptancePayload.TryCreate(
                SkirmishAcceptanceCensusCapture.CatalogId,
                SkirmishAcceptanceCensusCapture.DefinitionId,
                SkirmishAcceptanceCensusCapture.DefinitionVersion,
                SkirmishSizeId.War,
                SkirmishDifficultyId.Regular,
                393243,
                GameLocalization.EnglishLocaleCode,
                out SkirmishAriaAcceptancePayload war,
                out _));
            Assert.IsFalse(war.IsFirstVisitRegularStandard);

            Assert.IsFalse(SkirmishAriaAcceptancePayload.TryCreateFirstVisitS002(
                104731,
                "de",
                out _,
                out string localeError));
            Assert.IsTrue(localeError.IndexOf("en", StringComparison.Ordinal) >= 0);
        }

        [Test]
        public void AriaPlayAndWatchFacilitiesLogSelectedConfiguration()
        {
            Assert.IsTrue(SkirmishAriaAcceptancePayload.TryCreateFirstVisitS002(
                104731,
                GameLocalization.EnglishLocaleCode,
                out SkirmishAriaAcceptancePayload payload,
                out string error),
                error);
            Assert.IsTrue(payload.IsFirstVisitRegularStandard);

            string play = AriaPlayEditorValidation.AcceptExpandedPayload(in payload);
            Assert.IsTrue(play.StartsWith("[AriaPlayEditorValidation] selected ", StringComparison.Ordinal));
            Assert.IsTrue(play.IndexOf("definition=skirmish.s002", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(play.IndexOf("size=Standard", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(play.IndexOf("difficulty=Regular", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(play.IndexOf("seed=104731", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(play.IndexOf("locale=en", StringComparison.Ordinal) >= 0);

            string watch = SkirmishExpandedAriaWatchValidation.AcceptExpandedPayload(
                SkirmishAcceptanceCensusCapture.DefinitionId,
                SkirmishSizeId.Standard,
                SkirmishDifficultyId.Regular,
                130365,
                GameLocalization.PersianLocaleCode);
            Assert.IsTrue(watch.StartsWith(SkirmishExpandedAriaWatchValidation.SelectedPrefix, StringComparison.Ordinal));
            Assert.IsTrue(watch.IndexOf("seed=130365", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(watch.IndexOf("locale=fa-IR", StringComparison.Ordinal) >= 0);
            string legacyWatch = SkirmishIndustrialBasinAriaWatchProbe.AcceptExpandedPayload(in payload);
            Assert.IsTrue(legacyWatch.StartsWith("[SkirmishIndustrialBasinAriaWatch] selected ", StringComparison.Ordinal));
            Assert.IsTrue(legacyWatch.IndexOf("definition=skirmish.s002", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(watch.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(play.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
        }

        [Test]
        public void PendingRunRowKeepsEmptyResultAndCensusProbeDoesNotPublish()
        {
            SkirmishAcceptanceMatrixSlot[] slots = SkirmishAcceptanceScaffold.RequiredFirstVisitSlots();
            string row = SkirmishAcceptanceScaffold.FormatPendingRow(in slots[0], "1", "abc", "def");
            Assert.AreEqual(20, SkirmishAcceptanceScaffold.CountPendingResultColumns(row));
            Assert.IsTrue(row.StartsWith("S002-manual-rs-104731-en,S002,1,abc,def,Standard,Regular,104731,en,", StringComparison.Ordinal));
            Assert.IsFalse(row.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) >= 0);

            string census = SkirmishAcceptanceCensusProbe.CaptureRegularStandard();
            Assert.IsTrue(census.StartsWith("[SkirmishAcceptanceCensus] result=Passed", StringComparison.Ordinal));
            Assert.IsTrue(census.IndexOf("playable=0", StringComparison.Ordinal) >= 0);
        }

        [Test]
        public void PlayableFlipRequiresHashesAndEvidencePaths()
        {
            LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishAcceptanceCensusCapture.TryCaptureRegularStandard(
                authored, matrix, out SkirmishAcceptanceCensus census, out _));
            authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig row);
            var setup = new SkirmishResolvedSetup
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            var missing = new SkirmishPlayableFlipRequest
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                EvidenceDirectory = "missing-evidence",
                RequiredRelativeFiles = SkirmishAcceptanceScaffold.RequiredGameViewEvidenceFiles,
                ConfirmWrite = true
            };
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                row, authored.DefinitionS002, setup, in missing, path => false,
                out SkirmishPublicationStatus missingStatus, out List<SkirmishCompileReason> missingReasons));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, missingStatus);
            Assert.AreEqual(SkirmishReasonCode.MissingReadiness, missingReasons[0].Code);
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);

            var stale = missing;
            stale.ContentHash = "stale.hash";
            stale.EvidenceDirectory = "present";
            Assert.IsFalse(SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                row, authored.DefinitionS002, setup, in stale, path => true,
                out _, out List<SkirmishCompileReason> hashReasons));
            Assert.AreEqual(SkirmishReasonCode.MatrixMismatch, hashReasons[0].Code);

            var ready = missing;
            ready.EvidenceDirectory = "present";
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                row, authored.DefinitionS002, setup, in ready, path => true,
                out SkirmishPublicationStatus readyStatus, out _));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, readyStatus);
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);

            string dryRun = SkirmishPublicationFlipMenu.EvaluateS002PlayableFlip();
            Assert.IsTrue(dryRun.IndexOf("playable=0", StringComparison.Ordinal) >= 0);
            Assert.IsFalse(dryRun.IndexOf("playable=1", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig after));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, after.Status);
            Assert.IsTrue(SkirmishS002GameViewCapture.DescribeLaunchPayload().IndexOf("seed=104731", StringComparison.Ordinal) >= 0);
            Assert.AreEqual("_Evidence", SkirmishAcceptanceScaffold.RelativeEvidenceDirectory);
            Assert.IsTrue(SkirmishAcceptanceScaffold.HasRequiredGameViewEvidence("ok", path => true));
            Assert.IsFalse(SkirmishAcceptanceScaffold.HasRequiredGameViewEvidence("ok", path => false));
            Assert.AreEqual(
                Path.Combine("proj", "_Evidence"),
                SkirmishAcceptanceScaffold.ResolveEvidenceDirectory("proj", path => false));
        }

        [Test]
        public void S003GameViewNamesAndDryFlipDoNotPublish()
        {
            Assert.AreEqual(104732, SkirmishS003FirstVisit.SeedA);
            Assert.AreEqual(
                "s002-regular-standard-104731-playing.png",
                SkirmishAcceptanceScaffold.FormatRegularStandardPlayingPng(
                    SkirmishAcceptanceCensusCapture.CatalogId,
                    SkirmishAcceptanceCensusCapture.FirstVisitSeed));
            Assert.AreEqual(
                SkirmishAcceptanceScaffold.PlayingPngFileName,
                SkirmishAcceptanceScaffold.FormatRegularStandardPlayingPng("S002", 104731));
            Assert.AreEqual(
                SkirmishAcceptanceScaffold.GameViewSidecarFileName,
                SkirmishAcceptanceScaffold.FormatRegularStandardGameViewSidecar("S002", 104731));
            Assert.AreEqual(
                SkirmishAcceptanceScaffold.RelativeReportEvidenceDirectory,
                SkirmishAcceptanceScaffold.RelativeReportEvidenceDirectoryFor("S002"));
            Assert.AreEqual(
                "s003-regular-standard-104732-playing.png",
                SkirmishAcceptanceScaffold.S003PlayingPngFileName);
            Assert.AreEqual(
                "s003-regular-standard-104732-gameview.json",
                SkirmishAcceptanceScaffold.S003GameViewSidecarFileName);
            Assert.AreEqual(
                "Design/AgentReports/SkirmishExpansion/S003/_Evidence",
                SkirmishAcceptanceScaffold.S003RelativeReportEvidenceDirectory);
            Assert.AreEqual(2, SkirmishAcceptanceScaffold.S003RequiredGameViewEvidenceFiles.Length);

            LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishAcceptanceCensusCapture.TryCaptureS003RegularStandard(
                authored,
                matrix,
                out SkirmishAcceptanceCensus census,
                out List<SkirmishCompileReason> reasons),
                reasons == null || reasons.Count == 0 ? "census failed" : reasons[0].ToString());
            Assert.AreEqual("S003", census.CatalogId);
            Assert.AreEqual("skirmish.s003", census.DefinitionId);
            Assert.AreEqual(104732, census.Seed);
            Assert.AreEqual("Standard", census.Size);
            Assert.AreEqual("Regular", census.Difficulty);
            Assert.AreEqual(12, census.PlayerInfantry);
            Assert.AreEqual(450, census.MaterialsEach);
            Assert.AreEqual(1080, census.DeadlineSeconds);
            Assert.IsTrue(census.MeasuredLayoutBound);
            Assert.IsFalse(census.PlayableMarked);
            Assert.IsTrue(SkirmishAcceptanceCensusCapture.TryCaptureRegularStandard(
                authored, matrix, out SkirmishAcceptanceCensus s002, out _));
            Assert.AreEqual("S002", s002.CatalogId);
            Assert.AreEqual(104731, s002.Seed);
            Assert.AreNotEqual(s002.ContentHash, census.ContentHash);

            string described = SkirmishS003GameViewCapture.DescribeLaunchPayload();
            Assert.IsTrue(described.IndexOf("catalog=S003", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("definition=skirmish.s003", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("size=Standard", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("difficulty=Regular", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("seed=104732", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(
                SkirmishS003GameViewCapture.ReportEvidenceDirectory().EndsWith(
                    Path.Combine("S003", "_Evidence"),
                    StringComparison.Ordinal));
            Assert.AreEqual(
                Path.Combine("proj", "_Evidence"),
                SkirmishAcceptanceScaffold.ResolveS003EvidenceDirectory("proj", path => false));

            authored.Publication.TryGet("S003", out SkirmishPublicationRowConfig row);
            var setup = new SkirmishResolvedSetup
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            var missing = new SkirmishPlayableFlipRequest
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                EvidenceDirectory = "missing-evidence",
                RequiredRelativeFiles = SkirmishAcceptanceScaffold.S003RequiredGameViewEvidenceFiles,
                ConfirmWrite = true
            };
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                row, authored.DefinitionS003, setup, in missing, path => false,
                out SkirmishPublicationStatus missingStatus, out _));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, missingStatus);
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);

            var stale = missing;
            stale.ContentHash = "stale.hash";
            stale.EvidenceDirectory = "present";
            Assert.IsFalse(SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                row, authored.DefinitionS003, setup, in stale, path => true,
                out _, out List<SkirmishCompileReason> hashReasons));
            Assert.AreEqual(SkirmishReasonCode.MatrixMismatch, hashReasons[0].Code);

            var ready = missing;
            ready.ContentHash = census.ContentHash;
            ready.EvidenceDirectory = "present";
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                row, authored.DefinitionS003, setup, in ready, path => true,
                out SkirmishPublicationStatus readyStatus, out _));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, readyStatus);
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);

            string dryRun = SkirmishPublicationFlipMenu.EvaluateS003PlayableFlip();
            Assert.IsTrue(dryRun.IndexOf("catalog=S003", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(dryRun.IndexOf("playable=0", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(dryRun.IndexOf("confirm=0", StringComparison.Ordinal) >= 0);
            Assert.IsFalse(dryRun.IndexOf("playable=1", StringComparison.Ordinal) >= 0);

            var publication = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset");
            Assert.IsTrue(publication.TryGet("S002", out SkirmishPublicationRowConfig assetS002));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, assetS002.Status);
            Assert.IsTrue(publication.TryGet("S003", out SkirmishPublicationRowConfig assetS003));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, assetS003.Status);
        }

        [Test]
        public void S004GameViewNamesAndDryFlipDoNotPublish()
        {
            Assert.AreEqual(104733, SkirmishS004FirstVisit.SeedA);
            Assert.AreEqual(
                "s004-regular-standard-104733-playing.png",
                SkirmishAcceptanceScaffold.S004PlayingPngFileName);
            Assert.AreEqual(
                "s004-regular-standard-104733-gameview.json",
                SkirmishAcceptanceScaffold.S004GameViewSidecarFileName);
            Assert.AreEqual(
                "Design/AgentReports/SkirmishExpansion/S004/_Evidence",
                SkirmishAcceptanceScaffold.S004RelativeReportEvidenceDirectory);
            Assert.AreEqual(2, SkirmishAcceptanceScaffold.S004RequiredGameViewEvidenceFiles.Length);
            Assert.AreEqual(
                "s003-regular-standard-104732-playing.png",
                SkirmishAcceptanceScaffold.S003PlayingPngFileName);

            LoadMatrix(out List<SkirmishSetupMatrixRow> matrix);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishAcceptanceCensusCapture.TryCaptureS004RegularStandard(
                authored,
                matrix,
                out SkirmishAcceptanceCensus census,
                out List<SkirmishCompileReason> reasons),
                reasons == null || reasons.Count == 0 ? "census failed" : reasons[0].ToString());
            Assert.AreEqual("S004", census.CatalogId);
            Assert.AreEqual("skirmish.s004", census.DefinitionId);
            Assert.AreEqual(104733, census.Seed);
            Assert.AreEqual("Standard", census.Size);
            Assert.AreEqual("Regular", census.Difficulty);
            Assert.AreEqual(20, census.PlayerInfantry);
            Assert.AreEqual(3, census.PlayerGround);
            Assert.AreEqual(900, census.MaterialsEach);
            Assert.AreEqual(1080, census.DeadlineSeconds);
            Assert.IsTrue(census.MeasuredLayoutBound);
            Assert.IsFalse(census.PlayableMarked);
            Assert.IsTrue(SkirmishAcceptanceCensusCapture.TryCaptureS003RegularStandard(
                authored, matrix, out SkirmishAcceptanceCensus s003, out _));
            Assert.AreEqual("S003", s003.CatalogId);
            Assert.AreEqual(104732, s003.Seed);
            Assert.AreNotEqual(s003.ContentHash, census.ContentHash);
            Assert.IsTrue(SkirmishAcceptanceCensusCapture.TryCaptureRegularStandard(
                authored, matrix, out SkirmishAcceptanceCensus s002, out _));
            Assert.AreEqual("S002", s002.CatalogId);
            Assert.AreNotEqual(s002.ContentHash, census.ContentHash);

            string described = SkirmishS004GameViewCapture.DescribeLaunchPayload();
            Assert.IsTrue(described.IndexOf("catalog=S004", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("definition=skirmish.s004", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("size=Standard", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("difficulty=Regular", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("seed=104733", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(
                SkirmishS004GameViewCapture.ReportEvidenceDirectory().EndsWith(
                    Path.Combine("S004", "_Evidence"),
                    StringComparison.Ordinal));
            Assert.AreEqual(
                Path.Combine("proj", "_Evidence"),
                SkirmishAcceptanceScaffold.ResolveS004EvidenceDirectory("proj", path => false));

            authored.Publication.TryGet("S004", out SkirmishPublicationRowConfig row);
            var setup = new SkirmishResolvedSetup
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            var missing = new SkirmishPlayableFlipRequest
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                EvidenceDirectory = "missing-evidence",
                RequiredRelativeFiles = SkirmishAcceptanceScaffold.S004RequiredGameViewEvidenceFiles,
                ConfirmWrite = true
            };
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                row, authored.DefinitionS004, setup, in missing, path => false,
                out SkirmishPublicationStatus missingStatus, out _));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, missingStatus);
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);

            var stale = missing;
            stale.ContentHash = "stale.hash";
            stale.EvidenceDirectory = "present";
            Assert.IsFalse(SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                row, authored.DefinitionS004, setup, in stale, path => true,
                out _, out List<SkirmishCompileReason> hashReasons));
            Assert.AreEqual(SkirmishReasonCode.MatrixMismatch, hashReasons[0].Code);

            var ready = missing;
            ready.ContentHash = census.ContentHash;
            ready.EvidenceDirectory = "present";
            Assert.IsTrue(SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                row, authored.DefinitionS004, setup, in ready, path => true,
                out SkirmishPublicationStatus readyStatus, out _));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, readyStatus);
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, row.Status);

            string dryRun = SkirmishPublicationFlipMenu.EvaluateS004PlayableFlip();
            Assert.IsTrue(dryRun.IndexOf("catalog=S004", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(dryRun.IndexOf("playable=0", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(dryRun.IndexOf("confirm=0", StringComparison.Ordinal) >= 0);
            Assert.IsFalse(dryRun.IndexOf("playable=1", StringComparison.Ordinal) >= 0);

            var publication = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset");
            Assert.IsTrue(publication.TryGet("S002", out SkirmishPublicationRowConfig assetS002));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, assetS002.Status);
            Assert.IsTrue(publication.TryGet("S003", out SkirmishPublicationRowConfig assetS003));
            Assert.AreEqual(SkirmishPublicationStatus.Playable, assetS003.Status);
            Assert.IsTrue(publication.TryGet("S004", out SkirmishPublicationRowConfig assetS004));
            Assert.AreEqual(SkirmishPublicationStatus.InProgress, assetS004.Status);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedAcceptanceTests();
                suite.CheckedInRunsCsvUsesMandatedHeader();
                suite.AcceptanceMarkdownListsRequiredMatrix();
                suite.RegularStandardCensusCapturesStableHashesWithoutPlayable();
                suite.FirstVisitPayloadRejectsWarAndUnknownLocale();
                suite.AriaPlayAndWatchFacilitiesLogSelectedConfiguration();
                suite.PendingRunRowKeepsEmptyResultAndCensusProbeDoesNotPublish();
                suite.PlayableFlipRequiresHashesAndEvidencePaths();
                suite.S003GameViewNamesAndDryFlipDoNotPublish();
                suite.S004GameViewNamesAndDryFlipDoNotPublish();
                Debug.Log("[SkirmishExpandedAcceptanceTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedAcceptanceTests] result=Failed\n" + exception);
                throw;
            }
        }

        private static void LoadMatrix(out List<SkirmishSetupMatrixRow> matrix)
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(ProjectRoot(), out matrix, out string error), error);
        }

        private static string ProjectRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static int CountKind(SkirmishAcceptanceMatrixSlot[] slots, string kind)
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Kind == kind)
                    count++;
            }

            return count;
        }
    }
}
