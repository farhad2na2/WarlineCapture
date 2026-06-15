using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed partial class RuntimeCityArchwaySpawnSystem : SystemBase
{
    private readonly RuntimeCityArchwaySpawnState _state = new();

    public RuntimeCityArchwaySpawnState State => _state;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public int PlaceCentralArchwayBuildings(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementSystem placementSystem,
        List<GameObject> archwayPrefabs,
        int maxCount,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints)
    {
        return _state.PlaceCentralArchwayBuildings(
            context,
            placementSystem,
            archwayPrefabs,
            maxCount,
            centerRoadCell,
            roadCellSizeInGridCells,
            roadCells,
            ref rng,
            usedPlotCells,
            reservedFootprints);
    }
}

internal sealed class RuntimeCityArchwaySpawnState
{
    public int PlaceCentralArchwayBuildings(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementSystem placementSystem,
        List<GameObject> archwayPrefabs,
        int maxCount,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints)
    {
        if (archwayPrefabs == null || archwayPrefabs.Count == 0 || maxCount <= 0)
            return 0;

        int placed = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(120, maxCount * 24);
        int minDistance = Mathf.Max(1, context.Config.HallPlazaRadiusRoadCells + 1);
        int maxDistance = context.Config.HallPlazaRadiusRoadCells + 5;

        while (placed < maxCount && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int plotCell = context.BuildingPlotSystem.GetRandomScatterPlotCell(centerRoadCell, maxDistance, ref rng);
            int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
            if (distance < minDistance || distance > maxDistance)
                continue;
            if (roadCells.Contains(plotCell))
                continue;
            if (!context.BuildingPlotSystem.HasPlotSpacing(plotCell, usedPlotCells, 1))
                continue;

            GameObject prefab = archwayPrefabs[placed % archwayPrefabs.Count];
            if (prefab == null)
                continue;

            Vector2Int footprint = placementSystem.GetFootprint(context, prefab);
            Vector2Int preferredOrigin = context.BuildingPlotSystem.GetCenteredOriginForPlot(plotCell, footprint, roadCellSizeInGridCells);
            if (!placementSystem.TrySpawnAndReserve(
                    context,
                    new RuntimeCityBuildingPlacementSystem.Request(
                    prefab,
                    preferredOrigin,
                    footprint,
                    "Archway",
                    "Decorative archway near the town center.",
                    context.Config.DefaultBuildingMaxHealth,
                    reservedFootprints,
                    0),
                    out _))
            {
                continue;
            }

            usedPlotCells.Add(plotCell);
            placed++;
        }

        return placed;
    }
}
