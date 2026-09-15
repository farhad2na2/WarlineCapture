using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private bool _placementInstructionShown;
        private void ShowProductionCue()
        {
            if(_highlightPresentationSystem.HasPendingProduction)
            {
                _waitingForTutorialAction=true;
                _embeddedTutorialView.ApplyContextInstruction("ui.aria.production_wait.title","ui.aria.production_wait.body");
                _highlightPresentationSystem.ClearDirectTutorialCue();
                return;
            }
            Cue(_highlightPresentationSystem.ResolveProductionTutorialControl(out var key),key);
        }
        private bool ShowPendingPlacementInstruction(bool placing)
        {
            if(!placing)
            {
                if(_placementInstructionShown) _embeddedTutorialView.ApplyInteractionState(_activeCommandMode,false);
                _placementInstructionShown=false; return false;
            }
            _placementInstructionShown=true;
            _embeddedTutorialView.ApplyPlacementInstruction();
            Cue(_tutorialPlacement.ConfirmButton,"tutorial.next.confirm");
            return true;
        }
    }
}
