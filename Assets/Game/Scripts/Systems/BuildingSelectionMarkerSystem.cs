using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingSelectionMarkerSystem
{
    private const float MarkerHeightOffset = 0.035f;
    private const string RuntimeMarkerName = "BuildingSelectionMarkerRuntime";

    public delegate bool TryGetGridDelegate(out GridConfig grid);
    public delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);
    public delegate void DestroyObjectDelegate(UnityEngine.Object target);

    public readonly struct Context
    {
        public readonly RuntimeBuildingSystem<RuntimeBuildingData> RuntimeBuildingSystem;
        public readonly IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings;
        public readonly TryGetGridDelegate TryGetGrid;
        public readonly GetFootprintCenterDelegate GetFootprintCenter;
        public readonly GameObject MarkerPrefab;
        public readonly Transform MarkerParent;
        public readonly BuildingVisualSystem VisualSystem;
        public readonly FactionVisualSettings FactionVisualSettings;
        public readonly MaterialPropertyBlock MarkerPropertyBlock;
        public readonly DestroyObjectDelegate DestroyObject;

        public Context(
            RuntimeBuildingSystem<RuntimeBuildingData> runtimeBuildingSystem,
            IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
            TryGetGridDelegate tryGetGrid,
            GetFootprintCenterDelegate getFootprintCenter,
            GameObject markerPrefab,
            Transform markerParent,
            BuildingVisualSystem visualSystem,
            FactionVisualSettings factionVisualSettings,
            MaterialPropertyBlock markerPropertyBlock,
            DestroyObjectDelegate destroyObject)
        {
            RuntimeBuildingSystem = runtimeBuildingSystem;
            RuntimeBuildings = runtimeBuildings;
            TryGetGrid = tryGetGrid;
            GetFootprintCenter = getFootprintCenter;
            MarkerPrefab = markerPrefab;
            MarkerParent = markerParent;
            VisualSystem = visualSystem;
            FactionVisualSettings = factionVisualSettings;
            MarkerPropertyBlock = markerPropertyBlock;
            DestroyObject = destroyObject;
        }
    }

    private GameObject _markerInstance;
    private Renderer[] _markerRenderers;
    private Vector3 _baseRendererSize = Vector3.one;

    internal GameObject RuntimeMarkerForTests => _markerInstance;

    public void Refresh(Context context)
    {
        if (!TryResolveSelection(context, out RuntimeBuildingData building, out GridConfig grid))
        {
            Hide();
            return;
        }

        if (!EnsureMarker(context))
        {
            Hide();
            return;
        }

        Vector2Int footprint = building.Definition != null
            ? building.Definition.FootprintCells
            : Vector2Int.one;
        Vector3 center = context.GetFootprintCenter(building.OriginCell, footprint, grid);
        float y = building.Instance != null
            ? building.Instance.transform.position.y
            : center.y;
        center.y = y + MarkerHeightOffset;

        Transform markerTransform = _markerInstance.transform;
        markerTransform.SetPositionAndRotation(center, Quaternion.identity);
        markerTransform.localScale = ResolveScale(footprint, grid);
        SetActive(true);
    }

    public void Hide()
    {
        SetActive(false);
    }

    public void Dispose(Context context)
    {
        if (_markerInstance == null)
            return;

        if (context.DestroyObject != null)
        {
            context.DestroyObject(_markerInstance);
        }
        else if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(_markerInstance);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(_markerInstance);
        }

        _markerInstance = null;
        _markerRenderers = null;
        _baseRendererSize = Vector3.one;
    }

    private bool TryResolveSelection(Context context, out RuntimeBuildingData building, out GridConfig grid)
    {
        building = null;
        grid = default;

        if (context.RuntimeBuildingSystem == null ||
            context.RuntimeBuildings == null ||
            context.TryGetGrid == null ||
            context.GetFootprintCenter == null ||
            !context.TryGetGrid(out grid))
        {
            return false;
        }

        int? activeBuildingId = context.RuntimeBuildingSystem.CurrentActiveBuildingId;
        if (!activeBuildingId.HasValue)
            return false;

        return context.RuntimeBuildings.TryGetValue(activeBuildingId.Value, out building) &&
            building != null &&
            !building.IsDestroyed &&
            building.Definition != null;
    }

    private bool EnsureMarker(Context context)
    {
        if (_markerInstance != null)
            return true;

        if (context.MarkerPrefab == null)
            return false;

        _markerInstance = UnityEngine.Object.Instantiate(context.MarkerPrefab, context.MarkerParent);
        _markerInstance.name = RuntimeMarkerName;
        _markerInstance.SetActive(false);
        _markerRenderers = _markerInstance.GetComponentsInChildren<Renderer>(true);
        _baseRendererSize = CalculateRendererSize(_markerRenderers);

        Color markerColor = context.FactionVisualSettings != null
            ? context.FactionVisualSettings.GetColor(0)
            : new Color(0.15f, 0.85f, 0.2f, 1f);
        context.VisualSystem?.ApplyMarkerColor(_markerRenderers, markerColor, context.MarkerPropertyBlock);
        return true;
    }

    private Vector3 ResolveScale(Vector2Int footprint, GridConfig grid)
    {
        float width = Mathf.Max(grid.CellSize, footprint.x * grid.CellSize);
        float depth = Mathf.Max(grid.CellSize, footprint.y * grid.CellSize);
        float baseX = Mathf.Max(0.001f, _baseRendererSize.x);
        float baseZ = Mathf.Max(0.001f, _baseRendererSize.z);
        return new Vector3(width / baseX, 1f, depth / baseZ);
    }

    private static Vector3 CalculateRendererSize(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0)
            return Vector3.one;

        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? bounds.size : Vector3.one;
    }

    private void SetActive(bool active)
    {
        if (_markerInstance != null && _markerInstance.activeSelf != active)
            _markerInstance.SetActive(active);
    }
}
