using Game.Configs;
using Game.UI.Contracts;

namespace Game.Composition
{
    internal sealed class GameTextResolverAdapter : IUiLocalization
    {
        public string Get(string key, string fallback = "")
        {
            return GameText.Get(key, fallback);
        }

        public bool TryGet(string key, out string value)
        {
            return GameText.TryGet(key, out value);
        }

        public string Format(string key, string fallback, params object[] args)
        {
            return GameText.Format(key, fallback, args);
        }
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void BindRuntimeLocalization()
        {
            Game.UI.Runtime.UiShellRuntimeGateway.BindLocalization(new GameTextResolverAdapter());
        }

        public event System.Action LocaleChanged
        {
            add => GameLocalization.LocaleChanged += value;
            remove => GameLocalization.LocaleChanged -= value;
        }
        public string CurrentLocaleCode => GameLocalization.CurrentLocaleCode;
        public bool IsRightToLeft => GameLocalization.IsRightToLeft;
        public UnityEngine.Object CurrentFontAsset => GameLocalization.CurrentFontAsset;
        public int AvailableLocaleCount => GameLocalization.AvailableLocales.Count;
        public string[] GetLocaleShortLabels() => GameLocalization.GetLocaleShortLabels();
        public int GetLocaleIndex(string code) => GameLocalization.GetLocaleIndex(code);
        public string GetLocaleCode(int index) => GameLocalization.GetLocaleCode(index);
        public bool SetLocale(string code, bool persist = true) => GameLocalization.SetLocale(code, persist);
        public string GetBySource(string source) => GameLocalization.GetBySource(source);
        public bool TryGetBySource(string source, out string key, out string value) => GameLocalization.TryGetBySource(source, out key, out value);
        public bool TryGetSourceByLocalized(string value, out string key, out string source) => GameLocalization.TryGetSourceByLocalized(value, out key, out source);
    }
}
