using System.Collections.Generic;
using UnityEngine;
using CombinedRoadVisualData = RoadGridProjectionSystem.CombinedRoadVisualData;
using DragFirstAxis = RoadPathPlanningSystem.DragFirstAxis;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;
using TileConnectionMask = RoadNetworkSystem.TileConnectionMask;
using VariantData = RoadVisualVariantSystem.VariantData;

public sealed class RoadPreviewSystem
{
    public delegate RoadVisualType ResolveVisualTypeAction(Vector2Int cell, TileConnectionMask mask);
    public delegate bool TryGetVariantAction(RoadVisualType type, TileConnectionMask mask, out VariantData variant);

    public readonly struct Context
    {
        public readonly Dictionary<RoadVisualType, CombinedRoadVisualData> VisualData;
        public readonly Transform RoadRoot;
        public readonly Vector3 GridOrigin;
        public readonly float BuildPlaneY;
        public readonly float RoadGridSize;
        public readonly float PreviewAlpha;
        public readonly GameObject EndPrefab;
        public readonly RoadPathPlanningSystem PathPlanningSystem;
        public readonly RoadNetworkSystem NetworkSystem;
        public readonly ResolveVisualTypeAction ResolveVisualType;
        public readonly TryGetVariantAction TryGetVariant;

        public Context(
            Dictionary<RoadVisualType, CombinedRoadVisualData> visualData,
            Transform roadRoot,
            Vector3 gridOrigin,
            float buildPlaneY,
            float roadGridSize,
            float previewAlpha,
            GameObject endPrefab,
            RoadPathPlanningSystem pathPlanningSystem,
            RoadNetworkSystem networkSystem,
            ResolveVisualTypeAction resolveVisualType,
            TryGetVariantAction tryGetVariant)
        {
            VisualData = visualData;
            RoadRoot = roadRoot;
            GridOrigin = gridOrigin;
            BuildPlaneY = buildPlaneY;
            RoadGridSize = roadGridSize;
            PreviewAlpha = previewAlpha;
            EndPrefab = endPrefab;
            PathPlanningSystem = pathPlanningSystem;
            NetworkSystem = networkSystem;
            ResolveVisualType = resolveVisualType;
            TryGetVariant = tryGetVariant;
        }
    }

    private readonly List<GameObject> _previewObjects = new();
    private readonly Dictionary<RoadVisualType, Stack<GameObject>> _previewPool = new();
    private readonly Dictionary<GameObject, RoadVisualType> _previewObjectTypes = new();

    public void DisposePreview()
    {
        ClearPreview();

        foreach (var pool in _previewPool.Values)
        {
            while (pool.Count > 0)
            {
                GameObject preview = pool.Pop();
                if (preview != null)
                    UnityEngine.Object.Destroy(preview);
            }
        }

        _previewPool.Clear();
        _previewObjectTypes.Clear();
    }

    public void ClearPreview()
    {
        for (int i = 0; i < _previewObjects.Count; i++)
        {
            if (_previewObjects[i] != null)
                ReleasePreviewObject(_previewObjects[i]);
        }

        _previewObjects.Clear();
    }

    public void UpdatePreview(
        Context context,
        bool isDrawing,
        Vector2Int? pendingStartCell,
        Vector2Int currentDragCell,
        DragFirstAxis dragFirstAxis)
    {
        if (!isDrawing || !pendingStartCell.HasValue)
        {
            ClearPreview();
            return;
        }

        RebuildPreview(context, pendingStartCell.Value, currentDragCell, dragFirstAxis);
    }

