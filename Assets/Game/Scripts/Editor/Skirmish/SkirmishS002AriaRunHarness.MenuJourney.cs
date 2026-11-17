#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generic menu journey (gate 1: the scenario loads from the Skirmish menu). WARLINE_MENU_JOURNEY=1 sends ANY catalog
    /// through the S004 presented-controls journey (real touches: Skirmish card, the library's ScenarioCard_&lt;id&gt;,
    /// the setup's launch button) instead of TryQueueExpanded, and the harness then waits for the natively started
    /// match without nudging it (no programmatic enter/play/arm). Logs [SkirmishMenuJourney] catalog=&lt;id&gt; result=Passed|Failed.
    /// WARLINE_S004_MENU_JOURNEY=1 (the S004/S005 editor journeys) keeps its behaviour and also gets the new log line.
    /// A catalog that has no visible, launchable card (absent or still Planned) fails with that reason; nothing is published.
    /// </summary>
    public static partial class SkirmishS002AriaRunHarness
    {
        private static readonly List<string> menuPath = new();
        private static bool menuJourneyFailed, menuScenarioCardSeen, menuScrollLogged, menuPreflightDone;

        private static bool GenericMenuJourney => Environment.GetEnvironmentVariable("WARLINE_MENU_JOURNEY") == "1";

        private static bool AnyMenuJourney => GenericMenuJourney ||
            Environment.GetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY") == "1";

        // Called per job: a queue reuses the Editor and the previous job left menuStep at 6.
        private static void ResetMenuJourneyState()
        {
            StopS004MenuProbe();
            menuStep = 0;
            menuPath.Clear();
            menuJourneyFailed = menuScenarioCardSeen = menuScrollLogged = menuPreflightDone = false;
        }

        private static void FailMenuJourney(int step, string reason, string finishReason)
        {
            if (menuJourneyFailed) return;
            menuJourneyFailed = true;
            Debug.LogError(SkirmishMenuJourneyLog.FormatFailed(catalogId, step, reason));
            StopS004MenuProbe();
            Finish(true, finishReason ?? "menuJourney:" + reason);
        }

        // Null catalog entries means the library lists nothing for this catalog: stop before touching any control.
        private static bool MenuJourneyPreflightPasses()
        {
            if (!GenericMenuJourney || menuPreflightDone) return !menuJourneyFailed;
            menuPreflightDone = true;
            EnsureValidationLibrary("menuPreflight");
            var catalog = SkirmishBattleCatalogConfig.Load();
            string reason = SkirmishMenuJourneyLog.Preflight(catalog != null ? catalog.Entries : null, catalogId);
            if (reason == null) return true;
            FailMenuJourney(0, reason, null);
            return false;
        }

        private static bool TryReadMenuLaunchedSetup(EntityManager em, out string actualCatalog, out int actualSeed,
            out string size, out string difficulty)
        {
            actualCatalog = string.Empty; actualSeed = 0; size = difficulty = "unreadable";
            using var query = em.CreateEntityQuery(typeof(SkirmishResolvedSetupRecord));
            if (query.CalculateEntityCount() != 1) return false;
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(query.GetSingletonEntity()).Setup;
            if (setup == null) return false;
            actualCatalog = setup.CatalogId;
            actualSeed = setup.Seed;
            size = setup.SizeId.ToString();
            difficulty = setup.DifficultyId.ToString();
            return true;
        }

        /// <summary>True when the menu journey may log Passed. Generic journeys must also have launched Standard/Regular of the requested seed.</summary>
        private static bool CompleteMenuJourney(EntityManager em)
        {
            bool read = TryReadMenuLaunchedSetup(em, out string actualCatalog, out int actualSeed, out string size, out string difficulty);
            if (GenericMenuJourney)
            {
                string mismatch = read
                    ? SkirmishMenuJourneyLog.VerifySetup(catalogId, seed, actualCatalog, actualSeed, size, difficulty)
                    : "setupMismatch:resolvedSetupMissing";
                if (mismatch != null)
                {
                    FailMenuJourney(5, mismatch, "menuSetupMismatch");
                    return false;
                }
            }
            Debug.Log(SkirmishMenuJourneyLog.FormatPassed(catalogId, menuPath, read ? actualSeed : seed, size, difficulty,
                SkirmishValidationLibrary.IsCandidate(catalogId)));
            return true;
        }

        // WARLINE_VALIDATION_LIBRARY=1: present Planned authored missions in an Editor-memory library so the menu path can
        // launch them before publication. Idempotent; re-run after every domain reload because the scope is static.
        private static void EnsureValidationLibrary(string trigger)
        {
            if (!SkirmishValidationLibrary.Requested || SkirmishValidationLibrary.Active) return;
            if (candidateCatalogScope != null)
            {
                Debug.LogWarning(SkirmishValidationLibraryPlan.FormatDisabled("editorJourneyCatalogScopeActive"));
                return;
            }
            // A single-mission harness library (EnsureHarnessGenericLibrary) would hold the one Editor catalog slot.
            DisposeHarnessGenericLibrary();
            SkirmishValidationLibrary.Enable(trigger);
        }
    }
}
#endif
