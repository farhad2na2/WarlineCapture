using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M05NetworkBreakPresentationBuilder
    {
        public const string GuidePath = "Assets/Game/Configs/Missions/Chapter03/CH03M05_NetworkBreak_FieldGuide.asset";
        private const string PreviewPath = CH03M05NetworkBreakNarrativeBuilder.ArtRoot + "/CH03M05_NetworkBreak.png";

        [MenuItem("Game/Campaign/Network Break/Build Presentation")]
        public static void Build()
        {
            SeedLocalization(); BuildGuide(); BindArt(); AssetDatabase.SaveAssets();
            Debug.Log("[NetworkBreakPresentation] result=Passed locales=en,fa-IR preview=clean controls=existing-only");
        }

        private static void SeedLocalization()
        {
            var copy = new (string key, string en, string fa)[]
            {
                ("name", "Network Break", "شکستن شبکه"),
                ("summary", "Scan the verified bunker approach. Breach its controlled gate, disable the erasure node, then secure ARIA's sealed audit. Protected homes are off limits.", "راهِ تأییدشدهٔ پناهگاه رو بررسی کن. از دروازهٔ کنترل‌شده عبور کن، گرهٔ پاک‌سازی رو از کار بنداز و گزارش مُهرشدهٔ آریا رو سالم نگه دار. خانه‌های حفاظت‌شده هدف نیستن."),
                ("location", "Sahrin · Ash Line audit bunker", "سارین · پناهگاه بازرسی خط خاکستر"),
                ("enemy_intel", "Qassem is preparing an archive erasure. Expect a counterattack once the gate is hit. Do not fire on unverified buildings.", "قاسم برای پاک‌سازی بایگانی آماده می‌شه. با حمله به دروازه، ضدحمله شروع می‌شه. به ساختمان‌های تأییدنشده شلیک نکن."),
                ("objective.breach", "Breach verified gate", "از دروازهٔ تأییدشده عبور کن"),
                ("objective.disable", "Stop erasure node", "گرهٔ پاک‌سازی رو خاموش کن"),
                ("objective.archive", "Secure the audit", "گزارش رو حفظ کن"),
                ("star.1", "Complete the mission", "مأموریت رو کامل کن"),
                ("star.2", "Keep the APC intact", "نفربر رو سالم نگه دار"),
                ("star.3", "Finish within 9 minutes", "در کمتر از ۹ دقیقه تمام کن"),
                ("resources", "Scan first · protected homes off limits", "اول بررسی کن · خانه‌ها هدف نیستن"),
                ("forces", "8 rifles · heavy APC · no aircraft", "۸ تفنگدار · نفربر سنگین · بدون هواگرد"),
                ("deadline", "11 active minutes", "۱۱ دقیقه زمان فعال"),
                ("reward.card", "1,500 Commander XP · 7,000 Credits", "۱۵۰۰ تجربهٔ فرمانده · ۷۰۰۰ اعتبار"),
                ("result.victory", "AUDIT PRESERVED", "گزارش حفظ شد"),
                ("result.defeat", "NETWORK BREAK FAILED", "شکستن شبکه شکست خورد"),
                ("result.chapter", "CHAPTER 3 COMPLETE", "فصل سوم کامل شد"),
                ("result.unit_losses", "UNITS LOST", "نیروهای از دست‌رفته"),
                ("result.support", "HEAVY APC", "نفربر سنگین"),
                ("result.lost", "LOST", "از دست رفت"),
                ("result.survived", "INTACT", "سالم"),
                ("result.success", "The verified node is down and ARIA's sealed audit is intact. Its traffic confirms Vanguard moving toward the city.", "گرهٔ تأییدشده خاموش شد و گزارش مُهرشدهٔ آریا سالم موند. ترافیکش حرکت ونگارد به سوی شهر رو تأیید می‌کنه."),
                ("result.loss", "The team could not secure the archive. Regroup before Qassem completes the erasure.", "گروه نتونست بایگانی رو حفظ کنه. پیش از پاک‌سازی قاسم دوباره آماده شو."),
                ("result.timeout", "Qassem erased the audit before the team secured it.", "قاسم پیش از رسیدن گروه گزارش رو پاک کرد."),
                ("result.setup", "The bunker encounter could not be verified. No reward was settled.", "درگیری پناهگاه قابل تأیید نبود. پاداشی ثبت نشد."),
                ("hud.status", "Audit hold {0}s · guards {1} · time {2} · need {3}s", "حفظ گزارش {0} ثانیه · نگهبان {1} · زمان {2} · نیاز {3} ثانیه"),
                ("hud.counterattack", "Qassem's counterattack in {0}s", "ضدحملهٔ قاسم تا {0} ثانیه"),
                ("hud.recovery.complete", "Sealed audit secured", "گزارش مُهرشده حفظ شد"),
                ("hud.recovery.enemies", "Clear armed threats from the archive", "نیروهای مسلح رو از بایگانی دور کن"),
                ("hud.recovery.reinforcements", "Cover the archive; counterattack incoming", "بایگانی رو پوشش بده؛ ضدحمله در راهه"),
                ("hud.recovery.progress", "Hold the audit for {0}s more", "گزارش رو {0} ثانیهٔ دیگه حفظ کن"),
                ("hud.recovery.enter", "Move a surviving escort into the archive ring", "یک نیروی همراهِ زنده رو به محدودهٔ بایگانی ببر"),
                ("guide.title", "Network Break field guide", "راهنمای شکستن شبکه"),
                ("guide.example", "Use Select, Move, Attack and Hold. ARIA's Show Me points to the next action but never orders your troops.", "از انتخاب، حرکت، حمله و نگه داشتن استفاده کن. «نشان بده» آریا کار بعدی رو نشان می‌ده، اما به نیروهات فرمان نمی‌ده."),
                ("guide.mistake", "Do not attack protected homes or the archive. Disable only the mapped gate and erasure node.", "به خانه‌های حفاظت‌شده یا بایگانی حمله نکن. فقط دروازه و گرهٔ پاک‌سازیِ روی نقشه رو از کار بنداز."),
                ("guide.diagram", "SCAN → GATE → ERASURE NODE → ARCHIVE HOLD", "بررسی ← دروازه ← گرهٔ پاک‌سازی ← حفظ بایگانی")
            };
            string[] titles = {"Confirm the marked route", "Select your squad", "Open the gate", "Move through the breach", "Disable the erasure node", "Stop the counterattack", "Reach the archive", "Hold the archive"};
            string[] bodies = {
                "Use ARIA's Show Me to inspect the mapped gate and protected homes. Only the gate and erasure node are authorized targets; continue after verification.",
                "Select the rifle escort or APC with the normal unit selection control.",
                "Use Attack on the marked access gate. Keep fire away from unverified buildings.",
                "Use Move to take the rifles through the open gate toward the marked bunker approach.",
                "Attack the verified erasure node. Preserve the nearby physical archive.",
                "Qassem's counterattack is arriving. Attack its armed units and cover the archive approach.",
                "Move a surviving escort into the archive ring. Do not attack the archive.",
                "Hold the clear archive for 15 seconds while the sealed audit is secured."
            };
            string[] faTitles = {"مسیر علامت‌گذاری‌شده رو تأیید کن", "گروهت رو انتخاب کن", "دروازه رو باز کن", "از شکاف عبور کن", "گرهٔ پاک‌سازی رو از کار بنداز", "ضدحمله رو متوقف کن", "به بایگانی برس", "بایگانی رو حفظ کن"};
            string[] faBodies = {
                "با «نشان بده» آریا دروازهٔ روی نقشه و خانه‌های حفاظت‌شده رو بررسی کن. فقط دروازه و گرهٔ پاک‌سازی هدف مجازن؛ پس از تأیید ادامه بده.",
                "با فرمان معمول انتخاب، تفنگدارها یا نفربر رو انتخاب کن.",
                "با فرمان حمله، دروازهٔ مشخص‌شده رو بزن. به ساختمان‌های تأییدنشده شلیک نکن.",
                "با فرمان حرکت، تفنگدارها رو از دروازهٔ باز به سمت پناهگاه ببر.",
                "به گرهٔ پاک‌سازیِ تأییدشده حمله کن. بایگانی اصلی نزدیک اون باید سالم بمونه.",
                "ضدحملهٔ قاسم در راهه. به نیروهای مسلحش حمله کن و مسیر بایگانی رو پوشش بده.",
                "نیروی همراهِ زنده رو به محدودهٔ بایگانی ببر. به خود بایگانی حمله نکن.",
                "بایگانیِ امن رو ۱۵ ثانیه نگه دار تا گزارش مُهرشده ضبط بشه."
            };
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)
                ?? throw new InvalidOperationException("Localization catalog missing");
            var locales = new List<GameLocaleTable>();
            foreach (var locale in catalog.Locales)
            {
                if (locale.LocaleCode != "en" && locale.LocaleCode != "fa-IR") {locales.Add(locale); continue;}
                bool fa = locale.LocaleCode == "fa-IR";
                var entries = locale.Entries.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
                foreach (var line in copy) entries["mission.network_break." + line.key] = fa ? line.fa : line.en;
                for (int i = 0; i < titles.Length; i++)
                {
                    entries[$"mission.network_break.tutorial.{i + 1}.title"] = fa ? faTitles[i] : titles[i];
                    entries[$"mission.network_break.tutorial.{i + 1}.body"] = fa ? faBodies[i] : bodies[i];
                }
                foreach (var line in CH03M05NetworkBreakCopy.Brief.Concat(CH03M05NetworkBreakCopy.Comms).Concat(CH03M05NetworkBreakCopy.Debrief))
                    entries[line.Key] = fa ? line.Persian : line.English;
                locales.Add(new GameLocaleTable(locale.LocaleCode, locale.DisplayName, locale.ShortLabel,
                    locale.RightToLeft, locale.FontAsset,
                    entries.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new GameLocalizedStringRecord(x.Key, x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode, locales); EditorUtility.SetDirty(catalog);
        }

        private static void BuildGuide()
        {
            var guide = AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if (guide == null) {guide = ScriptableObject.CreateInstance<MissionFieldGuideConfig>(); AssetDatabase.CreateAsset(guide, GuidePath);}
            var basis = AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            var classes = basis.Classes.ToArray();
            for (int i = 0; i < classes.Length; i++)
                if (classes[i].Availability != MissionGuideAvailability.Unavailable)
                    classes[i].Availability = MissionGuideAvailability.Reference;
            guide.Configure(Enumerable.Range(1, 8).Select(i => new MissionGuideTopic
            {
                TitleKey = $"mission.network_break.tutorial.{i}.title",
                BodyKey = $"mission.network_break.tutorial.{i}.body",
                ExampleKey = "mission.network_break.guide.example",
                MistakeKey = "mission.network_break.guide.mistake",
                DiagramKey = "mission.network_break.guide.diagram"
            }).ToArray(), classes);
            EditorUtility.SetDirty(guide);
            GameObject root = PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try
            {
                var data = new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));
                data.FindProperty("networkBreakGuide").objectReferenceValue = guide;
                data.ApplyModifiedPropertiesWithoutUndo();
                MissionUiSerializedBindingsAuthoring.Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, M03RadarWarningUiBuilder.GuidePath);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }

        private static void BindArt()
        {
            Texture art = AssetDatabase.LoadAssetAtPath<Texture>(PreviewPath) ?? throw new InvalidOperationException(PreviewPath);
            foreach (string path in new[] {"Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab",
                "Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab"})
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var campaign = root.GetComponentInChildren<CampaignOperationsScreenView>(true);
                    UnityEngine.Object view = campaign != null ? (UnityEngine.Object)campaign : root.GetComponentInChildren<MissionBriefingScreenView>(true);
                    var data = new SerializedObject(view);
                    data.FindProperty(campaign != null ? "networkBreakMissionPreview" : "networkBreakMissionArt").objectReferenceValue = art;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally {PrefabUtility.UnloadPrefabContents(root);}
            }
            const string resultPath="Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(resultPath) != null)
            {
                GameObject root=PrefabUtility.LoadPrefabContents(resultPath);
                try
                {
                    var view=root.GetComponentInChildren<MissionResultPopupView>(true);
                    var data=new SerializedObject(view);
                    data.FindProperty("networkBreakResultBackdrop").objectReferenceValue=
                        AssetDatabase.LoadAssetAtPath<Texture>(CH03M05NetworkBreakNarrativeBuilder.ArtRoot+"/CH03M05_NetworkBreak_Debrief.png");
                    data.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root,resultPath);
                }
                finally {PrefabUtility.UnloadPrefabContents(root);}
            }
        }

        [MenuItem("Game/Campaign/Network Break/Build Player-Ready Checkpoint")]
        public static void BuildCheckpoint()
        {
            CH03M05NetworkBreakConfigBuilder.Build(); Build(); CH03M05NetworkBreakNarrativeBuilder.BuildAndInstall();
            Debug.Log("[NetworkBreakCheckpoint] result=Passed scope=CampaignConfiguration voices=local controls=existing-only");
        }
    }
}
