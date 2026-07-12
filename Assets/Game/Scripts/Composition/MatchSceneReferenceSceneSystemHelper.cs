using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Runtime;

namespace Game.Composition
{
    public sealed class MatchSceneReferenceSceneSystemHelper
    {
        private readonly System.Collections.Generic.List<GameObject> _roots = new(16);

        public bool TryGetLoadedMatchSceneView(out MatchSceneView view)
        {
            return TryGetLoadedSceneView(SceneLifecycleSceneSystemHelper.MatchSceneName, out view);
        }

        public bool TryGetLoadedSceneView(string sceneName, out MatchSceneView view)
        {
            view = null;

            if (string.IsNullOrEmpty(sceneName))
                return false;

            Scene scene = SceneManager.GetSceneByName(sceneName);
            return TryGetLoadedSceneView(scene, out view);
        }

        public bool TryGetLoadedSceneView(Scene scene, out MatchSceneView view)
        {
            view = null;
            _roots.Clear();
            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            scene.GetRootGameObjects(_roots);
            for (int i = 0; i < _roots.Count; i++)
            {
                GameObject root = _roots[i];
                if (root == null || !root.TryGetComponent(out MatchSceneView candidate))
                    continue;

                Scene candidateScene = candidate.gameObject.scene;
                if (!candidateScene.IsValid() || !candidateScene.isLoaded || candidateScene != scene)
                    continue;

                view = candidate;
                return true;
            }

            return false;
        }
    }
}
