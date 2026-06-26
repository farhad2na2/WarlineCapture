using Unity.Entities;
using UnityEngine;

public sealed class SelectionUiCommandSystem : ISelectionUiCommand
{
    private readonly RtsSelectionInputCompositionSystemHelper _inputSystem = new();
    private readonly FocusedUnitUiReadModelUiSystemHelper _focusedUnitUiReadModelSystem = new();
    private readonly System.Func<bool> _isGameplayInputLocked;

    public SelectionUiCommandSystem(System.Func<bool> isGameplayInputLocked = null)
    {
        _isGameplayInputLocked = isGameplayInputLocked;
    }

    public void CaptureUiClickSequence()
    {
        _inputSystem.CaptureUiClickSequence();
    }

    public bool RequestDeselectAll()
    {
        return Queue(RtsSelectionCommandIntentKind.DeselectAll);
    }

    public bool RequestSelectAll()
    {
        return QueueSelectAll(RtsSelectionCommandIntentKind.SelectAll);
    }

    public bool RequestSelectAllSoldiers()
    {
        return QueueSelectAll(RtsSelectionCommandIntentKind.SelectAllSoldiers);
    }

    public bool RequestSelectAllVehicles()
    {
        return QueueSelectAll(RtsSelectionCommandIntentKind.SelectAllVehicles);
    }

    public bool RequestEnterSelectionMode()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.EnterSelectionMode);
    }

    public bool RequestExitSelectionMode()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.ExitSelectionMode);
    }

    public bool RequestMoveCommandMode()
    {
        CaptureUiClickSequence();
        bool queued = Queue(RtsSelectionCommandIntentKind.EnterMoveTargetMode);
        SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
            $"requestMoveCommandMode queued={queued} frame={UnityEngine.Time.frameCount}");
        return queued;
    }

    public bool RequestAttackCommandMode()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.EnterAttackTargetMode);
    }

    public bool RequestScanCommandMode()
    {
        CaptureUiClickSequence();
        bool queued = Queue(RtsSelectionCommandIntentKind.EnterScanTargetMode);
        SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
            $"requestScanCommandMode queued={queued} frame={UnityEngine.Time.frameCount}");
        return queued;
    }

    public bool RequestBoardTargetMode()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.EnterBoardTargetMode);
    }

    public bool RequestHoldPosition()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.HoldPosition);
    }

    public bool RequestStop()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.Stop);
    }

    public bool RequestDestroyFocusedUnit()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.DestroyFocusedUnit);
    }

    public bool RequestReturnToBase()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.ReturnToBase);
    }

    public bool RequestBoardNearestSoldiers()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.BoardNearestSoldiers);
    }

    public bool RequestBoardAllSelectedTransport()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.BoardAllSelectedTransport);
    }

    public bool RequestCancelActiveCommandMode()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.CancelActiveCommandMode);
    }

    public bool RequestFocusedAttackOrTargetMode()
    {
        return Queue(RtsSelectionCommandIntentKind.ToggleAttackTargetMode);
    }

    public bool RequestFocusedTransportDisembark()
    {
        if (IsGameplayInputLocked())
            return false;

        if (!TryReadFocusedUnit(out Entity focusedUnit))
            return false;

        return _inputSystem.QueueDisembarkTransportCommandRequest(focusedUnit, UnityEngine.Time.frameCount);
    }

    public bool RequestFocusedTransportPassengerDisembark(Entity passenger)
    {
        if (IsGameplayInputLocked() || passenger == Entity.Null)
            return false;

        if (!TryReadFocusedUnit(out Entity focusedUnit))
            return false;

        return _inputSystem.QueueDisembarkTransportPassengerCommandRequest(focusedUnit, passenger, UnityEngine.Time.frameCount);
    }

    private bool Queue(RtsSelectionCommandIntentKind kind)
    {
        if (IsGameplayInputLocked())
        {
            SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                $"uiCommandQueueBlocked kind={kind} reason=GameplayInputLocked frame={UnityEngine.Time.frameCount}");
            if (kind == RtsSelectionCommandIntentKind.EnterScanTargetMode ||
                kind == RtsSelectionCommandIntentKind.Scan)
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
                    $"uiCommandQueueBlocked kind={kind} reason=GameplayInputLocked frame={UnityEngine.Time.frameCount}");
            }
            return false;
        }

        bool queued = _inputSystem.QueueCommandIntentRequest(kind, UnityEngine.Time.frameCount);
        if (kind == RtsSelectionCommandIntentKind.EnterMoveTargetMode)
        {
            SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                $"uiCommandQueued kind={kind} queued={queued} frame={UnityEngine.Time.frameCount}");
        }
        if (kind == RtsSelectionCommandIntentKind.EnterScanTargetMode ||
            kind == RtsSelectionCommandIntentKind.Scan)
        {
            SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
                $"uiCommandQueued kind={kind} queued={queued} frame={UnityEngine.Time.frameCount}");
        }

        return queued;
    }

    private bool QueueSelectAll(RtsSelectionCommandIntentKind kind)
    {
        if (IsGameplayInputLocked())
        {
            SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                $"uiCommandQueueBlocked kind={kind} reason=GameplayInputLocked frame={UnityEngine.Time.frameCount}");
            return false;
        }

        return _inputSystem.QueueSelectAllCommandRequest(
            kind,
            new Rect(0f, 0f, Screen.width, Screen.height),
            UnityEngine.Time.frameCount);
    }

    private bool IsGameplayInputLocked()
    {
        return _isGameplayInputLocked?.Invoke() == true;
    }

    private bool TryReadFocusedUnit(out Entity focusedUnit)
    {
        focusedUnit = Entity.Null;
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        if (!_focusedUnitUiReadModelSystem.TryRead(
                em,
                out FocusedUnitUiReadModelComponent model,
                out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> _))
        {
            return false;
        }

        if (model.HasFocusedUnit == 0 || model.FocusedUnit == Entity.Null)
            return false;

        focusedUnit = model.FocusedUnit;
        return true;
    }
}
