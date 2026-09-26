using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Texture routeReopenedMissionPreview;
        private void ApplyRouteReopenedCard()
        {
            Set(missionNumber,"CH02 · M05");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.objective.lifelines"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.reward.card"));
            if(missionPreviewImage!=null)missionPreviewImage.texture=routeReopenedMissionPreview;
        }
    }
}
