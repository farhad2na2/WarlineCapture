using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Scenes;
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
        private readonly OperationMapDenseCityCandidateRuntimeOverride
            denseCityCandidateRuntimeOverride = new();
        private OperationMapDefinition resolvedOperationMapDefinition;
        private OperationMapSceneView activeOperationMapSceneView;
        private OperationMapCanonicalPresentationMode loadedOperationMapCanonicalPresentationMode =
            OperationMapCanonicalPresentationMode.SourceRenderersPresent;
        private bool operationMapLoadFailureReported;
        private OperationMapLoadResultCode operationMapLoadFailureCode;
        private string operationMapLoadFailure;
        private bool operationMapReadinessPublished;
        private OperationMapReadinessFlags publishedOperationMapReadyFlags;
        private OperationMapReadinessFlags publishedOperationMapFailedFlags;
        private Entity loadedOperationMapSubSceneEntity;
        private bool operationMapSceneUnloadStartPending;

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

#if UNITY_EDITOR
        private static OperationMapCatalogConfig editorOperationMapCatalogOverrideForTests;
#endif

        public Camera WorldCamera => worldCamera;
        public Light DirectionalLight => directionalLight;
        public Volume GlobalVolume => globalVolume;
        public VisualQualityProfileAsset VisualQualityProfile => visualQualityProfile;
        public StaticMapPresentationManifest StaticMapPresentationManifest =>
            operationMapSceneLoadingSystem != null && operationMapSceneLoadingSystem.IsReady
                ? operationMapSceneLoadingSystem.Manifest
                : staticMapPresentationManifest;
        public OperationMapCanonicalPresentationMode CanonicalPresentationMode =>
            activeOperationMapSceneView != null
                ? activeOperationMapSceneView.CanonicalPresentationMode
                : loadedOperationMapCanonicalPresentationMode;
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
        public GridAuthoringConfig RuntimeGridConfig =>
            activeOperationMapSceneView != null
                ? activeOperationMapSceneView.GridAuthoringConfig
                : runtimeGridConfig;
        public IReadOnlyList<GridAuthoring> RuntimeGridDebugViews => runtimeGridDebugViews;
        public DayNightSystemConfig DayNightConfig => dayNightConfig;
        public FactionVisualSettingsConfig FactionVisualConfig => factionVisualConfig;
        public GameStringsConfig GameStringsConfig => gameStringsConfig;
        public PrefabPreviewCameraConfig PrefabPreviewCameraConfig => prefabPreviewCameraConfig;
        public AIPlanEntryStartupConfig AIPlanEntryConfig => aiPlanEntryConfig;
        public ResourceExchangeRecipeConfigSet ResourceExchangeConfig => resourceExchangeConfig;
        public IReadOnlyList<AIControllerConfig> AIControllerConfigs => aiControllerConfigs;
        public OperationMapCatalogConfig OperationMapCatalog => ResolveOperationMapCatalog();
        public string OperationMapId => operationMapId;
        public string ScenarioId => scenarioId;
        public string MissionId => missionId;
        public bool OperationMapContentReady =>
            operationMapSceneLoadingSystem == null || operationMapSceneLoadingSystem.IsReady;
        internal bool OperationMapSourceSceneLoadComplete =>
            operationMapSceneLoadingSystem != null &&
            operationMapSceneLoadingSystem.SourceSceneOperationComplete;
        internal bool OperationMapPresentationManifestLoadComplete =>
            operationMapSceneLoadingSystem != null &&
            operationMapSceneLoadingSystem.PresentationManifestOperationComplete;
        internal int OperationMapSourceSceneLoadOperationCount =>
            operationMapSceneLoadingSystem?.SourceSceneLoadOperationCount ?? 0;
        internal int OperationMapPresentationManifestLoadOperationCount =>
            operationMapSceneLoadingSystem?.PresentationManifestLoadOperationCount ?? 0;
        internal int OperationMapPackedEntitySceneLoadRequestCount =>
            operationMapSceneLoadingSystem?.PackedEntitySceneLoadRequestCount ?? 0;
        internal int OperationMapSourceSceneUnloadOperationCount =>
            operationMapSceneLoadingSystem?.SourceSceneUnloadOperationCount ?? 0;
        internal int OperationMapPackedEntitySceneUnloadRequestCount =>
            operationMapSceneLoadingSystem?.PackedEntitySceneUnloadRequestCount ?? 0;
        internal float OperationMapContentProgress01 =>
            operationMapSceneLoadingSystem?.Progress01 ?? 0f;
        internal string OperationMapContentFailure =>
            operationMapSceneLoadingSystem != null &&
            !string.IsNullOrEmpty(operationMapSceneLoadingSystem.Failure)
                ? operationMapSceneLoadingSystem.Failure
                : operationMapLoadFailure;
        internal OperationMapLoadResultCode OperationMapContentFailureCode =>
            operationMapSceneLoadingSystem != null &&
            operationMapSceneLoadingSystem.FailureCode != OperationMapLoadResultCode.None
                ? operationMapSceneLoadingSystem.FailureCode
                : operationMapLoadFailureCode;
        internal bool OperationMapContentUnloading =>
            operationMapSceneUnloadStartPending ||
            (operationMapSceneLoadingSystem != null && operationMapSceneLoadingSystem.IsUnloading);
        internal bool OperationMapContentUnloadComplete =>
            !operationMapSceneUnloadStartPending &&
            (operationMapSceneLoadingSystem == null || operationMapSceneLoadingSystem.UnloadComplete);
        internal bool OperationMapReadinessPublicationAvailable =>
            activeOperationMapSceneView != null && operationMapRuntimeBootstrapSystem != null;

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
            ApplyMatchEnvironmentAuthority();
            ApplyAudioListenerAuthority();
            if (Application.isPlaying)
                EnsureMatchRuntimeBound();
        }

        private void OnEnable()
        {
            ApplyMatchEnvironmentAuthority();
            ApplyAudioListenerAuthority();
            if (Application.isPlaying)
                EnsureMatchRuntimeBound();
        }

        private void ApplyMatchEnvironmentAuthority()
        {
            if (!Application.isPlaying)
                return;

            Scene matchScene = gameObject.scene;
            if (!matchScene.IsValid() || !matchScene.isLoaded || SceneManager.GetActiveScene() == matchScene)
                return;

            if (!SceneManager.SetActiveScene(matchScene))
            {
                throw new InvalidOperationException(
                    $"Failed to make the loaded Match scene active for authored environment ownership: '{matchScene.path}'.");
            }
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
            if (operationMapSceneUnloadStartPending)
                BeginOperationMapSourceSceneUnload();

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
            denseCityCandidateRuntimeOverride.Dispose();
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

            ApplyMatchEnvironmentAuthority();
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

        private void ShutdownMatchRuntimeBound(bool disposeSourceSceneLoad = true)
        {
            if (!matchRuntimeBound)
            {
                if (disposeSourceSceneLoad)
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
                if (disposeSourceSceneLoad)
                    DisposeOperationMapSourceSceneLoad();
            }
        }

        internal bool TryBeginOperationMapContentUnload(out string error)
        {
            if (operationMapSceneLoadingSystem == null ||
                operationMapSceneLoadingSystem.UnloadComplete ||
                operationMapSceneUnloadStartPending)
            {
                error = null;
                return true;
            }

            ShutdownMatchRuntimeBound(disposeSourceSceneLoad: false);
            Scene matchScene = gameObject.scene;
            if (matchScene.IsValid() && matchScene.isLoaded)
                SceneManager.SetActiveScene(matchScene);
            activeOperationMapSceneView = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[OperationMapSourceScene] stage=SceneUnloadRequested");
#endif
            operationMapSceneUnloadStartPending = true;
            error = null;
            return true;
        }

        internal void UpdateOperationMapContentUnload()
        {
            if (OperationMapContentUnloading)
                UpdateOperationMapSourceSceneLoad();
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

            if (resolvedOperationMapDefinition == null &&
                !TryResolveOperationMapDefinition(
                    out resolvedOperationMapDefinition,
                    out bool waiting,
                    out error))
            {
                if (waiting)
                    error = "Operation-map definition is still loading.";
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
                    resolvedOperationMapDefinition,
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
            operationMapReadinessPublished = false;
            publishedOperationMapReadyFlags = OperationMapReadinessFlags.None;
            publishedOperationMapFailedFlags = OperationMapReadinessFlags.None;
            loadedOperationMapSubSceneEntity = Entity.Null;
        }

        internal bool TryPublishOperationMapReadiness(
            bool presentationPreloadReady,
            bool presentationPreloadFailed,
            out string error)
        {
            if (activeOperationMapSceneView == null || operationMapRuntimeBootstrapSystem == null)
            {
                error = "Operation-map readiness requires a loaded view and published metadata.";
                return false;
            }

            ResolveInitialOperationMapReadiness(
                true,
                out OperationMapReadinessFlags readyFlags,
                out _);
            if (IsOperationMapSubSceneReady())
            {
                readyFlags |= OperationMapReadinessFlags.SubScene |
                              OperationMapReadinessFlags.AuthoredConversion;
            }
            if (activeOperationMapSceneView.MapSurfaceAuthoring.BakedSurfaceData != null)
                readyFlags |= OperationMapReadinessFlags.MapSurface;
            if (presentationPreloadReady)
                readyFlags |= OperationMapReadinessFlags.RequiredPresentationPreload;

            OperationMapReadinessFlags failedFlags = presentationPreloadFailed
                ? OperationMapReadinessFlags.RequiredPresentationPreload
                : OperationMapReadinessFlags.None;
            if (operationMapReadinessPublished &&
                publishedOperationMapReadyFlags == readyFlags &&
                publishedOperationMapFailedFlags == failedFlags)
            {
                error = null;
                return true;
            }

            if (!operationMapRuntimeBootstrapSystem.TryUpdateReadiness(
                    1,
                    readyFlags,
                    failedFlags,
                    out error))
                return false;

            operationMapReadinessPublished = true;
            publishedOperationMapReadyFlags = readyFlags;
            publishedOperationMapFailedFlags = failedFlags;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[OperationMapReadiness] ready={readyFlags} failed={failedFlags}");
#endif
            return true;
        }

        private bool IsOperationMapSubSceneReady()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            if (loadedOperationMapSubSceneEntity == Entity.Null ||
                !world.EntityManager.Exists(loadedOperationMapSubSceneEntity))
            {
                loadedOperationMapSubSceneEntity = SceneSystem.GetSceneEntity(
                    world.Unmanaged,
                    activeOperationMapSceneView.MapSubScene.SceneGUID);
            }

            return loadedOperationMapSubSceneEntity != Entity.Null &&
                   SceneSystem.IsSceneLoaded(world.Unmanaged, loadedOperationMapSubSceneEntity);
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

            if (!TryResolveOperationMapDefinition(
                    out OperationMapDefinition definition,
                    out bool waiting,
                    out string resolveError))
            {
                if (waiting)
                    return;
                ReportOperationMapLoadFailure(
                    OperationMapIdentityRules.IsValidOperationMapId(operationMapId)
                        ? OperationMapLoadResultCode.MissingDefinition
                        : OperationMapLoadResultCode.InvalidOperationMapId,
                    resolveError);
                return;
            }

            resolvedOperationMapDefinition = definition;
            operationMapSceneLoadingSystem = new OperationMapSceneLoadingSceneSystemHelper();
            if (!operationMapSceneLoadingSystem.TryStart(definition, out string error))
            {
                OperationMapLoadResultCode failureCode = operationMapSceneLoadingSystem.FailureCode;
                operationMapSceneLoadingSystem.Dispose();
                operationMapSceneLoadingSystem = null;
                ReportOperationMapLoadFailure(failureCode, error);
            }
        }

        private OperationMapCatalogConfig ResolveOperationMapCatalog()
        {
#if UNITY_EDITOR
            if (editorOperationMapCatalogOverrideForTests != null)
                return editorOperationMapCatalogOverrideForTests;
#endif
            return operationMapCatalog;
        }

        private bool TryResolveOperationMapDefinition(
            out OperationMapDefinition definition,
            out bool waiting,
            out string error)
        {
            return denseCityCandidateRuntimeOverride.TryResolve(
                ResolveOperationMapCatalog(),
                operationMapId,
                out definition,
                out waiting,
                out error);
        }

#if UNITY_EDITOR
        internal static void SetEditorOperationMapCatalogOverrideForTests(
            OperationMapCatalogConfig catalog)
        {
            editorOperationMapCatalogOverrideForTests = catalog;
        }
#endif

        private void UpdateOperationMapSourceSceneLoad()
        {
            if (operationMapLoadFailureReported)
                return;

            if (operationMapSceneLoadingSystem == null)
            {
                EnsureOperationMapSourceSceneLoad();
                return;
            }

            if (operationMapSceneUnloadStartPending)
                return;

            if (operationMapSceneLoadingSystem.IsUnloading)
            {
                operationMapSceneLoadingSystem.Update();
                return;
            }

            operationMapSceneLoadingSystem.Update();
            if (operationMapSceneLoadingSystem.HasFailed)
            {
                ReportOperationMapLoadFailure(
                    operationMapSceneLoadingSystem.FailureCode,
                    operationMapSceneLoadingSystem.Failure);
                return;
            }

            if (!operationMapSceneLoadingSystem.IsReady)
                return;

            activeOperationMapSceneView = operationMapSceneLoadingSystem.SceneView;
            loadedOperationMapCanonicalPresentationMode =
                activeOperationMapSceneView.CanonicalPresentationMode;
            if (!TryBindMatchRuntime(World.DefaultGameObjectInjectionWorld, out string error))
                ReportOperationMapLoadFailure(OperationMapLoadResultCode.MetadataBindFailed, error);
        }

        private void DisposeOperationMapSourceSceneLoad()
        {
            activeOperationMapSceneView = null;
            loadedOperationMapCanonicalPresentationMode =
                OperationMapCanonicalPresentationMode.SourceRenderersPresent;
            operationMapSceneUnloadStartPending = false;
            operationMapSceneLoadingSystem?.Dispose();
            operationMapSceneLoadingSystem = null;
            resolvedOperationMapDefinition = null;
            operationMapLoadFailureReported = false;
            operationMapLoadFailureCode = OperationMapLoadResultCode.None;
            operationMapLoadFailure = null;
        }

        private void BeginOperationMapSourceSceneUnload()
        {
            operationMapSceneUnloadStartPending = false;
            if (operationMapSceneLoadingSystem != null &&
                !operationMapSceneLoadingSystem.TryBeginUnload(out string unloadError))
            {
                ReportOperationMapLoadFailure(OperationMapLoadResultCode.SourceUnloadFailed, unloadError);
            }
        }

        private void ReportOperationMapLoadFailure(
            OperationMapLoadResultCode failureCode,
            string error)
        {
            if (operationMapLoadFailureReported)
                return;

            DisposeOperationMapMetadataBootstrap();
            activeOperationMapSceneView = null;
            operationMapSceneLoadingSystem?.Abort(error, failureCode);
            operationMapLoadFailureCode = failureCode;
            operationMapLoadFailure = error;
            operationMapLoadFailureReported = true;
            Debug.LogError($"[OperationMapSourceScene] code={failureCode} error={error}");
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
