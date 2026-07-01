internal sealed class SettingsScreenFlowUiSystemHelper
{
    public UISettingsModel LoadSettings(ISettingsControlsView view)
    {
        UISettingsModel model = SettingsService.Load();
        if (view != null)
        {
            view.Bind(model);
            view.ApplyVisualPreferences(model);
        }

        return model;
    }

    public UISettingsModel SaveSettings(ISettingsControlsView view, UISettingsModel currentModel)
    {
        UISettingsModel model = view != null
            ? view.ReadModelFromControls(currentModel)
            : currentModel;

        SettingsService.Save(model);
        SettingsService.ApplyRuntime(model);
        view?.ApplyVisualPreferences(model);
        return model;
    }

    public UISettingsModel ResetSettings(ISettingsControlsView view)
    {
        UISettingsModel model = SettingsService.ResetToDefaults();
        if (view != null)
        {
            view.Bind(model);
            view.ApplyVisualPreferences(model);
        }

        SettingsService.ApplyRuntime(model);
        return model;
    }
}
