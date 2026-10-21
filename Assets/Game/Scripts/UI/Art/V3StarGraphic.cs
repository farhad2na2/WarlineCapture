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
        [SerializeField, Range(0.45f, 0.95f)] private float insetScale = 0.67f;

        [SerializeField] private bool gradientInset;
        [SerializeField] private Color insetBottomColor;

        public void ConfigureOutlinedGradient(Color border, Color top, Color bottom, float insetRatio = 0.88f)
        {
            innerRadius = 0.382f;
            insetScale = Mathf.Clamp(insetRatio, 0.45f, 0.95f);
            color = border;
            insetColor = top;
            insetBottomColor = bottom;
            gradientInset = true;
            outlined = true;
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void Configure(Color fill, bool outline, Color inset)
        {
            gradientInset = false;
            color = fill;
            outlined = outline;
            insetColor = inset;
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void SetState(Color fill, bool outline, Color inset)
        {
            gradientInset = false;
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
            AddStar(vertexHelper, center, outer, color, color);
            if (outlined)
                AddStar(vertexHelper, center, outer * insetScale, insetColor, gradientInset ? insetBottomColor : insetColor);
        }

        private void AddStar(VertexHelper vertexHelper, Vector2 center, float outer, Color32 top, Color32 bottom)
        {
            int centerIndex = vertexHelper.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = center;
            vertex.color = Color32.Lerp(bottom, top, 0.5f);
            vertexHelper.AddVert(vertex);

            for (int i = 0; i < 10; i++)
            {
                float radius = i % 2 == 0 ? outer : outer * innerRadius;
                float angle = (90f + i * 36f) * Mathf.Deg2Rad;
                vertex.position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertex.color = Color32.Lerp(bottom, top, (vertex.position.y - center.y + outer) / (outer * 2f));
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
