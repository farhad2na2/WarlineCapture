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
                string title=UiShellRuntimeGateway.Localization.Get("chapter."+AtlasChapterKeys[c]+".name",AtlasChapterFallback[c]);int bar=title.IndexOf(" · ",System.StringComparison.Ordinal);if(bar>=0)title=title.Substring(bar+3);return fa?title:title.ToUpperInvariant();
            }
            var navigation=new MissionAtlasItem[5];
            for(int i=0;i<5;i++)
            {
                bool available=AtlasChapterButton(i)?.interactable??false;
                uint bits=31u<<(i*5);bool complete=(atlasModel.CompletedMissionMask&bits)==bits;
                navigation[i]=new MissionAtlasItem {Id="ChapterCard_"+(i+1),Title=(fa?"فصل ":"CHAPTER ")+AtlasRoman[i],Subtitle=Chapter(i),Comic=chapterComicPlates!=null&&i<chapterComicPlates.Length?chapterComicPlates[i]:null,Enabled=available,Selected=i==atlasChapter,Medals=-1,Status=complete?(fa?"تکمیل شده":"COMPLETED"):available?"":(fa?"قفل":"LOCKED")};
            }
            var cards=new MissionAtlasItem[5];int selected=0;
            for(int i=0;i<5;i++)
            {
                int n=atlasChapter*5+i; string id=CampaignMissionSequence.IdAt(n);
                bool chosen=id==atlasModel.SelectedMission.MissionId;if(chosen)selected=i;
                string key="chapter."+AtlasChapterKeys[atlasChapter]+".mission."+(i+1);
                cards[i]=new MissionAtlasItem {Id="CampaignMission_"+id,Title=(i+1).ToString("00"),Subtitle=UiShellRuntimeGateway.Localization.Get(key,AtlasMissionFallback[n]).ToUpperInvariant(),Medals=-1,Comic=missionComicPlates!=null&&n<missionComicPlates.Length?missionComicPlates[n]:null,Enabled=missionNodeButtons[i].interactable,Selected=chosen,Status=(atlasModel.CompletedMissionMask&(1u<<n))!=0?(fa?"تکمیل شده":"COMPLETED"):missionNodeButtons[i].interactable?"":(fa?"قفل":"LOCKED")};
            }
            Texture map=districtAtlasScenes!=null&&atlasChapter<districtAtlasScenes.Length?districtAtlasScenes[atlasChapter]:null;
            approvedAtlas.Present(new MissionAtlasPresentation {Missions=atlasMissions,Skirmish=false,Heading=atlasMissions?(fa?"کارزار / ":"CAMPAIGN / ")+Chapter(atlasChapter):(fa?"کارزار":"CAMPAIGN"),Navigation=navigation,MissionsOnMap=cards,RegionTitles=new[]{(fa?"فصل ":"CHAPTER ")+AtlasRoman[atlasChapter]+(fa?" · ۵ ماموریت":" · 5 MISSIONS")},SelectedRegion=0,Overview=districtAtlasScenes!=null&&districtAtlasScenes.Length>0?districtAtlasScenes[0]:map,Map=map,DetailTitle=atlasMissions?(selected+1).ToString("00")+" · "+cards[selected].Subtitle:Chapter(atlasChapter),PreviewComic=atlasMissions?cards[selected].Comic:navigation[atlasChapter].Comic,Purpose=atlasMissions?AtlasSource(missionBriefingText):(fa?"یک فصل را برای مشاهده ماموریت‌ها انتخاب کنید.":"Select a chapter to view its missions."),Objectives=atlasMissions?AtlasSource(primaryObjectiveText):"",Primary=atlasMissions?AtlasSource(launchMissionLabel):(fa?"انتخاب ماموریت":"CHOOSE MISSIONS"),Secondary=atlasMissions?(fa?"فصل‌ها":"CHAPTERS"):(fa?"بایگانی داستان":"STORY ARCHIVE"),CanSecondary=atlasMissions || atlasModel.AllRequiredMissionsCompleted,CanLaunch=atlasMissions?launchMissionButton.interactable:(AtlasChapterButton(atlasChapter)?.interactable??false)});
        }
    }
}
