#if UNITY_EDITOR
using System;
using Game.Configs;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        // Focused Editor controls of the real locale seam, not a mission/queue acceptance run.
        public static void RunEffectiveLocaleRestorationControls()
        {
            if (productionCanary != null || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Locale controls require an idle Editor with no owned production context");

            string originalEffectiveLocale = GameLocalization.CurrentLocaleCode;
            Action redirectOnce = null;
            try
            {
                GameLocalization.SetLocale(GameLocalization.EnglishLocaleCode, false);
                string baseline = GameLocalization.CurrentLocaleCode;
                RequireLocaleControl(baseline == GameLocalization.EnglishLocaleCode, "Actual English baseline required");
                GameLocalization.SetLocale(GameLocalization.PersianLocaleCode, false);
                RequireLocaleControl(GameLocalization.CurrentLocaleCode == GameLocalization.PersianLocaleCode,
                    "Actual Persian transition required; fallback is not a positive control");
                var positiveEvents = new JArray();
                string restoredLocale = RestoreProductionEffectiveLocale(baseline, GameLocalization.PersianLocaleCode,
                    (kind, facts) => positiveEvents.Add(new JObject { ["kind"] = kind, ["facts"] = facts.DeepClone() }));
                RequireLocaleControl(restoredLocale == baseline && GameLocalization.CurrentLocaleCode == baseline,
                    "Positive control did not restore the actual effective getter");
                RequireLocaleRestoreFact(positiveEvents, baseline, baseline, true);
                Debug.Log("[SkirmishLocaleRestorePositiveControl] result=Passed actual=" + restoredLocale + " missionCredit=0");

                GameLocalization.SetLocale(GameLocalization.PersianLocaleCode, false);
                RequireLocaleControl(GameLocalization.CurrentLocaleCode == GameLocalization.PersianLocaleCode,
                    "Actual Persian mismatch precondition required");
                int redirectCalls = 0;
                redirectOnce = () =>
                {
                    GameLocalization.LocaleChanged -= redirectOnce;
                    redirectCalls++;
                    GameLocalization.SetLocale(GameLocalization.PersianLocaleCode, false);
                };
                GameLocalization.LocaleChanged += redirectOnce;
                var mismatchEvents = new JArray();
                int successContinuations = 0;
                bool mismatchRejected = false;
                try
                {
                    RestoreProductionEffectiveLocale(baseline, GameLocalization.PersianLocaleCode,
                        (kind, facts) => mismatchEvents.Add(new JObject { ["kind"] = kind, ["facts"] = facts.DeepClone() }));
                    successContinuations++;
                }
                catch (InvalidOperationException error) when
                    (error.Message == "Actual effective locale restore differs from captured baseline")
                {
                    mismatchRejected = true;
                }
                RequireLocaleControl(mismatchRejected && redirectCalls == 1 && successContinuations == 0 &&
                    GameLocalization.CurrentLocaleCode == GameLocalization.PersianLocaleCode,
                    "One-shot subscriber must produce a measured getter mismatch before success continuation");
                RequireLocaleRestoreFact(mismatchEvents, baseline, GameLocalization.PersianLocaleCode, false);
                Debug.Log("[SkirmishLocaleRestoreMismatchControl] result=Passed redirects=" + redirectCalls +
                    " successContinuations=" + successContinuations + " missionCredit=0");
            }
            finally
            {
                if (redirectOnce != null) GameLocalization.LocaleChanged -= redirectOnce;
                if (!string.Equals(GameLocalization.CurrentLocaleCode, originalEffectiveLocale, StringComparison.OrdinalIgnoreCase))
                    GameLocalization.SetLocale(originalEffectiveLocale, false);
                RequireLocaleControl(string.Equals(GameLocalization.CurrentLocaleCode, originalEffectiveLocale, StringComparison.OrdinalIgnoreCase),
                    "Locale control finally failed to restore the caller's effective locale");
            }
            Debug.Log("[SkirmishEffectiveLocaleRestorationControls] result=Passed positive=1 mismatch=1 callerLocaleRestored=1 missionCredit=0");
        }

        private static void RequireLocaleRestoreFact(JArray events, string baseline, string actual, bool matches)
        {
            RequireLocaleControl(events.Count == 1, "Exactly one real restoration-check fact required");
            var row = (JObject)events[0];
            var facts = (JObject)row["facts"];
            RequireLocaleControl((string)row["kind"] == "EffectiveLocaleRestoreChecked" &&
                (string)facts["baseline_effective_locale"] == baseline && (string)facts["expected_effective_locale"] == baseline &&
                (string)facts["actual_effective_locale"] == actual && (string)facts["requested_mission_locale"] == GameLocalization.PersianLocaleCode &&
                (bool)facts["matches"] == matches, "Restoration fact must report independently measured locale and comparison");
        }

        private static void RequireLocaleControl(bool value, string error)
        {
            if (!value) throw new InvalidOperationException(error);
        }
    }
}
#endif
