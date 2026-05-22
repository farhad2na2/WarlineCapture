using UnityEngine;

public sealed class RtsSelectionInputSystem
{
    private uint _queuedMoveOrderToken;

    public Vector2 DragStart { get; set; }
    public Vector2 DragCurrent { get; set; }
    public Vector2 LastPointerPosition { get; set; }
    public bool PointerPressedOverUi { get; set; }
    public bool IsDraggingSelection { get; set; }
    public bool IgnoreNextLeftMouseRelease { get; set; }
    public bool SkipNextWorldReleaseAfterSelection { get; set; }
    public int IgnoreWorldCommandsUntilFrame { get; set; }
    public bool IgnoreUiClickUntilRelease { get; set; }
    public bool SelectionModeHoldArmed { get; set; }
    public float SelectionModeHoldStartTime { get; set; }
    public bool HasQueuedMoveOrder { get; private set; }
    public Vector2 QueuedMoveOrderScreenPosition { get; private set; }
    public int QueuedMoveOrderFrame { get; private set; } = -1;
    public Rect LastLiveSelectionRect { get; set; }
    public bool HasLiveSelectionRect { get; set; }
    public Vector2 LastKnownPointerPosition { get; private set; }
    public bool HasLastKnownPointerPosition { get; private set; }

    public void BeginPointerPress(Vector2 pointerPosition, bool pointerPressedOverUi)
    {
        PointerPressedOverUi = pointerPressedOverUi;
        DragStart = pointerPosition;
        DragCurrent = pointerPosition;
        LastPointerPosition = pointerPosition;
        IsDraggingSelection = false;
        SelectionModeHoldArmed = false;
    }

    public void ClearPointerReleaseState()
    {
        PointerPressedOverUi = false;
        IsDraggingSelection = false;
        SelectionModeHoldArmed = false;
        HasLiveSelectionRect = false;
    }

    public void ClearPendingReleaseSuppression()
    {
        IgnoreNextLeftMouseRelease = false;
        SkipNextWorldReleaseAfterSelection = false;
    }

    public void ArmSelectionModeHold(float time)
    {
        SelectionModeHoldArmed = true;
        SelectionModeHoldStartTime = time;
    }

    public void ClearSelectionModeHold()
    {
        SelectionModeHoldArmed = false;
    }

    public void CaptureUiClickSequence()
    {
        IgnoreUiClickUntilRelease = true;
        IgnoreNextLeftMouseRelease = true;
        PointerPressedOverUi = true;
        IsDraggingSelection = false;
    }

    public void UpdateLastKnownPointerPosition(Vector2 pointerPosition)
    {
        LastKnownPointerPosition = pointerPosition;
        HasLastKnownPointerPosition = true;
    }

    public bool TryGetLastKnownPointerPosition(out Vector2 pointerPosition)
    {
        pointerPosition = LastKnownPointerPosition;
        return HasLastKnownPointerPosition;
    }

    public void QueueMoveOrder(Vector2 screenPosition, int executeFrame)
    {
        _queuedMoveOrderToken++;
        HasQueuedMoveOrder = true;
        QueuedMoveOrderScreenPosition = screenPosition;
        QueuedMoveOrderFrame = executeFrame;
    }

    public bool TryConsumeQueuedMoveOrder(int currentFrame, out Vector2 screenPosition)
    {
        screenPosition = default;
        if (!HasQueuedMoveOrder || currentFrame < QueuedMoveOrderFrame)
            return false;

        HasQueuedMoveOrder = false;
        uint token = _queuedMoveOrderToken;
        screenPosition = QueuedMoveOrderScreenPosition;
        return token == _queuedMoveOrderToken;
    }
}
