using Game.UI.Contracts;
using Game.Components;
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
        private int futureChapter;
        private int futureMissionNumber;
        private FutureMissionComicPreviewController futureComicPreview;
        private bool chapterThreeOpeningSeen;

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
            if (campaignOperationsView != null)
                futureComicPreview ??= GetComponent<FutureMissionComicPreviewController>() ??
                    gameObject.AddComponent<FutureMissionComicPreviewController>();
            if (_bound) return;
            if (campaignOperationsView != null)
            {
                campaignOperationsView.LaunchMissionButton.onClick.AddListener(OpenBriefing);
                BindMissionNode(0, SelectM01);
                BindMissionNode(1, SelectM02);
                BindMissionNode(2, SelectM03);
                BindMissionNode(3, SelectM04);
                BindMissionNode(4, SelectM05);
                campaignOperationsView.ChapterOneButton?.onClick.AddListener(SelectChapterOne);
                campaignOperationsView.ChapterTwoButton?.onClick.AddListener(SelectChapterTwo);
                campaignOperationsView.ChapterTwoOverviewButton?.onClick.AddListener(SelectChapterTwo);
                campaignOperationsView.ChapterThreeButton?.onClick.AddListener(SelectChapterThree);
                campaignOperationsView.ChapterFourButton?.onClick.AddListener(SelectChapterFour);
                campaignOperationsView.ChapterFiveButton?.onClick.AddListener(SelectChapterFive);
            }
            if (missionBriefingView != null)
            {
                missionBriefingView.DeployOperationButton.onClick.AddListener(Deploy);
                missionBriefingView.EvidenceChainAirRouteButton?.onClick.AddListener(SelectAirRoute);
                missionBriefingView.EvidenceChainArmoredRouteButton?.onClick.AddListener(SelectArmoredRoute);
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
                UnbindMissionNode(4, SelectM05);
                campaignOperationsView.ChapterOneButton?.onClick.RemoveListener(SelectChapterOne);
                campaignOperationsView.ChapterTwoButton?.onClick.RemoveListener(SelectChapterTwo);
                campaignOperationsView.ChapterTwoOverviewButton?.onClick.RemoveListener(SelectChapterTwo);
                campaignOperationsView.ChapterThreeButton?.onClick.RemoveListener(SelectChapterThree);
                campaignOperationsView.ChapterFourButton?.onClick.RemoveListener(SelectChapterFour);
                campaignOperationsView.ChapterFiveButton?.onClick.RemoveListener(SelectChapterFive);
            }
            if (missionBriefingView != null)
            {
                missionBriefingView.DeployOperationButton.onClick.RemoveListener(Deploy);
                missionBriefingView.EvidenceChainAirRouteButton?.onClick.RemoveListener(SelectAirRoute);
                missionBriefingView.EvidenceChainArmoredRouteButton?.onClick.RemoveListener(SelectArmoredRoute);
                if (missionBriefingView.ReplayTutorialToggle != null)
                    missionBriefingView.ReplayTutorialToggle.onValueChanged.RemoveListener(SetReplayTutorial);
            }
            _bound = false;
            projectionApplied = false;
            futureComicPreview?.Close();
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
                    campaignOperationsView.EnableFutureComicChapters(campaignOperationsView.IsChapterFour ? 4 : futureChapter);
                    if (futureChapter >= 4)
                        campaignOperationsView.ApplyFutureComicMission(futureChapter,
                            Mathf.Max(1, futureMissionNumber));
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
            if (futureChapter >= 4)
            {
                OpenFutureComic(futureChapter, futureMissionNumber);
                return;
            }
            if (!UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                    UiCampaignMissionActionKind.OpenBriefing, missionId))
                return;
            UiShellRuntimeGateway.TryEnqueueRouteRequest(
                UiShellRouteIntent.OpenMenuRoute, UIRoute.MissionBriefing, true);
        }

        private void SelectM01()
        {
            if (TrySelectFutureMission(1)) return;
            SelectMission(campaignOperationsView.IsChapterThree ? Game.Missions.Contracts.CampaignMissionSequence.SignalTrace : campaignOperationsView.IsChapterTwo ? Game.Missions.Contracts.CampaignMissionSequence.Gridlock : UiCampaignMissionProjectionIds.M01);
        }
        private void SelectChapterOne(){futureChapter=0;futureMissionNumber=0;SelectMission(UiCampaignMissionProjectionIds.M01);campaignOperationsView.ShowMissionSelect();}
        private void SelectChapterTwo(){futureChapter=0;futureMissionNumber=0;SelectMission(Game.Missions.Contracts.CampaignMissionSequence.Gridlock);campaignOperationsView.ShowMissionSelect();}
        private void SelectChapterThree()
        {
            futureChapter = 0;
            futureMissionNumber = 0;
            SelectMission(Game.Missions.Contracts.CampaignMissionSequence.SignalTrace);
            campaignOperationsView.ShowMissionSelect();
            if (chapterThreeOpeningSeen)
                return;
            chapterThreeOpeningSeen = futureComicPreview != null &&
                futureComicPreview.PlaySequence("seq.ch03.open.hidden_network");
        }
        private void SelectChapterFour(){SelectMission(Game.Missions.Contracts.CampaignMissionSequence.AirCorridor);campaignOperationsView.ShowMissionSelect();}
        private void SelectChapterFive() => SelectFutureChapter(5);
        private void SelectM02()
        {
            if (TrySelectFutureMission(2)) return;
            SelectMission(campaignOperationsView.IsChapterThree
            ? Game.Missions.Contracts.CampaignMissionSequence.SafehouseSweep
            : campaignOperationsView.IsChapterTwo ? Game.Missions.Contracts.CampaignMissionSequence.SupplyLine : UiCampaignMissionProjectionIds.M02);
        }
        private void SelectM03()
        {
            if (TrySelectFutureMission(3)) return;
            SelectMission(campaignOperationsView.IsChapterThree ? Game.Missions.Contracts.CampaignMissionSequence.FalseFront : campaignOperationsView.IsChapterTwo ? Game.Missions.Contracts.CampaignMissionSequence.MarketLifeline : UiCampaignMissionProjectionIds.M03);
        }
        private void SelectM05()
        {
            if (TrySelectFutureMission(5)) return;
            if (campaignOperationsView.IsChapterThree)
            {
                SelectMission(Game.Missions.Contracts.CampaignMissionSequence.NetworkBreak);
                return;
            }
            SelectMission(campaignOperationsView.IsChapterTwo
            ? Game.Missions.Contracts.CampaignMissionSequence.RouteReopened
            : "saga.ch01.m05.breach_assault");
        }
        private void SelectM04()
        {
            if (TrySelectFutureMission(4)) return;
            SelectMission(campaignOperationsView.IsChapterTwo
            ? Game.Missions.Contracts.CampaignMissionSequence.PowerRelay
            : campaignOperationsView.IsChapterThree ? Game.Missions.Contracts.CampaignMissionSequence.EvidenceChain : "saga.ch01.m04.airlift");
        }

        private void SelectFutureChapter(int chapter)
        {
            futureChapter = chapter;
            futureMissionNumber = 1;
            campaignOperationsView.ShowMissionSelect();
            campaignOperationsView.ApplyFutureComicMission(chapter, 1);
        }

        private bool TrySelectFutureMission(int number)
        {
            if ((futureChapter==4 || campaignOperationsView.IsChapterFour) && number==3)
            { SelectMission(Game.Missions.Contracts.CampaignMissionSequence.SplitFront); return true; }
            if ((futureChapter==4 || campaignOperationsView.IsChapterFour) && number==2)
            { SelectMission(Game.Missions.Contracts.CampaignMissionSequence.SteelPush); return true; }
            if ((futureChapter==4 || campaignOperationsView.IsChapterFour) && number==1)
            { SelectMission(Game.Missions.Contracts.CampaignMissionSequence.AirCorridor); return true; }
            if(campaignOperationsView.IsChapterFour)futureChapter=4;
            if (futureChapter < 4)
                return false;
            futureMissionNumber = number;
            campaignOperationsView.ApplyFutureComicMission(futureChapter, number);
            OpenFutureComic(futureChapter, number);
            return true;
        }

        private void OpenFutureComic(int chapter, int number)
        {
            FutureMissionComicMission comic = FutureMissionComicCatalog.Find(chapter, number);
            if (comic != null)
                futureComicPreview?.Play(comic);
        }

        private void SelectMission(string selectedMissionId)
        {
            futureChapter = 0;
            futureMissionNumber = 0;
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

        private void SelectAirRoute() => missionBriefingView.SelectEvidenceChainRoute(EvidenceChainExtractionRoute.Air);
        private void SelectArmoredRoute() => missionBriefingView.SelectEvidenceChainRoute(EvidenceChainExtractionRoute.Armored);
    }
}
