using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// A visible Image with no sprite and a default white or grey tint. Those quads
    /// read as empty boxes in the briefing and side panels.
    /// </summary>
    public static class SkirmishLibraryBlankImage
    {
        public static bool IsBlank(Image image)
        {
            if (image == null || !image.enabled || !image.gameObject.activeInHierarchy)
                return false;
            if (image.sprite != null || image.color.a <= 0.05f)
                return false;
            Color color = image.color;
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            return max >= 0.45f && (max - min) <= 0.08f;
        }
    }
}
