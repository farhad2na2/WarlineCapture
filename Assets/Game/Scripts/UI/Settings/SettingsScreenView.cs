using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsScreenView : UIScreenView
{
    private readonly SettingsScreenFlowUiSystemHelper flowSystem = new();

    [SerializeField] private UISliderRowView masterVolumeRow;
    [SerializeField] private UISliderRowView musicVolumeRow;
    [SerializeField] private UISliderRowView sfxVolumeRow;
    [SerializeField] private UISegmentedControlView graphicsQualityControl;
    [SerializeField] private UISegmentedControlView frameRateControl;
    [SerializeField] private UISliderRowView cameraSensitivityRow;
    [SerializeField] private UIToggleRowView threatWarningsRow;
    [SerializeField] private UIToggleRowView highContrastRow;
    [SerializeField] private UIToggleRowView largeTextRow;
    [SerializeField] private TMP_Dropdown colorblindModeDropdown;
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private UIAccessibilityApplier accessibilityApplier;

    private UISettingsModel _model;
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
        _model = flowSystem.LoadSettings(this);
        _hasLoaded = true;
    }

    public void Bind(UISettingsModel model)
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
        _model = flowSystem.SaveSettings(this, _model);
    }

    public void ResetSettings()
    {
        _model = flowSystem.ResetSettings(this);
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

    internal UISettingsModel ReadModelFromControls(UISettingsModel model)
    {
        model.Audio.MasterVolume = GetSliderValue(masterVolumeRow, model.Audio.MasterVolume);
        model.Audio.MusicVolume = GetSliderValue(musicVolumeRow, model.Audio.MusicVolume);
        model.Audio.SfxVolume = GetSliderValue(sfxVolumeRow, model.Audio.SfxVolume);
        model.Controls.CameraSensitivity = GetSliderValue(cameraSensitivityRow, model.Controls.CameraSensitivity);
        model.Notifications.ThreatWarnings = GetToggleValue(threatWarningsRow, model.Notifications.ThreatWarnings);
        model.Accessibility.HighContrastUi = GetToggleValue(highContrastRow, model.Accessibility.HighContrastUi);
        model.Accessibility.LargeText = GetToggleValue(largeTextRow, model.Accessibility.LargeText);
        model.Accessibility.ColorblindMode = (UIColorblindMode)GetDropdownValue(colorblindModeDropdown, (int)model.Accessibility.ColorblindMode);
        model.Localization.Language = (UILanguage)GetDropdownValue(languageDropdown, (int)model.Localization.Language);
        return model;
    }

    internal void ApplyVisualPreferences(UISettingsModel model)
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
    private void OnGraphicsQualityChanged(int index) => _model.Graphics.Quality = (UIGraphicsQuality)index;
    private void OnFrameRateChanged(int index) => _model.Graphics.FrameRateMode = (UIFrameRateMode)index;
    private void OnColorblindModeChanged(int value) => _model.Accessibility.ColorblindMode = (UIColorblindMode)value;
    private void OnLanguageChanged(int value) => _model.Localization.Language = (UILanguage)value;

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
