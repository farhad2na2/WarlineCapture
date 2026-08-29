using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class SelectionUiCameraSystemHelper
    {
        public bool FocusProductionDelivery(Vector3 focusWorldPosition)
        {
            if (_cameraSystem == null || _cameraRequestSystem == null || _worldCamera == null ||
                !TryGetDefaultEntityManager(out EntityManager em) || HasValidTacticalFollowPose(em))
            {
                return false;
            }

            _matchHudZoomFocusWorldPosition = focusWorldPosition;
            _matchHudZoomTargetHeight = ResolveMatchHudZoomHeight(MatchHudZoomLevel.ZoomedIn);
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

        public void Dispose()
        {
            _tacticalFollowCameraStateQueryCache.Dispose();
        }
    }
}
