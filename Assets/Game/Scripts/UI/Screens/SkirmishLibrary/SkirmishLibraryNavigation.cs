using System;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Theater, then one battlefield, then one card. Does not launch.
    /// </summary>
    public static class SkirmishLibraryNavigation
    {
        public static bool OpenTheaterBattlefieldCard(
            ISkirmishLibraryViewModel library,
            string mapId,
            string scenarioId)
        {
            if (library == null || string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(scenarioId))
                return false;
            if (!library.SelectMapCollection(string.Empty) || library.Level != SkirmishLibraryLevel.Theater)
                return false;
            if (!library.SelectMapCollection(mapId) || library.Level != SkirmishLibraryLevel.Battlefield)
                return false;
            if (!string.Equals(library.Battlefield.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!library.SelectScenarioId(scenarioId))
                return false;
            return library.Level == SkirmishLibraryLevel.Battlefield &&
                   string.Equals(library.Battlefield.MapId, mapId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(library.Briefing.ScenarioId, scenarioId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
