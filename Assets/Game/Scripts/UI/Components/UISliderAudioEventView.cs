using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Slider))]
    public sealed class UISliderAudioEventView : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private UIAudioEventKind tickEvent = UIAudioEventKind.SliderTick;
        [SerializeField, Min(0f)] private float minimumValueDelta = 1f;

        private float _lastTickValue;
        private bool _hasTickValue;
        private bool _wired;

        public Slider Slider => slider;

        private void OnEnable()
        {
            Wire();
        }

        private void OnDisable()
        {
            if (slider != null && _wired)
            {
                slider.onValueChanged.RemoveListener(HandleValueChanged);
                _wired = false;
            }
        }

        public void Configure(UIAudioEventKind eventKind, float valueDelta)
        {
            tickEvent = eventKind;
            minimumValueDelta = Mathf.Max(0f, valueDelta);
            Wire();
        }

        private void HandleValueChanged(float value)
        {
            if (_hasTickValue && Mathf.Abs(value - _lastTickValue) < minimumValueDelta)
                return;

            _lastTickValue = value;
            _hasTickValue = true;
            UIAudioEventGateway.Raise(tickEvent);
        }

        private void Wire()
        {
            if (slider == null)
                slider = GetComponent<Slider>();

            if (slider == null || _wired)
                return;

            _lastTickValue = slider.value;
            _hasTickValue = true;
            slider.onValueChanged.AddListener(HandleValueChanged);
            _wired = true;
        }
    }
}
