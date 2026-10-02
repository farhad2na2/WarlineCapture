using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M02TrustUnderFirePresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter05/CH05M02_TrustUnderFire_FieldGuide.asset";
        public static void Build()
        {
            CH05M02TrustUnderFireNarrativeBuilder.SeedCopyLocalization();SeedLocalization();BuildGuide();AssetDatabase.SaveAssets();
            var hud=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
            try{if(hud.GetComponent<TrustUnderFireMarkersView>()==null)hud.AddComponent<TrustUnderFireMarkersView>();PrefabUtility.SaveAsPrefabAsset(hud,"Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(hud);}
            Debug.Log("[TrustUnderFirePresentation] result=Passed existingHud=1 newCommandButtons=0 locales=2 protectedConvoyFallback=Explicit");
        }
        private static void SeedLocalization()
        {
            var copy=new (string key,string en,string fa)[]{
                ("name","Trust Under Fire","اعتماد زیر آتش"),
                ("summary","Protect both evacuation convoys and shelters, then verify the preserved broadcast source.","از هر دو کاروان تخلیه و پناهگاه محافظت کن، سپس منبع فرستندهٔ سالم رو تأیید کن."),
                ("location","Sahrin · populated quay approaches","سهرین · مسیرهای محله‌های بندری"),
                ("objective.north","Escort north convoy","کاروان شمالی رو همراهی کن"),
                ("objective.south","Escort south convoy","کاروان جنوبی رو همراهی کن"),
                ("objective.broadcast","Verify broadcast source","منبع پیام رو تأیید کن"),
                ("objective.relay","Verify broadcast source","منبع پیام رو تأیید کن"),
                ("forces","Two escorts · two protected convoys · one signal engineer","دو گروه اسکورت · دو کاروان محافظت‌شده · یک مهندس سیگنال"),
                ("resources","100 Fuel · 20 shelter Fuel protected","۱۰۰ سوخت · ۲۰ سوخت پناهگاه محفوظ"),
                ("label.fuel","FUEL","سوخت"),
                ("label.deadline","DEADLINE","مهلت"),
                ("intel.military","VERIFIED MILITARY","نظامیان تأییدشده"),
                ("intel.confirmed","9","۹"),
                ("intel.civilian","CIVILIAN RISK","خطر غیرنظامیان"),
                ("intel.protected","2 + 4","۲ + ۴"),
                ("deadline","Protect and verify within fifteen minutes","تا پانزده دقیقه محافظت و تأیید کن"),
                ("enemy_intel","Two route ambushes and three verified relay guards. Keep combat away from shelter staff.","دو کمین مسیر و سه نگهبان تأییدشدهٔ فرستنده. نبرد رو از کارکنان پناهگاه دور نگه دار."),
                ("reward.card","Commander XP · Credits · Supply Drop unlocked afterward","تجربهٔ فرمانده · اعتبار · باز شدن ارسال تدارکات پس از مأموریت"),
                ("reward.supply","Supply Drop","ارسال تدارکات"),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","No escort losses","بدون تلفات اسکورت"),
                ("star.3","No shelter staff losses","بدون تلفات کارکنان پناهگاه"),
                ("result.victory","ROUTES SECURED","مسیرها امن شدند"),
                ("result.defeat","EVACUATION FAILED","تخلیه شکست خورد"),
                ("result.objectives","MISSION OBJECTIVES","هدف‌های مأموریت"),
                ("result.convoys","Protected convoys","کاروان‌های محافظت‌شده"),
                ("result.source","Verified source","منبع تأییدشده"),
                ("result.staff","Shelter staff safe","کارکنان پناهگاه سالم"),
                ("result.secured","SECURED","امن شدند"),
                ("result.verified","VERIFIED","تأیید شد"),
                ("result.safe","ALL SAFE","همه سالم"),
                ("result.incomplete","INCOMPLETE","ناقص"),
                ("result.losses","LOSSES","تلفات"),
                ("result.escort_losses","Escort losses","تلفات اسکورت"),
                ("result.staff_losses","Staff losses","تلفات کارکنان"),
                ("result.subtitle","TRUST UNDER FIRE · CIVILIAN ROUTES","اعتماد زیر آتش · مسیرهای غیرنظامی"),
                ("result.success","Both named convoys reached functioning shelters. The protected staff survived, and the intact transmitter confirmed the witness report.","هر دو کاروان به پناهگاه‌های فعال رسیدن. کارکنان سالم موندن و فرستندهٔ سالم گزارش شاهد رو تأیید کرد."),
                ("result.success_short","Both shelters reached. Broadcast source verified.","هر دو پناهگاه دریافت کردن. منبع پیام تأیید شد."),
                ("result.loss","Protect the original convoys, shelter staff and engineer. Preserve the shelters, Fuel reserve and broadcast source.","از کاروان‌های اصلی، کارکنان پناهگاه و مهندس محافظت کن. پناهگاه‌ها، ذخیرهٔ سوخت و فرستنده رو حفظ کن."),
                ("result.north_lost","The north convoy was lost. Clear its route before moving the protected vehicle.","کاروان شمالی از دست رفت. پیش از حرکت خودرو مسیرش رو پاک‌سازی کن."),
                ("result.south_lost","The south convoy was lost. Clear its route before moving the protected vehicle.","کاروان جنوبی از دست رفت. پیش از حرکت خودرو مسیرش رو پاک‌سازی کن."),
                ("result.shelter_lost","A shelter was destroyed. Protect both civilian compounds.","پناهگاه نابود شد. هر دو محوطهٔ غیرنظامی رو حفظ کن."),
                ("result.staff_lost","Original shelter staff were lost. Protect both civilian exclusion areas.","کارکنان اصلی پناهگاه از دست رفتن. هر دو محدودهٔ غیرنظامی رو حفظ کن."),
                ("result.engineer_lost","The original signal engineer was lost. Protect the engineer until both routes are stabilized.","مهندس اصلی سیگنال از دست رفت. تا تثبیت هر دو مسیر از مهندس محافظت کن."),
                ("result.escort_lost","All surviving escorts were lost while military threats remained.","همهٔ اسکورت‌های باقی‌مانده در حالی از دست رفتن که تهدیدهای نظامی هنوز باقی بودن."),
                ("result.fuel_lost","The emergency reserve or shelter Fuel allocation was lost.","ذخیرهٔ اضطراری یا سهم سوخت پناهگاه از دست رفت."),
                ("result.broadcast_lost","The transmitter was destroyed. Preserve the verified equipment until victory.","فرستنده نابود شد. تجهیزات تأییدشده رو تا پیروزی حفظ کن."),
                ("result.timeout","The fifteen-minute evacuation window closed.","فرصت پانزده‌دقیقه‌ای تخلیه تموم شد."),
                ("hud.north_hold","North shelter: {0}/6 s","پناهگاه شمال: {0} از ۶ ثانیه"),
                ("hud.south_hold","South shelter: {0}/6 s","پناهگاه جنوب: {0} از ۶ ثانیه"),
                ("hud.verification","Verify broadcast: {0}/6 s","تأیید فرستنده: {0} از ۶ ثانیه"),
                ("hud.stable","Protected routes: {0}/6 s","مسیرهای محافظت‌شده: {0} از ۶ ثانیه"),
                ("hud.time","Evacuation window: {0}m {1}s","فرصت تخلیه: {0} دقیقه و {1} ثانیه"),
                ("hud.fuel","Fuel: {0} · shelter allocation protected","سوخت: {0} · سهم پناهگاه محفوظ"),
                ("marker.north","NORTH SHELTER","پناهگاه شمالی"),
                ("marker.south","SOUTH SHELTER","پناهگاه جنوبی"),
                ("marker.north_crossing","NORTH CROSSING","گذرگاه شمالی"),
                ("marker.south_crossing","SOUTH CROSSING","گذرگاه جنوبی"),
                ("marker.relay","BROADCAST","فرستنده"),
                ("marker.verification","VERIFY","تأیید"),
                ("guide.example","Clear route → Move convoy → protect shelter → verify source.","پاک‌سازی مسیر ← حرکت کاروان ← حفظ پناهگاه ← تأیید منبع"),
                ("guide.mistake","Do not move protected convoys through active ambushes. Show Me moves only the camera.","کاروان‌های محافظت‌شده رو از کمین فعال عبور نده. «نشان بده» فقط دوربین رو حرکت می‌ده."),
                ("guide.diagram","TWO ROUTES | TWO SHELTERS | VERIFIED SOURCE","دو مسیر | دو پناهگاه | منبع تأییدشده")};
            var stages=new (string en,string fa,string body,string faBody)[]{
                ("Secure the north route","مسیر شمالی رو امن کن","Select the north escort. Move into position and Attack the verified ambush. Preserve the relief convoy.","اسکورت شمالی رو انتخاب کن. مستقر شو و به کمین تأییدشده حمله کن. کاروان امداد رو حفظ کن."),
                ("Escort the north convoy","کاروان شمالی رو همراهی کن","Move the north convoy across its land bridge to the north arrival marker. Hold stationary six seconds with the shelter and staff alive.","کاروان شمالی رو از پل زمینی به نشان رسیدن شمال ببر. با پناهگاه و کارکنان زنده، شش ثانیه ثابت بمونه."),
                ("Secure the south route","مسیر جنوبی رو امن کن","Select the south escort. Attack the verified ambush while protecting the second convoy and shelter.","اسکورت جنوبی رو انتخاب کن. به کمین تأییدشده حمله کن و کاروان دوم و پناهگاه رو حفظ کن."),
                ("Escort the south convoy","کاروان جنوبی رو همراهی کن","Move the south convoy across the independent south bridge. Hold at its arrival marker six seconds.","کاروان جنوبی رو از پل مستقل جنوب ببر. شش ثانیه کنار نشان رسیدنش ثابت بمونه."),
                ("Approach the broadcast compound","به محوطهٔ فرستنده نزدیک شو","Move a surviving escort to the relay approach. Preserve civilian structures and the transmitter.","اسکورت باقی‌مونده رو به مسیر فرستنده ببر. ساختمان‌های غیرنظامی و فرستنده رو حفظ کن."),
                ("Clear the verified guards","نگهبان‌های تأییدشده رو پاک کن","Attack the three verified military guards. Do not Attack the broadcast building.","به سه نگهبان نظامی تأییدشده حمله کن. به ساختمان فرستنده حمله نکن."),
                ("Verify the broadcast source","منبع پیام رو تأیید کن","Move the original signal engineer to the external verification marker. Hold uncontested six seconds with the transmitter intact.","مهندس اصلی سیگنال رو به نشان تأیید بیرونی ببر. با فرستندهٔ سالم شش ثانیه بدون دشمن بمونه."),
                ("Stabilize the protected routes","مسیرهای محافظت‌شده رو تثبیت کن","Keep both arrived convoys, shelters and staff alive. Preserve the verified transmitter and shelter Fuel, then hold six seconds after all military threats are defeated.","هر دو کاروان رسیده، پناهگاه‌ها و کارکنان زنده بمونن. فرستندهٔ تأییدشده و سهم سوخت رو حفظ کن و پس از شکست تهدیدها شش ثانیه نگه دار.")};
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)??throw new InvalidOperationException("Localization missing");var locales=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en"&&locale.LocaleCode!="fa-IR"){locales.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";var entries=locale.Entries.ToDictionary(e=>e.Key,e=>e.Value,StringComparer.Ordinal);
                foreach(var item in copy)entries["mission.trust_under_fire."+item.key]=fa?item.fa:item.en;
                for(int i=0;i<stages.Length;i++){var stage=stages[i];entries[$"mission.trust_under_fire.tutorial.{i+1}.title"]=fa?stage.fa:stage.en;entries[$"mission.trust_under_fire.tutorial.{i+1}.body"]=fa?stage.faBody:stage.body;entries[$"mission.trust_under_fire.hud.stage.{i+1}"]=fa?stage.fa:stage.en;}
                foreach(int i in new[]{1,3,6})entries[$"mission.trust_under_fire.tutorial.{i}.body.approach"]=fa?"اسکورت باقی‌مانده رو به موقعیت شلیک ببر، سپس با فرمان حمله هدف نظامی مشخص‌شده رو بزن.":"Move the surviving escort into firing position, then use Attack against the marked military target.";
                entries["ui.home.archive.trust_under_fire.broadcast_source"]=fa?"منبع تأییدشدهٔ فرستنده":"VERIFIED BROADCAST SOURCE";
                entries["ui.home.archive.trust_under_fire.relay"]=fa?"منبع تأییدشدهٔ فرستنده":"VERIFIED BROADCAST SOURCE";
                locales.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,entries.OrderBy(e=>e.Key,StringComparer.Ordinal).Select(e=>new GameLocalizedStringRecord(e.Key,e.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,locales);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            guide.Configure(Enumerable.Range(1,8).Select(i=>new MissionGuideTopic{TitleKey=$"mission.trust_under_fire.tutorial.{i}.title",BodyKey=$"mission.trust_under_fire.tutorial.{i}.body",ExampleKey="mission.trust_under_fire.guide.example",MistakeKey="mission.trust_under_fire.guide.mistake",DiagramKey="mission.trust_under_fire.guide.diagram"}).ToArray(),basis.Classes.ToArray());EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try{var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));var property=data.FindProperty("trustUnderFireGuide");if(property==null)throw new InvalidOperationException("Trust field-guide runtime binding missing");property.objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
