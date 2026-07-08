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
            true,
            true,
            "Review objective",
            "Focus the active objective before choosing the next order.",
            "HIGH",
            "SHOW ME",
            true,
            true,
            false,
            "ARIA CONTROL"),
            new UiAssistantHighlightModel(88, true, 7, 3101, 1, 12f, 3f, 9f, 1f));
        UiShellRuntimeGateway.Register(assistantGateway);

        ui.Update();

        RectTransform panel = overlay.Find("AriaAssistantPanel") as RectTransform;
        Assert.NotNull(panel);
        TMP_Text goals = panel.Find("GoalsBody")?.GetComponent<TMP_Text>();
        TMP_Text alerts = panel.Find("AlertsBody")?.GetComponent<TMP_Text>();
        TMP_Text ownership = panel.Find("OwnershipBody")?.GetComponent<TMP_Text>();
        TMP_Text recommendation = panel.Find("RecommendationBody")?.GetComponent<TMP_Text>();
        Image previewPulse = panel.Find("PreviewPulse")?.GetComponent<Image>();
        Button showMe = panel.Find("NextActionButton")?.GetComponent<Button>();
        Button giveControl = panel.Find("GiveControlButton")?.GetComponent<Button>();
        TMP_Text giveControlLabel = panel.Find("GiveControlButton/Label")?.GetComponent<TMP_Text>();

        Assert.NotNull(goals);
        Assert.NotNull(alerts);
        Assert.NotNull(ownership);
        Assert.NotNull(recommendation);
        Assert.NotNull(previewPulse);
        Assert.NotNull(showMe);
        Assert.NotNull(giveControl);
        Assert.NotNull(giveControlLabel);
        Assert.AreEqual("- Neutralize hostile patrol\n[x] Protect civilians", goals.text);
        Assert.AreEqual("HIGH: Fuel reserves empty", alerts.text);
        Assert.AreEqual("ARIA CONTROL", ownership.text);
        StringAssert.Contains("HIGH: Review objective", recommendation.text);
        Assert.IsTrue(previewPulse.gameObject.activeSelf);
        Assert.Greater(previewPulse.color.a, 0.4f);
        Assert.IsTrue(showMe.interactable);
        Assert.IsTrue(giveControl.interactable);
        Assert.AreEqual("DO IT", giveControlLabel.text);

        showMe.onClick.Invoke();

        Assert.AreEqual(1, assistantGateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.ShowRecommendation, assistantGateway.LastAssistantIntentKind);
        Assert.IsFalse(assistantGateway.LastAssistantIntentFromTakeover);

        giveControl.onClick.Invoke();

        Assert.AreEqual(2, assistantGateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.ExecuteRecommendation, assistantGateway.LastAssistantIntentKind);
        Assert.IsFalse(assistantGateway.LastAssistantIntentFromTakeover);

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
        private readonly UiAssistantPanelModel _assistantPanel;
        private readonly UiAssistantHighlightModel _assistantHighlight;
        public int AssistantIntentRequestCount { get; private set; }
        public UiAssistantCommandIntentKind LastAssistantIntentKind { get; private set; }
        public bool LastAssistantIntentFromTakeover { get; private set; }

        public FakeAssistantPanelGateway(
            UiAssistantPanelModel assistantPanel,
            UiAssistantHighlightModel assistantHighlight)
        {
            _assistantPanel = assistantPanel;
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
        public bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel) { assistantPanel = _assistantPanel; return true; }
        public bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight) { assistantHighlight = _assistantHighlight; return _assistantHighlight.Active; }
        public bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap) { minimap = UiMatchHudMinimapModel.Default; return false; }
        public bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer) { passengerDrawer = UiMatchHudPassengerDrawerModel.Hidden; return false; }
        public bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray) { squadTray = UiMatchHudSquadTrayModel.Default; return false; }
        public bool TryReadBuildDrawer(out UiBuildDrawerModel drawer) { drawer = UiBuildDrawerModel.Empty; return false; }
        public bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar) { placementBar = UiBuildPlacementConfirmationBarModel.Hidden; return false; }
        public bool TryReadArmoryCategory(out ArmoryCatalogCategory category) { category = ArmoryCatalogCategory.Characters; return false; }
        public bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category) => false;
        public bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands) => false;
        public bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion) => false;
    }
}
