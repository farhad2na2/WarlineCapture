using UnityEngine;

internal sealed class BuildingGameplayStartupCompositionSystem
{
    public void Initialize(
        BuildingGameplayCompositionSourceSystem childSystems,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        Camera worldCamera,
        Transform runtimeUiRoot,
        RoadFootprintQuerySystem roadFootprintQuerySystem,
        RoadFootprintQuerySystem.Context roadFootprintQueryContext,
        FactionVisualSettings factionVisuals,
        DayNightSystem dayNight)
    {
        childSystems.RuntimeResourceSystem.SetInitialDollars(
            BuildingStartupConfigProjectionSystem.ResolveInitialDollars(buildingPlacementConfig));
        childSystems.BuildingGameplayDependencySystem.SetStartupDependencies(
            null,
            factionVisuals,
            dayNight);
        childSystems.BuildingPlacementStartupSystem.ConfigureRoadFootprintQuery(
            roadFootprintQuerySystem,
            roadFootprintQueryContext);
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
