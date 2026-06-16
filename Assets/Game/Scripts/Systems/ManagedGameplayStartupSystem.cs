using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed partial class ManagedGameplayStartupSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

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
        public readonly System.Action<IMatchRuntimeUi> BindRoadMainMenu;
        public readonly System.Action<IMatchRuntimeUi, RuntimeGridBlockerSystem> BindRoadGameplayFeatures;
        public readonly BuildingSelectionClickSystem BuildingSelectionClick;
        public readonly BuildingSelectionClickSystem.Context BuildingSelectionClickContext;
        public readonly BuildingRuntimeCitySpawnSystem BuildingRuntimeCitySpawn;
        public readonly BuildingRuntimeCitySpawnSystem.Context BuildingRuntimeCitySpawnContext;
        public readonly BuildingUiCommandBoundary BuildingUiCommand;
        public readonly BuildingUiCommandBoundary.Context BuildingUiCommandContext;
        public readonly BuildingUiQuerySystem BuildingUiQuery;
        public readonly BuildingUiQuerySystem.Context BuildingUiQueryContext;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteraction;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly System.Action<IMatchRuntimeUi> BindBuildingMainMenu;
        public readonly System.Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> BindBuildingGameplayFeatures;
        public readonly System.Action DisposeBuildingGameplay;
        public readonly BuildingRuntimeUpdateSystem BuildingRuntimeUpdate;
        public readonly BuildingRuntimeUpdateSystem.Context BuildingRuntimeUpdateContext;
        public readonly System.Action<IMatchRuntimeUi> BindSelectionMainMenu;
        public readonly System.Action<IMatchHudSelectionPanelView> BindMatchHudSelectionPanel;
        public readonly System.Action SelectionRuntimeUpdate;
        public readonly System.Action DisposeSelection;
        public readonly SelectionUiCommandSystem SelectionUiCommand;
        public readonly SelectionUiReadModelSystem SelectionUiReadModel;
        public readonly SelectionUiCameraSystem SelectionUiCamera;
        public readonly SelectionBuildingInteractionSystem SelectionBuildingInteraction;
        public readonly SelectionScreenMarkerSystem SelectionScreenMarkers;
        public readonly ISelectionRectangleView SelectionRectangleView;
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
            System.Action<IMatchRuntimeUi> bindRoadMainMenu,
            System.Action<IMatchRuntimeUi, RuntimeGridBlockerSystem> bindRoadGameplayFeatures,
            BuildingSelectionClickSystem buildingSelectionClick,
            BuildingSelectionClickSystem.Context buildingSelectionClickContext,
            BuildingRuntimeCitySpawnSystem buildingRuntimeCitySpawn,
            BuildingRuntimeCitySpawnSystem.Context buildingRuntimeCitySpawnContext,
            BuildingUiCommandBoundary buildingUiCommand,
            BuildingUiCommandBoundary.Context buildingUiCommandContext,
            BuildingUiQuerySystem buildingUiQuery,
            BuildingUiQuerySystem.Context buildingUiQueryContext,
            BuildingPlacementInteractionSystem buildingPlacementInteraction,
            BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
            System.Action<IMatchRuntimeUi> bindBuildingMainMenu,
            System.Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> bindBuildingGameplayFeatures,
            System.Action disposeBuildingGameplay,
            BuildingRuntimeUpdateSystem buildingRuntimeUpdate,
            BuildingRuntimeUpdateSystem.Context buildingRuntimeUpdateContext,
            System.Action<IMatchRuntimeUi> bindSelectionMainMenu,
            System.Action<IMatchHudSelectionPanelView> bindMatchHudSelectionPanel,
            System.Action selectionRuntimeUpdate,
            System.Action disposeSelection,
            SelectionUiCommandSystem selectionUiCommand,
            SelectionUiReadModelSystem selectionUiReadModel,
            SelectionUiCameraSystem selectionUiCamera,
            SelectionBuildingInteractionSystem selectionBuildingInteraction,
            SelectionScreenMarkerSystem selectionScreenMarkers,
            ISelectionRectangleView selectionRectangleView,
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
        MapVehiclePlacementConfig mapVehiclePlacementConfig,
        RTSSelectionSystemConfig rtsSelectionConfig,
        RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig,
        GameStringsConfig gameStringsConfig,
        Camera worldCamera,
        Light directionalLight,
        Volume globalVolume,
        Transform runtimeTransportsRoot,
        Transform runtimeUiRoot,
        System.Func<Transform, RTSSelectionSystemConfig, ISelectionRectangleView> createSelectionRectangleView,
        System.Func<GameObject, Sprite> resolveSelectionPortraitSpriteFromPrefab,
        System.Func<GameObject, Sprite> resolveSelectionCardPortraitSpriteFromPrefab,
        BuildingProductionSystem.TryGetUnitProductionMetadataDelegate tryGetUnitProductionMetadata,
        BuildingProductionTransportSystem.PrepareTransportDropVisualDelegate prepareTransportDropVisual,
        BuildingSpawnPrefabSystem.ResolveSpawnableLookupKeyDelegate resolveSpawnableLookupKey,
        BuildingDefinitionSystem.TryGetBuildingDefinitionMetadataDelegate tryGetBuildingDefinitionMetadata,
        BuildingDefinitionSystem.TryGetUnitDefinitionMetadataDelegate tryGetUnitDefinitionMetadata,
        Transform mapBuildingAuthoringRoot,
        Transform mapVehicleAuthoringRoot,
        IMatchIntroStateQuery matchIntroStateQuery)
    {
        DayNightSystem dayNight = ResolveDayNightSystem();
        dayNight?.Init(dayNightConfig, directionalLight, globalVolume);

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
            runtimeTransportsRoot,
            runtimeUiRoot,
            road.RoadFootprintState,
            factionVisuals,
            dayNight,
            rtsSelectionConfig,
            mapBuildingPlacementConfig,
            mapVehiclePlacementConfig,
            mapBuildingAuthoringRoot,
            mapVehicleAuthoringRoot,
            resolveSelectionPortraitSpriteFromPrefab,
            tryGetUnitProductionMetadata,
            prepareTransportDropVisual,
            resolveSpawnableLookupKey,
            tryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata);

        Sprite ResolveSelectionPortraitSprite(EntityManager em, Entity entity)
        {
            return building.UiQuery.TryResolveLiveUnitPreviewPrefab(building.UiQueryContext, entity, out GameObject prefab)
                ? resolveSelectionPortraitSpriteFromPrefab?.Invoke(prefab)
                : null;
        }

        Sprite ResolveSelectionCardPortraitSprite(EntityManager em, Entity entity)
        {
            return building.UiQuery.TryResolveLiveUnitPreviewPrefab(building.UiQueryContext, entity, out GameObject prefab)
                ? resolveSelectionCardPortraitSpriteFromPrefab?.Invoke(prefab)
                : null;
        }

        Sprite ResolveSelectedBuildingPortraitSprite()
        {
            return building.UiQuery.TryGetSelectedBuildingPreviewPrefab(building.UiQueryContext, out GameObject prefab)
                ? resolveSelectionPortraitSpriteFromPrefab?.Invoke(prefab)
                : null;
        }

        bool TryResolveRuntimeBuildingInstance(Entity combatEntity, int runtimeBuildingId, out GameObject instance)
        {
            instance = null;
            if (building.RuntimeBuildings == null)
            {
                return false;
            }

            if (runtimeBuildingId > 0 &&
                building.RuntimeBuildings.TryGetValue(runtimeBuildingId, out RuntimeBuildingEntity runtimeBuilding) &&
                TryResolveRuntimeBuildingGameObject(runtimeBuilding, out instance))
            {
                return true;
            }

            foreach (RuntimeBuildingEntity candidateBuilding in building.RuntimeBuildings.Values)
            {
                if (candidateBuilding == null || candidateBuilding.CombatEntity != combatEntity)
                    continue;

                return TryResolveRuntimeBuildingGameObject(candidateBuilding, out instance);
            }

            return false;
        }

        static bool TryResolveRuntimeBuildingGameObject(RuntimeBuildingEntity runtimeBuilding, out GameObject instance)
        {
            instance = null;
            if (runtimeBuilding == null ||
                runtimeBuilding.IsDestroyed ||
                runtimeBuilding.Instance == null)
            {
                return false;
            }

            instance = runtimeBuilding.Instance;
            return true;
        }

        SelectionGameplayStartupSystem.Result selection = _selectionGameplayStartupSystem.Initialize(
            rtsSelectionConfig,
            worldCamera,
            runtimeUiRoot,
            createSelectionRectangleView,
            roadBuildReadModel,
            building.Interaction,
            building.InteractionContext,
            building.TrySelectFirstBuildingInScreenRect,
            ResolveSelectionPortraitSprite,
            ResolveSelectionCardPortraitSprite,
            ResolveSelectedBuildingPortraitSprite,
            TryResolveRuntimeBuildingInstance,
            factionVisuals,
            matchIntroStateQuery);

        _roadBuildCompositionSystem.BindBuildingInteraction(
            road,
            building.Interaction,
            building.InteractionContext);
        building.BindSelection(
            dayNight,
            selection.SelectionUiCamera,
            selection.SelectionBuildingInteraction,
            selection.ShouldBlockBuildingSelectionClick);

        building.InitializeCitizenPopulation(dayNight, worldCamera, runtimeCitySpawnerConfig);
        building.BindCitizenPopulation(
            dayNight,
            selection.SelectionUiCamera,
            selection.SelectionBuildingInteraction,
            building.CitizenPopulationComposition.EventSystem);

        GameStrings.Init(gameStringsConfig);
        System.Action<IMatchRuntimeUi> bindRoadMainMenu = mainMenu =>
            _roadBuildCompositionSystem.BindMainMenu(
                road,
                building.Interaction,
                building.InteractionContext,
                mainMenu);
        System.Action<IMatchRuntimeUi, RuntimeGridBlockerSystem> bindRoadGameplayFeatures = (mainMenu, runtimeGridBlockers) =>
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
            building.CitizenPopulationComposition,
            building.DisposeCitizenPopulation);
    }

    private static DayNightSystem ResolveDayNightSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<DayNightSystem>()
            : null;
    }
}
