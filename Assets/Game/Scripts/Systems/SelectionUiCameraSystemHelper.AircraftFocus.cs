using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public sealed partial class SelectionUiCameraSystemHelper
    {
        public bool TryFocusProducedAircraft(Entity actor)
        {
            if(_cameraSystem==null||_cameraRequestSystem==null||_worldCamera==null||!TryGetDefaultEntityManager(out var em)||HasValidTacticalFollowPose(em))return false;
            DeactivateMatchHudZoomTransition();
            bool queued=_cameraRequestSystem.QueueFocusAircraft(em,actor);
            if(queued)ProcessCameraRequests(em);
            return queued;
        }
        bool UpdateAircraftFocus()
        {
            if(_cameraSystem==null||!_cameraSystem.HasAircraftFocusContext||_cameraRequestSystem==null||_worldCamera==null||!TryGetDefaultEntityManager(out var em))return false;
            ProcessCameraRequests(em);
            return _cameraSystem.HasAircraftFocusContext;
        }
        bool RequestAircraftZoom(int delta)
        {
            if(!UpdateAircraftFocus()||!TryGetDefaultEntityManager(out var em)||HasValidTacticalFollowPose(em))return false;
            int current=_cameraSystem.AircraftFocusZoomLevel;
            int next=UnityEngine.Mathf.Clamp(current+delta,-1,1);
            if(next==current)return false;
            bool queued=_cameraRequestSystem.QueueZoomAircraft(em,next);
            if(queued)ProcessCameraRequests(em);
            return queued;
        }
    }
}
