using System;
using Game.Scripts.UI;
using Unity.Entities;
using UnityEngine;

internal sealed class MenuStartupSystem
{
    public MainMenuPlayUI Initialize(
        MenuView menuView,
        Action gameRequested,
        RoadBuildSystem roadBuild,
        BuildingUiCommandSystem buildingUiCommand,
        BuildingUiCommandSystem.Context buildingUiCommandContext,
        BuildingUiQuerySystem buildingUiQuery,
        BuildingUiQuerySystem.Context buildingUiQueryContext,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        Action<MainMenuPlayUI, RTSSelectionSystem> bindBuildingMainMenu,
        RTSSelectionSystem selection,
        DayNightSystem dayNight,
        CitizenPopulationSystem citizenPopulation,
        Camera worldCamera,
        GameplaySceneBindingSystem sceneBindingSystem,
        World world,
        Action<Exception> logException)
    {
        if (menuView != null)
        {
            menuView.GameRequested += gameRequested;
            menuView.Init(
                selection,
                worldCamera,
                dayNight,
                citizenPopulation,
                buildingUiCommand,
                buildingUiCommandContext);
            menuView.BindBuildingUiQuerySystem(
                buildingUiQuery,
                buildingUiQueryContext);
            menuView.NotifyBootstrapReady();
        }

        try
        {
            var mainMenu = new MainMenuPlayUI();
            mainMenu.Init(roadBuild, selection, dayNight);
            BindMenuDependencies(
                mainMenu,
                roadBuild,
                buildingPlacementInteraction,
                buildingPlacementInteractionContext,
                bindBuildingMainMenu,
                selection);
            BindSceneUi(sceneBindingSystem, world, selection);
            return mainMenu;
        }
        catch (Exception exception)
        {
            logException?.Invoke(exception);
            BindMenuDependencies(
                null,
                roadBuild,
                buildingPlacementInteraction,
                buildingPlacementInteractionContext,
                bindBuildingMainMenu,
                selection);
            BindSceneUi(sceneBindingSystem, world, selection);
            return null;
        }
    }

    public void Shutdown(MenuView menuView, Action gameRequested)
    {
        if (menuView != null)
            menuView.GameRequested -= gameRequested;
    }

    private void BindMenuDependencies(
        MainMenuPlayUI mainMenu,
        RoadBuildSystem roadBuild,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        Action<MainMenuPlayUI, RTSSelectionSystem> bindBuildingMainMenu,
        RTSSelectionSystem selection)
    {
        roadBuild?.BindDependencies(
            buildingPlacementInteraction,
            buildingPlacementInteractionContext,
            mainMenu);
        bindBuildingMainMenu?.Invoke(mainMenu, selection);
        selection?.BindDependencies(
            mainMenu,
            roadBuild,
            buildingPlacementInteraction,
            buildingPlacementInteractionContext);
    }

    private void BindSceneUi(
        GameplaySceneBindingSystem sceneBindingSystem,
        World world,
        RTSSelectionSystem selection)
    {
        sceneBindingSystem?.BindGameplayUiRuntimeDependencies(
            world,
            selection);
    }
}
