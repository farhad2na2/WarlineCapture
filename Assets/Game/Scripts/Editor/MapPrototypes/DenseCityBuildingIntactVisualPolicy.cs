using System;
using UnityEngine;

namespace Game.Editor
{
    internal static class DenseCityBuildingIntactVisualPolicy
    {
        private const string ModelBranchName = "Model";
        private const string EmbeddedDestroyedBranchName = "Destroyed";

        internal static bool ShouldIncludeRenderer(GameObject intactVisual, Renderer renderer)
        {
            if (intactVisual == null)
                throw new ArgumentNullException(nameof(intactVisual));
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));
            if (!TryGetEmbeddedDestroyedAlternative(
                    intactVisual.transform,
                    out _,
                    out Transform destroyedAlternative,
                    out string error))
            {
                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException(error);
                return true;
            }

            Transform branch = GetDirectBranch(intactVisual.transform, renderer.transform);
            return branch != destroyedAlternative;
        }

        internal static int NormalizeRealizedIntactVisual(GameObject intactVisual)
        {
            if (intactVisual == null)
                throw new ArgumentNullException(nameof(intactVisual));
            if (!TryGetEmbeddedDestroyedAlternative(
                    intactVisual.transform,
                    out _,
                    out Transform destroyedAlternative,
                    out string error))
            {
                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException(error);
                return 0;
            }

            UnityEngine.Object.DestroyImmediate(destroyedAlternative.gameObject);
            if (FindDirectChild(intactVisual.transform, EmbeddedDestroyedBranchName) != null)
            {
                throw new InvalidOperationException(
                    $"Dense-city intact visual '{intactVisual.name}' retained its embedded " +
                    "destroyed alternative after normalization.");
            }
            return 1;
        }

        internal static bool TryValidateNormalized(
            GameObject intactVisual,
            out string error)
        {
            if (intactVisual == null)
            {
                error = "Dense-city intact visual is missing.";
                return false;
            }
            if (!TryGetEmbeddedDestroyedAlternative(
                    intactVisual.transform,
                    out _,
                    out _,
                    out error))
            {
                return string.IsNullOrEmpty(error);
            }

            error =
                $"Dense-city intact visual '{intactVisual.name}' contains co-located direct " +
                $"'{ModelBranchName}' and '{EmbeddedDestroyedBranchName}' render branches.";
            return false;
        }

        private static bool TryGetEmbeddedDestroyedAlternative(
            Transform root,
            out Transform model,
            out Transform destroyedAlternative,
            out string error)
        {
            model = FindDirectChild(root, ModelBranchName);
            destroyedAlternative = FindDirectChild(root, EmbeddedDestroyedBranchName);
            error = null;
            if (model == null || destroyedAlternative == null)
                return false;
            if (model.GetComponentsInChildren<Renderer>(true).Length == 0 ||
                destroyedAlternative.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                error =
                    $"Dense-city intact visual '{root.name}' has ambiguous '{ModelBranchName}'/" +
                    $"'{EmbeddedDestroyedBranchName}' branches without renderers.";
                return false;
            }
            return true;
        }

        private static Transform FindDirectChild(Transform root, string name)
        {
            Transform match = null;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (!string.Equals(child.name, name, StringComparison.Ordinal))
                    continue;
                if (match != null)
                {
                    throw new InvalidOperationException(
                        $"Dense-city visual '{root.name}' has more than one direct '{name}' branch.");
                }
                match = child;
            }
            return match;
        }

        private static Transform GetDirectBranch(Transform root, Transform descendant)
        {
            Transform current = descendant;
            while (current != null && current.parent != root)
                current = current.parent;
            if (current == null)
            {
                throw new InvalidOperationException(
                    $"Renderer '{descendant.name}' is not beneath intact visual '{root.name}'.");
            }
            return current;
        }
    }
}
