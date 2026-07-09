using UnityEngine;

namespace Game.UI.Runtime
{
    public static class SettingsService
    {
        private const string Prefix = "Game.Settings.";
        private const string MasterVolumeKey = Prefix + "Audio.MasterVolume";
        private const string MusicVolumeKey = Prefix + "Audio.MusicVolume";
        private const string SfxVolumeKey = Prefix + "Audio.SfxVolume";
        private const string AlertsVolumeKey = Prefix + "Audio.AlertsVolume";
        private const string VoiceVolumeKey = Prefix + "Audio.VoiceVolume";
        private const string MusicEnabledKey = Prefix + "Audio.MusicEnabled";
        private const string SoundEnabledKey = Prefix + "Audio.SoundEnabled";
        private const string VoiceEnabledKey = Prefix + "Audio.VoiceEnabled";
        private const string GraphicsQualityKey = Prefix + "Graphics.Quality";
        private const string FrameRateModeKey = Prefix + "Graphics.FrameRateMode";
        private const string CameraSensitivityKey = Prefix + "Controls.CameraSensitivity";
        private const string ThreatWarningsKey = Prefix + "Notifications.ThreatWarnings";
        private const string HighContrastKey = Prefix + "Accessibility.HighContrastUi";
        private const string LargeTextKey = Prefix + "Accessibility.LargeText";
        private const string ColorblindModeKey = Prefix + "Accessibility.ColorblindMode";
        private const string LanguageKey = Prefix + "Localization.Language";
        private const string AssistanceLevelKey = Prefix + "Assistant.AssistanceLevel";
        private const string AssistantNarrationModeKey = Prefix + "Assistant.NarrationMode";
        private const string AssistantAllowTakeoverKey = Prefix + "Assistant.AllowTakeover";
        private const string AssistantSubtitlesEnabledKey = Prefix + "Assistant.SubtitlesEnabled";

        public static event System.Action<UISettingsModel> RuntimeApplied;

        public static UISettingsModel Defaults => new()
        {
            Audio = new AudioSettingsModel
            {
                MasterVolume = 80f,
                MusicVolume = 60f,
                SfxVolume = 85f,
                AlertsVolume = 90f,
                VoiceVolume = 85f,
                MusicEnabled = true,
                SoundEnabled = true,
                VoiceEnabled = true
            },
            Graphics = new GraphicsSettingsModel
            {
                Quality = UIGraphicsQuality.High,
                FrameRateMode = UIFrameRateMode.OneTwenty
            },
            Controls = new ControlsSettingsModel
            {
                CameraSensitivity = 55f
            },
            Notifications = new NotificationSettingsModel
            {
                ThreatWarnings = true
            },
            Accessibility = new AccessibilitySettingsModel
            {
                HighContrastUi = false,
                LargeText = false,
                ColorblindMode = UIColorblindMode.Off
            },
            Localization = new LocalizationSettingsModel
            {
                Language = UILanguage.English
            },
            Assistant = new AssistantSettingsModel
            {
                AssistanceLevel = UIAssistanceLevel.FullGuidance,
                NarrationMode = UIAssistantNarrationMode.Important,
                AllowTakeover = true,
                SubtitlesEnabled = true
            }
        };

