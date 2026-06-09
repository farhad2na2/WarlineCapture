using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class RtsSelectionInputSystem
{
    public const float MoveTargetDoubleClickSeconds = 0.35f;
    public const float MoveTargetDoubleClickPixels = 48f;

    private readonly RtsSelectionInputStateSystem _inputStateSystem = new();

    public Vector2 DragStart
    {
        get => ToVector2(ReadState().DragStart);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.DragStart = ToFloat2(value);
            WriteState(state);
        }
    }

    public Vector2 DragCurrent
    {
        get => ToVector2(ReadState().DragCurrent);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.DragCurrent = ToFloat2(value);
            WriteState(state);
        }
    }

    public Vector2 LastPointerPosition
    {
        get => ToVector2(ReadState().LastPointerPosition);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.LastPointerPosition = ToFloat2(value);
            WriteState(state);
        }
    }

    public bool PointerPressedOverUi
    {
        get => ToBool(ReadState().PointerPressedOverUi);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.PointerPressedOverUi = ToByte(value);
            WriteState(state);
        }
    }

    public bool IsDraggingSelection
    {
        get => ToBool(ReadState().IsDraggingSelection);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.IsDraggingSelection = ToByte(value);
            WriteState(state);
        }
    }

    public bool IgnoreNextLeftMouseRelease
    {
        get => ToBool(ReadState().IgnoreNextLeftMouseRelease);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.IgnoreNextLeftMouseRelease = ToByte(value);
            WriteState(state);
        }
    }

    public bool SkipNextWorldReleaseAfterSelection
    {
        get => ToBool(ReadState().SkipNextWorldReleaseAfterSelection);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.SkipNextWorldReleaseAfterSelection = ToByte(value);
            WriteState(state);
        }
    }

    public int IgnoreWorldCommandsUntilFrame
    {
        get => ReadState().IgnoreWorldCommandsUntilFrame;
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.IgnoreWorldCommandsUntilFrame = value;
            WriteState(state);
        }
    }

    public bool IgnoreUiClickUntilRelease
    {
        get => ToBool(ReadState().IgnoreUiClickUntilRelease);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.IgnoreUiClickUntilRelease = ToByte(value);
            WriteState(state);
        }
    }

    public bool SelectionModeHoldArmed
    {
        get => ToBool(ReadState().SelectionModeHoldArmed);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.SelectionModeHoldArmed = ToByte(value);
            WriteState(state);
        }
    }

    public float SelectionModeHoldStartTime
    {
        get => ReadState().SelectionModeHoldStartTime;
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.SelectionModeHoldStartTime = value;
            WriteState(state);
        }
    }

    public bool HasQueuedMoveOrder => ToBool(ReadState().HasQueuedMoveOrder);
    public Vector2 QueuedMoveOrderScreenPosition => ToVector2(ReadState().QueuedMoveOrderScreenPosition);
    public int QueuedMoveOrderFrame => ReadState().QueuedMoveOrderFrame;

    public Rect LastLiveSelectionRect
    {
        get => ToRect(ReadState().LastLiveSelectionRect);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.LastLiveSelectionRect = ToFloat4(value);
            WriteState(state);
        }
    }

    public bool HasLiveSelectionRect
    {
        get => ToBool(ReadState().HasLiveSelectionRect);
        set
        {
            RtsSelectionInputStateComponent state = ReadState();
            state.HasLiveSelectionRect = ToByte(value);
            WriteState(state);
        }
    }

    public Vector2 LastKnownPointerPosition => ToVector2(ReadState().LastKnownPointerPosition);
    public bool HasLastKnownPointerPosition => ToBool(ReadState().HasLastKnownPointerPosition);

    public bool TryGetActiveCommandMode(out TacticalCommandMode mode)
    {
        RtsSelectionInputStateComponent state = ReadState();
        mode = (TacticalCommandMode)state.ActiveCommandMode;
        return mode != TacticalCommandMode.None;
    }

    public bool HasActiveWorldTargetCommandMode(out TacticalCommandMode mode)
    {
        RtsSelectionInputStateComponent state = ReadState();
        mode = (TacticalCommandMode)state.ActiveCommandMode;
        return mode != TacticalCommandMode.None &&
               state.ActiveCommandModeRequiresWorldTarget != 0;
    }

    public void ArmCommandMode(TacticalCommandMode mode, int frame, bool oneShot, bool requiresWorldTarget)
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.ActiveCommandMode = (int)mode;
        state.ActiveCommandModeFrame = frame;
        state.ActiveCommandModeOneShot = ToByte(oneShot);
        state.ActiveCommandModeRequiresWorldTarget = ToByte(requiresWorldTarget);
        WriteState(state);
    }

    public void ClearActiveCommandMode()
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.ActiveCommandMode = (int)TacticalCommandMode.None;
        state.ActiveCommandModeFrame = 0;
        state.ActiveCommandModeOneShot = 0;
        state.ActiveCommandModeRequiresWorldTarget = 0;
        WriteState(state);
    }

    public bool ShouldClearActiveCommandModeAfterCommand(TacticalCommandMode mode)
    {
        RtsSelectionInputStateComponent state = ReadState();
        return (TacticalCommandMode)state.ActiveCommandMode == mode &&
               state.ActiveCommandModeOneShot != 0;
    }

    public bool IsMoveTargetDoubleClick(Vector2 screenPosition, float currentTime)
    {
        RtsSelectionInputStateComponent state = ReadState();
        if (state.HasLastMoveTargetClick == 0)
            return false;

        float elapsed = currentTime - state.LastMoveTargetClickTime;
        if (elapsed < 0f || elapsed > MoveTargetDoubleClickSeconds)
            return false;

        Vector2 previous = ToVector2(state.LastMoveTargetClickScreenPosition);
        return Vector2.Distance(previous, screenPosition) <= MoveTargetDoubleClickPixels;
    }

    public void RecordMoveTargetClick(Vector2 screenPosition, float currentTime)
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.HasLastMoveTargetClick = 1;
        state.LastMoveTargetClickScreenPosition = ToFloat2(screenPosition);
        state.LastMoveTargetClickTime = currentTime;
        WriteState(state);
    }

    public void BeginPointerPress(Vector2 pointerPosition, bool pointerPressedOverUi)
    {
        RtsSelectionInputStateComponent state = ReadState();
        float2 pointer = ToFloat2(pointerPosition);
        state.PointerPressedOverUi = ToByte(pointerPressedOverUi);
        state.DragStart = pointer;
        state.DragCurrent = pointer;
        state.LastPointerPosition = pointer;
        state.IsDraggingSelection = 0;
        state.SelectionModeHoldArmed = 0;
        state.LastLiveSelectionRect = ToFloat4(Rect.MinMaxRect(pointer.x, pointer.y, pointer.x, pointer.y));
        state.HasLiveSelectionRect = 0;
        WriteState(state);
    }

    public void ResetSelectionDragState(Vector2 pointerPosition)
    {
        RtsSelectionInputStateComponent state = ReadState();
        float2 pointer = ToFloat2(pointerPosition);
        state.DragStart = pointer;
        state.DragCurrent = pointer;
        state.LastPointerPosition = pointer;
        state.PointerPressedOverUi = 0;
        state.IsDraggingSelection = 0;
        state.SelectionModeHoldArmed = 0;
        state.LastLiveSelectionRect = ToFloat4(Rect.MinMaxRect(pointer.x, pointer.y, pointer.x, pointer.y));
        state.HasLiveSelectionRect = 0;
        WriteState(state);
    }

    public void ClearPointerReleaseState()
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.PointerPressedOverUi = 0;
        state.IsDraggingSelection = 0;
        state.SelectionModeHoldArmed = 0;
        state.HasLiveSelectionRect = 0;
        WriteState(state);
    }

    public void ClearPendingReleaseSuppression()
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.IgnoreNextLeftMouseRelease = 0;
        state.SkipNextWorldReleaseAfterSelection = 0;
        WriteState(state);
    }

    public void ArmSelectionModeHold(float time)
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.SelectionModeHoldArmed = 1;
        state.SelectionModeHoldStartTime = time;
        WriteState(state);
    }

    public void ClearSelectionModeHold()
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.SelectionModeHoldArmed = 0;
        WriteState(state);
    }

    public void CaptureUiClickSequence()
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.QueuedMoveOrderToken++;
        state.HasQueuedMoveOrder = 0;
        state.QueuedMoveOrderScreenPosition = default;
        state.QueuedMoveOrderFrame = -1;
        state.IgnoreWorldCommandsUntilFrame = math.max(state.IgnoreWorldCommandsUntilFrame, Time.frameCount + 1);
        state.IgnoreUiClickUntilRelease = 1;
        state.IgnoreNextLeftMouseRelease = 1;
        state.PointerPressedOverUi = 1;
        state.IsDraggingSelection = 0;
        state.HasLiveSelectionRect = 0;
        WriteState(state);
        ClearPendingMoveCommandRequests();
    }

    public void UpdateLastKnownPointerPosition(Vector2 pointerPosition)
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.LastKnownPointerPosition = ToFloat2(pointerPosition);
        state.HasLastKnownPointerPosition = 1;
        WriteState(state);
    }

    public bool TryGetLastKnownPointerPosition(out Vector2 pointerPosition)
    {
        RtsSelectionInputStateComponent state = ReadState();
        pointerPosition = ToVector2(state.LastKnownPointerPosition);
        return state.HasLastKnownPointerPosition != 0;
    }

    public void QueueMoveOrder(Vector2 screenPosition, int executeFrame)
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.QueuedMoveOrderToken++;
        state.HasQueuedMoveOrder = 1;
        state.QueuedMoveOrderScreenPosition = ToFloat2(screenPosition);
        state.QueuedMoveOrderFrame = executeFrame;
        WriteState(state);
    }

    public void ClearQueuedMoveOrder()
    {
        RtsSelectionInputStateComponent state = ReadState();
        state.QueuedMoveOrderToken++;
        state.HasQueuedMoveOrder = 0;
        state.QueuedMoveOrderScreenPosition = default;
        state.QueuedMoveOrderFrame = -1;
        WriteState(state);
    }

    public int ClearPendingMoveCommandRequests()
    {
        if (!_inputStateSystem.TryGetCommandBuffers(
                out _,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out _))
        {
            return 0;
        }

        int removed = 0;
        for (int i = commandRequests.Length - 1; i >= 0; i--)
        {
            if (commandRequests[i].Kind != RtsSelectionCommandIntentKind.Move)
                continue;

            commandRequests.RemoveAt(i);
            removed++;
        }

        return removed;
    }

    public bool QueueSelectionRectangleRequest(
        RtsSelectionPointerRequestKind kind,
        Rect screenRect,
        int frame,
        VisibleUnitSelectionSystem.Filter filter)
    {
        return _inputStateSystem.TryEnqueuePointerRequest(new RtsSelectionPointerRequestElement
        {
            Kind = kind,
            Frame = frame,
            ScreenPosition = ToFloat2(screenRect.center),
            DragStart = ToFloat2(screenRect.min),
            DragCurrent = ToFloat2(screenRect.max),
            SelectionFilter = (byte)filter
        });
    }

    public bool QueueMoveCommandRequest(Vector2 screenPosition, int frame)
    {
        return _inputStateSystem.TryEnqueueCommandRequest(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            Frame = frame,
            ScreenPosition = ToFloat2(screenPosition),
            HasScreenPosition = 1
        });
    }

    public bool QueueFocusUnitCommandRequest(Vector2 screenPosition, int frame)
    {
        return _inputStateSystem.TryEnqueueCommandRequest(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.FocusUnit,
            Frame = frame,
            ScreenPosition = ToFloat2(screenPosition),
            HasScreenPosition = 1
        });
    }

    public bool QueueAttackCommandRequest(Vector2 screenPosition, bool explicitAttackTargetModeActive, int frame)
    {
        return _inputStateSystem.TryEnqueueCommandRequest(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            Frame = frame,
            ScreenPosition = ToFloat2(screenPosition),
            ExplicitAttackTargetMode = explicitAttackTargetModeActive ? (byte)1 : (byte)0,
            HasScreenPosition = 1
        });
    }

    public bool QueueScanCommandRequest(Vector2 screenPosition, int frame)
    {
        return _inputStateSystem.TryEnqueueCommandRequest(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            Frame = frame,
            ScreenPosition = ToFloat2(screenPosition),
            HasScreenPosition = 1
        });
    }

    public bool QueueBoardTransportCommandRequest(Vector2 screenPosition, int frame)
    {
        return _inputStateSystem.TryEnqueueCommandRequest(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardTransport,
            Frame = frame,
            ScreenPosition = ToFloat2(screenPosition),
            HasScreenPosition = 1
        });
    }

    public bool QueueDisembarkTransportCommandRequest(Entity transport, int frame)
    {
        return _inputStateSystem.TryEnqueueCommandRequest(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            Frame = frame,
            TargetEntity = transport,
            HasTargetEntity = transport != Entity.Null ? (byte)1 : (byte)0
        });
    }

    public bool QueueCommandIntentRequest(RtsSelectionCommandIntentKind kind, int frame)
    {
        return _inputStateSystem.TryEnqueueCommandRequest(new RtsSelectionCommandIntentRequestElement
        {
            Kind = kind,
            Frame = frame
        });
    }

    public bool TryGetPointerRequests(out EntityManager em, out DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests)
    {
        return _inputStateSystem.TryGetPointerRequests(out em, out pointerRequests);
    }

    public bool TryGetCommandBuffers(
        out EntityManager em,
        out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        out DynamicBuffer<RtsSelectionCommandResultElement> commandResults)
    {
        return _inputStateSystem.TryGetCommandBuffers(out em, out commandRequests, out commandResults);
    }

    public bool TryGetCommandBuffers(
        out EntityManager em,
        out Entity entity,
        out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        out DynamicBuffer<RtsSelectionCommandResultElement> commandResults)
    {
        return _inputStateSystem.TryGetCommandBuffers(out em, out entity, out commandRequests, out commandResults);
    }

    public bool HasPendingExternalSelectionCommandRequests()
    {
        if (!TryGetCommandBuffers(out _, out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests, out _))
            return false;

        for (int i = 0; i < commandRequests.Length; i++)
        {
            switch (commandRequests[i].Kind)
            {
                case RtsSelectionCommandIntentKind.FocusUnit:
                case RtsSelectionCommandIntentKind.SelectAll:
                case RtsSelectionCommandIntentKind.SelectAllSoldiers:
                case RtsSelectionCommandIntentKind.SelectAllVehicles:
                case RtsSelectionCommandIntentKind.EnterSelectionMode:
                case RtsSelectionCommandIntentKind.ExitSelectionMode:
                case RtsSelectionCommandIntentKind.DeselectAll:
                case RtsSelectionCommandIntentKind.EnterMoveTargetMode:
                case RtsSelectionCommandIntentKind.EnterAttackTargetMode:
                case RtsSelectionCommandIntentKind.EnterScanTargetMode:
                case RtsSelectionCommandIntentKind.HoldPosition:
                case RtsSelectionCommandIntentKind.Stop:
                case RtsSelectionCommandIntentKind.ReturnToBase:
                case RtsSelectionCommandIntentKind.DestroyFocusedUnit:
                case RtsSelectionCommandIntentKind.BoardNearestSoldiers:
                case RtsSelectionCommandIntentKind.ToggleAttackTargetMode:
                case RtsSelectionCommandIntentKind.CancelAttackTargetMode:
                    return true;
            }
        }

        return false;
    }

    public bool HasPendingTransportCommandRequests()
    {
        if (!TryGetCommandBuffers(out _, out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests, out _))
            return false;

        for (int i = 0; i < commandRequests.Length; i++)
        {
            RtsSelectionCommandIntentKind kind = commandRequests[i].Kind;
            if (kind == RtsSelectionCommandIntentKind.BoardTransport ||
                kind == RtsSelectionCommandIntentKind.DisembarkTransport)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryConsumeQueuedMoveOrder(int currentFrame, out Vector2 screenPosition)
    {
        screenPosition = default;
        RtsSelectionInputStateComponent state = ReadState();
        if (state.HasQueuedMoveOrder == 0 || currentFrame < state.QueuedMoveOrderFrame)
            return false;

        state.HasQueuedMoveOrder = 0;
        uint token = state.QueuedMoveOrderToken;
        screenPosition = ToVector2(state.QueuedMoveOrderScreenPosition);
        WriteState(state);
        return token == state.QueuedMoveOrderToken;
    }

    private RtsSelectionInputStateComponent ReadState()
    {
        return _inputStateSystem.TryRead(out _, out RtsSelectionInputStateComponent state)
            ? state
            : new RtsSelectionInputStateComponent { QueuedMoveOrderFrame = -1 };
    }

    private void WriteState(RtsSelectionInputStateComponent state)
    {
        _inputStateSystem.TryWrite(state);
    }

    private static byte ToByte(bool value) => value ? (byte)1 : (byte)0;
    private static bool ToBool(byte value) => value != 0;

    private static Vector2 ToVector2(float2 value)
    {
        return new Vector2(value.x, value.y);
    }

    private static float2 ToFloat2(Vector2 value)
    {
        return new float2(value.x, value.y);
    }

    private static Rect ToRect(float4 value)
    {
        return Rect.MinMaxRect(value.x, value.y, value.z, value.w);
    }

    private static float4 ToFloat4(Rect value)
    {
        return new float4(value.xMin, value.yMin, value.xMax, value.yMax);
    }

}
