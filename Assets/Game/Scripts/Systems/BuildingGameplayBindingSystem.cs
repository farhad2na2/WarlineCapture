using System;
using Game.Scripts.UI;

internal sealed class BuildingGameplayBindingSystem
{
    public Action<MainMenuPlayUI> CreateMainMenuBinding(
        BuildingGameplayCompositionSourceSystem childSystems,
        DayNightSystem dayNight)
    {
        return mainMenu => childSystems.BuildingGameplayDependencySystem.BindRuntimeDependencies(mainMenu, dayNight);
    }

    public Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> CreateGameplayFeatureBinding(
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
