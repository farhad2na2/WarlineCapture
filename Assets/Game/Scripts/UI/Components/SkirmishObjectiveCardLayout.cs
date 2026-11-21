using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public enum HudOwnership : byte { Neutral, Friendly, Enemy }

    /// <summary>
    /// Layout and colour rules for the compact Skirmish objective card (W40, Boards 2 and 4; owner
    /// approval 2026-11-20). Values are canvas units on the 1920x1080 reference, where one unit is one
    /// pixel at 1080 height. Presentation only: no objective data, timers, tickets or gameplay.
    /// </summary>
    public static class SkirmishObjectiveCardLayout
    {
        // Card: one charcoal card with a warm keyline, 12 px under the resource bar, 16 px padding.
        public const float Padding = 16f, RowGap = 8f, ChipGap = 12f;
        public const float HeaderHeight = 72f, ControlHeight = 72f;
        public const float KeylineWidth = 3f, BelowResources = 12f, BelowWarning = 12f, DefaultTop = 105f;
        public const float MinWidth = 560f, CentreX = .515f, MaxWidthFraction = .43f;
        public const float Height = Padding * 2f + HeaderHeight + RowGap + ControlHeight;

        // Purpose / next-action row: amber objective glyph, purpose title, next action, then the timer.
        public const float GlyphSize = 40f, GlyphGap = 12f, TimerMinWidth = 96f, TimerPadding = 8f;
        public const float PurposeSize = 24f, NextActionMaxSize = 17f, NextActionMinSize = 12f;
        public const float CaptionSize = 15f, TimerSize = 30f, ScoreSize = 18f, ScoreMinSize = 12f;
        public const float PurposeSplit = .5f;

        // Status chips: camera icon and title on the first line, ownership (glyph + word) on its own line.
        public const float ChipInset = 12f, ChipIcon = 30f, ChipIconGap = 6f, OwnershipGlyphSize = 18f, ChipGlyphGap = 6f;
        public const float ChipMinWidth = 128f, ChipMaxWidth = 300f, ScorePadding = 12f, ScoreMinWidth = 120f, ScoreMaxWidth = 260f;
        public const float TitleMaxSize = 22f, TitleMinSize = 14f, DetailMaxSize = 20f, DetailMinSize = 12f, TagSize = 15f, TagGap = 6f;
        public const float CaptureBarHeight = 4f;

        public static readonly Color Keyline = new Color32(0xE7, 0x56, 0x2C, 0xFF);
        public static readonly Color CardTop = new Color32(25, 34, 38, 247);
        public static readonly Color CardBottom = new Color32(3, 8, 10, 252);
        public static readonly Color ObjectiveAmber = new Color32(243, 174, 0, 255);
        public static readonly Color Friendly = new Color32(0x49, 0xBD, 0x62, 0xFF);
        public static readonly Color Enemy = new Color32(0xF4, 0x5C, 0x3B, 0xFF);
        public static readonly Color Neutral = new Color32(0xBB, 0xC7, 0xCD, 0xFF);
        public static readonly Color DetailText = new Color32(230, 237, 239, 255);
        public static readonly Color MutedText = new Color32(170, 186, 192, 255);
        public static readonly Color StatusTag = new Color32(255, 196, 64, 255);

        /// <summary>Owner 0 is neutral (as the zone readout already treats it); otherwise own or opposing.</summary>
        public static HudOwnership Resolve(int ownerFaction, int observerFaction) =>
            ownerFaction == 0 ? HudOwnership.Neutral : ownerFaction == observerFaction ? HudOwnership.Friendly : HudOwnership.Enemy;

        public static Color Colour(HudOwnership owner) =>
            owner == HudOwnership.Friendly ? Friendly : owner == HudOwnership.Enemy ? Enemy : Neutral;

        public static HudOwnershipGlyph Glyph(HudOwnership owner) =>
            owner == HudOwnership.Friendly ? HudOwnershipGlyph.Shield : owner == HudOwnership.Enemy ? HudOwnershipGlyph.Diamond : HudOwnershipGlyph.Ring;

        public static HudButtonRole Role(HudOwnership owner) =>
            owner == HudOwnership.Friendly ? HudButtonRole.Friendly : owner == HudOwnership.Enemy ? HudButtonRole.Enemy : HudButtonRole.Unowned;

        /// <summary>Existing localized family names (Battle Library copy), used as the card's purpose line.</summary>
        public static string Purpose(SkirmishObjectiveKind kind, bool persian) => kind switch
        {
            SkirmishObjectiveKind.FrontlineControl => persian ? "کنترل خط" : "FRONTLINE CONTROL",
            SkirmishObjectiveKind.Breakthrough => persian ? "شکستن خط" : "BREAKTHROUGH",
            SkirmishObjectiveKind.ConvoyEscort => persian ? "اسکورت کاروان" : "CONVOY ESCORT",
            _ => persian ? "حمله به پایگاه" : "BASE ASSAULT"
        };

        /// <summary>ARIA panel heading (W42): Frontline Control, Breakthrough and Convoy Escort show their own
        /// family name (the same Battle Library copy as the card's purpose line). Base Assault, and any session
        /// without a known family, keeps the localized Base Assault title it showed before.</summary>
        public static string AriaHeading(SkirmishObjectiveKind kind, bool persian, string baseAssaultTitle) =>
            kind is SkirmishObjectiveKind.FrontlineControl or SkirmishObjectiveKind.Breakthrough or SkirmishObjectiveKind.ConvoyEscort
                ? Purpose(kind, persian)
                : baseAssaultTitle;

        /// <summary>The family's existing objective sentence (SkirmishObjectiveCopyTable) for the player's
        /// own side; null for Base Assault, which keeps the session's objective line.</summary>
        public static string ObjectiveCopyKey(SkirmishObjectiveKind kind, SkirmishPublicGoalPerspective perspective) =>
            kind == SkirmishObjectiveKind.FrontlineControl && perspective == SkirmishPublicGoalPerspective.Capture ? "skirmish.s007.objective" :
            kind == SkirmishObjectiveKind.Breakthrough && perspective == SkirmishPublicGoalPerspective.Evacuate ? "skirmish.s013.objective" :
            kind == SkirmishObjectiveKind.ConvoyEscort && perspective == SkirmishPublicGoalPerspective.Escort ? "skirmish.s019.objective" : null;

        /// <summary>Width a chip needs so its title line (icon, title, optional status tag) and its
        /// ownership line (optional glyph, word) both fit on one line each.</summary>
        public static float ChipWidth(float title, float detail, bool glyph, float tag = 0f)
        {
            float titleLine = ChipInset + ChipIcon + ChipIconGap + title + (tag > 0f ? TagGap + tag : 0f) + ChipInset;
            float detailLine = ChipInset + (glyph ? OwnershipGlyphSize + ChipGlyphGap : 0f) + detail + ChipInset;
            return Mathf.Clamp(Mathf.Ceil(Mathf.Max(titleLine, detailLine)), ChipMinWidth, ChipMaxWidth);
        }

        public static float ScoreWidth(float text) => Mathf.Clamp(Mathf.Ceil(text + 2f * ScorePadding), ScoreMinWidth, ScoreMaxWidth);

        public static float TimerWidth(float caption, float value) => Mathf.Max(TimerMinWidth, Mathf.Ceil(Mathf.Max(caption, value) + 2f * TimerPadding));

        /// <summary>Next action wraps onto at most two lines (the header asks for about half its one-line
        /// width); right-to-left copy stays on one line, because the Persian line box is about twice its size.</summary>
        public static float HeaderWidth(float purpose, float nextActionOneLine, float timer, bool wraps = true)
        {
            float next = nextActionOneLine <= 0f ? 0f : wraps ? nextActionOneLine * .56f : nextActionOneLine;
            return GlyphSize + GlyphGap + Mathf.Max(purpose, next) + GlyphGap + timer;
        }

        public static float StatusWidth(float[] widths, int count)
        {
            float total = 0f;
            for (int i = 0; i < count; i++) total += widths[i];
            return total + Mathf.Max(0, count - 1) * ChipGap;
        }

        /// <summary>Clear of the selection card and the ARIA rail at every supported aspect.</summary>
        public static float MaxWidth(float canvasWidth) =>
            Mathf.Max(MinWidth, Mathf.Min(canvasWidth * MaxWidthFraction, canvasWidth - 2f * SkirmishBaseAlertRowLayout.SidePanelReserve));

        public static float CardWidth(float status, float header, float canvasWidth) =>
            Mathf.Clamp(Mathf.Max(MinWidth, status + 2f * Padding, header + 2f * Padding), MinWidth, MaxWidth(canvasWidth));

        /// <summary>Distance from the canvas top: 12 px under the resource bar, or under a visible alert banner.</summary>
        public static float Top(float resourceBottom, float warningBottom) =>
            Mathf.Max(resourceBottom > 0f ? resourceBottom + BelowResources : DefaultTop, warningBottom > 0f ? warningBottom + BelowWarning : 0f);
    }
}
