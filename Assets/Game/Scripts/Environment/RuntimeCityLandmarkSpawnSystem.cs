using System.Collections.Generic;
using UnityEngine;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed class RuntimeCityLandmarkSpawnSystem
{
    private readonly RuntimeCityLandmarkSpawnState _state = new();

    public RuntimeCityLandmarkSpawnState State => _state;

    public void SpawnLandmarks(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityLandmarkOffsetState offsetSystem,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        _state.SpawnLandmarks(
            context,
            placementSystem,
            offsetSystem,
            centerRoadCell,
            roadCellSizeInGridCells,
            roadCells,
            ref rng,
            reservedFootprints);
    }
}

internal sealed class RuntimeCityLandmarkSpawnState
{
    public void SpawnLandmarks(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityLandmarkOffsetState offsetSystem,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        TrySpawnClockTower(context, placementSystem, offsetSystem, centerRoadCell, roadCellSizeInGridCells, roadCells, reservedFootprints);
        TrySpawnFountain(context, placementSystem, offsetSystem, centerRoadCell, roadCellSizeInGridCells, roadCells, ref rng, reservedFootprints);
        TrySpawnMonument(context, placementSystem, offsetSystem, centerRoadCell, roadCellSizeInGridCells, roadCells, ref rng, reservedFootprints);
        TrySpawnPillar(context, placementSystem, offsetSystem, centerRoadCell, roadCellSizeInGridCells, roadCells, ref rng, reservedFootprints);
    }

    private static void TrySpawnClockTower(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityLandmarkOffsetState offsetSystem,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        List<ReservedFootprint> reservedFootprints)
    {
        GameObject clockTowerPrefab = context.Config.ClockTowerPrefab;
        if (clockTowerPrefab == null)
            return;

        TrySpawnLandmark(
            context,
            placementSystem,
            offsetSystem,
            clockTowerPrefab,
            offsetSystem.ClockTowerOffsets,
            centerRoadCell,
            roadCellSizeInGridCells,
            roadCells,
            reservedFootprints,
            "Clock Tower",
            "Clock tower at the heart of the old town.");
    }

    private static void TrySpawnFountain(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityLandmarkOffsetState offsetSystem,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        GameObject fountainPrefab = context.PrefabSelectionSystem.GetRandomPrefab(context.Config.FountainPrefabs, ref rng);
        if (fountainPrefab == null)
            return;

        TrySpawnLandmark(
            context,
            placementSystem,
            offsetSystem,
            fountainPrefab,
            offsetSystem.FountainOffsets,
            centerRoadCell,
            roadCellSizeInGridCells,
            roadCells,
            reservedFootprints,
            "Fountain",
            "Town fountain near the center square.");
    }

    private static void TrySpawnMonument(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityLandmarkOffsetState offsetSystem,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        GameObject monumentPrefab = context.PrefabSelectionSystem.GetRandomPrefab(context.Config.MonumentPrefabs, ref rng);
        if (monumentPrefab == null)
            return;

        TrySpawnLandmark(
            context,
            placementSystem,
            offsetSystem,
            monumentPrefab,
            offsetSystem.MonumentOffsets,
            centerRoadCell,
            roadCellSizeInGridCells,
            roadCells,
            reservedFootprints,
            "Monument",
            "Town monument near the center square.");
    }

    private static void TrySpawnPillar(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityLandmarkOffsetState offsetSystem,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        GameObject pillarPrefab = context.PrefabSelectionSystem.GetRandomPrefab(context.Config.PillarPrefabs, ref rng);
        if (pillarPrefab == null)
            return;

        TrySpawnLandmark(
            context,
            placementSystem,
            offsetSystem,
            pillarPrefab,
            offsetSystem.PillarOffsets,
            centerRoadCell,
            roadCellSizeInGridCells,
            roadCells,
            reservedFootprints,
            "Pillar",
            "Stone pillar near the center district.");
    }

    private static void TrySpawnLandmark(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityLandmarkOffsetState offsetSystem,
        GameObject prefab,
        Vector2Int[] offsets,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        List<ReservedFootprint> reservedFootprints,
        string fallbackDisplayName,
        string fallbackDescription)
    {
        Vector2Int footprint = placementSystem.GetFootprint(context, prefab);

        for (int i = 0; i < offsets.Length; i++)
        {
            if (offsetSystem.IsTooCloseToHall(context.Config, offsets[i]))
                continue;

            Vector2Int preferredOrigin = context.BuildingPlotSystem.GetCenteredOriginForPlot(centerRoadCell + offsets[i], footprint, roadCellSizeInGridCells);
            if (placementSystem.TrySpawnAndReserve(
                context,
                new RuntimeCityBuildingPlacementPrefabSystemHelper.Request(
                    prefab,
                    preferredOrigin,
                    footprint,
                    fallbackDisplayName,
                    fallbackDescription,
                    context.Config.DefaultBuildingMaxHealth,
                    reservedFootprints,
                    context.Config.LandmarkClearanceCells,
                    roadCellSizeInGridCells,
                    roadCells),
                out _))
            {
                return;
            }
        }
    }
}
