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
            RunValidationStep(nameof(LegacySupportCommandTabRoutesToScanCommandMode), tests => tests.LegacySupportCommandTabRoutesToScanCommandMode());
            Debug.Log("[MatchHudCommandControlsCurrentPrefabValidation] result=Passed tests=4");
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
        Assert.NotNull(rootImage, "Scan command must have the same transparent root Image hit target as the working command buttons.");
        Assert.IsTrue(rootImage.raycastTarget, "Scan command root Image must receive raycasts.");
        Assert.AreSame(rootImage, scanButton.targetGraphic, "Scan command Button.targetGraphic must point to its own root hit target, not another tab frame.");
        Assert.IsTrue(scanButton.targetGraphic.transform.IsChildOf(scanButton.transform), "Scan command target graphic must belong to the ScanCommand hierarchy.");
    }

    [Test]
    public void MatchHudCommandButtonsSubmitSelectionCommandRequests()
    {
        MatchOverlayCommandControlsView controls = LoadControls();
        var inputSystem = new MatchOverlayCommandInputSystem();
        inputSystem.Bind(controls, new SelectionUiCommandSystem());

        AssertClickQueues(controls.SelectButton, RtsSelectionCommandIntentKind.EnterSelectionMode);
        AssertClickQueues(controls.MoveButton, RtsSelectionCommandIntentKind.EnterMoveTargetMode);
        AssertClickQueues(controls.AttackButton, RtsSelectionCommandIntentKind.EnterAttackTargetMode);
        AssertClickQueues(controls.ScanButton, RtsSelectionCommandIntentKind.EnterScanTargetMode);
        AssertClickQueues(controls.HoldButton, RtsSelectionCommandIntentKind.HoldPosition);
        AssertClickQueues(controls.StopButton, RtsSelectionCommandIntentKind.Stop);

        inputSystem.Unbind(controls);
    }

    [Test]
    public void LegacySupportCommandTabRoutesToScanCommandMode()
    {
        MatchOverlayCommandControlsView controls = LoadControls();
        Button supportButton = FindCommandTabButton(controls, "SupportCommand");
        Assert.NotNull(supportButton, "SCN08 Match HUD currently exposes SupportCommand as the legacy scan/support tab.");

        var inputSystem = new MatchOverlayCommandInputSystem();
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
        var inputState = new RtsSelectionInputSystem();
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
