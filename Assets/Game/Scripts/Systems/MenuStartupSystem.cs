using System;
using Game.Scripts.UI;
using Unity.Entities;
using UnityEngine;

public sealed class MenuStartupSystem
{
    public MainMenuPlayUI Initialize(
        MenuView menuView,
        Action gameRequested,
        RoadBuildSystem roadBuild,
        BuildingPlacementSystem buildingPlacement,
        RTSSelectionSystem selection,
        DayNightSystem dayNight,
        CitizenPopulationSystem citizenPopulation,
        Camera worldCamera,
        GameplaySceneBindingSystem sceneBindingSystem,
        Chapter01MissionTacticalRuntimeBinder chapter01TacticalBinder,
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
                buildingPlacement?.BuildingUiCommandSystem,
                buildingPlacement != null ? buildingPlacement.CreateBuildingUiCommandContext() : default);
            menuView.BindBuildingUiQuerySystem(
                buildingPlacement?.BuildingUiQuerySystem,
                buildingPlacement != null ? buildingPlacement.CreateBuildingUiQueryContext() : default);
            menuView.NotifyBootstrapReady();
        }

        try
        {
            var mainMenu = new MainMenuPlayUI();
            mainMenu.Init(roadBuild, selection, dayNight);
            BindMenuDependencies(mainMenu, roadBuild, buildingPlacement, selection, dayNight);
            BindSceneUi(sceneBindingSystem, chapter01TacticalBinder, world, selection);
            return mainMenu;
        }
        catch (Exception exception)
        {
            logException?.Invoke(exception);
            BindMenuDependencies(null, roadBuild, buildingPlacement, selection, dayNight);
            BindSceneUi(sceneBindingSystem, chapter01TacticalBinder, world, selection);
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
        BuildingPlacementSystem buildingPlacement,
        RTSSelectionSystem selection,
        DayNightSystem dayNight)
    {
        roadBuild?.BindDependencies(
            buildingPlacement?.BuildingPlacementInteractionSystem,
            buildingPlacement != null ? buildingPlacement.CreateBuildingPlacementInteractionContext() : default,
            mainMenu);
        buildingPlacement?.BindDependencies(roadBuild, mainMenu, dayNight, selection);
        selection?.BindDependencies(
            mainMenu,
            roadBuild,
            buildingPlacement?.BuildingPlacementInteractionSystem,
            buildingPlacement != null ? buildingPlacement.CreateBuildingPlacementInteractionContext() : default);
    }

    private void BindSceneUi(
        GameplaySceneBindingSystem sceneBindingSystem,
        Chapter01MissionTacticalRuntimeBinder chapter01TacticalBinder,
        World world,
        RTSSelectionSystem selection)
    {
        sceneBindingSystem?.BindGameplayUiRuntimeDependencies(
            chapter01TacticalBinder,
            world,
            selection);
    }
}
