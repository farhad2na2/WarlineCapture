using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField]private Texture m05MissionPreview;
        private void ApplyBreach(UiCampaignMissionModel mission)
        {
            if(mission.MissionId!="saga.ch01.m05.breach_assault")return;
            radarGuideAvailable=mission.Available;Set(missionNumber,"M05");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.m05.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.m05.summary"));Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.m05.objective.gate"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.m05.reward.card"));if(missionPreviewImage!=null)missionPreviewImage.texture=m05MissionPreview;
        }
    }
}
