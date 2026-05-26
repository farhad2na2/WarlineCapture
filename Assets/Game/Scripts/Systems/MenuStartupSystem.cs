using System;
using Game.Scripts.UI;
using Unity.Entities;
using UnityEngine;

internal sealed class MenuStartupSystem
{
    public MainMenuPlayUI Initialize(
        MenuView menuView,
        Action gameRequested,
        Action<MainMenuPlayUI> bindRoadMainMenu,
        BuildingUiCommandSystem buildingUiCommand,
        BuildingUiCommandSystem.Context buildingUiCommandContext,
        BuildingUiQuerySystem buildingUiQuery,
        BuildingUiQuerySystem.Context buildingUiQueryContext,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        Action<MainMenuPlayUI> bindBuildingMainMenu,
        Action<MainMenuPlayUI> bindSelectionMainMenu,
        SelectionUiCommandSystem selectionUiCommandSystem,
        SelectionUiReadModelSystem selectionUiReadModelSystem,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionScreenMarkerSystem selectionScreenMarkerSystem,
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
                selectionUiCommandSystem,
                selectionUiReadModelSystem,
                selectionUiCameraSystem,
                selectionScreenMarkerSystem,
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
            mainMenu.Init(selectionUiCommandSystem, dayNight);
            BindMenuDependencies(
                mainMenu,
                bindRoadMainMenu,
                buildingPlacementInteraction,
                buildingPlacementInteractionContext,
                bindBuildingMainMenu,
                bindSelectionMainMenu);
            BindSceneUi(sceneBindingSystem, world, selectionUiCommandSystem);
            return mainMenu;
        }
        catch (Exception exception)
        {
            logException?.Invoke(exception);
            BindMenuDependencies(
                null,
                bindRoadMainMenu,
                buildingPlacementInteraction,
                buildingPlacementInteractionContext,
                bindBuildingMainMenu,
                bindSelectionMainMenu);
            BindSceneUi(sceneBindingSystem, world, selectionUiCommandSystem);
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
        Action<MainMenuPlayUI> bindRoadMainMenu,
        BuildingPlacementInteractionSystem buildingPlacementInteraction,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        Action<MainMenuPlayUI> bindBuildingMainMenu,
        Action<MainMenuPlayUI> bindSelectionMainMenu)
    {
        bindRoadMainMenu?.Invoke(mainMenu);
        bindBuildingMainMenu?.Invoke(mainMenu);
        bindSelectionMainMenu?.Invoke(mainMenu);
    }

    private void BindSceneUi(
        GameplaySceneBindingSystem sceneBindingSystem,
        World world,
        SelectionUiCommandSystem selectionUiCommandSystem)
    {
        sceneBindingSystem?.BindGameplayUiRuntimeDependencies(
            world,
            selectionUiCommandSystem);
    }
}
