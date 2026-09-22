using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>
    /// Recorder checks shared by the S003 and S004 harness suites.
    /// They run without a live match and never write the checked-in runs.csv.
    /// </summary>
    public static class SkirmishExpandedAriaHarnessChecks
    {
        public static void AssertCheckedInRunsCsvStaysHeaderOnly(string relativeRunsPath)
        {
            string path = Path.Combine(ProjectRoot(), relativeRunsPath);
            string[] lines = File.ReadAllLines(path);
            Assert.AreEqual(1, lines.Length, "runs.csv must stay header-only until a live match appends a row.");
            Assert.AreEqual(SkirmishAcceptanceScaffold.RequiredHeader, lines[0]);
        }

        public static void AssertForcedVictoryDoesNotAppend(string catalogId, int seed, string runId, string evidenceDirectory)
        {
            string path = TempCsv(catalogId);
            try
            {
                File.WriteAllText(path, SkirmishAcceptanceScaffold.RequiredHeader + "\n");
                var facts = Finished(catalogId, seed, runId, evidenceDirectory, SkirmishExpandedAriaRunLog.ResultVictory);
                facts.ForcedVictory = true;
                Assert.IsFalse(SkirmishExpandedAriaRunLog.TryAppendTerminalRow(
                    catalogId, path, in facts, out string row, out string error));
                Assert.IsNull(row);
                Assert.IsTrue(error.IndexOf("Forced Victory", StringComparison.Ordinal) >= 0);
                Assert.AreEqual(1, File.ReadAllLines(path).Length);
                Assert.IsFalse(SkirmishExpandedAriaRunLog.IsCountedAriaWin(in facts, SkirmishExpandedAriaRunLog.ResultVictory));
            }
            finally
            {
                File.Delete(path);
            }
        }

        public static void AssertUnfinishedOutcomeIsRefused(string catalogId, int seed, string runId, string evidenceDirectory)
        {
            string path = TempCsv(catalogId);
            try
            {
                var unfinished = Finished(catalogId, seed, runId, evidenceDirectory, SkirmishExpandedAriaRunLog.ResultVictory);
                unfinished.MatchFinished = false;
                unfinished.Aborted = false;
                Assert.IsFalse(SkirmishExpandedAriaRunLog.TryAcceptTerminal(in unfinished, out _, out string error));
                Assert.IsTrue(error.IndexOf("has not finished", StringComparison.Ordinal) >= 0);
                Assert.IsFalse(SkirmishExpandedAriaRunLog.TryAppendTerminalRow(catalogId, path, in unfinished, out _, out _));
                Assert.IsFalse(File.Exists(path));

                var aborted = unfinished;
                aborted.Aborted = true;
                aborted.RunId = runId;
                Assert.IsTrue(SkirmishExpandedAriaRunLog.TryAppendTerminalRow(
                    catalogId, path, in aborted, out string row, out _));
                Assert.AreEqual(catalogId, Field(row, 1));
                Assert.AreEqual(SkirmishExpandedAriaRunLog.ResultAbort, Field(row, 13));
                Assert.IsFalse(row.IndexOf("," + SkirmishExpandedAriaRunLog.ResultVictory + ",", StringComparison.Ordinal) >= 0);
                Assert.IsFalse(SkirmishExpandedAriaRunLog.IsCountedAriaWin(in aborted, SkirmishExpandedAriaRunLog.ResultAbort));
            }
            finally
            {
                File.Delete(path);
            }
        }

        public static void AssertFinishedOutcomesAppendAndOnlyUnguidedVictoryCounts(
            string catalogId,
            int primarySeed,
            int countedSeed,
            string evidenceDirectory,
            string invalidSeedError)
        {
            string path = TempCsv(catalogId);
            try
            {
                string header = SkirmishAcceptanceScaffold.RequiredHeader + "\n";
                Assert.IsTrue(SkirmishExpandedAriaRunLog.TryNextAriaRunId(
                    catalogId,
                    SeedPredicate(catalogId),
                    invalidSeedError,
                    header,
                    primarySeed,
                    GameLocalization.EnglishLocaleCode,
                    out string first,
                    out _));
                Assert.AreEqual(RunId(catalogId, primarySeed, "en", 1), first);

                AppendFinished(path, catalogId, primarySeed, evidenceDirectory, RunId(catalogId, primarySeed, "en", 1), SkirmishExpandedAriaRunLog.ResultDefeat, true, 0, 0, false);
                AppendFinished(path, catalogId, primarySeed, evidenceDirectory, RunId(catalogId, primarySeed, "en", 2), SkirmishExpandedAriaRunLog.ResultDraw, true, 0, 0, false);
                AppendFinished(path, catalogId, primarySeed, evidenceDirectory, catalogId + "-row-slow", SkirmishExpandedAriaRunLog.ResultVictory, false, 0, 0, false);
                AppendFinished(path, catalogId, primarySeed, evidenceDirectory, catalogId + "-row-violation", SkirmishExpandedAriaRunLog.ResultVictory, true, 1, 0, false);
                AppendFinished(path, catalogId, primarySeed, evidenceDirectory, catalogId + "-row-intervention", SkirmishExpandedAriaRunLog.ResultVictory, true, 0, 2, false);
                AppendFinished(path, catalogId, countedSeed, evidenceDirectory, RunId(catalogId, countedSeed, "fa", 1), SkirmishExpandedAriaRunLog.ResultVictory, true, 0, 0, true);

                string[] lines = File.ReadAllLines(path);
                Assert.AreEqual(7, lines.Length);
                Assert.AreEqual(catalogId, Field(lines[1], 1));
                Assert.AreEqual(primarySeed.ToString(CultureInfo.InvariantCulture), Field(lines[1], 7));
                Assert.AreEqual(SkirmishExpandedAriaRunLog.ResultDefeat, Field(lines[1], 13));
                Assert.AreEqual(SkirmishExpandedAriaRunLog.ResultDraw, Field(lines[2], 13));
                Assert.AreEqual("0", Field(lines[3], 11));
                Assert.AreEqual(SkirmishExpandedAriaRunLog.ResultVictory, Field(lines[6], 13));
                Assert.AreEqual("1", Field(lines[6], 11));
                Assert.AreEqual("0", Field(lines[6], 16));
                Assert.AreEqual("0", Field(lines[6], 17));

                string used = File.ReadAllText(path);
                Assert.IsTrue(SkirmishExpandedAriaRunLog.TryNextAriaRunId(
                    catalogId,
                    SeedPredicate(catalogId),
                    invalidSeedError,
                    used,
                    primarySeed,
                    GameLocalization.EnglishLocaleCode,
                    out string next,
                    out _));
                Assert.AreEqual(RunId(catalogId, primarySeed, "en", 3), next);

                string filled = used + RunId(catalogId, primarySeed, "en", 3) + ",rest\n";
                Assert.IsFalse(SkirmishExpandedAriaRunLog.TryNextAriaRunId(
                    catalogId,
                    SeedPredicate(catalogId),
                    invalidSeedError,
                    filled,
                    primarySeed,
                    GameLocalization.EnglishLocaleCode,
                    out _,
                    out string full));
                Assert.IsTrue(full.IndexOf("three", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        public static void AssertForeignSeedIsRefused(string catalogId, int foreignSeed, string invalidSeedError, string expectedSeedText)
        {
            Assert.IsFalse(SkirmishExpandedAriaRunLog.TryNextAriaRunId(
                catalogId,
                SeedPredicate(catalogId),
                invalidSeedError,
                SkirmishAcceptanceScaffold.RequiredHeader + "\n",
                foreignSeed,
                GameLocalization.EnglishLocaleCode,
                out string runId,
                out string error));
            Assert.IsNull(runId);
            Assert.IsTrue(error.IndexOf(expectedSeedText, StringComparison.Ordinal) >= 0);
        }

        public static void AssertSimulationStallFailsFastAfterGrace()
        {
            Assert.AreEqual(45d, SkirmishExpandedAriaRunLog.SimulationStallGraceSeconds);
            Assert.IsFalse(SkirmishExpandedAriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: false,
                matchElapsedSeconds: 0f,
                wallSecondsSincePlaying: 10d));
            Assert.IsTrue(SkirmishExpandedAriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: false,
                matchElapsedSeconds: 0f,
                wallSecondsSincePlaying: SkirmishExpandedAriaRunLog.SimulationStallGraceSeconds));
            Assert.IsTrue(SkirmishExpandedAriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: true,
                matchElapsedSeconds: 0f,
                wallSecondsSincePlaying: SkirmishExpandedAriaRunLog.SimulationStallGraceSeconds + 1d));
            Assert.IsFalse(SkirmishExpandedAriaRunLog.IsSimulationNotAdvancing(
                playing: true,
                simulationActive: true,
                matchElapsedSeconds: 0.5f,
                wallSecondsSincePlaying: 120d));
            Assert.AreEqual(
                SkirmishExpandedAriaRunLog.AbortReasonSimulationNotAdvancing,
                "simulationNotAdvancing");
        }

        public static void AssertExpandedObjectiveClockProjectsOntoMatchElapsed(string catalogId)
        {
            using var world = new World(nameof(AssertExpandedObjectiveClockProjectsOntoMatchElapsed) + catalogId);
            EntityManager em = world.EntityManager;
            Entity session = em.CreateEntity();
            em.AddComponentData(session, new SkirmishExpandedSessionComponent
            {
                SessionId = "elapsed-" + catalogId,
                CatalogId = catalogId,
                IsLegacy = 0,
                Phase = SkirmishSessionPhase.Playing
            });
            em.AddComponentData(session, new SkirmishMatchState
            {
                SessionId = "elapsed-" + catalogId,
                Phase = SkirmishPhase.Preparing,
                ElapsedSeconds = 0f
            });
            em.AddComponentData(session, new SkirmishObjectiveClockComponent
            {
                ElapsedSeconds = 37.5f,
                DeadlineSeconds = 1080,
                Paused = 0,
                Playing = 1
            });

            SkirmishExpandedSessionControlService.ProjectMatchPhase(em, session);

            var match = em.GetComponentData<SkirmishMatchState>(session);
            Assert.AreEqual(SkirmishPhase.Playing, match.Phase);
            Assert.AreEqual(37.5f, match.ElapsedSeconds, 0.001f);
            Assert.AreEqual(
                37.5f,
                SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match),
                0.001f);
        }

        public static void AssertFirstVisitSeedCompiles(string catalogId, int seed)
        {
            using var world = new World(nameof(AssertFirstVisitSeedCompiles) + catalogId);
            EntityManager em = world.EntityManager;
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(ProjectRoot(), out List<SkirmishSetupMatrixRow> matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(
                authored, catalogId, out SkirmishScenarioDefinitionConfig definition));
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                catalogId,
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                seed,
                authored,
                matrix,
                manifest,
                out SkirmishResolvedSetup setup,
                out _,
                out List<SkirmishCompileReason> reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            Assert.AreEqual(catalogId, setup.CatalogId);
            Assert.AreEqual(seed, setup.Seed);
            Assert.AreEqual(SkirmishSizeId.Standard, setup.SizeId);
            Assert.AreEqual(SkirmishDifficultyId.Regular, setup.DifficultyId);
        }

        private static void AppendFinished(
            string path,
            string catalogId,
            int seed,
            string evidenceDirectory,
            string runId,
            string outcome,
            bool normalSpeed,
            int violations,
            int interventions,
            bool expectCounted)
        {
            var facts = Finished(catalogId, seed, runId, evidenceDirectory, outcome);
            facts.NormalSpeed = normalSpeed;
            facts.InputViolations = violations;
            facts.HumanInterventions = interventions;
            if (string.Equals(outcome, SkirmishExpandedAriaRunLog.ResultVictory, StringComparison.Ordinal) &&
                runId.IndexOf("-fa-", StringComparison.Ordinal) >= 0)
                facts.Locale = GameLocalization.PersianLocaleCode;
            Assert.IsTrue(SkirmishExpandedAriaRunLog.TryAppendTerminalRow(
                catalogId, path, in facts, out string row, out string error), error);
            Assert.AreEqual(outcome, Field(row, 13));
            Assert.AreEqual(catalogId, Field(row, 1));
            Assert.AreEqual(expectCounted, SkirmishExpandedAriaRunLog.IsCountedAriaWin(in facts, outcome));
        }

        private static SkirmishExpandedAriaTerminalFacts Finished(
            string catalogId,
            int seed,
            string runId,
            string evidenceDirectory,
            string outcome)
        {
            return new SkirmishExpandedAriaTerminalFacts
            {
                RunId = runId,
                DefinitionVersion = 1,
                CodeHash = "abc",
                ConfigHash = "def",
                Seed = seed,
                Locale = GameLocalization.EnglishLocaleCode,
                Device = "Editor",
                NormalSpeed = true,
                StartedAtUtc = "2026-09-22T00:00:00Z",
                MatchFinished = true,
                MatchOutcome = outcome,
                EndReason = "MainBaseDestroyed",
                DurationSeconds = 90f,
                TracePath = evidenceDirectory + "/sample.jsonl",
                LogPath = evidenceDirectory + "/sample-log.txt",
                ForcedVictory = false
            };
        }

        private static Func<int, bool> SeedPredicate(string catalogId)
        {
            if (catalogId == SkirmishAcceptanceCensusCapture.S003CatalogId)
                return SkirmishAcceptanceCensusCapture.IsS003RegularStandardSeed;
            if (catalogId == SkirmishAcceptanceCensusCapture.S004CatalogId)
                return SkirmishAcceptanceCensusCapture.IsS004RegularStandardSeed;
            return SkirmishAcceptanceCensusCapture.IsRegularStandardAriaSeed;
        }

        private static string RunId(string catalogId, int seed, string token, int attempt)
        {
            return catalogId + "-aria-rs-" + seed + "-" + token + "-" + attempt;
        }

        private static string Field(string row, int index)
        {
            string[] fields = row.Split(',');
            Assert.Greater(fields.Length, index);
            return fields[index];
        }

        private static string TempCsv(string catalogId)
        {
            return Path.Combine(Path.GetTempPath(), catalogId.ToLowerInvariant() + "-aria-harness-" + Guid.NewGuid().ToString("N") + ".csv");
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
    }
}
