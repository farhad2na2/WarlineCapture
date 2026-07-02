using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Runtime;

namespace Game.Composition
{
    public sealed class PerformanceDiagnosticsReferenceDiagnosticsSystemHelper
    {
        public bool TryGet(out PerformanceDiagnosticsSystemHelper diagnostics)
        {
            Scene scene = SceneManager.GetSceneByName(SceneLifecycleSceneSystemHelper.MenuSceneName);
            return TryGet(scene, out diagnostics);
        }

        public bool TryGet(Scene scene, out PerformanceDiagnosticsSystemHelper diagnostics)
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

                PerformanceDiagnosticsSystemHelper candidate = menuBootstrap.PerformanceDiagnostics;
                if (candidate == null)
                    continue;

                diagnostics = candidate;
                return true;
            }

            return false;
        }
    }
}
