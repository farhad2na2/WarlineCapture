using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class V3PolygonGraphic : MaskableGraphic
    {
        [SerializeField] private Vector2[] points = Array.Empty<Vector2>();
        [SerializeField] private Vector2 authoredSize;
        [SerializeField] private Color outlineColor = Color.clear;
        [SerializeField] private float outlineWidth;

        public void Configure(Vector2[] topLeftPoints, Color fill)
        {
            points = topLeftPoints ?? Array.Empty<Vector2>();
            authoredSize = Vector2.zero;
            outlineColor = Color.clear;
            outlineWidth = 0f;
            color = fill;
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void ConfigureResponsive(
            Vector2[] topLeftPoints,
            Color fill,
            Color configuredOutlineColor,
            float configuredOutlineWidth,
            Vector2 configuredAuthoredSize)
        {
            points = topLeftPoints ?? Array.Empty<Vector2>();
            authoredSize = configuredAuthoredSize;
            outlineColor = configuredOutlineColor;
            outlineWidth = Mathf.Max(0f, configuredOutlineWidth);
            color = fill;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (points == null || points.Length < 3)
                return;

            float scaleX = authoredSize.x > 0f ? rectTransform.rect.width / authoredSize.x : 1f;
            float scaleY = authoredSize.y > 0f ? rectTransform.rect.height / authoredSize.y : 1f;
            Color32 vertexColor = color;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 point = points[i];
                vertexHelper.AddVert(new Vector3(point.x * scaleX, -point.y * scaleY, 0f), vertexColor, Vector2.zero);
            }

            for (int i = 1; i < points.Length - 1; i++)
                vertexHelper.AddTriangle(0, i, i + 1);

            if (outlineWidth <= 0f || outlineColor.a <= 0f)
                return;

            Color32 edgeColor = outlineColor;
            float halfWidth = outlineWidth * .5f;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 startPoint = points[i];
                Vector2 endPoint = points[(i + 1) % points.Length];
                Vector2 start = new(startPoint.x * scaleX, -startPoint.y * scaleY);
                Vector2 end = new(endPoint.x * scaleX, -endPoint.y * scaleY);
                Vector2 direction = end - start;
                if (direction.sqrMagnitude <= 0.0001f)
                    continue;

                direction.Normalize();
                Vector2 normal = new(-direction.y, direction.x);
                normal *= halfWidth;
                int vertex = vertexHelper.currentVertCount;
                vertexHelper.AddVert(start - normal, edgeColor, Vector2.zero);
                vertexHelper.AddVert(start + normal, edgeColor, Vector2.zero);
                vertexHelper.AddVert(end + normal, edgeColor, Vector2.zero);
                vertexHelper.AddVert(end - normal, edgeColor, Vector2.zero);
                vertexHelper.AddTriangle(vertex, vertex + 1, vertex + 2);
                vertexHelper.AddTriangle(vertex, vertex + 2, vertex + 3);
            }
        }
    }
}
