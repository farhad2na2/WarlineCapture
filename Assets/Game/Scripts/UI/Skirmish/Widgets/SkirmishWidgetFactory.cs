using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Small uGUI construction helpers shared by the standalone battle-library widgets.</summary>
    public static class SkirmishWidgetFactory
    {
        public static readonly Color Gold = new Color(0.96f, 0.74f, 0.25f, 1f);
        public static readonly Color GoldSoft = new Color(0.96f, 0.74f, 0.25f, 0.35f);
        public static readonly Color Panel = new Color(0.055f, 0.075f, 0.09f, 0.94f);
        public static readonly Color PanelEdge = new Color(0.32f, 0.36f, 0.38f, 1f);
        public static readonly Color HeaderBar = new Color(0.03f, 0.05f, 0.07f, 0.96f);
        public static readonly Color TextPrimary = new Color(0.95f, 0.95f, 0.92f, 1f);
        public static readonly Color TextAccent = new Color(0.55f, 0.82f, 0.98f, 1f);
        public static readonly Color TextMuted = new Color(0.72f, 0.74f, 0.74f, 1f);
        public static readonly Color LockShade = new Color(0.02f, 0.03f, 0.04f, 0.72f);
        public static readonly Color EmptyCell = new Color(0.10f, 0.12f, 0.14f, 0.96f);
        public static readonly Color NewRibbon = new Color(0.78f, 0.10f, 0.08f, 1f);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Places a rect by its top-left corner inside a top-left-anchored parent space.</summary>
        public static void TopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null, bool raycast = false)
        {
            RectTransform rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            image.preserveAspect = false;
            return image;
        }

        public static RawImage Raw(string name, Transform parent, Texture texture, Color color)
        {
            RectTransform rect = Rect(name, parent);
            var raw = rect.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.color = color;
            raw.raycastTarget = false;
            return raw;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
            SkirmishWidgetContext context, TextAlignmentOptions alignment, bool bold = true)
        {
            RectTransform rect = Rect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (context != null && context.Font != null)
                label.font = context.Font;
            label.text = text ?? string.Empty;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(10f, size * 0.62f);
            label.color = color;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.isRightToLeftText = context != null && context.RightToLeft;
            label.alignment = Mirror(alignment, context != null && context.RightToLeft);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            if (context != null && context.ApplyText != null)
            {
                context.ApplyText(label, text ?? string.Empty, label.fontSizeMin, size);
                label.alignment = Mirror(alignment, context.RightToLeft);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                // TMP ellipsis on shaped RTL text can blank the whole line (seen on FA tabs, ribbons, pin headers).
                label.overflowMode = context.RightToLeft ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            }
            return label;
        }

        /// <summary>Mirrors left/right alignments for RTL locales; centred alignments are unchanged.</summary>
        public static TextAlignmentOptions Mirror(TextAlignmentOptions alignment, bool rightToLeft)
        {
            if (!rightToLeft)
                return alignment;
            switch (alignment)
            {
                case TextAlignmentOptions.Left: return TextAlignmentOptions.Right;
                case TextAlignmentOptions.Right: return TextAlignmentOptions.Left;
                case TextAlignmentOptions.TopLeft: return TextAlignmentOptions.TopRight;
                case TextAlignmentOptions.TopRight: return TextAlignmentOptions.TopLeft;
                case TextAlignmentOptions.MidlineLeft: return TextAlignmentOptions.MidlineRight;
                case TextAlignmentOptions.MidlineRight: return TextAlignmentOptions.MidlineLeft;
                case TextAlignmentOptions.BottomLeft: return TextAlignmentOptions.BottomRight;
                case TextAlignmentOptions.BottomRight: return TextAlignmentOptions.BottomLeft;
                case TextAlignmentOptions.BaselineLeft: return TextAlignmentOptions.BaselineRight;
                case TextAlignmentOptions.BaselineRight: return TextAlignmentOptions.BaselineLeft;
                default: return alignment;
            }
        }

        /// <summary>Mirrors an x position inside a container of <paramref name="width"/> for RTL.</summary>
        public static float MirrorX(float x, float itemWidth, float width, bool rightToLeft) =>
            rightToLeft ? width - x - itemWidth : x;
    }
}
