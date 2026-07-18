using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Rendering;
using Game.Runtime;

namespace Game.Composition
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MatchSceneView : MonoBehaviour
    {
        private readonly MatchBootstrapCompositionSystemHelper matchBootstrapSystem = new();
        private readonly List<AudioListener> audioListenersDisabledForMatch = new();
        private readonly List<GameObject> compositionSceneRoots = new(4);
        private AudioListener menuAudioListener;
        private bool matchRuntimeBound;
        private OperationMapRuntimeBootstrapSceneSystemHelper operationMapRuntimeBootstrapSystem;
        private OperationMapSceneLoadingSceneSystemHelper operationMapSceneLoadingSystem;
        private OperationMapSceneView activeOperationMapSceneView;
        private bool operationMapLoadFailureReported;

        [Header("Scene Refs")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Light directionalLight;
        [SerializeField] private Volume globalVolume;
        [SerializeField] private VisualQualityProfileAsset visualQualityProfile;
        [SerializeField] private StaticMapPresentationManifest staticMapPresentationManifest;
        [SerializeField] private CombinedMeshBaker decorationCombinedMeshBaker;
        [SerializeField] private Transform decorationRoot;
        [SerializeField] private Transform mapBuildingAuthoringRoot;
        [SerializeField] private Transform mapVehicleAuthoringRoot;
        [SerializeField] private MapSurfaceAuthoring mapSurfaceAuthoring;

        [Header("Configs")]
        [SerializeField] private RTSSelectionSystemConfig rtsSelectionConfig;
        [SerializeField] private RoadBuildSystemConfig roadBuildConfig;
        [SerializeField] private BuildingPlacementSystemConfig buildingPlacementConfig;
        [SerializeField] private MapBuildingPlacementConfig mapBuildingPlacementConfig;
        [SerializeField] private MapVehiclePlacementConfig mapVehiclePlacementConfig;
        [SerializeField] private UnitAttackTraceSystemConfig unitAttackTraceConfig;
        [SerializeField] private RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig;
        [SerializeField] private RuntimeDecorationSpawnerSystemConfig runtimeDecorationSpawnerConfig;
        [SerializeField] private RuntimeGridBlockerSystemConfig runtimeGridBlockerConfig;
        [SerializeField] private GridAuthoringConfig runtimeGridConfig;
        [SerializeField] private GridAuthoring[] runtimeGridDebugViews = Array.Empty<GridAuthoring>();
        [SerializeField] private DayNightSystemConfig dayNightConfig;
        [SerializeField] private FactionVisualSettingsConfig factionVisualConfig;
        [SerializeField] private GameStringsConfig gameStringsConfig;
        [SerializeField] private PrefabPreviewCameraConfig prefabPreviewCameraConfig;
        [SerializeField] private AIPlanEntryStartupConfig aiPlanEntryConfig;
        [SerializeField] private ResourceExchangeRecipeConfigSet resourceExchangeConfig;
        [SerializeField] private List<AIControllerConfig> aiControllerConfigs = new();

        [Header("Operation Map Compatibility")]
        [SerializeField] private OperationMapCatalogConfig operationMapCatalog;
        [SerializeField] private string operationMapId = "opmap.skirmish.desert_base_01";
        [SerializeField] private string scenarioId = "scenario.skirmish.desert_base_standard";
        [SerializeField] private string missionId = "skirmish";

        public Camera WorldCamera => worldCamera;
        public Light DirectionalLight => directionalLight;
        public Volume GlobalVolume => globalVolume;
        public VisualQualityProfileAsset VisualQualityProfile => visualQualityProfile;
        public StaticMapPresentationManifest StaticMapPresentationManifest =>
            operationMapSceneLoadingSystem != null && operationMapSceneLoadingSystem.IsReady
                ? operationMapSceneLoadingSystem.Manifest
                : staticMapPresentationManifest;
        public CombinedMeshBaker DecorationCombinedMeshBaker =>
            activeOperationMapSceneView != null
                ? activeOperationMapSceneView.DecorationCombinedMeshBaker
                : decorationCombinedMeshBaker;
        public Transform DecorationRoot =>
            activeOperationMapSceneView != null
                ? activeOperationMapSceneView.DecorationRoot
                : decorationRoot != null
                    ? decorationRoot
                    : decorationCombinedMeshBaker != null
                        ? decorationCombinedMeshBaker.transform
                        : null;
        public Transform MapBuildingAuthoringRoot =>
            activeOperationMapSceneView != null
                ? activeOperationMapSceneView.BuildingAuthoringRoot
                : mapBuildingAuthoringRoot;
        public Transform MapVehicleAuthoringRoot =>
            activeOperationMapSceneView != null
                ? activeOperationMapSceneView.VehicleAuthoringRoot
                : mapVehicleAuthoringRoot;
        public MapSurfaceAuthoring MapSurfaceAuthoring =>
            activeOperationMapSceneView != null
                ? activeOperationMapSceneView.MapSurfaceAuthoring
                : mapSurfaceAuthoring;
        public RTSSelectionSystemConfig RtsSelectionConfig => rtsSelectionConfig;
        public RoadBuildSystemConfig RoadBuildConfig => roadBuildConfig;
        public BuildingPlacementSystemConfig BuildingPlacementConfig => buildingPlacementConfig;
        public MapBuildingPlacementConfig MapBuildingPlacementConfig =>
            activeOperationMapSceneView != null
                ? activeOperationMapSceneView.BuildingPlacements
                : mapBuildingPlacementConfig;
        public MapVehiclePlacementConfig MapVehiclePlacementConfig =>
            activeOperationMapSceneView != null
                ? activeOperationMapSceneView.VehiclePlacements
                : mapVehiclePlacementConfig;
        public UnitAttackTraceSystemConfig UnitAttackTraceConfig => unitAttackTraceConfig;
        public RuntimeCitySpawnerSystemConfig RuntimeCitySpawnerConfig => runtimeCitySpawnerConfig;
        public RuntimeDecorationSpawnerSystemConfig RuntimeDecorationSpawnerConfig => runtimeDecorationSpawnerConfig;
        public RuntimeGridBlockerSystemConfig RuntimeGridBlockerConfig => runtimeGridBlockerConfig;
        public GridAuthoringConfig RuntimeGridConfig => runtimeGridConfig;
        public IReadOnlyList<GridAuthoring> RuntimeGridDebugViews => runtimeGridDebugViews;
        public DayNightSystemConfig DayNightConfig => dayNightConfig;
        public FactionVisualSettingsConfig FactionVisualConfig => factionVisualConfig;
        public GameStringsConfig GameStringsConfig => gameStringsConfig;
        public PrefabPreviewCameraConfig PrefabPreviewCameraConfig => prefabPreviewCameraConfig;
        public AIPlanEntryStartupConfig AIPlanEntryConfig => aiPlanEntryConfig;
        public ResourceExchangeRecipeConfigSet ResourceExchangeConfig => resourceExchangeConfig;
        public IReadOnlyList<AIControllerConfig> AIControllerConfigs => aiControllerConfigs;
        public OperationMapCatalogConfig OperationMapCatalog => operationMapCatalog;
        public string OperationMapId => operationMapId;
        public string ScenarioId => scenarioId;
        public string MissionId => missionId;
        public bool OperationMapContentReady =>
            operationMapSceneLoadingSystem == null || operationMapSceneLoadingSystem.IsReady;

        internal MatchBootstrapCompositionSystemHelper MatchBootstrap => matchBootstrapSystem;
        public bool GameplayStartRequested => matchBootstrapSystem.GameplayStartRequested;
        public bool GameplayStartComplete => matchBootstrapSystem.GameplayStartComplete;
        public bool GameplayStartFailed => matchBootstrapSystem.GameplayStartFailed;
        public string GameplayStartFailureMessage => matchBootstrapSystem.GameplayStartFailureMessage;
        public float GameplayStartProgress01 => matchBootstrapSystem.GameplayStartProgress01;
        public string GameplayStartStatus => matchBootstrapSystem.GameplayStartStatus;

        public void BeginGameplay()
        {
            matchBootstrapSystem.BeginGameplay();
        }

        private void Awake()
        {
            ApplyAudioListenerAuthority();
            if (Application.isPlaying)
                EnsureMatchRuntimeBound();
        }

        private void OnEnable()
        {
            ApplyAudioListenerAuthority();
            if (Application.isPlaying)
                EnsureMatchRuntimeBound();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                ApplyAudioListenerAuthority();
                return;
            }

            if (!matchRuntimeBound)
            {
                UpdateOperationMapSourceSceneLoad();
                if (!matchRuntimeBound)
                    return;
            }

            matchBootstrapSystem.Update();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!Application.isPlaying || !matchRuntimeBound)
                return;

            matchBootstrapSystem.OnApplicationFocus(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!Application.isPlaying || !matchRuntimeBound)
                return;

            matchBootstrapSystem.OnApplicationPause(pauseStatus);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || !matchRuntimeBound)
                return;

            matchBootstrapSystem.LateUpdate();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!Application.isPlaying || !matchRuntimeBound)
                return;

            matchBootstrapSystem.OnGUI();
        }
