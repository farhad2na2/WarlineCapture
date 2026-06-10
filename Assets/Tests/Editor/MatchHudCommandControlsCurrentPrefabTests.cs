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

    private MatchOverlayCommandControlsView LoadControls()
    {
        MatchOverlayCommandControlsView controls = _instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
        Assert.NotNull(controls, "SCN08_MatchHudContent must expose MatchOverlayCommandControlsView through its serialized content hierarchy.");
        return controls;
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
