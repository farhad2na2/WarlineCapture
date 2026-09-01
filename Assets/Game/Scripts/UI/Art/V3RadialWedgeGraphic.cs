using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class V3RadialWedgeGraphic : MaskableGraphic
    {
        [SerializeField] private Color topColor = Color.white;
        [SerializeField] private Color bottomColor = Color.gray;
        [SerializeField] private Color borderColor = Color.black;
        [SerializeField, Range(-360f, 360f)] private float startAngle = 60f;
        [SerializeField, Range(1f, 180f)] private float sweepAngle = 56f;
        [SerializeField, Range(0f, .95f)] private float innerRadius = .36f;
        [SerializeField, Range(.05f, 1f)] private float outerRadius = .49f;
        [SerializeField, Min(.5f)] private float borderWidth = 3f;
        [SerializeField, Range(2, 64)] private int arcSegments = 10;

        public void Configure(
            float startDegrees,
            float sweepDegrees,
            float innerRadius01,
            float outerRadius01,
            Color top,
            Color bottom,
            Color border,
            float width,
            bool receivesRaycasts)
        {
            startAngle = startDegrees;
            sweepAngle = Mathf.Clamp(sweepDegrees, 1f, 180f);
            innerRadius = Mathf.Clamp(innerRadius01, 0f, .95f);
            outerRadius = Mathf.Clamp(outerRadius01, innerRadius + .01f, 1f);
            topColor = top;
            bottomColor = bottom;
            borderColor = border;
            borderWidth = Mathf.Max(.5f, width);
            arcSegments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(sweepAngle) / 6f), 2, 64);
            color = Color.white;
            raycastTarget = receivesRaycasts;
            SetVerticesDirty();
        }

        public void SetPalette(Color top, Color bottom, Color border)
        {
            topColor = top;
            bottomColor = bottom;
            borderColor = border;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            float inner = radius * innerRadius;
            float outer = radius * outerRadius;
            if (outer <= inner || radius <= 0f)
                return;

            Vector2 center = rect.center;
            int segments = Mathf.Max(2, arcSegments);
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = (startAngle + sweepAngle * t) * Mathf.Deg2Rad;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outerPoint = center + direction * outer;
                Vector2 innerPoint = center + direction * inner;
                vertexHelper.AddVert(outerPoint, EvaluateColor(rect, outerPoint), Vector2.zero);
                vertexHelper.AddVert(innerPoint, EvaluateColor(rect, innerPoint), Vector2.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                int index = i * 2;
                vertexHelper.AddTriangle(index, index + 2, index + 1);
                vertexHelper.AddTriangle(index + 2, index + 3, index + 1);
            }

            float border = Mathf.Min(borderWidth, (outer - inner) * .45f);
            AddArcBorder(vertexHelper, center, inner, inner + border, segments);
            AddArcBorder(vertexHelper, center, outer - border, outer, segments);
            AddRadialBorder(vertexHelper, center, inner, outer, startAngle * Mathf.Deg2Rad, border);
            AddRadialBorder(vertexHelper, center, inner, outer, (startAngle + sweepAngle) * Mathf.Deg2Rad, border);
        }

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.Raycast(screenPoint, eventCamera) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
            {
                return false;
            }

            Rect rect = rectTransform.rect;
            Vector2 delta = localPoint - rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            float distance = delta.magnitude;
            if (distance < radius * innerRadius || distance > radius * outerRadius)
                return false;

            float angle = Mathf.Repeat(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, 360f);
            float relative = Mathf.Repeat(angle - Mathf.Repeat(startAngle, 360f), 360f);
            return relative <= sweepAngle;
        }

        private void AddArcBorder(
            VertexHelper vertexHelper,
            Vector2 center,
            float radiusA,
            float radiusB,
            int segments)
        {
            int first = vertexHelper.currentVertCount;
            Color32 border = borderColor;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = (startAngle + sweepAngle * t) * Mathf.Deg2Rad;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                vertexHelper.AddVert(center + direction * radiusA, border, Vector2.zero);
                vertexHelper.AddVert(center + direction * radiusB, border, Vector2.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                int index = first + i * 2;
                vertexHelper.AddTriangle(index, index + 2, index + 1);
                vertexHelper.AddTriangle(index + 2, index + 3, index + 1);
            }
        }

        private void AddRadialBorder(
            VertexHelper vertexHelper,
            Vector2 center,
            float inner,
            float outer,
            float angle,
            float width)
        {
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 normal = new(-direction.y, direction.x);
            Vector2 offset = normal * width * .5f;
            Vector2 a = center + direction * inner;
            Vector2 b = center + direction * outer;
            int first = vertexHelper.currentVertCount;
            Color32 border = borderColor;
            vertexHelper.AddVert(a - offset, border, Vector2.zero);
            vertexHelper.AddVert(a + offset, border, Vector2.zero);
            vertexHelper.AddVert(b + offset, border, Vector2.zero);
            vertexHelper.AddVert(b - offset, border, Vector2.zero);
            vertexHelper.AddTriangle(first, first + 1, first + 2);
            vertexHelper.AddTriangle(first, first + 2, first + 3);
        }

        private Color32 EvaluateColor(Rect rect, Vector2 point)
        {
            float t = rect.height > .001f ? Mathf.InverseLerp(rect.yMin, rect.yMax, point.y) : .5f;
            return Color.Lerp(bottomColor, topColor, t);
        }
    }
}
