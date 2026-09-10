using System;
using UnityEngine;

namespace Game.UI.Contracts
{
    /// <summary>Presentation-facing text and locale access. Catalog ownership stays in composition.</summary>
    public interface IUiLocalization : IGameTextResolver
    {
        event Action LocaleChanged;
        string CurrentLocaleCode { get; }
        bool IsRightToLeft { get; }
        UnityEngine.Object CurrentFontAsset { get; }
        int AvailableLocaleCount { get; }
        string[] GetLocaleShortLabels();
        int GetLocaleIndex(string code);
        string GetLocaleCode(int index);
        bool SetLocale(string code, bool persist = true);
        string GetBySource(string source);
        bool TryGetBySource(string source, out string key, out string value);
        bool TryGetSourceByLocalized(string value, out string key, out string source);
    }

    public static class UiLocaleCodes
    {
        public const string English = "en";
        public const string Persian = "fa-IR";
        public const string PreferenceKey = "Game.Localization.LocaleCode";
    }
}
