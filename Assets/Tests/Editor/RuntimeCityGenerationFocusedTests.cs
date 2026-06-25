#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class RuntimeCityGenerationFocusedTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new RuntimeCityGenerationFocusedTests();
            tests.CityCenterPlanning_ClampsStartOutsideBaseExclusions();
            tests.TownRoadLayout_BuildsConnectedRoadStrokesAndAutobahnExit();
            Debug.Log("[RuntimeCityGenerationFocusedValidation] result=Passed tests=2");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[RuntimeCityGenerationFocusedValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void CityCenterPlanning_ClampsStartOutsideBaseExclusions()
    {
        RuntimeCityLayoutState layout = new();
        RuntimeCityConfigCompositionSystemHelper.Snapshot config = CreateCityConfig(
            cityCount: 3,
            startCell: new Vector2Int(80, 80));
        GridConfig grid = CreateGrid(width: 256, height: 256);
        const int RoadCellSize = 4;
        int townRadius = layout.CalculateTownRadius(config);

        Vector2Int preferredCenter = layout.ClampRoadCellToBuildableArea(
            config.StartCell / RoadCellSize,
            grid,
            RoadCellSize,
            townRadius,
            config.HallPlazaRadiusRoadCells);
        List<RectInt> baseExclusionRoadRects = new()
        {
            new RectInt(preferredCenter.x - 2, preferredCenter.y - 2, 5, 5)
        };

        Vector2Int plannedCenter = layout.FindNearestRoadCellOutsideBaseExclusions(
            preferredCenter,
            baseExclusionRoadRects,
            grid,
            RoadCellSize,
            townRadius,
            config.HallPlazaRadiusRoadCells);

        layout.GetRoadGridBounds(
            grid,
            RoadCellSize,
            townRadius,
            config.HallPlazaRadiusRoadCells,
            out int minRoadX,
            out int maxRoadX,
            out int minRoadY,
            out int maxRoadY);

        Assert.IsTrue(RuntimeCityLayoutState.IsRoadCellInsideAnyBaseExclusion(preferredCenter, baseExclusionRoadRects));
        Assert.IsFalse(RuntimeCityLayoutState.IsRoadCellInsideAnyBaseExclusion(plannedCenter, baseExclusionRoadRects));
        Assert.IsTrue(RuntimeCityLayoutState.IsRoadCellWithinBounds(plannedCenter, minRoadX, maxRoadX, minRoadY, maxRoadY));

        RuntimeCityLayoutSystem.CityLayoutData existingCity = new()
        {
            CenterRoadCell = plannedCenter,
            TownRadius = townRadius
        };
        Assert.IsFalse(layout.IsCityCenterFarEnough(
            plannedCenter,
            new List<RuntimeCityLayoutSystem.CityLayoutData> { existingCity },
            townRadius,
            baseExclusionRoadRects,
            config));
    }

    [Test]
    public void TownRoadLayout_BuildsConnectedRoadStrokesAndAutobahnExit()
    {
        RuntimeCityRoadLayoutState roadLayout = new();
        var rng = new Unity.Mathematics.Random(123456u);
        Vector2Int center = new(32, 32);

        List<List<Vector2Int>> roadStrokes = roadLayout.BuildTownRoadStrokes(
            center,
            townRadius: 8,
            plazaRadius: 2,
            ref rng);
        HashSet<Vector2Int> roadCells = CollectAndAssertConnectedRoadCells(roadStrokes);

        Assert.GreaterOrEqual(roadStrokes.Count, 8, "Runtime city generation should create a town road network, not only one connector.");
        Assert.Greater(roadCells.Count, 40, "Town road strokes should cover enough road cells for downstream building placement.");

        List<Vector2Int> autobahn = roadLayout.BuildAutobahnPath(
            roadCells,
            center,
            CreateGrid(width: 256, height: 256),
            roadCellSizeInGridCells: 4,
            autobahnEdgeMarginRoadCells: 3,
            autobahnMinLengthRoadCells: 4);

        Assert.GreaterOrEqual(autobahn.Count, 3, "A generated city should expose a road stub usable for city-to-city chaining.");
        Assert.IsTrue(roadCells.Contains(autobahn[0]), "Autobahn path must start from a town road endpoint.");
        AssertStraightConnectedPath(autobahn);
    }

    private static HashSet<Vector2Int> CollectAndAssertConnectedRoadCells(List<List<Vector2Int>> roadStrokes)
    {
        HashSet<Vector2Int> roadCells = new();
        for (int strokeIndex = 0; strokeIndex < roadStrokes.Count; strokeIndex++)
        {
            List<Vector2Int> stroke = roadStrokes[strokeIndex];
            Assert.GreaterOrEqual(stroke.Count, 2, $"Road stroke {strokeIndex} should have at least two cells.");
            for (int i = 0; i < stroke.Count; i++)
            {
                roadCells.Add(stroke[i]);
                if (i == 0)
                    continue;

                Assert.AreEqual(
                    1,
                    ManhattanDistance(stroke[i - 1], stroke[i]),
                    $"Road stroke {strokeIndex} contains a gap or diagonal step at index {i}.");
            }
        }

        return roadCells;
    }

    private static void AssertStraightConnectedPath(List<Vector2Int> path)
    {
        Assert.GreaterOrEqual(path.Count, 2);
        Vector2Int expectedStep = path[1] - path[0];
        Assert.AreEqual(1, ManhattanDistance(Vector2Int.zero, expectedStep));
        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int step = path[i] - path[i - 1];
            Assert.AreEqual(expectedStep, step, $"Path should stay straight at index {i}.");
        }
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return math.abs(a.x - b.x) + math.abs(a.y - b.y);
    }

    private static GridConfig CreateGrid(int width, int height)
    {
        return new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = float3.zero
        };
    }

    private static RuntimeCityConfigCompositionSystemHelper.Snapshot CreateCityConfig(int cityCount, Vector2Int startCell)
    {
        List<GameObject> emptyPrefabs = new();
        return new RuntimeCityConfigCompositionSystemHelper.Snapshot(
            spawnOnStart: true,
            generateBuildings: false,
            randomSeed: 24681357,
            cityCount: cityCount,
            startCell: startCell,
            generationYieldInterval: 0,
            gasStationCount: 0,
            shopCount: 0,
            houseCount: 0,
            otherBuildingCount: 0,
            cityDecorationBuildingCount: 0,
            hallPlazaRadiusRoadCells: 2,
            extraTownRadiusRoadCells: 5,
            cityMinSpacingRoadCells: 18,
            ruralHouseRatio: 0.35f,
            gasStationMinSpacingRoadCells: 3,
            houseWallChance: 0f,
            houseWallMinDistanceCells: 2,
            houseWallMaxDistanceCells: 4,
            landmarkMinDistanceFromHallRoadCells: 3,
            landmarkClearanceCells: 4,
            autobahnMinLengthRoadCells: 8,
            autobahnEdgeMarginRoadCells: 3,
            defaultBuildingMaxHealth: 300,
            clockTowerPrefab: null,
            fountainPrefabs: emptyPrefabs,
            monumentPrefabs: emptyPrefabs,
            pillarPrefabs: emptyPrefabs,
            hallPrefabs: emptyPrefabs,
            gasStationPrefabs: emptyPrefabs,
            shopPrefabs: emptyPrefabs,
            housePrefabs: emptyPrefabs,
            otherBuildingPrefabs: emptyPrefabs,
            cityDecorationPrefabs: emptyPrefabs,
            houseWallPrefabs: emptyPrefabs,
            houseWallGatePrefab: null,
            houseWallPillarPrefab: null);
    }
}
#endif
