using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Toggle))]
    public sealed class UIToggleAudioEventView : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private UIAudioEventKind onEvent = UIAudioEventKind.ToggleOn;
        [SerializeField] private UIAudioEventKind offEvent = UIAudioEventKind.ToggleOff;
        private bool _wired;

        public Toggle Toggle => toggle;

        private void OnEnable()
        {
            Wire();
        }

        private void OnDisable()
        {
            if (toggle != null && _wired)
            {
                toggle.onValueChanged.RemoveListener(HandleValueChanged);
                _wired = false;
            }
        }

        public void Configure(UIAudioEventKind enabledEvent, UIAudioEventKind disabledEvent)
        {
            onEvent = enabledEvent;
            offEvent = disabledEvent;
            Wire();
        }

        private void HandleValueChanged(bool value)
        {
            UIAudioEventGateway.Raise(value ? onEvent : offEvent);
        }

        private void Wire()
        {
            if (toggle == null)
                toggle = GetComponent<Toggle>();

            if (toggle == null || _wired)
                return;

            toggle.onValueChanged.AddListener(HandleValueChanged);
            _wired = true;
        }
    }
}
