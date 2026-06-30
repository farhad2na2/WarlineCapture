using Unity.Entities;
using UnityEngine;

public sealed partial class RtsCameraSystem : SystemBase
{
    private const float SmoothFocusCompletionDistanceSq = 0.01f;

    private Vector3 _smoothFocusVelocity;
    private Vector3 _tacticalFollowPositionVelocity;
    private float _zoomTransitionVelocity;
    private float _pitchTransitionVelocity;
    private float _yawTransitionVelocity;
    private float _fieldOfViewTransitionVelocity;
    private float _orthographicSizeTransitionVelocity;

    public bool IsDragging { get; private set; }
    public bool HasSmoothFocusTarget { get; private set; }
    public Vector3 SmoothFocusTarget { get; private set; }
    public bool WasPlayRequested { get; set; }
    public bool WasBuildModeActive { get; set; }
    public bool IsZoomTransitionActive { get; set; }
    public float FullscreenIsoTargetHeight { get; set; }
    public float FullscreenIsoTargetOrthographicSize { get; set; }
    public bool NormalIsoModeActive { get; set; }

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

    public void SetSmoothFocusTarget(Vector3 focusWorldPosition, bool resetVelocity)
    {
        focusWorldPosition.y = 0f;
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
        if (resolvedSmoothTime <= 0.0001f)
        {
            worldCamera.transform.position = desiredPosition;
            ApplyTacticalFollowRotation(worldCamera, desiredPosition, lookAt, 1f);
            if (targetOrthographic)
                worldCamera.orthographicSize = Mathf.Max(0.1f, targetOrthographicSize);
            else
                worldCamera.fieldOfView = Mathf.Max(1f, targetFieldOfView);
            return true;
        }

        worldCamera.transform.position = Vector3.SmoothDamp(
            worldCamera.transform.position,
            desiredPosition,
            ref _tacticalFollowPositionVelocity,
            resolvedSmoothTime);

        ApplyTacticalFollowRotation(
            worldCamera,
            worldCamera.transform.position,
            lookAt,
            1f - Mathf.Exp(-UnityEngine.Time.deltaTime / resolvedSmoothTime));

        if (targetOrthographic)
        {
            worldCamera.orthographicSize = Mathf.SmoothDamp(
                worldCamera.orthographicSize,
                Mathf.Max(0.1f, targetOrthographicSize),
                ref _orthographicSizeTransitionVelocity,
                resolvedSmoothTime);
        }
        else
        {
            worldCamera.fieldOfView = Mathf.SmoothDamp(
                worldCamera.fieldOfView,
                Mathf.Max(1f, targetFieldOfView),
                ref _fieldOfViewTransitionVelocity,
                resolvedSmoothTime);
        }

        bool zoomReached = targetOrthographic
            ? Mathf.Abs(worldCamera.orthographicSize - targetOrthographicSize) <= 0.05f
            : Mathf.Abs(worldCamera.fieldOfView - targetFieldOfView) <= 0.05f;
        return Vector3.Distance(worldCamera.transform.position, desiredPosition) <= 0.05f &&
               zoomReached;
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
