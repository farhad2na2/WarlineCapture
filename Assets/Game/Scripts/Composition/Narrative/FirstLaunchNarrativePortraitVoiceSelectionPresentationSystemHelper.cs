using Game.Catalog.Contracts;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.UI.Runtime;
using UnityEngine;

namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativePortraitVoiceSelectionPresentationSystemHelper
    {
        private const int NeutralCommanderPortraitIndex = 6;
        private NarrativeCommanderIdentityView commanderView;
        private int portraitIndex = NeutralCommanderPortraitIndex;
        private Sprite portrait;

        public void Reset(NarrativeCommanderIdentityView nextCommanderView)
        {
            commanderView = nextCommanderView;
            portraitIndex = NeutralCommanderPortraitIndex;
            portrait = null;
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

        public AudioClip ResolveVoiceClip(NarrativeDialogueLineRecord line)
        {
            if (line.Speaker != NarrativeSpeakerId.Commander)
                return line.VoiceClip;

            return portraitIndex switch
            {
                0 or 2 or 5 => line.FemaleVoiceClip != null ? line.FemaleVoiceClip : line.VoiceClip,
                NeutralCommanderPortraitIndex => line.NeutralVoiceClip != null ? line.NeutralVoiceClip : line.VoiceClip,
                _ => line.VoiceClip
            };
        }
    }
}
