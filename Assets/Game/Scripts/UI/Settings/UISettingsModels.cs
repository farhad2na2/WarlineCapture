using System;

namespace Game.UI.Runtime
{
    [Serializable]
    public enum UIGraphicsQuality
    {
        Low = 0,
        Balanced = 1,
        High = 2,
        Ultra = 3
    }

    [Serializable]
    public enum UIFrameRateMode
    {
        Thirty = 0,
        Sixty = 1,
        OneTwenty = 2
    }

    [Serializable]
    public enum UIColorblindMode
    {
        Off = 0,
        Protanopia = 1,
        Deuteranopia = 2,
        Tritanopia = 3
    }

    [Serializable]
    public enum UILanguage
    {
        English = 0,
        // Legacy numeric values stay reserved so existing PlayerPrefs never migrate to Farsi by
        // accident. New language selection is locale-code driven by the shared catalog.
        German = 1,
        French = 2,
        Spanish = 3,
        Persian = 4
    }

    [Serializable]
    public enum UIAssistanceLevel
    {
        FullGuidance = 0,
        HintsOnly = 1,
        Minimal = 2,
        Off = 3
    }

    [Serializable]
    public enum UIAssistantNarrationMode
    {
        Off = 0,
        CriticalOnly = 1,
        Important = 2,
        All = 3
    }

    [Serializable]
    public enum UISubtitleSize
    {
        Small = 0,
        Standard = 1,
        Large = 2,
        ExtraLarge = 3
    }

    [Serializable]
    public enum UISubtitleBackgroundOpacity
    {
        ZeroPercent = 0,
        FiftyPercent = 1,
        SeventyFivePercent = 2,
        OneHundredPercent = 3
    }

    [Serializable]
    public struct AudioSettingsModel
    {
        public float MasterVolume;
        public float MusicVolume;
        public float SfxVolume;
        public float AlertsVolume;
        public float VoiceVolume;
        public bool MusicEnabled;
        public bool SoundEnabled;
        public bool VoiceEnabled;
    }

    [Serializable]
    public struct GraphicsSettingsModel
    {
        public UIGraphicsQuality Quality;
        public UIFrameRateMode FrameRateMode;
    }

    [Serializable]
    public struct ControlsSettingsModel
    {
        public float CameraSensitivity;
    }

    [Serializable]
    public struct NotificationSettingsModel
    {
        public bool ThreatWarnings;
    }

    [Serializable]
    public struct AccessibilitySettingsModel
    {
        public bool HighContrastUi;
        public bool LargeText;
        public UIColorblindMode ColorblindMode;
        public bool ReducedMotion;
    }

    [Serializable]
    public struct LocalizationSettingsModel
    {
        // Retained for migration from existing PlayerPrefs and serialized data.
        public UILanguage Language;
        public string LocaleCode;
    }

    [Serializable]
    public struct AssistantSettingsModel
    {
        public UIAssistanceLevel AssistanceLevel;
        public UIAssistantNarrationMode NarrationMode;
        public bool AllowTakeover;
        public bool SubtitlesEnabled;
    }

    [Serializable]
    public struct NarrativeSettingsModel
    {
        public bool SubtitlesEnabled;
        public UISubtitleSize SubtitleSize;
        public UISubtitleBackgroundOpacity BackgroundOpacity;
        public bool InstantText;
        public bool AutoAdvance;
    }

    [Serializable]
    public struct UISettingsModel
    {
        public AudioSettingsModel Audio;
        public GraphicsSettingsModel Graphics;
        public ControlsSettingsModel Controls;
        public NotificationSettingsModel Notifications;
        public AccessibilitySettingsModel Accessibility;
        public LocalizationSettingsModel Localization;
        public AssistantSettingsModel Assistant;
        public NarrativeSettingsModel Narrative;
    }
}
