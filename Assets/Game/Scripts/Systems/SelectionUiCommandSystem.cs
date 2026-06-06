using Unity.Entities;
using UnityEngine;

public sealed class SelectionUiCommandSystem
{
    private readonly RtsSelectionInputSystem _inputSystem = new();
    private readonly FocusedUnitUiReadModelSystem _focusedUnitUiReadModelSystem = new();

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

    public bool RequestHoldPosition()
    {
        return Queue(RtsSelectionCommandIntentKind.HoldPosition);
    }

    public bool RequestStop()
    {
        return Queue(RtsSelectionCommandIntentKind.Stop);
    }

    public bool RequestDestroyFocusedUnit()
    {
        return Queue(RtsSelectionCommandIntentKind.DestroyFocusedUnit);
    }

    public bool RequestFocusedAttackOrTargetMode()
    {
        return Queue(RtsSelectionCommandIntentKind.ToggleAttackTargetMode);
    }

    public bool RequestFocusedTransportDisembark()
    {
        if (!TryReadFocusedUnit(out Entity focusedUnit))
            return false;

        return _inputSystem.QueueDisembarkTransportCommandRequest(focusedUnit, Time.frameCount);
    }

    private bool Queue(RtsSelectionCommandIntentKind kind)
    {
        return _inputSystem.QueueCommandIntentRequest(kind, Time.frameCount);
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
