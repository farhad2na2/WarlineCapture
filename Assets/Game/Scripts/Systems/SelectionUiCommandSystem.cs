using Unity.Entities;
using UnityEngine;

public sealed class SelectionUiCommandSystem
{
    private readonly RtsSelectionInputSystem _inputSystem = new();
    private readonly FocusedUnitUiReadModelSystem _focusedUnitUiReadModelSystem = new();
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
        return Queue(RtsSelectionCommandIntentKind.SelectAll);
    }

    public bool RequestSelectAllSoldiers()
    {
        return Queue(RtsSelectionCommandIntentKind.SelectAllSoldiers);
    }

    public bool RequestSelectAllVehicles()
    {
        return Queue(RtsSelectionCommandIntentKind.SelectAllVehicles);
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
        return Queue(RtsSelectionCommandIntentKind.EnterMoveTargetMode);
    }

    public bool RequestAttackCommandMode()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.EnterAttackTargetMode);
    }

    public bool RequestScanCommandMode()
    {
        CaptureUiClickSequence();
        return Queue(RtsSelectionCommandIntentKind.EnterScanTargetMode);
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

        return _inputSystem.QueueDisembarkTransportCommandRequest(focusedUnit, Time.frameCount);
    }

    private bool Queue(RtsSelectionCommandIntentKind kind)
    {
        if (IsGameplayInputLocked())
            return false;

        return _inputSystem.QueueCommandIntentRequest(kind, Time.frameCount);
    }

    private bool IsGameplayInputLocked()
    {
        return _isGameplayInputLocked?.Invoke() == true;
    }

    private bool TryReadFocusedUnit(out Entity focusedUnit)
    {
        focusedUnit = Entity.Null;
        World world = World.DefaultGameObjectInjectionWorld;
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
