using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Game.Scripts.UI;

internal sealed class MatchBootstrapSystem
{
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RuntimeCameraReferenceSystem _runtimeCameraReferenceSystem = new();
    private readonly AIStartupSystem _aiStartupSystem = new();
    private readonly MissionStartupSystem _missionStartupSystem = new();
    private readonly InitialFactionSpawnCellSystem _initialFactionSpawnCellSystem = new();
    private readonly GameplaySceneBindingSystem _gameplaySceneBindingSystem = new();
    private readonly RuntimeRootSystem _runtimeRootSystem = new();
    private readonly MenuStartupSystem _menuStartupSystem = new();
    private readonly GameplayFeatureStartupSystem _gameplayFeatureStartupSystem = new();
    private readonly RuntimeGridBootstrapSystem _runtimeGridBootstrapSystem = new();
    private readonly MapSurfaceRuntimeBootstrapSystem _mapSurfaceRuntimeBootstrapSystem = new();
    private readonly CustomGameStartupSystem _customGameStartupSystem = new();
    private readonly MatchSceneReferenceSystem _matchSceneReferenceSystem = new();
    private readonly PerformanceDiagnosticsReferenceSystem _performanceDiagnosticsReferenceSystem = new();

    private readonly ManagedGameplayStartupSystem managedGameplayStartupSystem = new();
    private readonly GameplayRuntimeUpdateSystem gameplayRuntimeUpdateSystem = new();
    private readonly PerformanceDiagnosticsSystem fallbackPerformanceDiagnosticsSystem = new();
    private bool fallbackPerformanceDiagnosticsInitialized;
    private MatchSceneView sceneView;

    public MatchSceneView SceneView => sceneView;
    public bool HasSceneView => sceneView != null;
    private MatchSceneView MatchScene => sceneView;

    private MenuView MenuView => MatchScene != null ? MatchScene.MenuView : null;
    public Camera WorldCamera => MatchScene != null ? MatchScene.WorldCamera : null;
    public Light DirectionalLight => MatchScene != null ? MatchScene.DirectionalLight : null;
    public Volume GlobalVolume => MatchScene != null ? MatchScene.GlobalVolume : null;
    public CombinedMeshBaker DecorationCombinedMeshBaker => MatchScene != null ? MatchScene.DecorationCombinedMeshBaker : null;
    public Transform DecorationRoot => MatchScene != null ? MatchScene.DecorationRoot : null;
    private GameObject[] LegacyVisualRootsDisabledForM01 => MatchScene != null ? MatchScene.LegacyVisualRootsDisabledForM01 : Array.Empty<GameObject>();

    public RTSSelectionSystemConfig RtsSelectionConfig => MatchScene != null ? MatchScene.RtsSelectionConfig : null;
    public RoadBuildSystemConfig RoadBuildConfig => MatchScene != null ? MatchScene.RoadBuildConfig : null;
    public BuildingPlacementSystemConfig BuildingPlacementConfig => MatchScene != null ? MatchScene.BuildingPlacementConfig : null;
    public UnitAttackTraceSystemConfig UnitAttackTraceConfig => MatchScene != null ? MatchScene.UnitAttackTraceConfig : null;
    public RuntimeCitySpawnerSystemConfig RuntimeCitySpawnerConfig => MatchScene != null ? MatchScene.RuntimeCitySpawnerConfig : null;
    public RuntimeDecorationSpawnerSystemConfig RuntimeDecorationSpawnerConfig => MatchScene != null ? MatchScene.RuntimeDecorationSpawnerConfig : null;
    public RuntimeGridBlockerSystemConfig RuntimeGridBlockerConfig => MatchScene != null ? MatchScene.RuntimeGridBlockerConfig : null;
    public DayNightSystemConfig DayNightConfig => MatchScene != null ? MatchScene.DayNightConfig : null;
    public GameStringsConfig GameStringsConfig => MatchScene != null ? MatchScene.GameStringsConfig : null;
    public AIPlanEntryStartupConfig AIPlanEntryConfig => MatchScene != null ? MatchScene.AIPlanEntryConfig : null;
    public IReadOnlyList<AIControllerConfig> AIControllerConfigs => MatchScene != null ? MatchScene.AIControllerConfigs : Array.Empty<AIControllerConfig>();
    private FactionVisualSettingsConfig FactionVisualConfig => MatchScene != null ? MatchScene.FactionVisualConfig : null;
    private PrefabPreviewCameraConfig PrefabPreviewCameraConfig => MatchScene != null ? MatchScene.PrefabPreviewCameraConfig : null;
    private GridAuthoringConfig RuntimeGridConfig => MatchScene != null ? MatchScene.RuntimeGridConfig : null;
    private MapSurfaceAuthoring MapSurfaceAuthoring => MatchScene != null ? MatchScene.MapSurfaceAuthoring : null;

