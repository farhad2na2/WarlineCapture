using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
namespace Game.UI.Runtime
{
    internal sealed partial class AssistantHighlightPresentationSystemHelper
    {
        private RectTransform _selectionBox;
        private bool _selectionBoxRequested;
        private Vector2 _selectionDragStart,_selectionDragEnd;
        private PointerEventData _selectionRaycast;
        private EventSystem _selectionEventSystem;
        private readonly List<RaycastResult> _selectionHits = new(16);
        internal void ShowTutorialSelectionBox(Vector3 min,Vector3 max)
        {
            ShowTutorialWorld((min+max)*.5f);
            _selectionBoxRequested=true;
            if(_worldCamera==null)return;
            Vector2 lo=new(float.MaxValue,float.MaxValue),hi=new(float.MinValue,float.MinValue);
            for(int i=0;i<8;i++)
            {
                var point=_worldCamera.WorldToScreenPoint(new Vector3((i&1)==0?min.x:max.x,(i&2)==0?min.y:max.y,(i&4)==0?min.z:max.z));
                if(point.z<=0)return;
                lo=Vector2.Min(lo,point);hi=Vector2.Max(hi,point);
            }
            lo-=Vector2.one*16;hi+=Vector2.one*16;
            var safe=Screen.safeArea;
            if(!safe.Contains(lo)||!safe.Contains(hi))return;
            EnsureScreenTargetIndicator();
            if(_selectionBox==null)
            {
                var go=new GameObject("NormalSelectionDragGuide",typeof(RectTransform));
                _selectionBox=go.GetComponent<RectTransform>();_selectionBox.SetParent(_screenTargetCanvas.transform,false);
                _selectionBox.anchorMin=_selectionBox.anchorMax=_selectionBox.pivot=Vector2.zero;
                for(int i=0;i<4;i++)
                {
                    var edge=new GameObject("Edge",typeof(RectTransform),typeof(Image));edge.transform.SetParent(_selectionBox,false);
                    var graphic=edge.GetComponent<Image>();graphic.color=V3GuidanceYellow;graphic.raycastTarget=false;
                    var rect=(RectTransform)edge.transform;
                    rect.anchorMin=i<2?new Vector2(0,i):new Vector2(i-2,0);
                    rect.anchorMax=i<2?new Vector2(1,i):new Vector2(i-2,1);
                    rect.sizeDelta=i<2?new Vector2(0,3):new Vector2(3,0);rect.anchoredPosition=Vector2.zero;
                }
            }
            _selectionDragStart=lo;_selectionDragEnd=hi;
            var canvasRect=(RectTransform)_screenTargetCanvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,lo,null,out var localLo);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,hi,null,out var localHi);
            _selectionBox.anchoredPosition=localLo-canvasRect.rect.min;_selectionBox.sizeDelta=localHi-localLo;
            _selectionBox.gameObject.SetActive(true);
            if(_worldRingRoot!=null)_worldRingRoot.SetActive(false);
        }
        internal bool TryObserveVisibleSelectionDrag(out Vector2 start,out Vector2 end)
        {
            start=_selectionDragStart;end=_selectionDragEnd;
            return _selectionBoxRequested && _selectionBox!=null && _selectionBox.gameObject.activeInHierarchy &&
                SelectionDragIsUnobstructed();
        }
        private bool SelectionDragIsUnobstructed()
        {
            var events = EventSystem.current;
            if (events == null) return true;
            if (_selectionRaycast == null || _selectionEventSystem != events)
            {
                _selectionEventSystem = events;
                _selectionRaycast = new PointerEventData(events);
            }
            return SelectionPointIsUnobstructed(_selectionDragStart) &&
                SelectionPointIsUnobstructed(_selectionDragEnd) &&
                SelectionPointIsUnobstructed((_selectionDragStart + _selectionDragEnd) * .5f);
        }
        private bool SelectionPointIsUnobstructed(Vector2 point)
        {
            _selectionRaycast.position = point;
            _selectionHits.Clear();
            _selectionEventSystem.RaycastAll(_selectionRaycast, _selectionHits);
            return _selectionHits.Count == 0;
        }
        private void HideSelectionDrag()
        {
            _selectionBoxRequested=false;
            if(_selectionBox!=null)_selectionBox.gameObject.SetActive(false);
        }
    }
}
