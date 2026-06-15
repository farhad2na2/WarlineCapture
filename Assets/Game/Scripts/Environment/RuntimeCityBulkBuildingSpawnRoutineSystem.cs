using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;
using PlotCandidate = RuntimeCityBuildingPlotSystem.PlotCandidate;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed class RuntimeCityBulkBuildingSpawnRoutineSystem
{
    public sealed class GenerationRandomState
    {
        public Unity.Mathematics.Random Value;
    }

    public delegate void PlaceHouseYardWallsAction(
        List<RectInt> houseFootprints,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        GridConfig grid,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints);

    public delegate void PlaceCityDecorationBuildingsAction(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        List<GameObject> prefabs,
        int count,
        Vector2Int centerRoadCell,
        int townRadius,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints);

    public IEnumerator SpawnRoutine(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementSystem placementSystem,
        RuntimeCityBulkPlotPlanSystem plotPlanSystem,
        RuntimeCityEntryBuildingSpawnSystem entryBuildingSpawnSystem,
        RuntimeCityRoadsideBuildingSpawnSystem roadsideBuildingSpawnSystem,
        RuntimeCityRuralBuildingSpawnSystem ruralBuildingSpawnSystem,
        CityLayoutData city,
        GridConfig grid,
        int roadCellSizeInGridCells,
        GenerationRandomState rng,
        PlaceHouseYardWallsAction placeHouseYardWalls,
        PlaceCityDecorationBuildingsAction placeCityDecorationBuildings)
    {
        RuntimeCityConfigSystem.Snapshot config = context.Config;
        Vector2Int centerRoadCell = city.CenterRoadCell;
        int townRadius = city.TownRadius;
        HashSet<Vector2Int> roadCells = city.RoadCells;
        List<ReservedFootprint> reservedFootprints = city.ReservedFootprints;

        RuntimeCityBulkPlotPlanSystem.Plan plotPlan = plotPlanSystem.CreatePlan(context, city, townRadius, roadCells, centerRoadCell, ref rng.Value);
        List<PlotCandidate> centralPlots = plotPlan.CentralPlots;
        List<PlotCandidate> outerPlots = plotPlan.OuterPlots;
        List<PlotCandidate> entryPlots = plotPlan.EntryPlots;

        context.VisualSystem?.EnsureCityVisualRoot();

        var usedPlotCells = new List<Vector2Int>();
        var shopAndHouseFootprints = new List<RectInt>();
        var houseFootprints = new List<RectInt>();
        entryBuildingSpawnSystem.PlaceEntryShops(context, placementSystem, entryPlots, roadCellSizeInGridCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;
        entryBuildingSpawnSystem.PlaceEntryHouses(context, placementSystem, entryPlots, roadCellSizeInGridCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        yield return null;

        RuntimeCityRoadsideBuildingSpawnSystem.Plan roadsidePlan = roadsideBuildingSpawnSystem.CreatePlan(context);
        roadsideBuildingSpawnSystem.PlaceCentralShops(context, placementSystem, centralPlots, roadsidePlan, roadCellSizeInGridCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;
        roadsideBuildingSpawnSystem.PlaceGasStations(context, placementSystem, outerPlots, roadCellSizeInGridCells, ref rng.Value, usedPlotCells, reservedFootprints);
        yield return null;
        roadsideBuildingSpawnSystem.PlaceOuterShops(context, placementSystem, outerPlots, roadsidePlan, roadCellSizeInGridCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;

        int ruralHouseTarget = roadsidePlan.RuralHouseTarget;
        roadsideBuildingSpawnSystem.PlaceRoadsideHouses(context, placementSystem, outerPlots, roadsidePlan, roadCellSizeInGridCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        yield return null;
        ruralBuildingSpawnSystem.PlaceRuralBuildings(context, placementSystem, config.HousePrefabs, ruralHouseTarget, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        yield return null;
        placeHouseYardWalls(houseFootprints, centerRoadCell, roadCellSizeInGridCells, roadCells, grid, ref rng.Value, reservedFootprints);
        yield return null;

        int ruralOtherTarget = Mathf.RoundToInt(Mathf.Max(0, config.OtherBuildingCount) * Mathf.Clamp01(config.RuralHouseRatio));
        int roadsideOtherTarget = Mathf.Max(0, config.OtherBuildingCount - ruralOtherTarget);
        placementSystem.PlaceFromPlots(context, config.OtherBuildingPrefabs, outerPlots, roadsideOtherTarget, 1, roadCellSizeInGridCells, "Village Building", "Old town side building.", config.DefaultBuildingMaxHealth, ref rng.Value, usedPlotCells, reservedFootprints);
        yield return null;
        ruralBuildingSpawnSystem.PlaceRuralBuildings(context, placementSystem, config.OtherBuildingPrefabs, ruralOtherTarget, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng.Value, usedPlotCells, reservedFootprints);
        yield return null;
        placeCityDecorationBuildings(context, config.CityDecorationPrefabs, config.CityDecorationBuildingCount, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;
    }
}
