using System;
using System.Collections.Generic;
using System.Linq;
using Game.Catalog.Contracts;
using Game.Configs;
using UnityEditor;
using UnityEngine;
using Game.UI.Runtime;
using UnityEngine.UI;
namespace Game.Editor
{
    public static class CH02M02SupplyLinePresentationBuilder
    {
        public static void Build()
        {
            var copy=new (string key,string en,string fa)[]{
                ("guide.1.title","Protect Oil extraction","از استخراج نفت محافظت کن"),
                ("guide.1.body","Move rifles beside the Oil pump. The tray truck carries Oil automatically; check its load and route if production stalls.","تفنگدارها رو کنار پمپ نفت ببر. کامیون کفی خودکار نفت رو می‌بره؛ اگه تولید متوقف شد، بار و مسیرش رو بررسی کن."),
                ("guide.2.title","Protect refinery output","از خروجی پالایشگاه محافظت کن"),
                ("guide.2.body","Keep the refinery and both trucks safe. Oil becomes Fuel here; the tanker delivers it to the warehouse.","از پالایشگاه و هر دو کامیون محافظت کن. اینجا نفت به سوخت تبدیل می‌شه؛ تانکر اون رو به انبار می‌رسونه."),
                ("guide.3.title","Secure the reserve depot","انبار ذخیره رو امن کن"),
                ("guide.3.body","Defend the warehouse while the tanker unloads. Keep 20 civilian barrels and 20 relief barrels in storage.","تا تانکر بارش رو خالی می‌کنه از انبار دفاع کن. ۲۰ بشکهٔ غیرنظامی و ۲۰ بشکهٔ امداد رو در انبار نگه دار."),
                ("guide.4.title","Hold the supply line","خط تدارکات رو نگه دار"),
                ("guide.4.body","Defeat the sabotage teams and keep all links intact. Hold the full reserve for twenty seconds.","خرابکارها رو شکست بده و همهٔ حلقه‌ها رو سالم نگه دار. ذخیرهٔ کامل رو بیست ثانیه حفظ کن."),
                ("guide.recovery.title","Use the southern haul lane","از مسیر باربری جنوبی برو"),
                ("guide.recovery.body","Select the Oil truck, press Move and send it to the marked southern road. Automatic hauling resumes after arrival. Defend this alternate lane around the ruined block.","کامیون نفت رو انتخاب کن، حرکت رو بزن و به جادهٔ جنوبیِ مشخص‌شده بفرست. بعد از رسیدن، باربری خودکار ادامه پیدا می‌کنه. از این مسیر جایگزین کنار بلوک ویران‌شده محافظت کن."),
                ("reserve.allocate","Reserve 20 civilian Fuel","۲۰ بشکه برای مردم ذخیره کن"),
                ("reserve.allocated","Civilian reserve protected","ذخیرهٔ مردم محافظت می‌شه"),
                ("reserve.status","Fuel: {0}/40 | Civilian reserve: {1}/20","سوخت: {0}/۴۰ | ذخیرهٔ مردم: {1}/۲۰"),
                ("guide.title","Supply Line field guide","راهنمای خط تدارکات"),
                ("guide.example","Select rifles, press Move, then tap the threatened site. Trucks haul automatically. At 20 stored barrels, press Reserve civilian Fuel.","تفنگدارها رو انتخاب کن، حرکت رو بزن و روی محل در خطر بزن. کامیون‌ها خودکار بار می‌برن. وقتی ذخیره به ۲۰ بشکه رسید، دکمهٔ ذخیرهٔ مردم رو بزن."),
                ("guide.mistake","Do not send unarmed trucks into the sabotage teams. A stopped truck may be waiting for a full load; inspect its status before changing orders.","کامیون‌های بی‌سلاح رو به سمت خرابکارها نفرست. ممکنه کامیون متوقف‌شده منتظر بار کامل باشه؛ قبل از عوض کردن دستور، وضعیتش رو بررسی کن."),
                ("guide.diagram","OIL PUMP → TRAY TRUCK → REFINERY → TANKER → RESERVE","پمپ نفت ← کامیون کفی ← پالایشگاه ← تانکر ← ذخیره"),
                ("result.victory","Supply line restored","خط تدارکات برقرار شد"),
                ("result.defeat","Supply line lost","خط تدارکات از دست رفت"),
                ("result.success","Fuel is secured for clinics, water pumps and relief transport. The stolen manifest points toward the dormant Relay corridor.","سوخت درمانگاه‌ها، پمپ‌های آب و ترابری امداد امنه. بارنامهٔ دزدیده‌شده به مسیر متروک شبکه اشاره می‌کنه."),
                ("failure.squad","The defending rifle squads were lost. Retry with both sites covered.","تفنگدارهای مدافع از دست رفتن. دوباره تلاش کن و از هر دو محل محافظت کن."),
                ("failure.hauler","A required hauler was destroyed. Both trucks must survive.","یکی از کامیون‌های ضروری نابود شد. هر دو کامیون باید زنده بمونن."),
                ("failure.link","A critical supply building was destroyed. Protect the pump, refinery and reserve depot.","یکی از ساختمان‌های ضروری نابود شد. از پمپ، پالایشگاه و انبار ذخیره محافظت کن."),
                ("failure.deadline","The reserve was not secured within twelve minutes.","ذخیره تا پایان دوازده دقیقه امن نشد."),
                ("failure.integrity","The supply chain could not initialize safely. Exit and retry; no result or reward has been recorded.","زنجیرهٔ تدارکات درست راه‌اندازی نشد. خارج شو و دوباره تلاش کن؛ نتیجه یا پاداشی ثبت نشده."),
                ("objective.reserve.body","Reroute the Oil truck through the southern lane, allocate 20 civilian barrels, keep 40 in storage and hold the intact chain for 20 seconds after defeating the attackers.","۲۰ بشکه برای مردم کنار بذار، ۴۰ بشکه در انبار نگه دار و بعد از شکست مهاجم‌ها، زنجیرهٔ سالم رو ۲۰ ثانیه حفظ کن."),
                ("name","Supply Line","خط تدارکات"),
                ("summary","Restore the Oil-to-Fuel chain and protect the reserve for clinics, water pumps and JRC relief transport.","زنجیرهٔ نفت تا سوخت رو راه بنداز و از ذخیرهٔ درمانگاه‌ها، پمپ‌های آب و ترابری امداد محافظت کن."),
                ("location","Industrial supply yard","محوطهٔ تدارکات صنعتی"),
                ("enemy_intel","Ash Line sabotage teams are targeting the supply chain.","تیم‌های خرابکار خط خاکستر زنجیرهٔ تدارکات رو هدف گرفتن."),
                ("objective.oil","Supply Oil to the refinery","نفت رو به پالایشگاه برسون"),
                ("objective.fuel","Deliver refined Fuel to storage","سوخت تولیدشده رو به انبار برسون"),
                ("objective.reserve","Protect 40 barrels and hold for 20 seconds","از ۴۰ بشکه محافظت کن و ۲۰ ثانیه نگه دار"),
                ("objective.oil.body","The tray truck automatically hauls Oil from the pump to the refinery.","کامیون کفی خودکار نفت رو از پمپ به پالایشگاه می‌بره."),
                ("objective.fuel.body","Redirect the Oil truck through the marked southern lane. The tanker automatically carries refinery output to the reserve depot.","کامیون نفتِ خالی رو از مسیر جنوبیِ مشخص‌شده بفرست. تانکر خودکار سوخت پالایشگاه رو به انبار ذخیره می‌رسونه."),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","Lose no rifle soldiers","هیچ تفنگداری رو از دست نده"),
                ("star.3","Finish within 8 minutes","در کمتر از ۸ دقیقه تمام کن"),
                ("resources","20 civilian + 20 relief Fuel barrels","۲۰ بشکهٔ غیرنظامی + ۲۰ بشکهٔ امداد"),
                ("forces","8 rifles · Oil truck · Fuel tanker","۸ تفنگدار · کامیون نفت · تانکر سوخت"),
                ("deadline","12 minutes","۱۲ دقیقه"),
                ("time_limit","Time limit","مهلت"),
                ("reward.card","800 Commander XP · 4,000 Credits","۸۰۰ تجربهٔ فرمانده · ۴۰۰۰ اعتبار")};
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            var tables=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en" && locale.LocaleCode!="fa-IR"){tables.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var line in copy)entries["mission.supply_line."+line.key]=fa?line.fa:line.en;
                foreach(var line in CH02M02SupplyLineCopy.Comms)entries[line.Key]=fa?line.Persian:line.English;
                tables.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,tables);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            BuildControlsAndGuide();BuildPreviewBindings();
            Debug.Log("[SupplyLinePresentation] result=Passed scope=CaptionedCopy locales=en,fa-IR");
        }
        private static void BuildControlsAndGuide()
        {
            const string hud="Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
            var root=PrefabUtility.LoadPrefabContents(hud);
            try
            {
                foreach(var view in root.GetComponentsInChildren<MissionDefenseHudView>(true))
                {
                    var data=new SerializedObject(view);var warning=data.FindProperty("warningButton").objectReferenceValue as Button;
                    if(warning==null)continue;
                    var parent=data.FindProperty("actions").objectReferenceValue as GameObject;
                    if(parent==null)throw new InvalidOperationException("Mission action area missing.");
                    var obsolete=warning.transform.parent.Find("SupplyLineReserve");if(obsolete!=null)UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
                    var existing=parent.transform.Find("SupplyLineReserve");
                    var button=existing!=null?existing.GetComponent<Button>():UnityEngine.Object.Instantiate(warning,parent.transform);
                    button.name="SupplyLineReserve";button.onClick=new Button.ButtonClickedEvent();
                    var surface=button.GetComponent<Image>()??button.gameObject.AddComponent<Image>();surface.color=new Color(.04f,.35f,.43f,1f);surface.enabled=true;surface.raycastTarget=true;button.targetGraphic=surface;
                    foreach(Transform child in button.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                    var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(0,-80);rect.sizeDelta=new Vector2(360,56);
                    var textObject=new GameObject("Label",typeof(RectTransform),typeof(RTLTMPro.RTLTextMeshPro));textObject.transform.SetParent(button.transform,false);
                    var text=textObject.GetComponent<RTLTMPro.RTLTextMeshPro>();
                    text.font=root.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.font!=null).font;
                    text.text="Reserve civilian Fuel";text.color=Color.white;text.raycastTarget=false;text.alignment=TMPro.TextAlignmentOptions.Center;
                    text.enableAutoSizing=true;text.fontSizeMin=12;text.fontSizeMax=18;
                    text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=new Vector2(6,2);text.rectTransform.offsetMax=new Vector2(-6,-2);
                    var label=textObject.AddComponent<V3LocalizedTextBindingView>();label.Configure("mission.supply_line.reserve.allocate","Reserve civilian Fuel",false);
                    data.FindProperty("supplyReserveButton").objectReferenceValue=button;data.FindProperty("supplyReserveLabel").objectReferenceValue=label;
                    var existingStatus=parent.transform.Find("SupplyLineReserveStatus");if(existingStatus!=null)UnityEngine.Object.DestroyImmediate(existingStatus.gameObject);
                    var status=UnityEngine.Object.Instantiate(text,parent.transform);status.name="SupplyLineReserveStatus";
                    status.rectTransform.anchorMin=status.rectTransform.anchorMax=status.rectTransform.pivot=new Vector2(0,1);
                    status.rectTransform.anchoredPosition=new Vector2(0,-140);status.rectTransform.sizeDelta=new Vector2(360,40);
                    status.fontSizeMax=16;status.text="Fuel: 0/40 | Civilian reserve: 0/20";
                    var statusBinding=status.GetComponent<V3LocalizedTextBindingView>();statusBinding.Configure("",status.text,true);
                    data.FindProperty("supplyReserveStatus").objectReferenceValue=statusBinding;
                    data.ApplyModifiedPropertiesWithoutUndo();button.gameObject.SetActive(false);status.gameObject.SetActive(false);
                }
                MissionUiSerializedBindingsAuthoring.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,hud);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            const string path="Assets/Game/Configs/Missions/Chapter02/CH02M02_SupplyLine_FieldGuide.asset";
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(path);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,path);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);var classes=basis.Classes.ToArray();
            for(int i=0;i<classes.Length;i++)if(classes[i].Availability!=MissionGuideAvailability.Unavailable)classes[i].Availability=MissionGuideAvailability.Reference;
            guide.Configure(Enumerable.Range(1,4).Select(i=>new MissionGuideTopic {TitleKey="mission.supply_line.guide."+(i==2?"recovery":i.ToString())+".title",BodyKey="mission.supply_line.guide."+(i==2?"recovery":i.ToString())+".body",ExampleKey="mission.supply_line.guide.example",MistakeKey="mission.supply_line.guide.mistake",DiagramKey="mission.supply_line.guide.diagram"}).ToArray(),classes);
            EditorUtility.SetDirty(guide);
            root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try
            {
                var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));data.FindProperty("supplyLineGuide").objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();
                MissionUiSerializedBindingsAuthoring.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
        }
        private static void BuildPreviewBindings()
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture>(CH02M02SupplyLineMediaImporter.ArtRoot+"/SupplyChain.png")??throw new InvalidOperationException("Supply Line preview is missing.");
            foreach(var path in new[]{"Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab","Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab"})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var campaign=root.GetComponentInChildren<CampaignOperationsScreenView>(true);
                    UnityEngine.Object view=campaign!=null?(UnityEngine.Object)campaign:root.GetComponentInChildren<MissionBriefingScreenView>(true);
                    var data=new SerializedObject(view);data.FindProperty(campaign!=null?"supplyLineMissionPreview":"supplyLineMissionArt").objectReferenceValue=texture;
                    data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();
        }
        public static void BuildCheckpoint()
        {
            CH02M02SupplyLineRulesValidation.Run();CH02M02SupplyLineConfigBuilder.Build();Build();CH02M02SupplyLineNarrativeBuilder.BuildCaptionedArtAndInstall();
            Debug.Log("[SupplyLineCheckpoint] result=Passed scope=CaptionedConfiguration gameplay=Pending");
        }
    }
}
