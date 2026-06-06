using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsScreenSystem : WarlineCaptureScreenSystem
{
    [SerializeField] private WarlineCaptureSliderRowView masterVolumeRow;
    [SerializeField] private WarlineCaptureSliderRowView musicVolumeRow;
    [SerializeField] private WarlineCaptureSliderRowView sfxVolumeRow;
    [SerializeField] private WarlineCaptureSegmentedControlView graphicsQualityControl;
    [SerializeField] private WarlineCaptureSegmentedControlView frameRateControl;
    [SerializeField] private WarlineCaptureSliderRowView cameraSensitivityRow;
    [SerializeField] private WarlineCaptureToggleRowView threatWarningsRow;
    [SerializeField] private WarlineCaptureToggleRowView highContrastRow;
    [SerializeField] private WarlineCaptureToggleRowView largeTextRow;
    [SerializeField] private TMP_Dropdown colorblindModeDropdown;
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private WarlineCaptureUiAccessibilityApplier accessibilityApplier;

    private WarlineCaptureSettingsModel _model;
    private bool _hasLoaded;

    private void Awake()
    {
        WireEvents();
        LoadSettings();
    }

    private void OnDestroy()
    {
        UnwireEvents();
    }

    private void OnDisable()
    {
        if (_hasLoaded)
            SaveSettings();
    }

    public void LoadSettings()
    {
        _model = SettingsService.Load();
        _hasLoaded = true;
        Bind(_model);
        ApplyVisualPreferences(_model);
    }

    public void Bind(WarlineCaptureSettingsModel model)
    {
        _model = model;

        masterVolumeRow?.Bind("Master Volume", model.Audio.MasterVolume, 0f, 100f);
        musicVolumeRow?.Bind("Music", model.Audio.MusicVolume, 0f, 100f);
        sfxVolumeRow?.Bind("SFX", model.Audio.SfxVolume, 0f, 100f);
        graphicsQualityControl?.Bind(new[] { "LOW", "MEDIUM", "HIGH", "ULTRA" }, (int)model.Graphics.Quality);
        frameRateControl?.Bind(new[] { "30 FPS", "60 FPS", "120 FPS" }, (int)model.Graphics.FrameRateMode);
        cameraSensitivityRow?.Bind("CAMERA SENSITIVITY", model.Controls.CameraSensitivity, 0f, 100f);
        threatWarningsRow?.Bind("THREAT WARNINGS", "Show tactical warnings during missions.", model.Notifications.ThreatWarnings);
        highContrastRow?.Bind("High Contrast UI", "Increase panel and text contrast.", model.Accessibility.HighContrastUi);
        largeTextRow?.Bind("Large Text", "Increase UI text scale for readability.", model.Accessibility.LargeText);
        SetDropdownValue(colorblindModeDropdown, (int)model.Accessibility.ColorblindMode);
        SetDropdownValue(languageDropdown, (int)model.Localization.Language);
    }

    public void SaveSettings()
    {
        ReadControlsIntoModel();
        SettingsService.Save(_model);
        SettingsService.ApplyRuntime(_model);
        ApplyVisualPreferences(_model);
    }

    public void ResetSettings()
    {
        _model = SettingsService.ResetToDefaults();
        Bind(_model);
        SettingsService.ApplyRuntime(_model);
        ApplyVisualPreferences(_model);
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

        if (colorblindModeDropdown != null)
            colorblindModeDropdown.onValueChanged.AddListener(OnColorblindModeChanged);
        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSettings);
        if (applyButton != null)
            applyButton.onClick.AddListener(SaveSettings);
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
        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetSettings);
        if (applyButton != null)
            applyButton.onClick.RemoveListener(SaveSettings);
    }

    private void ReadControlsIntoModel()
    {
        _model.Audio.MasterVolume = GetSliderValue(masterVolumeRow, _model.Audio.MasterVolume);
        _model.Audio.MusicVolume = GetSliderValue(musicVolumeRow, _model.Audio.MusicVolume);
        _model.Audio.SfxVolume = GetSliderValue(sfxVolumeRow, _model.Audio.SfxVolume);
        _model.Controls.CameraSensitivity = GetSliderValue(cameraSensitivityRow, _model.Controls.CameraSensitivity);
        _model.Notifications.ThreatWarnings = GetToggleValue(threatWarningsRow, _model.Notifications.ThreatWarnings);
        _model.Accessibility.HighContrastUi = GetToggleValue(highContrastRow, _model.Accessibility.HighContrastUi);
        _model.Accessibility.LargeText = GetToggleValue(largeTextRow, _model.Accessibility.LargeText);
        _model.Accessibility.ColorblindMode = (WarlineCaptureColorblindMode)GetDropdownValue(colorblindModeDropdown, (int)_model.Accessibility.ColorblindMode);
        _model.Localization.Language = (WarlineCaptureLanguage)GetDropdownValue(languageDropdown, (int)_model.Localization.Language);
    }

    private void ApplyVisualPreferences(WarlineCaptureSettingsModel model)
    {
        accessibilityApplier?.Apply(model);
    }

    private void OnMasterVolumeChanged(float value) => _model.Audio.MasterVolume = value;
    private void OnMusicVolumeChanged(float value) => _model.Audio.MusicVolume = value;
    private void OnSfxVolumeChanged(float value) => _model.Audio.SfxVolume = value;
    private void OnCameraSensitivityChanged(float value) => _model.Controls.CameraSensitivity = value;
    private void OnThreatWarningsChanged(bool value) => _model.Notifications.ThreatWarnings = value;
    private void OnHighContrastChanged(bool value) => _model.Accessibility.HighContrastUi = value;
    private void OnLargeTextChanged(bool value) => _model.Accessibility.LargeText = value;
    private void OnGraphicsQualityChanged(int index) => _model.Graphics.Quality = (WarlineCaptureGraphicsQuality)index;
    private void OnFrameRateChanged(int index) => _model.Graphics.FrameRateMode = (WarlineCaptureFrameRateMode)index;
    private void OnColorblindModeChanged(int value) => _model.Accessibility.ColorblindMode = (WarlineCaptureColorblindMode)value;
    private void OnLanguageChanged(int value) => _model.Localization.Language = (WarlineCaptureLanguage)value;

    private static void AddSliderListener(WarlineCaptureSliderRowView row, UnityEngine.Events.UnityAction<float> action)
    {
        if (row != null && row.Slider != null)
            row.Slider.onValueChanged.AddListener(action);
    }

    private static void RemoveSliderListener(WarlineCaptureSliderRowView row, UnityEngine.Events.UnityAction<float> action)
    {
        if (row != null && row.Slider != null)
            row.Slider.onValueChanged.RemoveListener(action);
    }

    private static void AddToggleListener(WarlineCaptureToggleRowView row, UnityEngine.Events.UnityAction<bool> action)
    {
        if (row != null && row.Toggle != null)
            row.Toggle.onValueChanged.AddListener(action);
    }

    private static void RemoveToggleListener(WarlineCaptureToggleRowView row, UnityEngine.Events.UnityAction<bool> action)
    {
        if (row != null && row.Toggle != null)
            row.Toggle.onValueChanged.RemoveListener(action);
    }

    private static void AddSegmentListeners(WarlineCaptureSegmentedControlView control, System.Action<int> action)
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

    private static float GetSliderValue(WarlineCaptureSliderRowView row, float fallback)
    {
        return row != null && row.Slider != null ? row.Slider.value : fallback;
    }

    private static bool GetToggleValue(WarlineCaptureToggleRowView row, bool fallback)
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
