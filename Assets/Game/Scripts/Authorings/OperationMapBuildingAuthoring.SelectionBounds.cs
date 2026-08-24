using Unity.Mathematics;
using UnityEngine;

namespace Game.Authoring
{
    public sealed partial class OperationMapBuildingAuthoring
    {
        private static bool HasFiniteTransform(Transform owner)
        {
            Vector3 position = owner.localPosition;
            Quaternion rotation = owner.localRotation;
            Vector3 scale = owner.localScale;
            return IsFinite(position.x) && IsFinite(position.y) && IsFinite(position.z) &&
                   IsFinite(rotation.x) && IsFinite(rotation.y) && IsFinite(rotation.z) &&
                   IsFinite(rotation.w) && IsFinite(scale.x) && IsFinite(scale.y) &&
                   IsFinite(scale.z) && math.abs(scale.x) > 0.000001f &&
                   math.abs(scale.y) > 0.000001f && math.abs(scale.z) > 0.000001f;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static bool TryGetSelectionLocalBounds(
            Transform owner,
            GameObject visualRoot,
            out Bounds combinedBounds)
        {
            combinedBounds = default;
            if (owner == null || visualRoot == null)
                return false;

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Matrix4x4 worldToOwner = owner.worldToLocalMatrix;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Bounds localBounds = TransformBounds(
                    worldToOwner * renderer.localToWorldMatrix,
                    renderer.localBounds);
                if (!hasBounds)
                {
                    combinedBounds = localBounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(localBounds);
                }
            }

            return hasBounds &&
                   HasFiniteBounds(combinedBounds) &&
                   combinedBounds.extents.sqrMagnitude > 0.000001f;
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = bounds.center +
                                 Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                Vector3 transformed = matrix.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, transformed);
                max = Vector3.Max(max, transformed);
            }

            Bounds transformedBounds = new();
            transformedBounds.SetMinMax(min, max);
            return transformedBounds;
        }

        private static bool HasFiniteBounds(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            return IsFinite(center.x) && IsFinite(center.y) && IsFinite(center.z) &&
                   IsFinite(extents.x) && IsFinite(extents.y) && IsFinite(extents.z);
        }
    }
}
