using Game.UI.Contracts;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {

        private void ExecuteDefenseGuidance()
        {
            _highlightPresentationSystem.ClearUiSurfaceCue();
            switch(_lastPanelModel.TutorialStep)
            {
                case 1: UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenWarning); break;
                case 2: UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.FocusWarning); break;
                case 3: case 10: case 11:
                    UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.ContinueExplanation); break;
                case 4:
                    if(!_highlightPresentationSystem.IsBuildDrawerOpen && _activeCommandMode==Game.Tactical.Contracts.TacticalCommandMode.Build)
                        InvokeAvailable(_tutorialPlacement?.ConfirmButton);
                    else InvokeAvailable(_highlightPresentationSystem.ResolveBuildTutorialControl(true,true,out _));
                    break;
                case 5: InvokeAvailable(_commandControlsView?.MoveButton); break;
                case 6: InvokeAvailable(_commandControlsView?.HoldButton); break;
                case 7:
                    bool resume=UiShellRuntimeGateway.TryReadMissionDefense(out var model) && model.RequiresHoldResume;
                    InvokeAvailable(resume ? _commandControlsView?.HoldButton : _commandControlsView?.StopButton); break;
                case 12: UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide); break;
                case 8: UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.RadarPing); break;
                case 9: _highlightPresentationSystem.TryExecuteUiSurface(5,4); break;
            }
            // Completion belongs to the projection's accepted command/fact predicates.
        }

        private static void InvokeAvailable(Button button)
        { if(button!=null && button.isActiveAndEnabled && button.interactable) button.onClick.Invoke(); }
    }
}
