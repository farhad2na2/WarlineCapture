using UnityEngine;

internal sealed class BuildingGameplayStartupCompositionSystemHelper
{
    public void Initialize(
        BuildingGameplaySourceCompositionSystemHelper childSystems,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        Camera worldCamera,
        Transform runtimeUiRoot,
        RoadGridProjectionSystem.RoadFootprintState roadFootprintState,
        FactionVisualSettings factionVisuals,
        DayNightSystem dayNight)
    {
        childSystems.RuntimeResourceSystem.SetInitialDollars(
            BuildingStartupConfigProjectionSystem.ResolveInitialDollars(buildingPlacementConfig));
        childSystems.BuildingGameplayDependencyCompositionSystemHelper.SetStartupDependencies(
            null,
            factionVisuals,
            dayNight);
        childSystems.BuildingPlacementStartupSystemHelper.ConfigureRoadFootprintState(roadFootprintState);
        childSystems.BuildingPlacementStartupSystemHelper.Init(
            buildingPlacementConfig,
            worldCamera,
            runtimeUiRoot,
            childSystems.BuildingDefinitionSystem,
            childSystems.BuildingRunwaySystem,
            childSystems.BuildingPlacementPreviewPresentationSystemHelper,
            childSystems.RuntimeObjectPresentationHelper.DestroyRuntimeObject);
    }
}
