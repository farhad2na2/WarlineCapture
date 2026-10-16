using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class SelectionUiCameraSystemHelper
    {
        int placementToken;
        ulong placementWorld;
        Quaternion placementRotation;
        Vector3 placementSize;
        Rect placementViewport;
        float placementAspect;
        public bool TryFitPlacementPreview(int token,Vector3[] points,Quaternion modelRotation,Rect viewport,out Action release)
            =>TryFitPlacementPreviewOnSurface(token,points,modelRotation,viewport,Screen.width,Screen.height,out release);

        // Same production path with an explicit render surface for owning native
        // controls. The public normal-input boundary always uses actual Screen.
        internal bool TryFitPlacementPreviewOnSurface(int token,Vector3[] points,Quaternion modelRotation,Rect viewport,float surfaceWidth,float surfaceHeight,out Action release)
        {
            release=null;
            if(token==0||_worldCamera==null||_cameraSystem==null||_cameraRequestSystem==null||
                !TryGetDefaultEntityManager(out var em)||HasValidTacticalFollowPose(em)||
                !SkirmishMissionViewProfileApplication.TryResolve(em,out var profile))return false;
            if(points==null||points.Length<4)return false;
            if(!SkirmishPlacementCameraFit.TryViewportRect(viewport,_worldCamera.pixelRect,surfaceWidth,surfaceHeight,out viewport))return false;
            var size=Vector3.zero;var min=points[0];var max=min;
            foreach(var p in points){min=Vector3.Min(min,p);max=Vector3.Max(max,p);}size=max-min;
            release=()=>ReleasePlacementPreview(token);
            bool sameOwner=placementToken==token&&placementWorld==em.World.SequenceNumber&&_cameraSystem.HasPlacementPreviewPose;
            bool sameGeometry=placementRotation==modelRotation&&(placementSize-size).sqrMagnitude<=Mathf.Max(1,size.sqrMagnitude)*.000000001f&&placementViewport==viewport&&placementAspect==_worldCamera.aspect;
            if(sameOwner&&(_cameraSystem.PlacementPreviewManuallyAdjusted||sameGeometry))
                return SkirmishPlacementCameraFit.Contains(points,_worldCamera.transform.position,_worldCamera.transform.rotation,
                    _worldCamera.fieldOfView,_worldCamera.aspect,viewport,_worldCamera.nearClipPlane,_worldCamera.farClipPlane);
            var rotation=Quaternion.Euler(profile.Pitch,profile.Yaw,0);
            if(!SkirmishPlacementCameraFit.TryFit(points,rotation,profile.BuildFieldOfView,_worldCamera.aspect,viewport,
                _worldCamera.nearClipPlane,_worldCamera.farClipPlane,out var position,profile.BuildHeight))return false;
            placementToken=token;placementWorld=em.World.SequenceNumber;
            DeactivateMatchHudZoomTransition();
            _cameraRequestSystem.QueueSetWasBuildModeActive(em,true);
            _cameraRequestSystem.QueuePlacementPreviewPose(em,token,position,rotation,profile.BuildFieldOfView);
            ProcessCameraRequests(em);
            if(!_cameraSystem.HasPlacementPreviewPose||!SkirmishPlacementCameraFit.Contains(points,_worldCamera.transform.position,
                _worldCamera.transform.rotation,_worldCamera.fieldOfView,_worldCamera.aspect,viewport,_worldCamera.nearClipPlane,_worldCamera.farClipPlane))
            {ReleasePlacementPreview(token);return false;}
            placementToken=token;placementWorld=em.World.SequenceNumber;placementRotation=modelRotation;placementSize=size;placementViewport=viewport;placementAspect=_worldCamera.aspect;
            return true;
        }
        void ReleasePlacementPreview(int token)
        {
            if(placementToken!=token)return;
            placementToken=0;
            if(!TryGetDefaultEntityManager(out var em)||em.World.SequenceNumber!=placementWorld)return;
            _cameraRequestSystem.QueueReleasePlacementPreview(em,token);ProcessCameraRequests(em);
        }
    }
}
