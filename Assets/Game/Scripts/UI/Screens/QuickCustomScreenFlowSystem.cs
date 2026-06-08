internal sealed class QuickCustomScreenFlowSystem
{
    public void Initialize(QuickCustomScreenView view)
    {
        view?.Bind(QuickGameConfig.FromRuntimeState());
    }

    public void ResetToDefaults(QuickCustomScreenView view)
    {
        view?.Bind(QuickGameConfig.Defaults);
    }

    public void ApplyCurrentConfigToRuntime(QuickCustomScreenView view)
    {
        if (view == null)
            return;

        view.ReadConfigFromControls().ApplyToRuntimeState();
    }

    public void LaunchMatch(QuickCustomScreenView view)
    {
        if (view == null)
            return;

        ApplyCurrentConfigToRuntime(view);
        UIGameLaunchUtility.StartExistingGameplayAndHideRouter(view);
    }
}
