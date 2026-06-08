#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class RtsSelectionInputSystemTests
{
    private World _previousWorld;
    private World _testWorld;

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
            HasLiveSelectionRect = true
        };

        inputSystem.ClearPointerReleaseState();

        Assert.IsFalse(inputSystem.PointerPressedOverUi);
        Assert.IsFalse(inputSystem.IsDraggingSelection);
        Assert.IsFalse(inputSystem.SelectionModeHoldArmed);
        Assert.IsFalse(inputSystem.HasLiveSelectionRect);
    }

    [Test]
    public void CaptureUiClickSequence_SuppressesWorldReleaseUntilPointerEnds()
    {
        var inputSystem = new RtsSelectionInputSystem { IsDraggingSelection = true };

        inputSystem.CaptureUiClickSequence();

        Assert.IsTrue(inputSystem.IgnoreUiClickUntilRelease);
        Assert.IsTrue(inputSystem.IgnoreNextLeftMouseRelease);
        Assert.IsTrue(inputSystem.PointerPressedOverUi);
        Assert.IsFalse(inputSystem.IsDraggingSelection);
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

        Assert.IsFalse(pointerPressed.Contains("TryFocusUnit", StringComparison.Ordinal));
        Assert.IsFalse(pointerPressed.Contains("TryIssueAttackOrderToClickedUnit", StringComparison.Ordinal));
        Assert.IsFalse(pointerPressed.Contains("TryIssueBoardTransportOrderToClickedUnit", StringComparison.Ordinal));
        StringAssert.Contains("float dragDistance = Vector2.Distance(input.DragStart, pointerPosition);", pointerReleased);
        StringAssert.Contains("else if (dragDistance < context.DragThresholdPixels)", pointerReleased);
        StringAssert.Contains("context.TryFocusUnit?.Invoke(pointerPosition)", pointerReleased);
        StringAssert.Contains("context.TryIssueAttackOrderToClickedUnit?.Invoke(pointerPosition)", pointerReleased);
        Assert.IsFalse(pointerReleased.Contains("else if (context.TryIssueAttackOrderToClickedUnit?.Invoke(pointerPosition)", StringComparison.Ordinal));
        StringAssert.Contains("HandleWorldTargetCommand(context, input, activeMode, pointerPosition)", pointerReleased);
        StringAssert.Contains("activeMode == TacticalCommandMode.Attack", worldTargetCommand);
        StringAssert.Contains("context.TryIssueAttackOrderToClickedUnit.Invoke(pointerPosition)", worldTargetCommand);
        StringAssert.Contains("context.TryIssueBoardTransportOrderToClickedUnit?.Invoke(pointerPosition)", pointerReleased);
        StringAssert.Contains("input.HasActiveWorldTargetCommandMode(out TacticalCommandMode activeMode)", pointerReleased);
        StringAssert.Contains("input.IsMoveTargetDoubleClick(pointerPosition, Time.unscaledTime)", pointerReleased);
        StringAssert.Contains("HandlePersistentMoveTargetDoubleClick", pointerReleased);
        StringAssert.Contains("action=NoCommand", pointerReleased);
        Assert.IsFalse(pointerReleased.Contains("QueueMoveOrder(", StringComparison.Ordinal));
        StringAssert.Contains("CompleteSelectionMode(context)", pointerReleased);
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
    public void BuildingInput_DefersBuildingSelectionUntilPointerRelease()
    {
        string buildingInput = File.ReadAllText("Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickSystem.cs");
        string pointerPressed = ExtractBlockAfter(buildingInput, "if (pointer.WasPressedThisFrame)");
        string pointerReleased = ExtractBlockAfter(buildingInput, "if (pointer.WasReleasedThisFrame)");

        Assert.IsFalse(pointerPressed.Contains("HandleBuildingSelectionClick", StringComparison.Ordinal));
        StringAssert.Contains("Vector2.Distance(_buildingSelectionPressPosition, pointerPosition) < context.ClickDragThresholdPixels", pointerReleased);
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
