using System;
using Game.Rendering;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Game.Composition
{
#if UNITY_EDITOR
    internal static class OperationMapEditorPlayModeExitState
    {
        internal static bool IsExitingPlayMode { get; private set; }

        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            IsExitingPlayMode = false;
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                IsExitingPlayMode = true;
            else if (state is UnityEditor.PlayModeStateChange.EnteredEditMode or
                     UnityEditor.PlayModeStateChange.EnteredPlayMode)
                IsExitingPlayMode = false;
        }
    }
#endif

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
#if UNITY_EDITOR
            string sceneGuid = runtimeKey?.ToString();
            string scenePath = string.IsNullOrWhiteSpace(sceneGuid)
                ? null
                : UnityEditor.AssetDatabase.GUIDToAssetPath(sceneGuid);
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new InvalidOperationException(
                    $"Operation-map source-scene address is not a project scene GUID: {sceneGuid ?? "<null>"}");
            }

            UnityEngine.AsyncOperation editorLoad =
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    scenePath,
                    new LoadSceneParameters(LoadSceneMode.Additive));
            if (editorLoad == null)
            {
                throw new InvalidOperationException(
                    $"Operation-map source scene did not start loading: {scenePath}");
            }

            return new OperationMapEditorSourceSceneOperation(scenePath, editorLoad);
#else
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(
                runtimeKey,
                LoadSceneMode.Additive,
                activateOnLoad: true);
            return new OperationMapAddressablesSourceSceneOperation(handle);
#endif
        }
    }

#if UNITY_EDITOR
    internal sealed class OperationMapEditorSourceSceneOperation :
        IOperationMapSourceSceneOperation
    {
        private readonly string scenePath;
        private readonly UnityEngine.AsyncOperation loadOperation;
        private UnityEngine.AsyncOperation unloadOperation;
        private bool disposed;

        internal OperationMapEditorSourceSceneOperation(
            string scenePath,
            UnityEngine.AsyncOperation loadOperation)
        {
            this.scenePath = scenePath;
            this.loadOperation = loadOperation;
        }

        private Scene LoadedScene => SceneManager.GetSceneByPath(scenePath);

        public bool IsDone => loadOperation.isDone;
        public bool Succeeded => IsDone && LoadedScene.IsValid() && LoadedScene.isLoaded;
        public float Progress01 => loadOperation.progress;
        public Scene Scene => Succeeded ? LoadedScene : default;
        public string Failure => IsDone && !Succeeded
            ? $"Operation-map source scene did not load: {scenePath}"
            : null;
        public bool UnloadStarted { get; private set; }
        public bool UnloadDone => UnloadStarted && unloadOperation != null && unloadOperation.isDone;
        public bool UnloadSucceeded => UnloadDone && !LoadedScene.isLoaded;
        public float UnloadProgress01 => unloadOperation?.progress ?? 0f;
        public string UnloadFailure => UnloadDone && !UnloadSucceeded
            ? $"Operation-map source scene did not unload: {scenePath}"
            : null;

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
            if (!Succeeded)
            {
                error = "Operation-map source scene must finish loading before unload begins.";
                return false;
            }

            unloadOperation = SceneManager.UnloadSceneAsync(LoadedScene);
            if (unloadOperation == null)
            {
                error = $"Operation-map source scene did not start unloading: {scenePath}";
                return false;
            }

            UnloadStarted = true;
            error = null;
            return true;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (OperationMapEditorPlayModeExitState.IsExitingPlayMode ||
                !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ||
                UnloadStarted ||
                !Succeeded)
            {
                return;
            }

            unloadOperation = SceneManager.UnloadSceneAsync(LoadedScene);
            UnloadStarted = unloadOperation != null;
        }
    }
#endif

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
                unloadHandle = Addressables.UnloadSceneAsync(
                    handle,
                    autoReleaseHandle: false);
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
#if UNITY_EDITOR
            // The Editor unloads every play-mode scene as part of the transition back to
            // Edit Mode. Starting or releasing a second Addressables scene operation from
            // OnDisable races Addressables' own play-mode cleanup and leaves an invalid
            // scene handle in its tracked-scene set. Let that cleanup own the handles.
            if (OperationMapEditorPlayModeExitState.IsExitingPlayMode ||
                !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                unloadHandle = default;
                handle = default;
                return;
            }
#endif
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
