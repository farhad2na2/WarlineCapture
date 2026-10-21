using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [Serializable]
    public struct MissionAtlasSummaryRow
    {
        public string Title, Value;
        public bool Completed, Locked;
    }

    public sealed partial class MissionAtlasScreenView
    {
        [SerializeField] private TMP_Text detailSubtitle, objectivesHeading, copyHint;
        [SerializeField] private RectTransform stickyHeading;
        [SerializeField] private TMP_Text stickyTitle, stickySummary, medalLegend;
        [SerializeField] private Button nextRegion;
        [SerializeField] private Image nextRegionArrow;
        [SerializeField] private TMP_Text nextRegionLabel, armySizeHeading, difficultyHeading;
        [SerializeField] private RectTransform summaryRoot;
        [SerializeField] private TMP_Text summaryHeading;
        [SerializeField] private TMP_Text[] summaryTitles, summaryValues;
        [SerializeField] private MissionAtlasStatusGraphic[] summaryChecks, difficultyChecks;
        [SerializeField] private Image[] difficultyMedals;
        [SerializeField] private RectTransform medalLegendPanel;
        [SerializeField] private MissionAtlasStatusGraphic[] legendMedals;

        public void RefreshLayout()=>Layout(true);
        public Button[] DifficultyButtons=>difficulties;
        public Button NextRegionButton=>nextRegion;

        private void AuthorCardPolish(Card card,Transform parent)
        {
            card.Completion=Rect("Completed",parent).gameObject.AddComponent<MissionAtlasStatusGraphic>();
            card.Completion.Configure(MissionAtlasStatusGraphic.Kind.Check,true);card.Completion.gameObject.SetActive(false);
            card.Lock=Icon(parent,"Locked",Resources.Load<Sprite>(SkirmishBattleLibraryArt.ResourceRoot+"icon_lock"));card.Lock.gameObject.SetActive(false);
            card.MedalEngravings=new MissionAtlasStatusGraphic[4];
            // Engraving overlays retain the actual ledger-backed medal Image beneath them.
            // The four slots remain the same four difficulty tiers used by result/return proof.
        }
        private void AuthorPolish()
        {
            detailSubtitle=Label(detail,"SelectedMissionContext","",24);detailSubtitle.color=Muted;
            objectivesHeading=Label(detailCopy,"ObjectivesHeading","OBJECTIVES",25);objectivesHeading.color=Muted;
            copyHint=Label(detail,"MoreDetailsHint","",21);copyHint.color=Muted;copyHint.alignment=TextAlignmentOptions.BottomRight;
            stickyHeading=Panel(missionRoot,"PinnedRegionHeading",0,0,600,100).rectTransform;
            stickyTitle=Label(stickyHeading,"Title","",30);stickySummary=Label(stickyHeading,"Progress","",23);stickySummary.color=Muted;
            medalLegendPanel=Panel(missionRoot,"MedalLegend",0,0,240,90).rectTransform;
            legendMedals=new MissionAtlasStatusGraphic[4];for(int i=0;i<4;i++){legendMedals[i]=Rect("Medal_"+i,medalLegendPanel).gameObject.AddComponent<MissionAtlasStatusGraphic>();legendMedals[i].Configure(MissionAtlasStatusGraphic.Kind.Medal,true,i);}
            medalLegend=Label(medalLegendPanel,"DifficultyMedalLegend","",21);medalLegend.color=Muted;medalLegend.alignment=TextAlignmentOptions.TopRight;
            nextRegion=Command(missionRoot,"NextRegionButton","",null,0);nextRegionLabel=nextRegion.GetComponentInChildren<TMP_Text>();nextRegionLabel.alignment=TextAlignmentOptions.MidlineLeft;
            nextRegionArrow=Icon(nextRegion.transform,"Down",launchIcon);nextRegionArrow.rectTransform.localRotation=Quaternion.Euler(0,0,-90);
            armySizeHeading=Label(detail,"ArmySizeHeading","ARMY SIZE",23);armySizeHeading.color=Muted;
            difficultyHeading=Label(detail,"DifficultyHeading","DIFFICULTY",23);difficultyHeading.color=Muted;
            difficultyChecks=new MissionAtlasStatusGraphic[4];difficultyMedals=new Image[4];
            for(int i=0;i<4;i++)
            {
                difficultyMedals[i]=Icon(difficulties[i].transform,"TierMedal",Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(true,(Game.Skirmish.Contracts.SkirmishDifficultyId)(i+1))));
                difficultyMedals[i].color=new Color(1,1,1,0);
                var star=Rect("Engraving",difficultyMedals[i].transform).gameObject.AddComponent<MissionAtlasStatusGraphic>();Fill(star.rectTransform);star.Configure(MissionAtlasStatusGraphic.Kind.Medal,true,i);
                difficultyChecks[i]=Rect("Selected",difficulties[i].transform).gameObject.AddComponent<MissionAtlasStatusGraphic>();difficultyChecks[i].Configure(MissionAtlasStatusGraphic.Kind.Selected,true);
            }
            foreach(var region in regions)foreach(var card in region.Cards)
                for(int i=0;i<4;i++)
                {
                    card.MedalIcons[i].color=new Color(1,1,1,0);
                    var star=Rect("Engraving",card.MedalIcons[i].transform).gameObject.AddComponent<MissionAtlasStatusGraphic>();Fill(star.rectTransform);card.MedalEngravings[i]=star;
                }
            summaryRoot=Rect("ReadOnlyProgress",detail);summaryHeading=Label(summaryRoot,"Heading","",24);summaryHeading.color=Muted;
            summaryTitles=new TMP_Text[5];summaryValues=new TMP_Text[5];summaryChecks=new MissionAtlasStatusGraphic[5];
            for(int i=0;i<5;i++)
            {
                var row=Panel(summaryRoot,"ProgressRow_"+i,0,0,1,1);row.raycastTarget=false;
                summaryTitles[i]=Label(row.transform,"Title","",24);summaryValues[i]=Label(row.transform,"Value","",22);
                summaryChecks[i]=Rect("Completed",row.transform).gameObject.AddComponent<MissionAtlasStatusGraphic>();summaryChecks[i].Configure(MissionAtlasStatusGraphic.Kind.Check,true);
            }
        }
        private void WirePolish()
        {
            if(nextRegion==null)return;
            nextRegion.onClick.AddListener(()=>
            {
                if(state==null)return;int next=regionIndex+1;
                if(next>=state.RegionTitles.Length)return;
                JumpToRegion(next);ScrollRegion?.Invoke(next);
            });
        }
        private void PresentPolish()
        {
            if(detailSubtitle==null)return;
            Copy(detailSubtitle,state.DetailSubtitle);Copy(objectivesHeading,AtlasWords.Get("objectives","OBJECTIVES","هدف‌ها"));
            Copy(armySizeHeading,AtlasWords.Get("army_size","ARMY SIZE","اندازه نیرو"));Copy(difficultyHeading,AtlasWords.Get("difficulty","DIFFICULTY","دشواری"));
            summaryRoot.gameObject.SetActive(!state.Missions&&state.SummaryRows.Length>0);Copy(summaryHeading,state.SummaryHeading);
            for(int i=0;i<summaryTitles.Length;i++)
            {
                bool show=i<state.SummaryRows.Length;summaryTitles[i].transform.parent.gameObject.SetActive(show);if(!show)continue;
                var row=state.SummaryRows[i];Copy(summaryTitles[i],row.Title);Copy(summaryValues[i],row.Value);
                summaryValues[i].color=row.Completed?new Color32(126,218,128,255):Muted;summaryChecks[i].gameObject.SetActive(row.Completed);
            }
            UpdateStickyHeading();
        }
        private void UpdateStickyHeading()
        {
            if(stickyHeading==null||state==null)return;
            stickyHeading.gameObject.SetActive(state.Missions);
            int index=Mathf.Clamp(regionIndex<0?state.SelectedRegion:regionIndex,0,Mathf.Max(0,state.RegionTitles.Length-1));
            Copy(stickyTitle,index<state.RegionTitles.Length?state.RegionTitles[index]:"");
            Copy(stickySummary,index<state.RegionSummaries.Length?state.RegionSummaries[index]:"");
            Copy(medalLegend,state.Skirmish?AtlasWords.Get("difficulty_medals","DIFFICULTY MEDALS","مدال‌های دشواری"):"");
            bool more=state.Missions&&state.Skirmish&&index+1<state.RegionTitles.Length;nextRegion.gameObject.SetActive(more);
            if(more)Copy(nextRegionLabel,AtlasWords.Get("next","NEXT: ","بعدی: ")+state.RegionTitles[index+1]);
            if(state.Missions)Layout(true);
        }
        private void LayoutPolish(float side,float mapW,float mainH)
        {
            if(detailSubtitle==null)return;
            bool mission=state!=null&&state.Missions,skirmish=state!=null&&state.Skirmish;
            bool legend=mission&&skirmish&&mapW>1000;
            float headingWidth=legend?Mathf.Min(mapW-350,mapW*.58f):mapW-64;
            Place(stickyHeading,18,14,headingWidth,96);Place(stickyTitle.rectTransform,16,6,headingWidth-32,48);Place(stickySummary.rectTransform,16,57,headingWidth-32,30);
            medalLegendPanel.gameObject.SetActive(legend);Place(medalLegendPanel,mapW-314,14,268,96);Place(medalLegend.rectTransform,10,5,248,25);medalLegend.alignment=TextAlignmentOptions.TopLeft;SetSize(medalLegend,19);
            for(int i=0;i<4;i++)Place(legendMedals[i].rectTransform,14+i*61,39,42,42);
            float nextWidth=mapW>1000?Mathf.Min(640,mapW*.60f):mapW-64;
            Place(nextRegion.transform as RectTransform,18,mainH-52,nextWidth,42);SetSize(nextRegionLabel,24);
            Place(nextRegionArrow.rectTransform,nextWidth-50,5,32,32);
            var nextLabel=nextRegionLabel.rectTransform;nextLabel.offsetMin=new Vector2(14,2);nextLabel.offsetMax=new Vector2(-66,-2);
            SetSize(stickyTitle,mapW<900?26:30);SetSize(stickySummary,mapW<900?22:24);
            float previewH=Mathf.Min(270,(side-28)*.5625f);
            SetSize(purpose,side<440?26:30);SetSize(objectives,side<440?25:28);
            Place(detailTitle.rectTransform,18,10,side-36,62);SetSize(detailTitle,side<440?30:38);
            Place(detailSubtitle.rectTransform,18,72,side-36,54);Place(detailArt.transform as RectTransform,14,128,side-28,previewH);SetSize(detailSubtitle,side<440?22:24);
            float y=144+previewH;
            float copyHeight=skirmish&&mission?mainH-292-y:mainH-y-22;
            bool rows=state!=null&&!mission&&state.SummaryRows.Length>0;
            if(rows)copyHeight=Mathf.Min(skirmish?76:90,copyHeight);
            Place(detailScroll.transform as RectTransform,18,y,side-36,Mathf.Max(76,copyHeight));Fill(detailScroll.viewport);
            float purposeHeight=Mathf.Max(56,purpose.GetPreferredValues(purpose.text,side-36,0).y+10);
            float objectiveHeight=string.IsNullOrEmpty(objectives.text)?0:Mathf.Max(40,objectives.GetPreferredValues(objectives.text,side-36,0).y+10);
            float headingHeight=objectiveHeight>0?40:0;
            Place(detailCopy,0,0,side-36,purposeHeight+headingHeight+objectiveHeight+(objectiveHeight>0?12:0));
            Place(purpose.rectTransform,0,0,side-36,purposeHeight);Place(objectivesHeading.rectTransform,0,purposeHeight+8,side-36,32);
            objectivesHeading.gameObject.SetActive(objectiveHeight>0&&mission);Place(objectives.rectTransform,0,purposeHeight+headingHeight+8,side-36,objectiveHeight);
            bool needsScroll=detailCopy.rect.height>detailScroll.viewport.rect.height+4;
            Copy(copyHint,needsScroll?AtlasWords.Get("more_details","SWIPE FOR DETAILS","برای جزئیات بکشید"):"");
            Place(copyHint.rectTransform,18,y+Mathf.Max(76,copyHeight)-18,side-36,24);
            bool size=skirmish&&mission&&extraControls.childCount>0&&extraControls.GetChild(0).gameObject.activeSelf;
            armySizeHeading.gameObject.SetActive(size);difficultyHeading.gameObject.SetActive(skirmish&&mission);
            Place(armySizeHeading.rectTransform,18,mainH-294,side-36,36);Place(extraControls,18,mainH-254,side-36,64);
            if(size)foreach(var label in extraControls.GetComponentsInChildren<TMP_Text>(false))
            {
                SetSize(label,22);
                float longest=0;
                foreach(string word in label.text.Split(' '))longest=Mathf.Max(longest,label.GetPreferredValues(word,10000,10000).x);
                float available=(side-48)/3-12;
                if(longest>available)SetSize(label,22*available/longest);
            }
            Place(difficultyHeading.rectTransform,18,mainH-190,side-36,34);Place(difficultyRoot,18,mainH-156,side-36,144);
            float dw=(side-48)/2;
            for(int i=0;i<4;i++)
            {
                Place(difficulties[i].transform as RectTransform,(i%2)*(side-24)/2,(i/2)*72,dw,66);
                Place(difficultyMedals[i].rectTransform,dw*.5f-18,2,36,36);Place(difficultyChecks[i].rectTransform,dw-23,4,18,18);
                Place(difficultyLabels[i].rectTransform,4,37,dw-8,27);difficultyLabels[i].alignment=TextAlignmentOptions.Center;SetSize(difficultyLabels[i],22);
                if(side>=440)
                {
                    Place(difficultyMedals[i].rectTransform,12,9,48,48);Place(difficultyLabels[i].rectTransform,72,4,dw-86,58);
                    difficultyLabels[i].alignment=TextAlignmentOptions.MidlineLeft;
                }
                float available=difficultyLabels[i].rectTransform.rect.width-4;
                float labelWidth=difficultyLabels[i].GetPreferredValues(difficultyLabels[i].text,10000,10000).x;
                if(labelWidth>available)SetSize(difficultyLabels[i],22*available/labelWidth);
            }
            Place(summaryRoot,18,y+(skirmish?78:88),side-36,mainH-y-106);Place(summaryHeading.rectTransform,0,0,side-36,28);
            float sh=side<440||skirmish?64:Mathf.Clamp((mainH-y-142)/Mathf.Max(1,state?.SummaryRows.Length??1),48,64);
            for(int i=0;i<summaryTitles.Length;i++)
            {
                Place(summaryTitles[i].transform.parent as RectTransform,0,34+i*sh,side-36,sh-2);
                // Names and progress get separate lines so long mission titles remain readable.
                Place(summaryTitles[i].rectTransform,10,1,side-92,sh*.52f);
                Place(summaryValues[i].rectTransform,10,sh*.52f,side-92,sh*.48f);
                SetSize(summaryTitles[i],21);SetSize(summaryValues[i],19);
                Place(summaryChecks[i].rectTransform,side-74,sh*.5f-12,24,24);
            }
        }
    }

    public static class AtlasWords
    {
        public static string Get(string key,string en,string fa)=>UiShellRuntimeGateway.Localization.Get("ui.atlas."+key,UiShellRuntimeGateway.Localization.IsRightToLeft?fa:en);
        public static string Complete(int count,int total)=>total<=0?Get("not_released","NOT RELEASED","منتشر نشده"):string.Format(Get("complete","{0}/{1} COMPLETE","{0}/{1} تکمیل"),count,total);
        public static string Soon(int count)=>count>0?string.Format(Get("coming_soon_count","{0} COMING SOON","{0} به‌زودی"),count):Get("all_released","ALL RELEASED","همه منتشر شده");
        public static string Released(int count,int soon)=>string.Format(Get("released_count","{0} RELEASED · {1} SOON","{0} منتشرشده · {1} به‌زودی"),count,soon);
        public static string Brief(string text,int limit=170)
        {
            text=UiShellRuntimeGateway.Localization.GetBySource(text??string.Empty);
            if(string.IsNullOrWhiteSpace(text)||text.Length<=limit)return text??string.Empty;
            int end=text.IndexOf(". ",StringComparison.Ordinal);if(end>35&&end<limit)return text.Substring(0,end+1);
            end=text.LastIndexOf(' ',Math.Min(limit,text.Length-1));return text.Substring(0,Math.Max(0,end))+"…";
        }
    }
}
