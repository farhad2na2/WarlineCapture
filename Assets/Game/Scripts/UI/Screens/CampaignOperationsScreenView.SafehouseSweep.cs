using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Texture safehouseSweepMissionPreview;
        private void ApplySafehouseSweepCard()
        {
            Set(missionNumber,"CH03 · M02");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.objective.confirm"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.reward.card"));
            if(missionPreviewImage!=null)missionPreviewImage.texture=safehouseSweepMissionPreview;
        }
    }
}
