using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture evidenceChainMissionArt;

        private void ApplyEvidenceChain(in UiMissionBriefingModel model)
        {
            Set(missionNumber, "CH03 · M04");
            Set(operationCodename, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.name"));
            Set(missionTitle, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.name"));
            Set(missionSummary, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.summary"));
            Set(locationLabel, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.location"));
            Set(enemyIntelLabel, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.enemy_intel"));
            if (missionArtImage != null) missionArtImage.texture = evidenceChainMissionArt;
            string[] names = {"extract", "carrier", "landing"};
            for (int i = 0; i < (objectiveLabels?.Length ?? 0); i++)
                Set(objectiveLabels[i], UiShellRuntimeGateway.Localization.Get(i < 3
                    ? "mission.evidence_chain.objective." + names[i] : "mission.evidence_chain.star.3"));
            SetAt(conditionLabels, 0, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.resources"));
            SetAt(conditionLabels, 1, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.forces"));
            SetAt(conditionLabels, 2, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.deadline"));
        }
    }
}
