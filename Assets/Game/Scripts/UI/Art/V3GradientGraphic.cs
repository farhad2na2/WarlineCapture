using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Resolution-independent vertical gradient used inside the shared V3 chrome.
    /// Keeping the fill procedural avoids duplicate raster gradients in the UI atlases.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class V3GradientGraphic : MaskableGraphic
    {
        [SerializeField] private Color topLeftColor = Color.white;
        [SerializeField] private Color topRightColor = Color.white;
        [SerializeField] private Color bottomLeftColor = Color.black;
        [SerializeField] private Color bottomRightColor = Color.black;
        [SerializeField] private Color borderColor = Color.clear;
        [SerializeField, Min(0f)] private float borderWidth;

        public void Configure(Color top, Color bottom, Color border, float width)
        {
            topLeftColor = top;
            topRightColor = top;
            bottomLeftColor = bottom;
            bottomRightColor = bottom;
            borderColor = border;
            borderWidth = Mathf.Max(0f, width);
            SetVerticesDirty();
        }

        public void ConfigureCorners(
            Color topLeft,
            Color topRight,
            Color bottomLeft,
            Color bottomRight,
            Color border,
            float width)
        {
            topLeftColor = topLeft;
            topRightColor = topRight;
            bottomLeftColor = bottomLeft;
            bottomRightColor = bottomRight;
            borderColor = border;
            borderWidth = Mathf.Max(0f, width);
            SetVerticesDirty();
        }

        public void SetGradient(Color top, Color bottom)
        {
            topLeftColor = top;
            topRightColor = top;
            bottomLeftColor = bottom;
            bottomRightColor = bottom;
            SetVerticesDirty();
        }

        public void SetGradientCorners(Color topLeft, Color topRight, Color bottomLeft, Color bottomRight)
        {
            topLeftColor = topLeft;
            topRightColor = topRight;
            bottomLeftColor = bottomLeft;
            bottomRightColor = bottomRight;
            SetVerticesDirty();
        }

        public void SetBorder(Color border, float width)
        {
            borderColor = border;
            borderWidth = Mathf.Max(0f, width);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            float stroke = Mathf.Min(borderWidth, Mathf.Min(rect.width, rect.height) * 0.5f);

            Color32 topLeft = Multiply(topLeftColor, color);
            Color32 topRight = Multiply(topRightColor, color);
            Color32 bottomLeft = Multiply(bottomLeftColor, color);
            Color32 bottomRight = Multiply(bottomRightColor, color);
            Color32 border = Multiply(borderColor, color);

            AddFourCornerQuad(
                vertexHelper,
                rect.xMin + stroke,
                rect.yMin + stroke,
                rect.xMax - stroke,
                rect.yMax - stroke,
                bottomLeft,
                topLeft,
                topRight,
                bottomRight);

            if (stroke <= 0f || borderColor.a <= 0f)
                return;

            AddSolidQuad(vertexHelper, rect.xMin, rect.yMin, rect.xMin + stroke, rect.yMax, border);
            AddSolidQuad(vertexHelper, rect.xMax - stroke, rect.yMin, rect.xMax, rect.yMax, border);
            AddSolidQuad(vertexHelper, rect.xMin + stroke, rect.yMin, rect.xMax - stroke, rect.yMin + stroke, border);
            AddSolidQuad(vertexHelper, rect.xMin + stroke, rect.yMax - stroke, rect.xMax - stroke, rect.yMax, border);
        }

        private static Color32 Multiply(Color left, Color right)
        {
            return new Color(
                left.r * right.r,
                left.g * right.g,
                left.b * right.b,
                left.a * right.a);
        }

        private static void AddSolidQuad(VertexHelper helper, float xMin, float yMin, float xMax, float yMax, Color32 color)
        {
            AddFourCornerQuad(helper, xMin, yMin, xMax, yMax, color, color, color, color);
        }

        private static void AddFourCornerQuad(
            VertexHelper helper,
            float xMin,
            float yMin,
            float xMax,
            float yMax,
            Color32 bottomLeft,
            Color32 topLeft,
            Color32 topRight,
            Color32 bottomRight)
        {
            int start = helper.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(xMin, yMin);
            vertex.color = bottomLeft;
            helper.AddVert(vertex);
            vertex.position = new Vector3(xMin, yMax);
            vertex.color = topLeft;
            helper.AddVert(vertex);
            vertex.position = new Vector3(xMax, yMax);
            vertex.color = topRight;
            helper.AddVert(vertex);
            vertex.position = new Vector3(xMax, yMin);
            vertex.color = bottomRight;
            helper.AddVert(vertex);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start + 2, start + 3, start);
        }
    }
}
