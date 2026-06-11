using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

internal sealed class MatchBootstrapSystem
{
    private delegate bool TryResolveFactionSpawnCell(byte factionId, out int2 spawnCell);
    private enum GameplayStartStep : byte
    {
        Idle = 0,
        InitializeManagedRuntime,
        ResetStats,
        ProjectStartupConfig,
        CustomGameStartup,
        AiStartup,
        BindMainMenu,
        InitializeGameplayFeatures,
        FinalizeRuntimeState,
        Complete
    }

    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RuntimeCameraReferenceSystem _runtimeCameraReferenceSystem = new();
    private readonly VisualQualitySettingsSystem _visualQualitySettingsSystem = new();
    private readonly AIStartupSystem _aiStartupSystem = new();
    private readonly InitialFactionSpawnCellSystem _initialFactionSpawnCellSystem = new();
    private readonly GameplaySceneBindingSystem _gameplaySceneBindingSystem = new();
    private readonly RuntimeRootSystem _runtimeRootSystem = new();
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

    public Camera WorldCamera => MatchScene != null ? MatchScene.WorldCamera : null;
    public Light DirectionalLight => MatchScene != null ? MatchScene.DirectionalLight : null;
    public Volume GlobalVolume => MatchScene != null ? MatchScene.GlobalVolume : null;
    public VisualQualityProfileAsset VisualQualityProfile => MatchScene != null ? MatchScene.VisualQualityProfile : null;
    public CombinedMeshBaker DecorationCombinedMeshBaker => MatchScene != null ? MatchScene.DecorationCombinedMeshBaker : null;
    public Transform DecorationRoot => MatchScene != null ? MatchScene.DecorationRoot : null;

    public RTSSelectionSystemConfig RtsSelectionConfig => MatchScene != null ? MatchScene.RtsSelectionConfig : null;
    public RoadBuildSystemConfig RoadBuildConfig => MatchScene != null ? MatchScene.RoadBuildConfig : null;
    public BuildingPlacementSystemConfig BuildingPlacementConfig => MatchScene != null ? MatchScene.BuildingPlacementConfig : null;
    public MapBuildingPlacementConfig MapBuildingPlacementConfig => MatchScene != null ? MatchScene.MapBuildingPlacementConfig : null;
    public Transform MapBuildingAuthoringRoot => MatchScene != null ? MatchScene.MapBuildingAuthoringRoot : null;
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
    public ISelectionRectangleView SelectionRectangle { get; private set; }
    public MainMenuPlayUI MainMenu { get; private set; }
    public DayNightSystem DayNight { get; private set; }
    public FactionVisualSettings FactionVisuals { get; private set; }
    public UnitAttackTraceSystem UnitAttackTraces { get; private set; }
    public UnitImpostorRenderSystem UnitImpostors { get; private set; }
    public bool GameplayInitialized { get; private set; }
    public BuildingSelectionClickSystem.Context BuildingSelectionClickContext { get; private set; }
    public BuildingUiCommandSystem.Context BuildingUiCommandContext => _buildingUiCommandContext;
    public BuildingUiQuerySystem.Context BuildingUiQueryContext => _buildingUiQueryContext;
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
    private Action<IMatchRuntimeUi> _bindRoadMainMenu;
    private Action<IMatchRuntimeUi, RuntimeGridBlockerSystem> _bindRoadGameplayFeatures;
    private Action<IMatchRuntimeUi> _bindBuildingMainMenu;
    private Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> _bindBuildingGameplayFeatures;
    private Action<IMatchRuntimeUi> _bindSelectionMainMenu;
    private Action<IMatchHudSelectionPanelView> _bindMatchHudSelectionPanel;
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
    private bool _mainMenuBaseBindingsApplied;
    private bool _mainMenuRoadBindingApplied;
    private bool _mainMenuBuildingBindingApplied;
    private bool _mainMenuSelectionBindingApplied;
    private RuntimeGridBlockerSystem _mainMenuFeatureBoundGridBlockers;
    private RuntimeCityCompositionSystem _mainMenuFeatureBoundRuntimeCity;
    private IMatchHudSelectionPanelView _pendingMatchHudSelectionPanelView;
    private IMatchHudSelectionPanelView _boundMatchHudSelectionPanelView;
    private IMatchHudSelectionPanelView _deferredMatchHudSelectionPanelView;
    private bool _gameplayStartPending;
    private bool _gameplayStartRequested;
    private bool _gameplayStartComplete;
    private bool _managedRuntimeInitialized;
    private bool _visualQualitySettingsInitialized;
    private GameplayStartStep _gameplayStartStep;
    private AISettingsSnapshot _pendingAiSettingsSnapshot;
    private AIStartupSystem.Result _pendingAiStartupResult;
    private float _gameplayStartProgress01;
    private string _gameplayStartStatus = "Waiting for match scene";
    private Transform _runtimeBlockerRoot;
    private Transform _runtimeCityRoot;
    private Transform _runtimeUiRoot;

