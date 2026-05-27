using System.Collections.Generic;
using UnityEngine;
using PlotCandidate = RuntimeCityBuildingPlotSystem.PlotCandidate;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed class RuntimeCityEntryBuildingSpawnSystem
{
    public void PlaceEntryShops(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementSystem placementSystem,
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
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementSystem placementSystem,
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
