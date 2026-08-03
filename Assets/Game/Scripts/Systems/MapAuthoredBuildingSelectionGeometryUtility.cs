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

        internal enum EvaluationReason
        {
            None = 0,
            Accepted,
            MissingInstance,
            MissingAuthoredVisual,
            MissingPresentationCenter,
            ExactPresentationGeometryPresent,
            NoOwnedRendererBounds,
            NonFiniteBounds,
            NonPositivePlanarBounds,
            AreaExceedsCanonicalLimit,
            AxisExceedsCanonicalLimit,
            CenterExceedsCanonicalLimit
        }

        internal readonly struct Evaluation
        {
            public readonly EvaluationReason Reason;
            public readonly Bounds Bounds;
            public readonly int RendererCount;
            public readonly int IncludedRendererCount;
            public readonly int OutlineRendererCount;
            public readonly float CanonicalArea;
            public readonly float RendererArea;
            public readonly float CanonicalLongestAxis;
            public readonly float RendererLongestAxis;
            public readonly float CenterDistance;
            public readonly float MaximumCenterDistance;

            public bool Accepted => Reason == EvaluationReason.Accepted;

            public Evaluation(
                EvaluationReason reason,
                Bounds bounds = default,
                int rendererCount = 0,
                int includedRendererCount = 0,
                int outlineRendererCount = 0,
                float canonicalArea = 0f,
                float rendererArea = 0f,
                float canonicalLongestAxis = 0f,
                float rendererLongestAxis = 0f,
                float centerDistance = 0f,
                float maximumCenterDistance = 0f)
            {
                Reason = reason;
                Bounds = bounds;
                RendererCount = rendererCount;
                IncludedRendererCount = includedRendererCount;
                OutlineRendererCount = outlineRendererCount;
                CanonicalArea = canonicalArea;
                RendererArea = rendererArea;
                CanonicalLongestAxis = canonicalLongestAxis;
                RendererLongestAxis = rendererLongestAxis;
                CenterDistance = centerDistance;
                MaximumCenterDistance = maximumCenterDistance;
            }
        }

        internal static bool TryResolvePlausibleOwnedRendererBounds(
            GameObject instance,
            MapAuthoredBuildingVisualComponent authoredVisual,
            Vector2Int footprintCells,
            GridConfig grid,
            out Bounds bounds)
        {
            Evaluation evaluation = EvaluateOwnedRendererBounds(instance, authoredVisual, footprintCells, grid);
            bounds = evaluation.Bounds;
            return evaluation.Accepted;
        }

        internal static Evaluation EvaluateOwnedRendererBounds(
            GameObject instance,
            MapAuthoredBuildingVisualComponent authoredVisual,
            Vector2Int footprintCells,
            GridConfig grid)
        {
            if (instance == null)
                return new Evaluation(EvaluationReason.MissingInstance);
            if (authoredVisual == null)
                return new Evaluation(EvaluationReason.MissingAuthoredVisual);
            if (!authoredVisual.HasPresentationWorldCenter)
                return new Evaluation(EvaluationReason.MissingPresentationCenter);
            if (authoredVisual.HasPresentationGeometry)
                return new Evaluation(EvaluationReason.ExactPresentationGeometryPresent);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(false);
            bool hasBounds = false;
            int includedRendererCount = 0;
            int outlineRendererCount = 0;
            Bounds bounds = default;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null || !renderer.enabled)
                    continue;
                if (IsSelectionObjectOutlineRenderer(renderer))
                {
                    outlineRendererCount++;
                    continue;
                }

                if (hasBounds)
                    bounds.Encapsulate(renderer.bounds);
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                includedRendererCount++;
            }

            if (!hasBounds)
            {
                return new Evaluation(
                    EvaluationReason.NoOwnedRendererBounds,
                    rendererCount: renderers.Length,
                    includedRendererCount: includedRendererCount,
                    outlineRendererCount: outlineRendererCount);
            }
            if (!IsFinite(bounds.center) || !IsFinite(bounds.size))
            {
                return new Evaluation(
                    EvaluationReason.NonFiniteBounds,
                    bounds,
                    renderers.Length,
                    includedRendererCount,
                    outlineRendererCount);
            }

            float canonicalWidth = Mathf.Max(grid.CellSize, footprintCells.x * grid.CellSize);
            float canonicalDepth = Mathf.Max(grid.CellSize, footprintCells.y * grid.CellSize);
            float rendererWidth = Mathf.Max(0f, bounds.size.x);
            float rendererDepth = Mathf.Max(0f, bounds.size.z);
            float canonicalArea = canonicalWidth * canonicalDepth;
            float rendererArea = rendererWidth * rendererDepth;
            float canonicalLongestAxis = Mathf.Max(canonicalWidth, canonicalDepth);
            float rendererLongestAxis = Mathf.Max(rendererWidth, rendererDepth);
            EvaluationReason reason = EvaluationReason.Accepted;
            if (rendererWidth <= 0.001f || rendererDepth <= 0.001f)
                reason = EvaluationReason.NonPositivePlanarBounds;
            else if (rendererArea > canonicalArea * MaximumAreaRatio)
                reason = EvaluationReason.AreaExceedsCanonicalLimit;
            else if (rendererLongestAxis > canonicalLongestAxis * MaximumAxisRatio)
                reason = EvaluationReason.AxisExceedsCanonicalLimit;

            Vector3 authoredCenter = authoredVisual.PresentationWorldCenter;
            float horizontalCenterDistance = Vector2.Distance(
                new Vector2(bounds.center.x, bounds.center.z),
                new Vector2(authoredCenter.x, authoredCenter.z));
            float maximumCenterDistance = Mathf.Max(
                grid.CellSize,
                0.5f * (canonicalLongestAxis + rendererLongestAxis));
            if (reason == EvaluationReason.Accepted && horizontalCenterDistance > maximumCenterDistance)
                reason = EvaluationReason.CenterExceedsCanonicalLimit;

            return new Evaluation(
                reason,
                bounds,
                renderers.Length,
                includedRendererCount,
                outlineRendererCount,
                canonicalArea,
                rendererArea,
                canonicalLongestAxis,
                rendererLongestAxis,
                horizontalCenterDistance,
                maximumCenterDistance);
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
