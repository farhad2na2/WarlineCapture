using UnityEngine;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Game.Scripts.UI;

[DisallowMultipleComponent]
public sealed class GameBootstrap : MonoBehaviour
{
    private const double FreezeLogThresholdSeconds = 0.15d;
    private static readonly bool EnableFrameRateDiagnostics = true;
    private static readonly bool EnableSlowFrameDiagnostics = true;
    private const double LowFpsDiagThreshold = 30d;
    private const double FrameRateDiagIntervalSeconds = 2d;
    private const double FpsUiUpdateIntervalSeconds = 0.25d;
    private const double SlowFrameDiagThresholdSeconds = 0.025d;
    private const double SlowFrameDiagCooldownSeconds = 0.5d;
    private const int MaxAutoProfilerMarkerRecorders = 32;
    private const float M01PlayableStartOrthographicSize = 0.96f;
    private const float M01PlayableCameraHeight = 10f;
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RuntimeCameraReferenceSystem _runtimeCameraReferenceSystem = new();
    private readonly AIStartupSystem _aiStartupSystem = new();

    [Header("Scene Refs")]
    [SerializeField] private MenuView menuView;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Light directionalLight;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private CombinedMeshBaker decorationCombinedMeshBaker;
    [SerializeField] private Transform decorationRoot;
    [SerializeField] private Chapter01MissionTacticalRuntimeBinder chapter01TacticalBinder;
    [SerializeField] private GameObject[] legacyVisualRootsDisabledForM01 = Array.Empty<GameObject>();

    [Header("Configs")]
    [SerializeField] private RTSSelectionSystemConfig rtsSelectionConfig;
    [SerializeField] private RoadBuildSystemConfig roadBuildConfig;
    [SerializeField] private BuildingPlacementSystemConfig buildingPlacementConfig;
    [SerializeField] private UnitAttackTraceSystemConfig unitAttackTraceConfig;
    [SerializeField] private RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig;
    [SerializeField] private RuntimeDecorationSpawnerSystemConfig runtimeDecorationSpawnerConfig;
    [SerializeField] private RuntimeGridBlockerSystemConfig runtimeGridBlockerConfig;
    [SerializeField] private DayNightSystemConfig dayNightConfig;
    [SerializeField] private FactionVisualSettingsConfig factionVisualConfig;
    [SerializeField] private GameStringsConfig gameStringsConfig;
    [SerializeField] private PrefabPreviewCameraConfig prefabPreviewCameraConfig;
    [SerializeField] private List<AIControllerConfig> aiControllerConfigs = new();

    public Camera WorldCamera => worldCamera;
    public Light DirectionalLight => directionalLight;
    public Volume GlobalVolume => globalVolume;
    public CombinedMeshBaker DecorationCombinedMeshBaker => decorationCombinedMeshBaker;
    public Transform DecorationRoot => decorationRoot != null ? decorationRoot : (decorationCombinedMeshBaker != null ? decorationCombinedMeshBaker.transform : null);
    public Chapter01MissionTacticalRuntimeBinder Chapter01TacticalBinder => chapter01TacticalBinder;

    public RTSSelectionSystemConfig RtsSelectionConfig => rtsSelectionConfig;
    public RoadBuildSystemConfig RoadBuildConfig => roadBuildConfig;
    public BuildingPlacementSystemConfig BuildingPlacementConfig => buildingPlacementConfig;
    public UnitAttackTraceSystemConfig UnitAttackTraceConfig => unitAttackTraceConfig;
    public RuntimeCitySpawnerSystemConfig RuntimeCitySpawnerConfig => runtimeCitySpawnerConfig;
    public RuntimeDecorationSpawnerSystemConfig RuntimeDecorationSpawnerConfig => runtimeDecorationSpawnerConfig;
    public RuntimeGridBlockerSystemConfig RuntimeGridBlockerConfig => runtimeGridBlockerConfig;
    public DayNightSystemConfig DayNightConfig => dayNightConfig;
    public GameStringsConfig GameStringsConfig => gameStringsConfig;
    public IReadOnlyList<AIControllerConfig> AIControllerConfigs => aiControllerConfigs;

    public RuntimeGridBlockerSystem RuntimeGridBlockers { get; private set; }
    public RuntimeDecorationSpawnerSystem RuntimeDecorations { get; private set; }
    public RuntimeCitySpawnerSystem RuntimeCitySpawner { get; private set; }
    public RoadBuildSystem RoadBuild { get; private set; }
    public BuildingPlacementSystem BuildingPlacement { get; private set; }
    public RTSSelectionSystem Selection { get; private set; }
    public MainMenuPlayUI MainMenu { get; private set; }
    public DayNightSystem DayNight { get; private set; }
    public FactionVisualSettings FactionVisuals { get; private set; }
    public UnitAttackTraceSystem UnitAttackTraces { get; private set; }
    public UnitImpostorRenderSystem UnitImpostors { get; private set; }
    public CitizenPopulationSystem CitizenPopulation { get; private set; }
    public bool GameplayInitialized { get; private set; }
    private Entity _buildingPlacementRuntimeEntity;
    private bool _gameplayStartPending;
    private Transform _runtimeBlockerRoot;
    private Transform _runtimeCityRoot;
    private Transform _runtimeUiRoot;
    private readonly System.Text.StringBuilder _freezeLogBuilder = new();
    private readonly System.Text.StringBuilder _lastStepLogBuilder = new();
    private readonly Dictionary<string, StepPerfStats> _stepPerfStats = new();
    private double _lastUpdateTimestamp;
    private double _suppressFrameGapUntilTimestamp;
    private double _nextFrameRateDiagTimestamp;
    private double _nextSlowFrameDiagTimestamp;
    private double _frameRateDiagAccumulatedSeconds;
    private double _frameRateDiagUpdateAccumulatedSeconds;
    private double _frameRateDiagMaxUpdateSeconds;
    private double _fpsUiAccumulatedSeconds;
    private int _frameRateDiagFrames;
    private int _fpsUiFrames;
    private bool _lastApplicationFocused;
    private bool _applicationPaused;
    private int _lastGcGen0Count;
    private int _lastGcGen1Count;
    private int _lastGcGen2Count;
    private ProfilerRecorder _drawCallsRecorder;
    private ProfilerRecorder _batchesRecorder;
    private ProfilerRecorder _setPassCallsRecorder;
    private ProfilerRecorder _trianglesRecorder;
    private ProfilerRecorder _verticesRecorder;
    private readonly List<NamedProfilerRecorder> _markerRecorders = new();

