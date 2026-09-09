using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI.Runtime
{
    public sealed class MatchHudMinimapViewportDragRelay : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
    {
        private MatchHudMinimapView _view;

        public void Configure(MatchHudMinimapView view)
        {
            _view = view;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _view?.OnPointerDown(eventData);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData != null)
                eventData.useDragThreshold = false;
            _view?.OnInitializePotentialDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _view?.OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _view?.OnPointerUp(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _view?.OnPointerClick(eventData);
        }
    }
}
