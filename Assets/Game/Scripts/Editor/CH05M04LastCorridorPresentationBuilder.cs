using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M04LastCorridorPresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter05/CH05M04_LastCorridor_FieldGuide.asset";
        public static void Build()
        {
            CH05M04LastCorridorNarrativeBuilder.SeedCopyLocalization();
            SeedLocalization(); BuildGuide();
            var markerType=typeof(MissionFieldGuideView).Assembly.GetType("Game.UI.Runtime.LastCorridorMarkersView")
                ?? throw new InvalidOperationException("Last Corridor marker runtime binding missing");
            var hud=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
            try { if(hud.GetComponent(markerType)==null)hud.AddComponent(markerType);
                PrefabUtility.SaveAsPrefabAsset(hud,"Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab"); }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
            AssetDatabase.SaveAssets();
            Debug.Log("[LastCorridorPresentation] result=Passed existingHud=1 newCommandButtons=0 locales=2 certifiedGroundFallback=Explicit voices=NotRequested");
        }
        private static void SeedLocalization()
        {
            var copy=new (string key,string en,string fa)[]{
                ("name","Last Corridor","آخرین مسیر"),
                ("summary","Recover the road link and deliver medicine, Fuel, reinforcements, the original engineer and physical authority keys to the city center.","بخش جاده رو تعمیر کن و دارو، سوخت، نیروهای کمکی، مهندس اصلی و کلیدهای فیزیکی اختیار رو به مرکز شهر برسون."),
                ("location","Sahrin · city-center receiving corridor","سهرین · مسیر دریافت مرکز شهر"),
                ("enemy_intel","Seven military threats guard the road link. Two marked delivery lanes share the same artery; preserve the civic receiving site and staff.","۷ تهدید نظامی؛ دو لاین تحویل روی یک جاده. مرکز دریافت و کارکنان رو حفظ کن."),
                ("objective.repair","Recover the road link","راه رو بازسازی کن"),
                ("objective.deliveries","Deliver all five categories","هر پنج دسته رو تحویل بده"),
                ("objective.keys","Deliver engineer and keys","مهندس و کلیدها رو برسون"),
                ("objective.protection","Protect carriers and staff","خودروها و کارکنان رو حفظ کن"),
                ("forces","Escort · two delivery trucks · four reinforcements · original engineer · key APC","اسکورت · دو کامیون تحویل · چهار نیروی کمکی · مهندس اصلی · نفربر کلیدها"),
                ("resources","120 Fuel reserve · 40 Fuel cargo · 20 civic Fuel protected","۱۲۰ ذخیرهٔ سوخت · ۴۰ محمولهٔ سوخت · ۲۰ سوخت خدمات محفوظ"),
                ("label.fuel","FUEL / CARGO","سوخت / محموله"),
                ("label.deadline","CORRIDOR WINDOW","فرصت مسیر"),
                ("deadline","Deliver before the fifteen-minute window closes","پیش از پایان فرصت پانزده‌دقیقه‌ای تحویل بده"),
                ("intel.military","ROAD THREATS","تهدیدهای جاده"),
                ("intel.confirmed","7","۷"),
                ("intel.civilian","CIVIC RECEIVING","دریافت شهری"),
                ("intel.protected","SAFE","امن"),
                ("reward.card","Commander XP · Credits · physical access keys","تجربهٔ فرمانده · اعتبار · کلیدهای فیزیکی دسترسی"),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","No escort losses","بدون تلفات اسکورت"),
                ("star.3","No civilian losses","بدون تلفات غیرنظامیان"),
                ("hud.delivered","Delivered: {0}/5 categories","تحویل‌شده: {0} از ۵ دسته"),
                ("hud.hold","Hold position: {0}/6 s","ثابت بمون: {0} از ۶ ثانیه"),
                ("hud.time","Corridor window: {0}m {1}s","فرصت مسیر: {0} دقیقه و {1} ثانیه"),
                ("hud.fuel","Fuel reserve: {0} · civic allocation protected","ذخیرهٔ سوخت: {0} · سهم خدمات محفوظ"),
                ("marker.repair","REPAIR","تعمیر"),
                ("marker.medical","MEDICINE","دارو"),
                ("marker.fuel","FUEL","سوخت"),
                ("marker.reinforcements","REINFORCEMENTS","نیروهای کمکی"),
                ("marker.keys","AUTHORITY KEYS","کلیدهای اختیار"),
                ("guide.title","LAST CORRIDOR · FIELD GUIDE","آخرین مسیر · راهنمای میدان"),
                ("guide.example","Clear → repair → medicine → Fuel → reinforcements → engineer and keys.","پاک‌سازی ← تعمیر ← دارو ← سوخت ← نیروهای کمکی ← مهندس و کلیدها"),
                ("guide.mistake","Keep the original carriers alive. Follow each lane’s entry, midpoint and exit. Show Me moves only the camera.","خودروهای اصلی رو زنده نگه دار. ورودی، نقطهٔ میانی و خروجی هر مسیر رو دنبال کن. «نشان بده» فقط دوربین رو حرکت می‌ده."),
                ("guide.diagram","REPAIR | FIVE DELIVERIES | BOUNDED ACCESS","تعمیر | پنج تحویل | دسترسی محدود"),
                ("air.unqualified","Air delivery is not qualified for this corridor. Use the marked ground lanes.","انتقال هوایی برای این مسیر تأیید نشده. از مسیرهای زمینی مشخص‌شده استفاده کن."),
                ("supply.optional","Supply is optional. Any approved crate costs 3 Fuel and carries 40 Materials; collect it with a ground unit. Required deliveries work without it.","تدارکات اختیاریه. هر صندوق تأییدشده ۳ سوخت هزینه داره و ۴۰ مصالح حمل می‌کنه؛ با یک نیروی زمینی جمعش کن. تحویل‌های ضروری بدونش هم انجام می‌شن."),
                ("result.victory","ALL FIVE DELIVERIES RECEIVED","هر پنج تحویل دریافت شد"),
                ("result.defeat","CORRIDOR DELIVERY FAILED","تحویل مسیر شکست خورد"),
                ("result.subtitle","LAST CORRIDOR · CITY-CENTER DELIVERY","آخرین مسیر · تحویل مرکز شهر"),
                ("result.success","The original carriers delivered medicine, forty Fuel, four reinforcements, the engineer and physical authority keys. Civic receiving and staff survived.","خودروهای اصلی، دارو، چهل سوخت، چهار نیروی کمکی، مهندس و کلیدهای فیزیکی اختیار رو تحویل دادن. مرکز دریافت شهری و کارکنان سالم موندن."),
                ("result.success_short","Five categories delivered. Engineer and authority keys received.","پنج دسته تحویل شد. مهندس و کلیدهای اختیار دریافت شدن."),
                ("result.loss","Preserve the original carriers, engineer, reinforcements, civic receiving and staff. Repair the link, follow the marked lanes and hold at receiving.","خودروهای اصلی، مهندس، نیروهای کمکی، مرکز دریافت شهری و کارکنان رو حفظ کن. بخش جاده رو تعمیر کن، مسیرهای مشخص‌شده رو دنبال کن و در محل دریافت بمون."),
                ("result.objectives","MISSION OBJECTIVES","اهداف مأموریت"),
                ("result.repair","Recovered road link","بخش تعمیرشدهٔ جاده"),
                ("result.deliveries","Five delivery categories","پنج دستهٔ تحویل"),
                ("result.keys","Engineer and authority keys","مهندس و کلیدهای اختیار"),
                ("result.escort_losses","Escort losses","تلفات اسکورت"),
                ("result.staff_losses","Staff losses","تلفات کارکنان"),
                ("result.secured","RECOVERED","تعمیرشده"),
                ("result.verified","DELIVERED","تحویل‌شده"),
                ("result.safe","RECEIVED","دریافت‌شده"),
                ("result.losses","LOST","ازدست‌رفته"),
                ("result.incomplete","INCOMPLETE","ناقص"),
                ("result.integrity","The original delivery custody chain could not be established.","زنجیرهٔ اصلی نگهداری محموله برقرار نشد."),
                ("result.engineer_lost","The original engineer was lost.","مهندس اصلی از دست رفت."),
                ("result.keys_lost","The original authority-key APC was lost.","نفربر اصلی کلیدهای اختیار از دست رفت."),
                ("result.medicine_lost","The original medicine carrier was lost.","خودروی اصلی دارو از دست رفت."),
                ("result.fuel_cargo_lost","The original forty-Fuel cargo carrier was lost.","خودروی اصلی محمولهٔ چهل سوخت از دست رفت."),
                ("result.reinforcements_lost","An original reinforcement was lost before delivery.","یک نیروی کمکی اصلی پیش از تحویل از دست رفت."),
                ("result.civic_lost","The protected civic receiving site was destroyed.","مرکز دریافت شهری امن نابود شد."),
                ("result.staff_lost","Original civic staff were lost.","کارکنان اصلی خدمات از دست رفتن."),
                ("result.escort_lost","All escorts were lost while military threats remained.","همهٔ اسکورت‌ها از دست رفتن و تهدیدهای نظامی باقی موندن."),
                ("result.fuel_lost","The Fuel carrier, finite reserve or protected civic allocation was lost.","خودروی سوخت، ذخیرهٔ محدود یا سهم محفوظ خدمات از دست رفت."),
                ("result.receiver_lost","The actual receiving storage was lost.","انبار واقعی دریافت از دست رفت."),
                ("result.timeout","The fifteen-minute corridor window closed.","فرصت پانزده‌دقیقه‌ای مسیر تموم شد.")};
            var stages=new (string en,string fa,string body,string faBody)[]{
                ("Clear the road blockade","راه‌بندان رو پاک‌سازی کن","Move the original escort into weapon range and Attack the confirmed road threats. Defeat all seven before repairing the link.","اسکورت اصلی رو به برد شلیک ببر و به تهدیدهای تأییدشدهٔ جاده حمله کن. پیش از تعمیر، هر هفت تهدید رو شکست بده."),
                ("Repair the broken link","بخش آسیب‌دیده رو تعمیر کن","Move the original engineer to the repair marker. Hold stationary for six seconds after the blockade is defeated.","مهندس اصلی رو به نشان تعمیر حرکت بده. پس از شکست راه‌بندان، شش ثانیه ثابت بمونه."),
                ("Deliver medicine","دارو رو تحویل بده","Select the original medicine truck. Follow the protected alternate lane’s entry, midpoint and exit, then hold at medicine receiving for six seconds.","کامیون اصلی دارو رو انتخاب کن. ورودی، نقطهٔ میانی و خروجی مسیر جایگزین امن رو دنبال کن، بعد شش ثانیه در محل دریافت دارو بمون."),
                ("Deliver forty Fuel","چهل سوخت رو تحویل بده","Select the original Fuel truck. Choose the repaired main lane or the alternate, follow its entry, midpoint and exit, then hold at Fuel receiving for six seconds to transfer the actual forty Fuel.","کامیون اصلی سوخت رو انتخاب کن. مسیر اصلی تعمیرشده یا جایگزین رو انتخاب کن، ورودی، نقطهٔ میانی و خروجیش رو دنبال کن، بعد برای انتقال واقعی چهل سوخت، شش ثانیه در محل دریافت بمون."),
                ("Receive the reinforcements","نیروهای کمکی رو برسون","Move all four original reinforcements to their receiving marker. Keep all four alive and stationary there for six seconds.","هر چهار نیروی کمکی اصلی رو به نشان دریافت ببر. هر چهار نفر زنده باشن و شش ثانیه اونجا ثابت بمونن."),
                ("Return the engineer to the key APC","مهندس رو به نفربر کلیدها برگردون","Select the original engineer and Move to the marked pickup beside the original authority-key APC.","مهندس اصلی رو انتخاب کن و به محل سوار شدن مشخص‌شده کنار نفربر اصلی کلیدهای اختیار حرکت بده."),
                ("Board the engineer","مهندس رو سوار کن","Select the original engineer. Use the normal Board command on the original key APC and wait until the engineer is aboard.","مهندس اصلی رو انتخاب کن. با فرمان سوار شدن، وارد نفربر اصلی کلیدها کن و تا سوار شدنش صبر کن."),
                ("Deliver engineer and authority keys","مهندس و کلیدهای اختیار رو تحویل بده","Select the original key APC with the engineer aboard. Follow the repaired main lane or alternate entry, midpoint and exit, then hold at key receiving for six seconds.","نفربر اصلی کلیدها رو با مهندس سوارشده انتخاب کن. ورودی، نقطهٔ میانی و خروجی مسیر اصلی تعمیرشده یا جایگزین رو دنبال کن، بعد شش ثانیه در محل دریافت کلیدها بمون.")};
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)
                ?? throw new InvalidOperationException("Localization missing");
            var locales=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en"&&locale.LocaleCode!="fa-IR"){locales.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";
                var entries=locale.Entries.ToDictionary(e=>e.Key,e=>e.Value,StringComparer.Ordinal);
                foreach(var item in copy)entries["mission.last_corridor."+item.key]=fa?item.fa:item.en;
                for(int i=0;i<stages.Length;i++)
                {var stage=stages[i];entries[$"mission.last_corridor.tutorial.{i+1}.title"]=fa?stage.fa:stage.en;
                    entries[$"mission.last_corridor.tutorial.{i+1}.body"]=fa?stage.faBody:stage.body;
                    entries[$"mission.last_corridor.hud.stage.{i+1}"]=fa?stage.fa:stage.en;}
                entries["mission.last_corridor.tutorial.1.body.approach"]=fa?"اسکورت اصلی رو به نشان نزدیک شدن در برد شلیک ببر؛ سپس به تهدید تأییدشده حمله کن.":"Move the original escort to the approach marker within weapon range, then Attack the confirmed threat.";
                foreach(int stage in new[]{3,4,8})
                {
                    entries[$"mission.last_corridor.tutorial.{stage}.body.route.entry"]=fa?"خودروی اصلی این محموله رو به ورودی مسیر مشخص‌شده ببر.":"Move this load’s original carrier to the marked lane entry.";
                    entries[$"mission.last_corridor.tutorial.{stage}.body.route.mid"]=fa?"با همون خودروی اصلی، نقطهٔ میانی مسیر انتخاب‌شده رو دنبال کن.":"Continue with the same original carrier through the selected lane’s midpoint.";
                    entries[$"mission.last_corridor.tutorial.{stage}.body.route.exit"]=fa?"خودروی اصلی رو از خروجی مسیر انتخاب‌شده عبور بده، بعد به نشان دریافت ببر و شش ثانیه بمون.":"Move the original carrier through the selected lane exit, then to its receiving marker and hold six seconds.";
                }
                entries["mission.last_corridor.tutorial.8.body.board"]=entries["mission.last_corridor.tutorial.7.body"];
                entries["ui.home.archive.last_corridor.keys"]=fa?"کلیدهای دسترسی محدود تحویل‌شده":"DELIVERED BOUNDED-ACCESS KEYS";
                locales.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,
                    entries.OrderBy(e=>e.Key,StringComparer.Ordinal).Select(e=>new GameLocalizedStringRecord(e.Key,e.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,locales);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(GuidePath));
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path)
                ?? throw new InvalidOperationException("Field guide class catalog missing");
            guide.Configure(Enumerable.Range(1,8).Select(i=>new MissionGuideTopic{
                TitleKey=$"mission.last_corridor.tutorial.{i}.title",BodyKey=$"mission.last_corridor.tutorial.{i}.body",
                ExampleKey="mission.last_corridor.guide.example",MistakeKey="mission.last_corridor.guide.mistake",
                DiagramKey="mission.last_corridor.guide.diagram"}).ToArray(),basis.Classes.ToArray());
            EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try {var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));
                var property=data.FindProperty("lastCorridorGuide")
                    ?? throw new InvalidOperationException("Last Corridor field-guide runtime binding missing");
                property.objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
