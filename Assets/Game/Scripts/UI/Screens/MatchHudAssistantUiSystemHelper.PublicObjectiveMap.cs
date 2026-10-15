using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private AriaTouchTarget ObservePublicObjectiveMapFocus(Vector3 world,in AriaSkirmishObservation view)
        {
            if(watchMap==null||watchMap.MapRect==null)return default;
            var camera=ResolveEventCamera(watchMap.MapImage);
            Vector2 point;
            if(!view.MapOpen)point=RectTransformUtility.WorldToScreenPoint(camera,watchMap.MapRect.TransformPoint(watchMap.MapRect.rect.center));
            else if(!watchMap.TryGetPresentedMapPoint(world,camera,out point))return default;
            const int target=-21001;
            if(!PublicMapPointReachable(point))return default;
            var result=new AriaTouchTarget {Id=target,Position=point,Available=true};
            if(view.MapOpen)
            {
                // Reuse the actual public viewport-drag route; never press the
                // map as a troop destination while a command mode may be armed.
                if(watchMap.ViewportRect==null)return default;
                var viewport=watchMap.ViewportRect;
                var start=RectTransformUtility.WorldToScreenPoint(camera,viewport.TransformPoint(viewport.rect.center));
                if(!PublicMapPointReachable(start))return default;
                result.Position=start;result.Drag=true;
                result.DragEnd=(start-point).sqrMagnitude>25?point:point+Vector2.up*12;
                if(!PublicMapPointReachable(result.DragEnd))return default;
            }
            return result;
        }
        private bool PublicMapPointReachable(Vector2 point)
        {
            if(!Screen.safeArea.Contains(point)||watchRaycast==null||UnityEngine.EventSystems.EventSystem.current==null)return false;
            watchRaycast.position=point;watchHits.Clear();UnityEngine.EventSystems.EventSystem.current.RaycastAll(watchRaycast,watchHits);
            return watchHits.Count>0&&watchHits[0].gameObject.GetComponentInParent<MatchHudMinimapView>()==watchMap;
        }
    }
}
