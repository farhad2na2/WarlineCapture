using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Texture signalTraceMissionPreview;
        private void ApplySignalTraceCard()
        {
            Set(missionNumber,"CH03 · M01");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.objective.compare"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.signal_trace.reward.card"));
            if(missionPreviewImage!=null)missionPreviewImage.texture=signalTraceMissionPreview;
        }
    }
}
