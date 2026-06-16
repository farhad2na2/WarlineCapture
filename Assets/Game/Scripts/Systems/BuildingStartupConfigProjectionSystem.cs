using Unity.Entities;

internal partial struct BuildingStartupConfigProjectionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public static int ResolveInitialDollars(BuildingPlacementSystemConfig buildingPlacementConfig)
    {
        return buildingPlacementConfig != null && buildingPlacementConfig.InitialUnitsConfig != null
            ? buildingPlacementConfig.InitialUnitsConfig.InitialDollars
            : 0;
    }
}
