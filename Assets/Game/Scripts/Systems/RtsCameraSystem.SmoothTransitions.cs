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

        public Vector3 UpdateSmoothFocus(Vector3 currentGroundCenter, float smoothTime)
        {
            if (!HasSmoothFocusTarget)
                return currentGroundCenter;

            Vector3 smoothedCenter = Vector3.SmoothDamp(
                currentGroundCenter,
                SmoothFocusTarget,
                ref _smoothFocusVelocity,
                Mathf.Max(0.01f,
                    _smoothFocusTimeSeconds > 0f ? _smoothFocusTimeSeconds : smoothTime));

            Vector2 remaining = new(
                SmoothFocusTarget.x - smoothedCenter.x,
                SmoothFocusTarget.z - smoothedCenter.z);
            if (remaining.sqrMagnitude > SmoothFocusCompletionDistanceSq)
                return smoothedCenter;

            smoothedCenter = SmoothFocusTarget;
            ClearSmoothFocusTarget();
            return smoothedCenter;
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
