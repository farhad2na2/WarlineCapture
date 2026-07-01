using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SettingsPanelView : MonoBehaviour, ISettingsControlsView
{
    private static readonly string[] GraphicsQualityLabels = { "LOW", "MEDIUM", "HIGH", "ULTRA" };
    private static readonly string[] FrameRateLabels = { "30 FPS", "60 FPS", "120 FPS" };
    private static readonly string[] AssistanceLevelLabels = { "FULL", "HINTS", "MINIMAL", "OFF" };
    private static readonly string[] ColorblindModeLabels = { "OFF", "PRO", "DEU", "TRI" };
    private static readonly string[] LanguageLabels = { "EN", "DE", "FR", "ES" };

    [SerializeField] private UISliderRowView masterVolumeRow;
    [SerializeField] private UISliderRowView musicVolumeRow;
    [SerializeField] private UISliderRowView sfxVolumeRow;
    [SerializeField] private UISegmentedControlView graphicsQualityControl;
    [SerializeField] private UISegmentedControlView frameRateControl;
    [SerializeField] private UISliderRowView cameraSensitivityRow;
    [SerializeField] private UIToggleRowView threatWarningsRow;
    [SerializeField] private UIToggleRowView highContrastRow;
    [SerializeField] private UIToggleRowView largeTextRow;
    [SerializeField] private UISegmentedControlView assistanceLevelControl;
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

        masterVolumeRow?.Bind("Master Volume", model.Audio.MasterVolume, 0f, 100f);
        musicVolumeRow?.Bind("Music", model.Audio.MusicVolume, 0f, 100f);
        sfxVolumeRow?.Bind("SFX", model.Audio.SfxVolume, 0f, 100f);
        graphicsQualityControl?.Bind(GraphicsQualityLabels, (int)model.Graphics.Quality);
        frameRateControl?.Bind(FrameRateLabels, (int)model.Graphics.FrameRateMode);
        cameraSensitivityRow?.Bind("CAMERA SENSITIVITY", model.Controls.CameraSensitivity, 0f, 100f);
        threatWarningsRow?.Bind("THREAT WARNINGS", "Show tactical warnings during missions.", model.Notifications.ThreatWarnings);
        highContrastRow?.Bind("High Contrast UI", "Increase panel and text contrast.", model.Accessibility.HighContrastUi);
        largeTextRow?.Bind("Large Text", "Increase UI text scale for readability.", model.Accessibility.LargeText);
        assistanceLevelControl?.Bind(AssistanceLevelLabels, (int)model.Assistant.AssistanceLevel);
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
        AddSliderListener(cameraSensitivityRow, OnCameraSensitivityChanged);
        AddToggleListener(threatWarningsRow, OnThreatWarningsChanged);
        AddToggleListener(highContrastRow, OnHighContrastChanged);
        AddToggleListener(largeTextRow, OnLargeTextChanged);
        AddSegmentListeners(graphicsQualityControl, OnGraphicsQualityChanged);
        AddSegmentListeners(frameRateControl, OnFrameRateChanged);
        AddSegmentListeners(assistanceLevelControl, OnAssistanceLevelChanged);
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
        RemoveSliderListener(cameraSensitivityRow, OnCameraSensitivityChanged);
        RemoveToggleListener(threatWarningsRow, OnThreatWarningsChanged);
        RemoveToggleListener(highContrastRow, OnHighContrastChanged);
        RemoveToggleListener(largeTextRow, OnLargeTextChanged);

        if (colorblindModeDropdown != null)
            colorblindModeDropdown.onValueChanged.RemoveListener(OnColorblindModeChanged);
        if (languageDropdown != null)
            languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
    }

    private void OnMasterVolumeChanged(float value) => _model.Audio.MasterVolume = value;
    private void OnMusicVolumeChanged(float value) => _model.Audio.MusicVolume = value;
    private void OnSfxVolumeChanged(float value) => _model.Audio.SfxVolume = value;
    private void OnCameraSensitivityChanged(float value) => _model.Controls.CameraSensitivity = value;
    private void OnThreatWarningsChanged(bool value) => _model.Notifications.ThreatWarnings = value;
    private void OnHighContrastChanged(bool value) => _model.Accessibility.HighContrastUi = value;
    private void OnLargeTextChanged(bool value) => _model.Accessibility.LargeText = value;

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