    public RuntimeGridBlockerSystem RuntimeGridBlockers { get; private set; }
    public RuntimeDecorationSpawnerSystem RuntimeDecorations { get; private set; }
    public RuntimeCityCompositionSystem RuntimeCity { get; private set; }
    public RoadBuildReadModelSystem RoadBuildReadModel { get; private set; }
    public BuildingSelectionClickSystem BuildingSelectionClick { get; private set; }
    public BuildingUiCommandSystem BuildingUiCommand { get; private set; }
    public BuildingUiQuerySystem BuildingUiQuery { get; private set; }
    public BuildingRuntimeUpdateSystem BuildingRuntimeUpdate { get; private set; }
    public SelectionUiCommandSystem SelectionUiCommand { get; private set; }
    public SelectionUiReadModelSystem SelectionUiReadModel { get; private set; }
    public SelectionUiCameraSystem SelectionUiCamera { get; private set; }
    public SelectionBuildingInteractionSystem SelectionBuildingInteraction { get; private set; }
    public SelectionScreenMarkerSystem SelectionScreenMarkers { get; private set; }
    public SelectionRectangleView SelectionRectangle { get; private set; }
    public MainMenuPlayUI MainMenu { get; private set; }
    public DayNightSystem DayNight { get; private set; }
    public FactionVisualSettings FactionVisuals { get; private set; }
    public UnitAttackTraceSystem UnitAttackTraces { get; private set; }
    public UnitImpostorRenderSystem UnitImpostors { get; private set; }
    public bool GameplayInitialized { get; private set; }
    public BuildingSelectionClickSystem.Context BuildingSelectionClickContext { get; private set; }
    private BuildingRuntimeCitySpawnSystem _buildingRuntimeCitySpawn;
    private BuildingRuntimeCitySpawnSystem.Context _buildingRuntimeCitySpawnContext;
    private BuildingUiCommandSystem.Context _buildingUiCommandContext;
    private BuildingUiQuerySystem.Context _buildingUiQueryContext;
    private BuildingPlacementInteractionSystem _buildingPlacementInteraction;
    private BuildingPlacementInteractionSystem.Context _buildingPlacementInteractionContext;
    private RoadRuntimeGenerationSystem _roadRuntimeGeneration;
    private RoadRuntimeGenerationSystem.Context _roadRuntimeGenerationContext;
    private Action _roadRuntimeUpdate;
    private Action _roadOnGui;
    private Action _disposeRoad;
    private Action<MainMenuPlayUI> _bindRoadMainMenu;
    private Action<MainMenuPlayUI, RuntimeGridBlockerSystem> _bindRoadGameplayFeatures;
    private Action<MainMenuPlayUI> _bindBuildingMainMenu;
    private Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> _bindBuildingGameplayFeatures;
    private Action<MainMenuPlayUI> _bindSelectionMainMenu;
    private Action _selectionRuntimeUpdate;
    private Action _citizenPopulationRuntimeUpdate;
    private Action _disposeCitizenPopulation;
    private CitizenPopulationReadModelSystem _citizenPopulationReadModel;
    private CitizenPopulationEventSystem _citizenPopulationEventSystem;
    private Action _disposeSelection;
    private Action _disposeBuildingGameplay;
    private BuildingRuntimeUpdateSystem.Context _buildingRuntimeUpdateContext;
    private Entity _buildingRuntimeBoundaryEntity;
    private PerformanceDiagnosticsSystem _performanceDiagnosticsSystem;
    private bool _gameplayStartPending;
    private Transform _runtimeBlockerRoot;
    private Transform _runtimeCityRoot;
    private Transform _runtimeUiRoot;

