using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleHudRuntimeFeedbackSystemConnectionTests
{
    private const string MatchOverlayPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab";

    private World _world;
    private World _previousWorld;
    private GameObject _overlay;

    [SetUp]
    public void SetUp()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("BattleHudRuntimeFeedbackSystemConnectionTests");
        World.DefaultGameObjectInjectionWorld = _world;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchOverlayPrefabPath);
        Assert.NotNull(prefab, $"Missing match overlay prefab at {MatchOverlayPrefabPath}.");
        _overlay = Object.Instantiate(prefab);
        InvokeAwake(_overlay.GetComponent<BattleHudTacticalFeedbackView>());
    }

    [TearDown]
    public void TearDown()
    {
        if (_overlay != null)
            Object.DestroyImmediate(_overlay);

        if (_world != null && _world.IsCreated)
            _world.Dispose();

        World.DefaultGameObjectInjectionWorld = _previousWorld;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = false;
    }

    [Test]
    public void SelectionSystems_FocusAndDeselectPublishSelectionStateToBattleHud()
    {
        EntityManager em = _world.EntityManager;
        Entity unit = CreatePlayerUnit(em, "Alpha Squad", new int2(4, 6), 87);
        var state = new SelectionStateSystem();

        Assert.IsTrue(FocusUnit(em, unit, state));
        Assert.IsTrue(_overlay.transform.Find("SelectedEntityPanel").gameObject.activeSelf);
        AssertText("SelectedEntityPanel/NameText", "Alpha Squad");
        StringAssert.Contains("HP 87/100", TextAt("SelectedEntityPanel/StatusText"));

        UnitHealth health = em.GetComponentData<UnitHealth>(unit);
        health.Current = 42;
        em.SetComponentData(unit, health);
        ApplyHudSelection(em, unit);
        StringAssert.Contains("HP 42/100", TextAt("SelectedEntityPanel/StatusText"));

        ClearSelection(em, state, "PresenterSelectionTest");
        Assert.IsFalse(_overlay.transform.Find("SelectedEntityPanel").gameObject.activeSelf);
    }

    [Test]
    public void SelectionSystems_AttackTargetingPublishesCommandModeAndRejectedNoSelection()
    {
        EntityManager em = _world.EntityManager;
        Entity unit = CreatePlayerUnit(em, "Rifle Squad", new int2(2, 3), 100);
        em.AddComponentData(unit, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.AddComponentData(unit, new UnitAttack
        {
            Range = 4f,
            CooldownSeconds = 1f,
            Damage = 10,
            TraceVisibleSeconds = 0.05f
        });
        var state = new SelectionStateSystem();

        Assert.IsTrue(FocusUnit(em, unit, state));
        Assert.IsTrue(ArmFocusedAttackTargetMode(em, state));
        Assert.IsTrue(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);
        AssertText("CommandModeBanner/ModeText", "ATTACK ORDER");

        ClearSelection(em, state, "PresenterCommandModeTest");
        Assert.IsFalse(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);

        Assert.IsFalse(ArmFocusedAttackTargetMode(em, state));
        Assert.IsTrue(_overlay.transform.Find("InvalidCommandToast").gameObject.activeSelf);
        AssertText("InvalidCommandToast/MessageText", "Select a squad first.");
    }

    [Test]
    public void RuntimeFeedbackSystem_DoesNotExposeStaticWorldMarkerPreviewDuringLiveOrders()
    {
        Transform markerLayer = _overlay.transform.Find("WorldCommandMarkerLayer");
        Assert.NotNull(markerLayer);

        BattleHudRuntimeFeedbackView view = _overlay.GetComponent<BattleHudRuntimeFeedbackView>();
        Assert.NotNull(view);

        BattleHudRuntimeFeedbackSystem.SetWorldMarkersVisible(view, true);
        Assert.IsFalse(
            markerLayer.gameObject.activeSelf,
            "Live gameplay must not surface fixed screen-space marker preview art over scripted-start units.");
    }

    [Test]
    public void SelectionSystems_HoldAndStopClearCommandModesAndOrders()
    {
        EntityManager em = _world.EntityManager;
        Entity unit = CreatePlayerUnit(em, "Bravo Squad", new int2(7, 8), 100);
        AddActiveOrderComponents(em, unit);
        var state = new SelectionStateSystem();

        Assert.IsTrue(FocusUnit(em, unit, state));
        Assert.IsTrue(IssueImmediateSelectedUnitOrder(em, TacticalCommandMode.Hold));
        Assert.IsFalse(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);
        Assert.IsFalse(_overlay.transform.Find("InvalidCommandToast").gameObject.activeSelf);
        Assert.IsFalse(em.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRange>(unit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRetryCooldown>(unit));
        Assert.IsFalse(em.HasComponent<UnitLongDistanceMove>(unit));
        Assert.IsFalse(em.HasComponent<ManualMoveGroupMemberTag>(unit));
        Assert.IsFalse(em.HasComponent<BaseBreachOrder>(unit));
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(unit));
        Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(unit));
        Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit));
        Assert.AreEqual(0f, em.GetComponentData<UnitVehicleKinematics>(unit).CurrentSpeed);
        Assert.AreEqual(1, em.GetComponentData<UnitCombat>(unit).AutoEngage);
        StringAssert.Contains("HOLDING", TextAt("SelectedEntityPanel/StatusText"));

        AddActiveOrderComponents(em, unit);
        Assert.IsTrue(IssueImmediateSelectedUnitOrder(em, TacticalCommandMode.Stop));
        Assert.IsFalse(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);
        Assert.IsFalse(em.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRetryCooldown>(unit));
        Assert.IsFalse(em.HasComponent<UnitLongDistanceMove>(unit));
        Assert.IsFalse(em.HasComponent<ManualMoveGroupMemberTag>(unit));
        Assert.IsFalse(em.HasComponent<BaseBreachOrder>(unit));
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(unit));
        Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(unit));
        Assert.IsFalse(em.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit));
        Assert.AreEqual(0f, em.GetComponentData<UnitVehicleKinematics>(unit).CurrentSpeed);
        Assert.AreEqual(0, em.GetComponentData<UnitCombat>(unit).AutoEngage);
        StringAssert.Contains("IDLE", TextAt("SelectedEntityPanel/StatusText"));
    }

    [Test]
    public void MatchOverlay_HoldAndStopControlsInvokeSelectionSystemOrders()
    {
        EntityManager em = _world.EntityManager;
        Entity unit = CreatePlayerUnit(em, "Charlie Squad", new int2(5, 6), 100);
        AddActiveOrderComponents(em, unit);
        var state = new SelectionStateSystem();

        Assert.IsTrue(FocusUnit(em, unit, state));

        MatchOverlayCommandControlsView controls = _overlay.GetComponent<MatchOverlayCommandControlsView>();
        Assert.NotNull(controls);
        var commandInputSystem = new MatchOverlayCommandInputSystem();
        commandInputSystem.Bind(controls, new SelectionUiCommandSystem());

        Button holdButton = _overlay.transform.Find("CommandBar/HoldButton").GetComponent<Button>();
        Button stopButton = _overlay.transform.Find("CommandBar/StopButton").GetComponent<Button>();
        Assert.NotNull(holdButton);
        Assert.NotNull(stopButton);

        holdButton.onClick.Invoke();
        ProcessSelectionUiCommandRequests(em);
        Assert.IsFalse(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);
        Assert.IsFalse(_overlay.transform.Find("InvalidCommandToast").gameObject.activeSelf);
        Assert.IsFalse(em.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRange>(unit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(unit));
        Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit));
        Assert.AreEqual(1, em.GetComponentData<UnitCombat>(unit).AutoEngage);
        StringAssert.Contains("HOLDING", TextAt("SelectedEntityPanel/StatusText"));

        AddActiveOrderComponents(em, unit);
        stopButton.onClick.Invoke();
        ProcessSelectionUiCommandRequests(em);
        Assert.IsFalse(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);
        Assert.IsFalse(em.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRange>(unit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(unit));
        Assert.IsFalse(em.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit));
        Assert.AreEqual(0, em.GetComponentData<UnitCombat>(unit).AutoEngage);
        StringAssert.Contains("IDLE", TextAt("SelectedEntityPanel/StatusText"));
    }

    [Test]
    public void MatchOverlay_SelectControlEntersSelectionMode()
    {
        MatchOverlayCommandControlsView controls = _overlay.GetComponent<MatchOverlayCommandControlsView>();
        Assert.NotNull(controls);
        var commandInputSystem = new MatchOverlayCommandInputSystem();
        commandInputSystem.Bind(controls, new SelectionUiCommandSystem());

        Button selectButton = _overlay.transform.Find("CommandBar/SelectButton").GetComponent<Button>();
        Assert.NotNull(selectButton);
        Assert.IsTrue(selectButton.gameObject.activeSelf, "Select must remain visible so players can enter explicit selection mode.");

        selectButton.onClick.Invoke();
        ProcessSelectionUiCommandRequests(_world.EntityManager);

        Assert.IsTrue(new RuntimeGameplayStateSystem().SelectionModeActive);
        Assert.IsTrue(new RuntimeGameplayStateSystem().SuppressNextWorldClick);
        AssertText("CommandModeBanner/ModeText", "SELECT SQUAD");
    }

    [Test]
    public void MatchOverlay_CommandWheelStopControlInvokesStopAndClosesWheel()
    {
        EntityManager em = _world.EntityManager;
        Entity unit = CreatePlayerUnit(em, "Delta Squad", new int2(3, 4), 100);
        AddActiveOrderComponents(em, unit);
        var state = new SelectionStateSystem();

        Assert.IsTrue(FocusUnit(em, unit, state));

        MatchOverlayCommandControlsView controls = _overlay.GetComponent<MatchOverlayCommandControlsView>();
        CommandWheelPanelView wheel = _overlay.GetComponent<CommandWheelPanelView>();
        Assert.NotNull(controls);
        Assert.NotNull(wheel);
        var commandInputSystem = new MatchOverlayCommandInputSystem();
        commandInputSystem.Bind(controls, new SelectionUiCommandSystem());
        InvokeAwake(wheel);

        wheel.Open();
        Assert.IsTrue(wheel.IsOpen);
        controls.CommandWheelStopButton.onClick.Invoke();
        ProcessSelectionUiCommandRequests(em);

        Assert.IsFalse(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);
        Assert.IsFalse(wheel.IsOpen);
        Assert.IsFalse(em.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathFollow>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRange>(unit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit));
    }

    [Test]
    public void BuildDrawerAndCommandWheelPublishBuildAndSpecialModes()
    {
        BuildDrawerPanelView drawer = _overlay.GetComponent<BuildDrawerPanelView>();
        CommandWheelPanelView wheel = _overlay.GetComponent<CommandWheelPanelView>();
        InvokeAwake(drawer);
        InvokeAwake(wheel);

        drawer.Open();
        Assert.IsTrue(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);
        AssertText("CommandModeBanner/ModeText", "BUILD MODE");

        drawer.Close();
        Assert.IsFalse(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);

        wheel.Open();
        Assert.IsTrue(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);
        AssertText("CommandModeBanner/ModeText", "SPECIAL ORDER");

        wheel.Close();
        Assert.IsFalse(_overlay.transform.Find("CommandModeBanner").gameObject.activeSelf);
    }

    private static Entity CreatePlayerUnit(EntityManager em, string displayName, int2 cell, int health)
    {
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new Faction { Id = 0 });
        em.AddComponentData(entity, new UnitGrid { Cell = cell });
        em.AddComponentData(entity, new UnitHealth { Current = health, Max = 100 });
        em.AddComponentData(entity, new UnitDisplayInfo
        {
            Name = new FixedString64Bytes(displayName),
            Description = new FixedString128Bytes("Runtime feedback system test unit")
        });
        em.AddComponentData(entity, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 5f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        em.AddComponentData(entity, LocalTransform.FromPosition(new float3(cell.x, 0f, cell.y)));
        return entity;
    }

    private static void AddActiveOrderComponents(EntityManager em, Entity entity)
    {
        SetComponent(em, entity, new UnitTarget { Cell = new int2(10, 10) });
        SetComponent(em, entity, new UnitPathRequest { Goal = new int2(10, 10) });
        SetComponent(em, entity, new UnitPathFollow { PathIndex = 0 });
        SetComponent(em, entity, new UnitPathRange { Start = 0, Length = 2 });
        SetComponent(em, entity, new UnitPathRetryCooldown { ResumeFrame = 20 });
        SetComponent(em, entity, new UnitLongDistanceMove { FinalGoal = new int2(11, 11) });
        SetComponent(em, entity, new UnitTransportBoardingTarget { Transport = Entity.Null, Goal = new int2(12, 12) });
        SetComponent(em, entity, new UnitTransportRopeDisembarkRequest { ReferenceCell = new int2(13, 13) });
        SetComponent(em, entity, new UnitResourceHaulOrder { SourceBuildingId = 1, DestinationBuildingId = 2, TargetCell = new int2(14, 14), Phase = 1 });
        SetComponent(em, entity, new BaseBreachOrder { FinalTarget = Entity.Null, FinalCell = new int2(15, 15), Stage = BaseBreachOrder.StageMovingToFinalTarget });
        SetComponent(em, entity, new UnitVehicleKinematics { CurrentSpeed = 5f, StallSeconds = 2f });
        SetComponent(em, entity, new EngageTarget
        {
            Target = Entity.Null,
            Cell = new int2(10, 10),
            Position = new float3(10f, 0f, 10f),
            IsCommanded = 1
        });
        if (!em.HasComponent<AutoWanderMoveTag>(entity))
            em.AddComponent<AutoWanderMoveTag>(entity);
        if (!em.HasComponent<ManualMoveGroupMemberTag>(entity))
            em.AddComponent<ManualMoveGroupMemberTag>(entity);
    }

    private static bool FocusUnit(EntityManager em, Entity entity, SelectionStateSystem state)
    {
        var lifecycle = new FocusedUnitLifecycleSystem();
        return lifecycle.FocusUnitEntity(
            em,
            entity,
            state,
            new UnitTargetOrderSystem(),
            "BattleHudRuntimeFeedbackSystemConnectionTests",
            "BattleHudRuntimeFeedbackSystemConnectionTests",
            null,
            null,
            () => ClearHudSelection(em),
            (entityManager, focusedEntity) => ApplyHudSelection(entityManager, focusedEntity));
    }

    private static void ClearSelection(EntityManager em, SelectionStateSystem state, string reason)
    {
        var lifecycle = new FocusedUnitLifecycleSystem();
        lifecycle.ClearCurrentSelection(
            em,
            state,
            reason,
            null,
            () => ClearHudSelection(em));
        lifecycle.ClearFocusedUnit(state);
        ClearHudCommandMode(em);
    }

    private static bool ArmFocusedAttackTargetMode(EntityManager em, SelectionStateSystem state)
    {
        Entity focused = state.FocusedUnit;
        if (focused == Entity.Null ||
            !em.Exists(focused) ||
            !em.HasComponent<UnitCombat>(focused) ||
            em.GetComponentData<UnitCombat>(focused).CanAttack == 0)
        {
            ApplyHudCommandResult(em, TacticalCommandResult.Rejected(
                focused == Entity.Null ? TacticalCommandReasonCode.NoSelection : TacticalCommandReasonCode.TargetNotAttackable));
            return false;
        }

        ApplyHudCommandMode(em, TacticalCommandMode.Attack);
        return true;
    }

    private static bool IssueImmediateSelectedUnitOrder(EntityManager em, TacticalCommandMode mode)
    {
        bool clearEngageTarget = mode == TacticalCommandMode.Stop || mode == TacticalCommandMode.Hold;
        bool issued = new FocusedUnitCommandSystem().IssueImmediateSelectedUnitOrder(
            em,
            clearEngageTarget,
            mode == TacticalCommandMode.Hold,
            new UnitMoveOrderSystem());
        if (issued)
        {
            ApplyHudCommandMode(em, mode);
            ApplyHudCommandResult(em, TacticalCommandResult.Success());
            ClearHudCommandMode(em);
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using NativeArray<Entity> selectedEntities = query.ToEntityArray(Allocator.Temp);
            if (selectedEntities.Length > 0)
                ApplyHudSelection(em, selectedEntities[0]);
        }
        return issued;
    }

    private static void ProcessSelectionUiCommandRequests(EntityManager em)
    {
        var input = new RtsSelectionInputSystem();
        if (!input.TryGetCommandBuffers(
                out _,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
                out DynamicBuffer<RtsSelectionCommandResultElement> results))
        {
            return;
        }

        var pendingKinds = new List<RtsSelectionCommandIntentKind>(requests.Length);
        for (int i = 0; i < requests.Length; i++)
            pendingKinds.Add(requests[i].Kind);

        requests.Clear();

        foreach (RtsSelectionCommandIntentKind kind in pendingKinds)
        {
            if (kind == RtsSelectionCommandIntentKind.HoldPosition)
                IssueImmediateSelectedUnitOrder(em, TacticalCommandMode.Hold);
            else if (kind == RtsSelectionCommandIntentKind.Stop)
                IssueImmediateSelectedUnitOrder(em, TacticalCommandMode.Stop);
            else if (kind == RtsSelectionCommandIntentKind.EnterSelectionMode)
            {
                var runtimeGameplayStateSystem = new RuntimeGameplayStateSystem();
                runtimeGameplayStateSystem.SelectionModeActive = true;
                runtimeGameplayStateSystem.SuppressNextWorldClick = true;
                ApplyHudCommandMode(em, TacticalCommandMode.Select);
            }
            else if (kind == RtsSelectionCommandIntentKind.ExitSelectionMode)
            {
                var runtimeGameplayStateSystem = new RuntimeGameplayStateSystem();
                runtimeGameplayStateSystem.SelectionModeActive = false;
                runtimeGameplayStateSystem.SuppressNextWorldClick = true;
                ClearHudCommandMode(em);
            }
        }
    }

    private static void ApplyHudSelection(EntityManager em, Entity entity)
    {
        new SelectionHudFeedbackSystem().ApplySelection(em, entity, new SelectionUiQuerySystem());
    }

    private static void ClearHudSelection(EntityManager em)
    {
        new SelectionHudFeedbackSystem().ClearSelection(em);
    }

    private static void ApplyHudCommandMode(EntityManager em, TacticalCommandMode mode)
    {
        new SelectionHudFeedbackSystem().ApplyCommandMode(em, mode);
    }

    private static void ClearHudCommandMode(EntityManager em)
    {
        new SelectionHudFeedbackSystem().ClearCommandMode(em);
    }

    private static void ApplyHudCommandResult(EntityManager em, TacticalCommandResult result)
    {
        new SelectionHudFeedbackSystem().ApplyCommandResult(em, result);
    }

    private static void SetComponent<T>(EntityManager em, Entity entity, T component)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            em.SetComponentData(entity, component);
        else
            em.AddComponentData(entity, component);
    }

    private static void InvokeAwake(MonoBehaviour component)
    {
        Assert.NotNull(component);
        MethodInfo awake = component.GetType().GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(awake);
        awake.Invoke(component, null);
    }

    private void AssertText(string path, string expected)
    {
        Assert.AreEqual(expected, TextAt(path));
    }

    private string TextAt(string path)
    {
        Transform node = _overlay.transform.Find(path);
        Assert.NotNull(node, $"Missing UI node {path}.");
        TMP_Text text = node.GetComponent<TMP_Text>();
        Assert.NotNull(text, $"Missing TMP_Text on {path}.");
        return text.text;
    }
}
