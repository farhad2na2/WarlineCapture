using System;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Remembers the scenario a finished match should reopen, then applies it
    /// once the library view model exists again.
    /// </summary>
    public static class SkirmishLibraryReturn
    {
        public static string PendingScenarioId { get; private set; } = string.Empty;

        public static void Queue(string scenarioId) =>
            PendingScenarioId = scenarioId ?? string.Empty;

        public static bool Apply(ISkirmishLibraryViewModel library)
        {
            if (library == null || string.IsNullOrEmpty(PendingScenarioId))
                return false;

            string scenarioId = PendingScenarioId;
            library.RestoreFromPrefs();
            if (!library.SelectScenarioId(scenarioId))
                return false;
            if (library.Level != SkirmishLibraryLevel.Battlefield)
                return false;
            if (!string.Equals(library.Briefing.ScenarioId, scenarioId, StringComparison.OrdinalIgnoreCase))
                return false;

            PendingScenarioId = string.Empty;
            return true;
        }
    }
}
