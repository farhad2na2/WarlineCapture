using Game.Configs;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEngine;

namespace Game.Composition
{
    internal sealed partial class CampaignMissionDebriefCompositionSystemHelper
    {
        private FirstLaunchNarrativeLanguage renderedLanguage;
        private bool renderedWide;

        private void RefreshActivePresentation()
        {
            if (!running || !presentation.IsRunning) return;
            var language = CampaignMissionNarrativeCompositionUtility.ReadLanguage();
            bool wide = Screen.height > 0 && (float)Screen.width / Screen.height >= 2f;
            if (language == renderedLanguage && wide == renderedWide) return;
            NarrativeLocaleConfig locale = language == FirstLaunchNarrativeLanguage.Persian ? persianLocale : null;
            IGameTextResolver legacy = locale != null
                ? new FirstLaunchNarrativeLocaleTextCompositionSystemHelper(baseTextResolver, locale) : baseTextResolver;
            presentation.RefreshPresentation(new SharedLocalizationTextCompositionSystemHelper(legacy), locale, language != renderedLanguage);
            renderedLanguage = language;
            renderedWide = wide;
        }
    }
}
