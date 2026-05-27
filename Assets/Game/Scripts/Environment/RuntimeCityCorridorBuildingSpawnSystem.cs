using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;
using PlotCandidate = RuntimeCityBuildingPlotSystem.PlotCandidate;

internal sealed class RuntimeCityCorridorBuildingSpawnSystem
{
    public void SpawnCorridorEntranceBuildings(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementSystem placementSystem,
        CityLayoutData city,
        Vector2Int connectorCell,
        Vector2Int direction,
        int corridorLength,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng)
    {
        RuntimeCityConfigSystem.Snapshot config = context.Config;
        if (corridorLength <= 0)
            return;

        List<PlotCandidate> corridorPlots = context.BuildingPlotSystem.BuildCorridorRoadsidePlots(connectorCell, direction, corridorLength);

        if (corridorPlots.Count == 0)
            return;

        context.PrefabSelectionSystem.Shuffle(corridorPlots, ref rng);

        var usedPlotCells = new List<Vector2Int>();
        var placementAnchors = new List<RectInt>();
        placementSystem.PlaceFromPlots(context, config.ShopPrefabs, corridorPlots, Mathf.Min(2, Mathf.Max(0, config.ShopCount)), 0, roadCellSizeInGridCells, "Corridor Shop", "Shop near the city entrance road.", config.DefaultBuildingMaxHealth, ref rng, usedPlotCells, city.ReservedFootprints, placementAnchors);
        placementSystem.PlaceFromPlots(context, config.HousePrefabs, corridorPlots, Mathf.Min(6, Mathf.Max(0, config.HouseCount)), 0, roadCellSizeInGridCells, "Corridor House", "House near the city entrance road.", config.DefaultBuildingMaxHealth, ref rng, usedPlotCells, city.ReservedFootprints, placementAnchors);
    }
}
