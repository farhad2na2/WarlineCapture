using Unity.Entities;
using UnityEngine;

public sealed partial class RtsCameraSystem : SystemBase
{
    private const float SmoothFocusCompletionDistanceSq = 0.01f;
    private const float FallbackDeltaTime = 1f / 60f;

    private Vector3 _smoothFocusVelocity;
    private Vector3 _tacticalFollowPositionVelocity;
    private float _zoomTransitionVelocity;
    private float _pitchTransitionVelocity;
    private float _yawTransitionVelocity;
    private float _fieldOfViewTransitionVelocity;
    private float _orthographicSizeTransitionVelocity;
    private bool _hasGroundBoundary;
    private Rect _groundBoundary;

    public bool IsDragging { get; private set; }
    public bool HasSmoothFocusTarget { get; private set; }
    public Vector3 SmoothFocusTarget { get; private set; }
    public bool WasPlayRequested { get; set; }
    public bool WasBuildModeActive { get; set; }
    public bool IsZoomTransitionActive { get; set; }
    public float FullscreenIsoTargetHeight { get; set; }
    public float FullscreenIsoTargetOrthographicSize { get; set; }
    public bool NormalIsoModeActive { get; set; }
    public bool HasGroundBoundary => _hasGroundBoundary;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void SetDragging(bool isDragging)
    {
        IsDragging = isDragging;
    }

    public void ClearDragging()
    {
        IsDragging = false;
    }

    public void ResetSession()
    {
        ClearDragging();
        ClearSmoothFocusTarget();
    }

    public void ResetCameraModeSession()
    {
        WasPlayRequested = false;
        WasBuildModeActive = false;
        IsZoomTransitionActive = false;
        NormalIsoModeActive = false;
        ResetTransitionVelocities();
    }

    public void BeginZoomTransition(bool buildModeActive)
    {
        WasBuildModeActive = buildModeActive;
        IsZoomTransitionActive = true;
        _zoomTransitionVelocity = 0f;
    }

    public void CompleteZoomTransition()
    {
        IsZoomTransitionActive = false;
        ResetPerspectiveTransitionVelocities();
    }

    public void ResetTransitionVelocities()
    {
        ResetPerspectiveTransitionVelocities();
        _tacticalFollowPositionVelocity = Vector3.zero;
        _orthographicSizeTransitionVelocity = 0f;
    }

    private void ResetPerspectiveTransitionVelocities()
    {
        _zoomTransitionVelocity = 0f;
        _pitchTransitionVelocity = 0f;
        _yawTransitionVelocity = 0f;
        _fieldOfViewTransitionVelocity = 0f;
    }

    public void SetGroundBoundary(Rect boundary)
    {
        if (boundary.width <= 0.01f || boundary.height <= 0.01f)
        {
            ClearGroundBoundary();
            return;
        }

        _groundBoundary = boundary;
        _hasGroundBoundary = true;
    }

    public void ClearGroundBoundary()
    {
        _hasGroundBoundary = false;
        _groundBoundary = default;
    }

    public bool TryGetGroundBoundary(out Rect boundary)
    {
        boundary = _groundBoundary;
        return _hasGroundBoundary;
    }

    public void SetSmoothFocusTarget(Vector3 focusWorldPosition, bool resetVelocity)
    {
        focusWorldPosition.y = 0f;
        focusWorldPosition = ClampGroundPositionToBoundary(focusWorldPosition);
        SmoothFocusTarget = focusWorldPosition;
        HasSmoothFocusTarget = true;

        if (resetVelocity)
            _smoothFocusVelocity = Vector3.zero;
    }

    public void ClearSmoothFocusTarget()
    {
        HasSmoothFocusTarget = false;
        _smoothFocusVelocity = Vector3.zero;
    }

    public Vector3 UpdateSmoothFocus(Vector3 currentGroundCenter, float smoothTime)
    {
        if (!HasSmoothFocusTarget)
            return currentGroundCenter;

        Vector3 smoothedCenter = Vector3.SmoothDamp(
            currentGroundCenter,
            SmoothFocusTarget,
            ref _smoothFocusVelocity,
            Mathf.Max(0.01f, smoothTime));

        Vector2 remaining = new(
            SmoothFocusTarget.x - smoothedCenter.x,
            SmoothFocusTarget.z - smoothedCenter.z);
        if (remaining.sqrMagnitude > SmoothFocusCompletionDistanceSq)
            return smoothedCenter;

        smoothedCenter = SmoothFocusTarget;
        ClearSmoothFocusTarget();
        return smoothedCenter;
    }

