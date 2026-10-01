using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH04M02SteelPushPresentationBuilder
    {
        public const string GuidePath="Assets/Game/Configs/Missions/Chapter04/CH04M02_SteelPush_FieldGuide.asset";
        public static void BuildCheckpoint()
        {
            CH04M02SteelPushConfigBuilder.Build();SeedLocalization();BuildGuide();
            CH04M02SteelPushNarrativeBuilder.BuildAndInstall();AssetDatabase.SaveAssets();
            CH04M02SteelPushRulesValidation.Run();CampaignMissionComicCoverageValidation.ValidateAll();
            Debug.Log("[SteelPushCheckpoint] result=Passed dialogue=7 locales=en,fa-IR fuel=physical-finite controls=existing-only");
        }
        private static void SeedLocalization()
        {
            var copy=new(string key,string en,string fa)[]{
                ("name","Steel Push","پیشروی زرهی"),
                ("summary","Stop Vanguard's armored column. Defend the shared Fuel reserve and keep command vehicles away from the dormant Relay node. Use tanks, infantry and normal reinforcement controls; preserve the civilian allocation.","ستون زرهی ونگارد رو متوقف کن. ذخیرهٔ مشترک سوخت رو حفظ کن و نذار خودروهای فرماندهی به گره خاموش رله برسن. از تانک، پیاده و فرمان‌های معمول نیروی کمکی استفاده کن؛ سهم مردم رو محفوظ نگه دار."),
                ("location","Sahrin · emergency Fuel yard","سارین · محوطهٔ ذخیرهٔ اضطراری سوخت"),
                ("enemy_intel","Two staged Vanguard groups: 5 armored vehicles. Command vehicles seek the Relay.","دو گروه ونگارد: ۵ خودروی زرهی. خودروهای فرماندهی دنبال رله‌ان."),
                ("objective.column","Break the armored column","ستون زرهی رو بشکن"),
                ("objective.fuel","Protect the Fuel reserve","ذخیرهٔ سوخت رو حفظ کن"),
                ("objective.relay","Deny Relay access","راه رله رو ببند"),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","No friendly losses","نیروی خودی از دست نده"),
                ("star.3","Reserve undamaged","ذخیره آسیب نبینه"),
                ("resources","120 military Fuel · 40 protected for civilians","۱۲۰ سوخت نظامی · ۴۰ سهم محفوظ مردم"),
                ("forces","3 tanks · APC · 4 infantry · barracks","۳ تانک · نفربر · ۴ پیاده · پادگان"),
                ("deadline","Stop the column within 4 minutes","ستون رو تا ۴ دقیقه متوقف کن"),
                ("reward.card","2,000 Commander XP · 9,000 Credits · Smoke Screen","۲۰۰۰ تجربهٔ فرمانده · ۹۰۰۰ اعتبار · پردهٔ دود"),
                ("result.victory","FUEL RESERVE SECURE","ذخیرهٔ سوخت امن شد"),
                ("result.defeat","RESERVE DEFENSE FAILED","دفاع از ذخیره شکست خورد"),
                ("result.success","The column is stopped and civilian Fuel remains protected. Recovered orders name a final authority key. A Vanguard battery now threatens the forward base.","ستون متوقف شده و سهم سوخت مردم محفوظ مونده. دستورهای بازیابی‌شده از کلید نهایی اختیار اسم می‌برن. حالا یک آتشبار ونگارد پایگاه جلو رو تهدید می‌کنه."),
                ("result.loss","The reserve was lost or the Relay approach was breached. Keep armor across the road, protect the Fuel site and use the finite military reserve deliberately.","ذخیره از دست رفت یا راه رله شکسته شد. زرهی‌ها رو سر جاده نگه دار، محوطهٔ سوخت رو حفظ کن و ذخیرهٔ محدود نظامی رو حساب‌شده خرج کن."),
                ("result.losses","Friendly losses","تلفات خودی"),
                ("result.fuel","Fuel site","محوطهٔ سوخت"),
                ("result.intact","INTACT","سالم"),
                ("result.damaged","DAMAGED","آسیب‌دیده"),
                ("result.destroyed","LOST","از دست رفت"),
                ("tutorial.1.title","Defend shared Fuel","از سوخت مشترک دفاع کن"),
                ("tutorial.1.body","Three tanks, an APC and infantry defend the reserve. Armor movement spends finite military Fuel; infantry reinforcements use normal barracks resources. Protect 40 barrels for hospitals and water pumps. Continue to inspect the blocking position.","سه تانک، یک نفربر و پیاده‌ها از ذخیره دفاع می‌کنن. حرکت زرهی، سوخت محدود نظامی رو خرج می‌کنه؛ نیروی کمکی پیاده از منابع معمول پادگان استفاده می‌کنه. ۴۰ بشکه رو برای بیمارستان و پمپ‌های آب حفظ کن. برای بررسی جای سد دفاعی ادامه بده."),
                ("tutorial.2.title","Select the front tank","تانک جلو رو انتخاب کن"),
                ("tutorial.2.body","Select the front battle tank using the normal selection control. Show Me only moves the camera. Keep the supporting tanks, APC and infantry guarding the Fuel yard.","تانک جنگی جلو رو با انتخاب معمول انتخاب کن. «نشان بده» فقط دوربین رو می‌بره. تانک‌های پشتیبان، نفربر و پیاده‌ها رو برای حفاظت محوطهٔ سوخت نگه دار."),
                ("tutorial.3.title","Block the armored route","راه زرهی‌ها رو سد کن"),
                ("tutorial.3.body","Use Move to place the tank at the marked road position beside the supporting armor. Then choose Hold to keep the defense together and begin interception.","با حرکت، تانک رو به جای علامت‌گذاری‌شدهٔ جاده کنار زرهی‌های پشتیبان ببر. بعد «نگه دار» رو بزن تا دفاع کنار هم بمونه و رهگیری شروع بشه."),
                ("tutorial.hold.title","Hold the defensive line","خط دفاع رو نگه دار"),
                ("tutorial.hold.body","Choose the existing Hold command. The tank will cover the approach without chasing away from the supporting armor and protected Fuel reserve. Ordinary Attack can still prioritize a command vehicle.","فرمان معمول «نگه دار» رو بزن. تانک مسیر نزدیک شدن رو پوشش می‌ده و برای تعقیب از زرهی‌های پشتیبان و ذخیرهٔ محافظت‌شدهٔ سوخت دور نمی‌شه. با حملهٔ معمول هنوز می‌تونی خودروی فرماندهی رو در اولویت بذاری."),
                ("tutorial.shortage.title","Defend without moving","بدون حرکت دفاع کن"),
                ("tutorial.shortage.body","Military Fuel cannot cover that move. Select Hold to defend from here and begin the interception. Infantry can still move; use ordinary Attack to prioritize the command vehicles. The 40 civilian barrels stay protected.","سوخت نظامی برای این حرکت کافی نیست. «نگه دار» رو بزن تا از همین‌جا دفاع کنی و رهگیری شروع بشه. پیاده‌ها هنوز می‌تونن حرکت کنن؛ با حملهٔ معمول خودروهای فرماندهی رو در اولویت بذار. ۴۰ بشکهٔ مردم محفوظ می‌مونه."),
                ("tutorial.4.title","Break the lead group","گروه جلو رو متوقف کن"),
                ("tutorial.4.body","The lead group starts after 25 seconds. Keep your counterforce across the road. Reinforce through existing production controls if needed; do not chase vehicles away from the protected reserve.","گروه جلو پس از ۲۵ ثانیه راه می‌افته. نیروی مقابله رو سر جاده نگه دار. اگر لازم شد با تولید معمول نیروی کمکی بیار؛ برای تعقیب خودروها از ذخیرهٔ محافظت‌شده دور نشو."),
                ("tutorial.main.body","Main-column warning: two tanks and an APC enter at 75 seconds. Stop all five vehicles before they reach the Relay and keep the civilian Fuel site operational.","هشدار ستون اصلی: دو تانک و یک نفربر در ثانیهٔ ۷۵ وارد می‌شن. هر پنج خودرو رو قبل از رسیدن به رله متوقف کن و محوطهٔ سوخت مردم رو سالم نگه دار."),
                ("warning.prepare","PREPARATION · Position the front tank; military Fuel is finite.","آماده‌سازی · تانک جلو رو در جای دفاع ببر؛ سوخت نظامی محدوده."),
                ("warning.lead","LEAD GROUP · Armored car and command APC; block the service road.","گروه جلو · خودروی زرهی و نفربر فرماندهی؛ جادهٔ خدماتی رو سد کن."),
                ("warning.main","MAIN COLUMN · Two tanks and a command APC. Protect Fuel and deny Relay access.","ستون اصلی · دو تانک و نفربر فرماندهی. سوخت رو حفظ کن و راه رله رو ببند."),
                ("defense.complete","ARMORED COLUMN STOPPED","ستون زرهی متوقف شد"),
                ("defense.wave","Armored group {0}/{1}","گروه زرهی {0}/{1}"),
                ("defense.countdown","Hold the road · contact in {0}","جاده رو نگه دار · تماس تا {0}"),
                ("defense.fighting","Combined-arms defense · {0} vehicles left","دفاع ترکیبی · {0} خودرو باقی مونده"),
                ("guide.example","Select → Move → Hold; Attack prioritizes a confirmed command vehicle. Armor movement uses real stored Fuel. Reinforce through the existing barracks controls.","انتخاب ← حرکت ← نگه داشتن؛ حمله، خودروی فرماندهی تأییدشده رو در اولویت می‌ذاره. حرکت زرهی از سوخت ذخیره‌شدهٔ واقعی استفاده می‌کنه. با فرمان‌های معمول پادگان نیروی کمکی بیار."),
                ("guide.mistake","Do not chase armor past the reserve or sacrifice civilian Fuel. Infantry protects the site while tanks stop the approach.","زرهی‌ها رو پشت ذخیره تعقیب نکن و سهم سوخت مردم رو قربانی نکن. پیاده‌ها محوطه رو حفظ می‌کنن و تانک‌ها مسیر رو متوقف می‌کنن."),
                ("guide.diagram","ARMORED ROAD → TANKS + INFANTRY → FUEL / RELAY","جادهٔ زرهی ← تانک + پیاده ← سوخت / رله")
            };
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)??throw new InvalidOperationException("Localization missing");
            var locales=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en"&&locale.LocaleCode!="fa-IR"){locales.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var item in copy)entries["mission.steel_push."+item.key]=fa?item.fa:item.en;
                foreach(var line in CH04M02SteelPushCopy.Brief.Concat(CH04M02SteelPushCopy.Comms).Concat(CH04M02SteelPushCopy.Debrief))entries[line.Key]=fa?line.Persian:line.English;
                locales.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,locales);EditorUtility.SetDirty(catalog);
        }
        private static void BuildGuide()
        {
            var guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,GuidePath);}
            var basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            guide.Configure(Enumerable.Range(1,4).Select(i=>new MissionGuideTopic{TitleKey=$"mission.steel_push.tutorial.{i}.title",BodyKey=$"mission.steel_push.tutorial.{i}.body",ExampleKey="mission.steel_push.guide.example",MistakeKey="mission.steel_push.guide.mistake",DiagramKey="mission.steel_push.guide.diagram"}).ToArray(),basis.Classes.ToArray());EditorUtility.SetDirty(guide);
            var root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try{var data=new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));data.FindProperty("steelPushGuide").objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
