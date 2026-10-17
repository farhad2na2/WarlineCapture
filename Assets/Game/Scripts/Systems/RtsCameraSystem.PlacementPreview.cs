using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class RtsCameraSystem
    {
        int placementPreviewToken;
        Vector3 placementRestorePosition;
        Quaternion placementRestoreRotation;
        float placementRestoreFov,placementRestoreSize;
        bool placementRestoreOrthographic,placementRestoreNormalIso;
        public bool PlacementPreviewManuallyAdjusted {get;private set;}
        public bool HasPlacementPreviewPose=>placementPreviewToken!=0;
        public void ClearPlacementPreviewPose(Camera camera,bool restore)=>ReleasePlacementPreviewPose(camera,placementPreviewToken,restore);
        public void ApplyPlacementPreviewPose(Camera camera,int token,Vector3 position,Quaternion rotation,float fov)
        {
            if(camera==null||token==0||!float.IsFinite(position.x)||!float.IsFinite(position.y)||!float.IsFinite(position.z)||
                !float.IsFinite(fov)||fov<=1||fov>=179||!float.IsFinite(Quaternion.Dot(rotation,rotation))||Mathf.Abs(Quaternion.Dot(rotation,rotation)-1)>.001f)return;
            if(placementPreviewToken!=token)
            {
                ReleasePlacementPreviewPose(camera,placementPreviewToken,true);
                placementRestorePosition=camera.transform.position;placementRestoreRotation=camera.transform.rotation;
                placementRestoreFov=camera.fieldOfView;placementRestoreSize=camera.orthographicSize;placementRestoreOrthographic=camera.orthographic;
                placementRestoreNormalIso=NormalIsoModeActive;
                placementPreviewToken=token;
                PlacementPreviewManuallyAdjusted=false;
            }
            ClearSmoothFocusTarget();ClearSmoothPerspectiveTarget();IsZoomTransitionActive=false;MatchIntroZoomSettlePending=false;
            NormalIsoModeActive=false;camera.orthographic=false;camera.fieldOfView=fov;camera.transform.SetPositionAndRotation(position,rotation);
        }
        public void ObservePlacementPreviewPan(Camera camera,Vector3 priorPosition)
        {
            if(!HasPlacementPreviewPose||camera==null)return;
            var delta=camera.transform.position-priorPosition;
            if(delta.sqrMagnitude<=.000001f)return;
            PlacementPreviewManuallyAdjusted=true;
            placementRestorePosition+=new Vector3(delta.x,0,delta.z);
        }
        public void ObservePlacementPreviewGroundCenter(Camera camera)
        {
            if(!HasPlacementPreviewPose||camera==null)return;
            var forward=placementRestoreRotation*Vector3.forward;
            if(Mathf.Abs(forward.y)<.0001f)return;
            PlacementPreviewManuallyAdjusted=true;
            var ground=GetCameraGroundCenterWorld(camera);
            // A minimap target is absolute. Rebuild the old lens around that
            // accepted ground point rather than applying a relative preview delta.
            placementRestorePosition=ground+forward*(placementRestorePosition.y/forward.y);
        }
        public void ReleasePlacementPreviewPose(Camera camera,int token,bool restore)
        {
            if(placementPreviewToken==0||token!=placementPreviewToken)return;
            placementPreviewToken=0;
            PlacementPreviewManuallyAdjusted=false;
            if(camera==null||!restore)return;
            camera.transform.SetPositionAndRotation(placementRestorePosition,placementRestoreRotation);
            camera.fieldOfView=placementRestoreFov;camera.orthographic=placementRestoreOrthographic;camera.orthographicSize=placementRestoreSize;
            NormalIsoModeActive=placementRestoreNormalIso;
        }
    }
}
