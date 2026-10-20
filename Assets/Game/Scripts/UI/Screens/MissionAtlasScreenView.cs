using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public struct MissionAtlasItem
    {
        public string Id, Title, Subtitle, Status;
        public Texture Texture;
        public Sprite Comic;
        public bool Selected, Enabled, Dimmed, Icon;
        public int Medals;
    }

    public sealed class MissionAtlasPresentation
    {
        public bool Missions, Skirmish;
        public string Heading, DetailTitle, Purpose, Objectives, Primary, Secondary;
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
    public sealed class MissionAtlasScreenView : MonoBehaviour, IUiBackOverlayParticipant
    {
        [Serializable] public sealed class Card
        {
            public RectTransform Rect;
            public Button Button;
            public V3GradientGraphic Face;
            public MissionAtlasArtworkView Art;
            public TMP_Text Title, Subtitle, Status;
            public string FullTitle;
            public Image[] MedalIcons;
            [NonSerialized] public Action Click;
            public void Set(MissionAtlasItem item)
            {
                Rect.gameObject.SetActive(true);
                Rect.name = item.Id;FullTitle=item.Title;
                Button.interactable = item.Enabled;
                Face.Configure(Top, Bottom, item.Selected ? Cyan : Border, item.Selected ? 3 : 2);
                Art.Set(item.Comic, item.Texture);Art.ContainIcon(item.Icon);
                Art.Image.color = item.Enabled && !item.Dimmed ? Color.white : new Color(.55f,.60f,.63f,1);
                for(int i=0;i<(MedalIcons?.Length??0);i++)
                {
                    bool show=item.Medals>=0;MedalIcons[i].gameObject.SetActive(show);
                    if(show)MedalIcons[i].sprite=Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal((item.Medals & (1<<i))!=0,(Game.Skirmish.Contracts.SkirmishDifficultyId)(i+1)));
                }
                Copy(Title,item.Title); Copy(Subtitle,item.Subtitle); Copy(Status,item.Status);
                Title.color = item.Selected ? Cyan : item.Enabled ? Color.white : Muted;
                Status.color = item.Selected ? Cyan : Muted;
            }
        }
        [Serializable] public sealed class Region
        {
            public RectTransform Rect;
            public MissionAtlasArtworkView Map;
            public V3PolygonGraphic Tint;
            public TMP_Text Title;
            public Card[] Cards;
        }
        public static readonly Color Top = new Color32(15,30,36,255), Bottom = new Color32(3,14,19,255),
            Border = new Color32(70,92,103,255), Cyan = new Color32(0,210,248,255), Muted = new Color32(171,192,204,255);
        private static readonly Color[] ZoneColors = { new(.05f,.62f,1,.30f),new(1,.62f,.02f,.25f),new(1,.30f,.1f,.24f),new(.25f,.8f,.12f,.26f),new(.92f,.15f,.22f,.26f) };
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
            var track=Panel(missionRoot,"Scrollbar",0,0,20,1); scrollbar=track.gameObject.AddComponent<Scrollbar>();
            var handle=Panel(track.transform,"Handle",0,0,20,100); scrollbar.handleRect=handle.rectTransform; scrollbar.targetGraphic=handle;
            scrollbar.direction=Scrollbar.Direction.BottomToTop; scroll.verticalScrollbar=scrollbar;
            regions=new Region[skirmish?4:1];
            for(int i=0;i<regions.Length;i++)
            {
                var r=new Region {Rect=Rect("Region_"+i,content),Cards=new Card[skirmish?6:5]};
                r.Map=Artwork(r.Rect,"Battlefield"); r.Tint=Rect("RegionBoundary",r.Rect).gameObject.AddComponent<V3PolygonGraphic>();
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
            Wire(); Layout(true);
        }

        private void Wire()
        {
            if(wired || back==null) return; wired=true;
            back.onClick.AddListener(()=>Back?.Invoke()); primary.onClick.AddListener(()=>Primary?.Invoke()); secondary.onClick.AddListener(()=>Secondary?.Invoke());
            for(int i=0;i<navigation.Length;i++) {int n=i; navigation[i].Button.onClick.AddListener(()=>Navigate?.Invoke(n)); zones[i].Button.onClick.AddListener(()=>ChooseZone?.Invoke(n));}
            for(int i=0;i<regions.Length;i++) for(int j=0;j<regions[i].Cards.Length;j++) {int n=i*6+j; regions[i].Cards[j].Button.onClick.AddListener(()=>ChooseMission?.Invoke(n));}
            for(int i=0;i<4;i++) {int n=i; difficulties[i].onClick.AddListener(()=>ChooseDifficulty?.Invoke(n));}
            scroll.onValueChanged.AddListener(_=>OnScroll());
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
                difficulties[i].GetComponent<V3GradientGraphic>().Configure(Top,Bottom,i==selected?Cyan:Border,i==selected?3:2);
            }
        }
        public void JumpToRegion(int index)
        {
            if(state==null || !state.Missions)return;
            regionIndex=Mathf.Clamp(index,0,Mathf.Max(0,state.RegionTitles.Length-1)); scrolling=true;
            scroll.StopMovement(); float total=Mathf.Max(1,content.rect.height-scroll.viewport.rect.height);
            scroll.verticalNormalizedPosition=1-Mathf.Clamp01(regionIndex*RegionHeight()/total); scrolling=false;
        }
        private void OnScroll()
        {
            if(scrolling || state==null || !state.Missions || state.RegionTitles.Length<=1)return;
            int next=Mathf.Clamp(Mathf.RoundToInt(content.anchoredPosition.y/RegionHeight()),0,state.RegionTitles.Length-1);
            if(next==regionIndex)return; regionIndex=next; ScrollRegion?.Invoke(next);
        }
        private void LateUpdate() { if(frame!=null) { Wire(); Layout(false); } }
        private float RegionHeight()=>Mathf.Max(620,mapPanel.rect.height-64);
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
            Place(credits.transform.parent as RectTransform,w-392,16,376,96);
            Place(rail,16,128,nav,mainH); Place(mapPanel,nav+32,128,mapW,mainH); Place(detail,w-side-16,128,side,mainH);
            Fill(mapPanel.GetChild(0) as RectTransform);Fill(detail.GetChild(0) as RectTransform);
            Place(secondary.transform as RectTransform,16,932,w*.31f-24,132); Place(primary.transform as RectTransform,w*.31f+8,932,w*.69f-24,132);
            SetSize(heading,w<1800?36:48);SetSize(detailTitle,side<440?32:40);
            var primaryIcon=primary.transform.Find("Icon") as RectTransform;if(primaryIcon!=null)Place(primaryIcon,((RectTransform)primary.transform).rect.width*.18f,30,72,72);
            var secondaryIcon=secondary.transform.Find("Icon") as RectTransform;if(secondaryIcon!=null)Place(secondaryIcon,((RectTransform)secondary.transform).rect.width*.10f,30,72,72);
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
                r.Tint.ConfigureResponsive(new[]{new Vector2(0,40),new Vector2(rw*.22f,10),new Vector2(rw*.62f,22),new Vector2(rw-8,52),new Vector2(rw-14,rh-40),new Vector2(rw*.73f,rh-10),new Vector2(rw*.2f,rh-22),new Vector2(4,rh-54)},ZoneColors[i],i==0?Cyan:new Color(1,.65f,.1f,.9f),3,new Vector2(rw,rh));
                Place(r.Title.transform.parent as RectTransform,18,18,Mathf.Min(rw-36,780),64);Place(r.Title.rectTransform,16,4,Mathf.Min(rw-68,748),56);
                float cw=(rw-80)/2,ch=(rh-150)/3;
                for(int j=0;j<r.Cards.Length;j++) {Place(r.Cards[j].Rect,26+(j%2)*(cw+22),108+(j/2)*(ch+16),cw,ch);LayoutCard(r.Cards[j],cw,ch,false,false);}
            }
            LayoutOverview(mapW,mainH);
            float previewH=Mathf.Min(280,(side-28)*.5625f);
            Place(detailTitle.rectTransform,18,12,side-36,70);Place(detailArt.transform as RectTransform,14,86,side-28,previewH);
            float y=102+previewH;
            float copyHeight=state!=null&&state.Skirmish?mainH-264-y:mainH-y-18;
            Place(detailScroll.transform as RectTransform,18,y,side-36,Mathf.Max(80,copyHeight));Fill(detailScroll.viewport);
            float purposeHeight=Mathf.Max(76,purpose.GetPreferredValues(purpose.text,side-36,0).y+12),objectiveHeight=Mathf.Max(70,objectives.GetPreferredValues(objectives.text,side-36,0).y+12);
            Place(detailCopy,0,0,side-36,purposeHeight+objectiveHeight+20);
            Place(purpose.rectTransform,0,0,side-36,purposeHeight);Place(objectives.rectTransform,0,purposeHeight+20,side-36,objectiveHeight);
            Place(extraControls,18,mainH-252,side-36,78);Place(difficultyRoot,18,mainH-164,side-36,148);
            for(int i=0;i<4;i++){Place(difficulties[i].transform as RectTransform,(i%2)*(side-24)/2,(i/2)*76,(side-48)/2,68);SetSize(difficultyLabels[i],side<440?24:30);}
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
                    new[]{P(0,0),P(.5f,0),P(.5f,.33f),P(.25f,.39f),P(0,.33f)},
                    new[]{P(.5f,0),P(1,0),P(1,.33f),P(.72f,.39f),P(.5f,.33f)},
                    new[]{P(0,.33f),P(.25f,.39f),P(.5f,.33f),P(.48f,.70f),P(.24f,.74f),P(0,.67f)},
                    new[]{P(.5f,.33f),P(.72f,.39f),P(1,.33f),P(1,.70f),P(.75f,.76f),P(.48f,.70f)},
                    new[]{P(0,.67f),P(.24f,.74f),P(.48f,.70f),P(.75f,.76f),P(1,.70f),P(1,1),P(0,1)}
                };
                centers=new[]{P(.24f,.17f),P(.76f,.17f),P(.23f,.51f),P(.75f,.52f),P(.52f,.86f)};
            }
            float cw=Mathf.Min(380,w*.43f),ph=UiShellRuntimeGateway.Localization.IsRightToLeft?160:120;
            for(int i=0;i<5;i++)
            {Fill(overviewZones[i].rectTransform);overviewZones[i].ConfigureResponsive(shapes[i],ZoneColors[i],new Color(ZoneColors[i].r,ZoneColors[i].g,ZoneColors[i].b,.95f),3,new Vector2(w,h));Place(zones[i].Rect,centers[i].x-cw/2,centers[i].y-ph/2,cw,ph);LayoutCard(zones[i],cw,ph,false,true);}
        }
        private void LayoutCard(Card c,float w,float h,bool nav,bool plaque)
        {
            float art=plaque?0:nav?w*(w<380?.28f:.42f):w*.47f; c.Art.gameObject.SetActive(!plaque);
            Place(c.Art.transform as RectTransform,5,5,Mathf.Max(1,art-10),h-10);
            float x=plaque?16:art+12,tw=w-x-18;
            Place(c.Title.rectTransform,x,12,tw,46); Place(c.Subtitle.rectTransform,x,60,tw,h-102);Place(c.Status.rectTransform,x,h-39,tw,29);
            if(c.MedalIcons!=null)for(int i=0;i<c.MedalIcons.Length;i++)Place(c.MedalIcons[i].rectTransform,x+i*36,h-39,32,32);
            if(nav||plaque)
            {
                bool objective=nav && state!=null && state.Skirmish && state.Missions;
                bool mapNavigation=nav && state!=null && state.Skirmish && !state.Missions;
                float titleHeight=objective||mapNavigation?94:50;
                float subtitleTop=objective?112:mapNavigation?96:52;
                Place(c.Title.rectTransform,x,8,tw,titleHeight);Place(c.Subtitle.rectTransform,x,subtitleTop,tw,objective?h-116:mapNavigation?h-100:h-52);c.Status.gameObject.SetActive(false);
                if(nav && !string.IsNullOrEmpty(c.FullTitle))Copy(c.Title,w<380?c.FullTitle.Replace("CHAPTER ",""):c.FullTitle);
            }
            else c.Status.gameObject.SetActive(c.MedalIcons==null || !c.MedalIcons[0].gameObject.activeSelf);
            float fs=nav?27:plaque?30:32; if(w<380)fs=26;
            SetSize(c.Title,(nav||plaque)&&UiShellRuntimeGateway.Localization.IsRightToLeft?24:fs);SetSize(c.Subtitle,UiShellRuntimeGateway.Localization.IsRightToLeft&&(nav||plaque)?24:nav?26:28);SetSize(c.Status,24);
        }
        private Card CreateCard(Transform parent,string name)
        {
            var p=Panel(parent,name,0,0,400,170);var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p;b.transition=Selectable.Transition.None;
            var c=new Card{Rect=p.rectTransform,Button=b,Face=p,Art=Artwork(p.transform,"Thumbnail"),Title=Label(p.transform,"Title","",32),Subtitle=Label(p.transform,"Subtitle","",28),Status=Label(p.transform,"Status","",24),MedalIcons=new Image[4]};
            if(parent==rail)c.Art.AuthorIconSupport();
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
