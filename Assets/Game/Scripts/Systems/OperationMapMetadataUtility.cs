using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class OperationMapMetadataUtility
    {
        private const float MinimumProjectionExtent = 0.001f;

        public static bool TryResolveActiveGridConfig(
            EntityManager entityManager,
            out GridConfig grid,
            out bool hasActiveMap,
            out string error)
        {
            grid = default;
            if (!TryResolveActiveMetadata(entityManager, out BlobAssetReference<OperationMapBlob> metadata, out hasActiveMap, out error))
                return false;

            OperationMapGridBlob source = metadata.Value.Grid;
            if (!IsValidGrid(in source))
            {
                error = "Active operation-map grid metadata is invalid.";
                return false;
            }

            grid = new GridConfig
            {
                Width = source.Dimensions.x,
                Height = source.Dimensions.y,
                CellSize = source.CellSize,
                Origin = source.Origin
            };
            error = null;
            return true;
        }

        public static bool TryResolveActiveNavigationMetadata(
            EntityManager entityManager,
            out OperationMapGridBlob grid,
            out OperationMapNavigationMetadataBlob navigation,
            out bool hasActiveMap,
            out string error)
        {
            grid = default;
            navigation = default;
            if (!TryResolveActiveMetadata(entityManager, out BlobAssetReference<OperationMapBlob> metadata, out hasActiveMap, out error))
                return false;

            grid = metadata.Value.Grid;
            navigation = metadata.Value.Navigation;
            if (!IsValidGrid(in grid) || grid.AuthoredBlockedCellCount < 0)
            {
                error = "Active operation-map navigation grid metadata is invalid.";
                return false;
            }

            if (navigation.StaticGridBlockerCount < 0 ||
                navigation.UsesSurfaceMovementMetadata > 1 ||
                navigation.SupportsDynamicBlockers > 1 ||
                navigation.SupportsDynamicOccupancy > 1)
            {
                error = "Active operation-map navigation capability metadata is invalid.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryResolveActiveSurfaceMetadata(
            EntityManager entityManager,
            out OperationMapSurfaceMetadataBlob surface,
            out OperationMapGridBlob grid,
            out bool hasActiveMap,
            out string error)
        {
            surface = default;
            grid = default;
            if (!TryResolveActiveMetadata(entityManager, out BlobAssetReference<OperationMapBlob> metadata, out hasActiveMap, out error))
                return false;

            surface = metadata.Value.Surface;
            grid = metadata.Value.Grid;
            if (surface.RuntimeBlobHash.IsEmpty ||
                surface.SurfaceCount <= 0 ||
                surface.PayloadVersion <= 0 ||
                !math.isfinite(surface.MinimumHeight) ||
                !math.isfinite(surface.MaximumHeight) ||
                surface.MinimumHeight > surface.MaximumHeight)
            {
                error = "Active operation-map surface metadata is invalid.";
                return false;
            }

            if (grid.Dimensions.x <= 0 || grid.Dimensions.y <= 0 ||
                !math.isfinite(grid.CellSize) || grid.CellSize <= 0f ||
                !math.all(math.isfinite(grid.Origin)))
            {
                error = "Active operation-map surface grid metadata is invalid.";
                return false;
            }

            error = null;
            return true;
        }

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

        public static bool TryResolveActiveFactionAnchorCell(
            EntityManager entityManager,
            OperationMapAnchorKind kind,
            int factionId,
            int laneIndex,
            out int2 cell,
            out bool hasActiveMap,
            out bool hasMatchingAnchor,
            out string error)
        {
            cell = default;
            hasMatchingAnchor = false;
            if (kind != OperationMapAnchorKind.Spawn && kind != OperationMapAnchorKind.Deployment)
            {
                hasActiveMap = false;
                error = "Faction anchor lookup requires Spawn or Deployment kind.";
                return false;
            }

            if (!TryResolveActiveMetadata(entityManager, out BlobAssetReference<OperationMapBlob> metadata, out hasActiveMap, out error))
                return false;

            OperationMapGridBlob grid = metadata.Value.Grid;
            if (!IsValidGrid(in grid))
            {
                error = "Active operation-map faction anchor grid metadata is invalid.";
                return false;
            }

            int matchCount = 0;
            OperationMapAnchorBlob match = default;
            for (int index = 0; index < metadata.Value.Anchors.Length; index++)
            {
                OperationMapAnchorBlob candidate = metadata.Value.Anchors[index];
                if (candidate.Kind != kind ||
                    candidate.FactionId != factionId ||
                    candidate.LaneIndex != laneIndex)
                {
                    continue;
                }

                match = candidate;
                matchCount++;
            }

            if (matchCount == 0)
            {
                error = null;
                return false;
            }

            hasMatchingAnchor = true;
            if (matchCount != 1 || !math.all(math.isfinite(match.Position)))
            {
                error = $"Active operation map has invalid or ambiguous {kind} anchors for faction {factionId}, lane {laneIndex}.";
                return false;
            }

            GridConfig config = new()
            {
                Width = grid.Dimensions.x,
                Height = grid.Dimensions.y,
                CellSize = grid.CellSize,
                Origin = grid.Origin
            };
            cell = GridUtils.WorldToCell(in config, match.Position);
            if (!GridUtils.InBounds(cell, config.Width, config.Height))
            {
                error = $"Active operation-map {kind} anchor resolves outside the grid for faction {factionId}, lane {laneIndex}.";
                return false;
            }

            error = null;
            return true;
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

        private static bool TryResolveActiveMetadata(
            EntityManager entityManager,
            out BlobAssetReference<OperationMapBlob> metadataBlob,
            out bool hasActiveMap,
            out string error)
        {
            metadataBlob = default;
            hasActiveMap = false;

            using EntityQuery rootQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRootComponent>());
            int rootCount = rootQuery.CalculateEntityCount();
            if (rootCount == 0)
            {
                error = null;
                return false;
            }

            hasActiveMap = true;
            if (rootCount != 1)
            {
                error = $"Expected exactly one operation-map root, found {rootCount}.";
                return false;
            }

            Entity rootEntity = rootQuery.GetSingletonEntity();
            if (!entityManager.HasComponent<ActiveOperationMapComponent>(rootEntity) ||
                !entityManager.HasComponent<OperationMapMetadataComponent>(rootEntity))
            {
                error = "The operation-map root is missing active identity or metadata.";
                return false;
            }

            ActiveOperationMapComponent active =
                entityManager.GetComponentData<ActiveOperationMapComponent>(rootEntity);
            OperationMapMetadataComponent metadata =
                entityManager.GetComponentData<OperationMapMetadataComponent>(rootEntity);
            if (!metadata.Blob.IsCreated || metadata.Generation != active.Generation)
            {
                error = "Active operation-map metadata is missing or belongs to a different generation.";
                return false;
            }

            if (!metadata.Blob.Value.OperationMapId.Equals(active.OperationMapId))
            {
                error = "Active operation-map identity does not match its metadata blob.";
                return false;
            }

            metadataBlob = metadata.Blob;
            error = null;
            return true;
        }

        private static bool IsInside(float3 min, float3 max, float3 position) =>
            math.all(position >= min) && math.all(position <= max);

        private static bool IsFiniteProjection(in OperationMapMinimapBlob projection) =>
            math.all(math.isfinite(projection.ProjectionOrigin)) &&
            math.all(math.isfinite(projection.ProjectionSize)) &&
            math.isfinite(projection.OrientationDegrees) &&
            math.all(projection.ProjectionSize >= new float2(MinimumProjectionExtent));

        private static bool IsValidGrid(in OperationMapGridBlob grid) =>
            grid.Dimensions.x > 0 &&
            grid.Dimensions.y > 0 &&
            math.isfinite(grid.CellSize) &&
            grid.CellSize > 0f &&
            math.all(math.isfinite(grid.Origin));
    }
}
