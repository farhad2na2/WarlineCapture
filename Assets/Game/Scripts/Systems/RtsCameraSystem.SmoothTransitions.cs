using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class RtsCameraSystem
    {
        private const float SmoothFocusCompletionDistanceSq = 0.01f;

        private Vector3 _smoothFocusVelocity;
        private float _smoothFocusTimeSeconds;
        private float _smoothPerspectiveHeight;
        private float _smoothPerspectivePitch;
        private float _smoothPerspectiveYaw;
        private float _smoothPerspectiveFieldOfView;
        private float _smoothPerspectiveTimeSeconds;

        public bool HasSmoothFocusTarget { get; private set; }
        public Vector3 SmoothFocusTarget { get; private set; }
        public bool HasSmoothPerspectiveTarget { get; private set; }

        public void SetSmoothFocusTarget(
            Vector3 focusWorldPosition,
            bool resetVelocity,
            float smoothTimeSeconds = 0f)
        {
            focusWorldPosition.y = 0f;
            SmoothFocusTarget = ClampGroundPositionToBoundary(focusWorldPosition);
            HasSmoothFocusTarget = true;
            _smoothFocusTimeSeconds = Mathf.Max(0f, smoothTimeSeconds);

            if (resetVelocity)
                _smoothFocusVelocity = Vector3.zero;
        }

        public void ClearSmoothFocusTarget()
        {
            HasSmoothFocusTarget = false;
            _smoothFocusVelocity = Vector3.zero;
            _smoothFocusTimeSeconds = 0f;
        }

        public Vector3 UpdateSmoothFocus(Vector3 currentGroundCenter, float smoothTime, Camera worldCamera = null)
        {
            if (!HasSmoothFocusTarget)
                return currentGroundCenter;

            // A point can be inside the map while its camera viewport extends outside it.
            // Settle against the reachable center, otherwise boundary clamping fights smoothing forever.
            Vector3 reachableTarget = ClampFocusToCameraFootprint(worldCamera, SmoothFocusTarget);
            Vector3 smoothedCenter = Vector3.SmoothDamp(
                currentGroundCenter,
                reachableTarget,
                ref _smoothFocusVelocity,
                Mathf.Max(0.01f,
                    _smoothFocusTimeSeconds > 0f ? _smoothFocusTimeSeconds : smoothTime));

            Vector2 remaining = new(
                reachableTarget.x - smoothedCenter.x,
                reachableTarget.z - smoothedCenter.z);
            if (remaining.sqrMagnitude > SmoothFocusCompletionDistanceSq ||
                (worldCamera != null && HasSmoothPerspectiveTarget))
                return smoothedCenter;

            smoothedCenter = reachableTarget;
            ClearSmoothFocusTarget();
            return smoothedCenter;
        }

        internal Vector3 ClampFocusToCameraFootprint(Camera worldCamera, Vector3 target)
        {
            if (!_hasGroundBoundary || worldCamera == null || !TryGetCameraGroundBounds(worldCamera, out Rect footprint))
                return target;
            Vector3 center = GetCameraGroundCenterWorld(worldCamera);
            float minX = _groundBoundary.xMin + center.x - footprint.xMin;
            float maxX = _groundBoundary.xMax + center.x - footprint.xMax;
            float minZ = _groundBoundary.yMin + center.z - footprint.yMin;
            float maxZ = _groundBoundary.yMax + center.z - footprint.yMax;
            target.x = minX <= maxX ? Mathf.Clamp(target.x, minX, maxX) : (minX + maxX) * .5f;
            target.z = minZ <= maxZ ? Mathf.Clamp(target.z, minZ, maxZ) : (minZ + maxZ) * .5f;
            return target;
        }

        public void SetSmoothPerspectiveTarget(
            float height,
            float pitch,
            float yaw,
            float fieldOfView,
            float smoothTimeSeconds,
            bool resetVelocity)
        {
            _smoothPerspectiveHeight = height;
            _smoothPerspectivePitch = pitch;
            _smoothPerspectiveYaw = yaw;
            _smoothPerspectiveFieldOfView = fieldOfView;
            _smoothPerspectiveTimeSeconds = Mathf.Max(0.01f, smoothTimeSeconds);
            HasSmoothPerspectiveTarget = true;
            if (resetVelocity)
                ResetPerspectiveTransitionVelocities();
        }

        public void ClearSmoothPerspectiveTarget()
        {
            HasSmoothPerspectiveTarget = false;
            _smoothPerspectiveTimeSeconds = 0f;
            ResetPerspectiveTransitionVelocities();
        }

        public void UpdateSmoothPerspective(Camera worldCamera, float fallbackSmoothTime)
        {
            if (!HasSmoothPerspectiveTarget)
                return;

            bool arrived = UpdatePerspectiveCameraMode(
                worldCamera,
                _smoothPerspectiveHeight,
                _smoothPerspectivePitch,
                _smoothPerspectiveYaw,
                _smoothPerspectiveFieldOfView,
                _smoothPerspectiveTimeSeconds > 0f ? _smoothPerspectiveTimeSeconds : fallbackSmoothTime);
            if (arrived)
                ClearSmoothPerspectiveTarget();
        }
    }
}
