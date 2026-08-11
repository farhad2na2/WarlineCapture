using System;
using Game.Components;
using Game.Configs;
using Game.Rendering;

namespace Game.Composition
{
    internal sealed class OperationMapSceneLoadingSceneSystemHelper : IDisposable
    {
        private readonly IOperationMapSourceSceneApi sceneApi;
        private readonly IOperationMapPresentationManifestApi manifestApi;
        private readonly OperationMapSceneReferenceSceneSystemHelper sceneReference;
        private readonly OperationMapPackedEntitySceneOwnership packedEntitySceneOwnership;
        private IOperationMapSourceSceneOperation sceneOperation;
        private IOperationMapPresentationManifestOperation manifestOperation;
        private string expectedOperationMapId;
        private string expectedSourceSceneGuid;
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
            this.sceneReference = sceneReference ?? new OperationMapSceneReferenceSceneSystemHelper();
            packedEntitySceneOwnership = new OperationMapPackedEntitySceneOwnership(
                entitySceneApi ?? new OperationMapEntitySceneApi());
        }

        public bool IsLoading => sceneOperation != null && !IsReady && !HasFailed;
        public bool IsReady { get; private set; }
        public bool HasFailed => !string.IsNullOrEmpty(Failure);
        public bool SourceSceneOperationComplete => sceneOperation != null && sceneOperation.IsDone;
        public bool PresentationManifestOperationComplete =>
            manifestOperation != null && manifestOperation.IsDone;
        internal int SourceSceneLoadOperationCount { get; private set; }
        internal int PresentationManifestLoadOperationCount { get; private set; }
        internal int PackedEntitySceneLoadRequestCount =>
            packedEntitySceneOwnership.LoadRequestCount;
        internal int SourceSceneUnloadOperationCount { get; private set; }
        internal int PackedEntitySceneUnloadRequestCount =>
            packedEntitySceneOwnership.UnloadRequestCount;
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

            if (!OperationMapSceneLoadRequestValidation.TryCreate(
                    definition,
                    out OperationMapSceneLoadRequest request,
                    out OperationMapLoadResultCode failureCode,
                    out error))
            {
                return RejectStart(failureCode, error);
            }

            try
            {
                sceneOperation = sceneApi.LoadAdditive(request.SourceRuntimeKey);
                if (sceneOperation != null)
                    SourceSceneLoadOperationCount++;
                manifestOperation = request.UsesEntityScene
                    ? new EntitySceneSkippedPresentationManifestOperation()
                    : manifestApi.Load(request.ManifestRuntimeKey);
                if (!request.UsesEntityScene && manifestOperation != null)
                    PresentationManifestLoadOperationCount++;
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
                error =
                    "Operation-map source-scene or presentation-manifest load did not return an operation.";
                return RejectStart(OperationMapLoadResultCode.SourceLoadFailed, error);
            }

            expectedOperationMapId = request.OperationMapId;
            expectedSourceSceneGuid = request.SourceSceneGuid;
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
                UpdateEntitySceneReadiness(view);
                return;
            }

            if (!OperationMapPresentationManifestValidation.TryValidate(
                    manifestOperation.Manifest,
                    sceneOperation.Scene,
                    view,
                    expectedSourceSceneGuid,
                    out error))
            {
                Fail(OperationMapLoadResultCode.StaleContent, error);
                return;
            }

            PublishReady(view, manifestOperation.Manifest);
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
            SourceSceneUnloadOperationCount++;

            if (!packedEntitySceneOwnership.TryReleaseOwned(out _, out error))
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

            Fail(
                failureCode,
                string.IsNullOrWhiteSpace(failure)
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
                    error = "Operation-map failed EntityScene cleanup is still in progress.";
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

        private void UpdateEntitySceneReadiness(OperationMapSceneView view)
        {
            if (manifestOperation.Manifest != null)
            {
                Fail(
                    OperationMapLoadResultCode.StaleContent,
                    "EntityScene operation-map loads must not resolve a static presentation manifest.");
                return;
            }

            if (!packedEntitySceneOwnership.TryEnsureReady(
                    view,
                    expectedOperationMapId,
                    out bool ready,
                    out string error))
            {
                Fail(OperationMapLoadResultCode.MetadataBindFailed, error);
                return;
            }
            if (!ready)
            {
                SceneView = view;
                Manifest = null;
                Progress01 = 0.95f;
                return;
            }

            PublishReady(view, null);
        }

        private void PublishReady(
            OperationMapSceneView view,
            StaticMapPresentationManifest manifest)
        {
            SceneView = view;
            Manifest = manifest;
            Progress01 = 1f;
            IsReady = true;
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
            packedEntitySceneOwnership.ResetReleasedState();
            failedEntitySceneCleanupPending = false;
            unloading = false;
            UnloadComplete = false;
        }

        private void UpdateUnload()
        {
            Progress01 = sceneOperation.UnloadProgress01;
            if (!packedEntitySceneOwnership.TryReleaseOwned(
                    out bool packedReleaseComplete,
                    out string packedReleaseError))
            {
                Fail(OperationMapLoadResultCode.SourceUnloadFailed, packedReleaseError);
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
            if (!packedReleaseComplete)
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
            if (packedEntitySceneOwnership.OwnsScene)
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
            packedEntitySceneOwnership.TryReleaseOwned(out _, out _);
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

            if (!packedEntitySceneOwnership.TryReleaseOwned(
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
                    error = "Failed operation-map source scene is not available for cleanup.";
                    return false;
                }
                if (!sceneOperation.TryBeginUnload(out error))
                {
                    complete = false;
                    return false;
                }
            }

            bool sourceSceneComplete = sceneOperation == null || sceneOperation.UnloadDone;
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
            packedEntitySceneOwnership.TryReleaseOwned(out _, out _);
            sceneOperation?.Dispose();
            sceneOperation = null;
            manifestOperation?.Dispose();
            manifestOperation = null;
        }
    }
}
