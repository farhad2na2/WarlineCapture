using System;
using Game.Scripts.UI;
using UnityEngine;

internal sealed class BuildingGameplayCompositionSystem
{
    private readonly BuildingPlacementRuntimeTickContextSystem _runtimeTickContextSystem = new();

    public readonly struct Result
    {
        private readonly BuildingPlacementSystem PlacementFacade;
        public readonly BuildingSelectionClickSystem SelectionClick;
        public readonly BuildingSelectionClickSystem.Context SelectionClickContext;
        public readonly BuildingRuntimeUpdateSystem RuntimeUpdate;
        public readonly BuildingRuntimeUpdateSystem.Context RuntimeUpdateContext;
        public readonly BuildingRuntimeCitySpawnSystem RuntimeCitySpawn;
        public readonly BuildingRuntimeCitySpawnSystem.Context RuntimeCitySpawnContext;
        public readonly BuildingUiCommandSystem UiCommand;
        public readonly BuildingUiCommandSystem.Context UiCommandContext;
        public readonly BuildingUiQuerySystem UiQuery;
        public readonly BuildingUiQuerySystem.Context UiQueryContext;
        public readonly BuildingPlacementInteractionSystem Interaction;
        public readonly BuildingPlacementInteractionSystem.Context InteractionContext;
        public readonly Action<MainMenuPlayUI, RTSSelectionSystem> BindMainMenu;
        public readonly Action<MainMenuPlayUI, RTSSelectionSystem, RuntimeGridBlockerSystem, RuntimeCitySpawnerSystem, CitizenPopulationSystem> BindGameplayFeatures;
        public readonly Action Dispose;

        public Result(
            BuildingPlacementSystem placementFacade,
            BuildingSelectionClickSystem selectionClick,
            BuildingSelectionClickSystem.Context selectionClickContext,
            BuildingRuntimeUpdateSystem runtimeUpdate,
            BuildingRuntimeUpdateSystem.Context runtimeUpdateContext,
            BuildingRuntimeCitySpawnSystem runtimeCitySpawn,
            BuildingRuntimeCitySpawnSystem.Context runtimeCitySpawnContext,
            BuildingUiCommandSystem uiCommand,
            BuildingUiCommandSystem.Context uiCommandContext,
            BuildingUiQuerySystem uiQuery,
            BuildingUiQuerySystem.Context uiQueryContext,
            BuildingPlacementInteractionSystem interaction,
            BuildingPlacementInteractionSystem.Context interactionContext,
            Action<MainMenuPlayUI, RTSSelectionSystem> bindMainMenu,
            Action<MainMenuPlayUI, RTSSelectionSystem, RuntimeGridBlockerSystem, RuntimeCitySpawnerSystem, CitizenPopulationSystem> bindGameplayFeatures,
            Action dispose)
        {
            PlacementFacade = placementFacade;
            SelectionClick = selectionClick;
            SelectionClickContext = selectionClickContext;
            RuntimeUpdate = runtimeUpdate;
            RuntimeUpdateContext = runtimeUpdateContext;
            RuntimeCitySpawn = runtimeCitySpawn;
            RuntimeCitySpawnContext = runtimeCitySpawnContext;
            UiCommand = uiCommand;
            UiCommandContext = uiCommandContext;
            UiQuery = uiQuery;
            UiQueryContext = uiQueryContext;
            Interaction = interaction;
            InteractionContext = interactionContext;
            BindMainMenu = bindMainMenu;
            BindGameplayFeatures = bindGameplayFeatures;
            Dispose = dispose;
        }

        public void BindSelection(RoadBuildSystem roadBuild, DayNightSystem dayNight, RTSSelectionSystem selection)
        {
            PlacementFacade?.BindDependencies(roadBuild, null, dayNight, selection);
        }

        public CitizenPopulationSystem CreateCitizenPopulation(DayNightSystem dayNight, Camera worldCamera)
        {
            var citizenPopulation = new CitizenPopulationSystem();
            citizenPopulation.Init(
                PlacementFacade.RuntimeQuerySystem,
                PlacementFacade.CreateRuntimeBuildingQueryContext(),
                dayNight,
                worldCamera,
                PlacementFacade.RuntimeResourceSystem.CreateCitizenResourceContext(),
                PlacementFacade.RuntimeUnitPrefabSystem.CreateCitizenPrefabContext(PlacementFacade.CreateRuntimeUnitPrefabContext()));
            return citizenPopulation;
        }

        public void BindCitizenPopulation(
            RoadBuildSystem roadBuild,
            DayNightSystem dayNight,
            RTSSelectionSystem selection,
            CitizenPopulationSystem citizenPopulation)
        {
            PlacementFacade?.BindDependencies(
                roadBuild,
                null,
                dayNight,
                selection,
                citizenPopulationSystem: citizenPopulation);
        }
    }

