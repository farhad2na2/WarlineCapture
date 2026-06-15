using Unity.Entities;

public sealed partial class RoadMinimapEventSystem : SystemBase
{
    private IMatchRuntimeUi _mainMenuPlayUi;
    private bool _staticMinimapChanged;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnDestroy()
    {
        Clear();
    }

    protected override void OnUpdate()
    {
    }

    public void Configure(IMatchRuntimeUi mainMenuPlayUi)
    {
        _mainMenuPlayUi = mainMenuPlayUi;
    }

    public void PublishStaticMinimapChanged()
    {
        _staticMinimapChanged = true;
        Flush();
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
        _staticMinimapChanged = false;
        _mainMenuPlayUi = null;
    }
}
