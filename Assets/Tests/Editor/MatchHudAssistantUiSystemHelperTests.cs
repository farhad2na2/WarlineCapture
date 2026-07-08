using System;
using System.Collections.Generic;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHudAssistantUiSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.BindMatchHudAssistant_CreatesButtonPanelAndBlocksWorldClicks());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_AppliesAssistantPanelReadModel());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_VisualSurfaceKeepsControlsReadableInsideOverlay());
            passed++;

            Debug.Log($"[MatchHudAssistantUiValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[MatchHudAssistantUiValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<MatchHudAssistantUiSystemHelperTests> testCase)
    {
        var tests = new MatchHudAssistantUiSystemHelperTests();
        try
        {
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        UiShellRuntimeGateway.Register(null);
        GameObject[] roots = GameObject.FindGameObjectsWithTag("Untagged");
        for (int i = roots.Length - 1; i >= 0; i--)
        {
            if (roots[i] != null && roots[i].name.StartsWith("AssistantUiTest", StringComparison.Ordinal))
                UnityEngine.Object.DestroyImmediate(roots[i]);
            else if (roots[i] != null && roots[i].name.StartsWith("AriaAssistantPreviewHighlightRuntime", StringComparison.Ordinal))
                UnityEngine.Object.DestroyImmediate(roots[i]);
        }
    }

    [Test]
    public void BindMatchHudAssistant_CreatesButtonPanelAndBlocksWorldClicks()
    {
        RectTransform overlay = CreateRectRoot("AssistantUiTestOverlay", new Vector2(1920f, 1080f));
        RectTransform header = CreateRect("HeaderContent", overlay);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(0f, 1f);
        header.pivot = new Vector2(0f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(1920f, 160f);

        var runtimeState = new FakeMatchRuntimeState();
        var ui = new MainMenuPlayUI();
        ui.Init(null, runtimeState);
        ui.BindMatchHudAssistant(header.gameObject, overlay);

        RectTransform button = header.Find("AriaAssistantButton") as RectTransform;
        RectTransform panel = overlay.Find("AriaAssistantPanel") as RectTransform;
        Assert.NotNull(button, "Binding the match HUD assistant must add the ARIA header button.");
        Assert.NotNull(panel, "Binding the match HUD assistant must add the ARIA panel shell.");
        Assert.IsFalse(panel.gameObject.activeSelf, "The ARIA panel should start closed.");

        Button buttonComponent = button.GetComponent<Button>();
        Assert.NotNull(buttonComponent, "The ARIA header button must be a Unity Button.");
        buttonComponent.onClick.Invoke();

        Assert.IsTrue(panel.gameObject.activeSelf, "Clicking ARIA should open the panel shell.");
        Assert.IsTrue(runtimeState.SuppressNextWorldClick, "ARIA UI clicks must suppress the next world click.");

        Vector2 buttonPoint = CenterScreenPoint(button);
        Assert.IsTrue(ui.IsPointerOverAnyGameplayUi(buttonPoint, out string source));
        Assert.AreEqual("MatchHudAssistant", source);

        ui.Dispose();
        UnityEngine.Object.DestroyImmediate(overlay.gameObject);
    }

    [Test]
    public void BindMatchHudAssistant_AppliesAssistantPanelReadModel()
    {
        RectTransform overlay = CreateRectRoot("AssistantUiTestOverlay", new Vector2(1920f, 1080f));
        RectTransform header = CreateRect("HeaderContent", overlay);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(0f, 1f);
        header.pivot = new Vector2(0f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(1920f, 160f);

        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay);
        var assistantGateway = new FakeAssistantPanelGateway(
            new UiAssistantPanelModel(
            42,
            "- Neutralize hostile patrol\n[x] Protect civilians",
            "HIGH: Fuel reserves empty",
            "HIGH: Hostile patrol near base",
            true,
            true,
            true,
            "Review objective",
            "Focus the active objective before choosing the next order.",
            "HIGH",
            "SHOW ME",
            true,
            true,
            true,
            false,
            "ARIA CONTROL",
            "ARIA is executing a bounded action. STOP returns control."),
            new UiAssistantHighlightModel(88, true, 7, 3101, 1, 12f, 3f, 9f, 1f));
        UiShellRuntimeGateway.Register(assistantGateway);

        ui.Update();

        RectTransform panel = overlay.Find("AriaAssistantPanel") as RectTransform;
        Assert.NotNull(panel);
        TMP_Text goals = panel.Find("GoalsBody")?.GetComponent<TMP_Text>();
        TMP_Text alerts = panel.Find("AlertsBody")?.GetComponent<TMP_Text>();
        TMP_Text narration = panel.Find("NarrationSubtitle")?.GetComponent<TMP_Text>();
        TMP_Text ownership = panel.Find("OwnershipBody")?.GetComponent<TMP_Text>();
        TMP_Text recommendation = panel.Find("RecommendationBody")?.GetComponent<TMP_Text>();
        Image previewPulse = panel.Find("PreviewPulse")?.GetComponent<Image>();
        Button showMe = panel.Find("NextActionButton")?.GetComponent<Button>();
        Button giveControl = panel.Find("GiveControlButton")?.GetComponent<Button>();
        Button stop = panel.Find("StopButton")?.GetComponent<Button>();
        TMP_Text giveControlLabel = panel.Find("GiveControlButton/Label")?.GetComponent<TMP_Text>();

        Assert.NotNull(goals);
        Assert.NotNull(alerts);
        Assert.NotNull(narration);
        Assert.NotNull(ownership);
        Assert.NotNull(recommendation);
        Assert.NotNull(previewPulse);
        Assert.NotNull(showMe);
        Assert.NotNull(giveControl);
        Assert.NotNull(stop);
        Assert.NotNull(giveControlLabel);
        Assert.AreEqual("- Neutralize hostile patrol\n[x] Protect civilians", goals.text);
        Assert.AreEqual("HIGH: Fuel reserves empty", alerts.text);
        Assert.AreEqual("HIGH: Hostile patrol near base", narration.text);
        Assert.AreEqual("ARIA is executing a bounded action. STOP returns control.", ownership.text);
        StringAssert.Contains("HIGH: Review objective", recommendation.text);
        Assert.IsTrue(previewPulse.gameObject.activeSelf);
        Assert.Greater(previewPulse.color.a, 0.4f);
        GameObject worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.NotNull(worldRing);
        Assert.IsTrue(worldRing.activeSelf);
        LineRenderer worldRingRenderer = worldRing.GetComponent<LineRenderer>();
        Assert.NotNull(worldRingRenderer);
        Assert.AreEqual(96, worldRingRenderer.positionCount);
        Assert.AreEqual(12f + 2.35f, worldRingRenderer.GetPosition(0).x, 0.01f);
        Assert.AreEqual(3f + 0.38f, worldRingRenderer.GetPosition(0).y, 0.01f);
        Assert.AreEqual(9f, worldRingRenderer.GetPosition(0).z, 0.01f);
        Assert.IsTrue(showMe.interactable);
        Assert.IsTrue(giveControl.interactable);
        Assert.IsTrue(stop.interactable);
        Assert.AreEqual("DO IT", giveControlLabel.text);

        showMe.onClick.Invoke();

        Assert.AreEqual(1, assistantGateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.ShowRecommendation, assistantGateway.LastAssistantIntentKind);
        Assert.IsFalse(assistantGateway.LastAssistantIntentFromTakeover);

        giveControl.onClick.Invoke();

        Assert.AreEqual(2, assistantGateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.ExecuteRecommendation, assistantGateway.LastAssistantIntentKind);
        Assert.IsTrue(assistantGateway.LastAssistantIntentFromTakeover);

        stop.onClick.Invoke();

        Assert.AreEqual(3, assistantGateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.StopAssistantControl, assistantGateway.LastAssistantIntentKind);
        Assert.IsFalse(assistantGateway.LastAssistantIntentFromTakeover);

        assistantGateway.AssistantPanel = new UiAssistantPanelModel(
            43,
            "- Neutralize hostile patrol\n[x] Protect civilians",
            "HIGH: Fuel reserves empty",
            "HIGH: Hidden narration",
            false,
            true,
            true,
            "Review objective",
            "Focus the active objective before choosing the next order.",
            "HIGH",
            "SHOW ME",
            true,
            true,
            true,
            false,
            "ARIA CONTROL",
            "ARIA is executing a bounded action. STOP returns control.");
        ui.Update();

        Assert.IsFalse(narration.gameObject.activeSelf, "Assistant subtitles should hide when the setting is disabled.");
        Assert.AreEqual("HIGH: Fuel reserves empty", alerts.text, "Critical text alerts must remain visible when narration subtitles are hidden.");

        ui.Dispose();
        UnityEngine.Object.DestroyImmediate(overlay.gameObject);
    }

    [Test]
    public void BindMatchHudAssistant_VisualSurfaceKeepsControlsReadableInsideOverlay()
    {
        RectTransform overlay = CreateRectRoot("AssistantUiTestOverlay", new Vector2(1920f, 1080f));
        RectTransform header = CreateRect("HeaderContent", overlay);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(0f, 1f);
        header.pivot = new Vector2(0f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(1920f, 160f);

        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay);
        var assistantGateway = new FakeAssistantPanelGateway(
            new UiAssistantPanelModel(
            101,
            "- Neutralize hostile patrol\n[x] Protect civilians",
            "HIGH: Hostile patrol near base\nNORMAL: Fuel convoy ready",
            "HIGH: Hostile patrol near base",
            true,
            true,
            true,
            "Assign attack order",
            "Focus the visible hostile target, then execute the recommended attack order.",
            "HIGH",
            "SHOW ME",
            true,
            true,
            true,
            false,
            "ARIA CONTROL",
            "ARIA can execute one bounded tactical order. STOP returns control."),
            UiAssistantHighlightModel.Empty);
        UiShellRuntimeGateway.Register(assistantGateway);

        RectTransform button = header.Find("AriaAssistantButton") as RectTransform;
        Assert.NotNull(button);
        button.GetComponent<Button>().onClick.Invoke();
        ui.Update();

        RectTransform panel = overlay.Find("AriaAssistantPanel") as RectTransform;
        Assert.NotNull(panel);
        Assert.IsTrue(panel.gameObject.activeSelf, "The ARIA panel must be visible after clicking the header button.");

        AssertRectSize(button, new Vector2(228f, 78f), "ARIA header button");
        AssertRectSize(panel, new Vector2(640f, 590f), "ARIA assistant panel");
        AssertRectContained(panel, RequireChild(panel, "NextActionButton"), "Show Me button");
        AssertRectContained(panel, RequireChild(panel, "GiveControlButton"), "Do It button");
        AssertRectContained(panel, RequireChild(panel, "StopButton"), "Stop button");
        AssertRectContained(panel, RequireChild(panel, "NarrationSubtitle"), "Narration subtitle");

        AssertTextReadable(panel, "Title", "ARIA COMMAND ASSISTANT", 24f);
        AssertTextReadable(panel, "GoalsBody", "Neutralize hostile patrol", 18f);
        AssertTextReadable(panel, "AlertsBody", "Hostile patrol", 16f);
        AssertTextReadable(panel, "NarrationSubtitle", "HIGH: Hostile patrol near base", 14f);
        AssertTextReadable(panel, "RecommendationBody", "Focus the visible hostile target", 18f);
        AssertTextReadable(panel, "NextActionButton/Label", "SHOW ME", 16f);
        AssertTextReadable(panel, "GiveControlButton/Label", "DO IT", 16f);
        AssertTextReadable(panel, "StopButton/Label", "STOP", 16f);

        AssertNoOverlap(RequireChild(panel, "NextActionButton"), RequireChild(panel, "GiveControlButton"), "Show Me and Do It buttons");
        AssertNoOverlap(RequireChild(panel, "GiveControlButton"), RequireChild(panel, "CloseButton"), "Do It and Close buttons");
        AssertNoOverlap(RequireChild(panel, "GiveControlButton"), RequireChild(panel, "StopButton"), "Do It and Stop buttons");
        AssertNoOverlap(RequireChild(panel, "RecommendationBody"), RequireChild(panel, "NextActionButton"), "Recommendation text and command buttons");

        ui.Dispose();
        UnityEngine.Object.DestroyImmediate(overlay.gameObject);
    }

    private static RectTransform CreateRectRoot(string name, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        return root.GetComponent<RectTransform>();
    }

    private static Vector2 CenterScreenPoint(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(null, center);
    }

    private static RectTransform RequireChild(RectTransform parent, string path)
    {
        RectTransform child = parent.Find(path) as RectTransform;
        Assert.NotNull(child, $"{path} must exist under {parent.name}.");
        Assert.IsTrue(child.gameObject.activeInHierarchy, $"{path} must be visible.");
        return child;
    }

    private static void AssertTextReadable(RectTransform parent, string path, string expectedText, float minimumFontSize)
    {
        RectTransform rect = RequireChild(parent, path);
        TMP_Text text = rect.GetComponent<TMP_Text>();
        Assert.NotNull(text, $"{path} must have TMP text.");
        StringAssert.Contains(expectedText, text.text, $"{path} must show the expected assistant text.");
        Assert.GreaterOrEqual(text.fontSize, minimumFontSize, $"{path} font is too small for the match HUD.");
        Assert.Greater(rect.rect.width, 40f, $"{path} must have visible width.");
        Assert.Greater(rect.rect.height, 20f, $"{path} must have visible height.");
    }

    private static void AssertRectContained(RectTransform container, RectTransform child, string label)
    {
        Rect containerRect = WorldRect(container);
        Rect childRect = WorldRect(child);
        Assert.GreaterOrEqual(childRect.xMin, containerRect.xMin - 0.5f, $"{label} leaks left of its container.");
        Assert.LessOrEqual(childRect.xMax, containerRect.xMax + 0.5f, $"{label} leaks right of its container.");
        Assert.GreaterOrEqual(childRect.yMin, containerRect.yMin - 0.5f, $"{label} leaks below its container.");
        Assert.LessOrEqual(childRect.yMax, containerRect.yMax + 0.5f, $"{label} leaks above its container.");
    }

    private static void AssertRectSize(RectTransform rectTransform, Vector2 expectedSize, string label)
    {
        Assert.AreEqual(expectedSize.x, rectTransform.rect.width, 0.1f, $"{label} width drifted.");
        Assert.AreEqual(expectedSize.y, rectTransform.rect.height, 0.1f, $"{label} height drifted.");
    }

    private static void AssertNoOverlap(RectTransform first, RectTransform second, string label)
    {
        Rect firstRect = WorldRect(first);
        Rect secondRect = WorldRect(second);
        Assert.IsFalse(firstRect.Overlaps(secondRect), $"{label} should not overlap.");
    }

    private static Rect WorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float xMin = Mathf.Min(corners[0].x, corners[2].x);
        float xMax = Mathf.Max(corners[0].x, corners[2].x);
        float yMin = Mathf.Min(corners[0].y, corners[2].y);
        float yMax = Mathf.Max(corners[0].y, corners[2].y);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private sealed class FakeMatchRuntimeState : IMatchRuntimeState
    {
        public bool PlayRequested { get; set; }
        public bool SimulationActive { get; set; }
        public bool SelectionModeActive { get; set; }
        public bool BuildModeActive { get; set; }
        public bool ZoomInHeld { get; set; }
        public bool ZoomOutHeld { get; set; }
        public bool SuppressNextWorldClick { get; set; }
    }

    private sealed class FakeAssistantPanelGateway : IUiShellRuntimeGateway
    {
        private readonly UiAssistantHighlightModel _assistantHighlight;
        public int AssistantIntentRequestCount { get; private set; }
        public UiAssistantCommandIntentKind LastAssistantIntentKind { get; private set; }
        public bool LastAssistantIntentFromTakeover { get; private set; }
        public UiAssistantPanelModel AssistantPanel { get; set; }

        public FakeAssistantPanelGateway(
            UiAssistantPanelModel assistantPanel,
            UiAssistantHighlightModel assistantHighlight)
        {
            AssistantPanel = assistantPanel;
            _assistantHighlight = assistantHighlight;
        }

        public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory) => false;
        public bool TryEnqueueUiAction(UiActionKind kind, int payloadId) => false;
        public bool TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind kind, bool fromTakeover)
        {
            AssistantIntentRequestCount++;
            LastAssistantIntentKind = kind;
            LastAssistantIntentFromTakeover = fromTakeover;
            return true;
        }

        public bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading) { loading = default; return false; }
        public bool TrySetLoadingProgress(float progress01, string status, bool complete) => false;
        public bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics) { diagnostics = default; return false; }
        public bool TryReadShellState(out UiShellStateModel state) { state = default; return false; }
        public bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile) { profile = default; return false; }
        public bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources) { resources = default; return false; }
        public bool TryReadMissionResult(out UiMissionResultPopupModel result) { result = default; return false; }
        public bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection) { selection = UiMatchHudSelectionPanelModel.Hidden; return false; }
        public bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState) { commandState = default; return false; }
        public bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header) { header = UiMatchHudHeaderModel.Default; return false; }
        public bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces) { statusSurfaces = UiMatchHudStatusSurfacesModel.Default; return false; }
        public bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel) { assistantPanel = AssistantPanel; return true; }
        public bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight) { assistantHighlight = _assistantHighlight; return _assistantHighlight.Active; }
        public bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap) { minimap = UiMatchHudMinimapModel.Default; return false; }
        public bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer) { passengerDrawer = UiMatchHudPassengerDrawerModel.Hidden; return false; }
        public bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray) { squadTray = UiMatchHudSquadTrayModel.Default; return false; }
        public bool TryReadBuildDrawer(out UiBuildDrawerModel drawer) { drawer = UiBuildDrawerModel.Empty; return false; }
        public bool TryReadResourceExchange(out UiResourceExchangeModel exchange) { exchange = UiResourceExchangeModel.Empty; return false; }
        public bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar) { placementBar = UiBuildPlacementConfirmationBarModel.Hidden; return false; }
        public bool TryReadArmoryCategory(out ArmoryCatalogCategory category) { category = ArmoryCatalogCategory.Characters; return false; }
        public bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category) => false;
        public bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands) => false;
        public bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion) => false;
    }
}
