using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Resolution-independent perspective ground ring for V3 tactical feedback.
    /// The mesh follows the complete RectTransform ellipse so the marker reads as
    /// lying on the battlefield plane instead of facing the camera.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class V3EllipseRingGraphic : MaskableGraphic
    {
        [SerializeField, Min(.5f)] private float thickness = 3f;
        [SerializeField, Range(16, 128)] private int segments = 64;

        public void Configure(Color ringColor, float ringThickness, int segmentCount = 64)
        {
            color = ringColor;
            thickness = Mathf.Max(.5f, ringThickness);
            segments = Mathf.Clamp(segmentCount, 16, 128);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            float outerX = rect.width * .5f;
            float outerY = rect.height * .5f;
            if (outerX <= 0f || outerY <= 0f)
                return;

            float innerX = Mathf.Max(0f, outerX - thickness);
            float innerY = Mathf.Max(0f, outerY - thickness);
            Vector2 center = rect.center;
            Color32 vertexColor = color;
            int segmentCount = Mathf.Max(16, segments);

            for (int i = 0; i <= segmentCount; i++)
            {
                float angle = i * Mathf.PI * 2f / segmentCount;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vertexHelper.AddVert(center + new Vector2(cos * outerX, sin * outerY), vertexColor, Vector2.zero);
                vertexHelper.AddVert(center + new Vector2(cos * innerX, sin * innerY), vertexColor, Vector2.zero);
            }

            for (int i = 0; i < segmentCount; i++)
            {
                int index = i * 2;
                vertexHelper.AddTriangle(index, index + 2, index + 1);
                vertexHelper.AddTriangle(index + 2, index + 3, index + 1);
            }
        }
    }
}
