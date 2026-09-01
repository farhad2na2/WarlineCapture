using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class V3StarGraphic : MaskableGraphic
    {
        [SerializeField, Range(0.2f, 0.8f)] private float innerRadius = 0.46f;
        [SerializeField] private bool outlined;
        [SerializeField] private Color insetColor = new Color32(5, 13, 16, 255);
        [SerializeField, Range(0.45f, 0.85f)] private float insetScale = 0.67f;

        public void Configure(Color fill, bool outline, Color inset)
        {
            color = fill;
            outlined = outline;
            insetColor = inset;
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void SetState(Color fill, bool outline, Color inset)
        {
            color = fill;
            outlined = outline;
            insetColor = inset;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float outer = Mathf.Min(rect.width, rect.height) * 0.5f;
            AddStar(vertexHelper, center, outer, color);
            if (outlined)
                AddStar(vertexHelper, center, outer * insetScale, insetColor);
        }

        private void AddStar(VertexHelper vertexHelper, Vector2 center, float outer, Color32 fill)
        {
            int centerIndex = vertexHelper.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = center;
            vertex.color = fill;
            vertexHelper.AddVert(vertex);

            for (int i = 0; i < 10; i++)
            {
                float radius = i % 2 == 0 ? outer : outer * innerRadius;
                float angle = (90f + i * 36f) * Mathf.Deg2Rad;
                vertex.position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertexHelper.AddVert(vertex);
            }

            for (int i = 0; i < 10; i++)
                vertexHelper.AddTriangle(
                    centerIndex,
                    centerIndex + i + 1,
                    centerIndex + (i + 1) % 10 + 1);
        }
    }
}
