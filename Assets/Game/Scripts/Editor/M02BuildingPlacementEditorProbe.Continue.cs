using System;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M02BuildingPlacementEditorProbe
    {
        private const string ContinueMode = "Warline.M02.ContinueProbe";
        private static bool continueClicked, waitObserved, pointerCaptured;
        private static Vector3 pointerStart;
        private static float pointerSampleStart, pointerTravel;
        private static int pointerSamples;
        private static string continueLocale;
        public static void RunContinueEnglish() => StartContinue("en");
        public static void RunContinuePersian() => StartContinue("fa-IR");
        private static void StartContinue(string locale)
        {
            continueLocale = locale;
            GameLocalization.SetLocale(locale, false);
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            SessionState.SetBool(ContinueMode, true);
            SessionState.SetBool(RifleSingleClickMode, false);
            continueClicked = waitObserved = pointerCaptured = false;
            Run();
        }

        private static void AdvanceContinue(EntityManager em, Entity root, CampaignMissionAttemptFactsComponent facts)
        {
            if (!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel)) return;
            var aria = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if (aria == null) return;
            if (!continueClicked)
            {
                if (panel.TutorialStep != 5 || !aria.ContinueButton.IsActive()) return;
                Log("Continue click: building already complete=" + (facts.RequiredBuildingCompletedCount != 0));
                if (!IsTouchable(aria.ContinueButton)) return;
                ScreenCapture.CaptureScreenshot(Output + "/continue-before-" + GameLocalization.CurrentLocaleCode + ".png");
                var indicator = GameObject.Find("AriaAssistantTargetIndicatorRuntime");
                var arrow = indicator != null ? indicator.transform.Find("AttentionTapPointer") : null;
                if (arrow == null || !arrow.gameObject.activeInHierarchy) throw new InvalidOperationException("Plan B arrow is missing beside Continue.");
                var frame = (RectTransform)indicator.transform;
                var button = (RectTransform)aria.ContinueButton.transform;
                var corners = new Vector3[4]; frame.GetWorldCorners(corners);
                var buttonCorners = new Vector3[4]; button.GetWorldCorners(buttonCorners);
                float scale = frame.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
                if (Vector3.Distance((corners[0]+corners[2])*.5f,(buttonCorners[0]+buttonCorners[2])*.5f)>2*scale ||
                    corners[0].x>buttonCorners[0].x || corners[0].y>buttonCorners[0].y ||
                    corners[2].x<buttonCorners[2].x || corners[2].y<buttonCorners[2].y)
                    throw new InvalidOperationException("Tutorial frame does not surround the final Continue button bounds.");
                var caption = frame.Find("TopBorderCaption");
                if (caption == null || !caption.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Tutorial caption flickered off during a stable lesson.");
                if(frame.GetComponentInChildren<TutorialFocusFrameGraphic>()==null)
                    throw new InvalidOperationException("Double edge frame missing.");
                if (!pointerCaptured)
                {
                    pointerStart = arrow.position; pointerSampleStart=Time.unscaledTime; pointerTravel=0; pointerSamples=0;
                    pointerCaptured = true;
                    ScreenCapture.CaptureScreenshot(Output + "/planb-pointer-a-" + GameLocalization.CurrentLocaleCode + ".png");
                }
                pointerSamples++;
                pointerTravel=Mathf.Max(pointerTravel,Vector3.Distance(pointerStart,arrow.position));
                if(Time.unscaledTime-pointerSampleStart<3f) {nextAction=0;return;}
                if (!Game.UI.Runtime.SettingsService.Load().Accessibility.ReducedMotion && pointerTravel<5*scale)
                    throw new InvalidOperationException("Plan B pointer did not visibly travel during three seconds.");
                if(pointerSamples<20) throw new InvalidOperationException("Insufficient rendered frames for stability check.");
                Log("PlanB stable rendered frames="+pointerSamples+" travel="+pointerTravel+" centered,double-edge,caption-visible");
                ScreenCapture.CaptureScreenshot(Output + "/planb-pointer-b-" + GameLocalization.CurrentLocaleCode + ".png");
                if (!Click(aria.ContinueButton)) return;
                if (aria.ContinueButton.IsActive()) throw new InvalidOperationException("Continue did not hide immediately.");
                continueClicked = true; nextAction = EditorApplication.timeSinceStartup + 1;
                return;
            }
            if (panel.TutorialStep == 5)
            {
                if (aria.ContinueButton.IsActive()) throw new InvalidOperationException("Consumed Continue reappeared.");
                string expected = GameLocalization.Get("tutorial.m02.construction_wait.body");
                if (aria.CurrentInstructionBody != expected) throw new InvalidOperationException("Continue did not show the localized construction status: " + aria.CurrentInstructionBody);
                if (!waitObserved) ScreenCapture.CaptureScreenshot(Output + "/continue-wait-" + GameLocalization.CurrentLocaleCode + ".png");
                waitObserved = true; return;
            }
            if (panel.TutorialStep != 6) return;
            if (facts.RequiredBuildingCompletedCount == 0) throw new InvalidOperationException("Production lesson skipped construction completion.");
            if (string.IsNullOrWhiteSpace(aria.CurrentInstructionBody)) throw new InvalidOperationException("Production instruction is empty.");
            var cue = GameObject.Find("AriaAssistantTargetIndicatorRuntime");
            if (cue == null && !aria.ShowMeButton.IsActive()) return;
            ScreenCapture.CaptureScreenshot(Output + "/continue-next-" + GameLocalization.CurrentLocaleCode + ".png");
            Complete(true, "Continue " + GameLocalization.CurrentLocaleCode + ": real placement -> immediate button removal -> rifle-production instruction with cue; construction wait observed=" + waitObserved + ". No Show Me prerequisite.");
        }
    }
}
