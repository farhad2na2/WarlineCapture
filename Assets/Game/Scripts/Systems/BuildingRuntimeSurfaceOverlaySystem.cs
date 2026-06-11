using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingRuntimeSurfaceOverlaySystem
{
    private const float OverlayPadding = 0.25f;
    private readonly List<Renderer> _rendererBuffer = new(32);
    private readonly Dictionary<int, Transform> _runwayTransformByBuildingId = new();
    private readonly Dictionary<Renderer, bool> _runwaySurfaceRendererCache = new();

    public void Publish(
        EntityManager em,
        Entity boundaryEntity,
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
    {
        if (boundaryEntity == Entity.Null || runtimeBuildings == null)
            return;

        DynamicBuffer<BuildingRuntimeSurfaceOverlay> buffer = EnsureBoundaryBuffer(em, boundaryEntity);
        buffer.Clear();

        foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
        {
            RuntimeBuildingEntity building = pair.Value;
            if (building == null ||
                building.IsDestroyed ||
                building.Instance == null ||
                building.Definition == null)
            {
                continue;
            }

            if (TryBuildRunwayOverlay(building, out BuildingRuntimeSurfaceOverlay overlay))
                buffer.Add(overlay);
        }
    }

    private bool TryBuildRunwayOverlay(RuntimeBuildingEntity building, out BuildingRuntimeSurfaceOverlay overlay)
    {
        overlay = default;
        Transform instanceTransform = building.Instance.transform;
        BuildingDefinition definition = building.Definition;
        if (!definition.HasRunway)
            return false;

        Vector3 runwaySurfaceCenter = instanceTransform.TransformPoint(definition.RunwayLocalPosition);
        Vector3 center = runwaySurfaceCenter;
        Quaternion rotation = instanceTransform.rotation * definition.RunwayLocalRotation;

        float height = ResolveRunwayOverlayHeight(runwaySurfaceCenter);
        if (!TryResolveRunwayBounds(building.Id, instanceTransform, out Bounds visualBounds))
            return false;

        center = visualBounds.center;
        center.y = height;
        rotation = Quaternion.identity;
        Vector3 scaledHalfExtents = visualBounds.extents;

        overlay = new BuildingRuntimeSurfaceOverlay
        {
            BuildingRuntimeId = building.Id,
            Center = new float3(center.x, center.y, center.z),
            Rotation = new quaternion(rotation.x, rotation.y, rotation.z, rotation.w),
            HalfExtents = new float2(
                Mathf.Max(0.01f, Mathf.Abs(scaledHalfExtents.x) + OverlayPadding),
                Mathf.Max(0.01f, Mathf.Abs(scaledHalfExtents.z) + OverlayPadding)),
            Height = height,
            Normal = new float3(0f, 1f, 0f),
            SurfaceType = MapSurfaceType.Road,
            MovementMask = MapSurfaceMovementMask.AllGroundUnits
        };
        return true;
    }

    internal static float ResolveRunwayOverlayHeight(Vector3 runwaySurfaceCenter)
    {
        return runwaySurfaceCenter.y;
    }

    private bool TryResolveRunwayBounds(int buildingRuntimeId, Transform root, out Bounds bounds)
    {
        bounds = default;
        if (!TryGetRunwayTransform(buildingRuntimeId, root, out Transform runway))
            return false;

        _rendererBuffer.Clear();
        runway.GetComponentsInChildren(true, _rendererBuffer);

        bool found = TryEncapsulateRunwayRenderers(_rendererBuffer, filteredSurfaceOnly: true, out bounds);
        if (found)
            return true;

        found = false;
        for (int i = 0; i < _rendererBuffer.Count; i++)
        {
            Renderer renderer = _rendererBuffer[i];
            if (renderer == null)
                continue;

            if (found)
                bounds.Encapsulate(renderer.bounds);
            else
                bounds = renderer.bounds;
            found = true;
        }

        return found;
    }

    private bool TryEncapsulateRunwayRenderers(List<Renderer> renderers, bool filteredSurfaceOnly, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        if (renderers == null)
            return false;

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (filteredSurfaceOnly && !IsRunwaySurfaceRenderer(renderer))
                continue;

            if (found)
                bounds.Encapsulate(renderer.bounds);
            else
                bounds = renderer.bounds;
            found = true;
        }

        return found;
    }

    private bool IsRunwaySurfaceRenderer(Renderer renderer)
    {
        if (_runwaySurfaceRendererCache.TryGetValue(renderer, out bool isSurfaceRenderer))
            return isSurfaceRenderer;

        isSurfaceRenderer = ResolveRunwaySurfaceRenderer(renderer);
        _runwaySurfaceRendererCache[renderer] = isSurfaceRenderer;
        return isSurfaceRenderer;
    }

    private static bool ResolveRunwaySurfaceRenderer(Renderer renderer)
    {
        string objectName = renderer.transform.name;
        if (IsRunwayExcludedName(objectName))
            return false;

        bool isRunwaySurface = IsRunwaySurfaceName(objectName);
        if (renderer is not MeshRenderer)
            return isRunwaySurface;

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        string meshName = meshFilter != null && meshFilter.sharedMesh != null
            ? meshFilter.sharedMesh.name
            : null;
        if (IsRunwayExcludedName(meshName))
            return false;

        return isRunwaySurface || IsRunwaySurfaceName(meshName);
    }

    private bool TryGetRunwayTransform(int buildingRuntimeId, Transform root, out Transform runway)
    {
        if (_runwayTransformByBuildingId.TryGetValue(buildingRuntimeId, out runway))
            return runway != null;

        runway = FindChildRecursive(root, "Runway");
        _runwayTransformByBuildingId[buildingRuntimeId] = runway;
        return runway != null;
    }

    private static bool IsRunwayExcludedName(string name)
    {
        return ContainsOrdinalIgnoreCase(name, "Barrier") ||
               ContainsOrdinalIgnoreCase(name, "Fence") ||
               ContainsOrdinalIgnoreCase(name, "Sign") ||
               ContainsOrdinalIgnoreCase(name, "Light");
    }

    private static bool IsRunwaySurfaceName(string name)
    {
        return ContainsOrdinalIgnoreCase(name, "Runway") ||
               ContainsOrdinalIgnoreCase(name, "Road");
    }

    private static bool ContainsOrdinalIgnoreCase(string value, string part)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static DynamicBuffer<BuildingRuntimeSurfaceOverlay> EnsureBoundaryBuffer(EntityManager em, Entity entity)
    {
        if (!em.HasBuffer<BuildingRuntimeSurfaceOverlay>(entity))
            em.AddBuffer<BuildingRuntimeSurfaceOverlay>(entity);

        return em.GetBuffer<BuildingRuntimeSurfaceOverlay>(entity);
    }
}
