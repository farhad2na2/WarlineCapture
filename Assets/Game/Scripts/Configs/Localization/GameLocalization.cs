using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>
    /// Locale-code-driven string resolver shared by menu, match, popup, and narrative UI.
    /// Adding a language is a catalog-only operation; screens keep the same stable keys.
    /// </summary>
    public static class GameLocalization
    {
        public const string EnglishLocaleCode = "en";
        public const string PersianLocaleCode = "fa-IR";
        public const string CatalogResourcePath = "Localization/V3UiLocalizationCatalog";
        public const string LocalePreferenceKey = "Game.Localization.LocaleCode";

        private static readonly Dictionary<string, string> SourceEntries =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> CurrentEntries =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> SourceKeysByValue =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> CurrentKeysByValue =
            new(StringComparer.Ordinal);
        private static readonly List<SourceTemplate> SourceTemplates = new();
        private static readonly List<SourceTemplate> CurrentTemplates = new();
        private static readonly Regex FormatToken = new(
            @"\{(?<index>\d+)(?:,[^}:]+)?(?::[^}]+)?\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static GameLocalizationCatalog catalog;
        private static GameLocaleTable currentLocale;
        private static bool initialized;

        public static event Action LocaleChanged;

        public static string CurrentLocaleCode
        {
            get
            {
                EnsureInitialized();
                return currentLocale?.LocaleCode ?? EnglishLocaleCode;
            }
        }

        public static bool IsRightToLeft
        {
            get
            {
                EnsureInitialized();
                return currentLocale?.RightToLeft ?? false;
            }
        }

        public static UnityEngine.Object CurrentFontAsset
        {
            get
            {
                EnsureInitialized();
                return currentLocale?.FontAsset;
            }
        }

        public static IReadOnlyList<GameLocaleTable> AvailableLocales
        {
            get
            {
                EnsureInitialized();
                return catalog?.Locales ?? Array.Empty<GameLocaleTable>();
            }
        }

        public static string[] GetLocaleShortLabels()
        {
            EnsureInitialized();
            if (catalog?.Locales == null || catalog.Locales.Count == 0)
                return new[] { "EN" };

            string[] labels = new string[catalog.Locales.Count];
            for (int i = 0; i < catalog.Locales.Count; i++)
            {
                GameLocaleTable locale = catalog.Locales[i];
                labels[i] = !string.IsNullOrWhiteSpace(locale?.ShortLabel)
                    ? locale.ShortLabel
                    : locale?.LocaleCode ?? string.Empty;
            }
            return labels;
        }

        public static int GetLocaleIndex(string localeCode)
        {
            EnsureInitialized();
            if (catalog?.Locales == null)
                return 0;

            for (int i = 0; i < catalog.Locales.Count; i++)
            {
                if (string.Equals(
                        catalog.Locales[i]?.LocaleCode,
                        localeCode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return 0;
        }

        public static string GetLocaleCode(int index)
        {
            EnsureInitialized();
            if (catalog?.Locales == null || index < 0 || index >= catalog.Locales.Count)
                return catalog?.SourceLocaleCode ?? EnglishLocaleCode;
            return catalog.Locales[index]?.LocaleCode ?? catalog.SourceLocaleCode;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            catalog = null;
            currentLocale = null;
            initialized = false;
            SourceEntries.Clear();
            CurrentEntries.Clear();
            SourceKeysByValue.Clear();
            CurrentKeysByValue.Clear();
            SourceTemplates.Clear();
            CurrentTemplates.Clear();
            LocaleChanged = null;
        }

        public static void EnsureInitialized()
        {
            if (initialized)
                return;

            string requestedLocale = PlayerPrefs.GetString(LocalePreferenceKey, EnglishLocaleCode);
            Initialize(
                Resources.Load<GameLocalizationCatalog>(CatalogResourcePath),
                requestedLocale,
                persist: false);
        }

        public static void Initialize(
            GameLocalizationCatalog configuredCatalog,
            string localeCode,
            bool persist = false)
        {
            catalog = configuredCatalog;
            initialized = true;
            RebuildSourceLookup();
            ApplyLocale(localeCode, persist, notify: false);
        }

        public static bool SetLocale(string localeCode, bool persist = true)
        {
            EnsureInitialized();
            return ApplyLocale(localeCode, persist, notify: true);
        }

        public static string Get(string key, string fallback = "")
        {
            EnsureInitialized();
            if (!string.IsNullOrWhiteSpace(key))
            {
                if (CurrentEntries.TryGetValue(key, out string localized))
                    return localized;
                if (SourceEntries.TryGetValue(key, out string source))
                    return source;
            }

            return fallback ?? key ?? string.Empty;
        }

        public static bool TryGet(string key, out string value)
        {
            EnsureInitialized();
            if (!string.IsNullOrWhiteSpace(key) && CurrentEntries.TryGetValue(key, out value))
                return true;
            if (!string.IsNullOrWhiteSpace(key) && SourceEntries.TryGetValue(key, out value))
                return true;

            value = string.Empty;
            return false;
        }

        public static bool TryGetBySource(string sourceText, out string key, out string localized)
        {
            EnsureInitialized();
            if (!string.IsNullOrEmpty(sourceText) &&
                SourceKeysByValue.TryGetValue(sourceText, out key))
            {
                localized = Get(key, sourceText);
                return true;
            }

            if (!string.IsNullOrEmpty(sourceText))
            {
                for (int i = 0; i < SourceTemplates.Count; i++)
                {
                    SourceTemplate template = SourceTemplates[i];
                    Match match = template.Pattern.Match(sourceText);
                    if (!match.Success)
                        continue;

                    key = template.Key;
                    localized = ApplyCapturedTemplate(Get(key, template.Source), match);
                    return true;
                }
            }

            key = string.Empty;
            localized = sourceText ?? string.Empty;
            return false;
        }

        public static string GetBySource(string sourceText)
        {
            return TryGetBySource(sourceText, out _, out string localized)
                ? localized
                : sourceText ?? string.Empty;
        }

        public static bool TryGetSourceByLocalized(
            string localizedText,
            out string key,
            out string sourceText)
        {
            EnsureInitialized();
            if (!string.IsNullOrEmpty(localizedText) &&
                CurrentKeysByValue.TryGetValue(localizedText, out key) &&
                SourceEntries.TryGetValue(key, out sourceText))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(localizedText))
            {
                for (int i = 0; i < CurrentTemplates.Count; i++)
                {
                    SourceTemplate template = CurrentTemplates[i];
                    Match match = template.Pattern.Match(localizedText);
                    if (!match.Success || !SourceEntries.TryGetValue(template.Key, out string source))
                        continue;

                    key = template.Key;
                    sourceText = ApplyCapturedTemplate(source, match);
                    return true;
                }
            }

            key = string.Empty;
            sourceText = localizedText ?? string.Empty;
            return false;
        }

        public static string Format(string key, string fallback, params object[] args)
        {
            string format = Get(key, fallback);
            if (string.IsNullOrEmpty(format) || args == null || args.Length == 0)
                return format;

            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"[GameLocalization] Invalid format for key '{key}': {format}");
                return fallback ?? format;
            }
        }

        private static bool ApplyLocale(string localeCode, bool persist, bool notify)
        {
            string requested = string.IsNullOrWhiteSpace(localeCode)
                ? catalog?.SourceLocaleCode ?? EnglishLocaleCode
                : localeCode;
            GameLocaleTable resolved = catalog?.FindLocale(requested);
            resolved ??= catalog?.FindLocale(catalog.SourceLocaleCode);
            resolved ??= catalog != null && catalog.Locales.Count > 0 ? catalog.Locales[0] : null;

            string previous = currentLocale?.LocaleCode ?? string.Empty;
            currentLocale = resolved;
            RebuildCurrentLookup();

            string appliedCode = currentLocale?.LocaleCode ?? EnglishLocaleCode;
            if (persist)
            {
                PlayerPrefs.SetString(LocalePreferenceKey, appliedCode);
                PlayerPrefs.Save();
            }

            bool changed = !string.Equals(previous, appliedCode, StringComparison.OrdinalIgnoreCase);
            if (notify && changed)
                LocaleChanged?.Invoke();
            return currentLocale != null && string.Equals(
                currentLocale.LocaleCode,
                requested,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void RebuildSourceLookup()
        {
            SourceEntries.Clear();
            SourceKeysByValue.Clear();
            SourceTemplates.Clear();
            GameLocaleTable source = catalog?.FindLocale(catalog.SourceLocaleCode);
            CopyEntries(source, SourceEntries);
            foreach (KeyValuePair<string, string> entry in SourceEntries)
            {
                if (!string.IsNullOrEmpty(entry.Value) && !SourceKeysByValue.ContainsKey(entry.Value))
                    SourceKeysByValue.Add(entry.Value, entry.Key);
                if (!string.IsNullOrEmpty(entry.Value) && FormatToken.IsMatch(entry.Value))
                {
                    SourceTemplates.Add(new SourceTemplate(
                        entry.Key,
                        entry.Value,
                        BuildTemplatePattern(entry.Value)));
                }
            }
            SourceTemplates.Sort((left, right) =>
                right.LiteralLength.CompareTo(left.LiteralLength));
        }

        private static void RebuildCurrentLookup()
        {
            CurrentEntries.Clear();
            CurrentKeysByValue.Clear();
            CurrentTemplates.Clear();
            CopyEntries(currentLocale, CurrentEntries);
            foreach (KeyValuePair<string, string> entry in CurrentEntries)
            {
                if (!string.IsNullOrEmpty(entry.Value) && !CurrentKeysByValue.ContainsKey(entry.Value))
                    CurrentKeysByValue.Add(entry.Value, entry.Key);
                if (!string.IsNullOrEmpty(entry.Value) && FormatToken.IsMatch(entry.Value))
                {
                    CurrentTemplates.Add(new SourceTemplate(
                        entry.Key,
                        entry.Value,
                        BuildTemplatePattern(entry.Value)));
                }
            }
            CurrentTemplates.Sort((left, right) =>
                right.LiteralLength.CompareTo(left.LiteralLength));
        }

        private static void CopyEntries(
            GameLocaleTable locale,
            Dictionary<string, string> destination)
        {
            if (locale?.Entries == null)
                return;

            for (int i = 0; i < locale.Entries.Count; i++)
            {
                GameLocalizedStringRecord entry = locale.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                    continue;
                destination[entry.Key] = entry.Value ?? string.Empty;
            }
        }

        private static Regex BuildTemplatePattern(string source)
        {
            StringBuilder pattern = new("^");
            int position = 0;
            foreach (Match token in FormatToken.Matches(source))
            {
                pattern.Append(Regex.Escape(source.Substring(position, token.Index - position)));
                pattern.Append("(?<v");
                pattern.Append(token.Groups["index"].Value);
                pattern.Append(@">.*?)");
                position = token.Index + token.Length;
            }
            pattern.Append(Regex.Escape(source.Substring(position)));
            pattern.Append('$');
            return new Regex(pattern.ToString(), RegexOptions.CultureInvariant);
        }

        private static string ApplyCapturedTemplate(string localizedTemplate, Match match)
        {
            if (string.IsNullOrEmpty(localizedTemplate))
                return localizedTemplate ?? string.Empty;

            return FormatToken.Replace(localizedTemplate, token =>
            {
                Group capture = match.Groups[$"v{token.Groups["index"].Value}"];
                return capture.Success ? capture.Value : token.Value;
            });
        }

        private sealed class SourceTemplate
        {
            public readonly string Key;
            public readonly string Source;
            public readonly Regex Pattern;
            public readonly int LiteralLength;

            public SourceTemplate(string key, string source, Regex pattern)
            {
                Key = key;
                Source = source;
                Pattern = pattern;
                LiteralLength = FormatToken.Replace(source, string.Empty).Length;
            }
        }
    }
}
