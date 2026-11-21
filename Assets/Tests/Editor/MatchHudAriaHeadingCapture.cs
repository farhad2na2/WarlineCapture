using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using TMPro;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// W42 native fixture: the ARIA panel heading of the shipped SCN08 HUD for one Base Assault, Frontline
// Control, Breakthrough and Convoy Escort mission (S002, S031, S037, S020, seed 130365), EN and FA at
// 1920x1080. The heading comes from the shipped read model (UiShellEcsGateway.TryReadMatchHudAssistantPanel)
// over an isolated session world that holds each mission's real resolved setup; nothing in the fixture
// chooses the heading. Presentation only; it never claims mission readiness.
// Launch through Tools/CI/InvokeUnity.ps1 (no -quit); it exits the Editor itself.
public static class MatchHudAriaHeadingCapture
{
    private const string Prefab = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string Root = "Design/AgentReports/MatchUiPolish/Implemented/W42/";
    private const int Seed = 130365, Margin = 24;
    private static readonly (string id, string state, SkirmishObjectiveKind kind)[] Missions =
    {
        ("S002", "s002-ba", SkirmishObjectiveKind.BaseAssault),
        ("S031", "s031-fc", SkirmishObjectiveKind.FrontlineControl),
        ("S037", "s037-bt", SkirmishObjectiveKind.Breakthrough),
        ("S020", "s020-ce", SkirmishObjectiveKind.ConvoyEscort),
    };

    public static void RunBefore() => Launch("before", false);
    public static void RunAfter() => Launch("after", true);

