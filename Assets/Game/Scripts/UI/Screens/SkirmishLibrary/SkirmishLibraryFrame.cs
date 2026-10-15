using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Shared theatre frame. The briefing column sits on the left in left-to-right
    /// layouts. The panels fill the canvas, and a squarer canvas gives the column
    /// less width so the map stays dominant.
    /// </summary>
    public static class SkirmishLibraryFrame
    {
        public const float OriginX = 14f;
        public const float HeaderBand = 108f;
        public const float FooterBand = 112f;
        public const float Gap = 14f;
        public const float NarrowAspect = 1.55f;
        /// <summary>Theatre pulls the map across the gap and 2px into the briefing so the seam does not show.</summary>
        public const float TheatreSeamOverlap = 2f;

        public static void Resolve(
            float width,
            float height,
            bool briefingOnLeft,
            out float mapX,
            out float mapWidth,
            out float briefingX,
            out float briefingWidth,
            out float panelY,
            out float panelHeight)
        {
            if (width < 200f)
                width = 1672f;
            if (height < 200f)
                height = 941f;
            panelY = HeaderBand;
            panelHeight = Mathf.Max(480f, height - HeaderBand - FooterBand);
            bool narrow = width / Mathf.Max(1f, height) < NarrowAspect;
            float share = narrow ? 0.26f : 0.32f;
            float minBriefing = narrow ? 280f : 460f;
            float maxBriefing = narrow ? 380f : 600f;
            briefingWidth = Mathf.Clamp(width * share, minBriefing, maxBriefing);
            mapWidth = width - OriginX * 2f - Gap - briefingWidth;
            if (mapWidth < 480f)
            {
                mapWidth = 480f;
                briefingWidth = Mathf.Max(240f, width - OriginX * 2f - Gap - mapWidth);
            }

            if (briefingOnLeft)
            {
                briefingX = OriginX;
                mapX = OriginX + briefingWidth + Gap;
            }
            else
            {
                mapX = OriginX;
                briefingX = OriginX + mapWidth + Gap;
            }
        }

        /// <summary>
        /// Theatre map width is the resolved width plus <see cref="Gap"/> and <see cref="TheatreSeamOverlap"/>.
        /// Battlefield map and grid keep the resolved gap.
        /// </summary>
        public static void CloseTheatreSeam(
            bool theatre,
            bool briefingOnLeft,
            ref float mapX,
            ref float mapWidth,
            ref float briefingX,
            float briefingWidth)
        {
            if (!theatre)
                return;
            if (briefingOnLeft)
            {
                mapX = briefingX + briefingWidth - TheatreSeamOverlap;
                mapWidth += Gap + TheatreSeamOverlap;
                return;
            }

            mapWidth += Gap + TheatreSeamOverlap;
            briefingX = mapX + mapWidth - TheatreSeamOverlap;
        }
    }
}
