using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Runtime
{
    public enum TutorialPointerSide { Above, Below, Left, Right, AboveLeft, AboveRight, BelowLeft, BelowRight }

    // All inputs are screen pixels, so canvas scale, cutouts and orientation use
    // the same bounds. Reserve the complete bounce envelope, not just one frame.
    public static class TutorialTapPointerLayout
    {
        public static bool TryPlace(Rect target, Rect safe, Vector2 captionSize, float size,
            float gap, float travel, IReadOnlyList<Rect> obstacles,
            out Rect arrow, out Rect caption, out TutorialPointerSide side)
        {
            foreach (TutorialPointerSide candidate in new[] { TutorialPointerSide.Above, TutorialPointerSide.Below, TutorialPointerSide.Left, TutorialPointerSide.Right })
            {
                Vector2 direction = Outward(candidate);
                float targetExtent = (candidate == TutorialPointerSide.Above || candidate == TutorialPointerSide.Below) ? target.height / 2 : target.width / 2;
                Vector2 center = target.center + direction * (targetExtent + gap + size / 2 + travel);
                Rect pointer = new(center - Vector2.one * size / 2, Vector2.one * size);
                float labelExtent = (candidate == TutorialPointerSide.Above || candidate == TutorialPointerSide.Below) ? captionSize.y / 2 : captionSize.x / 2;
                Vector2 labelCenter = center + direction * (size / 2 + travel + gap + labelExtent);
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
            // A bottom-corner action can have a minimap above it and other actions beside it.
            // Approach diagonally through the open world area, retaining the full bounce envelope.
            foreach (var candidate in new[] { TutorialPointerSide.AboveLeft, TutorialPointerSide.AboveRight,
                         TutorialPointerSide.BelowLeft, TutorialPointerSide.BelowRight })
            {
                var direction = Outward(candidate);
                var signs = new Vector2(Mathf.Sign(direction.x), Mathf.Sign(direction.y));
                var corner = target.center + Vector2.Scale(target.size * .5f, signs);
                // A minimap above and a feedback banner beside the button can
                // require clearance on BOTH axes. Search nearest offsets first.
                for (int reach = 0; reach <= 12; reach++)
                for (int xReach = 0; xReach <= 6; xReach++)
                {
                    int yReach = reach - xReach;
                    if (yReach < 0 || yReach > 6) continue;
                    var halfDiagonal = size * .707107f;
                    var offset = Vector2.one * (halfDiagonal + gap + travel);
                    offset += new Vector2(xReach, yReach) * size;
                    var center = corner + Vector2.Scale(offset, signs);
                    var pointer = new Rect(center - Vector2.one * size * .5f, Vector2.one * size);
                    var envelope = new Rect(center - Vector2.one * (halfDiagonal + travel),
                        Vector2.one * (halfDiagonal + travel) * 2);
                    if (!Fits(envelope, safe, obstacles)) continue;
                    for (int labelAxis = 0; labelAxis < 2; labelAxis++)
                    {
                        var labelCenter = center;
                        if (labelAxis == 0) labelCenter.x += signs.x * (halfDiagonal + travel + gap + captionSize.x * .5f);
                        else labelCenter.y += signs.y * (halfDiagonal + travel + gap + captionSize.y * .5f);
                        var label = new Rect(labelCenter - captionSize * .5f, captionSize);
                        if (captionSize.sqrMagnitude > 0 &&
                            (!Fits(label, safe, obstacles) || label.Overlaps(envelope) || label.Overlaps(target))) continue;
                        arrow = pointer; caption = label; side = candidate; return true;
                    }
                }
            }
            arrow = caption = default; side = default; return false;
        }

        public static Vector2 Outward(TutorialPointerSide side) => side switch
        {
            TutorialPointerSide.Above => Vector2.up,
            TutorialPointerSide.Below => Vector2.down,
            TutorialPointerSide.Left => Vector2.left,
            TutorialPointerSide.Right => Vector2.right,
            TutorialPointerSide.AboveLeft => new Vector2(-1, 1).normalized,
            TutorialPointerSide.AboveRight => new Vector2(1, 1).normalized,
            TutorialPointerSide.BelowLeft => new Vector2(-1, -1).normalized,
            _ => new Vector2(1, -1).normalized
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
