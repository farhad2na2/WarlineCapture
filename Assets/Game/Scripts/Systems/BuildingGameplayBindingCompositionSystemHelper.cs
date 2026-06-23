using System;

internal sealed class BuildingGameplayBindingCompositionSystemHelper
{
    public Action<IMatchRuntimeUi> CreateMainMenuBinding(
        BuildingGameplaySourceCompositionSystemHelper childSystems,
        DayNightSystem dayNight)
    {
        return mainMenu => childSystems.BuildingGameplayDependencyCompositionSystemHelper.BindRuntimeDependencies(mainMenu, dayNight);
    }

    public Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> CreateGameplayFeatureBinding(
        BuildingGameplaySourceCompositionSystemHelper childSystems,
        DayNightSystem dayNight)
    {
        return (mainMenu, selectionUiCameraSystem, selectionBuildingInteractionSystem, runtimeGridBlockers, runtimeCity, citizenPopulationEventSystem) =>
            childSystems.BuildingGameplayDependencyCompositionSystemHelper.BindRuntimeDependencies(
                mainMenu,
                dayNight,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem,
                runtimeGridBlockers,
                runtimeCity,
                citizenPopulationEventSystem);
    }
}
