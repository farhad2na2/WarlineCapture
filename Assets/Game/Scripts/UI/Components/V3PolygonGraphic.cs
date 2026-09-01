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

        public void Configure(Vector2[] topLeftPoints, Color fill)
        {
            points = topLeftPoints ?? Array.Empty<Vector2>();
            color = fill;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (points == null || points.Length < 3)
                return;

            Color32 vertexColor = color;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 point = points[i];
                vertexHelper.AddVert(new Vector3(point.x, -point.y, 0f), vertexColor, Vector2.zero);
            }

            for (int i = 1; i < points.Length - 1; i++)
                vertexHelper.AddTriangle(0, i, i + 1);
        }
    }
}
