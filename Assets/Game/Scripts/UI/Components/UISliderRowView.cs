using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UISliderRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Slider slider;

    public TMP_Text LabelText => labelText;
    public TMP_Text ValueText => valueText;
    public Slider Slider => slider;

    public void Bind(string label, float value, float minValue, float maxValue, string valueFormat = "0")
    {
        if (labelText != null)
            labelText.text = label;

        if (slider != null)
        {
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.SetValueWithoutNotify(Mathf.Clamp(value, minValue, maxValue));
        }

        if (valueText != null)
        {
            string formattedValue = value.ToString(valueFormat);
            valueText.text = Mathf.Approximately(minValue, 0f) && Mathf.Approximately(maxValue, 100f)
                ? $"{formattedValue}%"
                : formattedValue;
        }
    }
}
