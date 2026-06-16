using Unity.Entities;

internal sealed partial class BuildingStartupConfigProjectionSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static int ResolveInitialDollars(BuildingPlacementSystemConfig buildingPlacementConfig)
    {
        return buildingPlacementConfig != null && buildingPlacementConfig.InitialUnitsConfig != null
            ? buildingPlacementConfig.InitialUnitsConfig.InitialDollars
            : 0;
    }
}
