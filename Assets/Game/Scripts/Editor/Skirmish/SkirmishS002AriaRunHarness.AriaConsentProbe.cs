#if UNITY_EDITOR
using System;
using Game.UI.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static AriaTouchInputUiSystemHelper ariaConsentTouch;
        private static double ariaConsentDeadline, ariaConsentDue;
        private static bool ariaStopVerified;

        private static bool StartS004AriaThroughPresentedControls()
        {
            if (UiShellRuntimeGateway.ReadAriaPlay().Active)
            {
                if (Environment.GetEnvironmentVariable("WARLINE_S004_STOP_PROBE") == "1" &&
                    (!ariaStopVerified || ariaConsentTouch == null || ariaConsentTouch.CompletedGestures < 5)) return false;
                if (ariaConsentTouch == null || ariaConsentTouch.CompletedGestures < 2)
                    throw new InvalidOperationException("ARIA started without the visible Touch consent flow.");
                Debug.Log("[S004AriaConsent] result=Passed input=Touch controls=Play,Confirm completed=" +
                    ariaConsentTouch.CompletedGestures + " interventions=" + ariaConsentTouch.PhysicalInterventions +
                    " unexpected=" + ariaConsentTouch.UnexpectedSamples);
                StopS004AriaConsentProbe();
                return true;
            }
            if (ariaConsentTouch == null)
            {
                ariaConsentTouch = new AriaTouchInputUiSystemHelper();
                ariaStopVerified = false;
                if (!ariaConsentTouch.Start()) throw new InvalidOperationException("ARIA consent Touch unavailable.");
                ariaConsentDeadline = UnityEditor.EditorApplication.timeSinceStartup + 60d;
                var runner = new GameObject("S004AriaConsentControlsProbe");
                UnityEngine.Object.DontDestroyOnLoad(runner);
                runner.AddComponent<SkirmishS004AriaConsentInputFrame>();
            }
            if (UnityEditor.EditorApplication.timeSinceStartup > ariaConsentDeadline)
                throw new InvalidOperationException("Visible ARIA consent flow timed out.");
            return false;
        }

        internal static void TickS004AriaConsentPlayerFrame()
        {
            if (ariaConsentTouch == null) return;
            ariaConsentTouch.Tick(Time.unscaledTime);
            bool stopProbe = Environment.GetEnvironmentVariable("WARLINE_S004_STOP_PROBE") == "1";
            bool active = UiShellRuntimeGateway.ReadAriaPlay().Active;
            if (ariaConsentTouch.IsBusy || (active && (!stopProbe || ariaConsentTouch.CompletedGestures != 2)) ||
                UnityEditor.EditorApplication.timeSinceStartup < ariaConsentDue) return;
            if (ariaConsentTouch.PlayerInterrupted || ariaConsentTouch.UnexpectedSamples != 0)
                return;
            if (stopProbe && ariaConsentTouch.CompletedGestures == 3 && !ariaStopVerified)
            {
                if (active) return;
                ariaStopVerified = true;
                Debug.Log("[S004AriaStop] result=Passed input=Touch active=0 restart=Pending");
            }
            uint count = ariaConsentTouch.CompletedGestures;
            string name = count == 0 || count == 2 || count == 3 ? "WatchAriaPlay" : "ConfirmWatchAria";
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (button.name == name && TryPresentedButtonPoint(button, out var point))
                {
                    if (ariaConsentTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                        ariaConsentDue = UnityEditor.EditorApplication.timeSinceStartup + .75d;
                    return;
                }
        }

        private static void StopS004AriaConsentProbe()
        {
            ariaConsentTouch?.Dispose();
            ariaConsentTouch = null;
        }
    }

    public sealed class SkirmishS004AriaConsentInputFrame : MonoBehaviour
    {
        private void Update() => SkirmishS002AriaRunHarness.TickS004AriaConsentPlayerFrame();
    }
}
#endif
