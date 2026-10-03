using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Route-jump screenshots and geometry findings for menu UI review. Does not establish player navigation or device acceptance.</summary>
    public static class MenuUiAuditCapture
    {
        private const string Marker = "[MenuUiAudit]";
        private const int SettledMissions = 12;
        private static readonly (UIRoute route, string name)[] Screens =
        {
            (UIRoute.MainMenu, "main-menu"),
            (UIRoute.Campaign, "campaign"),
            (UIRoute.QuickCustomSetup, "skirmish"),
            (UIRoute.Operations, "operations"),
            (UIRoute.CommanderProfile, "commander"),
        };
        private static readonly (int width, int height, string locale)[] Views =
        {
            (2400, 1080, "en"),
            (1920, 1080, "en"),
            (2048, 1536, "en"),
            (2400, 1080, "fa-IR"),
        };

        public static void Run() => _ = RunAsync(Views);
        public static void RunAfter() => _ = RunAfterChecked();
        public static Task<int> RunAfterChecked() => RunAsync(new[] {
            (1920,1080,"en"), (2400,1080,"en"), (2048,1536,"en"),
            (1920,1080,"fa-IR"), (2400,1080,"fa-IR"), (2048,1536,"fa-IR") }, true);
        public static void RunPhone() => _ = RunAsync(new[] { (2400, 1080, "en") });

        private static async Task<int> RunAsync((int width, int height, string locale)[] views, bool after = false)
        {
            string output = "Design/AgentReports/MenuUiUxAudit/" + (after ? "After/" : "Before/") + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(output);
            string previousRoot = Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            string previousLocale = GameLocalization.CurrentLocaleCode;
            var report = new StringBuilder();
            bool passed = false;
            bool playOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            var playOptions = EditorSettings.enterPlayModeOptions;
            bool allScreensCaptured = true;
            int capturedScreens = 0;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires Edit mode");
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",
                    Path.Combine(Path.GetTempPath(), "warline-menu-audit-" + Guid.NewGuid().ToString("N")));
                PrepareSave();
                MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
                EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath, OpenSceneMode.Single);

                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = playOptions | EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.EnterPlaymode();
                await Until(() => EditorApplication.isPlaying, 90);
                Application.runInBackground = true;
                await Until(() => UiShellRuntimeGateway.TryReadShellState(out var s) && s.ActiveRoute == UIRoute.MainMenu, 120);
                await Task.Delay(15000);
                foreach (var view in views)
                {
                    GameLocalization.SetLocale(view.locale, false);
                    MainMenuV3PrefabBuilder.SetGameViewResolution(view.width, view.height);
                    foreach (var screen in Screens)
                    {
                        if (!await Open(screen.route))
                        {
                            allScreensCaptured = false;
                            Debug.LogError(Marker + " unreachable route=" + screen.route);
                            report.AppendLine("## " + screen.name + " " + view.locale + " " + view.width + "x" + view.height + "\n- route unreachable\n");
                            continue;
                        }
                        string file = screen.name + "-" + view.locale + "-" + view.width + "x" + view.height;
                        report.AppendLine("## " + file);
                        string screenshot = output + "/" + file + ".png";
                        ScreenCapture.CaptureScreenshot(screenshot);
                        await Task.Delay(900);
                        try
                        {
                            await Until(() => File.Exists(screenshot) && new FileInfo(screenshot).Length > 0, 15);
                            capturedScreens++;
                        }
                        catch (TimeoutException)
                        {
                            allScreensCaptured = false;
                            Debug.LogError(Marker + " missing screenshot=" + screenshot);
                            report.AppendLine("- screenshot missing: " + file + ".png");
                        }
                        Audit(report, view.width, view.height);
                        report.AppendLine();
                    }
                }
                int expectedScreens = views.Length * Screens.Length;
                passed = allScreensCaptured && capturedScreens == expectedScreens;
                report.AppendLine("## Capture completeness");
                report.AppendLine("- Screenshots: " + capturedScreens + "/" + expectedScreens);
                report.AppendLine("- All requested routes and screenshots: " + (passed ? "complete" : "incomplete"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                File.WriteAllText(output + "/findings.md", report.ToString());
                if(EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.ExitPlaymode();
                    await Until(()=>!EditorApplication.isPlayingOrWillChangePlaymode,60);
                }
                EditorSettings.enterPlayModeOptions = playOptions;
                EditorSettings.enterPlayModeOptionsEnabled = playOptionsEnabled;

                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", previousRoot);
                GameLocalization.SetLocale(previousLocale, false);
                Debug.Log(Marker + " result=" + (passed ? "Passed" : "Failed") + " output=" + output);
                MissionEditorValidationExit.Complete(passed);
                if (passed) MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            }
            return passed ? 0 : 1;
        }

        internal static void PrepareSave()
        {
            var saves = SaveService.CreateDefault();
            var profile = saves.LoadProfile();
            profile.firstLaunchStatus = FirstLaunchProfileState.Completed;
            profile.firstLaunchWatched = true;
            profile.firstLaunchLanguage = "English";
            profile.firstLaunchCommanderDisplayName = "Commander";
            profile.firstLaunchCommanderPortraitIndex = 0;
            saves.SaveProfile(profile);
            var store = new CampaignMissionProgressStore(saves);
            for (int i = 0; i < SettledMissions; i++)
            {
                store.EnsureAvailable(CampaignMissionSequence.IdAt(i));
                store.Settle(CampaignMissionSequence.IdAt(i), "menu-audit-" + i, i, true, 3, 60000, null);
            }
            store.EnsureAvailable(CampaignMissionSequence.IdAt(SettledMissions));
        }

        private static async Task<bool> Open(UIRoute route)
        {
            try
            {
                var intent = route == UIRoute.MainMenu ? UiShellRouteIntent.ReturnToMainMenu : UiShellRouteIntent.OpenMenuRoute;
                await Until(() => UiShellRuntimeGateway.TryEnqueueRouteRequest(intent, route, false), 15);
                await Task.Delay(250);
                await Until(() => UiShellRuntimeGateway.TryReadShellState(out var s) && s.ActiveRoute == route &&
                    s.CurrentMode == UiShellMode.MainMenu && !s.IsTransitionRunning && LoadingClear(), 60);
                await Task.Delay(900);
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        internal static bool LoadingClear()
        {
            var shell=UnityEngine.Object.FindAnyObjectByType<UIShellView>();
            return shell!=null && (!shell.TryGetRegion(UIShellRegionId.LoadingLayer,out var loading) || loading.CanvasGroup.alpha < .01f || loading.ContentRoot.childCount==0);
        }

        private static void Audit(StringBuilder report, int width, int height)
        {
            var screen = new Rect(0, 0, width, height);
            // Phone reference: a 20:9 2400px-wide panel at ~400 dpi, so 1dp ~= 2.5px.
            float dp = width / 960f;
            var fonts = new Dictionary<string, int>();
            var sizes = new SortedDictionary<int, int>();
            int overflow = 0, tiny = 0, small = 0, offscreen = 0;
            foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text) || text.color.a < .05f || !ActuallyVisible(text.rectTransform)) continue;
                if (!TryScreenRect(text.rectTransform, out var rect) || rect.width < 1) continue;
                string font = text.font != null ? text.font.name : "null";
                fonts[font] = fonts.TryGetValue(font, out int n) ? n + 1 : 1;
                int sizeDp = Mathf.RoundToInt(PixelSize(text) / dp);
                sizes[sizeDp] = sizes.TryGetValue(sizeDp, out int c) ? c + 1 : 1;
                string label = NodePath(text.transform) + " \"" + Trim(text.text) + "\"";
                if (sizeDp < 11) { tiny++; report.AppendLine("- tiny-text " + sizeDp + "dp " + label); }
                if (text.isTextOverflowing || text.overflowMode == TextOverflowModes.Overflow && text.preferredHeight > text.rectTransform.rect.height + 2)
                { overflow++; report.AppendLine("- text-overflow " + label); }
                if (!screen.Overlaps(rect) || rect.xMin < -2 || rect.yMin < -2 || rect.xMax > width + 2 || rect.yMax > height + 2)
                { offscreen++; report.AppendLine("- text-offscreen " + label + " rect=" + rect); }
            }
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!button.isActiveAndEnabled || !button.interactable || !ActuallyVisible((RectTransform)button.transform)) continue;
                if (!TryScreenRect((RectTransform)button.transform, out var rect)) continue;
                float minDp = Mathf.Min(rect.width, rect.height) / dp;
                if (minDp < 44) { small++; report.AppendLine("- small-target " + Mathf.RoundToInt(minDp) + "dp " + NodePath(button.transform)); }
                if (rect.xMin < 4 || rect.yMin < 4 || rect.xMax > width - 4 || rect.yMax > height - 4)
                { offscreen++; report.AppendLine("- target-touches-edge " + NodePath(button.transform) + " rect=" + rect); }
            }
            report.AppendLine("- fonts: " + string.Join(", ", fonts.Select(f => f.Key + "x" + f.Value)));
            report.AppendLine("- text sizes dp: " + string.Join(", ", sizes.Select(s => s.Key + "x" + s.Value)));
            report.AppendLine("- totals overflow=" + overflow + " tiny=" + tiny + " smallTargets=" + small + " edge/offscreen=" + offscreen);
        }

        private static bool ActuallyVisible(RectTransform rect)
        {
            if(!TryScreenRect(rect,out var bounds))return false;
            foreach(var group in rect.GetComponentsInParent<CanvasGroup>())if(group.alpha<.05f)return false;
            foreach(var mask in rect.GetComponentsInParent<RectMask2D>())
                if(mask.isActiveAndEnabled && TryScreenRect((RectTransform)mask.transform,out var clip) && !clip.Overlaps(bounds))return false;
            return new Rect(0,0,Screen.width,Screen.height).Overlaps(bounds);
        }
        private static float PixelSize(TMP_Text text)
        {
            var root = text.canvas != null ? text.canvas.rootCanvas : null;
            if (root == null) return text.fontSize;
            float relative = text.rectTransform.lossyScale.y / Mathf.Max(1e-5f, root.transform.lossyScale.y);
            return text.fontSize * relative * root.scaleFactor;
        }

        private static bool TryScreenRect(RectTransform rect, out Rect result)
        {
            result = default;
            var canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null) return false;
            var camera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            result = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        private static string NodePath(Transform t)
        {
            var parts = new List<string>();
            for (int i = 0; t != null && i < 4; i++, t = t.parent) parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Trim(string s)
        {
            s = s.Replace("\n", " ");
            return s.Length > 40 ? s.Substring(0, 40) + "…" : s;
        }

        private static async Task Until(Func<bool> condition, double seconds)
        {
            double end = EditorApplication.timeSinceStartup + seconds;
            while (!condition())
            {
                if (EditorApplication.timeSinceStartup > end) throw new TimeoutException("Menu audit state timed out");
                await Task.Delay(100);
            }
        }
    }
}
