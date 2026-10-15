using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// A visible graphic with no artwork and a flat white or grey tint. Those quads
    /// read as empty boxes over the briefing and the side panel.
    /// </summary>
    public static class SkirmishLibraryBlankImage
    {
        public const float MinimumSpan = 64f;

        public static bool IsBlank(Image image)
        {
            if (!IsShown(image) || image.color.a <= 0.05f || !IsLightNeutral(image.color))
                return false;
            if (image.sprite == null)
                return IsLarge(image.rectTransform);
            string spriteName = image.sprite.name ?? string.Empty;
            bool defaultQuad = spriteName == "Background" || spriteName == "UISprite" || spriteName == "Knob";
            return defaultQuad && IsLarge(image.rectTransform);
        }

        public static bool IsBlank(RawImage image)
        {
            if (!IsShown(image) || image.texture != null || image.color.a <= 0.05f)
                return false;
            return IsLightNeutral(image.color) && IsLarge(image.rectTransform);
        }

        public static bool IsLightNeutral(Color color)
        {
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            return max >= 0.45f && (max - min) <= 0.08f;
        }

        public static bool IsLarge(RectTransform rect)
        {
            if (rect == null)
                return false;
            Rect size = rect.rect;
            float width = size.width > 2f ? size.width : rect.sizeDelta.x;
            float height = size.height > 2f ? size.height : rect.sizeDelta.y;
            return width >= MinimumSpan && height >= MinimumSpan;
        }

        private static bool IsShown(Graphic graphic)
        {
            return graphic != null && graphic.enabled && graphic.gameObject.activeInHierarchy;
        }
    }
}
