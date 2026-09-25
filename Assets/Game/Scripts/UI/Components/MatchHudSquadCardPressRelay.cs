using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchHudSquadCardPressRelay : MonoBehaviour, IPointerDownHandler
    {
        private Action<PointerEventData> _pressed;

        public void Bind(Action<PointerEventData> pressed) => _pressed = pressed;

        public void OnPointerDown(PointerEventData eventData) => _pressed?.Invoke(eventData);
    }
}
