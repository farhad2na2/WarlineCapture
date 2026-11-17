using Game.Missions.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private MissionAtlasScreenView approvedAtlas;
        [SerializeField] private UnityEngine.Events.UnityEvent approvedArchiveRequested = new UnityEngine.Events.UnityEvent();
        public UnityEngine.Events.UnityEvent ApprovedArchiveRequested => approvedArchiveRequested;
        [SerializeField] private Sprite[] missionComicPlates;
        [SerializeField] private Sprite[] chapterComicPlates;
        private UiCampaignOperationsModel atlasModel;
        private bool atlasMissions, atlasBound;
        private static bool rememberedAtlasMissions;
        private int atlasChapter;
        private static readonly string[] AtlasChapterKeys={"first_response","broken_grid","hidden_network","air_and_armor","citywide_command"};
        private static readonly string[] AtlasChapterFallback={"FIRST RESPONSE","BROKEN GRID","HIDDEN NETWORK","AIR AND ARMOR","CITYWIDE COMMAND"};
        private static readonly string[] AtlasRoman={"I","II","III","IV","V"};
        private static readonly string[] AtlasMissionFallback={"FIRST CONTACT","ESTABLISH THE BASE","RADAR WARNING","AIRLIFT","BREACH ASSAULT","GRIDLOCK","SUPPLY LINE","MARKET LIFELINE","POWER RELAY","ROUTE REOPENED","SIGNAL TRACE","SAFEHOUSE SWEEP","FALSE FRONT","EVIDENCE CHAIN","NETWORK BREAK","AIR CORRIDOR","STEEL PUSH","SPLIT FRONT","GROUNDED SIGNAL","ARMOR BREAK","CITYWIDE ALERT","TRUST UNDER FIRE","NETWORK COLLAPSE","LAST CORRIDOR","COMMAND NODE"};
        public MissionAtlasScreenView ApprovedAtlas => approvedAtlas;
        private static string AtlasSource(TMPro.TMP_Text text)=>text==null?string.Empty:text.GetComponent<V3LocalizedTextBindingView>()?.SourceValue??text.text;
        private Button AtlasChapterButton(int index)=>index switch {0=>ChapterOneButton,1=>ChapterTwoButton,2=>ChapterThreeButton,3=>ChapterFourButton,4=>ChapterFiveButton,_=>null};
        private void PresentApprovedAtlas(UiCampaignOperationsModel model)
        {
            if(approvedAtlas==null)return;atlasModel=model;
            int chapter=IsChapterFive?4:IsChapterFour?3:IsChapterThree?2:IsChapterTwo?1:0;
            atlasChapter=chapter;
            if(!atlasBound)
            {
                atlasBound=true;atlasMissions=rememberedAtlasMissions;
                approvedAtlas.Back=()=> {if(atlasMissions){atlasMissions=false;RenderApprovedAtlas();}else backRouteButton?.SubmitRouteRequest();};
                approvedAtlas.Secondary=()=> {if(atlasMissions){atlasMissions=false;RenderApprovedAtlas();}else approvedArchiveRequested.Invoke();};
                approvedAtlas.Primary=()=> {if(atlasMissions){if(launchMissionButton.interactable)launchMissionButton.onClick.Invoke();}else EnterAtlasChapter(atlasChapter);};
                approvedAtlas.Navigate=EnterAtlasChapter;approvedAtlas.ChooseZone=EnterAtlasChapter;
                approvedAtlas.ChooseMission=i=> {if(i<5 && missionNodeButtons[i].interactable)missionNodeButtons[i].onClick.Invoke();};
            }
            RenderApprovedAtlas();
        }
        private void EnterAtlasChapter(int index)
        {
            var button=AtlasChapterButton(index);if(button==null||!button.interactable)return;
            atlasMissions=true;button.onClick.Invoke();RenderApprovedAtlas();
        }
        private void RenderApprovedAtlas()
        {
            if(approvedAtlas==null)return;
            rememberedAtlasMissions=atlasMissions;
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
            string Chapter(int c)
            {
                string title=UiShellRuntimeGateway.Localization.Get("chapter."+AtlasChapterKeys[c]+".name",AtlasChapterFallback[c]);
                int bar=title.IndexOf(" · ",System.StringComparison.Ordinal);if(bar>=0)title=title.Substring(bar+3);
                return fa?title:title.ToUpperInvariant();
            }
            string ChapterNumber(int c)=>(fa?"فصل ":"CHAPTER ")+AtlasRoman[c];
            string Locked()=>AtlasWords.Get("locked","LOCKED","قفل");
            var navigation=new MissionAtlasItem[5];int chapterCompleted=0;
            for(int i=0;i<5;i++)
            {
                bool available=AtlasChapterButton(i)?.interactable??false;
                int done=CountAtlasBits((atlasModel.CompletedMissionMask>>(i*5))&31u);
                bool unlocked=((atlasModel.AvailableMissionMask>>(i*5))&31u)!=0;
                bool released=((atlasModel.ReadyMissionMask>>(i*5))&31u)!=0;
                if(i==atlasChapter)chapterCompleted=done;
                navigation[i]=new MissionAtlasItem
                {
                    Id="ChapterCard_"+(i+1),Title=Chapter(i),Subtitle=ChapterNumber(i),
                    Comic=chapterComicPlates!=null&&i<chapterComicPlates.Length?chapterComicPlates[i]:null,
                    Enabled=available,Selected=i==atlasChapter,Medals=-1,Completed=done==5,Locked=!unlocked,
                    Status=unlocked?AtlasWords.Complete(done,5):released?Locked():AtlasWords.Get("coming_soon","COMING SOON","به‌زودی")
                };
            }
            var cards=new MissionAtlasItem[5];var progressRows=new MissionAtlasSummaryRow[5];int selected=0;
            for(int i=0;i<5;i++)
            {
                int n=atlasChapter*5+i;string id=CampaignMissionSequence.IdAt(n);
                bool chosen=id==atlasModel.SelectedMission.MissionId;if(chosen)selected=i;
                bool complete=(atlasModel.CompletedMissionMask&(1u<<n))!=0,available=missionNodeButtons[i].interactable;
                bool released=(atlasModel.ReadyMissionMask&(1u<<n))!=0;
                string name=UiShellRuntimeGateway.Localization.Get("chapter."+AtlasChapterKeys[atlasChapter]+".mission."+(i+1),AtlasMissionFallback[n]);
                string status=complete?AtlasWords.Get("completed","COMPLETED","تکمیل شده"):available?AtlasWords.Get("available","AVAILABLE","در دسترس"):released?Locked():AtlasWords.Get("coming_soon","COMING SOON","به‌زودی");
                cards[i]=new MissionAtlasItem
                {
                    Id="CampaignMission_"+id,Title=fa?name:name.ToUpperInvariant(),
                    Subtitle=(i+1).ToString("00"),Medals=-1,
                    Comic=missionComicPlates!=null&&n<missionComicPlates.Length?missionComicPlates[n]:null,
                    Enabled=available,Selected=chosen,Status=status,Completed=complete,Locked=!available
                };
                progressRows[i]=new MissionAtlasSummaryRow {Title=(i+1).ToString("00")+" · "+cards[i].Title,Value=status,Completed=complete,Locked=!available};
            }
            Texture map=districtAtlasScenes!=null&&atlasChapter<districtAtlasScenes.Length?districtAtlasScenes[atlasChapter]:null;
            approvedAtlas.Present(new MissionAtlasPresentation
            {
                Missions=atlasMissions,Skirmish=false,
                Heading=atlasMissions?(fa?"کارزار / ":"CAMPAIGN / ")+Chapter(atlasChapter):AtlasWords.Get("choose_chapter","CAMPAIGN / CHOOSE CHAPTER","کارزار / انتخاب فصل"),
                Navigation=navigation,MissionsOnMap=cards,
                RegionTitles=new[]{ChapterNumber(atlasChapter)+" · "+Chapter(atlasChapter)},
                RegionSummaries=new[]{AtlasWords.Complete(chapterCompleted,5)},SelectedRegion=0,
                Overview=districtAtlasScenes!=null&&districtAtlasScenes.Length>0?districtAtlasScenes[0]:map,Map=map,
                DetailTitle=atlasMissions?cards[selected].Title:Chapter(atlasChapter),
                DetailSubtitle=atlasMissions?(fa?"ماموریت ":"MISSION ")+cards[selected].Subtitle+" · "+ChapterNumber(atlasChapter):ChapterNumber(atlasChapter)+" · "+chapterCompleted+"/5",
                PreviewComic=atlasMissions?cards[selected].Comic:navigation[atlasChapter].Comic,
                Purpose=atlasMissions?AtlasWords.Brief(AtlasSource(missionBriefingText)):AtlasWords.Get("choose_missions_chapter","Choose missions to continue.","ماموریت‌ها را انتخاب کنید."),
                Objectives=atlasMissions?AtlasSource(primaryObjectiveText):"",
                SummaryRows=atlasMissions?System.Array.Empty<MissionAtlasSummaryRow>():progressRows,
                SummaryHeading=AtlasWords.Get("chapter_progress","CHAPTER PROGRESS","پیشرفت فصل"),
                Primary=atlasMissions?AtlasSource(launchMissionLabel):AtlasWords.Get("choose_missions","CHOOSE MISSIONS","انتخاب ماموریت"),
                Secondary=atlasMissions?AtlasWords.Get("chapters","CHAPTERS","فصل‌ها"):AtlasWords.Get("story_archive","STORY ARCHIVE","بایگانی داستان"),
                CanSecondary=atlasMissions||atlasModel.AllRequiredMissionsCompleted,
                CanLaunch=atlasMissions?launchMissionButton.interactable:(AtlasChapterButton(atlasChapter)?.interactable??false)
            });
        }
        private static int CountAtlasBits(uint value)
        {int count=0;while(value!=0){count+=(int)(value&1);value>>=1;}return count;}
    }
}
