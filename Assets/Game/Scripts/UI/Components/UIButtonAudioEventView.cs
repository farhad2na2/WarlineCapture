using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonAudioEventView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private UIAudioEventKind clickEvent = UIAudioEventKind.ButtonPrimaryClick;
        [SerializeField] private UIAudioEventKind disabledTapEvent = UIAudioEventKind.ButtonDisabledTap;
        private bool _wired;

        public Button Button => button;
        public UIAudioEventKind ClickEvent => clickEvent;
        public UIAudioEventKind DisabledTapEvent => disabledTapEvent;

        private void OnEnable()
        {
            Wire();
        }

        private void OnDisable()
        {
            if (button != null && _wired)
            {
                button.onClick.RemoveListener(HandleClick);
                _wired = false;
            }
        }

        public void Configure(UIAudioEventKind enabledEvent, UIAudioEventKind disabledEvent = UIAudioEventKind.ButtonDisabledTap)
        {
            clickEvent = enabledEvent;
            disabledTapEvent = disabledEvent;
            Wire();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button != null && !button.interactable)
                UIAudioEventGateway.Raise(disabledTapEvent);
        }

        private void HandleClick()
        {
            UIAudioEventGateway.Raise(clickEvent);
        }

        private void Wire()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button == null || _wired)
                return;

            button.onClick.AddListener(HandleClick);
            _wired = true;
        }
    }
}