#endif

        private void OnDestroy()
        {
            RestoreAudioListenerAuthority();
            ShutdownMatchRuntimeBound();
        }

        private void OnDisable()
        {
            RestoreAudioListenerAuthority();
            ShutdownMatchRuntimeBound();
        }

        private void EnsureMatchRuntimeBound()
        {
            if (matchRuntimeBound)
                return;

            if (!HasCompatibilityMapReferences())
            {
                EnsureOperationMapSourceSceneLoad();
                return;
            }

            if (!TryBindMatchRuntime(
                    World.DefaultGameObjectInjectionWorld,
                    out string operationMapError))
                Debug.LogError($"[OperationMapCompatibility] {operationMapError}");
        }

        internal bool TryBindMatchRuntime(World world, out string error)
        {
            if (matchRuntimeBound)
            {
                error = null;
                return true;
            }

            if (!TryPublishCompatibilityOperationMapMetadata(world, out error))
                return false;

            try
            {
                matchBootstrapSystem.Awake(world, this, transform, gameObject.layer);
                matchRuntimeBound = true;
                error = null;
                return true;
            }
            catch
            {
                DisposeOperationMapMetadataBootstrap();
                throw;
            }
        }

        private void ShutdownMatchRuntimeBound()
        {
            if (!matchRuntimeBound)
            {
                DisposeOperationMapSourceSceneLoad();
                return;
            }

            GpuAnimationTeardownFence.TryFlushPendingStructuralChanges(World.DefaultGameObjectInjectionWorld);
            try
            {
                matchBootstrapSystem.OnDestroy();
            }
            finally
            {
                DisposeOperationMapMetadataBootstrap();
                matchRuntimeBound = false;
                DisposeOperationMapSourceSceneLoad();
            }
        }

        internal bool TryPublishCompatibilityOperationMapMetadata(World world, out string error)
        {
            DisposeOperationMapMetadataBootstrap();
            if (world == null || !world.IsCreated)
            {
                error = "A live default ECS World is required for operation-map metadata publication.";
                return false;
            }

            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                error = $"Invalid compatibility operation-map id '{operationMapId ?? "<null>"}'.";
                return false;
            }

            if (!OperationMapIdentityRules.IsValidScenarioId(scenarioId))
            {
                error = $"Invalid compatibility scenario id '{scenarioId ?? "<null>"}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(missionId) ||
                missionId.Length > OperationMapIdentityRules.MaximumIdLength)
            {
                error = "Compatibility mission id is required and must fit the operation-map identity budget.";
                return false;
            }

            operationMapRuntimeBootstrapSystem = new OperationMapRuntimeBootstrapSceneSystemHelper(world);
            var fixedScenarioId = new Unity.Collections.FixedString64Bytes(scenarioId);
            var fixedMissionId = new Unity.Collections.FixedString64Bytes(missionId);
            ResolveInitialOperationMapReadiness(
                activeOperationMapSceneView != null &&
                operationMapSceneLoadingSystem != null &&
                operationMapSceneLoadingSystem.IsReady,
                out OperationMapReadinessFlags readyFlags,
                out OperationMapReadinessFlags requiredFlags);
            if (operationMapRuntimeBootstrapSystem.TryPublish(
                    operationMapCatalog,
                    operationMapId,
                    in fixedScenarioId,
                    in fixedMissionId,
                    1,
                    readyFlags,
                    requiredFlags,
                    out _,
                    out error))
                return true;

            DisposeOperationMapMetadataBootstrap();
            return false;
        }

        internal static void ResolveInitialOperationMapReadiness(
            bool loadedOperationMap,
            out OperationMapReadinessFlags readyFlags,
            out OperationMapReadinessFlags requiredFlags)
        {
            if (!loadedOperationMap)
            {
                readyFlags = OperationMapReadinessFlags.Metadata;
                requiredFlags = OperationMapReadinessFlags.Metadata;
                return;
            }

            readyFlags = OperationMapReadinessFlags.SourceContent |
                         OperationMapReadinessFlags.Metadata |
                         OperationMapReadinessFlags.PresentationManifest;
            requiredFlags = readyFlags |
                            OperationMapReadinessFlags.SubScene |
                            OperationMapReadinessFlags.MapSurface |
                            OperationMapReadinessFlags.AuthoredConversion |
                            OperationMapReadinessFlags.RequiredPresentationPreload;
        }

        internal void DisposeOperationMapMetadataBootstrap()
        {
            operationMapRuntimeBootstrapSystem?.Dispose();
            operationMapRuntimeBootstrapSystem = null;
        }

        private bool HasCompatibilityMapReferences()
        {
            return mapSurfaceAuthoring != null &&
                mapBuildingAuthoringRoot != null &&
                mapVehicleAuthoringRoot != null &&
                mapBuildingPlacementConfig != null &&
                mapVehiclePlacementConfig != null;
        }

        private void EnsureOperationMapSourceSceneLoad()
        {
            if (operationMapSceneLoadingSystem != null)
                return;

            if (operationMapCatalog == null ||
                !operationMapCatalog.TryResolve(operationMapId, out OperationMapDefinition definition))
            {
                ReportOperationMapLoadFailure(
                    $"Operation-map id '{operationMapId ?? "<null>"}' is not present in the catalog.");
                return;
            }

            operationMapSceneLoadingSystem = new OperationMapSceneLoadingSceneSystemHelper();
            if (!operationMapSceneLoadingSystem.TryStart(definition, out string error))
            {
                operationMapSceneLoadingSystem.Dispose();
                operationMapSceneLoadingSystem = null;
                ReportOperationMapLoadFailure(error);
            }
        }

        private void UpdateOperationMapSourceSceneLoad()
        {
            if (operationMapLoadFailureReported)
                return;

            if (operationMapSceneLoadingSystem == null)
            {
                EnsureOperationMapSourceSceneLoad();
                return;
            }

            operationMapSceneLoadingSystem.Update();
            if (operationMapSceneLoadingSystem.HasFailed)
            {
                ReportOperationMapLoadFailure(operationMapSceneLoadingSystem.Failure);
                return;
            }

            if (!operationMapSceneLoadingSystem.IsReady)
                return;

            activeOperationMapSceneView = operationMapSceneLoadingSystem.SceneView;
            if (!TryBindMatchRuntime(World.DefaultGameObjectInjectionWorld, out string error))
                ReportOperationMapLoadFailure(error);
        }

        private void DisposeOperationMapSourceSceneLoad()
        {
            activeOperationMapSceneView = null;
            operationMapSceneLoadingSystem?.Dispose();
            operationMapSceneLoadingSystem = null;
            operationMapLoadFailureReported = false;
        }

        private void ReportOperationMapLoadFailure(string error)
        {
            if (operationMapLoadFailureReported)
                return;

            operationMapLoadFailureReported = true;
            Debug.LogError($"[OperationMapSourceScene] {error}");
        }

        private void ApplyAudioListenerAuthority()
        {
            AudioListener listener = worldCamera != null ? worldCamera.GetComponent<AudioListener>() : null;
            if (listener == null)
                return;

            AudioListener otherListener = ResolveMenuAudioListener();
            if (otherListener != null && otherListener != listener && otherListener.enabled)
            {
                if (!audioListenersDisabledForMatch.Contains(otherListener))
                    audioListenersDisabledForMatch.Add(otherListener);
                otherListener.enabled = false;
            }

            listener.enabled = true;
        }

        private void RestoreAudioListenerAuthority()
        {
            AudioListener listener = worldCamera != null ? worldCamera.GetComponent<AudioListener>() : null;
            if (listener != null)
                listener.enabled = false;

            for (int i = 0; i < audioListenersDisabledForMatch.Count; i++)
            {
                AudioListener disabledListener = audioListenersDisabledForMatch[i];
                if (disabledListener != null && disabledListener.gameObject.activeInHierarchy)
                    disabledListener.enabled = true;
            }

            audioListenersDisabledForMatch.Clear();
        }

        private AudioListener ResolveMenuAudioListener()
        {
            if (menuAudioListener != null)
                return menuAudioListener;

            Scene menuScene = SceneManager.GetSceneByName(SceneLifecycleSceneSystemHelper.MenuSceneName);
            if (!menuScene.IsValid() || !menuScene.isLoaded)
                return null;

            compositionSceneRoots.Clear();
            menuScene.GetRootGameObjects(compositionSceneRoots);
            for (int i = 0; i < compositionSceneRoots.Count; i++)
            {
                GameObject root = compositionSceneRoots[i];
                if (root == null || !root.TryGetComponent(out MenuBootstrapView menuBootstrap))
                    continue;

                Camera uiCamera = menuBootstrap.UiCamera;
                menuAudioListener = uiCamera != null ? uiCamera.GetComponent<AudioListener>() : null;
                break;
            }

            compositionSceneRoots.Clear();
            return menuAudioListener;
        }
    }
}
