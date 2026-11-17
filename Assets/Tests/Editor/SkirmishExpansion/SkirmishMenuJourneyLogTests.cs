#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Configs;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>Pure tests for the generic menu journey's preflight, setup check and log text.</summary>
    public sealed class SkirmishMenuJourneyLogTests
    {
        private static SkirmishBattleCatalogEntry Entry(string id, SkirmishBattleCatalogStatus status, int index) =>
            new() { ScenarioId = id, Status = status, PlayableScenarioIndex = index };

        [Test]
        public void PlayableEntryHasAVisibleLaunchableCard()
        {
            var entries = new List<SkirmishBattleCatalogEntry> { Entry("S001", SkirmishBattleCatalogStatus.Planned, -1), Entry("S002", SkirmishBattleCatalogStatus.Playable, 3) };
            Assert.IsNull(SkirmishMenuJourneyLog.Preflight(entries, "S002"));
        }

        [Test]
        public void PlannedEntryFailsWithTheReasonAndIsNeverPublished()
        {
            var entries = new List<SkirmishBattleCatalogEntry> { Entry("S026", SkirmishBattleCatalogStatus.Planned, -1) };
            Assert.AreEqual("catalogEntryNotPlayable:status=Planned,playableScenarioIndex=-1", SkirmishMenuJourneyLog.Preflight(entries, "S026"));
            // Playable status without a runtime index is still not launchable.
            entries[0] = Entry("S026", SkirmishBattleCatalogStatus.Playable, -1);
            StringAssert.StartsWith("catalogEntryNotPlayable", SkirmishMenuJourneyLog.Preflight(entries, "S026"));
        }

        [Test]
        public void MissingRowOrNoCatalogFails()
        {
            Assert.AreEqual("catalogEntryAbsentFromLibrary", SkirmishMenuJourneyLog.Preflight(new List<SkirmishBattleCatalogEntry>(), "S050"));
            Assert.AreEqual("catalogEntryAbsentFromLibrary", SkirmishMenuJourneyLog.Preflight(null, "S050"));
            Assert.AreEqual("noCatalogId", SkirmishMenuJourneyLog.Preflight(null, ""));
        }

        [Test]
        public void TimeoutReasonNamesTheStepTheJourneyWasWaitingIn()
        {
            Assert.AreEqual("mainMenuSkirmishCardNotPresented", SkirmishMenuJourneyLog.TimeoutReason(0, false));
            Assert.AreEqual("scenarioCardNotPresentedInLibrary", SkirmishMenuJourneyLog.TimeoutReason(1, false));
            Assert.AreEqual("scenarioCardNotTappable", SkirmishMenuJourneyLog.TimeoutReason(1, true));
            Assert.AreEqual("setupOrLaunchButtonNotReached", SkirmishMenuJourneyLog.TimeoutReason(4, true));
            Assert.AreEqual("matchDidNotStartAfterLaunch", SkirmishMenuJourneyLog.TimeoutReason(5, true));
            Assert.AreEqual("controlsTimeout", SkirmishMenuJourneyLog.TimeoutReason(9, true));
        }

        [Test]
        public void SetupMustBeTheRequestedCatalogSeedAtStandardRegular()
        {
            Assert.IsNull(SkirmishMenuJourneyLog.VerifySetup("S002", 130365, "S002", 130365, "Standard", "Regular"));
            Assert.AreEqual("setupMismatch:size=War,difficulty=Hard",
                SkirmishMenuJourneyLog.VerifySetup("S002", 130365, "S002", 130365, "War", "Hard"));
            Assert.AreEqual("setupMismatch:catalog=S004,seed=1",
                SkirmishMenuJourneyLog.VerifySetup("S002", 130365, "S004", 1, "Standard", "Regular"));
        }

        [Test]
        public void LogLinesCarryCatalogPathAndReason()
        {
            Assert.AreEqual("[SkirmishMenuJourney] catalog=S002 result=Passed path=Card_Skirmish>BattleLibrary(scroll)>ScenarioCard_S002>LaunchMissionButton seed=130365 size=Standard difficulty=Regular input=Touch matchStartedFromMenu=1",
                SkirmishMenuJourneyLog.FormatPassed("S002", new[] { "Card_Skirmish", "BattleLibrary(scroll)", "ScenarioCard_S002", "LaunchMissionButton" }, 130365, "Standard", "Regular"));
            Assert.AreEqual("[SkirmishMenuJourney] catalog=S026 result=Failed step=0 reason=catalogEntryAbsentFromLibrary",
                SkirmishMenuJourneyLog.FormatFailed("S026", 0, "catalogEntryAbsentFromLibrary"));
        }
    }
}
#endif
