using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MatchOverlayCommandInputUiSystemHelper
    {
        private sealed partial class Binding
        {
            public void RefreshCommandControlState(ISelectionUiReadModel selectionUiReadModel = null)
            {
                _view.RefreshMissionRestrictions();
                ISelectionUiReadModel readModel = selectionUiReadModel ?? _selectionUiReadModel;
                uint commandStateVersion = readModel != null ? readModel.CommandStateVersion : 0u;
                if (commandStateVersion != 0u &&
                    _hasAppliedVersionedCommandState &&
                    _lastAppliedCommandStateVersion == commandStateVersion)
                {
                    return;
                }

                // Keep the bottom command rail interactive so hover/selected feedback remains visible.
                // Unavailable commands still report the specific rejection reason through TryAcceptCapability.
                ApplyButtonInteractable(_view.HoldButton, true);
                ApplyButtonInteractable(_view.StopButton, true);
                ApplyButtonInteractable(_view.CommandWheelStopButton, readModel == null || readModel.FocusedUnitCanStop);
                // Keep Scan pressable so unavailable units surface an explicit rejection message.
                ApplyButtonInteractable(_view.ScanButton, true);
                // Keep Board pressable so no-selection and invalid-selection states can surface feedback.
                ApplyButtonInteractable(_view.BoardButton, true);

                _hasAppliedVersionedCommandState = commandStateVersion != 0u;
                _lastAppliedCommandStateVersion = commandStateVersion;
            }

            private bool TryAcceptCapability(CommandCapability capability)
            {
                ISelectionUiReadModel readModel = _selectionUiReadModel;
                if (readModel == null)
                    return true;

                if (!readModel.HasAnySelectedUnits)
                {
                    ApplyCommandResult(TacticalCommandResult.Rejected(
                        TacticalCommandReasonCode.NoSelection,
                        ResolveUnavailableFeedbackMessage(capability, TacticalCommandReasonCode.NoSelection)));
                    return false;
                }

                // A box/class selection has no single focused unit. Hold and Stop apply to
                // the group; the command owner validates each living, owned, movable actor.
                if (!readModel.HasFocusedUnit && capability is CommandCapability.Hold or CommandCapability.Stop)
                    return true;

                bool accepted = capability switch
                {
                    CommandCapability.Hold => readModel.FocusedUnitCanHold,
                    CommandCapability.Stop => readModel.FocusedUnitCanStop,
                    CommandCapability.Scan => readModel.FocusedUnitCanScan,
                    _ => true
                };
                if (accepted)
                    return true;

                TacticalCommandReasonCode reason = capability switch
                {
                    CommandCapability.Hold => readModel.FocusedUnitHoldDisabledReason,
                    CommandCapability.Stop => readModel.FocusedUnitStopDisabledReason,
                    CommandCapability.Scan => readModel.FocusedUnitScanDisabledReason,
                    _ => TacticalCommandReasonCode.CommandUnavailable
                };
                ApplyCommandResult(TacticalCommandResult.Rejected(reason, ResolveUnavailableFeedbackMessage(capability, reason)));
                return false;
            }

            private static TacticalCommandReasonCode ResolveFallbackReason(CommandCapability capability)
            {
                return capability switch
                {
                    CommandCapability.Scan => TacticalCommandReasonCode.ScanUnavailable,
                    _ => TacticalCommandReasonCode.NoSelection
                };
            }

            private string ResolveUnavailableFeedbackMessage(CommandCapability capability, TacticalCommandReasonCode reason)
            {
                if (reason == TacticalCommandReasonCode.NoSelection)
                {
                    return capability switch
                    {
                        CommandCapability.Hold => _gameTextResolver.Get("tactical.command.unavailable.hold_no_selection", "Select units before holding position."),
                        CommandCapability.Stop => _gameTextResolver.Get("tactical.command.unavailable.stop_no_selection", "Select units before stopping orders."),
                        CommandCapability.Scan => _gameTextResolver.Get("tactical.command.unavailable.scan_no_selection", "Select a scanner or combat unit first."),
                        _ => ResolveReasonText(reason)
                    };
                }

                if (capability == CommandCapability.Scan && reason == TacticalCommandReasonCode.ScanUnavailable)
                    return _gameTextResolver.Get("tactical.command.unavailable.scan_no_selection", "Select a scanner or combat unit first.");

                return ResolveReasonText(reason);
            }

            private string ResolveReasonText(TacticalCommandReasonCode reason)
            {
                return _gameTextResolver.Get(
                    TacticalCommandFeedbackText.ToDisplayTextKey(reason),
                    TacticalCommandFeedbackText.ToDisplayText(reason));
            }

            private static void ApplyButtonInteractable(Button button, bool interactable)
            {
                if (button != null)
                    button.interactable = interactable;
            }

        }
    }
}
