using Game.Skirmish.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private void ObservePublicObjectiveSupport(ref AriaSkirmishObservation view, AriaPresentedObjectiveFrame frame)
        {
            var support=new AriaObjectiveSupportObservation
            {
                Presented=frame.Presented, Perspective=(byte)frame.Goal.Perspective,
                SimulationSeconds=frame.SimulationSeconds,
                Evacuation=frame.Goal.Perspective==SkirmishPublicGoalPerspective.Evacuate,
                CaptureMajority=frame.Goal.Perspective==SkirmishPublicGoalPerspective.Capture
            };
            if(frame.Groups==null){view.ObjectiveSupport=default;return;}
            foreach(var group in frame.Groups)
            {
                if(group==null||!group.Own||!group.Living)continue;
                if(group.CombatInfantry&&group.Dismounted)support.DismountedCombatGroups++;
                if(group.PublicTerminalRisk||group.UnderObservedThreat)support.CriticalPublicRisk=true;
            }
            if(UiShellRuntimeGateway.TryReadSkirmishReadiness(out var readiness)&&readiness.Visible)
            {
                support.ResearchVisible=true;support.ResearchInProgress=readiness.InProgress;
                support.ResearchId=readiness.ResearchId;support.Readiness=readiness.Stage;
                support.ResearchMaterials=readiness.MaterialsCost;
            }
            view.ObjectiveSupport=support;
            ObserveAircraftService(ref view);
        }
    }
}
