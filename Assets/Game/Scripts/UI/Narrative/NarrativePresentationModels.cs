using System;
using Game.Catalog.Contracts;
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

    public static class NarrativeSubtitleStyleUtilitySystemHelper
    {
        public static NarrativeSubtitleStyle Resolve(in UISettingsModel settings)
        {
            return new NarrativeSubtitleStyle
            {
                Visible = settings.Narrative.SubtitlesEnabled,
                FontSize = settings.Narrative.SubtitleSize switch
                {
                    UISubtitleSize.Small => 40f,
                    UISubtitleSize.Large => 60f,
                    UISubtitleSize.ExtraLarge => 72f,
                    _ => 50f
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

    [Serializable]
    public struct NarrativePunctuationPresentationModel
    {
        public float CharactersPerSecond;
        public float CommaPauseSeconds;
        public float ClausePauseSeconds;
        public float SentencePauseSeconds;
        public float EllipsisPauseSeconds;
        public float TailHoldSeconds;
    }

    [Serializable]
    public struct NarrativeLocationPresentationModel
    {
        public bool Visible;
        public string Title;
        public string Subtitle;
    }
}
