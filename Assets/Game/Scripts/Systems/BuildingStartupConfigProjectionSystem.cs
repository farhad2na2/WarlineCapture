internal static class BuildingStartupConfigProjectionSystem
{
    public static int ResolveInitialDollars(BuildingPlacementSystemConfig buildingPlacementConfig)
    {
        return buildingPlacementConfig != null && buildingPlacementConfig.InitialUnitsConfig != null
            ? buildingPlacementConfig.InitialUnitsConfig.InitialDollars
            : 0;
    }
}
