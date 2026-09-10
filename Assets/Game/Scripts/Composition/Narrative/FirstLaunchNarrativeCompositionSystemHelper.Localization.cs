using System;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.Narrative.Runtime;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;

namespace Game.Composition
{
    internal sealed partial class FirstLaunchNarrativeCompositionSystemHelper
    {
        private void HandleLanguageSelected(FirstLaunchNarrativeLanguage language)
        {
            if (!awaitingLanguage ||
                language != FirstLaunchNarrativeLanguage.English &&
                language != FirstLaunchNarrativeLanguage.Persian)
            {
                return;
            }

            if (language == FirstLaunchNarrativeLanguage.Persian && persianLocale == null)
            {
                UnityEngine.Debug.LogError(
                    "[FirstLaunchNarrative] Persian was selected, but no Persian locale is configured.");
                languageChoiceView?.SetVisible(true);
                return;
            }

            profileComposition.CommitLanguage(language, true);
            UISettingsModel settings = SettingsService.Load();
            settings.Localization.Language = language == FirstLaunchNarrativeLanguage.Persian
                ? UILanguage.Persian
                : UILanguage.English;
            settings.Localization.LocaleCode = language == FirstLaunchNarrativeLanguage.Persian
                ? GameLocalization.PersianLocaleCode
                : GameLocalization.EnglishLocaleCode;
            SettingsService.Save(settings);
            SettingsService.ApplyRuntime(settings);
            awaitingLanguage = false;
            languageChoiceView?.SetVisible(false);
            if (StartNarrative(language))
                return;

            shellComposition.SetStartupDisposition(FirstLaunchNarrativeStartupDisposition.EnterMenu);
        }

        private static void SynchronizeUiLocale(FirstLaunchNarrativeLanguage language)
        {
            if (language == FirstLaunchNarrativeLanguage.Unselected ||
                UnityEngine.PlayerPrefs.HasKey(GameLocalization.LocalePreferenceKey))
                return;

            UISettingsModel settings = SettingsService.Load();
            UILanguage uiLanguage = language == FirstLaunchNarrativeLanguage.Persian
                ? UILanguage.Persian
                : UILanguage.English;
            if (settings.Localization.Language != uiLanguage)
            {
                settings.Localization.Language = uiLanguage;
            }
            settings.Localization.LocaleCode = uiLanguage == UILanguage.Persian
                ? GameLocalization.PersianLocaleCode
                : GameLocalization.EnglishLocaleCode;
            SettingsService.Save(settings);
            GameLocalization.SetLocale(
                uiLanguage == UILanguage.Persian
                    ? GameLocalization.PersianLocaleCode
                    : GameLocalization.EnglishLocaleCode,
                persist: true);
        }


        internal sealed class SharedLocaleCompositionSystemHelper : IGameTextResolver
        {
            private readonly IGameTextResolver fallbackResolver;

            public SharedLocaleCompositionSystemHelper(IGameTextResolver fallback)
            {
                fallbackResolver = fallback ?? FallbackGameTextResolver.Instance;
            }

            public string Get(string key, string fallback)
            {
                string resolvedFallback = fallbackResolver.Get(key, fallback);
                return GameLocalization.Get(key, resolvedFallback);
            }

            public bool TryGet(string key, out string value)
            {
                if (GameLocalization.TryGet(key, out value))
                    return true;

                return fallbackResolver.TryGet(key, out value);
            }

            public string Format(string key, string fallback, params object[] args)
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
                    return fallback ?? format;
                }
            }
        }
    }
}
