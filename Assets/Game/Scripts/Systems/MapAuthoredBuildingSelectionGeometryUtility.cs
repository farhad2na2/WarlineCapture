using System;
using Game.Components;
using UnityEngine;

namespace Game.Runtime
{
    internal static class MapAuthoredBuildingSelectionGeometryUtility
    {
        private const string SelectionObjectOutlineToken = "SelectionObjectOutline";
        private const float MaximumAreaRatio = 1.25f;
        private const float MaximumAxisRatio = 1.25f;

        internal static bool TryResolvePlausibleOwnedRendererBounds(
            GameObject instance,
            MapAuthoredBuildingVisualComponent authoredVisual,
            Vector2Int footprintCells,
            GridConfig grid,
            out Bounds bounds)
        {
            bounds = default;
            if (instance == null || authoredVisual == null ||
                !authoredVisual.HasPresentationWorldCenter || authoredVisual.HasPresentationGeometry)
            {
                return false;
            }

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(false);
            bool hasBounds = false;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null || !renderer.enabled || IsSelectionObjectOutlineRenderer(renderer))
                    continue;

                if (hasBounds)
                    bounds.Encapsulate(renderer.bounds);
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
            }

            if (!hasBounds || !IsFinite(bounds.center) || !IsFinite(bounds.size))
                return false;

            float canonicalWidth = Mathf.Max(grid.CellSize, footprintCells.x * grid.CellSize);
            float canonicalDepth = Mathf.Max(grid.CellSize, footprintCells.y * grid.CellSize);
            float rendererWidth = Mathf.Max(0f, bounds.size.x);
            float rendererDepth = Mathf.Max(0f, bounds.size.z);
            float canonicalArea = canonicalWidth * canonicalDepth;
            float rendererArea = rendererWidth * rendererDepth;
            float canonicalLongestAxis = Mathf.Max(canonicalWidth, canonicalDepth);
            float rendererLongestAxis = Mathf.Max(rendererWidth, rendererDepth);
            if (rendererWidth <= 0.001f || rendererDepth <= 0.001f ||
                rendererArea > canonicalArea * MaximumAreaRatio ||
                rendererLongestAxis > canonicalLongestAxis * MaximumAxisRatio)
            {
                return false;
            }

            Vector3 authoredCenter = authoredVisual.PresentationWorldCenter;
            float horizontalCenterDistance = Vector2.Distance(
                new Vector2(bounds.center.x, bounds.center.z),
                new Vector2(authoredCenter.x, authoredCenter.z));
            float maximumCenterDistance = Mathf.Max(
                grid.CellSize,
                0.5f * (canonicalLongestAxis + rendererLongestAxis));
            return horizontalCenterDistance <= maximumCenterDistance;
        }

        private static bool IsSelectionObjectOutlineRenderer(Renderer renderer)
        {
            Transform current = renderer.transform;
            while (current != null)
            {
                if (current.name.IndexOf(SelectionObjectOutlineToken, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                current = current.parent;
            }

            return false;
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
