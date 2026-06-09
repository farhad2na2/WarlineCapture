using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BuildDrawerQueueItemView : MonoBehaviour
{
    [SerializeField] private Button cancelButton;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text producerText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text percentageText;

    public Button CancelButton => cancelButton;

    public void Bind(
        string displayName,
        string producer,
        string time,
        float progress01,
        Sprite thumbnail,
        bool cancelable)
    {
        SetText(nameText, displayName);
        SetText(producerText, producer);
        SetText(timeText, time);

        float progress = Mathf.Clamp01(progress01);
        if (progressSlider != null)
            progressSlider.value = progress;

        if (percentageText != null)
            percentageText.text = Mathf.RoundToInt(progress * 100f).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";

        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = thumbnail;
            thumbnailImage.enabled = thumbnail != null;
        }

        if (cancelButton != null)
            cancelButton.interactable = cancelable;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }
}
