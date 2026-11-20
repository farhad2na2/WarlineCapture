using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Game.Configs;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// W39 native prefab fixture for Board 1 (left column) and Board 3 (ARIA panel states).
// Presentation only: drives the shipped SCN08 HUD views with fixture models, captures
// EN/FA at 1920x1080 and 2400x1080 and measures screen geometry. It never claims mission
// readiness. Launch through Tools/CI/InvokeUnity.ps1 (no -quit); it exits the Editor itself.
public static class MatchHudSidePanelsCapture
{
    private const string Prefab = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string Root = "Design/AgentReports/MatchUiPolish/Implemented/W39/";
    private const float Tolerance = 1.5f;

    public static void RunBefore() => Launch("before", false);
    public static void RunAfter() => Launch("after", true);

    private static async void Launch(string phase, bool enforce)
    {
        int code = 1;
        try { code = await Run(phase, enforce); }
        catch (Exception error) { Debug.LogException(error); Debug.LogError("[W39SidePanels] result=Failed phase=" + phase); }
        EditorApplication.Exit(code);
    }

    private static async Task<int> Run(string phase, bool enforce)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires idle Editor");
        var locale = GameLocalization.CurrentLocaleCode;
        var options = EditorSettings.enterPlayModeOptions; bool enabled = EditorSettings.enterPlayModeOptionsEnabled;
        string output = Root + phase; Directory.CreateDirectory(output);
        foreach (var old in Directory.GetFiles(output, "*.png")) File.Delete(old);
        var report = new StringBuilder();
        var failures = new List<string>();
        bool passed = false;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = options | EnterPlayModeOptions.DisableDomainReload;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            EditorApplication.EnterPlaymode(); await Until(() => EditorApplication.isPlaying);
            Application.runInBackground = true;
            var camera = new GameObject("W39Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.16f, .19f, .2f, 1);
            camera.orthographic = true; camera.orthographicSize = 540; camera.depth = 100; camera.transform.position = new Vector3(0, 0, -10);
            var canvas = new GameObject("W39Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder = 30000;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (other.rootCanvas != canvas) other.gameObject.SetActive(false);
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(4800, 2160); scaler.matchWidthOrHeight = .5f;
            var hud = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), canvas.transform, false);
            var rect = (RectTransform)hud.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var fixture = new Fixture(hud, camera);
            foreach (var language in new[] { GameLocalization.EnglishLocaleCode, GameLocalization.PersianLocaleCode })
            foreach (int width in new[] { 1920, 2400 })
            {
                GameLocalization.SetLocale(language, false); MainMenuV3PrefabBuilder.SetGameViewResolution(width, 1080); await Task.Delay(700);
                foreach (var state in new[] { "aria-two-actions", "aria-one-action", "aria-stopped" })
                {
                    fixture.Apply(state, language == GameLocalization.PersianLocaleCode);
                    for (int i = 0; i < 8; i++) { fixture.Apply(state, language == GameLocalization.PersianLocaleCode, refresh: true); await Task.Delay(60); }
                    Canvas.ForceUpdateCanvases();
                    string name = state + "-" + language + "-" + width;
                    if (Screen.width != width || Screen.height != 1080)
                        throw new InvalidOperationException($"Game view is {Screen.width}x{Screen.height}, expected {width}x1080");
                    var lines = fixture.Measure(width, 1080, state, failures, name);
                    report.AppendLine("## " + name).AppendLine(lines).AppendLine();
                    ScreenCapture.CaptureScreenshot(output + "/" + name + ".png"); await Task.Delay(600);
                }
            }
            await Until(() => Directory.GetFiles(output, "*.png").Length == 12 && Directory.GetFiles(output, "*.png").All(p => new FileInfo(p).Length > 0));
            File.WriteAllText(output + "/geometry.md", "# W39 " + phase + " geometry (screen px, top-left origin)\n\n" + report +
                "\n## Rule check\n\n" + (failures.Count == 0 ? "All W39 layout rules passed.\n" : string.Join("\n", failures.Select(f => "- " + f)) + "\n"));
            foreach (var failure in failures) Debug.Log("[W39SidePanels] rule " + failure);
            passed = !enforce || failures.Count == 0;
            Debug.Log("[W39SidePanels] " + (passed ? "result=Passed" : "rules=Failed") + " phase=" + phase + " captures=12 ruleFailures=" + failures.Count +
                " enforced=" + enforce + " missionReadiness=NotClaimed output=" + output);
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.ExitPlaymode(); await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode); }
            await Task.Delay(500);
            EditorSettings.enterPlayModeOptions = options; EditorSettings.enterPlayModeOptionsEnabled = enabled; GameLocalization.SetLocale(locale, false);
        }
        return passed ? 0 : 1;
    }

    private sealed class Fixture
    {
        private readonly GameObject hud;
        private readonly Camera camera;
        private readonly MatchHudSelectionPanelView selection;
        private readonly MatchHudSquadTrayView tray;
        private readonly AriaTutorialBriefingView aria;
        private readonly MissionHudTouchLayoutView dock;
        private readonly RectTransform commandRail;
        private readonly RectTransform utility;
        private readonly List<UiMatchHudSquadTrayCardModel> roster = new();
        private Sprite[] portraits;

        public Fixture(GameObject hud, Camera camera)
        {
            this.hud = hud; this.camera = camera;
            selection = hud.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            tray = hud.GetComponentInChildren<MatchHudSquadTrayView>(true);
            aria = hud.GetComponentsInChildren<AriaTutorialBriefingView>(true).First(v => v.name == "AriaAssistantButton");
            dock = hud.GetComponentInChildren<MissionHudTouchLayoutView>(true);
            commandRail = Find(hud.transform, "CommandRail") as RectTransform;
            utility = Find(aria.transform, "M03Actions") as RectTransform;
            for (int i = 0; i < 8; i++)
                roster.Add(new UiMatchHudSquadTrayCardModel(true, i < 3 ? "RIFLE (4)" : i == 3 ? "GUNNER (4)" : "ROCKETEER (4)", "4/4", 1f,
                    (uint)(i + 1), 4, i + 1, i == 0));
        }

        public void Apply(string state, bool fa, bool refresh = false)
        {
            if (portraits == null)
            {
                portraits = new Sprite[5];
                for (int i = 0; i < 5; i++) tray.TryGetPortraitSprite((MatchHudSquadTraySlot)(i + 1), out portraits[i]);
            }
            bool two = state == "aria-two-actions", one = state == "aria-one-action", stopped = state == "aria-stopped";
            if (!refresh)
            {
                // Board 1: a tall selected-unit card over the category cards, then the paginated roster.
                tray.SetSelectedSlot(MatchHudSquadTraySlot.Soldiers);
                if (two || one)
                {
                    selection.Apply(new MatchHudSelectionPanelModel(true, fa ? "گروه تفنگدار" : "RIFLE SQUAD",
                        fa ? "گروه ۱ | ضد پیاده" : "SQUAD 1  |  ANTI-INFANTRY", fa ? "در حال حرکت" : "MOVING TO MARKER", "120 / 120", 1f,
                        selection.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Soldiers), SelectionSummaryPortraitKind.Soldiers,
                        true, null, true, true, true));
                    selection.ApplyTransportPassengers(new MatchHudTransportPassengersModel(true, false, new UiEntityHandle(1, 1), 0, 4, true,
                        Array.Empty<MatchHudSelectionPanelPassengerItemModel>(), soldierPassengerCount: 0, soldierCapacity: two ? 4 : 0));
                    selection.ShowSelection();
                }
                else selection.HideSelection();
                tray.ConfigureExpandedRoster(models => { models.Clear(); models.AddRange(roster); },
                    id => portraits[(int)((id - 1) % 3)]);
                if (one) Invoke(tray, "RefreshScrollingRoster", true);
                else Invoke(tray, "RestoreCategoryCards");

                // Board 3: active with two actions, active with one action, stopped.
                aria.Apply(new UiAssistantPanelModel(1, string.Empty, string.Empty, string.Empty, false, false, !stopped,
                    stopped ? string.Empty : fa ? (two ? "سیگنال را پیدا کن" : "سیگنال را اسکن کن") : (two ? "LOCATE THE SIGNAL" : "SCAN THE SIGNAL"),
                    stopped ? string.Empty : fa ? (two ? "قبل از حرکت گروه، محدودهٔ اسکن را نشان بده." : "وقتی گروه آماده است، اسکن را بزن.")
                        : (two ? "Show the scan area before moving your squad." : "Select Scan when your squad is ready."),
                    "HIGH", "DO IT", two, false, true, false, "PLAYER CONTROL", string.Empty));
                aria.ApplyAccessibility(false, false);
                aria.SetPresentationVisible(!stopped);
                var alert = Find(aria.transform, "AlertCue")?.GetComponent<TMP_Text>();
                if (alert != null) { alert.text = string.Empty; alert.gameObject.SetActive(false); }
                if (utility != null)
                {
                    utility.gameObject.SetActive(!stopped);
                    foreach (Transform child in utility) child.gameObject.SetActive(!stopped && child.name == "FieldGuide");
                }
            }
            if (one) Invoke(tray, "RefreshScrollingRoster", true);
            aria.PresentWatch(stopped ? default : new AriaPlayModel(AriaPlayPhase.Observing, Vector2.zero, false, 1), true);
            aria.RefreshContentLayout();
        }

        public string Measure(int width, int height, string state, List<string> failures, string name)
        {
            var text = new StringBuilder();
            void Line(string label, Rect? r) { if (r.HasValue) text.AppendLine($"- {label}: x={r.Value.xMin:0.0} y={r.Value.yMin:0.0} w={r.Value.width:0.0} h={r.Value.height:0.0} right={r.Value.xMax:0.0} bottom={r.Value.yMax:0.0}"); else text.AppendLine($"- {label}: hidden"); }
            void Rule(bool ok, string rule) { text.AppendLine($"  - {(ok ? "PASS" : "FAIL")} {rule}"); if (!ok) failures.Add(name + ": " + rule); }
            Rect? Of(Component c) => c != null && c.gameObject.activeInHierarchy ? ScreenRect((RectTransform)c.transform, height) : (Rect?)null;

            float unit = ScreenRect((RectTransform)hud.transform.Find("V3Composition") ?? (RectTransform)hud.transform, height).height;
            text.AppendLine($"- screen: {width}x{height}");
            // Board 1 ------------------------------------------------------------------
            var panel = selection.VisiblePanelRect;
            Rect? sel = panel != null ? ScreenRect(panel, height) : (Rect?)null;
            Line("selection card", sel);
            Rect? map = dock != null && dock.Minimap != null ? ScreenRect(dock.Minimap, height) : (Rect?)null;
            Line("minimap", map);
            var cards = tray.GetComponentsInChildren<Button>(true).Where(b => b.gameObject.activeInHierarchy && b.transform.parent.name == "SquadScrollContent")
                .Select(b => ScreenRect((RectTransform)b.transform, height)).Where(r => r.xMax > 0).OrderBy(r => r.xMin).ToList();
            var trayRect = ScreenRect((RectTransform)tray.transform, height);
            Line("squad tray viewport", trayRect);
            var shown = cards.Where(r => r.xMin >= trayRect.xMin - .5f && r.xMax <= trayRect.xMax + .5f).ToList();
            if (shown.Count > 0) { Line("first presented card", shown[0]); Line("last presented card", shown[^1]); }
            text.AppendLine($"- presented cards: {shown.Count}; partially visible cards: {cards.Count(r => r.xMax > trayRect.xMin && r.xMin < trayRect.xMax) - shown.Count}");
            Rect? rail = commandRail != null ? ScreenRect(commandRail, height) : (Rect?)null;
            Line("command strip", rail);
            var commandButtons = commandRail != null ? commandRail.GetComponentsInChildren<Button>(false).Where(b => b.transform.parent.name == "Frame")
                .Select(b => ScreenRect((RectTransform)b.transform, height)).OrderBy(r => r.xMin).ToList() : new List<Rect>();
            if (commandButtons.Count > 0) Line("command buttons (union)", Rect.MinMaxRect(commandButtons.Min(r => r.xMin), commandButtons.Min(r => r.yMin), commandButtons.Max(r => r.xMax), commandButtons.Max(r => r.yMax)));
            if (sel.HasValue)
            {
                var frame = selection.CommandWheelOpenButton != null ? selection.CommandWheelOpenButton.transform.parent : null;
                foreach (var part in new[] { "PortraitFrame", "HealthPanel", "OrderLabel", "V3PlayerControl" })
                    Line("card " + part, frame != null && frame.Find(part) is RectTransform p && p.gameObject.activeInHierarchy ? ScreenRect(p, height) : (Rect?)null);
                Line("card COMMANDS", Of(selection.CommandWheelOpenButton));
            }

            float firstCardLeft = shown.Count > 0 ? shown[0].xMin : float.NaN;
            if (sel.HasValue) Rule(Mathf.Abs(sel.Value.xMin - 16) <= Tolerance, $"selection card left inset 16 px (is {sel.Value.xMin:0.0})");
            if (map.HasValue) Rule(Mathf.Abs(map.Value.xMin - 16) <= Tolerance, $"minimap left inset 16 px (is {map.Value.xMin:0.0})");
            if (shown.Count > 0) Rule(Mathf.Abs(firstCardLeft - 16) <= Tolerance, $"first category card left inset 16 px (is {firstCardLeft:0.0})");
            if (sel.HasValue && map.HasValue)
            {
                float gap = map.Value.yMin - sel.Value.yMax;
                Rule(gap >= 24 - Tolerance, $"selection card to minimap gap >= 24 px, never overlapping (is {gap:0.0})");
            }
            if (map.HasValue && shown.Count > 0)
            {
                float gap = shown.Min(r => r.yMin) - map.Value.yMax;
                Rule(Mathf.Abs(gap - 24) <= Tolerance, $"minimap to category cards gap 24 px (is {gap:0.0})");
            }
            for (int i = 1; i < shown.Count; i++)
            {
                float gap = shown[i].xMin - shown[i - 1].xMax;
                Rule(Mathf.Abs(gap - 8) <= Tolerance, $"category card gap {i} is 8 px (is {gap:0.0})");
            }
            if (shown.Count > 0 && rail.HasValue)
            {
                float gap = rail.Value.xMin - shown[^1].xMax;
                Rule(gap >= 12 - Tolerance, $"last presented card to command strip >= 12 px (is {gap:0.0})");
                Rule(trayRect.xMax <= rail.Value.xMin + .5f, $"command strip does not overlap the card tray (tray right {trayRect.xMax:0.0}, strip left {rail.Value.xMin:0.0})");
                float cardsBottom = height - shown.Max(r => r.yMax);
                Rule(Mathf.Abs(cardsBottom - 16) <= Tolerance, $"category cards 16 px above bottom edge (is {cardsBottom:0.0})");
            }
            if (commandButtons.Count > 0)
            {
                float bottom = height - commandButtons.Max(r => r.yMax);
                Rule(Mathf.Abs(bottom - 16) <= Tolerance, $"command buttons 16 px above bottom edge (is {bottom:0.0})");
            }
            if (sel.HasValue && selection.CommandWheelOpenButton != null && selection.CommandWheelOpenButton.gameObject.activeInHierarchy)
            {
                var commands = ScreenRect((RectTransform)selection.CommandWheelOpenButton.transform, height);
                Rule(commands.height >= 64 - .5f, $"COMMANDS target >= 64 px tall (is {commands.height:0.0})");
                Rule(Mathf.Abs(commands.xMin - sel.Value.xMin - 16) <= Tolerance, $"card inner padding 16 px (COMMANDS left is {commands.xMin - sel.Value.xMin:0.0})");
                var frame = selection.CommandWheelOpenButton.transform.parent;
                var portrait = frame.Find("PortraitFrame") as RectTransform;
                var health = frame.Find("HealthPanel") as RectTransform;
                if (portrait != null && portrait.gameObject.activeInHierarchy)
                {
                    var p = ScreenRect(portrait, height);
                    Rule(Mathf.Abs(p.yMin - sel.Value.yMin - 16) <= Tolerance, $"card top padding 16 px (is {p.yMin - sel.Value.yMin:0.0})");
                    Rule(Mathf.Abs(commands.yMin - p.yMax - 12) <= Tolerance, $"portrait to COMMANDS 12 px (is {commands.yMin - p.yMax:0.0})");
                }
                if (health != null && health.gameObject.activeInHierarchy)
                {
                    var h = ScreenRect(health, height);
                    Rule(Mathf.Abs(h.yMin - commands.yMax - 12) <= Tolerance, $"COMMANDS to health 12 px (is {h.yMin - commands.yMax:0.0})");
                }
            }

            // Board 3 ------------------------------------------------------------------
            var ariaRect = ScreenRect((RectTransform)aria.transform, height);
            Line("ARIA panel", ariaRect);
            var buttons = aria.GetComponentsInChildren<Button>(false).Where(b => b.gameObject.activeInHierarchy && b.transform.parent != null)
                .Select(b => (b, r: ScreenRect((RectTransform)b.transform, height))).Where(x => x.r.height > 20).OrderBy(x => x.r.yMin).ToList();
            foreach (var (b, r) in buttons)
            {
                var label = b.GetComponentInChildren<TMP_Text>(false);
                string caption = label == null ? "" : label is RTLTMPro.RTLTextMeshPro rtl ? rtl.OriginalText : label.text;
                var icon = b.transform.Find("HudActionIcon")?.GetComponent<Image>();
                text.AppendLine($"- ARIA button {b.name} \"{caption}\" icon={(icon != null && icon.isActiveAndEnabled && icon.sprite != null ? icon.sprite.name : "none")}: x={r.xMin:0.0} y={r.yMin:0.0} w={r.width:0.0} h={r.height:0.0}");
                Rule(!string.IsNullOrWhiteSpace(caption), $"ARIA button {b.name} is labelled");
                Rule(r.height >= 64 - .5f, $"ARIA button {b.name} >= 64 px tall (is {r.height:0.0})");
                Rule(Mathf.Abs(r.xMin - ariaRect.xMin - 16) <= Tolerance && Mathf.Abs(ariaRect.xMax - r.xMax - 16) <= Tolerance,
                    $"ARIA button {b.name} 16 px side insets (left {r.xMin - ariaRect.xMin:0.0}, right {ariaRect.xMax - r.xMax:0.0})");
            }
            for (int i = 1; i < buttons.Count; i++)
            {
                float gap = buttons[i].r.yMin - buttons[i - 1].r.yMax;
                Rule(Mathf.Abs(gap - 12) <= Tolerance, $"ARIA button gap {buttons[i - 1].b.name}->{buttons[i].b.name} 12 px (is {gap:0.0})");
            }
            if (buttons.Count > 0)
            {
                float bottom = ariaRect.yMax - buttons[^1].r.yMax;
                Rule(Mathf.Abs(bottom - 16) <= Tolerance, $"ARIA panel ends 16 px below its last button, no reserved space (is {bottom:0.0})");
            }
            var names = buttons.Select(x => x.b.name).ToList();
            string[] expected = state == "aria-two-actions" ? new[] { "ShowMeButton|FieldGuide", "FieldGuide|ShowMeButton", "WatchAriaPlay" }
                : state == "aria-one-action" ? new[] { "FieldGuide", "WatchAriaPlay" } : new[] { "WatchAriaPlay" };
            bool mapping = names.Count == expected.Length;
            for (int i = 0; mapping && i < expected.Length; i++) mapping = expected[i].Split('|').Contains(names[i]) ||
                (state == "aria-two-actions" && i < 2 && (names[i] == "ShowMeButton" || names[i] == "FieldGuide"));
            Rule(mapping, $"state {state} shows [{string.Join(", ", expected)}] (shows [{string.Join(", ", names)}])");
            if (state == "aria-two-actions")
            {
                var showMe = aria.ShowMeButton.transform.Find("HudActionIcon")?.GetComponent<Image>();
                Rule(showMe != null && showMe.sprite != null && showMe.sprite.name == "HudAction_" + HudButtonIcon.Camera, "Show Me uses the native camera-focus glyph");
            }
            var status = Find(aria.transform, "AriaStatus")?.GetComponent<TMP_Text>();
            string statusText = status != null && status.gameObject.activeInHierarchy ? (status is RTLTMPro.RTLTextMeshPro s ? s.OriginalText : status.text) : null;
            text.AppendLine($"- ARIA status line: {(statusText ?? "hidden")}");
            if (state == "aria-stopped") Rule(!string.IsNullOrWhiteSpace(statusText), "stopped state shows the ARIA status line");
            text.AppendLine($"- HUD composition height on screen: {unit:0.0} px");
            return text.ToString();
        }

        private Rect ScreenRect(RectTransform rect, int height)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(low.x, height - high.y, high.x, height - low.y);
        }
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
        return null;
    }

    private static void Invoke(object target, string method, params object[] args)
    {
        var info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (info == null) throw new MissingMethodException(target.GetType().Name, method);
        info.Invoke(target, args);
    }

    private static async Task Until(Func<bool> condition)
    {
        double end = EditorApplication.timeSinceStartup + 120;
        while (!condition()) { if (EditorApplication.timeSinceStartup > end) throw new TimeoutException("W39 fixture timed out"); await Task.Delay(100); }
    }
}
