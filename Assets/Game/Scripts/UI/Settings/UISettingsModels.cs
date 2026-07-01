using System;

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
    German = 1,
    French = 2,
    Spanish = 3
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
public struct AudioSettingsModel
{
    public float MasterVolume;
    public float MusicVolume;
    public float SfxVolume;
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
}

[Serializable]
public struct LocalizationSettingsModel
{
    public UILanguage Language;
}

[Serializable]
public struct AssistantSettingsModel
{
    public UIAssistanceLevel AssistanceLevel;
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
}
