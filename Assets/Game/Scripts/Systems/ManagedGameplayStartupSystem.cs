using Game.Scripts.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class ManagedGameplayStartupSystem
{
    private readonly RoadBuildCompositionSystem _roadBuildCompositionSystem = new();
    private readonly BuildingGameplayCompositionSystem _buildingGameplayCompositionSystem = new();
    private readonly SelectionGameplayStartupSystem _selectionGameplayStartupSystem = new();

    public readonly struct Result
    {
        public readonly DayNightSystem DayNight;
        public readonly FactionVisualSettings FactionVisuals;
        public readonly RoadBuildReadModelSystem RoadBuildReadModel;
        public readonly RoadRuntimeGenerationSystem RoadRuntimeGeneration;
        public readonly RoadRuntimeGenerationSystem.Context RoadRuntimeGenerationContext;
        public readonly System.Action RoadRuntimeUpdate;
        public readonly System.Action RoadOnGui;
        public readonly System.Action DisposeRoad;
        public readonly System.Action<MainMenuPlayUI> BindRoadMainMenu;
        public readonly System.Action<MainMenuPlayUI, RuntimeGridBlockerSystem> BindRoadGameplayFeatures;
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
        public readonly System.Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> BindBuildingGameplayFeatures;
        public readonly System.Action DisposeBuildingGameplay;
        public readonly BuildingRuntimeUpdateSystem BuildingRuntimeUpdate;
        public readonly BuildingRuntimeUpdateSystem.Context BuildingRuntimeUpdateContext;
        public readonly System.Action<MainMenuPlayUI> BindSelectionMainMenu;
        public readonly System.Action<MatchHudSelectionPanelView> BindMatchHudSelectionPanel;
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
        public readonly CitizenPopulationCompositionSystem.Result CitizenPopulationComposition;
        public readonly System.Action DisposeCitizenPopulation;

        public Result(
            DayNightSystem dayNight,
            FactionVisualSettings factionVisuals,
            RoadBuildReadModelSystem roadBuildReadModel,
            RoadRuntimeGenerationSystem roadRuntimeGeneration,
            RoadRuntimeGenerationSystem.Context roadRuntimeGenerationContext,
            System.Action roadRuntimeUpdate,
            System.Action roadOnGui,
            System.Action disposeRoad,
            System.Action<MainMenuPlayUI> bindRoadMainMenu,
            System.Action<MainMenuPlayUI, RuntimeGridBlockerSystem> bindRoadGameplayFeatures,
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
            System.Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> bindBuildingGameplayFeatures,
            System.Action disposeBuildingGameplay,
            BuildingRuntimeUpdateSystem buildingRuntimeUpdate,
            BuildingRuntimeUpdateSystem.Context buildingRuntimeUpdateContext,
            System.Action<MainMenuPlayUI> bindSelectionMainMenu,
            System.Action<MatchHudSelectionPanelView> bindMatchHudSelectionPanel,
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
            CitizenPopulationCompositionSystem.Result citizenPopulationComposition,
            System.Action disposeCitizenPopulation)
        {
            DayNight = dayNight;
            FactionVisuals = factionVisuals;
            RoadBuildReadModel = roadBuildReadModel;
            RoadRuntimeGeneration = roadRuntimeGeneration;
            RoadRuntimeGenerationContext = roadRuntimeGenerationContext;
            RoadRuntimeUpdate = roadRuntimeUpdate;
            RoadOnGui = roadOnGui;
            DisposeRoad = disposeRoad;
            BindRoadMainMenu = bindRoadMainMenu;
            BindRoadGameplayFeatures = bindRoadGameplayFeatures;
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
            BindMatchHudSelectionPanel = bindMatchHudSelectionPanel;
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
            CitizenPopulationComposition = citizenPopulationComposition;
            DisposeCitizenPopulation = disposeCitizenPopulation;
        }
    }

    public Result Initialize(
        DayNightSystemConfig dayNightConfig,
        FactionVisualSettingsConfig factionVisualConfig,
        RoadBuildSystemConfig roadBuildConfig,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        MapBuildingPlacementConfig mapBuildingPlacementConfig,
        RTSSelectionSystemConfig rtsSelectionConfig,
        UnitAttackTraceSystemConfig unitAttackTraceConfig,
        RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig,
        GameStringsConfig gameStringsConfig,
        PrefabPreviewCameraConfig prefabPreviewCameraConfig,
        Camera worldCamera,
        Light directionalLight,
        Volume globalVolume,
        Transform runtimeUiRoot,
        Transform mapBuildingAuthoringRoot,
        int ownerLayer)
    {
        var dayNight = new DayNightSystem();
        dayNight.Init(dayNightConfig, directionalLight, globalVolume);

        var factionVisuals = new FactionVisualSettings();
        factionVisuals.Init(factionVisualConfig);

        RoadBuildCompositionSystem.Result road = _roadBuildCompositionSystem.Initialize(
            roadBuildConfig,
            worldCamera,
            runtimeUiRoot);
        RoadBuildReadModelSystem roadBuildReadModel = road.RoadBuildReadModel;

        BuildingGameplayCompositionResultSystem.Result building = _buildingGameplayCompositionSystem.Initialize(
            buildingPlacementConfig,
            worldCamera,
            runtimeUiRoot,
            road.RoadFootprintQuery,
            road.RoadFootprintQueryContext,
            factionVisuals,
            dayNight,
            rtsSelectionConfig,
            mapBuildingPlacementConfig,
            mapBuildingAuthoringRoot);

        Sprite ResolveSelectionPortraitSprite(EntityManager em, Entity entity)
        {
            return building.UiQuery.TryResolveLiveUnitPreviewPrefab(building.UiQueryContext, entity, out GameObject prefab)
                ? SelectionPortraitSpriteResolverSystem.ResolveSelectionPortraitSprite(prefab)
                : null;
        }

        Sprite ResolveSelectionCardPortraitSprite(EntityManager em, Entity entity)
        {
            return building.UiQuery.TryResolveLiveUnitPreviewPrefab(building.UiQueryContext, entity, out GameObject prefab)
                ? SelectionPortraitSpriteResolverSystem.ResolveSelectionCardPortraitSprite(prefab)
                : null;
        }

        Sprite ResolveSelectedBuildingPortraitSprite()
        {
            return building.UiQuery.TryGetSelectedBuildingPreviewPrefab(building.UiQueryContext, out GameObject prefab)
                ? SelectionPortraitSpriteResolverSystem.ResolveSelectionPortraitSprite(prefab)
                : null;
        }

        SelectionGameplayStartupSystem.Result selection = _selectionGameplayStartupSystem.Initialize(
            rtsSelectionConfig,
            worldCamera,
            runtimeUiRoot,
            roadBuildReadModel,
            building.Interaction,
            building.InteractionContext,
            building.TrySelectFirstBuildingInScreenRect,
            ResolveSelectionPortraitSprite,
            ResolveSelectionCardPortraitSprite,
            ResolveSelectedBuildingPortraitSprite,
            factionVisuals);

        _roadBuildCompositionSystem.BindBuildingInteraction(
            road,
            building.Interaction,
            building.InteractionContext);
        building.BindSelection(
            dayNight,
            selection.SelectionUiCamera,
            selection.SelectionBuildingInteraction);

        var unitAttackTraces = new UnitAttackTraceSystem();
        unitAttackTraces.Init(unitAttackTraceConfig, worldCamera, ownerLayer, factionVisuals);

        var unitImpostors = new UnitImpostorRenderSystem();
        unitImpostors.Init(worldCamera, ownerLayer, buildingPlacementConfig != null ? buildingPlacementConfig.UnitPrefabRegistryConfig : null);

        building.InitializeCitizenPopulation(dayNight, worldCamera, runtimeCitySpawnerConfig);
        building.BindCitizenPopulation(
            dayNight,
            selection.SelectionUiCamera,
            selection.SelectionBuildingInteraction,
            building.CitizenPopulationComposition.EventSystem);

        GameStrings.Init(gameStringsConfig);
        SharedPrefabPreviewCache.Init(prefabPreviewCameraConfig);
        System.Action<MainMenuPlayUI> bindRoadMainMenu = mainMenu =>
            _roadBuildCompositionSystem.BindMainMenu(
                road,
                building.Interaction,
                building.InteractionContext,
                mainMenu);
        System.Action<MainMenuPlayUI, RuntimeGridBlockerSystem> bindRoadGameplayFeatures = (mainMenu, runtimeGridBlockers) =>
            _roadBuildCompositionSystem.BindRuntimeGameplayFeatures(
                road,
                building.Interaction,
                building.InteractionContext,
                mainMenu,
                runtimeGridBlockers);

        return new Result(
            dayNight,
            factionVisuals,
            roadBuildReadModel,
            road.RoadRuntimeGeneration,
            road.RoadRuntimeGenerationContext,
            road.RuntimeUpdate,
            road.OnGui,
            road.Dispose,
            bindRoadMainMenu,
            bindRoadGameplayFeatures,
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
            selection.BindMatchHudSelectionPanel,
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
            building.CitizenPopulationComposition,
            building.DisposeCitizenPopulation);
    }
}
