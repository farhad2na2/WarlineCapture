internal sealed class RuntimeCityMinimapEventSystem
{
    private IMatchRuntimeUi _mainMenuPlayUi;
    private bool _staticMinimapChanged;

    public void Configure(IMatchRuntimeUi mainMenuPlayUi)
    {
        _mainMenuPlayUi = mainMenuPlayUi;
    }

    public void PublishStaticMinimapChanged()
    {
        _staticMinimapChanged = true;
    }

    public void Flush()
    {
        if (!_staticMinimapChanged)
            return;

        _staticMinimapChanged = false;
        _mainMenuPlayUi?.NotifyStaticMinimapChanged();
    }

    public void Clear()
    {
        _mainMenuPlayUi = null;
        _staticMinimapChanged = false;
    }
}
