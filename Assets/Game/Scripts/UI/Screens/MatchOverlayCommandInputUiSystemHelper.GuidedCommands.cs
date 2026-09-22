using Game.Tactical.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MatchOverlayCommandInputUiSystemHelper
    {
        private sealed partial class Binding
        {
            private void OnSelectButtonClicked()
            {
                CaptureCommandUiClick();
                bool enterSelectionMode = !IsCommandModePresented(TacticalCommandMode.Select);
                bool queued = _selectionUiCommandSystem != null &&
                    (enterSelectionMode
                        ? _selectionUiCommandSystem.RequestEnterSelectionMode()
                        : _selectionUiCommandSystem.RequestExitSelectionMode());

                if (queued) _commandModeQueued?.Invoke(enterSelectionMode ? TacticalCommandMode.Select : TacticalCommandMode.None);
                if (!queued)
                    ApplyCommandResult(TacticalCommandResult.Rejected(
                        TacticalCommandReasonCode.CommandUnavailable,
                        "Selection command unavailable."));
            }

            private void OnMoveButtonClicked()
            {
                CaptureCommandUiClick();
                LogMoveCommandTrace(
                    $"moveButtonClicked view={_view.name} hasSelectionUi={_selectionUiCommandSystem != null}");
                bool queued = _selectionUiCommandSystem != null &&
                    _selectionUiCommandSystem.RequestMoveCommandMode();
                LogMoveCommandTrace($"moveButtonRequestMoveCommandMode queued={queued}");

                if (queued)
                {
                    _commandModeQueued?.Invoke(TacticalCommandMode.Move);
                    return;
                }

                ApplyCommandResult(TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Move command unavailable."));
            }

            private void OnAttackButtonClicked()
            {
                CaptureCommandUiClick();
                UiShellRuntimeGateway.TryAttackExpandedEnemyBase();
                bool queued = _selectionUiCommandSystem != null &&
                    _selectionUiCommandSystem.RequestAttackCommandMode();
                if (queued)
                {
                    _commandModeQueued?.Invoke(TacticalCommandMode.Attack);
                    return;
                }

                ApplyCommandResult(TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.CommandUnavailable,
                    "Attack command unavailable."));
            }
        }
    }
}