    public bool GameplayStartRequested => _gameplayStartRequested;
    public bool GameplayStartComplete => _gameplayStartComplete && !_gameplayStartPending;
    public float GameplayStartProgress01 => _gameplayStartComplete && _gameplayStartPending
        ? 0.98f
        : _gameplayStartProgress01;
    public string GameplayStartStatus => _gameplayStartComplete && _gameplayStartPending
        ? "Spawning world"
        : _gameplayStartStatus;

    public void Awake(MatchSceneView view, Transform ownerTransform, int ownerLayer)
    {
        Initialize(view);
        _matchSceneReferenceSystem.Register(view);
        _performanceDiagnosticsSystem = ResolvePerformanceDiagnosticsSystem();

        _runtimeRootSystem.Ensure(ownerTransform, ref _runtimeBlockerRoot, ref _runtimeCityRoot, ref _runtimeUiRoot);
        _runtimeCameraReferenceSystem.SetWorldCamera(WorldCamera);
    }

    public void Start()
    {
        _matchSceneReferenceSystem.Register(sceneView);
    }

    public void BeginGameplay()
    {
        if (_gameplayStartComplete)
            return;
        if (_gameplayStartRequested)
            return;

        _gameplayStartRequested = true;
        _gameplayStartComplete = false;
        _gameplayStartStep = GameplayStartStep.InitializeManagedRuntime;
        _gameplayStartProgress01 = 0f;
        _gameplayStartStatus = "Preparing match";
    }

