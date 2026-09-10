using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using Game.Catalog.Contracts;
using TMPro;
using RTLTMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    public static class M04AirliftPresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter01/M04_Airlift_FieldGuide.asset";
        public static void Build()
        {
            ImportCopy();BuildGuide();BuildHud();InstallSpeaker();
            Edit(M03RadarWarningUiBuilder.GuidePath,root=>{var data=new SerializedObject(root.GetComponent<MissionFieldGuideView>()??root.GetComponentInChildren<MissionFieldGuideView>(true));Ref(data,"extractionGuide",AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath));data.ApplyModifiedPropertiesWithoutUndo();});
            var art=AssetDatabase.LoadAssetAtPath<Texture>(M04AirliftMediaImporter.ArtRoot+"/M04-B01.png");
            Edit("Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab",root=>{var data=new SerializedObject(root.GetComponentInChildren<CampaignOperationsScreenView>(true));Ref(data,"m04MissionPreview",art);data.ApplyModifiedPropertiesWithoutUndo();});
            Edit("Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab",root=>{var data=new SerializedObject(root.GetComponentInChildren<MissionBriefingScreenView>(true));Ref(data,"m04MissionArt",art);data.ApplyModifiedPropertiesWithoutUndo();});
            Edit(MissionResultV3PrefabBuilder.PrefabPath,root=>{var data=new SerializedObject(root.GetComponent<MissionResultPopupView>());Ref(data,"m04ResultBackdrop",AssetDatabase.LoadAssetAtPath<Texture>(M04AirliftMediaImporter.ArtRoot+"/M04-B01.png"));data.ApplyModifiedPropertiesWithoutUndo();});
            MatchHudResourceTextRepair.Build();AssetDatabase.SaveAssets();Debug.Log("[M04AirliftPresentation] result=Passed locales=2 lessons=12 guideClasses=57 hud=bound speaker=Laila");
        }
        private static void ImportCopy()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);var tables=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en"&&locale.LocaleCode!="fa-IR"){tables.Add(locale);continue;}bool fa=locale.LocaleCode=="fa-IR";
                var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var copy in M04AirliftCopyCatalog.Ui)entries[copy.Key]=fa?copy.Persian:copy.English;
                foreach(var line in M04AirliftMediaImporter.Lines)entries[line.Key]=fa?line.Persian:line.English;
                for(int i=0;i<12;i++){var l=M04AirliftCopyCatalog.Lessons[i];string key="mission.m04.tutorial."+(i+1)+".";
                    entries[key+"title"]=fa?l.PersianTitle:l.Title;entries[key+"body"]=fa?l.PersianBody:l.Body;
                    entries[key+"example"]=fa?l.PersianExample:l.Example;entries[key+"mistake"]=fa?l.PersianMistake:l.Mistake;entries[key+"diagram"]=fa?"تیم ← نفربر ← محل فرود ← بالگرد ← خروج":"TEAM → APC → LANDING AREA → HELICOPTER → EXIT";}
                tables.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,
                    entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,tables);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            var source=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);if(source==null)throw new InvalidOperationException("M03 class inventory missing");
            var classes=source.Classes.ToArray();for(int i=0;i<classes.Length;i++)
            {
                string id=classes[i].Id;if(classes[i].Availability==MissionGuideAvailability.Unavailable)continue;
                classes[i].Availability=id.StartsWith("Unit_Chr_Civilian_",StringComparison.Ordinal)?MissionGuideAvailability.Protected:
                    id is "Unit_Veh_APC_Fast" or "Unit_Veh_Helicopter_Transport" or "Unit_Chr_Soldier_Male_02_Alt_02" or "Unit_Chr_Soldier_Male_02_Alt_04" or "Unit_Chr_Soldier_Female_01_Alt_01" or "Unit_Chr_Soldier_Female_02_Alt_01"?MissionGuideAvailability.Friendly:
                    id is "Unit_Chr_Insurgent_Male_03" or "Unit_Chr_Insurgent_Female_01" or "Unit_Chr_Insurgent_Female_02"?MissionGuideAvailability.Hostile:MissionGuideAvailability.Reference;
            }
            var topics=Enumerable.Range(1,12).Select(i=>new MissionGuideTopic{TitleKey=Key(i,"title"),BodyKey=Key(i,"body"),ExampleKey=Key(i,"example"),MistakeKey=Key(i,"mistake"),DiagramKey=Key(i,"diagram")}).ToArray();
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}guide.Configure(topics,classes);EditorUtility.SetDirty(guide);
        }
        private static string Key(int i,string field)=>"mission.m04.tutorial."+i+"."+field;
        private static void BuildHud()=>Edit("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab",root=>
        {
            var parent=root.GetComponentsInChildren<Transform>(true).First(x=>x.name=="HeaderContent");var old=parent.Find("M04Actions");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var actions=Rect("M04Actions",parent,520,181,640,126);
            var background=actions.gameObject.AddComponent<Image>();background.color=new Color(0.04f,.09f,.12f,.88f);background.raycastTarget=false;
            var guide=Button("Guide",actions,0,172,"mission.m03.guide.open");var team=Button("Team",actions,182,110,"mission.m04.focus.team");var landing=Button("Landing",actions,302,172,"mission.m04.focus.landing");var departure=Button("Departure",actions,484,130,"mission.m04.focus.departure");
            var status=Text("ExtractionStatus",actions,0,47,614,76,19,"");
            var view=parent.GetComponent<MissionExtractionHudView>()??parent.gameObject.AddComponent<MissionExtractionHudView>();var data=new SerializedObject(view);
            Ref(data,"actions",actions.gameObject);Ref(data,"guide",guide);Ref(data,"team",team);Ref(data,"landing",landing);Ref(data,"departure",departure);Ref(data,"status",status.GetComponent<V3LocalizedTextBindingView>());data.ApplyModifiedPropertiesWithoutUndo();actions.gameObject.SetActive(false);
        });
        private static void InstallSpeaker()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>("Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchSpeakers.asset");var data=new SerializedObject(catalog);var entries=data.FindProperty("speakers");SerializedProperty entry=null;
            for(int i=0;i<entries.arraySize;i++)if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("speakerId").intValue==(int)NarrativeSpeakerId.Laila)entry=entries.GetArrayElementAtIndex(i);
            if(entry==null)entry=entries.GetArrayElementAtIndex(entries.arraySize++);
            entry.FindPropertyRelative("speakerId").intValue=(int)NarrativeSpeakerId.Laila;
            foreach(var pair in new[]{("name","Captain Laila Nasser"),("role","Airlift pilot"),("accessibleLabel","Captain Laila Nasser, airlift pilot")})
            {entry.FindPropertyRelative(pair.Item1+"Key").stringValue="narrative.speaker.laila."+(pair.Item1=="accessibleLabel"?"name":pair.Item1);entry.FindPropertyRelative(pair.Item1+"Fallback").stringValue=pair.Item2;}
            entry.FindPropertyRelative("treatment").intValue=(int)NarrativeSpeakerTreatment.HumanPortrait;
            entry.FindPropertyRelative("identitySprite").objectReferenceValue=M04AirliftMediaImporter.LailaPortrait();entry.FindPropertyRelative("accentColor").colorValue=new Color(.91f,.66f,.27f);
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(catalog);
        }
        private static void Edit(string path,Action<GameObject> action){var root=PrefabUtility.LoadPrefabContents(path);try{action(root);MissionUiSerializedBindingsAuthoring.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        private static void Ref(SerializedObject data,string name,UnityEngine.Object value)=>data.FindProperty(name).objectReferenceValue=value;
        private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;}
        private static TMP_Text Text(string name,Transform parent,float x,float y,float w,float h,float size,string key){var rect=Rect(name,parent,x,y,w,h);var text=rect.gameObject.AddComponent<RTLTextMeshPro>();text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset");text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=12;text.fontSizeMax=size;text.color=Color.white;text.raycastTarget=false;text.alignment=TextAlignmentOptions.Center;text.gameObject.AddComponent<V3LocalizedTextBindingView>().Configure(key,GameText.Get(key),false);return text;}
        private static Button Button(string name,Transform parent,float x,float w,string key){var rect=Rect(name,parent,x,0,w,42);var image=rect.gameObject.AddComponent<Image>();image.color=new Color32(29,48,57,255);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;Text("Label",rect,6,2,w-12,38,18,key);return button;}
    }
}
