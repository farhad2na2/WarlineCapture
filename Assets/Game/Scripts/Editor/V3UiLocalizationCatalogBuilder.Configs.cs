using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    public static partial class V3UiLocalizationCatalogBuilder
    {
        public const string UiStringsConfigFolder = "Assets/Game/Configs/Localization";
        [Serializable] public sealed class UiStringsConfig { public UiStringConfigEntry[] entries; }
        [Serializable] public sealed class UiStringConfigEntry
        {
            public string key;
            public string source;
            public UiStringTranslation[] translations;
        }
        [Serializable] public sealed class UiStringTranslation { public string locale; public string value; }

        public static List<UiStringConfigEntry> ReadUiStringConfigs()
        {
            var entries = new List<UiStringConfigEntry>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in Directory.GetFiles(UiStringsConfigFolder, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                var config = JsonUtility.FromJson<UiStringsConfig>(File.ReadAllText(path));
                if (config?.entries == null) throw new InvalidOperationException($"No localization entries: {path}");
                foreach (var entry in config.entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.key) || string.IsNullOrWhiteSpace(entry.source) || !keys.Add(entry.key))
                        throw new InvalidOperationException($"Empty or duplicate localization key: {path}: {entry.key}");
                    var locales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var translation in entry.translations ?? Array.Empty<UiStringTranslation>())
                        if (string.IsNullOrWhiteSpace(translation.locale) || string.IsNullOrWhiteSpace(translation.value) || !locales.Add(translation.locale))
                            throw new InvalidOperationException($"Empty or duplicate translation: {entry.key}/{translation.locale}");
                    entries.Add(entry);
                }
            }
            return entries;
        }

        // Run after legacy imports so configuration is authoritative for duplicated source aliases.
        private static void ImportUiStringConfigs(Dictionary<string, string> english,
            Dictionary<string, string> persian, Dictionary<string, string> keysByEnglish)
        {
            foreach (var entry in ReadUiStringConfigs())
            {
                english[entry.key] = entry.source;
                keysByEnglish[entry.source] = entry.key;
                var translation = entry.translations?.FirstOrDefault(t => t.locale == GameLocalization.PersianLocaleCode);
                if (translation == null) throw new InvalidOperationException($"Missing Farsi config: {entry.key}");
                foreach (string key in english.Where(e => e.Value == entry.source).Select(e => e.Key).ToArray())
                    persian[key] = translation.value;
            }
        }

        [MenuItem("Game/UI/V3/Localization/Apply Configured UI Strings")]
        public static void ApplyConfiguredUiStrings()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Missing UI localization catalog.");
            var english = ToDictionary(catalog.FindLocale(catalog.SourceLocaleCode));
            foreach (var entry in ReadUiStringConfigs()) english[entry.key] = entry.source;
            var tables = new List<GameLocaleTable>();
            foreach (var locale in catalog.Locales)
            {
                var values = locale.LocaleCode == catalog.SourceLocaleCode ? english : ToDictionary(locale);
                if (locale.LocaleCode != catalog.SourceLocaleCode)
                    foreach (var entry in ReadUiStringConfigs())
                    {
                        var translation = entry.translations?.FirstOrDefault(t => t.locale == locale.LocaleCode);
                        if (translation == null) throw new InvalidOperationException($"Missing config translation: {locale.LocaleCode}/{entry.key}");
                        foreach (string key in english.Where(e => e.Value == entry.source).Select(e => e.Key))
                            values[key] = translation.value;
                    }
                tables.Add(new GameLocaleTable(locale.LocaleCode, locale.DisplayName, locale.ShortLabel,
                    locale.RightToLeft, locale.FontAsset, ToRecords(values)));
            }
            catalog.Configure(catalog.SourceLocaleCode, tables);
            ValidateAllLocaleTables(catalog);
            EditorUtility.SetDirty(catalog);
            const string narrativePath = "Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchNarrativeSequence.prefab";
            var root = PrefabUtility.LoadPrefabContents(narrativePath);
            try
            {
                ExtendConfiguredNarrativeBindings(root.GetComponent<NarrativeSequenceView>());
                PrefabUtility.SaveAsPrefabAsset(root, narrativePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            MissionResultV3PrefabBuilder.RepairLocalizedObjectiveSpacing();
            AssetDatabase.SaveAssets();
            RebuildSharedPersianFontCoverage();
            Debug.Log("[ConfiguredUiLocalization] result=Passed configEntries=" + ReadUiStringConfigs().Count);
        }

        public static void ExtendConfiguredNarrativeBindings(NarrativeSequenceView sequence)
        {
            var serialized = new SerializedObject(sequence);
            var targets = serialized.FindProperty("localizedTextTargets");
            var keys = serialized.FindProperty("localizedTextKeys");
            var fallbacks = serialized.FindProperty("localizedTextEnglishFallbacks");
            var entries = ReadUiStringConfigs().GroupBy(e => e.source, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var bound = new HashSet<UnityEngine.Object>();
            for (int i = 0; i < targets.arraySize; i++) bound.Add(targets.GetArrayElementAtIndex(i).objectReferenceValue);
            foreach (var target in sequence.GetComponentsInChildren<TMP_Text>(true))
            {
                if (bound.Contains(target) || !entries.TryGetValue(ReadAuthoredText(target), out var entry)) continue;
                int index = targets.arraySize;
                targets.arraySize++; keys.arraySize++; fallbacks.arraySize++;
                targets.GetArrayElementAtIndex(index).objectReferenceValue = target;
                keys.GetArrayElementAtIndex(index).stringValue = entry.key;
                fallbacks.GetArrayElementAtIndex(index).stringValue = entry.source;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ValidateConfiguredCatalogEntries(GameLocalizationCatalog catalog)
        {
            var source = CheckedTable(catalog.FindLocale(catalog.SourceLocaleCode));
            foreach (var entry in ReadUiStringConfigs())
            {
                if (!source.TryGetValue(entry.key, out string authored) || authored != entry.source)
                    throw new InvalidOperationException("Config has not been imported: " + entry.key);
                foreach (var locale in catalog.Locales.Where(l => l.LocaleCode != catalog.SourceLocaleCode))
                {
                    var translation = entry.translations?.FirstOrDefault(t => t.locale == locale.LocaleCode);
                    var values = CheckedTable(locale);
                    if (translation == null || !values.TryGetValue(entry.key, out string value) || value != translation.value)
                        throw new InvalidOperationException($"Config translation has not been imported: {locale.LocaleCode}/{entry.key}");
                }
            }
        }

        public static void ValidateFirstLaunchStaticBindings()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchNarrativeSequence.prefab");
            var serialized = new SerializedObject(root.GetComponent<NarrativeSequenceView>());
            var targets = serialized.FindProperty("localizedTextTargets");
            var bound = new HashSet<UnityEngine.Object>();
            for (int i = 0; i < targets.arraySize; i++) bound.Add(targets.GetArrayElementAtIndex(i).objectReferenceValue);
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                // Dialogue and player input have their own dynamic owners. Static setup labels must be explicit.
                bool setup = text.GetComponentInParent<NarrativeCommanderIdentityView>(true) != null ||
                             text.GetComponentInParent<NarrativeGuidanceChoiceView>(true) != null;
                if (!setup || text.GetComponentInParent<TMP_InputField>(true) != null || text.color.a == 0 ||
                    !IsTranslatable(ReadAuthoredText(text)) || ReadAuthoredText(text) == "CC") continue;
                if (!bound.Contains(text)) throw new InvalidOperationException("Unbound first-launch text: " + text.name + ": " + ReadAuthoredText(text));
            }
        }

        public static void ValidateAllLocaleTables(GameLocalizationCatalog catalog)
        {
            if (catalog == null) throw new InvalidOperationException("Missing locale catalog.");
            var source = CheckedTable(catalog.FindLocale(catalog.SourceLocaleCode));
            var errors = new List<string>();
            foreach (var locale in catalog.Locales)
            {
                var values = CheckedTable(locale);
                foreach (var pair in source)
                {
                    if (!values.TryGetValue(pair.Key, out string value) || string.IsNullOrWhiteSpace(value))
                        errors.Add($"{locale.LocaleCode}: missing {pair.Key}");
                    else if (FormatArguments(pair.Value) != FormatArguments(value))
                        errors.Add($"{locale.LocaleCode}: format arguments differ for {pair.Key}");
                }
                foreach (string key in values.Keys)
                    if (!source.ContainsKey(key)) errors.Add($"{locale.LocaleCode}: unknown key {key}");
            }
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static Dictionary<string, string> CheckedTable(GameLocaleTable locale)
        {
            if (locale == null) throw new InvalidOperationException("Missing locale table.");
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in locale.Entries)
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key) || !values.TryAdd(entry.Key, entry.Value))
                    throw new InvalidOperationException($"{locale.LocaleCode}: empty or duplicate key {entry?.Key}");
            return values;
        }
        private static string FormatArguments(string value) => string.Join(",", Regex.Matches(value ?? "", @"(?<!\{)\{(\d+)(?:,[^}:]+)?(?::[^}]+)?\}(?!\})")
            .Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(s => s, StringComparer.Ordinal));
    }

    // Fail a player build instead of silently shipping an incomplete newly added locale.
    public sealed class UiLocalizationBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            V3UiLocalizationCatalogBuilder.ValidateAllLocaleTables(
                AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath));
            V3UiLocalizationCatalogBuilder.ValidateConfiguredCatalogEntries(
                AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath));
            V3UiLocalizationCatalogBuilder.ValidateFirstLaunchStaticBindings();
        }
    }
}
