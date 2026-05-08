using System;

[Serializable]
public enum WarlineCaptureGraphicsQuality
{
    Low = 0,
    Balanced = 1,
    High = 2,
    Ultra = 3
}

[Serializable]
public enum WarlineCaptureFrameRateMode
{
    Thirty = 0,
    Sixty = 1,
    OneTwenty = 2
}

[Serializable]
public enum WarlineCaptureColorblindMode
{
    Off = 0,
    Protanopia = 1,
    Deuteranopia = 2,
    Tritanopia = 3
}

[Serializable]
public enum WarlineCaptureLanguage
{
    English = 0,
    German = 1,
    French = 2,
    Spanish = 3
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
    public WarlineCaptureGraphicsQuality Quality;
    public WarlineCaptureFrameRateMode FrameRateMode;
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
    public WarlineCaptureColorblindMode ColorblindMode;
}

[Serializable]
public struct LocalizationSettingsModel
{
    public WarlineCaptureLanguage Language;
}

[Serializable]
public struct WarlineCaptureSettingsModel
{
    public AudioSettingsModel Audio;
    public GraphicsSettingsModel Graphics;
    public ControlsSettingsModel Controls;
    public NotificationSettingsModel Notifications;
    public AccessibilitySettingsModel Accessibility;
    public LocalizationSettingsModel Localization;
}
