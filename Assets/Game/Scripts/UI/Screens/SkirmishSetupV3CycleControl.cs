using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SkirmishSetupV3CycleControl : MonoBehaviour
    {
        [SerializeField] private Button cycleButton;
        [SerializeField] private UISegmentedControlView segmented;
        [SerializeField] private UISliderRowView sliderRow;
        [SerializeField] private TMP_Text displayText;
        [SerializeField] private string[] segmentValues;
        [SerializeField] private string[] displayValues;
        [SerializeField] private float sliderMin;
        [SerializeField] private float sliderMax = 1f;
        [SerializeField] private float sliderStep = .5f;
        [SerializeField] private string sliderFormat = "0.0x";

        private string _lastDisplay;

        public void ConfigureSegment(
            Button button,
            UISegmentedControlView view,
            TMP_Text valueText,
            string[] configuredSegmentValues,
            string[] configuredDisplayValues)
        {
            cycleButton = button;
            segmented = view;
            sliderRow = null;
            displayText = valueText;
            segmentValues = configuredSegmentValues;
            displayValues = configuredDisplayValues;
        }

        public void ConfigureSlider(
            Button button,
            UISliderRowView view,
            TMP_Text valueText,
            float min,
            float max,
            float step,
            string format)
        {
            cycleButton = button;
            segmented = null;
            sliderRow = view;
            displayText = valueText;
            sliderMin = min;
            sliderMax = max;
            sliderStep = step;
            sliderFormat = format;
        }

        private void OnEnable()
        {
            if (cycleButton != null)
                cycleButton.onClick.AddListener(Cycle);
            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (cycleButton != null)
                cycleButton.onClick.RemoveListener(Cycle);
        }

        private void LateUpdate() => RefreshDisplay();

        private void Cycle()
        {
            if (segmented != null && segmentValues != null && segmentValues.Length > 0)
            {
                int next = (ResolveSelectedSegment() + 1) % segmentValues.Length;
                segmented.Bind(segmentValues, next);
            }
            else if (sliderRow?.Slider != null)
            {
                float next = sliderRow.Slider.value + sliderStep;
                if (next > sliderMax + .001f)
                    next = sliderMin;
                sliderRow.Slider.value = Mathf.Clamp(next, sliderMin, sliderMax);
            }
            RefreshDisplay();
        }

        private int ResolveSelectedSegment()
        {
            Button[] buttons = segmented?.SegmentButtons;
            if (buttons == null || buttons.Length == 0)
                return 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && !buttons[i].interactable)
                    return i;
            }
            return 0;
        }

        private void RefreshDisplay()
        {
            if (displayText == null)
                return;
            string value;
            if (segmented != null)
            {
                int selected = ResolveSelectedSegment();
                value = displayValues != null && selected >= 0 && selected < displayValues.Length
                    ? displayValues[selected]
                    : segmentValues != null && selected >= 0 && selected < segmentValues.Length
                        ? segmentValues[selected]
                        : string.Empty;
            }
            else if (sliderRow?.Slider != null)
            {
                value = sliderRow.Slider.value.ToString(sliderFormat);
            }
            else
            {
                value = string.Empty;
            }
            if (_lastDisplay == value)
                return;
            _lastDisplay = value;
            displayText.text = value;
        }
    }
}
