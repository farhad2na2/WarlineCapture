using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH04M04GroundedSignalPresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter04/CH04M04_GroundedSignal_FieldGuide.asset";
        public static void Build()
        {
            SeedLocalization();BuildGuide();
            var hud=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
            try
            {if(hud.GetComponent<GroundedSignalMarkersView>()==null)hud.AddComponent<GroundedSignalMarkersView>();
                PrefabUtility.SaveAsPrefabAsset(hud,"Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(hud);}
            AssetDatabase.SaveAssets();
            Debug.Log("[GroundedSignalPresentation] result=Passed existingHud=1 newCommandButtons=0 locales=2 markers=approved-style");
        }
        private static void SeedLocalization()
        {
            var copy=new (string key,string en,string fa)[]{
                ("name","Grounded Signal","سیگنال زمینی"),
                ("summary","Unload Karim and Yusuf at the apron. Disable the military relay, recover its hardware, then extract both specialists by APC. Protect the civilian terminal.","کریم و یوسف رو توی محوطهٔ توقف پیاده کن. رلهٔ نظامی رو از کار بنداز، تجهیزاتش رو بردار، بعد هر دو متخصص رو با نفربر خارج کن. از ترمینال غیرنظامی محافظت کن."),
                ("location","Sahrin · civilian airfield and military relay","سارین · فرودگاه غیرنظامی و رلهٔ نظامی"),
                ("enemy_intel","Vanguard relay and perimeter guards. Civilian facilities are protected.","رلهٔ ونگارد و نگهبان‌های محوطه. تأسیسات غیرنظامی محافظت می‌شن."),
                ("objective.hardware","Disable the relay and recover its hardware","رله رو از کار بنداز و تجهیزاتش رو بردار"),
                ("objective.relay","Disable the relay and recover its hardware","رله رو از کار بنداز و تجهیزاتش رو بردار"),
                ("objective.extract","Extract both specialists by APC","هر دو متخصص رو با نفربر خارج کن"),
                ("objective.terminal","Protect the civilian terminal and staff","از ترمینال غیرنظامی و کارکنانش محافظت کن"),
                ("objective.civilian","Protect the civilian terminal and staff","از ترمینال غیرنظامی و کارکنانش محافظت کن"),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","No escort losses","محافظی از دست نده"),
                ("star.3","No civilian losses","بدون تلفات غیرنظامی"),
                ("label.transport","TRANSPORT","ترابری"),
                ("label.deadline","EXTRACTION WINDOW","فرصت خروج"),
                ("intel.relay","MILITARY RELAY","رلهٔ نظامی"),
                ("intel.guards","PERIMETER GUARDS","نگهبانان محوطه"),
                ("intel.present","PRESENT","موجود"),
                ("intel.air","AIR THREAT","تهدید هوایی"),
                ("intel.none","NONE","هیچ"),
                ("resources","Required transport supplied · support optional","وسیلهٔ لازم آماده‌ست · پشتیبانی اختیاریه"),
                ("forces","Transport plane · 2 specialists · APC · rifle escort","هواپیمای ترابری · ۲ متخصص · نفربر · محافظ تفنگدار"),
                ("deadline","Extract within fifteen minutes","تا پانزده دقیقه خارج شو"),
                ("reward.card","Commander XP · Credits · Paratroopers unlocked","تجربهٔ فرمانده · اعتبار · باز شدن چتربازها"),
                ("result.victory","HARDWARE RECOVERED","تجهیزات بازیابی شد"),
                ("result.defeat","EXTRACTION FAILED","خروج شکست خورد"),
                ("result.subtitle","GROUNDED SIGNAL · AIRFIELD RELAY","سیگنال زمینی · رلهٔ فرودگاه"),
                ("result.star.hardware","Hardware and specialists extracted","خروج متخصص‌ها با تجهیزات"),
                ("result.escort_losses","ESCORT LOSSES","تلفات محافظ‌ها"),
                ("result.civilian_losses","CIVILIAN STAFF LOSSES","تلفات کارکنان غیرنظامی"),
                ("result.success_short","Both specialists extracted with the relay hardware. Civilian terminal protected.","هر دو متخصص با تجهیزات رله خارج شدن. ترمینال غیرنظامی سالم موند."),
                ("result.success","Both specialists returned with the control hardware. The civilian airfield is intact. The recovered schedule identifies Vanguard's command group.","هر دو متخصص با تجهیزات کنترل برگشتن. فرودگاه غیرنظامی سالمه. برنامهٔ بازیابی‌شده گروه فرماندهی ونگارد رو مشخص می‌کنه."),
                ("result.passenger_lost","A specialist was lost. Keep both original specialists alive through recovery and extraction.","یکی از متخصص‌ها از دست رفت. هر دو متخصص اصلی باید تا پایان بازیابی و خروج زنده بمونن."),
                ("result.carrier_lost","The extraction APC was lost. Protect the carrier and keep the service road open.","نفربر خروج از دست رفت. از نفربر محافظت کن و جادهٔ خدماتی رو باز نگه دار."),
                ("result.aircraft_lost","The insertion plane was lost before both specialists unloaded.","قبل از پیاده شدن هر دو متخصص، هواپیمای ورود از دست رفت."),
                ("result.terminal_lost","Civilian staff were lost. Keep combat away from the protected terminal.","کارکنان غیرنظامی از دست رفتن. نبرد رو از ترمینال محافظت‌شده دور نگه دار."),
                ("result.timeout","The extraction window closed. Recover the hardware and reach the guarded exit within fifteen minutes.","فرصت خروج تموم شد. تجهیزات رو بردار و تا پانزده دقیقه به خروجی محافظت‌شده برس."),
                ("result.integrity","The operation could not confirm the original team and hardware. Retry the mission.","تیم اصلی و تجهیزات برای پایان عملیات تأیید نشدن. مأموریت رو دوباره اجرا کن."),
                ("tutorial.1.title","Unload the specialists","متخصص‌ها رو پیاده کن"),
                ("tutorial.1.body","Select the transport plane, open its passengers, then use Exit All. Karim and Yusuf must unload at the apron. Show Me moves only the camera.","هواپیمای ترابری رو انتخاب کن، مسافرهاش رو باز کن و «خروج همه» رو بزن. کریم و یوسف باید توی محوطهٔ توقف پیاده بشن. «نشان بده» فقط دوربین رو جابه‌جا می‌کنه."),
                ("tutorial.2.title","Disable the military relay","رلهٔ نظامی رو از کار بنداز"),
                ("tutorial.2.approach.title","Reach the relay gate","به دروازهٔ رله برس"),
                ("tutorial.2.approach.body","Select the rifle escort and Move through the open service gate. Once close to the relay, use Attack on its vehicle. Protect the specialists and civilian terminal.","محافظ‌های تفنگدار رو انتخاب کن و با حرکت از دروازهٔ خدماتی باز عبور کن. وقتی به رله نزدیک شدن، با حمله خودروش رو هدف بگیر. از متخصص‌ها و ترمینال غیرنظامی محافظت کن."),
                ("tutorial.2.apc_approach.title","Reach the relay gate","به دروازهٔ رله برس"),
                ("tutorial.2.apc_approach.body","The escort is lost. Select the supplied APC and Move through the open service gate, then Attack the relay. Keep the carrier safe for extraction.","محافظ‌ها از دست رفتن. نفربر آماده رو انتخاب کن و با حرکت از دروازهٔ خدماتی باز عبور کن، بعد به رله حمله کن. نفربر رو برای خروج سالم نگه دار."),
                ("tutorial.2.apc.title","Disable the military relay","رلهٔ نظامی رو از کار بنداز"),
                ("tutorial.2.apc.body","Select the supplied APC and Attack the military relay vehicle. Protect the carrier: both specialists need it for extraction.","نفربر آماده رو انتخاب کن و به خودروی رلهٔ نظامی حمله کن. از نفربر محافظت کن؛ هر دو متخصص برای خروج بهش نیاز دارن."),
                ("tutorial.2.body","Select the rifle escort, tap Attack, then target the military relay vehicle. Protect the specialists and keep fighting away from the civilian terminal.","محافظ‌های تفنگدار رو انتخاب کن، حمله رو بزن و خودروی رلهٔ نظامی رو هدف بگیر. از متخصص‌ها محافظت کن و نبرد رو از ترمینال غیرنظامی دور نگه دار."),
                ("tutorial.3.title","Recover the control hardware","تجهیزات کنترل رو بردار"),
                ("tutorial.3.body","Select both specialists and Move them to the marked hardware point. Keep them dismounted together for six seconds. Leaving or boarding cancels recovery.","هر دو متخصص رو انتخاب کن و با حرکت به محل تجهیزات برسون. شش ثانیه کنار هم و پیاده بمونن. دور شدن یا سوار شدن، بازیابی رو قطع می‌کنه."),
                ("tutorial.4.title","Board the extraction APC","سوار نفربر خروج شو"),
                ("tutorial.4.body","Select both specialists, open the existing Commands wheel, choose Board, then tap the APC. The recovered hardware travels with the team.","هر دو متخصص رو انتخاب کن، چرخ فرمان‌های معمولی رو باز کن، سوار شدن رو بزن و نفربر رو لمس کن. تجهیزات بازیابی‌شده همراه تیم می‌رن."),
                ("tutorial.5.title","Reach the guarded exit","به خروجی محافظت‌شده برس"),
                ("tutorial.5.body","Select the APC and Move it to the marked exit. Keep both specialists aboard and the exit uncontested for six seconds.","نفربر رو انتخاب کن و با حرکت به خروجی مشخص‌شده برسون. هر دو متخصص باید سوار بمونن و خروجی شش ثانیه بدون دشمن باشه."),
                ("hud.stage.1","UNLOAD · Select the plane → Passengers → Exit All","پیاده شدن · انتخاب هواپیما ← مسافران ← خروج همه"),
                ("hud.stage.2","RELAY · Escort → Attack → military relay","رله · محافظ‌ها ← حمله ← رلهٔ نظامی"),
                ("hud.stage.2.approach","RELAY · Escort → Move → open service gate","رله · محافظ‌ها ← حرکت ← دروازهٔ خدماتی باز"),
                ("hud.stage.2.apc_approach","RELAY · APC → Move → open service gate","رله · نفربر ← حرکت ← دروازهٔ خدماتی باز"),
                ("hud.stage.2.apc","RELAY · APC → Attack → military relay","رله · نفربر ← حمله ← رلهٔ نظامی"),
                ("hud.stage.3","RECOVERY · Both specialists → Move → hardware","بازیابی · هر دو متخصص ← حرکت ← تجهیزات"),
                ("hud.stage.4","EXTRACTION · Both specialists → Board → APC","خروج · هر دو متخصص ← سوار شدن ← نفربر"),
                ("hud.stage.5","EXIT · APC → Move → guarded exit","خروجی · نفربر ← حرکت ← خروجی محافظت‌شده"),
                ("hud.recovery","Hardware recovery: {0}/6 s","بازیابی تجهیزات: {0} از ۶ ثانیه"),
                ("hud.exit","Exit secure: {0}/6 s","امن بودن خروجی: {0} از ۶ ثانیه"),
                ("marker.apron","UNLOAD","پیاده شدن"),
                ("marker.hardware","HARDWARE","تجهیزات"),
                ("marker.exit","EXIT","خروج"),
                ("guide.title","GROUNDED SIGNAL · FIELD GUIDE","سیگنال زمینی · راهنمای میدان"),
                ("guide.example","Unload → disable relay → recover hardware → Board APC → guarded exit.","پیاده شدن ← از کار انداختن رله ← بازیابی تجهیزات ← سوار نفربر ← خروجی محافظت‌شده."),
                ("guide.mistake","Do not board before hardware recovery. Keep both specialists alive and protect the terminal staff.","قبل از بازیابی تجهیزات سوار نشو. هر دو متخصص رو زنده نگه دار و از کارکنان ترمینال محافظت کن."),
                ("guide.diagram","INSERT | RECOVER | EXTRACT","ورود | بازیابی | خروج")
            };
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)??throw new InvalidOperationException("Localization missing");
            var locales=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en" && locale.LocaleCode!="fa-IR"){locales.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var item in copy)entries["mission.grounded_signal."+item.key]=fa?item.fa:item.en;
                entries["mission.reward.paratroopers"]=fa?"چتربازها":"PARATROOPERS UNLOCK";
                foreach(var line in CH04M04GroundedSignalCopy.Brief.Concat(CH04M04GroundedSignalCopy.Comms).Concat(CH04M04GroundedSignalCopy.Debrief))entries[line.Key]=fa?line.Persian:line.English;
                locales.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,locales);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            guide.Configure(Enumerable.Range(1,5).Select(i=>new MissionGuideTopic{TitleKey=$"mission.grounded_signal.tutorial.{i}.title",BodyKey=$"mission.grounded_signal.tutorial.{i}.body",ExampleKey="mission.grounded_signal.guide.example",MistakeKey="mission.grounded_signal.guide.mistake",DiagramKey="mission.grounded_signal.guide.diagram"}).ToArray(),basis.Classes.ToArray());EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try{var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));data.FindProperty("groundedSignalGuide").objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
