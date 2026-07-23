using System;
using Game.Components;
using Game.Configs;
using Game.Rendering;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Game.Composition
{
    internal interface IOperationMapSourceSceneOperation : IDisposable
    {
        bool IsDone { get; }
        bool Succeeded { get; }
        float Progress01 { get; }
        Scene Scene { get; }
        string Failure { get; }
        bool UnloadStarted { get; }
        bool UnloadDone { get; }
        bool UnloadSucceeded { get; }
        float UnloadProgress01 { get; }
        string UnloadFailure { get; }
        bool TryBeginUnload(out string error);
    }

    internal interface IOperationMapSourceSceneApi
    {
        IOperationMapSourceSceneOperation LoadAdditive(object runtimeKey);
    }

    internal interface IOperationMapPresentationManifestOperation : IDisposable
    {
        bool IsDone { get; }
        bool Succeeded { get; }
        float Progress01 { get; }
        StaticMapPresentationManifest Manifest { get; }
        string Failure { get; }
    }

    internal interface IOperationMapPresentationManifestApi
    {
        IOperationMapPresentationManifestOperation Load(object runtimeKey);
    }

    internal interface IOperationMapEntitySceneApi
    {
        bool TryEnsureReady(
            string sceneGuid,
            string expectedOperationMapId,
            ref Entity sceneEntity,
            ref bool ownsScene,
            out bool ready,
            out string error);

        bool TryReleaseOwned(
            ref Entity sceneEntity,
            ref bool ownsScene,
            ref bool releaseStarted,
            out bool complete,
            out string error);
    }

    internal sealed class OperationMapEntitySceneApi : IOperationMapEntitySceneApi
    {
        public bool TryEnsureReady(
            string sceneGuidValue,
            string expectedOperationMapId,
            ref Entity sceneEntity,
            ref bool ownsScene,
            out bool ready,
            out string error)
        {
            ready = false;
            error = null;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                error = "Packed EntityScene loading requires the default ECS world.";
                return false;
            }

            var sceneGuid = new Hash128(sceneGuidValue);
            if (!sceneGuid.IsValid)
            {
                error =
                    "Packed EntityScene definition has an invalid authored SubScene GUID: " +
                    $"'{sceneGuidValue}'.";
                return false;
            }

            if (sceneEntity == Entity.Null || !world.EntityManager.Exists(sceneEntity))
                sceneEntity = SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid);

            if (sceneEntity != Entity.Null &&
                world.EntityManager.HasComponent<RequestSceneLoaded>(sceneEntity))
            {
                ready = SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity);
                if (ready &&
                    !OperationMapEntityPresentationReadinessUtility.TryValidate(
                        world.EntityManager,
                        sceneEntity,
                        expectedOperationMapId,
                        out error))
                {
                    ready = false;
                    return false;
                }
                return true;
            }

            sceneEntity = SceneSystem.LoadSceneAsync(world.Unmanaged, sceneGuid);
            ownsScene = sceneEntity != Entity.Null;
            if (!ownsScene)
            {
                error = $"Packed EntityScene load did not start for GUID '{sceneGuid}'.";
                return false;
            }

            ready = SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity);
            if (ready &&
                !OperationMapEntityPresentationReadinessUtility.TryValidate(
                    world.EntityManager,
                    sceneEntity,
                    expectedOperationMapId,
                    out error))
            {
                ready = false;
                return false;
            }
            return true;
        }

        public bool TryReleaseOwned(
            ref Entity sceneEntity,
            ref bool ownsScene,
            ref bool releaseStarted,
            out bool complete,
            out string error)
        {
            complete = false;
            error = null;
            if (!ownsScene)
            {
                sceneEntity = Entity.Null;
                releaseStarted = false;
                complete = true;
                return true;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                error = "Packed EntityScene unload requires the default ECS world.";
                return false;
            }

            if (sceneEntity == Entity.Null || !world.EntityManager.Exists(sceneEntity))
            {
                sceneEntity = Entity.Null;
                ownsScene = false;
                releaseStarted = false;
                complete = true;
                return true;
            }

            if (!releaseStarted)
            {
                SceneSystem.UnloadScene(
                    world.Unmanaged,
                    sceneEntity,
                    SceneSystem.UnloadParameters.DestroyMetaEntities);
                releaseStarted = true;
            }

            complete = !world.EntityManager.Exists(sceneEntity);
            if (!complete)
                return true;

            sceneEntity = Entity.Null;
            ownsScene = false;
            releaseStarted = false;
            return true;
        }
    }

    internal sealed class EntitySceneSkippedPresentationManifestOperation :
        IOperationMapPresentationManifestOperation
    {
        public bool IsDone => true;
        public bool Succeeded => true;
        public float Progress01 => 1f;
        public StaticMapPresentationManifest Manifest => null;
        public string Failure => null;
        public void Dispose()
        {
        }
    }

    internal sealed class OperationMapAddressablesSourceSceneApi : IOperationMapSourceSceneApi
    {
        public IOperationMapSourceSceneOperation LoadAdditive(object runtimeKey)
        {
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(
                runtimeKey,
                LoadSceneMode.Additive,
                activateOnLoad: true);
            return new OperationMapAddressablesSourceSceneOperation(handle);
        }
    }

    internal sealed class OperationMapAddressablesPresentationManifestApi :
        IOperationMapPresentationManifestApi
    {
        public IOperationMapPresentationManifestOperation Load(object runtimeKey)
        {
            AsyncOperationHandle<StaticMapPresentationManifest> handle =
                Addressables.LoadAssetAsync<StaticMapPresentationManifest>(runtimeKey);
            return new OperationMapAddressablesPresentationManifestOperation(handle);
        }
    }

    internal sealed class OperationMapAddressablesPresentationManifestOperation :
        IOperationMapPresentationManifestOperation
    {
        private AsyncOperationHandle<StaticMapPresentationManifest> handle;
        private bool disposed;

        public OperationMapAddressablesPresentationManifestOperation(
            AsyncOperationHandle<StaticMapPresentationManifest> handle)
        {
            this.handle = handle;
        }

        public bool IsDone => handle.IsValid() && handle.IsDone;
        public bool Succeeded =>
            handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded;
        public float Progress01 => handle.IsValid() ? handle.PercentComplete : 0f;
        public StaticMapPresentationManifest Manifest => Succeeded ? handle.Result : null;
        public string Failure => handle.IsValid()
            ? handle.OperationException?.Message
            : "Operation-map presentation-manifest handle is invalid.";

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (handle.IsValid())
                Addressables.Release(handle);
        }
    }

    internal sealed class OperationMapAddressablesSourceSceneOperation :
        IOperationMapSourceSceneOperation
    {
        private AsyncOperationHandle<SceneInstance> handle;
        private AsyncOperationHandle<SceneInstance> unloadHandle;
        private bool disposed;

        public OperationMapAddressablesSourceSceneOperation(
            AsyncOperationHandle<SceneInstance> handle)
        {
            this.handle = handle;
        }

        public bool IsDone => handle.IsValid() && handle.IsDone;
        public bool Succeeded =>
            handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded;
        public float Progress01 => handle.IsValid() ? handle.PercentComplete : 0f;
        public Scene Scene => Succeeded ? handle.Result.Scene : default;
        public string Failure => handle.IsValid()
            ? handle.OperationException?.Message
            : "Operation-map source-scene handle is invalid.";
        public bool UnloadStarted { get; private set; }
        public bool UnloadDone => UnloadStarted && unloadHandle.IsValid() && unloadHandle.IsDone;
        public bool UnloadSucceeded =>
            UnloadDone && unloadHandle.Status == AsyncOperationStatus.Succeeded;
        public float UnloadProgress01 =>
            UnloadStarted && unloadHandle.IsValid() ? unloadHandle.PercentComplete : 0f;
        public string UnloadFailure => unloadHandle.IsValid()
            ? unloadHandle.OperationException?.Message
            : "Operation-map source-scene unload handle is invalid.";

        public bool TryBeginUnload(out string error)
        {
            if (disposed)
            {
                error = "Operation-map source-scene operation is disposed.";
                return false;
            }
            if (UnloadStarted)
            {
                error = null;
                return true;
            }
            if (!handle.IsValid() || !handle.IsDone || handle.Status != AsyncOperationStatus.Succeeded)
            {
                error = "Operation-map source scene must finish loading before unload begins.";
                return false;
            }

            try
            {
                unloadHandle = Addressables.UnloadSceneAsync(handle, autoReleaseHandle: false);
                if (!unloadHandle.IsValid())
                {
                    error = "Operation-map source-scene unload did not return a valid operation.";
                    return false;
                }

                UnloadStarted = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.Log("[OperationMapSourceScene] stage=SceneUnloadStarted");
#endif
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = $"Operation-map source-scene unload did not start: {exception.Message}";
                return false;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (UnloadStarted)
            {
                if (unloadHandle.IsValid())
                    Addressables.Release(unloadHandle);
                unloadHandle = default;
                handle = default;
                return;
            }

            if (handle.IsValid() && handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
                Addressables.UnloadSceneAsync(handle, autoReleaseHandle: true);
            else if (handle.IsValid())
                Addressables.Release(handle);
            handle = default;
        }
    }

    internal sealed class OperationMapSceneLoadingSceneSystemHelper : IDisposable
    {
        private readonly IOperationMapSourceSceneApi sceneApi;
        private readonly IOperationMapPresentationManifestApi manifestApi;
        private readonly IOperationMapEntitySceneApi entitySceneApi;
        private readonly OperationMapSceneReferenceSceneSystemHelper sceneReference;
        private IOperationMapSourceSceneOperation sceneOperation;
        private IOperationMapPresentationManifestOperation manifestOperation;
        private string expectedOperationMapId;
        private string expectedSourceSceneGuid;
        private Entity packedEntityScene;
        private bool ownsPackedEntityScene;
        private bool packedEntitySceneReleaseStarted;
        private bool failedEntitySceneCleanupPending;
        private bool disposed;
        private bool unloading;

        public OperationMapSceneLoadingSceneSystemHelper(
            IOperationMapSourceSceneApi sceneApi = null,
            IOperationMapPresentationManifestApi manifestApi = null,
            OperationMapSceneReferenceSceneSystemHelper sceneReference = null,
            IOperationMapEntitySceneApi entitySceneApi = null)
        {
            this.sceneApi = sceneApi ?? new OperationMapAddressablesSourceSceneApi();
            this.manifestApi = manifestApi ??
                new OperationMapAddressablesPresentationManifestApi();
            this.entitySceneApi = entitySceneApi ?? new OperationMapEntitySceneApi();
            this.sceneReference = sceneReference ?? new OperationMapSceneReferenceSceneSystemHelper();
        }

        public bool IsLoading => sceneOperation != null && !IsReady && !HasFailed;
        public bool IsReady { get; private set; }
        public bool HasFailed => !string.IsNullOrEmpty(Failure);
        public bool SourceSceneOperationComplete => sceneOperation != null && sceneOperation.IsDone;
        public bool PresentationManifestOperationComplete => manifestOperation != null && manifestOperation.IsDone;
        public float Progress01 { get; private set; }
        public string Failure { get; private set; }
        public OperationMapLoadResultCode FailureCode { get; private set; }
        public OperationMapSceneView SceneView { get; private set; }
        public StaticMapPresentationManifest Manifest { get; private set; }
        public bool IsUnloading => unloading && !UnloadComplete && !HasFailed;
        public bool UnloadComplete { get; private set; }

        public bool TryStart(OperationMapDefinition definition, out string error)
        {
            if (disposed)
            {
                error = "Operation-map source-scene loader is disposed.";
                return RejectStart(OperationMapLoadResultCode.InvalidRequest, error);
            }

            if (sceneOperation != null)
            {
                error = "Operation-map source-scene loader already owns an operation.";
                return RejectStart(OperationMapLoadResultCode.Busy, error);
            }

            if (definition == null)
            {
                error = "Operation-map definition is required.";
                return RejectStart(OperationMapLoadResultCode.MissingDefinition, error);
            }

            if (!definition.TryValidateIdentity(out error))
                return RejectStart(OperationMapLoadResultCode.InvalidOperationMapId, error);

            AssetReference sourceReference = definition.SourceSceneReference;
            if (sourceReference == null || !sourceReference.RuntimeKeyIsValid())
            {
                error = "Operation-map source-scene reference is missing or invalid.";
                return RejectStart(OperationMapLoadResultCode.MissingSourceContent, error);
            }

            bool entityScene =
                OperationMapEntityScenePresentationPolicy.ShouldSkipStaticManifestStreamerAndOwnership(
                    definition);
            AssetReference manifestReference = definition.StaticPresentationManifestReference;
            if (!entityScene &&
                (manifestReference == null || !manifestReference.RuntimeKeyIsValid()))
            {
                error = "Operation-map presentation-manifest reference is missing or invalid.";
                return RejectStart(OperationMapLoadResultCode.MissingSourceContent, error);
            }

            if (entityScene &&
                manifestReference != null &&
                !string.IsNullOrEmpty(manifestReference.AssetGUID))
            {
                error =
                    "EntityScene operation-map loads must not bind a production static presentation manifest.";
                return RejectStart(OperationMapLoadResultCode.StaleContent, error);
            }

            try
            {
                sceneOperation = sceneApi.LoadAdditive(sourceReference.RuntimeKey);
                manifestOperation = entityScene
                    ? new EntitySceneSkippedPresentationManifestOperation()
                    : manifestApi.Load(manifestReference.RuntimeKey);
            }
            catch (Exception exception)
            {
                ReleaseOperations();
                error = $"Operation-map source-scene load did not start: {exception.Message}";
                return RejectStart(OperationMapLoadResultCode.SourceLoadFailed, error);
            }

            if (sceneOperation == null || manifestOperation == null)
            {
                ReleaseOperations();
                error = "Operation-map source-scene or presentation-manifest load did not return an operation.";
                return RejectStart(OperationMapLoadResultCode.SourceLoadFailed, error);
            }

            expectedOperationMapId = definition.OperationMapId;
            expectedSourceSceneGuid = sourceReference.AssetGUID;
            Progress01 = 0f;
            error = null;
            return true;
        }

        public void Update()
        {
            if (disposed || sceneOperation == null || HasFailed)
                return;

            if (unloading)
            {
                UpdateUnload();
                return;
            }
            if (IsReady)
                return;

            Progress01 = (sceneOperation.Progress01 + manifestOperation.Progress01) * 0.5f;
            if (sceneOperation.IsDone && !sceneOperation.Succeeded)
            {
                Fail(
                    OperationMapLoadResultCode.SourceLoadFailed,
                    string.IsNullOrWhiteSpace(sceneOperation.Failure)
                        ? "Operation-map source-scene load failed."
                        : sceneOperation.Failure);
                return;
            }

            if (manifestOperation.IsDone && !manifestOperation.Succeeded)
            {
                Fail(
                    OperationMapLoadResultCode.PresentationPreloadFailed,
                    string.IsNullOrWhiteSpace(manifestOperation.Failure)
                        ? "Operation-map presentation-manifest load failed."
                        : manifestOperation.Failure);
                return;
            }

            if (!sceneOperation.IsDone || !manifestOperation.IsDone)
                return;

            if (!sceneReference.TryGetLoadedSceneView(
                    sceneOperation.Scene,
                    expectedOperationMapId,
                    out OperationMapSceneView view,
                    out string error) ||
                !view.TryValidate(out error))
            {
                Fail(OperationMapLoadResultCode.MetadataBindFailed, error);
                return;
            }

            if (OperationMapEntityScenePresentationPolicy.UsesEntityScenePresentation(view.Definition))
            {
                if (manifestOperation.Manifest != null)
                {
                    Fail(
                        OperationMapLoadResultCode.StaleContent,
                        "EntityScene operation-map loads must not resolve a static presentation manifest.");
                    return;
                }

                if (!TryEnsurePackedEntitySceneReady(view, out bool packedReady, out error))
                {
                    Fail(OperationMapLoadResultCode.MetadataBindFailed, error);
                    return;
                }
                if (!packedReady)
                {
                    SceneView = view;
                    Manifest = null;
                    Progress01 = 0.95f;
                    return;
                }

                SceneView = view;
                Manifest = null;
                Progress01 = 1f;
                IsReady = true;
                return;
            }

            if (!TryValidateManifest(
                    manifestOperation.Manifest,
                    sceneOperation.Scene,
                    view,
                    expectedSourceSceneGuid,
                    out error))
            {
                Fail(OperationMapLoadResultCode.StaleContent, error);
                return;
            }

            SceneView = view;
            Manifest = manifestOperation.Manifest;
            Progress01 = 1f;
            IsReady = true;
        }

        public bool TryBeginUnload(out string error)
        {
            if (disposed)
            {
                error = "Operation-map source-scene loader is disposed.";
                return false;
            }
            if (HasFailed)
            {
                error = Failure;
                return false;
            }
            if (UnloadComplete || unloading)
            {
                error = null;
                return true;
            }
            if (!IsReady || sceneOperation == null)
            {
                error = "Operation-map source scene must be ready before unload begins.";
                return false;
            }
            if (!sceneOperation.TryBeginUnload(out error))
                return false;

            if (!TryReleaseOwnedPackedEntityScene(out _, out error))
                return false;
            unloading = true;
            IsReady = false;
            Progress01 = 0f;
            return true;
        }

        public void Abort(
            string failure,
            OperationMapLoadResultCode failureCode = OperationMapLoadResultCode.Interrupted)
        {
            if (disposed || HasFailed)
                return;

            Fail(failureCode, string.IsNullOrWhiteSpace(failure)
                ? "Operation-map source-scene load was aborted."
                : failure);
        }

        public bool TryReset(out string error)
        {
            if (disposed)
            {
                error = "Operation-map source-scene loader is disposed.";
                return false;
            }

            if (HasFailed)
            {
                if (!TryCompleteFailedEntitySceneCleanup(
                        out bool cleanupComplete,
                        out error))
                {
                    return false;
                }
                if (!cleanupComplete)
                {
                    error =
                        "Operation-map failed EntityScene cleanup is still in progress.";
                    return false;
                }
            }

            ReleaseOperations();
            ResetResultState();
            error = null;
            return true;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            ReleaseOperations();
            SceneView = null;
            Manifest = null;
            IsReady = false;
            Progress01 = 0f;
        }

        private void ResetResultState()
        {
            SceneView = null;
            Manifest = null;
            IsReady = false;
            Progress01 = 0f;
            Failure = null;
            FailureCode = OperationMapLoadResultCode.None;
            expectedOperationMapId = null;
            expectedSourceSceneGuid = null;
            packedEntityScene = Entity.Null;
            ownsPackedEntityScene = false;
            packedEntitySceneReleaseStarted = false;
            failedEntitySceneCleanupPending = false;
            unloading = false;
            UnloadComplete = false;
        }

        private void UpdateUnload()
        {
            Progress01 = sceneOperation.UnloadProgress01;
            if (!TryReleaseOwnedPackedEntityScene(
                    out bool packedEntitySceneReleaseComplete,
                    out string packedEntitySceneReleaseError))
            {
                Fail(
                    OperationMapLoadResultCode.SourceUnloadFailed,
                    packedEntitySceneReleaseError);
                return;
            }
            if (!sceneOperation.UnloadDone)
                return;
            if (!sceneOperation.UnloadSucceeded)
            {
                Fail(
                    OperationMapLoadResultCode.SourceUnloadFailed,
                    string.IsNullOrWhiteSpace(sceneOperation.UnloadFailure)
                        ? "Operation-map source-scene unload failed."
                        : sceneOperation.UnloadFailure);
                return;
            }
            if (!packedEntitySceneReleaseComplete)
            {
                Progress01 = 0.95f;
                return;
            }

            ReleaseOperations();
            SceneView = null;
            Manifest = null;
            unloading = false;
            UnloadComplete = true;
            Progress01 = 1f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log("[OperationMapSourceScene] stage=AddressablesUnloadComplete");
#endif
        }

        private bool RejectStart(OperationMapLoadResultCode failureCode, string error)
        {
            FailureCode = failureCode;
            Failure = error;
            return false;
        }

        private void Fail(OperationMapLoadResultCode failureCode, string error)
        {
            FailureCode = failureCode;
            Failure = string.IsNullOrWhiteSpace(error)
                ? "Operation-map source-scene load failed."
                : error;
            if (ownsPackedEntityScene)
            {
                failedEntitySceneCleanupPending = true;
                TryBeginFailedEntitySceneCleanup();
            }
            else
            {
                ReleaseOperations();
            }
            SceneView = null;
            Manifest = null;
            IsReady = false;
            unloading = false;
            Progress01 = 0f;
        }

        private void TryBeginFailedEntitySceneCleanup()
        {
            TryReleaseOwnedPackedEntityScene(out _, out _);
            if (sceneOperation != null &&
                sceneOperation.IsDone &&
                sceneOperation.Succeeded &&
                !sceneOperation.UnloadStarted)
            {
                sceneOperation.TryBeginUnload(out _);
            }
        }

        private bool TryCompleteFailedEntitySceneCleanup(
            out bool complete,
            out string error)
        {
            complete = true;
            error = null;
            if (!failedEntitySceneCleanupPending)
                return true;

            if (!TryReleaseOwnedPackedEntityScene(
                    out bool entitySceneComplete,
                    out error))
            {
                complete = false;
                return false;
            }

            if (sceneOperation != null && !sceneOperation.UnloadStarted)
            {
                if (!sceneOperation.IsDone || !sceneOperation.Succeeded)
                {
                    complete = false;
                    error =
                        "Failed operation-map source scene is not available for cleanup.";
                    return false;
                }
                if (!sceneOperation.TryBeginUnload(out error))
                {
                    complete = false;
                    return false;
                }
            }

            bool sourceSceneComplete =
                sceneOperation == null || sceneOperation.UnloadDone;
            if (sceneOperation != null &&
                sceneOperation.UnloadDone &&
                !sceneOperation.UnloadSucceeded)
            {
                complete = false;
                error = string.IsNullOrWhiteSpace(sceneOperation.UnloadFailure)
                    ? "Failed operation-map source-scene cleanup did not complete."
                    : sceneOperation.UnloadFailure;
                return false;
            }

            complete = entitySceneComplete && sourceSceneComplete;
            if (complete)
                failedEntitySceneCleanupPending = false;
            return true;
        }

        private void ReleaseOperations()
        {
            TryReleaseOwnedPackedEntityScene(out _, out _);
            sceneOperation?.Dispose();
            sceneOperation = null;
            manifestOperation?.Dispose();
            manifestOperation = null;
        }

        private bool TryEnsurePackedEntitySceneReady(
            OperationMapSceneView view,
            out bool ready,
            out string error)
        {
            ready = false;
            error = null;
            if (view.MapSubScene.SceneGUID.IsValid)
            {
                ready = true;
                return true;
            }

            return entitySceneApi.TryEnsureReady(
                view.Definition.NavigationMetadata.AuthoredSubSceneGuid,
                expectedOperationMapId,
                ref packedEntityScene,
                ref ownsPackedEntityScene,
                out ready,
                out error);
        }

        private bool TryReleaseOwnedPackedEntityScene(
            out bool complete,
            out string error)
        {
            return entitySceneApi.TryReleaseOwned(
                ref packedEntityScene,
                ref ownsPackedEntityScene,
                ref packedEntitySceneReleaseStarted,
                out complete,
                out error);
        }

        private static bool TryValidateManifest(
            StaticMapPresentationManifest manifest,
            Scene scene,
            OperationMapSceneView view,
            string loadedSceneGuid,
            out string error)
        {
            if (manifest == null ||
                !StaticMapPresentationManifest.HasRequiredIdentity(
                    manifest.SchemaVersion,
                    manifest.OperationMapId,
                    manifest.CanonicalSceneGuid,
                    manifest.CanonicalScenePath))
            {
                error = "Operation-map presentation manifest identity is missing or unsupported.";
                return false;
            }

            if (!string.Equals(view.Definition.SourceSceneReference.AssetGUID, loadedSceneGuid, StringComparison.Ordinal))
            {
                error = "Operation-map definition does not identify the loaded source scene.";
                return false;
            }

            string presentationSourceGuid = view.CanonicalPresentationMode ==
                OperationMapCanonicalPresentationMode.PresentationOnly
                    ? view.PresentationSourceSceneGuid
                    : loadedSceneGuid;
            string presentationSourcePath = view.CanonicalPresentationMode ==
                OperationMapCanonicalPresentationMode.PresentationOnly
                    ? view.PresentationSourceScenePath
                    : scene.path;
            if (!string.Equals(manifest.OperationMapId, view.OperationMapId, StringComparison.Ordinal) ||
                !string.Equals(manifest.CanonicalSceneGuid, presentationSourceGuid, StringComparison.Ordinal) ||
                !string.Equals(manifest.CanonicalScenePath, presentationSourcePath, StringComparison.Ordinal))
            {
                error = "Operation-map presentation manifest does not match the declared authoring source.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(manifest.ContentHash) ||
                string.IsNullOrWhiteSpace(manifest.CanonicalSceneDependencyHash) ||
                manifest.ChunkSize <= 0f ||
                manifest.Chunks == null || manifest.Chunks.Count == 0 ||
                manifest.Sources == null || manifest.Sources.Count == 0)
            {
                error = "Operation-map presentation manifest content is incomplete.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
