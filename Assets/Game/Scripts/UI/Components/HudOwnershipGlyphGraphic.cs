using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public enum HudOwnershipGlyph : byte { Ring, Shield, Diamond, Objective }

    /// <summary>
    /// Procedural HUD glyphs (W40, Board 4): a filled shield for friendly, a diamond for enemy, a
    /// hollow ring for neutral, and the amber objective target on the objective card. Drawn from the
    /// rect so it stays crisp at any HUD scale. Presentation only; never a raycast target.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudOwnershipGlyphGraphic : MaskableGraphic
    {
        private static readonly Vector2[] ShieldOutline =
        {
            new(-.80f, .86f), new(.80f, .86f), new(.80f, .06f), new(.46f, -.56f), new(0f, -.96f), new(-.46f, -.56f), new(-.80f, .06f)
        };
        private static readonly Vector2[] DiamondOutline = { new(0f, .96f), new(.96f, 0f), new(0f, -.96f), new(-.96f, 0f) };

        [SerializeField] private HudOwnershipGlyph shape;
        public HudOwnershipGlyph Shape => shape;

        public void Present(HudOwnershipGlyph value, Color tint)
        {
            raycastTarget = false;
            if (shape != value) { shape = value; SetVerticesDirty(); }
            if (color != tint) color = tint;
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 centre = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            if (radius <= 0f) return;
            Color32 tint = color;
            switch (shape)
            {
                case HudOwnershipGlyph.Shield: Fan(helper, centre, radius, ShieldOutline, tint); break;
                case HudOwnershipGlyph.Diamond: Fan(helper, centre, radius, DiamondOutline, tint); break;
                case HudOwnershipGlyph.Ring: Ring(helper, centre, radius * .92f, Mathf.Max(2f, radius * .3f), tint); break;
                default:
                    Ring(helper, centre, radius * .74f, Mathf.Max(2f, radius * .17f), tint);
                    float tick = Mathf.Max(2f, radius * .14f);
                    Quad(helper, centre + new Vector2(-tick * .5f, radius * .58f), centre + new Vector2(tick * .5f, radius), tint);
                    Quad(helper, centre + new Vector2(-tick * .5f, -radius), centre + new Vector2(tick * .5f, -radius * .58f), tint);
                    Quad(helper, centre + new Vector2(radius * .58f, -tick * .5f), centre + new Vector2(radius, tick * .5f), tint);
                    Quad(helper, centre + new Vector2(-radius, -tick * .5f), centre + new Vector2(-radius * .58f, tick * .5f), tint);
                    Disc(helper, centre, radius * .17f, tint);
                    break;
            }
        }

        private static void Fan(VertexHelper helper, Vector2 centre, float radius, Vector2[] outline, Color32 tint)
        {
            int start = helper.currentVertCount;
            helper.AddVert(centre, tint, Vector4.zero);
            foreach (var point in outline) helper.AddVert(centre + point * radius, tint, Vector4.zero);
            for (int i = 0; i < outline.Length; i++)
                helper.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % outline.Length);
        }

        private static void Ring(VertexHelper helper, Vector2 centre, float outer, float thickness, Color32 tint)
        {
            const int Segments = 32;
            float inner = Mathf.Max(0f, outer - thickness);
            int start = helper.currentVertCount;
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                helper.AddVert(centre + direction * outer, tint, Vector4.zero);
                helper.AddVert(centre + direction * inner, tint, Vector4.zero);
            }
            for (int i = 0; i < Segments; i++)
            {
                int a = start + i * 2;
                helper.AddTriangle(a, a + 2, a + 1);
                helper.AddTriangle(a + 1, a + 2, a + 3);
            }
        }

        private static void Disc(VertexHelper helper, Vector2 centre, float radius, Color32 tint)
        {
            const int Segments = 20;
            int start = helper.currentVertCount;
            helper.AddVert(centre, tint, Vector4.zero);
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                helper.AddVert(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector4.zero);
            }
            for (int i = 0; i < Segments; i++) helper.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % Segments);
        }

        private static void Quad(VertexHelper helper, Vector2 min, Vector2 max, Color32 tint)
        {
            int start = helper.currentVertCount;
            helper.AddVert(new Vector3(min.x, min.y), tint, Vector4.zero);
            helper.AddVert(new Vector3(min.x, max.y), tint, Vector4.zero);
            helper.AddVert(new Vector3(max.x, max.y), tint, Vector4.zero);
            helper.AddVert(new Vector3(max.x, min.y), tint, Vector4.zero);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start + 2, start + 3, start);
        }
    }
}
