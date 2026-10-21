using System;
using System.Collections.Generic;

namespace Game.Configs
{
    // Exact definition lookup only. This grants no source-fit, launch or readiness credit.
    public static class SkirmishDefinitionVersionResolver
    {
        public static bool IsSupported(string catalogId, int version) =>
            SkirmishMissionDispatch.Canonical(catalogId) && version > 0 &&
            (catalogId != "S002" || version == 2 || version == 3);

        public static bool TryResolve(IReadOnlyList<SkirmishScenarioDefinitionConfig> current,
            IReadOnlyList<SkirmishScenarioDefinitionConfig> historical, string catalogId, int version,
            out SkirmishScenarioDefinitionConfig definition)
        {
            definition = null;
            if (!IsSupported(catalogId, version)) return false;
            int matches = 0;
            foreach (var list in new[] { current, historical })
            {
                if (list == null) continue;
                foreach (var candidate in list)
                {
                    if (candidate == null || candidate.CatalogId != catalogId || candidate.ContentVersion != version) continue;
                    if (candidate.DefinitionId != "skirmish." + catalogId.ToLowerInvariant() ||
                        string.IsNullOrWhiteSpace(candidate.ContentHash)) { definition = null; return false; }
                    definition = candidate; matches++;
                }
            }
            if (matches == 1) return true;
            definition = null;
            return false;
        }
    }
}
