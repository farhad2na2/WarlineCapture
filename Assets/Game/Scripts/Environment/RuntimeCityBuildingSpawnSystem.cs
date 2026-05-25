using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;
using PlotCandidate = RuntimeCityBuildingPlotSystem.PlotCandidate;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed class RuntimeCityBuildingSpawnSystem
{
    private enum YardSide
    {
        North,
        East,
        South,
        West
    }

    private RuntimeCityConfigSystem.Snapshot _config;
    private RuntimeCityBuildingPlotSystem _runtimeCityBuildingPlotSystem;
    private RuntimeCityWalkabilitySystem _runtimeCityWalkabilitySystem;
    private RuntimeCityPrefabSelectionSystem _runtimeCityPrefabSelectionSystem;
    private RuntimeCityVisualSystem _runtimeCityVisualSystem;
    private RuntimeCitySpawnBridgeSystem _runtimeCitySpawnBridgeSystem;

    private int gasStationCount => _config.GasStationCount;
    private int shopCount => _config.ShopCount;
    private int houseCount => _config.HouseCount;
    private int otherBuildingCount => _config.OtherBuildingCount;
    private int cityDecorationBuildingCount => _config.CityDecorationBuildingCount;
    private int hallPlazaRadiusRoadCells => _config.HallPlazaRadiusRoadCells;
    private float ruralHouseRatio => _config.RuralHouseRatio;
    private int gasStationMinSpacingRoadCells => _config.GasStationMinSpacingRoadCells;
    private float houseWallChance => _config.HouseWallChance;
    private int houseWallMinDistanceCells => _config.HouseWallMinDistanceCells;
    private int houseWallMaxDistanceCells => _config.HouseWallMaxDistanceCells;
    private int landmarkMinDistanceFromHallRoadCells => _config.LandmarkMinDistanceFromHallRoadCells;
    private int landmarkClearanceCells => _config.LandmarkClearanceCells;
    private int defaultBuildingMaxHealth => _config.DefaultBuildingMaxHealth;
    private GameObject clockTowerPrefab => _config.ClockTowerPrefab;
    private List<GameObject> fountainPrefabs => _config.FountainPrefabs;
    private List<GameObject> monumentPrefabs => _config.MonumentPrefabs;
    private List<GameObject> pillarPrefabs => _config.PillarPrefabs;
    private List<GameObject> hallPrefabs => _config.HallPrefabs;
    private List<GameObject> gasStationPrefabs => _config.GasStationPrefabs;
    private List<GameObject> shopPrefabs => _config.ShopPrefabs;
    private List<GameObject> housePrefabs => _config.HousePrefabs;
    private List<GameObject> otherBuildingPrefabs => _config.OtherBuildingPrefabs;
    private List<GameObject> cityDecorationPrefabs => _config.CityDecorationPrefabs;
    private List<GameObject> houseWallPrefabs => _config.HouseWallPrefabs;
    private GameObject houseWallGatePrefab => _config.HouseWallGatePrefab;
    private GameObject houseWallPillarPrefab => _config.HouseWallPillarPrefab;

    public void Configure(
        RuntimeCityConfigSystem.Snapshot config,
        RuntimeCityBuildingPlotSystem buildingPlotSystem,
        RuntimeCityWalkabilitySystem walkabilitySystem,
        RuntimeCityPrefabSelectionSystem prefabSelectionSystem,
        RuntimeCityVisualSystem visualSystem,
        RuntimeCitySpawnBridgeSystem spawnBridgeSystem)
    {
        _config = config;
        _runtimeCityBuildingPlotSystem = buildingPlotSystem;
        _runtimeCityWalkabilitySystem = walkabilitySystem;
        _runtimeCityPrefabSelectionSystem = prefabSelectionSystem;
        _runtimeCityVisualSystem = visualSystem;
        _runtimeCitySpawnBridgeSystem = spawnBridgeSystem;
    }

    public void SpawnCityImportantBuildings(CityLayoutData city, int roadCellSizeInGridCells, ref Unity.Mathematics.Random rng)
    {
        EnsureCityHall(city, roadCellSizeInGridCells, ref rng);
        TrySpawnClockTower(city.CenterRoadCell, roadCellSizeInGridCells, city.RoadCells, city.ReservedFootprints);
        TrySpawnFountain(city.CenterRoadCell, roadCellSizeInGridCells, city.RoadCells, ref rng, city.ReservedFootprints);
        TrySpawnMonument(city.CenterRoadCell, roadCellSizeInGridCells, city.RoadCells, ref rng, city.ReservedFootprints);
        TrySpawnPillar(city.CenterRoadCell, roadCellSizeInGridCells, city.RoadCells, ref rng, city.ReservedFootprints);
    }

    public void EnsureCityHall(CityLayoutData city, int roadCellSizeInGridCells, ref Unity.Mathematics.Random rng)
    {
        if (city.HallPlaced)
            return;

        city.HallPlaced = TrySpawnHall(city.CenterRoadCell, roadCellSizeInGridCells, ref rng, city.ReservedFootprints);
        if (!city.HallPlaced)
            Debug.LogWarning($"[RuntimeCitySpawnerSystem] Hall could not be placed for city at {city.CenterRoadCell}.");
    }

    public sealed class GenerationRandomState
    {
        public Unity.Mathematics.Random Value;
    }

    public IEnumerator SpawnCityBulkBuildingsRoutine(CityLayoutData city, GridConfig grid, int roadCellSizeInGridCells, GenerationRandomState rng)
    {
        Vector2Int centerRoadCell = city.CenterRoadCell;
        int townRadius = city.TownRadius;
        HashSet<Vector2Int> roadCells = city.RoadCells;
        List<ReservedFootprint> reservedFootprints = city.ReservedFootprints;

        List<PlotCandidate> centralPlots = _runtimeCityBuildingPlotSystem.CollectRoadsidePlots(roadCells, centerRoadCell, townRadius, hallPlazaRadiusRoadCells + 1, hallPlazaRadiusRoadCells + 3);
        List<PlotCandidate> outerPlots = _runtimeCityBuildingPlotSystem.CollectRoadsidePlots(roadCells, centerRoadCell, townRadius, hallPlazaRadiusRoadCells + 4, townRadius + 1);
        List<PlotCandidate> entryPlots = city.HasIncomingAnchor
            ? _runtimeCityBuildingPlotSystem.CollectEntryRoadsidePlots(city, townRadius)
            : new List<PlotCandidate>();
        _runtimeCityPrefabSelectionSystem.Shuffle(centralPlots, ref rng.Value);
        _runtimeCityPrefabSelectionSystem.Shuffle(outerPlots, ref rng.Value);
        _runtimeCityPrefabSelectionSystem.Shuffle(entryPlots, ref rng.Value);

        _runtimeCityVisualSystem.EnsureCityVisualRoot();

        var usedPlotCells = new List<Vector2Int>();
        var shopAndHouseFootprints = new List<RectInt>();
        var houseFootprints = new List<RectInt>();
        PlaceFromPlots(shopPrefabs, entryPlots, Mathf.Min(2, Mathf.Max(0, shopCount)), 0, roadCellSizeInGridCells, "Entry Shop", "Roadside shop near the city entrance.", ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;
        PlaceFromPlots(housePrefabs, entryPlots, Mathf.Min(4, Mathf.Max(0, houseCount)), 0, roadCellSizeInGridCells, "Entry House", "House near the city entrance road.", ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        yield return null;

        int centralShopTarget = Mathf.Min(shopCount, Mathf.Max(0, Mathf.RoundToInt(shopCount * 0.65f)));
        PlaceFromPlots(shopPrefabs, centralPlots, centralShopTarget, 1, roadCellSizeInGridCells, "Market", "Old town market.", ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;
        PlaceFromPlots(gasStationPrefabs, outerPlots, gasStationCount, gasStationMinSpacingRoadCells, roadCellSizeInGridCells, "Gas Station", "Roadside fuel stop.", ref rng.Value, usedPlotCells, reservedFootprints);
        yield return null;
        PlaceFromPlots(shopPrefabs, outerPlots, Mathf.Max(0, shopCount - centralShopTarget), 1, roadCellSizeInGridCells, "Shop", "Old town shop.", ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;

        int ruralHouseTarget = Mathf.RoundToInt(Mathf.Max(0, houseCount) * Mathf.Clamp01(ruralHouseRatio));
        int roadsideHouseTarget = Mathf.Max(0, houseCount - ruralHouseTarget);
        PlaceFromPlots(housePrefabs, outerPlots, roadsideHouseTarget, 1, roadCellSizeInGridCells, "House", "Old town house.", ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        yield return null;
        PlaceRuralHouses(housePrefabs, ruralHouseTarget, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints, houseFootprints);
        yield return null;
        PlaceHouseYardWalls(houseFootprints, centerRoadCell, roadCellSizeInGridCells, roadCells, grid, ref rng.Value, reservedFootprints);
        yield return null;

        int ruralOtherTarget = Mathf.RoundToInt(Mathf.Max(0, otherBuildingCount) * Mathf.Clamp01(ruralHouseRatio));
        int roadsideOtherTarget = Mathf.Max(0, otherBuildingCount - ruralOtherTarget);
        PlaceFromPlots(otherBuildingPrefabs, outerPlots, roadsideOtherTarget, 1, roadCellSizeInGridCells, "Village Building", "Old town side building.", ref rng.Value, usedPlotCells, reservedFootprints);
        yield return null;
        PlaceRuralHouses(otherBuildingPrefabs, ruralOtherTarget, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng.Value, usedPlotCells, reservedFootprints);
        yield return null;
        PlaceCityDecorationBuildings(cityDecorationPrefabs, cityDecorationBuildingCount, centerRoadCell, townRadius, roadCellSizeInGridCells, roadCells, ref rng.Value, usedPlotCells, reservedFootprints, shopAndHouseFootprints);
        yield return null;
    }

    public void SpawnCorridorEntranceBuildings(
        CityLayoutData city,
        Vector2Int connectorCell,
        Vector2Int direction,
        int corridorLength,
        int roadCellSizeInGridCells,
        ref Unity.Mathematics.Random rng)
    {
        if (corridorLength <= 0)
            return;

        List<PlotCandidate> corridorPlots = _runtimeCityBuildingPlotSystem.BuildCorridorRoadsidePlots(connectorCell, direction, corridorLength);

        if (corridorPlots.Count == 0)
            return;

        _runtimeCityPrefabSelectionSystem.Shuffle(corridorPlots, ref rng);

        var usedPlotCells = new List<Vector2Int>();
        var placementAnchors = new List<RectInt>();
        PlaceFromPlots(shopPrefabs, corridorPlots, Mathf.Min(2, Mathf.Max(0, shopCount)), 0, roadCellSizeInGridCells, "Corridor Shop", "Shop near the city entrance road.", ref rng, usedPlotCells, city.ReservedFootprints, placementAnchors);
        PlaceFromPlots(housePrefabs, corridorPlots, Mathf.Min(6, Mathf.Max(0, houseCount)), 0, roadCellSizeInGridCells, "Corridor House", "House near the city entrance road.", ref rng, usedPlotCells, city.ReservedFootprints, placementAnchors);
    }

    private bool TrySpawnHall(Vector2Int centerRoadCell, int roadCellSizeInGridCells, ref Unity.Mathematics.Random rng, List<ReservedFootprint> reservedFootprints)
    {
        if (hallPrefabs == null || hallPrefabs.Count == 0)
            return false;

        var hallCandidates = new List<GameObject>(hallPrefabs);
        _runtimeCityPrefabSelectionSystem.Shuffle(hallCandidates, ref rng);

        Vector2Int[] offsets =
        {
            Vector2Int.zero,
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(2, 1),
            new Vector2Int(-2, 1),
            new Vector2Int(2, -1),
            new Vector2Int(-2, -1),
            new Vector2Int(1, 2),
            new Vector2Int(-1, 2),
            new Vector2Int(1, -2),
            new Vector2Int(-1, -2)
        };

        for (int prefabIndex = 0; prefabIndex < hallCandidates.Count; prefabIndex++)
        {
            GameObject hallPrefab = hallCandidates[prefabIndex];
            if (hallPrefab == null)
                continue;

            Vector2Int footprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(hallPrefab);
            for (int offsetIndex = 0; offsetIndex < offsets.Length; offsetIndex++)
            {
                Vector2Int hallOrigin = _runtimeCityBuildingPlotSystem.GetCenteredOriginForPlot(centerRoadCell + offsets[offsetIndex], footprint, roadCellSizeInGridCells);
                if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(hallOrigin, footprint, reservedFootprints, landmarkClearanceCells))
                    continue;

                if (!_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding(
                        hallPrefab,
                        hallOrigin,
                        out int buildingId,
                        out Vector2Int actualHallOrigin,
                        out Vector2Int actualHallFootprint,
                        hallPrefab.name,
                        "Old town civic center.",
                        footprint,
                        defaultBuildingMaxHealth))
                {
                    continue;
                }

                if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(actualHallOrigin, actualHallFootprint, reservedFootprints, landmarkClearanceCells))
                {
                    _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                    continue;
                }

                _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, actualHallOrigin, actualHallFootprint, landmarkClearanceCells);
                return true;
            }
        }

        return false;
    }

    private void TrySpawnClockTower(Vector2Int centerRoadCell, int roadCellSizeInGridCells, HashSet<Vector2Int> roadCells, List<ReservedFootprint> reservedFootprints)
    {
        if (clockTowerPrefab == null)
            return;

            Vector2Int footprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(clockTowerPrefab);
        Vector2Int[] offsets =
        {
            new(3, 0),
            new(-3, 0),
            new(0, 3),
            new(0, -3),
            new(3, 2),
            new(-3, 2),
            new(3, -2),
            new(-3, -2),
            new(4, 1),
            new(-4, 1),
            new(1, 4),
            new(-1, 4)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (IsTooCloseToHall(centerRoadCell, offsets[i]))
                continue;

            Vector2Int preferredOrigin = _runtimeCityBuildingPlotSystem.GetCenteredOriginForPlot(centerRoadCell + offsets[i], footprint, roadCellSizeInGridCells);
            if (_runtimeCityWalkabilitySystem.DoesRectOverlapRoadCells(new RectInt(preferredOrigin, footprint), roadCellSizeInGridCells, roadCells))
                continue;
            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, landmarkClearanceCells))
                continue;

            if (_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding(
                    clockTowerPrefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "Clock Tower",
                    "Clock tower at the heart of the old town.",
                    footprint,
                    defaultBuildingMaxHealth))
            {
                if (_runtimeCityWalkabilitySystem.DoesRectOverlapRoadCells(new RectInt(actualOrigin, actualFootprint), roadCellSizeInGridCells, roadCells))
                {
                    _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                    continue;
                }

                if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, landmarkClearanceCells))
                {
                    _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                    continue;
                }

                _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, landmarkClearanceCells);
                return;
            }
        }
    }

    private void TrySpawnFountain(Vector2Int centerRoadCell, int roadCellSizeInGridCells, HashSet<Vector2Int> roadCells, ref Unity.Mathematics.Random rng, List<ReservedFootprint> reservedFootprints)
    {
        GameObject fountainPrefab = _runtimeCityPrefabSelectionSystem.GetRandomPrefab(fountainPrefabs, ref rng);
        if (fountainPrefab == null)
            return;

            Vector2Int footprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(fountainPrefab);
        Vector2Int[] offsets =
        {
            new(2, 3),
            new(-2, 3),
            new(3, 2),
            new(-3, 2),
            new(2, -3),
            new(-2, -3),
            new(3, -2),
            new(-3, -2),
            new(4, 0),
            new(-4, 0),
            new(0, 4),
            new(0, -4)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (IsTooCloseToHall(centerRoadCell, offsets[i]))
                continue;

            Vector2Int preferredOrigin = _runtimeCityBuildingPlotSystem.GetCenteredOriginForPlot(centerRoadCell + offsets[i], footprint, roadCellSizeInGridCells);
            if (_runtimeCityWalkabilitySystem.DoesRectOverlapRoadCells(new RectInt(preferredOrigin, footprint), roadCellSizeInGridCells, roadCells))
                continue;
            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, landmarkClearanceCells))
                continue;

            if (_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding(
                    fountainPrefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "Fountain",
                    "Town fountain near the center square.",
                    footprint,
                    defaultBuildingMaxHealth))
            {
                if (_runtimeCityWalkabilitySystem.DoesRectOverlapRoadCells(new RectInt(actualOrigin, actualFootprint), roadCellSizeInGridCells, roadCells))
                {
                    _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                    continue;
                }

                if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, landmarkClearanceCells))
                {
                    _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                    continue;
                }

                _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, landmarkClearanceCells);
                return;
            }
        }
    }

    private void TrySpawnMonument(Vector2Int centerRoadCell, int roadCellSizeInGridCells, HashSet<Vector2Int> roadCells, ref Unity.Mathematics.Random rng, List<ReservedFootprint> reservedFootprints)
    {
        GameObject monumentPrefab = _runtimeCityPrefabSelectionSystem.GetRandomPrefab(monumentPrefabs, ref rng);
        if (monumentPrefab == null)
            return;

            Vector2Int footprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(monumentPrefab);
        Vector2Int[] offsets =
        {
            new(3, 4),
            new(-3, 4),
            new(4, 3),
            new(-4, 3),
            new(3, -4),
            new(-3, -4),
            new(4, -3),
            new(-4, -3),
            new(5, 0),
            new(-5, 0),
            new(0, 5),
            new(0, -5)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (IsTooCloseToHall(centerRoadCell, offsets[i]))
                continue;

            Vector2Int preferredOrigin = _runtimeCityBuildingPlotSystem.GetCenteredOriginForPlot(centerRoadCell + offsets[i], footprint, roadCellSizeInGridCells);
            if (_runtimeCityWalkabilitySystem.DoesRectOverlapRoadCells(new RectInt(preferredOrigin, footprint), roadCellSizeInGridCells, roadCells))
                continue;
            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, landmarkClearanceCells))
                continue;

            if (_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding(
                    monumentPrefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "Monument",
                    "Town monument near the center square.",
                    footprint,
                    defaultBuildingMaxHealth))
            {
                if (_runtimeCityWalkabilitySystem.DoesRectOverlapRoadCells(new RectInt(actualOrigin, actualFootprint), roadCellSizeInGridCells, roadCells))
                {
                    _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                    continue;
                }

                if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, landmarkClearanceCells))
                {
                    _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                    continue;
                }

                _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, landmarkClearanceCells);
                return;
            }
        }
    }

    private void TrySpawnPillar(Vector2Int centerRoadCell, int roadCellSizeInGridCells, HashSet<Vector2Int> roadCells, ref Unity.Mathematics.Random rng, List<ReservedFootprint> reservedFootprints)
    {
        GameObject pillarPrefab = _runtimeCityPrefabSelectionSystem.GetRandomPrefab(pillarPrefabs, ref rng);
        if (pillarPrefab == null)
            return;

            Vector2Int footprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(pillarPrefab);
        Vector2Int[] offsets =
        {
            new(5, 2),
            new(-5, 2),
            new(2, 5),
            new(-2, 5),
            new(5, -2),
            new(-5, -2),
            new(2, -5),
            new(-2, -5),
            new(6, 0),
            new(-6, 0),
            new(0, 6),
            new(0, -6)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (IsTooCloseToHall(centerRoadCell, offsets[i]))
                continue;

            Vector2Int preferredOrigin = _runtimeCityBuildingPlotSystem.GetCenteredOriginForPlot(centerRoadCell + offsets[i], footprint, roadCellSizeInGridCells);
            if (_runtimeCityWalkabilitySystem.DoesRectOverlapRoadCells(new RectInt(preferredOrigin, footprint), roadCellSizeInGridCells, roadCells))
                continue;
            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, landmarkClearanceCells))
                continue;

            if (_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding(
                    pillarPrefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "Pillar",
                    "Stone pillar near the center district.",
                    footprint,
                    defaultBuildingMaxHealth))
            {
                if (_runtimeCityWalkabilitySystem.DoesRectOverlapRoadCells(new RectInt(actualOrigin, actualFootprint), roadCellSizeInGridCells, roadCells))
                {
                    _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                    continue;
                }

                if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, landmarkClearanceCells))
                {
                    _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                    continue;
                }

                _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, landmarkClearanceCells);
                return;
            }
        }
    }

    private bool IsTooCloseToHall(Vector2Int centerRoadCell, Vector2Int offset)
    {
        int distance = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
        return distance < Mathf.Max(1, landmarkMinDistanceFromHallRoadCells);
    }

    private void PlaceFromPlots(
        List<GameObject> prefabs,
        List<PlotCandidate> candidates,
        int count,
        int minPlotSpacing,
        int roadCellSizeInGridCells,
        string fallbackDisplayName,
        string fallbackDescription,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> placementAnchors = null,
        List<RectInt> secondaryPlacementAnchors = null)
    {
        if (prefabs == null || prefabs.Count == 0 || count <= 0 || candidates == null || candidates.Count == 0)
            return;

        int placed = 0;
        for (int i = 0; i < candidates.Count && placed < count; i++)
        {
            PlotCandidate candidate = candidates[i];
            if (!_runtimeCityBuildingPlotSystem.HasPlotSpacing(candidate.PlotCell, usedPlotCells, minPlotSpacing))
                continue;

            GameObject prefab = _runtimeCityPrefabSelectionSystem.GetRandomPrefab(prefabs, ref rng);
            if (prefab == null)
                continue;

            Vector2Int footprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(prefab);
            Vector2Int preferredOrigin = _runtimeCityBuildingPlotSystem.GetCenteredOriginForPlot(candidate.PlotCell, footprint, roadCellSizeInGridCells);
            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, 0))
                continue;

            if (!_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding(
                    prefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    fallbackDisplayName,
                    fallbackDescription,
                    footprint,
                    defaultBuildingMaxHealth))
                continue;

            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, 0))
            {
                _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                continue;
            }

            usedPlotCells.Add(candidate.PlotCell);
            _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, 0);
            placementAnchors?.Add(new RectInt(actualOrigin, actualFootprint));
            secondaryPlacementAnchors?.Add(new RectInt(actualOrigin, actualFootprint));
            placed++;
        }
    }

    private void PlaceRuralHouses(
        List<GameObject> prefabs,
        int count,
        Vector2Int centerRoadCell,
        int townRadius,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        ref Unity.Mathematics.Random rng,
        List<Vector2Int> usedPlotCells,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> placementAnchors = null,
        List<RectInt> secondaryPlacementAnchors = null)
    {
        if (prefabs == null || prefabs.Count == 0 || count <= 0)
            return;

        int attempts = 0;
        int placed = 0;
        int maxAttempts = Mathf.Max(120, count * 20);
        while (placed < count && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int plotCell = new(
                centerRoadCell.x + rng.NextInt(-(townRadius + 3), townRadius + 4),
                centerRoadCell.y + rng.NextInt(-(townRadius + 3), townRadius + 4));

            int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
            if (distance < hallPlazaRadiusRoadCells + 5 || distance > townRadius + 3)
                continue;
            if (roadCells.Contains(plotCell))
                continue;
            if (!_runtimeCityBuildingPlotSystem.HasPlotSpacing(plotCell, usedPlotCells, 1))
                continue;

            GameObject prefab = _runtimeCityPrefabSelectionSystem.GetRandomPrefab(prefabs, ref rng);
            if (prefab == null)
                continue;

            Vector2Int footprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(prefab);
            Vector2Int preferredOrigin = _runtimeCityBuildingPlotSystem.GetCenteredOriginForPlot(plotCell, footprint, roadCellSizeInGridCells);
            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, 0))
                continue;

            if (!_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding(
                    prefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "House",
                    "Rural old town house.",
                    footprint,
                    defaultBuildingMaxHealth))
            {
                continue;
            }

            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, 0))
            {
                _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                continue;
            }

            usedPlotCells.Add(plotCell);
            _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, 0);
            placementAnchors?.Add(new RectInt(actualOrigin, actualFootprint));
            secondaryPlacementAnchors?.Add(new RectInt(actualOrigin, actualFootprint));
            placed++;
        }
    }

    private void PlaceHouseYardWalls(
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

        var shuffledHouses = new List<RectInt>(houseFootprints);
        _runtimeCityPrefabSelectionSystem.Shuffle(shuffledHouses, ref rng);

        int targetCount = Mathf.RoundToInt(shuffledHouses.Count * Mathf.Clamp01(houseWallChance));
        int builtCount = 0;
        for (int i = 0; i < shuffledHouses.Count && builtCount < targetCount; i++)
        {
            if (TryBuildHouseYardWall(shuffledHouses[i], centerRoadCell, roadCellSizeInGridCells, roadCells, grid, ref rng, reservedFootprints))
                builtCount++;
        }
    }

    private bool TryBuildHouseYardWall(
        RectInt houseRect,
        Vector2Int centerRoadCell,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        GridConfig grid,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        int minPadding = Mathf.Max(1, Mathf.Min(houseWallMinDistanceCells, houseWallMaxDistanceCells));
        int maxPadding = Mathf.Max(minPadding, houseWallMaxDistanceCells);
        var candidatePaddings = new List<int>();
        for (int padding = minPadding; padding <= maxPadding; padding++)
            candidatePaddings.Add(padding);
        _runtimeCityPrefabSelectionSystem.Shuffle(candidatePaddings, ref rng);

        for (int i = 0; i < candidatePaddings.Count; i++)
        {
            RectInt yardRect = _runtimeCityWalkabilitySystem.ExpandRect(houseRect, candidatePaddings[i]);
            if (!_runtimeCityWalkabilitySystem.CanPlaceHouseYardRect(yardRect, houseRect, roadCellSizeInGridCells, roadCells, reservedFootprints, grid))
                continue;

            Vector2Int cityCenterGridCell = new(
                centerRoadCell.x * roadCellSizeInGridCells + Mathf.FloorToInt(roadCellSizeInGridCells * 0.5f),
                centerRoadCell.y * roadCellSizeInGridCells + Mathf.FloorToInt(roadCellSizeInGridCells * 0.5f));
            YardSide gateSide = GetPreferredYardGateSide(houseRect, cityCenterGridCell);
            GameObject wallPrefab = _runtimeCityPrefabSelectionSystem.GetRandomPrefab(houseWallPrefabs, ref rng);
            if (wallPrefab == null)
                return false;

            BuildYardBoundaryVisuals(yardRect, gateSide, wallPrefab, houseWallGatePrefab, houseWallPillarPrefab, grid);
            _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, yardRect.position, yardRect.size, 0);
            return true;
        }

        return false;
    }

    private void BuildYardBoundaryVisuals(
        RectInt yardRect,
        YardSide gateSide,
        GameObject wallPrefab,
        GameObject gatePrefab,
        GameObject pillarPrefab,
        GridConfig grid)
    {
        int horizontalThickness = _runtimeCityPrefabSelectionSystem.GetMinorFootprint(wallPrefab);
        int verticalThickness = _runtimeCityPrefabSelectionSystem.GetMinorFootprint(wallPrefab);
        int horizontalGateLength = Mathf.Max(1, _runtimeCityPrefabSelectionSystem.GetMajorFootprint(gatePrefab));
        int verticalGateLength = Mathf.Max(1, _runtimeCityPrefabSelectionSystem.GetMajorFootprint(gatePrefab));

        int northGateStart = gateSide == YardSide.North ? GetCenteredOpeningStart(yardRect.width, horizontalGateLength) : -1;
        int southGateStart = gateSide == YardSide.South ? GetCenteredOpeningStart(yardRect.width, horizontalGateLength) : -1;
        int eastGateStart = gateSide == YardSide.East ? GetCenteredOpeningStart(yardRect.height, verticalGateLength) : -1;
        int westGateStart = gateSide == YardSide.West ? GetCenteredOpeningStart(yardRect.height, verticalGateLength) : -1;

        PlaceHorizontalWallSide(yardRect, yardRect.yMin, yardRect.width, wallPrefab, gatePrefab, grid, southGateStart, horizontalGateLength, false, horizontalThickness);
        PlaceHorizontalWallSide(yardRect, yardRect.yMax - horizontalThickness, yardRect.width, wallPrefab, gatePrefab, grid, northGateStart, horizontalGateLength, false, horizontalThickness);
        PlaceVerticalWallSide(yardRect, yardRect.xMin, yardRect.height, wallPrefab, gatePrefab, grid, westGateStart, verticalGateLength, verticalThickness);
        PlaceVerticalWallSide(yardRect, yardRect.xMax - verticalThickness, yardRect.height, wallPrefab, gatePrefab, grid, eastGateStart, verticalGateLength, verticalThickness);

        if (pillarPrefab != null)
        {
            Vector2Int pillarFootprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(pillarPrefab);
            _runtimeCityVisualSystem.SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMin, yardRect.yMin), pillarFootprint, Quaternion.identity, grid);
            _runtimeCityVisualSystem.SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMax - pillarFootprint.x, yardRect.yMin), pillarFootprint, Quaternion.identity, grid);
            _runtimeCityVisualSystem.SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMin, yardRect.yMax - pillarFootprint.y), pillarFootprint, Quaternion.identity, grid);
            _runtimeCityVisualSystem.SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMax - pillarFootprint.x, yardRect.yMax - pillarFootprint.y), pillarFootprint, Quaternion.identity, grid);
        }
    }

    private void PlaceHorizontalWallSide(
        RectInt yardRect,
        int yOrigin,
        int totalLength,
        GameObject wallPrefab,
        GameObject gatePrefab,
        GridConfig grid,
        int gateStartOffset,
        int gateLength,
        bool rotateGate,
        int thickness)
    {
        PlaceHorizontalWallRun(yardRect.xMin, yOrigin, totalLength, wallPrefab, grid, thickness, gateStartOffset, gateLength);
        if (gateStartOffset >= 0)
        {
            Vector2Int gateFootprint = new(Mathf.Max(1, gateLength), Mathf.Max(1, _runtimeCityPrefabSelectionSystem.GetMinorFootprint(gatePrefab)));
            _runtimeCityVisualSystem.SpawnVisualOnlyPrefab(gatePrefab, new Vector2Int(yardRect.xMin + gateStartOffset, yOrigin), gateFootprint, rotateGate ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity, grid);
        }
    }

    private void PlaceHorizontalWallRun(
        int xOrigin,
        int yOrigin,
        int totalLength,
        GameObject wallPrefab,
        GridConfig grid,
        int thickness,
        int gateStartOffset,
        int gateLength)
    {
        int segmentLength = Mathf.Max(1, _runtimeCityPrefabSelectionSystem.GetMajorFootprint(wallPrefab));
        int current = 0;
        while (current < totalLength)
        {
            if (gateStartOffset >= 0 && current >= gateStartOffset && current < gateStartOffset + gateLength)
            {
                current = gateStartOffset + gateLength;
                continue;
            }

            int nextStop = totalLength;
            if (gateStartOffset >= 0 && current < gateStartOffset)
                nextStop = gateStartOffset;
            int pieceLength = Mathf.Min(segmentLength, nextStop - current);
            if (pieceLength <= 0)
                break;

            _runtimeCityVisualSystem.SpawnVisualOnlyPrefab(
                wallPrefab,
                new Vector2Int(xOrigin + current, yOrigin),
                new Vector2Int(pieceLength, Mathf.Max(1, thickness)),
                Quaternion.identity,
                grid);
            current += pieceLength;
        }
    }

    private void PlaceVerticalWallSide(
        RectInt yardRect,
        int xOrigin,
        int totalLength,
        GameObject wallPrefab,
        GameObject gatePrefab,
        GridConfig grid,
        int gateStartOffset,
        int gateLength,
        int thickness)
    {
        PlaceVerticalWallRun(xOrigin, yardRect.yMin, totalLength, wallPrefab, grid, thickness, gateStartOffset, gateLength);
        if (gateStartOffset >= 0)
        {
            Vector2Int gateFootprint = new(Mathf.Max(1, _runtimeCityPrefabSelectionSystem.GetMinorFootprint(gatePrefab)), Mathf.Max(1, gateLength));
            _runtimeCityVisualSystem.SpawnVisualOnlyPrefab(gatePrefab, new Vector2Int(xOrigin, yardRect.yMin + gateStartOffset), gateFootprint, Quaternion.Euler(0f, 90f, 0f), grid);
        }
    }

    private void PlaceVerticalWallRun(
        int xOrigin,
        int yOrigin,
        int totalLength,
        GameObject wallPrefab,
        GridConfig grid,
        int thickness,
        int gateStartOffset,
        int gateLength)
    {
        int segmentLength = Mathf.Max(1, _runtimeCityPrefabSelectionSystem.GetMajorFootprint(wallPrefab));
        int current = 0;
        while (current < totalLength)
        {
            if (gateStartOffset >= 0 && current >= gateStartOffset && current < gateStartOffset + gateLength)
            {
                current = gateStartOffset + gateLength;
                continue;
            }

            int nextStop = totalLength;
            if (gateStartOffset >= 0 && current < gateStartOffset)
                nextStop = gateStartOffset;
            int pieceLength = Mathf.Min(segmentLength, nextStop - current);
            if (pieceLength <= 0)
                break;

            _runtimeCityVisualSystem.SpawnVisualOnlyPrefab(
                wallPrefab,
                new Vector2Int(xOrigin, yOrigin + current),
                new Vector2Int(Mathf.Max(1, thickness), pieceLength),
                Quaternion.Euler(0f, 90f, 0f),
                grid);
            current += pieceLength;
        }
    }

    private static int GetCenteredOpeningStart(int totalLength, int openingLength)
    {
        if (openingLength >= totalLength - 1)
            return Mathf.Max(0, (totalLength - Mathf.Max(1, totalLength / 2)) / 2);

        return Mathf.Clamp((totalLength - openingLength) / 2, 1, Mathf.Max(1, totalLength - openingLength - 1));
    }

    private static YardSide GetPreferredYardGateSide(RectInt houseRect, Vector2Int centerRoadCell)
    {
        Vector2 houseCenter = new(houseRect.center.x, houseRect.center.y);
        Vector2 cityCenter = new(centerRoadCell.x, centerRoadCell.y);
        Vector2 delta = cityCenter - houseCenter;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x >= 0f ? YardSide.East : YardSide.West;

        return delta.y >= 0f ? YardSide.North : YardSide.South;
    }

    private void PlaceCityDecorationBuildings(
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

        List<GameObject> clothCoverPrefabs = prefabs.FindAll(static prefab => prefab != null && prefab.name.Contains("ClothCover", StringComparison.OrdinalIgnoreCase));
        List<GameObject> archwayPrefabs = prefabs.FindAll(static prefab => prefab != null && prefab.name.Contains("Archway", StringComparison.OrdinalIgnoreCase));
        List<GameObject> freeScatterPrefabs = prefabs.FindAll(static prefab =>
            prefab != null &&
            !prefab.name.Contains("ClothCover", StringComparison.OrdinalIgnoreCase) &&
            !prefab.name.Contains("Archway", StringComparison.OrdinalIgnoreCase));
        int clothPlaced = PlaceClothCoverBuildings(
            clothCoverPrefabs,
            count,
            ref rng,
            reservedFootprints,
            shopAndHouseFootprints);
        int archwaysPlaced = PlaceCentralArchwayBuildings(
            archwayPrefabs,
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

        int attempts = 0;
        int placed = 0;
        int maxAttempts = Mathf.Max(160, remainingCount * 24);
        int maxDistance = townRadius + 3;
        List<GameObject> randomPrefabs = freeScatterPrefabs.Count > 0 ? freeScatterPrefabs : prefabs;

        while (placed < remainingCount && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int plotCell = _runtimeCityBuildingPlotSystem.GetRandomScatterPlotCell(centerRoadCell, maxDistance, ref rng);

            int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
            if (distance > maxDistance)
                continue;
            if (roadCells.Contains(plotCell))
                continue;
            if (!_runtimeCityBuildingPlotSystem.HasPlotSpacing(plotCell, usedPlotCells, 1))
                continue;

            GameObject prefab = _runtimeCityPrefabSelectionSystem.GetRandomPrefab(randomPrefabs, ref rng);
            if (prefab == null)
                continue;

            Vector2Int footprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(prefab);
            Vector2Int preferredOrigin = _runtimeCityBuildingPlotSystem.GetCenteredOriginForPlot(plotCell, footprint, roadCellSizeInGridCells);
            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, 0))
                continue;

            if (!_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding(
                    prefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "City Decoration",
                    "Decorative old-town structure.",
                    footprint,
                    defaultBuildingMaxHealth))
            {
                continue;
            }

            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, 0))
            {
                _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                continue;
            }

            usedPlotCells.Add(plotCell);
            _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, 0);
            placed++;
        }
    }

    private int PlaceCentralArchwayBuildings(
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
        int minDistance = Mathf.Max(1, hallPlazaRadiusRoadCells + 1);
        int maxDistance = hallPlazaRadiusRoadCells + 5;

        while (placed < maxCount && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int plotCell = _runtimeCityBuildingPlotSystem.GetRandomScatterPlotCell(centerRoadCell, maxDistance, ref rng);
            int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
            if (distance < minDistance || distance > maxDistance)
                continue;
            if (roadCells.Contains(plotCell))
                continue;
            if (!_runtimeCityBuildingPlotSystem.HasPlotSpacing(plotCell, usedPlotCells, 1))
                continue;

            GameObject prefab = archwayPrefabs[placed % archwayPrefabs.Count];
            if (prefab == null)
                continue;

            Vector2Int footprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(prefab);
            Vector2Int preferredOrigin = _runtimeCityBuildingPlotSystem.GetCenteredOriginForPlot(plotCell, footprint, roadCellSizeInGridCells);
            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, 0))
                continue;

            if (!_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding(
                    prefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "Archway",
                    "Decorative archway near the town center.",
                    footprint,
                    defaultBuildingMaxHealth))
            {
                continue;
            }

            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, 0))
            {
                _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                continue;
            }

            usedPlotCells.Add(plotCell);
            _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, 0);
            placed++;
        }

        return placed;
    }

    private int PlaceClothCoverBuildings(
        List<GameObject> clothCoverPrefabs,
        int maxCount,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        if (clothCoverPrefabs == null || clothCoverPrefabs.Count == 0 || maxCount <= 0 || shopAndHouseFootprints == null || shopAndHouseFootprints.Count == 0)
            return 0;

        var anchorIndices = new List<int>(shopAndHouseFootprints.Count);
        for (int i = 0; i < shopAndHouseFootprints.Count; i++)
            anchorIndices.Add(i);
        _runtimeCityPrefabSelectionSystem.Shuffle(anchorIndices, ref rng);

        int placed = 0;
        int anchorCursor = 0;
        int prefabCursor = 0;
        int targetCount = Mathf.Min(maxCount, clothCoverPrefabs.Count);
        while (placed < targetCount && anchorCursor < anchorIndices.Count)
        {
            GameObject prefab = clothCoverPrefabs[prefabCursor % clothCoverPrefabs.Count];
            prefabCursor++;
            RectInt anchor = shopAndHouseFootprints[anchorIndices[anchorCursor]];
            anchorCursor++;

            if (TrySpawnAdjacentDecoration(prefab, anchor, ref rng, reservedFootprints))
                placed++;
        }

        return placed;
    }

    private bool TrySpawnAdjacentDecoration(
        GameObject prefab,
        RectInt anchorRect,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        if (prefab == null)
            return false;

        Vector2Int footprint = _runtimeCityPrefabSelectionSystem.GetCachedFootprintCells(prefab);
        var candidateOrigins = _runtimeCityBuildingPlotSystem.BuildAdjacentOrigins(anchorRect, footprint);
        _runtimeCityPrefabSelectionSystem.Shuffle(candidateOrigins, ref rng);

        for (int i = 0; i < candidateOrigins.Count; i++)
        {
            Vector2Int preferredOrigin = candidateOrigins[i];
            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(preferredOrigin, footprint, reservedFootprints, 0))
                continue;

            if (!_runtimeCitySpawnBridgeSystem.TrySpawnCityBuilding(
                    prefab,
                    preferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    "City Decoration",
                    "Decorative structure beside a town building.",
                    footprint,
                    defaultBuildingMaxHealth))
            {
                continue;
            }

            if (_runtimeCityWalkabilitySystem.WouldBeTooCloseToReserved(actualOrigin, actualFootprint, reservedFootprints, 0) ||
                !_runtimeCityWalkabilitySystem.TouchesRect(new RectInt(actualOrigin, actualFootprint), anchorRect))
            {
                _runtimeCitySpawnBridgeSystem.DeleteCityBuilding(buildingId);
                continue;
            }

            _runtimeCityWalkabilitySystem.ReserveFootprint(reservedFootprints, actualOrigin, actualFootprint, 0);
            return true;
        }

        return false;
    }

}
