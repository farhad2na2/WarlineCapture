using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField]private Texture m04MissionPreview;
        private void ApplyAirlift(UiCampaignMissionModel mission)
        {
            if(mission.MissionId!="saga.ch01.m04.airlift")return;
            radarGuideAvailable=mission.Available;Set(missionNumber,"M04");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.m04.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.m04.summary"));Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.m04.objective.extract"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.m04.reward.card"));if(missionPreviewImage!=null)missionPreviewImage.texture=m04MissionPreview;
        }
    }
}
