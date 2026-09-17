using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Imports reviewed Farsi copy without rebuilding UI prefabs or touching other locales.</summary>
    public static partial class PersianConversationalToneImporter
    {
        private const string ReportRoot = "Design/AgentReports/PersianConversationalTone/";
        [Serializable] private sealed class Change { public string before; public string after; }
        [Serializable] private sealed class Changes { public Change[] entries; }
        [Serializable] private sealed class TextEntry { public string key; public string text; public string value; }
        [Serializable] private sealed class TextSource { public TextEntry[] entries; public TextEntry[] text; public TextEntry[] lines; }

        [MenuItem("Game/Localization/Apply Approved Conversational Farsi")]
        public static void Import()
        {
            var changes = JsonUtility.FromJson<Changes>(File.ReadAllText(ReportRoot + "caption_migration.json"))
                .entries.ToDictionary(e => e.before, e => e.after, StringComparer.Ordinal);
            var canonical = CanonicalCopy();
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Missing central localization catalog.");
            var otherLocalesBefore = SnapshotOtherLocales(catalog);
            var tables = new List<GameLocaleTable>();
            int changed = 0;
            foreach (var locale in catalog.Locales)
            {
                if (locale.LocaleCode != "fa-IR") { tables.Add(locale); continue; }
                var values = locale.Entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal);
                // Remove only the temporary draft keys introduced by this migration.
                foreach (string key in values.Keys.Where(k => Regex.IsMatch(k, @"^mission\.m0[45]\.guide\.(example|mistake)\.\d+$")).ToArray()) values.Remove(key);
                foreach (string key in values.Keys.ToArray())
                {
                    string previous = values[key];
                    if (changes.TryGetValue(previous, out string replacement)) values[key] = replacement;
                    if (canonical.TryGetValue(key, out string authoritative)) values[key] = authoritative;
                    if (previous != values[key]) changed++;
                }
                foreach (var entry in canonical) values[entry.Key] = entry.Value;
                tables.Add(new GameLocaleTable(locale.LocaleCode, locale.DisplayName, locale.ShortLabel,
                    locale.RightToLeft, locale.FontAsset, values.OrderBy(e => e.Key, StringComparer.Ordinal)
                        .Select(e => new GameLocalizedStringRecord(e.Key, e.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode, tables);
            if (SnapshotOtherLocales(catalog) != otherLocalesBefore)
                throw new InvalidOperationException("Import changed an unrelated language.");
            EditorUtility.SetDirty(catalog);

            // Comics resolve this shared narrative locale directly, not only the HUD catalog.
            var narrative = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath);
            if (narrative == null) throw new InvalidOperationException("Missing Farsi narrative locale.");
            var serialized = new SerializedObject(narrative);
            var text = serialized.FindProperty("text");
            for (int i = 0; i < text.arraySize; i++)
            {
                var entry = text.GetArrayElementAtIndex(i);
                string key = entry.FindPropertyRelative("key").stringValue;
                var value = entry.FindPropertyRelative("value");
                if (changes.TryGetValue(value.stringValue, out string replacement)) value.stringValue = replacement;
                if (canonical.TryGetValue(key, out string authoritative)) value.stringValue = authoritative;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(narrative);
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log($"[PersianConversationalToneImport] result=Passed changed={changed} canonical={canonical.Count} otherLocales=unchanged");
        }

        public static void Validate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            var expectedCopy = CanonicalCopy();
            var actual = catalog.FindLocale("fa-IR").Entries.ToDictionary(e => e.Key, e => e.Value);
            var english = catalog.FindLocale(catalog.SourceLocaleCode).Entries.ToDictionary(e => e.Key, e => e.Value);
            foreach (var expected in expectedCopy)
            {
                if (!actual.TryGetValue(expected.Key, out string value) || value != expected.Value)
                    throw new InvalidOperationException("Farsi source/catalog mismatch: " + expected.Key);
                if (english.TryGetValue(expected.Key, out string source) && Placeholders(source) != Placeholders(value))
                    throw new InvalidOperationException("Farsi placeholder mismatch: " + expected.Key);
            }
            var narrative = new SerializedObject(AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath));
            var entries = narrative.FindProperty("text");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                string key = entry.FindPropertyRelative("key").stringValue;
                if (expectedCopy.TryGetValue(key, out string expected) && entry.FindPropertyRelative("value").stringValue != expected)
                    throw new InvalidOperationException("Narrative caption mismatch: " + key);
            }
            Debug.Log("[PersianConversationalToneValidation] result=Passed captions=canonical placeholders=preserved");
        }

        private static string Placeholders(string text) => string.Join(",", Regex.Matches(text ?? "", @"\{\d+(?:[^}]*)\}").Cast<Match>().Select(m => m.Value).OrderBy(v => v));
        private static string SnapshotOtherLocales(GameLocalizationCatalog catalog) => string.Join("\n", catalog.Locales
            .Where(l => l.LocaleCode != "fa-IR").SelectMany(l => l.Entries.Select(e => l.LocaleCode + "|" + e.Key + "|" + e.Value)));

        private static Dictionary<string, string> CanonicalCopy()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var shared = JsonUtility.FromJson<TextSource>(File.ReadAllText("Assets/Game/Audio/GeneratedSource/aria_match_voice_fa_text_catalog_v0_1.json"));
            foreach (var e in shared.entries) result[e.key] = e.text;
            var first = JsonUtility.FromJson<TextSource>(File.ReadAllText("Assets/Game/Data/Narrative/FirstLaunch/first_launch_persian_text_catalog.json"));
            foreach (var e in first.text) result[e.key] = e.value;
            foreach (var e in first.lines) result[e.key] = e.text;
            foreach (var e in M02EstablishBaseTextCatalog.Brief.Concat(M02EstablishBaseTextCatalog.Comms).Concat(M02EstablishBaseTextCatalog.Debrief)) result[e.TextKey] = e.Persian;
            foreach (var e in M03RadarWarningCopyCatalog.Brief.Concat(M03RadarWarningCopyCatalog.Comms).Concat(M03RadarWarningCopyCatalog.Debrief).Concat(M03RadarWarningCopyCatalog.DebriefOutcomes)) result[e.Key] = e.Persian;
            foreach (var e in M03RadarWarningUiCopyCatalog.Entries.Concat(M03RadarWarningGuideCopyCatalog.Entries)) result[e.Key] = e.Persian;
            for (int i = 0; i < M03RadarWarningTutorialCopyCatalog.Steps.Length; i++)
            {
                var step = M03RadarWarningTutorialCopyCatalog.Steps[i];
                result[$"mission.m03.tutorial.{i + 1}.title"] = step.PersianTitle;
                result[$"mission.m03.tutorial.{i + 1}.body"] = step.PersianBody;
            }
            foreach (var e in M04AirliftCopyCatalog.Brief.Concat(M04AirliftCopyCatalog.Comms).Concat(M04AirliftCopyCatalog.Debrief)) result[e.Key] = e.Persian;
            foreach (var e in M05BreachAssaultCopyCatalog.Brief.Concat(M05BreachAssaultCopyCatalog.Comms).Concat(M05BreachAssaultCopyCatalog.Debrief)) result[e.Key] = e.Persian;
            foreach (var e in M04AirliftCopyCatalog.Ui.Concat(M05BreachAssaultCopyCatalog.Ui)) result[e.Key] = e.Persian;
            AddLessons(result, "m04", M04AirliftCopyCatalog.Lessons);
            AddLessons(result, "m05", M05BreachAssaultCopyCatalog.Lessons);
            return result;
        }

        private static void AddLessons(Dictionary<string, string> result, string mission,
            (string Title, string PersianTitle, string Body, string PersianBody, string Example, string PersianExample, string Mistake, string PersianMistake)[] lessons)
        {
            for (int i = 0; i < lessons.Length; i++)
            {
                var lesson = lessons[i];
                result[$"mission.{mission}.tutorial.{i + 1}.title"] = lesson.PersianTitle;
                result[$"mission.{mission}.tutorial.{i + 1}.body"] = lesson.PersianBody;
                result[$"mission.{mission}.tutorial.{i + 1}.example"] = lesson.PersianExample;
                result[$"mission.{mission}.tutorial.{i + 1}.mistake"] = lesson.PersianMistake;
            }
        }
    }
}
