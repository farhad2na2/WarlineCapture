using System.Collections.Generic;
using UnityEngine;
using PlotCandidate = RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed class RuntimeCityEntryBuildingSpawnSystem
{
    private readonly RuntimeCityEntryBuildingSpawnState _state = new();

    public RuntimeCityEntryBuildingSpawnState State => _state;

    public void PlaceEntryShops(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> entryPlots,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        _state.PlaceEntryShops(
            context,
            placementSystem,
            entryPlots,
            roadCellSizeInGridCells,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints);
    }

    public void PlaceEntryHouses(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> entryPlots,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints,
        List<RectInt> houseFootprints)
    {
        _state.PlaceEntryHouses(
            context,
            placementSystem,
            entryPlots,
            roadCellSizeInGridCells,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints,
            houseFootprints);
    }
}

internal sealed class RuntimeCityEntryBuildingSpawnState
{
    public void PlaceEntryShops(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> entryPlots,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        RuntimeCityConfigSystem.Snapshot config = context.Config;
        placementSystem.PlaceFromPlots(
            context,
            config.ShopPrefabs,
            entryPlots,
            Mathf.Min(2, Mathf.Max(0, config.ShopCount)),
            0,
            roadCellSizeInGridCells,
            "Entry Shop",
            "Roadside shop near the city entrance.",
            config.DefaultBuildingMaxHealth,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints);
    }

    public void PlaceEntryHouses(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> entryPlots,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints,
        List<RectInt> houseFootprints)
    {
        RuntimeCityConfigSystem.Snapshot config = context.Config;
        placementSystem.PlaceFromPlots(
            context,
            config.HousePrefabs,
            entryPlots,
            Mathf.Min(4, Mathf.Max(0, config.HouseCount)),
            0,
            roadCellSizeInGridCells,
            "Entry House",
            "House near the city entrance road.",
            config.DefaultBuildingMaxHealth,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints,
            houseFootprints);
    }
}
