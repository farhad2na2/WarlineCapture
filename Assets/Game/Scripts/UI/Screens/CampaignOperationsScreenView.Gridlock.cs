using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private Button chapterOneButton,chapterTwoButton,chapterTwoOverviewButton;
        [SerializeField] private GameObject chapterTwoLock;
        [SerializeField] private TMP_Text selectedChapterLabel,chapterTwoStatus;
        [SerializeField] private Texture gridlockMissionPreview;
        [SerializeField] private TMP_Text[] chapterMissionNames;
        [SerializeField] private GameObject chapterTwoRailLock;
        public Button ChapterOneButton=>chapterOneButton;
        public Button ChapterTwoButton=>chapterTwoButton;
        public Button ChapterTwoOverviewButton=>chapterTwoOverviewButton;
        public bool IsChapterTwo {get;private set;}
        private void ApplyGridlockChapter(in UiCampaignOperationsModel model)
        {
            IsChapterTwo=model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.Gridlock || model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.SupplyLine;
            bool unlocked=(model.AvailableMissionMask&(1<<5))!=0;
            if(chapterTwoButton!=null) chapterTwoButton.interactable=unlocked;
            if(chapterTwoOverviewButton!=null) chapterTwoOverviewButton.interactable=unlocked;
            if(chapterTwoLock!=null) chapterTwoLock.SetActive(!unlocked);
            if(chapterTwoRailLock!=null) chapterTwoRailLock.SetActive(!unlocked);
            Set(chapterTwoStatus,UiShellRuntimeGateway.Localization.Get(unlocked?"chapter.broken_grid.available":"chapter.broken_grid.locked"));
            Set(selectedChapterLabel,UiShellRuntimeGateway.Localization.Get(IsChapterTwo?"chapter.broken_grid.name":"chapter.first_response.name"));
            for(int i=0;i<(chapterMissionNames?.Length??0);i++)
            {
                string key=IsChapterTwo?"chapter.broken_grid.mission."+(i+1):"chapter.first_response.mission."+(i+1);
                Set(chapterMissionNames[i],UiShellRuntimeGateway.Localization.Get(key));
            }
            if(!IsChapterTwo) return;
            if(model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.SupplyLine){ApplySupplyLineCard();return;}
            Set(missionNumber,"CH02 · M01");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.gridlock.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.gridlock.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.gridlock.objective.site_a"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.gridlock.reward.card"));
            if(missionPreviewImage!=null) missionPreviewImage.texture=gridlockMissionPreview;
        }
    }
}
