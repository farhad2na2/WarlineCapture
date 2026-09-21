using System;
using System.Collections.Generic;
using System.Linq;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    public static class CH02M01GridlockPresentationBuilder
    {
        public static void Build()
        {
            ImportCopy();BuildGuide();
            const string path="Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view=root.GetComponentInChildren<CampaignOperationsScreenView>(true);var data=new SerializedObject(view);
                data.FindProperty("gridlockMissionPreview").objectReferenceValue=GridlockPreview();
                var chapterOne=view.ChapterCards[0];var chapterTwo=view.ChapterCards[1];
                var one=BindButton(chapterOne);var two=BindButton(chapterTwo);
                data.FindProperty("chapterOneButton").objectReferenceValue=one;data.FindProperty("chapterTwoButton").objectReferenceValue=two;
                var overview=Find(root.transform,"ChapterII");var overviewButton=BindButton(overview);
                data.FindProperty("chapterTwoOverviewButton").objectReferenceValue=overviewButton;
                data.FindProperty("chapterTwoStatus").objectReferenceValue=overview.Find("Subtitle").GetComponent<TMP_Text>();
                data.FindProperty("chapterTwoLock").objectReferenceValue=overview.Find("Lock").gameObject;
                var names=root.GetComponentsInChildren<TMP_Text>(true).Where(x=>x.name=="Name" && x.transform.parent.name=="MissionLabel").ToArray();
                var nameBindings=data.FindProperty("chapterMissionNames");nameBindings.arraySize=names.Length;
                for(int i=0;i<names.Length;i++) nameBindings.GetArrayElementAtIndex(i).objectReferenceValue=names[i];
                var railLock=chapterTwo.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name=="Lock" || x.name=="LockIcon");
                data.FindProperty("chapterTwoRailLock").objectReferenceValue=railLock!=null?railLock.gameObject:null;
                data.ApplyModifiedPropertiesWithoutUndo();MissionUiSerializedBindingsAuthoring.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            const string briefingPath="Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab";
            var briefing=PrefabUtility.LoadPrefabContents(briefingPath);
            try
            {
                var data=new SerializedObject(briefing.GetComponentInChildren<MissionBriefingScreenView>(true));
                data.FindProperty("gridlockMissionArt").objectReferenceValue=GridlockPreview();
                data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(briefing,briefingPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(briefing);}
            AssetDatabase.SaveAssets();Debug.Log("[GridlockPresentation] result=Passed locales=en,fa-IR chapterButtons=3");
        }
        private static Texture GridlockPreview()=>AssetDatabase.LoadAssetAtPath<Texture>(CH02M01GridlockMediaImporter.ArtRoot+"/CH02-B01.png")
            ?? throw new InvalidOperationException("Gridlock campaign preview art is missing.");
        private static void BuildGuide()
        {
            const string path="Assets/Game/Configs/Missions/Chapter02/CH02M01_Gridlock_FieldGuide.asset";
            var source=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            var classes=source.Classes.ToArray();
            for(int i=0;i<classes.Length;i++)
                if(classes[i].Availability!=MissionGuideAvailability.Unavailable)classes[i].Availability=MissionGuideAvailability.Reference;
            var topics=Enumerable.Range(1,10).Select(i=>new MissionGuideTopic {TitleKey=GuideKey(i,"title"),BodyKey=GuideKey(i,"body"),ExampleKey=GuideKey(i,"example"),MistakeKey=GuideKey(i,"mistake"),DiagramKey=GuideKey(i,"diagram")}).ToArray();
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(path);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,path);}
            guide.Configure(topics,classes);EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try
            {
                var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));
                data.FindProperty("gridlockGuide").objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();
                MissionUiSerializedBindingsAuthoring.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static string GuideKey(int i,string suffix)=>"mission.gridlock.tutorial."+i+"."+suffix;
        private static Transform Find(Transform root,string name)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) if(t.name==name) return t;
            throw new InvalidOperationException("Gridlock UI binding missing: "+name);
        }
        private static Button BindButton(Transform row)
        {
            var image=row.GetComponent<Graphic>()??row.gameObject.AddComponent<Image>();image.raycastTarget=true;
            var button=row.GetComponent<Button>()??row.gameObject.AddComponent<Button>();button.targetGraphic=image;return button;
        }
        public static void ImportCopy()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            var tables=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en" && locale.LocaleCode!="fa-IR"){tables.Add(locale);continue;}
                var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var copy in CH02M01GridlockCopyCatalog.Ui) entries[copy.Key]=locale.LocaleCode=="fa-IR"?copy.Persian:copy.English;
                foreach(var copy in CH02M01GridlockNarrativeCopy.Comms) entries[copy.Key]=locale.LocaleCode=="fa-IR"?copy.Persian:copy.English;
                for(int i=0;i<CH02M01GridlockTutorialCopy.Lessons.Length;i++)
                {
                    var lesson=CH02M01GridlockTutorialCopy.Lessons[i];string prefix="mission.gridlock.tutorial."+(i+1)+".";
                    entries[prefix+"title"]=locale.LocaleCode=="fa-IR"?lesson.PersianTitle:lesson.EnglishTitle;
                    entries[prefix+"body"]=locale.LocaleCode=="fa-IR"?lesson.Persian:lesson.English;
                    entries[prefix+"example"]=locale.LocaleCode=="fa-IR"?"اگه دستور اشتباه دادی، توقف کن، واحد درست رو دوباره انتخاب کن و «حرکت» رو بزن.":"After a mistaken order, press Stop, reselect the intended unit, then press Move.";
                    entries[prefix+"mistake"]=locale.LocaleCode=="fa-IR"?"فادی و کارگرها مسلح نیستن. قبل از جلو بردن اون‌ها، مسیر رو با تفنگدارها امن کن.":"Fadi and the workers are unarmed. Secure their approach with the rifles first.";
                    entries[prefix+"diagram"]=locale.LocaleCode=="fa-IR"?"محل اول ← محل دوم ← کامیون امداد ← بیمارستان":"SITE A → SITE B → RELIEF TRUCK → HOSPITAL";
                }
                tables.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,
                    entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,tables);EditorUtility.SetDirty(catalog);
        }
    }
}