    public void Awake(MatchSceneView view, Transform ownerTransform, int ownerLayer)
    {
        Initialize(view);
        _matchSceneReferenceSystem.Register(view);
        _performanceDiagnosticsSystem = ResolvePerformanceDiagnosticsSystem();

        _runtimeRootSystem.Ensure(ownerTransform, ref _runtimeBlockerRoot, ref _runtimeCityRoot, ref _runtimeUiRoot);

        ManagedGameplayStartupSystem.Result managedSystems = InitializeManagedRuntime(
            DayNightConfig,
            FactionVisualConfig,
            RoadBuildConfig,
            BuildingPlacementConfig,
            RtsSelectionConfig,
            UnitAttackTraceConfig,
            GameStringsConfig,
            PrefabPreviewCameraConfig,
            WorldCamera,
            DirectionalLight,
            GlobalVolume,
            _runtimeUiRoot,
            ownerLayer);

        DayNight = managedSystems.DayNight;
        FactionVisuals = managedSystems.FactionVisuals;
        RoadBuildReadModel = managedSystems.RoadBuildReadModel;
        _roadRuntimeGeneration = managedSystems.RoadRuntimeGeneration;
        _roadRuntimeGenerationContext = managedSystems.RoadRuntimeGenerationContext;
        _roadRuntimeUpdate = managedSystems.RoadRuntimeUpdate;
        _roadOnGui = managedSystems.RoadOnGui;
        _disposeRoad = managedSystems.DisposeRoad;
        _bindRoadMainMenu = managedSystems.BindRoadMainMenu;
        _bindRoadGameplayFeatures = managedSystems.BindRoadGameplayFeatures;
        BuildingSelectionClick = managedSystems.BuildingSelectionClick;
        BuildingSelectionClickContext = managedSystems.BuildingSelectionClickContext;
        _buildingRuntimeCitySpawn = managedSystems.BuildingRuntimeCitySpawn;
        _buildingRuntimeCitySpawnContext = managedSystems.BuildingRuntimeCitySpawnContext;
        BuildingUiCommand = managedSystems.BuildingUiCommand;
        _buildingUiCommandContext = managedSystems.BuildingUiCommandContext;
        BuildingUiQuery = managedSystems.BuildingUiQuery;
        _buildingUiQueryContext = managedSystems.BuildingUiQueryContext;
        _buildingPlacementInteraction = managedSystems.BuildingPlacementInteraction;
        _buildingPlacementInteractionContext = managedSystems.BuildingPlacementInteractionContext;
        _bindBuildingMainMenu = managedSystems.BindBuildingMainMenu;
        _bindBuildingGameplayFeatures = managedSystems.BindBuildingGameplayFeatures;
        _bindSelectionMainMenu = managedSystems.BindSelectionMainMenu;
        _selectionRuntimeUpdate = managedSystems.SelectionRuntimeUpdate;
        _disposeSelection = managedSystems.DisposeSelection;
        _disposeBuildingGameplay = managedSystems.DisposeBuildingGameplay;
        BuildingRuntimeUpdate = managedSystems.BuildingRuntimeUpdate;
        _buildingRuntimeUpdateContext = managedSystems.BuildingRuntimeUpdateContext;
        SelectionUiCommand = managedSystems.SelectionUiCommand;
        SelectionUiReadModel = managedSystems.SelectionUiReadModel;
        SelectionUiCamera = managedSystems.SelectionUiCamera;
        SelectionBuildingInteraction = managedSystems.SelectionBuildingInteraction;
        SelectionScreenMarkers = managedSystems.SelectionScreenMarkers;
        SelectionRectangle = managedSystems.SelectionRectangleView;
        UnitAttackTraces = managedSystems.UnitAttackTraces;
        UnitImpostors = managedSystems.UnitImpostors;
        _disposeCitizenPopulation = managedSystems.DisposeCitizenPopulation;
        _citizenPopulationRuntimeUpdate = managedSystems.CitizenPopulationComposition != null
            ? managedSystems.CitizenPopulationComposition.RuntimeUpdateSystem.Update
            : null;
        _citizenPopulationReadModel = managedSystems.CitizenPopulationComposition?.ReadModel;
        _citizenPopulationEventSystem = managedSystems.CitizenPopulationComposition?.EventSystem;
        EnsureBuildingRuntimeBoundaryEntity();
        _runtimeCameraReferenceSystem.SetWorldCamera(WorldCamera);
    }

