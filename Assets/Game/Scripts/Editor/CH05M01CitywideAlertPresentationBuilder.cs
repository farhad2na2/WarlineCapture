using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M01CitywideAlertPresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter05/CH05M01_CitywideAlert_FieldGuide.asset";
        public static void Build()
        {
            CH05M01CitywideAlertNarrativeBuilder.SeedCopyLocalization();SeedLocalization();BuildGuide();
            var hud=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
            try { if(hud.GetComponent<CitywideAlertMarkersView>()==null)hud.AddComponent<CitywideAlertMarkersView>();
                PrefabUtility.SaveAsPrefabAsset(hud,"Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab"); }
            finally {PrefabUtility.UnloadPrefabContents(hud);}
            AssetDatabase.SaveAssets();Debug.Log("[CitywideAlertPresentation] result=Passed existingHud=1 newCommandButtons=0 locales=2 markers=approved-style");
        }
        private static void SeedLocalization()
        {
            var copy=new (string key,string en,string fa)[]{
                ("name","Citywide Alert","هشدار سراسری شهر"),
                ("summary","Protect the clinic and water/power district. Recruit reinforcements, respond to both attacks and restore blocked civic services.","از کلینیک و محلهٔ آب و برق محافظت کن. نیروی کمکی جذب کن، به هر دو حمله واکنش بده و خدمات مسدودشده رو بازیابی کن."),
                ("location","Sahrin · civic response districts","سهرین · محله‌های امداد شهری"),
                ("enemy_intel","Clinic saboteurs; utility armor and drones. Both attacks are warned.","خرابکارهای کلینیک؛ زرهی و پهپادهای خدمات. هر دو حمله هشدار دارن."),
                ("objective.clinic","Keep the clinic operational","کلینیک رو فعال نگه دار"),
                ("objective.utility","Keep water and power operational","آب و برق رو فعال نگه دار"),
                ("objective.perimeter","Establish the reinforcement perimeter","محدودهٔ نیروهای کمکی رو تثبیت کن"),
                ("resources","180 Materials · 200 Fuel · 40 civic Fuel protected","۱۸۰ مصالح · ۲۰۰ سوخت · ۴۰ سوخت خدمات محفوظ"),
                ("forces","Two response groups · two engineers · G2A · radar · two Barracks","دو گروه واکنش · دو مهندس · پدافند · رادار · دو پادگان"),
                ("deadline","Stabilize both districts within fifteen minutes","هر دو محله رو تا پانزده دقیقه تثبیت کن"),
                ("label.fuel","CIVIC FUEL RESERVE","ذخیرهٔ سوخت خدمات"),
                ("label.deadline","RESPONSE WINDOW","فرصت واکنش"),
                ("intel.heavy","GROUND THREAT","تهدید زمینی"),
                ("intel.present","WARNED","هشدار داده شد"),
                ("intel.air","AIR THREAT","تهدید هوایی"),
                ("intel.incoming","INCOMING","در راه"),
                ("warning.0","Clinic sabotage · protect staff and the engineer","خرابکاری کلینیک · از کارکنان و مهندس محافظت کن"),
                ("warning.1","Utility armor · protect water and power","زرهی در خدمات · آب و برق رو حفظ کن"),
                ("warning.2","Incoming drones · maintain radar coverage","پهپادهای ورودی · پوشش رادار رو حفظ کن"),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","No friendly combat losses","بدون تلفات نیروهای رزمی"),
                ("star.3","No civic staff losses","بدون تلفات کارکنان خدمات"),
                ("reward.card","Commander XP · Credits","تجربهٔ فرمانده · اعتبار"),
                ("result.victory","BOTH DISTRICTS SECURE","هر دو محله امن شدند"),
                ("result.defeat","CITY RESPONSE FAILED","واکنش شهری شکست خورد"),
                ("result.subtitle","CITYWIDE ALERT · CIVIC PERIMETER","هشدار سراسری شهر · محدودهٔ خدمات"),
                ("result.success","Both civic services remain operational. The original engineers and civic staff are safe, and recruited reinforcements have secured the perimeter.","هر دو مرکز خدمات فعال موندن. مهندس‌های اصلی و کارکنان خدمات سالم‌اند و نیروهای جذب‌شده محدوده رو امن کردن."),
                ("result.success_short","Both civic fronts operational. Reinforcement perimeter established.","هر دو جبههٔ خدمات فعال‌اند. محدودهٔ نیروهای کمکی تثبیت شد."),
                ("result.loss","Protect both civic services, engineers, staff, producers and emergency reserves, then retry.","از هر دو مرکز خدمات، مهندس‌ها، کارکنان، پادگان‌ها و ذخیره‌های اضطراری محافظت کن و دوباره تلاش کن."),
                ("result.combat_losses","COMBAT LOSSES","تلفات رزمی"),
                ("result.staff_losses","CIVIC STAFF LOSSES","تلفات کارکنان خدمات"),
                ("hud.recovery","Service recovery: {0}/6 s","بازیابی خدمات: {0} از ۶ ثانیه"),
                ("hud.clinic_recovery","Clinic recovery: {0}/6 s","بازیابی کلینیک: {0} از ۶ ثانیه"),
                ("hud.utility_recovery","Utility recovery: {0}/6 s","بازیابی خدمات: {0} از ۶ ثانیه"),
                ("hud.obstruction","Clear the obstruction: {0} s remaining","مانع رو پاک کن: {0} ثانیه باقی"),
                ("hud.clinic_obstruction","Clinic obstruction: {0} s remaining","مانع کلینیک: {0} ثانیه باقی"),
                ("hud.utility_obstruction","Utility obstruction: {0} s remaining","مانع خدمات: {0} ثانیه باقی"),
                ("hud.time","Response window: {0}","فرصت واکنش: {0}"),
                ("hud.hold_time","Stabilize: {0}/6 s · Time: {1}","تثبیت: {0} از ۶ ثانیه · زمان: {1}"),
                ("hud.fuel","Response Fuel: {0} · civic reserve protected","سوخت واکنش: {0} · ذخیرهٔ خدمات محفوظ"),
                ("hud.materials","Materials: {0} · recruit at either Barracks","مصالح: {0} · از یکی از پادگان‌ها نیرو جذب کن"),
                ("marker.clinic","CLINIC","کلینیک"),
                ("marker.utility","WATER / POWER","آب و برق"),
                ("marker.recovery","RECOVERY","بازیابی"),
                ("marker.coverage","COVERAGE","پوشش"),
                ("marker.perimeter","PERIMETER","محدوده"),
                ("guide.title","CITYWIDE ALERT · FIELD GUIDE","هشدار سراسری شهر · راهنمای میدان"),
                ("guide.example","Recruit → cover air → respond → recover → perimeter.","جذب نیرو ← پوشش هوایی ← واکنش ← بازیابی ← محدوده"),
                ("guide.mistake","Keep both original engineers alive. Clear hostile obstruction before each six-second recovery. Show Me only moves the camera.","هر دو مهندس اصلی رو زنده نگه دار. پیش از هر بازیابی شش‌ثانیه‌ای، مانع دشمن رو پاک کن. «نشان بده» فقط دوربین رو جابه‌جا می‌کنه."),
                ("guide.diagram","RECRUIT | RESPOND | RESTORE","جذب | واکنش | بازیابی"),
                ("result.integrity","The supplied roster or service ownership was lost. Retry from the campaign.","نیروهای آماده یا مالکیت خدمات از دست رفت. از صفحهٔ کارزار دوباره تلاش کن."),
                ("result.clinic_lost","The clinic was destroyed. Protect its service compound.","کلینیک نابود شد. از محوطهٔ خدمات محافظت کن."),
                ("result.utility_lost","Water and power were destroyed. Protect the service compound.","آب و برق نابود شدن. از محوطهٔ خدمات محافظت کن."),
                ("result.staff_lost","Civic staff were lost. Keep combat away from both staff groups.","کارکنان خدمات از دست رفتن. نبرد رو از هر دو گروه دور نگه دار."),
                ("result.engineer_lost","An original engineer was lost. Both are required for recovery.","مهندس اصلی از دست رفت. برای بازیابی به هر دو نیاز داری."),
                ("result.response_lost","A response group was lost before its front was secured.","گروه واکنش پیش از امن شدن جبهه از دست رفت."),
                ("result.fuel_reserve_lost","Emergency Fuel storage or the protected civic allocation was lost.","ذخیرهٔ اضطراری یا سهم سوخت خدمات از دست رفت."),
                ("result.producer_lost","A supplied Barracks was lost. Protect both production lots.","پادگان آماده از دست رفت. از هر دو محوطهٔ تولید محافظت کن."),
                ("result.service_outage","A front remained blocked for sixty seconds. Clear hostiles and bring its original engineer to the recovery marker.","جبهه شصت ثانیه مسدود موند. دشمن رو پاک کن و مهندس اصلی رو به نشان بازیابی برسون."),
                ("result.deadline","The fifteen-minute response window closed.","فرصت پانزده‌دقیقه‌ای تموم شد."),
                ("result.timeout","The response window closed.","فرصت واکنش تموم شد."),
                ("tutorial.1.title","Recruit reinforcements","نیروی کمکی جذب کن"),
                ("tutorial.1.body","Select either supplied Barracks. Recruit Rifleman Male IV using existing production controls. A paid production request supplies four riflemen; Materials are finite.","یکی از پادگان‌های آماده رو انتخاب کن. با کنترل‌های تولید، تیرانداز مرد چهار رو جذب کن. تولید پرداخت‌شده چهار تیرانداز می‌ده؛ مصالح محدوده."),
                ("hud.stage.1","Recruit reinforcements","نیروی کمکی جذب کن"),
                ("tutorial.2.title","Cover the air approach","مسیر هوایی رو پوشش بده"),
                ("tutorial.2.body","Select the G2A and Move to the coverage marker near the supplied radar. Protect both Fuel reserves.","پدافند رو انتخاب کن و به نشان پوشش کنار رادار حرکت بده. هر دو ذخیرهٔ سوخت رو حفظ کن."),
                ("hud.stage.2","Cover the air approach","مسیر هوایی رو پوشش بده"),
                ("tutorial.3.title","Respond at the clinic","به کلینیک واکنش نشون بده"),
                ("tutorial.3.body","Move the surviving response force into position at the clinic and Attack the verified saboteurs and APC. Protect the staff and original engineer.","نیروی واکنش باقی‌مونده رو کنار کلینیک مستقر کن و به خرابکارها و نفربر تأییدشده حمله کن. کارکنان و مهندس اصلی رو حفظ کن."),
                ("hud.stage.3","Respond at the clinic","به کلینیک واکنش نشون بده"),
                ("tutorial.4.title","Restore the clinic","کلینیک رو بازیابی کن"),
                ("tutorial.4.body","Clear hostile obstruction. Move the original clinic engineer to the recovery marker outside the building. Hold stationary and uncontested for six seconds.","مانع دشمن رو پاک کن. مهندس اصلی کلینیک رو به نشان بازیابی بیرون ساختمان حرکت بده. شش ثانیه ثابت و بدون دشمن بمونه."),
                ("hud.stage.4","Restore the clinic","کلینیک رو بازیابی کن"),
                ("tutorial.5.title","Respond at water and power","به آب و برق واکنش نشون بده"),
                ("tutorial.5.body","Attack the verified ground threats at water and power with the surviving response force. Keep the G2A covered while it intercepts the warned drones.","با نیروی واکنش باقی‌مونده به تهدیدهای زمینی تأییدشده کنار آب و برق حمله کن. پدافند رو حفظ کن تا پهپادهای هشدار داده‌شده رو رهگیری کنه."),
                ("hud.stage.5","Respond at water and power","به آب و برق واکنش نشون بده"),
                ("tutorial.6.title","Restore water and power","آب و برق رو بازیابی کن"),
                ("tutorial.6.body","Clear hostile obstruction. Move the original utility engineer to the recovery marker outside the compound. Hold for six uncontested seconds.","مانع دشمن رو پاک کن. مهندس اصلی خدمات رو به نشان بازیابی بیرون محوطه حرکت بده. شش ثانیه بدون دشمن بمونه."),
                ("hud.stage.6","Restore water and power","آب و برق رو بازیابی کن"),
                ("tutorial.7.title","Establish the perimeter","محدوده رو برقرار کن"),
                ("tutorial.7.body","Select an actually recruited rifleman and Move to the perimeter marker. Keep the reinforcement there.","تیرانداز واقعاً جذب‌شده رو انتخاب کن و به نشان محدوده حرکت بده. همون‌جا بمونه."),
                ("hud.stage.7","Establish the perimeter","محدوده رو برقرار کن"),
                ("tutorial.8.title","Stabilize both districts","هر دو محله رو تثبیت کن"),
                ("tutorial.8.body","Keep both services operational, both original engineers and all staff alive, and a recruited reinforcement stationary at the perimeter. Hold six seconds after all military threats are defeated.","هر دو مرکز فعال، مهندس‌های اصلی و کارکنان زنده و نیروی جذب‌شده در محدوده ثابت بمونن. پس از شکست همهٔ تهدیدهای نظامی شش ثانیه حفظ کن."),
                ("hud.stage.8","Stabilize both districts","هر دو محله رو تثبیت کن"),
                ("hud.clinic_outage","Clinic service blocked · recover within {0}s","خدمات کلینیک مسدوده · تا {0} ثانیه بازیابی کن"),
                ("hud.utility_outage","Utility service blocked · recover within {0}s","خدمات آب و برق مسدوده · تا {0} ثانیه بازیابی کن"),
                ("hud.stable","Stable perimeter: {0}/6 s","تثبیت محدوده: {0} از ۶ ثانیه"),
                ("result.star.services","Both civic services and perimeter secured","هر دو مرکز خدمات و محدوده امن شدند"),
                ("result.fuel_lost","Emergency Fuel storage or the protected civic allocation was lost.","ذخیرهٔ اضطراری یا سهم سوخت خدمات از دست رفت."),
                ("result.outage","A front remained blocked for sixty seconds. Clear hostiles and bring its original engineer to the recovery marker.","جبهه شصت ثانیه مسدود موند. دشمن رو پاک کن و مهندس اصلی رو به نشان بازیابی برسون."),
            };
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)??throw new InvalidOperationException("Localization missing");
            var locales=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en" && locale.LocaleCode!="fa-IR"){locales.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                entries["ui.home.archive.citywide_alert.relay_timing"]=fa?"زمان‌بندی گره‌های فعال رله":"ACTIVE RELAY TIMING";
                foreach(var item in copy)entries["mission.citywide_alert."+item.key]=fa?item.fa:item.en;
                locales.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,locales);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            guide.Configure(Enumerable.Range(1,8).Select(i=>new MissionGuideTopic{TitleKey=$"mission.citywide_alert.tutorial.{i}.title",BodyKey=$"mission.citywide_alert.tutorial.{i}.body",ExampleKey="mission.citywide_alert.guide.example",MistakeKey="mission.citywide_alert.guide.mistake",DiagramKey="mission.citywide_alert.guide.diagram"}).ToArray(),basis.Classes.ToArray());EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try {var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));data.FindProperty("citywideAlertGuide").objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
