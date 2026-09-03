using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class V3UiLocalizationCatalogBuilder
    {
        public const string CatalogPath =
            "Assets/Game/Resources/Localization/V3UiLocalizationCatalog.asset";
        public const string MissingPersianReportPath =
            "Documentation/V3_UI_LOCALIZATION_MISSING_FA.md";

        private const string PrefabRoot = "Assets/Game/Prefabs/UI";
        private const string GameStringsPath = "Assets/Game/Configs/Scene/Game_GameStrings_Config.asset";
        private const string MatchPersianPath =
            "Assets/Game/Audio/GeneratedSource/aria_match_voice_fa_text_catalog_v0_1.json";
        private const string PersianFontPath =
            "Assets/Game/Art/UI/Fonts/NotoSansArabic/NotoSansArabic-Narrative SDF.asset";
        private const string FirstLaunchLocalePath =
            "Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchPersianLocale.asset";
        private const string FirstLaunchSequencePath =
            "Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchSequence.asset";
        private const string FirstLaunchSpeakersPath =
            "Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchSpeakers.asset";

        private static readonly Dictionary<string, string> CommonPersian = new(StringComparer.Ordinal)
        {
            ["ACCEPT"] = "پذیرش",
            ["ALL"] = "همه",
            ["APPLY"] = "اعمال",
            ["ARIA"] = "آریا",
            ["ARMORY"] = "زرادخانه",
            ["ATTACK"] = "حمله",
            ["BACK"] = "بازگشت",
            ["BUILD"] = "ساخت",
            ["BUILDINGS"] = "ساختمان‌ها",
            ["CAMPAIGN"] = "کارزار",
            ["CANCEL"] = "لغو",
            ["CHAPTERS"] = "فصل‌ها",
            ["CLOSE"] = "بستن",
            ["COMMAND"] = "فرماندهی",
            ["COMMANDER"] = "فرمانده",
            ["COMMANDER PROFILE"] = "پروفایل فرمانده",
            ["COMPLETE"] = "تکمیل‌شده",
            ["COMPLETED"] = "تکمیل‌شده",
            ["CONFIRM"] = "تأیید",
            ["CONTINUE"] = "ادامه",
            ["CREDITS"] = "اعتبار",
            ["DESTROY"] = "نابود کردن",
            ["ENEMIES"] = "دشمنان",
            ["EVENTS"] = "رویدادها",
            ["EXIT"] = "خروج",
            ["FRIENDLIES"] = "نیروهای خودی",
            ["FUEL"] = "سوخت",
            ["GAMEPLAY"] = "گیم‌پلی",
            ["HIGH"] = "زیاد",
            ["IN PROGRESS"] = "در حال انجام",
            ["INBOX"] = "صندوق پیام",
            ["INFANTRY"] = "پیاده‌نظام",
            ["LIVE"] = "فعال",
            ["LOADOUT"] = "تجهیزات",
            ["LOCKED"] = "قفل‌شده",
            ["LOW"] = "کم",
            ["MAIN MENU"] = "منوی اصلی",
            ["MATERIALS"] = "مصالح",
            ["MEDIUM"] = "متوسط",
            ["MISSION BRIEFING"] = "توجیه مأموریت",
            ["MISSION COMPLETE"] = "مأموریت تکمیل شد",
            ["MISSION SUMMARY"] = "خلاصه مأموریت",
            ["MOVE"] = "حرکت",
            ["NEXT"] = "بعدی",
            ["NO"] = "خیر",
            ["NORMAL"] = "عادی",
            ["OBJECTIVES"] = "اهداف",
            ["OFF"] = "خاموش",
            ["OIL"] = "نفت",
            ["ON"] = "روشن",
            ["OPERATIONS"] = "عملیات",
            ["PARTS"] = "قطعات",
            ["PAUSE"] = "مکث",
            ["PERFORMANCE"] = "عملکرد",
            ["PREV"] = "قبلی",
            ["RANKING"] = "رتبه‌بندی",
            ["RESTART"] = "شروع دوباره",
            ["RESET"] = "بازنشانی",
            ["RESUME"] = "ادامه بازی",
            ["REWARDS"] = "پاداش‌ها",
            ["SAVE"] = "ذخیره",
            ["SELECT"] = "انتخاب",
            ["SETTINGS"] = "تنظیمات",
            ["SHOW ME"] = "نشان بده",
            ["SKIP"] = "رد کردن",
            ["SOLDIERS"] = "سربازان",
            ["STANDARD"] = "استاندارد",
            ["STOP"] = "توقف",
            ["STORE"] = "فروشگاه",
            ["SUBTITLES"] = "زیرنویس",
            ["SUPPORT"] = "پشتیبانی",
            ["TARGET"] = "هدف",
            ["UNITS"] = "نیروها",
            ["UPGRADE"] = "ارتقا",
            ["UPGRADES"] = "ارتقاها",
            ["VEHICLES"] = "خودروها",
            ["VICTORY"] = "پیروزی",
            ["VIDEO"] = "تصویر",
            ["VOICE"] = "صدا",
            ["YES"] = "بله"
        };

        [MenuItem("Game/UI/V3/Localization/Rebuild Catalog And Bind All Screens")]
        public static void RebuildCatalogAndBindAllScreens()
        {
            Dictionary<string, string> english = new(StringComparer.Ordinal);
            Dictionary<string, string> persian = new(StringComparer.Ordinal);
            Dictionary<string, string> keysByEnglish = new(StringComparer.Ordinal);

            ImportGameStrings(english, keysByEnglish);
            ImportMatchPersian(english, persian, keysByEnglish);
            ImportFirstLaunchNarrative(english, persian, keysByEnglish);
            ImportM02Narrative(english, persian, keysByEnglish);
            ImportRuntimeUiStrings(english, persian, keysByEnglish);
            ImportRuntimeStaticUiCatalogs(english, keysByEnglish);
            ImportCommonPersian(persian, keysByEnglish);

            List<string> prefabPaths = FindUiPrefabPaths();
            int bindingCount = 0;
            for (int i = 0; i < prefabPaths.Count; i++)
                bindingCount += BindPrefab(prefabPaths[i], english, keysByEnglish);

            // Common translations can now resolve the stable auto keys introduced by the prefab pass.
            ImportCommonPersian(persian, keysByEnglish);
            int seededPersian = V3PersianUiTranslationSeeder.FillMissing(english, persian);
            GameLocalizationCatalog catalog = BuildCatalog(english, persian);
            List<string> missing = FindMissingPersian(english, persian);
            WriteMissingReport(prefabPaths.Count, bindingCount, english.Count, persian.Count, missing);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    $"V3 localization catalog has {missing.Count} missing Farsi value(s). " +
                    $"See {MissingPersianReportPath}.");
            }

            Debug.Log(
                $"[V3UiLocalizationCatalogBuilder] result=Passed prefabs={prefabPaths.Count} " +
                $"bindings={bindingCount} keys={english.Count} fa={persian.Count} " +
                $"seededFa={seededPersian} missingFa={missing.Count} " +
                $"catalog={CatalogPath} report={MissingPersianReportPath}");
        }

        [MenuItem("Game/UI/V3/Localization/Validate Bindings And Coverage")]
        public static void ValidateBindingsAndCoverage()
        {
            GameLocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(CatalogPath);
            if (catalog == null)
                throw new InvalidOperationException($"Missing localization catalog: {CatalogPath}");

            GameLocaleTable english = catalog.FindLocale(GameLocalization.EnglishLocaleCode);
            GameLocaleTable persian = catalog.FindLocale(GameLocalization.PersianLocaleCode);
            if (english == null || persian == null || !persian.RightToLeft || persian.FontAsset == null)
                throw new InvalidOperationException("Localization catalog requires EN and RTL FA tables with a Farsi font.");

            Dictionary<string, string> englishEntries = ToDictionary(english);
            Dictionary<string, string> persianEntries = ToDictionary(persian);
            int prefabs = 0;
            int bindings = 0;
            List<string> errors = new();

            foreach (KeyValuePair<string, string> entry in englishEntries)
            {
                if (!persianEntries.TryGetValue(entry.Key, out string value) ||
                    string.IsNullOrWhiteSpace(value))
                {
                    errors.Add($"catalog:{entry.Key} missing FA value for '{entry.Value}'");
                }
            }

            foreach (string path in FindUiPrefabPaths())
            {
                prefabs++;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    errors.Add($"{path}: prefab could not be loaded");
                    continue;
                }

                if (UsesSpecializedNarrativeLocalization(path))
                    continue;

                TMP_Text[] text = prefab.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < text.Length; i++)
                {
                    string source = NormalizeSource(ReadAuthoredText(text[i]));
                    if (!IsTranslatable(source))
                        continue;

                    V3LocalizedTextBinding binding = text[i].GetComponent<V3LocalizedTextBinding>();
                    if (binding == null)
                    {
                        errors.Add($"{path}:{HierarchyPath(text[i].transform)} missing binding");
                        continue;
                    }

                    bindings++;
                    if (string.IsNullOrWhiteSpace(binding.LocalizationKey) ||
                        !englishEntries.ContainsKey(binding.LocalizationKey))
                    {
                        errors.Add($"{path}:{HierarchyPath(text[i].transform)} missing EN key");
                    }
                    else if (!persianEntries.TryGetValue(binding.LocalizationKey, out string value) ||
                             string.IsNullOrWhiteSpace(value))
                    {
                        errors.Add($"{path}:{HierarchyPath(text[i].transform)} missing FA value");
                    }
                }
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"V3 localization validation failed with {errors.Count} issue(s).\n" +
                    string.Join("\n", errors.Take(50)));
            }

            Debug.Log(
                $"[V3UiLocalizationCatalogBuilder] validation=Passed prefabs={prefabs} " +
                $"bindings={bindings} en={englishEntries.Count} fa={persianEntries.Count}");
        }

        private static int BindPrefab(
            string path,
            Dictionary<string, string> english,
            Dictionary<string, string> keysByEnglish)
        {
            if (UsesSpecializedNarrativeLocalization(path))
                return 0;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            int count = 0;
            try
            {
                TMP_Text[] text = root.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < text.Length; i++)
                {
                    string source = NormalizeSource(ReadAuthoredText(text[i]));
                    if (!IsTranslatable(source))
                        continue;

                    string key = GetOrCreateKey(source, english, keysByEnglish);
                    V3LocalizedTextBinding binding = text[i].GetComponent<V3LocalizedTextBinding>();
                    if (binding == null)
                    {
                        binding = text[i].gameObject.AddComponent<V3LocalizedTextBinding>();
                        changed = true;
                    }

                    if (!string.Equals(binding.LocalizationKey, key, StringComparison.Ordinal) ||
                        !string.Equals(binding.EnglishFallback, source, StringComparison.Ordinal))
                    {
                        binding.Configure(key, source, observeRuntimeChanges: true);
                        EditorUtility.SetDirty(binding);
                        changed = true;
                    }
                    count++;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return count;
        }

        private static GameLocalizationCatalog BuildCatalog(
            Dictionary<string, string> english,
            Dictionary<string, string> persian)
        {
            EnsureAssetFolder("Assets/Game/Resources");
            EnsureAssetFolder("Assets/Game/Resources/Localization");
            GameLocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameLocalizationCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            TMP_FontAsset persianFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PersianFontPath);
            if (persianFont == null)
                throw new InvalidOperationException($"Missing Persian font: {PersianFontPath}");

            List<GameLocaleTable> localeTables = new()
            {
                new GameLocaleTable(
                    GameLocalization.EnglishLocaleCode,
                    "English",
                    "EN",
                    isRightToLeft: false,
                    localeFont: null,
                    ToRecords(english)),
                new GameLocaleTable(
                    GameLocalization.PersianLocaleCode,
                    "فارسی",
                    "FA",
                    isRightToLeft: true,
                    localeFont: persianFont,
                    ToRecords(persian))
            };

            // Additional locales are configuration data: a rebuild refreshes EN/FA source tables
            // but never removes a language added to the central catalog.
            for (int i = 0; i < catalog.Locales.Count; i++)
            {
                GameLocaleTable existing = catalog.Locales[i];
                if (existing == null ||
                    string.Equals(existing.LocaleCode, GameLocalization.EnglishLocaleCode,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(existing.LocaleCode, GameLocalization.PersianLocaleCode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                localeTables.Add(existing);
            }

            catalog.Configure(GameLocalization.EnglishLocaleCode, localeTables);
            return catalog;
        }

        private static IEnumerable<GameLocalizedStringRecord> ToRecords(
            Dictionary<string, string> entries) =>
            entries.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new GameLocalizedStringRecord(pair.Key, pair.Value));

        private static void ImportGameStrings(
            Dictionary<string, string> english,
            Dictionary<string, string> keysByEnglish)
        {
            GameStringsConfig config = AssetDatabase.LoadAssetAtPath<GameStringsConfig>(GameStringsPath);
            if (config == null)
                throw new InvalidOperationException($"Missing game strings config: {GameStringsPath}");

            for (int i = 0; i < config.Entries.Count; i++)
            {
                GameStringConfigEntry entry = config.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                    continue;
                AddEnglish(entry.Key, entry.Value, english, keysByEnglish);
            }
        }

        private static void ImportMatchPersian(
            Dictionary<string, string> english,
            Dictionary<string, string> persian,
            Dictionary<string, string> keysByEnglish)
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(MatchPersianPath);
            if (source == null)
                throw new InvalidOperationException($"Missing reviewed match Persian catalog: {MatchPersianPath}");
            MatchPersianCatalog parsed = JsonUtility.FromJson<MatchPersianCatalog>(source.text);
            if (parsed?.entries == null)
                return;

            for (int i = 0; i < parsed.entries.Length; i++)
            {
                MatchPersianEntry entry = parsed.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;
                AddEnglish(entry.key, entry.englishText, english, keysByEnglish);
                if (!string.IsNullOrWhiteSpace(entry.text))
                    persian[entry.key] = entry.text;
            }
        }

        private static void ImportFirstLaunchNarrative(
            Dictionary<string, string> english,
            Dictionary<string, string> persian,
            Dictionary<string, string> keysByEnglish)
        {
            NarrativeLocaleConfig locale =
                AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(FirstLaunchLocalePath);
            if (locale != null)
            {
                for (int i = 0; i < locale.Text.Count; i++)
                {
                    NarrativeLocaleTextRecord entry = locale.Text[i];
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.Key))
                        persian[entry.Key] = entry.Value ?? string.Empty;
                }
            }

            NarrativeSequenceConfig sequence =
                AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchSequencePath);
            if (sequence != null)
            {
                for (int stateIndex = 0; stateIndex < sequence.States.Count; stateIndex++)
                {
                    NarrativeStateRecord state = sequence.States[stateIndex];
                    AddEnglish(state.LocationTitleKey, state.LocationTitleFallback, english, keysByEnglish);
                    AddEnglish(state.LocationSubtitleKey, state.LocationSubtitleFallback, english, keysByEnglish);
                    for (int lineIndex = 0; lineIndex < state.Lines.Count; lineIndex++)
                    {
                        NarrativeDialogueLineRecord line = state.Lines[lineIndex];
                        AddEnglish(line.TextKey, line.EnglishFallback, english, keysByEnglish);
                    }
                }
            }

            NarrativeSpeakerCatalog speakers =
                AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchSpeakersPath);
            if (speakers == null)
                return;
            for (int i = 0; i < speakers.Speakers.Count; i++)
            {
                NarrativeSpeakerRecord speaker = speakers.Speakers[i];
                AddEnglish(speaker.NameKey, speaker.NameFallback, english, keysByEnglish);
                AddEnglish(speaker.RoleKey, speaker.RoleFallback, english, keysByEnglish);
                AddEnglish(speaker.AccessibleLabelKey, speaker.AccessibleLabelFallback, english, keysByEnglish);
            }
        }

        private static void ImportM02Narrative(
            Dictionary<string, string> english,
            Dictionary<string, string> persian,
            Dictionary<string, string> keysByEnglish)
        {
            ImportM02Lines(M02EstablishBaseLocalizedText.Brief, english, persian, keysByEnglish);
            ImportM02Lines(M02EstablishBaseLocalizedText.Comms, english, persian, keysByEnglish);
            ImportM02Lines(M02EstablishBaseLocalizedText.Debrief, english, persian, keysByEnglish);
        }

        private static void ImportRuntimeUiStrings(
            Dictionary<string, string> english,
            Dictionary<string, string> persian,
            Dictionary<string, string> keysByEnglish)
        {
            AddRuntimeText("narrative.first_launch.language.title", "SELECT STORY LANGUAGE", "زبان داستان را انتخاب کنید");
            AddRuntimeText("narrative.first_launch.language.info", "This can be changed later\nin Command Settings.", "بعداً می‌توانید این مورد را\nدر تنظیمات فرماندهی تغییر دهید.");
            AddRuntimeText("narrative.first_launch.language.continue", "CONTINUE   ›", "ادامه   ‹");
            AddRuntimeText("ui.common.alerts", "ALERTS", "هشدارها");
            AddRuntimeText("ui.common.music", "MUSIC", "موسیقی");
            AddRuntimeText("ui.common.sound", "SOUND", "صدا");
            AddRuntimeText("ui.common.voice", "VOICE", "گفتار");
            AddRuntimeText("ui.common.sfx", "SFX", "جلوه صوتی");
            AddRuntimeText("ui.common.skip", "SKIP", "ردکردن");
            AddRuntimeText("ui.common.pause", "PAUSE", "مکث");
            AddRuntimeText("ui.settings.command_title", "COMMAND SETTINGS", "تنظیمات فرماندهی");
            AddRuntimeText("ui.settings.master_volume", "MASTER VOLUME", "صدای اصلی");
            AddRuntimeText("ui.settings.music_volume", "MUSIC VOLUME", "صدای موسیقی");
            AddRuntimeText("ui.settings.sound_volume", "SOUND VOLUME", "صدای جلوه‌ها");
            AddRuntimeText("ui.settings.camera_sensitivity", "CAMERA SENSITIVITY", "حساسیت دوربین");
            AddRuntimeText("ui.settings.threat_warnings", "THREAT WARNINGS", "هشدارهای تهدید");
            AddRuntimeText("ui.settings.high_contrast", "High Contrast UI", "رابط کاربری با کنتراست بالا");
            AddRuntimeText("ui.settings.large_text", "Large Text", "متن بزرگ");
            AddRuntimeText("ui.settings.assistant_takeover", "Assistant Takeover", "کنترل توسط دستیار");
            AddRuntimeText("ui.settings.assistant_subtitles", "Assistant Subtitles", "زیرنویس دستیار");
            AddRuntimeText("ui.settings.music_description", "Adjust in-game music volume.", "صدای موسیقی بازی را تنظیم کنید.");
            AddRuntimeText("ui.settings.sound_description", "Adjust in-game sound effects volume.", "صدای جلوه‌های صوتی بازی را تنظیم کنید.");
            AddRuntimeText("ui.settings.voice_description", "Adjust in-game voice volume.", "صدای گفتار بازی را تنظیم کنید.");
            AddRuntimeText("ui.settings.threat_description", "Show tactical warnings during missions.", "هشدارهای تاکتیکی را هنگام مأموریت‌ها نمایش دهید.");
            AddRuntimeText("ui.settings.contrast_description", "Increase panel and text contrast.", "کنتراست پنل و متن را افزایش دهید.");
            AddRuntimeText("ui.settings.large_text_description", "Increase UI text scale for readability.", "اندازه متن رابط کاربری را برای خوانایی افزایش دهید.");
            AddRuntimeText("ui.settings.takeover_description", "Allow assistant-guided bounded actions.", "اقدام‌های محدود با هدایت دستیار را مجاز کنید.");
            AddRuntimeText("ui.settings.subtitles_description", "Show narration subtitles in the assistant panel.", "زیرنویس روایت را در پنل دستیار نمایش دهید.");
            AddRuntimeText("ui.settings.enable_music_description", "Enable command music playback.", "پخش موسیقی فرماندهی را فعال کنید.");
            AddRuntimeText("ui.settings.enable_sound_description", "Enable UI, combat, alert, and ambience sounds.", "صدای رابط کاربری، نبرد، هشدار و محیط را فعال کنید.");
            AddRuntimeText("ui.settings.enable_voice_description", "Enable tactical assistant voice lines.", "گفتار دستیار تاکتیکی را فعال کنید.");
            AddRuntimeText("ui.hud.tap_rifle_squad", "TAP RIFLE SQUAD\n▼", "روی گروه تفنگدار بزنید\n▼");
            AddRuntimeText("ui.hud.aria_target", "ARIA TARGET", "هدف آریا");
            AddRuntimeText("ui.common.queued", "QUEUED", "در صف");
            AddRuntimeText("ui.aria.elapsed_hms", "ELAPSED: {0}:{1:00}:{2:00}", "سپری‌شده: {0}:{1:00}:{2:00}");
            AddRuntimeText("ui.aria.elapsed_ms", "ELAPSED: {0:00}:{1:00}", "سپری‌شده: {0:00}:{1:00}");
            AddRuntimeText("ui.hud.materials", "Materials", "مصالح");
            AddRuntimeText("ui.hud.oil", "Oil", "نفت");
            AddRuntimeText("ui.hud.civilian_risk", "Civilian Risk", "خطر برای غیرنظامیان");
            AddRuntimeText("ui.hud.fuel", "Fuel", "سوخت");
            AddRuntimeText("ui.hud.credits", "Credits", "اعتبار");
            AddRuntimeText("ui.aria.name", "ARIA", "آریا");
            AddRuntimeText("ui.skirmish.income_multiplier", "Income Multiplier", "ضریب درآمد");
            AddRuntimeText("ui.skirmish.player_auto_ai", "Player Auto AI", "کنترل خودکار بازیکن");
            AddRuntimeText("ui.skirmish.player_auto_ai_description", "Let the AI control your faction for simulation tests.", "برای آزمایش شبیه‌سازی، کنترل جناح خود را به هوش مصنوعی بسپارید.");
            AddRuntimeText("ui.skirmish.fog_description", "Hide unexplored areas.", "مناطق کشف‌نشده را پنهان کنید.");
            AddRuntimeText("ui.skirmish.intel_description", "Reveal enemy tech on scout.", "فناوری دشمن را هنگام شناسایی آشکار کنید.");
            AddRuntimeText("ui.skirmish.starting_money", "Starting Money", "اعتبار آغازین");
            AddRuntimeText("ui.skirmish.aggression", "Aggression", "تهاجم");
            AddRuntimeText("ui.skirmish.fog_of_war", "FOG OF WAR", "مه جنگ");
            AddRuntimeText("ui.skirmish.intel_reveal", "INTEL REVEAL", "افشای اطلاعات");
            AddRuntimeText("ui.store.purchase_price", "PURCHASE {0}", "خرید {0}");
            AddRuntimeText("ui.inbox.open_via_intel", "OPEN VIA INTEL", "بازکردن از بخش اطلاعات");
            AddRuntimeText("ui.inbox.from", "From: {0}", "از: {0}");
            AddRuntimeText("ui.inbox.from_highlight", "From: <color=#77B936>{0}</color>", "از: <color=#77B936>{0}</color>");
            AddRuntimeText("ui.campaign.mission_unavailable", "MISSION DATA UNAVAILABLE", "داده مأموریت در دسترس نیست");
            AddRuntimeText("ui.aria.state.next", "NEXT", "بعدی");
            AddRuntimeText("ui.aria.state.warn", "WARN", "هشدار");
            AddRuntimeText("ui.aria.state.control", "CTRL", "کنترل");
            AddRuntimeText("ui.aria.state.off", "OFF", "خاموش");
            AddRuntimeText("ui.aria.state.idle", "IDLE", "آماده");
            AddRuntimeText("ui.common.play", "PLAY", "پخش");
            AddRuntimeText("ui.common.oldest", "OLDEST", "قدیمی‌ترین");
            AddRuntimeText("ui.common.unread", "UNREAD", "خوانده‌نشده");
            AddRuntimeText("ui.common.marked_read", "MARKED READ", "خوانده‌شده");
            AddRuntimeText("ui.common.mark_read", "MARK READ", "علامت‌گذاری خوانده‌شده");
            AddRuntimeText("ui.common.filters", "FILTERS", "فیلترها");
            AddRuntimeText("ui.common.on", "ON", "روشن");
            AddRuntimeText("ui.common.off", "OFF", "خاموش");
            AddRuntimeText("ui.events.remaining", "{0} REMAINING", "{0} باقی‌مانده");
            AddRuntimeText("ui.store.category_offers", "{0} OFFERS", "پیشنهادهای {0}");
            AddRuntimeText("ui.campaign.title", "CAMPAIGN", "کارزار");
            AddRuntimeText("ui.campaign.m01_name", "FIRST CONTACT", "نخستین تماس");
            AddRuntimeText("ui.campaign.m02_name", "ESTABLISH THE BASE", "پایگاه را بنا کنید");
            AddRuntimeText("ui.campaign.build_barrack", "BUILD\nBARRACK", "ساخت\nسربازخانه");
            AddRuntimeText("ui.campaign.secure_corridor", "SECURE\nCORRIDOR", "امن‌سازی\nمسیر");
            AddRuntimeText("ui.campaign.start_briefing", "START BRIEFING", "شروع توجیه");
            AddRuntimeText("ui.splash.default_tip", "Prepare your squads before entering hostile districts.", "پیش از ورود به مناطق دشمن، گروه‌های خود را آماده کنید.");
            AddRuntimeText("ui.narrative.confirm_skip_accessible", "Confirm skip to gameplay", "رفتن مستقیم به بازی را تأیید کنید");

            // Match HUD values are frequently rebuilt from live ECS data. Template entries let
            // V3LocalizedTextBinding translate the rendered value without teaching each view
            // about individual languages.
            AddRuntimeText("ui.hud.passengers_capacity", "PASSENGERS {0}/{1}", "مسافران {0}/{1}");
            AddRuntimeText("ui.hud.passengers_soldiers_capacity", "PASSENGERS {0}/{1} | SOLDIERS {2}/{3}", "مسافران {0}/{1} | سربازان {2}/{3}");
            AddRuntimeText("ui.hud.passengers_soldiers_vehicles_capacity", "PASSENGERS {0}/{1} | SOLDIERS {2}/{3} | VEHICLES {4}/{5}", "مسافران {0}/{1} | سربازان {2}/{3} | خودروها {4}/{5}");
            AddRuntimeText("ui.hud.no_passengers", "NO PASSENGERS ONBOARD", "هیچ مسافری سوار نیست");
            AddRuntimeText("ui.hud.exit_all", "EXIT ALL", "خروج همه");
            AddRuntimeText("ui.hud.oil_barrels_capacity", "OIL BARRELS {0}/{1}", "بشکه‌های نفت {0}/{1}");
            AddRuntimeText("ui.hud.fuel_capacity", "FUEL {0}/{1}", "سوخت {0}/{1}");
            AddRuntimeText("ui.hud.oil_capacity", "OIL {0}/{1}", "نفت {0}/{1}");
            AddRuntimeText("ui.hud.oil_fuel_capacity", "OIL {0}/{1} | FUEL {2}/{3}", "نفت {0}/{1} | سوخت {2}/{3}");
            AddRuntimeText("ui.hud.cargo_capacity", "CARGO 0/{0}", "محموله ۰/{0}");
            AddRuntimeText("ui.hud.material_fabrication_status", "OIL {0}/{1} | MATERIALS {2}/{3}\n{4} OIL > {5} MATERIALS / {6}s\n{7}%", "نفت {0}/{1} | مصالح {2}/{3}\n{4} نفت > {5} مصالح / {6} ثانیه\n{7}٪");
            AddRuntimeText("ui.hud.health_empty", "HEALTH -", "سلامت -");
            AddRuntimeText("ui.hud.onboard", "ONBOARD", "سوارشده");

            AddRuntimeText("ui.aria.player_control", "PLAYER CONTROL", "کنترل بازیکن");
            AddRuntimeText("ui.aria.controlling", "ARIA CONTROLLING", "کنترل با آریا");
            AddRuntimeText("ui.aria.stop_aria", "STOP ARIA", "توقف آریا");
            AddRuntimeText("ui.aria.do_it", "DO IT", "انجامش بده");
            AddRuntimeText("ui.aria.primary_state", "PRIMARY / {0}", "اصلی / {0}");
            AddRuntimeText("ui.aria.priority_age.critical_new", "CRITICAL / NEW", "بحرانی / جدید");
            AddRuntimeText("ui.aria.priority_age.critical_active", "CRITICAL / ACTIVE", "بحرانی / فعال");
            AddRuntimeText("ui.aria.priority_age.critical_expiring", "CRITICAL / EXPIRING", "بحرانی / رو به پایان");
            AddRuntimeText("ui.aria.priority_age.high_new", "HIGH / NEW", "زیاد / جدید");
            AddRuntimeText("ui.aria.priority_age.high_active", "HIGH / ACTIVE", "زیاد / فعال");
            AddRuntimeText("ui.aria.priority_age.high_expiring", "HIGH / EXPIRING", "زیاد / رو به پایان");
            AddRuntimeText("ui.aria.priority_age.normal_new", "NORMAL / NEW", "عادی / جدید");
            AddRuntimeText("ui.aria.priority_age.normal_active", "NORMAL / ACTIVE", "عادی / فعال");
            AddRuntimeText("ui.aria.priority_age.normal_expiring", "NORMAL / EXPIRING", "عادی / رو به پایان");
            AddRuntimeText("ui.aria.priority_age.low_new", "LOW / NEW", "کم / جدید");
            AddRuntimeText("ui.aria.priority_age.low_active", "LOW / ACTIVE", "کم / فعال");
            AddRuntimeText("ui.aria.priority_age.low_expiring", "LOW / EXPIRING", "کم / رو به پایان");
            AddRuntimeText("ui.aria.step", "STEP {0}/{1}", "مرحله {0}/{1}");
            AddRuntimeText("ui.aria.moving_to_cover", "MOVING TO COVER", "در حال حرکت به پوشش");
            AddRuntimeText("ui.aria.choose_destination", "CHOOSE DESTINATION", "مقصد را انتخاب کنید");
            AddRuntimeText("ui.aria.press_move", "PRESS MOVE", "حرکت را بزنید");
            AddRuntimeText("ui.aria.attack_issued", "ATTACK ORDER ISSUED", "دستور حمله صادر شد");
            AddRuntimeText("ui.aria.choose_enemy", "CHOOSE ENEMY", "دشمن را انتخاب کنید");
            AddRuntimeText("ui.aria.press_attack", "PRESS ATTACK", "حمله را بزنید");
            AddRuntimeText("ui.aria.moving_body", "Your squad is moving to the marked cover position.", "گروه شما در حال حرکت به موقعیت پوشش علامت‌گذاری‌شده است.");
            AddRuntimeText("ui.aria.move_target_body", "Tap the highlighted destination to move your squad.", "برای حرکت گروه، روی مقصد علامت‌گذاری‌شده بزنید.");
            AddRuntimeText("ui.aria.move_button_body", "Tap MOVE to select the move command.", "برای انتخاب دستور حرکت، روی «حرکت» بزنید.");
            AddRuntimeText("ui.aria.attacking_body", "Your squad is engaging the highlighted enemy.", "گروه شما در حال درگیری با دشمن علامت‌گذاری‌شده است.");
            AddRuntimeText("ui.aria.attack_target_body", "Tap the highlighted enemy to issue the attack.", "برای صدور دستور حمله، روی دشمن علامت‌گذاری‌شده بزنید.");
            AddRuntimeText("ui.aria.attack_button_body", "Tap ATTACK to select the attack command.", "برای انتخاب دستور حمله، روی «حمله» بزنید.");

            AddRuntimeText("ui.exchange.materials_amount", "{0} MATERIALS", "{0} مصالح");
            AddRuntimeText("ui.exchange.oil_amount", "{0} OIL", "{0} نفت");
            AddRuntimeText("ui.exchange.fuel_amount", "{0} FUEL", "{0} سوخت");
            AddRuntimeText("ui.exchange.rush_amount", "{0} RUSH", "{0} تسریع");
            AddRuntimeText("ui.exchange.materials_to_oil_rate", "1 MATERIALS -> {0} OIL", "۱ مصالح ← {0} نفت");
            AddRuntimeText("ui.exchange.materials_to_fuel_rate", "1 MATERIALS -> {0} FUEL", "۱ مصالح ← {0} سوخت");
            AddRuntimeText("ui.exchange.materials_to_rush_rate", "1 MATERIALS -> {0} RUSH", "۱ مصالح ← {0} تسریع");
            AddRuntimeText("ui.exchange.oil_to_materials_rate", "1 OIL -> {0} MATERIALS", "۱ نفت ← {0} مصالح");
            AddRuntimeText("ui.exchange.oil_to_fuel_rate", "1 OIL -> {0} FUEL", "۱ نفت ← {0} سوخت");
            AddRuntimeText("ui.exchange.oil_to_rush_rate", "1 OIL -> {0} RUSH", "۱ نفت ← {0} تسریع");
            AddRuntimeText("ui.exchange.fuel_to_materials_rate", "1 FUEL -> {0} MATERIALS", "۱ سوخت ← {0} مصالح");
            AddRuntimeText("ui.exchange.fuel_to_oil_rate", "1 FUEL -> {0} OIL", "۱ سوخت ← {0} نفت");
            AddRuntimeText("ui.exchange.fuel_to_rush_rate", "1 FUEL -> {0} RUSH", "۱ سوخت ← {0} تسریع");
            AddRuntimeText("ui.exchange.rush_to_materials_rate", "1 RUSH -> {0} MATERIALS", "۱ تسریع ← {0} مصالح");
            AddRuntimeText("ui.exchange.rush_to_oil_rate", "1 RUSH -> {0} OIL", "۱ تسریع ← {0} نفت");
            AddRuntimeText("ui.exchange.rush_to_fuel_rate", "1 RUSH -> {0} FUEL", "۱ تسریع ← {0} سوخت");
            AddRuntimeText("ui.exchange.storage_required", "Storage capacity required.", "ظرفیت ذخیره‌سازی لازم است.");
            AddRuntimeText("ui.exchange.transport_required", "Logistics transport required.", "وسیله حمل‌ونقل لجستیکی لازم است.");
            AddRuntimeText("ui.exchange.no_requirements", "No special requirements.", "شرایط ویژه‌ای لازم نیست.");
            AddRuntimeText("ui.exchange.blocked", "BLOCKED: {0}", "مسدود: {0}");
            AddRuntimeText("ui.exchange.confirm_instruction", "Confirm to start a timed logistics exchange.", "برای شروع مبادله زمان‌دار لجستیکی تأیید کنید.");
            AddRuntimeText("ui.exchange.unavailable", "Exchange unavailable", "مبادله در دسترس نیست");
            AddRuntimeText("ui.exchange.route_locked", "Route locked", "مسیر قفل است");
            AddRuntimeText("ui.exchange.insufficient_materials", "Insufficient Materials", "مصالح کافی نیست");
            AddRuntimeText("ui.exchange.insufficient_oil", "Insufficient Oil", "نفت کافی نیست");
            AddRuntimeText("ui.exchange.insufficient_fuel", "Insufficient Fuel", "سوخت کافی نیست");
            AddRuntimeText("ui.exchange.queue_full", "Queue full", "صف پر است");
            AddRuntimeText("ui.exchange.storage_full", "Storage full", "ذخیره‌گاه پر است");
            AddRuntimeText("ui.exchange.storage_missing", "Storage missing", "ذخیره‌گاه موجود نیست");
            AddRuntimeText("ui.exchange.rush_unavailable", "Rush unavailable", "تسریع در دسترس نیست");
            AddRuntimeText("ui.exchange.insufficient_rush", "Insufficient Rush Tickets", "بلیت تسریع کافی نیست");
            AddRuntimeText("ui.exchange.cancel_unavailable", "Cancel unavailable", "لغو در دسترس نیست");
            AddRuntimeText("ui.exchange.mission_ending", "Mission ending", "مأموریت در حال پایان است");

            AddRuntimeText("ui.briefing.location", "LOCATION: {0}", "موقعیت: {0}");
            AddRuntimeText("ui.briefing.hostiles_delayed", "{0} HOSTILES | DELAYED PATROL", "{0} دشمن | گشت با تأخیر");
            AddRuntimeText("ui.briefing.hostiles_confirmed", "{0} CONFIRMED", "{0} تأییدشده");
            AddRuntimeText("ui.briefing.destroy_patrol_count", "DESTROY THE HOSTILE PATROL ({0})", "گشت دشمن را نابود کنید ({0})");
            AddRuntimeText("ui.briefing.starting_resources", "{0} CR / {1} MAT", "{0} اعتبار / {1} مصالح");
            AddRuntimeText("ui.briefing.barracks_count", "BARRACKS x{0}", "سربازخانه ×{0}");
            AddRuntimeText("ui.result.combat_summary", "SQUAD LOSSES  {0}     •     ENEMIES DEFEATED  {1}", "تلفات گروه  {0}     •     دشمنان شکست‌خورده  {1}");
            AddRuntimeText("ui.result.star_count", "{0} / 3 STARS", "{0} / ۳ ستاره");
            AddRuntimeText("ui.result.reward_amount", "{0}     +{1}", "{0}     +{1}");
            AddRuntimeText("ui.campaign.mission_row", "{0}  |  {1}  |  {2}/3{3}", "{0}  |  {1}  |  {2}/۳{3}");
            AddRuntimeText("ui.campaign.best_time", "  |  BEST {0}:{1}", "  |  بهترین {0}:{1}");
            AddRuntimeText("ui.build.title", "BUILD {0}", "ساخت {0}");
            AddRuntimeText("ui.build.cost", "{0} CR / {1} MAT", "{0} اعتبار / {1} مصالح");

            void AddRuntimeText(string key, string source, string value)
            {
                AddEnglish(key, source, english, keysByEnglish);
                persian[key] = value;
            }
        }

        private static void ImportRuntimeStaticUiCatalogs(
            Dictionary<string, string> english,
            Dictionary<string, string> keysByEnglish)
        {
            Type[] catalogOwners =
            {
                typeof(EventsV3View),
                typeof(StoreCommandExchangeV3View),
                typeof(InboxV3View),
                typeof(MatchHudSquadTrayView)
            };
            HashSet<object> visited = new(ReferenceComparer.Instance);
            for (int typeIndex = 0; typeIndex < catalogOwners.Length; typeIndex++)
            {
                FieldInfo[] fields = catalogOwners[typeIndex].GetFields(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                {
                    FieldInfo field = fields[fieldIndex];
                    if (field.IsLiteral || typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                        continue;
                    CollectRuntimeUiStrings(field.GetValue(null), 0, visited, english, keysByEnglish);
                }
            }
        }

        private static void CollectRuntimeUiStrings(
            object value,
            int depth,
            HashSet<object> visited,
            Dictionary<string, string> english,
            Dictionary<string, string> keysByEnglish)
        {
            if (value == null || depth > 5)
                return;
            if (value is string text)
            {
                string normalized = NormalizeSource(text);
                if (IsTranslatable(normalized))
                    GetOrCreateKey(normalized, english, keysByEnglish);
                return;
            }

            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || typeof(UnityEngine.Object).IsAssignableFrom(type))
                return;
            if (!type.IsValueType && !visited.Add(value))
                return;

            if (value is IEnumerable sequence)
            {
                int count = 0;
                foreach (object item in sequence)
                {
                    if (count++ >= 1000)
                        break;
                    CollectRuntimeUiStrings(item, depth + 1, visited, english, keysByEnglish);
                }
                return;
            }

            if (!string.Equals(type.Namespace, "Game.UI.Runtime", StringComparison.Ordinal))
                return;
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
                CollectRuntimeUiStrings(fields[i].GetValue(value), depth + 1, visited, english, keysByEnglish);
        }

        private static void ImportM02Lines(
            IEnumerable<M02NarrativeLocalizedLine> lines,
            Dictionary<string, string> english,
            Dictionary<string, string> persian,
            Dictionary<string, string> keysByEnglish)
        {
            foreach (M02NarrativeLocalizedLine line in lines)
            {
                AddEnglish(line.TextKey, line.English, english, keysByEnglish);
                if (!string.IsNullOrWhiteSpace(line.TextKey) && !string.IsNullOrWhiteSpace(line.Persian))
                    persian[line.TextKey] = line.Persian;
            }
        }

        private static void ImportCommonPersian(
            Dictionary<string, string> persian,
            Dictionary<string, string> keysByEnglish)
        {
            foreach (KeyValuePair<string, string> translation in CommonPersian)
            {
                if (keysByEnglish.TryGetValue(translation.Key, out string key) &&
                    (!persian.TryGetValue(key, out string existing) ||
                     string.IsNullOrWhiteSpace(existing)))
                {
                    persian[key] = translation.Value;
                }
            }
        }

        private static string GetOrCreateKey(
            string source,
            Dictionary<string, string> english,
            Dictionary<string, string> keysByEnglish)
        {
            if (keysByEnglish.TryGetValue(source, out string existing))
                return existing;

            string key = $"ui.v3.{Slug(source)}.{Hash(source)}";
            AddEnglish(key, source, english, keysByEnglish);
            return key;
        }

        private static void AddEnglish(
            string key,
            string value,
            Dictionary<string, string> english,
            Dictionary<string, string> keysByEnglish)
        {
            string normalized = NormalizeSource(value);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrEmpty(normalized))
                return;
            english[key] = normalized;
            if (!keysByEnglish.ContainsKey(normalized))
                keysByEnglish.Add(normalized, key);
        }

        private static List<string> FindUiPrefabPaths() =>
            AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

        private static bool UsesSpecializedNarrativeLocalization(string path) =>
            path.Contains("/Narrative/FirstLaunch/", StringComparison.Ordinal);

        private static bool IsTranslatable(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            bool hasLatinLetter = false;
            bool hasSpace = false;
            bool hasDot = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character >= '\u0600' && character <= '\u06ff')
                    return false;
                if (character is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                    hasLatinLetter = true;
                if (char.IsWhiteSpace(character))
                    hasSpace = true;
                if (character == '.')
                    hasDot = true;
            }

            // Lowercase dotted identifiers are data keys, not player-facing text.
            if (hasDot && !hasSpace && string.Equals(value, value.ToLowerInvariant(), StringComparison.Ordinal))
                return false;
            return hasLatinLetter;
        }

        private static string ReadAuthoredText(TMP_Text text) =>
            text is RTLTMPro.RTLTextMeshPro rtl ? rtl.OriginalText : text.text;

        private static string NormalizeSource(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\u200B", string.Empty).Trim();

        private static string Slug(string source)
        {
            StringBuilder result = new();
            bool previousSeparator = false;
            for (int i = 0; i < source.Length && result.Length < 32; i++)
            {
                char character = char.ToLowerInvariant(source[i]);
                if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
                {
                    result.Append(character);
                    previousSeparator = false;
                }
                else if (!previousSeparator && result.Length > 0)
                {
                    result.Append('_');
                    previousSeparator = true;
                }
            }
            return result.ToString().Trim('_') is { Length: > 0 } slug ? slug : "text";
        }

        private static string Hash(string source)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(source));
            StringBuilder result = new(10);
            for (int i = 0; i < 5; i++)
                result.Append(bytes[i].ToString("x2"));
            return result.ToString();
        }

        private static Dictionary<string, string> ToDictionary(GameLocaleTable locale)
        {
            Dictionary<string, string> result = new(StringComparer.Ordinal);
            for (int i = 0; i < locale.Entries.Count; i++)
            {
                GameLocalizedStringRecord entry = locale.Entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Key))
                    result[entry.Key] = entry.Value ?? string.Empty;
            }
            return result;
        }

        private static List<string> FindMissingPersian(
            Dictionary<string, string> english,
            Dictionary<string, string> persian) =>
            english.Keys.Where(key => !persian.TryGetValue(key, out string value) ||
                                      string.IsNullOrWhiteSpace(value))
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();

        private static void WriteMissingReport(
            int prefabCount,
            int bindingCount,
            int englishCount,
            int persianCount,
            List<string> missing)
        {
            GameLocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(CatalogPath);
            GameLocaleTable english = catalog?.FindLocale(GameLocalization.EnglishLocaleCode);
            Dictionary<string, string> values = english != null
                ? ToDictionary(english)
                : new Dictionary<string, string>(StringComparer.Ordinal);
            StringBuilder report = new();
            report.AppendLine("# V3 UI localization coverage");
            report.AppendLine();
            report.AppendLine("Generated by `Game/UI/V3/Localization/Rebuild Catalog And Bind All Screens`.");
            report.AppendLine();
            report.AppendLine($"- UI prefabs audited: {prefabCount}");
            report.AppendLine($"- localized text bindings: {bindingCount}");
            report.AppendLine($"- English keys: {englishCount}");
            report.AppendLine($"- Persian values: {persianCount}");
            report.AppendLine($"- missing Persian values: {missing.Count}");
            report.AppendLine();
            report.AppendLine("## Missing Persian values");
            report.AppendLine();
            if (missing.Count == 0)
            {
                report.AppendLine("None.");
            }
            else
            {
                for (int i = 0; i < missing.Count; i++)
                {
                    values.TryGetValue(missing[i], out string value);
                    report.AppendLine($"- `{missing[i]}` — {value?.Replace("\n", " ")}");
                }
            }

            File.WriteAllText(MissingPersianReportPath, report.ToString(), Encoding.UTF8);
        }

        private static string HierarchyPath(Transform transform)
        {
            Stack<string> parts = new();
            Transform current = transform;
            while (current != null)
            {
                parts.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", parts);
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }

        [Serializable]
        private sealed class MatchPersianCatalog
        {
            public MatchPersianEntry[] entries;
        }

        [Serializable]
        private sealed class MatchPersianEntry
        {
            public string key;
            public string englishText;
            public string text;
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new();

            public new bool Equals(object left, object right) => ReferenceEquals(left, right);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
