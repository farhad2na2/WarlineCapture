using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIModeCardView : MonoBehaviour
{
    [SerializeField] private Image artImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject lockRoot;
    [SerializeField] private GameObject notificationBadge;
    [SerializeField] private Button button;

    public Image ArtImage => artImage;
    public TMP_Text TitleText => titleText;
    public TMP_Text SubtitleText => subtitleText;
    public TMP_Text DescriptionText => subtitleText;
    public TMP_Text ProgressText => progressText;
    public GameObject LockRoot => lockRoot;
    public GameObject NotificationBadge => notificationBadge;
    public Button Button => button;

    public void Bind(string title, string subtitle, string progress = "", bool locked = false, bool hasNotification = false, Sprite art = null)
    {
        if (titleText != null)
            titleText.text = title;

        if (subtitleText != null)
            subtitleText.text = subtitle;

        if (progressText != null)
            progressText.text = progress;

        if (lockRoot != null)
            lockRoot.SetActive(locked);

        if (notificationBadge != null)
            notificationBadge.SetActive(hasNotification);

        if (artImage != null)
        {
            artImage.sprite = art;
            artImage.enabled = art != null;
        }
    }
}
