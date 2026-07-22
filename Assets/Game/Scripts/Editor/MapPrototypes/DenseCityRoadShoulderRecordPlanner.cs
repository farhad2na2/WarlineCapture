using System;
using System.Collections.Generic;
using Game.Runtime;
using UnityEngine;

namespace Game.Editor
{
    internal static class DenseCityRoadShoulderRecordPlanner
    {
        internal static DenseCityRoadShoulderRecordInput[] Create(
            IReadOnlyList<RoadGridProjectionSystem.RoadFootprintBoundsData> footprintBounds,
            Matrix4x4 roadWorldMatrix,
            Vector2Int chunk)
        {
            if (footprintBounds == null)
                throw new ArgumentNullException(nameof(footprintBounds));

            var sidewalkBounds = new List<Bounds>(footprintBounds.Count);
            for (int index = 0; index < footprintBounds.Count; index++)
            {
                RoadGridProjectionSystem.RoadFootprintBoundsData footprint = footprintBounds[index];
                if (footprint == null ||
                    footprint.Kind != RoadGridProjectionSystem.RoadFootprintKind.Sidewalk)
                {
                    continue;
                }

                Bounds bounds = footprint.Bounds;
                if (!IsFinitePositive(bounds))
                    throw new InvalidOperationException("Authored road sidewalk bounds must be finite and positive.");
                sidewalkBounds.Add(bounds);
            }

            sidewalkBounds.Sort(CompareBounds);
            float widthScale = GetHorizontalAxisLength(roadWorldMatrix.GetColumn(0));
            float depthScale = GetHorizontalAxisLength(roadWorldMatrix.GetColumn(2));
            var result = new DenseCityRoadShoulderRecordInput[sidewalkBounds.Count];
            for (int index = 0; index < sidewalkBounds.Count; index++)
            {
                Bounds bounds = sidewalkBounds[index];
                Vector3 localSurfaceCenter = new(bounds.center.x, bounds.max.y, bounds.center.z);
                Vector3 worldCenter = roadWorldMatrix.MultiplyPoint3x4(localSurfaceCenter);
                Matrix4x4 shoulderWorldMatrix = roadWorldMatrix;
                shoulderWorldMatrix.SetColumn(3, new Vector4(worldCenter.x, worldCenter.y, worldCenter.z, 1f));
                result[index] = new DenseCityRoadShoulderRecordInput(
                    shoulderWorldMatrix,
                    new Vector2(bounds.size.x * widthScale, bounds.size.z * depthScale),
                    worldCenter.y,
                    chunk);
            }

            return result;
        }

        private static float GetHorizontalAxisLength(Vector4 axis)
        {
            float length = new Vector2(axis.x, axis.z).magnitude;
            if (!float.IsFinite(length) || length <= 0.000001f)
                throw new ArgumentOutOfRangeException(nameof(axis));
            return length;
        }

        private static bool IsFinitePositive(Bounds bounds) =>
            IsFinite(bounds.center) &&
            IsFinite(bounds.size) &&
            bounds.size.x > 0f &&
            bounds.size.z > 0f;

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static int CompareBounds(Bounds left, Bounds right)
        {
            int comparison = left.center.x.CompareTo(right.center.x);
            if (comparison != 0)
                return comparison;
            comparison = left.center.z.CompareTo(right.center.z);
            if (comparison != 0)
                return comparison;
            comparison = left.center.y.CompareTo(right.center.y);
            if (comparison != 0)
                return comparison;
            comparison = left.size.x.CompareTo(right.size.x);
            if (comparison != 0)
                return comparison;
            comparison = left.size.z.CompareTo(right.size.z);
            if (comparison != 0)
                return comparison;
            return left.size.y.CompareTo(right.size.y);
        }
    }
}