    private struct StepPerfStats
    {
        public double TotalSeconds;
        public double MaxSeconds;
        public int Samples;
    }

    private struct NamedProfilerRecorder
    {
        public string Name;
        public ProfilerRecorder Recorder;
    }

    private void Awake()
    {
        Application.runInBackground = true;
        _lastApplicationFocused = Application.isFocused;
        _lastUpdateTimestamp = Time.realtimeSinceStartupAsDouble;
        _nextFrameRateDiagTimestamp = _lastUpdateTimestamp + FrameRateDiagIntervalSeconds;
        StartProfilerRecorders();
        _lastGcGen0Count = GC.CollectionCount(0);
        _lastGcGen1Count = GC.CollectionCount(1);
        _lastGcGen2Count = GC.CollectionCount(2);

        EnsureRuntimeRoots();

        DayNight = new DayNightSystem();
        DayNight.Init(dayNightConfig, directionalLight, globalVolume);

        FactionVisuals = new FactionVisualSettings();
        FactionVisuals.Init(factionVisualConfig);

        RoadBuild = new RoadBuildSystem();
        RoadBuild.Init(roadBuildConfig, worldCamera, _runtimeUiRoot, null);

        BuildingPlacement = new BuildingPlacementSystem();
        BuildingPlacement.Init(buildingPlacementConfig, worldCamera, _runtimeUiRoot, RoadBuild, null, FactionVisuals, DayNight);
        EnsureBuildingPlacementRuntimeComponent();

        Selection = new RTSSelectionSystem();
        Selection.Init(rtsSelectionConfig, worldCamera, _runtimeUiRoot, null, RoadBuild, BuildingPlacement, FactionVisuals);

        RoadBuild.BindDependencies(BuildingPlacement);
        BuildingPlacement.BindDependencies(RoadBuild, null, DayNight, Selection);
        Selection.BindDependencies(null, RoadBuild, BuildingPlacement);

        UnitAttackTraces = new UnitAttackTraceSystem();
        UnitAttackTraces.Init(unitAttackTraceConfig, worldCamera, gameObject.layer, FactionVisuals);

        UnitImpostors = new UnitImpostorRenderSystem();
        UnitImpostors.Init(worldCamera, gameObject.layer, buildingPlacementConfig != null ? buildingPlacementConfig.UnitPrefabRegistryConfig : null);

        CitizenPopulation = new CitizenPopulationSystem();
        CitizenPopulation.Init(BuildingPlacement, DayNight, worldCamera);
        BuildingPlacement.BindDependencies(RoadBuild, null, DayNight, Selection, citizenPopulationSystem: CitizenPopulation);
        GameStrings.Init(gameStringsConfig);
        SharedPrefabPreviewCache.Init(prefabPreviewCameraConfig);
        _runtimeCameraReferenceSystem.SetWorldCamera(worldCamera);
    }

    private void Start()
    {
        if (menuView != null)
            menuView.GameRequested += BeginGameplay;

        if (menuView != null)
        {
            menuView.Init(Selection, BuildingPlacement, worldCamera, DayNight, CitizenPopulation);
            menuView.NotifyBootstrapReady();
        }

        try
        {
            MainMenu = new MainMenuPlayUI();
            MainMenu.Init(RoadBuild, BuildingPlacement, Selection, DayNight);
            RoadBuild?.BindDependencies(BuildingPlacement, MainMenu);
            BuildingPlacement?.BindDependencies(RoadBuild, MainMenu, DayNight, Selection);
            Selection?.BindDependencies(MainMenu, RoadBuild, BuildingPlacement);
            BindGameplayUiRuntimeDependencies();
        }
        catch (Exception exception)
        {
            MainMenu = null;
            Debug.LogException(exception);
            RoadBuild?.BindDependencies(BuildingPlacement, null);
            BuildingPlacement?.BindDependencies(RoadBuild, null, DayNight, Selection);
            Selection?.BindDependencies(null, RoadBuild, BuildingPlacement);
            BindGameplayUiRuntimeDependencies();
        }
    }

    public void BeginGameplay()
    {
        GameRuntimeStats.Reset();
        _aiStartupSystem.LogConfigValidation(aiControllerConfigs);
        chapter01TacticalBinder?.TryApplyActiveMission(worldCamera);
        Chapter01M01PlayableRuntime.TryInitializeActiveMission(
            World.DefaultGameObjectInjectionWorld,
            chapter01TacticalBinder != null ? chapter01TacticalBinder.TacticalMapLoader : null,
            out _);
        ApplyM01ProductionSceneVisibility();
        ApplyFixedTacticalMissionGuardrails();
        AIStartupSystem.Result aiStartupResult = _aiStartupSystem.Initialize(
            World.DefaultGameObjectInjectionWorld,
            aiControllerConfigs,
            Chapter01M01PlayableRuntime.IsActiveMission(),
            TryGetConfiguredFactionSpawnCell);
        if (aiStartupResult.HasPlayerAutoMode)
            _runtimeGameplayStateSystem.PlayerAutoModeEnabled = aiStartupResult.PlayerAutoModeEnabled;
        EnsureGameplaySystemsInitialized();
        _gameplayStartPending = true;
        _runtimeGameplayStateSystem.PlayRequested = true;
        _runtimeCameraReferenceSystem.SetWorldCamera(worldCamera);
        _runtimeGameplayStateSystem.SelectionModeActive = false;
        _runtimeGameplayStateSystem.BuildModeActive = false;
        _runtimeGameplayStateSystem.ZoomInHeld = false;
        _runtimeGameplayStateSystem.ZoomOutHeld = false;
        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        _runtimeGameplayStateSystem.FullscreenMapOpen = false;
        _runtimeGameplayStateSystem.FullscreenMapIsoMode = false;
        _runtimeGameplayStateSystem.InitialCameraFocusRequested = false;
        if (!FocusCameraOnM01CameraStart())
            FocusCameraOnConfiguredFactionBase(0);
    }

