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
        public const float ReferenceHeight = TheatreSeatGeometry.SagaReferenceHeight;

        // The shell's CanvasScaler uses a 4800x2160 coordinate space. Keep authored
        // control sizes in the same 1080-high frame at every canvas density.
        public static void FitComposition(RectTransform composition)
        {
            if (composition == null || composition.parent is not RectTransform parent
                || parent.rect.width < 2f || parent.rect.height < 2f)
                return;
            TheatreSeatGeometry.ResolveSagaFrame(parent.rect.width, parent.rect.height, out float width, out float height, out float scale);
            composition.anchorMin = composition.anchorMax = new Vector2(.5f, .5f);
            composition.pivot = new Vector2(.5f, .5f);
            composition.sizeDelta = new Vector2(width, height);
            composition.localRotation = Quaternion.identity;
            composition.localScale = new Vector3(scale, scale, 1f);
            composition.anchoredPosition = Vector2.zero;
        }

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
            out float panelHeight,
            bool theatre = false)
        {
            if (width < 200f)
                width = 1672f;
            if (height < 200f)
                height = 941f;
            panelY = HeaderBand;
            panelHeight = Mathf.Max(480f, height - HeaderBand - FooterBand);
            bool narrow = width / Mathf.Max(1f, height) < NarrowAspect;
            float share = 0.26f;
            float minBriefing = 280f;
            float maxBriefing = narrow ? 380f : 560f;
            briefingWidth = theatre ? TheatreSeatGeometry.SagaSummaryWidth(width,height) : Mathf.Clamp(width * share, minBriefing, maxBriefing);
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
