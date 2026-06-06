using System;
using Game.Scripts.UI;
using Unity.Entities;
using UnityEngine;

internal sealed class MenuStartupSystem
{
    private readonly SceneLifecycleSystem sceneLifecycleSystem = new();
    private readonly MatchStartSystem matchStartSystem = new();

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
        CitizenPopulationReadModelSystem citizenPopulationReadModel,
        Camera worldCamera,
        GameplaySceneBindingSystem sceneBindingSystem,
        World world,
        Action<Exception> logException)
    {
        if (menuView != null)
        {
            menuView.GameRequested += RequestMatchStart;
            menuView.Init(
                selectionUiCommandSystem,
                selectionUiReadModelSystem,
                selectionUiCameraSystem,
                selectionScreenMarkerSystem,
                worldCamera,
                dayNight,
                citizenPopulationReadModel,
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
            mainMenu.Init(selectionUiCommandSystem, dayNight, selectionUiCameraSystem);
            BindMenuDependencies(
                mainMenu,
                bindRoadMainMenu,
                buildingPlacementInteraction,
                buildingPlacementInteractionContext,
                bindBuildingMainMenu,
                bindSelectionMainMenu);
            BindSceneUi(sceneBindingSystem, world, selectionUiCommandSystem, mainMenu);
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
            BindSceneUi(sceneBindingSystem, world, selectionUiCommandSystem, null);
            return null;
        }
    }

    public void Shutdown(MenuView menuView, Action gameRequested)
    {
        if (menuView != null)
            menuView.GameRequested -= RequestMatchStart;
    }

    private void RequestMatchStart()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogError("[MenuPlay] Cannot queue Match start because the default ECS world is missing.");
            return;
        }

        EntityManager entityManager = world.EntityManager;
        bool loadQueued = sceneLifecycleSystem.QueueLoadMatch(entityManager);
        bool startQueued = matchStartSystem.QueueStartAfterMatchLoaded(entityManager);
        if (!loadQueued || !startQueued)
            Debug.LogError($"[MenuPlay] Failed to queue Match start. loadQueued={(loadQueued ? 1 : 0)} startQueued={(startQueued ? 1 : 0)}");
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
        SelectionUiCommandSystem selectionUiCommandSystem,
        MainMenuPlayUI mainMenu)
    {
        sceneBindingSystem?.BindGameplayUiRuntimeDependencies(
            world,
            selectionUiCommandSystem,
            mainMenu);
    }
}
