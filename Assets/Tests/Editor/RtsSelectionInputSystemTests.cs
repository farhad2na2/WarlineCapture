#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class RtsSelectionInputSystemTests
{
    private World _previousWorld;
    private World _testWorld;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.MoveCommandRequest_StoresResolvedTargetCellAndWorldPosition());
            RunCase(test => test.AttackCommandRequest_StoresResolvedTargetEntity());
            RunCase(test => test.ScanCommandRequest_StoresResolvedTargetCellAndWorldPosition());
            RunCase(test => test.BoardTransportCommandRequest_StoresResolvedTargetEntity());
            RunCase(test => test.SelectAllCommandRequest_StoresScreenRect());
            RunCase(test => test.SelectAllCommandSystem_QueuesSelectionRectangleAndClearsCommandMode());
            RunCase(test => test.SelectAllCommandSystem_MapsVariantRequestsToSelectionFilters());
            RunCase(test => test.DeselectAllCommandSystem_RemovesSelectedTagsAndClearsCommandMode());
            RunCase(test => test.SelectionModeCommandSystem_EnterSelectionModeMutatesEcsStateAndClearsMoveRequests());
            RunCase(test => test.SelectionModeCommandSystem_ExitSelectionModeMutatesEcsState());
            RunCase(test => test.MoveTargetModeCommandSystem_ArmsMoveModeWhenSelectionCanMove());
            RunCase(test => test.MoveTargetModeCommandSystem_RejectsWithoutSelectedMoveUnit());
            RunCase(test => test.AttackTargetModeCommandSystem_ArmsAttackModeWhenSelectionCanAttack());
            RunCase(test => test.AttackTargetModeCommandSystem_AirDefenseLauncherReportsAutoEngageOnly());
            RunCase(test => test.AttackTargetModeCommandSystem_RejectsSelectedNonAttackUnit());
            RunCase(test => test.AttackTargetModeCommandSystem_MixedAirDefenseAndAttackUnitEntersTargetMode());
            RunCase(test => test.ScanTargetModeCommandSystem_ArmsScanModeAndClearsMoveRequests());
            RunCase(test => test.BoardTargetModeCommandSystem_SelectedTransportAndPassengerUsesTransportFirstMode());
            RunCase(test => test.BoardTargetModeCommandSystem_SelectedPassengerUsesPassengerToTransportMode());
            RunCase(test => test.BoardTargetModeCommandSystem_ActiveBoardModeTogglesOff());
            RunCase(test => test.BoardTargetModeCommandSystem_RejectsSelectedNonBoardableUnit());
            RunCase(test => test.CancelActiveCommandModeSystem_ClearsCommandAndSelectionMode());
            RunCase(test => test.HasPendingAttackCommandRequestsOrResults_DetectsAttackResults());
            RunCase(test => test.HasPendingMoveCommandRequestsOrResults_DetectsMoveResults());
            RunCase(test => test.HasPendingScanCommandRequestsOrResults_DetectsScanResults());
            RunCase(test => test.HasPendingTransportCommandRequests_DetectsTransportResults());
            RunCase(test => test.RuntimeInput_ActiveWorldCommandClickDoesNotFallThroughToFocusSelection());
            RunCase(test => test.RuntimeInput_AttackCommandModeAllowsCameraPanWhileTargeting());
            RunCase(test => test.RuntimeInput_TransportFirstBoardModePansUnlessPassengerDragStarts());
            RunCase(test => test.PointerTargetCommandSystem_UsesBoundaryPassForResolvedCommandTargets());
            UnityEngine.Debug.Log("[RtsSelectionInputSystemValidation] result=Passed tests=30");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[RtsSelectionInputSystemValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    private static void RunCase(Action<RtsSelectionInputSystemTests> action)
    {
        var test = new RtsSelectionInputSystemTests();
        test.SetUp();
        try
        {
            action(test);
        }
        finally
        {
            test.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _testWorld = new World("RtsSelectionInputSystemTests");
        World.DefaultGameObjectInjectionWorld = _testWorld;
    }

    [TearDown]
    public void TearDown()
    {
        if (_testWorld != null && _testWorld.IsCreated)
            _testWorld.Dispose();

        World.DefaultGameObjectInjectionWorld = _previousWorld;
        _testWorld = null;
        _previousWorld = null;
    }

    [Test]
    public void BeginPointerPress_SetsDragOriginAndClearsSelectionDrag()
    {
        var inputSystem = new RtsSelectionInputSystem
        {
            IsDraggingSelection = true,
            SelectionModeHoldArmed = true,
            BoardPassengerDragArmed = true,
            HasLiveSelectionRect = true,
            LastLiveSelectionRect = new Rect(100f, 200f, 300f, 400f)
        };
        Vector2 pointer = new(12f, 34f);

        inputSystem.BeginPointerPress(pointer, pointerPressedOverUi: true);

        Assert.AreEqual(pointer, inputSystem.DragStart);
        Assert.AreEqual(pointer, inputSystem.DragCurrent);
        Assert.AreEqual(pointer, inputSystem.LastPointerPosition);
        Assert.IsTrue(inputSystem.PointerPressedOverUi);
        Assert.IsFalse(inputSystem.IsDraggingSelection);
        Assert.IsFalse(inputSystem.SelectionModeHoldArmed);
        Assert.IsFalse(inputSystem.BoardPassengerDragArmed);
        Assert.IsFalse(inputSystem.HasLiveSelectionRect);
        Assert.AreEqual(new Rect(pointer.x, pointer.y, 0f, 0f), inputSystem.LastLiveSelectionRect);
    }

    [Test]
    public void ResetSelectionDragState_ClearsStaleRectangleAtPointerPosition()
    {
        var inputSystem = new RtsSelectionInputSystem
        {
            PointerPressedOverUi = true,
            IsDraggingSelection = true,
            SelectionModeHoldArmed = true,
            BoardPassengerDragArmed = true,
            HasLiveSelectionRect = true,
            LastLiveSelectionRect = new Rect(10f, 20f, 500f, 600f)
        };
        Vector2 pointer = new(300f, 400f);

        inputSystem.ResetSelectionDragState(pointer);

        Assert.AreEqual(pointer, inputSystem.DragStart);
        Assert.AreEqual(pointer, inputSystem.DragCurrent);
        Assert.AreEqual(pointer, inputSystem.LastPointerPosition);
        Assert.IsFalse(inputSystem.PointerPressedOverUi);
        Assert.IsFalse(inputSystem.IsDraggingSelection);
        Assert.IsFalse(inputSystem.SelectionModeHoldArmed);
        Assert.IsFalse(inputSystem.BoardPassengerDragArmed);
        Assert.IsFalse(inputSystem.HasLiveSelectionRect);
        Assert.AreEqual(new Rect(pointer.x, pointer.y, 0f, 0f), inputSystem.LastLiveSelectionRect);
    }

    [Test]
    public void LastLiveSelectionRect_RoundTripsAsMinMaxScreenRect()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Rect rightwardDrag = Rect.MinMaxRect(900f, 250f, 1200f, 500f);

        inputSystem.LastLiveSelectionRect = rightwardDrag;

        Assert.AreEqual(rightwardDrag, inputSystem.LastLiveSelectionRect);
    }

    [Test]
    public void ClearPointerReleaseState_ClearsReleaseScopedFlags()
    {
        var inputSystem = new RtsSelectionInputSystem
        {
            PointerPressedOverUi = true,
            IsDraggingSelection = true,
            SelectionModeHoldArmed = true,
            BoardPassengerDragArmed = true,
            HasLiveSelectionRect = true
        };

        inputSystem.ClearPointerReleaseState();

        Assert.IsFalse(inputSystem.PointerPressedOverUi);
        Assert.IsFalse(inputSystem.IsDraggingSelection);
        Assert.IsFalse(inputSystem.SelectionModeHoldArmed);
        Assert.IsFalse(inputSystem.BoardPassengerDragArmed);
        Assert.IsFalse(inputSystem.HasLiveSelectionRect);
    }

    [Test]
    public void CaptureUiClickSequence_SuppressesWorldReleaseUntilPointerEnds()
    {
        var inputSystem = new RtsSelectionInputSystem { IsDraggingSelection = true, BoardPassengerDragArmed = true };

        inputSystem.CaptureUiClickSequence();

        Assert.IsTrue(inputSystem.IgnoreUiClickUntilRelease);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.PointerPressedOverUi);
        Assert.IsFalse(inputSystem.IsDraggingSelection);
        Assert.IsFalse(inputSystem.BoardPassengerDragArmed);
        Assert.IsFalse(inputSystem.HasLiveSelectionRect);
    }

    [Test]
    public void CaptureUiClickSequence_CancelsQueuedAndPendingMoveOrders()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 screenPosition = new(123f, 456f);

        inputSystem.QueueMoveOrder(screenPosition, Time.frameCount);
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(screenPosition, Time.frameCount));

        inputSystem.CaptureUiClickSequence();

        Assert.IsFalse(inputSystem.HasQueuedMoveOrder);
        Assert.IsFalse(inputSystem.TryConsumeQueuedMoveOrder(Time.frameCount + 10, out _));
        Assert.AreEqual(0, inputSystem.ClearPendingMoveCommandRequests());
        Assert.GreaterOrEqual(inputSystem.IgnoreWorldCommandsUntilFrame, Time.frameCount + 1);
    }

    [Test]
    public void MoveCommandRequest_StoresResolvedTargetCellAndWorldPosition()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 screenPosition = new(123f, 456f);
        int2 targetCell = new(17, 23);
        Vector3 worldPosition = new(17.5f, 0f, 23.5f);

        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(screenPosition, targetCell, worldPosition, Time.frameCount));

        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Move, requests[0].Kind);
        Assert.AreEqual(targetCell, requests[0].TargetCell);
        Assert.AreEqual(new float3(worldPosition.x, worldPosition.y, worldPosition.z), requests[0].WorldPosition);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Cell, requests[0].TargetKind);
        Assert.AreEqual(1, requests[0].HasTargetCell);
        Assert.AreEqual(1, requests[0].HasWorldPosition);
        Assert.AreEqual(1, requests[0].HasScreenPosition);
        Assert.IsTrue(inputSystem.HasPendingMoveCommandRequestsOrResults());
    }

    [Test]
    public void ScanCommandRequest_StoresResolvedTargetCellAndWorldPosition()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 screenPosition = new(321f, 654f);
        int2 targetCell = new(9, 14);
        Vector3 worldPosition = new(9.5f, 0f, 14.5f);

        Assert.IsTrue(inputSystem.QueueScanCommandRequest(screenPosition, targetCell, worldPosition, Time.frameCount));

        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, requests[0].Kind);
        Assert.AreEqual(targetCell, requests[0].TargetCell);
        Assert.AreEqual(new float3(worldPosition.x, worldPosition.y, worldPosition.z), requests[0].WorldPosition);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Cell, requests[0].TargetKind);
        Assert.AreEqual(1, requests[0].HasTargetCell);
        Assert.AreEqual(1, requests[0].HasWorldPosition);
        Assert.AreEqual(1, requests[0].HasScreenPosition);
        Assert.IsTrue(inputSystem.HasPendingScanCommandRequestsOrResults());
    }

    [Test]
    public void AttackCommandRequest_StoresResolvedTargetEntity()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Entity target = new() { Index = 64, Version = 2 };
        Vector2 screenPosition = new(444f, 555f);

        Assert.IsTrue(inputSystem.QueueAttackCommandRequest(
            screenPosition,
            target,
            explicitAttackTargetModeActive: true,
            frame: Time.frameCount));

        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Attack, requests[0].Kind);
        Assert.AreEqual(target, requests[0].TargetEntity);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Entity, requests[0].TargetKind);
        Assert.AreEqual(1, requests[0].ExplicitAttackTargetMode);
        Assert.AreEqual(1, requests[0].HasTargetEntity);
        Assert.AreEqual(1, requests[0].HasScreenPosition);
        Assert.IsTrue(inputSystem.HasPendingAttackCommandRequestsOrResults());
    }

    [Test]
    public void BoardTransportCommandRequest_StoresResolvedTargetEntity()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Entity transport = new() { Index = 42, Version = 7 };
        Vector2 screenPosition = new(222f, 333f);

        Assert.IsTrue(inputSystem.QueueBoardTransportCommandRequest(transport, screenPosition, Time.frameCount));

        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.BoardTransport, requests[0].Kind);
        Assert.AreEqual(transport, requests[0].TargetEntity);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Entity, requests[0].TargetKind);
        Assert.AreEqual(1, requests[0].HasTargetEntity);
        Assert.AreEqual(1, requests[0].HasScreenPosition);
        Assert.IsTrue(inputSystem.HasPendingTransportCommandRequests());
    }

    [Test]
    public void SelectAllCommandRequest_StoresScreenRect()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Rect screenRect = Rect.MinMaxRect(0f, 0f, 1920f, 1080f);

        Assert.IsTrue(inputSystem.QueueSelectAllCommandRequest(screenRect, Time.frameCount));

        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.SelectAll, requests[0].Kind);
        Assert.AreEqual(RtsSelectionCommandTargetKind.ScreenRect, requests[0].TargetKind);
        Assert.AreEqual(new float2(screenRect.center.x, screenRect.center.y), requests[0].ScreenPosition);
        Assert.AreEqual(new float2(screenRect.min.x, screenRect.min.y), requests[0].DragStart);
        Assert.AreEqual(new float2(screenRect.max.x, screenRect.max.y), requests[0].DragCurrent);
        Assert.AreEqual(1, requests[0].HasScreenPosition);
        Assert.AreEqual(1, requests[0].HasScreenRect);
        Assert.IsTrue(inputSystem.HasPendingExternalSelectionCommandRequests());
    }

    [Test]
    public void SelectAllCommandSystem_QueuesSelectionRectangleAndClearsCommandMode()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Rect screenRect = Rect.MinMaxRect(10f, 20f, 300f, 420f);
        inputSystem.ArmBoardCommandMode(
            BoardCommandModeDirection.TransportToPassenger,
            _testWorld.EntityManager.CreateEntity(),
            Time.frameCount,
            oneShot: true);
        inputSystem.IgnoreNextLeftMouseRelease = true;
        inputSystem.SkipNextWorldReleaseAfterSelection = true;

        Assert.IsTrue(inputSystem.QueueSelectAllCommandRequest(screenRect, frame: 81));
        SystemHandle selectAllCommandSystem = _testWorld.CreateSystem<RtsSelectionSelectAllCommandSystem>();

        selectAllCommandSystem.Update(_testWorld.Unmanaged);

        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out EntityManager em,
            out Entity commandEntity,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
        DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests =
            em.GetBuffer<RtsSelectionPointerRequestElement>(commandEntity);
        Assert.AreEqual(1, pointerRequests.Length);
        Assert.AreEqual(RtsSelectionPointerRequestKind.SelectionRectCommitted, pointerRequests[0].Kind);
        Assert.AreEqual(new float2(screenRect.min.x, screenRect.min.y), pointerRequests[0].DragStart);
        Assert.AreEqual(new float2(screenRect.max.x, screenRect.max.y), pointerRequests[0].DragCurrent);
        Assert.AreEqual((byte)VisibleUnitSelectionSystem.Filter.All, pointerRequests[0].SelectionFilter);
        Assert.IsFalse(inputSystem.HasActiveWorldTargetCommandMode(out _));
        Assert.IsFalse(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsFalse(inputSystem.SkipNextWorldReleaseAfterSelection);
        Assert.IsFalse(inputSystem.BoardPassengerDragArmed);
        Assert.IsTrue(inputSystem.HasPendingSelectionRectangleRequests());
    }

    [Test]
    public void SelectAllCommandSystem_MapsVariantRequestsToSelectionFilters()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Rect screenRect = Rect.MinMaxRect(0f, 0f, 1280f, 720f);
        Assert.IsTrue(inputSystem.QueueSelectAllCommandRequest(RtsSelectionCommandIntentKind.SelectAll, screenRect, frame: 90));
        Assert.IsTrue(inputSystem.QueueSelectAllCommandRequest(RtsSelectionCommandIntentKind.SelectAllSoldiers, screenRect, frame: 91));
        Assert.IsTrue(inputSystem.QueueSelectAllCommandRequest(RtsSelectionCommandIntentKind.SelectAllVehicles, screenRect, frame: 92));
        SystemHandle selectAllCommandSystem = _testWorld.CreateSystem<RtsSelectionSelectAllCommandSystem>();

        selectAllCommandSystem.Update(_testWorld.Unmanaged);

        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out EntityManager em,
            out Entity commandEntity,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
        DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests =
            em.GetBuffer<RtsSelectionPointerRequestElement>(commandEntity);
        Assert.AreEqual(3, pointerRequests.Length);
        Assert.AreEqual((byte)VisibleUnitSelectionSystem.Filter.All, pointerRequests[0].SelectionFilter);
        Assert.AreEqual((byte)VisibleUnitSelectionSystem.Filter.Soldiers, pointerRequests[1].SelectionFilter);
        Assert.AreEqual((byte)VisibleUnitSelectionSystem.Filter.Vehicles, pointerRequests[2].SelectionFilter);
    }

    [Test]
    public void DeselectAllCommandSystem_RemovesSelectedTagsAndClearsCommandMode()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity first = em.CreateEntity(typeof(SelectedUnitTag));
        Entity second = em.CreateEntity(typeof(SelectedUnitTag));
        var inputSystem = new RtsSelectionInputSystem();
        inputSystem.ArmCommandMode(
            TacticalCommandMode.Attack,
            Time.frameCount,
            oneShot: true,
            requiresWorldTarget: true);
        inputSystem.IgnoreNextLeftMouseRelease = true;
        inputSystem.SkipNextWorldReleaseAfterSelection = true;
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.DeselectAll, frame: 101));
        SystemHandle deselectAllCommandSystem = _testWorld.CreateSystem<RtsSelectionDeselectAllCommandSystem>();

        deselectAllCommandSystem.Update(_testWorld.Unmanaged);

        Assert.IsFalse(em.HasComponent<SelectedUnitTag>(first));
        Assert.IsFalse(em.HasComponent<SelectedUnitTag>(second));
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
        Assert.IsFalse(inputSystem.HasActiveWorldTargetCommandMode(out _));
        Assert.IsFalse(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsFalse(inputSystem.SkipNextWorldReleaseAfterSelection);
    }

    [Test]
    public void SelectionModeCommandSystem_EnterSelectionModeMutatesEcsStateAndClearsMoveRequests()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: false);
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 pointer = new(12f, 34f);
        inputSystem.UpdateLastKnownPointerPosition(pointer);
        inputSystem.ArmCommandMode(
            TacticalCommandMode.Attack,
            frame: 99,
            oneShot: true,
            requiresWorldTarget: true);
        inputSystem.QueueMoveOrder(new Vector2(88f, 99f), executeFrame: 120);
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new Vector2(10f, 20f), frame: 121));
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterSelectionMode, frame: 122));

        bool processed = RtsSelectionModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 200,
            out bool enteredSelectionMode,
            out bool exitedSelectionMode,
            out RtsSelectionCommandIntentKind lastProcessedKind);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsTrue(enteredSelectionMode);
        Assert.IsFalse(exitedSelectionMode);
        Assert.AreEqual(RtsSelectionCommandIntentKind.EnterSelectionMode, lastProcessedKind);
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.IsFalse(inputSystem.HasActiveWorldTargetCommandMode(out _));
        Assert.IsFalse(inputSystem.HasQueuedMoveOrder);
        Assert.IsFalse(inputSystem.TryConsumeQueuedMoveOrder(200, out _));
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.SkipNextWorldReleaseAfterSelection);
        Assert.AreEqual(201, inputSystem.IgnoreWorldCommandsUntilFrame);
        Assert.AreEqual(pointer, inputSystem.DragStart);
        Assert.AreEqual(pointer, inputSystem.DragCurrent);
        Assert.AreEqual(pointer, inputSystem.LastPointerPosition);
        Assert.IsFalse(inputSystem.HasLiveSelectionRect);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void SelectionModeCommandSystem_ExitSelectionModeMutatesEcsState()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 pointer = new(56f, 78f);
        inputSystem.UpdateLastKnownPointerPosition(pointer);
        inputSystem.IsDraggingSelection = true;
        inputSystem.HasLiveSelectionRect = true;
        inputSystem.SkipNextWorldReleaseAfterSelection = true;
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new Vector2(10f, 20f), frame: 129));
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.ExitSelectionMode, frame: 130));

        bool processed = RtsSelectionModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 240,
            out bool enteredSelectionMode,
            out bool exitedSelectionMode,
            out RtsSelectionCommandIntentKind lastProcessedKind);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsFalse(enteredSelectionMode);
        Assert.IsTrue(exitedSelectionMode);
        Assert.AreEqual(RtsSelectionCommandIntentKind.ExitSelectionMode, lastProcessedKind);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsFalse(inputSystem.SkipNextWorldReleaseAfterSelection);
        Assert.AreEqual(241, inputSystem.IgnoreWorldCommandsUntilFrame);
        Assert.AreEqual(pointer, inputSystem.DragStart);
        Assert.AreEqual(pointer, inputSystem.DragCurrent);
        Assert.IsFalse(inputSystem.IsDraggingSelection);
        Assert.IsFalse(inputSystem.HasLiveSelectionRect);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Move, requests[0].Kind);
    }

    [Test]
    public void MoveTargetModeCommandSystem_ArmsMoveModeWhenSelectionCanMove()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        Entity selectedUnit = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove));
        em.SetComponentData(selectedUnit, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(selectedUnit, new UnitGrid { Cell = new int2(2, 3) });
        em.SetComponentData(selectedUnit, new UnitMove { Speed = 1f, WalkSpeed = 1f, ArriveDistance = 0.1f });
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 pointer = new(72f, 96f);
        inputSystem.UpdateLastKnownPointerPosition(pointer);
        inputSystem.QueueMoveOrder(new Vector2(10f, 20f), executeFrame: 130);
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new Vector2(11f, 22f), frame: 131));
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterMoveTargetMode, frame: 132));

        bool processed = RtsSelectionMoveTargetModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 260,
            out bool accepted,
            out TacticalCommandReasonCode rejectionReason);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsTrue(accepted);
        Assert.AreEqual(TacticalCommandReasonCode.None, rejectionReason);
        Assert.IsTrue(inputSystem.HasActiveWorldTargetCommandMode(out TacticalCommandMode mode));
        Assert.AreEqual(TacticalCommandMode.Move, mode);
        Assert.IsFalse(inputSystem.HasQueuedMoveOrder);
        Assert.IsFalse(inputSystem.TryConsumeQueuedMoveOrder(260, out _));
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.SkipNextWorldReleaseAfterSelection);
        Assert.AreEqual(261, inputSystem.IgnoreWorldCommandsUntilFrame);
        Assert.AreEqual(pointer, inputSystem.DragStart);
        Assert.AreEqual(pointer, inputSystem.DragCurrent);
        Assert.AreEqual(pointer, inputSystem.LastPointerPosition);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void MoveTargetModeCommandSystem_RejectsWithoutSelectedMoveUnit()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        var inputSystem = new RtsSelectionInputSystem();
        inputSystem.ArmCommandMode(
            TacticalCommandMode.Attack,
            frame: 150,
            oneShot: true,
            requiresWorldTarget: true);
        inputSystem.QueueMoveOrder(new Vector2(10f, 20f), executeFrame: 170);
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new Vector2(11f, 22f), frame: 171));
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterMoveTargetMode, frame: 172));

        bool processed = RtsSelectionMoveTargetModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 300,
            out bool accepted,
            out TacticalCommandReasonCode rejectionReason);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsFalse(accepted);
        Assert.AreEqual(TacticalCommandReasonCode.NoSelection, rejectionReason);
        Assert.IsFalse(inputSystem.HasActiveWorldTargetCommandMode(out _));
        Assert.IsFalse(inputSystem.HasQueuedMoveOrder);
        Assert.IsFalse(inputSystem.TryConsumeQueuedMoveOrder(300, out _));
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void HasPendingMoveCommandRequestsOrResults_DetectsMoveResults()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out _,
            out DynamicBuffer<RtsSelectionCommandResultElement> results));

        results.Add(new RtsSelectionCommandResultElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            HasCommandResult = 1
        });

        Assert.IsTrue(inputSystem.HasPendingMoveCommandRequestsOrResults());
    }

    [Test]
    public void HasPendingAttackCommandRequestsOrResults_DetectsAttackResults()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out _,
            out DynamicBuffer<RtsSelectionCommandResultElement> results));

        results.Add(new RtsSelectionCommandResultElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            HasCommandResult = 1
        });

        Assert.IsTrue(inputSystem.HasPendingAttackCommandRequestsOrResults());
    }

    [Test]
    public void HasPendingScanCommandRequestsOrResults_DetectsScanResults()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out _,
            out DynamicBuffer<RtsSelectionCommandResultElement> results));

        results.Add(new RtsSelectionCommandResultElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            HasCommandResult = 1
        });

        Assert.IsTrue(inputSystem.HasPendingScanCommandRequestsOrResults());
    }

    [Test]
    public void HasPendingTransportCommandRequests_DetectsTransportResults()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out _,
            out DynamicBuffer<RtsSelectionCommandResultElement> results));

        results.Add(new RtsSelectionCommandResultElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardTransport,
            HasCommandResult = 1
        });

        Assert.IsTrue(inputSystem.HasPendingTransportCommandRequests());
    }

    private static Entity CreateRuntimeGameplayState(EntityManager em, bool selectionModeActive)
    {
        Entity entity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(entity, new RuntimeGameplayStateComponent
        {
            PlayRequested = 1,
            SelectionModeActive = selectionModeActive ? (byte)1 : (byte)0
        });
        return entity;
    }

    [Test]
    public void CommandModeState_ArmsAndClearsWorldTargetMode()
    {
        var inputSystem = new RtsSelectionInputSystem();

        Assert.IsFalse(inputSystem.TryGetActiveCommandMode(out _));
        Assert.IsFalse(inputSystem.HasActiveWorldTargetCommandMode(out _));

        inputSystem.ArmCommandMode(
            TacticalCommandMode.Move,
            frame: 42,
            oneShot: true,
            requiresWorldTarget: true);

        Assert.IsTrue(inputSystem.TryGetActiveCommandMode(out TacticalCommandMode activeMode));
        Assert.AreEqual(TacticalCommandMode.Move, activeMode);
        Assert.IsTrue(inputSystem.HasActiveWorldTargetCommandMode(out TacticalCommandMode targetMode));
        Assert.AreEqual(TacticalCommandMode.Move, targetMode);

        inputSystem.ClearActiveCommandMode();

        Assert.IsFalse(inputSystem.TryGetActiveCommandMode(out _));
        Assert.IsFalse(inputSystem.HasActiveWorldTargetCommandMode(out _));
    }

    [Test]
    public void BoardCommandModeState_StoresDirectionAndLockedTransport()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Entity transport = new Entity { Index = 123, Version = 4 };

        inputSystem.ArmBoardCommandMode(
            BoardCommandModeDirection.TransportToPassenger,
            transport,
            frame: 77,
            oneShot: true);

        Assert.IsTrue(inputSystem.TryGetActiveCommandMode(out TacticalCommandMode activeMode));
        Assert.AreEqual(TacticalCommandMode.Board, activeMode);
        Assert.IsTrue(inputSystem.HasActiveWorldTargetCommandMode(out TacticalCommandMode targetMode));
        Assert.AreEqual(TacticalCommandMode.Board, targetMode);
        Assert.IsTrue(inputSystem.TryGetActiveBoardCommandMode(out BoardCommandModeDirection direction, out Entity lockedTransport));
        Assert.AreEqual(BoardCommandModeDirection.TransportToPassenger, direction);
        Assert.AreEqual(transport, lockedTransport);

        inputSystem.ClearActiveCommandMode();

        Assert.IsFalse(inputSystem.TryGetActiveBoardCommandMode(out _, out _));
    }

    [Test]
    public void MoveTargetDoubleClick_RequiresRecentNearbyMoveTargetClick()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 firstClick = new(100f, 200f);
        Vector2 nearbySecondClick = new(118f, 214f);
        Vector2 farSecondClick = new(240f, 320f);

        Assert.IsFalse(inputSystem.IsMoveTargetDoubleClick(firstClick, currentTime: 10f));

        inputSystem.RecordMoveTargetClick(firstClick, currentTime: 10f);

        Assert.IsTrue(inputSystem.IsMoveTargetDoubleClick(nearbySecondClick, currentTime: 10.2f));
        Assert.IsFalse(inputSystem.IsMoveTargetDoubleClick(farSecondClick, currentTime: 10.2f));
        Assert.IsFalse(inputSystem.IsMoveTargetDoubleClick(nearbySecondClick, currentTime: 11f));
    }

    [Test]
    public void SelectionUiCommandSystem_MoveButtonQueuesEnterMoveTargetModeAndSuppressesRelease()
    {
        var commandSystem = new SelectionUiCommandSystem();

        Assert.IsTrue(commandSystem.RequestMoveCommandMode());

        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.IgnoreUiClickUntilRelease);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.EnterMoveTargetMode, requests[0].Kind);
    }

    [Test]
    public void SelectionUiCommandSystem_MoveButtonDoesNotQueueWhileGameplayInputLocked()
    {
        var commandSystem = new SelectionUiCommandSystem(() => true);

        Assert.IsFalse(commandSystem.RequestMoveCommandMode());

        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.IgnoreUiClickUntilRelease);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void SelectionUiCommandSystem_AttackButtonQueuesEnterAttackTargetModeAndSuppressesRelease()
    {
        var commandSystem = new SelectionUiCommandSystem();

        Assert.IsTrue(commandSystem.RequestAttackCommandMode());

        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.IgnoreUiClickUntilRelease);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.EnterAttackTargetMode, requests[0].Kind);
    }

    [Test]
    public void AttackTargetModeCommandSystem_ArmsAttackModeWhenSelectionCanAttack()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        CreateSelectedAttackUnit(em);
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 pointer = new(21f, 43f);
        inputSystem.UpdateLastKnownPointerPosition(pointer);
        inputSystem.QueueMoveOrder(new Vector2(10f, 20f), executeFrame: 150);
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new Vector2(11f, 22f), frame: 151));
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterAttackTargetMode, frame: 152));

        bool processed = RtsSelectionAttackTargetModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 310,
            out bool accepted,
            out bool airDefenseAutoEngageOnly,
            out TacticalCommandReasonCode rejectionReason);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsTrue(accepted);
        Assert.IsFalse(airDefenseAutoEngageOnly);
        Assert.AreEqual(TacticalCommandReasonCode.None, rejectionReason);
        Assert.IsTrue(inputSystem.HasActiveWorldTargetCommandMode(out TacticalCommandMode activeMode));
        Assert.AreEqual(TacticalCommandMode.Attack, activeMode);
        Assert.IsFalse(inputSystem.HasQueuedMoveOrder);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.SkipNextWorldReleaseAfterSelection);
        Assert.AreEqual(311, inputSystem.IgnoreWorldCommandsUntilFrame);
        Assert.AreEqual(pointer, inputSystem.DragStart);
        Assert.AreEqual(pointer, inputSystem.DragCurrent);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void AttackTargetModeCommandSystem_AirDefenseLauncherReportsAutoEngageOnly()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        CreateSelectedAirDefenseLauncher(em);
        var inputSystem = new RtsSelectionInputSystem();
        inputSystem.ArmCommandMode(TacticalCommandMode.Move, frame: 99, oneShot: true, requiresWorldTarget: true);
        inputSystem.QueueMoveOrder(new Vector2(10f, 20f), executeFrame: 160);
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new Vector2(11f, 22f), frame: 161));
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterAttackTargetMode, frame: 11));

        bool processed = RtsSelectionAttackTargetModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 320,
            out bool accepted,
            out bool airDefenseAutoEngageOnly,
            out TacticalCommandReasonCode rejectionReason);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsFalse(accepted);
        Assert.IsTrue(airDefenseAutoEngageOnly);
        Assert.AreEqual(TacticalCommandReasonCode.None, rejectionReason);
        Assert.IsFalse(inputSystem.HasActiveWorldTargetCommandMode(out _));
        Assert.IsFalse(inputSystem.HasQueuedMoveOrder);
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void AttackTargetModeCommandSystem_RejectsSelectedNonAttackUnit()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        Entity nonAttackUnit = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove));
        em.SetComponentData(nonAttackUnit, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(nonAttackUnit, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        var inputSystem = new RtsSelectionInputSystem();
        inputSystem.ArmCommandMode(TacticalCommandMode.Move, frame: 99, oneShot: true, requiresWorldTarget: true);
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterAttackTargetMode, frame: 11));

        bool processed = RtsSelectionAttackTargetModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 330,
            out bool accepted,
            out bool airDefenseAutoEngageOnly,
            out TacticalCommandReasonCode rejectionReason);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsFalse(accepted);
        Assert.IsFalse(airDefenseAutoEngageOnly);
        Assert.AreEqual(TacticalCommandReasonCode.TargetNotAttackable, rejectionReason);
        Assert.IsFalse(inputSystem.HasActiveWorldTargetCommandMode(out _));
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
    }

    [Test]
    public void AttackTargetModeCommandSystem_MixedAirDefenseAndAttackUnitEntersTargetMode()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        CreateSelectedAirDefenseLauncher(em);
        CreateSelectedAttackUnit(em);
        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterAttackTargetMode, frame: 11));

        bool processed = RtsSelectionAttackTargetModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 340,
            out bool accepted,
            out bool airDefenseAutoEngageOnly,
            out TacticalCommandReasonCode rejectionReason);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsTrue(accepted);
        Assert.IsFalse(airDefenseAutoEngageOnly);
        Assert.AreEqual(TacticalCommandReasonCode.None, rejectionReason);
        Assert.IsTrue(inputSystem.HasActiveWorldTargetCommandMode(out TacticalCommandMode activeMode));
        Assert.AreEqual(TacticalCommandMode.Attack, activeMode);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
    }

    private static Entity CreateSelectedAttackUnit(EntityManager em)
    {
        Entity attackUnit = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform));
        em.SetComponentData(attackUnit, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(attackUnit, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        em.SetComponentData(attackUnit, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(attackUnit, new UnitAttack { Range = 100f, CooldownSeconds = 1f, Damage = 10, TraceVisibleSeconds = 0.1f });
        em.SetComponentData(attackUnit, LocalTransform.FromPosition(float3.zero));
        return attackUnit;
    }

    private static Entity CreateSelectedAirDefenseLauncher(EntityManager em)
    {
        Entity launcher = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitHealth),
            typeof(AirMissileLauncherComponent),
            typeof(AirMissileLauncherStateComponent));
        em.SetComponentData(launcher, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(launcher, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(launcher, new AirMissileLauncherComponent
        {
            MinRange = 8f,
            BaseDetectionRange = 220f,
            MaxDetectionRange = 420f,
            LockSeconds = 0.35f,
            LaunchDelaySeconds = 0.12f,
            ReloadSeconds = 1.8f,
            MissileSpeed = 95f,
            MissileTurnRateDegreesPerSecond = 220f,
            MissileLifetimeSeconds = 7f,
            ProximityFuseRadius = 4f,
            AirTargetDamage = 120,
            IncomingMissileDamage = 9999,
            TrackingQuality = 0.75f
        });
        em.SetComponentData(launcher, new AirMissileLauncherStateComponent { Phase = (byte)AirMissileLauncherPhase.Idle });
        return launcher;
    }

    private static Entity CreateSelectedBoardPassenger(EntityManager em)
    {
        Entity passenger = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitFootprint),
            typeof(UnitMovementBehavior));
        em.SetComponentData(passenger, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(passenger, new UnitGrid { Cell = new int2(3, 4) });
        em.SetComponentData(passenger, new UnitMove
        {
            Speed = 1f,
            WalkSpeed = 1f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.1f
        });
        em.SetComponentData(passenger, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(passenger, new UnitMovementBehavior { UsesVehicleMotion = 0 });
        return passenger;
    }

    private static Entity CreateSelectedBoardTransport(EntityManager em, int capacity = 4, int passengerCount = 0)
    {
        Entity transport = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(LocalTransform),
            typeof(UnitTransportCapacity));
        em.SetComponentData(transport, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(transport, new UnitGrid { Cell = new int2(10, 10) });
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(2, 2) });
        em.SetComponentData(transport, LocalTransform.FromPosition(float3.zero));
        em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = capacity });
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.AddBuffer<UnitTransportPassengerElement>(transport);
        for (int i = 0; i < passengerCount; i++)
        {
            passengers.Add(new UnitTransportPassengerElement { Passenger = em.CreateEntity() });
        }

        return transport;
    }

    [Test]
    public void ScanTargetModeCommandSystem_ArmsScanModeAndClearsMoveRequests()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 pointer = new(64f, 128f);
        inputSystem.UpdateLastKnownPointerPosition(pointer);
        inputSystem.QueueMoveOrder(new Vector2(10f, 20f), executeFrame: 170);
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new Vector2(11f, 22f), frame: 171));
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterScanTargetMode, frame: 172));

        bool processed = RtsSelectionScanTargetModeCommandSystem.ProcessPendingRequests(em, currentFrame: 350);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsTrue(inputSystem.HasActiveWorldTargetCommandMode(out TacticalCommandMode activeMode));
        Assert.AreEqual(TacticalCommandMode.Scan, activeMode);
        Assert.IsFalse(inputSystem.HasQueuedMoveOrder);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.SkipNextWorldReleaseAfterSelection);
        Assert.AreEqual(351, inputSystem.IgnoreWorldCommandsUntilFrame);
        Assert.AreEqual(pointer, inputSystem.DragStart);
        Assert.AreEqual(pointer, inputSystem.DragCurrent);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void SelectionUiCommandSystem_BoardButtonQueuesEnterBoardTargetModeAndSuppressesRelease()
    {
        var commandSystem = new SelectionUiCommandSystem();

        Assert.IsTrue(commandSystem.RequestBoardTargetMode());

        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.IgnoreUiClickUntilRelease);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.EnterBoardTargetMode, requests[0].Kind);
    }

    [Test]
    public void BoardTargetModeCommandSystem_SelectedTransportAndPassengerUsesTransportFirstMode()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        CreateSelectedBoardPassenger(em);
        Entity transport = CreateSelectedBoardTransport(em);
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 pointer = new(32f, 48f);
        inputSystem.UpdateLastKnownPointerPosition(pointer);
        inputSystem.QueueMoveOrder(new Vector2(10f, 20f), executeFrame: 10);
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new Vector2(11f, 22f), frame: 10));
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterBoardTargetMode, frame: 11));

        bool processed = RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 360,
            out bool accepted,
            out bool toggledOff,
            out BoardCommandModeDirection direction,
            out Entity lockedTransport,
            out TacticalCommandReasonCode rejectionReason);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsTrue(accepted);
        Assert.IsFalse(toggledOff);
        Assert.AreEqual(BoardCommandModeDirection.TransportToPassenger, direction);
        Assert.AreEqual(transport, lockedTransport);
        Assert.AreEqual(TacticalCommandReasonCode.None, rejectionReason);
        Assert.IsTrue(inputSystem.TryGetActiveBoardCommandMode(out BoardCommandModeDirection activeDirection, out Entity activeLockedTransport));
        Assert.AreEqual(BoardCommandModeDirection.TransportToPassenger, activeDirection);
        Assert.AreEqual(transport, activeLockedTransport);
        Assert.IsFalse(inputSystem.HasQueuedMoveOrder);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.SkipNextWorldReleaseAfterSelection);
        Assert.AreEqual(361, inputSystem.IgnoreWorldCommandsUntilFrame);
        Assert.AreEqual(pointer, inputSystem.DragStart);
        Assert.AreEqual(pointer, inputSystem.DragCurrent);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void BoardTargetModeCommandSystem_SelectedPassengerUsesPassengerToTransportMode()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        CreateSelectedBoardPassenger(em);
        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterBoardTargetMode, frame: 11));

        bool processed = RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 370,
            out bool accepted,
            out bool toggledOff,
            out BoardCommandModeDirection direction,
            out Entity transport,
            out TacticalCommandReasonCode rejectionReason);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsTrue(accepted);
        Assert.IsFalse(toggledOff);
        Assert.AreEqual(BoardCommandModeDirection.PassengerToTransport, direction);
        Assert.AreEqual(Entity.Null, transport);
        Assert.AreEqual(TacticalCommandReasonCode.None, rejectionReason);
        Assert.IsTrue(inputSystem.TryGetActiveBoardCommandMode(out BoardCommandModeDirection activeDirection, out Entity activeLockedTransport));
        Assert.AreEqual(BoardCommandModeDirection.PassengerToTransport, activeDirection);
        Assert.AreEqual(Entity.Null, activeLockedTransport);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
    }

    [Test]
    public void BoardTargetModeCommandSystem_ActiveBoardModeTogglesOff()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        var inputSystem = new RtsSelectionInputSystem();
        Entity transport = CreateSelectedBoardTransport(em);
        inputSystem.ArmBoardCommandMode(
            BoardCommandModeDirection.TransportToPassenger,
            transport,
            frame: 12,
            oneShot: true);
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterBoardTargetMode, frame: 13));

        bool processed = RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 380,
            out bool accepted,
            out bool toggledOff,
            out BoardCommandModeDirection direction,
            out Entity lockedTransport,
            out TacticalCommandReasonCode rejectionReason);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsFalse(accepted);
        Assert.IsTrue(toggledOff);
        Assert.AreEqual(BoardCommandModeDirection.None, direction);
        Assert.AreEqual(Entity.Null, lockedTransport);
        Assert.AreEqual(TacticalCommandReasonCode.None, rejectionReason);
        Assert.IsFalse(inputSystem.TryGetActiveBoardCommandMode(out _, out _));
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
    }

    [Test]
    public void BoardTargetModeCommandSystem_RejectsSelectedNonBoardableUnit()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        Entity selected = em.CreateEntity(typeof(SelectedUnitTag), typeof(Faction));
        em.SetComponentData(selected, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.EnterBoardTargetMode, frame: 11));

        bool processed = RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests(
            em,
            currentFrame: 390,
            out bool accepted,
            out bool toggledOff,
            out BoardCommandModeDirection direction,
            out Entity transport,
            out TacticalCommandReasonCode rejectionReason);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsFalse(accepted);
        Assert.IsFalse(toggledOff);
        Assert.AreEqual(BoardCommandModeDirection.None, direction);
        Assert.AreEqual(Entity.Null, transport);
        Assert.AreEqual(TacticalCommandReasonCode.CommandUnavailable, rejectionReason);
        Assert.IsFalse(inputSystem.TryGetActiveBoardCommandMode(out _, out _));
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
    }

    [Test]
    public void CancelActiveCommandModeSystem_ClearsCommandAndSelectionMode()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        Entity transport = CreateSelectedBoardTransport(em);
        var inputSystem = new RtsSelectionInputSystem();
        inputSystem.ArmBoardCommandMode(
            BoardCommandModeDirection.TransportToPassenger,
            transport,
            frame: 42,
            oneShot: true);
        inputSystem.BoardPassengerDragArmed = true;
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.CancelActiveCommandMode, frame: 43));

        bool processed = RtsSelectionCancelActiveCommandModeSystem.ProcessPendingRequests(em);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.IsFalse(inputSystem.TryGetActiveCommandMode(out _));
        Assert.IsFalse(inputSystem.TryGetActiveBoardCommandMode(out _, out _));
        Assert.IsFalse(inputSystem.BoardPassengerDragArmed);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void SelectionUiCommandSystem_BoardAllQueuesBoardAllSelectedTransportAndSuppressesRelease()
    {
        var commandSystem = new SelectionUiCommandSystem();

        Assert.IsTrue(commandSystem.RequestBoardAllSelectedTransport());

        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.IgnoreUiClickUntilRelease);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.BoardAllSelectedTransport, requests[0].Kind);
    }

    [Test]
    public void SelectionUiCommandSystem_CancelFeedbackQueuesCancelActiveCommandModeAndSuppressesRelease()
    {
        var commandSystem = new SelectionUiCommandSystem();

        Assert.IsTrue(commandSystem.RequestCancelActiveCommandMode());

        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.IgnoreUiClickUntilRelease);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.CancelActiveCommandMode, requests[0].Kind);
    }

    [Test]
    public void BoardSelectedTransportPassengerRequest_StoresTransportPassengerAndScreenRect()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Entity transport = new Entity { Index = 45, Version = 2 };
        Entity passenger = new Entity { Index = 91, Version = 7 };
        Rect screenRect = Rect.MinMaxRect(10f, 20f, 110f, 220f);

        Assert.IsTrue(inputSystem.QueueBoardSelectedTransportPassengerCommandRequest(transport, passenger, screenRect, frame: 12));

        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger, requests[0].Kind);
        Assert.AreEqual(transport, requests[0].TargetEntity);
        Assert.AreEqual(passenger, requests[0].SecondaryTargetEntity);
        Assert.AreEqual(1, requests[0].HasTargetEntity);
        Assert.AreEqual(1, requests[0].HasSecondaryTargetEntity);
        Assert.AreEqual(1, requests[0].HasScreenRect);
        Assert.AreEqual(new Unity.Mathematics.float2(screenRect.min.x, screenRect.min.y), requests[0].DragStart);
        Assert.AreEqual(new Unity.Mathematics.float2(screenRect.max.x, screenRect.max.y), requests[0].DragCurrent);
    }

    [Test]
    public void BoardPreview_UsesTransportOnlyPredicateForPassengerFirstMode()
    {
        string startup = File.ReadAllText("Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs");
        string previewTarget = ExtractBlockAfter(startup, "bool IsValidBoardTransportPreviewTarget");

        StringAssert.Contains("IsBoardTransportWithAvailableSeats(em, target)", previewTarget);
        Assert.IsFalse(
            previewTarget.Contains("IsBoardCommandAvailable(em, target)", StringComparison.Ordinal),
            "Passenger-first Board preview must not use broad Board availability, because that also includes soldiers.");
    }

    [Test]
    public void BoardAllSelectedTransport_PlansApproachCellsBeforeStructuralOrderMutation()
    {
        string startup = File.ReadAllText("Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs");
        string boarding = ExtractBlockAfter(startup, "bool TryIssueFocusedTransportBoarding");

        int planningIndex = boarding.IndexOf("plannedOrders.Add(new TransportBoardingOrder", StringComparison.Ordinal);
        int mutationIndex = boarding.IndexOf("unitMoveOrderSystem.ClearMovementOrderComponents", StringComparison.Ordinal);
        Assert.GreaterOrEqual(planningIndex, 0, "Board All transport boarding must collect planned orders before mutating ECS components.");
        Assert.GreaterOrEqual(mutationIndex, 0, "Board All transport boarding must still issue movement orders after planning.");
        Assert.Less(
            planningIndex,
            mutationIndex,
            "Board All must not mutate ECS components while GridWalkable.AsNativeArray() and other grid arrays are still being used for approach-cell search.");
    }

    [Test]
    public void BoardAllSelectedTransport_CapsPlannedOrdersAtAvailableSeats()
    {
        string startup = File.ReadAllText("Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs");
        string boarding = ExtractBlockAfter(startup, "bool TryIssueFocusedTransportBoarding");

        StringAssert.Contains("int occupiedSeats = em.GetBuffer<UnitTransportPassengerElement>(transport).Length + CountPendingBoardingOrders(em, transport);", boarding);
        StringAssert.Contains("int availableSeats = capacity - occupiedSeats;", boarding);
        StringAssert.Contains("using NativeList<TransportBoardingOrder> plannedOrders = new(math.min(candidates.Count, availableSeats), Allocator.Temp);", boarding);
        StringAssert.Contains("plannedOrders.Length < availableSeats", boarding);
    }

    [Test]
    public void BoardAllSelectedTransport_ClearsCommandFeedbackActionsOnSuccess()
    {
        string startup = File.ReadAllText("Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs");
        string boardFocusedTransport = ExtractBlockAfter(startup, "void BoardFocusedTransport");

        int clearModeIndex = boardFocusedTransport.IndexOf("selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext())", StringComparison.Ordinal);
        int successIndex = boardFocusedTransport.IndexOf("TacticalCommandResult.Success($\"Boarding", StringComparison.Ordinal);

        Assert.GreaterOrEqual(clearModeIndex, 0, "Successful Board All must clear Board command mode so BOARD ALL and CANCEL disappear.");
        Assert.GreaterOrEqual(successIndex, 0, "Successful Board All must still show a success message.");
        Assert.Less(clearModeIndex, successIndex, "Clear command mode before showing the success result so the message remains but action buttons are hidden.");
    }

    [Test]
    public void SelectAll_ClearsPriorCommandModeFeedbackBeforeSelecting()
    {
        string focusCommands = File.ReadAllText("Assets/Game/Scripts/Systems/RtsSelectionFocusCommandSystem.cs");
        string selectAll = ExtractBlockAfter(focusCommands, "public void SelectAllVisiblePlayerUnits");

        int clearModeIndex = selectAll.IndexOf("context.InputSystem.ClearActiveCommandMode()", StringComparison.Ordinal);
        int clearHudIndex = selectAll.IndexOf("context.ClearHudCommandMode?.Invoke()", StringComparison.Ordinal);
        int hideMarkersIndex = selectAll.IndexOf("context.SetHudWorldMarkersVisible?.Invoke(false)", StringComparison.Ordinal);
        int queueSelectionIndex = selectAll.IndexOf("context.QueueSelectionRectangleRequest?.Invoke", StringComparison.Ordinal);

        Assert.GreaterOrEqual(clearModeIndex, 0, "Select All must exit any prior command mode.");
        Assert.GreaterOrEqual(clearHudIndex, 0, "Select All must clear command feedback actions such as BOARD ALL and CANCEL.");
        Assert.GreaterOrEqual(hideMarkersIndex, 0, "Select All must hide command targeting markers from the previous mode.");
        Assert.GreaterOrEqual(queueSelectionIndex, 0, "Select All must still issue the selection rectangle request.");
        Assert.Less(clearModeIndex, queueSelectionIndex);
        Assert.Less(clearHudIndex, queueSelectionIndex);
        Assert.Less(hideMarkersIndex, queueSelectionIndex);
    }

    [Test]
    public void CancelActiveCommandMode_ClearsModeWithoutPersistentCancelMessage()
    {
        string startup = File.ReadAllText("Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs");
        string cancelProcessor = ExtractBlockAfter(startup, "void ProcessCancelActiveCommandModeRequests");

        StringAssert.Contains("RtsSelectionCancelActiveCommandModeSystem.ProcessPendingRequests", cancelProcessor);
        StringAssert.Contains("selectionHudFeedbackSystem.ClearCommandMode(CreateHudFeedbackContext())", cancelProcessor);
        Assert.IsFalse(
            cancelProcessor.Contains("Command cancelled", StringComparison.Ordinal),
            "Cancel should clear mode/buttons without leaving a feedback message on screen.");
    }

    [Test]
    public void SelectionUiCommandSystem_HoldButtonQueuesHoldAndSuppressesRelease()
    {
        var commandSystem = new SelectionUiCommandSystem();

        Assert.IsTrue(commandSystem.RequestHoldPosition());

        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.IgnoreUiClickUntilRelease);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.HoldPosition, requests[0].Kind);
    }

    [Test]
    public void SelectionUiCommandSystem_StopButtonQueuesStopAndSuppressesRelease()
    {
        var commandSystem = new SelectionUiCommandSystem();

        Assert.IsTrue(commandSystem.RequestStop());

        var inputSystem = new RtsSelectionInputSystem();
        Assert.IsTrue(inputSystem.IgnoreUiClickUntilRelease);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Stop, requests[0].Kind);
    }

    [Test]
    public void QueueMoveOrder_ConsumesOnlyAtOrAfterExecutionFrame()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 orderPosition = new(5f, 7f);

        inputSystem.QueueMoveOrder(orderPosition, executeFrame: 10);

        Assert.IsFalse(inputSystem.TryConsumeQueuedMoveOrder(9, out _));
        Assert.IsTrue(inputSystem.TryConsumeQueuedMoveOrder(10, out Vector2 consumedPosition));
        Assert.AreEqual(orderPosition, consumedPosition);
        Assert.IsFalse(inputSystem.TryConsumeQueuedMoveOrder(10, out _));
    }

    [Test]
    public void LastKnownPointerPosition_ReportsOnlyAfterUpdate()
    {
        var inputSystem = new RtsSelectionInputSystem();
        Vector2 pointer = new(9f, 11f);

        Assert.IsFalse(inputSystem.TryGetLastKnownPointerPosition(out _));

        inputSystem.UpdateLastKnownPointerPosition(pointer);

        Assert.IsTrue(inputSystem.TryGetLastKnownPointerPosition(out Vector2 lastKnown));
        Assert.AreEqual(pointer, lastKnown);
    }

    [Test]
    public void RuntimeInput_DefersUnitSelectionUntilPointerRelease()
    {
        string runtimeInput = File.ReadAllText("Assets/Game/Scripts/Systems/RtsSelectionRuntimeInputSystem.cs");
        string pointerPressed = ExtractMethod(runtimeInput, "HandlePointerPressed");
        string pointerReleased = ExtractMethod(runtimeInput, "HandlePointerReleased");
        string worldTargetCommand = ExtractBlockAfter(runtimeInput, "private static bool HandleWorldTargetCommand");
        string commandPan = ExtractBlockAfter(runtimeInput, "private static bool AllowsCameraPanDuringCommandMode");
        string passengerPress = ExtractBlockAfter(runtimeInput, "private static bool IsTransportFirstBoardPassengerPress");
        string boardRectCommand = ExtractBlockAfter(runtimeInput, "private static bool HandleBoardPassengerRectCommand");

        Assert.IsFalse(pointerPressed.Contains("TryFocusUnit", StringComparison.Ordinal));
        Assert.IsFalse(pointerPressed.Contains("TryIssueAttackOrderToClickedUnit", StringComparison.Ordinal));
        Assert.IsFalse(pointerPressed.Contains("TryIssueBoardTransportOrderToClickedUnit", StringComparison.Ordinal));
        StringAssert.Contains("input.BoardPassengerDragArmed = IsTransportFirstBoardPassengerPress(context, input, pointerPosition);", pointerPressed);
        StringAssert.Contains("bool allowCommandPan = AllowsCameraPanDuringCommandMode(input) && !input.PointerPressedOverUi;", pointerPressed);
        StringAssert.Contains("context.SetCameraDragging?.Invoke(allowCommandPan)", pointerPressed);
        StringAssert.Contains("direction != BoardCommandModeDirection.TransportToPassenger", passengerPress);
        StringAssert.Contains("context.IsBoardSelectedTransportPassengerTarget.Invoke(transport, pointerPosition)", passengerPress);
        StringAssert.Contains("activeMode == TacticalCommandMode.Attack", commandPan);
        StringAssert.Contains("return !input.BoardPassengerDragArmed;", commandPan);
        StringAssert.Contains("direction == BoardCommandModeDirection.PassengerToTransport", commandPan);
        StringAssert.Contains("float dragDistance = Vector2.Distance(input.DragStart, pointerPosition);", pointerReleased);
        StringAssert.Contains("else if (dragDistance < context.DragThresholdPixels)", pointerReleased);
        StringAssert.Contains("context.TryFocusUnit?.Invoke(pointerPosition)", pointerReleased);
        StringAssert.Contains("context.TryIssueAttackOrderToClickedUnit?.Invoke(pointerPosition)", pointerReleased);
        Assert.IsFalse(pointerReleased.Contains("else if (context.TryIssueAttackOrderToClickedUnit?.Invoke(pointerPosition)", StringComparison.Ordinal));
        StringAssert.Contains("HandleWorldTargetCommand(context, input, activeMode, pointerPosition)", pointerReleased);
        StringAssert.Contains("activeMode == TacticalCommandMode.Attack", worldTargetCommand);
        StringAssert.Contains("context.TryIssueAttackOrderToClickedUnit.Invoke(pointerPosition)", worldTargetCommand);
        StringAssert.Contains("activeMode == TacticalCommandMode.Board", worldTargetCommand);
        StringAssert.Contains("input.TryGetActiveBoardCommandMode", worldTargetCommand);
        StringAssert.Contains("context.TryIssueBoardTransportOrderToClickedUnit.Invoke(pointerPosition)", worldTargetCommand);
        StringAssert.Contains("context.TryIssueBoardSelectedTransportOrderToClickedUnit.Invoke(transport, pointerPosition)", worldTargetCommand);
        StringAssert.Contains("HandleBoardPassengerRectCommand", pointerReleased);
        StringAssert.Contains("if (!input.BoardPassengerDragArmed)", boardRectCommand);
        StringAssert.Contains("context.TryIssueBoardSelectedTransportOrderToPassengerRect.Invoke(transport, screenRect)", boardRectCommand);
        StringAssert.Contains("input.HasActiveWorldTargetCommandMode(out TacticalCommandMode activeMode)", pointerReleased);
        StringAssert.Contains("input.IsMoveTargetDoubleClick(pointerPosition, Time.unscaledTime)", pointerReleased);
        StringAssert.Contains("HandlePersistentMoveTargetDoubleClick", pointerReleased);
        StringAssert.Contains("action=NoCommand", pointerReleased);
        Assert.IsFalse(pointerReleased.Contains("QueueMoveOrder(", StringComparison.Ordinal));
        StringAssert.Contains("CompleteSelectionMode(context)", pointerReleased);
    }

    [Test]
    public void RuntimeInput_ActiveWorldCommandClickDoesNotFallThroughToFocusSelection()
    {
        var inputSystem = new RtsSelectionInputSystem();
        var runtimeState = new RuntimeGameplayStateSystem
        {
            PlayRequested = true,
            SelectionModeActive = false,
            BuildModeActive = false,
            SuppressNextWorldClick = false
        };
        Vector2 pointer = new(220f, 140f);
        inputSystem.BeginPointerPress(pointer, pointerPressedOverUi: false);
        inputSystem.ArmCommandMode(
            TacticalCommandMode.Attack,
            Time.frameCount,
            oneShot: true,
            requiresWorldTarget: true);

        int attackTargetCalls = 0;
        int focusCalls = 0;
        int clearCommandCalls = 0;
        bool cameraDragging = true;

        var context = new RtsSelectionRuntimeInputSystem.Context(
            runtimeGameplayStateSystem: runtimeState,
            inputSystem: inputSystem,
            mainMenuPlayUi: null,
            dragThresholdPixels: 8f,
            selectionModeHoldSeconds: 0.35f,
            getExplicitAttackTargetModeActive: null,
            setExplicitAttackTargetModeActive: null,
            getCameraDragging: () => cameraDragging,
            setCameraDragging: dragging => cameraDragging = dragging,
            isPointerOverAnyUi: _ => false,
            isPointerOverGameplayUi: _ => false,
            tryIssueAttackOrderToClickedUnit: _ =>
            {
                attackTargetCalls++;
                return false;
            },
            tryIssueScanOrder: null,
            orderMarkerSystem: null,
            tryGetDefaultEntityManager: null,
            tryGetScanClickedCell: null,
            setHudWorldMarkersVisible: null,
            tryIssueBoardTransportOrderToClickedUnit: null,
            tryIssueBoardSelectedTransportOrderToClickedUnit: null,
            tryIssueBoardSelectedTransportOrderToPassengerRect: null,
            isBoardSelectedTransportPassengerTarget: null,
            tryFocusUnit: _ =>
            {
                focusCalls++;
                return true;
            },
            panCamera: null,
            issueMoveOrder: null,
            processSelectionRectangleRequests: null,
            clearCommandMode: () => clearCommandCalls++,
            logClickDiagnostic: null,
            buildClickDebugSummary: _ => "summary=test",
            isGameplayInputLocked: null);

        InvokeRuntimePointerRelease(context, pointer);

        Assert.AreEqual(1, attackTargetCalls);
        Assert.AreEqual(0, focusCalls);
        Assert.AreEqual(0, clearCommandCalls);
        Assert.IsFalse(cameraDragging);
        Assert.IsFalse(inputSystem.PointerPressedOverUi);
        Assert.IsFalse(inputSystem.IsDraggingSelection);
        Assert.IsFalse(inputSystem.SelectionModeHoldArmed);
        Assert.IsFalse(inputSystem.HasLiveSelectionRect);
        Assert.IsFalse(inputSystem.BoardPassengerDragArmed);
        Assert.IsTrue(inputSystem.HasActiveWorldTargetCommandMode(out TacticalCommandMode activeMode));
        Assert.AreEqual(TacticalCommandMode.Attack, activeMode);
    }

    [Test]
    public void RuntimeInput_AttackCommandModeAllowsCameraPanWhileTargeting()
    {
        var inputSystem = new RtsSelectionInputSystem();
        var runtimeState = new RuntimeGameplayStateSystem
        {
            PlayRequested = true,
            SelectionModeActive = false,
            BuildModeActive = false,
            SuppressNextWorldClick = false
        };
        Vector2 pressPosition = new(16f, 24f);
        Vector2 heldPosition = new(22f, 31f);
        bool cameraDragging = false;
        int panCalls = 0;
        Vector2 panDelta = default;
        inputSystem.ArmCommandMode(
            TacticalCommandMode.Attack,
            Time.frameCount,
            oneShot: true,
            requiresWorldTarget: true);

        RtsSelectionRuntimeInputSystem.Context context = CreateRuntimeInputContext(
            runtimeState,
            inputSystem,
            getCameraDragging: () => cameraDragging,
            setCameraDragging: dragging => cameraDragging = dragging,
            panCamera: delta =>
            {
                panCalls++;
                panDelta = delta;
            });

        InvokeRuntimePointerPressed(context, pressPosition);
        Assert.IsTrue(cameraDragging);

        InvokeRuntimePointerHeld(context, heldPosition);

        Assert.AreEqual(1, panCalls);
        Assert.AreEqual(heldPosition - pressPosition, panDelta);
    }

    [Test]
    public void RuntimeInput_TransportFirstBoardModePansUnlessPassengerDragStarts()
    {
        var inputSystem = new RtsSelectionInputSystem();
        var runtimeState = new RuntimeGameplayStateSystem
        {
            PlayRequested = true,
            SelectionModeActive = false,
            BuildModeActive = false,
            SuppressNextWorldClick = false
        };
        Entity transport = _testWorld.EntityManager.CreateEntity();
        Vector2 pressPosition = new(48f, 96f);
        Vector2 heldPosition = new(58f, 105f);
        bool cameraDragging = false;
        int panCalls = 0;

        inputSystem.ArmBoardCommandMode(
            BoardCommandModeDirection.TransportToPassenger,
            transport,
            Time.frameCount,
            oneShot: true);
        RtsSelectionRuntimeInputSystem.Context nonPassengerContext = CreateRuntimeInputContext(
            runtimeState,
            inputSystem,
            getCameraDragging: () => cameraDragging,
            setCameraDragging: dragging => cameraDragging = dragging,
            panCamera: _ => panCalls++,
            isBoardSelectedTransportPassengerTarget: (_, _) => false);

        InvokeRuntimePointerPressed(nonPassengerContext, pressPosition);
        Assert.IsFalse(inputSystem.BoardPassengerDragArmed);
        Assert.IsTrue(cameraDragging);
        InvokeRuntimePointerHeld(nonPassengerContext, heldPosition);
        Assert.AreEqual(1, panCalls);

        cameraDragging = false;
        panCalls = 0;
        inputSystem.ArmBoardCommandMode(
            BoardCommandModeDirection.TransportToPassenger,
            transport,
            Time.frameCount,
            oneShot: true);
        RtsSelectionRuntimeInputSystem.Context passengerContext = CreateRuntimeInputContext(
            runtimeState,
            inputSystem,
            getCameraDragging: () => cameraDragging,
            setCameraDragging: dragging => cameraDragging = dragging,
            panCamera: _ => panCalls++,
            isBoardSelectedTransportPassengerTarget: (_, _) => true);

        InvokeRuntimePointerPressed(passengerContext, pressPosition);
        Assert.IsTrue(inputSystem.BoardPassengerDragArmed);
        Assert.IsFalse(cameraDragging);
        InvokeRuntimePointerHeld(passengerContext, heldPosition);
        Assert.AreEqual(0, panCalls);
        Assert.IsTrue(inputSystem.IsDraggingSelection);
        Assert.IsTrue(inputSystem.HasLiveSelectionRect);
    }

    [Test]
    public void PointerTargetCommandSystem_UsesBoundaryPassForResolvedCommandTargets()
    {
        string pointerTarget = File.ReadAllText("Assets/Game/Scripts/Systems/RtsSelectionPointerTargetCommandSystem.cs");
        StringAssert.Contains("private readonly struct PointerTargetBoundaryPass", pointerTarget);

        string move = ExtractMethodBodyByName(pointerTarget, "private bool TryQueueResolvedMoveCommand");
        StringAssert.Contains("PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);", move);
        StringAssert.Contains("targetBoundary.TryGetClickedUnitEntity", move);
        StringAssert.Contains("targetBoundary.TryGetClickedCell", move);
        Assert.IsFalse(move.Contains("TryGetClickedCell(context", StringComparison.Ordinal));

        string attack = ExtractMethodBodyByName(pointerTarget, "private bool TryQueueResolvedAttackCommand");
        StringAssert.Contains("PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);", attack);
        StringAssert.Contains("targetBoundary.TryGetClickedUnitEntity", attack);
        Assert.IsFalse(attack.Contains("TryGetClickedUnitEntity(context", StringComparison.Ordinal));

        string scan = ExtractMethodBodyByName(pointerTarget, "private bool TryQueueResolvedScanCommand");
        StringAssert.Contains("PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);", scan);
        StringAssert.Contains("targetBoundary.TryGetClickedCell", scan);
        Assert.IsFalse(scan.Contains("TryGetClickedCell(context", StringComparison.Ordinal));

        string board = ExtractMethodBodyByName(pointerTarget, "private bool TryQueueResolvedBoardTransportCommand");
        StringAssert.Contains("PointerTargetBoundaryPass targetBoundary = CreatePointerTargetBoundaryPass(context);", board);
        StringAssert.Contains("targetBoundary.TryGetClickedUnitEntity", board);
        StringAssert.Contains("targetBoundary.TryGetClickedCell", board);

        string clickedCellWrapper = ExtractMethodBodyByName(pointerTarget, "TryGetClickedCell(Context context");
        StringAssert.Contains("return CreatePointerTargetBoundaryPass(context).TryGetClickedCell", clickedCellWrapper);
    }

    [Test]
    public void BuildingSelectionInteraction_ClearFocusedUnitClearsSelectedUnitTags()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity selectedUnit = em.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitMove), typeof(SelectedUnitTag));
        em.SetComponentData(selectedUnit, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(selectedUnit, new UnitGrid { Cell = Unity.Mathematics.int2.zero });
        em.SetComponentData(selectedUnit, new UnitMove { Speed = 1f });

        var selectionState = new SelectionStateSystem();
        selectionState.SetFocusedUnit(selectedUnit);
        selectionState.CacheSelectedMoveEntity(em, selectedUnit);
        var buildingInteractionSystem = new SelectionBuildingInteractionSystem();
        buildingInteractionSystem.Init(selectionState, null, null);

        buildingInteractionSystem.ClearFocusedUnit();

        Assert.IsFalse(em.HasComponent<SelectedUnitTag>(selectedUnit));
        Assert.AreEqual(Entity.Null, selectionState.FocusedUnit);
        Assert.AreEqual(0, selectionState.CachedSelectedMoveEntities.Count);
    }

    [Test]
    public void SelectionRectangle_SelectsBuildingFallbackWhenNoUnitsAreInRect()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity queueEntity = em.CreateEntity();
        DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests = em.AddBuffer<RtsSelectionPointerRequestElement>(queueEntity);
        pointerRequests.Add(new RtsSelectionPointerRequestElement
        {
            Kind = RtsSelectionPointerRequestKind.SelectionRectCommitted,
            DragStart = new Unity.Mathematics.float2(10f, 20f),
            DragCurrent = new Unity.Mathematics.float2(90f, 120f),
            SelectionFilter = (byte)VisibleUnitSelectionSystem.Filter.All
        });

        bool clearedUnitSelection = false;
        bool selectedBuildingFallback = false;
        bool clearedBuildingSelection = false;
        var system = new SelectionRectangleRequestSystem();

        bool processed = system.ProcessPendingRequests(
            em,
            pointerRequests,
            null,
            new SelectionUiQuerySystem(),
            new VisibleUnitSelectionSystem(),
            new SelectionStateSystem(),
            new FocusedUnitLifecycleSystem(),
            new System.Collections.Generic.List<Entity>(),
            (_, reason) => clearedUnitSelection = reason == "SelectUnitsInRectangle",
            (_, _) => { },
            (_, _) => Assert.Fail("Unit HUD selection should not be applied when building fallback is selected."),
            _ => Assert.Fail("Squad HUD selection should not be applied when building fallback is selected."),
            _ => { },
            () => clearedBuildingSelection = true,
            _ =>
            {
                selectedBuildingFallback = true;
                return true;
            });

        Assert.IsTrue(processed);
        Assert.IsTrue(clearedUnitSelection);
        Assert.IsTrue(selectedBuildingFallback);
        Assert.IsFalse(clearedBuildingSelection);
        Assert.AreEqual(0, pointerRequests.Length);
    }

    [Test]
    public void AttackTargetLookup_ReturnsRuntimeBuildingFromScreenClick()
    {
        EntityManager em = _testWorld.EntityManager;
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 64,
            Height = 64,
            CellSize = 1f,
            Origin = float3.zero
        });

        Entity building = em.CreateEntity(
            typeof(RuntimeBuildingCombatTag),
            typeof(RuntimeBuildingCombatInfo),
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(building, new RuntimeBuildingCombatInfo
        {
            RuntimeBuildingId = 7,
            OwnerFactionId = FactionIdentitySystem.EnemyFactionId,
            OriginCell = new int2(10, 10),
            FootprintCells = new int2(3, 3)
        });
        em.SetComponentData(building, new Faction { Id = FactionIdentitySystem.EnemyFactionId });
        em.SetComponentData(building, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(building, LocalTransform.FromPosition(new float3(11.5f, 0f, 11.5f)));

        GameObject cameraObject = new("RtsSelectionInputSystemTests_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        try
        {
            camera.orthographic = true;
            camera.orthographicSize = 24f;
            camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            camera.transform.position = new Vector3(16f, 50f, 16f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Vector3 screen = camera.WorldToScreenPoint(new Vector3(11.5f, 0f, 11.5f));

            var pointerSystem = new RtsSelectionPointerTargetCommandSystem();
            var context = new RtsSelectionPointerTargetCommandSystem.Context(
                runtimeGameplayStateSystem: null,
                inputSystem: null,
                selectionStateSystem: new SelectionStateSystem(),
                focusedUnitLifecycleSystem: null,
                unitTargetOrderSystem: null,
                focusableUnitLookupSystem: new FocusableUnitLookupSystem(),
                transportBoardingCommandSystem: default,
                unitTransportCapacitySystem: default,
                unitTransportBoardingQuerySystem: default,
                unitTransportBoardingRuleSystem: default,
                unitTransportApproachCellSystem: default,
                unitTransportAirPickupSystem: default,
                buildingTargetMoveOrderSystem: default,
                buildingPlacementInteractionSystem: null,
                buildingPlacementInteractionContext: default,
                worldCamera: camera,
                tryGetEntityManager: null,
                tryGetPointerPosition: null,
                getExplicitAttackTargetModeActive: null,
                setExplicitAttackTargetModeActive: null,
                applyHudCommandMode: null,
                applyHudCommandResult: null,
                clearHudSelection: null,
                clearHudCommandMode: null,
                applyHudSelection: null,
                clearCurrentSelection: null,
                requestMoveOrderScreenMarker: null,
                setCameraDragging: null,
                processAttackCommandRequests: null,
                processScanCommandRequests: null,
                processTransportCommandRequests: null,
                processMoveCommandRequests: null,
                logSelectionDiagnostic: null,
                describeEntity: null);

            bool hit = pointerSystem.TryGetClickedAttackTargetEntity(
                context,
                new Vector2(screen.x, screen.y),
                em,
                out Entity selectedTarget);

            Assert.IsTrue(hit);
            Assert.AreEqual(building, selectedTarget);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void BuildingInput_DefersBuildingSelectionUntilPointerRelease()
    {
        string buildingInput = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickSystem.cs");
        string pointerPressed = ExtractBlockAfter(buildingInput, "if (pointer.WasPressedThisFrame)");
        string pointerReleased = ExtractBlockAfter(buildingInput, "if (pointer.WasReleasedThisFrame)");
        string clickGate = ExtractBlockAfter(buildingInput, "private static BuildingSelectionClickGate GetBuildingSelectionClickGate");

        Assert.IsFalse(pointerPressed.Contains("HandleBuildingSelectionClick", StringComparison.Ordinal));
        StringAssert.Contains("!gate.BlockedByCommandMode", pointerPressed);
        StringAssert.Contains("context.ShouldBlockBuildingSelectionClick?.Invoke() == true", clickGate);
        StringAssert.Contains("Vector2.Distance(_buildingSelectionPressPosition, pointerPosition) < context.ClickDragThresholdPixels", pointerReleased);
        StringAssert.Contains("!gate.BlockedByCommandMode", pointerReleased);
        StringAssert.Contains("context.SelectionClickSystem?.HandleBuildingSelectionClick", pointerReleased);
    }

    private static string ExtractMethod(string source, string methodName)
    {
        string marker = $"private static void {methodName}";
        int start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.GreaterOrEqual(start, 0, $"{methodName} was not found.");
        int bodyStart = source.IndexOf('{', start);
        Assert.GreaterOrEqual(bodyStart, 0, $"{methodName} body was not found.");

        int depth = 0;
        for (int i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{')
                depth++;
            else if (source[i] == '}')
                depth--;

            if (depth == 0)
                return source.Substring(bodyStart, i - bodyStart + 1);
        }

        Assert.Fail($"{methodName} body was not closed.");
        return string.Empty;
    }

    private static string ExtractMethodBodyByName(string source, string methodName)
    {
        int nameIndex = source.IndexOf(methodName, StringComparison.Ordinal);
        Assert.GreaterOrEqual(nameIndex, 0, $"{methodName} was not found.");
        int signatureStart = source.LastIndexOf('\n', nameIndex);
        signatureStart = signatureStart < 0 ? 0 : signatureStart + 1;
        int bodyStart = source.IndexOf('{', nameIndex);
        Assert.GreaterOrEqual(bodyStart, signatureStart, $"{methodName} body was not found.");

        int depth = 0;
        for (int i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{')
                depth++;
            else if (source[i] == '}')
                depth--;

            if (depth == 0)
                return source.Substring(bodyStart, i - bodyStart + 1);
        }

        Assert.Fail($"{methodName} body was not closed.");
        return string.Empty;
    }

    private static RtsSelectionRuntimeInputSystem.Context CreateRuntimeInputContext(
        RuntimeGameplayStateSystem runtimeState,
        RtsSelectionInputSystem inputSystem,
        Func<bool> getCameraDragging = null,
        Action<bool> setCameraDragging = null,
        Action<Vector2> panCamera = null,
        Func<Entity, Vector2, bool> isBoardSelectedTransportPassengerTarget = null)
    {
        return new RtsSelectionRuntimeInputSystem.Context(
            runtimeGameplayStateSystem: runtimeState,
            inputSystem: inputSystem,
            mainMenuPlayUi: null,
            dragThresholdPixels: 8f,
            selectionModeHoldSeconds: 0.35f,
            getExplicitAttackTargetModeActive: null,
            setExplicitAttackTargetModeActive: null,
            getCameraDragging: getCameraDragging,
            setCameraDragging: setCameraDragging,
            isPointerOverAnyUi: _ => false,
            isPointerOverGameplayUi: _ => false,
            tryIssueAttackOrderToClickedUnit: null,
            tryIssueScanOrder: null,
            orderMarkerSystem: null,
            tryGetDefaultEntityManager: null,
            tryGetScanClickedCell: null,
            setHudWorldMarkersVisible: null,
            tryIssueBoardTransportOrderToClickedUnit: null,
            tryIssueBoardSelectedTransportOrderToClickedUnit: null,
            tryIssueBoardSelectedTransportOrderToPassengerRect: null,
            isBoardSelectedTransportPassengerTarget: isBoardSelectedTransportPassengerTarget,
            tryFocusUnit: null,
            panCamera: panCamera,
            issueMoveOrder: null,
            processSelectionRectangleRequests: null,
            clearCommandMode: null,
            logClickDiagnostic: null,
            buildClickDebugSummary: _ => "summary=test",
            isGameplayInputLocked: null);
    }

    private static void InvokeRuntimePointerPressed(
        RtsSelectionRuntimeInputSystem.Context context,
        Vector2 pointerPosition)
    {
        InvokeRuntimePointerMethod("HandlePointerPressed", context, pointerPosition);
    }

    private static void InvokeRuntimePointerHeld(
        RtsSelectionRuntimeInputSystem.Context context,
        Vector2 pointerPosition)
    {
        InvokeRuntimePointerMethod("HandlePointerHeld", context, pointerPosition);
    }

    private static void InvokeRuntimePointerRelease(
        RtsSelectionRuntimeInputSystem.Context context,
        Vector2 pointerPosition)
    {
        InvokeRuntimePointerMethod("HandlePointerReleased", context, pointerPosition);
    }

    private static void InvokeRuntimePointerMethod(
        string methodName,
        RtsSelectionRuntimeInputSystem.Context context,
        Vector2 pointerPosition)
    {
        System.Reflection.MethodInfo method = typeof(RtsSelectionRuntimeInputSystem).GetMethod(
            methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        try
        {
            method.Invoke(null, new object[] { context, pointerPosition });
        }
        catch (System.Reflection.TargetInvocationException exception) when (exception.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static string ExtractBlockAfter(string source, string marker)
    {
        int start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.GreaterOrEqual(start, 0, $"{marker} was not found.");
        int bodyStart = source.IndexOf('{', start);
        Assert.GreaterOrEqual(bodyStart, 0, $"{marker} block body was not found.");

        int depth = 0;
        for (int i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{')
                depth++;
            else if (source[i] == '}')
                depth--;

            if (depth == 0)
                return source.Substring(bodyStart, i - bodyStart + 1);
        }

        Assert.Fail($"{marker} block body was not closed.");
        return string.Empty;
    }

}
#endif