    public Result Initialize(
        BuildingPlacementSystemConfig buildingPlacementConfig,
        Camera worldCamera,
        Transform runtimeUiRoot,
        RoadBuildSystem roadBuild,
        FactionVisualSettings factionVisuals,
        DayNightSystem dayNight)
    {
        var placementFacade = new BuildingPlacementSystem();
        placementFacade.Init(buildingPlacementConfig, worldCamera, runtimeUiRoot, roadBuild, null, factionVisuals, dayNight);

        var runtimeUpdate = new BuildingRuntimeUpdateSystem();
        return new Result(
            placementFacade,
            placementFacade.BuildingSelectionClickSystem,
            placementFacade.CreateBuildingSelectionClickContext(),
            runtimeUpdate,
            new BuildingRuntimeUpdateSystem.Context(
                () => placementFacade.RuntimeTickSystem.Update(_runtimeTickContextSystem.Create(CreateRuntimeTickSource(placementFacade)))),
            placementFacade.RuntimeCitySpawnSystem,
            placementFacade.CreateRuntimeCitySpawnContext(),
            placementFacade.BuildingUiCommandSystem,
            placementFacade.CreateBuildingUiCommandContext(),
            placementFacade.BuildingUiQuerySystem,
            placementFacade.CreateBuildingUiQueryContext(),
            placementFacade.BuildingPlacementInteractionSystem,
            placementFacade.CreateBuildingPlacementInteractionContext(),
            (mainMenu, selection) => placementFacade.BindDependencies(roadBuild, mainMenu, dayNight, selection),
            (mainMenu, selection, runtimeGridBlockers, runtimeCitySpawner, citizenPopulation) =>
                placementFacade.BindDependencies(
                    roadBuild,
                    mainMenu,
                    dayNight,
                    selection,
                    runtimeGridBlockers,
                    runtimeCitySpawner,
                    citizenPopulation),
            placementFacade.Dispose);
    }

    internal static BuildingPlacementRuntimeTickContextSystem.Source CreateRuntimeTickSource(BuildingPlacementSystem placement)
    {
        var tickDomains = placement.RuntimeTickDomains;
        BuildingRuntimeVisualSystem.Context runtimeVisualContext = placement.CreateBuildingRuntimeVisualContext();
        BuildingCombatSystem.Context<RuntimeBuildingData> combatContext = placement.CreateBuildingCombatContext();
        BuildingBarrierSystem.Context barrierContext = placement.CreateBuildingBarrierContext();
        return new BuildingPlacementRuntimeTickContextSystem.Source(
            CreateProductionRuntimeTickContext(placement),
            CreateRuntimeBoundaryPublishContext(placement),
            () => tickDomains.RuntimeVisual.UpdateBuildingResourceVisuals(runtimeVisualContext, Time.time),
            () => tickDomains.Combat.SyncDestroyedRuntimeBuildingCombatEntities(
                combatContext,
                Time.time,
                tickDomains.DestroyedBuildingLifetime),
            () => tickDomains.Combat.UpdateDestroyedBuildings(combatContext, Time.time),
            () => tickDomains.Barrier.UpdateRoadBarrierDoors(barrierContext, Time.deltaTime),
            () => tickDomains.Redirect.FlushPendingMarkerRefresh(
                () => tickDomains.RuntimeVisual.RefreshBuildingMarkerVisibility(runtimeVisualContext)),
            () => placement.WorldCamera,
            () => placement.ActivePlacement,
            placement.UpdateActivePlacementPointer,
            () => placement.PlayRequested,
            () => placement.BuildModeActive,
            placement.HidePlacementOutline,
            placement.ShouldIgnoreBuildingSelectionThisFrame,
            placement.IsPointerOverAnyGameplayUi,
            () => placement.HasActiveBuilding,
            placement.IsPointerOverUnitCommandUi,
            placement.SuppressNextWorldClick,
            placement.HandleBuildingSelectionClick,
            () => placement.RuntimeBuildingCount,
            placement.DiagnosticsEnabled,
            placement.DiagnosticsFreezeLogThresholdSeconds);
    }

    private static BuildingProductionRuntimeTickSystem.Context CreateProductionRuntimeTickContext(BuildingPlacementSystem placement)
    {
        return new BuildingProductionRuntimeTickSystem.Context(
            placement.RuntimeBuildings,
            placement.DayNightSystem,
            placement.FactionResourceSystem,
            placement.ProductionUpdateSystem,
            placement.CreateBuildingProductionUpdateContext(),
            placement.ResourceHaulerBridgeSystem,
            placement.CreateBuildingResourceHaulerBridgeContext(),
            placement.BuildingSpawnSystem,
            () => placement.BuildingSpawnRandomState,
            value => placement.BuildingSpawnRandomState = value,
            GameRuntimeStats.RecordOilExtracted,
            GameRuntimeStats.RecordFuelProduced,
            placement.OilBarrelsPerFuelBarrelRatio);
    }

    private static BuildingRuntimeBoundaryPublishSystem.Context CreateRuntimeBoundaryPublishContext(BuildingPlacementSystem placement)
    {
        return new BuildingRuntimeBoundaryPublishSystem.Context(
            placement.TryGetEntityManagerForRuntimeTick,
            placement.EnsureEntityQueries,
            placement.RuntimeBoundarySystem,
            placement.DefinitionSystem,
            placement.RuntimeSpawnSystem,
            placement.CreateBuildingRuntimeSpawnContext(),
            placement.ProductionRequestSystem,
            placement.CreateBuildingProductionRequestContext(),
            placement.RuntimeQuerySystem,
            placement.CreateBuildingRuntimeQueryContext(),
            placement.FactionResourceSystem,
            () => placement.RuntimeBoundaryQuery,
            placement.RuntimeBuildings);
    }

    public void BindSelection(Result building, RoadBuildSystem roadBuild, DayNightSystem dayNight, RTSSelectionSystem selection)
    {
        building.BindSelection(roadBuild, dayNight, selection);
    }

    public CitizenPopulationSystem CreateCitizenPopulation(Result building, DayNightSystem dayNight, Camera worldCamera)
    {
        return building.CreateCitizenPopulation(dayNight, worldCamera);
    }

    public void BindCitizenPopulation(
        Result building,
        RoadBuildSystem roadBuild,
        DayNightSystem dayNight,
        RTSSelectionSystem selection,
        CitizenPopulationSystem citizenPopulation)
    {
        building.BindCitizenPopulation(roadBuild, dayNight, selection, citizenPopulation);
    }
}
