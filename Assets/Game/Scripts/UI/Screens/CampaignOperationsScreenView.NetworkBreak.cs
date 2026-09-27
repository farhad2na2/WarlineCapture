using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Texture networkBreakMissionPreview;

        private void ApplyNetworkBreakCard()
        {
            Set(missionNumber, "CH03 · M05");
            Set(missionName, UiShellRuntimeGateway.Localization.Get("mission.network_break.name"));
            Set(missionBriefingText, UiShellRuntimeGateway.Localization.Get("mission.network_break.summary"));
            Set(primaryObjectiveText, UiShellRuntimeGateway.Localization.Get("mission.network_break.objective.breach"));
            Set(rewardSummaryText, UiShellRuntimeGateway.Localization.Get("mission.network_break.reward.card"));
            if (missionPreviewImage != null) missionPreviewImage.texture = networkBreakMissionPreview;
        }
    }
}
