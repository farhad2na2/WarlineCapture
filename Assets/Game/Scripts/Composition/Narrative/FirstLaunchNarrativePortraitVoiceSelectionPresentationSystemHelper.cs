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
        private const int NeutralCommanderPortraitIndex = 6;
        private readonly Dictionary<string, NarrativeLocaleVoiceRecord> localizedVoices = new(StringComparer.Ordinal);
        private NarrativeCommanderIdentityView commanderView;
        private int portraitIndex = NeutralCommanderPortraitIndex;
        private Sprite portrait;

        public void Reset(
            NarrativeCommanderIdentityView nextCommanderView,
            NarrativeLocaleConfig localeConfig)
        {
            commanderView = nextCommanderView;
            portraitIndex = NeutralCommanderPortraitIndex;
            portrait = null;
            SetLocale(localeConfig);
        }

        public void Apply(
            FirstLaunchNarrativeInteractivePresentationSystemHelper interactivePresentation,
            in NarrativeCommanderIdentityData identity,
            int selectedPortraitIndex)
        {
            interactivePresentation?.ApplyCommanderIdentity(identity, selectedPortraitIndex);
            Capture(selectedPortraitIndex);
        }

        public void Capture(int selectedPortraitIndex)
        {
            portraitIndex = selectedPortraitIndex;
            portrait = commanderView?.SelectedPortrait;
        }

        public Sprite ResolvePortrait(
            NarrativeDialogueLineRecord line,
            NarrativeSpeakerRecord speaker)
        {
            return line.Speaker == NarrativeSpeakerId.Commander && portrait != null
                ? portrait
                : speaker.IdentitySprite;
        }

        private AudioClip ResolveVoiceClip(
            NarrativeSpeakerId speaker,
            AudioClip voiceClip,
            AudioClip femaleVoiceClip,
            AudioClip neutralVoiceClip)
        {
            if (speaker != NarrativeSpeakerId.Commander)
                return voiceClip;

            return portraitIndex switch
            {
                0 or 2 or 5 => femaleVoiceClip != null ? femaleVoiceClip : voiceClip,
                NeutralCommanderPortraitIndex => neutralVoiceClip != null ? neutralVoiceClip : voiceClip,
                _ => voiceClip
            };
        }
    }
}
