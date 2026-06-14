using UnityEngine;

internal sealed class BuildingGameplayStartupCompositionSystem
{
    public void Initialize(
        BuildingGameplayCompositionSourceSystem childSystems,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        Camera worldCamera,
        Transform runtimeUiRoot,
        RoadGridProjectionSystem.RoadFootprintState roadFootprintState,
        FactionVisualSettings factionVisuals,
        DayNightSystem dayNight)
    {
        childSystems.RuntimeResourceSystem.SetInitialDollars(
            BuildingStartupConfigProjectionSystem.ResolveInitialDollars(buildingPlacementConfig));
        childSystems.BuildingGameplayDependencySystem.SetStartupDependencies(
            null,
            factionVisuals,
            dayNight);
        childSystems.BuildingPlacementStartupSystem.ConfigureRoadFootprintState(roadFootprintState);
        childSystems.BuildingPlacementStartupSystem.Init(
            buildingPlacementConfig,
            worldCamera,
            runtimeUiRoot,
            childSystems.BuildingDefinitionSystem,
            childSystems.BuildingRunwaySystem,
            childSystems.BuildingPlacementPreviewSystem,
            childSystems.BuildingRuntimeObjectSystem.DestroyRuntimeObject);
    }
}
