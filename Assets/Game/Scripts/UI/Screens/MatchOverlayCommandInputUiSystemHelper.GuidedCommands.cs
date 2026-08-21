using Game.Tactical.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MatchOverlayCommandInputUiSystemHelper
    {
        private sealed partial class Binding
        {
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
