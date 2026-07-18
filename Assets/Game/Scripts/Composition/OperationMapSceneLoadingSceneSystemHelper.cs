using System;
using Game.Configs;
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
    }

    internal interface IOperationMapSourceSceneApi
    {
        IOperationMapSourceSceneOperation LoadAdditive(object runtimeKey);
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

    internal sealed class OperationMapAddressablesSourceSceneOperation :
        IOperationMapSourceSceneOperation
    {
        private AsyncOperationHandle<SceneInstance> handle;
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

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (!handle.IsValid())
                return;

            if (handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
                Addressables.UnloadSceneAsync(handle, autoReleaseHandle: true);
            else
                Addressables.Release(handle);
        }
    }

    internal sealed class OperationMapSceneLoadingSceneSystemHelper : IDisposable
    {
        private readonly IOperationMapSourceSceneApi sceneApi;
        private readonly OperationMapSceneReferenceSceneSystemHelper sceneReference;
        private IOperationMapSourceSceneOperation operation;
        private string expectedOperationMapId;
        private bool disposed;

        public OperationMapSceneLoadingSceneSystemHelper(
            IOperationMapSourceSceneApi sceneApi = null,
            OperationMapSceneReferenceSceneSystemHelper sceneReference = null)
        {
            this.sceneApi = sceneApi ?? new OperationMapAddressablesSourceSceneApi();
            this.sceneReference = sceneReference ?? new OperationMapSceneReferenceSceneSystemHelper();
        }

        public bool IsLoading => operation != null && !IsReady && !HasFailed;
        public bool IsReady { get; private set; }
        public bool HasFailed => !string.IsNullOrEmpty(Failure);
        public float Progress01 { get; private set; }
        public string Failure { get; private set; }
        public OperationMapSceneView SceneView { get; private set; }

        public bool TryStart(OperationMapDefinition definition, out string error)
        {
            if (disposed)
            {
                error = "Operation-map source-scene loader is disposed.";
                return false;
            }

            if (operation != null)
            {
                error = "Operation-map source-scene loader already owns an operation.";
                return false;
            }

            if (definition == null)
            {
                error = "Operation-map definition is required.";
                return false;
            }

            if (!definition.TryValidateIdentity(out error))
                return false;

            AssetReference sourceReference = definition.SourceSceneReference;
            if (sourceReference == null || !sourceReference.RuntimeKeyIsValid())
            {
                error = "Operation-map source-scene reference is missing or invalid.";
                return false;
            }

            try
            {
                operation = sceneApi.LoadAdditive(sourceReference.RuntimeKey);
            }
            catch (Exception exception)
            {
                error = $"Operation-map source-scene load did not start: {exception.Message}";
                return false;
            }

            if (operation == null)
            {
                error = "Operation-map source-scene load did not return an operation.";
                return false;
            }

            expectedOperationMapId = definition.OperationMapId;
            Progress01 = 0f;
            error = null;
            return true;
        }

        public void Update()
        {
            if (disposed || operation == null || IsReady || HasFailed)
                return;

            Progress01 = operation.Progress01;
            if (!operation.IsDone)
                return;

            if (!operation.Succeeded)
            {
                Fail(string.IsNullOrWhiteSpace(operation.Failure)
                    ? "Operation-map source-scene load failed."
                    : operation.Failure);
                return;
            }

            if (!sceneReference.TryGetLoadedSceneView(
                    operation.Scene,
                    expectedOperationMapId,
                    out OperationMapSceneView view,
                    out string error) ||
                !view.TryValidate(out error))
            {
                Fail(error);
                return;
            }

            SceneView = view;
            Progress01 = 1f;
            IsReady = true;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            operation?.Dispose();
            operation = null;
            SceneView = null;
            IsReady = false;
            Progress01 = 0f;
        }

        private void Fail(string error)
        {
            Failure = string.IsNullOrWhiteSpace(error)
                ? "Operation-map source-scene load failed."
                : error;
            operation.Dispose();
            operation = null;
            SceneView = null;
            IsReady = false;
            Progress01 = 0f;
        }
    }
}
