using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture supplyLineMissionArt;
        private void ApplySupplyLine(in UiMissionBriefingModel model)
        {
            Set(missionNumber,"CH02 · M02");
            Set(operationCodename,UiShellRuntimeGateway.Localization.Get("mission.supply_line.name"));
            Set(missionTitle,UiShellRuntimeGateway.Localization.Get("mission.supply_line.name"));
            Set(missionSummary,UiShellRuntimeGateway.Localization.Get("mission.supply_line.summary"));
            Set(locationLabel,UiShellRuntimeGateway.Localization.Get("mission.supply_line.location"));
            Set(enemyIntelLabel,UiShellRuntimeGateway.Localization.Get("mission.supply_line.enemy_intel"));
            if(missionArtImage!=null) missionArtImage.texture=supplyLineMissionArt;
            string[] names={"oil","fuel","reserve"};
            for(int i=0;i<(objectiveLabels?.Length??0);i++) Set(objectiveLabels[i],UiShellRuntimeGateway.Localization.Get(i<3?"mission.supply_line.objective."+names[i]:"mission.supply_line.star.3"));
            SetAt(conditionLabels,0,UiShellRuntimeGateway.Localization.Get("mission.supply_line.resources"));
            SetAt(conditionLabels,1,UiShellRuntimeGateway.Localization.Get("mission.supply_line.forces"));
            SetAt(conditionLabels,2,UiShellRuntimeGateway.Localization.Get("mission.supply_line.deadline"));
            SetAt(conditionNameLabels,0,UiShellRuntimeGateway.Localization.Get("mission.m02.resources.label"));
            SetAt(conditionNameLabels,1,UiShellRuntimeGateway.Localization.Get("mission.m03.label.forces"));
            SetAt(conditionNameLabels,2,UiShellRuntimeGateway.Localization.Get("mission.supply_line.time_limit"));
        }
    }
}
