using System;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        private static IUiLocalization localization = new UnboundLocalization();
        public static IUiLocalization Localization => localization;

        // Bound once by application composition before UI is instantiated. Tests can inject a catalog-free provider.
        public static void BindLocalization(IUiLocalization provider)
        {
            localization = provider ?? new UnboundLocalization();
        }

        private sealed class UnboundLocalization : IUiLocalization
        {
            public event Action LocaleChanged { add { } remove { } }
            public string CurrentLocaleCode => UiLocaleCodes.English;
            public bool IsRightToLeft => false;
            public UnityEngine.Object CurrentFontAsset => null;
            public int AvailableLocaleCount => 2;
            public string[] GetLocaleShortLabels() => new[] { "EN", "FA" };
            public int GetLocaleIndex(string code) => code == UiLocaleCodes.Persian ? 1 : 0;
            public string GetLocaleCode(int index) => index == 1 ? UiLocaleCodes.Persian : UiLocaleCodes.English;
            public bool SetLocale(string code, bool persist = true) => false;
            public string Get(string key, string fallback = "") => string.IsNullOrEmpty(fallback) ? key ?? "" : fallback;
            public bool TryGet(string key, out string value) { value = ""; return false; }
            public string Format(string key, string fallback, params object[] args) => string.Format(Get(key, fallback), args);
            public string GetBySource(string source) => source ?? "";
            public bool TryGetBySource(string source, out string key, out string value) { key = ""; value = source ?? ""; return false; }
            public bool TryGetSourceByLocalized(string value, out string key, out string source) { key = ""; source = value ?? ""; return false; }
        }
    }
}
