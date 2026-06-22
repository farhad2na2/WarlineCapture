using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PerformanceDiagnosticsReferenceSystem
{
    public bool TryGet(out PerformanceDiagnosticsSystem diagnostics)
    {
        Scene scene = SceneManager.GetSceneByName(SceneLifecycleSystem.MenuSceneName);
        return TryGet(scene, out diagnostics);
    }

    public bool TryGet(Scene scene, out PerformanceDiagnosticsSystem diagnostics)
    {
        diagnostics = null;
        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
                continue;

            if (!root.TryGetComponent(out MenuBootstrapView menuBootstrap))
                continue;

            if (!menuBootstrap.IsPerformanceDiagnosticsInitialized)
                continue;

            PerformanceDiagnosticsSystem candidate = menuBootstrap.PerformanceDiagnostics;
            if (candidate == null)
                continue;

            diagnostics = candidate;
            return true;
        }

        return false;
    }
}
