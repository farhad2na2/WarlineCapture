using System.Collections;
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
        private Coroutine _selectionRefresh;

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
            {
                campaignOperationsView.LaunchMissionButton.onClick.AddListener(OpenBriefing);
                BindMissionNode(0, SelectM01);
                BindMissionNode(1, SelectM02);
            }
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
            {
                campaignOperationsView.LaunchMissionButton.onClick.RemoveListener(OpenBriefing);
                UnbindMissionNode(0, SelectM01);
                UnbindMissionNode(1, SelectM02);
            }
            if (missionBriefingView != null)
            {
                missionBriefingView.DeployOperationButton.onClick.RemoveListener(Deploy);
                if (missionBriefingView.ReplayTutorialToggle != null)
                    missionBriefingView.ReplayTutorialToggle.onValueChanged.RemoveListener(SetReplayTutorial);
            }
            _bound = false;
            if (_selectionRefresh != null)
            {
                StopCoroutine(_selectionRefresh);
                _selectionRefresh = null;
            }
        }

        public void Refresh()
        {
            UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Refresh, missionId);
            if (campaignOperationsView != null)
            {
                if (UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel campaign))
                {
                    missionId = campaign.SelectedMission.MissionId;
                    campaignOperationsView.Apply(campaign);
                }
                else
                    campaignOperationsView.ApplyUnavailable();
            }
            if (missionBriefingView != null)
            {
                if (UiShellRuntimeGateway.TryReadMissionBriefing(out UiMissionBriefingModel briefing))
                {
                    missionId = briefing.MissionId;
                    missionBriefingView.Apply(in briefing);
                }
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

        private void SelectM01() => SelectMission(UiCampaignMissionProjectionIds.M01);
        private void SelectM02() => SelectMission(UiCampaignMissionProjectionIds.M02);

        private void SelectMission(string selectedMissionId)
        {
            if (!UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                    UiCampaignMissionActionKind.Select, selectedMissionId))
                return;
            missionId = selectedMissionId;
            if (_selectionRefresh != null)
                StopCoroutine(_selectionRefresh);
            _selectionRefresh = StartCoroutine(RefreshAfterProjection());
        }

        private IEnumerator RefreshAfterProjection()
        {
            yield return null;
            yield return null;
            _selectionRefresh = null;
            Refresh();
        }

        private void BindMissionNode(int index, UnityEngine.Events.UnityAction action)
        {
            if (campaignOperationsView.MissionNodeButtons != null &&
                index < campaignOperationsView.MissionNodeButtons.Length &&
                campaignOperationsView.MissionNodeButtons[index] != null)
                campaignOperationsView.MissionNodeButtons[index].onClick.AddListener(action);
        }

        private void UnbindMissionNode(int index, UnityEngine.Events.UnityAction action)
        {
            if (campaignOperationsView.MissionNodeButtons != null &&
                index < campaignOperationsView.MissionNodeButtons.Length &&
                campaignOperationsView.MissionNodeButtons[index] != null)
                campaignOperationsView.MissionNodeButtons[index].onClick.RemoveListener(action);
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
