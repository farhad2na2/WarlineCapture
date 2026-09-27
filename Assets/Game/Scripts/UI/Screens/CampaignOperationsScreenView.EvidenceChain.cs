using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Texture evidenceChainMissionPreview;

        private void ApplyEvidenceChainCard()
        {
            Set(missionNumber, "CH03 · M04");
            Set(missionName, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.name"));
            Set(missionBriefingText, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.summary"));
            Set(primaryObjectiveText, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.objective.extract"));
            Set(rewardSummaryText, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.reward.card"));
            if (missionPreviewImage != null) missionPreviewImage.texture = evidenceChainMissionPreview;
        }
    }
}
