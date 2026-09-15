using System;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MatchOverlayCommandInputUiSystemHelper
    {
        private sealed partial class Binding
        {
            private void BindCommandTabRuntimeFallbacks()
            {
                MatchOverlayCommandTabView[] tabs = _view.CommandTabGroup != null ? _view.CommandTabGroup.Tabs : null;
                if (tabs == null)
                    return;

                for (int i = 0; i < tabs.Length; i++)
                {
                    Button button = tabs[i]?.Button;
                    if (button == null)
                        continue;

                    Button capturedButton = button;
                    bool scanAlias = IsScanAliasCommandButton(capturedButton);
                    UnityEngine.Events.UnityAction action = () => OnCommandTabRuntimeClick(capturedButton, scanAlias);
                    capturedButton.onClick.AddListener(action);
                    _commandTabRuntimeListeners.Add((capturedButton, action));
                }
            }

            private void UnbindCommandTabRuntimeFallbacks()
            {
                for (int i = 0; i < _commandTabRuntimeListeners.Count; i++)
                    _commandTabRuntimeListeners[i].Button?.onClick.RemoveListener(_commandTabRuntimeListeners[i].Action);

                _commandTabRuntimeListeners.Clear();
            }

            private void OnCommandTabRuntimeClick(Button button, bool scanAlias)
            {
                if (scanAlias)
                {
                    // M3 loans a mission radar action on this slot. It must not go
                    // through selected-unit Scan capability/targeting.
                    if (UiShellRuntimeGateway.TryReadMissionDefense(out var defense))
                    {
                        CaptureCommandUiClick();
                        bool queued = UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.RadarPing);
                        ApplyCommandResult(queued
                            ? TacticalCommandResult.Success(_gameTextResolver.Get("mission.m03.ping.requested", "Radar sweep requested."))
                            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.ScanUnavailable, defense.PingText));
                        return;
                    }
                    OnScanButtonClicked();
                    return;
                }

                CaptureCommandUiClick();
            }

            private bool IsScanAliasCommandButton(Button button)
            {
                return button != null &&
                       !IsKnownCommandButton(button) &&
                       string.Equals(button.name, "SupportCommand", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
