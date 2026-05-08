using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureObjectiveRowView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image progressFill;
    [SerializeField] private GameObject completeIcon;

    public Image Icon => icon;
    public TMP_Text LabelText => labelText;
    public TMP_Text ProgressText => progressText;
    public Image ProgressFill => progressFill;
    public GameObject CompleteIcon => completeIcon;

    public void Bind(string label, string progress, float normalizedProgress, bool complete = false, Sprite iconSprite = null)
    {
        if (labelText != null)
            labelText.text = label;

        if (progressText != null)
            progressText.text = progress;

        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(normalizedProgress);

        if (completeIcon != null)
            completeIcon.SetActive(complete);

        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
        }
    }
}
