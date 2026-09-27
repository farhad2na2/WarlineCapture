using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture evidenceChainMissionArt;
        [SerializeField] private RectTransform evidenceChainRoutePanel;
        [SerializeField] private Button evidenceChainAirRouteButton;
        [SerializeField] private Button evidenceChainArmoredRouteButton;
        [SerializeField] private TMP_Text evidenceChainRouteTitle;
        [SerializeField] private TMP_Text evidenceChainAirTitle;
        [SerializeField] private TMP_Text evidenceChainAirBody;
        [SerializeField] private TMP_Text evidenceChainArmoredTitle;
        [SerializeField] private TMP_Text evidenceChainArmoredBody;
        [SerializeField] private TMP_Text evidenceChainCustody;

        public Button EvidenceChainAirRouteButton => evidenceChainAirRouteButton;
        public Button EvidenceChainArmoredRouteButton => evidenceChainArmoredRouteButton;

        public void SelectEvidenceChainRoute(EvidenceChainExtractionRoute route)
        {
            EvidenceChainRouteChoice.Selected = route;
            RefreshEvidenceChainRouteChoice();
        }

        private void RefreshEvidenceChainRouteChoice()
        {
            bool air = EvidenceChainRouteChoice.Selected == EvidenceChainExtractionRoute.Air;
            SetRouteCard(evidenceChainAirRouteButton, air);
            SetRouteCard(evidenceChainArmoredRouteButton, !air);
        }

        private static void SetRouteCard(Button button, bool selected)
        {
            if (button == null) return;
            var panel = button.GetComponent<V3GradientGraphic>();
            if (panel != null)
                panel.Configure(selected ? new Color32(8, 60, 83, 255) : new Color32(29, 34, 33, 255),
                    new Color32(3, 12, 17, 255),
                    selected ? new Color32(0, 202, 237, 255) : new Color32(196, 151, 49, 255), 3f);
        }

        private void ApplyEvidenceChain(in UiMissionBriefingModel model)
        {
            bool persian = GameLocalization.CurrentLocaleCode == "fa-IR";
            Set(screenSubtitle, persian ? "فصل سوم · شبکهٔ پنهان" : "CHAPTER III - HIDDEN NETWORK");
            Set(missionNumber, "M04");
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
            Set(evidenceChainRouteTitle, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.route.title"));
            Set(evidenceChainAirTitle, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.route.air.title"));
            Set(evidenceChainAirBody, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.route.air.body"));
            Set(evidenceChainArmoredTitle, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.route.armored.title"));
            Set(evidenceChainArmoredBody, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.route.armored.body"));
            Set(evidenceChainCustody, UiShellRuntimeGateway.Localization.Get("mission.evidence_chain.route.custody"));
            RefreshEvidenceChainRouteChoice();
        }
    }
}