    public bool PanCamera(Camera worldCamera, Vector2 screenDelta, float panSensitivity)
    {
        if (worldCamera == null)
            return false;

        Vector3 flatRight = worldCamera.transform.right;
        flatRight.y = 0f;
        flatRight.Normalize();

        Vector3 flatForward = worldCamera.transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 worldDelta =
            (-flatRight * screenDelta.x + -flatForward * screenDelta.y) * panSensitivity;

        worldCamera.transform.position += worldDelta;
        ClampCameraToGroundBoundary(worldCamera);
        return true;
    }

    public void UpdatePerspectiveZoom(
        Camera worldCamera,
        float zoomDirection,
        float zoomSpeed,
        float deltaTime,
        float minZoomHeight,
        float maxZoomHeight)
    {
        if (worldCamera == null || Mathf.Approximately(zoomDirection, 0f))
            return;

        Vector3 zoomDelta = worldCamera.transform.forward * (zoomDirection * zoomSpeed * deltaTime);
        Vector3 currentPosition = worldCamera.transform.position;
        Vector3 targetPosition = currentPosition + zoomDelta;

        float clampedHeight = Mathf.Clamp(targetPosition.y, minZoomHeight, maxZoomHeight);
        if (!Mathf.Approximately(targetPosition.y, currentPosition.y))
        {
            float t = (clampedHeight - currentPosition.y) / (targetPosition.y - currentPosition.y);
            targetPosition = currentPosition + (zoomDelta * t);
        }
        else
        {
            targetPosition.y = clampedHeight;
        }

        worldCamera.transform.position = targetPosition;
        ClampCameraToGroundBoundary(worldCamera);
    }

    public void UpdateFullscreenIsoZoom(
        float zoomDirection,
        float zoomSpeed,
        float deltaTime,
        float minZoomHeight,
        float maxZoomHeight)
    {
        if (Mathf.Approximately(zoomDirection, 0f))
            return;

        FullscreenIsoTargetHeight = Mathf.Clamp(
            FullscreenIsoTargetHeight - (zoomDirection * zoomSpeed * deltaTime),
            minZoomHeight,
            maxZoomHeight);
        FullscreenIsoTargetOrthographicSize = Mathf.Clamp(
            FullscreenIsoTargetOrthographicSize - (zoomDirection * (zoomSpeed * 0.6f) * deltaTime),
            8f,
            48f);
    }

    public void MoveCameraGroundCenterTo(Camera worldCamera, Vector3 focusWorldPosition)
    {
        if (worldCamera == null)
            return;

        Vector3 currentGroundCenter = GetCameraGroundCenterWorld(worldCamera);
        Vector3 position = worldCamera.transform.position;
        position.x += focusWorldPosition.x - currentGroundCenter.x;
        position.z += focusWorldPosition.z - currentGroundCenter.z;
        worldCamera.transform.position = position;
        ClampCameraToGroundBoundary(worldCamera);
    }

    public Vector3 GetCameraGroundCenterWorld(Camera worldCamera)
    {
        if (worldCamera == null)
            return Vector3.zero;

        Plane groundPlane = new(Vector3.up, Vector3.zero);
        Ray ray = worldCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return groundPlane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : worldCamera.transform.position;
    }

    public void ApplyPerspectiveCameraModeInstant(Camera worldCamera, float height, float pitch, float yaw, float fieldOfView)
    {
        if (worldCamera == null)
            return;

        worldCamera.orthographic = false;
        Vector3 position = worldCamera.transform.position;
        position.y = height;
        worldCamera.transform.position = position;
        worldCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        worldCamera.fieldOfView = fieldOfView;
        ClampCameraToGroundBoundary(worldCamera);
    }

    public void ApplyFullscreenIsoCameraModeInstant(Camera worldCamera, float height, float orthographicSize, float pitch, float yaw)
    {
        if (worldCamera == null)
            return;

        worldCamera.orthographic = true;
        Vector3 position = worldCamera.transform.position;
        position.y = height;
        worldCamera.transform.position = position;
        worldCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        worldCamera.orthographicSize = orthographicSize;
        ClampCameraToGroundBoundary(worldCamera);
    }

    public float GetVisibleGroundVerticalSpan(Camera worldCamera)
    {
        if (worldCamera == null)
            return 0f;

        if (!TryGetGroundPointFromViewport(worldCamera, new Vector2(0.5f, 0f), out Vector3 topPoint) ||
            !TryGetGroundPointFromViewport(worldCamera, new Vector2(0.5f, 1f), out Vector3 bottomPoint))
            return 0f;

        return Vector3.Distance(topPoint, bottomPoint);
    }

