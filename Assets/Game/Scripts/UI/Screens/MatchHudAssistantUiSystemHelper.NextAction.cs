using Game.Tactical.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private bool _waitingForTutorialAction, _continueTutorialAction, _selectionActionRequested;
        private BuildPlacementConfirmationBarView _tutorialPlacement;
        internal void BindPlacementConfirmation(BuildPlacementConfirmationBarView view) => _tutorialPlacement = view;
        private bool HasPendingTutorialPlacement => _tutorialPlacement != null && _tutorialPlacement.HasPendingPlacement;
        private MatchHudSelectionPanelView _tutorialSelection;
        private ThreatAlertV3PopupView _tutorialWarning;

        private void TickNextTutorialAction()
        {
            _waitingForTutorialAction = false; _continueTutorialAction = false; _selectionActionRequested=false;
            bool managed = UsesNextTutorialAction;
            if(managed && UiShellRuntimeGateway.TryReadMatchHudCommandState(out var commandState))
                _activeCommandMode=commandState.ActiveCommandMode;
            bool placing = HasPendingTutorialPlacement;
            if (UiShellRuntimeGateway.IsMissionFieldGuidePresenting() || !managed || _embeddedTutorialView == null || (!_embeddedTutorialView.IsPresentationVisible && !placing && !_highlightPresentationSystem.IsBuildDrawerOpen) ||
                !_lastPanelModel.HasRecommendation || _tutorialCinematicSuspended)
            { _highlightPresentationSystem.ClearDirectTutorialCue(); return; }
            int step=_lastPanelModel.TutorialStep;
            if(ShowPendingPlacementInstruction(placing)) return;
            if(_lastPanelModel.TutorialStepCount==5) { ShowFirstContactNextAction(step); return; }
            if(_lastPanelModel.TutorialStepCount==4) {ShowSupplyLineNextAction();return;}
            if(_lastPanelModel.TutorialStepCount==10) {ShowGridlockNextAction();return;}
            if(_lastPanelModel.TutorialStepCount==8) {ShowBreachNextAction(step);return;}
            if(_lastPanelModel.TutorialStepCount==9)
            {
                if(step>=7) ShowEarlyMissionThreat();
                else if(step==6) ShowProductionCue();
                else if(step==5) Cue(_embeddedTutorialView.ContinueButton,"tutorial.next.continue");
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
                    if (jump != null && jump.IsActive()) Cue(jump, "ui.v3.jump_to_threat.b1cb566ff9");
                    else Cue(_embeddedTutorialView.ContinueButton,"tutorial.next.continue");
                    break;
                case 3: ShowConstructionCue(true,false); break;
                case 4: ShowConstructionCue(true,true); break;
                case 9: ShowProductionCue(); break;
                case 5: ShowCommandOrDestination(_commandControlsView?.MoveButton,TacticalCommandMode.Move); break;
                case 6: ShowSelectionOrControl(_commandControlsView?.HoldButton,"mission.m03.guide.control.8"); break;
                case 7:
                    ShowSelectionOrControl(_commandControlsView?.HoldButton,"mission.m03.guide.control.8"); break;
                case 8: Cue(_commandControlsView?.ScanButton,"mission.m03.guide.control.6"); break;
                case 10: case 11: case 12: ShowDefenseBattleAction(); break;
                default: _highlightPresentationSystem.ClearDirectTutorialCue(); break;
            }
        }

        private void WaitForTutorialArrival()
        {
            _waitingForTutorialAction = true;
            _highlightPresentationSystem.ClearDirectTutorialCue();
            _embeddedTutorialView?.ApplyInteractionState(_activeCommandMode, true);
        }

        private void ShowFirstContactNextAction(int step)
        {
            if (step == 1)
            {
                if (UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target) && !target.NeedsSelection)
                    _highlightPresentationSystem.ClearDirectTutorialCue();
                else ShowSelectionTarget(default);
            }
            else if (step == 2) ShowCommandOrDestination(_commandControlsView?.MoveButton, TacticalCommandMode.Move);
            else if (step is 3 or 4) ShowEarlyMissionThreat();
            else WaitForTutorialArrival(); // The mission finale owns this transition.
        }

        private void ShowEarlyMissionThreat()
        {
            if (!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))
            { _highlightPresentationSystem.ClearDirectTutorialCue(); return; }
            if (_lastPanelModel.TutorialStepCount == 9 && _lastPanelModel.TutorialStep == 7)
            { ShowTutorialWorld(target.Destination, false); return; }
            if (target.NeedsSelection) { ShowSelectionTarget(target.Selection); return; }
            if (target.Moving || target.ExecutingAttack) { WaitForTutorialArrival(); return; }
            if (_activeCommandMode == TacticalCommandMode.Attack) ShowTutorialWorld(target.Destination, false);
            else Cue(_commandControlsView?.AttackButton, "ui.aria.press_attack");
        }

        private void ShowConstructionCue(bool defense,bool placement)
        {
            if(HasPendingTutorialPlacement)
            {
                Cue(_tutorialPlacement?.ConfirmButton,"tutorial.next.confirm");
                return; // An invalid footprint retains the existing world placement preview and reason.
            }
            Cue(_highlightPresentationSystem.ResolveBuildTutorialControl(defense,placement,out var key),key);
        }

        private void Cue(Button button,string key)
        {
            if(button!=null && button==_embeddedTutorialView?.ContinueButton)
            { _continueTutorialAction=true; _embeddedTutorialView.SetContinueAvailable(_lastPanelModel.CanExecute); }
            _highlightPresentationSystem.ShowTutorialControl(button,key);
        }

        private bool ShowMissingSelection(out UiMissionTutorialTarget target)
        {
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out target)) return false;
            if(!target.NeedsSelection) return false;
            ShowSelectionTarget(target.Selection,target.RequiredSelectionCount>1); return true;
        }

        private void ShowSelectionTarget(Vector3 position,bool group=false)
        {
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target) || !target.NeedsSelection) return;
            _selectionActionRequested=true;
            _embeddedTutorialView.SetNormalSelectionInstruction(_activeCommandMode==TacticalCommandMode.Select,target.DragSelection);
            if(_activeCommandMode!=TacticalCommandMode.Select)
            {Cue(_commandControlsView?.SelectButton,"ui.aria.press_select");return;}
            if(target.DragSelection)
            {
                _highlightPresentationSystem.ShowTutorialSelectionBox(target.SelectionMin,target.SelectionMax);
                if(_focusNextTutorialWorld && UiShellRuntimeGateway.TryFocusMissionTutorialTarget(true))
                {_tutorialFocusPendingUntil=Time.unscaledTime+2f;_tutorialFocusPendingStep=_lastPanelModel.TutorialStep;}
            }
            else ShowTutorialWorld(target.Selection,true);
        }

        private void ShowSelectionOrControl(Button button,string key)
        { if(!ShowMissingSelection(out _)) Cue(button,key); }

        private void ShowCommandOrDestination(Button button,TacticalCommandMode mode)
        {
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))
            { _highlightPresentationSystem.ClearDirectTutorialCue(); return; }
            if(target.NeedsSelection) { ShowSelectionTarget(target.Selection,target.RequiredSelectionCount>1); return; }
            if(target.Moving) { WaitForTutorialArrival(); return; }
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
                case 10:
                    if(UiShellRuntimeGateway.TryReadMissionExtraction(out var extraction))
                    {
                        if(!extraction.AircraftAtLanding) ShowCommandOrDestination(_commandControlsView?.MoveButton,TacticalCommandMode.Move);
                        else ShowTutorialWorld(extraction.LandingCenter,false,waitingArea:true);
                    }
                    else _highlightPresentationSystem.ClearDirectTutorialCue();
                    break;
                case 12: _highlightPresentationSystem.ClearDirectTutorialCue(); break;
                default: Cue(_embeddedTutorialView.ContinueButton,"tutorial.next.continue"); break;
            }
        }
    }
}
