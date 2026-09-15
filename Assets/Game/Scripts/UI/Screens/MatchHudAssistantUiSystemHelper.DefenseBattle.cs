using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private void ShowDefenseBattleAction()
        {
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))
            { _highlightPresentationSystem.ClearDirectTutorialCue(); return; }
            if(target.NeedsSelection) {ShowSelectionTarget(target.Selection);return;}
            if(target.Moving)
            {
                _waitingForTutorialAction=true;
                ShowTutorialWorld(target.Destination,false);
                return;
            }
            switch(target.BattleAction)
            {
                case UiTutorialBattleAction.Move:
                    if(_activeCommandMode==TacticalCommandMode.Move) ShowTutorialWorld(target.Destination,false);
                    else Cue(_commandControlsView?.MoveButton,"ui.aria.press_move");
                    break;
                case UiTutorialBattleAction.Hold:
                    Cue(_commandControlsView?.HoldButton,"mission.m03.guide.control.8");
                    break;
                default:
                    // This ring marks the defended road/contact; ARIA explicitly explains the wait.
                    _waitingForTutorialAction=true;
                    ShowTutorialWorld(target.Destination,false);
                    break;
            }
        }
    }
}
