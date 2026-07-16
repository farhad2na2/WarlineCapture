using Game.Components;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class OperationMapMetadataUtility
    {
        private const float MinimumProjectionExtent = 0.001f;

        public static bool TryFindAnchor(
            ref OperationMapBlob metadata,
            in FixedString64Bytes anchorId,
            out OperationMapAnchorBlob anchor)
        {
            for (int index = 0; index < metadata.Anchors.Length; index++)
            {
                OperationMapAnchorBlob candidate = metadata.Anchors[index];
                if (candidate.Id.Equals(anchorId))
                {
                    anchor = candidate;
                    return true;
                }
            }

            anchor = default;
            return false;
        }

        public static bool TryFindCamera(
            ref OperationMapBlob metadata,
            in FixedString64Bytes cameraId,
            out OperationMapCameraBlob camera)
        {
            for (int index = 0; index < metadata.Cameras.Length; index++)
            {
                OperationMapCameraBlob candidate = metadata.Cameras[index];
                if (candidate.Id.Equals(cameraId))
                {
                    camera = candidate;
                    return true;
                }
            }

            camera = default;
            return false;
        }

        public static bool IsInsideWorldBounds(in OperationMapBoundsComponent bounds, float3 position) =>
            IsInside(bounds.WorldMin, bounds.WorldMax, position);

        public static bool IsInsidePlayableBounds(in OperationMapBoundsComponent bounds, float3 position) =>
            IsInside(bounds.PlayableMin, bounds.PlayableMax, position);

        public static bool IsInsideCameraBounds(in OperationMapBoundsComponent bounds, float3 position) =>
            IsInside(bounds.CameraMin, bounds.CameraMax, position);

        public static float3 ClampToWorldBounds(in OperationMapBoundsComponent bounds, float3 position) =>
            math.clamp(position, bounds.WorldMin, bounds.WorldMax);

        public static float3 ClampToPlayableBounds(in OperationMapBoundsComponent bounds, float3 position) =>
            math.clamp(position, bounds.PlayableMin, bounds.PlayableMax);

        public static float3 ClampToCameraBounds(in OperationMapBoundsComponent bounds, float3 position) =>
            math.clamp(position, bounds.CameraMin, bounds.CameraMax);

        public static bool TryWorldToMinimapNormalized(
            in OperationMapMinimapBlob projection,
            float3 worldPosition,
            out float2 normalized)
        {
            if (!IsFiniteProjection(projection))
            {
                normalized = default;
                return false;
            }

            float angleRadians = math.radians(projection.OrientationDegrees);
            math.sincos(angleRadians, out float sine, out float cosine);
            float2 worldDelta = worldPosition.xz - projection.ProjectionOrigin.xz;
            float2 local = new(
                worldDelta.x * cosine + worldDelta.y * sine,
                -worldDelta.x * sine + worldDelta.y * cosine);
            normalized = local / projection.ProjectionSize;
            return math.all(math.isfinite(normalized));
        }

        public static bool TryMinimapNormalizedToWorldClamped(
            in OperationMapMinimapBlob projection,
            float2 normalized,
            float worldY,
            out float3 worldPosition)
        {
            if (!IsFiniteProjection(projection) ||
                !math.all(math.isfinite(normalized)) ||
                !math.isfinite(worldY))
            {
                worldPosition = default;
                return false;
            }

            float2 local = math.saturate(normalized) * projection.ProjectionSize;
            float angleRadians = math.radians(projection.OrientationDegrees);
            math.sincos(angleRadians, out float sine, out float cosine);
            float2 worldDelta = new(
                local.x * cosine - local.y * sine,
                local.x * sine + local.y * cosine);
            worldPosition = new float3(
                projection.ProjectionOrigin.x + worldDelta.x,
                worldY,
                projection.ProjectionOrigin.z + worldDelta.y);
            return math.all(math.isfinite(worldPosition));
        }

        public static bool IsInsideNormalizedProjection(float2 normalized) =>
            math.all(normalized >= float2.zero) && math.all(normalized <= new float2(1f));

        private static bool IsInside(float3 min, float3 max, float3 position) =>
            math.all(position >= min) && math.all(position <= max);

        private static bool IsFiniteProjection(in OperationMapMinimapBlob projection) =>
            math.all(math.isfinite(projection.ProjectionOrigin)) &&
            math.all(math.isfinite(projection.ProjectionSize)) &&
            math.isfinite(projection.OrientationDegrees) &&
            math.all(projection.ProjectionSize >= new float2(MinimumProjectionExtent));
    }
}
