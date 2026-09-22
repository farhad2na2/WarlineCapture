using System;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishS003AriaHarnessTests
    {
        [Test]
        public void CheckedInRunsCsvStaysHeaderOnly()
        {
            SkirmishExpandedAriaHarnessChecks.AssertCheckedInRunsCsvStaysHeaderOnly(
                SkirmishAcceptanceScaffold.S003RelativeRunsPath);
        }

        [Test]
        public void ForcedVictoryDoesNotAppend()
        {
            SkirmishExpandedAriaHarnessChecks.AssertForcedVictoryDoesNotAppend(
                SkirmishAcceptanceCensusCapture.S003CatalogId,
                SkirmishS003FirstVisit.SeedA,
                "S003-aria-rs-104732-en-1",
                SkirmishAcceptanceScaffold.S003RelativeReportEvidenceDirectory);
        }

        [Test]
        public void UnfinishedOutcomeIsRefusedUntilTheMatchEnds()
        {
            SkirmishExpandedAriaHarnessChecks.AssertUnfinishedOutcomeIsRefused(
                SkirmishAcceptanceCensusCapture.S003CatalogId,
                SkirmishS003FirstVisit.SeedA,
                "S003-aria-rs-104732-en-1",
                SkirmishAcceptanceScaffold.S003RelativeReportEvidenceDirectory);
        }

        [Test]
        public void FinishedOutcomesAppendAndOnlyUnguidedVictoryCounts()
        {
            SkirmishExpandedAriaHarnessChecks.AssertFinishedOutcomesAppendAndOnlyUnguidedVictoryCounts(
                SkirmishAcceptanceCensusCapture.S003CatalogId,
                SkirmishS003FirstVisit.SeedA,
                SkirmishS003FirstVisit.SeedB,
                SkirmishAcceptanceScaffold.S003RelativeReportEvidenceDirectory,
                SkirmishS003AriaRunHarness.InvalidSeedError);
        }

        [Test]
        public void S002SeedIsNotAnS003Slot()
        {
            SkirmishExpandedAriaHarnessChecks.AssertForeignSeedIsRefused(
                SkirmishAcceptanceCensusCapture.S003CatalogId,
                SkirmishAcceptanceCensusCapture.FirstVisitSeed,
                SkirmishS003AriaRunHarness.InvalidSeedError,
                "104732");
        }

        [Test]
        public void PayloadLoggersDoNotStampVictory()
        {
            string described = SkirmishS003AriaRunHarness.DescribeLaunch(
                SkirmishS003FirstVisit.SeedA,
                GameLocalization.EnglishLocaleCode);
            Assert.IsTrue(described.IndexOf("catalog=S003", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("seed=104732", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(SkirmishS003AriaRunHarness.EvidenceDirectory().Replace('\\', '/').EndsWith(
                "S003/_Evidence",
                StringComparison.Ordinal));

            string persian = SkirmishS003AriaRunHarness.DescribeLaunch(
                SkirmishS003FirstVisit.SeedA,
                GameLocalization.PersianLocaleCode);
            Assert.IsTrue(persian.IndexOf("locale=fa-IR", StringComparison.Ordinal) >= 0);

            Assert.IsTrue(SkirmishAriaAcceptancePayload.TryCreateFirstVisitS003(
                SkirmishS003FirstVisit.SeedA,
                GameLocalization.EnglishLocaleCode,
                out SkirmishAriaAcceptancePayload payload,
                out string error),
                error);
            string play = AriaPlayEditorValidation.AcceptExpandedPayload(in payload);
            string watch = SkirmishExpandedAriaWatchValidation.AcceptExpandedPayload(in payload);
            Assert.IsTrue(play.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(watch.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(play.IndexOf("seed=104732", StringComparison.Ordinal) >= 0);

            InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(() =>
                SkirmishS003AriaRunHarness.DescribeLaunch(
                    SkirmishAcceptanceCensusCapture.FirstVisitSeed,
                    GameLocalization.EnglishLocaleCode));
            Assert.IsTrue(rejected.Message.IndexOf("104732", StringComparison.Ordinal) >= 0);
        }

        [Test]
        public void SimulationStallFailsFastAfterGrace()
        {
            SkirmishExpandedAriaHarnessChecks.AssertSimulationStallFailsFastAfterGrace();
        }

        [Test]
        public void ExpandedObjectiveClockProjectsOntoMatchElapsed()
        {
            SkirmishExpandedAriaHarnessChecks.AssertExpandedObjectiveClockProjectsOntoMatchElapsed(
                SkirmishAcceptanceCensusCapture.S003CatalogId);
        }

        [Test]
        public void FirstVisitSeedCompilesWithoutStampingVictory()
        {
            SkirmishExpandedAriaHarnessChecks.AssertFirstVisitSeedCompiles(
                SkirmishAcceptanceCensusCapture.S003CatalogId,
                SkirmishS003FirstVisit.SeedA);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishS003AriaHarnessTests();
                suite.CheckedInRunsCsvStaysHeaderOnly();
                suite.ForcedVictoryDoesNotAppend();
                suite.UnfinishedOutcomeIsRefusedUntilTheMatchEnds();
                suite.FinishedOutcomesAppendAndOnlyUnguidedVictoryCounts();
                suite.S002SeedIsNotAnS003Slot();
                suite.PayloadLoggersDoNotStampVictory();
                suite.SimulationStallFailsFastAfterGrace();
                suite.ExpandedObjectiveClockProjectsOntoMatchElapsed();
                suite.FirstVisitSeedCompilesWithoutStampingVictory();
                Debug.Log("[SkirmishS003AriaHarnessTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishS003AriaHarnessTests] result=Failed\n" + exception);
                throw;
            }
        }
    }
}
