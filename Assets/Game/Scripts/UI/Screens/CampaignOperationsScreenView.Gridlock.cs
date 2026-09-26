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
            IsChapterTwo=model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.Gridlock || model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.SupplyLine || model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.MarketLifeline || model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.PowerRelay;
            bool unlocked=(model.AvailableMissionMask&(1<<5))!=0;
            if(chapterTwoButton!=null) chapterTwoButton.interactable=unlocked;
            if(chapterTwoOverviewButton!=null) chapterTwoOverviewButton.interactable=unlocked;
            if(chapterTwoLock!=null) chapterTwoLock.SetActive(!unlocked);
            GameObject railLock=ResolveChapterTwoRailLock();
            if(railLock!=null) railLock.SetActive(!unlocked);
            ApplyChapterTabAppearance(chapterOneButton,!IsChapterTwo,true);
            ApplyChapterTabAppearance(chapterTwoButton,IsChapterTwo,unlocked);
            Set(chapterTwoStatus,UiShellRuntimeGateway.Localization.Get(unlocked?"chapter.broken_grid.available":"chapter.broken_grid.locked"));
            Set(selectedChapterLabel,UiShellRuntimeGateway.Localization.Get(IsChapterTwo?"chapter.broken_grid.name":"chapter.first_response.name"));
            for(int i=0;i<(chapterMissionNames?.Length??0);i++)
            {
                string key=IsChapterTwo?"chapter.broken_grid.mission."+(i+1):"chapter.first_response.mission."+(i+1);
                Set(chapterMissionNames[i],UiShellRuntimeGateway.Localization.Get(key));
            }
            if(!IsChapterTwo) return;
            if(model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.PowerRelay){ApplyPowerRelayCard();return;}
            if(model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.MarketLifeline){ApplyMarketLifelineCard();return;}
            if(model.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.SupplyLine){ApplySupplyLineCard();return;}
            Set(missionNumber,"CH02 · M01");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.gridlock.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.gridlock.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.gridlock.objective.site_a"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.gridlock.reward.card"));
            if(missionPreviewImage!=null) missionPreviewImage.texture=gridlockMissionPreview;
        }

        private GameObject ResolveChapterTwoRailLock()
        {
            if(chapterTwoRailLock!=null)return chapterTwoRailLock;
            Transform icon=chapterTwoButton!=null?chapterTwoButton.transform.Find("Icon"):null;
            return icon!=null?icon.gameObject:null;
        }

        private static void ApplyChapterTabAppearance(Button button,bool selected,bool available)
        {
            if(button==null)return;
            V3GradientGraphic gradient=button.GetComponent<V3GradientGraphic>();
            if(gradient!=null)
                gradient.Configure(
                    selected?new Color32(153,101,3,255):new Color32(22,32,35,250),
                    selected?new Color32(58,35,2,255):new Color32(5,12,15,252),
                    selected?new Color32(243,174,0,255):new Color32(69,81,85,255),3f);
            Transform shade=button.transform.Find("ArtClip/Shade");
            Image shadeImage=shade!=null?shade.GetComponent<Image>():null;
            if(shadeImage!=null)shadeImage.color=new Color(0f,0f,0f,selected?0.30f:0.68f);
            Transform subtitle=button.transform.Find("Subtitle");
            TMP_Text subtitleText=subtitle!=null?subtitle.GetComponent<TMP_Text>():null;
            if(subtitleText!=null)subtitleText.color=selected?new Color32(243,174,0,255):available?new Color32(190,202,207,255):new Color32(140,158,164,255);
        }
    }
}
