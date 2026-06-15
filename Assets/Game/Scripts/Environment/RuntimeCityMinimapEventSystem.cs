using Unity.Entities;

internal sealed partial class RuntimeCityMinimapEventSystem : SystemBase
{
    private IMatchRuntimeUi _mainMenuPlayUi;
    private bool _staticMinimapChanged;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnDestroy()
    {
        Clear();
    }

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