    public void Start()
    {
        _matchSceneReferenceSystem.Register(sceneView);
        MainMenu = _menuStartupSystem.Initialize(
            MenuView,
            BeginGameplay,
            _bindRoadMainMenu,
            BuildingUiCommand,
            _buildingUiCommandContext,
            BuildingUiQuery,
            _buildingUiQueryContext,
            _buildingPlacementInteraction,
            _buildingPlacementInteractionContext,
            _bindBuildingMainMenu,
            _bindSelectionMainMenu,
            SelectionUiCommand,
            SelectionUiReadModel,
            SelectionUiCamera,
            SelectionScreenMarkers,
            DayNight,
            _citizenPopulationReadModel,
            WorldCamera,
            _gameplaySceneBindingSystem,
            World.DefaultGameObjectInjectionWorld,
            Debug.LogException);
    }

    public void BeginGameplay()
    {
        GameRuntimeStats.Reset();
        ProjectRuntimeStartupConfig(
            World.DefaultGameObjectInjectionWorld,
            _runtimeGridBootstrapSystem,
            _mapSurfaceRuntimeBootstrapSystem,
            RuntimeGridConfig,
            MapSurfaceAuthoring,
            _initialFactionSpawnCellSystem,
            BuildingPlacementConfig,
            _aiStartupSystem,
            AIControllerConfigs);
        if (WarlineCaptureMissionSession.HasActiveMission)
        {
            LogRuntimeEcsBootstrapState("beforeMissionInit");
            _missionStartupSystem.Initialize(
                World.DefaultGameObjectInjectionWorld,
                WorldCamera,
                DayNight,
                LegacyVisualRootsDisabledForM01);
            LogRuntimeEcsBootstrapState("afterMissionInit");
        }
        else
        {
            _missionStartupSystem.ApplySkirmishSceneDefaults(DayNight, LegacyVisualRootsDisabledForM01);
            _customGameStartupSystem.InitializeFromLegacyConfigs(
                World.DefaultGameObjectInjectionWorld,
                BuildingPlacementConfig != null ? BuildingPlacementConfig.InitialUnitsConfig : null,
                BuildingPlacementConfig != null ? BuildingPlacementConfig.UnitPrefabRegistryConfig : null);
            Debug.Log("[SkirmishStart] Mission startup skipped because no active mission session is set.");
            LogRuntimeEcsBootstrapState("skirmishMissionSkipped");
        }
        AIStartupSystem.Result aiStartupResult = InitializeAiStartupConfig(
            World.DefaultGameObjectInjectionWorld,
            _aiStartupSystem,
            AIControllerConfigs,
            AIPlanEntryConfig,
            _initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell);
        if (aiStartupResult.HasPlayerAutoMode)
            _runtimeGameplayStateSystem.PlayerAutoModeEnabled = aiStartupResult.PlayerAutoModeEnabled;
        InitializeGameplaySystemsIfNeeded();
        _gameplayStartPending = true;
        _runtimeCameraReferenceSystem.SetWorldCamera(WorldCamera);
        _runtimeGameplayStateSystem.ResetForGameplayStart();
        _missionStartupSystem.FocusInitialCamera(
            World.DefaultGameObjectInjectionWorld,
            SelectionUiCamera,
            WorldCamera,
            _initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell,
            0);
    }

    public void Update()
    {
        UpdateRuntime(
            MenuView,
            GameplayInitialized,
            _runtimeGameplayStateSystem,
            _performanceDiagnosticsSystem,
            _missionStartupSystem,
            _roadRuntimeUpdate,
            BuildingRuntimeUpdate,
            _buildingRuntimeUpdateContext,
            _selectionRuntimeUpdate,
            WorldCamera,
            RuntimeCity,
            RuntimeGridBlockers,
            RuntimeDecorations,
            DayNight,
            _citizenPopulationRuntimeUpdate,
            MainMenu,
            UnitImpostors,
            ref _gameplayStartPending);
    }

    public void OnApplicationFocus(bool hasFocus)
    {
        OnApplicationFocus(_performanceDiagnosticsSystem, hasFocus);
    }

    public void OnApplicationPause(bool pauseStatus)
    {
        OnApplicationPause(_performanceDiagnosticsSystem, pauseStatus);
    }

