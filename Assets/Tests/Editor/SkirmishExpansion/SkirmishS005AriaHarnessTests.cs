using System;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishS005AriaHarnessTests
    {
        [Test]
        public void CheckedInRunsCsvStaysHeaderOnly()
        {
            SkirmishExpandedAriaHarnessChecks.AssertCheckedInRunsCsvStaysHeaderOnly(
                SkirmishAcceptanceScaffold.S005RelativeRunsPath);
        }

        [Test]
        public void ForcedVictoryDoesNotAppend()
        {
            SkirmishExpandedAriaHarnessChecks.AssertForcedVictoryDoesNotAppend(
                SkirmishAcceptanceCensusCapture.S005CatalogId,
                SkirmishS005FirstVisit.SeedA,
                "S005-aria-rs-104734-en-1",
                SkirmishAcceptanceScaffold.S005RelativeReportEvidenceDirectory);
        }

        [Test]
        public void UnfinishedOutcomeIsRefusedUntilTheMatchEnds()
        {
            SkirmishExpandedAriaHarnessChecks.AssertUnfinishedOutcomeIsRefused(
                SkirmishAcceptanceCensusCapture.S005CatalogId,
                SkirmishS005FirstVisit.SeedA,
                "S005-aria-rs-104734-en-1",
                SkirmishAcceptanceScaffold.S005RelativeReportEvidenceDirectory);
        }

        [Test]
        public void FinishedOutcomesAppendAndOnlyUnguidedVictoryCounts()
        {
            SkirmishExpandedAriaHarnessChecks.AssertFinishedOutcomesAppendAndOnlyUnguidedVictoryCounts(
                SkirmishAcceptanceCensusCapture.S005CatalogId,
                SkirmishS005FirstVisit.SeedA,
                SkirmishS005FirstVisit.SeedB,
                SkirmishAcceptanceScaffold.S005RelativeReportEvidenceDirectory,
                SkirmishS005AriaRunHarness.InvalidSeedError);
        }

        [Test]
        public void S002SeedIsNotAnS005Slot()
        {
            SkirmishExpandedAriaHarnessChecks.AssertForeignSeedIsRefused(
                SkirmishAcceptanceCensusCapture.S005CatalogId,
                SkirmishAcceptanceCensusCapture.FirstVisitSeed,
                SkirmishS005AriaRunHarness.InvalidSeedError,
                "104734");
        }

        [Test]
        public void PayloadLoggersDoNotStampVictory()
        {
            string described = SkirmishS005AriaRunHarness.DescribeLaunch(
                SkirmishS005FirstVisit.SeedA,
                GameLocalization.EnglishLocaleCode);
            Assert.IsTrue(described.IndexOf("catalog=S005", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("seed=104734", StringComparison.Ordinal) >= 0);
            Assert.IsTrue(described.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(SkirmishS005AriaRunHarness.EvidenceDirectory().Replace('\\', '/').EndsWith(
                "S005/_Evidence",
                StringComparison.Ordinal));

            string persian = SkirmishS005AriaRunHarness.DescribeLaunch(
                SkirmishS005FirstVisit.SeedA,
                GameLocalization.PersianLocaleCode);
            Assert.IsTrue(persian.IndexOf("locale=fa-IR", StringComparison.Ordinal) >= 0);

            Assert.IsTrue(SkirmishAriaAcceptancePayload.TryCreateFirstVisitS005(
                SkirmishS005FirstVisit.SeedA,
                GameLocalization.EnglishLocaleCode,
                out SkirmishAriaAcceptancePayload payload,
                out string error),
                error);
            string play = AriaPlayEditorValidation.AcceptExpandedPayload(in payload);
            string watch = SkirmishExpandedAriaWatchValidation.AcceptExpandedPayload(in payload);
            Assert.IsTrue(play.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(watch.IndexOf("Victory", StringComparison.OrdinalIgnoreCase) < 0);
            Assert.IsTrue(play.IndexOf("seed=104734", StringComparison.Ordinal) >= 0);

            InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(() =>
                SkirmishS005AriaRunHarness.DescribeLaunch(
                    SkirmishAcceptanceCensusCapture.FirstVisitSeed,
                    GameLocalization.EnglishLocaleCode));
            Assert.IsTrue(rejected.Message.IndexOf("104734", StringComparison.Ordinal) >= 0);
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
                SkirmishAcceptanceCensusCapture.S005CatalogId);
        }

        [Test]
        public void FirstVisitSeedCompilesWithoutStampingVictory()
        {
            SkirmishExpandedAriaHarnessChecks.AssertFirstVisitSeedCompiles(
                SkirmishAcceptanceCensusCapture.S005CatalogId,
                SkirmishS005FirstVisit.SeedA);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishS005AriaHarnessTests();
                suite.CheckedInRunsCsvStaysHeaderOnly();
                suite.ForcedVictoryDoesNotAppend();
                suite.UnfinishedOutcomeIsRefusedUntilTheMatchEnds();
                suite.FinishedOutcomesAppendAndOnlyUnguidedVictoryCounts();
                suite.S002SeedIsNotAnS005Slot();
                suite.PayloadLoggersDoNotStampVictory();
                suite.SimulationStallFailsFastAfterGrace();
                suite.ExpandedObjectiveClockProjectsOntoMatchElapsed();
                suite.FirstVisitSeedCompilesWithoutStampingVictory();
                Debug.Log("[SkirmishS005AriaHarnessTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishS005AriaHarnessTests] result=Failed\n" + exception);
                throw;
            }
        }
    }
}
