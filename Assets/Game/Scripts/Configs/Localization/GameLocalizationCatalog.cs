using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class GameLocalizedStringRecord
    {
        [SerializeField] private string key;
        [SerializeField, TextArea] private string value;

        public string Key => key;
        public string Value => value;

        public GameLocalizedStringRecord(string localizedKey, string localizedValue)
        {
            key = localizedKey ?? string.Empty;
            value = localizedValue ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class GameLocaleTable
    {
        [SerializeField] private string localeCode = "en";
        [SerializeField] private string displayName = "English";
        [SerializeField] private string shortLabel = "EN";
        [SerializeField] private bool rightToLeft;
        [SerializeField] private UnityEngine.Object fontAsset;
        [SerializeField] private List<GameLocalizedStringRecord> entries = new();

        public string LocaleCode => localeCode;
        public string DisplayName => displayName;
        public string ShortLabel => shortLabel;
        public bool RightToLeft => rightToLeft;
        public UnityEngine.Object FontAsset => fontAsset;
        public IReadOnlyList<GameLocalizedStringRecord> Entries => entries;

        public GameLocaleTable(
            string code,
            string name,
            string label,
            bool isRightToLeft,
            UnityEngine.Object localeFont,
            IEnumerable<GameLocalizedStringRecord> localizedEntries)
        {
            localeCode = code ?? string.Empty;
            displayName = name ?? string.Empty;
            shortLabel = label ?? string.Empty;
            rightToLeft = isRightToLeft;
            fontAsset = localeFont;
            entries = localizedEntries != null
                ? new List<GameLocalizedStringRecord>(localizedEntries)
                : new List<GameLocalizedStringRecord>();
        }
    }

    [CreateAssetMenu(
        menuName = "Game/Localization/Game Localization Catalog",
        fileName = "V3UiLocalizationCatalog")]
    public sealed class GameLocalizationCatalog : ScriptableObject
    {
        [SerializeField] private string sourceLocaleCode = "en";
        [SerializeField] private List<GameLocaleTable> locales = new();

        public string SourceLocaleCode => sourceLocaleCode;
        public IReadOnlyList<GameLocaleTable> Locales => locales;

        public void Configure(string sourceCode, IEnumerable<GameLocaleTable> localeTables)
        {
            sourceLocaleCode = string.IsNullOrWhiteSpace(sourceCode)
                ? "en"
                : sourceCode;
            locales = localeTables != null
                ? new List<GameLocaleTable>(localeTables)
                : new List<GameLocaleTable>();
        }

        public GameLocaleTable FindLocale(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
                return null;

            for (int i = 0; i < locales.Count; i++)
            {
                GameLocaleTable locale = locales[i];
                if (locale != null && string.Equals(
                        locale.LocaleCode,
                        localeCode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return locale;
                }
            }

            return null;
        }
    }
}