    private void ApplyFixedTacticalMissionGuardrails()
    {
        if (DayNight == null)
            return;

        DayNight.SetRuntimeVisualsEnabled(!Chapter01M01PlayableRuntime.IsActiveMission());
    }

    private void ApplyM01ProductionSceneVisibility()
    {
        bool hideLegacyVisuals = Chapter01M01PlayableRuntime.IsActiveMission();
        if (legacyVisualRootsDisabledForM01 == null)
            return;

        for (int i = 0; i < legacyVisualRootsDisabledForM01.Length; i++)
        {
            GameObject visualRoot = legacyVisualRootsDisabledForM01[i];
            if (visualRoot != null)
                visualRoot.SetActive(!hideLegacyVisuals);
        }
    }

    private void FocusCameraOnConfiguredFactionBase(byte factionId)
    {
        if (Selection == null || !TryGetConfiguredFactionSpawnCell(factionId, out int2 spawnCell))
            return;

        Vector3 focusWorldPosition = new(spawnCell.x, 0f, spawnCell.y);
        World world = World.DefaultGameObjectInjectionWorld;
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

        Selection.FollowCameraGroundCenterTo(focusWorldPosition);
    }

    private bool FocusCameraOnM01CameraStart()
    {
        if (Selection == null ||
            !Chapter01M01PlayableRuntime.TryGetCameraStartWorld(
                chapter01TacticalBinder != null ? chapter01TacticalBinder.TacticalMapLoader : null,
                out Vector3 cameraStartWorld))
        {
            return false;
        }

        ApplyM01ProductionCameraPose(cameraStartWorld);
        Selection.FollowCameraGroundCenterTo(cameraStartWorld);
        Selection.MoveCameraGroundCenterTo(cameraStartWorld);
        ApplyM01ProductionCameraPose(cameraStartWorld);
        return true;
    }

    private void ApplyM01ProductionCameraPose(Vector3 cameraStartWorld)
    {
        if (worldCamera == null)
            return;

        worldCamera.orthographic = true;
        worldCamera.orthographicSize = ResolveM01ProductionOrthographicSize();
        worldCamera.nearClipPlane = Mathf.Min(worldCamera.nearClipPlane, 0.05f);
        worldCamera.farClipPlane = Mathf.Max(worldCamera.farClipPlane, M01PlayableCameraHeight + 10f);
        cameraStartWorld = ClampM01CameraCenterToTacticalMap(cameraStartWorld);
        worldCamera.transform.SetPositionAndRotation(
            new Vector3(cameraStartWorld.x, M01PlayableCameraHeight, cameraStartWorld.z),
            Quaternion.Euler(90f, 0f, 0f));
    }

    private float ResolveM01ProductionOrthographicSize()
    {
        TacticalMapRuntimeLoader loader = chapter01TacticalBinder != null ? chapter01TacticalBinder.TacticalMapLoader : null;
        TacticalMapDefinition definition = loader != null ? loader.Definition : null;
        if (definition == null || worldCamera == null || worldCamera.aspect <= 0.0001f)
            return M01PlayableStartOrthographicSize;

        float widthFitOrthographicSize = definition.VisibleWorldSize.x / (2f * worldCamera.aspect);
        return Mathf.Clamp(widthFitOrthographicSize, 0.72f, M01PlayableStartOrthographicSize);
    }

    public bool ApplyM01ProductionCameraPoseForCurrentAspect()
    {
        if (!Chapter01M01PlayableRuntime.TryGetCameraStartWorld(
                chapter01TacticalBinder != null ? chapter01TacticalBinder.TacticalMapLoader : null,
                out Vector3 cameraStartWorld))
        {
            return false;
        }

        Vector3 cameraCenter = TryResolveM01ProductionFrameCenter(out Vector3 productionFrameCenter)
            ? productionFrameCenter
            : cameraStartWorld;
        ApplyM01ProductionCameraPose(cameraCenter);
        return true;
    }

    private bool TryResolveM01ProductionFrameCenter(out Vector3 cameraCenter)
    {
        cameraCenter = default;
        TacticalMapRuntimeLoader loader = chapter01TacticalBinder != null ? chapter01TacticalBinder.TacticalMapLoader : null;
        if (loader == null)
            return false;

        bool hasAny = false;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;
        IncludeM01FrameAnchor(loader, Chapter01M01PlayableRuntime.PlayerSpawnAnchorId, ref min, ref max, ref hasAny);
        IncludeM01FrameAnchor(loader, Chapter01M01PlayableRuntime.EnemySpawnAnchorId, ref min, ref max, ref hasAny);
        IncludeM01FrameAnchor(loader, Chapter01M01PlayableRuntime.DecorCommandPointEntityId, ref min, ref max, ref hasAny);
        IncludeM01FrameAnchor(loader, Chapter01M01PlayableRuntime.ObjectiveAnchorId, ref min, ref max, ref hasAny);
        if (!hasAny)
            return false;

        cameraCenter = (min + max) * 0.5f;
        cameraCenter.y = 0f;
        return true;
    }

    private static void IncludeM01FrameAnchor(TacticalMapRuntimeLoader loader, string anchorId, ref Vector3 min, ref Vector3 max, ref bool hasAny)
    {
        if (loader == null || !loader.TryGetAnchorWorldPosition(anchorId, out Vector3 world))
            return;

        if (!hasAny)
        {
            min = world;
            max = world;
            hasAny = true;
            return;
        }

        min = Vector3.Min(min, world);
        max = Vector3.Max(max, world);
    }

    private void ApplyM01ProductionCameraPoseIfActive()
    {
        ApplyM01ProductionCameraPoseForCurrentAspect();
    }

