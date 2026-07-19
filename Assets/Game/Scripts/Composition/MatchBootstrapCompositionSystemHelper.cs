using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Game.Rendering.Contracts;
using Game.UI.Contracts;
using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Rendering;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.Composition
{
    internal sealed class MatchBootstrapCompositionSystemHelper
    {
        private RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
        private RuntimeCameraReferenceSystem _runtimeCameraReferenceSystem;
        private VisualQualitySettingsSystem _visualQualitySettingsSystem;
        private readonly GameplaySceneBindingSceneSystemHelper _gameplaySceneBindingSystem = new();
        private RuntimeRootSceneSystemHelper _runtimeRootSceneSystemHelper;
        private readonly GameplayFeatureStartupCompositionSystemHelper _gameplayFeatureStartupSystem = new();
        private readonly PerformanceDiagnosticsReferenceCompositionSystemHelper _performanceDiagnosticsReferenceSystem = new();
        private readonly MatchSceneReferenceCompositionSystemHelper _matchSceneReferenceSystem = new();
        private readonly MatchIntroEcsStateQuery matchIntroStateQuery = new();

        private readonly ManagedGameplayStartupSystemHelper managedGameplayStartupSystem = new();
        private readonly MatchGameplayStartupCompositionSystemHelper gameplayStartupSystem = new();
        private readonly GameplayRuntimeUpdateCompositionSystemHelper gameplayRuntimeUpdateSystem = new();
        private readonly PerformanceDiagnosticsSystemHelper fallbackPerformanceDiagnosticsSystemHelper = new();
        private readonly StaticMapPresentationOwnership mapVisuals = new();
        private readonly IGameTextResolver gameTextResolver = new GameTextResolverAdapter();
        private readonly SharedPrefabPreviewCache _prefabPreviewCache = new();
        private bool fallbackPerformanceDiagnosticsInitialized;
        private bool _staticMapBatchingInitialized;
        private World runtimeWorld;
        private MatchSceneView sceneView;
        public MatchSceneView SceneView => sceneView;
        public bool HasSceneView => sceneView != null;

        public Camera WorldCamera => sceneView != null ? sceneView.WorldCamera : null;
        public Light DirectionalLight => sceneView != null ? sceneView.DirectionalLight : null;
        public Volume GlobalVolume => sceneView != null ? sceneView.GlobalVolume : null;
        public VisualQualityProfileAsset VisualQualityProfile => sceneView != null ? sceneView.VisualQualityProfile : null;
        public CombinedMeshBaker DecorationCombinedMeshBaker => sceneView != null ? sceneView.DecorationCombinedMeshBaker : null;
        public Transform DecorationRoot => sceneView != null ? sceneView.DecorationRoot : null;

        public RTSSelectionSystemConfig RtsSelectionConfig => sceneView != null ? sceneView.RtsSelectionConfig : null;
        public RoadBuildSystemConfig RoadBuildConfig => sceneView != null ? sceneView.RoadBuildConfig : null;
        public BuildingPlacementSystemConfig BuildingPlacementConfig => sceneView != null ? sceneView.BuildingPlacementConfig : null;
        public MapBuildingPlacementConfig MapBuildingPlacementConfig => sceneView != null ? sceneView.MapBuildingPlacementConfig : null;
        public MapVehiclePlacementConfig MapVehiclePlacementConfig => sceneView != null ? sceneView.MapVehiclePlacementConfig : null;
        public Transform MapBuildingAuthoringRoot => sceneView != null ? sceneView.MapBuildingAuthoringRoot : null;
        public Transform MapVehicleAuthoringRoot => sceneView != null ? sceneView.MapVehicleAuthoringRoot : null;
        public UnitAttackTraceSystemConfig UnitAttackTraceConfig => sceneView != null ? sceneView.UnitAttackTraceConfig : null;
        public RuntimeCitySpawnerSystemConfig RuntimeCitySpawnerConfig => sceneView != null ? sceneView.RuntimeCitySpawnerConfig : null;
        public RuntimeDecorationSpawnerSystemConfig RuntimeDecorationSpawnerConfig => sceneView != null ? sceneView.RuntimeDecorationSpawnerConfig : null;
        public RuntimeGridBlockerSystemConfig RuntimeGridBlockerConfig => sceneView != null ? sceneView.RuntimeGridBlockerConfig : null;
        public DayNightSystemConfig DayNightConfig => sceneView != null ? sceneView.DayNightConfig : null;
        public GameStringsConfig GameStringsConfig => sceneView != null ? sceneView.GameStringsConfig : null;
        public AIPlanEntryStartupConfig AIPlanEntryConfig => sceneView != null ? sceneView.AIPlanEntryConfig : null;
        private ResourceExchangeRecipeConfigSet ResourceExchangeConfig =>
            sceneView != null ? sceneView.ResourceExchangeConfig : null;
        public IReadOnlyList<AIControllerConfig> AIControllerConfigs => sceneView != null ? sceneView.AIControllerConfigs : Array.Empty<AIControllerConfig>();
        private FactionVisualSettingsConfig FactionVisualConfig => sceneView != null ? sceneView.FactionVisualConfig : null;
        private PrefabPreviewCameraConfig PrefabPreviewCameraConfig => sceneView != null ? sceneView.PrefabPreviewCameraConfig : null;
        private GridAuthoringConfig RuntimeGridConfig => sceneView != null ? sceneView.RuntimeGridConfig : null;
        private MapSurfaceAuthoring MapSurfaceAuthoring => sceneView != null ? sceneView.MapSurfaceAuthoring : null;

        public RuntimeGridBlockerPresentationSystemHelper RuntimeGridBlockers { get; private set; }
        public RuntimeDecorationSpawnerPresentationSystemHelper RuntimeDecorations { get; private set; }
        public RuntimeCityCompositionSystemHelper RuntimeCity { get; private set; }
        public RoadBuildReadModelCompositionSystemHelper RoadBuildReadModel { get; private set; }
        public BuildingSelectionClickUtilitySystemHelper BuildingSelectionClick { get; private set; }
        public BuildingUiCommandSystemHelper BuildingUiCommand { get; private set; }
        public BuildingUiQueryUiSystemHelper BuildingUiQuery { get; private set; }
        public IBuildingUiCommand BuildingUiCommandContract { get; private set; }
        public IBuildingUiQuery BuildingUiQueryContract { get; private set; }
        public BuildingRuntimeUpdateCompositionSystemHelper BuildingRuntimeUpdate { get; private set; }
        public SelectionUiCommandUiSystemHelper SelectionUiCommand { get; private set; }
        public SelectionUiReadModelUiSystemHelper SelectionUiReadModel { get; private set; }
        public SelectionUiCameraSystemHelper SelectionUiCamera { get; private set; }
        public SelectionBuildingInteractionCompositionSystemHelper SelectionBuildingInteraction { get; private set; }
        public SelectionScreenMarkerUiSystemHelper SelectionScreenMarkers { get; private set; }
        public ISelectionRectangleView SelectionRectangle { get; private set; }
        public ISelectionDiagnosticsSink SelectionDiagnosticsSink { get; } = new SelectionDiagnosticsSinkAdapter();
        public MainMenuPlayUI MainMenu { get; private set; }
        public DayNightSystem DayNight { get; private set; }
        public FactionVisualSettings FactionVisuals { get; private set; }
        public IUnitAttackTraceRenderer UnitAttackTraces { get; private set; }
        public IUnitImpostorRenderer UnitImpostors { get; private set; }
        public bool GameplayInitialized { get; private set; }
        public BuildingSelectionClickUtilitySystemHelper.Context BuildingSelectionClickContext { get; private set; }
        public BuildingUiCommandSystemHelper.Context BuildingUiCommandContext => _buildingUiCommandContext;
        public BuildingUiQueryUiSystemHelper.Context BuildingUiQueryContext => _buildingUiQueryContext;
        private BuildingRuntimeCitySpawnBridgeCompositionSystemHelper _buildingRuntimeCitySpawn;
        private BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context _buildingRuntimeCitySpawnContext;
        private BuildingUiCommandSystemHelper.Context _buildingUiCommandContext;
        private BuildingUiQueryUiSystemHelper.Context _buildingUiQueryContext;
        private IMatchRuntimeState _matchRuntimeState;
        private IMatchHudCameraControl _matchHudCameraControl;
        private IMatchHudMinimapDataSource _matchHudMinimapDataSource;
        private ISelectionRectangleState _selectionRectangleState;
        private BuildingPlacementInteractionCompositionSystemHelper _buildingPlacementInteraction;
        private BuildingPlacementInteractionCompositionSystemHelper.Context _buildingPlacementInteractionContext;
        private RoadRuntimeGenerationCompositionSystemHelper _roadRuntimeGeneration;
        private RoadRuntimeGenerationCompositionSystemHelper.Context _roadRuntimeGenerationContext;
        private Action _roadRuntimeUpdate;
        private Action _roadOnGui;
        private Action _disposeRoad;
        private Action<IMatchRuntimeUi> _bindRoadMainMenu;
        private Action<IMatchRuntimeUi, RuntimeGridBlockerPresentationSystemHelper> _bindRoadGameplayFeatures;
        private Action<IMatchRuntimeUi> _bindBuildingMainMenu;
        private Action<IMatchRuntimeUi, SelectionUiCameraSystemHelper, SelectionBuildingInteractionCompositionSystemHelper, RuntimeGridBlockerPresentationSystemHelper, RuntimeCityCompositionSystemHelper, CitizenPopulationEventCompositionSystemHelper> _bindBuildingGameplayFeatures;
        private Action<IMatchRuntimeUi> _bindSelectionMainMenu;
        private Action<IMatchHudSelectionPanelView> _bindMatchHudSelectionPanel;
        private Action _selectionRuntimeUpdate;
        private Action _citizenPopulationRuntimeUpdate;
        private Action _disposeCitizenPopulation;
        private CitizenPopulationReadModelCompositionSystemHelper _citizenPopulationReadModel;
        private CitizenPopulationEventCompositionSystemHelper _citizenPopulationEventSystem;
        private Action _disposeSelection;
        private Action _disposeBuildingGameplay;
        private BuildingRuntimeUpdateCompositionSystemHelper.Context _buildingRuntimeUpdateContext;
        private Entity _buildingRuntimeBoundaryEntity;
        private PerformanceDiagnosticsSystemHelper _performanceDiagnosticsSystem;
        private bool _mainMenuBaseBindingsApplied;
        private bool _mainMenuRoadBindingApplied;
        private bool _mainMenuBuildingBindingApplied;
        private bool _mainMenuSelectionBindingApplied;
        private RuntimeGridBlockerPresentationSystemHelper _mainMenuFeatureBoundGridBlockers;
        private RuntimeCityCompositionSystemHelper _mainMenuFeatureBoundRuntimeCity;
        private IMatchHudSelectionPanelView _pendingMatchHudSelectionPanelView;
        private IMatchHudSelectionPanelView _boundMatchHudSelectionPanelView;
        private IMatchHudSelectionPanelView _deferredMatchHudSelectionPanelView;
        private bool _managedRuntimeInitialized;
        private bool _visualQualitySettingsInitialized;
        private bool _runtimeSettingsChangeSubscribed;
        private bool _hasLatestRuntimeSettings;
        private UISettingsModel _latestRuntimeSettings;
        private Transform _runtimeBlockerRoot;
        private Transform _runtimeCityRoot;
        private Transform _runtimeTransportsRoot;
        private Transform _runtimeUiRoot;

        public bool GameplayStartRequested => gameplayStartupSystem.GameplayStartRequested;
        public bool GameplayStartComplete => gameplayStartupSystem.GameplayStartComplete;
        public bool GameplayStartFailed => gameplayStartupSystem.GameplayStartFailed;
        public string GameplayStartFailureMessage => gameplayStartupSystem.GameplayStartFailureMessage;
        public float GameplayStartProgress01 => gameplayStartupSystem.GameplayStartProgress01;
        public string GameplayStartStatus => gameplayStartupSystem.GameplayStartStatus;

        public void Awake(World runtimeWorld, MatchSceneView view, Transform ownerTransform, int ownerLayer)
        {
            this.runtimeWorld = runtimeWorld;
            _runtimeGameplayStateSystem.Bind(runtimeWorld.EntityManager);
            Initialize(view);
            _performanceDiagnosticsSystem = ResolvePerformanceDiagnosticsSystemHelper(runtimeWorld.EntityManager);
            _matchSceneReferenceSystem.Register(runtimeWorld.EntityManager, view);
            matchIntroStateQuery.Bind(runtimeWorld);
            _runtimeCameraReferenceSystem = ResolveRuntimeCameraReferenceSystem(runtimeWorld);

            ResolveRuntimeRootSceneSystemHelper()?.Ensure(
                ownerTransform,
                ref _runtimeBlockerRoot,
                ref _runtimeCityRoot,
                ref _runtimeTransportsRoot,
                ref _runtimeUiRoot);
            _runtimeCameraReferenceSystem?.SetWorldCamera(WorldCamera);
        }

        public void BeginGameplay()
        {
            gameplayStartupSystem.BeginGameplay();
        }

        public void Update()
        {
            gameplayStartupSystem.Advance(BuildingRuntimeUpdate, _buildingRuntimeUpdateContext);

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
                ref gameplayStartupSystem.PendingState);
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            OnApplicationFocus(_performanceDiagnosticsSystem, hasFocus);
        }

        public void OnApplicationPause(bool pauseStatus)
        {
            OnApplicationPause(_performanceDiagnosticsSystem, pauseStatus);
        }

        public void OnApplicationFocus(PerformanceDiagnosticsSystemHelper performanceDiagnosticsSystem, bool hasFocus)
        {
            ForwardApplicationFocus(performanceDiagnosticsSystem, hasFocus);
        }

        public void OnApplicationPause(PerformanceDiagnosticsSystemHelper performanceDiagnosticsSystem, bool pauseStatus)
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void OnGUI()
        {
            OnGuiRuntime(
                GameplayInitialized,
                _runtimeGameplayStateSystem,
                _performanceDiagnosticsSystem,
                _roadOnGui,
                SelectionRectangle);
        }
#endif

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
                gameplayStartupSystem.MapSurfaceRuntimeBootstrapSystem,
                _runtimeCameraReferenceSystem,
                _performanceDiagnosticsSystem);
            gameplayRuntimeUpdateSystem.Dispose();
            _visualQualitySettingsSystem?.Dispose();
            mapVisuals.Dispose();
            matchIntroStateQuery.Reset();

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
            _matchRuntimeState = null;
            _matchHudCameraControl = null;
            _matchHudMinimapDataSource = null;
            _selectionRectangleState = null;
            BuildingSelectionClick = null;
            BuildingSelectionClickContext = default;
            _buildingRuntimeCitySpawn = null;
            _buildingRuntimeCitySpawnContext = default;
            BuildingUiCommand = null;
            _buildingUiCommandContext = default;
            BuildingUiCommandContract = null;
            BuildingUiQuery = null;
            _buildingUiQueryContext = default;
            BuildingUiQueryContract = null;
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
            gameplayStartupSystem.ResetForShutdown();
            _managedRuntimeInitialized = false;
            _visualQualitySettingsInitialized = false;
            _staticMapBatchingInitialized = false;
            runtimeWorld = null;
        }

        public void Initialize(MatchSceneView view)
        {
            sceneView = view;
            gameplayStartupSystem.Bind(
                sceneView,
                _runtimeGameplayStateSystem,
                InitializeManagedRuntimeIfNeeded,
                () => EnsureMainMenuRuntimeDependencies(resetRuntimeState: true),
                InitializeGameplaySystemsIfNeeded,
                ResolveRuntimeCameraReferenceSystem);
            EnsureRuntimeSettingsChangeSubscription();
            if (!_hasLatestRuntimeSettings)
            {
                _latestRuntimeSettings = SettingsService.Load();
                _hasLatestRuntimeSettings = true;
            }
        }

        public void Shutdown()
        {
            ReleaseRuntimeSettingsChangeSubscription();
            _runtimeGameplayStateSystem.ResetForMatchShutdown();
            if (runtimeWorld != null && runtimeWorld.IsCreated)
                _matchSceneReferenceSystem.Clear(runtimeWorld.EntityManager, sceneView);
            matchIntroStateQuery.Reset();
            sceneView = null;
            runtimeWorld = null;
        }

        public PerformanceDiagnosticsSystemHelper ResolvePerformanceDiagnosticsSystemHelper(EntityManager entityManager)
        {
            if (_performanceDiagnosticsReferenceSystem.TryGet(entityManager, out PerformanceDiagnosticsSystemHelper persistentDiagnostics))
                return persistentDiagnostics;

            if (!fallbackPerformanceDiagnosticsInitialized)
            {
                Application.runInBackground = true;
                fallbackPerformanceDiagnosticsSystemHelper.Initialize();
                fallbackPerformanceDiagnosticsInitialized = true;
            }

            return fallbackPerformanceDiagnosticsSystemHelper;
        }

        private void ForwardApplicationFocus(PerformanceDiagnosticsSystemHelper performanceDiagnosticsSystem, bool hasFocus)
        {
            if (fallbackPerformanceDiagnosticsInitialized && performanceDiagnosticsSystem == fallbackPerformanceDiagnosticsSystemHelper)
                fallbackPerformanceDiagnosticsSystemHelper.OnApplicationFocus(hasFocus);
        }

        private void ForwardApplicationPause(PerformanceDiagnosticsSystemHelper performanceDiagnosticsSystem, bool pauseStatus)
        {
            if (fallbackPerformanceDiagnosticsInitialized && performanceDiagnosticsSystem == fallbackPerformanceDiagnosticsSystemHelper)
                fallbackPerformanceDiagnosticsSystemHelper.OnApplicationPause(pauseStatus);
        }

        public ManagedGameplayStartupSystemHelper.Result InitializeManagedRuntime(
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
            Func<Transform, RTSSelectionSystemConfig, ISelectionRectangleView> createSelectionRectangleView,
            Transform mapBuildingAuthoringRoot,
            Transform mapVehicleAuthoringRoot,
            IMatchIntroStateQuery matchIntroStateQuery)
        {
            return managedGameplayStartupSystem.Initialize(
                runtimeWorld.EntityManager,
                dayNightConfig,
                factionVisualConfig,
                roadBuildConfig,
                buildingPlacementConfig,
                mapBuildingPlacementConfig,
                mapVehiclePlacementConfig,
                rtsSelectionConfig,
                runtimeCitySpawnerConfig,
                gameStringsConfig,
                worldCamera,
                directionalLight,
                globalVolume,
                runtimeTransportsRoot,
                runtimeUiRoot,
                createSelectionRectangleView,
                SelectionPortraitSpriteResolverUiSystemHelper.ResolveSelectionPortraitSprite,
                SelectionPortraitSpriteResolverUiSystemHelper.ResolveSelectionCardPortraitSprite,
                BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata,
                BuildingProductionUnitMetadataPrefabSystemHelper.PrepareTransportDropVisual,
                BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata,
                mapBuildingAuthoringRoot,
                mapVehicleAuthoringRoot,
                matchIntroStateQuery);
        }

        public MainMenuPlayUI EnsureMainMenuRuntimeDependencies(bool resetRuntimeState = false)
        {
            if (SelectionUiCommand == null)
                return MainMenu;

            if (MainMenu == null)
                MainMenu = new MainMenuPlayUI();

            if (!resetRuntimeState && AreMainMenuRuntimeDependenciesCurrent())
                return MainMenu;

            EnsureUiRuntimeAdapters();
            MainMenu.Init(SelectionUiCommand, _matchRuntimeState, _matchHudCameraControl, _matchHudMinimapDataSource, gameTextResolver, resetRuntimeState);
            ApplyMainMenuBaseBindings();
            ApplyMainMenuFeatureBindingsIfReady();
            return MainMenu;
        }

        public bool AreMainMenuRuntimeDependenciesCurrent()
        {
            if (SelectionUiCommand == null || MainMenu == null || !_mainMenuBaseBindingsApplied)
                return false;

            if (!GameplayInitialized)
                return true;

            return _mainMenuFeatureBoundGridBlockers == RuntimeGridBlockers &&
                   _mainMenuFeatureBoundRuntimeCity == RuntimeCity;
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

        public void UpdateRuntime(
            bool gameplayInitialized,
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            PerformanceDiagnosticsSystemHelper performanceDiagnosticsSystem,
            Action roadBuildRuntimeUpdate,
            BuildingRuntimeUpdateCompositionSystemHelper buildingRuntimeUpdate,
            BuildingRuntimeUpdateCompositionSystemHelper.Context buildingRuntimeUpdateContext,
            Action selectionRuntimeUpdate,
            Camera worldCamera,
            RuntimeCityCompositionSystemHelper runtimeCity,
            RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
            RuntimeDecorationSpawnerPresentationSystemHelper runtimeDecorations,
            DayNightSystem dayNight,
            Action citizenPopulationRuntimeUpdate,
            IMatchRuntimeUi mainMenu,
            IUnitImpostorRenderer unitImpostors,
            ref bool gameplayStartPending)
        {
            gameplayRuntimeUpdateSystem.Update(runtimeWorld, gameplayInitialized,
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
            PerformanceDiagnosticsSystemHelper performanceDiagnosticsSystem,
            IUnitAttackTraceRenderer unitAttackTraces,
            IUnitImpostorRenderer unitImpostors)
        {
            gameplayRuntimeUpdateSystem.LateUpdate(
                gameplayInitialized,
                runtimeGameplayStateSystem,
                performanceDiagnosticsSystem,
                unitAttackTraces,
                unitImpostors);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void OnGuiRuntime(
            bool gameplayInitialized,
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            PerformanceDiagnosticsSystemHelper performanceDiagnosticsSystem,
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
#endif

        public void ShutdownRuntime(
            MainMenuPlayUI mainMenu,
            Action disposeSelection,
            Action disposeBuildingGameplay,
            Action disposeRoad,
            IUnitAttackTraceRenderer unitAttackTraces,
            IUnitImpostorRenderer unitImpostors,
            Action disposeCitizenPopulation,
            DayNightSystem dayNight,
            RuntimeDecorationSpawnerPresentationSystemHelper runtimeDecorations,
            RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
            RuntimeCityCompositionSystemHelper runtimeCity,
            MapSurfaceRuntimeBootstrapSceneSystemHelper mapSurfaceRuntimeBootstrapSystem,
            RuntimeCameraReferenceSystem runtimeCameraReferenceSystem,
            PerformanceDiagnosticsSystemHelper performanceDiagnosticsSystem)
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
            mapSurfaceRuntimeBootstrapSystem?.DisposeRuntimeSurface();
            runtimeCameraReferenceSystem?.ClearWorldCamera();
            ReleasePerformanceDiagnostics(performanceDiagnosticsSystem);
            _prefabPreviewCache.Dispose();
        }

        private void ReleasePerformanceDiagnostics(PerformanceDiagnosticsSystemHelper performanceDiagnosticsSystem)
        {
            if (!fallbackPerformanceDiagnosticsInitialized ||
                performanceDiagnosticsSystem != fallbackPerformanceDiagnosticsSystemHelper)
            {
                return;
            }

            fallbackPerformanceDiagnosticsSystemHelper.Dispose();
            fallbackPerformanceDiagnosticsInitialized = false;
        }


        private RuntimeCameraReferenceSystem ResolveRuntimeCameraReferenceSystem(World world)
        {
            if (world == null || !world.IsCreated)
                return null;

            _runtimeCameraReferenceSystem = world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();
            return _runtimeCameraReferenceSystem;
        }

        private VisualQualitySettingsSystem ResolveVisualQualitySettingsSystem(World world)
        {
            if (world == null || !world.IsCreated)
                return null;

            BindVisualQualitySettingsSystem(world.GetOrCreateSystemManaged<VisualQualitySettingsSystem>());
            return _visualQualitySettingsSystem;
        }

        internal bool IsRuntimeSettingsChangeSubscribed => _runtimeSettingsChangeSubscribed;

        internal void BindVisualQualitySettingsSystem(VisualQualitySettingsSystem system)
        {
            _visualQualitySettingsSystem = system;
            ApplyLatestVisualQualitySettings();
        }

        internal void BindDayNightSystem(DayNightSystem system)
        {
            DayNight = system;
            ApplyVisualQualityEnvironmentPolicy();
        }

        internal static VisualQualityRuntimeMode ToVisualQualityRuntimeMode(UIGraphicsQuality quality)
        {
            return quality switch
            {
                UIGraphicsQuality.Low => VisualQualityRuntimeMode.Low,
                UIGraphicsQuality.Balanced => VisualQualityRuntimeMode.Medium,
                UIGraphicsQuality.High => VisualQualityRuntimeMode.High,
                UIGraphicsQuality.Ultra => VisualQualityRuntimeMode.Ultra,
                _ => VisualQualityRuntimeMode.High
            };
        }

        private RuntimeRootSceneSystemHelper ResolveRuntimeRootSceneSystemHelper()
        {
            _runtimeRootSceneSystemHelper ??= new RuntimeRootSceneSystemHelper();
            return _runtimeRootSceneSystemHelper;
        }

        private static UnitAttackTracePresentationSystemHelper ResolveUnitAttackTracePresentationSystemHelper()
        {
            return new UnitAttackTracePresentationSystemHelper();
        }

        private UnitImpostorPresentationSystemHelper ResolveUnitImpostorPresentationSystemHelper()
        {
            return new UnitImpostorPresentationSystemHelper(_prefabPreviewCache);
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

            if (rtsSelectionConfig != null)
                view.ApplyStyle(rtsSelectionConfig.SelectionFill, rtsSelectionConfig.SelectionBorder);
            return view;
        }

        private void InitializeManagedRuntimeIfNeeded()
        {
            if (_managedRuntimeInitialized)
                return;

            InitializeVisualQualitySettingsIfNeeded();
            InitializeStaticMapBatchingIfNeeded();
            int ownerLayer = sceneView != null ? sceneView.gameObject.layer : 0;

            ManagedGameplayStartupSystemHelper.Result managedSystems = InitializeManagedRuntime(
                DayNightConfig,
                FactionVisualConfig,
                RoadBuildConfig,
                BuildingPlacementConfig,
                MapBuildingPlacementConfig,
                MapVehiclePlacementConfig,
                RtsSelectionConfig,
                RuntimeCitySpawnerConfig,
                GameStringsConfig,
                WorldCamera,
                DirectionalLight,
                GlobalVolume,
                _runtimeTransportsRoot,
                _runtimeUiRoot,
                EnsureSelectionRectangleView,
                MapBuildingAuthoringRoot,
                MapVehicleAuthoringRoot,
                matchIntroStateQuery);

            BindDayNightSystem(managedSystems.DayNight);
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
            BuildingUiCommandContract = new BuildingUiCommandAdapter(BuildingUiCommand, _buildingUiCommandContext);
            BuildingUiQueryContract = new BuildingUiQueryAdapter(BuildingUiQuery, _buildingUiQueryContext);
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
            EnsureUiRuntimeAdapters();
            if (SelectionRectangle is SelectionRectangleView selectionRectangleView)
                selectionRectangleView.BindState(_selectionRectangleState);
            InitializeRenderingSystems(ownerLayer);
            _disposeCitizenPopulation = managedSystems.DisposeCitizenPopulation;
            _citizenPopulationRuntimeUpdate = managedSystems.CitizenPopulationComposition != null
                ? managedSystems.CitizenPopulationComposition.RuntimeUpdateSystem.Update
                : null;
            _citizenPopulationReadModel = managedSystems.CitizenPopulationComposition?.ReadModel;
            _citizenPopulationEventSystem = managedSystems.CitizenPopulationComposition?.EventSystem;
            _buildingRuntimeBoundaryEntity = MatchBuildingRuntimeBootstrapStartupSystemHelper.Ensure(_buildingRuntimeBoundaryEntity);
            ResolveRuntimeCameraReferenceSystem(runtimeWorld)?.SetWorldCamera(WorldCamera);
            _managedRuntimeInitialized = true;
            if (MainMenu != null)
            {
                MainMenu.Init(SelectionUiCommand, _matchRuntimeState, _matchHudCameraControl, _matchHudMinimapDataSource, gameTextResolver, resetRuntimeState: false);
                ApplyMainMenuBaseBindings();
            }

            if (_pendingMatchHudSelectionPanelView != null)
                BindMatchHudSelectionPanel(_pendingMatchHudSelectionPanelView);
        }

        private void InitializeRenderingSystems(int ownerLayer)
        {
            UnitAttackTracePresentationSystemHelper unitAttackTraces = ResolveUnitAttackTracePresentationSystemHelper();
            unitAttackTraces?.Init(UnitAttackTraceConfig, WorldCamera, ownerLayer);
            UnitAttackTraces = unitAttackTraces;

            UnitImpostorPresentationSystemHelper unitImpostors = ResolveUnitImpostorPresentationSystemHelper();
            unitImpostors?.Init(
                WorldCamera,
                ownerLayer,
                BuildingPlacementConfig != null ? BuildingPlacementConfig.UnitPrefabRegistryConfig : null,
                UnitRenderingMetadataAuthoringSystem.TryGetUnitRenderingMetadata);
            UnitImpostors = unitImpostors;

            _prefabPreviewCache.ConfigureUnitRenderingMetadataResolver(UnitRenderingMetadataAuthoringSystem.TryGetUnitRenderingMetadata);
            _prefabPreviewCache.Init(PrefabPreviewCameraConfig);
        }

        private void EnsureUiRuntimeAdapters()
        {
            _matchRuntimeState ??= new MatchRuntimeStateAdapter(_runtimeGameplayStateSystem);
            _matchHudCameraControl = new MatchHudCameraControlAdapter(SelectionUiCamera);
            _matchHudMinimapDataSource ??= new MatchHudMinimapDataSourceAdapter();
            _selectionRectangleState ??= new SelectionRectangleStateAdapter(
                _matchRuntimeState,
                runtimeWorld.EntityManager);
        }

        private void InitializeVisualQualitySettingsIfNeeded()
        {
            if (_visualQualitySettingsInitialized)
                return;

            VisualQualitySettingsSystem visualQualitySettingsSystem = ResolveVisualQualitySettingsSystem(runtimeWorld);
            if (visualQualitySettingsSystem == null)
                return;

            visualQualitySettingsSystem.Initialize(VisualQualityProfile, WorldCamera, DirectionalLight, GlobalVolume);
            _visualQualitySettingsInitialized = true;
            ApplyLatestVisualQualitySettings();
        }

        private void EnsureRuntimeSettingsChangeSubscription()
        {
            if (_runtimeSettingsChangeSubscribed)
                return;

            SettingsService.RuntimeApplied += OnRuntimeSettingsApplied;
            _runtimeSettingsChangeSubscribed = true;
        }

        private void ReleaseRuntimeSettingsChangeSubscription()
        {
            if (!_runtimeSettingsChangeSubscribed)
                return;

            SettingsService.RuntimeApplied -= OnRuntimeSettingsApplied;
            _runtimeSettingsChangeSubscribed = false;
            _hasLatestRuntimeSettings = false;
            _latestRuntimeSettings = default;
        }

        private void OnRuntimeSettingsApplied(UISettingsModel settings)
        {
            _latestRuntimeSettings = settings;
            _hasLatestRuntimeSettings = true;
            ApplyLatestVisualQualitySettings();
        }

        private void ApplyLatestVisualQualitySettings()
        {
            if (!_hasLatestRuntimeSettings ||
                _visualQualitySettingsSystem == null ||
                !_visualQualitySettingsSystem.IsInitialized)
            {
                return;
            }

            bool tierChanged = _visualQualitySettingsSystem.ApplyRuntimeMode(
                ToVisualQualityRuntimeMode(_latestRuntimeSettings.Graphics.Quality));
            if (tierChanged)
                ApplyVisualQualityEnvironmentPolicy();
        }

        private void ApplyVisualQualityEnvironmentPolicy()
        {
            if (DayNight == null || _visualQualitySettingsSystem == null)
                return;

            DayNight.SetQualityShadowStrengthCap(_visualQualitySettingsSystem.AppliedShadowStrengthCap);
            DayNight.ReapplyVisualStateAfterQualityChange();
        }

        private void InitializeStaticMapBatchingIfNeeded()
        {
            if (_staticMapBatchingInitialized)
                return;

            Transform mapRoot = ResolveStaticMapRoot();
            if (mapRoot == null)
                return;

            mapVisuals.Initialize(Application.platform, sceneView.StaticMapPresentationManifest, mapRoot,
                MapBuildingAuthoringRoot,
                MapVehicleAuthoringRoot,
                DecorationRoot);
            _staticMapBatchingInitialized = true;
        }

        private Transform ResolveStaticMapRoot()
        {
            Transform current = MapSurfaceAuthoring != null ? MapSurfaceAuthoring.transform : null;
            while (current != null)
            {
                if (string.Equals(current.name, "Map", StringComparison.Ordinal))
                    return current;

                current = current.parent;
            }

            return null;
        }

        private void InitializeGameplaySystemsIfNeeded()
        {
            if (GameplayInitialized)
                return;

            GameplayFeatureStartupCompositionSystemHelper.Result gameplaySystems = _gameplayFeatureStartupSystem.Initialize(
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
                sceneView != null ? sceneView.RuntimeGridDebugViews : null,
                _gameplaySceneBindingSystem);

            RuntimeCity = gameplaySystems.RuntimeCity;
            RuntimeGridBlockers = gameplaySystems.RuntimeGridBlockers;
            RuntimeDecorations = gameplaySystems.RuntimeDecorations;
            GameplayInitialized = true;
            ApplyMainMenuFeatureBindingsIfReady();
        }

    }
}
