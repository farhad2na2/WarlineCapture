using Unity.Entities;

internal sealed partial class BuildingGameplayChildSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public BuildingGameplayCompositionSourceSystem Create()
    {
        return new BuildingGameplayCompositionSourceSystem();
    }
}
