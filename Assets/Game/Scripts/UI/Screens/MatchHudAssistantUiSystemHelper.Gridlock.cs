using Game.Tactical.Contracts;
using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private void ShowGridlockNextAction()
        {
            RefreshBreachInputMode();
            if(!_lastPanelModel.CanShow || !UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))
            {_highlightPresentationSystem.ClearDirectTutorialCue();return;}
            if(target.BattleAction==UiTutorialBattleAction.Watch)
            {
                _waitingForTutorialAction=true;_highlightPresentationSystem.ShowTutorialArea(target.Destination,target.AreaRadius);return;
            }
            if(target.NeedsSelection){ShowSelectionTarget(target.Selection,false);return;}
            if(target.Moving){WaitForTutorialArrival();return;}
            if(_activeCommandMode==TacticalCommandMode.Move)ShowTutorialWorld(target.Destination,false);
            else Cue(_commandControlsView?.MoveButton,"ui.aria.press_move");
        }
    }
}