    private Vector3 ClampM01CameraCenterToTacticalMap(Vector3 cameraCenter)
    {
        TacticalMapRuntimeLoader loader = chapter01TacticalBinder != null ? chapter01TacticalBinder.TacticalMapLoader : null;
        TacticalMapDefinition definition = loader != null ? loader.Definition : null;
        if (definition == null || worldCamera == null || !worldCamera.orthographic)
            return cameraCenter;

        float halfHeight = worldCamera.orthographicSize;
        float halfWidth = halfHeight * worldCamera.aspect;
        float xMin = definition.WorldOrigin.x + halfWidth;
        float xMax = definition.WorldOrigin.x + definition.VisibleWorldSize.x - halfWidth;
        float zMin = definition.WorldOrigin.y + halfHeight;
        float zMax = definition.WorldOrigin.y + definition.VisibleWorldSize.y - halfHeight;
        float mapCenterX = definition.WorldOrigin.x + definition.VisibleWorldSize.x * 0.5f;
        float mapCenterZ = definition.WorldOrigin.y + definition.VisibleWorldSize.y * 0.5f;

        cameraCenter.x = xMin <= xMax
            ? Mathf.Clamp(cameraCenter.x, xMin, xMax)
            : mapCenterX;
        cameraCenter.z = zMin <= zMax
            ? Mathf.Clamp(cameraCenter.z, zMin, zMax)
            : mapCenterZ;
        return cameraCenter;
    }

