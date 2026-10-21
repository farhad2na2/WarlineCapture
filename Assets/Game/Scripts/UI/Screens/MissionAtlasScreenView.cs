using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public struct MissionAtlasItem
    {
        public string Id, Title, Subtitle, Status, ObjectiveId;
        public Texture Texture;
        public Sprite Comic;
        public bool Selected, Enabled, Dimmed, Icon, Completed, Locked;
        public int Medals;
    }

    public sealed class MissionAtlasPresentation
    {
        public bool Missions, Skirmish;
        public string Heading, DetailTitle, DetailSubtitle, Purpose, Objectives, Primary, Secondary;
        public string[] RegionSummaries = Array.Empty<string>();
        public MissionAtlasSummaryRow[] SummaryRows = Array.Empty<MissionAtlasSummaryRow>();
        public string SummaryHeading;
        public bool CanLaunch;
        public bool CanSecondary = true;
        public Texture Overview, Map, Preview;
        public Sprite PreviewComic;
        public MissionAtlasItem[] Navigation = Array.Empty<MissionAtlasItem>();
        public MissionAtlasItem[] MissionsOnMap = Array.Empty<MissionAtlasItem>();
        public string[] RegionTitles = Array.Empty<string>();
        public int SelectedRegion;
    }

    /// <summary>
    /// Shared, prefab-authored presentation for Campaign and Skirmish. All gameplay
    /// commands remain with the original screen binders; this view owns only UI.
    /// Its logical height is always 1080, including on a 2160-unit shell canvas.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed partial class MissionAtlasScreenView : MonoBehaviour, IUiBackOverlayParticipant
    {
        [Serializable] public sealed class Card
        {
            public RectTransform Rect;
            public Button Button;
            public V3GradientGraphic Face;
            public MissionAtlasArtworkView Art;
            public TMP_Text Title, Subtitle, Status, MissionCode;
            public string FullTitle, FullSubtitle, FullStatus;
            public Image[] MedalIcons;
            public MissionAtlasStatusGraphic Completion;
            public Image Lock;
            public MissionAtlasStatusGraphic[] MedalEngravings;
            [NonSerialized] public Action Click;
            public bool HasEarnedMedalVisual(Game.Skirmish.Contracts.SkirmishDifficultyId difficulty)
            {
                int index=(int)difficulty-1;
                if(MedalIcons==null||index<0||index>=MedalIcons.Length)return false;
                var image=MedalIcons[index];
                if(!image.gameObject.activeInHierarchy||image.sprite!=Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(true,difficulty)))return false;
                var engraving=MedalEngravings!=null&&index<MedalEngravings.Length?MedalEngravings[index]:null;
                return engraving!=null?engraving.Earned&&engraving.isActiveAndEnabled&&!engraving.canvasRenderer.cull: image.color.a>0;
            }
            public void Set(MissionAtlasItem item)
            {
                Rect.gameObject.SetActive(true);
                Rect.name = item.Id;FullTitle=item.Title;FullSubtitle=item.Subtitle;FullStatus=item.Status;
                Button.interactable = item.Enabled;
                Face.Configure(item.Selected?new Color32(9,48,65,255):Top, item.Selected?new Color32(3,21,32,255):Bottom, item.Selected ? Cyan : Border, item.Selected ? 3 : 2);
                Art.Set(item.Comic, item.Texture);Art.SetObjective(item.ObjectiveId);Art.ContainIcon(item.Icon);
                Art.Image.color = item.Enabled && !item.Dimmed ? Color.white : new Color(.55f,.60f,.63f,1);
                for(int i=0;i<(MedalIcons?.Length??0);i++)
                {
                    bool show=item.Medals>=0;MedalIcons[i].gameObject.SetActive(show);
                    if(show)MedalIcons[i].sprite=Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal((item.Medals & (1<<i))!=0,(Game.Skirmish.Contracts.SkirmishDifficultyId)(i+1)));
                }
                if(Completion!=null)Completion.gameObject.SetActive(item.Completed);
                if(Lock!=null)Lock.gameObject.SetActive(item.Locked);
                for(int i=0;i<(MedalEngravings?.Length??0);i++)
                    MedalEngravings[i]?.Configure(MissionAtlasStatusGraphic.Kind.Medal,(item.Medals&(1<<i))!=0,i);
                Copy(Title,item.Title); Copy(Subtitle,item.Subtitle); Copy(Status,item.Status);
                Title.color = item.Selected ? Cyan : item.Enabled ? Color.white : Muted;
                Status.color = item.Completed ? new Color32(129,218,128,255) : Muted;
            }
        }
        [Serializable] public sealed class Region
        {
            public RectTransform Rect;
            public MissionAtlasArtworkView Map;
            public V3PolygonGraphic Tint, EdgeGlow;
            public TMP_Text Title;
            public Card[] Cards;
        }
        public static readonly Color Top = new Color32(15,34,44,255), Bottom = new Color32(3,15,24,255),
            Border = new Color32(70,92,103,255), Cyan = new Color32(0,210,248,255), Muted = new Color32(171,192,204,255);
        private static readonly Color[] ZoneColors = { new(.03f,.65f,.95f,.20f),new(.96f,.61f,.10f,.18f),new(.14f,.65f,.93f,.18f),new(.35f,.76f,.21f,.18f),new(.86f,.25f,.25f,.18f) };
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite backIcon, settingsIcon, creditsIcon, mapIcon, launchIcon;
        [SerializeField] private RectTransform frame, rail, mapPanel, detail, extraControls;
        [SerializeField] private Button back, primary, secondary, settings;
        [SerializeField] private TMP_Text heading, primaryLabel, secondaryLabel, detailTitle, purpose, objectives, credits;
        [SerializeField] private MissionAtlasArtworkView overviewArt, detailArt;
        [SerializeField] private RectTransform overviewRoot, missionRoot, content;
        [SerializeField] private ScrollRect scroll, detailScroll;
        [SerializeField] private RectTransform detailCopy;
        [SerializeField] private Scrollbar scrollbar;
        [SerializeField] private Card[] navigation, zones;
        [SerializeField] private V3PolygonGraphic[] overviewZones;
        [SerializeField] private Region[] regions;
        [SerializeField] private Button[] difficulties;
        [SerializeField] private TMP_Text[] difficultyLabels;
        [SerializeField] private RectTransform difficultyRoot;
        private MissionAtlasPresentation state;
        private Vector2 lastSize;
        private Matrix4x4 lastHostMatrix;
        private Rect lastSafe;
        private bool lastRtl, wired, scrolling;
        private int regionIndex = -1;
        public Action Back, Primary, Secondary;
        public Action<int> Navigate, ChooseMission, ChooseZone, ChooseDifficulty, ScrollRegion;
        public RectTransform ExtraControls => extraControls;
        public Button PrimaryButton => primary;
        public Button SecondaryButton => secondary;
        public bool IsOpen => Application.isPlaying && isActiveAndEnabled && ShowingMissions;
        public bool HandleBack() {if(!IsOpen)return false;Back?.Invoke();return true;}
        private void OnEnable() {if(Application.isPlaying)UiBackOverlayRegistry.Register(this);}
        private void OnDisable()=>UiBackOverlayRegistry.Unregister(this);
        public Button BackButton => back;
        public ScrollRect MissionScroll => scroll;
        public Card[] NavigationCards => navigation;
        public Card[] TerritoryCards => zones;
        public Region[] Regions => regions;
        public bool ShowingMissions => state != null && state.Missions;
        public int VisibleRegion => regionIndex;

        public void Author(TMP_FontAsset authoredFont, V3UiArtCatalog art, Sprite arrow, Sprite map, Sprite launch, bool skirmish = true)
        {
            font=authoredFont; backIcon=arrow; mapIcon=map; launchIcon=launch;
            settingsIcon=art.SettingsIcon; creditsIcon=art.CreditsIcon;
            frame=Rect("AtlasFrame",transform); frame.sizeDelta=new Vector2(2400,1080);
            Panel(frame,"Background",0,0,2400,1080).raycastTarget=true;
            back=Command(frame,"BackButton","BACK",backIcon,0);
            heading=Label(Panel(frame,"Heading",0,0,1,1).transform,"ScreenTitle","CAMPAIGN",48);
            settings=Command(frame,"SettingsButton","",settingsIcon,0);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(Game.UI.Contracts.UiShellRouteIntent.OpenSettings,Game.UI.Contracts.UIRoute.Settings,false);
            var wallet=Panel(frame,"Credits",0,0,1,1);
            var coin=Icon(wallet.transform,"Icon",creditsIcon); Place(coin.rectTransform,18,20,56,56);
            credits=Label(wallet.transform,"Value","—",40); Place(credits.rectTransform,90,10,240,76);
            credits.gameObject.AddComponent<MainMenuAccountHeaderView>().Configure(credits);
            rail=Rect("Navigation",frame);
            navigation=new Card[5]; for(int i=0;i<5;i++) navigation[i]=CreateCard(rail,"Navigation_"+i);
            mapPanel=Rect("MapPanel",frame); Panel(mapPanel,"MapFrame",0,0,1,1);
            overviewRoot=Rect("Overview",mapPanel);
            overviewArt=Artwork(overviewRoot,"ContinuousAtlas");
            overviewZones=new V3PolygonGraphic[5]; zones=new Card[5];
            for(int i=0;i<5;i++)
            {
                var polygon=Rect("Territory_"+i,overviewRoot).gameObject.AddComponent<V3PolygonGraphic>();
                overviewZones[i]=polygon; zones[i]=CreateCard(overviewRoot,"TerritoryChoice_"+i);
            }
            missionRoot=Rect("MissionMap",mapPanel);
            scroll=missionRoot.gameObject.AddComponent<ScrollRect>();
            var viewport=Rect("Viewport",missionRoot); viewport.gameObject.AddComponent<RectMask2D>();
            var hit=viewport.gameObject.AddComponent<Image>(); hit.color=Color.clear;
            content=Rect("Regions",viewport); content.pivot=new Vector2(0,1);
            scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.vertical=true;
            scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=75; scroll.decelerationRate=.08f;
            var track=Panel(missionRoot,"Scrollbar",0,0,20,1); track.Configure(new Color32(5,24,39,255),new Color32(4,16,27,255),Border,1); scrollbar=track.gameObject.AddComponent<Scrollbar>();
            var handle=Panel(track.transform,"Handle",0,0,20,100); handle.Configure(new Color32(27,217,255,255),new Color32(0,119,207,255),new Color32(77,227,255,255),1); Fill(handle.rectTransform); scrollbar.handleRect=handle.rectTransform; scrollbar.targetGraphic=handle;
            scrollbar.transition=Selectable.Transition.None;scrollbar.direction=Scrollbar.Direction.BottomToTop; scroll.verticalScrollbar=scrollbar;
            regions=new Region[skirmish?4:1];
            for(int i=0;i<regions.Length;i++)
            {
                var r=new Region {Rect=Rect("Region_"+i,content),Cards=new Card[skirmish?6:5]};
                r.Map=Artwork(r.Rect,"Battlefield"); r.EdgeGlow=Rect("TerritoryEdgeGlow",r.Rect).gameObject.AddComponent<V3PolygonGraphic>(); r.Tint=Rect("RegionBoundary",r.Rect).gameObject.AddComponent<V3PolygonGraphic>();
                r.Title=Label(Panel(r.Rect,"RegionHeading",20,16,700,64).transform,"Label","",32);
                for(int j=0;j<r.Cards.Length;j++) r.Cards[j]=CreateCard(r.Rect,"Mission_"+j);
                regions[i]=r;
            }
            detail=Rect("MissionDetail",frame); Panel(detail,"DetailFrame",0,0,1,1);
            detailTitle=Label(detail,"SelectedMission","",40); detailTitle.color=Cyan;
            detailArt=Artwork(detail,"SelectedComic");
            var copyFrame=Rect("BriefingScroll",detail);detailScroll=copyFrame.gameObject.AddComponent<ScrollRect>();
            var copyViewport=Rect("Viewport",copyFrame);copyViewport.gameObject.AddComponent<RectMask2D>();var copyHit=copyViewport.gameObject.AddComponent<Image>();copyHit.color=Color.clear;
            detailCopy=Rect("Copy",copyViewport);detailScroll.content=detailCopy;detailScroll.viewport=copyViewport;detailScroll.horizontal=false;detailScroll.scrollSensitivity=60;detailScroll.movementType=ScrollRect.MovementType.Clamped;
            purpose=Label(detailCopy,"Purpose","",32); objectives=Label(detailCopy,"Objectives","",29);
            extraControls=Rect("MissionSize",detail);
            difficultyRoot=Rect("Difficulty",detail);
            difficulties=new Button[4]; difficultyLabels=new TMP_Text[4];
            for(int i=0;i<4;i++) { difficulties[i]=Command(difficultyRoot,"Difficulty_"+i,"",null,0); difficultyLabels[i]=difficulties[i].GetComponentInChildren<TMP_Text>(); }
            secondary=Command(frame,"SecondaryCommand","CHAPTERS",mapIcon,1);
            primary=Command(frame,"StartBattleButton","START BRIEFING",launchIcon,2);
            secondaryLabel=secondary.GetComponentInChildren<TMP_Text>(); primaryLabel=primary.GetComponentInChildren<TMP_Text>();
            AuthorPolish(); Wire(); Layout(true);
        }

        private void Wire()
        {
            if(wired || back==null) return; wired=true;
            back.onClick.AddListener(()=>Back?.Invoke()); primary.onClick.AddListener(()=>Primary?.Invoke()); secondary.onClick.AddListener(()=>Secondary?.Invoke());
            for(int i=0;i<navigation.Length;i++) {int n=i; navigation[i].Button.onClick.AddListener(()=>Navigate?.Invoke(n)); zones[i].Button.onClick.AddListener(()=>ChooseZone?.Invoke(n));}
            for(int i=0;i<regions.Length;i++) for(int j=0;j<regions[i].Cards.Length;j++) {int n=i*6+j; regions[i].Cards[j].Button.onClick.AddListener(()=>ChooseMission?.Invoke(n));}
            for(int i=0;i<4;i++) {int n=i; difficulties[i].onClick.AddListener(()=>ChooseDifficulty?.Invoke(n));}
            scroll.onValueChanged.AddListener(_=>OnScroll());
            WirePolish();
        }
        public void Present(MissionAtlasPresentation next)
        {
            if(next==null || frame==null) return; Wire(); state=next;
            Copy(heading,next.Heading); Copy(primaryLabel,next.Primary); Copy(secondaryLabel,next.Secondary);
            Copy(detailTitle,next.DetailTitle); Copy(purpose,next.Purpose); Copy(objectives,next.Objectives);
            primary.interactable=next.CanLaunch;secondary.interactable=next.CanSecondary; overviewRoot.gameObject.SetActive(!next.Missions); missionRoot.gameObject.SetActive(next.Missions);
            overviewArt.Set(null,next.Overview); detailArt.Set(next.PreviewComic,next.Preview);
            for(int i=0;i<5;i++)
            {
                navigation[i].Rect.gameObject.SetActive(i<next.Navigation.Length);
                if(i<next.Navigation.Length) navigation[i].Set(next.Navigation[i]);
                zones[i].Rect.gameObject.SetActive(!next.Missions && i<next.Navigation.Length);
                if(!next.Missions && i<next.Navigation.Length) { var z=next.Navigation[i]; z.Comic=null;z.Texture=null; zones[i].Set(z); }
            }
            for(int i=0;i<regions.Length;i++)
            {
                bool visible=next.Missions && i<next.RegionTitles.Length;
                regions[i].Rect.gameObject.SetActive(visible); if(!visible)continue;
                regions[i].Map.Set(null,next.Map); Copy(regions[i].Title,next.RegionTitles[i]);
                for(int j=0;j<regions[i].Cards.Length;j++)
                {
                    int index=i*6+j;
                    bool exists=index<next.MissionsOnMap.Length && !string.IsNullOrEmpty(next.MissionsOnMap[index].Id);
                    regions[i].Cards[j].Rect.gameObject.SetActive(exists);
                    if(exists)regions[i].Cards[j].Set(next.MissionsOnMap[index]);
                }
            }
            difficultyRoot.gameObject.SetActive(next.Skirmish && next.Missions);
            extraControls.gameObject.SetActive(next.Skirmish && next.Missions);
            PresentPolish();
            Layout(true);
            if(next.Missions && regionIndex!=next.SelectedRegion) JumpToRegion(next.SelectedRegion);
            if(!next.Missions)regionIndex=-1;
        }
        public void ShowNotice(string message) {Copy(purpose,message);if(detailScroll!=null)detailScroll.verticalNormalizedPosition=1;}
        public void ShowLaunching() {primary.interactable=false;Copy(primaryLabel,UiShellRuntimeGateway.Localization.IsRightToLeft?"در حال آماده‌سازی":"PREPARING");}
        public void SetDifficulties(string[] labels,int selected,bool[] enabled)
        {
            for(int i=0;i<4;i++)
            {
                Copy(difficultyLabels[i],labels[i]); difficulties[i].interactable=enabled==null || enabled[i];
                difficulties[i].GetComponent<V3GradientGraphic>().Configure(i==selected?new Color32(3,77,116,255):Top,i==selected?new Color32(2,35,59,255):Bottom,i==selected?Cyan:Border,i==selected?3:2);
                if(difficultyChecks!=null&&i<difficultyChecks.Length)difficultyChecks[i].gameObject.SetActive(i==selected);
            }
        }
        public void JumpToRegion(int index)
        {
            if(state==null || !state.Missions)return;
            regionIndex=Mathf.Clamp(index,0,Mathf.Max(0,state.RegionTitles.Length-1)); scrolling=true;
            scroll.StopMovement(); float total=Mathf.Max(1,content.rect.height-scroll.viewport.rect.height);
            scroll.verticalNormalizedPosition=1-Mathf.Clamp01(regionIndex*RegionHeight()/total); scrolling=false;UpdateStickyHeading();
        }
        private void OnScroll()
        {
            if(scrolling || state==null || !state.Missions || state.RegionTitles.Length<=1)return;
            int next=Mathf.Clamp(Mathf.FloorToInt((content.anchoredPosition.y+2)/RegionHeight()),0,state.RegionTitles.Length-1);
            if(next==regionIndex)return; regionIndex=next; ScrollRegion?.Invoke(next);UpdateStickyHeading();
        }
        private void LateUpdate() { if(frame!=null) { Wire(); Layout(false); } }
        private float RegionHeight()=>Mathf.Max(680,mapPanel.rect.height-40);
        private void Layout(bool force)
        {
            Canvas canvas=GetComponentInParent<Canvas>(); var c=canvas!=null?canvas.rootCanvas.transform as RectTransform:null;
            Vector2 size=c!=null?c.rect.size:new Vector2(2400,1080); Rect safe=Application.isPlaying?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);
            bool rtl=UiShellRuntimeGateway.Localization.IsRightToLeft;
            if(!force && size==lastSize && safe==lastSafe && rtl==lastRtl && lastHostMatrix==transform.localToWorldMatrix)return;
            lastHostMatrix=transform.localToWorldMatrix;
            lastSize=size;lastSafe=safe;lastRtl=rtl;
            float left=0,right=0,top=0,bottom=0;
            if(c!=null && Screen.width>0 && Screen.height>0 && Application.isPlaying)
            {left=size.x*safe.xMin/Screen.width;right=size.x*(Screen.width-safe.xMax)/Screen.width;top=size.y*(Screen.height-safe.yMax)/Screen.height;bottom=size.y*safe.yMin/Screen.height;}
            float scale=Mathf.Max(.1f,(size.y-top-bottom)/1080); float w=(size.x-left-right)/scale;
            frame.anchorMin=frame.anchorMax=new Vector2(.5f,.5f);frame.pivot=new Vector2(0,1);frame.sizeDelta=new Vector2(w,1080);
            if(c!=null) {float ps=Mathf.Max(.0001f,transform.lossyScale.x);frame.localScale=Vector3.one*scale*c.lossyScale.x/ps;frame.position=c.TransformPoint(new Vector3(c.rect.xMin+left,c.rect.yMax-top,0));}
            float nav=Mathf.Clamp(w*.185f,280,450), side=Mathf.Clamp(w*.24f,350,580), mapW=w-nav-side-64, mainH=788;
            Place(frame.GetChild(0) as RectTransform,0,0,w,1080);
            Place(back.transform as RectTransform,16,16,204,96); Place(heading.transform.parent as RectTransform,236,16,w-780,96);
            Place(heading.rectTransform,24,8,w-828,80); Place(settings.transform as RectTransform,w-528,16,120,96);
            var gear=settings.transform.Find("Icon") as RectTransform;
            if(gear!=null){gear.anchorMin=gear.anchorMax=gear.pivot=Vector2.one*.5f;gear.anchoredPosition=Vector2.zero;gear.sizeDelta=new Vector2(64,64);}
            Place(credits.transform.parent as RectTransform,w-392,16,376,96);
            Place(rail,16,128,nav,mainH); Place(mapPanel,nav+32,128,mapW,mainH); Place(detail,w-side-16,128,side,mainH);
            Fill(mapPanel.GetChild(0) as RectTransform);Fill(detail.GetChild(0) as RectTransform);
            Place(secondary.transform as RectTransform,16,932,w*.31f-24,132); Place(primary.transform as RectTransform,w*.31f+8,932,w*.69f-24,132);
            SetSize(heading,w<1800?36:48);SetSize(detailTitle,side<440?32:40);
            var primaryIcon=primary.transform.Find("Icon") as RectTransform;if(primaryIcon!=null)Place(primaryIcon,((RectTransform)primary.transform).rect.width*.18f,30,72,72);
            var secondaryIcon=secondary.transform.Find("Icon") as RectTransform;if(secondaryIcon!=null)Place(secondaryIcon,24,30,72,72);
            secondaryLabel.rectTransform.offsetMin=new Vector2(112,4);SetSize(secondaryLabel,w<1800?30:34);
            float nh=state!=null && state.Missions && state.Skirmish?165:148;
            for(int i=0;i<5;i++) {Place(navigation[i].Rect,0,i*(nh+12),nav,nh); LayoutCard(navigation[i],nav,nh,true,false);}
            Fill(overviewRoot);Fill(overviewArt.transform as RectTransform);Fill(missionRoot);Fill(scroll.viewport);
            scroll.viewport.offsetMax=new Vector2(-28,0);
            Place(scrollbar.transform as RectTransform,mapW-22,4,18,mainH-8);
            int count=state!=null?Mathf.Max(1,state.RegionTitles.Length):4; float rh=RegionHeight();
            content.anchorMin=content.anchorMax=new Vector2(0,1);content.sizeDelta=new Vector2(mapW-28,rh*count+64);
            scroll.vertical=count>1;scrollbar.gameObject.SetActive(count>1);
            for(int i=0;i<regions.Length;i++)
            {
                var r=regions[i];float rw=mapW-28;
                Place(r.Rect,0,i*rh,rw,rh);Fill(r.Map.transform as RectTransform);Fill(r.Tint.rectTransform);
                bool selectedRegion=state==null || i==state.SelectedRegion;
                // Leave terrain visible around every row, with the same clear district treatment as Operations.
                Vector2 P(float x,float y)=>new(rw*x,rh*y);
                var contour=new[]{P(.014f,.21f),P(.07f,.13f),P(.18f,.09f),P(.31f,.035f),P(.43f,.07f),P(.56f,.055f),P(.68f,.11f),P(.80f,.085f),P(.94f,.15f),P(.985f,.24f),P(.963f,.41f),P(.985f,.60f),P(.97f,.81f),P(.958f,.945f),P(.84f,.973f),P(.70f,.98f),P(.56f,.994f),P(.43f,.975f),P(.29f,.982f),P(.15f,.969f),P(.045f,.946f),P(.029f,.82f),P(.045f,.66f),P(.018f,.47f)};
                Color zone=selectedRegion?new Color(.0f,.50f,.82f,.40f):ZoneColors[i];
                Color edge=selectedRegion?Cyan:new Color(zone.r,zone.g,zone.b,.95f);
                if(r.EdgeGlow!=null){Fill(r.EdgeGlow.rectTransform);r.EdgeGlow.ConfigureResponsive(contour,selectedRegion?new Color(0,.08f,.12f,.26f):new Color(0,.04f,.06f,.08f),new Color(edge.r,edge.g,edge.b,.20f),9,new Vector2(rw,rh));}
                r.Tint.ConfigureResponsive(contour,zone,edge,3,new Vector2(rw,rh));
                Place(r.Title.transform.parent as RectTransform,18,18,Mathf.Min(rw-36,780),64);Place(r.Title.rectTransform,16,4,Mathf.Min(rw-68,748),56);
                r.Title.transform.parent.gameObject.SetActive(false);
                float inset=rw<850?38:Mathf.Clamp(rw*.12f,70,142);float cw=(rw-inset*2-24)/2,ch=(rh-190)/3;
                for(int j=0;j<r.Cards.Length;j++) {Place(r.Cards[j].Rect,inset+(j%2)*(cw+24),130+(j/2)*(ch+14),cw,ch);LayoutCard(r.Cards[j],cw,ch,false,false);}
            }
            LayoutOverview(mapW,mainH);
            LayoutPolish(side,mapW,mainH);
            foreach(var art in GetComponentsInChildren<MissionAtlasArtworkView>(true))art.Refresh();
        }
        private void LayoutOverview(float w,float h)
        {
            Vector2 P(float x,float y)=>new(w*x,h*y);
            Vector2[][] shapes={new[]{P(0,0),P(.46f,0),P(.53f,.22f),P(.25f,.40f),P(0,.29f)},new[]{P(.46f,0),P(1,0),P(1,.29f),P(.73f,.38f),P(.53f,.22f)},new[]{P(.25f,.40f),P(.53f,.22f),P(.73f,.38f),P(.69f,.66f),P(.50f,.80f),P(.31f,.64f)},new[]{P(0,.29f),P(.25f,.40f),P(.31f,.64f),P(.50f,.80f),P(.56f,1),P(0,1)},new[]{P(1,.29f),P(1,1),P(.56f,1),P(.50f,.80f),P(.69f,.66f),P(.73f,.38f)}};
            Vector2[] centers={P(.23f,.20f),P(.77f,.18f),P(.51f,.49f),P(.23f,.80f),P(.80f,.79f)};
            if(state!=null && state.Skirmish)
            {
                // Match the existing theatre painting: camps/city above, pass/refinery
                // in the middle, and the airfield across the lower edge.
                shapes=new[]{
                    new[]{P(0,0),P(.44f,0),P(.49f,.10f),P(.46f,.22f),P(.51f,.32f),P(.35f,.37f),P(.18f,.34f),P(0,.38f)},
                    new[]{P(.44f,0),P(1,0),P(1,.35f),P(.84f,.32f),P(.69f,.39f),P(.51f,.32f),P(.46f,.22f),P(.49f,.10f)},
                    new[]{P(0,.38f),P(.18f,.34f),P(.35f,.37f),P(.51f,.32f),P(.46f,.50f),P(.51f,.62f),P(.43f,.72f),P(.28f,.68f),P(.14f,.75f),P(0,.69f)},
                    new[]{P(.51f,.32f),P(.69f,.39f),P(.84f,.32f),P(1,.35f),P(1,.72f),P(.83f,.68f),P(.72f,.76f),P(.57f,.71f),P(.43f,.72f),P(.51f,.62f),P(.46f,.50f)},
                    new[]{P(0,.69f),P(.14f,.75f),P(.28f,.68f),P(.43f,.72f),P(.57f,.71f),P(.72f,.76f),P(.83f,.68f),P(1,.72f),P(1,1),P(0,1)}
                };
                centers=new[]{P(.24f,.17f),P(.76f,.17f),P(.23f,.51f),P(.75f,.52f),P(.52f,.86f)};
            }
            float cw=Mathf.Min(380,w*.43f),ph=UiShellRuntimeGateway.Localization.IsRightToLeft?176:148;
            for(int i=0;i<5;i++)
            {Fill(overviewZones[i].rectTransform);overviewZones[i].ConfigureResponsive(shapes[i],state!=null&&i<state.Navigation.Length&&state.Navigation[i].Selected?new Color(.0f,.50f,.82f,.40f):ZoneColors[i],state!=null&&i<state.Navigation.Length&&state.Navigation[i].Selected?Cyan:new Color(ZoneColors[i].r,ZoneColors[i].g,ZoneColors[i].b,.95f),3,new Vector2(w,h));Place(zones[i].Rect,Mathf.Clamp(centers[i].x-cw/2,12,Mathf.Max(12,w-cw-12)),Mathf.Clamp(centers[i].y-ph/2,12,Mathf.Max(12,h-ph-12)),cw,ph);LayoutCard(zones[i],cw,ph,false,true);}
        }
        private void LayoutCard(Card c,float w,float h,bool nav,bool plaque)
        {
            if(c.MissionCode!=null)c.MissionCode.gameObject.SetActive(false);
            if(!nav&&!plaque&&state!=null&&state.Skirmish){LayoutSkirmishMissionCard(c,w,h);return;}
            bool objective=nav&&state!=null&&state.Missions&&state.Skirmish;
            float art=plaque?0:objective?Mathf.Min(104,w*.28f):nav?w*(w<380?.30f:.34f):w*(w<340?.25f:.28f);
            c.Art.gameObject.SetActive(!plaque);float imageHeight=nav?h-10:Mathf.Max(1,(art-10)*.5625f);Place(c.Art.transform as RectTransform,5,nav?5:(h-imageHeight)*.5f,Mathf.Max(1,art-10),imageHeight);
            float x=plaque?14:art+12,tw=w-x-16;
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
            bool compactMission=!nav&&!plaque&&state!=null&&state.Skirmish&&w<380&&!fa;
            float titleHeight=nav||plaque?64:compactMission?60:76;
            Place(c.Title.rectTransform,x,8,tw,titleHeight);
            Place(c.Subtitle.rectTransform,x,nav||plaque?76:compactMission?70:86,tw,nav||plaque?32:compactMission?66:fa?66:54);
            if(!nav&&!plaque)Copy(c.Subtitle,compactMission?(c.FullSubtitle??string.Empty).Replace(" · ","\n"):c.FullSubtitle);
            Place(c.Status.rectTransform,x,h-38,tw,34);c.Status.gameObject.SetActive(true);
            if(c.Completion!=null)Place((RectTransform)c.Completion.transform,w-38,10,28,28);
            if(c.Lock!=null)Place(c.Lock.rectTransform,w-38,10,28,28);

            if(c.MedalIcons!=null)for(int i=0;i<c.MedalIcons.Length;i++)Place(c.MedalIcons[i].rectTransform,x+i*42,h-42,38,38);
            if(c.MedalIcons!=null&&c.MedalIcons.Length>0&&c.MedalIcons[0].gameObject.activeSelf)c.Status.gameObject.SetActive(false);
            if(plaque){c.Art.gameObject.SetActive(false);Place(c.Subtitle.rectTransform,x,72,tw,34);}
            if(state!=null&&!state.Skirmish&&!plaque)
            {
                Place(c.Subtitle.rectTransform,x,10,tw,30);
                Place(c.Title.rectTransform,x,nav?42:54,tw,nav?64:76);
            }
            bool mark=(c.Completion!=null&&c.Completion.gameObject.activeSelf)||(c.Lock!=null&&c.Lock.gameObject.activeSelf);
            if(mark)
            {
                if(state!=null&&!state.Skirmish&&!plaque)c.Subtitle.rectTransform.sizeDelta=new Vector2(tw-34,30);
                else c.Title.rectTransform.sizeDelta=new Vector2(tw-34,titleHeight);
            }
            if(nav)Copy(c.Status,w<380?(c.FullStatus??string.Empty).Replace("COMING SOON","SOON"):c.FullStatus);
            float size=nav?26:plaque?27:w<380?26:30;
            SetSize(c.Title,fa?24:size);
            if(!plaque)
            {
                // Keep a long single word intact rather than ellipsizing it inside a large card.
                float longest=0;
                foreach(string word in (c.Title.text??string.Empty).Split(' '))
                    longest=Mathf.Max(longest,c.Title.GetPreferredValues(word,10000,10000).x);
                float titleWidth=c.Title.rectTransform.rect.width;
                if(longest>titleWidth-8)SetSize(c.Title,Mathf.Max(18,c.Title.fontSize*(titleWidth-8)/longest));
            }
            SetSize(c.Subtitle,nav||plaque?23:compactMission||fa?20:24);SetSize(c.Status,22);
            FitLabelHeight(c.Title,18);FitLabelHeight(c.Subtitle,16);
        }
        private void LayoutSkirmishMissionCard(Card c,float w,float h)
        {
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft,narrow=w<380;
            float art=w*.40f,iw=art-24,ih=Mathf.Min(h-28,iw*.875f),x=art+16,tw=w-x-20;
            c.Art.gameObject.SetActive(true);Place(c.Art.transform as RectTransform,16,(h-ih)*.5f,iw,ih);
            bool mark=(c.Completion!=null&&c.Completion.gameObject.activeSelf)||(c.Lock!=null&&c.Lock.gameObject.activeSelf);
            float titleWidth=tw-(!narrow&&mark?28:0);
            Place(c.Title.rectTransform,x,14,titleWidth,64);SetSize(c.Title,fa?26:narrow?30:34);
            c.Title.alignment=fa?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.TopLeft;
            float longest=0;
            foreach(string word in (c.Title.text??string.Empty).Split(' '))longest=Mathf.Max(longest,c.Title.GetPreferredValues(word,10000,10000).x);
            if(longest>titleWidth-8)SetSize(c.Title,Mathf.Max(18,c.Title.fontSize*(titleWidth-8)/longest));
            float titleHeight=Mathf.Clamp(c.Title.GetPreferredValues(c.Title.text,titleWidth,0).y,44,68);
            Place(c.Title.rectTransform,x,14,titleWidth,titleHeight);FitLabelHeight(c.Title,20);
            // Separate code/profile labels keep Latin IDs and Persian fallback metrics from colliding.
            string[] metadata=(c.FullSubtitle??string.Empty).Split(new[]{'\n'},2);
            c.MissionCode.gameObject.SetActive(true);Copy(c.MissionCode,metadata[0]);Copy(c.Subtitle,metadata.Length>1?metadata[1]:string.Empty);
            float codeY=14+titleHeight;
            Place(c.MissionCode.rectTransform,x,codeY,tw,26);Place(c.Subtitle.rectTransform,x,codeY+26,tw,28);
            c.MissionCode.alignment=c.Subtitle.alignment=fa?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
            SetSize(c.MissionCode,narrow?22:24);SetSize(c.Subtitle,fa?20:narrow?20:22);
            float profileWidth=c.Subtitle.GetPreferredValues(c.Subtitle.text,10000,10000).x;
            if(profileWidth>tw-4)SetSize(c.Subtitle,Mathf.Max(16,c.Subtitle.fontSize*(tw-4)/profileWidth));
            FitLabelHeight(c.Subtitle,16);
            Place(c.Status.rectTransform,x,h-48,tw,32);c.Status.gameObject.SetActive(true);SetSize(c.Status,narrow?20:22);
            if(c.Completion!=null)Place(c.Completion.rectTransform,narrow?art-34:w-38,narrow?(h-ih)*.5f+6:12,26,26);
            if(c.Lock!=null)Place(c.Lock.rectTransform,narrow?art-34:w-38,narrow?(h-ih)*.5f+6:12,26,26);
            float medal=Mathf.Min(38,(tw-18)/4);
            if(c.MedalIcons!=null)for(int i=0;i<c.MedalIcons.Length;i++)Place(c.MedalIcons[i].rectTransform,x+i*(medal+6),h-50,medal,medal);
            if(c.MedalIcons!=null&&c.MedalIcons.Length>0&&c.MedalIcons[0].gameObject.activeSelf)c.Status.gameObject.SetActive(false);
        }
        private static void FitLabelHeight(TMP_Text label,float minimum)
        {
            label.ForceMeshUpdate();
            float preferred=label.GetPreferredValues(label.text,label.rectTransform.rect.width,0).y;
            float height=label.rectTransform.rect.height-2;
            if(preferred>height&&height>0)SetSize(label,Mathf.Max(minimum,label.fontSize*height/preferred));
        }
        private Card CreateCard(Transform parent,string name)
        {
            var p=Panel(parent,name,0,0,400,170);var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p;b.transition=Selectable.Transition.None;
            var c=new Card{Rect=p.rectTransform,Button=b,Face=p,Art=Artwork(p.transform,"Thumbnail"),Title=Label(p.transform,"Title","",32),Subtitle=Label(p.transform,"Subtitle","",28),Status=Label(p.transform,"Status","",24),MedalIcons=new Image[4]};
            c.MissionCode=Label(p.transform,"MissionCode","",24);c.MissionCode.gameObject.SetActive(false);
            if(parent==rail)c.Art.AuthorIconSupport();
            AuthorCardPolish(c,p.transform);
            for(int i=0;i<4;i++){c.MedalIcons[i]=Icon(p.transform,"Medal_"+i,null);c.MedalIcons[i].gameObject.SetActive(false);}return c;
        }
        private Button Command(Transform parent,string name,string copy,Sprite icon,int role)
        {
            var p=Panel(parent,name,0,0,200,96);
            p.Configure(role==2?new Color32(36,123,28,255):role==1?new Color32(0,51,98,255):Top,role==2?new Color32(7,64,20,255):role==1?new Color32(0,25,53,255):Bottom,role==2?new Color32(80,230,25,255):Cyan,2);
            var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p; b.transition=Selectable.Transition.ColorTint;
            var t=Label(p.transform,"Label",copy,role==2?48:34); t.alignment=TextAlignmentOptions.Center;
            t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=new Vector2(icon!=null?74:8,4);t.rectTransform.offsetMax=new Vector2(-8,-4);
            if(icon!=null) {var im=Icon(p.transform,"Icon",icon);Place(im.rectTransform,16,22,56,56);}
            return b;
        }
        private TMP_Text Label(Transform p,string name,string text,float size)
        {
            var r=Rect(name,p);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.color=Color.white;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Ellipsis;SetSize(t,size);t.text=text;return t;
        }
        public static void Copy(TMP_Text t,string value)
        {
            if(t==null)return;
            if(!Application.isPlaying){t.text=value??string.Empty;return;}
            UiLocalizedText.Set(t,value??string.Empty);var binding=t.GetComponent<V3LocalizedTextBindingView>();if(binding!=null)binding.ConfigureFontBounds(t.fontSize,t.fontSize);
        }
        private static void SetSize(TMP_Text t,float n) {t.enableAutoSizing=false;t.fontSize=t.fontSizeMin=t.fontSizeMax=n;t.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(n,n);}
        private static Image Icon(Transform p,string name,Sprite sprite) {var i=Rect(name,p).gameObject.AddComponent<Image>();i.sprite=sprite;i.preserveAspect=true;i.raycastTarget=false;return i;}
        private static MissionAtlasArtworkView Artwork(Transform p,string name) {var r=Rect(name,p);return r.gameObject.AddComponent<MissionAtlasArtworkView>();}
        private static V3GradientGraphic Panel(Transform p,string name,float x,float y,float w,float h) {var r=Rect(name,p);Place(r,x,y,w,h);var g=r.gameObject.AddComponent<V3GradientGraphic>();g.Configure(Top,Bottom,Border,2);return g;}
        private static RectTransform Rect(string name,Transform p) {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.pivot=r.anchorMin=r.anchorMax=new Vector2(0,1);return r;}
        public static void Place(RectTransform r,float x,float y,float w,float h) {if(r==null)return;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        public static void Fill(RectTransform r) {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
