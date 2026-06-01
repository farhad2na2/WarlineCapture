using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingRuntimeSurfaceOverlaySystem
{
    private const float OverlayPadding = 0.25f;

    public void Publish(
        EntityManager em,
        Entity boundaryEntity,
        IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings)
    {
        if (boundaryEntity == Entity.Null || runtimeBuildings == null)
            return;

        DynamicBuffer<BuildingRuntimeSurfaceOverlay> buffer = EnsureBoundaryBuffer(em, boundaryEntity);
        buffer.Clear();

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in runtimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
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

    private static bool TryBuildRunwayOverlay(RuntimeBuildingData building, out BuildingRuntimeSurfaceOverlay overlay)
    {
        overlay = default;
        Transform instanceTransform = building.Instance.transform;
        BuildingDefinition definition = building.Definition;
        Vector3 runwaySurfaceCenter = instanceTransform.TransformPoint(definition.RunwayLocalPosition);
        Vector3 center = runwaySurfaceCenter;
        Quaternion rotation = instanceTransform.rotation * definition.RunwayLocalRotation;

        float height = ResolveRunwayOverlayHeight(runwaySurfaceCenter);
        if (!TryResolveRunwayBounds(instanceTransform, out Bounds visualBounds))
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

    private static bool TryResolveRunwayBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        Transform runway = FindChildRecursive(root, "Runway");
        if (runway == null)
            return false;

        Renderer[] renderers = runway.GetComponentsInChildren<Renderer>(true);
        bool found = TryEncapsulateRunwayRenderers(renderers, filteredSurfaceOnly: true, out bounds);
        if (found)
            return true;

        found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
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

    private static bool TryEncapsulateRunwayRenderers(Renderer[] renderers, bool filteredSurfaceOnly, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        if (renderers == null)
            return false;

        for (int i = 0; i < renderers.Length; i++)
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

    private static bool IsRunwaySurfaceRenderer(Renderer renderer)
    {
        string objectName = renderer.transform.name;
        string meshName = renderer is MeshRenderer
            ? renderer.GetComponent<MeshFilter>()?.sharedMesh?.name
            : string.Empty;
        string name = $"{objectName} {meshName}";
        if (name.IndexOf("Barrier", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Fence", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Sign", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        return name.IndexOf("Runway", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0;
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
