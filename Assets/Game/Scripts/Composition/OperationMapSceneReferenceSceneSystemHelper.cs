using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Composition
{
    internal sealed class OperationMapSceneReferenceSceneSystemHelper
    {
        private readonly List<GameObject> roots = new(4);
        private readonly List<OperationMapSceneView> candidates = new(2);

        public bool TryGetLoadedSceneView(
            Scene scene,
            string expectedOperationMapId,
            out OperationMapSceneView view,
            out string error)
        {
            view = null;
            roots.Clear();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                error = "Operation-map source scene is not loaded.";
                return false;
            }

            scene.GetRootGameObjects(roots);
            int viewCount = 0;
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                    continue;

                candidates.Clear();
                root.GetComponentsInChildren(true, candidates);
                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    OperationMapSceneView candidate = candidates[candidateIndex];
                    if (candidate == null || candidate.gameObject.scene != scene)
                        continue;

                    view = candidate;
                    viewCount++;
                }
            }

            if (viewCount != 1)
            {
                view = null;
                error = $"Operation-map source scene requires exactly one scene view; found {viewCount}.";
                return false;
            }

            if (!string.IsNullOrEmpty(expectedOperationMapId) &&
                !string.Equals(view.OperationMapId, expectedOperationMapId, StringComparison.Ordinal))
            {
                view = null;
                error = "Loaded operation-map scene view identity does not match the requested map.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
