using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>A separate, resolution-independent road over the Campaign plate. Never receives pointer events.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SkirmishSagaRoadGraphic : MaskableGraphic
    {
        [SerializeField]
        private Vector2[] points = System.Array.Empty<Vector2>();
        [SerializeField]
        private bool dotted;
        [SerializeField]
        private float roadWidth = 24f;
        public void Configure(IReadOnlyList<Vector2> route, bool dots = false, float width = 24f)
        {
            points = new Vector2[route.Count];
            for (int i = 0; i < points.Length; i++)
                points[i] = route[i];
            dotted = dots;
            roadWidth = width;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points == null || points.Length < 2)
                return;
            var samples = new List<Vector2>();
            Rect r = rectTransform.rect;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 a = points[Mathf.Max(0, i - 1)], b = points[i], c = points[i + 1], d = points[Mathf.Min(points.Length - 1, i + 2)];
                for (int step = 0; step < 16; step++)
                {
                    float t = step / 16f, tt = t * t, ttt = tt * t;
                    Vector2 p = dotted ? Vector2.Lerp(b, c, t) : .5f * ((2f * b) + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * tt + (-a + 3f * b - 3f * c + d) * ttt);
                    samples.Add(new Vector2(r.xMin + p.x * r.width, r.yMax - p.y * r.height));
                }
            }

            Vector2 last = points[^1];
            samples.Add(new Vector2(r.xMin + last.x * r.width, r.yMax - last.y * r.height));
            if (dotted)
            {
                float distance = 0f, next = 0f;
                for (int i = 1; i < samples.Count; i++)
                {
                    Vector2 delta = samples[i] - samples[i - 1];
                    float length = delta.magnitude;
                    while (next <= distance + length)
                    {
                        Vector2 center = Vector2.Lerp(samples[i - 1], samples[i], (next - distance) / Mathf.Max(.001f, length));
                        Disc(vh, center + new Vector2(0, -2), roadWidth * .64f, new Color(0, 0, 0, .7f));
                        Disc(vh, center, roadWidth * .42f, new Color(1f, .88f, .53f, .95f));
                        next += roadWidth * 2.2f;
                    }

                    distance += length;
                }

                return;
            }

            Stroke(vh, samples, roadWidth + 10, new Color(.08f, .04f, .02f, .48f));
            Stroke(vh, samples, roadWidth + 5, new Color(.6f, .47f, .3f, .25f));
            Stroke(vh, samples, roadWidth, new Color(.48f, .32f, .17f, .93f));
            Stroke(vh, samples, roadWidth - 7, new Color(.76f, .56f, .31f, .86f));
        }

        private static void Stroke(VertexHelper vh, List<Vector2> samples, float width, Color tint)
        {
            for (int i = 0; i < samples.Count; i++)
            {
                Vector2 tangent = (samples[Mathf.Min(i + 1, samples.Count - 1)] - samples[Mathf.Max(0, i - 1)]).normalized;
                Vector2 n = new(-tangent.y, tangent.x);
                n *= width * .5f;
                int k = vh.currentVertCount;
                vh.AddVert(samples[i] + n, tint, Vector2.zero);
                vh.AddVert(samples[i] - n, tint, Vector2.one);
                if (i > 0)
                {
                    vh.AddTriangle(k - 2, k, k - 1);
                    vh.AddTriangle(k - 1, k, k + 1);
                }
            }
        }

        private static void Disc(VertexHelper vh, Vector2 center, float radius, Color tint)
        {
            const int sides = 12;
            int k = vh.currentVertCount;
            vh.AddVert(center, tint, Vector2.zero);
            for (int i = 0; i <= sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                if (i > 0)
                    vh.AddTriangle(k, k + i, k + i + 1);
            }
        }
    }
}
