using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CampaignMissionScreenBinder : MonoBehaviour
    {
        [SerializeField] private CampaignOperationsScreenView campaignOperationsView;
        [SerializeField] private MissionBriefingScreenView missionBriefingView;
        [SerializeField] private string missionId = "saga.ch01.m01.first_contact";
        private bool _bound;

        public void Configure(CampaignOperationsScreenView view, string selectedMissionId)
        {
            campaignOperationsView = view;
            missionBriefingView = null;
            missionId = selectedMissionId;
        }

        public void Configure(MissionBriefingScreenView view, string selectedMissionId)
        {
            campaignOperationsView = null;
            missionBriefingView = view;
            missionId = selectedMissionId;
        }

        private void OnEnable()
        {
            campaignOperationsView ??= GetComponent<CampaignOperationsScreenView>();
            missionBriefingView ??= GetComponent<MissionBriefingScreenView>();
            if (_bound) return;
            if (campaignOperationsView != null)
                campaignOperationsView.LaunchMissionButton.onClick.AddListener(OpenBriefing);
            if (missionBriefingView != null)
            {
                missionBriefingView.DeployOperationButton.onClick.AddListener(Deploy);
                if (missionBriefingView.ReplayTutorialToggle != null)
                    missionBriefingView.ReplayTutorialToggle.onValueChanged.AddListener(SetReplayTutorial);
            }
            _bound = campaignOperationsView != null || missionBriefingView != null;
            Refresh();
        }

        private void OnDisable()
        {
            if (!_bound) return;
            if (campaignOperationsView != null)
                campaignOperationsView.LaunchMissionButton.onClick.RemoveListener(OpenBriefing);
            if (missionBriefingView != null)
            {
                missionBriefingView.DeployOperationButton.onClick.RemoveListener(Deploy);
                if (missionBriefingView.ReplayTutorialToggle != null)
                    missionBriefingView.ReplayTutorialToggle.onValueChanged.RemoveListener(SetReplayTutorial);
            }
            _bound = false;
        }

        public void Refresh()
        {
            UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Refresh, missionId);
            if (campaignOperationsView != null)
            {
                if (UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel campaign))
                    campaignOperationsView.Apply(campaign);
                else
                    campaignOperationsView.ApplyUnavailable();
            }
            if (missionBriefingView != null)
            {
                if (UiShellRuntimeGateway.TryReadMissionBriefing(out UiMissionBriefingModel briefing))
                    missionBriefingView.Apply(in briefing);
                else
                    missionBriefingView.ApplyUnavailable();
            }
        }

        private void OpenBriefing()
        {
            if (!UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                    UiCampaignMissionActionKind.OpenBriefing, missionId))
                return;
            UiShellRuntimeGateway.TryEnqueueRouteRequest(
                UiShellRouteIntent.OpenMenuRoute, UIRoute.MissionBriefing, true);
        }

        private void SetReplayTutorial(bool enabled)
        {
            UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                UiCampaignMissionActionKind.SetReplayTutorial, missionId, enabled);
        }

        private void Deploy()
        {
            if (!UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                    UiCampaignMissionActionKind.Deploy, missionId))
                return;
            missionBriefingView.DeployOperationButton.interactable = false;
            if (missionBriefingView.ReplayTutorialToggle != null)
                missionBriefingView.ReplayTutorialToggle.interactable = false;
        }
    }
}
