internal sealed class QuickCustomScreenFlowSystem
{
    public void Initialize(QuickCustomScreenView view, IQuickCustomGameConfigStore configStore)
    {
        view?.Bind(configStore != null ? configStore.Current : UiQuickCustomGameConfig.Defaults);
    }

    public void ResetToDefaults(QuickCustomScreenView view, IQuickCustomGameConfigStore configStore)
    {
        view?.Bind(configStore != null ? configStore.Defaults : UiQuickCustomGameConfig.Defaults);
    }

    public void ApplyCurrentConfig(QuickCustomScreenView view, IQuickCustomGameConfigStore configStore)
    {
        if (view == null || configStore == null)
            return;

        configStore.Apply(view.ReadConfigFromControls());
    }

    public void LaunchMatch(
        QuickCustomScreenView view,
        IQuickCustomGameConfigStore configStore,
        IMatchLaunchCommand launchCommand)
    {
        if (view == null)
            return;

        ApplyCurrentConfig(view, configStore);
        launchCommand?.LaunchMatch(view);
    }
}
