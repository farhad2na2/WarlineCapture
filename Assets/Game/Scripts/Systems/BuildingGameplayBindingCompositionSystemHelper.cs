using System;

internal sealed class BuildingGameplayBindingCompositionSystemHelper
{
    public Action<IMatchRuntimeUi> CreateMainMenuBinding(
        BuildingGameplayCompositionSourceSystem childSystems,
        DayNightSystem dayNight)
    {
        return mainMenu => childSystems.BuildingGameplayDependencySystem.BindRuntimeDependencies(mainMenu, dayNight);
    }

    public Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> CreateGameplayFeatureBinding(
        BuildingGameplayCompositionSourceSystem childSystems,
        DayNightSystem dayNight)
    {
        return (mainMenu, selectionUiCameraSystem, selectionBuildingInteractionSystem, runtimeGridBlockers, runtimeCity, citizenPopulationEventSystem) =>
            childSystems.BuildingGameplayDependencySystem.BindRuntimeDependencies(
                mainMenu,
                dayNight,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem,
                runtimeGridBlockers,
                runtimeCity,
                citizenPopulationEventSystem);
    }
}
