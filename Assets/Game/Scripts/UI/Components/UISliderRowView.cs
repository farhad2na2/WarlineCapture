using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class UISliderRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Slider slider;

        private float _minValue;
        private float _maxValue = 100f;
        private string _valueFormat = "0";

        public TMP_Text LabelText => labelText;
        public TMP_Text ValueText => valueText;
        public Slider Slider => slider;

        private void Awake()
        {
            if (slider != null)
                slider.onValueChanged.AddListener(UpdateValueText);
        }

        private void OnDestroy()
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(UpdateValueText);
        }

        public void Bind(string label, float value, float minValue, float maxValue, string valueFormat = "0")
        {
            if (labelText != null)
                labelText.text = label;

            _minValue = minValue;
            _maxValue = maxValue;
            _valueFormat = valueFormat;

            if (slider != null)
            {
                slider.minValue = minValue;
                slider.maxValue = maxValue;
                slider.SetValueWithoutNotify(Mathf.Clamp(value, minValue, maxValue));
            }

            UpdateValueText(value);
        }

        private void UpdateValueText(float value)
        {
            if (valueText == null)
                return;

            string formattedValue = Mathf.Clamp(value, _minValue, _maxValue).ToString(_valueFormat);
            valueText.text = Mathf.Approximately(_minValue, 0f) && Mathf.Approximately(_maxValue, 100f)
                ? $"{formattedValue}%"
                : formattedValue;
        }
    }
}
