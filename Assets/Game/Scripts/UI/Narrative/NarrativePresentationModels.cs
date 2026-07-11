using System;
using Game.Configs;
using UnityEngine;

namespace Game.UI.Runtime
{
    public enum NarrativeDialoguePhase
    {
        Hidden = 0,
        Revealing = 1,
        AdvanceReady = 2
    }

    [Serializable]
    public struct NarrativeSubtitleStyle
    {
        public bool Visible;
        public float FontSize;
        public float BackgroundOpacity;
        public bool InstantText;
        public bool ReducedMotion;
    }

    public static class NarrativeSubtitleStyleResolver
    {
        public static NarrativeSubtitleStyle Resolve(in UISettingsModel settings)
        {
            return new NarrativeSubtitleStyle
            {
                Visible = settings.Narrative.SubtitlesEnabled,
                FontSize = settings.Narrative.SubtitleSize switch
                {
                    UISubtitleSize.Small => 24f,
                    UISubtitleSize.Large => 36f,
                    UISubtitleSize.ExtraLarge => 44f,
                    _ => 30f
                },
                BackgroundOpacity = settings.Narrative.BackgroundOpacity switch
                {
                    UISubtitleBackgroundOpacity.ZeroPercent => 0f,
                    UISubtitleBackgroundOpacity.FiftyPercent => 0.5f,
                    UISubtitleBackgroundOpacity.OneHundredPercent => 1f,
                    _ => 0.75f
                },
                InstantText = settings.Narrative.InstantText,
                ReducedMotion = settings.Accessibility.ReducedMotion
            };
        }
    }

    [Serializable]
    public struct NarrativeSpeakerPresentationModel
    {
        public NarrativeSpeakerId SpeakerId;
        public string DisplayName;
        public string Role;
        public string AccessibleLabel;
        public Sprite IdentitySprite;
        public Color AccentColor;
        public NarrativeSpeakerTreatment Treatment;
    }

    [Serializable]
    public struct NarrativePanelPresentationModel
    {
        public string StateId;
        public Sprite PanelSprite;
        public Color Tint;
    }
}
