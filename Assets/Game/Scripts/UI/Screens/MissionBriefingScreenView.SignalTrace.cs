using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture signalTraceMissionArt;
        private void ApplySignalTrace(in UiMissionBriefingModel model)
        {
            Set(missionNumber,"CH03 · M01");Set(operationCodename,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.name"));
            Set(missionTitle,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.name"));Set(missionSummary,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.summary"));
            Set(locationLabel,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.location"));Set(enemyIntelLabel,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.enemy_intel"));
            if(missionArtImage!=null)missionArtImage.texture=signalTraceMissionArt;
            string[] names={"compare","intercept","recover"};for(int i=0;i<(objectiveLabels?.Length??0);i++)Set(objectiveLabels[i],UiShellRuntimeGateway.Localization.Get(i<3?"mission.signal_trace.objective."+names[i]:"mission.signal_trace.star.3"));
            SetAt(conditionLabels,0,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.resources"));SetAt(conditionLabels,1,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.forces"));SetAt(conditionLabels,2,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.deadline"));
        }
    }
}
