using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Gold selection frame: four solid edges plus a soft outer glow band. Re-applying replaces the old frame.</summary>
    public static class SkirmishGlowFrameWidget
    {
        public const string FrameName = "GlowFrame";
        public static readonly string[] EdgeNames = { "Top", "Bottom", "Left", "Right" };

        public static RectTransform Apply(RectTransform target, SkirmishWidgetContext context, Color? color = null,
            float thickness = 3f, float glow = 8f)
        {
            Remove(target);
            float t = context != null ? context.Px(thickness) : thickness;
            float g = context != null ? context.Px(glow) : glow;
            Color edge = color ?? SkirmishWidgetFactory.Gold;
            RectTransform frame = SkirmishWidgetFactory.Rect(FrameName, target);
            SkirmishWidgetFactory.Stretch(frame, -g, -g, -g, -g);
            frame.SetAsLastSibling();

            // Glow: four translucent bands outside the edge.
            Color soft = new Color(edge.r, edge.g, edge.b, 0.28f);
            Band(frame, "GlowTop", soft, new Vector2(0f, 1f), new Vector2(1f, 1f), 0f, 0f, 0f, g, g);
            Band(frame, "GlowBottom", soft, new Vector2(0f, 0f), new Vector2(1f, 0f), 0f, 0f, g, 0f, g);
            Band(frame, "GlowLeft", soft, new Vector2(0f, 0f), new Vector2(0f, 1f), 0f, g, g, g, g);
            Band(frame, "GlowRight", soft, new Vector2(1f, 0f), new Vector2(1f, 1f), g, 0f, g, g, g);

            // Solid edges sit exactly on the target bounds.
            RectTransform edges = SkirmishWidgetFactory.Rect("Edges", frame);
            SkirmishWidgetFactory.Stretch(edges, g, g, g, g);
            Edge(edges, EdgeNames[0], edge, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, t));
            Edge(edges, EdgeNames[1], edge, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, t));
            Edge(edges, EdgeNames[2], edge, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(t, 0f));
            Edge(edges, EdgeNames[3], edge, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(t, 0f));
            return frame;
        }

        public static void Remove(RectTransform target)
        {
            if (target == null) return;
            Transform old = target.Find(FrameName);
            if (old == null) return;
            if (Application.isPlaying) Object.Destroy(old.gameObject);
            else Object.DestroyImmediate(old.gameObject);
        }

        private static void Edge(RectTransform parent, string name, Color color, Vector2 min, Vector2 max, Vector2 size)
        {
            Image image = SkirmishWidgetFactory.Image(name, parent, color);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(min.x == max.x ? min.x : 0.5f, min.y == max.y ? min.y : 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void Band(RectTransform parent, string name, Color color, Vector2 min, Vector2 max,
            float left, float right, float bottom, float top, float g)
        {
            Image image = SkirmishWidgetFactory.Image(name, parent, color);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            if (min.y == max.y)
            {
                rect.sizeDelta = new Vector2(0f, g);
                rect.anchoredPosition = new Vector2(0f, min.y > 0.5f ? -g * 0.5f : g * 0.5f);
            }
            else
            {
                rect.offsetMin = new Vector2(min.x > 0.5f ? -g : 0f, g);
                rect.offsetMax = new Vector2(min.x > 0.5f ? 0f : g, -g);
            }
        }
    }
}
