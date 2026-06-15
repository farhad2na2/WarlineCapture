using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed partial class RuntimeCityHallSpawnSystem : SystemBase
{
    private readonly RuntimeCityHallSpawnState _state = new();

    public RuntimeCityHallSpawnState State => _state;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void EnsureCityHall(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityLandmarkOffsetState offsetSystem,
        CityLayoutData city,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng)
    {
        _state.EnsureCityHall(context, placementSystem, offsetSystem, city, roadCellSizeInGridCells, ref rng);
    }
}

internal sealed class RuntimeCityHallSpawnState
{
    public void EnsureCityHall(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityLandmarkOffsetState offsetSystem,
        CityLayoutData city,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng)
    {
        if (city.HallPlaced)
            return;

        city.HallPlaced = TrySpawnHall(context, placementSystem, offsetSystem, city.CenterRoadCell, roadCellSizeInGridCells, ref rng, city.ReservedFootprints);
        if (!city.HallPlaced)
            context.DiagnosticSystem?.LogHallPlacementFailed(city.CenterRoadCell);
    }

    private static bool TrySpawnHall(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementState placementSystem,
        RuntimeCityLandmarkOffsetState offsetSystem,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        RuntimeCityConfigSystem.Snapshot config = context.Config;
        if (config.HallPrefabs == null || config.HallPrefabs.Count == 0)
            return false;

        var hallCandidates = new List<GameObject>(config.HallPrefabs);
        context.PrefabSelectionSystem.Shuffle(hallCandidates, ref rng);

        Vector2Int[] offsets = offsetSystem.HallOffsets;

        for (int prefabIndex = 0; prefabIndex < hallCandidates.Count; prefabIndex++)
        {
            GameObject hallPrefab = hallCandidates[prefabIndex];
            if (hallPrefab == null)
                continue;

            Vector2Int footprint = placementSystem.GetFootprint(context, hallPrefab);
            for (int offsetIndex = 0; offsetIndex < offsets.Length; offsetIndex++)
            {
                Vector2Int hallOrigin = context.BuildingPlotSystem.GetCenteredOriginForPlot(centerRoadCell + offsets[offsetIndex], footprint, roadCellSizeInGridCells);
                if (placementSystem.TrySpawnAndReserve(
                    context,
                    new RuntimeCityBuildingPlacementSystem.Request(
                        hallPrefab,
                        hallOrigin,
                        footprint,
                        hallPrefab.name,
                        "Old town civic center.",
                        config.DefaultBuildingMaxHealth,
                        reservedFootprints,
                        config.LandmarkClearanceCells),
                    out _))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
