using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class LoadoutSquadPrepScreenView : MonoBehaviour
    {
        [SerializeField] private Button editLoadoutButton;
        [SerializeField] private Button deployButton;
        [SerializeField] private string fallbackMissionId = UiCampaignMissionProjectionIds.M02;
        private bool _bound;

        public Button EditLoadoutButton => editLoadoutButton;
        public Button DeployButton => deployButton;

        public void Configure(Button editButton, Button deployCommandButton)
        {
            editLoadoutButton = editButton;
            deployButton = deployCommandButton;
        }

        private void OnEnable()
        {
            RefreshBindings();
        }

        public void RefreshBindings()
        {
            if (_bound || deployButton == null)
                return;

            deployButton.onClick.AddListener(SubmitDeploy);
            _bound = true;
        }

        private void OnDisable()
        {
            if (!_bound)
                return;

            if (deployButton != null)
                deployButton.onClick.RemoveListener(SubmitDeploy);
            _bound = false;
        }

        private void SubmitDeploy()
        {
            string missionId = fallbackMissionId;
            if (UiShellRuntimeGateway.TryReadMissionBriefing(out UiMissionBriefingModel briefing) &&
                briefing.IsValid &&
                !string.IsNullOrWhiteSpace(briefing.MissionId))
            {
                missionId = briefing.MissionId;
            }

            if (!UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                    UiCampaignMissionActionKind.Deploy,
                    missionId))
            {
                Debug.LogError($"[LoadoutSquadPrep] Mission deploy request was rejected. mission={missionId}");
                return;
            }

            deployButton.interactable = false;
        }
    }
}
