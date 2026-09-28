using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH04M01AirCorridorPresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter04/CH04M01_AirCorridor_FieldGuide.asset";
        public static void BuildCheckpoint()
        {
            CH04M01AirCorridorConfigBuilder.Build();SeedLocalization();BuildGuide();
            CH04M01AirCorridorNarrativeBuilder.BuildAndInstall();AssetDatabase.SaveAssets();
            CH04M01AirCorridorRulesValidation.Run();AirMissileLauncherValidationRunner.Run();CampaignMissionComicCoverageValidation.ValidateAll();
            Debug.Log("[AirCorridorCheckpoint] result=Passed voices=12 comics=6 locales=en,fa-IR controls=existing-only");
        }
        private static void SeedLocalization()
        {
            var copy=new (string key,string en,string fa)[]{
                ("name","Air Corridor","مسیر هوایی"),
                ("summary","Keep the medical flight corridor open. Link automatic missile launchers to the radar, cover the west and north approaches, and stop both Vanguard attack waves.","مسیر پروازهای پزشکی رو باز نگه دار. پرتابگرهای خودکار رو به رادار وصل نگه دار، مسیرهای غربی و شمالی رو پوشش بده و هر دو موج ونگارد رو متوقف کن."),
                ("location","Sahrin · relief airfield","سارین · فرودگاه امداد"),
                ("enemy_intel","Vanguard: 3 west, then 3 north. Confirmed jets/drones only; protect relief flights.","ونگارد: ۳ مهاجم از غرب، بعد ۳ از شمال. فقط جنگنده و پهپاد تأییدشده؛ پروازهای امداد رو حفظ کن."),
                ("objective.aircraft","Stop both air waves","هر دو موج رو متوقف کن"),
                ("objective.radar","Protect the radar","رادار رو حفظ کن"),
                ("objective.corridor","Keep corridor open","مسیر رو باز نگه دار"),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","No friendly losses","نیروی خودی از دست نده"),
                ("star.3","Radar undamaged","رادار آسیب نبینه"),
                ("resources","Radar improves range and tracking","رادار برد و دقت رو بهتر می‌کنه"),
                ("forces","2 automatic launchers · radar · escort","۲ پرتابگر خودکار · رادار · همراه"),
                ("deadline","Clear both waves within 4 minutes","هر دو موج رو تا ۴ دقیقه پاک کن"),
                ("reward.card","1,800 Commander XP · 8,500 Credits","۱۸۰۰ تجربهٔ فرمانده · ۸۵۰۰ اعتبار"),
                ("result.victory","AIR CORRIDOR SECURE","مسیر هوایی امن شد"),
                ("result.defeat","AIR CORRIDOR LOST","مسیر هوایی از دست رفت"),
                ("result.success","Both Vanguard waves are stopped. Medical cargo and evacuees cleared the relief corridor. Flight data warns of an armored advance toward the fuel reserves.","هر دو موج ونگارد متوقف شدن. محمولهٔ پزشکی و تخلیه‌شده‌ها از مسیر امداد عبور کردن. داده‌های پرواز از پیشروی زرهی به سمت ذخیره‌های سوخت خبر می‌ده."),
                ("result.loss","The radar or relief corridor was lost. Keep launchers in linked coverage and stop confirmed attackers before they reach the inner airfield.","رادار یا مسیر امداد از دست رفت. پرتابگرها رو در پوشش متصل نگه دار و مهاجم‌های تأییدشده رو پیش از رسیدن به داخل فرودگاه متوقف کن."),
                ("result.losses","Friendly losses","تلفات خودی"),
                ("result.radar","Radar","رادار"),
                ("result.intact","INTACT","سالم"),
                ("result.damaged","DAMAGED","آسیب‌دیده"),
                ("result.destroyed","LOST","از دست رفت"),
                ("tutorial.1.title","Prepare linked air defense","دفاع هوایی متصل رو آماده کن"),
                ("tutorial.1.body","Launchers fire automatically. Protect the radar to retain its range and tracking bonus. Use normal Select, Move and Hold controls; Continue to inspect the coverage position.","پرتابگرها خودکار شلیک می‌کنن. رادار رو حفظ کن تا برد و دقت بیشتر باقی بمونه. از انتخاب، حرکت و نگه داشتن معمول استفاده کن؛ برای بررسی جای پوشش ادامه بده."),
                ("tutorial.2.title","Select the west launcher","پرتابگر غربی رو انتخاب کن"),
                ("tutorial.2.body","Select the west missile launcher with the standard selection control. Show Me only focuses the camera; it does not move or order the launcher.","پرتابگر موشکی غربی رو با فرمان انتخاب معمول انتخاب کن. «نشان بده» فقط دوربین رو می‌بره؛ به پرتابگر فرمان نمی‌ده."),
                ("tutorial.3.title","Cover the west approach","مسیر غربی رو پوشش بده"),
                ("tutorial.3.body","Use Move to position the west launcher at the marked coverage point. Keep the north launcher and radar linked. The first wave begins after preparation.","با حرکت، پرتابگر غربی رو به نقطهٔ پوشش علامت‌گذاری‌شده ببر. پرتابگر شمالی و رادار رو متصل نگه دار. موج اول بعد از آماده‌سازی شروع می‌شه."),
                ("tutorial.4.title","Protect the relief corridor","مسیر امداد رو حفظ کن"),
                ("tutorial.4.body","West probes enter after 20 seconds. Launchers automatically acquire, lock, fire and reload. Keep the radar alive; do not chase contacts away from linked coverage.","مهاجم‌های غربی پس از ۲۰ ثانیه وارد می‌شن. پرتابگرها خودکار هدف می‌گیرن، قفل می‌کنن، شلیک و دوباره آماده می‌شن. رادار رو زنده نگه دار؛ از پوشش متصل دور نشو."),
                ("tutorial.main.body","Northern attack warning: the main wave enters at 65 seconds. Maintain overlapping automatic coverage and protect the radar until all six attackers are stopped.","هشدار حملهٔ شمالی: موج اصلی در ثانیهٔ ۶۵ وارد می‌شه. پوشش خودکارِ هم‌پوشان رو نگه دار و تا توقف هر شش مهاجم، رادار رو حفظ کن."),
                ("warning.prepare","PREPARATION · Position the western launcher before the approach clock starts.","آماده‌سازی · پیش از شروع زمان حمله، پرتابگر غربی رو در جای پوشش قرار بده."),
                ("warning.west","WESTERN APPROACH · Two drones and one jet. Keep the radar protected; launcher fire is automatic.","مسیر غربی · دو پهپاد و یک جنگنده. رادار رو حفظ کن؛ پرتابگر خودکار شلیک می‌کنه."),
                ("warning.north","NORTHERN APPROACH · Two jets and one drone. Maintain overlapping radar-linked coverage.","مسیر شمالی · دو جنگنده و یک پهپاد. پوشش هم‌پوشانِ متصل به رادار رو نگه دار."),
                ("defense.complete","AIR CORRIDOR SECURE","مسیر هوایی امن شد"),
                ("defense.wave","Air attack {0}/{1}","حملهٔ هوایی {0}/{1}"),
                ("defense.countdown","Cover approaches · contact in {0}","مسیرها رو پوشش بده · تماس تا {0}"),
                ("defense.fighting","Automatic defense · {0} aircraft left","دفاع خودکار · {0} هواگرد باقی مونده"),
                ("guide.example","Select → Move → Hold. Missile targeting is automatic; radar support depends on distance.","انتخاب ← حرکت ← نگه داشتن. هدف‌گیری موشک خودکاره؛ پشتیبانی رادار به فاصله بستگی داره."),
                ("guide.mistake","Do not send launchers chasing aircraft. Keep radar support and overlapping approach coverage.","پرتابگرها رو دنبال هواگردها نفرست. پشتیبانی رادار و پوشش هم‌پوشان رو نگه دار."),
                ("guide.diagram","WEST + NORTH → LINKED LAUNCHERS + RADAR → RELIEF CORRIDOR","غرب + شمال ← پرتابگرهای متصل + رادار ← مسیر امداد")
            };
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)??throw new InvalidOperationException("Localization missing");
            var locales=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en"&&locale.LocaleCode!="fa-IR"){locales.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var item in copy)entries["mission.air_corridor."+item.key]=fa?item.fa:item.en;
                foreach(var line in CH04M01AirCorridorCopy.Brief.Concat(CH04M01AirCorridorCopy.Comms).Concat(CH04M01AirCorridorCopy.Debrief))entries[line.Key]=fa?line.Persian:line.English;
                locales.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,locales);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            guide.Configure(Enumerable.Range(1,4).Select(i=>new MissionGuideTopic{TitleKey=$"mission.air_corridor.tutorial.{i}.title",BodyKey=$"mission.air_corridor.tutorial.{i}.body",ExampleKey="mission.air_corridor.guide.example",MistakeKey="mission.air_corridor.guide.mistake",DiagramKey="mission.air_corridor.guide.diagram"}).ToArray(),basis.Classes.ToArray());EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try{var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));data.FindProperty("airCorridorGuide").objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
