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
            placementFacade.RuntimeContextSystem.CreateCitySpawnContext(placementFacade.CreateBuildingRuntimeContextSource()),
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
        var inputDomains = placement.RuntimeInputDomains;
        BuildingRuntimeVisualSystem.Context runtimeVisualContext = placement.CreateBuildingRuntimeVisualContext();
        BuildingCombatSystem.Context<RuntimeBuildingData> combatContext = placement.CreateBuildingCombatContext();
        BuildingBarrierSystem.Context barrierContext = placement.CreateBuildingBarrierContext();
        var inputRuntimeTickSystem = new BuildingPlacementInputRuntimeTickSystem();
        BuildingPlacementInputRuntimeTickSystem.Context inputContext = CreateInputRuntimeTickContext(placement, inputDomains);
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
            () => inputRuntimeTickSystem.Update(inputContext),
            CreateRuntimeTickDiagnosticsContext(placement));
    }

    private static BuildingPlacementInputRuntimeTickSystem.Context CreateInputRuntimeTickContext(
        BuildingPlacementSystem placement,
        (BuildingPlacementInputSystem PlacementInput, BuildingPlacementPreviewSystem Preview, RuntimeGameplayStateSystem RuntimeState, Func<MainMenuPlayUI> GetMainMenu, BuildingSelectionClickSystem SelectionClick) inputDomains)
    {
        return new BuildingPlacementInputRuntimeTickSystem.Context(
            () => placement.WorldCamera,
            () => placement.ActivePlacement,
            inputDomains.PlacementInput,
            placement.CreateActivePlacementPointerContext(),
            () => placement.PlayRequested,
            () => placement.BuildModeActive,
            inputDomains.Preview,
            () => placement.HasActiveBuilding,
            inputDomains.RuntimeState,
            inputDomains.GetMainMenu,
            inputDomains.SelectionClick,
            placement.CreateBuildingSelectionClickContext());
    }

    private static BuildingProductionRuntimeTickSystem.Context CreateProductionRuntimeTickContext(BuildingPlacementSystem placement)
    {
        RuntimeBuildingSystem<RuntimeBuildingData> registry = placement.RuntimeBuildingRegistry;
        return new BuildingProductionRuntimeTickSystem.Context(
            registry.Buildings,
            placement.DayNightSystem,
            placement.FactionResourceSystem,
            placement.ProductionUpdateSystem,
            placement.ProductionContextSystem.CreateProductionUpdateContext(placement.CreateBuildingProductionContextSource()),
            placement.ResourceHaulerBridgeSystem,
            placement.ProductionContextSystem.CreateResourceHaulerBridgeContext(placement.CreateBuildingProductionContextSource()),
            placement.BuildingSpawnSystem,
            () => placement.BuildingSpawnRandomState,
            value => placement.BuildingSpawnRandomState = value,
            GameRuntimeStats.RecordOilExtracted,
            GameRuntimeStats.RecordFuelProduced,
            placement.OilBarrelsPerFuelBarrelRatio);
    }

    private static BuildingPlacementRuntimeTickDiagnosticsSystem.Context CreateRuntimeTickDiagnosticsContext(BuildingPlacementSystem placement)
    {
        RuntimeBuildingSystem<RuntimeBuildingData> registry = placement.RuntimeBuildingRegistry;
        return new BuildingPlacementRuntimeTickDiagnosticsSystem.Context(
            () => registry.Count,
            Debug.Log);
    }

    private static BuildingRuntimeBoundaryPublishSystem.Context CreateRuntimeBoundaryPublishContext(BuildingPlacementSystem placement)
    {
        RuntimeBuildingSystem<RuntimeBuildingData> registry = placement.RuntimeBuildingRegistry;
        return new BuildingRuntimeBoundaryPublishSystem.Context(
            placement.TryGetEntityManagerForRuntimeTick,
            placement.EnsureEntityQueries,
            placement.RuntimeBoundarySystem,
            placement.DefinitionSystem,
            placement.RuntimeSpawnSystem,
            placement.RuntimeContextSystem.CreateSpawnContext(placement.CreateBuildingRuntimeContextSource()),
            placement.ProductionRequestSystem,
            placement.ProductionContextSystem.CreateProductionRequestContext(placement.CreateBuildingProductionContextSource()),
            placement.RuntimeQuerySystem,
            placement.CreateBuildingRuntimeQueryContext(),
            placement.FactionResourceSystem,
            () => placement.RuntimeBoundaryQuery,
            registry.Buildings);
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
