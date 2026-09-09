using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private void ExecuteExtractionGuidance()
        {
            switch(_lastPanelModel.TutorialStep)
            {
                case 1: UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.ContinuePlan); break;
                case 3: case 6: case 11: InvokeAvailable(_commandControlsView?.MoveButton); break;
                case 5: case 9: InvokeAvailable(_commandControlsView?.BoardButton); break;
                case 2: case 4: case 8: InvokeAvailable(_commandControlsView?.SelectButton); break;
                default: UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide); break;
            }
        }
    }
}