    public void OnApplicationFocus(PerformanceDiagnosticsSystem performanceDiagnosticsSystem, bool hasFocus)
    {
        ForwardApplicationFocus(performanceDiagnosticsSystem, hasFocus);
    }

    public void OnApplicationPause(PerformanceDiagnosticsSystem performanceDiagnosticsSystem, bool pauseStatus)
    {
        ForwardApplicationPause(performanceDiagnosticsSystem, pauseStatus);
    }

    public void LateUpdate()
    {
        LateUpdateRuntime(
            GameplayInitialized,
            _runtimeGameplayStateSystem,
            _performanceDiagnosticsSystem,
            UnitAttackTraces,
            UnitImpostors);
    }

    public void OnGUI()
    {
        OnGuiRuntime(
            GameplayInitialized,
            _runtimeGameplayStateSystem,
            _performanceDiagnosticsSystem,
            _roadOnGui,
            SelectionRectangle);
    }

    public void OnDestroy()
    {
        ShutdownRuntime(
            _menuStartupSystem,
            MenuView,
            BeginGameplay,
            MainMenu,
            _disposeSelection,
            _disposeBuildingGameplay,
            _disposeRoad,
            UnitAttackTraces,
            UnitImpostors,
            _disposeCitizenPopulation,
            DayNight,
            RuntimeDecorations,
            RuntimeGridBlockers,
            RuntimeCity,
            _mapSurfaceRuntimeBootstrapSystem,
            _runtimeCameraReferenceSystem,
            _performanceDiagnosticsSystem);

        MainMenu = null;
        SelectionUiCommand = null;
        SelectionUiReadModel = null;
        SelectionUiCamera = null;
        SelectionBuildingInteraction = null;
        SelectionScreenMarkers = null;
        SelectionRectangle = null;
        BuildingSelectionClick = null;
        BuildingSelectionClickContext = default;
        _buildingRuntimeCitySpawn = null;
        _buildingRuntimeCitySpawnContext = default;
        BuildingUiCommand = null;
        _buildingUiCommandContext = default;
        BuildingUiQuery = null;
        _buildingUiQueryContext = default;
        _buildingPlacementInteraction = null;
        _buildingPlacementInteractionContext = default;
        _roadRuntimeGenerationContext = default;
        _roadRuntimeUpdate = null;
        _roadOnGui = null;
        _disposeRoad = null;
        _bindRoadMainMenu = null;
        _bindRoadGameplayFeatures = null;
        _bindBuildingMainMenu = null;
        _bindBuildingGameplayFeatures = null;
        _bindSelectionMainMenu = null;
        _selectionRuntimeUpdate = null;
        _citizenPopulationRuntimeUpdate = null;
        _disposeCitizenPopulation = null;
        _citizenPopulationReadModel = null;
        _citizenPopulationEventSystem = null;
        _disposeSelection = null;
        _disposeBuildingGameplay = null;
        BuildingRuntimeUpdate = null;
        _buildingRuntimeUpdateContext = default;
        _roadRuntimeGeneration = null;
        FactionVisuals = null;
        UnitAttackTraces = null;
        UnitImpostors = null;
        DayNight = null;
        RuntimeDecorations = null;
        RuntimeGridBlockers = null;
        RuntimeCity = null;
    }


    public void Initialize(MatchSceneView view)
    {
        sceneView = view;
    }

    public void Shutdown()
    {
        _matchSceneReferenceSystem.Clear(sceneView);
        sceneView = null;
    }

    public PerformanceDiagnosticsSystem ResolvePerformanceDiagnosticsSystem()
    {
        if (_performanceDiagnosticsReferenceSystem.TryGet(World.DefaultGameObjectInjectionWorld, out PerformanceDiagnosticsSystem persistentDiagnostics))
            return persistentDiagnostics;

        if (!fallbackPerformanceDiagnosticsInitialized)
        {
            Application.runInBackground = true;
            fallbackPerformanceDiagnosticsSystem.Initialize();
            fallbackPerformanceDiagnosticsInitialized = true;
        }

        return fallbackPerformanceDiagnosticsSystem;
    }

