using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SettingsPopupContext
{
    Menu = 0,
    Match = 1
}

[DisallowMultipleComponent]
public sealed class SettingsPopupView : MonoBehaviour
{
    private readonly SettingsScreenFlowUiSystemHelper flowSystem = new();

    [SerializeField] private SettingsPopupContext context;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private SettingsPanelView settingsPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button applyButton;

    private UISettingsModel _model;
    private System.Action _closeRequested;

    public SettingsPopupContext Context => context;
    public Button CloseButton => closeButton;
    public Button ResetButton => resetButton;
    public Button ApplyButton => applyButton;
    public SettingsPanelView SettingsPanel => settingsPanel;

    private void Awake()
    {
        ApplyContextTitle();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSettings);
        if (applyButton != null)
            applyButton.onClick.AddListener(SaveSettings);

        LoadSettings();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetSettings);
        if (applyButton != null)
            applyButton.onClick.RemoveListener(SaveSettings);

        _closeRequested = null;
    }

    public void BindClose(System.Action closeRequested)
    {
        _closeRequested = closeRequested;
    }

    public void ConfigureContext(SettingsPopupContext popupContext)
    {
        context = popupContext;
        ApplyContextTitle();
    }

    public void LoadSettings()
    {
        _model = flowSystem.LoadSettings(settingsPanel);
    }

    public void SaveSettings()
    {
        _model = flowSystem.SaveSettings(settingsPanel, _model);
    }

    public void ResetSettings()
    {
        _model = flowSystem.ResetSettings(settingsPanel);
    }

    private void Close()
    {
        _closeRequested?.Invoke();
    }

    private void ApplyContextTitle()
    {
        if (titleText != null)
            titleText.text = context == SettingsPopupContext.Match ? "MATCH SETTINGS" : "COMMAND SETTINGS";
    }
}
