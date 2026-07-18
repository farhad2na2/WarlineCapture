using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Rendering;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Game.Composition
{
    internal interface IStaticMapPresentationAddressablesSceneOperation :
        IStaticMapPresentationSceneOperation,
        IDisposable
    {
        bool Succeeded { get; }
        bool SceneLoaded { get; }
    }

    internal interface IStaticMapPresentationAddressablesSceneBackend
    {
        IStaticMapPresentationAddressablesSceneOperation LoadAdditive(string address);
        IStaticMapPresentationAddressablesSceneOperation Unload(
            IStaticMapPresentationAddressablesSceneOperation loadOperation);
    }

    internal sealed class StaticMapPresentationAddressablesSceneApi :
        IStaticMapPresentationSceneApi,
        IStaticMapPresentationManifestBindingSceneApi
    {
        private sealed class SceneState
        {
            internal IStaticMapPresentationAddressablesSceneOperation Load;
            internal IStaticMapPresentationAddressablesSceneOperation Unload;
        }

        private readonly IStaticMapPresentationAddressablesSceneBackend backend;
        private readonly Dictionary<string, string> addressesByPath = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SceneState> statesByPath = new(StringComparer.Ordinal);

        internal StaticMapPresentationAddressablesSceneApi(
            IStaticMapPresentationAddressablesSceneBackend backend = null)
        {
            this.backend = backend ?? new StaticMapPresentationAddressablesSceneBackend();
        }

        internal int RetainedSceneCount => statesByPath.Count;

        public bool TryBindManifest(
            StaticMapPresentationManifest manifest,
            out string error)
        {
            if (statesByPath.Count != 0)
            {
                error = "Static-map Addressables scenes must drain before binding another manifest.";
                return false;
            }

            if (manifest == null ||
                !StaticMapPresentationManifest.HasRequiredIdentity(
                    manifest.SchemaVersion,
                    manifest.OperationMapId,
                    manifest.CanonicalSceneGuid,
                    manifest.CanonicalScenePath) ||
                manifest.Chunks == null || manifest.Chunks.Count == 0)
            {
                error = "Static-map Addressables manifest is missing or invalid.";
                return false;
            }

            addressesByPath.Clear();
            error = null;
            for (int index = 0; index < manifest.Chunks.Count; index++)
            {
                StaticMapPresentationChunkEntry chunk = manifest.Chunks[index];
                if (chunk == null || string.IsNullOrWhiteSpace(chunk.ScenePath) ||
                    !OperationMapContentAddressContract.TryBuildPresentationChunkAddress(
                        manifest.OperationMapId,
                        chunk.ChunkId,
                        out string address,
                        out error) ||
                    !addressesByPath.TryAdd(chunk.ScenePath, address))
                {
                    addressesByPath.Clear();
                    error ??= $"Static-map Addressables chunk {index} has a duplicate or invalid scene path.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool IsLoaded(string scenePath)
        {
            if (!statesByPath.TryGetValue(scenePath, out SceneState state))
                return false;

            CompleteUnload(scenePath, state);
            return statesByPath.TryGetValue(scenePath, out state) && state.Load.SceneLoaded;
        }

        public IStaticMapPresentationSceneOperation LoadAdditive(string scenePath)
        {
            if (!addressesByPath.TryGetValue(scenePath, out string address))
                throw new InvalidOperationException($"Static-map scene path is not bound: {scenePath}");

            if (statesByPath.TryGetValue(scenePath, out SceneState existing))
            {
                if (!existing.Load.IsDone || existing.Unload != null)
                    throw new InvalidOperationException($"Static-map scene already has an active operation: {scenePath}");
                if (existing.Load.Succeeded && existing.Load.SceneLoaded)
                    return null;

                existing.Load.Dispose();
                statesByPath.Remove(scenePath);
            }

            IStaticMapPresentationAddressablesSceneOperation operation =
                backend.LoadAdditive(address);
            if (operation == null)
                return null;

            statesByPath.Add(scenePath, new SceneState { Load = operation });
            return operation;
        }

        public IStaticMapPresentationSceneOperation Unload(string scenePath)
        {
            if (!statesByPath.TryGetValue(scenePath, out SceneState state) ||
                !state.Load.IsDone || !state.Load.Succeeded || !state.Load.SceneLoaded)
                return null;
            if (state.Unload != null)
                throw new InvalidOperationException($"Static-map scene unload is already active: {scenePath}");

            state.Unload = backend.Unload(state.Load);
            return state.Unload == null
                ? null
                : new StaticMapPresentationAddressablesUnloadOperation(
                    state.Unload,
                    () => CompleteUnload(scenePath, state));
        }

        private void CompleteUnload(string scenePath, SceneState state)
        {
            if (state.Unload == null || !state.Unload.IsDone)
                return;

            if (state.Unload.Succeeded || !state.Load.SceneLoaded)
            {
                state.Unload.Dispose();
                state.Load.Dispose();
                statesByPath.Remove(scenePath);
                return;
            }

            state.Unload.Dispose();
            state.Unload = null;
        }
    }

    internal sealed class StaticMapPresentationAddressablesUnloadOperation :
        IStaticMapPresentationSceneOperation
    {
        private readonly IStaticMapPresentationAddressablesSceneOperation operation;
        private readonly Action complete;

        internal StaticMapPresentationAddressablesUnloadOperation(
            IStaticMapPresentationAddressablesSceneOperation operation,
            Action complete)
        {
            this.operation = operation;
            this.complete = complete;
        }

        public bool IsDone
        {
            get
            {
                bool done = operation.IsDone;
                if (done)
                    complete();
                return done;
            }
        }

        public float Progress01 => operation.Progress01;
    }

    internal sealed class StaticMapPresentationAddressablesSceneBackend :
        IStaticMapPresentationAddressablesSceneBackend
    {
        public IStaticMapPresentationAddressablesSceneOperation LoadAdditive(string address)
        {
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(
                address,
                LoadSceneMode.Additive,
                activateOnLoad: true);
            return new StaticMapPresentationAddressablesLoadOperation(handle);
        }

        public IStaticMapPresentationAddressablesSceneOperation Unload(
            IStaticMapPresentationAddressablesSceneOperation loadOperation)
        {
            if (loadOperation is not StaticMapPresentationAddressablesLoadOperation addressablesLoad)
                throw new InvalidOperationException("Addressables unload requires its retained load operation.");

            AsyncOperationHandle<SceneInstance> handle =
                Addressables.UnloadSceneAsync(addressablesLoad.TakeHandle(), autoReleaseHandle: false);
            return new StaticMapPresentationAddressablesUnloadHandleOperation(handle);
        }
    }

    internal sealed class StaticMapPresentationAddressablesLoadOperation :
        IStaticMapPresentationAddressablesSceneOperation
    {
        private AsyncOperationHandle<SceneInstance> handle;
        private bool transferred;
        private bool disposed;

        internal StaticMapPresentationAddressablesLoadOperation(
            AsyncOperationHandle<SceneInstance> handle)
        {
            this.handle = handle;
        }

        public bool IsDone => handle.IsValid() && handle.IsDone;
        public bool Succeeded => handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded;
        public bool SceneLoaded => Succeeded && handle.Result.Scene.IsValid() && handle.Result.Scene.isLoaded;
        public float Progress01 => handle.IsValid() ? handle.PercentComplete : 0f;

        internal AsyncOperationHandle<SceneInstance> TakeHandle()
        {
            if (disposed || transferred || !handle.IsValid())
                throw new InvalidOperationException("Addressables scene-load handle is unavailable for unload.");
            transferred = true;
            return handle;
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (!transferred && handle.IsValid())
                Addressables.Release(handle);
        }
    }

    internal sealed class StaticMapPresentationAddressablesUnloadHandleOperation :
        IStaticMapPresentationAddressablesSceneOperation
    {
        private AsyncOperationHandle<SceneInstance> handle;
        private bool disposed;

        internal StaticMapPresentationAddressablesUnloadHandleOperation(
            AsyncOperationHandle<SceneInstance> handle)
        {
            this.handle = handle;
        }

        public bool IsDone => !handle.IsValid() || handle.IsDone;
        public bool Succeeded => !handle.IsValid() || handle.Status == AsyncOperationStatus.Succeeded;
        public bool SceneLoaded => false;
        public float Progress01 => handle.IsValid() ? handle.PercentComplete : 1f;
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (handle.IsValid())
                Addressables.Release(handle);
        }
    }
}
