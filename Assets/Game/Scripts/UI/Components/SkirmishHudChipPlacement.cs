using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Keeps world-anchored mission chips (rendezvous, enemy base, public objective labels)
    /// inside the open play area, clear of the HUD. The play area is derived from the
    /// screen rectangles of the HUD controls that actually block taps (the same raycast
    /// targets that make a world point unreachable), so the rule follows the live layout in
    /// left-to-right and right-to-left locales without hardcoded pixels. Pure and
    /// EditMode-testable; all rectangles are screen pixels with y up.
    /// </summary>
    public static class SkirmishHudChipPlacement
    {
        /// <summary>HUD chrome is docked to the screen edges; a rect farther than this
        /// fraction of the screen from every edge is a world-tracking overlay, not chrome.</summary>
        public const float DockedFraction = 1f / 3f;

        /// <summary>Rects larger than this fraction of the screen are full-screen catchers
        /// or outlines, not controls the chip could avoid.</summary>
        public const float MaximumOccluderFraction = .4f;

        /// <summary>
        /// True when <paramref name="graphic"/> would take a tap at <paramref name="screenPoint"/>,
        /// by the same rules GraphicRaycaster applies: active, raycast target, not culled, and
        /// accepted by every raycast filter above it. Shell screens and popups stay active while
        /// hidden (CanvasGroup alpha 0, blocksRaycasts false); they are not HUD.
        /// </summary>
        public static bool BlocksTaps(Graphic graphic, Vector2 screenPoint, Camera eventCamera) =>
            graphic != null && graphic.isActiveAndEnabled && graphic.raycastTarget &&
            !graphic.canvasRenderer.cull && graphic.Raycast(screenPoint, eventCamera);

        /// <summary>True when <paramref name="rect"/> is HUD chrome a chip must avoid.</summary>
        public static bool IsHudOccluder(Rect rect, Rect screen)
        {
            if (rect.width <= 0 || rect.height <= 0 || screen.width <= 0 || screen.height <= 0) return false;
            if (!rect.Overlaps(screen)) return false;
            if (rect.width * rect.height > screen.width * screen.height * MaximumOccluderFraction) return false;
            float horizontal = Mathf.Min(rect.xMin - screen.xMin, screen.xMax - rect.xMax) / screen.width;
            float vertical = Mathf.Min(rect.yMin - screen.yMin, screen.yMax - rect.yMax) / screen.height;
            return Mathf.Min(horizontal, vertical) <= DockedFraction;
        }

        /// <summary>Reference points tried after the requested one, as screen fractions.</summary>
        private static readonly Vector2[] FallbackReferences =
        {
            new(.5f, .5f), new(.35f, .5f), new(.65f, .5f), new(.5f, .35f), new(.5f, .65f),
            new(.25f, .4f), new(.75f, .4f), new(.25f, .6f), new(.75f, .6f), new(.5f, .25f), new(.5f, .75f),
        };

        /// <summary>
        /// The largest rectangle inside <paramref name="screen"/> that overlaps no HUD occluder,
        /// grown around <paramref name="reference"/> (normally the screen centre) and, when that
        /// point is covered or leaves less room, around a spread of other open points. Returns
        /// false only when every reference point is under the HUD.
        /// </summary>
        public static bool TrySafePlayArea(Rect screen, IReadOnlyList<Rect> hud, Vector2 reference, out Rect area)
        {
            area = default;
            if (screen.width <= 0 || screen.height <= 0) return false;
            var occluders = new List<Rect>();
            if (hud != null)
                foreach (var rect in hud)
                    if (IsHudOccluder(rect, screen)) occluders.Add(rect);
            float best = 0;
            if (screen.Contains(reference) && TryAreaAround(screen, occluders, reference, out var around))
            { area = around; best = around.width * around.height; }
            foreach (var fraction in FallbackReferences)
            {
                var point = new Vector2(screen.xMin + screen.width * fraction.x, screen.yMin + screen.height * fraction.y);
                if (!TryAreaAround(screen, occluders, point, out around)) continue;
                float size = around.width * around.height;
                if (size > best * 1.0001f) { best = size; area = around; }
            }
            return best > 0;
        }

        private static bool TryAreaAround(Rect screen, List<Rect> occluders, Vector2 reference, out Rect area)
        {
            area = default;
            var lows = new List<float> { screen.yMin };
            var highs = new List<float> { screen.yMax };
            foreach (var rect in occluders)
            {
                if (rect.xMin < reference.x && rect.xMax > reference.x && rect.yMin < reference.y && rect.yMax > reference.y)
                    return false; // This reference point is under the HUD.
                if (rect.yMax <= reference.y && rect.yMax > screen.yMin) lows.Add(rect.yMax);
                if (rect.yMin >= reference.y && rect.yMin < screen.yMax) highs.Add(rect.yMin);
            }

            float best = 0;
            foreach (float low in lows)
            foreach (float high in highs)
            {
                if (high <= low) continue;
                float left = screen.xMin, right = screen.xMax;
                bool valid = true;
                foreach (var rect in occluders)
                {
                    if (rect.yMax <= low || rect.yMin >= high) continue; // Not in this band.
                    if (rect.xMax <= reference.x) left = Mathf.Max(left, rect.xMax);
                    else if (rect.xMin >= reference.x) right = Mathf.Min(right, rect.xMin);
                    else { valid = false; break; }
                }
                if (!valid || right <= left) continue;
                float size = (right - left) * (high - low);
                if (size > best) { best = size; area = Rect.MinMaxRect(left, low, right, high); }
            }
            return best > 0;
        }

        /// <summary>
        /// Like <see cref="TryPlace"/>, but never refuses: when the play area is unknown
        /// (<paramref name="areaKnown"/> false) or too small for the chip and its cue, the chip is
        /// clamped into the play area without the cue margin, then into <paramref name="screen"/>.
        /// A world-anchored chip whose anchor is on screen is always shown.
        /// </summary>
        public static Vector2 PlaceVisible(Vector2 anchor, Vector2 size, Vector2 pivot, Rect area, bool areaKnown, Rect screen,
            float cueMargin, out bool clamped)
        {
            if (areaKnown && TryPlace(anchor, size, pivot, area, cueMargin, out var position, out clamped)) return position;
            if (areaKnown && TryPlace(anchor, size, pivot, area, 0, out position, out clamped)) return position;
            float x = size.x <= screen.width
                ? Mathf.Clamp(anchor.x, screen.xMin + size.x * pivot.x, screen.xMax - size.x * (1 - pivot.x)) : screen.center.x;
            float y = size.y <= screen.height
                ? Mathf.Clamp(anchor.y, screen.yMin + size.y * pivot.y, screen.yMax - size.y * (1 - pivot.y)) : screen.center.y;
            position = new Vector2(x, y);
            clamped = (position - anchor).sqrMagnitude > .01f;
            return position;
        }

        /// <summary>
        /// Places a chip of <paramref name="size"/> whose <paramref name="pivot"/> wants to sit
        /// on <paramref name="anchor"/>. When the anchor lies in <paramref name="area"/> and the
        /// whole chip fits there, it sits on the anchor (<paramref name="clamped"/> false).
        /// Otherwise it is clamped inside the area inset by <paramref name="cueMargin"/>, which
        /// leaves room for a direction cue beside the chip. Returns false when the chip cannot
        /// fit at all (the caller hides it).
        /// </summary>
        public static bool TryPlace(Vector2 anchor, Vector2 size, Vector2 pivot, Rect area, float cueMargin,
            out Vector2 position, out bool clamped)
        {
            position = anchor;
            clamped = false;
            if (area.width <= 0 || area.height <= 0) return false;
            if (area.Contains(anchor) && Inside(ChipRect(anchor, size, pivot), area)) return true;
            var inset = Rect.MinMaxRect(area.xMin + cueMargin, area.yMin + cueMargin, area.xMax - cueMargin, area.yMax - cueMargin);
            if (inset.width < size.x || inset.height < size.y) return false;
            float x = Mathf.Clamp(anchor.x, inset.xMin + size.x * pivot.x, inset.xMax - size.x * (1 - pivot.x));
            float y = Mathf.Clamp(anchor.y, inset.yMin + size.y * pivot.y, inset.yMax - size.y * (1 - pivot.y));
            position = new Vector2(x, y);
            clamped = true;
            return true;
        }

        /// <summary>The screen rect a chip occupies when its pivot sits at <paramref name="position"/>.</summary>
        public static Rect ChipRect(Vector2 position, Vector2 size, Vector2 pivot) =>
            new(position.x - size.x * pivot.x, position.y - size.y * pivot.y, size.x, size.y);

        /// <summary>
        /// A direction cue just outside <paramref name="chip"/>, on the side facing
        /// <paramref name="anchor"/>, at half of <paramref name="cueMargin"/> from the chip edge.
        /// <paramref name="degrees"/> is the screen direction toward the anchor (0 = right,
        /// counter-clockwise), matching <see cref="HudDirectionArrowGraphic"/>.
        /// </summary>
        public static Vector2 CuePoint(Rect chip, Vector2 anchor, float cueMargin, out float degrees)
        {
            Vector2 centre = chip.center;
            Vector2 delta = anchor - centre;
            if (delta.sqrMagnitude < 1e-4f) delta = Vector2.up;
            Vector2 direction = delta.normalized;
            degrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float hx = chip.width * .5f, hy = chip.height * .5f;
            float toEdge = Mathf.Min(
                Mathf.Abs(direction.x) > 1e-4f ? hx / Mathf.Abs(direction.x) : float.MaxValue,
                Mathf.Abs(direction.y) > 1e-4f ? hy / Mathf.Abs(direction.y) : float.MaxValue);
            Vector2 edge = centre + direction * toEdge;
            // Step out perpendicular to the edge that was hit so the cue stays in the margin band.
            Vector2 outward = Mathf.Abs(edge.x - chip.xMin) < .01f ? Vector2.left
                : Mathf.Abs(edge.x - chip.xMax) < .01f ? Vector2.right
                : Mathf.Abs(edge.y - chip.yMin) < .01f ? Vector2.down : Vector2.up;
            return edge + outward * (cueMargin * .5f);
        }

        private static bool Inside(Rect inner, Rect outer) =>
            inner.xMin >= outer.xMin && inner.xMax <= outer.xMax && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
    }
}
