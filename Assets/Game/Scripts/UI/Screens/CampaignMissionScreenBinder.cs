using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CampaignOperationsScreenView))]
    public sealed class CampaignMissionScreenBinder : MonoBehaviour
    {
        [SerializeField] private CampaignOperationsScreenView campaignOperationsView;
        [SerializeField] private string missionId = "saga.ch01.m01.first_contact";
        private bool _bound;

        public void Configure(CampaignOperationsScreenView view, string selectedMissionId)
        {
            campaignOperationsView = view;
            missionId = selectedMissionId;
        }

        private void OnEnable()
        {
            if (campaignOperationsView == null)
                campaignOperationsView = GetComponent<CampaignOperationsScreenView>();
            if (campaignOperationsView == null || _bound) return;
            campaignOperationsView.LaunchMissionButton.onClick.AddListener(OpenBriefing);
            _bound = true;
            Refresh();
        }

        private void OnDisable()
        {
            if (!_bound || campaignOperationsView == null) return;
            campaignOperationsView.LaunchMissionButton.onClick.RemoveListener(OpenBriefing);
            _bound = false;
        }

        public void Refresh()
        {
            UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Refresh, missionId);
            if (UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel model))
                campaignOperationsView.Apply(model);
            else
                campaignOperationsView.ApplyUnavailable();
        }

        private void OpenBriefing()
        {
            if (!UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                    UiCampaignMissionActionKind.OpenBriefing, missionId))
                return;
            UiShellRuntimeGateway.TryEnqueueRouteRequest(
                UiShellRouteIntent.OpenMenuRoute, UIRoute.MissionBriefing, true);
        }
    }
}
