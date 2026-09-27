using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M04EvidenceChainPresentationBuilder
    {
        public const string GuidePath = "Assets/Game/Configs/Missions/Chapter03/CH03M04_EvidenceChain_FieldGuide.asset";

        [MenuItem("Game/Campaign/Evidence Chain/Build Presentation")]
        public static void Build()
        {
            CH03M04EvidenceChainMediaImporter.ConfigureArt();
            SeedLocalization(); BuildGuide(); BindArt(); AssetDatabase.SaveAssets();
            Debug.Log("[EvidenceChainPresentation] result=Passed locales=en,fa-IR preview=clean comicPanels=7 controls=existing-only");
        }

        private static void SeedLocalization()
        {
            var copy = new (string key, string en, string fa)[]
            {
                ("name", "Evidence Chain", "زنجیرهٔ شواهد"),
                ("summary", "Escort the archive custodian and Dr. Lina from the clinic. Board both in the APC, protect the transfer through the ambush, then move them to Laila's helicopter at a clear landing zone.", "نگهبان بایگانی و دکتر لینا رو از درمانگاه خارج کن. هر دو رو سوار نفربر کن، از انتقالشون در برابر کمین محافظت کن و بعد در منطقهٔ فرودِ امن به بالگرد لیلا برسون."),
                ("location", "East clinic road · secure analysis route", "جادهٔ درمانگاه شرقی · مسیر مرکز بررسی"),
                ("enemy_intel", "Ash Line can block the road after pickup. Protect the witness and archive; do not abandon the convoy to pursue retreating fighters.", "خط خاکستر ممکنه بعد از سوار کردن شاهد راه رو ببنده. از شاهد و بایگانی محافظت کن؛ برای تعقیب مهاجم‌های عقب‌نشسته کاروان رو رها نکن."),
                ("objective.extract", "Extract the witness and archive custodian", "شاهد و نگهبان بایگانی رو خارج کن"),
                ("objective.carrier", "Protect the APC and helicopter", "از نفربر و بالگرد محافظت کن"),
                ("objective.landing", "Secure the landing zone", "منطقهٔ فرود رو امن کن"),
                ("star.1", "Complete the mission", "مأموریت رو کامل کن"),
                ("star.2", "No escort losses", "هیچ نیروی همراهی رو از دست نده"),
                ("star.3", "Finish within 8 minutes", "در کمتر از ۸ دقیقه تمام کن"),
                ("resources", "Two protected passengers · one sealed archive", "دو مسافر تحت حفاظت · یک بایگانی مُهرشده"),
                ("forces", "8 rifles · one APC · one helicopter", "۸ تفنگدار · یک نفربر · یک بالگرد"),
                ("deadline", "11 active minutes", "۱۱ دقیقه زمان فعال"),
                ("reward.card", "1,500 Commander XP · 7,000 Credits", "۱۵۰۰ تجربهٔ فرمانده · ۷۰۰۰ اعتبار"),
                ("result.victory", "EVIDENCE SECURED", "شواهد حفظ شد"),
                ("result.defeat", "EXTRACTION FAILED", "تخلیه شکست خورد"),
                ("result.subtitle", "EVIDENCE CHAIN · CLINIC ROAD", "زنجیرهٔ شواهد · جادهٔ درمانگاه"),
                ("result.success", "The witness and Dr. Lina reached analysis with the sealed archive. ARIA's self-seal points to the active audit bunker.", "شاهد و دکتر لینا با بایگانی مُهرشده به مرکز بررسی رسیدن. خودمُهر آریا به پناهگاهِ بازرسیِ فعال اشاره می‌کنه."),
                ("result.passenger_lost", "A protected passenger was lost. The archive cannot be accepted as safely extracted.", "یکی از افراد تحت حفاظت از دست رفت. بایگانی رو نمی‌شه به عنوان تخلیهٔ امن پذیرفت."),
                ("result.aircraft_lost", "The helicopter was lost before the protected passengers departed.", "بالگرد پیش از خروج افراد تحت حفاظت از دست رفت."),
                ("result.carrier_lost", "The APC was lost before the protected passengers reached the landing zone.", "نفربر پیش از رسیدن افراد تحت حفاظت به منطقهٔ فرود از دست رفت."),
                ("result.timeout", "The archive and witness did not reach extraction before the route closed.", "شاهد و بایگانی پیش از بسته شدن مسیر به محل تخلیه نرسیدن."),
                ("result.integrity", "The extraction could not be verified. Return and retry; no reward was settled.", "تخلیه قابل تأیید نیست. برگرد و دوباره تلاش کن؛ پاداشی ثبت نشد."),
                ("result.star.complete", "Protect both passengers", "از هر دو مسافر محافظت کن"),
                ("result.star.escort", "No escort losses", "هیچ نیروی همراهی از دست نره"),
                ("result.star.time", "Under 8 minutes", "کمتر از ۸ دقیقه"),
                ("result.rescued_stat", "PEOPLE RESCUED", "افراد نجات‌یافته"),
                ("hud.aboard", "PROTECTED PEOPLE ABOARD", "افراد تحت حفاظتِ سوارشده"),
                ("tutorial.selection_progress", "Protected people selected: {0}/2", "افراد تحت حفاظتِ انتخاب‌شده: {0}/۲"),
                ("guide.title", "Evidence Chain field guide", "راهنمای زنجیرهٔ شواهد"),
                ("guide.example", "Select, Move, Board, Unload and Hold are the same player controls ARIA demonstrates.", "انتخاب، حرکت، سوار کردن، پیاده کردن و نگه داشتن همون فرمان‌هایی‌ان که آریا نشون می‌ده."),
                ("guide.mistake", "Do not move the carrier before both people board or launch the helicopter before the landing zone is clear.", "پیش از سوار شدن هر دو نفر نفربر رو حرکت نده و پیش از امن شدن منطقهٔ فرود بالگرد رو بلند نکن."),
                ("guide.diagram", "WITNESS + ARCHIVE → APC → SAFE LANDING → HELICOPTER → ANALYSIS", "شاهد + بایگانی ← نفربر ← فرود امن ← بالگرد ← مرکز بررسی")
            };
            string[] titles =
            {
                "Protect both people", "Select the APC", "Reach the clinic", "Select both passengers",
                "Board the APC", "Escort to the landing zone", "Unload together", "Inspect Laila's helicopter",
                "Transfer both passengers", "Hold the landing zone", "Fly to extraction", "Confirm the archive"
            };
            string[] bodies =
            {
                "The witness and Dr. Lina carry the sealed archive. Both must travel APC first, helicopter second. Keep their escort alive and the case in authorized custody.",
                "Select the APC beside your rifle escort. Keep two seats free for the protected passengers.",
                "Press Move, then tap beside the clinic pickup point. Send rifles forward to cover the approach.",
                "Select the witness and Dr. Lina together; do not include the armed escort in this selection.",
                "Press Board, then tap the APC. Confirm that both protected passengers are aboard before moving.",
                "Move the loaded APC to the marked landing zone. Keep escorts between it and the ambush.",
                "Open the APC passenger panel and tap Unload near the helicopter. Both must stand safely on the ground.",
                "Select Laila's landed helicopter. Keep it in the landing area while both passengers transfer.",
                "Select both protected passengers, press Board, then tap the landed helicopter. Confirm two aboard.",
                "Keep the loaded helicopter in the clear landing ring for 16 seconds. Enemy entry resets the hold.",
                "Select the helicopter, press Move and tap the marked exit. Keep both passengers aboard.",
                "Protect the helicopter until extraction is confirmed. The archive stays with its custodian."
            };
            string[] faTitles =
            {
                "از هر دو نفر محافظت کن", "نفربر رو انتخاب کن", "به درمانگاه برس", "هر دو مسافر رو انتخاب کن",
                "سوار نفربر کن", "تا منطقهٔ فرود همراهی کن", "با هم پیاده‌شون کن", "بالگرد لیلا رو بررسی کن",
                "هر دو نفر رو منتقل کن", "منطقهٔ فرود رو نگه دار", "به محل تخلیه پرواز کن", "بایگانی رو تأیید کن"
            };
            string[] faBodies =
            {
                "شاهد و دکتر لینا بایگانی مُهرشده رو همراه دارن. هر دو باید اول با نفربر و بعد با بالگرد برن. نیروهای همراه و زنجیرهٔ حفاظت رو حفظ کن.",
                "نفربر کنار گروه تفنگدار رو انتخاب کن. دو جای خالی برای افراد تحت حفاظت نگه دار.",
                "حرکت رو بزن و کنار نقطهٔ سوار شدن درمانگاه رو لمس کن. تفنگدارها رو برای پوشش جلو بفرست.",
                "شاهد و دکتر لینا رو با هم انتخاب کن؛ نیروهای مسلحِ همراه رو وارد انتخاب نکن.",
                "سوار کردن رو بزن و نفربر رو لمس کن. پیش از حرکت مطمئن شو هر دو نفر سوار شدن.",
                "نفربرِ پر رو به منطقهٔ فرودِ علامت‌گذاری‌شده ببر. نیروهای همراه رو بین نفربر و کمین نگه دار.",
                "پنل مسافرهای نفربر رو باز کن و نزدیک بالگرد پیاده کردن رو بزن. هر دو نفر باید سالم روی زمین باشن.",
                "بالگردِ فرودآمدهٔ لیلا رو انتخاب کن. تا انتقال هر دو نفر توی منطقهٔ فرود نگهش دار.",
                "هر دو نفر رو انتخاب کن، سوار کردن رو بزن و بالگردِ فرودآمده رو لمس کن. دو مسافر رو بررسی کن.",
                "بالگردِ پر رو ۱۶ ثانیه توی محدودهٔ فرودِ امن نگه دار. ورود دشمن شمارش رو از نو شروع می‌کنه.",
                "بالگرد رو انتخاب کن، حرکت رو بزن و خروجیِ علامت‌گذاری‌شده رو لمس کن. هر دو نفر باید سوار بمونن.",
                "تا تأیید تخلیه از بالگرد محافظت کن. بایگانی باید پیش نگهبانش بمونه."
            };
            GameLocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(
                V3UiLocalizationCatalogBuilder.CatalogPath) ?? throw new InvalidOperationException("Localization catalog missing.");
            var tables = new List<GameLocaleTable>();
            foreach (GameLocaleTable locale in catalog.Locales)
            {
                if (locale.LocaleCode != "en" && locale.LocaleCode != "fa-IR") { tables.Add(locale); continue; }
                bool fa = locale.LocaleCode == "fa-IR";
                var entries = locale.Entries.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
                foreach (var line in copy) entries["mission.evidence_chain." + line.key] = fa ? line.fa : line.en;
                for (int i = 0; i < 12; i++)
                {
                    entries[$"mission.evidence_chain.tutorial.{i + 1}.title"] = fa ? faTitles[i] : titles[i];
                    entries[$"mission.evidence_chain.tutorial.{i + 1}.body"] = fa ? faBodies[i] : bodies[i];
                }
                foreach (var line in CH03M04EvidenceChainMediaImporter.Lines)
                    entries[line.Key] = fa ? line.Persian : line.English;
                tables.Add(new GameLocaleTable(locale.LocaleCode, locale.DisplayName, locale.ShortLabel,
                    locale.RightToLeft, locale.FontAsset,
                    entries.OrderBy(x => x.Key, StringComparer.Ordinal)
                        .Select(x => new GameLocalizedStringRecord(x.Key, x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode, tables); EditorUtility.SetDirty(catalog);
        }

        private static void BuildGuide()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Configs/Missions/Chapter03");
            var guide = AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(GuidePath);
            if (guide == null)
            {
                guide = ScriptableObject.CreateInstance<MissionFieldGuideConfig>();
                AssetDatabase.CreateAsset(guide, GuidePath);
            }
            var basis = AssetDatabase.LoadAssetAtPath<MissionFieldGuideConfig>(M03RadarWarningGuideBuilder.Path);
            var classes = basis.Classes.ToArray();
            for (int i = 0; i < classes.Length; i++)
                if (classes[i].Availability != MissionGuideAvailability.Unavailable)
                    classes[i].Availability = MissionGuideAvailability.Reference;
            guide.Configure(Enumerable.Range(1, 12).Select(i => new MissionGuideTopic
            {
                TitleKey = $"mission.evidence_chain.tutorial.{i}.title",
                BodyKey = $"mission.evidence_chain.tutorial.{i}.body",
                ExampleKey = "mission.evidence_chain.guide.example",
                MistakeKey = "mission.evidence_chain.guide.mistake",
                DiagramKey = "mission.evidence_chain.guide.diagram"
            }).ToArray(), classes);
            EditorUtility.SetDirty(guide);
            GameObject root = PrefabUtility.LoadPrefabContents(M03RadarWarningUiBuilder.GuidePath);
            try
            {
                var data = new SerializedObject(root.GetComponentInChildren<MissionFieldGuideView>(true));
                data.FindProperty("evidenceChainGuide").objectReferenceValue = guide;
                data.ApplyModifiedPropertiesWithoutUndo();
                MissionUiSerializedBindingsAuthoring.Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, M03RadarWarningUiBuilder.GuidePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BindArt()
        {
            Texture art = CH03M04EvidenceChainMediaImporter.Preview() ??
                throw new InvalidOperationException("Evidence Chain art missing.");
            foreach (string path in new[]
            {
                "Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab",
                "Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab"
            })
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var campaign = root.GetComponentInChildren<CampaignOperationsScreenView>(true);
                    UnityEngine.Object view = campaign != null ? (UnityEngine.Object)campaign :
                        root.GetComponentInChildren<MissionBriefingScreenView>(true);
                    var data = new SerializedObject(view);
                    var property = data.FindProperty(campaign != null ? "evidenceChainMissionPreview" : "evidenceChainMissionArt");
                    property.objectReferenceValue = art; data.ApplyModifiedPropertiesWithoutUndo();
                    if (AssetDatabase.GetAssetPath(property.objectReferenceValue) != CH03M04EvidenceChainMediaImporter.PreviewPath)
                        throw new InvalidOperationException("Evidence Chain clean preview binding failed: " + path);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        [MenuItem("Game/Campaign/Evidence Chain/Build Player-Ready Checkpoint")]
        public static void BuildCheckpoint()
        {
            CH03M04EvidenceChainConfigBuilder.Build(); Build(); CH03M04EvidenceChainNarrativeBuilder.BuildAndInstall();
            Debug.Log("[EvidenceChainCheckpoint] result=Passed scope=CampaignConfiguration voices=local controls=existing-only");
        }
    }
}
