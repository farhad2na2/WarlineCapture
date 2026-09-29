using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH04M03SplitFrontPresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter04/CH04M03_SplitFront_FieldGuide.asset";
        public static void BuildCheckpoint()
        {
            CH04M03SplitFrontConfigBuilder.Build();SeedLocalization();BuildGuide();
            CH04M03SplitFrontNarrativeBuilder.BuildAndInstall();AssetDatabase.SaveAssets();
            MissionSupportUiStyle.BuildSplitFrontHud();CH04M03SplitFrontRulesValidation.Run();CampaignMissionComicCoverageValidation.ValidateAll();
            Debug.Log("[SplitFrontCheckpoint] result=Passed dialogue=8 locales=en,fa-IR launcher=normal-attack controls=existing-only");
        }
        private static void SeedLocalization()
        {
            var copy=new(string key,string en,string fa)[]{
                ("attack.unsafe","Target unavailable or too close to protected civilians.","هدف در دسترس نیست یا به مردمِ محافظت‌شده خیلی نزدیکه."),
                ("name","Split Front","دو جبهه"),
                ("summary","Neutralize the verified Vanguard battery while defending the forward base. Keep civilians safe. Select the launcher, tap Attack, then tap the battery. Smoke support is optional.","آتشبار تأییدشدهٔ ونگارد رو از کار بنداز و از پایگاه جلو دفاع کن. مردم رو محفوظ نگه دار. پرتابگر رو انتخاب کن، حمله رو بزن، بعد آتشبار رو لمس کن. پشتیبانی دود اختیاریه."),
                ("location","Sahrin · forward base and battery","سارین · پایگاه جلو و آتشبار"),
                ("enemy_intel","Verified Vanguard battery; Ash Line diversion attacks the forward base.","آتشبار ونگارد تأیید شده؛ حملهٔ انحرافی خط خاکستر به پایگاه جلو."),
                ("objective.battery","Destroy the verified battery","آتشبار تأییدشده رو نابود کن"),
                ("objective.base","Defend the forward base","از پایگاه جلو دفاع کن"),
                ("objective.civilians","Protect civilian shelters","پناهگاه‌های مردم رو حفظ کن"),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","No friendly losses","نیروی خودی از دست نده"),
                ("star.3","No civilian losses","کسی از مردم از دست نره"),
                ("resources","Attack · minimum range · Hold before launch","حمله · حداقل برد · توقف با حفظ موضع قبل از پرتاب"),
                ("forces","Launcher · 3 tanks · 4 infantry · barracks","پرتابگر · ۳ تانک · ۴ پیاده · پادگان"),
                ("deadline","Smoke is optional · finish within 4 minutes","دود اختیاریه · تا ۴ دقیقه کامل کن"),
                ("reward.card","2,000 Commander XP · 9,000 Credits · Precision Strike","۲۰۰۰ تجربهٔ فرمانده · ۹۰۰۰ اعتبار · حملهٔ دقیق"),
                ("result.victory","BOTH FRONTS SECURE","هر دو جبهه امن شد"),
                ("result.defeat","SPLIT DEFENSE FAILED","دفاع دو جبهه شکست خورد"),
                ("result.success","The battery and diversion are stopped, the base holds and civilians are safe. The targeting package expected ARIA authorization; its supply trail leads to an air-support site.","آتشبار و حملهٔ انحرافی متوقف شدن، پایگاه پابرجاست و مردم امنن. بستهٔ هدف‌گیری منتظر مجوز آریا بود؛ رد تأمینش به مرکز پشتیبانی هوایی می‌رسه."),
                ("result.loss","Hold the base while the launcher engages the verified battery. Protect the shelters and keep enough defenders on the approach.","وقتی پرتابگر آتشبار تأییدشده رو می‌زنه، پایگاه رو حفظ کن. از پناهگاه‌ها محافظت کن و نیروی کافی سر مسیر نگه دار."),
                ("result.losses","Friendly losses","تلفات خودی"),
                ("result.base","Forward base","پایگاه جلو"),
                ("result.intact","INTACT","سالم"),
                ("result.damaged","DAMAGED","آسیب‌دیده"),
                ("result.destroyed","LOST","از دست رفت"),
                ("tutorial.1.title","Hold both fronts","هر دو جبهه رو نگه دار"),
                ("tutorial.1.body","Keep tanks and infantry defending the base. Use the launcher against the verified battery with the existing Attack command. Smoke is optional. Continue to inspect the target.","تانک‌ها و پیاده‌ها رو برای دفاع پایگاه نگه دار. با فرمان حملهٔ معمولی، پرتابگر رو به آتشبار تأییدشده بزن. دود اختیاریه. برای بررسی هدف ادامه بده."),
                ("tutorial.2.title","Select the launcher","پرتابگر رو انتخاب کن"),
                ("tutorial.2.body","Select the ground launcher. Show Me moves the camera only. Leave the defenders covering the base approach.","پرتابگر زمینی رو انتخاب کن. «نشان بده» فقط دوربین رو جابه‌جا می‌کنه. مدافع‌ها رو برای پوشش مسیر پایگاه نگه دار."),
                ("tutorial.3.title","Attack the battery","به آتشبار حمله کن"),
                ("tutorial.3.body","Select the launcher, tap Attack, then tap the battery. Keep defenders at the base and protect civilians. Targets must be within range and clear of civilian shelters.","پرتابگر رو انتخاب کن، حمله رو بزن، بعد آتشبار رو لمس کن. مدافع‌ها رو در پایگاه نگه دار و مردم رو محفوظ نگه دار. هدف باید در برد و دور از پناهگاه‌های مردم باشه."),
                ("tutorial.4.title","Keep both fronts secure","هر دو جبهه رو امن نگه دار"),
                ("tutorial.4.body","The launcher prepares, fires and reloads while attacking its target. Use Hold to stop before launch. A missile in flight cannot be recalled. Keep defenders at the base.","پرتابگر موقع حمله به هدفش آماده می‌شه، شلیک می‌کنه و دوباره بارگذاری می‌کنه. برای توقف قبل از پرتاب، حفظ موضع رو بزن. موشک در پرواز برنمی‌گرده. مدافع‌ها رو در پایگاه نگه دار."),
                ("tutorial.main.body","The battery is silent. Keep the defenders on the approach until the diversion is stopped.","آتشبار خاموش شده. مدافع‌ها رو سر مسیر نگه دار تا حملهٔ انحرافی متوقف بشه."),
                ("warning.prepare","PREPARATION · Select the launcher; leave defenders at the base.","آماده‌سازی · پرتابگر رو انتخاب کن؛ مدافع‌ها در پایگاه بمونن."),
                ("warning.lead","BATTERY · Verified target beyond minimum range.","آتشبار · هدف تأییدشده بیرون حداقل برد."),
                ("warning.main","DIVERSION · Keep defenders covering the base approach.","حملهٔ انحرافی · مدافع‌ها مسیر پایگاه رو پوشش بدن."),
                ("defense.complete","BOTH FRONTS SECURE","هر دو جبهه امن شد"),
                ("defense.wave","Front {0}/{1}","جبههٔ {0}/{1}"),
                ("defense.countdown","Hold the base · contact in {0}","پایگاه رو حفظ کن · تماس تا {0}"),
                ("defense.fighting","Split defense · {0} hostiles left","دفاع دو جبهه · {0} دشمن باقی مونده"),
                ("guide.example","Select launcher → Attack → battery. Hold stops preparation before launch. Infantry and tanks keep defending the base.","انتخاب پرتابگر ← حمله ← آتشبار. حفظ موضع آماده‌سازی رو قبل از پرتاب متوقف می‌کنه. پیاده‌ها و تانک‌ها دفاع پایگاه رو ادامه بدن."),
                ("guide.mistake","Do not move every defender to the battery. Never fire inside minimum range or near the civilian shelters. A missile in flight cannot be recalled.","همهٔ مدافع‌ها رو سمت آتشبار نبر. داخل حداقل برد یا کنار پناهگاه‌های مردم شلیک نکن. موشک در حال پرواز برنمی‌گرده."),
                ("guide.diagram","DEFEND BASE | VERIFY BATTERY | PROTECT CIVILIANS","دفاع پایگاه | تأیید آتشبار | حفاظت مردم"),












            };
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)??throw new InvalidOperationException("Localization missing");
            var locales=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en"&&locale.LocaleCode!="fa-IR"){locales.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var key in entries.Keys.Where(k=>k.StartsWith("mission.split_front.hud.",StringComparison.Ordinal)).ToArray())entries.Remove(key);
                foreach(var item in copy)entries["mission.split_front."+item.key]=fa?item.fa:item.en;
                entries["mission.reward.precision_strike"]=fa?"حملهٔ دقیق":"PRECISION STRIKE UNLOCK";
                entries["selection.order.missile_preparing"]=fa?"موشک در حال آماده‌سازی":"Preparing missile";
                entries["tactical.feedback.missile_preparing"]=fa?"موشک در حال آماده‌سازیه.":"Preparing missile.";
                foreach(var line in CH04M03SplitFrontCopy.Brief.Concat(CH04M03SplitFrontCopy.Comms).Concat(CH04M03SplitFrontCopy.Debrief))entries[line.Key]=fa?line.Persian:line.English;
                locales.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,locales);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            guide.Configure(Enumerable.Range(1,4).Select(i=>new MissionGuideTopic{TitleKey=$"mission.split_front.tutorial.{i}.title",BodyKey=$"mission.split_front.tutorial.{i}.body",ExampleKey="mission.split_front.guide.example",MistakeKey="mission.split_front.guide.mistake",DiagramKey="mission.split_front.guide.diagram"}).ToArray(),basis.Classes.ToArray());EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try{var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));data.FindProperty("splitFrontGuide").objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
