using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Layout rules for the base-alert row (designer retest 2026-11-18, S002): two two-line
    /// controls (status heading over a detail line, the base-strip style) sized to their text,
    /// centred between the selection panel and the ARIA panel. Pure; EditMode tested.
    /// </summary>
    public static class SkirmishBaseAlertRowLayout
    {
        public const float RowHeight = 80f;
        public const float Spacing = 8f;
        /// <summary>HudIconButtonView label insets: 76 on the icon side, 12 on the far side.</summary>
        public const float LabelInsets = 88f;
        /// <summary>Breathing room so a label never touches its border.</summary>
        public const float TextPadding = 16f;
        public const float MinButtonWidth = 240f;
        /// <summary>Width per side kept clear for the selection panel (left) and the ARIA panel (right) on the 1080-high canvas.</summary>
        public const float SidePanelReserve = 490f;

        public static float MaxRowWidth(float canvasWidth) =>
            Mathf.Max(2f * MinButtonWidth + Spacing, canvasWidth - 2f * SidePanelReserve);

        public static float ButtonWidth(float textWidth) =>
            Mathf.Max(MinButtonWidth, Mathf.Ceil(textWidth) + LabelInsets + TextPadding);

        /// <summary>
        /// Widths for the focus control and (when shown) the defender control. Each fits its widest
        /// line; if the pair is wider than the free space they shrink proportionally and
        /// <paramref name="fits"/> is false.
        /// </summary>
        public static Vector2 Fit(float focusText, float defenderText, bool defenderShown, float canvasWidth, out bool fits)
        {
            float focus = ButtonWidth(focusText);
            float defender = defenderShown ? ButtonWidth(defenderText) : 0f;
            float total = focus + (defenderShown ? Spacing + defender : 0f);
            float available = MaxRowWidth(canvasWidth);
            fits = total <= available;
            if (fits) return new Vector2(focus, defender);
            float scale = (available - (defenderShown ? Spacing : 0f)) / (focus + defender);
            return new Vector2(focus * scale, defender * scale);
        }

        public static float RowWidth(Vector2 widths, bool defenderShown) =>
            widths.x + (defenderShown ? Spacing + widths.y : 0f);

        /// <summary>
        /// Holds a downward offset (distance from the top) while <paramref name="active"/>: it may move
        /// down when something new appears above it, never back up until the alert ends. This keeps the
        /// alert controls still when the top banner expires.
        /// </summary>
        public static float HoldDown(float current, ref float held, bool active)
        {
            if (!active) { held = float.NaN; return current; }
            held = float.IsNaN(held) ? current : Mathf.Max(held, current);
            return held;
        }
    }

    /// <summary>
    /// One readable hit at a time: a brief impact ring at the base hit point and, when the attacker
    /// is visible, a shot line from it. Starts on each new hit event; lasts <see cref="Seconds"/>.
    /// </summary>
    public struct SkirmishBaseHitCue
    {
        public const float Seconds = .7f;
        private int lastEvent;
        private float startedAt;
        private bool running;

        /// <summary>Returns the cue progress 0..1 while it runs, or -1.</summary>
        public float Observe(int hitEventId, float now)
        {
            if (hitEventId > 0 && hitEventId != lastEvent)
            {
                lastEvent = hitEventId; startedAt = now; running = true;
            }
            if (!running) return -1f;
            float t = (now - startedAt) / Seconds;
            if (t < 0f || t >= 1f) { running = false; return -1f; }
            return t;
        }

        public void Reset() => this = default;
    }
}