    private void ForwardApplicationFocus(PerformanceDiagnosticsSystem performanceDiagnosticsSystem, bool hasFocus)
    {
        if (fallbackPerformanceDiagnosticsInitialized && performanceDiagnosticsSystem == fallbackPerformanceDiagnosticsSystem)
            fallbackPerformanceDiagnosticsSystem.OnApplicationFocus(hasFocus);
    }

    private void ForwardApplicationPause(PerformanceDiagnosticsSystem performanceDiagnosticsSystem, bool pauseStatus)
    {
        if (fallbackPerformanceDiagnosticsInitialized && performanceDiagnosticsSystem == fallbackPerformanceDiagnosticsSystem)
            fallbackPerformanceDiagnosticsSystem.OnApplicationPause(pauseStatus);
    }

    public ManagedGameplayStartupSystem.Result InitializeManagedRuntime(
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
        return managedGameplayStartupSystem.Initialize(
            dayNightConfig,
            factionVisualConfig,
            roadBuildConfig,
            buildingPlacementConfig,
            rtsSelectionConfig,
            unitAttackTraceConfig,
            gameStringsConfig,
            prefabPreviewCameraConfig,
            worldCamera,
            directionalLight,
            globalVolume,
            runtimeUiRoot,
            ownerLayer);
    }

    public void ProjectRuntimeStartupConfig(
        World world,
        RuntimeGridBootstrapSystem runtimeGridBootstrapSystem,
        MapSurfaceRuntimeBootstrapSystem mapSurfaceRuntimeBootstrapSystem,
        GridAuthoringConfig runtimeGridConfig,
        MapSurfaceAuthoring mapSurfaceAuthoring,
        InitialFactionSpawnCellSystem initialFactionSpawnCellSystem,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        AIStartupSystem aiStartupSystem,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs)
    {
        if (runtimeGridConfig == null)
        {
            Debug.LogError("[MatchBootstrap] missingRuntimeGridConfig");
            return;
        }

        runtimeGridBootstrapSystem.Ensure(
            world,
            runtimeGridConfig.Width,
            runtimeGridConfig.Height,
            runtimeGridConfig.CellSize,
            runtimeGridConfig.Origin);
        mapSurfaceRuntimeBootstrapSystem.Ensure(world, mapSurfaceAuthoring);
        initialFactionSpawnCellSystem.Configure(
            world,
            buildingPlacementConfig != null ? buildingPlacementConfig.InitialUnitsConfig : null);
        aiStartupSystem.LogConfigValidation(aiControllerConfigs);
    }

    public AIStartupSystem.Result InitializeAiStartupConfig(
        World world,
        AIStartupSystem aiStartupSystem,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AIPlanEntryStartupConfig aiPlanEntryConfig,
        AIStartupSystem.TryResolveFactionSpawnCell tryResolveFactionSpawnCell)
    {
        return aiStartupSystem.Initialize(
            world,
            aiControllerConfigs,
            aiPlanEntryConfig,
            tryResolveFactionSpawnCell);
    }

    public void UpdateRuntime(
        MenuView menuView,
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        MissionStartupSystem missionStartupSystem,
        Action roadBuildRuntimeUpdate,
        BuildingRuntimeUpdateSystem buildingRuntimeUpdate,
        BuildingRuntimeUpdateSystem.Context buildingRuntimeUpdateContext,
        Action selectionRuntimeUpdate,
        Camera worldCamera,
        RuntimeCityCompositionSystem runtimeCity,
        RuntimeGridBlockerSystem runtimeGridBlockers,
        RuntimeDecorationSpawnerSystem runtimeDecorations,
        DayNightSystem dayNight,
        Action citizenPopulationRuntimeUpdate,
        MainMenuPlayUI mainMenu,
        UnitImpostorRenderSystem unitImpostors,
        ref bool gameplayStartPending)
    {
        gameplayRuntimeUpdateSystem.Update(
            menuView,
            gameplayInitialized,
            runtimeGameplayStateSystem,
            performanceDiagnosticsSystem,
            missionStartupSystem,
            roadBuildRuntimeUpdate,
            buildingRuntimeUpdate,
            buildingRuntimeUpdateContext,
            selectionRuntimeUpdate,
            worldCamera,
            runtimeCity,
            runtimeGridBlockers,
            runtimeDecorations,
            dayNight,
            citizenPopulationRuntimeUpdate,
            mainMenu,
            unitImpostors,
            ref gameplayStartPending);
    }

