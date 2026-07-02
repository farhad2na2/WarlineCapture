using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class UIResourceCounterView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Button plusButton;

        public Image Icon => icon;
        public TMP_Text ValueText => valueText;
        public Button PlusButton => plusButton;

        public void Bind(string value, Sprite iconSprite = null, bool showPlusButton = true)
        {
            if (valueText != null)
                valueText.text = value;

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }

            if (plusButton != null)
                plusButton.gameObject.SetActive(showPlusButton);
        }
    }
}
