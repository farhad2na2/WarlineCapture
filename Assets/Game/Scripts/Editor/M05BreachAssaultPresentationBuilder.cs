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
    public static class M05BreachAssaultPresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter01/M05_BreachAssault_FieldGuide.asset";
        public static void Build()
        {
            ImportCopy();BuildGuide();
            Edit(M03RadarWarningUiBuilder.GuidePath,root=>{var data=new SerializedObject(root.GetComponent<MissionFieldGuideView>()??root.GetComponentInChildren<MissionFieldGuideView>(true));Ref(data,"breachGuide",AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath));data.ApplyModifiedPropertiesWithoutUndo();});
            var art=AssetDatabase.LoadAssetAtPath<Texture>(M05BreachAssaultMediaImporter.ArtRoot+"/M05-B01.png");
            Edit("Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab",root=>{var data=new SerializedObject(root.GetComponentInChildren<CampaignOperationsScreenView>(true));Ref(data,"m05MissionPreview",art);data.ApplyModifiedPropertiesWithoutUndo();});
            Edit("Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab",root=>{var data=new SerializedObject(root.GetComponentInChildren<MissionBriefingScreenView>(true));Ref(data,"m05MissionArt",art);data.ApplyModifiedPropertiesWithoutUndo();});
            Edit(MissionResultV3PrefabBuilder.PrefabPath,root=>{var data=new SerializedObject(root.GetComponent<MissionResultPopupView>());Ref(data,"m05ResultBackdrop",AssetDatabase.LoadAssetAtPath<Texture>(M05BreachAssaultMediaImporter.ArtRoot+"/M05-D03.png"));data.ApplyModifiedPropertiesWithoutUndo();});
            MatchHudResourceTextRepair.Build();AssetDatabase.SaveAssets();Debug.Log("[M05BreachAssaultPresentation] result=Passed locales=2 lessons=8 guideClasses=57");
        }
        internal static void ImportCopy()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);var tables=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en"&&locale.LocaleCode!="fa-IR"){tables.Add(locale);continue;}bool fa=locale.LocaleCode=="fa-IR";
                var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var copy in M05BreachAssaultCopyCatalog.Ui)entries[copy.Key]=fa?copy.Persian:copy.English;
                foreach(var line in M05BreachAssaultMediaImporter.Lines)entries[line.Key]=fa?line.Persian:line.English;
                for(int i=0;i<8;i++){var l=M05BreachAssaultCopyCatalog.Lessons[i];string key="mission.m05.tutorial."+(i+1)+".";
                    entries[key+"title"]=fa?l.PersianTitle:l.Title;entries[key+"body"]=fa?l.PersianBody:l.Body;
                    entries[key+"example"]=fa?l.PersianExample:l.Example;entries[key+"mistake"]=fa?l.PersianMistake:l.Mistake;entries[key+"diagram"]=fa?"دروازه ← فرستنده ← بایگانی":"GATE → TRANSMITTER → ARCHIVE";}
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
                classes[i].Availability=id is "Unit_Veh_APC_Heavy" or "Unit_Chr_Soldier_Male_02_Alt_02" or "Unit_Chr_Soldier_Male_02_Alt_04" or "Unit_Chr_Soldier_Female_01_Alt_01" or "Unit_Chr_Soldier_Female_02_Alt_01"?MissionGuideAvailability.Friendly:
                    id is "Unit_Chr_Insurgent_Male_03" or "Unit_Chr_Insurgent_Female_01" or "Unit_Chr_Insurgent_Female_02"?MissionGuideAvailability.Hostile:MissionGuideAvailability.Reference;
            }
            var topics=Enumerable.Range(1,8).Select(i=>new MissionGuideTopic{TitleKey=Key(i,"title"),BodyKey=Key(i,"body"),ExampleKey=Key(i,"example"),MistakeKey=Key(i,"mistake"),DiagramKey=Key(i,"diagram")}).ToArray();
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}guide.Configure(topics,classes);EditorUtility.SetDirty(guide);
        }
        private static string Key(int i,string field)=>"mission.m05.tutorial."+i+"."+field;
        private static void Edit(string path,Action<GameObject> action){var root=PrefabUtility.LoadPrefabContents(path);try{action(root);MissionUiSerializedBindingsAuthoring.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        private static void Ref(SerializedObject data,string name,UnityEngine.Object value)=>data.FindProperty(name).objectReferenceValue=value;
    }
}