    public void LateUpdateRuntime(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        UnitAttackTraceSystem unitAttackTraces,
        UnitImpostorRenderSystem unitImpostors)
    {
        gameplayRuntimeUpdateSystem.LateUpdate(
            gameplayInitialized,
            runtimeGameplayStateSystem,
            performanceDiagnosticsSystem,
            unitAttackTraces,
            unitImpostors);
    }

    public void OnGuiRuntime(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        Action roadBuildOnGui,
        SelectionRectangleView selectionRectangleView)
    {
        gameplayRuntimeUpdateSystem.OnGui(
            gameplayInitialized,
            runtimeGameplayStateSystem,
            performanceDiagnosticsSystem,
            roadBuildOnGui,
            selectionRectangleView);
    }

    public void ShutdownRuntime(
        MenuStartupSystem menuStartupSystem,
        MenuView menuView,
        Action gameRequested,
        MainMenuPlayUI mainMenu,
        Action disposeSelection,
        Action disposeBuildingGameplay,
        Action disposeRoad,
        UnitAttackTraceSystem unitAttackTraces,
        UnitImpostorRenderSystem unitImpostors,
        Action disposeCitizenPopulation,
        DayNightSystem dayNight,
        RuntimeDecorationSpawnerSystem runtimeDecorations,
        RuntimeGridBlockerSystem runtimeGridBlockers,
        RuntimeCityCompositionSystem runtimeCity,
        MapSurfaceRuntimeBootstrapSystem mapSurfaceRuntimeBootstrapSystem,
        RuntimeCameraReferenceSystem runtimeCameraReferenceSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem)
    {
        Shutdown();
        menuStartupSystem?.Shutdown(menuView, gameRequested);
        mainMenu?.Dispose();
        disposeSelection?.Invoke();
        disposeBuildingGameplay?.Invoke();
        disposeRoad?.Invoke();
        unitAttackTraces?.Dispose();
        unitImpostors?.Dispose();
        disposeCitizenPopulation?.Invoke();
        dayNight?.Dispose();
        runtimeDecorations?.Dispose();
        runtimeGridBlockers?.Dispose();
        runtimeCity?.Dispose();
        mapSurfaceRuntimeBootstrapSystem?.Dispose(World.DefaultGameObjectInjectionWorld);
        runtimeCameraReferenceSystem?.ClearWorldCamera();
        ReleasePerformanceDiagnostics(performanceDiagnosticsSystem);
        SharedPrefabPreviewCache.ReleaseAll();
    }

    private void ReleasePerformanceDiagnostics(PerformanceDiagnosticsSystem performanceDiagnosticsSystem)
    {
        if (!fallbackPerformanceDiagnosticsInitialized ||
            performanceDiagnosticsSystem != fallbackPerformanceDiagnosticsSystem)
        {
            return;
        }

        fallbackPerformanceDiagnosticsSystem.Dispose();
        fallbackPerformanceDiagnosticsInitialized = false;
    }

