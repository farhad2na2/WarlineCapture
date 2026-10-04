#if UNITY_EDITOR
using System;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static AriaTouchInputUiSystemHelper menuTouch;
        private static int menuStep;
        private static double menuDue;
        private static double menuDeadline;

        public static void RunS004EditorAriaEn() => RunEditorJourney("en", false);
        public static void RunS004EditorAriaStopEn() => RunEditorJourney("en", false, true);
        public static void RunS004EditorManualFa() => RunEditorJourney("fa-IR", true);
        private static void RunEditorJourney(string language, bool manual, bool stopProbe=false)
        {
            SessionState.SetBool("S004EditorJourney.OptionsSaved", true);
            SessionState.SetBool("S004EditorJourney.OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt("S004EditorJourney.Options", (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableDomainReload;
            // Match the shared native-probe completion contract: acquire one
            // refresh suspension and release it after the owned Play session ends.
            AssetDatabase.DisallowAutoRefresh();
            SessionState.SetBool("Warline.MissionValidation.ResumeRefresh", true);
            Environment.SetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY", "1");
            Environment.SetEnvironmentVariable("WARLINE_S002_LOCALE", language);
            Environment.SetEnvironmentVariable("WARLINE_S002_SEED", "104733");
            Environment.SetEnvironmentVariable("WARLINE_REVIEW_WIDTH", "2400");
            Environment.SetEnvironmentVariable("WARLINE_REVIEW_HEIGHT", "1080");
            Environment.SetEnvironmentVariable("WARLINE_TERMINAL_ACTION", manual ? "MAIN MENU" : "ADJUST SETUP");
            Environment.SetEnvironmentVariable("WARLINE_S004_STOP_PROBE", stopProbe ? "1" : null);
            Environment.SetEnvironmentVariable("WARLINE_S004_MANUAL_PROBE", manual ? "1" : null);
            menuStep = 0;
            RunS004AriaAndExit();
        }

        private static void RestoreS004EditorJourneyOptions()
        {
            if (!SessionState.GetBool("S004EditorJourney.OptionsSaved", false)) return;
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt("S004EditorJourney.Options", 0);
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("S004EditorJourney.OptionsEnabled", false);
            SessionState.EraseBool("S004EditorJourney.OptionsSaved");
        }

        private static bool EnterS004ThroughPresentedControls()
        {
            if (menuStep == 6) return true;
            if (menuTouch == null)
            {
                ConfigureBackgroundInput();
                menuStep = 0;
                menuDeadline = EditorApplication.timeSinceStartup + 90d;
                menuTouch = new AriaTouchInputUiSystemHelper();
                if (!menuTouch.Start()) throw new InvalidOperationException("Menu Touch unavailable.");
                var runner = new GameObject("S004MenuInputProbe");
                UnityEngine.Object.DontDestroyOnLoad(runner);
                runner.AddComponent<SkirmishS004MenuInputFrame>();
            }
            return false;
        }

        internal static void TickS004MenuPlayerFrame()
        {
            if (menuTouch == null) return;
            if (EditorApplication.timeSinceStartup > menuDeadline)
            {
                Debug.LogError("[S004MenuJourney] result=Failed reason=controlsTimeout step=" + menuStep);
                Finish(true, "menuControlsTimeout");
                return;
            }
            menuTouch.Tick(Time.unscaledTime);
            if (menuTouch.PlayerInterrupted || menuTouch.UnexpectedSamples != 0)
                throw new InvalidOperationException("Menu input interrupted.");
            if (menuTouch.IsBusy || EditorApplication.timeSinceStartup < menuDue) return;
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
            if (menuStep == 5)
            {
                menuStep = 6;
                StopS004MenuProbe();
                return;
            }
            if (menuStep == 0)
            {
                foreach (var b in buttons)
                    if ((b.name == "Card_Skirmish" || b.transform.parent.name == "Card_Skirmish") && MenuTap(b)) { menuStep++; return; }
                foreach (var b in buttons)
                    if ((b.name == "SkipButton" || b.name == "ConfirmButton") && MenuTap(b)) return;
            }
            else if (menuStep == 1)
            {
                foreach (var b in buttons)
                    if (b.name == "ScenarioCard_" + SkirmishAcceptanceCensusCapture.S004CatalogId && MenuTap(b))
                    { menuStep = 4; return; }
                foreach (var scroll in UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude))
                {
                    if (scroll.name != "BattleLibrary" || scroll.viewport == null) continue;
                    var rect = scroll.viewport;
                    var canvas = scroll.GetComponentInParent<Canvas>();
                    var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                    var from = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector2(rect.rect.center.x, rect.rect.yMin + rect.rect.height * .2f)));
                    var to = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector2(rect.rect.center.x, rect.rect.yMax - rect.rect.height * .2f)));
                    if (menuTouch.TryGesture(from, to, .12f, .45f, Time.unscaledTime))
                        menuDue = EditorApplication.timeSinceStartup + 1d;
                    return;
                }
            }
            else if (menuStep == 4)
            {
                foreach (var setup in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude))
                {
                    if (!setup.IsBaseAssaultSetup || setup.SelectedScenarioId != catalogId) continue;
                    // Fixed seed is a test fixture; entry, selection and launch use native input.
                    setup.PrepareScenarioAdjustment(setup.ReadConfigFromControls().ScenarioIndex, seed);
                    foreach (var label in setup.GetComponentsInChildren<TMP_Text>())
                        if (label.name == "OperationName" || label.name == "SeedHelp")
                        {
                            label.ForceMeshUpdate();
                            Debug.Log("[S004SetupText] label=" + label.name + " rect=" + label.rectTransform.rect +
                                " fontSize=" + label.fontSize + " characters=" + label.textInfo.characterCount +
                                " visible=" + label.textInfo.characterCount + " overflow=" + label.isTextOverflowing);
                        }
                    ScreenCapture.CaptureScreenshot(LiveTracePath() + ".setup.png");
                    foreach (var b in setup.GetComponentsInChildren<Button>())
                        if ((b.name.Contains("Launch") || b.name.Contains("Start")) && MenuTap(b))
                        {
                            Debug.Log("[S004MenuJourney] result=Passed input=Touch controls=Skirmish,Scroll,S004,Launch seedFixture=" + seed);
                            menuStep = 5;
                            return;
                        }
                }
            }
        }

        private static bool MenuTap(Button b)
        {
            if (!TryPresentedButtonPoint(b, out var p)) return false;
            bool accepted = menuTouch.TryGesture(p, p, .15f, 0f, Time.unscaledTime);
            if (accepted) menuDue = EditorApplication.timeSinceStartup + 1d;
            return accepted;
        }

        private static void StopS004MenuProbe()
        {
            menuTouch?.Dispose();
            menuTouch = null;
        }
    }

    public sealed class SkirmishS004MenuInputFrame : MonoBehaviour
    {
        private void Update() => SkirmishS002AriaRunHarness.TickS004MenuPlayerFrame();
    }
}
#endif
