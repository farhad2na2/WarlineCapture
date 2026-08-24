using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scenes;
using Game.Components;
using Game.Configs;
using Game.Rendering;

namespace Game.Composition
{
    public sealed partial class MatchSceneView
    {
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
    }
}
