using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField]private Texture m05MissionArt;
        private void ApplyBreach(in UiMissionBriefingModel model)
        {
            Set(missionNumber,"M05");Set(operationCodename,UiShellRuntimeGateway.Localization.Get("mission.m05.name"));Set(missionTitle,UiShellRuntimeGateway.Localization.Get("mission.m05.name"));
            Set(missionSummary,UiShellRuntimeGateway.Localization.Get("mission.m05.summary"));Set(locationLabel,UiShellRuntimeGateway.Localization.Get("mission.m05.location"));Set(enemyIntelLabel,UiShellRuntimeGateway.Localization.Get("mission.m05.enemy_intel"));
            if(missionArtImage!=null)missionArtImage.texture=m05MissionArt;
            string[] objectives={"gate","core","archive"};for(int i=0;i<(objectiveLabels?.Length??0);i++)Set(objectiveLabels[i],i<3?UiShellRuntimeGateway.Localization.Get("mission.m05.objective."+objectives[i]):UiShellRuntimeGateway.Localization.Get("mission.m05.star.3"));
            SetAt(conditionLabels,0,UiShellRuntimeGateway.Localization.Get("mission.m05.resources"));SetAt(conditionLabels,1,UiShellRuntimeGateway.Localization.Get("mission.m05.access"));SetAt(conditionLabels,2,UiShellRuntimeGateway.Localization.Get("mission.m05.options"));
            SetAt(conditionNameLabels,0,UiShellRuntimeGateway.Localization.Get("mission.m02.resources.label"));SetAt(conditionNameLabels,1,UiShellRuntimeGateway.Localization.Get("mission.m03.label.forces"));SetAt(conditionNameLabels,2,UiShellRuntimeGateway.Localization.Get("mission.m03.guide"));
            SetAt(rewardLabels,2,UiShellRuntimeGateway.Localization.Get("mission.m05.reward.ghillie")+" + "+UiShellRuntimeGateway.Localization.Get("mission.m05.reward.armor"));SetAt(rewardValues,2,UiShellRuntimeGateway.Localization.Get("mission.reward.unlock"));
            for(int i=3;i<(rewardRows?.Length??0);i++)if(rewardRows[i]!=null)rewardRows[i].gameObject.SetActive(false);
        }
    }
}
