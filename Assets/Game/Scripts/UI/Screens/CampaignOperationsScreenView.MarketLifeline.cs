using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Texture marketLifelineMissionPreview;
        private void ApplyMarketLifelineCard()
        {
            Set(missionNumber,"CH02 · M03");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.objective.market"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.reward.card"));
            if(missionPreviewImage!=null)missionPreviewImage.texture=marketLifelineMissionPreview;
        }
    }
}
