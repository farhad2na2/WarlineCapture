using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHudCommandFeedbackPanelTests
{
    private const string MatchHudContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private GameObject _root;

    public static void RunFocusedValidation()
    {
        try
        {
            RunValidationStep(nameof(RuntimeFeedbackSystem_AppliesCommandFeedbackSeverityIcons), tests => tests.RuntimeFeedbackSystem_AppliesCommandFeedbackSeverityIcons());
            RunValidationStep(nameof(RuntimeFeedbackSystem_HoldStopAndScanUseClearCommandPrompts), tests => tests.RuntimeFeedbackSystem_HoldStopAndScanUseClearCommandPrompts());
            RunValidationStep(nameof(MatchHudContentPrefab_HasCommandFeedbackReferencesAssigned), tests => tests.MatchHudContentPrefab_HasCommandFeedbackReferencesAssigned());
            RunValidationStep(nameof(RuntimeFeedbackSystem_AppliesBoardFeedbackActions), tests => tests.RuntimeFeedbackSystem_AppliesBoardFeedbackActions());
            RunValidationStep(nameof(RuntimeFeedbackSystem_CommandModePromptDoesNotAutoHide), tests => tests.RuntimeFeedbackSystem_CommandModePromptDoesNotAutoHide());
            RunValidationStep(nameof(RuntimeFeedbackSystem_HoldStopPromptsClearAndResultsAutoHide), tests => tests.RuntimeFeedbackSystem_HoldStopPromptsClearAndResultsAutoHide());
            RunValidationStep(nameof(RuntimeFeedbackSystem_SuccessResultAutoHidesAfterDuration), tests => tests.RuntimeFeedbackSystem_SuccessResultAutoHidesAfterDuration());
            RunValidationStep(nameof(RuntimeFeedbackSystem_RejectedResultAutoHidesAfterErrorDuration), tests => tests.RuntimeFeedbackSystem_RejectedResultAutoHidesAfterErrorDuration());
            RunValidationStep(nameof(RuntimeFeedbackSystem_BoardErrorRestoresBoardPromptAndActions), tests => tests.RuntimeFeedbackSystem_BoardErrorRestoresBoardPromptAndActions());
            RunValidationStep(nameof(RuntimeFeedbackSystem_BoardSuccessClearsPromptFallbackAndAutoHides), tests => tests.RuntimeFeedbackSystem_BoardSuccessClearsPromptFallbackAndAutoHides());
            RunValidationStep(nameof(SelectButtonClick_QueuesRequestAndFeedbackClearsBoardActions), tests => tests.SelectButtonClick_QueuesRequestAndFeedbackClearsBoardActions());
            RunValidationStep(nameof(HoldStopScanButtons_WhenNoSelectionShowRecommendedFeedback), tests => tests.HoldStopScanButtons_WhenNoSelectionShowRecommendedFeedback());
            RunValidationStep(nameof(ScanButtonClick_WhenReadModelRejectsShowsFeedbackWithoutQueueing), tests => tests.ScanButtonClick_WhenReadModelRejectsShowsFeedbackWithoutQueueing());
            RunValidationStep(nameof(MatchHudContentPrefab_UpdatesActualFeedbackIconForMessageSeverity), tests => tests.MatchHudContentPrefab_UpdatesActualFeedbackIconForMessageSeverity());
            Debug.Log("[MatchHudCommandFeedbackValidation] result=Passed tests=14");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudCommandFeedbackValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RunValidationStep(string name, System.Action<MatchHudCommandFeedbackPanelTests> step)
    {
        var tests = new MatchHudCommandFeedbackPanelTests();
        try
        {
            step(tests);
            Debug.Log($"[MatchHudCommandFeedbackValidation] step={name} result=Passed");
        }
        finally
        {
            tests.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    [Test]
    public void RuntimeFeedbackSystem_AppliesCommandFeedbackSeverityIcons()
    {
        _root = new GameObject("FeedbackView");
        var panel = new GameObject("FeedbackPanel");
        var textNode = new GameObject("FeedbackText");
        var iconNode = new GameObject("FeedbackIcon");

        panel.transform.SetParent(_root.transform);
        textNode.transform.SetParent(panel.transform);
        iconNode.transform.SetParent(panel.transform);

        var view = _root.AddComponent<BattleHudRuntimeFeedbackView>();
        TMP_Text text = textNode.AddComponent<TextMeshProUGUI>();
        Image icon = iconNode.AddComponent<Image>();
        Sprite neutral = CreateTestSprite("FeedbackNeutral");
        Sprite ready = CreateTestSprite("FeedbackReady");
        Sprite warning = CreateTestSprite("FeedbackWarning");
        Sprite error = CreateTestSprite("FeedbackError");
        SetPrivateField(view, "feedbackPanel", panel);
        SetPrivateField(view, "feedbackText", text);
        SetPrivateField(view, "feedbackIcon", icon);
        SetPrivateField(view, "neutralIcon", neutral);
        SetPrivateField(view, "readyIcon", ready);
        SetPrivateField(view, "warningIcon", warning);
        SetPrivateField(view, "errorIcon", error);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, TacticalCommandMode.Attack);
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Tap hostile target.", text.text);
        Assert.AreSame(ready, icon.sprite);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
            view,
            TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotEnemy));
        Assert.AreEqual("Target is not hostile.", text.text);
        Assert.AreSame(error, icon.sprite);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
            view,
            TacticalCommandResult.Success("Destroyed selected unit."));
        Assert.AreEqual("Destroyed selected unit.", text.text);
        Assert.AreSame(warning, icon.sprite);
    }

    [Test]
    public void RuntimeFeedbackSystem_HoldStopAndScanUseClearCommandPrompts()
    {
        BattleHudRuntimeFeedbackView view = CreateFeedbackView(out GameObject panel, out TMP_Text text);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, TacticalCommandMode.Hold);
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Hold position and return fire.", text.text);
        Assert.AreEqual(CommandFeedbackSeverity.Ready, TacticalCommandFeedbackText.ToInstructionSeverity(TacticalCommandMode.Hold));
        Assert.AreEqual(TacticalCommandMode.Hold, view.CurrentCommandMode);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, TacticalCommandMode.Stop);
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Stop selected units and clear orders.", text.text);
        Assert.AreEqual(CommandFeedbackSeverity.Warning, TacticalCommandFeedbackText.ToInstructionSeverity(TacticalCommandMode.Stop));
        Assert.AreEqual(TacticalCommandMode.Stop, view.CurrentCommandMode);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, TacticalCommandMode.Scan);
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Tap scan area.", text.text);
        Assert.AreEqual(CommandFeedbackSeverity.Ready, TacticalCommandFeedbackText.ToInstructionSeverity(TacticalCommandMode.Scan));
        Assert.AreEqual(TacticalCommandMode.Scan, view.CurrentCommandMode);
    }

    [Test]
    public void MatchHudContentPrefab_HasCommandFeedbackReferencesAssigned()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab, $"Missing Match HUD content prefab at {MatchHudContentPrefabPath}.");

        var view = prefab.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
        Assert.NotNull(view, "SCN08 Match HUD content must expose BattleHudRuntimeFeedbackView.");

        var serializedView = new SerializedObject(view);
        AssertObjectReference(serializedView, "feedbackPanel");
        AssertObjectReference(serializedView, "feedbackText");
        AssertObjectReference(serializedView, "feedbackIcon");
        AssertObjectReference(serializedView, "feedbackActionsRoot");
        AssertObjectReference(serializedView, "boardAllButton");
        AssertObjectReference(serializedView, "boardAllButtonLabel");
        AssertObjectReference(serializedView, "cancelButton");
        AssertObjectReference(serializedView, "cancelButtonLabel");
        AssertObjectReference(serializedView, "neutralIcon");
        AssertObjectReference(serializedView, "readyIcon");
        AssertObjectReference(serializedView, "warningIcon");
        AssertObjectReference(serializedView, "errorIcon");

        Image icon = view.FeedbackIcon;
        Assert.NotNull(icon, "SCN08 command feedback must serialize the actual FeedbackPanel Icon image.");
        Assert.AreSame(
            FindRequiredImage(prefab.transform, "FooterContent/FeedbackPanel/Frame/Icon"),
            icon,
            "SCN08 command feedback icon must point at FooterContent/FeedbackPanel/Frame/Icon.");
        Assert.IsTrue(icon.enabled, "SCN08 command feedback icon Image must be enabled like the Build Drawer instruction icon.");
        Assert.AreSame(serializedView.FindProperty("neutralIcon").objectReferenceValue, icon.sprite);

        Assert.AreSame(
            FindRequiredButton(prefab.transform, "FooterContent/FeedbackPanel/Frame/Actions/BoardAllButton"),
            serializedView.FindProperty("boardAllButton").objectReferenceValue);
        Assert.AreSame(
            FindRequiredButton(prefab.transform, "FooterContent/FeedbackPanel/Frame/Actions/CancelButton"),
            serializedView.FindProperty("cancelButton").objectReferenceValue);
    }

    [Test]
    public void RuntimeFeedbackSystem_AppliesBoardFeedbackActions()
    {
        _root = new GameObject("FeedbackView");
        var panel = new GameObject("FeedbackPanel");
        var textNode = new GameObject("FeedbackText");
        var actions = new GameObject("Actions");
        var boardAllNode = new GameObject("BoardAllButton");
        var boardAllLabelNode = new GameObject("BoardAllLabel");
        var cancelNode = new GameObject("CancelButton");
        var cancelLabelNode = new GameObject("CancelLabel");

        panel.transform.SetParent(_root.transform);
        textNode.transform.SetParent(panel.transform);
        actions.transform.SetParent(panel.transform);
        boardAllNode.transform.SetParent(actions.transform);
        boardAllLabelNode.transform.SetParent(boardAllNode.transform);
        cancelNode.transform.SetParent(actions.transform);
        cancelLabelNode.transform.SetParent(cancelNode.transform);

        var view = _root.AddComponent<BattleHudRuntimeFeedbackView>();
        TMP_Text text = textNode.AddComponent<TextMeshProUGUI>();
        Button boardAll = boardAllNode.AddComponent<Button>();
        TMP_Text boardAllLabel = boardAllLabelNode.AddComponent<TextMeshProUGUI>();
        Button cancel = cancelNode.AddComponent<Button>();
        TMP_Text cancelLabel = cancelLabelNode.AddComponent<TextMeshProUGUI>();
        SetPrivateField(view, "feedbackPanel", panel);
        SetPrivateField(view, "feedbackText", text);
        SetPrivateField(view, "feedbackActionsRoot", actions);
        SetPrivateField(view, "boardAllButton", boardAll);
        SetPrivateField(view, "boardAllButtonLabel", boardAllLabel);
        SetPrivateField(view, "cancelButton", cancel);
        SetPrivateField(view, "cancelButtonLabel", cancelLabel);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyBoardCommandMode(
            view,
            UiBoardCommandModeDirection.TransportToPassenger,
            boardAllInteractable: true);

        Assert.IsTrue(panel.activeSelf);
        Assert.IsTrue(actions.activeSelf);
        Assert.AreEqual("Select units to board or use BOARD ALL.", text.text);
        Assert.IsTrue(boardAll.gameObject.activeSelf);
        Assert.IsTrue(boardAll.interactable);
        Assert.AreEqual("BOARD ALL", boardAllLabel.text);
        Assert.IsTrue(cancel.gameObject.activeSelf);
        Assert.IsTrue(cancel.interactable);
        Assert.AreEqual("CANCEL", cancelLabel.text);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyBoardCommandMode(
            view,
            UiBoardCommandModeDirection.PassengerToTransport,
            boardAllInteractable: false);

        Assert.IsTrue(actions.activeSelf);
        Assert.AreEqual("Select a transport.", text.text);
        Assert.IsFalse(boardAll.gameObject.activeSelf);
        Assert.IsTrue(cancel.gameObject.activeSelf);

        BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(view);
        Assert.IsFalse(actions.activeSelf);
    }

    [Test]
    public void RuntimeFeedbackSystem_CommandModePromptDoesNotAutoHide()
    {
        BattleHudRuntimeFeedbackView view = CreateFeedbackView(out GameObject panel, out TMP_Text text);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, TacticalCommandMode.Move);
        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, Time.unscaledTime + 20f);

        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Choose destination.", text.text);
    }

    [Test]
    public void RuntimeFeedbackSystem_HoldStopPromptsClearAndResultsAutoHide()
    {
        BattleHudRuntimeFeedbackView view = CreateFeedbackView(out GameObject panel, out TMP_Text text);
        float now = Time.unscaledTime;

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, TacticalCommandMode.Hold);
        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, now + 20f);
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Hold position and return fire.", text.text);
        Assert.AreEqual(TacticalCommandMode.Hold, view.CurrentCommandMode);

        BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(view);
        Assert.IsFalse(panel.activeSelf);
        Assert.AreEqual(TacticalCommandMode.None, view.CurrentCommandMode);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(view, TacticalCommandResult.Success("Holding current position."));
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Holding current position.", text.text);
        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, now + BattleHudRuntimeFeedbackUiSystemHelper.SuccessFeedbackDurationSeconds + 1f);
        Assert.IsFalse(panel.activeSelf);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, TacticalCommandMode.Stop);
        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, now + 30f);
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Stop selected units and clear orders.", text.text);
        Assert.AreEqual(TacticalCommandMode.Stop, view.CurrentCommandMode);

        BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(view);
        Assert.IsFalse(panel.activeSelf);
        Assert.AreEqual(TacticalCommandMode.None, view.CurrentCommandMode);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(view, TacticalCommandResult.Success("Stopped selected units."));
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Stopped selected units.", text.text);
        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, now + BattleHudRuntimeFeedbackUiSystemHelper.ErrorFeedbackDurationSeconds + 1f);
        Assert.IsFalse(panel.activeSelf);
    }

    [Test]
    public void RuntimeFeedbackSystem_SuccessResultAutoHidesAfterDuration()
    {
        BattleHudRuntimeFeedbackView view = CreateFeedbackView(out GameObject panel, out TMP_Text text);
        float now = Time.unscaledTime;

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(view, TacticalCommandResult.Success("Boarding 3 units."));
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Boarding 3 units.", text.text);

        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, now + BattleHudRuntimeFeedbackUiSystemHelper.SuccessFeedbackDurationSeconds * 0.5f);
        Assert.IsTrue(panel.activeSelf);

        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, now + BattleHudRuntimeFeedbackUiSystemHelper.SuccessFeedbackDurationSeconds + 1f);
        Assert.IsFalse(panel.activeSelf);
    }

    [Test]
    public void RuntimeFeedbackSystem_RejectedResultAutoHidesAfterErrorDuration()
    {
        BattleHudRuntimeFeedbackView view = CreateFeedbackView(out GameObject panel, out TMP_Text text);
        float now = Time.unscaledTime;

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(view, TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Select units or a building first.", text.text);

        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, now + BattleHudRuntimeFeedbackUiSystemHelper.ErrorFeedbackDurationSeconds * 0.5f);
        Assert.IsTrue(panel.activeSelf);

        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, now + BattleHudRuntimeFeedbackUiSystemHelper.ErrorFeedbackDurationSeconds + 1f);
        Assert.IsFalse(panel.activeSelf);
    }

    [Test]
    public void RuntimeFeedbackSystem_BoardErrorRestoresBoardPromptAndActions()
    {
        BattleHudRuntimeFeedbackView view = CreateBoardFeedbackView(
            out GameObject actions,
            out TMP_Text text,
            out Button boardAll,
            out Button cancel);
        float now = Time.unscaledTime;

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyBoardCommandMode(
            view,
            UiBoardCommandModeDirection.TransportToPassenger,
            boardAllInteractable: true);
        Assert.IsTrue(actions.activeSelf);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
            view,
            TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable, "No nearby units can board this transport."));
        Assert.AreEqual("No nearby units can board this transport.", text.text);
        Assert.IsFalse(actions.activeSelf);

        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, now + BattleHudRuntimeFeedbackUiSystemHelper.ErrorFeedbackDurationSeconds + 1f);

        Assert.AreEqual("Select units to board or use BOARD ALL.", text.text);
        Assert.IsTrue(actions.activeSelf);
        Assert.IsTrue(boardAll.gameObject.activeSelf);
        Assert.IsTrue(cancel.gameObject.activeSelf);
    }

    [Test]
    public void RuntimeFeedbackSystem_BoardSuccessClearsPromptFallbackAndAutoHides()
    {
        BattleHudRuntimeFeedbackView view = CreateBoardFeedbackView(
            out GameObject actions,
            out TMP_Text text,
            out _,
            out _);
        float now = Time.unscaledTime;

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyBoardCommandMode(
            view,
            UiBoardCommandModeDirection.TransportToPassenger,
            boardAllInteractable: true);
        BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(view);
        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(view, TacticalCommandResult.Success("Boarding 3 units."));

        Assert.AreEqual("Boarding 3 units.", text.text);
        Assert.IsFalse(actions.activeSelf);

        BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(view, now + BattleHudRuntimeFeedbackUiSystemHelper.SuccessFeedbackDurationSeconds + 1f);
        Assert.IsFalse(view.FeedbackPanel.activeSelf);
        Assert.IsFalse(actions.activeSelf);
    }

    [Test]
    public void SelectButtonClick_QueuesRequestAndFeedbackClearsBoardActions()
    {
        _root = new GameObject("SelectButtonFeedbackBoundary");
        var controlsObject = new GameObject("Controls");
        var selectButtonObject = new GameObject("SelectButton");
        var feedbackObject = new GameObject("FeedbackView");
        var panel = new GameObject("FeedbackPanel");
        var textNode = new GameObject("FeedbackText");
        var actions = new GameObject("Actions");
        var boardAllObject = new GameObject("BoardAllButton");
        var cancelObject = new GameObject("CancelButton");

        controlsObject.transform.SetParent(_root.transform);
        selectButtonObject.transform.SetParent(controlsObject.transform);
        feedbackObject.transform.SetParent(_root.transform);
        panel.transform.SetParent(feedbackObject.transform);
        textNode.transform.SetParent(panel.transform);
        actions.transform.SetParent(panel.transform);
        boardAllObject.transform.SetParent(actions.transform);
        cancelObject.transform.SetParent(actions.transform);

        var controls = controlsObject.AddComponent<MatchOverlayCommandControlsView>();
        Button selectButton = selectButtonObject.AddComponent<Button>();
        var feedbackView = feedbackObject.AddComponent<BattleHudRuntimeFeedbackView>();
        TMP_Text text = textNode.AddComponent<TextMeshProUGUI>();
        Button boardAll = boardAllObject.AddComponent<Button>();
        Button cancel = cancelObject.AddComponent<Button>();

        SetPrivateField(controls, "selectButton", selectButton);
        SetPrivateField(feedbackView, "feedbackPanel", panel);
        SetPrivateField(feedbackView, "feedbackText", text);
        SetPrivateField(feedbackView, "feedbackActionsRoot", actions);
        SetPrivateField(feedbackView, "boardAllButton", boardAll);
        SetPrivateField(feedbackView, "cancelButton", cancel);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyBoardCommandMode(
            feedbackView,
            UiBoardCommandModeDirection.TransportToPassenger,
            boardAllInteractable: true);
        Assert.IsTrue(actions.activeSelf, "Test setup must start with Board feedback actions visible.");

        var commandSink = new RecordingSelectionUiCommand();
        var inputSystem = new MatchOverlayCommandInputUiSystemHelper();
        inputSystem.Bind(controls, commandSink, feedbackView);

        selectButton.onClick.Invoke();

        Assert.AreEqual(1, commandSink.EnterSelectionModeRequests);
        Assert.IsTrue(actions.activeSelf, "Input click must queue an ECS request without directly clearing Board presentation.");

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(feedbackView, TacticalCommandMode.Select);

        Assert.IsFalse(actions.activeSelf, "Command-mode feedback must clear stale Board feedback actions.");
    }

    [Test]
    public void ScanButtonClick_WhenReadModelRejectsShowsFeedbackWithoutQueueing()
    {
        _root = new GameObject("ScanButtonFeedbackBoundary");
        var controlsObject = new GameObject("Controls");
        var scanButtonObject = new GameObject("ScanButton");
        var feedbackObject = new GameObject("FeedbackView");
        var panel = new GameObject("FeedbackPanel");
        var textNode = new GameObject("FeedbackText");

        controlsObject.transform.SetParent(_root.transform);
        scanButtonObject.transform.SetParent(controlsObject.transform);
        feedbackObject.transform.SetParent(_root.transform);
        panel.transform.SetParent(feedbackObject.transform);
        textNode.transform.SetParent(panel.transform);

        var controls = controlsObject.AddComponent<MatchOverlayCommandControlsView>();
        Button scanButton = scanButtonObject.AddComponent<Button>();
        var feedbackView = feedbackObject.AddComponent<BattleHudRuntimeFeedbackView>();
        TMP_Text text = textNode.AddComponent<TextMeshProUGUI>();

        SetPrivateField(controls, "scanButton", scanButton);
        SetPrivateField(feedbackView, "feedbackPanel", panel);
        SetPrivateField(feedbackView, "feedbackText", text);

        var commandSink = new RecordingSelectionUiCommand();
        var readModel = new FakeSelectionUiReadModel
        {
            CanScan = false,
            ScanReason = TacticalCommandReasonCode.ScanUnavailable
        };
        var inputSystem = new MatchOverlayCommandInputUiSystemHelper();
        inputSystem.Bind(
            controls,
            commandSink,
            feedbackView,
            selectionUiReadModel: readModel);

        Assert.IsTrue(scanButton.interactable, "Scan must remain pressable so unavailable commands can show feedback.");

        scanButton.onClick.Invoke();

        Assert.AreEqual(0, commandSink.ScanModeRequests, "Rejected Scan must not queue scan target mode.");
        Assert.IsTrue(panel.activeSelf, "Rejected Scan must show HUD feedback.");
        Assert.AreEqual("Select a scanner or combat unit first.", text.text);
    }

    [Test]
    public void HoldStopScanButtons_WhenNoSelectionShowRecommendedFeedback()
    {
        _root = new GameObject("NoSelectionCommandFeedbackBoundary");
        var controlsObject = new GameObject("Controls");
        var holdButtonObject = new GameObject("HoldButton");
        var stopButtonObject = new GameObject("StopButton");
        var scanButtonObject = new GameObject("ScanButton");
        var feedbackObject = new GameObject("FeedbackView");
        var panel = new GameObject("FeedbackPanel");
        var textNode = new GameObject("FeedbackText");

        controlsObject.transform.SetParent(_root.transform);
        holdButtonObject.transform.SetParent(controlsObject.transform);
        stopButtonObject.transform.SetParent(controlsObject.transform);
        scanButtonObject.transform.SetParent(controlsObject.transform);
        feedbackObject.transform.SetParent(_root.transform);
        panel.transform.SetParent(feedbackObject.transform);
        textNode.transform.SetParent(panel.transform);

        var controls = controlsObject.AddComponent<MatchOverlayCommandControlsView>();
        Button holdButton = holdButtonObject.AddComponent<Button>();
        Button stopButton = stopButtonObject.AddComponent<Button>();
        Button scanButton = scanButtonObject.AddComponent<Button>();
        var feedbackView = feedbackObject.AddComponent<BattleHudRuntimeFeedbackView>();
        TMP_Text text = textNode.AddComponent<TextMeshProUGUI>();

        SetPrivateField(controls, "holdButton", holdButton);
        SetPrivateField(controls, "stopButton", stopButton);
        SetPrivateField(controls, "scanButton", scanButton);
        SetPrivateField(feedbackView, "feedbackPanel", panel);
        SetPrivateField(feedbackView, "feedbackText", text);

        var commandSink = new RecordingSelectionUiCommand();
        var readModel = new FakeSelectionUiReadModel
        {
            HasSelectedUnits = false,
            CanHold = true,
            CanStop = true,
            CanScan = true,
            HoldReason = TacticalCommandReasonCode.NoSelection,
            StopReason = TacticalCommandReasonCode.NoSelection,
            ScanReason = TacticalCommandReasonCode.NoSelection
        };
        var inputSystem = new MatchOverlayCommandInputUiSystemHelper();
        inputSystem.Bind(
            controls,
            commandSink,
            feedbackView,
            selectionUiReadModel: readModel);

        holdButton.onClick.Invoke();
        Assert.IsTrue(panel.activeSelf, "Rejected Hold must show HUD feedback.");
        Assert.AreEqual("Select units before holding position.", text.text);
        Assert.AreEqual(0, commandSink.HoldRequests, "Rejected Hold must not queue a hold command.");

        stopButton.onClick.Invoke();
        Assert.IsTrue(panel.activeSelf, "Rejected Stop must show HUD feedback.");
        Assert.AreEqual("Select units before stopping orders.", text.text);
        Assert.AreEqual(0, commandSink.StopRequests, "Rejected Stop must not queue a stop command.");

        scanButton.onClick.Invoke();
        Assert.IsTrue(panel.activeSelf, "Rejected Scan must show HUD feedback.");
        Assert.AreEqual("Select a scanner or combat unit first.", text.text);
        Assert.AreEqual(0, commandSink.ScanModeRequests, "Rejected Scan must not queue scan target mode.");
    }

    [Test]
    public void MatchHudContentPrefab_UpdatesActualFeedbackIconForMessageSeverity()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab, $"Missing Match HUD content prefab at {MatchHudContentPrefabPath}.");
        _root = Object.Instantiate(prefab);

        var view = _root.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
        Assert.NotNull(view, "SCN08 Match HUD content must expose BattleHudRuntimeFeedbackView.");
        Image icon = view.FeedbackIcon;
        Assert.NotNull(icon, "SCN08 command feedback must serialize the actual FeedbackPanel Icon image.");
        Assert.AreSame(
            FindRequiredImage(_root.transform, "FooterContent/FeedbackPanel/Frame/Icon"),
            icon,
            "Runtime command feedback must update the FooterContent feedback icon.");

        var serializedView = new SerializedObject(view);
        Sprite ready = (Sprite)serializedView.FindProperty("readyIcon").objectReferenceValue;
        Sprite error = (Sprite)serializedView.FindProperty("errorIcon").objectReferenceValue;
        Sprite warning = (Sprite)serializedView.FindProperty("warningIcon").objectReferenceValue;
        Assert.NotNull(ready);
        Assert.NotNull(error);
        Assert.NotNull(warning);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(view, TacticalCommandMode.Move);
        Assert.AreSame(ready, icon.sprite);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
            view,
            TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
        Assert.AreSame(error, icon.sprite);

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
            view,
            TacticalCommandResult.Success("PLACEMENT CANCELLED"));
        Assert.AreSame(warning, icon.sprite);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing private field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static Sprite CreateTestSprite(string name)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        sprite.name = name;
        return sprite;
    }

    private static Image FindRequiredImage(Transform root, string path)
    {
        Transform child = root.Find(path);
        Assert.NotNull(child, $"Missing prefab path {path}.");

        Image image = child.GetComponent<Image>();
        Assert.NotNull(image, $"Missing Image component at {path}.");
        return image;
    }

    private static Button FindRequiredButton(Transform root, string path)
    {
        Transform child = root.Find(path);
        Assert.NotNull(child, $"Missing prefab path {path}.");

        Button button = child.GetComponent<Button>();
        Assert.NotNull(button, $"Missing Button component at {path}.");
        return button;
    }

    private BattleHudRuntimeFeedbackView CreateFeedbackView(out GameObject panel, out TMP_Text text)
    {
        _root = new GameObject("FeedbackView");
        panel = new GameObject("FeedbackPanel");
        var textNode = new GameObject("FeedbackText");
        panel.transform.SetParent(_root.transform);
        textNode.transform.SetParent(panel.transform);

        var view = _root.AddComponent<BattleHudRuntimeFeedbackView>();
        text = textNode.AddComponent<TextMeshProUGUI>();
        SetPrivateField(view, "feedbackPanel", panel);
        SetPrivateField(view, "feedbackText", text);
        return view;
    }

    private BattleHudRuntimeFeedbackView CreateBoardFeedbackView(
        out GameObject actions,
        out TMP_Text text,
        out Button boardAll,
        out Button cancel)
    {
        BattleHudRuntimeFeedbackView view = CreateFeedbackView(out GameObject panel, out text);
        actions = new GameObject("Actions");
        var boardAllNode = new GameObject("BoardAllButton");
        var cancelNode = new GameObject("CancelButton");
        actions.transform.SetParent(panel.transform);
        boardAllNode.transform.SetParent(actions.transform);
        cancelNode.transform.SetParent(actions.transform);
        boardAll = boardAllNode.AddComponent<Button>();
        cancel = cancelNode.AddComponent<Button>();
        SetPrivateField(view, "feedbackActionsRoot", actions);
        SetPrivateField(view, "boardAllButton", boardAll);
        SetPrivateField(view, "cancelButton", cancel);
        return view;
    }

    private static void AssertObjectReference(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        Assert.NotNull(property, $"Missing serialized property {propertyName}.");
        Assert.NotNull(property.objectReferenceValue, $"Missing serialized reference {propertyName}.");
    }

    private sealed class RecordingSelectionUiCommand : ISelectionUiCommand
    {
        public int EnterSelectionModeRequests { get; private set; }
        public int ScanModeRequests { get; private set; }
        public int HoldRequests { get; private set; }
        public int StopRequests { get; private set; }

        public void CaptureUiClickSequence()
        {
        }

        public bool RequestDeselectAll() => true;

        public bool RequestEnterSelectionMode()
        {
            EnterSelectionModeRequests++;
            return true;
        }

        public bool RequestExitSelectionMode() => true;

        public bool RequestMoveCommandMode() => true;

        public bool RequestAttackCommandMode() => true;

        public bool RequestScanCommandMode()
        {
            ScanModeRequests++;
            return true;
        }

        public bool RequestBoardTargetMode() => true;

        public bool RequestToggleTacticalFollowCameraMode() => true;

        public bool RequestHoldPosition()
        {
            HoldRequests++;
            return true;
        }

        public bool RequestStop()
        {
            StopRequests++;
            return true;
        }

        public bool RequestBoardAllSelectedTransport() => true;

        public bool RequestCancelActiveCommandMode() => true;
    }

    private sealed class FakeSelectionUiReadModel : ISelectionUiReadModel
    {
        public bool CanHold;
        public bool CanStop;
        public bool CanScan;
        public bool HasSelectedUnits = true;
        public TacticalCommandReasonCode HoldReason;
        public TacticalCommandReasonCode StopReason;
        public TacticalCommandReasonCode ScanReason;

        public bool HasAnySelectedUnits => HasSelectedUnits;
        public bool FocusedUnitCanHold => CanHold;
        public TacticalCommandReasonCode FocusedUnitHoldDisabledReason => HoldReason;
        public bool FocusedUnitCanStop => CanStop;
        public TacticalCommandReasonCode FocusedUnitStopDisabledReason => StopReason;
        public bool FocusedUnitCanScan => CanScan;
        public TacticalCommandReasonCode FocusedUnitScanDisabledReason => ScanReason;
    }
}