    public bool TryGetGroundPointFromViewport(Camera worldCamera, Vector2 viewport, out Vector3 point)
    {
        point = Vector3.zero;
        if (worldCamera == null)
            return false;

        Plane groundPlane = new(Vector3.up, Vector3.zero);
        Ray ray = worldCamera.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
        if (!groundPlane.Raycast(ray, out float distance))
            return false;

        point = ray.GetPoint(distance);
        return true;
    }

    public bool TryGetCameraGroundBounds(Camera worldCamera, out Rect bounds)
    {
        bounds = default;
        if (worldCamera == null)
            return false;

        if (!TryGetGroundPointFromViewport(worldCamera, new Vector2(0f, 0f), out Vector3 bottomLeft) ||
            !TryGetGroundPointFromViewport(worldCamera, new Vector2(1f, 0f), out Vector3 bottomRight) ||
            !TryGetGroundPointFromViewport(worldCamera, new Vector2(0f, 1f), out Vector3 topLeft) ||
            !TryGetGroundPointFromViewport(worldCamera, new Vector2(1f, 1f), out Vector3 topRight))
            return false;

        float minX = Mathf.Min(bottomLeft.x, bottomRight.x, topLeft.x, topRight.x);
        float maxX = Mathf.Max(bottomLeft.x, bottomRight.x, topLeft.x, topRight.x);
        float minZ = Mathf.Min(bottomLeft.z, bottomRight.z, topLeft.z, topRight.z);
        float maxZ = Mathf.Max(bottomLeft.z, bottomRight.z, topLeft.z, topRight.z);
        bounds = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        return true;
    }

    public float CalculateOrthographicSizeForGroundSpan(
        Camera worldCamera,
        float targetGroundSpan,
        float height,
        float pitch,
        float yaw,
        float fallbackOrthographicSize)
    {
        if (worldCamera == null || targetGroundSpan <= 0.01f)
            return fallbackOrthographicSize;

        bool originalOrthographic = worldCamera.orthographic;
        Vector3 originalPosition = worldCamera.transform.position;
        Quaternion originalRotation = worldCamera.transform.rotation;
        float originalFieldOfView = worldCamera.fieldOfView;
        float originalOrthographicSize = worldCamera.orthographicSize;

        try
        {
            ApplyFullscreenIsoCameraModeInstant(worldCamera, height, 1f, pitch, yaw);
            float spanAtUnitSize = GetVisibleGroundVerticalSpan(worldCamera);
            if (spanAtUnitSize <= 0.01f)
                return fallbackOrthographicSize;

            return targetGroundSpan / spanAtUnitSize;
        }
        finally
        {
            worldCamera.orthographic = originalOrthographic;
            worldCamera.transform.position = originalPosition;
            worldCamera.transform.rotation = originalRotation;
            worldCamera.fieldOfView = originalFieldOfView;
            worldCamera.orthographicSize = originalOrthographicSize;
        }
    }

    public float CalculatePerspectiveHeightForGroundSpan(
        Camera worldCamera,
        float targetGroundSpan,
        float pitch,
        float yaw,
        float fieldOfView,
        float minZoomHeight,
        float maxZoomHeight,
        float fallbackZoomHeight)
    {
        if (worldCamera == null || targetGroundSpan <= 0.01f)
            return fallbackZoomHeight;

        bool originalOrthographic = worldCamera.orthographic;
        Vector3 originalPosition = worldCamera.transform.position;
        Quaternion originalRotation = worldCamera.transform.rotation;
        float originalFieldOfView = worldCamera.fieldOfView;
        float originalOrthographicSize = worldCamera.orthographicSize;

        try
        {
            float low = minZoomHeight;
            float high = maxZoomHeight;

            for (int i = 0; i < 18; i++)
            {
                float mid = (low + high) * 0.5f;
                ApplyPerspectiveCameraModeInstant(worldCamera, mid, pitch, yaw, fieldOfView);
                float span = GetVisibleGroundVerticalSpan(worldCamera);
                if (span < targetGroundSpan)
                    low = mid;
                else
                    high = mid;
            }

            return (low + high) * 0.5f;
        }
        finally
        {
            worldCamera.orthographic = originalOrthographic;
            worldCamera.transform.position = originalPosition;
            worldCamera.transform.rotation = originalRotation;
            worldCamera.fieldOfView = originalFieldOfView;
            worldCamera.orthographicSize = originalOrthographicSize;
        }
    }