    private static async void Launch(string phase, bool enforce)
    {
        int code = 1;
        try { code = await Run(phase, enforce); }
        catch (Exception error) { Debug.LogException(error); Debug.LogError("[W42AriaHeading] result=Failed phase=" + phase); }
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
        var crops = new Dictionary<string, Rect>();
        bool passed = false;
        int expected = Missions.Length * 2;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = options | EnterPlayModeOptions.DisableDomainReload;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            EditorApplication.EnterPlaymode(); await Until(() => EditorApplication.isPlaying);
            Application.runInBackground = true;
            var camera = new GameObject("W42Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.16f, .19f, .2f, 1);
            camera.orthographic = true; camera.orthographicSize = 540; camera.depth = 100; camera.transform.position = new Vector3(0, 0, -10);
            var canvas = new GameObject("W42Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder = 30000;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (other.rootCanvas != canvas) other.gameObject.SetActive(false);
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(4800, 2160); scaler.matchWidthOrHeight = .5f;
            var hud = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), canvas.transform, false);
            var rect = (RectTransform)hud.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var selection = hud.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            if (selection != null) selection.HideSelection();
            var aria = hud.GetComponentsInChildren<AriaTutorialBriefingView>(true).First(v => v.name == "AriaAssistantButton");
            var gateway = new W40ObjectiveCardGateway();
            UiShellRuntimeGateway.Register(gateway);
            foreach (var language in new[] { GameLocalization.EnglishLocaleCode, GameLocalization.PersianLocaleCode })
            {
                GameLocalization.SetLocale(language, false); MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080); await Task.Delay(700);
                foreach (var mission in Missions)
                {
                    if (!SkirmishSetupBriefing.TryLoad(mission.id, Seed, out var setup) || setup == null)
                        throw new InvalidOperationException("No resolved setup for " + mission.id);
                    gateway.Match = new UiSkirmishModel { Expanded = true, PlayerHealth = 800, EnemyHealth = 800, CatalogId = mission.id };
                    var panel = ReadPanel(mission.id, setup);
                    aria.Apply(panel);
                    aria.ApplyAccessibility(false, false);
                    aria.SetPresentationVisible(true);
                    var alert = Find(aria.transform, "AlertCue")?.GetComponent<TMP_Text>();
                    if (alert != null) { alert.text = string.Empty; alert.gameObject.SetActive(false); }
                    for (int i = 0; i < 10; i++)
                    {
                        aria.PresentWatch(new AriaPlayModel(AriaPlayPhase.Observing, Vector2.zero, false, 1), true);
                        aria.RefreshContentLayout(); await Task.Delay(60);
                    }
                    Canvas.ForceUpdateCanvases();
                    if (Screen.width != 1920 || Screen.height != 1080)
                        throw new InvalidOperationException($"Game view is {Screen.width}x{Screen.height}, expected 1920x1080");
                    string name = mission.state + "-" + language + "-1920";
                    report.AppendLine("## " + name).AppendLine(Measure(aria, camera, mission, setup, panel, language, failures, name, out var crop)).AppendLine();
                    crops[name] = crop;
                    await Task.Yield();
                    ScreenCapture.CaptureScreenshot(output + "/" + name + ".png"); await Task.Delay(600);
                }
            }
            await Until(() => Directory.GetFiles(output, "*.png").Length == expected && Directory.GetFiles(output, "*.png").All(p => new FileInfo(p).Length > 0));
            await Task.Delay(1000);
            foreach (var pair in crops) Crop(output + "/" + pair.Key + ".png", pair.Value);
            File.WriteAllText(output + "/geometry.md", "# W42 " + phase + " ARIA heading (screen px, top-left origin, 1920x1080)\n\n" +
                "The heading is read from the shipped read model over an isolated session holding each mission's resolved setup (seed " + Seed + ").\n" +
                "These are native-view renders, not live mission frames. Each PNG is the ARIA panel plus a " + Margin + " px margin.\n\n" + report +
                "\n## Rule check\n\n" + (failures.Count == 0 ? "All W42 heading rules passed.\n" : string.Join("\n", failures.Select(f => "- " + f)) + "\n"));
            foreach (var failure in failures) Debug.Log("[W42AriaHeading] rule " + failure);
            passed = !enforce || failures.Count == 0;
            Debug.Log("[W42AriaHeading] " + (passed ? "result=Passed" : "rules=Failed") + " phase=" + phase + " captures=" + expected + " ruleFailures=" + failures.Count +
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

    // The production Skirmish ARIA read model over an isolated playing session with the mission's setup.
    private static UiAssistantPanelModel ReadPanel(string id, SkirmishResolvedSetup setup)
    {
        using var world = new World("W42AriaHeading." + id);
        var em = world.EntityManager;
        var session = em.CreateEntity(typeof(SkirmishMatchState));
        em.SetComponentData(session, new SkirmishMatchState { Phase = SkirmishPhase.Playing });
        em.AddComponentData(session, new SkirmishExpandedSessionComponent { CatalogId = id, ObjectiveKind = setup.ObjectiveKind });
        em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });
        var previous = World.DefaultGameObjectInjectionWorld;
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            if (!UiShellEcsGateway.TryReadMatchHudAssistantPanel(out var panel)) throw new InvalidOperationException("No ARIA panel model for " + id);
            return panel;
        }
        finally { World.DefaultGameObjectInjectionWorld = previous; }
    }

    private static string Measure(AriaTutorialBriefingView aria, Camera camera, (string id, string state, SkirmishObjectiveKind kind) mission,
        SkirmishResolvedSetup setup, UiAssistantPanelModel panel, string language, List<string> failures, string name, out Rect crop)
    {
        const int height = 1080, width = 1920;
        var text = new StringBuilder();
        void Line(string label, Rect r) => text.AppendLine($"- {label}: x={r.xMin:0.0} y={r.yMin:0.0} w={r.width:0.0} h={r.height:0.0} right={r.xMax:0.0} bottom={r.yMax:0.0}");
        void Rule(bool ok, string rule) { text.AppendLine($"  - {(ok ? "PASS" : "FAIL")} {rule}"); if (!ok) failures.Add(name + ": " + rule); }
        bool fa = language == GameLocalization.PersianLocaleCode;
        string family = Family(mission.kind, fa);
        text.AppendLine($"- mission {mission.id}: resolved objective {setup.ObjectiveKind}");
        Rule(setup.ObjectiveKind == mission.kind, $"{mission.id} resolves to {mission.kind}");
        Rect panelRect = ScreenRect((RectTransform)aria.transform, height, camera);
        Line("ARIA panel", panelRect);
        Rule(panelRect.xMin >= 0 && panelRect.yMin >= 0 && panelRect.xMax <= width && panelRect.yMax <= height, "ARIA panel inside the 1920x1080 safe area");
        var title = aria.TitleText;
        string shown = Caption(title);
        text.AppendLine($"- read model heading \"{panel.RecommendationTitle}\"; shown heading \"{shown}\"; expected \"{family}\"");
        Rule(title != null && title.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(shown), "heading shown");
        Rule(shown == family, $"heading names the mission's objective family \"{family}\" (is \"{shown}\")");
        Rect titleRect = title != null ? ScreenRect(title.rectTransform, height, camera) : default;
        if (title != null)
        {
            title.ForceMeshUpdate(true, true);
            var bounds = title.textBounds;
            var a = RectTransformUtility.WorldToScreenPoint(camera, title.rectTransform.TransformPoint(bounds.min));
            var c = RectTransformUtility.WorldToScreenPoint(camera, title.rectTransform.TransformPoint(bounds.max));
            var drawn = Rect.MinMaxRect(Mathf.Min(a.x, c.x), height - Mathf.Max(a.y, c.y), Mathf.Max(a.x, c.x), height - Mathf.Min(a.y, c.y));
            Line("heading frame", titleRect);
            text.AppendLine($"- heading drawn: x={drawn.xMin:0.0}-{drawn.xMax:0.0} y={drawn.yMin:0.0}-{drawn.yMax:0.0} lines={title.textInfo.lineCount} size={title.fontSize:0.#} rtl={title.isRightToLeftText} truncated={title.isTextTruncated}");
            Rule(drawn.xMin >= panelRect.xMin - .5f && drawn.xMax <= panelRect.xMax + .5f && drawn.yMin >= panelRect.yMin - .5f && drawn.yMax <= panelRect.yMax + .5f,
                "heading drawn inside the ARIA panel");
            Rule(drawn.xMin >= titleRect.xMin - .5f && drawn.xMax <= titleRect.xMax + .5f, "heading drawn inside its own frame width (no horizontal clipping)");
            Rule(title.textInfo.lineCount <= 1, $"heading on one line (lines {title.textInfo.lineCount})");
            Rule(!title.isTextTruncated, "heading not truncated");
        }
        foreach (var button in aria.GetComponentsInChildren<Button>(false).Where(b => b.gameObject.activeInHierarchy))
        {
            var r = ScreenRect((RectTransform)button.transform, height, camera);
            Line($"control {button.name}", r);
            Rule(r.height >= 64 - .5f && r.width >= 64 - .5f, $"control {button.name} target >= 64 px (is {r.width:0.0} x {r.height:0.0})");
            Rule(r.xMin >= 0 && r.yMin >= 0 && r.xMax <= width && r.yMax <= height, $"control {button.name} inside the safe area");
        }
        crop = Rect.MinMaxRect(Mathf.Max(0, panelRect.xMin - Margin), Mathf.Max(0, panelRect.yMin - Margin),
            Mathf.Min(width, panelRect.xMax + Margin), Mathf.Min(height, panelRect.yMax + Margin));
        return text.ToString();
    }

    // Expected copy: the localized Base Assault catalog title, otherwise the Battle Library family names.
    private static string Family(SkirmishObjectiveKind kind, bool fa) => kind switch
    {
        SkirmishObjectiveKind.FrontlineControl => fa ? "کنترل خط" : "FRONTLINE CONTROL",
        SkirmishObjectiveKind.Breakthrough => fa ? "شکستن خط" : "BREAKTHROUGH",
        SkirmishObjectiveKind.ConvoyEscort => fa ? "اسکورت کاروان" : "CONVOY ESCORT",
        _ => GameText.Get("ui.skirmish.base_assault", "BASE ASSAULT")
    };

    private static void Crop(string path, Rect topDown)
    {
        var frame = new Texture2D(2, 2, TextureFormat.RGB24, false);
        try
        {
            if (!frame.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("Unreadable capture " + path);
            int x = Mathf.Clamp(Mathf.FloorToInt(topDown.xMin), 0, frame.width - 1);
            int w = Mathf.Clamp(Mathf.CeilToInt(topDown.xMax) - x, 1, frame.width - x);
            int yTop = Mathf.Clamp(Mathf.FloorToInt(topDown.yMin), 0, frame.height - 1);
            int h = Mathf.Clamp(Mathf.CeilToInt(topDown.yMax) - yTop, 1, frame.height - yTop);
            var band = new Texture2D(w, h, TextureFormat.RGB24, false);
            band.SetPixels(frame.GetPixels(x, frame.height - yTop - h, w, h)); band.Apply();
            File.WriteAllBytes(path, band.EncodeToPNG());
            UnityEngine.Object.Destroy(band);
        }
        finally { UnityEngine.Object.Destroy(frame); }
    }

    private static string Caption(TMP_Text label) => label == null ? "" : label is RTLTMPro.RTLTextMeshPro rtl ? rtl.OriginalText : label.text;

    private static Rect ScreenRect(RectTransform rect, int height, Camera eventCamera)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Vector2 low = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
        Vector2 high = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);
        return Rect.MinMaxRect(low.x, height - high.y, high.x, height - low.y);
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
        return null;
    }

    private static async Task Until(Func<bool> condition)
    {
        double end = EditorApplication.timeSinceStartup + 120;
        while (!condition()) { if (EditorApplication.timeSinceStartup > end) throw new TimeoutException("W42 fixture timed out"); await Task.Delay(100); }
    }
}
