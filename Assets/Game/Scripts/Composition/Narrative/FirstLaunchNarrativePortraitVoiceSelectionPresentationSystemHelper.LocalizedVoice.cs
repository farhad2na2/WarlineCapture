using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.UI.Runtime;
using UnityEngine;

namespace Game.Composition
{
    internal sealed partial class FirstLaunchNarrativePortraitVoiceSelectionPresentationSystemHelper
    {
        public void SetLocale(NarrativeLocaleConfig localeConfig)
        {
            localizedVoices.Clear();
            if (localeConfig == null)
                return;

            IReadOnlyList<NarrativeLocaleVoiceRecord> localeVoices = localeConfig.Voices;
            for (int i = 0; i < localeVoices.Count; i++)
            {
                NarrativeLocaleVoiceRecord voice = localeVoices[i];
                if (voice == null || string.IsNullOrWhiteSpace(voice.LineId))
                    continue;
                localizedVoices[voice.LineId] = voice;
            }
        }

        public AudioClip ResolveVoiceClip(NarrativeDialogueLineRecord line)
        {
            if (localizedVoices.TryGetValue(line.LineId, out NarrativeLocaleVoiceRecord localized))
            {
                return ResolveVoiceClip(
                    line.Speaker,
                    localized.VoiceClip,
                    localized.FemaleVoiceClip,
                    localized.NeutralVoiceClip);
            }

            return ResolveVoiceClip(
                line.Speaker,
                line.VoiceClip,
                line.FemaleVoiceClip,
                line.NeutralVoiceClip);
        }

    }
}
