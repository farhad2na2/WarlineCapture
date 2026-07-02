using NUnit.Framework;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

public sealed class MatchHudCommandControlsCurrentPrefabTests
{
    private const string MatchHudContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";

    private World _previousWorld;
    private World _world;
    private GameObject _instance;

    public static void RunFocusedValidation()
    {
        try
        {
            RunValidationStep(nameof(MatchHudCommandControlsHaveSerializedButtonReferences), tests => tests.MatchHudCommandControlsHaveSerializedButtonReferences());
            RunValidationStep(nameof(MatchHudScanCommandHasOwnRaycastTarget), tests => tests.MatchHudScanCommandHasOwnRaycastTarget());
            RunValidationStep(nameof(MatchHudCommandButtonsSubmitSelectionCommandRequests), tests => tests.MatchHudCommandButtonsSubmitSelectionCommandRequests());
            RunValidationStep(nameof(MatchHudBoardButtonIsRailCommandAndQueuesBoardTargetMode), tests => tests.MatchHudBoardButtonIsRailCommandAndQueuesBoardTargetMode());
            RunValidationStep(nameof(MatchHudFooterSectionBoardButtonQueuesBoardTargetMode), tests => tests.MatchHudFooterSectionBoardButtonQueuesBoardTargetMode());
            RunValidationStep(nameof(MatchHudSelectionPanelCameraButtonQueuesToggleFollowMode), tests => tests.MatchHudSelectionPanelCameraButtonQueuesToggleFollowMode());
            RunValidationStep(nameof(MatchHudSelectionPanelCameraButtonUsesCommandButtonSpriteStates), tests => tests.MatchHudSelectionPanelCameraButtonUsesCommandButtonSpriteStates());
            RunValidationStep(nameof(MatchHudSelectionPanelCameraButtonAppliesEnabledAndSelectedState), tests => tests.MatchHudSelectionPanelCameraButtonAppliesEnabledAndSelectedState());
            RunValidationStep(nameof(MatchHudSelectionPanelCameraButtonHoverAndPressUseSpriteSwapStates), tests => tests.MatchHudSelectionPanelCameraButtonHoverAndPressUseSpriteSwapStates());
            RunValidationStep(nameof(MatchHudRightQuickRailZoomButtonsUseSpriteSwapStates), tests => tests.MatchHudRightQuickRailZoomButtonsUseSpriteSwapStates());
            RunValidationStep(nameof(MatchHudRightQuickRailZoomButtonsApplyStepState), tests => tests.MatchHudRightQuickRailZoomButtonsApplyStepState());
            RunValidationStep(nameof(MatchHudThreatJumpPanelBindsTitleAndAutoHides), tests => tests.MatchHudThreatJumpPanelBindsTitleAndAutoHides());
            RunValidationStep(nameof(LegacySupportCommandTabRoutesToScanCommandMode), tests => tests.LegacySupportCommandTabRoutesToScanCommandMode());
            Debug.Log("[MatchHudCommandControlsCurrentPrefabValidation] result=Passed tests=13");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudCommandControlsCurrentPrefabValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    public static void RunThreatJumpPanelValidation()
    {
        try
        {
            RunValidationStep(nameof(MatchHudThreatJumpPanelBindsTitleAndAutoHides), tests => tests.MatchHudThreatJumpPanelBindsTitleAndAutoHides());
            Debug.Log("[MatchHudThreatJumpPanelValidation] result=Passed tests=1");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudThreatJumpPanelValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RunValidationStep(string name, System.Action<MatchHudCommandControlsCurrentPrefabTests> step)
    {
        var tests = new MatchHudCommandControlsCurrentPrefabTests();
        tests.SetUp();
        try
        {
            step(tests);
            Debug.Log($"[MatchHudCommandControlsCurrentPrefabValidation] step={name} result=Passed");
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("MatchHudCommandControlsCurrentPrefabTests");
        World.DefaultGameObjectInjectionWorld = _world;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab, MatchHudContentPrefabPath);
        _instance = Object.Instantiate(prefab);
    }

    [TearDown]
    public void TearDown()
    {
        if (_instance != null)
            Object.DestroyImmediate(_instance);

        if (_world != null && _world.IsCreated)
            _world.Dispose();

        World.DefaultGameObjectInjectionWorld = _previousWorld;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = false;
    }

    [Test]
    public void MatchHudCommandControlsHaveSerializedButtonReferences()
    {
        MatchOverlayCommandControlsView controls = LoadControls();

        AssertButton(controls.SelectButton, "Select");
        AssertButton(controls.MoveButton, "Move");
        AssertButton(controls.AttackButton, "Attack");
        AssertButton(controls.ScanButton, "Scan");
        AssertButton(controls.BoardButton, "Board");
        AssertButton(controls.BuildButton, "Build");
        AssertButton(controls.HoldButton, "Hold");
        AssertButton(controls.StopButton, "Stop");
        Assert.NotNull(controls.CommandTabGroup, "Command tab group must be serialized so runtime code avoids hierarchy lookup.");
    }

    [Test]
    public void MatchHudScanCommandHasOwnRaycastTarget()
    {
        MatchOverlayCommandControlsView controls = LoadControls();
        Button scanButton = controls.ScanButton;
        Assert.NotNull(scanButton, "Scan command button must be serialized.");

        Image rootImage = scanButton.GetComponent<Image>();
        Assert.NotNull(rootImage, "Scan command must keep a transparent root Image hit target.");
        Assert.IsTrue(rootImage.raycastTarget, "Scan command root Image must receive raycasts.");
        Assert.NotNull(scanButton.targetGraphic, "Scan command Button.targetGraphic must be assigned.");
        Assert.IsTrue(scanButton.targetGraphic.raycastTarget, "Scan command target graphic must receive raycasts.");
        Assert.IsTrue(scanButton.targetGraphic.transform.IsChildOf(scanButton.transform), "Scan command target graphic must belong to the ScanCommand hierarchy.");
    }

    [Test]
    public void MatchHudCommandButtonsSubmitSelectionCommandRequests()
    {
        MatchOverlayCommandControlsView controls = LoadControls();
        var inputSystem = new MatchOverlayCommandInputUiSystemHelper();
        inputSystem.Bind(controls, new SelectionUiCommandUiSystemHelper());

        AssertClickQueues(controls.SelectButton, RtsSelectionCommandIntentKind.EnterSelectionMode);
        AssertClickQueues(controls.MoveButton, RtsSelectionCommandIntentKind.EnterMoveTargetMode);
        AssertClickQueues(controls.AttackButton, RtsSelectionCommandIntentKind.EnterAttackTargetMode);
        AssertClickQueues(controls.ScanButton, RtsSelectionCommandIntentKind.EnterScanTargetMode);
        AssertClickQueues(controls.BoardButton, RtsSelectionCommandIntentKind.EnterBoardTargetMode);
        AssertClickQueues(controls.HoldButton, RtsSelectionCommandIntentKind.HoldPosition);
        AssertClickQueues(controls.StopButton, RtsSelectionCommandIntentKind.Stop);

        inputSystem.Unbind(controls);
    }

    [Test]
    public void MatchHudBoardButtonIsRailCommandAndQueuesBoardTargetMode()
    {
        MatchOverlayCommandControlsView controls = LoadControls();
        Button boardButton = controls.BoardButton;
        Assert.NotNull(boardButton, "BoardButton must exist in the Match HUD prefab.");
        Assert.IsTrue(IsChildOfNamedTransform(boardButton.transform, "CommandRail"), "BoardButton must live under the bottom CommandRail.");
        Assert.IsFalse(IsChildOfNamedTransform(boardButton.transform, "CommandButtons"), "BoardButton must no longer live in the selected-squad CommandButtons cluster.");

        var inputSystem = new MatchOverlayCommandInputUiSystemHelper();
        var selectionUiCommand = new SelectionUiCommandUiSystemHelper();
        inputSystem.Bind(controls, selectionUiCommand);
        inputSystem.RefreshCommandControlState();
        Assert.IsTrue(boardButton.interactable, "BoardButton must stay clickable when no unit is selected so it can show selection-required feedback.");
        ClearCommandRequests();

        boardButton.onClick.Invoke();

        Assert.IsTrue(TryGetCommandRequests(out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests));
        Assert.AreEqual(1, requests.Length, "Clicking BoardButton must queue exactly one command request.");
        Assert.AreEqual(RtsSelectionCommandIntentKind.EnterBoardTargetMode, requests[0].Kind);

        inputSystem.Unbind(controls);
    }

    [Test]
    public void MatchHudFooterSectionBoardButtonQueuesBoardTargetMode()
    {
        GameObject footerInstance = null;
        try
        {
            footerInstance = InstantiateFooterSection();
            MatchOverlayCommandControlsView controls = footerInstance.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            Assert.NotNull(controls, "Footer section must expose command controls when installed by UIShellContentView.");
            Assert.NotNull(controls.BoardButton, "Footer section command controls must serialize BoardButton directly.");

            var inputSystem = new MatchOverlayCommandInputUiSystemHelper();
            var selectionUiCommand = new SelectionUiCommandUiSystemHelper();
            inputSystem.Bind(controls, selectionUiCommand);
            ClearCommandRequests();

            controls.BoardButton.onClick.Invoke();

            Assert.IsTrue(TryGetCommandRequests(out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests));
            Assert.AreEqual(1, requests.Length, "Clicking the section-installed BoardButton must queue exactly one command request.");
            Assert.AreEqual(RtsSelectionCommandIntentKind.EnterBoardTargetMode, requests[0].Kind);

            inputSystem.Unbind(controls);
        }
        finally
        {
            if (footerInstance != null)
                Object.DestroyImmediate(footerInstance);
        }
    }

    [Test]
    public void MatchHudSelectionPanelCameraButtonQueuesToggleFollowMode()
    {
        MatchHudSelectionPanelView selectionPanel = _instance.GetComponentInChildren<MatchHudSelectionPanelView>(true);
        Assert.NotNull(selectionPanel, "SCN08_MatchHudContent must expose MatchHudSelectionPanelView.");
        Button cameraButton = GetSerializedCameraButton(selectionPanel);

        var selectionUiCommand = new SelectionUiCommandUiSystemHelper();
        selectionPanel.BindCameraAction(() => selectionUiCommand.RequestToggleTacticalFollowCameraMode());
        ClearTacticalFollowCameraRequests();

        cameraButton.onClick.Invoke();

        Assert.IsTrue(TryGetTacticalFollowCameraRequests(out DynamicBuffer<TacticalFollowCameraRequestElement> requests));
        Assert.AreEqual(1, requests.Length, "Clicking CameraButton must queue exactly one follow-camera request.");
        Assert.AreEqual(TacticalFollowCameraRequestKind.ToggleFollowMode, requests[0].Kind);
    }

    [Test]
    public void MatchHudSelectionPanelCameraButtonUsesCommandButtonSpriteStates()
    {
        MatchHudSelectionPanelView selectionPanel = _instance.GetComponentInChildren<MatchHudSelectionPanelView>(true);
        Assert.NotNull(selectionPanel, "SCN08_MatchHudContent must expose MatchHudSelectionPanelView.");
        Button cameraButton = GetSerializedCameraButton(selectionPanel);
        Image targetImage = cameraButton.targetGraphic as Image;

        Assert.AreEqual(Selectable.Transition.SpriteSwap, cameraButton.transition, "CameraButton must use SpriteSwap like the other Target Lock command buttons.");
        Assert.NotNull(targetImage, "CameraButton must use an Image target graphic for sprite-state swaps.");
        Assert.IsTrue(targetImage.raycastTarget, "CameraButton target graphic must receive raycasts.");
        Assert.IsTrue(targetImage.transform.IsChildOf(cameraButton.transform), "CameraButton target graphic must belong to the CameraButton hierarchy.");
        Assert.NotNull(cameraButton.spriteState.highlightedSprite, "CameraButton highlighted/hover sprite is required.");
        Assert.NotNull(cameraButton.spriteState.pressedSprite, "CameraButton pressed/impact sprite is required.");
        Assert.NotNull(cameraButton.spriteState.selectedSprite, "CameraButton selected/current sprite is required.");
        Assert.NotNull(cameraButton.spriteState.disabledSprite, "CameraButton disabled sprite is required.");

        Button[] nestedButtons = cameraButton.GetComponentsInChildren<Button>(true);
        Assert.AreEqual(1, nestedButtons.Length, "CameraButton must remain the actual clickable root, with no hidden child hotspot buttons.");
        Assert.AreSame(cameraButton, nestedButtons[0], "CameraButton nested button scan should only find the root button.");
    }

    [Test]
    public void MatchHudSelectionPanelCameraButtonAppliesEnabledAndSelectedState()
    {
        MatchHudSelectionPanelView selectionPanel = _instance.GetComponentInChildren<MatchHudSelectionPanelView>(true);
        Assert.NotNull(selectionPanel, "SCN08_MatchHudContent must expose MatchHudSelectionPanelView.");
        Button cameraButton = GetSerializedCameraButton(selectionPanel);
        Image targetImage = cameraButton.targetGraphic as Image;
        Assert.NotNull(targetImage, "CameraButton must use an Image target graphic for state application.");

        selectionPanel.SetSelectionVisible(true);
        selectionPanel.SetCameraActionEnabled(false);
        Assert.IsFalse(cameraButton.interactable, "CameraButton must be disabled when the tactical-follow read model has no followable target.");

        selectionPanel.SetCameraActionEnabled(true);
        Assert.IsTrue(cameraButton.interactable, "CameraButton must be enabled when the tactical-follow read model has a followable target.");

        selectionPanel.SetCameraActionSelected(true);
        Assert.AreSame(cameraButton.spriteState.selectedSprite, targetImage.sprite, "CameraButton selected state must persist visually while follow mode is active.");
        Assert.AreSame(cameraButton.spriteState.selectedSprite, targetImage.overrideSprite, "CameraButton selected state must own the rendered SpriteSwap override while follow mode is active.");

        targetImage.overrideSprite = null;
        selectionPanel.SetCameraActionSelected(true);
        Assert.AreSame(cameraButton.spriteState.selectedSprite, targetImage.overrideSprite, "CameraButton selected state must be reasserted after normal UI transitions clear the override.");

        selectionPanel.SetCameraActionSelected(false);
        Assert.IsNull(targetImage.sprite, "CameraButton must restore its transparent normal sprite when follow mode exits.");
        Assert.IsNull(targetImage.overrideSprite, "CameraButton must release its selected SpriteSwap override when follow mode exits.");
    }

    [Test]
    public void MatchHudSelectionPanelCameraButtonHoverAndPressUseSpriteSwapStates()
    {
        MatchHudSelectionPanelView selectionPanel = _instance.GetComponentInChildren<MatchHudSelectionPanelView>(true);
        Assert.NotNull(selectionPanel, "SCN08_MatchHudContent must expose MatchHudSelectionPanelView.");
        Button cameraButton = GetSerializedCameraButton(selectionPanel);
        Image targetImage = cameraButton.targetGraphic as Image;
        Assert.NotNull(targetImage, "CameraButton must use an Image target graphic for live sprite transitions.");

        ActivateHierarchy(cameraButton.transform);
        selectionPanel.SetSelectionVisible(true);
        selectionPanel.SetCameraActionEnabled(true);
        selectionPanel.SetCameraActionSelected(false);
        Assert.IsNull(targetImage.sprite, "CameraButton starts from the transparent normal sprite.");
        Assert.IsNull(targetImage.overrideSprite, "CameraButton starts with no live transition override sprite.");

        var pointerEvent = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left
        };
        Assert.IsTrue(cameraButton.IsActive(), "CameraButton must be active before live pointer transition validation.");
        cameraButton.OnPointerEnter(pointerEvent);
        Assert.AreSame(cameraButton.spriteState.highlightedSprite, targetImage.overrideSprite, "CameraButton hover must use the highlighted sprite.");

        cameraButton.OnPointerDown(pointerEvent);
        Assert.AreSame(cameraButton.spriteState.pressedSprite, targetImage.overrideSprite, "CameraButton press must use the pressed sprite.");

        cameraButton.OnPointerUp(pointerEvent);
        Assert.AreSame(cameraButton.spriteState.highlightedSprite, targetImage.overrideSprite, "CameraButton release while hovered must return to highlighted sprite.");

        cameraButton.OnPointerExit(pointerEvent);
        Assert.IsNull(targetImage.overrideSprite, "CameraButton exit must restore the transparent normal sprite.");
    }

    [Test]
    public void MatchHudRightQuickRailZoomButtonsUseSpriteSwapStates()
    {
        MatchHudRightQuickRailView rail = _instance.GetComponentInChildren<MatchHudRightQuickRailView>(true);
        Assert.NotNull(rail, "SCN08_MatchHudContent must expose MatchHudRightQuickRailView.");

        AssertZoomButtonUsesSpriteSwap(rail.ZoomInButton, "ZoomInButton");
        AssertZoomButtonUsesSpriteSwap(rail.ZoomOutButton, "ZoomOutButton");
    }

    [Test]
    public void MatchHudRightQuickRailZoomButtonsApplyStepState()
    {
        MatchHudRightQuickRailView rail = _instance.GetComponentInChildren<MatchHudRightQuickRailView>(true);
        Assert.NotNull(rail, "SCN08_MatchHudContent must expose MatchHudRightQuickRailView.");
        Assert.NotNull(rail.ZoomInButton, "ZoomInButton must resolve from the right quick rail hierarchy.");
        Assert.NotNull(rail.ZoomOutButton, "ZoomOutButton must resolve from the right quick rail hierarchy.");

        int zoomInClicks = 0;
        int zoomOutClicks = 0;
        MatchHudZoomControlState state = MatchHudZoomControlState.Default;
        rail.BindZoomControls(
            () => zoomInClicks++,
            () => zoomOutClicks++,
            () => state);

        rail.RefreshZoomControls();
        Assert.IsTrue(rail.ZoomInButton.interactable, "Default zoom should enable ZoomInButton.");
        Assert.IsTrue(rail.ZoomOutButton.interactable, "Default zoom should enable ZoomOutButton.");

        rail.ZoomInButton.onClick.Invoke();
        Assert.AreEqual(1, zoomInClicks, "ZoomInButton click should route through the rail callback.");

        state = new MatchHudZoomControlState(zoomInEnabled: false, zoomOutEnabled: true);
        rail.RefreshZoomControls();
        Assert.IsFalse(rail.ZoomInButton.interactable, "Min zoom disables ZoomInButton visually.");
        Assert.IsTrue(rail.ZoomOutButton.interactable, "Min zoom keeps ZoomOutButton enabled.");

        state = new MatchHudZoomControlState(zoomInEnabled: true, zoomOutEnabled: false);
        rail.RefreshZoomControls();
        Assert.IsTrue(rail.ZoomInButton.interactable, "Max zoom keeps ZoomInButton enabled.");
        Assert.IsFalse(rail.ZoomOutButton.interactable, "Max zoom disables ZoomOutButton visually.");

        rail.ZoomOutButton.onClick.Invoke();
        Assert.AreEqual(1, zoomOutClicks, "ZoomOutButton click should route through the rail callback.");
    }

    [Test]
    public void MatchHudThreatJumpPanelBindsTitleAndAutoHides()
    {
        GameObject headerInstance = null;
        try
        {
            headerInstance = InstantiateHeaderSection();
            Transform panelTransform = headerInstance.transform.Find("ThreatJumpPanel");
            Assert.NotNull(panelTransform, "HeaderContent must expose ThreatJumpPanel for runtime warnings.");
            GameObject panel = panelTransform.gameObject;
            TMP_Text title = panelTransform.Find("Title")?.GetComponent<TMP_Text>();
            Assert.NotNull(title, "ThreatJumpPanel/Title must be a TMP text object.");

            var ui = new MainMenuPlayUI();
            ui.BindMatchHudThreatJumpPanel(headerInstance);
            Assert.IsFalse(panel.activeSelf, "Binding the header must hide the placeholder threat panel until a real warning arrives.");

            Assert.IsTrue(ui.TryShowMatchHudThreatWarning("Ground vehicle attack detected - ETA 12s", 42f));
            Assert.IsTrue(panel.activeSelf, "A real warning must enable ThreatJumpPanel.");
            Assert.AreEqual("Ground vehicle attack detected - ETA 12s", title.text);

            ui.TickMatchHudThreatWarning(41.9f);
            Assert.IsTrue(panel.activeSelf, "The threat panel must remain visible until its timeout.");

            ui.TickMatchHudThreatWarning(42.01f);
            Assert.IsFalse(panel.activeSelf, "The threat panel must auto-hide after its timeout.");
        }
        finally
        {
            if (headerInstance != null)
                Object.DestroyImmediate(headerInstance);
        }
    }

    [Test]
    public void LegacySupportCommandTabRoutesToScanCommandMode()
    {
        MatchOverlayCommandControlsView controls = LoadControls();
        Button supportButton = FindCommandTabButton(controls, "SupportCommand");
        Assert.NotNull(supportButton, "SCN08 Match HUD currently exposes SupportCommand as the legacy scan/support tab.");

        var inputSystem = new MatchOverlayCommandInputUiSystemHelper();
        inputSystem.Bind(controls, new SelectionUiCommandUiSystemHelper());

        AssertClickQueues(supportButton, RtsSelectionCommandIntentKind.EnterScanTargetMode);

        inputSystem.Unbind(controls);
    }

    private MatchOverlayCommandControlsView LoadControls()
    {
        MatchOverlayCommandControlsView controls = _instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
        Assert.NotNull(controls, "SCN08_MatchHudContent must expose MatchOverlayCommandControlsView through its serialized content hierarchy.");
        return controls;
    }

    private static GameObject InstantiateFooterSection()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab, MatchHudContentPrefabPath);
        UIShellContentSectionsView sections = prefab.GetComponent<UIShellContentSectionsView>();
        Assert.NotNull(sections, "SCN08_MatchHudContent must expose shell content sections.");
        Assert.IsTrue(sections.TryGetSection(UIShellContentSectionId.Footer, out GameObject footerSource));
        Assert.NotNull(footerSource, "SCN08 footer section source must exist.");
        return Object.Instantiate(footerSource);
    }

    private static GameObject InstantiateHeaderSection()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab, MatchHudContentPrefabPath);
        UIShellContentSectionsView sections = prefab.GetComponent<UIShellContentSectionsView>();
        Assert.NotNull(sections, "SCN08_MatchHudContent must expose shell content sections.");
        Assert.IsTrue(sections.TryGetSection(UIShellContentSectionId.Header, out GameObject headerSource));
        Assert.NotNull(headerSource, "SCN08 header section source must exist.");
        return Object.Instantiate(headerSource);
    }

    private static Button FindCommandTabButton(MatchOverlayCommandControlsView controls, string buttonName)
    {
        MatchOverlayCommandTabView[] tabs = controls.CommandTabGroup != null ? controls.CommandTabGroup.Tabs : null;
        if (tabs == null)
            return null;

        for (int i = 0; i < tabs.Length; i++)
        {
            Button button = tabs[i]?.Button;
            if (button != null && button.name == buttonName)
                return button;
        }

        return null;
    }

    private static bool IsChildOfNamedTransform(Transform transform, string parentName)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.name == parentName)
                return true;
        }

        return false;
    }

    private static void ActivateHierarchy(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
            current.gameObject.SetActive(true);
    }

    private static Button GetSerializedCameraButton(MatchHudSelectionPanelView selectionPanel)
    {
        var serializedPanel = new SerializedObject(selectionPanel);
        SerializedProperty cameraActionProperty = serializedPanel.FindProperty("cameraAction");
        Assert.NotNull(cameraActionProperty, "MatchHudSelectionPanelView must serialize cameraAction.");
        Button cameraButton = cameraActionProperty.objectReferenceValue as Button;
        Assert.NotNull(cameraButton, "CameraButton must be serialized on MatchHudSelectionPanelView.");
        Assert.IsTrue(IsChildOfNamedTransform(cameraButton.transform, "CommandButtons"), "CameraButton should remain in the selected-squad CommandButtons cluster.");
        Assert.IsTrue(cameraButton.interactable, "CameraButton prefab state should be interactable; runtime read model can disable it later.");
        return cameraButton;
    }

    private static void AssertZoomButtonUsesSpriteSwap(Button button, string label)
    {
        Assert.NotNull(button, $"{label} must resolve from RightContent/ZommButtons.");
        Image targetImage = button.targetGraphic as Image;
        Assert.AreEqual(Selectable.Transition.SpriteSwap, button.transition, $"{label} must use SpriteSwap like command buttons.");
        Assert.NotNull(targetImage, $"{label} must use an Image target graphic for sprite-state swaps.");
        Assert.IsTrue(targetImage.raycastTarget, $"{label} target graphic must receive raycasts.");
        Assert.IsTrue(targetImage.transform.IsChildOf(button.transform), $"{label} target graphic must belong to the button hierarchy.");
        Assert.NotNull(button.spriteState.highlightedSprite, $"{label} highlighted/hover sprite is required.");
        Assert.NotNull(button.spriteState.pressedSprite, $"{label} pressed/impact sprite is required.");
        Assert.NotNull(button.spriteState.selectedSprite, $"{label} selected/current sprite is required.");
        Assert.NotNull(button.spriteState.disabledSprite, $"{label} disabled sprite is required.");
    }

    private void AssertClickQueues(Button button, RtsSelectionCommandIntentKind expectedKind)
    {
        Assert.NotNull(button);
        ClearCommandRequests();

        button.onClick.Invoke();

        Assert.IsTrue(TryGetCommandRequests(out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests));
        Assert.AreEqual(1, requests.Length, $"Clicking {button.name} must queue exactly one command request.");
        Assert.AreEqual(expectedKind, requests[0].Kind);
    }

    private void ClearCommandRequests()
    {
        Assert.IsTrue(TryGetCommandRequests(out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests));
        requests.Clear();
    }

    private bool TryGetCommandRequests(out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests)
    {
        var inputState = new RtsSelectionInputCompositionSystemHelper();
        return inputState.TryGetCommandBuffers(
            out _,
            out requests,
            out DynamicBuffer<RtsSelectionCommandResultElement> _);
    }

    private void ClearTacticalFollowCameraRequests()
    {
        if (TryGetTacticalFollowCameraRequests(out DynamicBuffer<TacticalFollowCameraRequestElement> requests))
            requests.Clear();
    }

    private bool TryGetTacticalFollowCameraRequests(out DynamicBuffer<TacticalFollowCameraRequestElement> requests)
    {
        requests = default;
        EntityManager em = _world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<TacticalFollowCameraRequestQueueComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity entity = query.GetSingletonEntity();
        if (!em.HasBuffer<TacticalFollowCameraRequestElement>(entity))
            return false;

        requests = em.GetBuffer<TacticalFollowCameraRequestElement>(entity);
        return true;
    }

    private static void AssertButton(Button button, string label)
    {
        Assert.NotNull(button, $"{label} button reference is required.");
        Assert.IsTrue(button.interactable, $"{label} button should be interactable.");
    }
}
