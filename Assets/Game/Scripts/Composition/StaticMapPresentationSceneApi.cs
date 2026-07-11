using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Composition
{
    internal interface IStaticMapPresentationSceneOperation
    {
        bool IsDone { get; }
        float Progress01 { get; }
    }

    internal interface IStaticMapPresentationSceneApi
    {
        bool IsLoaded(string scenePath);
        IStaticMapPresentationSceneOperation LoadAdditive(string scenePath);
        IStaticMapPresentationSceneOperation Unload(string scenePath);
    }

    internal sealed class StaticMapPresentationUnitySceneApi : IStaticMapPresentationSceneApi
    {
        public bool IsLoaded(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            return scene.IsValid() && scene.isLoaded;
        }

        public IStaticMapPresentationSceneOperation LoadAdditive(string scenePath)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            return operation == null
                ? null
                : new StaticMapPresentationUnitySceneOperation(operation);
        }

        public IStaticMapPresentationSceneOperation Unload(string scenePath)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(scenePath);
            return operation == null
                ? null
                : new StaticMapPresentationUnitySceneOperation(operation);
        }
    }

    internal sealed class StaticMapPresentationUnitySceneOperation :
        IStaticMapPresentationSceneOperation
    {
        private readonly AsyncOperation _operation;

        internal StaticMapPresentationUnitySceneOperation(AsyncOperation operation)
        {
            _operation = operation;
        }

        public bool IsDone => _operation.isDone;
        public float Progress01 => _operation.progress;
    }
}
