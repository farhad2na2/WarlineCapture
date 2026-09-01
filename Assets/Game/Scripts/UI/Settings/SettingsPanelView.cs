using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SettingsPanelView : MonoBehaviour, ISettingsControlsView
    {
        private static readonly string[] GraphicsQualityLabels = { "LOW", "MEDIUM", "HIGH", "ULTRA" };
        private static readonly string[] FrameRateLabels = { "30 FPS", "60 FPS", "120 FPS" };
        private static readonly string[] AssistanceLevelLabels = { "FULL", "HINTS", "MINIMAL", "OFF" };
        private static readonly string[] NarrationModeLabels = { "OFF", "CRITICAL", "IMPORTANT", "ALL" };
        private static readonly string[] ColorblindModeLabels = { "OFF", "PRO", "DEU", "TRI" };
        private static readonly string[] LanguageLabels = { "EN", "DE", "FR", "ES" };

        [SerializeField] private UISliderRowView masterVolumeRow;
        [SerializeField] private UISliderRowView musicVolumeRow;
        [SerializeField] private UISliderRowView sfxVolumeRow;
        [SerializeField] private UISliderRowView alertsVolumeRow;
        [SerializeField] private UISliderRowView voiceVolumeRow;
        [SerializeField] private UIToggleRowView musicEnabledRow;
        [SerializeField] private UIToggleRowView soundEnabledRow;
        [SerializeField] private UIToggleRowView voiceEnabledRow;
        [SerializeField] private UISegmentedControlView graphicsQualityControl;
        [SerializeField] private UISegmentedControlView frameRateControl;
        [SerializeField] private UISliderRowView cameraSensitivityRow;
        [SerializeField] private UIToggleRowView threatWarningsRow;
        [SerializeField] private UIToggleRowView highContrastRow;
        [SerializeField] private UIToggleRowView largeTextRow;
        [SerializeField] private UISegmentedControlView assistanceLevelControl;
        [SerializeField] private UISegmentedControlView narrationModeControl;
        [SerializeField] private UIToggleRowView assistantTakeoverRow;
        [SerializeField] private UIToggleRowView assistantSubtitlesRow;
        [SerializeField] private UISegmentedControlView colorblindModeControl;
        [SerializeField] private UISegmentedControlView languageControl;
        [SerializeField] private TMP_Dropdown colorblindModeDropdown;
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private UIAccessibilityApplier accessibilityApplier;

        private UISettingsModel _model;

        private void Awake()
        {
            WireEvents();
        }

        private void OnDestroy()
        {
            UnwireEvents();
        }

        public void Bind(UISettingsModel model)
        {
            _model = model;

            masterVolumeRow?.Bind("MASTER VOLUME", model.Audio.MasterVolume, 0f, 100f);
            musicVolumeRow?.Bind("MUSIC VOLUME", model.Audio.MusicVolume, 0f, 100f);
            sfxVolumeRow?.Bind("SOUND VOLUME", model.Audio.SfxVolume, 0f, 100f);
            alertsVolumeRow?.Bind("ALERTS", model.Audio.AlertsVolume, 0f, 100f);
            voiceVolumeRow?.Bind("VOICE", model.Audio.VoiceVolume, 0f, 100f);
            musicEnabledRow?.Bind("MUSIC", "Adjust in-game music volume.", model.Audio.MusicEnabled);
            soundEnabledRow?.Bind("SOUND", "Adjust in-game sound effects volume.", model.Audio.SoundEnabled);
            voiceEnabledRow?.Bind("VOICE", "Adjust in-game voice volume.", model.Audio.VoiceEnabled);
            graphicsQualityControl?.Bind(GraphicsQualityLabels, (int)model.Graphics.Quality);
            frameRateControl?.Bind(FrameRateLabels, (int)model.Graphics.FrameRateMode);
            cameraSensitivityRow?.Bind("CAMERA SENSITIVITY", model.Controls.CameraSensitivity, 0f, 100f);
            threatWarningsRow?.Bind("THREAT WARNINGS", "Show tactical warnings during missions.", model.Notifications.ThreatWarnings);
            highContrastRow?.Bind("High Contrast UI", "Increase panel and text contrast.", model.Accessibility.HighContrastUi);
            largeTextRow?.Bind("Large Text", "Increase UI text scale for readability.", model.Accessibility.LargeText);
            assistanceLevelControl?.Bind(AssistanceLevelLabels, (int)model.Assistant.AssistanceLevel);
            narrationModeControl?.Bind(NarrationModeLabels, (int)model.Assistant.NarrationMode);
            assistantTakeoverRow?.Bind("Assistant Takeover", "Allow assistant-guided bounded actions.", model.Assistant.AllowTakeover);
            assistantSubtitlesRow?.Bind("Assistant Subtitles", "Show narration subtitles in the assistant panel.", model.Assistant.SubtitlesEnabled);
            colorblindModeControl?.Bind(ColorblindModeLabels, (int)model.Accessibility.ColorblindMode);
            languageControl?.Bind(LanguageLabels, (int)model.Localization.Language);
            SetDropdownValue(colorblindModeDropdown, (int)model.Accessibility.ColorblindMode);
            SetDropdownValue(languageDropdown, (int)model.Localization.Language);
        }

        public UISettingsModel ReadModelFromControls(UISettingsModel model)
        {
            model.Audio.MasterVolume = GetSliderValue(masterVolumeRow, model.Audio.MasterVolume);
            model.Audio.MusicVolume = GetSliderValue(musicVolumeRow, model.Audio.MusicVolume);
            model.Audio.SfxVolume = GetSliderValue(sfxVolumeRow, model.Audio.SfxVolume);
            model.Audio.AlertsVolume = GetSliderValue(alertsVolumeRow, model.Audio.AlertsVolume);
            model.Audio.VoiceVolume = GetSliderValue(voiceVolumeRow, model.Audio.VoiceVolume);
            model.Audio.MusicEnabled = GetToggleValue(musicEnabledRow, model.Audio.MusicEnabled);
            model.Audio.SoundEnabled = GetToggleValue(soundEnabledRow, model.Audio.SoundEnabled);
            model.Audio.VoiceEnabled = GetToggleValue(voiceEnabledRow, model.Audio.VoiceEnabled);
            model.Graphics.Quality = _model.Graphics.Quality;
            model.Graphics.FrameRateMode = _model.Graphics.FrameRateMode;
            model.Controls.CameraSensitivity = GetSliderValue(cameraSensitivityRow, model.Controls.CameraSensitivity);
            model.Notifications.ThreatWarnings = GetToggleValue(threatWarningsRow, model.Notifications.ThreatWarnings);
            model.Accessibility.HighContrastUi = GetToggleValue(highContrastRow, model.Accessibility.HighContrastUi);
            model.Accessibility.LargeText = GetToggleValue(largeTextRow, model.Accessibility.LargeText);
            model.Accessibility.ColorblindMode = colorblindModeDropdown != null
                ? (UIColorblindMode)GetDropdownValue(colorblindModeDropdown, (int)_model.Accessibility.ColorblindMode)
                : _model.Accessibility.ColorblindMode;
            model.Localization.Language = languageDropdown != null
                ? (UILanguage)GetDropdownValue(languageDropdown, (int)_model.Localization.Language)
                : _model.Localization.Language;
            model.Assistant.AssistanceLevel = _model.Assistant.AssistanceLevel;
            model.Assistant.NarrationMode = _model.Assistant.NarrationMode;
            model.Assistant.AllowTakeover = GetToggleValue(assistantTakeoverRow, _model.Assistant.AllowTakeover);
            model.Assistant.SubtitlesEnabled = GetToggleValue(assistantSubtitlesRow, _model.Assistant.SubtitlesEnabled);
            return model;
        }

        public void ApplyVisualPreferences(UISettingsModel model)
        {
            accessibilityApplier?.Apply(model);
        }

        private void WireEvents()
        {
            AddSliderListener(masterVolumeRow, OnMasterVolumeChanged);
            AddSliderListener(musicVolumeRow, OnMusicVolumeChanged);
            AddSliderListener(sfxVolumeRow, OnSfxVolumeChanged);
            AddSliderListener(alertsVolumeRow, OnAlertsVolumeChanged);
            AddSliderListener(voiceVolumeRow, OnVoiceVolumeChanged);
            AddToggleListener(musicEnabledRow, OnMusicEnabledChanged);
            AddToggleListener(soundEnabledRow, OnSoundEnabledChanged);
            AddToggleListener(voiceEnabledRow, OnVoiceEnabledChanged);
            AddSliderListener(cameraSensitivityRow, OnCameraSensitivityChanged);
            AddToggleListener(threatWarningsRow, OnThreatWarningsChanged);
            AddToggleListener(highContrastRow, OnHighContrastChanged);
            AddToggleListener(largeTextRow, OnLargeTextChanged);
            AddToggleListener(assistantTakeoverRow, OnAssistantTakeoverChanged);
            AddToggleListener(assistantSubtitlesRow, OnAssistantSubtitlesChanged);
            AddSegmentListeners(graphicsQualityControl, OnGraphicsQualityChanged);
            AddSegmentListeners(frameRateControl, OnFrameRateChanged);
            AddSegmentListeners(assistanceLevelControl, OnAssistanceLevelChanged);
            AddSegmentListeners(narrationModeControl, OnNarrationModeChanged);
            AddSegmentListeners(colorblindModeControl, OnColorblindModeChanged);
            AddSegmentListeners(languageControl, OnLanguageChanged);

            if (colorblindModeDropdown != null)
                colorblindModeDropdown.onValueChanged.AddListener(OnColorblindModeChanged);
            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        private void UnwireEvents()
        {
            RemoveSliderListener(masterVolumeRow, OnMasterVolumeChanged);
            RemoveSliderListener(musicVolumeRow, OnMusicVolumeChanged);
            RemoveSliderListener(sfxVolumeRow, OnSfxVolumeChanged);
            RemoveSliderListener(alertsVolumeRow, OnAlertsVolumeChanged);
            RemoveSliderListener(voiceVolumeRow, OnVoiceVolumeChanged);
            RemoveToggleListener(musicEnabledRow, OnMusicEnabledChanged);
            RemoveToggleListener(soundEnabledRow, OnSoundEnabledChanged);
            RemoveToggleListener(voiceEnabledRow, OnVoiceEnabledChanged);
            RemoveSliderListener(cameraSensitivityRow, OnCameraSensitivityChanged);
            RemoveToggleListener(threatWarningsRow, OnThreatWarningsChanged);
            RemoveToggleListener(highContrastRow, OnHighContrastChanged);
            RemoveToggleListener(largeTextRow, OnLargeTextChanged);
            RemoveToggleListener(assistantTakeoverRow, OnAssistantTakeoverChanged);
            RemoveToggleListener(assistantSubtitlesRow, OnAssistantSubtitlesChanged);

            if (colorblindModeDropdown != null)
                colorblindModeDropdown.onValueChanged.RemoveListener(OnColorblindModeChanged);
            if (languageDropdown != null)
                languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        }

        private void OnMasterVolumeChanged(float value) => _model.Audio.MasterVolume = value;
        private void OnMusicVolumeChanged(float value) => _model.Audio.MusicVolume = value;
        private void OnSfxVolumeChanged(float value) => _model.Audio.SfxVolume = value;
        private void OnAlertsVolumeChanged(float value) => _model.Audio.AlertsVolume = value;
        private void OnVoiceVolumeChanged(float value) => _model.Audio.VoiceVolume = value;
        private void OnMusicEnabledChanged(bool value) => _model.Audio.MusicEnabled = value;
        private void OnSoundEnabledChanged(bool value)
        {
            _model.Audio.SoundEnabled = value;
            if (value)
                UIAudioEventGateway.Raise(UIAudioEventKind.SettingsSoundConfirm);
        }

        private void OnVoiceEnabledChanged(bool value)
        {
            _model.Audio.VoiceEnabled = value;
            if (value)
                UIAudioEventGateway.Raise(UIAudioEventKind.SettingsVoiceSample);
        }
        private void OnCameraSensitivityChanged(float value) => _model.Controls.CameraSensitivity = value;
        private void OnThreatWarningsChanged(bool value) => _model.Notifications.ThreatWarnings = value;
        private void OnHighContrastChanged(bool value) => _model.Accessibility.HighContrastUi = value;
        private void OnLargeTextChanged(bool value) => _model.Accessibility.LargeText = value;
        private void OnAssistantTakeoverChanged(bool value) => _model.Assistant.AllowTakeover = value;
        private void OnAssistantSubtitlesChanged(bool value) => _model.Assistant.SubtitlesEnabled = value;

        private void OnGraphicsQualityChanged(int index)
        {
            _model.Graphics.Quality = (UIGraphicsQuality)index;
            graphicsQualityControl?.Bind(GraphicsQualityLabels, (int)_model.Graphics.Quality);
        }

        private void OnFrameRateChanged(int index)
        {
            _model.Graphics.FrameRateMode = (UIFrameRateMode)index;
            frameRateControl?.Bind(FrameRateLabels, (int)_model.Graphics.FrameRateMode);
        }

        private void OnAssistanceLevelChanged(int index)
        {
            _model.Assistant.AssistanceLevel = (UIAssistanceLevel)index;
            assistanceLevelControl?.Bind(AssistanceLevelLabels, (int)_model.Assistant.AssistanceLevel);
        }

        private void OnNarrationModeChanged(int index)
        {
            _model.Assistant.NarrationMode = (UIAssistantNarrationMode)index;
            narrationModeControl?.Bind(NarrationModeLabels, (int)_model.Assistant.NarrationMode);
        }

        private void OnColorblindModeChanged(int value)
        {
            _model.Accessibility.ColorblindMode = (UIColorblindMode)value;
            colorblindModeControl?.Bind(ColorblindModeLabels, (int)_model.Accessibility.ColorblindMode);
        }

        private void OnLanguageChanged(int value)
        {
            _model.Localization.Language = (UILanguage)value;
            languageControl?.Bind(LanguageLabels, (int)_model.Localization.Language);
        }

        private static void AddSliderListener(UISliderRowView row, UnityEngine.Events.UnityAction<float> action)
        {
            if (row != null && row.Slider != null)
                row.Slider.onValueChanged.AddListener(action);
        }

        private static void RemoveSliderListener(UISliderRowView row, UnityEngine.Events.UnityAction<float> action)
        {
            if (row != null && row.Slider != null)
                row.Slider.onValueChanged.RemoveListener(action);
        }

        private static void AddToggleListener(UIToggleRowView row, UnityEngine.Events.UnityAction<bool> action)
        {
            if (row != null && row.Toggle != null)
                row.Toggle.onValueChanged.AddListener(action);
        }

        private static void RemoveToggleListener(UIToggleRowView row, UnityEngine.Events.UnityAction<bool> action)
        {
            if (row != null && row.Toggle != null)
                row.Toggle.onValueChanged.RemoveListener(action);
        }

        private static void AddSegmentListeners(UISegmentedControlView control, System.Action<int> action)
        {
            if (control?.SegmentButtons == null)
                return;

            for (int i = 0; i < control.SegmentButtons.Length; i++)
            {
                int index = i;
                Button button = control.SegmentButtons[i];
                if (button != null)
                    button.onClick.AddListener(() => action(index));
            }
        }

        private static float GetSliderValue(UISliderRowView row, float fallback)
        {
            return row != null && row.Slider != null ? row.Slider.value : fallback;
        }

        private static bool GetToggleValue(UIToggleRowView row, bool fallback)
        {
            return row != null && row.Toggle != null ? row.Toggle.isOn : fallback;
        }

        private static int GetDropdownValue(TMP_Dropdown dropdown, int fallback)
        {
            return dropdown != null ? dropdown.value : fallback;
        }

        private static void SetDropdownValue(TMP_Dropdown dropdown, int value)
        {
            if (dropdown != null)
                dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, Mathf.Max(0, dropdown.options.Count - 1)));
        }
    }
}
