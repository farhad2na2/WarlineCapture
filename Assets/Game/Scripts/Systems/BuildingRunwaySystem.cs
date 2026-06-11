using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingRunwaySystem
{
    public delegate Vector2Int GetPlacementFootprintDelegate(BuildingDefinition definition, bool rotateVertical);

    public bool TryGetNearestAirportRunway(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        Vector3 origin,
        out RuntimeBuildingEntity airport,
        out Vector3 runwayCenter,
        out Quaternion runwayRotation,
        out Vector3 runwayHalfExtents)
    {
        airport = null;
        runwayCenter = Vector3.zero;
        runwayRotation = Quaternion.identity;
        runwayHalfExtents = new Vector3(8f, 0.5f, 24f);
        if (runtimeBuildings == null)
            return false;

        float bestDistance = float.PositiveInfinity;
        if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingDictionary)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingDictionary)
            {
                TryUseNearestAirportRunway(pair.Value, origin, ref bestDistance, ref airport, ref runwayCenter, ref runwayRotation, ref runwayHalfExtents);
            }

            return airport != null;
        }

        foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
        {
            TryUseNearestAirportRunway(pair.Value, origin, ref bestDistance, ref airport, ref runwayCenter, ref runwayRotation, ref runwayHalfExtents);
        }

        return airport != null;
    }

    public bool HasAvailableAirportRunway(IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
    {
        if (runtimeBuildings == null)
            return false;

        if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingDictionary)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingDictionary)
            {
                if (IsAvailableAirportRunway(pair.Value))
                    return true;
            }

            return false;
        }

        foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
        {
            if (IsAvailableAirportRunway(pair.Value))
                return true;
        }

        return false;
    }

    private static bool IsAvailableAirportRunway(RuntimeBuildingEntity candidate)
    {
        return candidate != null &&
               !candidate.IsDestroyed &&
               candidate.Instance != null &&
               candidate.Definition != null &&
               candidate.Definition.HasRunway;
    }

    private static void TryUseNearestAirportRunway(
        RuntimeBuildingEntity candidate,
        Vector3 origin,
        ref float bestDistance,
        ref RuntimeBuildingEntity airport,
        ref Vector3 runwayCenter,
        ref Quaternion runwayRotation,
        ref Vector3 runwayHalfExtents)
    {
        if (candidate == null || candidate.IsDestroyed || candidate.Instance == null || candidate.Definition == null || !candidate.Definition.HasRunway)
            return;

        Vector3 candidateCenter = candidate.Instance.transform.TransformPoint(candidate.Definition.RunwayLocalPosition);
        float distance = (candidateCenter - origin).sqrMagnitude;
        if (distance >= bestDistance)
            return;

        bestDistance = distance;
        airport = candidate;
        runwayCenter = candidateCenter;
        runwayRotation = candidate.Instance.transform.rotation * candidate.Definition.RunwayLocalRotation;
        runwayHalfExtents = Vector3.Scale(candidate.Definition.RunwayHalfExtents, candidate.Instance.transform.lossyScale);
    }

    public RectInt GetEffectivePlacementRect(
        BuildingDefinition definition,
        Vector2Int originCell,
        GridConfig grid,
        bool rotateVertical,
        float buildPlaneY,
        GetPlacementFootprintDelegate getPlacementFootprint)
    {
        Vector2Int modelFootprint = getPlacementFootprint != null ? getPlacementFootprint(definition, rotateVertical) : Vector2Int.one;
        RectInt modelRect = new(originCell, modelFootprint);
        if (definition == null || !definition.HasRunway)
            return modelRect;

        if (!TryGetRunwayFootprintRect(definition, originCell, grid, rotateVertical, buildPlaneY, getPlacementFootprint, out RectInt runwayRect))
            return modelRect;

        return UnionRects(modelRect, runwayRect);
    }

    public bool TryGetRunwayLocalData(GameObject prefab, out Vector3 localPosition, out Quaternion localRotation, out Vector3 halfExtents)
    {
        localPosition = Vector3.zero;
        localRotation = Quaternion.identity;
        halfExtents = new Vector3(8f, 0.5f, 24f);
        if (prefab == null)
            return false;

        Transform runway = null;
        Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == "Runway")
            {
                runway = transforms[i];
                break;
            }
        }

        if (runway == null)
            return false;

        Transform runwayStart = null;
        Transform runwayEnd = null;
        for (int i = 0; i < runway.childCount; i++)
        {
            Transform child = runway.GetChild(i);
            if (child == null)
                continue;
            if (child.name == "Runway_Start")
                runwayStart = child;
            else if (child.name == "Runway_End")
                runwayEnd = child;
        }

        if (runwayStart != null && runwayEnd != null)
        {
            Vector3 worldStart = runwayStart.position;
            Vector3 worldEnd = runwayEnd.position;
            Vector3 worldDirection = worldEnd - worldStart;
            Vector3 planarDirection = new(worldDirection.x, 0f, worldDirection.z);
            if (planarDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 worldCenter = (worldStart + worldEnd) * 0.5f;
                localPosition = prefab.transform.InverseTransformPoint(worldCenter);
                Quaternion worldRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
                localRotation = Quaternion.Inverse(prefab.transform.rotation) * worldRotation;
                halfExtents = new Vector3(
                    8f,
                    0.5f,
                    Mathf.Max(8f, planarDirection.magnitude * 0.5f));
                return true;
            }
        }

        localPosition = runway.localPosition;
        localRotation = runway.localRotation;

        Renderer runwayRenderer = runway.GetComponentInChildren<Renderer>(true);
        if (runwayRenderer != null)
        {
            Bounds bounds = runwayRenderer.localBounds;
            halfExtents = bounds.extents;
            if (halfExtents.x <= 0.01f || halfExtents.z <= 0.01f)
                halfExtents = new Vector3(8f, 0.5f, 24f);
        }

        return true;
    }

    private bool TryGetRunwayFootprintRect(
        BuildingDefinition definition,
        Vector2Int originCell,
        GridConfig grid,
        bool rotateVertical,
        float buildPlaneY,
        GetPlacementFootprintDelegate getPlacementFootprint,
        out RectInt runwayRect)
    {
        runwayRect = default;
        if (definition == null || !definition.HasRunway || grid.CellSize <= 0f || getPlacementFootprint == null)
            return false;

        Vector2Int modelFootprint = getPlacementFootprint(definition, rotateVertical);
        Vector3 buildingCenter = GetFootprintCenter(originCell, modelFootprint, grid, buildPlaneY);
        Vector3 visualOffset = definition.HasLocalBounds
            ? new Vector3(definition.LocalBounds.center.x, 0f, definition.LocalBounds.center.z)
            : Vector3.zero;
        Quaternion placementRotation = rotateVertical ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        Vector3 runwayCenter = buildingCenter + placementRotation * (definition.RunwayLocalPosition - visualOffset);
        Quaternion runwayRotation = placementRotation * definition.RunwayLocalRotation;

        Vector3 halfExtents = definition.RunwayHalfExtents;
        Vector3[] corners =
        {
            runwayCenter + runwayRotation * new Vector3(-halfExtents.x, 0f, -halfExtents.z),
            runwayCenter + runwayRotation * new Vector3(-halfExtents.x, 0f, halfExtents.z),
            runwayCenter + runwayRotation * new Vector3(halfExtents.x, 0f, -halfExtents.z),
            runwayCenter + runwayRotation * new Vector3(halfExtents.x, 0f, halfExtents.z)
        };

        float minX = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxZ = float.NegativeInfinity;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 local = corners[i] - (Vector3)grid.Origin;
            minX = Mathf.Min(minX, local.x);
            minZ = Mathf.Min(minZ, local.z);
            maxX = Mathf.Max(maxX, local.x);
            maxZ = Mathf.Max(maxZ, local.z);
        }

        int cellMinX = Mathf.FloorToInt(minX / grid.CellSize);
        int cellMinY = Mathf.FloorToInt(minZ / grid.CellSize);
        int cellMaxX = Mathf.CeilToInt(maxX / grid.CellSize);
        int cellMaxY = Mathf.CeilToInt(maxZ / grid.CellSize);
        if (cellMaxX <= cellMinX || cellMaxY <= cellMinY)
            return false;

        runwayRect = new RectInt(cellMinX, cellMinY, cellMaxX - cellMinX, cellMaxY - cellMinY);
        return true;
    }

    private static Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid, float buildPlaneY)
    {
        return new Vector3(
            grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
            buildPlaneY,
            grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
    }

    private static RectInt UnionRects(RectInt a, RectInt b)
    {
        int xMin = Mathf.Min(a.xMin, b.xMin);
        int yMin = Mathf.Min(a.yMin, b.yMin);
        int xMax = Mathf.Max(a.xMax, b.xMax);
        int yMax = Mathf.Max(a.yMax, b.yMax);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }
}
