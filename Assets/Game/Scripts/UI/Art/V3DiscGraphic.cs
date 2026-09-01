using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Sharp resolution-independent filled circle for small V3 controls.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class V3DiscGraphic : MaskableGraphic
    {
        [SerializeField, Range(16, 96)] private int segments = 48;

        public void Configure(Color fillColor, int segmentCount = 48)
        {
            color = fillColor;
            segments = Mathf.Clamp(segmentCount, 16, 96);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            Color32 vertexColor = color;

            vertexHelper.AddVert(center, vertexColor, new Vector2(.5f, .5f));
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                vertexHelper.AddVert(
                    center + direction * radius,
                    vertexColor,
                    new Vector2(.5f + direction.x * .5f, .5f + direction.y * .5f));
            }

            for (int i = 0; i < segments; i++)
                vertexHelper.AddTriangle(0, i + 1, i + 2);
        }
    }
}
