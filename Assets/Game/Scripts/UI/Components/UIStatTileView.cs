using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class UIStatTileView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text deltaText;

        public Image Icon => icon;
        public TMP_Text LabelText => labelText;
        public TMP_Text ValueText => valueText;
        public TMP_Text DeltaText => deltaText;

        public void Bind(string label, string value, string delta = "", Sprite iconSprite = null)
        {
            if (labelText != null)
                labelText.text = label;

            if (valueText != null)
                valueText.text = value;

            if (deltaText != null)
                deltaText.text = delta;

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }
        }
    }
}
