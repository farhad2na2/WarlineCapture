using Game.Tactical.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private BuildPlacementConfirmationBarView _tutorialPlacement;
        private MatchHudSelectionPanelView _tutorialSelection;
        private ThreatAlertV3PopupView _tutorialWarning;

        private void TickNextTutorialAction()
        {
            bool managed = UsesNextTutorialAction;
            if(managed && UiShellRuntimeGateway.TryReadMatchHudCommandState(out var commandState))
                _activeCommandMode=commandState.ActiveCommandMode;
            bool placing = _activeCommandMode==TacticalCommandMode.Build && !_highlightPresentationSystem.IsBuildDrawerOpen;
            if (UiShellRuntimeGateway.IsMissionFieldGuidePresenting() || !managed || _embeddedTutorialView == null || (!_embeddedTutorialView.IsPresentationVisible && !placing) ||
                !_lastPanelModel.HasRecommendation || _tutorialCinematicSuspended)
            { _highlightPresentationSystem.ClearDirectTutorialCue(); return; }
            int step=_lastPanelModel.TutorialStep;
            if(_lastPanelModel.TutorialStepCount==8) {ShowBreachNextAction(step);return;}
            if(_lastPanelModel.TutorialStepCount==9)
            {
                if(step==6) ShowProductionCue();
                else if(step==5) Cue(_embeddedTutorialView.DoItButton,"tutorial.next.continue");
                else ShowConstructionCue(false,step==4);
                return;
            }
            if(UiShellRuntimeGateway.TryReadMissionExtraction(out _)) { ShowExtractionNextAction(step); return; }
            switch(step)
            {
                case 1: case 2:
                    if(_tutorialWarning==null && _buttonRoot!=null)
                        _tutorialWarning=_buttonRoot.root.GetComponentInChildren<ThreatAlertV3PopupView>(true);
                    var jump=_tutorialWarning?.JumpToThreatButton;
                    Cue(jump!=null && jump.IsActive() ? jump : _embeddedTutorialView.DoItButton,"tutorial.next.continue"); break;
                case 4: ShowConstructionCue(true,true); break;
                case 9: ShowProductionCue(); break;
                case 5: ShowCommandOrDestination(_commandControlsView?.MoveButton,TacticalCommandMode.Move); break;
                case 6: ShowSelectionOrControl(_commandControlsView?.HoldButton,"mission.m03.guide.control.8"); break;
                case 7:
                    bool resume=UiShellRuntimeGateway.TryReadMissionDefense(out var defense) && defense.RequiresHoldResume;
                    ShowSelectionOrControl(resume ? _commandControlsView?.HoldButton : _commandControlsView?.StopButton,
                        resume ? "mission.m03.guide.control.8" : "mission.m03.guide.control.10"); break;
                case 8: Cue(_commandControlsView?.SupportButton,"mission.m03.guide.control.6"); break;
                case 10: case 11: Cue(_embeddedTutorialView.DoItButton,"tutorial.next.continue"); break;
                default: Cue(_embeddedTutorialView.DoItButton,"tutorial.next.continue"); break;
            }
        }

        private void ShowConstructionCue(bool defense,bool placement)
        {
            if(!_highlightPresentationSystem.IsBuildDrawerOpen && _activeCommandMode==TacticalCommandMode.Build)
            {
                if(_tutorialPlacement==null && _buttonRoot!=null)
                    _tutorialPlacement=_buttonRoot.root.GetComponentInChildren<BuildPlacementConfirmationBarView>(true);
                Cue(_tutorialPlacement?.ConfirmButton,"tutorial.next.confirm");
                return; // An invalid footprint retains the existing world placement preview and reason.
            }
            Cue(_highlightPresentationSystem.ResolveBuildTutorialControl(defense,placement,out var key),key);
        }

        private void ShowProductionCue() => Cue(_highlightPresentationSystem.ResolveProductionTutorialControl(out var key),key);
        private void Cue(Button button,string key) => _highlightPresentationSystem.ShowTutorialControl(button,key);

        private bool ShowMissingSelection(out UiMissionTutorialTarget target)
        {
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out target)) return false;
            if(!target.NeedsSelection) return false;
            ShowSelectionTarget(target.Selection,target.RequiredSelectionCount>1); return true;
        }

        private void ShowSelectionTarget(Vector3 position,bool group=false)
        {
            if(_activeCommandMode!=TacticalCommandMode.Select && (group || _activeCommandMode!=TacticalCommandMode.None))
                Cue(_commandControlsView?.SelectButton,"ui.guidance.select_squad");
            else ShowTutorialWorld(position,true);
        }

        private void ShowSelectionOrControl(Button button,string key)
        { if(!ShowMissingSelection(out _)) Cue(button,key); }

        private void ShowCommandOrDestination(Button button,TacticalCommandMode mode)
        {
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))
            { _highlightPresentationSystem.ClearDirectTutorialCue(); return; }
            if(target.NeedsSelection) { ShowSelectionTarget(target.Selection,target.RequiredSelectionCount>1); return; }
            if(target.Moving) { _highlightPresentationSystem.ClearDirectTutorialCue(); return; }
            if(_activeCommandMode==mode) ShowTutorialWorld(target.Destination,false);
            else Cue(button,mode==TacticalCommandMode.Board
                ? (_commandControlsView?.CommandWheelPanel?.IsOpen == true ? "tutorial.next.board" : "ui.v3.commands.92e37c53d8")
                : "ui.aria.press_move");
        }

        private void ShowExtractionNextAction(int step)
        {
            switch(step)
            {
                case 2: case 4: case 8:
                    if(UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target) && target.NeedsSelection)
                        ShowSelectionTarget(target.Selection,target.RequiredSelectionCount>1);
                    else _highlightPresentationSystem.ClearDirectTutorialCue();
                    break;
                case 3: case 6: case 11: ShowCommandOrDestination(_commandControlsView?.MoveButton,TacticalCommandMode.Move); break;
                case 5: case 9: ShowCommandOrDestination(_commandControlsView?.CommandWheelPanel?.NextBoardButton,TacticalCommandMode.Board); break;
                case 7:
                    if(ShowMissingSelection(out _)) break;
                    if(_tutorialSelection==null && _buttonRoot!=null)
                        _tutorialSelection=_buttonRoot.root.GetComponentInChildren<MatchHudSelectionPanelView>(true);
                    Cue(_tutorialSelection?.ResolvePassengerTutorialButton(),"tutorial.next.unload"); break;
                case 10: case 12: _highlightPresentationSystem.ClearDirectTutorialCue(); break;
                default: Cue(_embeddedTutorialView.DoItButton,"tutorial.next.continue"); break;
            }
        }
    }
}
