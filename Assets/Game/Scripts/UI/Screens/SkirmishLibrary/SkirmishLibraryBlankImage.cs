using TMPro;
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

        public static bool IsNarrowBar(Image image)
        {
            if (!IsShown(image))
                return false;
            Measure(image.rectTransform, out float width, out float height);
            return width < 34f && height > 56f && height > width * 2.2f;
        }

        public static bool IsMedalArt(Image image)
        {
            if (image == null)
                return false;
            string spriteName = image.sprite != null ? image.sprite.name : string.Empty;
            if (spriteName.IndexOf("medal", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            string name = image.name ?? string.Empty;
            if (name.StartsWith("Medal", System.StringComparison.Ordinal))
                return true;
            Transform parent = image.transform.parent;
            return name == "Icon" && parent != null &&
                (parent.name.StartsWith("Slot", System.StringComparison.Ordinal) ||
                 parent.name.StartsWith("Medal", System.StringComparison.Ordinal));
        }

        public static bool IsGreyTint(Image image)
        {
            if (image == null)
                return false;
            Color color = image.color;
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            float luminance = (color.r + color.g + color.b) / 3f;
            return color.a > 0.35f && (max - min) <= 0.16f && luminance >= 0.18f && luminance <= 0.92f;
        }

        public static bool OverlapsText(RectTransform rect, TMP_Text text)
        {
            if (rect == null || text == null || !text.gameObject.activeInHierarchy || !text.enabled)
                return false;
            if (string.IsNullOrEmpty(text.text))
                return false;
            return WorldRect(rect).Overlaps(WorldRect(text.rectTransform));
        }

        private static void Measure(RectTransform rect, out float width, out float height)
        {
            width = 0f;
            height = 0f;
            if (rect == null)
                return;
            Rect size = rect.rect;
            width = Mathf.Abs(size.width) > 0.5f ? Mathf.Abs(size.width) : Mathf.Abs(rect.sizeDelta.x);
            height = Mathf.Abs(size.height) > 0.5f ? Mathf.Abs(size.height) : Mathf.Abs(rect.sizeDelta.y);
        }

        private static Rect WorldRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float minX = corners[0].x;
            float maxX = corners[0].x;
            float minY = corners[0].y;
            float maxY = corners[0].y;
            for (int i = 1; i < corners.Length; i++)
            {
                minX = Mathf.Min(minX, corners[i].x);
                maxX = Mathf.Max(maxX, corners[i].x);
                minY = Mathf.Min(minY, corners[i].y);
                maxY = Mathf.Max(maxY, corners[i].y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static bool IsShown(Graphic graphic)
        {
            return graphic != null && graphic.enabled && graphic.gameObject.activeInHierarchy;
        }
    }
}
