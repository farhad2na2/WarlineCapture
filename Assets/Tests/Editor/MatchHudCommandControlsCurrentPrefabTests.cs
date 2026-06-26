using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
            RunValidationStep(nameof(LegacySupportCommandTabRoutesToScanCommandMode), tests => tests.LegacySupportCommandTabRoutesToScanCommandMode());
            Debug.Log("[MatchHudCommandControlsCurrentPrefabValidation] result=Passed tests=6");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudCommandControlsCurrentPrefabValidation] result=Failed");
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
        inputSystem.Bind(controls, new SelectionUiCommandSystem());

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
        var selectionUiCommand = new SelectionUiCommandSystem();
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
            var selectionUiCommand = new SelectionUiCommandSystem();
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
    public void LegacySupportCommandTabRoutesToScanCommandMode()
    {
        MatchOverlayCommandControlsView controls = LoadControls();
        Button supportButton = FindCommandTabButton(controls, "SupportCommand");
        Assert.NotNull(supportButton, "SCN08 Match HUD currently exposes SupportCommand as the legacy scan/support tab.");

        var inputSystem = new MatchOverlayCommandInputUiSystemHelper();
        inputSystem.Bind(controls, new SelectionUiCommandSystem());

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

    private static void AssertButton(Button button, string label)
    {
        Assert.NotNull(button, $"{label} button reference is required.");
        Assert.IsTrue(button.interactable, $"{label} button should be interactable.");
    }
}
