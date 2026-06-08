using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class MatchSceneView : MonoBehaviour
{
    private readonly MatchBootstrapSystem matchBootstrapSystem = new();

    [Header("Scene Refs")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Light directionalLight;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private CombinedMeshBaker decorationCombinedMeshBaker;
    [SerializeField] private Transform decorationRoot;
    [SerializeField] private Transform mapBuildingAuthoringRoot;
    [SerializeField] private MapSurfaceAuthoring mapSurfaceAuthoring;

    [Header("Configs")]
    [SerializeField] private RTSSelectionSystemConfig rtsSelectionConfig;
    [SerializeField] private RoadBuildSystemConfig roadBuildConfig;
    [SerializeField] private BuildingPlacementSystemConfig buildingPlacementConfig;
    [SerializeField] private MapBuildingPlacementConfig mapBuildingPlacementConfig;
    [SerializeField] private UnitAttackTraceSystemConfig unitAttackTraceConfig;
    [SerializeField] private RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig;
    [SerializeField] private RuntimeDecorationSpawnerSystemConfig runtimeDecorationSpawnerConfig;
    [SerializeField] private RuntimeGridBlockerSystemConfig runtimeGridBlockerConfig;
    [SerializeField] private GridAuthoringConfig runtimeGridConfig;
    [SerializeField] private DayNightSystemConfig dayNightConfig;
    [SerializeField] private FactionVisualSettingsConfig factionVisualConfig;
    [SerializeField] private GameStringsConfig gameStringsConfig;
    [SerializeField] private PrefabPreviewCameraConfig prefabPreviewCameraConfig;
    [SerializeField] private AIPlanEntryStartupConfig aiPlanEntryConfig;
    [SerializeField] private List<AIControllerConfig> aiControllerConfigs = new();

    public Camera WorldCamera => worldCamera;
    public Light DirectionalLight => directionalLight;
    public Volume GlobalVolume => globalVolume;
    public CombinedMeshBaker DecorationCombinedMeshBaker => decorationCombinedMeshBaker;
    public Transform DecorationRoot => decorationRoot != null ? decorationRoot : (decorationCombinedMeshBaker != null ? decorationCombinedMeshBaker.transform : null);
    public Transform MapBuildingAuthoringRoot => mapBuildingAuthoringRoot;
    public MapSurfaceAuthoring MapSurfaceAuthoring => mapSurfaceAuthoring;
    public RTSSelectionSystemConfig RtsSelectionConfig => rtsSelectionConfig;
    public RoadBuildSystemConfig RoadBuildConfig => roadBuildConfig;
    public BuildingPlacementSystemConfig BuildingPlacementConfig => buildingPlacementConfig;
    public MapBuildingPlacementConfig MapBuildingPlacementConfig => mapBuildingPlacementConfig;
    public UnitAttackTraceSystemConfig UnitAttackTraceConfig => unitAttackTraceConfig;
    public RuntimeCitySpawnerSystemConfig RuntimeCitySpawnerConfig => runtimeCitySpawnerConfig;
    public RuntimeDecorationSpawnerSystemConfig RuntimeDecorationSpawnerConfig => runtimeDecorationSpawnerConfig;
    public RuntimeGridBlockerSystemConfig RuntimeGridBlockerConfig => runtimeGridBlockerConfig;
    public GridAuthoringConfig RuntimeGridConfig => runtimeGridConfig;
    public DayNightSystemConfig DayNightConfig => dayNightConfig;
    public FactionVisualSettingsConfig FactionVisualConfig => factionVisualConfig;
    public GameStringsConfig GameStringsConfig => gameStringsConfig;
    public PrefabPreviewCameraConfig PrefabPreviewCameraConfig => prefabPreviewCameraConfig;
    public AIPlanEntryStartupConfig AIPlanEntryConfig => aiPlanEntryConfig;
    public IReadOnlyList<AIControllerConfig> AIControllerConfigs => aiControllerConfigs;

    internal MatchBootstrapSystem MatchBootstrap => matchBootstrapSystem;
    public bool GameplayStartRequested => matchBootstrapSystem.GameplayStartRequested;
    public bool GameplayStartComplete => matchBootstrapSystem.GameplayStartComplete;
    public float GameplayStartProgress01 => matchBootstrapSystem.GameplayStartProgress01;
    public string GameplayStartStatus => matchBootstrapSystem.GameplayStartStatus;

    public void BeginGameplay()
    {
        matchBootstrapSystem.BeginGameplay();
    }

    private void Awake()
    {
        matchBootstrapSystem.Awake(this, transform, gameObject.layer);
    }

    private void Start()
    {
        matchBootstrapSystem.Start();
    }

    private void Update()
    {
        matchBootstrapSystem.Update();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        matchBootstrapSystem.OnApplicationFocus(hasFocus);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        matchBootstrapSystem.OnApplicationPause(pauseStatus);
    }

    private void LateUpdate()
    {
        matchBootstrapSystem.LateUpdate();
    }

    private void OnGUI()
    {
        matchBootstrapSystem.OnGUI();
    }

    private void OnDestroy()
    {
        matchBootstrapSystem.OnDestroy();
    }
}
