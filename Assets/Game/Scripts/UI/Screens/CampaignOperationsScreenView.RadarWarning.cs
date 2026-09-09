using Game.Configs;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Button footerStoryArchiveButton;
        [SerializeField] private Texture m03MissionPreview;
        private bool radarGuideAvailable;
        private void OpenRadarGuideArchive()
        {if(radarGuideAvailable) UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide);}
        private void ApplyRadarWarning(UiCampaignMissionModel mission)
        {
            radarGuideAvailable=mission.MissionId==UiCampaignMissionProjectionIds.M03 && mission.Available;
            if (mission.MissionId != UiCampaignMissionProjectionIds.M03) return;
            Set(missionNumber,"M03");
            Set(missionName,GameLocalization.Get("mission.m03.name","RADAR WARNING"));
            Set(missionBriefingText,GameLocalization.Get("mission.m03.summary","Read the warning, prepare your defense, and stop the western convoy."));
            Set(primaryObjectiveText,GameLocalization.Get("mission.m03.objective.stop_convoy","STOP THE CONVOY"));
            Set(rewardSummaryText,GameLocalization.Get("mission.m03.reward.card","400 XP | 2,000 CREDITS | GUARD TOWER | RADAR PING"));
            if (missionPreviewImage != null) missionPreviewImage.texture=m03MissionPreview;
        }
    }
}
