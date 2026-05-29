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
            SelectionModeHoldArmed = true
        };
        Vector2 pointer = new(12f, 34f);

        inputSystem.BeginPointerPress(pointer, pointerPressedOverUi: true);

        Assert.AreEqual(pointer, inputSystem.DragStart);
        Assert.AreEqual(pointer, inputSystem.DragCurrent);
        Assert.AreEqual(pointer, inputSystem.LastPointerPosition);
        Assert.IsTrue(inputSystem.PointerPressedOverUi);
        Assert.IsFalse(inputSystem.IsDraggingSelection);
        Assert.IsFalse(inputSystem.SelectionModeHoldArmed);
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

        Assert.IsFalse(pointerPressed.Contains("TryFocusUnit", StringComparison.Ordinal));
        Assert.IsFalse(pointerPressed.Contains("TryIssueAttackOrderToClickedUnit", StringComparison.Ordinal));
        Assert.IsFalse(pointerPressed.Contains("TryIssueBoardTransportOrderToClickedUnit", StringComparison.Ordinal));
        StringAssert.Contains("Vector2.Distance(input.DragStart, pointerPosition) < context.DragThresholdPixels", pointerReleased);
        StringAssert.Contains("context.TryFocusUnit?.Invoke(pointerPosition)", pointerReleased);
        StringAssert.Contains("context.TryIssueAttackOrderToClickedUnit?.Invoke(pointerPosition)", pointerReleased);
        StringAssert.Contains("context.TryIssueBoardTransportOrderToClickedUnit?.Invoke(pointerPosition)", pointerReleased);
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
