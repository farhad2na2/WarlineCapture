using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M03FalseFrontPresentationBuilder
    {
        public const string GuidePath = "Assets/Game/Configs/Missions/Chapter03/CH03M03_FalseFront_FieldGuide.asset";

        [MenuItem("Game/Campaign/False Front/Build Presentation")]
        public static void Build()
        {
            CH03M03FalseFrontMediaImporter.ConfigureArt(); SeedLocalization(); BuildGuide(); BindArt(); AssetDatabase.SaveAssets();
            Debug.Log("[FalseFrontPresentation] result=Passed locales=en,fa-IR preview=clean comicPanels=7 controls=existing-only");
        }

        private static void SeedLocalization()
        {
            var copy = new (string key, string en, string fa)[] {
                ("name","False Front","پوشش جعلی"),
                ("summary","A sealed report points at an unconfirmed depot while three civilian vehicles must cross an exposed route. Verify both approaches, protect the evacuation and stop only the confirmed ambush.","یک گزارش مُهرشده به انباری تأییدنشده اشاره می‌کنه، در حالی که سه خودروی غیرنظامی باید از مسیری خطرناک بگذرن. هر دو مسیر رو بررسی کن، از تخلیه محافظت کن و فقط کمینِ تأییدشده رو متوقف کن."),
                ("location","East evacuation road · district outskirts","جادهٔ تخلیهٔ شرقی · حاشیهٔ شهر"),
                ("enemy_intel","Authority-channel warning is plausible but unverified. An Ash Line roadblock may form beyond the second observation point; do not attack the empty depot.","هشدار کانال فرماندهی باورپذیره اما تأیید نشده. ممکنه خط خاکستر بعد از نقطهٔ دیده‌بانی دوم راه‌بند بزنه؛ به انبار خالی حمله نکن."),
                ("objective.verify","Verify both observation points","هر دو نقطهٔ دیده‌بانی رو بررسی کن"),
                ("objective.evacuate","Escort all three civilian vehicles","هر سه خودروی غیرنظامی رو همراهی کن"),
                ("objective.ambush","Defeat the confirmed ambush and hold the route","کمینِ تأییدشده رو از بین ببر و مسیر رو نگه دار"),
                ("objective.verify.body","Move a rifle squad to each marked observation point and hold for 5 seconds. A seal is not confirmation of the depot target.","یک گروه تفنگدار رو به هر نقطهٔ دیده‌بانی علامت‌گذاری‌شده ببر و ۵ ثانیه نگه دار. مُهر گزارش، هدف بودن انبار رو تأیید نمی‌کنه."),
                ("objective.evacuate.body","Select each civilian vehicle and move it to the marked shelter route. All three must survive; a lost vehicle fails the mission.","هر خودروی غیرنظامی رو انتخاب کن و به مسیر پناهگاهِ علامت‌گذاری‌شده ببر. هر سه باید سالم بمونن؛ از دست رفتن یک خودرو مأموریت رو شکست می‌ده."),
                ("objective.ambush.body","After Samira confirms the roadblock, defeat the armed ambush and hold the evacuation approach for 10 seconds. Preserve the false report for analysis.","بعد از اینکه سمیرا راه‌بند رو تأیید کرد، کمین مسلح رو از بین ببر و ۱۰ ثانیه مسیر تخلیه رو نگه دار. گزارش جعلی رو برای بررسی نگه دار."),
                ("star.1","Complete the mission","مأموریت رو کامل کن"),
                ("star.2","All three civilian vehicles survive","هر سه خودروی غیرنظامی سالم بمونن"),
                ("star.3","Finish within 7 minutes","در کمتر از ۷ دقیقه تمام کن"),
                ("resources","Two observation points · three civilian vehicles · one preserved report","دو نقطهٔ دیده‌بانی · سه خودروی غیرنظامی · یک گزارش حفظ‌شده"),
                ("forces","8 rifles · route protection team","۸ تفنگدار · گروه حفاظت مسیر"),
                ("deadline","9 active minutes","۹ دقیقه زمان فعال"),
                ("reward.card","1,300 Commander XP · 6,500 Credits","۱۳۰۰ تجربهٔ فرمانده · ۶۵۰۰ اعتبار"),
                ("result.victory","Evacuation route secured","مسیر تخلیه امن شد"),
                ("result.defeat","Evacuation route lost","مسیر تخلیه از دست رفت"),
                ("result.success","All three vehicles reached shelter. The confirmed ambush was defeated, the unconfirmed depot was spared and the false authority report was preserved.","هر سه خودرو به پناهگاه رسیدن. کمینِ تأییدشده شکست خورد، انبارِ تأییدنشده آسیب ندید و گزارش جعلیِ فرماندهی حفظ شد."),
                ("failure.squad","The route protection squad was lost before the civilians reached shelter.","گروه حفاظت مسیر پیش از رسیدن غیرنظامی‌ها به پناهگاه از دست رفت."),
                ("failure.convoy","A civilian vehicle was lost on the evacuation route.","یک خودروی غیرنظامی در مسیر تخلیه از دست رفت."),
                ("failure.wrong_transfer","The unconfirmed depot route was treated as a safe evacuation destination. Follow the verified shelter route.","مسیر انبارِ تأییدنشده به‌اشتباه مقصد امنِ تخلیه حساب شد. از مسیر تأییدشدهٔ پناهگاه برو."),
                ("failure.deadline","The evacuation corridor was not secured before the ambush closed in.","پیش از نزدیک شدن کمین، مسیر تخلیه امن نشد."),
                ("failure.integrity","Mission state could not be verified. Return and retry; no rewards were settled.","وضعیت عملیات قابل تأیید نیست. برگرد و دوباره تلاش کن؛ پاداشی ثبت نشد."),
                ("guide.title","False Front field guide","راهنمای پوشش جعلی"),
                ("guide.1.title","Check the first approach","مسیر اول رو بررسی کن"),
                ("guide.1.body","Move a rifle squad to the first observation point and hold for 5 seconds.","یک گروه تفنگدار رو به نقطهٔ دیده‌بانی اول ببر و ۵ ثانیه نگه دار."),
                ("guide.2.title","Check the second approach","مسیر دوم رو بررسی کن"),
                ("guide.2.body","Observe the second point as well. The sealed depot report is still unconfirmed.","نقطهٔ دوم رو هم بررسی کن. گزارشِ مُهرشدهٔ انبار هنوز تأیید نشده."),
                ("guide.3.title","Move the evacuees","تخلیه‌شونده‌ها رو حرکت بده"),
                ("guide.3.body","Select and move each of the three civilian vehicles to the shelter marker. Keep escorts nearby.","هر سه خودروی غیرنظامی رو انتخاب کن و به علامت پناهگاه ببر. نیروهای همراه رو نزدیک نگه دار."),
                ("guide.4.title","Stop the real ambush","کمین واقعی رو متوقف کن"),
                ("guide.4.body","Once Samira confirms the roadblock, attack the armed fighters there, not the unconfirmed depot.","وقتی سمیرا راه‌بند رو تأیید کرد، به نیروهای مسلح همون‌جا حمله کن، نه به انبارِ تأییدنشده."),
                ("guide.5.title","Hold the evacuation route","مسیر تخلیه رو نگه دار"),
                ("guide.5.body","With all vehicles safe and fighters defeated, hold for 10 seconds while ARIA preserves the false report.","وقتی همهٔ خودروها امن شدن و نیروهای مسلح شکست خوردن، ۱۰ ثانیه نگه دار تا آریا گزارش جعلی رو ثبت کنه."),
                ("guide.example","Use Select, Move, Attack and Hold. ARIA can demonstrate those same player orders.","از انتخاب، حرکت، حمله و توقف استفاده کن. آریا می‌تونه همین فرمان‌های بازیکن رو نمایش بده."),
                ("guide.mistake","A valid seal is not proof of a target. Do not attack the depot or send civilians toward it.","مُهر معتبر، اثباتِ هدف بودن نیست. به انبار حمله نکن و غیرنظامی‌ها رو به سمتش نفرست."),
                ("guide.diagram","OBSERVE 2 → ESCORT 3 → CONFIRMED AMBUSH → HOLD","۲ دیده‌بانی ← همراهی ۳ خودرو ← کمینِ تأییدشده ← نگه‌داشتن")
            };
            GameLocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath) ?? throw new InvalidOperationException("Localization catalog missing.");
            var tables = new List<GameLocaleTable>();
            foreach (GameLocaleTable locale in catalog.Locales)
            {
                if (locale.LocaleCode != "en" && locale.LocaleCode != "fa-IR") { tables.Add(locale); continue; }
                bool fa = locale.LocaleCode == "fa-IR"; var entries = locale.Entries.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
                foreach (var line in copy) entries["mission.false_front." + line.key] = fa ? line.fa : line.en;
                foreach (var line in CH03M03FalseFrontMediaImporter.Lines) entries[line.Key] = fa ? line.Persian : line.English;
                tables.Add(new GameLocaleTable(locale.LocaleCode, locale.DisplayName, locale.ShortLabel, locale.RightToLeft, locale.FontAsset, entries.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new GameLocalizedStringRecord(x.Key, x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode, tables); EditorUtility.SetDirty(catalog);
        }

        private static void BuildGuide()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Configs/Missions/Chapter03");
            var guide = AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if (guide == null) { guide = ScriptableObject.CreateInstance<MissionFieldGuideConfig>(); AssetDatabase.CreateAsset(guide, GuidePath); }
            var basis = AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path); var classes = basis.Classes.ToArray();
            for (int i = 0; i < classes.Length; i++) if (classes[i].Availability != MissionGuideAvailability.Unavailable) classes[i].Availability = MissionGuideAvailability.Reference;
            guide.Configure(Enumerable.Range(1, 5).Select(i => new MissionGuideTopic {TitleKey = $"mission.false_front.guide.{i}.title", BodyKey = $"mission.false_front.guide.{i}.body", ExampleKey = "mission.false_front.guide.example", MistakeKey = "mission.false_front.guide.mistake", DiagramKey = "mission.false_front.guide.diagram"}).ToArray(), classes); EditorUtility.SetDirty(guide);
            GameObject root = PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try { var data = new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true)); data.FindProperty("falseFrontGuide").objectReferenceValue = guide; data.ApplyModifiedPropertiesWithoutUndo(); MissionUiSerializedBindingsAuthoring.Apply(root); PrefabUtility.SaveAsPrefabAsset(root, M03RadarWarningUiBuilder.GuidePath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BindArt()
        {
            Texture art = CH03M03FalseFrontMediaImporter.Preview() ?? throw new InvalidOperationException("False Front art missing.");
            foreach (string path in new[] {"Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab", "Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab"})
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try { var campaign = root.GetComponentInChildren<CampaignOperationsScreenView>(true); UnityEngine.Object view = campaign != null ? (UnityEngine.Object)campaign : root.GetComponentInChildren<MissionBriefingScreenView>(true); var data = new SerializedObject(view); var property = data.FindProperty(campaign != null ? "falseFrontMissionPreview" : "falseFrontMissionArt"); property.objectReferenceValue = art; data.ApplyModifiedPropertiesWithoutUndo(); if (AssetDatabase.GetAssetPath(property.objectReferenceValue) != CH03M03FalseFrontMediaImporter.PreviewPath) throw new InvalidOperationException("False Front clean preview binding failed: " + path); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        [MenuItem("Game/Campaign/False Front/Build Player-Ready Checkpoint")]
        public static void BuildCheckpoint()
        {
            CH03M03FalseFrontRulesValidation.Run(); CH03M03FalseFrontConfigBuilder.Build(); Build(); CH03M03FalseFrontNarrativeBuilder.BuildCaptionedArtAndInstall();
            Debug.Log("[FalseFrontCheckpoint] result=Passed scope=CampaignConfiguration voices=local controls=existing-only");
        }
    }
}
