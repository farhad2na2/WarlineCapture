using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Texture falseFrontMissionPreview;

        private void ApplyFalseFrontCard()
        {
            Set(missionNumber, "CH03 · M03");
            Set(missionName, UiShellRuntimeGateway.Localization.Get("mission.false_front.name"));
            Set(missionBriefingText, UiShellRuntimeGateway.Localization.Get("mission.false_front.summary"));
            Set(primaryObjectiveText, UiShellRuntimeGateway.Localization.Get("mission.false_front.objective.verify"));
            Set(rewardSummaryText, UiShellRuntimeGateway.Localization.Get("mission.false_front.reward.card"));
            if (missionPreviewImage != null) missionPreviewImage.texture = falseFrontMissionPreview;
        }
    }
}
