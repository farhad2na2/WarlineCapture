using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchHudSquadCardPressRelay : MonoBehaviour, IPointerDownHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Action<PointerEventData> _pressed;
        private Action _cancelPress;
        private ScrollRect _scroll;
        public bool Dragged { get; private set; }
        public void Bind(Action<PointerEventData> pressed) => _pressed = pressed;
        public void ConfigureScroll(ScrollRect scroll, Action cancelPress)
        { _scroll = scroll; _cancelPress = cancelPress; }
        public void OnPointerDown(PointerEventData eventData)
        { Dragged = false; _pressed?.Invoke(eventData); }
        public void OnInitializePotentialDrag(PointerEventData eventData) => _scroll?.OnInitializePotentialDrag(eventData);
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            Dragged = true;
            eventData.eligibleForClick = false;
            _cancelPress?.Invoke();
            _scroll?.OnBeginDrag(eventData);
        }
        public void OnDrag(PointerEventData eventData) => _scroll?.OnDrag(eventData);
        public void OnEndDrag(PointerEventData eventData) => _scroll?.OnEndDrag(eventData);
    }
}