    private bool TryGetConfiguredFactionSpawnCell(byte factionId, out int2 spawnCell)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
            {
                Entity entity = entities[entityIndex];
                if (!em.Exists(entity) || !em.HasBuffer<InitialUnitsFactionSpawnEntry>(entity))
                    continue;

                DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns = em.GetBuffer<InitialUnitsFactionSpawnEntry>(entity);
                for (int i = 0; i < factionSpawns.Length; i++)
                {
                    if (factionSpawns[i].FactionId != factionId)
                        continue;

                    spawnCell = factionSpawns[i].SpawnCell;
                    return true;
                }
            }
        }

        InitialUnitsSpawnerAuthoringConfig initialUnitsConfig = buildingPlacementConfig != null
            ? buildingPlacementConfig.InitialUnitsConfig
            : null;
        if (initialUnitsConfig != null && initialUnitsConfig.Factions != null)
        {
            for (int i = 0; i < initialUnitsConfig.Factions.Count; i++)
            {
                InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = initialUnitsConfig.Factions[i];
                if (faction == null || faction.FactionId != factionId)
                    continue;

                spawnCell = new int2(faction.SpawnCell.x, faction.SpawnCell.y);
                return true;
            }
        }

        spawnCell = default;
        return false;
    }

    private void Update()
    {
        bool gameplayActive = GameplayInitialized && _runtimeGameplayStateSystem.PlayRequested;
        double now = Time.realtimeSinceStartupAsDouble;
        bool applicationFocused = Application.isFocused;
        if (applicationFocused != _lastApplicationFocused)
        {
            _lastApplicationFocused = applicationFocused;
            _lastUpdateTimestamp = now;
            _suppressFrameGapUntilTimestamp = now + 0.5d;
        }

        bool canReportFrameGap =
            gameplayActive &&
            applicationFocused &&
            !_applicationPaused &&
            now >= _suppressFrameGapUntilTimestamp;

        if (canReportFrameGap && _lastUpdateTimestamp > 0d)
        {
            double gapSeconds = now - _lastUpdateTimestamp;
            if (gapSeconds >= FreezeLogThresholdSeconds)
            {
                Debug.Log($"[FreezeDetect] Frame gap frame={Time.frameCount} Gap={(gapSeconds * 1000d):F1}ms GC={BuildGcDeltaString()} LastSteps={_lastStepLogBuilder}");
            }
        }
        _lastUpdateTimestamp = now;

        double frameStart = Time.realtimeSinceStartupAsDouble;
        _freezeLogBuilder.Clear();
        _lastStepLogBuilder.Clear();
        bool hadSlowStep = false;

        hadSlowStep |= TimedStep("MenuCanvasInput", () => menuView?.SyncInputState());
        if (gameplayActive)
        {
            GameRuntimeStats.RecordMissionElapsed(Time.deltaTime);
            hadSlowStep |= TimedStep("Chapter01M01Runtime", () =>
            {
                Chapter01M01PlayableRuntime.TryInitializeActiveMission(
                    World.DefaultGameObjectInjectionWorld,
                    chapter01TacticalBinder != null ? chapter01TacticalBinder.TacticalMapLoader : null,
                    out _);
            });
            hadSlowStep |= TimedStep("RoadBuild", () => RoadBuild?.Update());
            hadSlowStep |= TimedStep("BuildingPlacement", () => BuildingPlacement?.Update());
            hadSlowStep |= TimedStep("Selection", () => Selection?.Update());
            hadSlowStep |= TimedStep("M01ProductionCamera", ApplyM01ProductionCameraPoseIfActive);
            hadSlowStep |= TimedStep("RuntimeCitySpawner", () => RuntimeCitySpawner?.Update());
            hadSlowStep |= TimedStep("RuntimeGridBlockers", () => RuntimeGridBlockers?.Update());
            hadSlowStep |= TimedStep("RuntimeDecorations", () => RuntimeDecorations?.Update());
            hadSlowStep |= TimedStep("DayNight", () => DayNight?.Update());
            hadSlowStep |= TimedStep("CitizenPopulation", () => CitizenPopulation?.Update());
        }
        hadSlowStep |= TimedStep("MenuCanvas", () => menuView?.SyncRuntimeState());
        hadSlowStep |= TimedStep("MainMenu", () => MainMenu?.Update());

        if (_gameplayStartPending && IsGameplayStartComplete())
        {
            _gameplayStartPending = false;
            menuView?.NotifyGameplayReady();
        }

        if (gameplayActive)
            WarlineCaptureMatchResultFlow.TryCompleteActiveMissionFromLoadedScene();

        double totalSeconds = Time.realtimeSinceStartupAsDouble - frameStart;
        RecordUpdateFrameStats(totalSeconds);
        if (gameplayActive && (hadSlowStep || totalSeconds >= FreezeLogThresholdSeconds))
        {
            if (_freezeLogBuilder.Length > 0)
                _freezeLogBuilder.Append(", ");

            _freezeLogBuilder.Append("GC=");
            _freezeLogBuilder.Append(BuildGcDeltaString());
            _freezeLogBuilder.Append(", ");
            _freezeLogBuilder.Append("Total=");
            _freezeLogBuilder.Append((totalSeconds * 1000d).ToString("F1"));
            _freezeLogBuilder.Append("ms");
            Debug.Log($"[FreezeDetect] Update hitch frame={Time.frameCount} {_freezeLogBuilder}");
        }
        LogSlowUpdateDiagnosticsIfNeeded(gameplayActive, totalSeconds, now);

        UpdateFpsLabel();
        UpdateFrameRateDiagnostics(gameplayActive, now);
        CaptureGcCounts();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        _lastApplicationFocused = hasFocus;
        _lastUpdateTimestamp = Time.realtimeSinceStartupAsDouble;
        _suppressFrameGapUntilTimestamp = _lastUpdateTimestamp + 0.5d;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        _applicationPaused = pauseStatus;
        _lastUpdateTimestamp = Time.realtimeSinceStartupAsDouble;
        _suppressFrameGapUntilTimestamp = _lastUpdateTimestamp + 0.5d;
    }

    private void LateUpdate()
    {
        if (!(GameplayInitialized && _runtimeGameplayStateSystem.PlayRequested))
            return;

        double start = Time.realtimeSinceStartupAsDouble;
        UnitAttackTraces?.LateUpdate();
        UnitImpostors?.LateUpdate();
        double elapsed = Time.realtimeSinceStartupAsDouble - start;
        if (elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect] LateUpdate hitch frame={Time.frameCount} UnitRenderLate={(elapsed * 1000d):F1}ms impostors={UnitImpostors?.LastDrawnCount ?? 0} GC={BuildGcDeltaString()}");
    }

    private void OnGUI()
    {
        if (!(GameplayInitialized && _runtimeGameplayStateSystem.PlayRequested))
            return;

        double start = Time.realtimeSinceStartupAsDouble;
        RoadBuild?.OnGui();
        Selection?.OnGui();
        double elapsed = Time.realtimeSinceStartupAsDouble - start;
        if (elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect] OnGUI hitch frame={Time.frameCount} Total={(elapsed * 1000d):F1}ms GC={BuildGcDeltaString()}");
    }

    private void OnDestroy()
    {
        if (menuView != null)
            menuView.GameRequested -= BeginGameplay;

        ClearBuildingPlacementRuntimeComponent();
        MainMenu?.Dispose();
        Selection?.Dispose();
        BuildingPlacement?.Dispose();
        RoadBuild?.Dispose();
        UnitAttackTraces?.Dispose();
        UnitImpostors?.Dispose();
        CitizenPopulation?.Dispose();
        DayNight?.Dispose();
        RuntimeDecorations?.Dispose();
        RuntimeGridBlockers?.Dispose();
        RuntimeCitySpawner?.Dispose();
        MainMenu = null;
        Selection = null;
        BuildingPlacement = null;
        RoadBuild = null;
        FactionVisuals = null;
        UnitAttackTraces = null;
        UnitImpostors = null;
        CitizenPopulation = null;
        DayNight = null;
        RuntimeDecorations = null;
        RuntimeGridBlockers = null;
        RuntimeCitySpawner = null;
        _runtimeCameraReferenceSystem.ClearWorldCamera();
        DisposeProfilerRecorders();
        SharedPrefabPreviewCache.ReleaseAll();
    }

    private void EnsureBuildingPlacementRuntimeComponent()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated || BuildingPlacement == null)
            return;

        EntityManager em = world.EntityManager;
        if (_buildingPlacementRuntimeEntity == Entity.Null || !em.Exists(_buildingPlacementRuntimeEntity))
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingPlacementRuntimeComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                _buildingPlacementRuntimeEntity = query.GetSingletonEntity();
            }
            else
            {
                _buildingPlacementRuntimeEntity = em.CreateEntity();
                em.SetName(_buildingPlacementRuntimeEntity, "BuildingPlacementRuntimeEntity");
                em.AddComponentObject(_buildingPlacementRuntimeEntity, new BuildingPlacementRuntimeComponent());
            }
        }

        BuildingPlacementRuntimeComponent component = em.GetComponentObject<BuildingPlacementRuntimeComponent>(_buildingPlacementRuntimeEntity);
        component.BuildingPlacement = BuildingPlacement;
    }

    private void ClearBuildingPlacementRuntimeComponent()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated || _buildingPlacementRuntimeEntity == Entity.Null)
            return;

        EntityManager em = world.EntityManager;
        if (!em.Exists(_buildingPlacementRuntimeEntity) || !em.HasComponent<BuildingPlacementRuntimeComponent>(_buildingPlacementRuntimeEntity))
            return;

        BuildingPlacementRuntimeComponent component = em.GetComponentObject<BuildingPlacementRuntimeComponent>(_buildingPlacementRuntimeEntity);
        component.BuildingPlacement = null;
    }

    private void StartProfilerRecorders()
    {
        _drawCallsRecorder = StartProfilerRecorder(ProfilerCategory.Render, "Draw Calls Count");
        _batchesRecorder = StartProfilerRecorder(ProfilerCategory.Render, "Batches Count");
        _setPassCallsRecorder = StartProfilerRecorder(ProfilerCategory.Render, "SetPass Calls Count");
        _trianglesRecorder = StartProfilerRecorder(ProfilerCategory.Render, "Triangles Count");
        _verticesRecorder = StartProfilerRecorder(ProfilerCategory.Render, "Vertices Count");
        AddProfilerMarkerRecorder(ProfilerCategory.Internal, "PlayerLoop");
        AddProfilerMarkerRecorder(ProfilerCategory.Internal, "EditorLoop");
        AddProfilerMarkerRecorder(ProfilerCategory.Internal, "Overhead");
        AddProfilerMarkerRecorder(ProfilerCategory.Internal, "WaitForTargetFPS");
        AddProfilerMarkerRecorder(ProfilerCategory.Render, "Camera.Render");
        AddProfilerMarkerRecorder(ProfilerCategory.Render, "RenderPipelineManager.DoRenderLoop_Internal");
        AddProfilerMarkerRecorder(ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread");
        AddProfilerMarkerRecorder(ProfilerCategory.Render, "Gfx.PresentFrame");
        AddProfilerMarkerRecorder(ProfilerCategory.Scripts, "BehaviourUpdate");
        AddProfilerMarkerRecorder(ProfilerCategory.Scripts, "LateBehaviourUpdate");
        AddProfilerMarkerRecorder(ProfilerCategory.Scripts, "Canvas.SendWillRenderCanvases");
        AddAvailablePlayerLoopMarkerRecorders();
    }

    private static ProfilerRecorder StartProfilerRecorder(ProfilerCategory category, string statName)
    {
        try
        {
            return ProfilerRecorder.StartNew(category, statName);
        }
        catch
        {
            return default;
        }
    }

    private void AddProfilerMarkerRecorder(ProfilerCategory category, string statName)
    {
        if (HasProfilerMarkerRecorder(statName))
            return;

        ProfilerRecorder recorder = StartProfilerRecorder(category, statName);
        if (!recorder.Valid)
            return;

        _markerRecorders.Add(new NamedProfilerRecorder
        {
            Name = statName,
            Recorder = recorder
        });
    }

    private void AddAvailablePlayerLoopMarkerRecorders()
    {
        try
        {
            List<ProfilerRecorderHandle> handles = new();
            ProfilerRecorderHandle.GetAvailable(handles);
            int added = 0;
            for (int i = 0; i < handles.Count && added < MaxAutoProfilerMarkerRecorders; i++)
            {
                ProfilerRecorderHandle handle = handles[i];
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                string name = description.Name.ToString();
                if (!ShouldTrackProfilerMarker(name) || HasProfilerMarkerRecorder(name))
                    continue;

                ProfilerRecorder recorder = StartProfilerRecorder(description.Category, name);
                if (!recorder.Valid)
                    continue;

                _markerRecorders.Add(new NamedProfilerRecorder
                {
                    Name = name,
                    Recorder = recorder
                });
                added++;
            }
        }
        catch
        {
            // Marker enumeration is diagnostic-only and can vary by Unity/editor platform.
        }
    }

    private bool HasProfilerMarkerRecorder(string statName)
    {
        for (int i = 0; i < _markerRecorders.Count; i++)
        {
            if (string.Equals(_markerRecorders[i].Name, statName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool ShouldTrackProfilerMarker(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return
            name.Contains("PlayerLoop", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("EditorLoop", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Render", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Camera", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Canvas", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("UI", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Entities", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Script", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Wait", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Present", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Gfx", StringComparison.OrdinalIgnoreCase);
    }

    private void DisposeProfilerRecorders()
    {
        if (_drawCallsRecorder.Valid) _drawCallsRecorder.Dispose();
        if (_batchesRecorder.Valid) _batchesRecorder.Dispose();
        if (_setPassCallsRecorder.Valid) _setPassCallsRecorder.Dispose();
        if (_trianglesRecorder.Valid) _trianglesRecorder.Dispose();
        if (_verticesRecorder.Valid) _verticesRecorder.Dispose();
        for (int i = 0; i < _markerRecorders.Count; i++)
        {
            ProfilerRecorder recorder = _markerRecorders[i].Recorder;
            if (recorder.Valid)
                recorder.Dispose();
        }
        _markerRecorders.Clear();
    }

    private void UpdateFrameRateDiagnostics(bool gameplayActive, double now)
    {
        if (!EnableFrameRateDiagnostics)
            return;

        if (_applicationPaused)
        {
            _frameRateDiagFrames = 0;
            _frameRateDiagAccumulatedSeconds = 0d;
            _frameRateDiagUpdateAccumulatedSeconds = 0d;
            _frameRateDiagMaxUpdateSeconds = 0d;
            _stepPerfStats.Clear();
            _nextFrameRateDiagTimestamp = now + FrameRateDiagIntervalSeconds;
            return;
        }

        _frameRateDiagFrames++;
        _frameRateDiagAccumulatedSeconds += Time.unscaledDeltaTime;
        if (now < _nextFrameRateDiagTimestamp)
            return;

        double averageFrameMs = _frameRateDiagFrames > 0
            ? (_frameRateDiagAccumulatedSeconds * 1000d) / _frameRateDiagFrames
            : 0d;
        double averageFps = averageFrameMs > 0d ? 1000d / averageFrameMs : 0d;
        double updateAverageMs = _frameRateDiagFrames > 0
            ? (_frameRateDiagUpdateAccumulatedSeconds * 1000d) / _frameRateDiagFrames
            : 0d;
        if (averageFps < LowFpsDiagThreshold)
        {
            GetRuntimeUnitCounts(out int units, out int modelInstances);
            string label = gameplayActive ? "FrameRateDiag" : "FrameRateDiag:PreGame";
            string preGameDetails = gameplayActive
                ? string.Empty
                : $" vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} lastSteps={_lastStepLogBuilder}";
            Debug.Log(
                $"[{label}] fps={averageFps:F1} avgFrame={averageFrameMs:F1}ms " +
                $"updateAvg={updateAverageMs:F1}ms updateMax={_frameRateDiagMaxUpdateSeconds * 1000d:F1}ms " +
                $"{BuildFrameTimingDiagString()} " +
                $"drawCalls={ReadProfilerRecorder(_drawCallsRecorder)} batches={ReadProfilerRecorder(_batchesRecorder)} " +
                $"setPass={ReadProfilerRecorder(_setPassCallsRecorder)} tris={ReadProfilerRecorder(_trianglesRecorder)} verts={ReadProfilerRecorder(_verticesRecorder)} " +
                $"units={units} models={modelInstances} impostors={UnitImpostors?.LastDrawnCount ?? 0} " +
                $"memory={BuildMemoryDiagString()} focused={(Application.isFocused ? 1 : 0)}{preGameDetails} " +
                $"stepStats={BuildStepStatsString()} markers={BuildProfilerMarkerDiagString()}");
        }

        _frameRateDiagFrames = 0;
        _frameRateDiagAccumulatedSeconds = 0d;
        _frameRateDiagUpdateAccumulatedSeconds = 0d;
        _frameRateDiagMaxUpdateSeconds = 0d;
        _stepPerfStats.Clear();
        _nextFrameRateDiagTimestamp = now + FrameRateDiagIntervalSeconds;
    }

    private void UpdateFpsLabel()
    {
        if (menuView == null)
            return;

        _fpsUiFrames++;
        _fpsUiAccumulatedSeconds += Time.unscaledDeltaTime;
        if (_fpsUiAccumulatedSeconds < FpsUiUpdateIntervalSeconds)
            return;

        double fps = _fpsUiAccumulatedSeconds > 0d ? _fpsUiFrames / _fpsUiAccumulatedSeconds : 0d;
        menuView.SetFpsLabel(Mathf.RoundToInt((float)fps));
        _fpsUiFrames = 0;
        _fpsUiAccumulatedSeconds = 0d;
    }

    private static long ReadProfilerRecorder(ProfilerRecorder recorder)
    {
        return recorder.Valid ? recorder.LastValue : -1L;
    }

    private static void GetRuntimeUnitCounts(out int units, out int modelInstances)
    {
        units = 0;
        modelInstances = 0;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        using EntityQuery unitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<Faction>());
        using EntityQuery modelQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitModelInstanceReference>());
        units = unitQuery.CalculateEntityCount();
        modelInstances = modelQuery.CalculateEntityCount();
    }

    private void EnsureRuntimeRoots()
    {
        if (_runtimeBlockerRoot == null)
        {
            var runtimeBlockersObject = new GameObject("RuntimeBlockers");
            runtimeBlockersObject.transform.SetParent(transform, false);
            _runtimeBlockerRoot = runtimeBlockersObject.transform;
        }

        if (_runtimeCityRoot == null)
        {
            var runtimeCityObject = new GameObject("RuntimeCity");
            runtimeCityObject.transform.SetParent(transform, false);
            _runtimeCityRoot = runtimeCityObject.transform;
        }

        if (_runtimeUiRoot == null)
        {
            var runtimeUiObject = new GameObject("RuntimeUi");
            runtimeUiObject.transform.SetParent(transform, false);
            _runtimeUiRoot = runtimeUiObject.transform;
        }
    }

    private void EnsureGameplaySystemsInitialized()
    {
        if (GameplayInitialized)
            return;

        RuntimeCitySpawner = new RuntimeCitySpawnerSystem();
        RuntimeCitySpawner.Init(runtimeCitySpawnerConfig, RoadBuild, BuildingPlacement, _runtimeCityRoot, MainMenu);

        RuntimeGridBlockers = new RuntimeGridBlockerSystem();
        RuntimeGridBlockers.Init(runtimeGridBlockerConfig, _runtimeBlockerRoot, RuntimeCitySpawner);
        RoadBuild?.BindDependencies(BuildingPlacement, MainMenu, RuntimeGridBlockers);
        BindRuntimeGridBlockerDebugViews(RuntimeGridBlockers);
        BuildingPlacement?.BindDependencies(
            RoadBuild,
            MainMenu,
            DayNight,
            Selection,
            RuntimeGridBlockers,
            RuntimeCitySpawner,
            CitizenPopulation);

        RuntimeDecorations = new RuntimeDecorationSpawnerSystem();
        RuntimeDecorations.Init(runtimeDecorationSpawnerConfig, DecorationRoot, decorationCombinedMeshBaker, RuntimeCitySpawner, RuntimeGridBlockers);

        GameplayInitialized = true;
    }

    private static void BindRuntimeGridBlockerDebugViews(RuntimeGridBlockerSystem runtimeGridBlockers)
    {
        foreach (GridAuthoring grid in Resources.FindObjectsOfTypeAll<GridAuthoring>())
        {
            if (grid == null || !grid.gameObject.scene.IsValid())
                continue;

            grid.BindRuntimeGridBlockers(runtimeGridBlockers);
        }
    }

    private void BindGameplayUiRuntimeDependencies()
    {
        TacticalMapRuntimeLoader loader = chapter01TacticalBinder != null ? chapter01TacticalBinder.TacticalMapLoader : null;
        World world = World.DefaultGameObjectInjectionWorld;

        foreach (MatchOverlayCommandControlsController controls in Resources.FindObjectsOfTypeAll<MatchOverlayCommandControlsController>())
        {
            if (IsLoadedSceneObject(controls))
                controls.BindDependencies(Selection);
        }

        foreach (AssistantRuntimeBinding binding in Resources.FindObjectsOfTypeAll<AssistantRuntimeBinding>())
        {
            if (!IsLoadedSceneObject(binding))
                continue;

            WarlineCaptureRouter router = binding.GetComponentInParent<WarlineCaptureRouter>(true);
            WarlineCaptureMatchResultFlow resultFlow = binding.GetComponentInParent<WarlineCaptureMatchResultFlow>(true);
            MatchObjectivePanelController objectivePanel = binding.GetComponentInParent<MatchObjectivePanelController>(true);
            BattleHudGameplayBridge bridge = binding.GetComponentInParent<BattleHudGameplayBridge>(true);
            if (bridge == null)
                bridge = FindLoadedSceneComponent<BattleHudGameplayBridge>();

            binding.BindRuntimeDependencies(
                world,
                loader,
                Selection,
                bridge,
                router,
                resultFlow,
                objectivePanel);
        }
    }

    private static T FindLoadedSceneComponent<T>() where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (IsLoadedSceneObject(component))
                return component;
        }

        return null;
    }

    private static bool IsLoadedSceneObject(Component component)
    {
        return component != null &&
            component.gameObject != null &&
            component.gameObject.scene.IsValid() &&
            component.gameObject.scene.isLoaded;
    }

    private bool IsGameplayStartComplete()
    {
        if (!GameplayInitialized || !_runtimeGameplayStateSystem.PlayRequested)
            return false;
        if (RuntimeCitySpawner != null && !RuntimeCitySpawner.HasSpawned)
            return false;
        if (RuntimeGridBlockers != null && !RuntimeGridBlockers.HasSpawned)
            return false;
        if (RuntimeDecorations != null && !RuntimeDecorations.HasSpawned)
            return false;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        EntityQuery allSpawnConfigs = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        EntityQuery initializedSpawnConfigs = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());

        int totalConfigCount = allSpawnConfigs.CalculateEntityCount();
        int initializedConfigCount = initializedSpawnConfigs.CalculateEntityCount();
        allSpawnConfigs.Dispose();
        initializedSpawnConfigs.Dispose();

        return totalConfigCount == 0 || initializedConfigCount >= totalConfigCount;
    }

    private bool TimedStep(string name, System.Action action)
    {
        double start = Time.realtimeSinceStartupAsDouble;
        action?.Invoke();
        double elapsed = Time.realtimeSinceStartupAsDouble - start;
        RecordStepStats(name, elapsed);

        if (_lastStepLogBuilder.Length > 0)
            _lastStepLogBuilder.Append(", ");

        _lastStepLogBuilder.Append(name);
        _lastStepLogBuilder.Append('=');
        _lastStepLogBuilder.Append((elapsed * 1000d).ToString("F1"));
        _lastStepLogBuilder.Append("ms");

        if (elapsed < FreezeLogThresholdSeconds)
            return false;

        if (_freezeLogBuilder.Length > 0)
            _freezeLogBuilder.Append(", ");

        _freezeLogBuilder.Append(name);
        _freezeLogBuilder.Append('=');
        _freezeLogBuilder.Append((elapsed * 1000d).ToString("F1"));
        _freezeLogBuilder.Append("ms");
        return true;
    }

    private void RecordUpdateFrameStats(double totalSeconds)
    {
        _frameRateDiagUpdateAccumulatedSeconds += totalSeconds;
        if (totalSeconds > _frameRateDiagMaxUpdateSeconds)
            _frameRateDiagMaxUpdateSeconds = totalSeconds;
    }

    private void RecordStepStats(string name, double elapsed)
    {
        if (!_stepPerfStats.TryGetValue(name, out StepPerfStats stats))
            stats = default;

        stats.TotalSeconds += elapsed;
        stats.Samples++;
        if (elapsed > stats.MaxSeconds)
            stats.MaxSeconds = elapsed;
        _stepPerfStats[name] = stats;
    }

    private void LogSlowUpdateDiagnosticsIfNeeded(bool gameplayActive, double totalSeconds, double now)
    {
        if (!EnableSlowFrameDiagnostics || totalSeconds < SlowFrameDiagThresholdSeconds || now < _nextSlowFrameDiagTimestamp)
            return;

        _nextSlowFrameDiagTimestamp = now + SlowFrameDiagCooldownSeconds;
        GetRuntimeUnitCounts(out int units, out int modelInstances);
        string label = gameplayActive ? "PerfDiag" : "PerfDiag:PreGame";
        Debug.Log(
            $"[{label}] slowUpdate frame={Time.frameCount} total={totalSeconds * 1000d:F1}ms " +
            $"gc={BuildGcDeltaString()} {BuildFrameTimingDiagString()} steps={_lastStepLogBuilder} units={units} models={modelInstances} " +
            $"drawCalls={ReadProfilerRecorder(_drawCallsRecorder)} batches={ReadProfilerRecorder(_batchesRecorder)} " +
            $"setPass={ReadProfilerRecorder(_setPassCallsRecorder)} tris={ReadProfilerRecorder(_trianglesRecorder)} verts={ReadProfilerRecorder(_verticesRecorder)} " +
            $"memory={BuildMemoryDiagString()} uiToolkit=0 " +
            $"gameplayInitialized={(GameplayInitialized ? 1 : 0)} playRequested={(_runtimeGameplayStateSystem.PlayRequested ? 1 : 0)} " +
            $"focused={(Application.isFocused ? 1 : 0)} vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} " +
            $"markers={BuildProfilerMarkerDiagString()}");
    }

    private string BuildStepStatsString()
    {
        if (_stepPerfStats.Count == 0)
            return "none";

        System.Text.StringBuilder builder = new();
        foreach (KeyValuePair<string, StepPerfStats> pair in _stepPerfStats)
        {
            StepPerfStats stats = pair.Value;
            double avgMs = stats.Samples > 0 ? (stats.TotalSeconds * 1000d) / stats.Samples : 0d;
            if (builder.Length > 0)
                builder.Append("|");
            builder.Append(pair.Key);
            builder.Append(":avg=");
            builder.Append(avgMs.ToString("F1"));
            builder.Append("ms,max=");
            builder.Append((stats.MaxSeconds * 1000d).ToString("F1"));
            builder.Append("ms");
        }

        return builder.ToString();
    }

    private static string BuildMemoryDiagString()
    {
        return
            $"alloc={Profiler.GetTotalAllocatedMemoryLong() / (1024L * 1024L)}MB " +
            $"reserved={Profiler.GetTotalReservedMemoryLong() / (1024L * 1024L)}MB " +
            $"mono={Profiler.GetMonoUsedSizeLong() / (1024L * 1024L)}MB";
    }

    private static string BuildFrameTimingDiagString()
    {
        FrameTimingManager.CaptureFrameTimings();
        FrameTiming[] timings = new FrameTiming[1];
        uint count = FrameTimingManager.GetLatestTimings(1, timings);
        if (count == 0)
            return "frameTiming=unavailable";

        FrameTiming timing = timings[0];
        return
            $"cpuFrame={timing.cpuFrameTime:F1}ms " +
            $"cpuMain={timing.cpuMainThreadFrameTime:F1}ms " +
            $"cpuRender={timing.cpuRenderThreadFrameTime:F1}ms " +
            $"gpu={timing.gpuFrameTime:F1}ms";
    }

    private string BuildProfilerMarkerDiagString()
    {
        if (_markerRecorders.Count == 0)
            return "none";

        System.Text.StringBuilder builder = new();
        for (int i = 0; i < _markerRecorders.Count; i++)
        {
            NamedProfilerRecorder entry = _markerRecorders[i];
            if (!entry.Recorder.Valid)
                continue;

            long value = entry.Recorder.LastValue;
            if (value <= 0)
                continue;

            if (builder.Length > 0)
                builder.Append("|");
            builder.Append(entry.Name);
            builder.Append("=");
            builder.Append((value / 1000000d).ToString("F1"));
            builder.Append("ms");
        }

        return builder.Length > 0 ? builder.ToString() : "none-active";
    }

    private string BuildGcDeltaString()
    {
        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        return $"{gen0 - _lastGcGen0Count}/{gen1 - _lastGcGen1Count}/{gen2 - _lastGcGen2Count}";
    }

    private void CaptureGcCounts()
    {
        _lastGcGen0Count = GC.CollectionCount(0);
        _lastGcGen1Count = GC.CollectionCount(1);
        _lastGcGen2Count = GC.CollectionCount(2);
    }
}
