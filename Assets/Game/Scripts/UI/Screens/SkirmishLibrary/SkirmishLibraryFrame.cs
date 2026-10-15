using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Shared theatre frame. The briefing column sits on the left in left-to-right
    /// layouts. A squarer canvas gives that column less width so the map stays dominant.
    /// </summary>
    public static class SkirmishLibraryFrame
    {
        public const float OriginX = 14f;
        public const float PanelY = 108f;
        public const float PanelHeight = 688f;
        public const float Gap = 14f;
        public const float WideMapWidth = 1040f;
        public const float WideBriefingWidth = 590f;
        public const float NarrowBriefingWidth = 400f;
        public const float NarrowAspect = 1.55f;

        public static void Resolve(
            float canvasAspect,
            float extraWidth,
            bool briefingOnLeft,
            out float mapX,
            out float mapWidth,
            out float briefingX,
            out float briefingWidth)
        {
            bool narrow = canvasAspect > 0.01f && canvasAspect < NarrowAspect;
            briefingWidth = narrow ? NarrowBriefingWidth : WideBriefingWidth;
            float content = WideMapWidth + WideBriefingWidth + Gap + Mathf.Max(0f, extraWidth);
            mapWidth = Mathf.Max(720f, content - briefingWidth - Gap);
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
    }
}
