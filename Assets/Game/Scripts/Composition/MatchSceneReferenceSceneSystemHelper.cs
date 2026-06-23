using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MatchSceneReferenceSceneSystemHelper
{
    public bool TryGetLoadedMatchSceneView(out MatchSceneView view)
    {
        return TryGetLoadedSceneView(SceneLifecycleSystem.MatchSceneName, out view);
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

        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
                continue;

            if (!root.TryGetComponent(out MatchSceneView candidate))
                continue;

            if (!IsLoadedSceneView(candidate, scene))
                continue;

            view = candidate;
            return true;
        }

        return false;
    }

    private static bool IsLoadedSceneView(MatchSceneView view, Scene expectedScene)
    {
        if (view == null || view.gameObject == null)
            return false;

        Scene scene = view.gameObject.scene;
        return scene.IsValid() &&
               scene.isLoaded &&
               scene == expectedScene;
    }
}
