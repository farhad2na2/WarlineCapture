using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed partial class RuntimeCityDecorationBuildingSpawnSystem : SystemBase
{
    private readonly RuntimeCityDecorationBuildingSpawnState _state = new();

    public RuntimeCityDecorationBuildingSpawnState State => _state;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void PlaceCityDecorationBuildings(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityDecorationPrefabGroupState decorationPrefabGroupSystem,
        RuntimeCityClothCoverSpawnState clothCoverSpawnSystem,
        RuntimeCityArchwaySpawnState archwaySpawnSystem,
        RuntimeCityFreeScatterDecorationState freeScatterDecorationSystem,
        List<GameObject> prefabs,
        int count,
        Vector2Int centerRoadCell,
        int townRadius,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        _state.PlaceCityDecorationBuildings(
            context,
            placementSystem,
            decorationPrefabGroupSystem,
            clothCoverSpawnSystem,
            archwaySpawnSystem,
            freeScatterDecorationSystem,
            prefabs,
            count,
            centerRoadCell,
            townRadius,
            roadCellSizeInGridCells,
            roadCells,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints);
    }
}

internal sealed class RuntimeCityDecorationBuildingSpawnState
{
    public void PlaceCityDecorationBuildings(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityDecorationPrefabGroupState decorationPrefabGroupSystem,
        RuntimeCityClothCoverSpawnState clothCoverSpawnSystem,
        RuntimeCityArchwaySpawnState archwaySpawnSystem,
        RuntimeCityFreeScatterDecorationState freeScatterDecorationSystem,
        List<GameObject> prefabs,
        int count,
        Vector2Int centerRoadCell,
        int townRadius,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        if (prefabs == null || prefabs.Count == 0 || count <= 0)
            return;

        RuntimeCityDecorationPrefabGroupSystem.Groups decorationGroups = decorationPrefabGroupSystem.CreateGroups(prefabs);
        int clothPlaced = clothCoverSpawnSystem.PlaceClothCoverBuildings(
            context,
            placementSystem,
            decorationGroups.ClothCoverPrefabs,
            count,
            ref rng,
            reservedFootprints,
            shopAndHouseFootprints);
        int archwaysPlaced = archwaySpawnSystem.PlaceCentralArchwayBuildings(
            context,
            placementSystem,
            decorationGroups.ArchwayPrefabs,
            count - clothPlaced,
            centerRoadCell,
            roadCellSizeInGridCells,
            roadCells,
            ref rng,
            usedPlotCells,
            reservedFootprints);
        int remainingCount = count - clothPlaced - archwaysPlaced;
        if (remainingCount <= 0)
            return;

        List<GameObject> randomPrefabs = decorationGroups.FreeScatterPrefabs.Count > 0 ? decorationGroups.FreeScatterPrefabs : prefabs;
        freeScatterDecorationSystem.PlaceFreeScatterDecorations(
            context,
            placementSystem,
            randomPrefabs,
            remainingCount,
            centerRoadCell,
            townRadius,
            roadCellSizeInGridCells,
            roadCells,
            ref rng,
            usedPlotCells,
            reservedFootprints);
    }
}
