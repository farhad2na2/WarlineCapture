using UnityEngine;

namespace Game.UI.Runtime
{
    public enum MilitaryUiMaterialKind { None, Automatic, FracturedGlass, BallisticWeave, TacticalGlass, AnodizedMetal, CeramicArmor }

    public sealed partial class V3GradientGraphic
    {
        [SerializeField] private MilitaryUiMaterialKind militaryMaterial;
        private static readonly Material[] sharedMaterials = new Material[7];
        public MilitaryUiMaterialKind MilitaryMaterial => militaryMaterial;
        public MilitaryUiMaterialKind ResolvedMilitaryMaterial
        {
            get
            {
                if (militaryMaterial != MilitaryUiMaterialKind.Automatic) return militaryMaterial;
                Color fill = topLeftColor;
                // Leave illustration shades, outlines, and translucent feedback untouched.
                if (Mathf.Min(fill.a, bottomLeftColor.a) < .92f || Mathf.Max(fill.r, fill.g, fill.b) < .025f)
                    return MilitaryUiMaterialKind.None;
                float max = Mathf.Max(fill.r, fill.g, fill.b), min = Mathf.Min(fill.r, fill.g, fill.b);
                if (max - min < .10f) return MilitaryUiMaterialKind.CeramicArmor;
                if (fill.r > fill.g * 2f && fill.r > fill.b * 1.35f)
                    return MilitaryUiMaterialKind.FracturedGlass;
                if (fill.r > fill.b * 1.5f && fill.g > fill.b * 1.5f && fill.r > fill.g * 1.1f)
                    return MilitaryUiMaterialKind.AnodizedMetal;
                if (fill.g > fill.r && fill.g > fill.b * 1.15f)
                    return MilitaryUiMaterialKind.BallisticWeave;
                return MilitaryUiMaterialKind.TacticalGlass;
            }
        }

        public static string MaterialName(MilitaryUiMaterialKind kind) => kind switch
        {
            MilitaryUiMaterialKind.FracturedGlass => "fractured-glass",
            MilitaryUiMaterialKind.BallisticWeave => "ballistic-weave",
            MilitaryUiMaterialKind.TacticalGlass => "tactical-glass",
            MilitaryUiMaterialKind.AnodizedMetal => "anodized-metal",
            MilitaryUiMaterialKind.CeramicArmor => "ceramic-armor",
            _ => null
        };

        private Material SurfaceMaterial
        {
            get
            {
                var kind = ResolvedMilitaryMaterial;
                if (kind <= MilitaryUiMaterialKind.Automatic) return null;
                int index = (int)kind;
                if (sharedMaterials[index] == null)
                    sharedMaterials[index] = Resources.Load<Material>("MilitaryUiMaterials/" + MaterialName(kind));
                return sharedMaterials[index];
            }
        }

        public override Material material
        {
            get => SurfaceMaterial != null ? SurfaceMaterial : base.material;
            set => base.material = value;
        }
        public override Texture mainTexture => SurfaceMaterial != null ? SurfaceMaterial.mainTexture : base.mainTexture;

        public void SetMilitaryMaterial(MilitaryUiMaterialKind kind)
        {
            militaryMaterial = kind;
            SetMaterialDirty(); SetVerticesDirty();
        }

        private void AddMaterialFill(UnityEngine.UI.VertexHelper helper, float xMin, float yMin, float xMax, float yMax,
            Color32 bottomLeft, Color32 topLeft, Color32 topRight, Color32 bottomRight)
        {
            if (SurfaceMaterial == null)
            {
                AddFourCornerQuad(helper, xMin, yMin, xMax, yMax, bottomLeft, topLeft, topRight, bottomRight);
                return;
            }
            float width = xMax - xMin, height = yMax - yMin;
            if (width <= 0 || height <= 0) return;
            // Preserve edge detail on wide controls rather than stretching the cracks and seams.
            float edgeX = Mathf.Min(32f / width, .25f), edgeY = Mathf.Min(32f / height, .25f);
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
            {
                float left = Slice(col, edgeX), right = Slice(col + 1, edgeX);
                float bottom = Slice(row, edgeY), top = Slice(row + 1, edgeY);
                int start = helper.currentVertCount;
                AddSurfaceVertex(helper, xMin, yMin, width, height, left, bottom, Slice(col, .16f), Slice(row, .16f), bottomLeft, topLeft, topRight, bottomRight);
                AddSurfaceVertex(helper, xMin, yMin, width, height, left, top, Slice(col, .16f), Slice(row + 1, .16f), bottomLeft, topLeft, topRight, bottomRight);
                AddSurfaceVertex(helper, xMin, yMin, width, height, right, top, Slice(col + 1, .16f), Slice(row + 1, .16f), bottomLeft, topLeft, topRight, bottomRight);
                AddSurfaceVertex(helper, xMin, yMin, width, height, right, bottom, Slice(col + 1, .16f), Slice(row, .16f), bottomLeft, topLeft, topRight, bottomRight);
                helper.AddTriangle(start, start + 1, start + 2); helper.AddTriangle(start + 2, start + 3, start);
            }
        }

        private static float Slice(int index, float edge) => index == 0 ? 0 : index == 1 ? edge : index == 2 ? 1 - edge : 1;
        private void AddSurfaceVertex(UnityEngine.UI.VertexHelper helper, float x, float y, float width, float height,
            float px, float py, float u, float v, Color bottomLeft, Color topLeft, Color topRight, Color bottomRight)
        {
            var vertex = UnityEngine.UIVertex.simpleVert;
            vertex.position = new Vector3(x + width * px, y + height * py);
            vertex.uv0 = ResolvedMilitaryMaterial == MilitaryUiMaterialKind.BallisticWeave
                ? new Vector4(px * width / 220f, py * height / 220f, 0, 0)
                : new Vector4(u, v, 0, 0);
            vertex.color = Color.Lerp(Color.Lerp(bottomLeft, topLeft, py), Color.Lerp(bottomRight, topRight, py), px);
            helper.AddVert(vertex);
        }
    }
}
