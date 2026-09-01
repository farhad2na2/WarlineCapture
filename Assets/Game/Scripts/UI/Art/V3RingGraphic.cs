using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class V3RingGraphic : MaskableGraphic
    {
        [SerializeField, Min(0.5f)] private float thickness = 3f;
        [SerializeField, Range(12, 96)] private int segments = 48;

        public void Configure(Color ringColor, float ringThickness, int segmentCount = 48)
        {
            color = ringColor;
            thickness = Mathf.Max(0.5f, ringThickness);
            segments = Mathf.Clamp(segmentCount, 12, 96);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            float outerRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float innerRadius = Mathf.Max(0f, outerRadius - thickness);
            Vector2 center = rect.center;
            Color32 vertexColor = color;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                vertexHelper.AddVert(center + direction * outerRadius, vertexColor, Vector2.zero);
                vertexHelper.AddVert(center + direction * innerRadius, vertexColor, Vector2.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                int index = i * 2;
                vertexHelper.AddTriangle(index, index + 2, index + 1);
                vertexHelper.AddTriangle(index + 2, index + 3, index + 1);
            }
        }
    }
}
