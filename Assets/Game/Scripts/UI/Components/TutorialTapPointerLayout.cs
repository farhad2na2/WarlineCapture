using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Runtime
{
    public enum TutorialPointerSide { Above, Below, Left, Right }

    // All inputs are screen pixels, so canvas scale, cutouts and orientation use
    // the same bounds. Reserve the complete bounce envelope, not just one frame.
    public static class TutorialTapPointerLayout
    {
        public static bool TryPlace(Rect target, Rect safe, Vector2 captionSize, float size,
            float gap, float travel, IReadOnlyList<Rect> obstacles,
            out Rect arrow, out Rect caption, out TutorialPointerSide side)
        {
            foreach (TutorialPointerSide candidate in System.Enum.GetValues(typeof(TutorialPointerSide)))
            {
                Vector2 direction = Outward(candidate);
                float targetExtent = (candidate == TutorialPointerSide.Above || candidate == TutorialPointerSide.Below) ? target.height / 2 : target.width / 2;
                Vector2 center = target.center + direction * (targetExtent + gap + size / 2 + travel);
                Rect pointer = new(center - Vector2.one * size / 2, Vector2.one * size);
                float labelExtent = (candidate == TutorialPointerSide.Above || candidate == TutorialPointerSide.Below) ? captionSize.y / 2 : captionSize.x / 2;
                Vector2 labelCenter = center + direction * (size / 2 + gap + labelExtent);
                Rect label = new(labelCenter - captionSize / 2, captionSize);
                // Labels may slide along an edge; the arrow always points at the target.
                if ((candidate == TutorialPointerSide.Above || candidate == TutorialPointerSide.Below))
                    label.x = Mathf.Clamp(label.x, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - label.width));
                else label.y = Mathf.Clamp(label.y, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - label.height));
                Rect envelope = pointer;
                if ((candidate == TutorialPointerSide.Above || candidate == TutorialPointerSide.Below)) { envelope.yMin -= travel; envelope.yMax += travel; }
                else { envelope.xMin -= travel; envelope.xMax += travel; }
                if (!Fits(envelope, safe, obstacles) || captionSize.sqrMagnitude > 0 && !Fits(label, safe, obstacles)) continue;
                arrow = pointer; caption = label; side = candidate; return true;
            }
            arrow = caption = default; side = default; return false;
        }

        public static Vector2 Outward(TutorialPointerSide side) => side switch
        {
            TutorialPointerSide.Above => Vector2.up,
            TutorialPointerSide.Below => Vector2.down,
            TutorialPointerSide.Left => Vector2.left,
            _ => Vector2.right
        };

        private static bool Fits(Rect value, Rect safe, IReadOnlyList<Rect> obstacles)
        {
            if (value.xMin < safe.xMin || value.xMax > safe.xMax || value.yMin < safe.yMin || value.yMax > safe.yMax) return false;
            if (obstacles != null)
                for (int i = 0; i < obstacles.Count; i++) if (value.Overlaps(obstacles[i])) return false;
            return true;
        }
    }
}
