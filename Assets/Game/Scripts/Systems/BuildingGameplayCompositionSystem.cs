using System;
using Game.Scripts.UI;
using UnityEngine;

internal sealed class BuildingGameplayCompositionSystem
{
    private readonly BuildingPlacementRuntimeTickContextSystem _runtimeTickContextSystem = new();

    public readonly struct Result
    {
        private readonly BuildingGameplaySystem Building;
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
        public readonly Action<MainMenuPlayUI> BindMainMenu;
        public readonly Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationSystem> BindGameplayFeatures;
        public readonly Action Dispose;

        public Result(
            BuildingGameplaySystem building,
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
            Action<MainMenuPlayUI> bindMainMenu,
            Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationSystem> bindGameplayFeatures,
            Action dispose)
        {
            Building = building;
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

        public void BindSelection(
            DayNightSystem dayNight,
            SelectionUiCameraSystem selectionUiCameraSystem,
            SelectionBuildingInteractionSystem selectionBuildingInteractionSystem)
        {
            Building?.BindDependencies(
                null,
                default,
                null,
                dayNight,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem);
        }

        public CitizenPopulationSystem CreateCitizenPopulation(DayNightSystem dayNight, Camera worldCamera)
        {
            var citizenPopulation = new CitizenPopulationSystem();
            BuildingRuntimeQuerySystem.Context runtimeQueryContext = Building.CreateRuntimeBuildingQueryContext();
            BuildingRuntimeResourcePrefabContextSystem.Source resourcePrefabSource = Building.CreateRuntimeResourcePrefabContextSource();
            CitizenResourceSystem.Context resourceContext = Building.RuntimeResourcePrefabContextSystem.CreateCitizenResourceContext(resourcePrefabSource);
            CitizenPrefabSystem.Context prefabContext = Building.RuntimeResourcePrefabContextSystem.CreateCitizenPrefabContext(resourcePrefabSource);
            citizenPopulation.Init(
                Building.RuntimeQuerySystem,
                runtimeQueryContext,
                dayNight,
                worldCamera,
                resourceContext,
                prefabContext);
            return citizenPopulation;
        }

        public void BindCitizenPopulation(
            DayNightSystem dayNight,
            SelectionUiCameraSystem selectionUiCameraSystem,
            SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
            CitizenPopulationSystem citizenPopulation)
        {
            Building?.BindDependencies(
                null,
                default,
                null,
                dayNight,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem,
                citizenPopulationSystem: citizenPopulation);
        }
    }

    public Result Initialize(
        BuildingPlacementSystemConfig buildingPlacementConfig,
        Camera worldCamera,
        Transform runtimeUiRoot,
        RoadFootprintQuerySystem roadFootprintQuerySystem,
        RoadFootprintQuerySystem.Context roadFootprintQueryContext,
        FactionVisualSettings factionVisuals,
        DayNightSystem dayNight)
    {
        var building = new BuildingGameplaySystem();
        building.Init(
            buildingPlacementConfig,
            worldCamera,
            runtimeUiRoot,
            roadFootprintQuerySystem,
            roadFootprintQueryContext,
            null,
            factionVisuals,
            dayNight);

        var runtimeUpdate = new BuildingRuntimeUpdateSystem();
        return new Result(
            building,
            building.BuildingSelectionClickSystem,
            building.CreateBuildingSelectionClickContext(),
            runtimeUpdate,
            new BuildingRuntimeUpdateSystem.Context(
                () => building.RuntimeTickSystem.Update(_runtimeTickContextSystem.Create(CreateRuntimeTickSource(building)))),
            building.RuntimeCitySpawnSystem,
            building.RuntimeContextSystem.CreateCitySpawnContext(building.CreateBuildingRuntimeContextSource()),
            building.BuildingUiCommandSystem,
            building.CreateBuildingUiCommandContext(),
            building.BuildingUiQuerySystem,
            building.CreateBuildingUiQueryContext(),
            building.BuildingPlacementInteractionSystem,
            building.CreateBuildingPlacementInteractionContext(),
            mainMenu => building.BindDependencies(null, default, mainMenu, dayNight),
            (mainMenu, selectionUiCameraSystem, selectionBuildingInteractionSystem, runtimeGridBlockers, runtimeCity, citizenPopulation) =>
                building.BindDependencies(
                    null,
                    default,
                    mainMenu,
                    dayNight,
                    selectionUiCameraSystem,
                    selectionBuildingInteractionSystem,
                    runtimeGridBlockers,
                    runtimeCity,
                    citizenPopulation),
            building.Dispose);
    }

    internal static BuildingPlacementRuntimeTickContextSystem.Source CreateRuntimeTickSource(BuildingGameplaySystem placement)
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
        BuildingGameplaySystem placement,
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

    private static BuildingProductionRuntimeTickSystem.Context CreateProductionRuntimeTickContext(BuildingGameplaySystem placement)
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

    private static BuildingPlacementRuntimeTickDiagnosticsSystem.Context CreateRuntimeTickDiagnosticsContext(BuildingGameplaySystem placement)
    {
        RuntimeBuildingSystem<RuntimeBuildingData> registry = placement.RuntimeBuildingRegistry;
        return new BuildingPlacementRuntimeTickDiagnosticsSystem.Context(
            () => registry.Count,
            Debug.Log);
    }

    private static BuildingRuntimeBoundaryPublishSystem.Context CreateRuntimeBoundaryPublishContext(BuildingGameplaySystem placement)
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

    public void BindSelection(
        Result building,
        DayNightSystem dayNight,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem)
    {
        building.BindSelection(dayNight, selectionUiCameraSystem, selectionBuildingInteractionSystem);
    }

    public CitizenPopulationSystem CreateCitizenPopulation(Result building, DayNightSystem dayNight, Camera worldCamera)
    {
        return building.CreateCitizenPopulation(dayNight, worldCamera);
    }

    public void BindCitizenPopulation(
        Result building,
        DayNightSystem dayNight,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationSystem citizenPopulation)
    {
        building.BindCitizenPopulation(
            dayNight,
            selectionUiCameraSystem,
            selectionBuildingInteractionSystem,
            citizenPopulation);
    }
}
