internal interface ISettingsControlsView
{
    void Bind(UISettingsModel model);
    UISettingsModel ReadModelFromControls(UISettingsModel model);
    void ApplyVisualPreferences(UISettingsModel model);
}