    public bool UpdatePerspectiveCameraMode(
        Camera worldCamera,
        float targetHeight,
        float targetPitch,
        float targetYaw,
        float targetFieldOfView,
        float smoothTime)
    {
        if (worldCamera == null)
            return true;

        if (worldCamera.orthographic)
            worldCamera.orthographic = false;

        float newHeight = Mathf.SmoothDamp(
            worldCamera.transform.position.y,
            targetHeight,
            ref _zoomTransitionVelocity,
            smoothTime);

        Vector3 position = worldCamera.transform.position;
        position.y = newHeight;
        worldCamera.transform.position = position;

        Vector3 euler = worldCamera.transform.rotation.eulerAngles;
        float newPitch = Mathf.SmoothDampAngle(euler.x, targetPitch, ref _pitchTransitionVelocity, smoothTime);
        float newYaw = Mathf.SmoothDampAngle(euler.y, targetYaw, ref _yawTransitionVelocity, smoothTime);
        worldCamera.transform.rotation = Quaternion.Euler(newPitch, newYaw, 0f);

        worldCamera.fieldOfView = Mathf.SmoothDamp(
            worldCamera.fieldOfView,
            targetFieldOfView,
            ref _fieldOfViewTransitionVelocity,
            smoothTime);

        ClampCameraToGroundBoundary(worldCamera);

        return Mathf.Abs(newHeight - targetHeight) <= 0.05f &&
               Mathf.Abs(Mathf.DeltaAngle(newPitch, targetPitch)) <= 0.1f &&
               Mathf.Abs(Mathf.DeltaAngle(newYaw, targetYaw)) <= 0.1f &&
               Mathf.Abs(worldCamera.fieldOfView - targetFieldOfView) <= 0.05f;
    }

    public bool UpdateFullscreenIsoCameraMode(
        Camera worldCamera,
        float targetHeight,
        float targetOrthographicSize,
        float targetPitch,
        float targetYaw,
        float smoothTime)
    {
        if (worldCamera == null)
            return true;

        if (!worldCamera.orthographic)
            worldCamera.orthographic = true;

        float newHeight = Mathf.SmoothDamp(
            worldCamera.transform.position.y,
            targetHeight,
            ref _zoomTransitionVelocity,
            smoothTime);

        Vector3 position = worldCamera.transform.position;
        position.y = newHeight;
        worldCamera.transform.position = position;

        Vector3 euler = worldCamera.transform.rotation.eulerAngles;
        float newPitch = Mathf.SmoothDampAngle(euler.x, targetPitch, ref _pitchTransitionVelocity, smoothTime);
        float newYaw = Mathf.SmoothDampAngle(euler.y, targetYaw, ref _yawTransitionVelocity, smoothTime);
        worldCamera.transform.rotation = Quaternion.Euler(newPitch, newYaw, 0f);

        worldCamera.orthographicSize = Mathf.SmoothDamp(
            worldCamera.orthographicSize,
            targetOrthographicSize,
            ref _orthographicSizeTransitionVelocity,
            smoothTime);

        ClampCameraToGroundBoundary(worldCamera);

        return Mathf.Abs(newHeight - targetHeight) <= 0.05f &&
               Mathf.Abs(Mathf.DeltaAngle(newPitch, targetPitch)) <= 0.1f &&
               Mathf.Abs(Mathf.DeltaAngle(newYaw, targetYaw)) <= 0.1f &&
               Mathf.Abs(worldCamera.orthographicSize - targetOrthographicSize) <= 0.05f;
    }

