#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Configs;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Editor-memory validation library (WARLINE_VALIDATION_LIBRARY=1). While active, the Skirmish library lists every
    /// authored mission that is still Planned in the shipped catalog as a launchable card badged "[VALIDATION]", so the
    /// real menu path (Skirmish card, library card, setup, launch) can be exercised before acceptance and publication.
    /// It only installs SkirmishBattleCatalogConfig.UseEditorValidationCatalog with an in-memory copy of the shipped
    /// rows. It never writes the shipped catalog, never publishes, grants no readiness, and cannot exist in a player
    /// build (this file is UNITY_EDITOR only and the environment switch is read only by the Editor harness).
    /// </summary>
    public static class SkirmishValidationLibrary
    {
        private static IDisposable libraryScope;
        private static SkirmishBattleCatalogConfig candidate;
        private static readonly HashSet<string> plannedIds = new(StringComparer.Ordinal);

        public static bool Requested =>
            Environment.GetEnvironmentVariable(SkirmishValidationLibraryPlan.EnvironmentVariable) == "1";

        public static bool Active => libraryScope != null;

        /// <summary>True for a card that is visible only for validation: presented by this scope, or a real-catalog row carrying the marker.</summary>
        public static bool IsCandidate(string catalogId)
        {
            if (catalogId == null) return false;
            if (libraryScope != null && plannedIds.Contains(catalogId)) return true;
            var library = SkirmishBattleCatalogConfig.Load();
            return library != null && library.TryGet(catalogId, out var row) && row.IsValidationVisible;
        }

        /// <summary>Idempotent. False when the library could not be installed (the reason is logged, never thrown).</summary>
        public static bool Enable(string trigger)
        {
            if (libraryScope != null) return true;
            var shipped = Resources.Load<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogConfig.ResourceName);
            var authored = SkirmishExpansionCatalogConfig.Load();
            if (shipped == null || authored == null)
            {
                Debug.LogError(SkirmishValidationLibraryPlan.FormatDisabled("catalogMissing"));
                return false;
            }
            var sources = new List<SkirmishValidationLibraryPlan.AuthoredSource>();
            if (authored.Definitions != null)
                foreach (var definition in authored.Definitions)
                    if (definition != null)
                        sources.Add(new SkirmishValidationLibraryPlan.AuthoredSource(definition.CatalogId, definition.DefinitionId,
                            definition.ContentVersion, definition.CampaignMap != null, definition.MapLayout != null));
            var plan = SkirmishValidationLibraryPlan.Build(shipped.Entries, sources);

            var created = ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();
            created.hideFlags = HideFlags.HideAndDontSave;
            created.Configure(plan.Rows);
            try { libraryScope = SkirmishBattleCatalogConfig.UseEditorValidationCatalog(created); }
            catch (InvalidOperationException)
            {
                UnityEngine.Object.DestroyImmediate(created);
                Debug.LogError(SkirmishValidationLibraryPlan.FormatDisabled("anotherEditorValidationCatalogActive"));
                return false;
            }
            candidate = created;
            plannedIds.Clear();
            foreach (string id in plan.Planned) plannedIds.Add(id);
            Debug.Log(SkirmishValidationLibraryPlan.FormatEnabled(plan, trigger));
            return true;
        }

        public static void Disable()
        {
            libraryScope?.Dispose();
            libraryScope = null;
            if (candidate != null) UnityEngine.Object.DestroyImmediate(candidate);
            candidate = null;
            plannedIds.Clear();
        }
    }
}
#endif
