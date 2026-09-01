using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ArmoryV3CategoryTabVisual : MonoBehaviour
    {
        [SerializeField] private V3GradientGraphic panel;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;

        private static readonly Color SelectedTopLeft = new Color32(17, 116, 178, 255);
        private static readonly Color SelectedTopRight = new Color32(6, 88, 151, 255);
        private static readonly Color SelectedBottomLeft = new Color32(3, 63, 113, 255);
        private static readonly Color SelectedBottomRight = new Color32(4, 75, 129, 255);
        private static readonly Color IdleTop = new Color32(31, 41, 46, 255);
        private static readonly Color IdleBottom = new Color32(8, 15, 18, 255);
        private static readonly Color Border = new Color32(89, 105, 110, 255);
        private static readonly Color Cyan = new Color32(26, 191, 239, 255);
        private static readonly Color White = new Color32(239, 242, 238, 255);

        public void Configure(V3GradientGraphic targetPanel, TMP_Text targetLabel, Image targetIcon)
        {
            panel = targetPanel;
            label = targetLabel;
            icon = targetIcon;
        }

        public void SetSelected(bool selected)
        {
            if (panel != null)
            {
                if (selected)
                {
                    panel.ConfigureCorners(
                        SelectedTopLeft,
                        SelectedTopRight,
                        SelectedBottomLeft,
                        SelectedBottomRight,
                        Cyan,
                        3f);
                }
                else
                {
                    panel.Configure(IdleTop, IdleBottom, Border, 3f);
                }
            }

            if (label != null)
                label.color = White;
            if (icon != null)
                icon.color = selected ? Cyan : White;
        }
    }
}
