using Game.Tactical.Contracts;

namespace Game.UI.Contracts
{
    public interface ISelectionUiReadModel
    {
        bool HasAnySelectedUnits { get; }
        bool FocusedUnitCanHold { get; }
        TacticalCommandReasonCode FocusedUnitHoldDisabledReason { get; }
        bool FocusedUnitCanStop { get; }
        TacticalCommandReasonCode FocusedUnitStopDisabledReason { get; }
        bool FocusedUnitCanScan { get; }
        TacticalCommandReasonCode FocusedUnitScanDisabledReason { get; }
    }
}
