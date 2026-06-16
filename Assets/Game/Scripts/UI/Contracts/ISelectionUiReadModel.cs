public interface ISelectionUiReadModel
{
    bool FocusedUnitCanHold { get; }
    TacticalCommandReasonCode FocusedUnitHoldDisabledReason { get; }
    bool FocusedUnitCanStop { get; }
    TacticalCommandReasonCode FocusedUnitStopDisabledReason { get; }
    bool FocusedUnitCanScan { get; }
    TacticalCommandReasonCode FocusedUnitScanDisabledReason { get; }
}
