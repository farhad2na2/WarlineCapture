using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Battlefield zone tabs on the approved-3 grid.
    /// The mockup uses equal-width bars, a warm idle fill, and a lighter selected bar with a gold edge.
    /// Height stays under the 64px trial: that band left the card preview on the 40% floor.
    /// </summary>
    public static class SkirmishZoneTabWidget
    {
        public const float Height = 52f;
        public const float Gap = 14f;
        public const float OriginX = 12f;
        public const float OriginY = 2f;

        public static float BandBottom => OriginY + Height;

        public static float Width(float gridWidth, int tabCount)
        {
            float span = gridWidth > 240f ? gridWidth - OriginX * 2f : 1008f;
            if (tabCount <= 0)
                return 160f;
            return (span - Gap * (tabCount - 1)) / tabCount;
        }

        public static float Left(int index, float gridWidth, int tabCount)
        {
            return OriginX + index * (Width(gridWidth, tabCount) + Gap);
        }

        public static void Apply(Button button, bool selected)
        {
            if (button == null)
                return;
            SkirmishLibraryButtonStateView state = button.GetComponent<SkirmishLibraryButtonStateView>();
            if (state == null && button.targetGraphic is V3GradientGraphic graphic)
            {
                state = button.gameObject.AddComponent<SkirmishLibraryButtonStateView>();
                state.Bind(graphic);
            }

            if (state == null)
                return;
            if (selected)
            {
                state.ApplyPalette(
                    new Color(0.90f, 0.78f, 0.56f, 1f),
                    new Color(0.68f, 0.54f, 0.36f, 1f),
                    new Color(0.95f, 0.74f, 0.22f, 1f),
                    true,
                    true);
                return;
            }

            state.ApplyPalette(
                new Color(0.58f, 0.52f, 0.46f, 1f),
                new Color(0.36f, 0.31f, 0.27f, 1f),
                new Color(0.40f, 0.34f, 0.28f, 1f),
                true,
                false);
        }

        public static void StyleLabel(TMP_Text label)
        {
            if (label == null)
                return;
            label.color = new Color(0.14f, 0.09f, 0.05f, 1f);
        }
    }
}
