using System.Collections.Generic;
using UnityEngine;
using PlotCandidate = RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;
using RoadsidePlan = RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper.Plan;

internal sealed class RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper
{
    public readonly struct Plan
    {
        public readonly int CentralShopTarget;
        public readonly int RuralHouseTarget;
        public readonly int RoadsideHouseTarget;

        public Plan(int centralShopTarget, int ruralHouseTarget, int roadsideHouseTarget)
        {
            CentralShopTarget = centralShopTarget;
            RuralHouseTarget = ruralHouseTarget;
            RoadsideHouseTarget = roadsideHouseTarget;
        }
    }

    private readonly RuntimeCityRoadsideBuildingSpawnState _state = new();

    public RuntimeCityRoadsideBuildingSpawnState State => _state;

    public RoadsidePlan CreatePlan(RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context)
    {
        return _state.CreatePlan(context);
    }

    public void PlaceCentralShops(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> centralPlots,
        RoadsidePlan plan,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        _state.PlaceCentralShops(
            context,
            placementSystem,
            centralPlots,
            plan,
            roadCellSizeInGridCells,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints);
    }

    public void PlaceGasStations(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> outerPlots,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints)
    {
        _state.PlaceGasStations(
            context,
            placementSystem,
            outerPlots,
            roadCellSizeInGridCells,
            ref rng,
            usedPlotCells,
            reservedFootprints);
    }

    public void PlaceOuterShops(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> outerPlots,
        RoadsidePlan plan,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        _state.PlaceOuterShops(
            context,
            placementSystem,
            outerPlots,
            plan,
            roadCellSizeInGridCells,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints);
    }

    public void PlaceRoadsideHouses(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> outerPlots,
        RoadsidePlan plan,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints,
        List<RectInt> houseFootprints)
    {
        _state.PlaceRoadsideHouses(
            context,
            placementSystem,
            outerPlots,
            plan,
            roadCellSizeInGridCells,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints,
            houseFootprints);
    }
}

internal sealed class RuntimeCityRoadsideBuildingSpawnState
{
    public RoadsidePlan CreatePlan(RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context)
    {
        RuntimeCityConfigCompositionSystemHelper.Snapshot config = context.Config;
        int centralShopTarget = Mathf.Min(config.ShopCount, Mathf.Max(0, Mathf.RoundToInt(config.ShopCount * 0.65f)));
        int ruralHouseTarget = Mathf.RoundToInt(Mathf.Max(0, config.HouseCount) * Mathf.Clamp01(config.RuralHouseRatio));
        int roadsideHouseTarget = Mathf.Max(0, config.HouseCount - ruralHouseTarget);
        return new RoadsidePlan(centralShopTarget, ruralHouseTarget, roadsideHouseTarget);
    }

    public void PlaceCentralShops(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> centralPlots,
        RoadsidePlan plan,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        RuntimeCityConfigCompositionSystemHelper.Snapshot config = context.Config;
        placementSystem.PlaceFromPlots(
            context,
            config.ShopPrefabs,
            centralPlots,
            plan.CentralShopTarget,
            1,
            roadCellSizeInGridCells,
            "Market",
            "Old town market.",
            config.DefaultBuildingMaxHealth,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints);
    }

    public void PlaceGasStations(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> outerPlots,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints)
    {
        RuntimeCityConfigCompositionSystemHelper.Snapshot config = context.Config;
        placementSystem.PlaceFromPlots(
            context,
            config.GasStationPrefabs,
            outerPlots,
            config.GasStationCount,
            config.GasStationMinSpacingRoadCells,
            roadCellSizeInGridCells,
            "Gas Station",
            "Roadside fuel stop.",
            config.DefaultBuildingMaxHealth,
            ref rng,
            usedPlotCells,
            reservedFootprints);
    }

    public void PlaceOuterShops(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> outerPlots,
        RoadsidePlan plan,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        RuntimeCityConfigCompositionSystemHelper.Snapshot config = context.Config;
        placementSystem.PlaceFromPlots(
            context,
            config.ShopPrefabs,
            outerPlots,
            Mathf.Max(0, config.ShopCount - plan.CentralShopTarget),
            1,
            roadCellSizeInGridCells,
            "Shop",
            "Old town shop.",
            config.DefaultBuildingMaxHealth,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints);
    }

    public void PlaceRoadsideHouses(
        RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        List<PlotCandidate> outerPlots,
        RoadsidePlan plan,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints,
        List<RectInt> houseFootprints)
    {
        RuntimeCityConfigCompositionSystemHelper.Snapshot config = context.Config;
        placementSystem.PlaceFromPlots(
            context,
            config.HousePrefabs,
            outerPlots,
            plan.RoadsideHouseTarget,
            1,
            roadCellSizeInGridCells,
            "House",
            "Old town house.",
            config.DefaultBuildingMaxHealth,
            ref rng,
            usedPlotCells,
            reservedFootprints,
            shopAndHouseFootprints,
            houseFootprints);
    }
}
