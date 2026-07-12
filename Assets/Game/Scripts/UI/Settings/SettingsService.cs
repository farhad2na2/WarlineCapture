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
        private const string MusicSoundtrackVersionKey = Prefix + "Audio.MusicSoundtrackVersion";
        private const int CurrentMusicSoundtrackVersion = 1;
        private const string SoundEnabledKey = Prefix + "Audio.SoundEnabled";
        private const string VoiceEnabledKey = Prefix + "Audio.VoiceEnabled";
        private const string GraphicsQualityKey = Prefix + "Graphics.Quality";
        private const string FrameRateModeKey = Prefix + "Graphics.FrameRateMode";
        private const string CameraSensitivityKey = Prefix + "Controls.CameraSensitivity";
        private const string ThreatWarningsKey = Prefix + "Notifications.ThreatWarnings";
        private const string HighContrastKey = Prefix + "Accessibility.HighContrastUi";
        private const string LargeTextKey = Prefix + "Accessibility.LargeText";
        private const string ColorblindModeKey = Prefix + "Accessibility.ColorblindMode";
        private const string ReducedMotionKey = Prefix + "Accessibility.ReducedMotion";
        private const string LegacyReducedMotionKey = "Game.ReducedMotion";
        private const string LanguageKey = Prefix + "Localization.Language";
        private const string AssistanceLevelKey = Prefix + "Assistant.AssistanceLevel";
        private const string AssistantNarrationModeKey = Prefix + "Assistant.NarrationMode";
        private const string AssistantAllowTakeoverKey = Prefix + "Assistant.AllowTakeover";
        private const string AssistantSubtitlesEnabledKey = Prefix + "Assistant.SubtitlesEnabled";
        private const string NarrativeSubtitlesEnabledKey = Prefix + "Narrative.SubtitlesEnabled";
        private const string NarrativeSubtitleSizeKey = Prefix + "Narrative.SubtitleSize";
        private const string NarrativeBackgroundOpacityKey = Prefix + "Narrative.BackgroundOpacity";
        private const string NarrativeInstantTextKey = Prefix + "Narrative.InstantText";
        private const string NarrativeAutoAdvanceKey = Prefix + "Narrative.AutoAdvance";

        public static event System.Action<UISettingsModel> RuntimeApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeAppliedSubscribers()
        {
            RuntimeApplied = null;
        }

        public static UISettingsModel Defaults => DefaultsForPlatform(IsAndroidRuntime);

        internal static UISettingsModel DefaultsForPlatform(bool isAndroid)
        {
            return new UISettingsModel
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
                    FrameRateMode = isAndroid ? UIFrameRateMode.Sixty : UIFrameRateMode.OneTwenty
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
                    ColorblindMode = UIColorblindMode.Off,
                    ReducedMotion = false
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
                },
                Narrative = new NarrativeSettingsModel
                {
                    SubtitlesEnabled = true,
                    SubtitleSize = UISubtitleSize.Standard,
                    BackgroundOpacity = UISubtitleBackgroundOpacity.SeventyFivePercent,
                    InstantText = false,
                    AutoAdvance = true
                }
            };
        }

        public static UISettingsModel Load()
        {
            return LoadForPlatform(IsAndroidRuntime);
        }

        internal static UISettingsModel LoadForPlatform(bool isAndroid)
        {
            UISettingsModel defaults = DefaultsForPlatform(isAndroid);
            return new UISettingsModel
            {
                Audio = new AudioSettingsModel
                {
                    MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaults.Audio.MasterVolume),
                    MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaults.Audio.MusicVolume),
                    SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaults.Audio.SfxVolume),
                    AlertsVolume = PlayerPrefs.GetFloat(AlertsVolumeKey, defaults.Audio.AlertsVolume),
                    VoiceVolume = PlayerPrefs.GetFloat(VoiceVolumeKey, defaults.Audio.VoiceVolume),
                    MusicEnabled = LoadMusicEnabled(defaults.Audio.MusicEnabled),
                    SoundEnabled = GetBool(SoundEnabledKey, defaults.Audio.SoundEnabled),
                    VoiceEnabled = GetBool(VoiceEnabledKey, defaults.Audio.VoiceEnabled)
                },
                Graphics = new GraphicsSettingsModel
                {
                    Quality = GetEnum(GraphicsQualityKey, defaults.Graphics.Quality),
                    FrameRateMode = NormalizeFrameRateMode(
                        GetEnum(FrameRateModeKey, defaults.Graphics.FrameRateMode),
                        isAndroid)
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
                    ColorblindMode = GetEnum(ColorblindModeKey, defaults.Accessibility.ColorblindMode),
                    ReducedMotion = LoadReducedMotionPreference(defaults.Accessibility.ReducedMotion)
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
                },
                Narrative = new NarrativeSettingsModel
                {
                    SubtitlesEnabled = GetBool(NarrativeSubtitlesEnabledKey, defaults.Narrative.SubtitlesEnabled),
                    SubtitleSize = GetEnum(NarrativeSubtitleSizeKey, defaults.Narrative.SubtitleSize),
                    BackgroundOpacity = GetEnum(NarrativeBackgroundOpacityKey, defaults.Narrative.BackgroundOpacity),
                    InstantText = GetBool(NarrativeInstantTextKey, defaults.Narrative.InstantText),
                    AutoAdvance = GetBool(NarrativeAutoAdvanceKey, defaults.Narrative.AutoAdvance)
                }
            };
        }

        public static void Save(UISettingsModel model)
        {
            SaveForPlatform(model, IsAndroidRuntime);
        }

        internal static void SaveForPlatform(UISettingsModel model, bool isAndroid)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp(model.Audio.MasterVolume, 0f, 100f));
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp(model.Audio.MusicVolume, 0f, 100f));
            PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp(model.Audio.SfxVolume, 0f, 100f));
            PlayerPrefs.SetFloat(AlertsVolumeKey, Mathf.Clamp(model.Audio.AlertsVolume, 0f, 100f));
            PlayerPrefs.SetFloat(VoiceVolumeKey, Mathf.Clamp(model.Audio.VoiceVolume, 0f, 100f));
            PlayerPrefs.SetInt(MusicEnabledKey, model.Audio.MusicEnabled ? 1 : 0);
            PlayerPrefs.SetInt(MusicSoundtrackVersionKey, CurrentMusicSoundtrackVersion);
            PlayerPrefs.SetInt(SoundEnabledKey, model.Audio.SoundEnabled ? 1 : 0);
            PlayerPrefs.SetInt(VoiceEnabledKey, model.Audio.VoiceEnabled ? 1 : 0);
            PlayerPrefs.SetInt(GraphicsQualityKey, (int)model.Graphics.Quality);
            PlayerPrefs.SetInt(
                FrameRateModeKey,
                (int)NormalizeFrameRateMode(model.Graphics.FrameRateMode, isAndroid));
            PlayerPrefs.SetFloat(CameraSensitivityKey, Mathf.Clamp(model.Controls.CameraSensitivity, 0f, 100f));
            PlayerPrefs.SetInt(ThreatWarningsKey, model.Notifications.ThreatWarnings ? 1 : 0);
            PlayerPrefs.SetInt(HighContrastKey, model.Accessibility.HighContrastUi ? 1 : 0);
            PlayerPrefs.SetInt(LargeTextKey, model.Accessibility.LargeText ? 1 : 0);
            PlayerPrefs.SetInt(ColorblindModeKey, (int)model.Accessibility.ColorblindMode);
            int reducedMotion = model.Accessibility.ReducedMotion ? 1 : 0;
            PlayerPrefs.SetInt(ReducedMotionKey, reducedMotion);
            PlayerPrefs.SetInt(LegacyReducedMotionKey, reducedMotion);
            PlayerPrefs.SetInt(LanguageKey, (int)model.Localization.Language);
            PlayerPrefs.SetInt(AssistanceLevelKey, (int)model.Assistant.AssistanceLevel);
            PlayerPrefs.SetInt(AssistantNarrationModeKey, (int)model.Assistant.NarrationMode);
            PlayerPrefs.SetInt(AssistantAllowTakeoverKey, model.Assistant.AllowTakeover ? 1 : 0);
            PlayerPrefs.SetInt(AssistantSubtitlesEnabledKey, model.Assistant.SubtitlesEnabled ? 1 : 0);
            PlayerPrefs.SetInt(NarrativeSubtitlesEnabledKey, model.Narrative.SubtitlesEnabled ? 1 : 0);
            PlayerPrefs.SetInt(NarrativeSubtitleSizeKey, (int)model.Narrative.SubtitleSize);
            PlayerPrefs.SetInt(NarrativeBackgroundOpacityKey, (int)model.Narrative.BackgroundOpacity);
            PlayerPrefs.SetInt(NarrativeInstantTextKey, model.Narrative.InstantText ? 1 : 0);
            PlayerPrefs.SetInt(NarrativeAutoAdvanceKey, model.Narrative.AutoAdvance ? 1 : 0);
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
            ApplyRuntimeForPlatform(model, IsAndroidRuntime);
        }

        internal static void ApplyRuntimeForPlatform(UISettingsModel model, bool isAndroid)
        {
            model.Graphics.FrameRateMode = NormalizeFrameRateMode(model.Graphics.FrameRateMode, isAndroid);
            AudioListener.volume = Mathf.Clamp01(model.Audio.MasterVolume / 100f);
            Application.targetFrameRate = ResolveTargetFrameRate(model.Graphics.FrameRateMode, isAndroid);

            int qualityIndex = ResolveUnityQualityIndex(model.Graphics.Quality);
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(qualityIndex, true);

            PublishRuntimeSettings(model);
        }

        internal static void PublishRuntimeSettings(UISettingsModel model)
        {
            RuntimeApplied?.Invoke(model);
        }

        internal static UIFrameRateMode NormalizeFrameRateMode(UIFrameRateMode mode, bool isAndroid)
        {
            return isAndroid && mode == UIFrameRateMode.OneTwenty
                ? UIFrameRateMode.Sixty
                : mode;
        }

        internal static int ResolveTargetFrameRate(UIFrameRateMode mode, bool isAndroid)
        {
            return NormalizeFrameRateMode(mode, isAndroid) switch
            {
                UIFrameRateMode.Thirty => 30,
                UIFrameRateMode.Sixty => 60,
                UIFrameRateMode.OneTwenty => 120,
                _ => -1
            };
        }

        private static bool IsAndroidRuntime => Application.platform == RuntimePlatform.Android;

        internal static bool LoadReducedMotionPreference()
        {
            return LoadReducedMotionPreference(Defaults.Accessibility.ReducedMotion);
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

        private static bool LoadMusicEnabled(bool fallback)
        {
            int appliedSoundtrackVersion = PlayerPrefs.GetInt(MusicSoundtrackVersionKey, 0);
            if (appliedSoundtrackVersion >= CurrentMusicSoundtrackVersion)
                return GetBool(MusicEnabledKey, fallback);

            PlayerPrefs.SetInt(MusicEnabledKey, 1);
            PlayerPrefs.SetInt(MusicSoundtrackVersionKey, CurrentMusicSoundtrackVersion);
            PlayerPrefs.Save();
            return true;
        }

        private static bool LoadReducedMotionPreference(bool fallback)
        {
            if (PlayerPrefs.HasKey(ReducedMotionKey))
                return GetBool(ReducedMotionKey, fallback);

            if (!PlayerPrefs.HasKey(LegacyReducedMotionKey))
                return fallback;

            bool reducedMotion = GetBool(LegacyReducedMotionKey, fallback);
            PlayerPrefs.SetInt(ReducedMotionKey, reducedMotion ? 1 : 0);
            PlayerPrefs.Save();
            return reducedMotion;
        }
    }
}
