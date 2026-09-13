using Game.Tactical.Contracts;
using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private static int TutorialCueBit(byte step, UiTutorialNarrationPhase phase)
        {
            if (step is < 1 or > 12 || phase > UiTutorialNarrationPhase.WorldTarget)
                return -1;
            return ((step - 1) * 2) + (int)phase;
        }

        private void RefreshBreachInputMode() {if(UiShellRuntimeGateway.TryReadBreachInputMode(out int mode))_activeCommandMode=(TacticalCommandMode)mode;}
        private void ShowBreachNextAction(int step)
        {
            RefreshBreachInputMode();
            if(step==1) {Cue(_embeddedTutorialView.DoItButton,"tutorial.next.continue");return;}
            if(step==8 && !_lastPanelModel.CanExecute) {_highlightPresentationSystem.ClearDirectTutorialCue();return;}
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target)) {_highlightPresentationSystem.ClearDirectTutorialCue();return;}
            if(step==2 || target.NeedsSelection) {ShowSelectionTarget(target.Selection);return;}
            bool attack=step is 3 or 5 or 6;
            var mode=attack?TacticalCommandMode.Attack:TacticalCommandMode.Move;
            if(_activeCommandMode==mode) ShowTutorialWorld(target.Destination,false);
            else Cue(attack?_commandControlsView?.AttackButton:_commandControlsView?.MoveButton,attack?"ui.aria.press_attack":"ui.aria.press_move");
        }
        private void ExecuteBreachGuidance()
        {
            RefreshBreachInputMode();
            int step=_lastPanelModel.TutorialStep;
            if(step==1) {UiShellRuntimeGateway.TryContinueBreachPlan();return;}
            if(step==8 && !_lastPanelModel.CanExecute || !UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target)) return;
            if(target.NeedsSelection || step==2)
            {
                if(_activeCommandMode is not (TacticalCommandMode.Select or TacticalCommandMode.None)) InvokeAvailable(_commandControlsView?.SelectButton);
                else UiShellRuntimeGateway.TrySelectBreachActor();
                return;
            }
            if(TryAdvanceTutorialCommandSubstep()) return;
            UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.ExecuteRecommendation,fromTakeover:true);
        }
    }
}
