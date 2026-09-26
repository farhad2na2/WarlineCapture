using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Texture powerRelayMissionPreview;
        private void ApplyPowerRelayCard()
        {
            Set(missionNumber,"CH02 · M04");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.power_relay.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.power_relay.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.power_relay.objective.shelter"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.power_relay.reward.card"));
            if(missionPreviewImage!=null)missionPreviewImage.texture=powerRelayMissionPreview;
        }
    }
}