    private void RebuildPreview(Context context, Vector2Int startCell, Vector2Int endCell, DragFirstAxis dragFirstAxis)
    {
        ClearPreview();

        RoadPathPlanningSystem.PreviewPlan previewPlan = context.PathPlanningSystem.BuildPreviewPlan(
            startCell,
            endCell,
            dragFirstAxis,
            context.NetworkSystem);
        List<Vector2Int> path = previewPlan.Path;
        if (path.Count == 0)
            return;

        if (path.Count == 1)
        {
            TileConnectionMask defaultMask = new(false, true, false, false);
            if (context.TryGetVariant(RoadVisualType.End, defaultMask, out var defaultVariant) && context.EndPrefab != null)
            {
                GameObject preview = GetPreviewObject(context, RoadVisualType.End);
                if (preview == null)
                    return;

                preview.name = $"End_Preview_{startCell.x}_{startCell.y}";
                ApplyPlacement(context, preview.transform, startCell, defaultVariant);
                _previewObjects.Add(preview);
            }

            return;
        }

        foreach (var cell in previewPlan.DirtyCells)
        {
            TileConnectionMask mask = context.PathPlanningSystem.GetPreviewMask(cell, previewPlan.ProposedEdges, context.NetworkSystem);
            RoadVisualType type = context.ResolveVisualType(cell, mask);
            if (type == RoadVisualType.None || !context.TryGetVariant(type, mask, out var variant))
                continue;

            GameObject preview = GetPreviewObject(context, type);
            if (preview == null)
                continue;

            preview.name = $"{type}_Preview_{cell.x}_{cell.y}";
            ApplyPlacement(context, preview.transform, cell, variant);
            _previewObjects.Add(preview);
        }
    }

    private GameObject GetPreviewObject(Context context, RoadVisualType type)
    {
        if (_previewPool.TryGetValue(type, out var pool))
        {
            while (pool.Count > 0)
            {
                GameObject pooled = pool.Pop();
                if (pooled == null)
                    continue;

                pooled.SetActive(true);
                return pooled;
            }
        }

        GameObject preview = CreateRuntimeRoadObject(context, type);
        if (preview != null)
            _previewObjectTypes[preview] = type;

        return preview;
    }

    private void ReleasePreviewObject(GameObject preview)
    {
        if (preview == null)
            return;

        preview.SetActive(false);

        if (!_previewObjectTypes.TryGetValue(preview, out var type))
        {
            UnityEngine.Object.Destroy(preview);
            return;
        }

        if (!_previewPool.TryGetValue(type, out var pool))
        {
            pool = new Stack<GameObject>();
            _previewPool.Add(type, pool);
        }

        pool.Push(preview);
    }

    private static GameObject CreateRuntimeRoadObject(Context context, RoadVisualType type)
    {
        if (!context.VisualData.TryGetValue(type, out var visualData) ||
            visualData.Mesh == null ||
            visualData.Materials == null)
        {
            return null;
        }

        GameObject roadObject = new($"{type}_Preview");
        roadObject.transform.SetParent(context.RoadRoot, false);

        var meshFilter = roadObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = visualData.Mesh;

        var meshRenderer = roadObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = visualData.Materials;
        SetPreviewMaterials(meshRenderer, context.PreviewAlpha);

        return roadObject;
    }

    private static void SetPreviewMaterials(Renderer renderer, float alpha)
    {
        var materials = renderer.sharedMaterials;
        var previewMaterials = new Material[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null)
            {
                previewMaterials[i] = null;
                continue;
            }

            previewMaterials[i] = new Material(materials[i]);
            if (previewMaterials[i].HasProperty("_Color"))
            {
                Color color = previewMaterials[i].color;
                color.a = alpha;
                previewMaterials[i].color = color;
            }
        }

        renderer.sharedMaterials = previewMaterials;
    }

    private static void ApplyPlacement(Context context, Transform target, Vector2Int cell, VariantData variant)
    {
        target.SetPositionAndRotation(
            GetPlacementPosition(context, cell, variant),
            variant.Rotation);
        target.localScale = variant.Scale;
    }

    private static Vector3 GetPlacementPosition(Context context, Vector2Int cell, VariantData variant)
    {
        Vector3 basePosition = context.GridOrigin + new Vector3(
            cell.x * context.RoadGridSize,
            context.BuildPlaneY,
            cell.y * context.RoadGridSize);
        Vector3[] corners =
        {
            new(0f, 0f, 0f),
            new(context.RoadGridSize, 0f, 0f),
            new(0f, 0f, context.RoadGridSize),
            new(context.RoadGridSize, 0f, context.RoadGridSize)
        };

        float minX = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 transformed = variant.Rotation * Vector3.Scale(corners[i], variant.Scale);
            if (transformed.x < minX)
                minX = transformed.x;
            if (transformed.z < minZ)
                minZ = transformed.z;
        }

        return basePosition - new Vector3(minX, 0f, minZ);
    }
}
