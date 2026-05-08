using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureActionButtonView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private GameObject lockRoot;
    [SerializeField] private Button button;

    public Image Icon => icon;
    public TMP_Text LabelText => labelText;
    public TMP_Text CostText => costText;
    public GameObject LockRoot => lockRoot;
    public Button Button => button;

    public void Bind(string label, string cost = "", bool locked = false, Sprite iconSprite = null)
    {
        if (labelText != null)
            labelText.text = label;

        if (costText != null)
            costText.text = cost;

        if (lockRoot != null)
            lockRoot.SetActive(locked);

        if (button != null)
            button.interactable = !locked;

        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
        }
    }
}
