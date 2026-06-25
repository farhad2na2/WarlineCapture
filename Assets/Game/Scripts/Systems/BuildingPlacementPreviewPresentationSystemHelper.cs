using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class BuildingPlacementPreviewPresentationSystemHelper
{
    public readonly struct WallPreviewRun
    {
        public readonly IReadOnlyList<Vector2Int> Origins;
        public readonly bool Vertical;

        public WallPreviewRun(IReadOnlyList<Vector2Int> origins, bool vertical)
        {
            Origins = origins;
            Vertical = vertical;
        }
    }

    public delegate GameObject CreateVisualDelegate(BuildingDefinition definition, Transform parent);
    public delegate void PositionVisualDelegate(GameObject instance, Vector2Int originCell, BuildingDefinition definition, GridConfig grid, bool rotateVertical);
    public delegate Vector3 FootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);

    private GameObject _placementOutline;
    private MeshRenderer _placementOutlineRenderer;
    private MaterialPropertyBlock _previewPropertyBlock;
    private readonly List<WallPreviewRun> _wallPreviewRuns = new();
    private Action<UnityEngine.Object> _destroyRuntimeObject;
    private float _outlineHeight;
    private Color _validColor;
    private Color _invalidColor;

    public void Init(
        Transform runtimeRoot,
        float outlineHeight,
        Color validColor,
        Color invalidColor,
        Action<UnityEngine.Object> destroyRuntimeObject)
    {
        _outlineHeight = outlineHeight;
        _validColor = validColor;
        _invalidColor = invalidColor;
        _destroyRuntimeObject = destroyRuntimeObject;
        _previewPropertyBlock ??= new MaterialPropertyBlock();

        if (_placementOutline != null)
            return;

        _placementOutline = new GameObject("PlacementOutline");
        _placementOutline.transform.SetParent(runtimeRoot, false);
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "PlacementVolume";
        box.transform.SetParent(_placementOutline.transform, false);
        Collider collider = box.GetComponent<Collider>();
        if (collider != null)
            DestroyRuntimeObject(collider);

        _placementOutlineRenderer = box.GetComponent<MeshRenderer>();
        _placementOutlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _placementOutlineRenderer.receiveShadows = false;
        _placementOutlineRenderer.sharedMaterial = CreatePlacementMaterial();

        ApplyPlacementMaterialColor(_validColor);
        _placementOutline.SetActive(false);
    }

    public void Dispose()
    {
        if (_placementOutline != null)
            DestroyRuntimeObject(_placementOutline);

        _placementOutline = null;
        _placementOutlineRenderer = null;
        _destroyRuntimeObject = null;
    }

    public void HideOutline()
    {
        if (_placementOutline != null && _placementOutline.activeSelf)
            _placementOutline.SetActive(false);
    }

    public void UpdateOutline(
        Vector2Int originCell,
        Vector2Int footprintCells,
        GridConfig grid,
        BuildingDefinition definition,
        bool valid,
        FootprintCenterDelegate getFootprintCenter)
    {
        if (_placementOutline == null || _placementOutlineRenderer == null || getFootprintCenter == null)
            return;

        float width = footprintCells.x * grid.CellSize;
        float depth = footprintCells.y * grid.CellSize;
        float height = GetOutlineHeight(definition);
        Vector3 center = getFootprintCenter(originCell, footprintCells, grid) + new Vector3(0f, height * 0.5f, 0f);

        _placementOutline.transform.SetPositionAndRotation(center, Quaternion.identity);
        _placementOutlineRenderer.transform.localPosition = Vector3.zero;
        _placementOutlineRenderer.transform.localScale = new Vector3(
            Mathf.Max(grid.CellSize, width),
            height,
            Mathf.Max(grid.CellSize, depth));

        ApplyPlacementMaterialColor(valid ? _validColor : _invalidColor);
        _placementOutline.SetActive(true);
    }

    public void UpdateWallOutline(
        IReadOnlyList<Vector2Int> origins,
        Vector2Int footprintCells,
        GridConfig grid,
        BuildingDefinition definition,
        bool valid,
        FootprintCenterDelegate getFootprintCenter)
    {
        if (origins == null || origins.Count == 0)
        {
            HideOutline();
            return;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        for (int i = 0; i < origins.Count; i++)
        {
            Vector2Int origin = origins[i];
            minX = Mathf.Min(minX, origin.x);
            minY = Mathf.Min(minY, origin.y);
            maxX = Mathf.Max(maxX, origin.x + footprintCells.x);
            maxY = Mathf.Max(maxY, origin.y + footprintCells.y);
        }

        UpdateOutline(
            new Vector2Int(minX, minY),
            new Vector2Int(maxX - minX, maxY - minY),
            grid,
            definition,
            valid,
            getFootprintCenter);
    }

    public void RebuildWallPreview(
        GameObject previewInstance,
        BuildingDefinition definition,
        IReadOnlyList<WallPreviewRun> committedRuns,
        IReadOnlyList<Vector2Int> currentOrigins,
        bool vertical,
        bool hideCurrentPreview,
        bool valid,
        GridConfig grid,
        CreateVisualDelegate createVisual,
        PositionVisualDelegate positionVisual)
    {
        if (previewInstance == null || createVisual == null || positionVisual == null)
            return;

        Transform root = previewInstance.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
            DestroyRuntimeObject(root.GetChild(i).gameObject);

        if (committedRuns != null)
        {
            for (int runIndex = 0; runIndex < committedRuns.Count; runIndex++)
            {
                WallPreviewRun run = committedRuns[runIndex];
                if (run.Origins == null)
                    continue;

                for (int i = 0; i < run.Origins.Count; i++)
                {
                    GameObject segment = createVisual(definition, root);
                    if (segment == null)
                        continue;

                    positionVisual(segment, run.Origins[i], definition, grid, run.Vertical);
                    SetPreviewSegmentValid(segment, true);
                }
            }
        }

        if (hideCurrentPreview || currentOrigins == null)
            return;

        for (int i = 0; i < currentOrigins.Count; i++)
        {
            GameObject segment = createVisual(definition, root);
            if (segment == null)
                continue;

            positionVisual(segment, currentOrigins[i], definition, grid, vertical);
            SetPreviewSegmentValid(segment, valid);
        }
    }

    public void RebuildWallPlacementPreview(
        BuildingPlacementLifecycleCompositionSystemHelper.PlacementState placement,
        IReadOnlyList<Vector2Int> origins,
        bool vertical,
        GridConfig grid,
        CreateVisualDelegate createVisual,
        PositionVisualDelegate positionVisual)
    {
        if (placement?.PreviewInstance == null)
            return;

        _wallPreviewRuns.Clear();
        if (placement.CommittedWallRuns != null)
        {
            for (int runIndex = 0; runIndex < placement.CommittedWallRuns.Count; runIndex++)
            {
                BuildingPlacementInputUiSystemHelper.WallRun run = placement.CommittedWallRuns[runIndex];
                if (run?.Origins == null)
                    continue;

                _wallPreviewRuns.Add(new WallPreviewRun(run.Origins, run.Vertical));
            }
        }

        RebuildWallPreview(
            placement.PreviewInstance,
            placement.Definition,
            _wallPreviewRuns,
            origins,
            vertical,
            placement.HideCurrentWallPreview,
            placement.IsValid,
            grid,
            createVisual,
            positionVisual);
    }

    private float GetOutlineHeight(BuildingDefinition definition)
    {
        float baseHeight = Mathf.Max(0.5f, _outlineHeight);
        if (definition?.HasLocalBounds == true)
            baseHeight = Mathf.Max(baseHeight, definition.LocalBounds.size.y + _outlineHeight);

        return baseHeight;
    }

    private void SetPreviewSegmentValid(GameObject segment, bool valid)
    {
        if (segment == null)
            return;

        Renderer[] renderers = segment.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(_previewPropertyBlock);
            Color tint = valid ? Color.white : new Color(1f, 0.45f, 0.45f, 1f);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                _previewPropertyBlock.SetColor("_BaseColor", tint);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
                _previewPropertyBlock.SetColor("_Color", tint);
            renderer.SetPropertyBlock(_previewPropertyBlock);
        }
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

    private void ApplyPlacementMaterialColor(Color color)
    {
        if (_placementOutlineRenderer == null)
            return;

        Color c = color;
        c.a = 0.28f;
        Material material = _placementOutlineRenderer.sharedMaterial;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", c);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", c);
    }

    private void DestroyRuntimeObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (_destroyRuntimeObject != null)
            _destroyRuntimeObject(target);
        else if (Application.isPlaying)
            UnityEngine.Object.Destroy(target);
        else
            UnityEngine.Object.DestroyImmediate(target);
    }
}
