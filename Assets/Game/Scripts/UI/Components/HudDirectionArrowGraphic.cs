using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Procedural chevron arrow pointing along +X of its RectTransform. Never raycasts.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudDirectionArrowGraphic : MaskableGraphic
    {
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 tip = new(rect.xMax, rect.center.y);
            Vector2 upper = new(rect.xMin, rect.yMax);
            Vector2 notch = new(rect.xMin + rect.width * .34f, rect.center.y);
            Vector2 lower = new(rect.xMin, rect.yMin);
            Color32 tint = color;
            vertexHelper.AddVert(tip, tint, Vector2.zero);
            vertexHelper.AddVert(upper, tint, Vector2.zero);
            vertexHelper.AddVert(notch, tint, Vector2.zero);
            vertexHelper.AddVert(lower, tint, Vector2.zero);
            vertexHelper.AddTriangle(0, 1, 2);
            vertexHelper.AddTriangle(0, 2, 3);
        }
    }
}
