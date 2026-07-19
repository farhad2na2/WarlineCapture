using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public readonly struct MatchHudMinimapProjectionGrid
    {
        public readonly Vector3 Origin;
        public readonly float Width;
        public readonly float Height;

        public MatchHudMinimapProjectionGrid(Vector3 origin, float width, float height)
        {
            Origin = origin;
            Width = Mathf.Max(0.001f, width);
            Height = Mathf.Max(0.001f, height);
        }

        public static MatchHudMinimapProjectionGrid FromGridModel(MatchHudMinimapGridModel grid)
        {
            return new MatchHudMinimapProjectionGrid(
                grid.Origin,
                grid.WorldWidth,
                grid.WorldHeight);
        }
    }

    public static class MatchHudMinimapProjectionUiSystemHelper
    {
        private const float MinLocalWindowHeight = 160f;
        private const float LocalWindowVisibleScale = 4.5f;
        private const int ViewportCornerCount = 4;

        public static bool TryWorldToNormalized(MatchHudMinimapProjectionGrid grid, Vector3 worldPosition, out Vector2 normalized)
        {
            float normalizedX = (worldPosition.x - grid.Origin.x) / grid.Width;
            float normalizedY = (worldPosition.z - grid.Origin.z) / grid.Height;
            normalized = new Vector2(normalizedX, normalizedY);
            return !float.IsNaN(normalizedX) && !float.IsNaN(normalizedY);
        }

        public static Vector3 NormalizedToWorld(MatchHudMinimapProjectionGrid grid, Vector2 normalized)
        {
            return new Vector3(
                grid.Origin.x + Mathf.Clamp01(normalized.x) * grid.Width,
                grid.Origin.y,
                grid.Origin.z + Mathf.Clamp01(normalized.y) * grid.Height);
        }

        public static Vector3 ClampWorldToGrid(MatchHudMinimapGridModel fullGridConfig, Vector3 worldPosition)
        {
            MatchHudMinimapProjectionGrid fullGrid = MatchHudMinimapProjectionGrid.FromGridModel(fullGridConfig);
            return new Vector3(
                Mathf.Clamp(worldPosition.x, fullGrid.Origin.x, fullGrid.Origin.x + fullGrid.Width),
                fullGrid.Origin.y,
                Mathf.Clamp(worldPosition.z, fullGrid.Origin.z, fullGrid.Origin.z + fullGrid.Height));
        }

        public static MatchHudMinimapProjectionGrid CreateFullGridIncludingCamera(
            MatchHudMinimapGridModel fullGridConfig,
            Camera worldCamera)
        {
            MatchHudMinimapProjectionGrid fullGrid = MatchHudMinimapProjectionGrid.FromGridModel(fullGridConfig);
            if (!TryGetCameraGroundBounds(worldCamera, fullGrid, out _, out _, out Rect cameraBounds))
                return fullGrid;

            float minX = Mathf.Min(fullGrid.Origin.x, cameraBounds.xMin);
            float minZ = Mathf.Min(fullGrid.Origin.z, cameraBounds.yMin);
            float maxX = Mathf.Max(fullGrid.Origin.x + fullGrid.Width, cameraBounds.xMax);
            float maxZ = Mathf.Max(fullGrid.Origin.z + fullGrid.Height, cameraBounds.yMax);
            return new MatchHudMinimapProjectionGrid(
                new Vector3(minX, fullGrid.Origin.y, minZ),
                maxX - minX,
                maxZ - minZ);
        }

        public static bool TryGetCameraViewportRect(Camera worldCamera, MatchHudMinimapProjectionGrid grid, out Rect normalizedRect)
        {
            normalizedRect = default;
            if (worldCamera == null)
                return false;

            Plane groundPlane = new(Vector3.up, new Vector3(0f, grid.Origin.y, 0f));
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            bool found = false;
            for (int i = 0; i < ViewportCornerCount; i++)
            {
                if (!TryRaycastViewport(worldCamera, groundPlane, GetViewportCorner(i), out Vector3 worldPoint) ||
                    !TryWorldToNormalized(grid, worldPoint, out Vector2 normalized))
                {
                    continue;
                }

                found = true;
                minX = Mathf.Min(minX, normalized.x);
                minY = Mathf.Min(minY, normalized.y);
                maxX = Mathf.Max(maxX, normalized.x);
                maxY = Mathf.Max(maxY, normalized.y);
            }

            if (!found)
                return TryGetFallbackCameraViewportRect(worldCamera, groundPlane, grid, out normalizedRect);

            float width = Mathf.Max(0f, maxX - minX);
            float height = Mathf.Max(0f, maxY - minY);
            if (TryRaycastViewport(worldCamera, groundPlane, new Vector3(0.5f, 0.5f, 0f), out Vector3 centerWorld) &&
                TryWorldToNormalized(grid, centerWorld, out Vector2 centerNormalized))
            {
                minX = centerNormalized.x - width * 0.5f;
                maxX = centerNormalized.x + width * 0.5f;
                minY = centerNormalized.y - height * 0.5f;
                maxY = centerNormalized.y + height * 0.5f;
            }

            minX = Mathf.Clamp01(minX);
            minY = Mathf.Clamp01(minY);
            maxX = Mathf.Clamp01(maxX);
            maxY = Mathf.Clamp01(maxY);
            normalizedRect = Rect.MinMaxRect(minX, minY, Mathf.Max(minX, maxX), Mathf.Max(minY, maxY));
            return normalizedRect.width > 0f && normalizedRect.height > 0f;
        }

        public static bool TryGetCameraGroundBoundsCenter(Camera worldCamera, float groundY, out Vector3 center)
        {
            center = default;
            if (worldCamera == null)
                return false;

            MatchHudMinimapProjectionGrid groundGrid = new(Vector3.up * groundY, 1f, 1f);
            if (!TryGetCameraGroundBounds(worldCamera, groundGrid, out center, out _))
                return false;

            return true;
        }

        public static bool TryGetCameraGroundCenter(Camera worldCamera, float groundY, out Vector3 center)
        {
            center = default;
            if (worldCamera == null)
                return false;

            Plane groundPlane = new(Vector3.up, new Vector3(0f, groundY, 0f));
            return TryRaycastViewport(worldCamera, groundPlane, new Vector3(0.5f, 0.5f, 0f), out center);
        }

        public static MatchHudMinimapProjectionGrid CreateCameraCenteredGrid(
            MatchHudMinimapGridModel fullGridConfig,
            Camera worldCamera,
            float mapAspect)
        {
            mapAspect = Mathf.Max(0.1f, mapAspect);
            MatchHudMinimapProjectionGrid fullGrid = MatchHudMinimapProjectionGrid.FromGridModel(fullGridConfig);
            if (TryGetCameraGroundBounds(worldCamera, fullGrid, out Vector3 boundsCenter, out Vector2 visibleSize))
            {
                Vector3 centeredOn = TryGetCameraGroundCenter(worldCamera, fullGrid.Origin.y, out Vector3 rayCenter)
                    ? rayCenter
                    : boundsCenter;
                return CreateCenteredGrid(fullGridConfig, centeredOn, visibleSize, mapAspect);
            }

            Vector3 center = ResolveCameraGroundCenter(worldCamera, fullGrid);
            visibleSize = ResolveCameraGroundSize(worldCamera, fullGrid);
            return CreateCenteredGrid(fullGridConfig, center, visibleSize, mapAspect);
        }

        public static MatchHudMinimapProjectionGrid CreateCenteredGrid(
            MatchHudMinimapGridModel fullGridConfig,
            Vector3 center,
            Vector2 visibleSize,
            float mapAspect)
        {
            MatchHudMinimapProjectionGrid fullGrid = MatchHudMinimapProjectionGrid.FromGridModel(fullGridConfig);
            mapAspect = Mathf.Max(0.1f, mapAspect);
            float localHeight = Mathf.Max(MinLocalWindowHeight, visibleSize.y * LocalWindowVisibleScale);
            float localWidth = Mathf.Max(localHeight * mapAspect, visibleSize.x * LocalWindowVisibleScale);
            localHeight = Mathf.Max(localHeight, localWidth / mapAspect);
            localWidth = Mathf.Min(localWidth, fullGrid.Width);
            localHeight = Mathf.Min(localHeight, fullGrid.Height);

            float originX = center.x - localWidth * 0.5f;
            float originZ = center.z - localHeight * 0.5f;
            return new MatchHudMinimapProjectionGrid(
                new Vector3(originX, fullGrid.Origin.y, originZ),
                localWidth,
                localHeight);
        }

        public static void ConfigureCaptureCamera(Camera captureCamera, MatchHudMinimapProjectionGrid grid, int cullingMask)
        {
            if (captureCamera == null)
                return;

            Vector3 center = grid.Origin + new Vector3(grid.Width * 0.5f, 0f, grid.Height * 0.5f);
            float captureHeight = 128f;
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = grid.Height * 0.5f;
            captureCamera.aspect = Mathf.Max(0.1f, grid.Width / Mathf.Max(0.001f, grid.Height));
            captureCamera.transform.position = center + Vector3.up * captureHeight;
            captureCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            captureCamera.nearClipPlane = 0.1f;
            captureCamera.farClipPlane = captureHeight + 256f;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = new Color(0.025f, 0.035f, 0.035f, 1f);
            captureCamera.cullingMask = cullingMask;
            captureCamera.enabled = false;
        }

        private static Vector3 ResolveCameraGroundCenter(Camera worldCamera, MatchHudMinimapProjectionGrid fullGrid)
        {
            if (worldCamera != null &&
                TryRaycastViewport(worldCamera, new Plane(Vector3.up, new Vector3(0f, fullGrid.Origin.y, 0f)), new Vector3(0.5f, 0.5f, 0f), out Vector3 center))
            {
                return center;
            }

            return fullGrid.Origin + new Vector3(fullGrid.Width * 0.5f, 0f, fullGrid.Height * 0.5f);
        }

        private static Vector2 ResolveCameraGroundSize(Camera worldCamera, MatchHudMinimapProjectionGrid fullGrid)
        {
            if (worldCamera == null)
                return new Vector2(fullGrid.Width, fullGrid.Height);

            Plane groundPlane = new(Vector3.up, new Vector3(0f, fullGrid.Origin.y, 0f));
            bool found = false;
            float minX = float.PositiveInfinity;
            float minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxZ = float.NegativeInfinity;
            for (int i = 0; i < ViewportCornerCount; i++)
            {
                if (!TryRaycastViewport(worldCamera, groundPlane, GetViewportCorner(i), out Vector3 worldPoint))
                    continue;

                found = true;
                minX = Mathf.Min(minX, worldPoint.x);
                minZ = Mathf.Min(minZ, worldPoint.z);
                maxX = Mathf.Max(maxX, worldPoint.x);
                maxZ = Mathf.Max(maxZ, worldPoint.z);
            }

            if (found)
                return new Vector2(Mathf.Max(1f, maxX - minX), Mathf.Max(1f, maxZ - minZ));

            if (worldCamera.orthographic)
                return new Vector2(worldCamera.orthographicSize * 2f * worldCamera.aspect, worldCamera.orthographicSize * 2f);

            float fallback = Mathf.Max(MinLocalWindowHeight, worldCamera.transform.position.y * 2f);
            return new Vector2(fallback * Mathf.Max(0.1f, worldCamera.aspect), fallback);
        }

        private static bool TryGetCameraGroundBounds(
            Camera worldCamera,
            MatchHudMinimapProjectionGrid fullGrid,
            out Vector3 center,
            out Vector2 size)
        {
            return TryGetCameraGroundBounds(worldCamera, fullGrid, out center, out size, out _);
        }

        private static bool TryGetCameraGroundBounds(
            Camera worldCamera,
            MatchHudMinimapProjectionGrid fullGrid,
            out Vector3 center,
            out Vector2 size,
            out Rect bounds)
        {
            center = default;
            size = default;
            bounds = default;
            if (worldCamera == null)
                return false;

            Plane groundPlane = new(Vector3.up, new Vector3(0f, fullGrid.Origin.y, 0f));
            bool found = false;
            float minX = float.PositiveInfinity;
            float minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxZ = float.NegativeInfinity;
            for (int i = 0; i < ViewportCornerCount; i++)
            {
                if (!TryRaycastViewport(worldCamera, groundPlane, GetViewportCorner(i), out Vector3 worldPoint))
                    continue;

                found = true;
                minX = Mathf.Min(minX, worldPoint.x);
                minZ = Mathf.Min(minZ, worldPoint.z);
                maxX = Mathf.Max(maxX, worldPoint.x);
                maxZ = Mathf.Max(maxZ, worldPoint.z);
            }

            if (!found)
                return false;

            center = new Vector3((minX + maxX) * 0.5f, fullGrid.Origin.y, (minZ + maxZ) * 0.5f);
            size = new Vector2(Mathf.Max(1f, maxX - minX), Mathf.Max(1f, maxZ - minZ));
            bounds = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
            return true;
        }

        private static bool TryGetFallbackCameraViewportRect(Camera worldCamera, Plane groundPlane, MatchHudMinimapProjectionGrid grid, out Rect normalizedRect)
        {
            normalizedRect = default;
            if (!TryRaycastViewport(worldCamera, groundPlane, new Vector3(0.5f, 0.5f, 0f), out Vector3 center) ||
                !TryWorldToNormalized(grid, center, out Vector2 normalizedCenter))
            {
                return false;
            }

            float normalizedHeight = Mathf.Clamp01((worldCamera.orthographicSize * 2f) / grid.Height);
            float normalizedWidth = Mathf.Clamp01((worldCamera.orthographicSize * 2f * worldCamera.aspect) / grid.Width);
            normalizedRect = Rect.MinMaxRect(
                Mathf.Clamp01(normalizedCenter.x - normalizedWidth * 0.5f),
                Mathf.Clamp01(normalizedCenter.y - normalizedHeight * 0.5f),
                Mathf.Clamp01(normalizedCenter.x + normalizedWidth * 0.5f),
                Mathf.Clamp01(normalizedCenter.y + normalizedHeight * 0.5f));
            return normalizedRect.width > 0f && normalizedRect.height > 0f;
        }

        private static Vector3 GetViewportCorner(int index)
        {
            return index switch
            {
                0 => new Vector3(0f, 0f, 0f),
                1 => new Vector3(1f, 0f, 0f),
                2 => new Vector3(1f, 1f, 0f),
                _ => new Vector3(0f, 1f, 0f)
            };
        }

        private static bool TryRaycastViewport(Camera camera, Plane groundPlane, Vector3 viewportPoint, out Vector3 point)
        {
            point = default;
            Ray ray = camera.ViewportPointToRay(viewportPoint);
            if (!groundPlane.Raycast(ray, out float distance))
                return false;

            point = ray.GetPoint(distance);
            return !float.IsNaN(point.x) && !float.IsNaN(point.z);
        }
    }
}
