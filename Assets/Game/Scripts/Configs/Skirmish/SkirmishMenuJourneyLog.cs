using System.Collections.Generic;

namespace Game.Configs
{
    /// <summary>
    /// Pure rules and log text for the generic Skirmish menu journey (gate 1: scenario assets load from the Skirmish menu).
    /// Nothing here touches Unity UI; the Editor harness feeds it what it observed.
    /// </summary>
    public static class SkirmishMenuJourneyLog
    {
        public const string Marker = "[SkirmishMenuJourney]";

        /// <summary>
        /// Null when the catalog has a visible, launchable card. Otherwise the reason the journey cannot start from the menu:
        /// no row at all, or a row that is still Planned (the library lists it, but its launch button is disabled).
        /// </summary>
        public static string Preflight(IReadOnlyList<SkirmishBattleCatalogEntry> entries, string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId)) return "noCatalogId";
            if (entries != null)
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry.ScenarioId != catalogId) continue;
                    if (entry.IsPlayable) return null;
                    return "catalogEntryNotPlayable:status=" + entry.Status + ",playableScenarioIndex=" + entry.PlayableScenarioIndex;
                }
            return "catalogEntryAbsentFromLibrary";
        }

        /// <summary>Why a step never finished, by the step the journey was waiting in (0 menu, 1 library, 4 setup, 5 launch).</summary>
        public static string TimeoutReason(int step, bool scenarioCardEverPresented) => step switch
        {
            0 => "mainMenuSkirmishCardNotPresented",
            1 => scenarioCardEverPresented ? "scenarioCardNotTappable" : "scenarioCardNotPresentedInLibrary",
            4 => "setupOrLaunchButtonNotReached",
            5 => "matchDidNotStartAfterLaunch",
            _ => "controlsTimeout"
        };

        /// <summary>Null when the match the menu launched is the requested catalog and seed at Standard size and Regular difficulty.</summary>
        public static string VerifySetup(string requestedCatalog, int requestedSeed,
            string actualCatalog, int actualSeed, string actualSize, string actualDifficulty)
        {
            var mismatches = new List<string>();
            if (actualCatalog != requestedCatalog) mismatches.Add("catalog=" + actualCatalog);
            if (actualSeed != requestedSeed) mismatches.Add("seed=" + actualSeed);
            if (actualSize != "Standard") mismatches.Add("size=" + actualSize);
            if (actualDifficulty != "Regular") mismatches.Add("difficulty=" + actualDifficulty);
            return mismatches.Count == 0 ? null : "setupMismatch:" + string.Join(",", mismatches);
        }

        /// <summary>validationCandidate marks a card that only the Editor validation library presented (still Planned in the shipped catalog).</summary>
        public static string FormatPassed(string catalogId, IReadOnlyList<string> pathControls, int seed, string size, string difficulty,
            bool validationCandidate = false) =>
            Marker + " catalog=" + catalogId + " result=Passed path=" + string.Join(">", pathControls) +
            " seed=" + seed + " size=" + size + " difficulty=" + difficulty + " input=Touch matchStartedFromMenu=1" +
            (validationCandidate ? " validationCandidate=1 published=0" : "");

        public static string FormatFailed(string catalogId, int step, string reason) =>
            Marker + " catalog=" + catalogId + " result=Failed step=" + step + " reason=" + reason;
    }
}
