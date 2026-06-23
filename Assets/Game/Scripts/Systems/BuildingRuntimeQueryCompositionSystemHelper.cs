using System;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingRuntimeQueryCompositionSystemHelper
{
    internal delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    internal delegate bool TryGetGridDataDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData);

    internal delegate RectInt GetEffectivePlacementRectDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingDefinition definition,
        Vector2Int originCell,
        GridConfig grid,
        bool rotateVertical);

    public bool IsHouseBuilding(BuildingGameplaySourceCompositionSystemHelper source, RuntimeBuildingEntity building)
    {
        if (building?.Definition == null)
            return false;

        if (building.Definition.Role == BuildingRole.House)
            return true;

        if (building.Definition.Role != BuildingRole.None)
            return false;

        GameObject prefab = building.Definition.Prefab;
        string prefabName = prefab != null ? prefab.name : string.Empty;
        if (source.BuildingGameplayDependencyCompositionSystemHelper.IsConfiguredHousePrefab(prefab))
            return true;

        return prefabName.IndexOf("house", StringComparison.OrdinalIgnoreCase) >= 0 &&
               !building.Definition.IsWall;
    }

    public bool TryResolveBuildingFocusWorldPosition(
        BuildingGameplaySourceCompositionSystemHelper source,
        RuntimeBuildingEntity building,
        TryGetEntityManagerDelegate tryGetEntityManager,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (building == null)
            return false;

        if (building.Instance != null &&
            building.Definition != null &&
            source.BuildingGameplayGridDataSystem.TryGetGridForSelection(
                source.BuildingGameplayEcsQuerySystem,
                (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                out GridConfig grid))
        {
            worldPosition = source.BuildingPlacementGridSystem.GetFootprintCenter(
                building.OriginCell,
                building.Definition.FootprintCells,
                grid,
                source.BuildingPlacementStartupSystem.BuildPlaneY);
            return true;
        }

        if (building.Instance == null)
            return false;

        worldPosition = building.Instance.transform.position;
        worldPosition.y = 0f;
        return true;
    }

    public bool TryGetRuntimeBuilding(
        BuildingGameplaySourceCompositionSystemHelper source,
        int id,
        out RuntimeBuildingEntity building)
    {
        if (source.RuntimeBuildingSystem.TryGetBuilding(id, out building) && building != null && !building.IsDestroyed)
            return true;

        building = null;
        return false;
    }

    public RectInt GetEffectivePlacementRect(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingDefinition definition,
        Vector2Int originCell,
        GridConfig grid,
        bool rotateVertical = false)
    {
        return source.BuildingRunwaySystem.GetEffectivePlacementRect(
            definition,
            originCell,
            grid,
            rotateVertical,
            source.BuildingPlacementStartupSystem.BuildPlaneY,
            source.BuildingPlacementGridSystem.GetPlacementFootprint);
    }

    public bool OverlapsAnyRuntimeBuilding(
        BuildingGameplaySourceCompositionSystemHelper source,
        RectInt candidateRect,
        TryGetGridDataDelegate tryGetGridData,
        GetEffectivePlacementRectDelegate getEffectivePlacementRect)
    {
        if (source.RuntimeBuildingSystem.Buildings == null || source.RuntimeBuildingSystem.Buildings.Count == 0)
            return false;
        if (!tryGetGridData(source, out _, out GridConfig grid, out _, out _))
            return false;

        foreach (var entry in source.RuntimeBuildingSystem.Buildings)
        {
            RuntimeBuildingEntity building = entry.Value;
            if (building?.Definition == null || building.IsDestroyed)
                continue;

            RectInt existingRect = getEffectivePlacementRect(source, building.Definition, building.OriginCell, grid, false);
            if (candidateRect.Overlaps(existingRect))
                return true;
        }

        return false;
    }
}