    public void Update()
    {
        AdvanceGameplayStartPipeline();
        UpdateRuntime(
            GameplayInitialized,
            _runtimeGameplayStateSystem,
            _performanceDiagnosticsSystem,
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
        _visualQualitySettingsSystem.Update();
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
        _visualQualitySettingsSystem.Dispose();

        MainMenu = null;
        _mainMenuBaseBindingsApplied = false;
        _mainMenuRoadBindingApplied = false;
        _mainMenuBuildingBindingApplied = false;
        _mainMenuSelectionBindingApplied = false;
        _mainMenuFeatureBoundGridBlockers = null;
        _mainMenuFeatureBoundRuntimeCity = null;
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
        _bindMatchHudSelectionPanel = null;
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
        _pendingMatchHudSelectionPanelView = null;
        _boundMatchHudSelectionPanelView = null;
        _deferredMatchHudSelectionPanelView = null;
        RuntimeDecorations = null;
        RuntimeGridBlockers = null;
        RuntimeCity = null;
        _gameplayStartRequested = false;
        _gameplayStartComplete = false;
        _managedRuntimeInitialized = false;
        _visualQualitySettingsInitialized = false;
        _gameplayStartStep = GameplayStartStep.Idle;
        _gameplayStartProgress01 = 0f;
        _gameplayStartStatus = "Waiting for match scene";
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
        Func<Transform, RTSSelectionSystemConfig, ISelectionRectangleView> createSelectionRectangleView,
        Transform mapBuildingAuthoringRoot,
        int ownerLayer)
    {
        return managedGameplayStartupSystem.Initialize(
            dayNightConfig,
            factionVisualConfig,
            roadBuildConfig,
            buildingPlacementConfig,
            mapBuildingPlacementConfig,
            rtsSelectionConfig,
            unitAttackTraceConfig,
            runtimeCitySpawnerConfig,
            gameStringsConfig,
            prefabPreviewCameraConfig,
            worldCamera,
            directionalLight,
            globalVolume,
            runtimeUiRoot,
            createSelectionRectangleView,
            mapBuildingAuthoringRoot,
            ownerLayer);
    }

    public void ProjectFactionVisualConfig(World world, FactionVisualSettingsConfig factionVisualConfig)
    {
        if (world == null || !world.IsCreated || factionVisualConfig == null)
            return;

        EntityManager em = world.EntityManager;
        FactionVisualConfig config = new()
        {
            PlayerColor = ToFloat4(factionVisualConfig.PlayerColor),
            EnemyColor = ToFloat4(factionVisualConfig.EnemyColor),
            NeutralColor = ToFloat4(factionVisualConfig.NeutralColor)
        };

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<FactionVisualConfig>());
        Entity entity = query.IsEmptyIgnoreFilter
            ? em.CreateEntity(typeof(FactionVisualConfig))
            : query.GetSingletonEntity();
        em.SetComponentData(entity, config);
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
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AISettingsSnapshot aiSettings)
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
        aiStartupSystem.LogConfigValidation(aiControllerConfigs, aiSettings);
    }

    private static float4 ToFloat4(Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
    }

    public AIStartupSystem.Result InitializeAiStartupConfig(
        World world,
        AIStartupSystem aiStartupSystem,
        IReadOnlyList<AIControllerConfig> aiControllerConfigs,
        AIPlanEntryStartupConfig aiPlanEntryConfig,
        AISettingsSnapshot aiSettings,
        AIStartupSystem.TryResolveFactionSpawnCell tryResolveFactionSpawnCell)
    {
        return aiStartupSystem.Initialize(
            world,
            aiControllerConfigs,
            aiPlanEntryConfig,
            tryResolveFactionSpawnCell,
            aiSettings);
    }

    public MainMenuPlayUI EnsureMainMenuRuntimeDependencies(bool resetRuntimeState = false)
    {
        if (SelectionUiCommand == null)
            return MainMenu;

        if (MainMenu == null)
            MainMenu = new MainMenuPlayUI();

        MainMenu.Init(SelectionUiCommand, DayNight, SelectionUiCamera, resetRuntimeState);
        ApplyMainMenuBaseBindings();
        ApplyMainMenuFeatureBindingsIfReady();
        return MainMenu;
    }

    public void BindMatchHudSelectionPanel(IMatchHudSelectionPanelView view)
    {
        _pendingMatchHudSelectionPanelView = view;
        if (_bindMatchHudSelectionPanel == null)
        {
            if (_deferredMatchHudSelectionPanelView == view)
                return;

            _deferredMatchHudSelectionPanelView = view;
            return;
        }

        if (_boundMatchHudSelectionPanelView == view)
            return;

        _bindMatchHudSelectionPanel.Invoke(view);
        _boundMatchHudSelectionPanelView = view;
        _deferredMatchHudSelectionPanelView = null;
    }

    private void ApplyMainMenuBaseBindings()
    {
        if (MainMenu == null)
            return;

        if (!_mainMenuRoadBindingApplied && _bindRoadMainMenu != null)
        {
            _bindRoadMainMenu.Invoke(MainMenu);
            _mainMenuRoadBindingApplied = true;
        }

        if (!_mainMenuBuildingBindingApplied && _bindBuildingMainMenu != null)
        {
            _bindBuildingMainMenu.Invoke(MainMenu);
            _mainMenuBuildingBindingApplied = true;
        }

        if (!_mainMenuSelectionBindingApplied && _bindSelectionMainMenu != null)
        {
            _bindSelectionMainMenu.Invoke(MainMenu);
            _mainMenuSelectionBindingApplied = true;
        }

        _mainMenuBaseBindingsApplied =
            _mainMenuRoadBindingApplied &&
            _mainMenuBuildingBindingApplied &&
            _mainMenuSelectionBindingApplied;
    }

    private void ApplyMainMenuFeatureBindingsIfReady()
    {
        if (!GameplayInitialized || MainMenu == null)
            return;
        if (_mainMenuFeatureBoundGridBlockers == RuntimeGridBlockers &&
            _mainMenuFeatureBoundRuntimeCity == RuntimeCity)
        {
            return;
        }

        _bindRoadGameplayFeatures?.Invoke(MainMenu, RuntimeGridBlockers);
        _bindBuildingGameplayFeatures?.Invoke(
            MainMenu,
            SelectionUiCamera,
            SelectionBuildingInteraction,
            RuntimeGridBlockers,
            RuntimeCity,
            _citizenPopulationEventSystem);
        _mainMenuFeatureBoundGridBlockers = RuntimeGridBlockers;
        _mainMenuFeatureBoundRuntimeCity = RuntimeCity;
    }

    private static bool FocusInitialCameraOnConfiguredFactionBase(
        World world,
        SelectionUiCameraSystem selectionUiCameraSystem,
        TryResolveFactionSpawnCell resolveFactionSpawnCell,
        byte fallbackFactionId)
    {
        if (selectionUiCameraSystem == null ||
            resolveFactionSpawnCell == null ||
            !resolveFactionSpawnCell(fallbackFactionId, out int2 spawnCell))
        {
            return false;
        }

        Vector3 focusWorldPosition = new(spawnCell.x, 0f, spawnCell.y);
        if (world != null && world.IsCreated)
        {
            EntityManager em = world.EntityManager;
            using EntityQuery gridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            if (!gridQuery.IsEmptyIgnoreFilter)
            {
                Entity gridEntity = gridQuery.GetSingletonEntity();
                GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
                focusWorldPosition = GridUtils.CellToWorldCenter(grid, spawnCell);
            }
        }

        selectionUiCameraSystem.FollowCameraGroundCenterTo(focusWorldPosition);
        return true;
    }

    public void UpdateRuntime(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
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
        IMatchRuntimeUi mainMenu,
        UnitImpostorRenderSystem unitImpostors,
        ref bool gameplayStartPending)
    {
        gameplayRuntimeUpdateSystem.Update(
            gameplayInitialized,
            runtimeGameplayStateSystem,
            performanceDiagnosticsSystem,
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
        ISelectionRectangleView selectionRectangleView)
    {
        gameplayRuntimeUpdateSystem.OnGui(
            gameplayInitialized,
            runtimeGameplayStateSystem,
            performanceDiagnosticsSystem,
            roadBuildOnGui,
            selectionRectangleView);
    }

    public void ShutdownRuntime(
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

    private void AdvanceGameplayStartPipeline()
    {
        if (!_gameplayStartRequested || _gameplayStartComplete)
            return;

        switch (_gameplayStartStep)
        {
            case GameplayStartStep.InitializeManagedRuntime:
                SetGameplayStartProgress(0.02f, "Preparing gameplay runtime");
                InitializeManagedRuntimeIfNeeded();
                _gameplayStartStep = GameplayStartStep.ResetStats;
                break;

            case GameplayStartStep.ResetStats:
                SetGameplayStartProgress(0.10f, "Resetting match state");
                GameRuntimeStats.Reset();
                _pendingAiSettingsSnapshot = AISettingsRuntimeState.CurrentSnapshot;
                ProjectFactionVisualConfig(World.DefaultGameObjectInjectionWorld, FactionVisualConfig);
                _gameplayStartStep = GameplayStartStep.ProjectStartupConfig;
                break;

            case GameplayStartStep.ProjectStartupConfig:
                SetGameplayStartProgress(0.24f, "Preparing map data");
                ProjectRuntimeStartupConfig(
                    World.DefaultGameObjectInjectionWorld,
                    _runtimeGridBootstrapSystem,
                    _mapSurfaceRuntimeBootstrapSystem,
                    RuntimeGridConfig,
                    MapSurfaceAuthoring,
                    _initialFactionSpawnCellSystem,
                    BuildingPlacementConfig,
                    _aiStartupSystem,
                    AIControllerConfigs,
                    _pendingAiSettingsSnapshot);
                _gameplayStartStep = GameplayStartStep.CustomGameStartup;
                break;

            case GameplayStartStep.CustomGameStartup:
                SetGameplayStartProgress(0.38f, "Preparing unit prefabs");
                _customGameStartupSystem.InitializeFromLegacyConfigs(
                    World.DefaultGameObjectInjectionWorld,
                    BuildingPlacementConfig != null ? BuildingPlacementConfig.InitialUnitsConfig : null,
                    BuildingPlacementConfig != null ? BuildingPlacementConfig.UnitPrefabRegistryConfig : null);
                _gameplayStartStep = GameplayStartStep.AiStartup;
                break;

            case GameplayStartStep.AiStartup:
                SetGameplayStartProgress(0.52f, "Preparing AI factions");
                _pendingAiStartupResult = InitializeAiStartupConfig(
                    World.DefaultGameObjectInjectionWorld,
                    _aiStartupSystem,
                    AIControllerConfigs,
                    AIPlanEntryConfig,
                    _pendingAiSettingsSnapshot,
                    _initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell);
                if (_pendingAiStartupResult.HasPlayerAutoMode)
                    _runtimeGameplayStateSystem.PlayerAutoModeEnabled = _pendingAiStartupResult.PlayerAutoModeEnabled;
                _gameplayStartStep = GameplayStartStep.BindMainMenu;
                break;

            case GameplayStartStep.BindMainMenu:
                SetGameplayStartProgress(0.66f, "Binding match HUD");
                EnsureMainMenuRuntimeDependencies(resetRuntimeState: true);
                _gameplayStartStep = GameplayStartStep.InitializeGameplayFeatures;
                break;

            case GameplayStartStep.InitializeGameplayFeatures:
                SetGameplayStartProgress(0.80f, "Starting gameplay systems");
                InitializeGameplaySystemsIfNeeded();
                _gameplayStartStep = GameplayStartStep.FinalizeRuntimeState;
                break;

            case GameplayStartStep.FinalizeRuntimeState:
                SetGameplayStartProgress(0.92f, "Focusing camera");
                _gameplayStartPending = true;
                _runtimeCameraReferenceSystem.SetWorldCamera(WorldCamera);
                _runtimeGameplayStateSystem.ResetForGameplayStart();
                FocusInitialCameraOnConfiguredFactionBase(
                    World.DefaultGameObjectInjectionWorld,
                    SelectionUiCamera,
                    _initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell,
                    0);
                _gameplayStartStep = GameplayStartStep.Complete;
                break;

            case GameplayStartStep.Complete:
                SetGameplayStartProgress(0.98f, "Spawning world");
                _gameplayStartComplete = true;
                break;
        }
    }

    private void SetGameplayStartProgress(float progress01, string status)
    {
        _gameplayStartProgress01 = Mathf.Clamp01(progress01);
        _gameplayStartStatus = string.IsNullOrEmpty(status) ? "Starting match" : status;
    }

    private static ISelectionRectangleView EnsureSelectionRectangleView(
        Transform runtimeUiRoot,
        RTSSelectionSystemConfig rtsSelectionConfig)
    {
        if (runtimeUiRoot == null)
            return null;

        SelectionRectangleView view = runtimeUiRoot.GetComponent<SelectionRectangleView>();
        if (view == null)
            view = runtimeUiRoot.gameObject.AddComponent<SelectionRectangleView>();

        view.ApplyConfig(rtsSelectionConfig);
        return view;
    }

    private void InitializeManagedRuntimeIfNeeded()
    {
        if (_managedRuntimeInitialized)
            return;

        InitializeVisualQualitySettingsIfNeeded();

        ManagedGameplayStartupSystem.Result managedSystems = InitializeManagedRuntime(
            DayNightConfig,
            FactionVisualConfig,
            RoadBuildConfig,
            BuildingPlacementConfig,
            MapBuildingPlacementConfig,
            RtsSelectionConfig,
            UnitAttackTraceConfig,
            RuntimeCitySpawnerConfig,
            GameStringsConfig,
            PrefabPreviewCameraConfig,
            WorldCamera,
            DirectionalLight,
            GlobalVolume,
            _runtimeUiRoot,
            EnsureSelectionRectangleView,
            MapBuildingAuthoringRoot,
            MatchScene != null ? MatchScene.gameObject.layer : 0);

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
        _bindMatchHudSelectionPanel = managedSystems.BindMatchHudSelectionPanel;
        _mainMenuBaseBindingsApplied = false;
        _mainMenuRoadBindingApplied = false;
        _mainMenuBuildingBindingApplied = false;
        _mainMenuSelectionBindingApplied = false;
        _boundMatchHudSelectionPanelView = null;
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
        _managedRuntimeInitialized = true;
        if (MainMenu != null)
        {
            MainMenu.Init(SelectionUiCommand, DayNight, SelectionUiCamera, resetRuntimeState: false);
            ApplyMainMenuBaseBindings();
        }

        if (_pendingMatchHudSelectionPanelView != null)
            BindMatchHudSelectionPanel(_pendingMatchHudSelectionPanelView);
    }

    private void InitializeVisualQualitySettingsIfNeeded()
    {
        if (_visualQualitySettingsInitialized)
            return;

        _visualQualitySettingsSystem.Initialize(VisualQualityProfile, WorldCamera, DirectionalLight, GlobalVolume);
        _visualQualitySettingsInitialized = true;
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
        ApplyMainMenuFeatureBindingsIfReady();
    }

}
