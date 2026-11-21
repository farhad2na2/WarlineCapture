using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Owner-approved W39 spacing for the match HUD left column (Board 1) and the ARIA panel
    /// (Board 3). Values are pixels on the 1920x1080 reference screen; the HUD sections are
    /// authored on a 1672x941 reference that scales by height, so one HUD unit is 1080/941 px
    /// at both 1920x1080 and 2400x1080. Presentation only.
    /// </summary>
    public static class MatchHudSidePanelLayout
    {
        public const float ReferenceScreenHeight = 1080f;
        public const float HudReferenceHeight = 941f;

        public const float SafeInsetPx = 16f;          // left safe inset of card, minimap and first category card
        public const float ColumnGapPx = 24f;           // card -> minimap and minimap -> categories
        public const float CategoryGapPx = 8f;          // between category cards
        public const float CommandStripGapPx = 12f;     // last category card -> command strip (minimum)
        public const float BottomSafePx = 16f;          // category cards and command buttons above the bottom edge
        public const float CardPaddingPx = 16f;         // selected-unit card inner padding
        public const float CardSectionGapPx = 12f;      // between selected-unit card sections
        public const float MinimumTargetPx = 64f;       // every interactive target
        public const float AriaInsetPx = 16f;           // ARIA panel inner inset
        public const float AriaGapPx = 12f;             // ARIA vertical gaps

        public static float Units(float referencePixels) => referencePixels * HudReferenceHeight / ReferenceScreenHeight;
        public static float Pixels(float hudUnits) => hudUnits * ReferenceScreenHeight / HudReferenceHeight;

        /// <summary>The minimap sits one column gap above the top of the category cards.</summary>
        public static float MinimapBottom(float categoryCardsTop, float fallback) =>
            float.IsNaN(categoryCardsTop) || float.IsInfinity(categoryCardsTop) ? fallback : categoryCardsTop + Units(ColumnGapPx);

        /// <summary>Largest selected-unit card height that keeps one column gap above the minimap.</summary>
        public static float SelectionHeightLimit(float panelTop, float minimapTop) =>
            Mathf.Max(80f, panelTop - minimapTop - Units(ColumnGapPx));
    }

    /// <summary>ARIA panel states. Stopped, ActiveOneAction and ActiveTwoActions are the approved Board 3 states.</summary>
    public enum AriaPanelState : byte { HeaderOnly, GuidanceWithoutPlay, Stopped, ActiveNoAction, ActiveOneAction, ActiveTwoActions }

    public static class AriaPanelStates
    {
        /// <summary>Maps the visible ARIA content and controls to a panel state.</summary>
        public static AriaPanelState Classify(bool hasGuidance, int guidanceActions, bool playStopVisible, bool ariaActive)
        {
            if (!playStopVisible) return hasGuidance || guidanceActions > 0 ? AriaPanelState.GuidanceWithoutPlay : AriaPanelState.HeaderOnly;
            if (!ariaActive) return AriaPanelState.Stopped;
            return guidanceActions >= 2 ? AriaPanelState.ActiveTwoActions
                : guidanceActions == 1 ? AriaPanelState.ActiveOneAction : AriaPanelState.ActiveNoAction;
        }
    }
}
