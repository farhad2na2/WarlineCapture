using Unity.Entities;

internal partial struct BuildingGameplayChildSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public BuildingGameplayCompositionSourceSystem Create()
    {
        return new BuildingGameplayCompositionSourceSystem();
    }
}
