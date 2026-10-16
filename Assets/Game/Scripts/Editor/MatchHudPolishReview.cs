#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Fresh native English/Persian HUD evidence, separate from historic captures.</summary>
    public static class MatchHudPolishReview
    {
        public static void RunBefore() => Run("Before");
        public static void RunAfter() => Run("After");

        private static async void Run(string phase)
        {
            bool passed = false;
            try
            {
                string output = "Design/AgentReports/MatchHudPolish20261016/" + phase;
                int status = await HudButtonIconNativeValidation.RunPolishReview(output);
                passed = status == 0;
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                Debug.Log("[MatchHudPolishReview] result=" + (passed ? "Passed" : "Failed") +
                    " phase=" + phase + " locales=en,fa-IR widths=1280,1920,2400");
                MissionEditorValidationExit.Complete(passed);
            }
        }
    }
}
#endif
