using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CampaignMissionScreenBinderView : MonoBehaviour
    {
        [SerializeField] private CampaignOperationsScreenView campaignOperationsView;
        [SerializeField] private MissionBriefingScreenView missionBriefingView;
        [SerializeField] private string missionId = "saga.ch01.m01.first_contact";
        private bool _bound;
        private uint appliedVersion;
        private string appliedLocale;
        private bool projectionApplied;

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
                BindMissionNode(2, SelectM03);
                BindMissionNode(3, SelectM04);
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
                UnbindMissionNode(2, SelectM03);
                UnbindMissionNode(3, SelectM04);
            }
            if (missionBriefingView != null)
            {
                missionBriefingView.DeployOperationButton.onClick.RemoveListener(Deploy);
                if (missionBriefingView.ReplayTutorialToggle != null)
                    missionBriefingView.ReplayTutorialToggle.onValueChanged.RemoveListener(SetReplayTutorial);
            }
            _bound = false;
            projectionApplied = false;
        }

        public void Refresh()
        {
            UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Refresh, missionId);
            projectionApplied = false;
            RefreshProjection();
        }

        public void RefreshProjection()
        {
            string locale = UiShellRuntimeGateway.Localization.CurrentLocaleCode;
            if (campaignOperationsView != null)
            {
                if (UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel campaign))
                {
                    if(projectionApplied && appliedVersion == campaign.Version && appliedLocale == locale) return;
                    projectionApplied = true; appliedVersion = campaign.Version; appliedLocale = locale;
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
                    if(projectionApplied && appliedVersion == briefing.Version && appliedLocale == locale) return;
                    projectionApplied = true; appliedVersion = briefing.Version; appliedLocale = locale;
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
        private void SelectM03() => SelectMission(UiCampaignMissionProjectionIds.M03);
        private void SelectM04() => SelectMission("saga.ch01.m04.airlift");

        private void SelectMission(string selectedMissionId)
        {
            if (!UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                    UiCampaignMissionActionKind.Select, selectedMissionId))
                return;
            missionId = selectedMissionId;
            projectionApplied = false;
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