    private void EnsureBuildingRuntimeBoundaryEntity()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        if (_buildingRuntimeBoundaryEntity == Entity.Null || !em.Exists(_buildingRuntimeBoundaryEntity))
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>());
            if (!query.IsEmptyIgnoreFilter)
            {
                _buildingRuntimeBoundaryEntity = query.GetSingletonEntity();
            }
            else
            {
                _buildingRuntimeBoundaryEntity = em.CreateEntity();
                em.SetName(_buildingRuntimeBoundaryEntity, "BuildingRuntimeBoundaryEntity");
            }
        }

        EnsureBuildingRuntimeBoundaryBuffers(em, _buildingRuntimeBoundaryEntity);
    }

    private static void EnsureBuildingRuntimeBoundaryBuffers(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<BuildingRuntimeBoundaryTag>(entity))
            em.AddComponent<BuildingRuntimeBoundaryTag>(entity);
        EnsureBuffer<BuildingConfiguredSpawnableReadModel>(em, entity);
        EnsureBuffer<BuildingConfiguredUnitReadModel>(em, entity);
        EnsureBuffer<BuildingRuntimeFactionSummary>(em, entity);
        EnsureBuffer<BuildingRuntimeOwnedBuildingSummary>(em, entity);
        EnsureBuffer<BuildingRuntimeUnitProductionSummary>(em, entity);
        EnsureBuffer<BuildingFactionProductionSpawnPointReadModel>(em, entity);
        EnsureBuffer<BuildingFactionUnitProductionRequest>(em, entity);
        EnsureBuffer<BuildingFactionResourceSellRequest>(em, entity);
        EnsureBuffer<BuildingRuntimeSpawnRequest>(em, entity);
        EnsureBuffer<BuildingRuntimeSurfaceOverlay>(em, entity);
    }

    private static void EnsureBuffer<T>(EntityManager em, Entity entity)
        where T : unmanaged, IBufferElementData
    {
        if (!em.HasBuffer<T>(entity))
            em.AddBuffer<T>(entity);
    }

    private static void LogRuntimeEcsBootstrapState(string phase)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogWarning($"[RuntimeVisualDiag] phase={phase} world=missing");
            return;
        }

        EntityManager em = world.EntityManager;
        using EntityQuery gridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        using EntityQuery initialSpawnQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        using EntityQuery registryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitPrefabRegistryTag>());
        using EntityQuery prefabCandidateQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Prefab>());
        using EntityQuery unitQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>());
        using EntityQuery modelQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitModelInstanceReference>());
        using EntityQuery sourceKeyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSourcePrefabKey>());
        using EntityQuery sourceKeyFallbackVisualQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<Unity.Transforms.LocalTransform>(),
            ComponentType.Exclude<UnitModelInstanceReference>(),
            ComponentType.Exclude<UnitRenderBudgetCulledUnitTag>(),
            ComponentType.Exclude<MissionRuntimeEntityId>());
        using EntityQuery missionFallbackVisualQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<MissionRuntimeEntityId>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<Unity.Transforms.LocalTransform>(),
            ComponentType.Exclude<UnitModelInstanceReference>());
        string activeMissionId = WarlineCaptureMissionSession.ActiveMissionId;
        int hasActiveMission = WarlineCaptureMissionSession.HasActiveMission ? 1 : 0;
        int isFirstContactMission = activeMissionId == ChapterOneMissionCatalog.FirstContactMissionId ? 1 : 0;

        Debug.Log(
            $"[RuntimeVisualDiag] phase={phase} " +
            $"activeMission={hasActiveMission} mission={activeMissionId} isM01={isFirstContactMission} " +
            $"gridConfigs={gridQuery.CalculateEntityCount()} initialSpawnConfigs={initialSpawnQuery.CalculateEntityCount()} " +
            $"unitRegistries={registryQuery.CalculateEntityCount()} prefabCandidates={prefabCandidateQuery.CalculateEntityCount()} " +
            $"units={unitQuery.CalculateEntityCount()} " +
            $"sourceKeys={sourceKeyQuery.CalculateEntityCount()} sourceKeyFallbackVisuals={sourceKeyFallbackVisualQuery.CalculateEntityCount()} models={modelQuery.CalculateEntityCount()} " +
            $"missionFallbackVisuals={missionFallbackVisualQuery.CalculateEntityCount()}");
    }

    private void InitializeGameplaySystemsIfNeeded()
    {
        if (GameplayInitialized)
            return;

        GameplayFeatureStartupSystem.Result gameplaySystems = _gameplayFeatureStartupSystem.Initialize(
            RuntimeCitySpawnerConfig,
            RuntimeGridBlockerConfig,
            RuntimeDecorationSpawnerConfig,
            _roadRuntimeGeneration,
            _roadRuntimeGenerationContext,
            _bindRoadGameplayFeatures,
            _buildingRuntimeCitySpawn,
            _buildingRuntimeCitySpawnContext,
            _buildingPlacementInteraction,
            _buildingPlacementInteractionContext,
            _bindBuildingGameplayFeatures,
            MainMenu,
            SelectionUiCamera,
            SelectionBuildingInteraction,
            _citizenPopulationEventSystem,
            _runtimeCityRoot,
            _runtimeBlockerRoot,
            DecorationRoot,
            DecorationCombinedMeshBaker,
            _gameplaySceneBindingSystem);

        RuntimeCity = gameplaySystems.RuntimeCity;
        RuntimeGridBlockers = gameplaySystems.RuntimeGridBlockers;
        RuntimeDecorations = gameplaySystems.RuntimeDecorations;
        GameplayInitialized = true;
    }

}
