using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture powerRelayMissionArt;
        private void ApplyPowerRelay(in UiMissionBriefingModel model)
        {
            Set(missionNumber,"CH02 · M04");Set(operationCodename,UiShellRuntimeGateway.Localization.Get("mission.power_relay.name"));
            Set(missionTitle,UiShellRuntimeGateway.Localization.Get("mission.power_relay.name"));Set(missionSummary,UiShellRuntimeGateway.Localization.Get("mission.power_relay.summary"));
            Set(locationLabel,UiShellRuntimeGateway.Localization.Get("mission.power_relay.location"));Set(enemyIntelLabel,UiShellRuntimeGateway.Localization.Get("mission.power_relay.enemy_intel"));
            if(missionArtImage!=null)missionArtImage.texture=powerRelayMissionArt;
            string[] names={"shelter","restore","secure"};for(int i=0;i<(objectiveLabels?.Length??0);i++)Set(objectiveLabels[i],UiShellRuntimeGateway.Localization.Get(i<3?"mission.power_relay.objective."+names[i]:"mission.power_relay.star.3"));
            SetAt(conditionLabels,0,UiShellRuntimeGateway.Localization.Get("mission.power_relay.resources"));SetAt(conditionLabels,1,UiShellRuntimeGateway.Localization.Get("mission.power_relay.forces"));SetAt(conditionLabels,2,UiShellRuntimeGateway.Localization.Get("mission.power_relay.deadline"));
        }
    }
}
