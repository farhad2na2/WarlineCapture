using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture gridlockMissionArt;
        private void ApplyGridlock(in UiMissionBriefingModel model)
        {
            Set(missionNumber,"CH02 · M01");
            Set(operationCodename,UiShellRuntimeGateway.Localization.Get("mission.gridlock.name"));
            Set(missionTitle,UiShellRuntimeGateway.Localization.Get("mission.gridlock.name"));
            Set(missionSummary,UiShellRuntimeGateway.Localization.Get("mission.gridlock.summary"));
            Set(locationLabel,UiShellRuntimeGateway.Localization.Get("mission.gridlock.location"));
            Set(enemyIntelLabel,UiShellRuntimeGateway.Localization.Get("mission.gridlock.enemy_intel"));
            if(missionArtImage!=null) missionArtImage.texture=gridlockMissionArt;
            string[] names={"site_a","site_b","delivery"};
            for(int i=0;i<(objectiveLabels?.Length??0);i++) Set(objectiveLabels[i],UiShellRuntimeGateway.Localization.Get(i<3?"mission.gridlock.objective."+names[i]:"mission.gridlock.star.3"));
            SetAt(conditionLabels,0,UiShellRuntimeGateway.Localization.Get("mission.gridlock.resources"));
            SetAt(conditionLabels,1,UiShellRuntimeGateway.Localization.Get("mission.gridlock.forces"));
            SetAt(conditionLabels,2,UiShellRuntimeGateway.Localization.Get("mission.gridlock.deadline"));
            SetAt(conditionNameLabels,0,UiShellRuntimeGateway.Localization.Get("mission.m02.resources.label"));
            SetAt(conditionNameLabels,1,UiShellRuntimeGateway.Localization.Get("mission.m03.label.forces"));
            SetAt(conditionNameLabels,2,UiShellRuntimeGateway.Localization.Get("mission.gridlock.time_limit"));
        }
    }
}
