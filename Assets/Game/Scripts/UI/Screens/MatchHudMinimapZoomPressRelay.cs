using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI.Runtime
{
    public sealed class MatchHudMinimapZoomPressRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private MatchHudMinimapView _view;
        private int _direction;

        public void Configure(MatchHudMinimapView view, int direction)
        {
            _view = view;
            _direction = direction;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _view?.NotifyZoomHeld(_direction, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _view?.NotifyZoomHeld(_direction, false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _view?.NotifyZoomHeld(_direction, false);
        }
    }
}
