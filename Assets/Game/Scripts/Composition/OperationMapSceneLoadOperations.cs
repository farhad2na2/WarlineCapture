using System;
using Game.Rendering;
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
}
