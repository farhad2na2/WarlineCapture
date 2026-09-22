using System;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishS004AriaHarnessTests
    {
        [Test]
        public void CheckedInRunsCsvStaysHeaderOnly()
        {
            SkirmishExpandedAriaHarnessChecks.AssertCheckedInRunsCsvStaysHeaderOnly(
                SkirmishAcceptanceScaffold.S004RelativeRunsPath);
        }

        [Test]
        public void ForcedVictoryDoesNotAppend()
        {
            SkirmishExpandedAriaHarnessChecks.AssertForcedVictoryDoesNotAppend(
                SkirmishAcceptanceCensusCapture.S004CatalogId,
                SkirmishS004FirstVisit.SeedA,
                "S004-aria-rs-104733-en-1",
                SkirmishAcceptanceScaffold.S004RelativeReportEvidenceDirectory);
        }

        [Test]
        public void UnfinishedOutcomeIsRefusedUntilTheMatchEnds()
        {
            SkirmishExpandedAriaHarnessChecks.AssertUnfinishedOutcomeIsRefused(
                SkirmishAcceptanceCensusCapture.S004CatalogId,
                SkirmishS004FirstVisit.SeedA,
                "S004-aria-rs-104733-en-1",
                SkirmishAcceptanceScaffold.S004RelativeReportEvidenceDirectory);
        }

        [Test]
        public void FinishedOutcomesAppendAndOnlyUnguidedVictoryCounts()
        {
            SkirmishExpandedAriaHarnessChecks.AssertFinishedOutcomesAppendAndOnlyUnguidedVictoryCounts(
                SkirmishAcceptanceCensusCapture.S004CatalogId,
                SkirmishS004FirstVisit.SeedA,
                SkirmishS004FirstVisit.SeedB,
                SkirmishAcceptanceScaffold.S004RelativeReportEvidenceDirectory,
                SkirmishS004AriaRunHarness.InvalidSeedError);
        }

        [Test]
        public void S002SeedIsNotAnS004Slot()
        {
            SkirmishExpandedAriaHarnessChecks.AssertForeignSeedIsRefused(
                SkirmishAcceptanceCensusCapture.S004CatalogId,
                SkirmishAcceptanceCensusCapture.FirstVisitSeed,
                SkirmishS004AriaRunHarness.InvalidSeedError,
                "104733");
        }

        [Test]
        public void PayloadLoggersDoNotStampVictory()
        {
            string described = SkirmishS004AriaRunHarness.DescribeLaunch(
                SkirmishS004FirstVisit.SeedA,
                GameLocalization.EnglishLocaleCode);
            Assert.IsTrue(described.IndexOf("catalog=S004", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("seed=104733", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(SkirmishS004AriaRunHarness.EvidenceDirectory().Replace('\\', '/').EndsWith(
                "S004/_Evidence",
                StringComparison.Ordinal));

            string persian = SkirmishS004AriaRunHarness.DescribeLaunch(
                SkirmishS004FirstVisit.SeedA,
                GameLocalization.PersianLocaleCode);
            Assert.IsTrue(persian.IndexOf("locale=fa-IR", StringComparison.Ordinal) >= 0);

            Assert.IsTrue(SkirmishAriaAcceptancePayload.TryCreateFirstVisitS004(
                SkirmishS004FirstVisit.SeedA,
                GameLocalization.EnglishLocaleCode,
                out SkirmishAriaAcceptancePayload payload,
                out string error),
                error);
            string play = AriaPlayEditorValidation.AcceptExpandedPayload(in payload);
            string watch = SkirmishExpandedAriaWatchValidation.AcceptExpandedPayload(in payload);
            Assert.IsTrue(play.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(watch.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(play.IndexOf("seed=104733", StringComparison.Ordinal) >= 0);

            InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(() =>
                SkirmishS004AriaRunHarness.DescribeLaunch(
                    SkirmishAcceptanceCensusCapture.FirstVisitSeed,
                    GameLocalization.EnglishLocaleCode));
            Assert.IsTrue(rejected.Message.IndexOf("104733", StringComparison.Ordinal) >= 0);
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
                SkirmishAcceptanceCensusCapture.S004CatalogId);
        }

        [Test]
        public void FirstVisitSeedCompilesWithoutStampingVictory()
        {
            SkirmishExpandedAriaHarnessChecks.AssertFirstVisitSeedCompiles(
                SkirmishAcceptanceCensusCapture.S004CatalogId,
                SkirmishS004FirstVisit.SeedA);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishS004AriaHarnessTests();
                suite.CheckedInRunsCsvStaysHeaderOnly();
                suite.ForcedVictoryDoesNotAppend();
                suite.UnfinishedOutcomeIsRefusedUntilTheMatchEnds();
                suite.FinishedOutcomesAppendAndOnlyUnguidedVictoryCounts();
                suite.S002SeedIsNotAnS004Slot();
                suite.PayloadLoggersDoNotStampVictory();
                suite.SimulationStallFailsFastAfterGrace();
                suite.ExpandedObjectiveClockProjectsOntoMatchElapsed();
                suite.FirstVisitSeedCompilesWithoutStampingVictory();
                Debug.Log("[SkirmishS004AriaHarnessTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishS004AriaHarnessTests] result=Failed\n" + exception);
                throw;
            }
        }
    }
}
