using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [Serializable]
    public sealed class BuildDrawerTabView
    {
        [SerializeField] private BuildDrawerCategory category;
        [SerializeField] private Button button;
        [SerializeField] private Image frame;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject disabledOverlay;

        public BuildDrawerCategory Category => category;
        public Button Button => button;
        public Image Frame => frame;
        public TMP_Text LabelText => labelText;
        public TMP_Text CountText => countText;

        public void Apply(
            bool selected,
            bool interactable,
            int itemCount,
            Sprite selectedFrameSprite,
            Sprite normalFrameSprite)
        {
            if (button != null)
                button.interactable = interactable;

            if (frame != null)
            {
                Sprite target = selected ? selectedFrameSprite : normalFrameSprite;
                if (target != null)
                    frame.sprite = target;
            }

            if (countText != null)
                countText.text = itemCount > 0 ? itemCount.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;

            if (disabledOverlay != null)
                disabledOverlay.SetActive(!interactable);

            V3GradientGraphic gradient = button != null
                ? button.GetComponent<V3GradientGraphic>()
                : null;
            if (gradient != null)
            {
                gradient.Configure(
                    !interactable
                        ? new Color32(43, 49, 52, 255)
                        : selected
                            ? new Color32(93, 69, 13, 255)
                            : new Color32(31, 43, 48, 255),
                    !interactable
                        ? new Color32(10, 15, 17, 255)
                        : selected
                            ? new Color32(24, 18, 4, 255)
                            : new Color32(4, 10, 12, 255),
                    !interactable
                        ? new Color32(76, 85, 88, 255)
                        : selected
                            ? new Color32(255, 195, 21, 255)
                            : new Color32(92, 106, 109, 255),
                    3f);
            }
        }
    }
}
