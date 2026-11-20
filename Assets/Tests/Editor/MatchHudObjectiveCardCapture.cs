using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Game.Configs;
using Game.Editor;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// W40 native fixture for Board 2 (compact objective card) and Board 4 (ownership colours, text fit).
// Presentation only: the shipped SkirmishMatchView and SCN08 HUD prefab are driven with fixture
// match/objective models named after the audited missions. Captures EN/FA at 1920x1080 and 2400x1080
// (top 600 px band, full width) and measures screen geometry. It never claims mission readiness.
// Launch through Tools/CI/InvokeUnity.ps1 (no -quit); it exits the Editor itself.
public static class MatchHudObjectiveCardCapture
{
    private const string Prefab = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string Root = "Design/AgentReports/MatchUiPolish/Implemented/W40/";
    private const float Tolerance = 1.5f;
    private const int Band = 600;
    private static readonly string[] States =
        { "s002-ba", "s026-ba-first-order", "s031-fc", "s011-fc-neutral", "s037-bt", "s041-bt-exit-open", "s020-ce", "s046-ce-service" };

    public static void RunBefore() => Launch("before", false);
    public static void RunAfter() => Launch("after", true);

    private static async void Launch(string phase, bool enforce)
    {
        int code = 1;
        try { code = await Run(phase, enforce); }
        catch (Exception error) { Debug.LogException(error); Debug.LogError("[W40ObjectiveCard] result=Failed phase=" + phase); }
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
        int expected = States.Length * 4;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = options | EnterPlayModeOptions.DisableDomainReload;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            EditorApplication.EnterPlaymode(); await Until(() => EditorApplication.isPlaying);
            Application.runInBackground = true;
            var camera = new GameObject("W40Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.16f, .19f, .2f, 1);
            camera.orthographic = true; camera.orthographicSize = 540; camera.depth = 100; camera.transform.position = new Vector3(0, 0, -10);
            var canvas = new GameObject("W40Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder = 30000;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (other.rootCanvas != canvas) other.gameObject.SetActive(false);
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(4800, 2160); scaler.matchWidthOrHeight = .5f;
            var hud = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), canvas.transform, false);
            var rect = (RectTransform)hud.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var gateway = new W40ObjectiveCardGateway();
            UiShellRuntimeGateway.Register(gateway);
            var fixture = new Fixture(hud, camera, gateway);
            foreach (var language in new[] { GameLocalization.EnglishLocaleCode, GameLocalization.PersianLocaleCode })
            foreach (int width in new[] { 1920, 2400 })
            {
                GameLocalization.SetLocale(language, false); MainMenuV3PrefabBuilder.SetGameViewResolution(width, 1080); await Task.Delay(700);
                foreach (var state in States)
                {
                    bool fa = language == GameLocalization.PersianLocaleCode;
                    fixture.Apply(state, fa);
                    for (int i = 0; i < 10; i++) { fixture.Refresh(state); await Task.Delay(60); }
                    Canvas.ForceUpdateCanvases();
                    string name = state + "-" + language + "-" + width;
                    if (Screen.width != width || Screen.height != 1080)
                        throw new InvalidOperationException($"Game view is {Screen.width}x{Screen.height}, expected {width}x1080");
                    report.AppendLine("## " + name).AppendLine(fixture.Measure(width, 1080, state, failures, name)).AppendLine();
                    await Task.Yield();
                    await CaptureBand(output + "/" + name + ".png");
                }
            }
            await Until(() => Directory.GetFiles(output, "*.png").Length == expected && Directory.GetFiles(output, "*.png").All(p => new FileInfo(p).Length > 0));
            await Task.Delay(1000);
            foreach (var png in Directory.GetFiles(output, "*.png")) CropToBand(png);
            File.WriteAllText(output + "/geometry.md", "# W40 " + phase + " geometry (screen px, top-left origin)\n\n" +
                "Fixture states are named after the audited missions; they are native-view renders of fixture models, not live mission frames.\n\n" + report +
                "\n## Rule check\n\n" + (failures.Count == 0 ? "All W40 layout rules passed.\n" : string.Join("\n", failures.Select(f => "- " + f)) + "\n"));
            foreach (var failure in failures) Debug.Log("[W40ObjectiveCard] rule " + failure);
            passed = !enforce || failures.Count == 0;
            Debug.Log("[W40ObjectiveCard] " + (passed ? "result=Passed" : "rules=Failed") + " phase=" + phase + " captures=" + expected + " ruleFailures=" + failures.Count +
                " enforced=" + enforce + " missionReadiness=NotClaimed output=" + output);
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.ExitPlaymode(); await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode); }
            await Task.Delay(500);
            EditorSettings.enterPlayModeOptions = options; EditorSettings.enterPlayModeOptionsEnabled = enabled; GameLocalization.SetLocale(locale, false);
        }
        return passed ? 0 : 1;
    }

    private static async Task CaptureBand(string path)
    {
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(600);
    }

    // Full-width top band (resource bar, objective card, ARIA rail top and the in-world base chip).
    private static void CropToBand(string path)
    {
        var frame = new Texture2D(2, 2, TextureFormat.RGB24, false);
        try
        {
            if (!frame.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("Unreadable capture " + path);
            int height = Mathf.Min(Band, frame.height);
            var band = new Texture2D(frame.width, height, TextureFormat.RGB24, false);
            band.SetPixels(frame.GetPixels(0, frame.height - height, frame.width, height)); band.Apply();
            File.WriteAllBytes(path, band.EncodeToPNG());
            UnityEngine.Object.Destroy(band);
        }
        finally { UnityEngine.Object.Destroy(frame); }
    }

    private sealed class Fixture
    {
        private readonly GameObject hud;
        private readonly Camera camera;
        private readonly W40ObjectiveCardGateway gateway;
        private readonly RectTransform resources, threat;
        private readonly AriaTutorialBriefingView aria;
        private SkirmishMatchView view;
        private readonly List<int> expectedOwners = new();

        public Fixture(GameObject hud, Camera camera, W40ObjectiveCardGateway gateway)
        {
            this.hud = hud; this.camera = camera; this.gateway = gateway;
            resources = Find(hud.transform, "ResourceStrip") as RectTransform;
            threat = Find(hud.transform, "ThreatJumpPanel") as RectTransform;
            aria = hud.GetComponentsInChildren<AriaTutorialBriefingView>(true).FirstOrDefault(v => v.name == "AriaAssistantButton");
            var selection = hud.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            if (selection != null) selection.HideSelection();
        }

        public void Apply(string state, bool fa)
        {
            if (threat != null) { var visibility = threat.GetComponent<MatchHudThreatVisibilityView>(); if (visibility != null) visibility.SetRequested(false); else threat.gameObject.SetActive(false); }
            // A fresh view per state, as a new match would create it.
            if (view != null) UnityEngine.Object.DestroyImmediate(view.gameObject);
            gateway.Match = MatchModel(state, fa);
            gateway.Objective = ObjectiveModel(state, fa);
            view = SkirmishMatchView.Create();
            if (aria != null)
            {
                string title = Family(state, fa);
                aria.Apply(new UiAssistantPanelModel(1, string.Empty, string.Empty, string.Empty, false, false, true, title,
                    fa ? "یک کارت گروه را لمس می‌کنم تا سربازانش را انتخاب کنم." : "I'll tap a squad card to select its soldiers.",
                    "HIGH", "DO IT", false, false, true, false, "PLAYER CONTROL", string.Empty));
                aria.ApplyAccessibility(false, false);
                aria.SetPresentationVisible(true);
                var alert = Find(aria.transform, "AlertCue")?.GetComponent<TMP_Text>();
                if (alert != null) { alert.text = string.Empty; alert.gameObject.SetActive(false); }
                var utility = Find(aria.transform, "M03Actions");
                if (utility != null) foreach (Transform child in utility) child.gameObject.SetActive(child.name == "FieldGuide");
            }
        }

        public void Refresh(string state)
        {
            aria?.PresentWatch(new AriaPlayModel(AriaPlayPhase.Observing, Vector2.zero, false, 1), true);
            aria?.RefreshContentLayout();
            if (view == null || !state.Contains("-ba")) return;
            // In-world enemy base status (fog: reported position), below the card.
            float x = Screen.width * .55f, y = Screen.height - 560f;
            view.PresentEnemyBaseMarker(new Vector3(x, y, 20f), true, new Vector3(160f, 0f, 220f), Vector3.zero);
        }

        private static string Family(string state, bool fa) =>
            state.Contains("-fc") ? (fa ? "کنترل خط" : "FRONTLINE CONTROL") : state.Contains("-bt") ? (fa ? "شکستن خط" : "BREAKTHROUGH")
            : state.Contains("-ce") ? (fa ? "اسکورت کاروان" : "CONVOY ESCORT") : (fa ? "حمله به پایگاه" : "BASE ASSAULT");

        private static string Copy(string key)
        {
            bool fa = GameLocalization.CurrentLocaleCode == GameLocalization.PersianLocaleCode;
            foreach (var row in SkirmishObjectiveCopyTable.Rows) if (row.Key == key) return fa ? row.Farsi : row.English;
            return GameText.Get(key, key);
        }

        private UiSkirmishModel MatchModel(string state, bool fa)
        {
            string objective = state.Contains("-fc") ? Copy("skirmish.s007.objective") : state.Contains("-bt") ? Copy("skirmish.s013.objective")
                : state.Contains("-ce") ? Copy("skirmish.s019.objective") : GameText.Get("skirmish.s002.objective", "Destroy the original enemy Barracks while your original Barracks survives.");
            string time = state.Contains("-fc") ? "19:53" : state.Contains("-bt") ? "8:53" : state.Contains("-ce") ? "7:22" : "17:53";
            return new UiSkirmishModel
            {
                Expanded = true, PlayerHealth = 800, EnemyHealth = 800, CatalogId = state.Substring(0, 4).ToUpperInvariant(),
                PlayerBase = GameText.Get("ui.skirmish.player_base", "YOUR MAIN BASE") + "\n800 / 800",
                EnemyBase = GameText.Get("ui.skirmish.enemy_base", "ENEMY MAIN BASE") + "\n" + GameText.Get("ui.skirmish.base_not_in_sight", "NOT IN SIGHT"),
                EnemyBaseKnowledgeKnown = true, EnemyBaseKnowledge = 0,
                Clock = GameText.Get("ui.skirmish.time_remaining", "TIME LEFT") + "  " + time,
                Objective = objective,
                FirstOrderText = state == "s026-ba-first-order" ? GameText.Get("skirmish.s026.first_order", "Advance your infantry along the main avenue to the crossroads.") : null,
                Infantry = "20 / 48",
            };
        }

        private UiSkirmishPublicObjectiveModel ObjectiveModel(string state, bool fa)
        {
            expectedOwners.Clear();
            if (state.Contains("-ba")) { expectedOwners.Add(1); expectedOwners.Add(2); return null; }
            long second = TimeSpan.TicksPerSecond;
            var nodes = new List<SkirmishPublicGoalNode>();
            var score = new SkirmishPublicGoalScore();
            SkirmishObjectiveKind kind; SkirmishPublicGoalPerspective perspective;
            SkirmishPublicGoalNode Area(string id, SkirmishPublicGoalNodeKind nodeKind, float x, float z) =>
                new SkirmishPublicGoalNode { PublicId = id, Kind = nodeKind, Provenance = SkirmishPublicGoalProvenance.PublicObjective, PositionKnown = true, WorldX = x, WorldZ = z };
            if (state.Contains("-fc"))
            {
                kind = SkirmishObjectiveKind.FrontlineControl; perspective = SkirmishPublicGoalPerspective.Capture;
                score.TicketsKnown = true; score.OwnTickets = 500; score.OpposingTickets = 500;
                bool neutral = state.Contains("neutral");
                int[] owners = neutral ? new[] { 0, 0, 0 } : new[] { 1, 2, 0 };
                for (int i = 0; i < 3; i++)
                {
                    var zone = Area("zone." + (char)('a' + i), SkirmishPublicGoalNodeKind.CaptureZone, -120 + 120 * i, 40);
                    zone.OwnerKnown = true; zone.OwnerFaction = owners[i];
                    if (!neutral && i == 1) zone.Contested = true;
                    if (!neutral && i == 2) { zone.CaptureProgressKnown = true; zone.CapturingFaction = 1; zone.CaptureFraction = .4f; zone.CapturePhase = SkirmishPublicCapturePhase.Capturing; }
                    nodes.Add(zone); expectedOwners.Add(owners[i]);
                }
            }
            else if (state.Contains("-bt"))
            {
                kind = SkirmishObjectiveKind.Breakthrough; perspective = SkirmishPublicGoalPerspective.Evacuate;
                bool open = state.Contains("exit-open");
                score.ProgressKnown = true; score.Required = 8; score.OriginalSubjects = 12; score.Completed = open ? 3 : 0; score.ExitStateKnown = true; score.ExitOpen = open;
                var a = Area("corridor.a", SkirmishPublicGoalNodeKind.ExitCorridor, -100, 0); a.WorkProgressKnown = true; a.RequiredWorkTicks = 20 * second; a.WorkTicks = open ? 20 * second : 0;
                var b = Area("corridor.b", SkirmishPublicGoalNodeKind.ExitCorridor, 100, 0); b.WorkProgressKnown = true; b.RequiredWorkTicks = 20 * second; b.WorkTicks = open ? 12 * second : 0;
                nodes.Add(a); nodes.Add(b); nodes.Add(Area("exit", SkirmishPublicGoalNodeKind.EvacuationExit, 0, 200));
                expectedOwners.AddRange(new[] { -1, -1, -1 });
            }
            else
            {
                kind = SkirmishObjectiveKind.ConvoyEscort; perspective = SkirmishPublicGoalPerspective.Escort;
                score.ProgressKnown = true; score.Required = 2; score.OriginalSubjects = 3;
                nodes.Add(Area("destination", SkirmishPublicGoalNodeKind.DeliveryDestination, 0, 300));
                expectedOwners.AddRange(new[] { -1, -1 });
            }
            if (!SkirmishPublicGoalSnapshot.TryCreate("w40." + state, 1, 1, kind, perspective, 60 * second, 480 * second, SkirmishOutcomeKind.None, score, nodes.ToArray(), out var goal, out var error))
                throw new InvalidOperationException("Fixture goal " + state + ": " + error);
            var model = new UiSkirmishPublicObjectiveModel { Applicable = true, Available = true, AttemptId = "w40-" + state, Goal = goal, CommandFeedbackReady = false, GroundCellSize = 4 };
            if (state.Contains("service"))
                model.Service = new UiPublicConvoyService { PublicId = "service.pad", Available = true, Materials = 900, EligibilityRadiusMetres = 12, World = new Vector3(40, 0, 80) };
            return model;
        }

        public string Measure(int width, int height, string state, List<string> failures, string name)
        {
            var text = new StringBuilder();
            void Line(string label, Rect? r) { if (r.HasValue) text.AppendLine($"- {label}: x={r.Value.xMin:0.0} y={r.Value.yMin:0.0} w={r.Value.width:0.0} h={r.Value.height:0.0} right={r.Value.xMax:0.0} bottom={r.Value.yMax:0.0}"); else text.AppendLine($"- {label}: hidden"); }
            void Rule(bool ok, string rule) { text.AppendLine($"  - {(ok ? "PASS" : "FAIL")} {rule}"); if (!ok) failures.Add(name + ": " + rule); }
            text.AppendLine($"- screen: {width}x{height}");
            var root = view.transform;
            var card = root.Find("BaseObjectives") as RectTransform;
            Rect cardRect = ScreenRect(card, height, null);
            Line("objective card", cardRect);
            Rect? res = resources != null ? ScreenRect(resources, height, camera) : (Rect?)null;
            Line("resource bar", res);
            Rect? ariaRect = aria != null ? ScreenRect((RectTransform)aria.transform, height, camera) : (Rect?)null;
            Line("ARIA panel", ariaRect);

            // Board 2: one compact card -----------------------------------------------------------
            var bars = new List<string>();
            foreach (Transform child in root)
                if (child.gameObject.activeInHierarchy && child != card && (child.name == "FirstOrderPrompt" || child.name == "BaseObjectives")) bars.Add(child.name);
            var extra = root.Find("FirstOrderPrompt");
            if (extra != null && extra.gameObject.activeInHierarchy) Line("second centre bar (FirstOrderPrompt)", ScreenRect((RectTransform)extra, height, null));
            Rule(bars.Count == 0, $"one objective card, no stacked centre bar (extra: [{string.Join(", ", bars)}])");
            if (res.HasValue) Rule(Mathf.Abs(cardRect.yMin - res.Value.yMax - 12) <= Tolerance, $"card 12 px below the resource bar (is {cardRect.yMin - res.Value.yMax:0.0})");
            Rule(cardRect.xMin >= 0 && cardRect.xMax <= width && cardRect.yMin >= 0, "card inside the safe area");
            if (ariaRect.HasValue) Rule(cardRect.xMax <= ariaRect.Value.xMin - 8, $"card clear of the ARIA panel (card right {cardRect.xMax:0.0}, ARIA left {ariaRect.Value.xMin:0.0})");
            var keyline = card != null ? card.GetComponent<V3GradientGraphic>() : null;
            Color border = keyline != null ? (Color)Field(keyline, "borderColor") : Color.clear; float stroke = keyline != null ? (float)Field(keyline, "borderWidth") : 0;
            text.AppendLine($"- card keyline: #{ColorUtility.ToHtmlStringRGB(border)} {stroke:0.#} px");
            Rule(Near(border, Hex("#E7562C")) && stroke >= 2 && stroke <= 3, "warm 2-3 px keyline #E7562C");
            var header = card != null ? card.Find("ObjectiveHeader") as RectTransform : null;
            var status = card != null ? card.Find("ObjectiveStatus") as RectTransform : null;
            Rect? head = header != null && header.gameObject.activeInHierarchy ? ScreenRect(header, height, null) : (Rect?)null;
            Rect? row = status != null && status.gameObject.activeInHierarchy ? ScreenRect(status, height, null) : (Rect?)null;
            Line("purpose / next action row", head); Line("status row", row);
            Rule(head.HasValue && row.HasValue && head.Value.yMax <= row.Value.yMin + .5f, "purpose and next action first, then status");
            if (head.HasValue && row.HasValue)
            {
                Rule(Mathf.Abs(head.Value.yMin - cardRect.yMin - 16) <= Tolerance && Mathf.Abs(head.Value.xMin - cardRect.xMin - 16) <= Tolerance &&
                     Mathf.Abs(cardRect.xMax - head.Value.xMax - 16) <= Tolerance && Mathf.Abs(cardRect.yMax - row.Value.yMax - 16) <= Tolerance,
                    $"16 px card padding (top {head.Value.yMin - cardRect.yMin:0.0}, left {head.Value.xMin - cardRect.xMin:0.0}, right {cardRect.xMax - head.Value.xMax:0.0}, bottom {cardRect.yMax - row.Value.yMax:0.0})");
                var purpose = Text(header, "ObjectivePurpose"); var next = Text(header, "ObjectiveNextAction"); var timer = Text(header, "MatchClockValue");
                text.AppendLine($"- purpose \"{purpose}\"; next action \"{next}\"; timer \"{timer}\"");
                Rule(!string.IsNullOrWhiteSpace(purpose) && !string.IsNullOrWhiteSpace(next), "purpose and next action shown");
                Rule(!string.IsNullOrWhiteSpace(timer), "timer shown in the card");
            }
            if (state == "s002-ba" && width == 1920 && GameLocalization.CurrentLocaleCode == GameLocalization.EnglishLocaleCode)
                text.AppendLine($"- BA English card size: {cardRect.width:0.0} x {cardRect.height:0.0} (board target about 560 x 106)");
            if (state == "s002-ba") Rule(cardRect.width >= 560 - Tolerance, $"BA card at least the 560 px target width (is {cardRect.width:0.0})");

            // Controls and text fit -----------------------------------------------------------------
            var buttons = card != null ? card.GetComponentsInChildren<Button>(false).Where(b => b.gameObject.activeInHierarchy).ToList() : new List<Button>();
            int gold = 0;
            foreach (var b in buttons)
            {
                var r = ScreenRect((RectTransform)b.transform, height, null);
                string caption = Caption(b.GetComponentInChildren<TMP_Text>(false));
                Line($"control {b.name} \"{caption}\"", r);
                Rule(r.height >= 64 - .5f, $"control {b.name} target >= 64 px tall (is {r.height:0.0})");
                Rule(r.xMin >= cardRect.xMin - .5f && r.xMax <= cardRect.xMax + .5f && r.yMin >= cardRect.yMin - .5f && r.yMax <= cardRect.yMax + .5f, $"control {b.name} inside the card");
                var surface = b.transform.Find("HudRoleSurface")?.GetComponent<V3GradientGraphic>();
                if (surface != null && Near((Color)Field(surface, "borderColor"), new Color(1, .7f, .05f))) gold++;
            }
            Rule(gold <= 1, $"at most one gold camera action (is {gold})");
            if (card != null)
                foreach (var label in card.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (!label.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(label.text)) continue;
                    label.ForceMeshUpdate(true, true);
                    var frame = label.GetComponentInParent<Button>() != null ? (RectTransform)label.GetComponentInParent<Button>().transform : card;
                    var frameRect = ScreenRect(frame, height, null);
                    var bounds = label.textBounds;
                    var a = RectTransformUtility.WorldToScreenPoint(null, label.rectTransform.TransformPoint(bounds.min));
                    var c = RectTransformUtility.WorldToScreenPoint(null, label.rectTransform.TransformPoint(bounds.max));
                    var drawn = Rect.MinMaxRect(Mathf.Min(a.x, c.x), height - Mathf.Max(a.y, c.y), Mathf.Max(a.x, c.x), height - Mathf.Min(a.y, c.y));
                    int lines = label.textInfo.lineCount, authored = Caption(label).Split('\n').Length;
                    string what = $"text \"{Caption(label).Replace("\n", " / ")}\" in {frame.name}";
                    text.AppendLine($"- {what}: drawn x={drawn.xMin:0.0}-{drawn.xMax:0.0} y={drawn.yMin:0.0}-{drawn.yMax:0.0} lines={lines} size={label.fontSize:0.#}");
                    Rule(drawn.xMin >= frameRect.xMin - .5f && drawn.xMax <= frameRect.xMax + .5f && drawn.yMin >= frameRect.yMin - .5f && drawn.yMax <= frameRect.yMax + .5f,
                        $"{what} stays inside its frame");
                    if (frame != card) Rule(lines <= authored, $"{what} does not wrap (lines {lines}, authored {authored})");
                }

            // Board 4: ownership ------------------------------------------------------------------------
            var focus = new List<Button> { view.PlayerFocusButton };
            var third = buttons.FirstOrDefault(b => b != view.PlayerFocusButton && b != view.EnemyFocusButton);
            if (state.Contains("-fc")) { focus.Add(view.EnemyFocusButton); focus.Add(third); }
            else focus.Add(view.EnemyFocusButton);
            for (int i = 0; i < expectedOwners.Count && i < focus.Count; i++)
            {
                if (expectedOwners[i] < 0 || focus[i] == null) continue;
                // Fixture order: BA [own base, enemy base]; FC goal nodes A, B, C -> player, enemy, third control.
                int owner = expectedOwners[i];
                string expectedShape = owner == 1 ? "Shield" : owner == 2 ? "Diamond" : "Ring";
                Color expectedColour = owner == 1 ? Hex("#49BD62") : owner == 2 ? Hex("#F45C3B") : Hex("#BBC7CD");
                var glyph = focus[i].GetComponentsInChildren<Graphic>(false).FirstOrDefault(g => g.GetType().Name == "HudOwnershipGlyphGraphic");
                string shape = glyph != null ? glyph.GetType().GetProperty("Shape")?.GetValue(glyph)?.ToString() : "none";
                var surface = focus[i].transform.Find("HudRoleSurface")?.GetComponent<V3GradientGraphic>();
                Color edge = surface != null ? (Color)Field(surface, "borderColor") : Color.clear;
                text.AppendLine($"- ownership {focus[i].name}: owner={owner} glyph={shape} keyline=#{ColorUtility.ToHtmlStringRGB(edge)}");
                Rule(shape == expectedShape, $"{Caption(focus[i].GetComponentInChildren<TMP_Text>())} glyph {expectedShape} (is {shape})");
                Rule(Near(edge, expectedColour), $"{Caption(focus[i].GetComponentInChildren<TMP_Text>())} keyline #{ColorUtility.ToHtmlStringRGB(expectedColour)} (is #{ColorUtility.ToHtmlStringRGB(edge)})");
            }
            if (state.Contains("-ba"))
            {
                var marker = root.Find("EnemyBaseObjectiveMarker");
                if (marker != null && marker.gameObject.activeInHierarchy)
                {
                    Line("in-world enemy base chip", ScreenRect((RectTransform)marker, height, null));
                    var glyph = marker.GetComponentsInChildren<Graphic>(false).FirstOrDefault(g => g.GetType().Name == "HudOwnershipGlyphGraphic");
                    string shape = glyph != null ? glyph.GetType().GetProperty("Shape")?.GetValue(glyph)?.ToString() : "none";
                    var chip = marker.Find("Chip")?.GetComponent<V3GradientGraphic>();
                    Color edge = chip != null ? (Color)Field(chip, "borderColor") : Color.clear;
                    text.AppendLine($"- in-world enemy base: glyph={shape} keyline=#{ColorUtility.ToHtmlStringRGB(edge)}");
                    Rule(shape == "Diamond", $"in-world enemy base glyph Diamond (is {shape})");
                    Rule(Near(new Color(edge.r, edge.g, edge.b, 1), Hex("#F45C3B")), $"in-world enemy base keyline #F45C3B (is #{ColorUtility.ToHtmlStringRGB(edge)})");
                }
                else Rule(false, "in-world enemy base chip shown");
            }
            return text.ToString();
        }

        private static string Text(Transform root, string name)
        {
            var label = Find(root, name)?.GetComponent<TMP_Text>();
            return label != null && label.gameObject.activeInHierarchy ? Caption(label) : null;
        }

        private static string Caption(TMP_Text label) => label == null ? "" : label is RTLTMPro.RTLTextMeshPro rtl ? rtl.OriginalText : label.text;

        private static Rect ScreenRect(RectTransform rect, int height, Camera eventCamera)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 low = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 high = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);
            return Rect.MinMaxRect(low.x, height - high.y, high.x, height - low.y);
        }
    }

    private static object Field(object target, string name) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(target);

    private static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var colour); return colour; }
    private static bool Near(Color a, Color b) => Mathf.Abs(a.r - b.r) < .02f && Mathf.Abs(a.g - b.g) < .02f && Mathf.Abs(a.b - b.b) < .02f;

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
        return null;
    }

    private static async Task Until(Func<bool> condition)
    {
        double end = EditorApplication.timeSinceStartup + 120;
        while (!condition()) { if (EditorApplication.timeSinceStartup > end) throw new TimeoutException("W40 fixture timed out"); await Task.Delay(100); }
    }
}