        public static UISettingsModel Load()
        {
            UISettingsModel defaults = Defaults;
            return new UISettingsModel
            {
                Audio = new AudioSettingsModel
                {
                    MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaults.Audio.MasterVolume),
                    MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaults.Audio.MusicVolume),
                    SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaults.Audio.SfxVolume),
                    AlertsVolume = PlayerPrefs.GetFloat(AlertsVolumeKey, defaults.Audio.AlertsVolume),
                    VoiceVolume = PlayerPrefs.GetFloat(VoiceVolumeKey, defaults.Audio.VoiceVolume),
                    MusicEnabled = GetBool(MusicEnabledKey, defaults.Audio.MusicEnabled),
                    SoundEnabled = GetBool(SoundEnabledKey, defaults.Audio.SoundEnabled),
                    VoiceEnabled = GetBool(VoiceEnabledKey, defaults.Audio.VoiceEnabled)
                },
                Graphics = new GraphicsSettingsModel
                {
                    Quality = GetEnum(GraphicsQualityKey, defaults.Graphics.Quality),
                    FrameRateMode = GetEnum(FrameRateModeKey, defaults.Graphics.FrameRateMode)
                },
                Controls = new ControlsSettingsModel
                {
                    CameraSensitivity = PlayerPrefs.GetFloat(CameraSensitivityKey, defaults.Controls.CameraSensitivity)
                },
                Notifications = new NotificationSettingsModel
                {
                    ThreatWarnings = PlayerPrefs.GetInt(ThreatWarningsKey, defaults.Notifications.ThreatWarnings ? 1 : 0) == 1
                },
                Accessibility = new AccessibilitySettingsModel
                {
                    HighContrastUi = PlayerPrefs.GetInt(HighContrastKey, defaults.Accessibility.HighContrastUi ? 1 : 0) == 1,
                    LargeText = PlayerPrefs.GetInt(LargeTextKey, defaults.Accessibility.LargeText ? 1 : 0) == 1,
                    ColorblindMode = GetEnum(ColorblindModeKey, defaults.Accessibility.ColorblindMode)
                },
                Localization = new LocalizationSettingsModel
                {
                    Language = GetEnum(LanguageKey, defaults.Localization.Language)
                },
                Assistant = new AssistantSettingsModel
                {
                    AssistanceLevel = GetEnum(AssistanceLevelKey, defaults.Assistant.AssistanceLevel),
                    NarrationMode = GetEnum(AssistantNarrationModeKey, defaults.Assistant.NarrationMode),
                    AllowTakeover = PlayerPrefs.GetInt(AssistantAllowTakeoverKey, defaults.Assistant.AllowTakeover ? 1 : 0) == 1,
                    SubtitlesEnabled = PlayerPrefs.GetInt(AssistantSubtitlesEnabledKey, defaults.Assistant.SubtitlesEnabled ? 1 : 0) == 1
                }
            };
        }

        public static void Save(UISettingsModel model)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp(model.Audio.MasterVolume, 0f, 100f));
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp(model.Audio.MusicVolume, 0f, 100f));
            PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp(model.Audio.SfxVolume, 0f, 100f));
            PlayerPrefs.SetFloat(AlertsVolumeKey, Mathf.Clamp(model.Audio.AlertsVolume, 0f, 100f));
            PlayerPrefs.SetFloat(VoiceVolumeKey, Mathf.Clamp(model.Audio.VoiceVolume, 0f, 100f));
            PlayerPrefs.SetInt(MusicEnabledKey, model.Audio.MusicEnabled ? 1 : 0);
            PlayerPrefs.SetInt(SoundEnabledKey, model.Audio.SoundEnabled ? 1 : 0);
            PlayerPrefs.SetInt(VoiceEnabledKey, model.Audio.VoiceEnabled ? 1 : 0);
            PlayerPrefs.SetInt(GraphicsQualityKey, (int)model.Graphics.Quality);
            PlayerPrefs.SetInt(FrameRateModeKey, (int)model.Graphics.FrameRateMode);
            PlayerPrefs.SetFloat(CameraSensitivityKey, Mathf.Clamp(model.Controls.CameraSensitivity, 0f, 100f));
            PlayerPrefs.SetInt(ThreatWarningsKey, model.Notifications.ThreatWarnings ? 1 : 0);
            PlayerPrefs.SetInt(HighContrastKey, model.Accessibility.HighContrastUi ? 1 : 0);
            PlayerPrefs.SetInt(LargeTextKey, model.Accessibility.LargeText ? 1 : 0);
            PlayerPrefs.SetInt(ColorblindModeKey, (int)model.Accessibility.ColorblindMode);
            PlayerPrefs.SetInt(LanguageKey, (int)model.Localization.Language);
            PlayerPrefs.SetInt(AssistanceLevelKey, (int)model.Assistant.AssistanceLevel);
            PlayerPrefs.SetInt(AssistantNarrationModeKey, (int)model.Assistant.NarrationMode);
            PlayerPrefs.SetInt(AssistantAllowTakeoverKey, model.Assistant.AllowTakeover ? 1 : 0);
            PlayerPrefs.SetInt(AssistantSubtitlesEnabledKey, model.Assistant.SubtitlesEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static UISettingsModel ResetToDefaults()
        {
            UISettingsModel defaults = Defaults;
            Save(defaults);
            return defaults;
        }

        public static void ApplyRuntime(UISettingsModel model)
        {
            AudioListener.volume = Mathf.Clamp01(model.Audio.MasterVolume / 100f);
            Application.targetFrameRate = model.Graphics.FrameRateMode switch
            {
                UIFrameRateMode.Thirty => 30,
                UIFrameRateMode.Sixty => 60,
                UIFrameRateMode.OneTwenty => 120,
                _ => -1
            };

            int qualityIndex = ResolveUnityQualityIndex(model.Graphics.Quality);
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(qualityIndex, true);

            RuntimeApplied?.Invoke(model);
        }

        private static int ResolveUnityQualityIndex(UIGraphicsQuality quality)
        {
            if (QualitySettings.names.Length == 0)
                return 0;

            string qualityName = quality switch
            {
                UIGraphicsQuality.Low => "Low",
                UIGraphicsQuality.Balanced => "Mobile",
                UIGraphicsQuality.High => "Mobile",
                UIGraphicsQuality.Ultra => "Ultra",
                _ => "Mobile"
            };

            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                if (string.Equals(QualitySettings.names[i], qualityName, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return Mathf.Clamp(1, 0, QualitySettings.names.Length - 1);
        }

        private static T GetEnum<T>(string key, T fallback) where T : struct
        {
            int fallbackValue = System.Convert.ToInt32(fallback);
            int storedValue = PlayerPrefs.GetInt(key, fallbackValue);
            return System.Enum.IsDefined(typeof(T), storedValue) ? (T)System.Enum.ToObject(typeof(T), storedValue) : fallback;
        }

        private static bool GetBool(string key, bool fallback)
        {
            return PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;
        }
    }
}
