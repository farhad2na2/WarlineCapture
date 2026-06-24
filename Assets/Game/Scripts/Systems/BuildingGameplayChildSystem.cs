using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
internal partial struct BuildingGameplayChildSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public BuildingGameplaySourceCompositionSystemHelper Create()
    {
        return new BuildingGameplaySourceCompositionSystemHelper();
    }
}
