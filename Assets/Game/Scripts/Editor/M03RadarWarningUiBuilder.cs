using System;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using RTLTMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class M03RadarWarningUiBuilder
    {
        public const string GuidePath="Assets/Game/Prefabs/UI/Popups/M03_FieldGuide.prefab";
        private const string HudPath="Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
        private const string ShellPath="Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab";
        private static readonly Color Ink=new Color32(13,25,31,255),Raised=new Color32(29,48,57,255),Cyan=new Color32(63,216,225,255);
        private static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset");

        public static void Build()
        {
            M03RadarWarningGuideBuilder.Build(); M03RadarWarningLocalizationBuilder.Import();
            BuildGuide(); BuildHud(); BuildResultGuide(); BuildCampaignMedia();
            Edit(ThreatAlertV3PrefabBuilder.PrefabPath,ConfigureThreat);
            var scene=EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            var content=UnityEngine.Object.FindAnyObjectByType<UIShellContentView>(FindObjectsInactive.Include);
            if(content==null) throw new InvalidOperationException("Menu's authored shell content owner is missing.");
            {
                var data=new SerializedObject(content);
                Ref(data,"threatAlertPopupPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(ThreatAlertV3PrefabBuilder.PrefabPath));
                Ref(data,"missionFieldGuidePrefab",AssetDatabase.LoadAssetAtPath<GameObject>(GuidePath)); data.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[M03UiBuilder] result=Passed guide=57/12 warning=canonical radarPing=existingSupportSlot optionalSkip=bound");
        }
        private static void BuildCampaignMedia()
        {
            var art=AssetDatabase.LoadAssetAtPath<Texture>(M03RadarWarningMediaImporter.ArtRoot+"/M03-B01.png");
            Edit("Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab",root=>
            {
                var data=new SerializedObject(root.GetComponentInChildren<CampaignOperationsScreenView>(true));
                Ref(data,"m03MissionPreview",art);
                Ref(data,"footerStoryArchiveButton",Find(root,"FooterStoryArchiveButton").GetComponent<UnityEngine.UI.Button>());
                data.ApplyModifiedPropertiesWithoutUndo();
            });
            Edit("Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab",root=>
            {
                var data=new SerializedObject(root.GetComponentInChildren<MissionBriefingScreenView>(true));
                Ref(data,"m03MissionArt",art); data.ApplyModifiedPropertiesWithoutUndo();
            });
        }
        internal static void BuildGuide()
        {
            var root=new GameObject("M03_FieldGuide",typeof(RectTransform));
            try
            {
                var rootRect=(RectTransform)root.transform; rootRect.anchorMin=Vector2.zero; rootRect.anchorMax=Vector2.one; rootRect.sizeDelta=Vector2.zero;
                var composition=Rect("Composition",root.transform,0,0,1672,941); composition.anchorMin=composition.anchorMax=composition.pivot=new Vector2(.5f,.5f); composition.anchoredPosition=Vector2.zero;
                var scrim=Panel("Scrim",composition,0,0,1672,941,new Color(0,0,0,.82f));
                var surface=Panel("Guide",composition,112,16,1448,909,Ink);
                var responsive=composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
                responsive.Configure(new Vector2(1672,941),MainMenuV3SectionAlignment.Center,shouldExpandToCanvasWidth:true,
                    targetsAnchoredToCenter:new[]{surface},targetsExpandedAcrossWidth:new[]{scrim});
                Text("Title",surface,30,0,1010,80,30,"mission.m03.guide.title");
                Text("Paused",surface,30,70,1010,52,19,"mission.m03.guide.pause");
                var close=Button("Close",surface,1110,24,238,54,"mission.m03.guide.close");
                var topics=Button("Topics",surface,30,132,228,62,"mission.m03.guide.topics");
                var classes=Button("Classes",surface,272,132,228,62,"mission.m03.guide.classes");
                var filter=Button("Filter",surface,518,132,450,62,"mission.m03.guide.all");
                var radioButton=Button("RadioArchive",surface,518,132,450,62,"mission.m03.guide.radio");
                var searchRect=Panel("Search",surface,986,132,362,62,Raised);
                var input=searchRect.gameObject.AddComponent<TMP_InputField>();
                var inputText=Text("Input",searchRect,15,3,332,56,21,"");
                var placeholder=Text("Placeholder",searchRect,15,3,332,56,21,"mission.m03.guide.search"); placeholder.color=new Color(.6f,.7f,.72f);
                input.textViewport=searchRect; input.textComponent=inputText; input.placeholder=placeholder; input.characterLimit=80;
                var viewport=Rect("ReadingViewport",surface,30,205,1318,532);
                viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,0);
                viewport.gameObject.AddComponent<RectMask2D>();
                var reading=Rect("ReadingContent",viewport,0,0,1318,925);
                reading.anchorMin=new Vector2(0,1); reading.anchorMax=Vector2.one; reading.sizeDelta=new Vector2(0,925);
                var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=reading;
                scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=32;
                var heading=Text("PageTitle",reading,2,0,1300,100,32,"");
                var body=Text("Body",reading,2,112,1040,350,24,"");
                var portraitRect=Rect("Portrait",reading,1080,126,220,220); var portrait=portraitRect.gameObject.AddComponent<Image>(); portrait.preserveAspect=true; portrait.raycastTarget=false;
                var radioRect=Rect("RadioPanel",reading,154,335,1000,450); var radioPanel=radioRect.gameObject.AddComponent<Image>(); radioPanel.preserveAspect=true; radioPanel.raycastTarget=false;
                var example=Text("Example",reading,2,480,1280,142,23,""); example.color=new Color(.56f,.88f,.86f);
                var mistake=Text("Mistake",reading,2,646,1280,142,21,""); mistake.color=new Color(1,.76f,.43f);
                var diagram=Text("Diagram",reading,2,815,1280,90,20,""); diagram.alignment=TextAlignmentOptions.Center;
                Text("ScrollHint",surface,360,728,660,42,18,"mission.m03.guide.scroll").alignment=TextAlignmentOptions.Center;
                var previous=Button("Previous",surface,30,744,244,68,"mission.m03.guide.previous");
                var next=Button("Next",surface,1104,744,244,68,"mission.m03.guide.next");
                var page=Text("PageNumber",surface,590,744,200,68,22,""); page.alignment=TextAlignmentOptions.Center;
                var data=new SerializedObject(root.AddComponent<MissionFieldGuideView>());
                Ref(data,"scroll",scroll); Ref(data,"guide",AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path));
                Ref(data,"topicsButton",topics); Ref(data,"classesButton",classes); Ref(data,"previousButton",previous); Ref(data,"nextButton",next);
                Ref(data,"closeButton",close); Ref(data,"filterButton",filter); Ref(data,"search",input); Ref(data,"portrait",portrait);
                Ref(data,"radioButton",radioButton); Ref(data,"radioPanel",radioPanel);
                Ref(data,"radio16x9",M03RadarWarningMediaImporter.Panel("C01",false)); Ref(data,"radio20x9",M03RadarWarningMediaImporter.Panel("C01",true));
                Ref(data,"title",Binding(heading)); Ref(data,"body",Binding(body)); Ref(data,"example",Binding(example)); Ref(data,"mistake",Binding(mistake));
                Ref(data,"diagram",Binding(diagram)); Ref(data,"page",Binding(page)); Ref(data,"filterLabel",filter.GetComponentInChildren<V3LocalizedTextBindingView>());
                data.ApplyModifiedPropertiesWithoutUndo(); MissionSupportUiStyle.Guide(root); MissionUiSerializedBindingsAuthoring.Apply(root); PrefabUtility.SaveAsPrefabAsset(root,GuidePath);
            }
            finally {UnityEngine.Object.DestroyImmediate(root);}
        }
        public static void RepairGuide()
        {
            BuildGuide(); AssetDatabase.SaveAssets();
            Debug.Log("[MissionGuideRepair] result=Passed style=BuildDrawer tabs=left close=squareX");
        }
        public static void RepairHud()
        {
            BuildGuide(); BuildHud(); M04AirliftPresentationBuilder.BuildHud(); AssetDatabase.SaveAssets();
            Debug.Log("[M03HudRepair] result=Passed controls=integratedAria warning=singleOwner");
        }
        internal static void BuildHud()=>Edit(HudPath,root=>
        {
            var tutorial=root.GetComponentInChildren<AriaTutorialBriefingView>(true);
            var tutorialData=new SerializedObject(tutorial);
            Ref(tutorialData,"missionPortraitClip",Find(tutorial.gameObject,"PortraitClip"));
            Ref(tutorialData,"missionPortraitStage",Find(tutorial.gameObject,"PortraitStage"));
            Ref(tutorialData,"missionTelemetry",Find(tutorial.gameObject,"V3Telemetry").gameObject);
            tutorialData.ApplyModifiedPropertiesWithoutUndo();
            var composition=Find(root,"HeaderContent");
            foreach(string name in new[]{"M03Actions","SkipCameraTour","ReturnWarningCamera","ReadWarning","RadarStatus"})
                foreach(var old in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name==name).ToArray())
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
            var threat=composition.Find("ThreatJumpPanel");
            if(threat.GetComponent<MatchHudThreatVisibilityView>()==null) threat.gameObject.AddComponent<MatchHudThreatVisibilityView>();
            var skipTour=Button("SkipCameraTour",composition,765,94,360,54,"mission.m03.camera.skip");
            // Header coordinates share the existing responsive HUD reference space.
            var actions=Rect("M03Actions",composition,660,181,540,95);
            var warningTitle=Find(composition.Find("ThreatJumpPanel").gameObject,"Title").GetComponent<TMP_Text>();
            warningTitle.enableAutoSizing=true; warningTitle.fontSizeMin=12; warningTitle.fontSizeMax=18;
            var guide=Button("FieldGuide",actions,0,0,172,42,"mission.m03.guide.open");
            var warning=Button("ReadWarning",actions,182,0,172,42,"mission.m03.action.warning");
            var skip=Button("SkipLesson",actions,364,0,176,42,"mission.m03.optional.skip");
            var status=Text("RadarStatus",actions,0,47,354,40,20,"");
            var returnCamera=Button("ReturnWarningCamera",actions,364,47,176,42,"mission.m03.camera.return");
            var support=Find(root,"SupportCommand"); var scan=Find(root,"ScanCommand");
            var supportText=support.GetComponentsInChildren<TMP_Text>(true).First();
            var obsolete=root.GetComponent<MissionDefenseHudView>(); if(obsolete!=null) UnityEngine.Object.DestroyImmediate(obsolete);
            var view=composition.GetComponent<MissionDefenseHudView>() ?? composition.gameObject.AddComponent<MissionDefenseHudView>();
            var data=new SerializedObject(view);
            Ref(data,"skipCameraTourButton",skipTour);
            Ref(data,"returnCameraButton",returnCamera);
            Ref(data,"actions",actions.gameObject); Ref(data,"guideButton",guide); Ref(data,"warningButton",warning); Ref(data,"skipButton",skip);
            Ref(data,"status",null); data.ApplyModifiedPropertiesWithoutUndo();
            var footer=Find(root,"FooterContent");
            var supportView=footer.GetComponent<MissionDefenseHudView>() ?? footer.gameObject.AddComponent<MissionDefenseHudView>();
            data=new SerializedObject(supportView);
            Ref(data,"supportButton",support.GetComponent<Button>()); Ref(data,"supportLabel",Binding(supportText)); Ref(data,"status",Binding(status));
            Ref(data,"supportIcon",Find(support.gameObject,"Icon").GetComponent<Image>()); Ref(data,"radarIcon",Find(scan.gameObject,"Icon").GetComponent<Image>().sprite);
            data.ApplyModifiedPropertiesWithoutUndo(); MissionSupportUiStyle.Defense(root); actions.gameObject.SetActive(false);
        });
        internal static void ConfigureThreat(GameObject root)
        {
            var view=root.GetComponent<ThreatAlertV3PopupView>(); var surface=view.AlertSurface.transform;
            var old=surface.Find("MissionWarningDetails"); if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var details=Text("MissionWarningDetails",surface,36,138,668,362,30,""); details.gameObject.SetActive(false);
            var data=new SerializedObject(view);
            var row=surface.Find("ButtonRow");
            var previousGuide=row.Find("WarningFieldGuide"); if(previousGuide!=null) UnityEngine.Object.DestroyImmediate(previousGuide.gameObject);
            var guide=Button("WarningFieldGuide",row,476,0,228,84,"mission.m03.guide.open"); guide.gameObject.SetActive(false);
            Ref(data,"missionGuideButton",guide);
            Ref(data,"missionJumpIcon",view.JumpToThreatButton.transform.Find("Icon").GetComponent<RectTransform>());
            Ref(data,"jumpLabel",view.JumpToThreatButton.transform.Find("LabelText").GetComponent<TMP_Text>());
            Ref(data,"authoredExampleBody",surface.Find("BodyRoot").gameObject);
            Ref(data,"missionWarningDetails",details); Ref(data,"detailsLocalization",Binding(details));
            data.ApplyModifiedPropertiesWithoutUndo();
            // Static labels remain compatible with the old alert; only M3's live detail body is substituted.
        }
        private static void Edit(string path,Action<GameObject> action)
        {var root=PrefabUtility.LoadPrefabContents(path); try {action(root); MissionUiSerializedBindingsAuthoring.Apply(root); PrefabUtility.SaveAsPrefabAsset(root,path);} finally {PrefabUtility.UnloadPrefabContents(root);}}
        private static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        private static void Ref(SerializedObject data,string name,UnityEngine.Object value)
        {var p=data.FindProperty(name) ?? throw new InvalidOperationException("Missing serialized binding "+name); p.objectReferenceValue=value;}
        private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(w,h); return rect;
        }
        private static RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
        {var rect=Rect(name,parent,x,y,w,h); var image=rect.gameObject.AddComponent<Image>(); image.color=color; return rect;}
        private static TextMeshProUGUI Text(string name,Transform parent,float x,float y,float w,float h,float size,string key)
        {
            var rect=Rect(name,parent,x,y,w,h); var text=rect.gameObject.AddComponent<RTLTextMeshPro>(); text.font=Font; text.fontSize=size;
            text.color=Color.white; text.raycastTarget=false; text.alignment=TextAlignmentOptions.TopLeft;
            text.enableAutoSizing=true; text.fontSizeMax=size; text.fontSizeMin=size-3; text.overflowMode=TextOverflowModes.Truncate;
            string fallback=M03RadarWarningGuideCopyCatalog.Entries.FirstOrDefault(e=>e.Key==key).English ?? key;
            text.text=fallback; Binding(text).Configure(key,fallback,false); return text;
        }
        private static V3LocalizedTextBindingView Binding(TMP_Text text)=>text.GetComponent<V3LocalizedTextBindingView>() ?? text.gameObject.AddComponent<V3LocalizedTextBindingView>();
        private static Button Button(string name,Transform parent,float x,float y,float w,float h,string key)
        {
            var rect=Panel(name,parent,x,y,w,h,Raised); var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>();
            var colors=button.colors; colors.highlightedColor=Cyan; colors.pressedColor=new Color(.35f,.7f,.8f); button.colors=colors;
            var text=Text("Label",rect,6,2,w-12,h-4,h<=54 ? 18 : 20,key); text.fontSizeMin=12; text.alignment=TextAlignmentOptions.Center; return button;
        }
    }
}
