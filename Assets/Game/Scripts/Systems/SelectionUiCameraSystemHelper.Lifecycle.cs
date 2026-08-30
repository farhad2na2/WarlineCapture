using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class SelectionUiCameraSystemHelper
    {
        private const float ProductionDeliveryZoomHeight = 32f;
        private const float ProductionDeliveryFocusForwardOffset = 4f;

        public bool FocusProductionDelivery(Vector3 focusWorldPosition)
        {
            if (_cameraSystem == null || _cameraRequestSystem == null || _worldCamera == null ||
                !TryGetDefaultEntityManager(out EntityManager em) || HasValidTacticalFollowPose(em))
            {
                return false;
            }

            _matchHudZoomFocusWorldPosition = ResolveProductionDeliveryFocusPoint(
                focusWorldPosition,
                _worldCamera.transform.forward);
            _matchHudZoomTargetHeight = ResolveProductionDeliveryZoomHeight(_minZoomHeight, _maxZoomHeight);
            _matchHudZoomLevel = MatchHudZoomLevel.ZoomedIn;
            _matchHudZoomTransitionActive = true;
            _cameraRequestSystem.QueueClearSmoothFocusTarget(em);
            _cameraRequestSystem.QueueClearDragging(em);
            _cameraRequestSystem.QueueCompleteZoomTransition(em);
            _cameraRequestSystem.QueueResetTransitionVelocities(em);
            _cameraRequestSystem.QueueSetNormalIsoModeActive(em, false);
            ProcessCameraRequests(em);
            UpdateZoomTransition();
            return true;
        }

        internal static float ResolveProductionDeliveryZoomHeight(float minZoomHeight, float maxZoomHeight)
        {
            return Mathf.Clamp(ProductionDeliveryZoomHeight, minZoomHeight, maxZoomHeight);
        }

        internal static Vector3 ResolveProductionDeliveryFocusPoint(
            Vector3 focusWorldPosition,
            Vector3 cameraForward)
        {
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude <= 0.0001f)
                return focusWorldPosition;

            return focusWorldPosition + (cameraForward.normalized * ProductionDeliveryFocusForwardOffset);
        }

        public void Dispose()
        {
            _tacticalFollowCameraStateQueryCache.Dispose();
        }
    }
}
