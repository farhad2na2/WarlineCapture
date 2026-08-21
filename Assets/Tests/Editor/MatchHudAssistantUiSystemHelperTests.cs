using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MatchHudAssistantUiSystemHelperTests
{
    private const string PopupPrefabPath =
        "Assets/Game/Prefabs/UI/Shell/Popups/POP13_ARIACommandAssistantPopup.prefab";
    private const string MatchHudPrefabPath =
        "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private bool _openedScene;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.PopupPrefab_BindsLockedLandscapeHierarchyAndMenuReference());
            passed++;
            RunCase(test => test.MatchHudPrefab_ContainsEditableAssistantButton());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_UsesPrefabButtonAndRestoresObjectives());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_MissingPrefabButtonDoesNotCreateRuntimeFallback());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_AppliesStructuredRowsWithoutCreatingPopupObjects());
            passed++;
            RunCase(test => test.AssistantPanelUi_UnchangedModelApplicationsAllocateZeroManagedBytes());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_EnforcesPopupExclusivity());
            passed++;
            RunCase(test => test.BindMatchHudAssistant_CloseEscapeAndStopHaveSeparateSemantics());
            passed++;
            RunCase(test => test.GuidedCommandHighlight_PointsAtCommandBeforeWorldTarget());
            passed++;
            RunCase(test => test.FirstShowMe_SelectSquad_ClosesPanelAndShowsVisibleIndicator());
            passed++;
            RunCase(test => test.FirstShowMe_SelectSquad_ShowsImmediateUiCueBeforeEcsHighlight());
            passed++;
            RunCase(test => test.MissionReplay_ResetClearsCompletedGuidanceState());
            passed++;
            RunCase(test => test.DiagnosticSuppressionState_IsOwnedByEachHudInstance());
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

    public static void RunDiagnosticOwnershipValidation()
    {
        MatchHudAssistantUiSystemHelperTests tests = new();
        try
        {
            tests.DiagnosticSuppressionState_IsOwnedByEachHudInstance();
            Debug.Log("[MatchHudAssistantDiagnosticOwnershipValidation] result=Passed");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudAssistantDiagnosticOwnershipValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    public static void RunActionLabelValidation()
    {
        MatchHudAssistantUiSystemHelperTests tests = new();
        try
        {
            tests.BindMatchHudAssistant_AppliesStructuredRowsWithoutCreatingPopupObjects();
            Debug.Log("[MatchHudAssistantActionLabelValidation] result=Passed tests=1");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudAssistantActionLabelValidation] result=Failed passed=0");
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
        GameObject[] roots = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = roots.Length - 1; i >= 0; i--)
        {
            GameObject root = roots[i];
            if (root == null || EditorUtility.IsPersistent(root))
                continue;
            if (root.name.StartsWith("AssistantUiTest", StringComparison.Ordinal) ||
                root.name.StartsWith("AriaAssistantPreviewHighlightRuntime", StringComparison.Ordinal) ||
                root.name.StartsWith("AriaAssistantTargetIndicatorRuntime", StringComparison.Ordinal))
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        if (_openedScene)
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        _openedScene = false;
    }

    [Test]
    public void DiagnosticSuppressionState_IsOwnedByEachHudInstance()
    {
        Type helperType = typeof(MatchHudAssistantUiSystemHelper);
        FieldInfo missingButton = helperType.GetField(
            "_loggedMissingButton",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo invalidButton = helperType.GetField(
            "_loggedInvalidButton",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(missingButton);
        Assert.NotNull(invalidButton);
        Assert.IsFalse(missingButton.IsStatic);
        Assert.IsFalse(invalidButton.IsStatic);
    }

    [Test]
    public void PopupPrefab_BindsLockedLandscapeHierarchyAndMenuReference()
    {
        GameObject prefab = LoadPopupPrefab();
        AriaCommandAssistantPopupView view = prefab.GetComponent<AriaCommandAssistantPopupView>();
        Assert.NotNull(view, "POP-13 prefab must own the focused ARIA popup view.");

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.NotNull(instance);
        instance.name = "AssistantUiTestPopupPrefab";
        view = instance.GetComponent<AriaCommandAssistantPopupView>();
        Assert.IsTrue(view.TryBindHierarchy(), "The view must bind every required stable prefab child.");
        Assert.AreEqual(new Vector2(2460f, 1510f), view.LandscapeLayout.sizeDelta);
        Assert.AreEqual(new Vector2(0f, 156f), view.LandscapeLayout.anchoredPosition);
        Assert.NotNull(FindNamed(view.transform, "GoalRow0"));
        Assert.NotNull(FindNamed(view.transform, "AlertRow2"));
        Assert.NotNull(FindNamed(view.transform, "ReportRow1"));
        Assert.NotNull(FindNamed(view.transform, "TargetMarker2"));

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        _openedScene = true;
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        Assert.AreSame(prefab, content.AriaCommandAssistantPopupPrefab);
    }

    [Test]
    public void MatchHudPrefab_ContainsEditableAssistantButton()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudPrefabPath);
        Assert.NotNull(prefab, MatchHudPrefabPath);

        Transform button = FindNamed(prefab.transform, "AriaAssistantButton");
        Assert.NotNull(button, "The Match HUD prefab must own the ARIA access button.");
        Assert.NotNull(button.GetComponent<Image>());
        Assert.NotNull(button.GetComponent<Button>());
        Assert.NotNull(button.GetComponent<Canvas>());
        Assert.NotNull(button.GetComponent<GraphicRaycaster>());
        Assert.NotNull(FindNamed(button, "Label")?.GetComponent<TMP_Text>());
        Assert.NotNull(FindNamed(button, "State")?.GetComponent<TMP_Text>());
        Assert.NotNull(FindNamed(button, "AlertCue")?.GetComponent<TMP_Text>());
    }

    [Test]
    public void BindMatchHudAssistant_UsesPrefabButtonAndRestoresObjectives()
    {
        CreateHudHarness(
            includeAssistantButton: true,
            out RectTransform overlay,
            out RectTransform header,
            out RectTransform objectives);
        RectTransform prefabButton = objectives.parent.Find("AriaAssistantButton") as RectTransform;
        var runtimeState = new FakeMatchRuntimeState();
        var ui = new MainMenuPlayUI();
        ui.Init(null, runtimeState);
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());

        RectTransform button = objectives.parent.Find("AriaAssistantButton") as RectTransform;
        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        Assert.NotNull(button);
        Assert.AreSame(prefabButton, button, "Binding must reuse the prefab-owned button instance.");
        Assert.NotNull(popup);
        Assert.IsTrue(objectives.gameObject.activeSelf, "ARIA must augment the HUD without hiding objectives.");
        Assert.AreEqual(new Vector2(454f, 155f), button.sizeDelta);
        Assert.IsFalse(popup.gameObject.activeSelf, "ARIA starts closed.");

        button.GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(popup.IsOpen);
        Assert.IsTrue(runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(ui.IsPointerOverAnyGameplayUi(CenterScreenPoint(button), out string source));
        Assert.AreEqual("MatchHudAssistant", source);

        ui.Dispose();
        Assert.IsTrue(objectives.gameObject.activeSelf, "Unbind must restore the old objective root.");
        Assert.AreSame(
            prefabButton,
            objectives.parent.Find("AriaAssistantButton") as RectTransform,
            "Unbind must not destroy the prefab-owned button.");
    }

    [Test]
    public void BindMatchHudAssistant_MissingPrefabButtonDoesNotCreateRuntimeFallback()
    {
        CreateHudHarness(
            includeAssistantButton: false,
            out RectTransform overlay,
            out RectTransform header,
            out RectTransform objectives);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        const string expectedError =
            "[ARIA] Match HUD prefab is missing HeaderContent/AriaAssistantButton; runtime button creation is disabled.";
        string capturedError = null;
        Application.LogCallback captureError = (condition, _, logType) =>
        {
            if (logType == LogType.Error && string.Equals(condition, expectedError, StringComparison.Ordinal))
                capturedError = condition;
        };
        Application.logMessageReceived += captureError;
        try
        {
            ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        }
        finally
        {
            Application.logMessageReceived -= captureError;
        }

        Assert.AreEqual(expectedError, capturedError);

        RectTransform button = header.Find("AriaAssistantButton") as RectTransform;
        Assert.IsNull(button, "Runtime binding must not recreate a missing prefab button.");
        Assert.IsTrue(objectives.gameObject.activeSelf, "A failed binding must keep objectives visible.");
        Assert.IsNull(overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true));

        ui.Dispose();
    }

    [Test]
    public void BindMatchHudAssistant_AppliesStructuredRowsWithoutCreatingPopupObjects()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var gateway = new FakeAssistantPanelGateway(CreateStructuredModel(42), CreateHighlightModel(88));
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());

        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        Assert.NotNull(popup);
        int childCountBefore = popup.GetComponentsInChildren<Transform>(true).Length;
        ui.Update();
        int childCountAfter = popup.GetComponentsInChildren<Transform>(true).Length;
        Assert.AreEqual(childCountBefore, childCountAfter, "Read-model binding must reuse the prefab hierarchy.");

        Assert.AreEqual("Secure the relay", Text(popup.transform, "Goal0Title"));
        Assert.AreEqual("PRIMARY / ACTIVE", Text(popup.transform, "Goal0StateText"));
        Assert.IsFalse(FindNamed(popup.transform, "GoalRow2").gameObject.activeSelf);
        Assert.AreEqual("HOSTILE ARMOR", Text(popup.transform, "Alert0Body"));
        Assert.AreEqual("CRITICAL / NEW", Text(popup.transform, "Alert0PriorityText"));
        Assert.AreEqual("Fuel convoy ready", Text(popup.transform, "Report0Body"));
        Assert.AreEqual("LOW / ACTIVE", Text(popup.transform, "Report0PriorityText"));
        Assert.AreEqual("Raven Tank", Text(popup.transform, "TargetNameText"));
        Assert.AreEqual("PRESENTED", Text(popup.transform, "NarrationStateText"));
        Assert.IsTrue(FindNamed(popup.transform, "NarrationWaveform").gameObject.activeSelf);
        Assert.AreEqual("ELAPSED: 02:07", Text(popup.transform, "ElapsedText"));
        Assert.IsTrue(FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().interactable);
        Assert.IsTrue(FindNamed(popup.transform, "DoItButton").GetComponent<Button>().interactable);
        Assert.IsTrue(FindNamed(popup.transform, "StopButton").GetComponent<Button>().interactable);
        Assert.AreEqual("SHOW ME", Text(popup.transform, "ShowMeButtonLabel"));
        Assert.AreEqual("DO IT", Text(popup.transform, "DoItButtonLabel"));

        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen, "SHOW ME must close ARIA so the camera reveal is visible.");
        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        FindNamed(popup.transform, "DoItButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen, "DO IT must close ARIA so selection or movement feedback is visible.");
        Assert.AreEqual(2, gateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.ExecuteRecommendation, gateway.LastAssistantIntentKind);
        Assert.IsTrue(gateway.LastAssistantIntentFromTakeover);

        GameObject worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.NotNull(worldRing);
        Assert.AreEqual(48, worldRing.GetComponent<LineRenderer>().positionCount);
        GameObject targetIndicator = FindLoadedObject("AriaAssistantTargetIndicatorRuntime");
        Assert.NotNull(targetIndicator, "World-target Show Me must create an explicit screen indicator.");
        Assert.AreEqual("ARIA TARGET\n\u25bc", targetIndicator.GetComponentInChildren<TMP_Text>(true).text);
        Assert.IsFalse(targetIndicator.GetComponent<CanvasGroup>().blocksRaycasts);
        Assert.IsFalse(popup.PreviewHighlight.raycastTarget, "ARIA preview visuals must never block gameplay input.");
        ui.Dispose();
    }

    [Test]
    public void AssistantPanelUi_UnchangedModelApplicationsAllocateZeroManagedBytes()
    {
        GameObject instance = UnityEngine.Object.Instantiate(LoadPopupPrefab());
        instance.name = "AssistantUiTestAllocationPopup";
        AriaCommandAssistantPopupView popup = instance.GetComponent<AriaCommandAssistantPopupView>();
        Assert.NotNull(popup);
        Assert.IsTrue(popup.TryBindHierarchy());

        var panel = new AssistantPanelUiSystemHelper();
        panel.Bind(popup, null, null);
        UiAssistantPanelModel model = CreateStructuredModel(73);
        panel.ApplyReadModel(model);
        for (int i = 0; i < 16; i++)
            panel.ApplyReadModel(model);

        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            panel.ApplyReadModel(model);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Assert.AreEqual(0, allocatedBytes, "Repeated applications of one assistant model version must allocate zero managed bytes.");
        panel.Unbind();
    }

    [Test]
    public void BindMatchHudAssistant_EnforcesPopupExclusivity()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());

        GameObject buildRoot = CreatePopupPeer<BuildDrawerView>("AssistantUiTestBuildDrawer");
        GameObject mapRoot = CreatePopupPeer<MatchHudFullMapPopupView>("AssistantUiTestFullMap");
        GameObject exchangeRoot = CreatePopupPeer<ResourceExchangePopupView>("AssistantUiTestResourceExchange");
        ui.BindBuildDrawer(buildRoot.GetComponent<BuildDrawerView>());
        ui.BindMatchHudFullMapPopup(mapRoot.GetComponent<MatchHudFullMapPopupView>());
        ui.BindResourceExchangePopup(exchangeRoot.GetComponent<ResourceExchangePopupView>());

        int buildClosed = 0;
        int mapClosed = 0;
        int exchangeClosed = 0;
        ui.ConfigureLargeTacticalPopupCloseActions(
            () => { buildClosed++; buildRoot.SetActive(false); },
            () => { mapClosed++; mapRoot.SetActive(false); },
            () => { exchangeClosed++; exchangeRoot.SetActive(false); });

        RectTransform button = objectives.parent.Find("AriaAssistantButton") as RectTransform;
        button.GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(1, buildClosed);
        Assert.AreEqual(1, mapClosed);
        Assert.AreEqual(1, exchangeClosed);
        Assert.IsTrue(overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true).IsOpen);
        ui.Dispose();
    }

    [Test]
    public void BindMatchHudAssistant_CloseEscapeAndStopHaveSeparateSemantics()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(91, canExecute: false),
            UiAssistantHighlightModel.Empty);
        UiShellRuntimeGateway.Register(gateway);
        var panelStates = new List<bool>();
        var ui = new MainMenuPlayUI();
        ui.MatchHudAssistantPanelOpenChanged += panelStates.Add;
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        ui.Update();

        Button access = objectives.parent.Find("AriaAssistantButton").GetComponent<Button>();
        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        Assert.IsFalse(FindNamed(popup.transform, "DoItButton").GetComponent<Button>().interactable);
        Assert.AreEqual("DO IT", Text(popup.transform, "DoItButtonLabel"));
        access.onClick.Invoke();
        FindNamed(popup.transform, "HeaderCloseButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen);
        Assert.AreEqual(0, gateway.AssistantIntentRequestCount, "Header CLOSE must not enqueue a command.");

        access.onClick.Invoke();
        FindNamed(popup.transform, "CloseButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen);
        Assert.AreEqual(0, gateway.AssistantIntentRequestCount, "Bottom CLOSE must not enqueue a command.");

        access.onClick.Invoke();
        FindNamed(popup.transform, "StopButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(popup.IsOpen, "STOP is a command and must not close the popup.");
        Assert.AreEqual(1, gateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.StopAssistantControl, gateway.LastAssistantIntentKind);

        Assert.IsTrue(ui.TryCloseMatchHudAssistantForBack());
        Assert.IsFalse(popup.IsOpen, "Escape/back closes ARIA first.");
        Assert.AreEqual(1, gateway.AssistantIntentRequestCount, "Escape/back must not enqueue STOP.");
        Assert.IsFalse(gateway.LastAssistantPanelOpen);
        CollectionAssert.Contains(gateway.AssistantPanelOpenStates, true);
        CollectionAssert.Contains(panelStates, true);
        Assert.IsFalse(panelStates[panelStates.Count - 1]);
        ui.Dispose();
    }

    [Test]
    public void GuidedCommandHighlight_PointsAtCommandBeforeWorldTarget()
    {
        AssertGuidedCommandHighlight(
            recommendationKind: 2,
            commandMode: TacticalCommandMode.Move,
            initialLabel: "PRESS MOVE\n\u25bc",
            armedLabel: "CLICK DESTINATION\n\u25bc");
        AssertGuidedCommandHighlight(
            recommendationKind: 3,
            commandMode: TacticalCommandMode.Attack,
            initialLabel: "PRESS ATTACK\n\u25bc",
            armedLabel: "CLICK ENEMY\n\u25bc");
    }

    [Test]
    public void FirstShowMe_SelectSquad_ClosesPanelAndShowsVisibleIndicator()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var cameraObject = new GameObject("AssistantUiTestCamera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(12f, 3f, 0f);
        cameraObject.transform.rotation = Quaternion.identity;

        UiAssistantHighlightModel selectSquadHighlight = CreateHighlightModel(
            version: 501u,
            recommendationKind: 1,
            targetKind: 6);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(500u),
            UiAssistantHighlightModel.Empty)
        {
            HighlightAfterShowMe = selectSquadHighlight
        };
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        ui.Update();

        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        Assert.IsTrue(popup.IsOpen);
        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen, "The real first Show Me closes the ARIA popup.");

        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();

        GameObject indicator = FindLoadedObject("AriaAssistantTargetIndicatorRuntime");
        GameObject worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.NotNull(indicator);
        Assert.IsTrue(indicator.activeInHierarchy,
            "The first Select-squad Show Me indicator must remain visible after the popup closes.");
        Assert.AreEqual("SELECT SQUAD\n\u25bc", indicator.GetComponentInChildren<TMP_Text>(true).text);
        Assert.NotNull(worldRing);
        Assert.IsTrue(worldRing.activeInHierarchy,
            "The first Show Me must retain the world ring around the squad.");
        Assert.AreEqual(1, gateway.AssistantIntentRequestCount);
        Assert.AreEqual(UiAssistantCommandIntentKind.ShowRecommendation, gateway.LastAssistantIntentKind);
        ui.Dispose();
    }

    [Test]
    public void FirstShowMe_SelectSquad_ShowsImmediateUiCueBeforeEcsHighlight()
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(510u, recommendationKind: 1, recommendationTargetKind: 6),
            UiAssistantHighlightModel.Empty);
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.ConfigureMatchHudSquadTrayBinding(view =>
            view?.Bind(slot => view.SetSelectedSlot(slot)));
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        MatchHudSquadTrayView squadTray = CreateSquadTray(overlay);
        ui.BindMatchHudSquadTray(squadTray);
        MatchOverlayCommandControlsView commandControls = CreateCommandControls(overlay);
        ui.BindMatchHudCommandControls(commandControls);
        ui.Update();

        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();

        GameObject cue = FindLoadedObject("AriaAssistantTargetIndicatorRuntime");
        Assert.NotNull(cue);
        Assert.IsTrue(cue.activeInHierarchy,
            "The first Show Me click must point at the Rifle Squad UI immediately, before ECS responds.");
        Assert.AreEqual("SELECT SQUAD\n\u25bc", cue.GetComponentInChildren<TMP_Text>(true).text);
        Assert.IsFalse(cue.transform.IsChildOf(squadTray.transform),
            "The squad-button arrow must use the top-level HUD canvas so the tray cannot clip it.");
        Assert.IsFalse(popup.IsOpen);
        Assert.AreEqual(1, gateway.AssistantIntentRequestCount);

        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();
        Assert.IsTrue(cue.activeInHierarchy,
            "The Select Squad cue must persist while the player has not clicked the squad button.");

        FindNamed(squadTray.transform, "SoldierCard").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(cue.activeSelf,
            "Selecting the highlighted squad button must complete and remove the Select Squad cue.");
        Assert.AreEqual(UiAssistantCommandIntentKind.StopAssistantControl, gateway.LastAssistantIntentKind);

        // Deliberately leave the panel projection on Select to cover the one-frame stale
        // read observed in the real mission. The next explicit Show Me must still teach Move.
        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(popup.IsOpen);
        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen);
        Assert.IsTrue(cue.activeInHierarchy);
        Assert.AreEqual("PRESS MOVE\n\u25bc", cue.GetComponentInChildren<TMP_Text>(true).text,
            "After the squad is selected, the next Show Me must point to the Move command button.");

        gateway.AssistantHighlight = CreateHighlightModel(511u, recommendationKind: 2);
        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();
        var commandInput = new MatchOverlayCommandInputUiSystemHelper();
        commandInput.Bind(
            commandControls,
            new AcceptedSelectionUiCommand(),
            commandModeQueued: ui.AcknowledgeMatchHudGuidedCommandMode);
        commandControls.MoveButton.onClick.Invoke();
        Assert.IsFalse(cue.activeSelf,
            "Clicking Move must finish the command-button instruction before the destination instruction.");

        // Keep the structured panel deliberately stale on Select. The active highlight is
        // already Move, so the next explicit Show Me must reveal the ground destination.
        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();
        GameObject worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.NotNull(worldRing);
        Assert.IsTrue(worldRing.activeSelf,
            "The explicit Show Me after clicking Move must reveal the authored ground target.");
        Assert.AreEqual("CLICK DESTINATION\n\u25bc", cue.GetComponentInChildren<TMP_Text>(true).text);
        commandInput.Unbind(commandControls);
        ui.Dispose();
    }

    [Test]
    public void MissionReplay_ResetClearsCompletedGuidanceState()
    {
        var presentation = new AssistantHighlightPresentationSystemHelper();
        presentation.ApplyReadModel(CreateHighlightModel(601u, recommendationKind: 2));
        SetPrivateField(presentation, "_selectSquadCompleted", true);
        SetPrivateField(presentation, "_commandGuidanceArmed", true);
        SetPrivateField(presentation, "_awaitingNextShowMe", true);
        SetPrivateField(presentation, "_worldTargetShowRequested", true);

        presentation.ResetForMissionAttempt();

        Assert.IsFalse(GetPrivateField<bool>(presentation, "_selectSquadCompleted"));
        Assert.IsFalse(GetPrivateField<bool>(presentation, "_commandGuidanceArmed"));
        Assert.IsFalse(GetPrivateField<bool>(presentation, "_awaitingNextShowMe"));
        Assert.IsFalse(GetPrivateField<bool>(presentation, "_worldTargetShowRequested"));
        Assert.IsFalse(presentation.LastAppliedModel.Active,
            "A replay must not reuse the previous attempt's squad centroid or command step.");
    }

    private static void AssertGuidedCommandHighlight(
        byte recommendationKind,
        TacticalCommandMode commandMode,
        string initialLabel,
        string armedLabel)
    {
        CreateHudHarness(true, out RectTransform overlay, out RectTransform header, out RectTransform objectives);
        var gateway = new FakeAssistantPanelGateway(
            CreateStructuredModel(
                (uint)(100 + recommendationKind),
                recommendationKind: recommendationKind,
                recommendationTargetKind: 1),
            CreateHighlightModel((uint)(200 + recommendationKind), recommendationKind));
        UiShellRuntimeGateway.Register(gateway);
        var ui = new MainMenuPlayUI();
        ui.Init(null, new FakeMatchRuntimeState());
        ui.BindMatchHudAssistant(header.gameObject, overlay, LoadPopupPrefab());
        MatchOverlayCommandControlsView commandControls = CreateCommandControls(overlay);
        ui.BindMatchHudCommandControls(commandControls);
        ui.ApplyMatchHudCommandMode(commandMode);
        var commandInput = new MatchOverlayCommandInputUiSystemHelper();
        commandInput.Bind(
            commandControls,
            new AcceptedSelectionUiCommand(),
            commandModeQueued: ui.AcknowledgeMatchHudGuidedCommandMode);
        ui.Update();
        // Passive ECS projection can arrive immediately after the Show Me read model.
        // It must not be mistaken for the player's command-button click.
        ui.ApplyMatchHudCommandMode(commandMode);

        GameObject indicator = FindLoadedObject("AriaAssistantTargetIndicatorRuntime");
        GameObject worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.NotNull(indicator);
        Assert.IsTrue(indicator.activeInHierarchy,
            "The guided command-button indicator must be visible in the active HUD hierarchy.");
        Assert.IsTrue(indicator.GetComponent<Canvas>().overrideSorting,
            "The animated ARIA indicator must have an isolated canvas to avoid rebuilding the full HUD.");
        Assert.AreEqual(Vector3.one, indicator.transform.localScale);
        Assert.AreEqual(initialLabel, indicator.GetComponentInChildren<TMP_Text>(true).text);
        Assert.IsTrue(worldRing == null || !worldRing.activeSelf,
            "Before the command is armed, ARIA must teach the command button instead of exposing the world target.");

        Button guidedButton = commandMode == TacticalCommandMode.Move
            ? commandControls.MoveButton
            : commandControls.AttackButton;
        guidedButton.onClick.Invoke();
        worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.IsFalse(indicator.activeSelf,
            "Accepting the command button must end this Show Me step immediately.");
        Assert.IsTrue(worldRing == null || !worldRing.activeSelf,
            "ARIA must wait for another Show Me request before exposing the world target.");

        // Opening the ARIA popup can clear the transient gameplay command mode. The
        // acknowledged guidance step must still remember that Move/Attack was pressed.
        ui.ApplyMatchHudCommandMode(TacticalCommandMode.None);

        gateway.AssistantHighlight = CreateHighlightModel(
            (uint)(300 + recommendationKind), recommendationKind);
        SetPrivateField(ui, "_nextAssistantPanelRefreshTime", 0f);
        ui.Update();
        Assert.IsFalse(indicator.activeSelf,
            "A refreshed ECS preview must not reveal the world target without another Show Me click.");

        objectives.parent.Find("AriaAssistantButton").GetComponent<Button>().onClick.Invoke();
        AriaCommandAssistantPopupView popup = overlay.GetComponentInChildren<AriaCommandAssistantPopupView>(true);
        Assert.IsTrue(popup.IsOpen);
        FindNamed(popup.transform, "ShowMeButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(popup.IsOpen);
        worldRing = GameObject.Find("AriaAssistantPreviewHighlightRuntime");
        Assert.AreEqual(armedLabel, indicator.GetComponentInChildren<TMP_Text>(true).text);
        Assert.NotNull(worldRing);
        Assert.IsTrue(worldRing.activeSelf,
            "The next explicit Show Me after arming the command must reveal its world target.");
        ui.CompleteMatchHudGuidedWorldTarget(commandMode);
        Assert.IsFalse(indicator.activeSelf,
            "Completing the highlighted world action must remove its screen arrow immediately.");
        Assert.IsFalse(worldRing.activeSelf,
            "Completing the highlighted world action must remove its ground ring immediately.");
        commandInput.Unbind(commandControls);
        ui.Dispose();
        UiShellRuntimeGateway.Register(null);
    }

    private static UiAssistantPanelModel CreateStructuredModel(
        uint version,
        bool canExecute = true,
        byte recommendationKind = 0,
        byte recommendationTargetKind = 0)
    {
        return new UiAssistantPanelModel(
            version,
            true,
            127,
            new UiAssistantGoalRowModel(true, 10, "Secure the relay", "Hold the uplink perimeter.", 0, 2, true),
            new UiAssistantGoalRowModel(true, 11, "Protect civilians", "Keep the evacuation route open.", 2, 3, false),
            UiAssistantGoalRowModel.Empty,
            new UiAssistantMessageRowModel(true, 20, "HOSTILE ARMOR", "Raven Tank is firing on Echo Squad.", 3, 1, 1, true, false),
            UiAssistantMessageRowModel.Empty,
            UiAssistantMessageRowModel.Empty,
            new UiAssistantMessageRowModel(true, 30, "Fuel convoy ready", "Depot route is clear.", 0, 2, 2, false, false),
            UiAssistantMessageRowModel.Empty,
            new UiAssistantTargetLockModel(true, 2, 1, "Raven Tank", "Echo Squad", "140 M", "72 / 100", "HOSTILE", "PREVIEW", "Line of fire verified."),
            new UiAssistantNarrationModel((byte)UiAssistantNarrationStateKind.Presented, 3, "PRESENTED", "Hostile armor identified.", string.Empty, true),
            true,
            "Focus hostile armor",
            "Preview the verified hostile source before dispatch.",
            "CRITICAL",
            "SHOW ME",
            true,
            canExecute,
            true,
            false,
            "PLAYER CONTROL",
            "You are issuing orders directly.",
            recommendationKind: recommendationKind,
            recommendationTargetKind: recommendationTargetKind);
    }

    private static UiAssistantHighlightModel CreateHighlightModel(
        uint version,
        byte recommendationKind = 0,
        byte targetKind = 1)
    {
        return new UiAssistantHighlightModel(
            version, true, 7, 3101, recommendationKind, targetKind, 12f, 3f, 9f, 1f);
    }

    private static MatchOverlayCommandControlsView CreateCommandControls(RectTransform parent)
    {
        var root = new GameObject(
            "AssistantUiTestCommandControls",
            typeof(RectTransform),
            typeof(MatchOverlayCommandControlsView));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(900f, 180f);
        Button move = CreateCommandButton("MoveCommandButton", rootRect, new Vector2(-180f, 0f));
        Button attack = CreateCommandButton("AttackCommandButton", rootRect, new Vector2(180f, 0f));
        SetPrivateField(root.GetComponent<MatchOverlayCommandControlsView>(), "moveButton", move);
        SetPrivateField(root.GetComponent<MatchOverlayCommandControlsView>(), "attackButton", attack);
        return root.GetComponent<MatchOverlayCommandControlsView>();
    }

    private static MatchHudSquadTrayView CreateSquadTray(RectTransform parent)
    {
        var root = new GameObject(
            "AssistantUiTestSquadTray",
            typeof(RectTransform),
            typeof(MatchHudSquadTrayView));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Button soldier = CreateCommandButton("SoldierCard", rootRect, new Vector2(-500f, -350f));
        var cards = new MatchHudSquadTrayView.Card[5];
        cards[0] = new MatchHudSquadTrayView.Card
        {
            Button = soldier,
            FrameImage = soldier.GetComponent<Image>(),
            PortraitImage = soldier.GetComponent<Image>()
        };
        SetPrivateField(root.GetComponent<MatchHudSquadTrayView>(), "cards", cards);
        return root.GetComponent<MatchHudSquadTrayView>();
    }

    private static Button CreateCommandButton(string name, RectTransform parent, Vector2 position)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(260f, 120f);
        Button button = root.GetComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        return button;
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, name);
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, name);
        return (T)field.GetValue(target);
    }

    private static void CreateHudHarness(
        bool includeAssistantButton,
        out RectTransform overlay,
        out RectTransform header,
        out RectTransform objectives)
    {
        overlay = CreateRectRoot("AssistantUiTestOverlay", new Vector2(4800f, 2160f));
        header = CreateRect("HeaderContent", overlay);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, 560f);

        objectives = CreateRect("ObjectivesPanel", header);
        objectives.anchorMin = new Vector2(0f, 1f);
        objectives.anchorMax = new Vector2(0f, 1f);
        objectives.pivot = new Vector2(0f, 1f);
        objectives.anchoredPosition = new Vector2(16f, -16f);
        objectives.sizeDelta = new Vector2(670f, 520f);
        RectTransform objectivesContent = CreateRect("ObjectivesContent", objectives);
        RectTransform elapsed = CreateRect("Elapsed", objectivesContent);
        elapsed.gameObject.AddComponent<MatchHudObjectivesElapsedView>();

        if (includeAssistantButton)
            CreateAssistantButton(header);
    }

    private static void CreateAssistantButton(RectTransform parent)
    {
        var root = new GameObject(
            "AriaAssistantButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(Canvas),
            typeof(GraphicRaycaster));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(16f, -16f);
        rect.sizeDelta = new Vector2(454f, 155f);
        root.GetComponent<Button>().targetGraphic = root.GetComponent<Image>();

        CreateAssistantButtonText("Label", rect, "ARIA");
        CreateAssistantButtonText("State", rect, "PLAYER CONTROL");
        TMP_Text alertCue = CreateAssistantButtonText("AlertCue", rect, string.Empty);
        alertCue.gameObject.SetActive(false);
    }

    private static TMP_Text CreateAssistantButtonText(string name, RectTransform parent, string value)
    {
        RectTransform rect = CreateRect(name, parent);
        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        return text;
    }

    private static RectTransform CreateRectRoot(string name, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        return root.GetComponent<RectTransform>();
    }

    private static GameObject CreatePopupPeer<T>(string name) where T : Component
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(T));
        root.SetActive(true);
        return root;
    }

    private static GameObject LoadPopupPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
        Assert.NotNull(prefab, PopupPrefabPath);
        return prefab;
    }

    private static Transform FindNamed(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindNamed(root.GetChild(i), name);
            if (match != null)
                return match;
        }
        return null;
    }

    private static GameObject FindLoadedObject(string name)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int index = 0; index < objects.Length; index++)
        {
            if (objects[index] != null && objects[index].name == name)
                return objects[index];
        }
        return null;
    }

    private static string Text(Transform root, string name)
    {
        Transform match = FindNamed(root, name);
        Assert.NotNull(match, name);
        TMP_Text text = match.GetComponent<TMP_Text>();
        Assert.NotNull(text, name);
        return text.text;
    }

    private static Vector2 CenterScreenPoint(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * 0.5f);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T match = root.GetComponentInChildren<T>(true);
            if (match != null)
                return match;
        }
        return null;
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

    private sealed class AcceptedSelectionUiCommand : ISelectionUiCommand
    {
        public void CaptureUiClickSequence() { }
        public bool RequestDeselectAll() => true;
        public bool RequestEnterSelectionMode() => true;
        public bool RequestExitSelectionMode() => true;
        public bool RequestMoveCommandMode() => true;
        public bool RequestAttackCommandMode() => true;
        public bool RequestScanCommandMode() => true;
        public bool RequestBoardTargetMode() => true;
        public bool RequestToggleTacticalFollowCameraMode() => true;
        public bool RequestHoldPosition() => true;
        public bool RequestStop() => true;
        public bool RequestBoardAllSelectedTransport() => true;
        public bool RequestCancelActiveCommandMode() => true;
    }

    private sealed class FakeAssistantPanelGateway : IUiShellRuntimeGateway, IUiAssistantPanelStateGateway
    {
        public UiAssistantHighlightModel AssistantHighlight { get; set; }
        public UiAssistantHighlightModel HighlightAfterShowMe { get; set; }
        public int AssistantIntentRequestCount { get; private set; }
        public UiAssistantCommandIntentKind LastAssistantIntentKind { get; private set; }
        public bool LastAssistantIntentFromTakeover { get; private set; }
        public UiAssistantPanelModel AssistantPanel { get; set; }
        public List<bool> AssistantPanelOpenStates { get; } = new();
        public bool LastAssistantPanelOpen => AssistantPanelOpenStates.Count > 0 &&
                                              AssistantPanelOpenStates[AssistantPanelOpenStates.Count - 1];

        public FakeAssistantPanelGateway(UiAssistantPanelModel assistantPanel, UiAssistantHighlightModel assistantHighlight)
        {
            AssistantPanel = assistantPanel;
            AssistantHighlight = assistantHighlight;
        }

        public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory) => false;
        public bool TryEnqueueUiAction(UiActionKind kind, int payloadId) => false;
        public bool TrySetAssistantPanelOpen(bool open)
        {
            AssistantPanelOpenStates.Add(open);
            return true;
        }

        public bool TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind kind, bool fromTakeover)
        {
            AssistantIntentRequestCount++;
            LastAssistantIntentKind = kind;
            LastAssistantIntentFromTakeover = fromTakeover;
            if (kind == UiAssistantCommandIntentKind.ShowRecommendation && HighlightAfterShowMe.Active)
                AssistantHighlight = HighlightAfterShowMe;
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
        public bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight) { assistantHighlight = AssistantHighlight; return AssistantHighlight.Active; }
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
