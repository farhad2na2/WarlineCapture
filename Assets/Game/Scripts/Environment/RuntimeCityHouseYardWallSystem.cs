using System.Collections.Generic;
using UnityEngine;
using ReservedFootprint = RuntimeCityWalkabilityUtilitySystemHelper.ReservedFootprint;
using YardSide = RuntimeCityYardGateUtilitySystemHelper.YardSide;

internal sealed class RuntimeCityHouseYardWallSystem
{
    private readonly RuntimeCityHouseYardWallState _state = new();

    public RuntimeCityHouseYardWallState State => _state;

    public void PlaceHouseYardWalls(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityWalkabilityState walkabilitySystem,
        RuntimeCityYardWallPlanState yardWallPlanSystem,
        RuntimeCityYardGateState yardGateSystem,
        RuntimeCityYardWallVisualState yardWallVisualHelper,
        RuntimeCityVisualPresentationSystemHelper visualSystem,
        List<GameObject> houseWallPrefabs,
        GameObject houseWallGatePrefab,
        GameObject houseWallPillarPrefab,
        float houseWallChance,
        int houseWallMinDistanceCells,
        int houseWallMaxDistanceCells,
        List<RectInt> houseFootprints,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        GridConfig grid,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        _state.PlaceHouseYardWalls(
            context,
            placementSystem,
            prefabSelectionSystem,
            walkabilitySystem,
            yardWallPlanSystem,
            yardGateSystem,
            yardWallVisualHelper,
            visualSystem,
            houseWallPrefabs,
            houseWallGatePrefab,
            houseWallPillarPrefab,
            houseWallChance,
            houseWallMinDistanceCells,
            houseWallMaxDistanceCells,
            houseFootprints,
            centerRoadCell,
            roadCellSizeInGridCells,
            roadCells,
            grid,
            ref rng,
            reservedFootprints);
    }
}

internal sealed class RuntimeCityHouseYardWallState
{
    public void PlaceHouseYardWalls(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityWalkabilityState walkabilitySystem,
        RuntimeCityYardWallPlanState yardWallPlanSystem,
        RuntimeCityYardGateState yardGateSystem,
        RuntimeCityYardWallVisualState yardWallVisualHelper,
        RuntimeCityVisualPresentationSystemHelper visualSystem,
        List<GameObject> houseWallPrefabs,
        GameObject houseWallGatePrefab,
        GameObject houseWallPillarPrefab,
        float houseWallChance,
        int houseWallMinDistanceCells,
        int houseWallMaxDistanceCells,
        List<RectInt> houseFootprints,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        GridConfig grid,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        if (houseFootprints == null || houseFootprints.Count == 0)
            return;
        if (houseWallPrefabs == null || houseWallPrefabs.Count == 0 || houseWallGatePrefab == null)
            return;

        RuntimeCityYardWallPlanUtilitySystemHelper.HousePlan plan = yardWallPlanSystem.CreateHousePlan(
            houseFootprints,
            houseWallChance,
            prefabSelectionSystem,
            ref rng);
        int builtCount = 0;
        for (int i = 0; i < plan.ShuffledHouses.Count && builtCount < plan.TargetCount; i++)
        {
            if (TryBuildHouseYardWall(
                    context,
                    placementSystem,
                    prefabSelectionSystem,
                    walkabilitySystem,
                    yardWallPlanSystem,
                    yardGateSystem,
                    yardWallVisualHelper,
                    visualSystem,
                    houseWallPrefabs,
                    houseWallGatePrefab,
                    houseWallPillarPrefab,
                    houseWallMinDistanceCells,
                    houseWallMaxDistanceCells,
                    plan.ShuffledHouses[i],
                    centerRoadCell,
                    roadCellSizeInGridCells,
                    roadCells,
                    grid,
                    ref rng,
                    reservedFootprints))
            {
                builtCount++;
            }
        }
    }

    private bool TryBuildHouseYardWall(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityWalkabilityState walkabilitySystem,
        RuntimeCityYardWallPlanState yardWallPlanSystem,
        RuntimeCityYardGateState yardGateSystem,
        RuntimeCityYardWallVisualState yardWallVisualHelper,
        RuntimeCityVisualPresentationSystemHelper visualSystem,
        List<GameObject> houseWallPrefabs,
        GameObject houseWallGatePrefab,
        GameObject houseWallPillarPrefab,
        int houseWallMinDistanceCells,
        int houseWallMaxDistanceCells,
        RectInt houseRect,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        GridConfig grid,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        if (!yardWallPlanSystem.TryFindYardRect(
                walkabilitySystem,
                prefabSelectionSystem,
                houseRect,
                houseWallMinDistanceCells,
                houseWallMaxDistanceCells,
                roadCellSizeInGridCells,
                roadCells,
                reservedFootprints,
                grid,
                ref rng,
                out RectInt yardRect))
        {
            return false;
        }

        Vector2Int cityCenterGridCell = new(
            centerRoadCell.x * roadCellSizeInGridCells + Mathf.FloorToInt(roadCellSizeInGridCells * 0.5f),
            centerRoadCell.y * roadCellSizeInGridCells + Mathf.FloorToInt(roadCellSizeInGridCells * 0.5f));
        YardSide gateSide = yardGateSystem.GetPreferredYardGateSide(houseRect, cityCenterGridCell);
        GameObject wallPrefab = prefabSelectionSystem.GetRandomPrefab(houseWallPrefabs, ref rng);
        if (wallPrefab == null)
            return false;

        yardWallVisualHelper.BuildYardBoundaryVisuals(context, placementSystem, prefabSelectionSystem, visualSystem, yardGateSystem, yardRect, gateSide, wallPrefab, houseWallGatePrefab, houseWallPillarPrefab, grid);
        walkabilitySystem.ReserveFootprint(reservedFootprints, yardRect.position, yardRect.size, 0);
        return true;
    }
}
