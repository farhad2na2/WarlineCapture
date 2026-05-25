using Game.Scripts.UI;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class ManagedGameplayStartupSystem
{
    private readonly BuildingGameplayCompositionSystem _buildingGameplayCompositionSystem = new();
    private readonly SelectionGameplayStartupSystem _selectionGameplayStartupSystem = new();

    public readonly struct Result
    {
        public readonly DayNightSystem DayNight;
        public readonly FactionVisualSettings FactionVisuals;
        public readonly RoadBuildSystem RoadBuild;
        public readonly BuildingSelectionClickSystem BuildingSelectionClick;
        public readonly BuildingSelectionClickSystem.Context BuildingSelectionClickContext;
        public readonly BuildingRuntimeCitySpawnSystem BuildingRuntimeCitySpawn;
        public readonly BuildingRuntimeCitySpawnSystem.Context BuildingRuntimeCitySpawnContext;
        public readonly BuildingUiCommandSystem BuildingUiCommand;
        public readonly BuildingUiCommandSystem.Context BuildingUiCommandContext;
        public readonly BuildingUiQuerySystem BuildingUiQuery;
        public readonly BuildingUiQuerySystem.Context BuildingUiQueryContext;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteraction;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly System.Action<MainMenuPlayUI> BindBuildingMainMenu;
        public readonly System.Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCitySpawnerSystem, CitizenPopulationSystem> BindBuildingGameplayFeatures;
        public readonly System.Action DisposeBuildingGameplay;
        public readonly BuildingRuntimeUpdateSystem BuildingRuntimeUpdate;
        public readonly BuildingRuntimeUpdateSystem.Context BuildingRuntimeUpdateContext;
        public readonly System.Action<MainMenuPlayUI> BindSelectionMainMenu;
        public readonly System.Action SelectionRuntimeUpdate;
        public readonly System.Action DisposeSelection;
        public readonly SelectionUiCommandSystem SelectionUiCommand;
        public readonly SelectionUiReadModelSystem SelectionUiReadModel;
        public readonly SelectionUiCameraSystem SelectionUiCamera;
        public readonly SelectionBuildingInteractionSystem SelectionBuildingInteraction;
        public readonly SelectionScreenMarkerSystem SelectionScreenMarkers;
        public readonly SelectionRectangleView SelectionRectangleView;
        public readonly UnitAttackTraceSystem UnitAttackTraces;
        public readonly UnitImpostorRenderSystem UnitImpostors;
        public readonly CitizenPopulationSystem CitizenPopulation;

        public Result(
            DayNightSystem dayNight,
            FactionVisualSettings factionVisuals,
            RoadBuildSystem roadBuild,
            BuildingSelectionClickSystem buildingSelectionClick,
            BuildingSelectionClickSystem.Context buildingSelectionClickContext,
            BuildingRuntimeCitySpawnSystem buildingRuntimeCitySpawn,
            BuildingRuntimeCitySpawnSystem.Context buildingRuntimeCitySpawnContext,
            BuildingUiCommandSystem buildingUiCommand,
            BuildingUiCommandSystem.Context buildingUiCommandContext,
            BuildingUiQuerySystem buildingUiQuery,
            BuildingUiQuerySystem.Context buildingUiQueryContext,
            BuildingPlacementInteractionSystem buildingPlacementInteraction,
            BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
            System.Action<MainMenuPlayUI> bindBuildingMainMenu,
            System.Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCitySpawnerSystem, CitizenPopulationSystem> bindBuildingGameplayFeatures,
            System.Action disposeBuildingGameplay,
            BuildingRuntimeUpdateSystem buildingRuntimeUpdate,
            BuildingRuntimeUpdateSystem.Context buildingRuntimeUpdateContext,
            System.Action<MainMenuPlayUI> bindSelectionMainMenu,
            System.Action selectionRuntimeUpdate,
            System.Action disposeSelection,
            SelectionUiCommandSystem selectionUiCommand,
            SelectionUiReadModelSystem selectionUiReadModel,
            SelectionUiCameraSystem selectionUiCamera,
            SelectionBuildingInteractionSystem selectionBuildingInteraction,
            SelectionScreenMarkerSystem selectionScreenMarkers,
            SelectionRectangleView selectionRectangleView,
            UnitAttackTraceSystem unitAttackTraces,
            UnitImpostorRenderSystem unitImpostors,
            CitizenPopulationSystem citizenPopulation)
        {
            DayNight = dayNight;
            FactionVisuals = factionVisuals;
            RoadBuild = roadBuild;
            BuildingSelectionClick = buildingSelectionClick;
            BuildingSelectionClickContext = buildingSelectionClickContext;
            BuildingRuntimeCitySpawn = buildingRuntimeCitySpawn;
            BuildingRuntimeCitySpawnContext = buildingRuntimeCitySpawnContext;
            BuildingUiCommand = buildingUiCommand;
            BuildingUiCommandContext = buildingUiCommandContext;
            BuildingUiQuery = buildingUiQuery;
            BuildingUiQueryContext = buildingUiQueryContext;
            BuildingPlacementInteraction = buildingPlacementInteraction;
            BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
            BindBuildingMainMenu = bindBuildingMainMenu;
            BindBuildingGameplayFeatures = bindBuildingGameplayFeatures;
            DisposeBuildingGameplay = disposeBuildingGameplay;
            BuildingRuntimeUpdate = buildingRuntimeUpdate;
            BuildingRuntimeUpdateContext = buildingRuntimeUpdateContext;
            BindSelectionMainMenu = bindSelectionMainMenu;
            SelectionRuntimeUpdate = selectionRuntimeUpdate;
            DisposeSelection = disposeSelection;
            SelectionUiCommand = selectionUiCommand;
            SelectionUiReadModel = selectionUiReadModel;
            SelectionUiCamera = selectionUiCamera;
            SelectionBuildingInteraction = selectionBuildingInteraction;
            SelectionScreenMarkers = selectionScreenMarkers;
            SelectionRectangleView = selectionRectangleView;
            UnitAttackTraces = unitAttackTraces;
            UnitImpostors = unitImpostors;
            CitizenPopulation = citizenPopulation;
        }
    }

    public Result Initialize(
        DayNightSystemConfig dayNightConfig,
        FactionVisualSettingsConfig factionVisualConfig,
        RoadBuildSystemConfig roadBuildConfig,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        RTSSelectionSystemConfig rtsSelectionConfig,
        UnitAttackTraceSystemConfig unitAttackTraceConfig,
        GameStringsConfig gameStringsConfig,
        PrefabPreviewCameraConfig prefabPreviewCameraConfig,
        Camera worldCamera,
        Light directionalLight,
        Volume globalVolume,
        Transform runtimeUiRoot,
        int ownerLayer)
    {
        var dayNight = new DayNightSystem();
        dayNight.Init(dayNightConfig, directionalLight, globalVolume);

        var factionVisuals = new FactionVisualSettings();
        factionVisuals.Init(factionVisualConfig);

        var roadBuild = new RoadBuildSystem();
        roadBuild.Init(roadBuildConfig, worldCamera, runtimeUiRoot, null);

        BuildingGameplayCompositionSystem.Result building = _buildingGameplayCompositionSystem.Initialize(
            buildingPlacementConfig,
            worldCamera,
            runtimeUiRoot,
            roadBuild,
            factionVisuals,
            dayNight);

        SelectionGameplayStartupSystem.Result selection = _selectionGameplayStartupSystem.Initialize(
            rtsSelectionConfig,
            worldCamera,
            runtimeUiRoot,
            roadBuild,
            building.Interaction,
            building.InteractionContext,
            factionVisuals);

        roadBuild.BindDependencies(
            building.Interaction,
            building.InteractionContext);
        _buildingGameplayCompositionSystem.BindSelection(
            building,
            roadBuild,
            dayNight,
            selection.SelectionUiCamera,
            selection.SelectionBuildingInteraction);

        var unitAttackTraces = new UnitAttackTraceSystem();
        unitAttackTraces.Init(unitAttackTraceConfig, worldCamera, ownerLayer, factionVisuals);

        var unitImpostors = new UnitImpostorRenderSystem();
        unitImpostors.Init(worldCamera, ownerLayer, buildingPlacementConfig != null ? buildingPlacementConfig.UnitPrefabRegistryConfig : null);

        CitizenPopulationSystem citizenPopulation = _buildingGameplayCompositionSystem.CreateCitizenPopulation(
            building,
            dayNight,
            worldCamera);
        _buildingGameplayCompositionSystem.BindCitizenPopulation(
            building,
            roadBuild,
            dayNight,
            selection.SelectionUiCamera,
            selection.SelectionBuildingInteraction,
            citizenPopulation);

        GameStrings.Init(gameStringsConfig);
        SharedPrefabPreviewCache.Init(prefabPreviewCameraConfig);

        return new Result(
            dayNight,
            factionVisuals,
            roadBuild,
            building.SelectionClick,
            building.SelectionClickContext,
            building.RuntimeCitySpawn,
            building.RuntimeCitySpawnContext,
            building.UiCommand,
            building.UiCommandContext,
            building.UiQuery,
            building.UiQueryContext,
            building.Interaction,
            building.InteractionContext,
            building.BindMainMenu,
            building.BindGameplayFeatures,
            building.Dispose,
            building.RuntimeUpdate,
            building.RuntimeUpdateContext,
            selection.BindSelectionMainMenu,
            selection.SelectionRuntimeUpdate,
            selection.DisposeSelection,
            selection.SelectionUiCommand,
            selection.SelectionUiReadModel,
            selection.SelectionUiCamera,
            selection.SelectionBuildingInteraction,
            selection.SelectionScreenMarkers,
            selection.SelectionRectangleView,
            unitAttackTraces,
            unitImpostors,
            citizenPopulation);
    }
}
