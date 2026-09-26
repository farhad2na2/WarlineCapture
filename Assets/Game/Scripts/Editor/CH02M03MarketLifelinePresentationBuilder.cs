using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH02M03MarketLifelinePresentationBuilder
    {
        [MenuItem("Game/Campaign/Market Lifeline/Build Presentation")]
        public static void Build()
        {
            SeedLocalization();
            BuildGuide();
            BindPreviewArt();
            AssetDatabase.SaveAssets();
            Debug.Log("[MarketLifelinePresentation] result=Passed locales=en,fa-IR controls=existing-only");
        }

        private static void SeedLocalization()
        {
            var copy = new (string key,string en,string fa)[]
            {
                ("name","Market Lifeline","شریان بازار"),
                ("summary","Compare two authored manifests, escort the legitimate relief convoy and expose the copied Relay transfer without closing Old Market.","دو بارنامهٔ آماده رو مقایسه کن، کاروان امدادی واقعی رو برسون و انتقال کپی‌شدهٔ شبکه رو بدون بستن بازار قدیمی افشا کن."),
                ("location","Old Market exchange yard","محوطهٔ مبادلهٔ بازار قدیمی"),
                ("enemy_intel","Two armed Ash Line teams are hiding among the freight approaches—not among the market workers.","دو گروه مسلح خط خاکستر در مسیرهای باربری پنهان شدن، نه بین کارگرهای بازار."),
                ("objective.delivery","Deliver all three relief trucks","هر سه کامیون امدادی رو برسون"),
                ("objective.manifest","Compare both manifest transfers","هر دو انتقال بارنامه رو مقایسه کن"),
                ("objective.market","Keep Old Market open","بازار قدیمی رو باز نگه دار"),
                ("objective.delivery.body","After both inspections, escort only the three legitimate relief trucks to the delivery yard. Delivering the separate copied transfer fails the operation.","بعد از هر دو بررسی، فقط سه کامیون امدادی واقعی رو به محوطهٔ تحویل برسون. تحویل انتقال کپی‌شدهٔ جداگانه باعث شکست عملیات می‌شه."),
                ("objective.manifest.body","Move a rifle squad to each marked manifest table and hold for 6 seconds at each. Yasin will identify the legitimate trade pattern after both records are inspected.","یک گروه تفنگدار رو به هر میز بارنامهٔ مشخص‌شده ببر و در هرکدوم ۶ ثانیه نگه دار. یاسین بعد از بررسی هر دو سند، الگوی دادوستد واقعی رو مشخص می‌کنه."),
                ("objective.market.body","Defeat the armed teams, then keep the delivery yard secure for 10 seconds without blocking the market route.","گروه‌های مسلح رو شکست بده، بعد محوطهٔ تحویل رو ۱۰ ثانیه امن نگه دار، بدون اینکه مسیر بازار بسته بشه."),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","Lose no rifle soldiers","هیچ تفنگداری رو از دست نده"),
                ("star.3","Finish within 7 minutes","در کمتر از ۷ دقیقه تمام کن"),
                ("resources","Two manifests · three relief trucks · one copied transfer","دو بارنامه · سه کامیون امدادی · یک انتقال کپی‌شده"),
                ("forces","8 rifles · 3 legitimate relief trucks · 1 held transfer","۸ تفنگدار · ۳ کامیون امدادی واقعی · ۱ انتقال توقیف‌شده"),
                ("deadline","10 active minutes","۱۰ دقیقه زمان فعال"),
                ("reward.card","900 Commander XP · 4,500 Credits","۹۰۰ تجربهٔ فرمانده · ۴۵۰۰ اعتبار"),
                ("result.victory","Market lifeline restored","شریان بازار برقرار شد"),
                ("result.defeat","Market relief interrupted","امدادرسانی بازار متوقف شد"),
                ("result.success","Relief reached Old Market, legitimate trade stayed open and the false manifests exposed Relay-era storage sites.","امداد به بازار قدیمی رسید، دادوستد عادی باز موند و بارنامه‌های جعلی انبارهای قدیمی شبکه رو آشکار کرد."),
                ("failure.squad","The escort squads were lost. Retry and keep rifles between the convoy and the armed teams.","گروه‌های اسکورت از دست رفتن. دوباره تلاش کن و تفنگدارها رو بین کاروان و گروه‌های مسلح نگه دار."),
                ("failure.convoy","A relief truck was destroyed. All three loads must reach Old Market.","یک کامیون امدادی نابود شد. هر سه محموله باید به بازار قدیمی برسن."),
                ("failure.wrong_transfer","The copied transfer entered the civilian delivery yard. Compare both manifests and escort only Yasin's legitimate relief convoy.","انتقال کپی‌شده وارد محوطهٔ تحویل غیرنظامی شد. هر دو بارنامه رو مقایسه کن و فقط کاروان امدادی واقعی یاسین رو برسون."),
                ("failure.deadline","The relief loads did not reach Old Market within ten minutes.","محموله‌های امدادی تا پایان ده دقیقه به بازار قدیمی نرسیدن."),
                ("failure.integrity","Mission state could not be verified. Return and retry; no rewards were settled.","وضعیت عملیات قابل تأیید نیست. برگرد و دوباره تلاش کن؛ پاداشی ثبت نشده."),
                ("guide.title","Market Lifeline field guide","راهنمای شریان بازار"),
                ("guide.1.title","Inspect the legitimate manifest","بارنامهٔ واقعی رو بررسی کن"),
                ("guide.1.body","Move one rifle squad to Yasin's marked manifest table and hold there for 6 seconds.","یک گروه تفنگدار رو به میز بارنامهٔ مشخص‌شدهٔ یاسین ببر و ۶ ثانیه همون‌جا نگه دار."),
                ("guide.2.title","Inspect the copied manifest","بارنامهٔ کپی‌شده رو بررسی کن"),
                ("guide.2.body","Move the squad to the second marked table and hold for 6 seconds. ARIA and Yasin compare both records.","گروه رو به میز دوم ببر و ۶ ثانیه نگه دار. آریا و یاسین هر دو سند رو مقایسه می‌کنن."),
                ("guide.3.title","Escort the legitimate convoy","کاروان واقعی رو اسکورت کن"),
                ("guide.3.body","Move only the three relief trucks to the delivery yard. Leave the separate copied-transfer truck outside.","فقط سه کامیون امدادی رو به محوطهٔ تحویل ببر. کامیون جداگانهٔ انتقال کپی‌شده رو بیرون نگه دار."),
                ("guide.4.title","Remove the armed threat","تهدید مسلح رو از بین ببر"),
                ("guide.4.body","Select rifles, press Attack and tap a confirmed hostile. Keep civilians and trucks behind the squad.","تفنگدارها رو انتخاب کن، «حمله» رو بزن و روی دشمن تأییدشده بزن. غیرنظامی‌ها و کامیون‌ها رو پشت گروه نگه دار."),
                ("guide.5.title","Hold the market open","بازار رو باز نگه دار"),
                ("guide.5.body","After delivery and comparison, keep the yard clear for 10 seconds. Ordinary trade continues automatically.","بعد از تحویل و مقایسه، محوطه رو ۱۰ ثانیه امن نگه دار. دادوستد عادی خودکار ادامه پیدا می‌کنه."),
                ("guide.example","Use only Select, Move, Attack and Hold. ARIA can demonstrate the same orders.","فقط از انتخاب، حرکت، حمله و توقف استفاده کن. آریا هم می‌تونه همین فرمان‌ها رو اجرا کنه."),
                ("guide.mistake","Do not deliver the separate copied-transfer truck. Inspect both tables before committing the relief convoy.","کامیون جداگانهٔ انتقال کپی‌شده رو تحویل نده. قبل از فرستادن کاروان امداد، هر دو میز رو بررسی کن."),
                ("guide.diagram","2 MANIFESTS → COMPARE · 3 RELIEF TRUCKS → DELIVER · COPY → HOLD","۲ بارنامه ← مقایسه · ۳ کامیون امداد ← تحویل · نسخهٔ کپی ← توقف")
            };
            GameLocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            if(catalog == null) throw new InvalidOperationException("Localization catalog missing.");
            var tables = new List<GameLocaleTable>();
            foreach(GameLocaleTable locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en" && locale.LocaleCode!="fa-IR"){tables.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var line in copy) entries["mission.market_lifeline."+line.key]=fa?line.fa:line.en;
                foreach(var line in CH02M03MarketLifelineCopy.Brief.Concat(CH02M03MarketLifelineCopy.Comms).Concat(CH02M03MarketLifelineCopy.Debrief)) entries[line.Key]=fa?line.Persian:line.English;
                tables.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,tables);EditorUtility.SetDirty(catalog);
        }

        private static void BuildGuide()
        {
            const string path="Assets/Game/Configs/Missions/Chapter02/CH02M03_MarketLifeline_FieldGuide.asset";
            MissionFieldGuideConfig guide=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(path);
            if(guide==null){guide=ScriptableObject.CreateInstance<MissionFieldGuideConfig>();AssetDatabase.CreateAsset(guide,path);}
            MissionFieldGuideConfig basis=AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            MissionGuideClass[] classes=basis.Classes.ToArray();
            for(int i=0;i<classes.Length;i++)if(classes[i].Availability!=MissionGuideAvailability.Unavailable)classes[i].Availability=MissionGuideAvailability.Reference;
            guide.Configure(Enumerable.Range(1,5).Select(i=>new MissionGuideTopic{TitleKey=$"mission.market_lifeline.guide.{i}.title",BodyKey=$"mission.market_lifeline.guide.{i}.body",ExampleKey="mission.market_lifeline.guide.example",MistakeKey="mission.market_lifeline.guide.mistake",DiagramKey="mission.market_lifeline.guide.diagram"}).ToArray(),classes);
            EditorUtility.SetDirty(guide);
            GameObject root=PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try
            {
                SerializedObject data=new(root.GetComponentInChildren<MissionFieldGuideView>(true));
                data.FindProperty("marketLifelineGuide").objectReferenceValue=guide;data.ApplyModifiedPropertiesWithoutUndo();
                MissionUiSerializedBindingsAuthoring.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,M03RadarWarningUiBuilder.GuidePath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        private static void BindPreviewArt()
        {
            Texture art=AssetDatabase.LoadAssetAtPath<Texture>(CH02M03MarketLifelineMediaImporter.ArtRoot+"/OldMarket.png")??throw new InvalidOperationException("Market Lifeline preview is missing.");
            foreach(string path in new[]{"Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab","Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab"})
            {
                GameObject root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    CampaignOperationsScreenView campaign=root.GetComponentInChildren<CampaignOperationsScreenView>(true);
                    UnityEngine.Object view=campaign!=null?(UnityEngine.Object)campaign:root.GetComponentInChildren<MissionBriefingScreenView>(true);
                    SerializedObject data=new(view);data.FindProperty(campaign!=null?"marketLifelineMissionPreview":"marketLifelineMissionArt").objectReferenceValue=art;data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }

        [MenuItem("Game/Campaign/Market Lifeline/Build Player-Ready Checkpoint")]
        public static void BuildCheckpoint()
        {
            CH02M03MarketLifelineRulesValidation.Run();CH02M03MarketLifelineConfigBuilder.Build();Build();CH02M03MarketLifelineNarrativeBuilder.BuildCaptionedArtAndInstall();
            Debug.Log("[MarketLifelineCheckpoint] result=Passed scope=CaptionedConfiguration controls=existing-only");
        }
    }
}
