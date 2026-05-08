using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureRewardItemView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image rarityFrame;

    public Image Icon => icon;
    public TMP_Text QuantityText => quantityText;
    public Image RarityFrame => rarityFrame;

    public void Bind(string quantity, Color rarityColor, Sprite iconSprite = null)
    {
        if (quantityText != null)
            quantityText.text = quantity;

        if (rarityFrame != null)
            rarityFrame.color = rarityColor;

        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
        }
    }
}