    public bool UpdateTacticalFollowPose(
        Camera worldCamera,
        Vector3 desiredPosition,
        Vector3 lookAt,
        float targetFieldOfView,
        float smoothTime,
        bool targetOrthographic = false,
        float targetOrthographicSize = 0f)
    {
        if (worldCamera == null)
            return true;

        if (worldCamera.orthographic != targetOrthographic)
            worldCamera.orthographic = targetOrthographic;

        float resolvedSmoothTime = Mathf.Max(0f, smoothTime);
        float resolvedDeltaTime = UnityEngine.Time.deltaTime > 0f ? UnityEngine.Time.deltaTime : FallbackDeltaTime;
        if (resolvedSmoothTime <= 0.0001f)
        {
            worldCamera.transform.position = desiredPosition;
            ApplyTacticalFollowRotation(worldCamera, desiredPosition, lookAt, 1f);
            if (targetOrthographic)
                worldCamera.orthographicSize = Mathf.Max(0.1f, targetOrthographicSize);
            else
                worldCamera.fieldOfView = Mathf.Max(1f, targetFieldOfView);
            ClampCameraToGroundBoundary(worldCamera);
            return true;
        }

        worldCamera.transform.position = Vector3.SmoothDamp(
            worldCamera.transform.position,
            desiredPosition,
            ref _tacticalFollowPositionVelocity,
            resolvedSmoothTime,
            Mathf.Infinity,
            resolvedDeltaTime);

        ApplyTacticalFollowRotation(
            worldCamera,
            worldCamera.transform.position,
            lookAt,
            1f - Mathf.Exp(-resolvedDeltaTime / resolvedSmoothTime));

        if (targetOrthographic)
        {
            worldCamera.orthographicSize = Mathf.SmoothDamp(
                worldCamera.orthographicSize,
                Mathf.Max(0.1f, targetOrthographicSize),
                ref _orthographicSizeTransitionVelocity,
                resolvedSmoothTime,
                Mathf.Infinity,
                resolvedDeltaTime);
        }
        else
        {
            worldCamera.fieldOfView = Mathf.SmoothDamp(
                worldCamera.fieldOfView,
                Mathf.Max(1f, targetFieldOfView),
                ref _fieldOfViewTransitionVelocity,
                resolvedSmoothTime,
                Mathf.Infinity,
                resolvedDeltaTime);
        }

        bool zoomReached = targetOrthographic
            ? Mathf.Abs(worldCamera.orthographicSize - targetOrthographicSize) <= 0.05f
            : Mathf.Abs(worldCamera.fieldOfView - targetFieldOfView) <= 0.05f;
        ClampCameraToGroundBoundary(worldCamera);
        return Vector3.Distance(worldCamera.transform.position, desiredPosition) <= 0.05f &&
               zoomReached;
    }

    public void ClampCameraToGroundBoundary(Camera worldCamera)
    {
        if (!_hasGroundBoundary || worldCamera == null)
            return;

        FitCameraFootprintToGroundBoundary(worldCamera);

        if (!TryGetCameraGroundBounds(worldCamera, out Rect footprint))
            return;

        Vector3 offset = Vector3.zero;
        if (footprint.width <= _groundBoundary.width)
        {
            if (footprint.xMin < _groundBoundary.xMin)
                offset.x = _groundBoundary.xMin - footprint.xMin;
            else if (footprint.xMax > _groundBoundary.xMax)
                offset.x = _groundBoundary.xMax - footprint.xMax;
        }
        else
        {
            offset.x = _groundBoundary.center.x - footprint.center.x;
        }

        if (footprint.height <= _groundBoundary.height)
        {
            if (footprint.yMin < _groundBoundary.yMin)
                offset.z = _groundBoundary.yMin - footprint.yMin;
            else if (footprint.yMax > _groundBoundary.yMax)
                offset.z = _groundBoundary.yMax - footprint.yMax;
        }
        else
        {
            offset.z = _groundBoundary.center.y - footprint.center.y;
        }

        if (offset.sqrMagnitude > 0.000001f)
            worldCamera.transform.position += offset;
    }

    private void FitCameraFootprintToGroundBoundary(Camera worldCamera)
    {
        for (int i = 0; i < 8; i++)
        {
            if (!TryGetCameraGroundBounds(worldCamera, out Rect footprint))
                return;

            float scaleX = footprint.width > _groundBoundary.width && footprint.width > 0.01f
                ? _groundBoundary.width / footprint.width
                : 1f;
            float scaleZ = footprint.height > _groundBoundary.height && footprint.height > 0.01f
                ? _groundBoundary.height / footprint.height
                : 1f;
            float scale = Mathf.Min(scaleX, scaleZ);
            if (scale >= 0.999f)
                return;

            if (worldCamera.orthographic)
            {
                worldCamera.orthographicSize = Mathf.Max(0.1f, worldCamera.orthographicSize * scale * 0.995f);
            }
            else
            {
                Vector3 position = worldCamera.transform.position;
                position.y = Mathf.Max(0.1f, position.y * scale * 0.995f);
                worldCamera.transform.position = position;
            }
        }
    }

    private Vector3 ClampGroundPositionToBoundary(Vector3 position)
    {
        if (!_hasGroundBoundary)
            return position;

        position.x = Mathf.Clamp(position.x, _groundBoundary.xMin, _groundBoundary.xMax);
        position.z = Mathf.Clamp(position.z, _groundBoundary.yMin, _groundBoundary.yMax);
        return position;
    }

    private static void ApplyTacticalFollowRotation(Camera worldCamera, Vector3 position, Vector3 lookAt, float t)
    {
        Vector3 lookDirection = lookAt - position;
        if (lookDirection.sqrMagnitude <= 0.0001f)
            return;

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        worldCamera.transform.rotation = Quaternion.Slerp(
            worldCamera.transform.rotation,
            desiredRotation,
            Mathf.Clamp01(t));
    }
}
