using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Object;

internal sealed class RoadBuildPlacementVisualSystem
{
    internal sealed class State
    {
        public GameObject PlacementOutline;
        public Transform[] PlacementOutlineEdges;
        public MeshRenderer[] PlacementOutlineRenderers;
    }

    public State CreateState()
    {
        return new State();
    }

    public void CreatePlacementOutline(State state, Transform runtimeRoot, Color placementValidColor)
    {
        if (state == null)
            return;

        state.PlacementOutline = new GameObject("PlacementOutline");
        state.PlacementOutline.transform.SetParent(runtimeRoot, false);
        state.PlacementOutlineEdges = new Transform[4];
        state.PlacementOutlineRenderers = new MeshRenderer[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = $"PlacementOutlineEdge_{i}";
            edge.transform.SetParent(state.PlacementOutline.transform, false);
            var collider = edge.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = edge.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = CreatePlacementMaterial();
            state.PlacementOutlineEdges[i] = edge.transform;
            state.PlacementOutlineRenderers[i] = renderer;
        }

        ApplyPlacementMaterialColor(state, placementValidColor);
        state.PlacementOutline.SetActive(false);
    }

    public void Dispose(State state)
    {
        if (state == null)
            return;

        if (state.PlacementOutline != null)
            Destroy(state.PlacementOutline);

        state.PlacementOutline = null;
        state.PlacementOutlineEdges = null;
        state.PlacementOutlineRenderers = null;
    }

    public void UpdatePlacementOutline(
        State state,
        Vector2Int originCell,
        Vector2Int footprintCells,
        GridConfig grid,
        float buildPlaneY,
        float placementOutlineWidth,
        float placementOutlineHeight,
        Color placementValidColor,
        Color placementInvalidColor,
        bool valid)
    {
        if (state?.PlacementOutline == null ||
            state.PlacementOutlineEdges == null ||
            state.PlacementOutlineRenderers == null)
            return;

        float width = footprintCells.x * grid.CellSize;
        float depth = footprintCells.y * grid.CellSize;
        float thickness = Mathf.Max(0.2f, placementOutlineWidth);
        float height = Mathf.Max(0.08f, placementOutlineHeight);
        Vector3 center = new(
            grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
            buildPlaneY + height * 0.5f,
            grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);

        state.PlacementOutline.transform.SetPositionAndRotation(center, Quaternion.identity);

        state.PlacementOutlineEdges[0].localPosition = new Vector3(0f, 0f, depth * 0.5f);
        state.PlacementOutlineEdges[0].localScale = new Vector3(width + thickness, height, thickness);

        state.PlacementOutlineEdges[1].localPosition = new Vector3(0f, 0f, -depth * 0.5f);
        state.PlacementOutlineEdges[1].localScale = new Vector3(width + thickness, height, thickness);

        state.PlacementOutlineEdges[2].localPosition = new Vector3(width * 0.5f, 0f, 0f);
        state.PlacementOutlineEdges[2].localScale = new Vector3(thickness, height, depth + thickness);

        state.PlacementOutlineEdges[3].localPosition = new Vector3(-width * 0.5f, 0f, 0f);
        state.PlacementOutlineEdges[3].localScale = new Vector3(thickness, height, depth + thickness);

        ApplyPlacementMaterialColor(state, valid ? placementValidColor : placementInvalidColor);
        state.PlacementOutline.SetActive(true);
    }

    public void HidePlacementOutline(State state)
    {
        if (state?.PlacementOutline != null)
            state.PlacementOutline.SetActive(false);
    }

    private Material CreatePlacementMaterial()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Universal Render Pipeline/Simple Lit") ??
            Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");

        var material = new Material(shader);
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetOverrideTag("RenderType", "Transparent");

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        return material;
    }

    private void ApplyPlacementMaterialColor(State state, Color color)
    {
        if (state?.PlacementOutlineRenderers == null)
            return;

        Color transparentColor = color;
        transparentColor.a = 0.22f;
        for (int i = 0; i < state.PlacementOutlineRenderers.Length; i++)
        {
            var renderer = state.PlacementOutlineRenderers[i];
            if (renderer == null)
                continue;

            Material material = renderer.material;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", transparentColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", transparentColor);
        }
    }
}
