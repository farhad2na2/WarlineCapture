using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Texture supplyLineMissionPreview;
        private void ApplySupplyLineCard()
        {
            Set(missionNumber,"CH02 · M02");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.supply_line.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.supply_line.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.supply_line.objective.reserve"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.supply_line.reward.card"));
            if(missionPreviewImage!=null)missionPreviewImage.texture=supplyLineMissionPreview;
        }
    }
}
