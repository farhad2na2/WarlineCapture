using Game.Components;
using Game.Configs;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
            tests.TownRoadLayout_VisualTerminalPolicyCompactsOnlyConfiguredOuterRoads();
            tests.RoadVisualPrototype_CreatesContinuousShoulderedConnections();
            tests.RoadVisualPrototype_DeduplicatesRepeatedCellsAndEdgesWithinOneRoot();
            tests.RoadVisualPrototype_DisposeDestroysRootWithoutDestroyingBorrowedMaterials();
            tests.RoadVisualPrototype_BoundaryIsRAndDOnlyAndOwnerDisposesIt();
            tests.RoadsidePlots_CarryCardinalRoadFacingIntent();
            tests.VisualPrototypeDistrictIntent_OrdersMarketResidentialAndUtilityPlots();
            tests.VisualPrototypeDamageScatter_BiasesTowardAuthoredCorridor();
            tests.VisualPrototypeScatterPolicy_BoundsDamageWithoutChangingProductionDefault();
            tests.VisualPrototypeDecorationPolicy_ReservesDamageAnchorsWithoutChangingProductionDefault();
            tests.AlgorithmicDistrictPresentation_CreatesConfiguredIrregularSurfaces();
            tests.AlgorithmicAftermathPresentation_GroupsDressingAroundPlacedDamage();
            tests.AlgorithmicAftermathPresentation_FillsSparseSeedsFromConfiguredDistrict();
            tests.AlgorithmicAftermathPresentation_ReservesAuthoredAnchorsWhenDamageIsDense();
            tests.GenerationProgress_ReportsMonotonicStagesAndCompletion();
            tests.GenerationProgress_CancellationPreservesLastKnownWork();
            tests.GenerationRecovery_SchedulesOnlyOneDeterministicFallback();
            tests.GenerationRecovery_TerminalProgressPreservesEvidence();
            tests.VisualOnlySpawnBridge_SpawnsAndDeletesUsingExistingPresentation();
            tests.VisualOnlySpawnBridge_RotatesAndRecentersRectangularFootprint();
            tests.VisualOnlyPresentation_GroundsLowestRendererPoint();
            tests.PlanOnlySpawnBridge_TracksPlacementWithoutCreatingVisuals();
            tests.VisualPrototypePrefabSelection_LimitsConsecutiveRepetitionDeterministically();
            tests.VisualQualityClearance_PreservesLargeRockWithoutExplicitCleanup();
            tests.VisualQualityClearance_SuppressesLargeRockWithExplicitCleanup();
            tests.VisualQualityClearance_SuppressesSmallDressingOutsideDistrictFootprint();
            tests.VisualQualityFoundation_BorrowedMaterialSurvivesDisposeAndColliderIsRemoved();
            tests.VisualQualityBoundary_IsGenerationTimeOnlyAndOwnersDisposeIt();
            tests.RuntimeCameraPose_PreservesStageAndClampsPresentationValues();
            tests.RuntimeDistrictModuleRecipe_SupportsExactPrefabReplayOrIndexedSlices();
            tests.RuntimePrototypeArchitecture_UsesPassiveViewAndSystemBaseOwner();
            Debug.Log("[RuntimeCityGenerationFocusedValidation] result=Passed tests=33");
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

        RuntimeCityLayoutUtilitySystemHelper.CityLayoutData existingCity = new()
        {
            CenterRoadCell = plannedCenter,
            TownRadius = townRadius
        };
        Assert.IsFalse(layout.IsCityCenterFarEnough(
            plannedCenter,
            new List<RuntimeCityLayoutUtilitySystemHelper.CityLayoutData> { existingCity },
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

    [Test]
    public void TownRoadLayout_VisualTerminalPolicyCompactsOnlyConfiguredOuterRoads()
    {
        Vector2Int center = new(32, 32);
        var standardRng = new Unity.Mathematics.Random(123456u);
        var standard = new RuntimeCityRoadLayoutState();
        List<List<Vector2Int>> standardStrokes = standard.BuildTownRoadStrokes(
            center,
            townRadius: 12,
            plazaRadius: 2,
            ref standardRng);

        var compactRng = new Unity.Mathematics.Random(123456u);
        var compact = new RuntimeCityRoadLayoutState();
        compact.ConfigureTerminalPolicy(new RuntimeCityRoadTerminalPolicy(
            northRadialTrim: 4,
            eastRadialTrim: 4,
            southRadialTrim: 3,
            westRadialTrim: 1,
            maximumOuterStreetLength: 3));
        List<List<Vector2Int>> compactStrokes = compact.BuildTownRoadStrokes(
            center,
            townRadius: 12,
            plazaRadius: 2,
            ref compactRng);

        Assert.AreEqual(standardStrokes.Count, compactStrokes.Count);
        Assert.AreEqual(standardStrokes[4][standardStrokes[4].Count - 1].y - 4, compactStrokes[4][compactStrokes[4].Count - 1].y);
        Assert.AreEqual(standardStrokes[5][standardStrokes[5].Count - 1].y + 3, compactStrokes[5][compactStrokes[5].Count - 1].y);
        Assert.AreEqual(standardStrokes[6][standardStrokes[6].Count - 1].x - 4, compactStrokes[6][compactStrokes[6].Count - 1].x);
        Assert.AreEqual(standardStrokes[7][standardStrokes[7].Count - 1].x + 1, compactStrokes[7][compactStrokes[7].Count - 1].x);
        for (int strokeIndex = 12; strokeIndex < 16; strokeIndex++)
        {
            Assert.LessOrEqual(
                compactStrokes[strokeIndex].Count - 1,
                3,
                $"Visual outer street {strokeIndex} exceeded its configured cap.");
        }

        HashSet<Vector2Int> compactCells = CollectAndAssertConnectedRoadCells(compactStrokes);
        Assert.Greater(compactCells.Count, 40);

        var explicitDefaultRng = new Unity.Mathematics.Random(123456u);
        var explicitDefault = new RuntimeCityRoadLayoutState();
        explicitDefault.ConfigureTerminalPolicy(default);
        List<List<Vector2Int>> defaultStrokes = explicitDefault.BuildTownRoadStrokes(
            center,
            townRadius: 12,
            plazaRadius: 2,
            ref explicitDefaultRng);
        for (int strokeIndex = 0; strokeIndex < standardStrokes.Count; strokeIndex++)
            CollectionAssert.AreEqual(standardStrokes[strokeIndex], defaultStrokes[strokeIndex]);
    }

    [Test]
    public void RoadsidePlots_CarryCardinalRoadFacingIntent()
    {
        var plots = new RuntimeCityBuildingPlotState();
        var roadCells = new HashSet<Vector2Int> { new(10, 10) };

        List<RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate> candidates = plots.CollectRoadsidePlots(
            roadCells,
            new Vector2Int(10, 10),
            townRadius: 2,
            minDistance: 0,
            maxDistance: 2);

        Assert.AreEqual(4, candidates.Count);
        for (int i = 0; i < candidates.Count; i++)
        {
            RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate candidate = candidates[i];
            Assert.AreEqual(
                new Vector2Int(10, 10),
                candidate.PlotCell + candidate.RoadFacingDirection,
                $"Roadside plot {candidate.PlotCell} must face its adjacent road cell.");
            Assert.AreEqual(1, ManhattanDistance(Vector2Int.zero, candidate.RoadFacingDirection));
        }
    }

    [Test]
    public void RoadVisualPrototype_CreatesContinuousShoulderedConnections()
    {
        GameObject runtimeRoot = new("RuntimeCityRoadVisualTestRoot");
        var roadVisuals = new RuntimeCityRoadVisualPrototypeSystemHelper();
        try
        {
            roadVisuals.Configure(
                runtimeRoot.transform,
                CreateGrid(width: 64, height: 64),
                roadCellSizeInGridCells: 4,
                material: null,
                shoulderMaterial: null,
                roadColor: new Color(0.12f, 0.13f, 0.14f, 1f),
                shoulderColor: new Color(0.56f, 0.42f, 0.28f, 1f));
            roadVisuals.CreateStroke(
                new List<Vector2Int> { new(4, 5), new(5, 5), new(6, 5) },
                isAutobahn: false,
                useAutobahnConnectorAtStart: false,
                useAutobahnConnectorAtEnd: false);

            Transform roadRoot = runtimeRoot.transform.Find("RuntimeCityRoadVisuals");
            Assert.IsNotNull(roadRoot);
            Transform firstLink = roadRoot.Find("RoadLink_4_5_5_5");
            Transform secondLink = roadRoot.Find("RoadLink_5_5_6_5");
            Assert.IsNotNull(firstLink, "Adjacent road cells need a continuous asphalt connector.");
            Assert.IsNotNull(secondLink, "Every adjacent pair in a stroke needs a connector.");
            Assert.Greater(firstLink.localScale.x, 4f, "Road links must overlap their node pads to avoid seams.");
            Assert.IsNotNull(roadRoot.Find("RoadShoulderLink_4_5_5_5"));
            Assert.AreEqual(3, roadVisuals.RoadCellCount);
        }
        finally
        {
            roadVisuals.Dispose();
            UnityEngine.Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void RoadVisualPrototype_DeduplicatesRepeatedCellsAndEdgesWithinOneRoot()
    {
        GameObject runtimeRoot = new("RuntimeCityRoadVisualBoundedRoot");
        var roadVisuals = new RuntimeCityRoadVisualPrototypeSystemHelper();
        try
        {
            roadVisuals.Configure(
                runtimeRoot.transform,
                CreateGrid(width: 64, height: 64),
                roadCellSizeInGridCells: 4,
                material: null,
                shoulderMaterial: null,
                roadColor: Color.gray,
                shoulderColor: Color.black);
            var cells = new List<Vector2Int> { new(4, 5), new(5, 5), new(6, 5) };

            roadVisuals.CreateStroke(cells, false, false, false);
            Transform roadRoot = runtimeRoot.transform.Find("RuntimeCityRoadVisuals");
            Assert.IsNotNull(roadRoot);
            Assert.AreEqual(10, roadRoot.childCount, "Three nodes and two edges each own one road and shoulder slab.");

            roadVisuals.CreateStroke(cells, false, false, false);
            cells.Reverse();
            roadVisuals.CreateStroke(cells, false, false, false);

            Assert.AreEqual(3, roadVisuals.RoadCellCount);
            Assert.AreEqual(3, roadVisuals.StrokeCount);
            Assert.AreEqual(10, roadRoot.childCount, "Repeated or reversed strokes must not grow the R&D root.");
        }
        finally
        {
            roadVisuals.Dispose();
            UnityEngine.Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void RoadVisualPrototype_DisposeDestroysRootWithoutDestroyingBorrowedMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Assert.IsNotNull(shader);
        GameObject runtimeRoot = new("RuntimeCityRoadVisualDisposeRoot");
        Material roadMaterial = new(shader) { name = "BorrowedRoadMaterial" };
        Material shoulderMaterial = new(shader) { name = "BorrowedShoulderMaterial" };
        var roadVisuals = new RuntimeCityRoadVisualPrototypeSystemHelper();
        try
        {
            roadVisuals.Configure(
                runtimeRoot.transform,
                CreateGrid(width: 64, height: 64),
                roadCellSizeInGridCells: 4,
                material: roadMaterial,
                shoulderMaterial: shoulderMaterial,
                roadColor: Color.gray,
                shoulderColor: Color.black,
                cloneSourceMaterials: false);
            roadVisuals.CreateStroke(
                new List<Vector2Int> { new(4, 5), new(5, 5) },
                false,
                false,
                false);
            Assert.IsNotNull(runtimeRoot.transform.Find("RuntimeCityRoadVisuals"));

            roadVisuals.Dispose();

            Assert.IsNull(runtimeRoot.transform.Find("RuntimeCityRoadVisuals"));
            Assert.IsTrue(roadMaterial != null);
            Assert.IsTrue(shoulderMaterial != null);
            Assert.AreEqual(0, roadVisuals.RoadCellCount);
            Assert.AreEqual(0, roadVisuals.StrokeCount);
        }
        finally
        {
            roadVisuals.Dispose();
            UnityEngine.Object.DestroyImmediate(roadMaterial);
            UnityEngine.Object.DestroyImmediate(shoulderMaterial);
            UnityEngine.Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void RoadVisualPrototype_BoundaryIsRAndDOnlyAndOwnerDisposesIt()
    {
        string environmentRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "Game/Scripts/Environment"));
        string helperPath = Path.Combine(environmentRoot, "RuntimeCityRoadVisualPrototypeSystemHelper.cs");
        string helperSource = File.ReadAllText(helperPath);
        string ownerSource = File.ReadAllText(Path.Combine(
            environmentRoot,
            "RuntimeCityRAndDMapCompositionSystemHelper.cs"));

        Assert.AreEqual(286, File.ReadAllLines(helperPath).Length);
        Assert.AreEqual(10976, new FileInfo(helperPath).Length);
        Assert.AreEqual(1, CountOccurrences(
            ownerSource,
            "new RuntimeCityRoadVisualPrototypeSystemHelper()"));
        Assert.AreEqual(1, CountOccurrences(ownerSource, "_roadVisuals.Configure("));
        Assert.AreEqual(1, CountOccurrences(ownerSource, "_roadVisuals?.Dispose();"));
        Assert.That(
            ownerSource.Replace("\r\n", "\n"),
            Does.Contain("DisposeGeneration();\n            DestroyGeneratedRoot();"));
        Assert.That(helperSource, Does.Contain("RuntimeCityRoad_RnD"));
        Assert.That(helperSource, Does.Contain("RuntimeCityRoadShoulder_RnD"));
        Assert.That(helperSource, Does.Not.Contain("World.DefaultGameObjectInjectionWorld"));
        Assert.That(helperSource, Does.Not.Contain("EntityManager"));
        Assert.That(helperSource, Does.Not.Contain("Update("));
    }

    [Test]
    public void VisualPrototypeDistrictIntent_OrdersMarketResidentialAndUtilityPlots()
    {
        Vector2Int center = new(20, 20);
        var plots = new List<RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate>
        {
            new() { PlotCell = new Vector2Int(26, 20), DistanceFromCenter = 6 },
            new() { PlotCell = new Vector2Int(14, 20), DistanceFromCenter = 6 },
            new() { PlotCell = new Vector2Int(20, 26), DistanceFromCenter = 6 },
            new() { PlotCell = new Vector2Int(20, 14), DistanceFromCenter = 6 }
        };

        List<RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate> market =
            RuntimeCityBulkPlotPlanState.CreateDistrictOrderedPlots(null, plots, center, Vector2Int.left);
        List<RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate> residential =
            RuntimeCityBulkPlotPlanState.CreateDistrictOrderedPlots(null, plots, center, Vector2Int.down);
        List<RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate> utility =
            RuntimeCityBulkPlotPlanState.CreateDistrictOrderedPlots(null, plots, center, Vector2Int.right);

        Assert.AreEqual(new Vector2Int(14, 20), market[0].PlotCell);
        Assert.AreEqual(new Vector2Int(20, 14), residential[0].PlotCell);
        Assert.AreEqual(new Vector2Int(26, 20), utility[0].PlotCell);
        Assert.AreEqual(new Vector2Int(26, 20), plots[0].PlotCell, "District ordering must not mutate the shared plot list.");

        var boundedPlots = new List<RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate>
        {
            new() { PlotCell = new Vector2Int(10, 20), DistanceFromCenter = 10 },
            new() { PlotCell = new Vector2Int(15, 20), DistanceFromCenter = 5 }
        };
        List<RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate> boundedMarket =
            RuntimeCityBulkPlotPlanState.CreateDistrictOrderedPlots(
                null,
                boundedPlots,
                center,
                Vector2Int.left,
                preferredMaximumDistance: 6);
        Assert.AreEqual(
            new Vector2Int(15, 20),
            boundedMarket[0].PlotCell,
            "Visual district ordering must prefer the bounded density band over an extreme directional candidate.");

        var state = new RuntimeCityBulkPlotPlanState();
        state.ConfigureDistrictIntent(true);
        Assert.AreEqual(new Vector2Int(1, -1), state.ResidentialScatterDirection);
        Assert.AreEqual(new Vector2Int(1, 1), state.UtilityScatterDirection);
        Assert.AreEqual(1, state.RuralScatterRadiusOffset);
        state.ConfigureDistrictIntent(false);
        Assert.AreEqual(Vector2Int.zero, state.ResidentialScatterDirection);
        Assert.AreEqual(Vector2Int.zero, state.UtilityScatterDirection);
        Assert.AreEqual(3, state.RuralScatterRadiusOffset);
    }

    [Test]
    public void VisualPrototypeDamageScatter_BiasesTowardAuthoredCorridor()
    {
        Vector2Int center = new(20, 20);
        Vector2Int northCandidate = new(23, 27);
        Vector2Int southCandidate = new(17, 14);

        Vector2Int reflected = RuntimeCityFreeScatterDecorationState.ApplyDirectionalBias(
            northCandidate,
            center,
            Vector2Int.down);
        Vector2Int retained = RuntimeCityFreeScatterDecorationState.ApplyDirectionalBias(
            southCandidate,
            center,
            Vector2Int.down);

        Vector2Int southwest = RuntimeCityFreeScatterDecorationState.ApplyDirectionalBias(
            northCandidate,
            center,
            new Vector2Int(-1, -1));

        Assert.AreEqual(new Vector2Int(23, 13), reflected);
        Assert.AreEqual(southCandidate, retained);
        Assert.AreEqual(new Vector2Int(17, 13), southwest);
    }

    [Test]
    public void VisualPrototypeScatterPolicy_BoundsDamageWithoutChangingProductionDefault()
    {
        var state = new RuntimeCityFreeScatterDecorationState();

        Assert.AreEqual(3, state.MaximumDistanceOffset);
        Assert.AreEqual(13, state.CalculateMaximumDistance(10));

        state.ConfigureMaximumDistanceOffset(2);
        Assert.AreEqual(2, state.MaximumDistanceOffset);
        Assert.AreEqual(12, state.CalculateMaximumDistance(10));

        state.ConfigureMaximumDistanceOffset(-2);
        Assert.AreEqual(0, state.MaximumDistanceOffset);
        Assert.AreEqual(10, state.CalculateMaximumDistance(10));

        state.ConfigureMaximumDistanceOffset(2);
        state.ConfigureMaximumAxisDistanceInset(2);
        Assert.IsFalse(state.IsWithinMaximumDistance(new Vector2Int(32, 20), new Vector2Int(20, 20), 12, out _));
        Assert.IsFalse(state.IsWithinMaximumDistance(new Vector2Int(31, 20), new Vector2Int(20, 20), 12, out _));
        Assert.IsTrue(state.IsWithinMaximumDistance(new Vector2Int(30, 20), new Vector2Int(20, 20), 12, out _));
        Assert.IsTrue(state.IsWithinMaximumDistance(new Vector2Int(26, 26), new Vector2Int(20, 20), 12, out _));
        Assert.IsFalse(state.IsWithinMaximumDistance(new Vector2Int(27, 26), new Vector2Int(20, 20), 12, out _));

        var ruralState = new RuntimeCityRuralBuildingSpawnState();
        Assert.IsTrue(ruralState.IsWithinMaximumDistance(
            new Vector2Int(31, 20),
            new Vector2Int(20, 20),
            11,
            out int productionDistance));
        Assert.AreEqual(11, productionDistance);

        ruralState.ConfigureMaximumAxisDistanceInset(1);
        Assert.IsFalse(ruralState.IsWithinMaximumDistance(
            new Vector2Int(31, 20),
            new Vector2Int(20, 20),
            11,
            out _));
        Assert.IsTrue(ruralState.IsWithinMaximumDistance(
            new Vector2Int(30, 21),
            new Vector2Int(20, 20),
            11,
            out _));
    }

    [Test]
    public void VisualPrototypeDecorationPolicy_ReservesDamageAnchorsWithoutChangingProductionDefault()
    {
        var state = new RuntimeCityDecorationBuildingSpawnState();

        Assert.AreEqual(14, state.CalculateArchwayBudget(14, 0));
        Assert.AreEqual(6, state.CalculateArchwayBudget(14, 8));

        state.ConfigureMinimumFreeScatterCount(4);
        Assert.AreEqual(10, state.CalculateArchwayBudget(14, 0));
        Assert.AreEqual(2, state.CalculateArchwayBudget(14, 8));
        Assert.AreEqual(0, state.CalculateArchwayBudget(3, 0));

        state.ConfigureMinimumFreeScatterCount(-1);
        Assert.AreEqual(14, state.CalculateArchwayBudget(14, 0));
        Assert.AreEqual(0, state.CalculateArchwayBudget(4, 6));
    }

    [Test]
    public void AlgorithmicDistrictPresentation_CreatesConfiguredIrregularSurfaces()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Assert.IsNotNull(shader);
        Material material = new(shader);
        GameObject root = new("RuntimeCityDistrictPresentationTestRoot");
        var presentation = new RuntimeCityAlgorithmicDistrictPresentationSystemHelper();
        var settings = new List<Game.Configs.RuntimeOperationMapAlgorithmicDistrictSurfaceSettings>
        {
            new(
                "TestMarketApron",
                material,
                new Vector2(-2f, 1f),
                new Vector2(5f, 4f),
                new Color(0.5f, 0.3f, 0.2f, 1f),
                11u)
        };
        try
        {
            presentation.CreateSurfaces(
                settings,
                100u,
                Vector3.zero,
                10f,
                new Color(0.45f, 0.32f, 0.20f, 1f),
                root.transform);

            Assert.AreEqual(3, presentation.SurfaceCount);
            Transform surface = root.transform.Find("RuntimeCityDistrictSurfaces/TestMarketApron");
            Transform transition = root.transform.Find("RuntimeCityDistrictSurfaces/TestMarketApron_Transition");
            Transform outerTransition = root.transform.Find("RuntimeCityDistrictSurfaces/TestMarketApron_OuterTransition");
            Assert.IsNotNull(surface);
            Assert.IsNotNull(transition);
            Assert.IsNotNull(outerTransition);
            Assert.AreEqual(new Vector3(-20f, 0.012f, 10f), surface.position);
            Assert.AreEqual(new Vector3(50f, 1f, 40f), surface.localScale);
            Assert.AreEqual(new Vector3(-20f, 0.006f, 10f), transition.position);
            Assert.AreEqual(new Vector3(57.5f, 1f, 46f), transition.localScale);
            Assert.AreEqual(new Vector3(-20f, 0.003f, 10f), outerTransition.position);
            Assert.That(outerTransition.localScale.x, Is.EqualTo(67f).Within(0.001f));
            Assert.That(outerTransition.localScale.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(outerTransition.localScale.z, Is.EqualTo(53.6f).Within(0.001f));
            Assert.IsNotNull(surface.GetComponent<MeshFilter>()?.sharedMesh);
        }
        finally
        {
            presentation.Dispose();
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void AlgorithmicAftermathPresentation_GroupsDressingAroundPlacedDamage()
    {
        GameObject root = new("RuntimeCityAftermathPresentationTestRoot");
        GameObject visualRoot = new("RuntimeCityVisuals");
        visualRoot.transform.SetParent(root.transform, false);
        GameObject damageAnchorPrefab = new("DamageAnchor");
        GameObject damageAnchor = new("DamageAnchor_Visual");
        damageAnchor.transform.SetParent(visualRoot.transform, false);
        damageAnchor.transform.position = new Vector3(-20f, 0f, -30f);
        GameObject anchorVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        anchorVisual.transform.SetParent(damageAnchor.transform, false);
        anchorVisual.transform.localPosition = new Vector3(0f, 1f, 0f);
        anchorVisual.transform.localScale = new Vector3(2f, 2f, 2f);
        GameObject dressingPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dressingPrefab.name = "SmallRubble";
        dressingPrefab.transform.localScale = Vector3.one * 0.4f;
        var settings = new Game.Configs.RuntimeOperationMapAlgorithmicAftermathSettings(
            "TestAftermathDressing",
            new List<GameObject> { dressingPrefab },
            anchorGroupCount: 1,
            dressingItemsPerGroup: 3,
            minimumRadius: 4f,
            maximumRadius: 6f,
            minimumScale: 1.5f,
            maximumScale: 2f,
            preferredExposureDirection: new Vector2(-1f, -1f),
            preferredExposureArcDegrees: 70f,
            deterministicSeedOffset: 19u);
        var presentation = new RuntimeCityAlgorithmicAftermathPresentationSystemHelper();
        try
        {
            presentation.CreateGroupedDressing(
                settings,
                new List<GameObject> { damageAnchorPrefab },
                seed: 100u,
                root.transform);

            Assert.AreEqual(1, presentation.GroupCount);
            Assert.GreaterOrEqual(presentation.DressingCount, 1);
            Assert.LessOrEqual(presentation.DressingCount, settings.RequestedItemCount);
            Assert.GreaterOrEqual(presentation.MaximumPlanarExtent, settings.MinScale);
            Transform dressingRoot = root.transform.Find("TestAftermathDressing");
            Assert.IsNotNull(dressingRoot);
            Renderer[] dressingRenderers = dressingRoot.GetComponentsInChildren<Renderer>(true);
            Assert.AreEqual(presentation.DressingCount, dressingRenderers.Length);
            for (int i = 0; i < dressingRenderers.Length; i++)
            {
                Vector3 offset = dressingRenderers[i].bounds.center - damageAnchor.transform.position;
                Vector2 planarOffset = new(offset.x, offset.z);
                Assert.LessOrEqual(planarOffset.magnitude, settings.MaxRadius + 1f);
                Assert.Greater(
                    Vector2.Dot(planarOffset.normalized, settings.ExposureDirection),
                    0f,
                    "Directional aftermath dressing must stay on the authored exposure side of its anchor.");
                Assert.That(dressingRenderers[i].transform.localScale.x, Is.InRange(settings.MinScale, settings.MaxScale));
            }
        }
        finally
        {
            presentation.Dispose();
            UnityEngine.Object.DestroyImmediate(dressingPrefab);
            UnityEngine.Object.DestroyImmediate(damageAnchorPrefab);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void AlgorithmicAftermathPresentation_FillsSparseSeedsFromConfiguredDistrict()
    {
        GameObject root = new("RuntimeCityAftermathFallbackTestRoot");
        GameObject visualRoot = new("RuntimeCityVisuals");
        visualRoot.transform.SetParent(root.transform, false);
        GameObject dressingPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dressingPrefab.name = "FallbackRubble";
        dressingPrefab.transform.localScale = Vector3.one * 0.4f;
        var settings = new RuntimeOperationMapAlgorithmicAftermathSettings(
            "FallbackAftermathDressing",
            new List<GameObject> { dressingPrefab },
            anchorGroupCount: 4,
            dressingItemsPerGroup: 3,
            minimumRadius: 2f,
            maximumRadius: 3f,
            minimumScale: 0.8f,
            maximumScale: 1f,
            preferredExposureDirection: new Vector2(-1f, -1f),
            preferredExposureArcDegrees: 70f,
            deterministicSeedOffset: 23u,
            fallbackCenterOffset: new Vector2(-5f, -6.5f),
            fallbackSpacingInRoadCells: 2f);
        var presentation = new RuntimeCityAlgorithmicAftermathPresentationSystemHelper();
        try
        {
            presentation.CreateGroupedDressing(
                settings,
                Array.Empty<GameObject>(),
                seed: 26071502u,
                cityCenter: new Vector3(-1f, 0f, -1f),
                roadCellWorldSize: 10f,
                root.transform);

            Assert.AreEqual(4, presentation.FallbackAnchorCount);
            Assert.AreEqual(4, presentation.GroupCount);
            Assert.GreaterOrEqual(presentation.DressingCount, 8);
            Assert.LessOrEqual(presentation.DressingCount, settings.RequestedItemCount);
            Transform dressingRoot = root.transform.Find("FallbackAftermathDressing");
            Assert.IsNotNull(dressingRoot);
            Assert.AreEqual(4, dressingRoot.childCount);
        }
        finally
        {
            presentation.Dispose();
            UnityEngine.Object.DestroyImmediate(dressingPrefab);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void AlgorithmicAftermathPresentation_ReservesAuthoredAnchorsWhenDamageIsDense()
    {
        GameObject root = new("RuntimeCityAftermathReserveTestRoot");
        GameObject visualRoot = new("RuntimeCityVisuals");
        visualRoot.transform.SetParent(root.transform, false);
        GameObject damageAnchorPrefab = new("DamageAnchor");
        for (int anchorIndex = 0; anchorIndex < 4; anchorIndex++)
        {
            GameObject damageAnchor = new("DamageAnchor_Visual");
            damageAnchor.transform.SetParent(visualRoot.transform, false);
            damageAnchor.transform.position = new Vector3(35f + (anchorIndex * 12f), 0f, 35f);
            GameObject anchorVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anchorVisual.transform.SetParent(damageAnchor.transform, false);
            anchorVisual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        }

        GameObject dressingPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dressingPrefab.name = "ReservedRubble";
        dressingPrefab.transform.localScale = Vector3.one * 0.4f;
        var settings = new RuntimeOperationMapAlgorithmicAftermathSettings(
            "ReservedAftermathDressing",
            new List<GameObject> { dressingPrefab },
            anchorGroupCount: 4,
            dressingItemsPerGroup: 1,
            minimumRadius: 2f,
            maximumRadius: 3f,
            minimumScale: 0.8f,
            maximumScale: 1f,
            preferredExposureDirection: new Vector2(-1f, -1f),
            preferredExposureArcDegrees: 70f,
            deterministicSeedOffset: 29u,
            fallbackCenterOffset: new Vector2(-5f, -6.5f),
            fallbackSpacingInRoadCells: 2f,
            minimumAuthoredAnchorGroupCount: 2);
        var presentation = new RuntimeCityAlgorithmicAftermathPresentationSystemHelper();
        try
        {
            presentation.CreateGroupedDressing(
                settings,
                new List<GameObject> { damageAnchorPrefab },
                seed: 26071501u,
                cityCenter: Vector3.zero,
                roadCellWorldSize: 10f,
                root.transform);

            Assert.AreEqual(
                2,
                presentation.FallbackAnchorCount,
                "Dense seeds must retain the recipe-owned foreground incident anchors.");
            Assert.AreEqual(4, presentation.GroupCount);
            Assert.AreEqual(settings.RequestedItemCount, presentation.DressingCount);
            Transform dressingRoot = root.transform.Find("ReservedAftermathDressing");
            Assert.IsNotNull(dressingRoot);
            Assert.AreEqual(4, dressingRoot.childCount);
        }
        finally
        {
            presentation.Dispose();
            UnityEngine.Object.DestroyImmediate(dressingPrefab);
            UnityEngine.Object.DestroyImmediate(damageAnchorPrefab);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void GenerationProgress_ReportsMonotonicStagesAndCompletion()
    {
        var progress = new RuntimeCityGenerationProgressState();
        progress.Begin(26071501u, requestedCityCount: 1);
        float planningProgress = progress.Current.Progress01;

        progress.Report(RuntimeCityGenerationStage.Roads, generatedCityCount: 1, completedWorkItems: 1, totalWorkItems: 1);
        float roadsProgress = progress.Current.Progress01;
        progress.Report(RuntimeCityGenerationStage.Landmarks, generatedCityCount: 1, completedWorkItems: 1, totalWorkItems: 1);
        float landmarkProgress = progress.Current.Progress01;
        progress.Report(RuntimeCityGenerationStage.Buildings, generatedCityCount: 1, completedWorkItems: 5, totalWorkItems: 11);
        float buildingProgress = progress.Current.Progress01;
        progress.Report(RuntimeCityGenerationStage.Decorations, generatedCityCount: 1, completedWorkItems: 10, totalWorkItems: 11);
        float decorationProgress = progress.Current.Progress01;
        progress.Report(RuntimeCityGenerationStage.Finalizing, generatedCityCount: 1, completedWorkItems: 1, totalWorkItems: 1);
        float finalizingProgress = progress.Current.Progress01;
        progress.Complete(generatedCityCount: 1);

        Assert.AreEqual(26071501u, progress.Current.Seed);
        Assert.AreEqual(RuntimeCityGenerationStage.Completed, progress.Current.Stage);
        Assert.AreEqual(1f, progress.Current.Progress01);
        Assert.IsTrue(progress.Current.IsTerminal);
        Assert.Less(planningProgress, roadsProgress);
        Assert.Less(roadsProgress, landmarkProgress);
        Assert.Less(landmarkProgress, buildingProgress);
        Assert.Less(buildingProgress, decorationProgress);
        Assert.Less(decorationProgress, finalizingProgress);
    }

    [Test]
    public void GenerationProgress_CancellationPreservesLastKnownWork()
    {
        var progress = new RuntimeCityGenerationProgressState();
        progress.Begin(77u, requestedCityCount: 2);
        progress.Report(RuntimeCityGenerationStage.Buildings, generatedCityCount: 1, completedWorkItems: 4, totalWorkItems: 22);
        RuntimeCityGenerationProgress beforeCancel = progress.Current;

        progress.Cancel();

        Assert.AreEqual(RuntimeCityGenerationStage.Cancelled, progress.Current.Stage);
        Assert.AreEqual(beforeCancel.Progress01, progress.Current.Progress01);
        Assert.AreEqual(beforeCancel.CompletedWorkItems, progress.Current.CompletedWorkItems);
        Assert.AreEqual(beforeCancel.TotalWorkItems, progress.Current.TotalWorkItems);
        Assert.IsTrue(progress.Current.IsTerminal);
    }

    [Test]
    public void GenerationRecovery_SchedulesOnlyOneDeterministicFallback()
    {
        RuntimeOperationMapVisualRecipe primary =
            ScriptableObject.CreateInstance<RuntimeOperationMapVisualRecipe>();
        RuntimeOperationMapVisualRecipe fallback =
            ScriptableObject.CreateInstance<RuntimeOperationMapVisualRecipe>();
        var recovery = new RuntimeOperationMapGenerationRecoverySystemHelper();
        try
        {
            Assert.IsFalse(recovery.TryScheduleFallback(
                frameCount: 20,
                fallbackEnabled: false,
                primary,
                fallback,
                "disabled"));
            Assert.IsFalse(recovery.TryScheduleFallback(
                frameCount: 20,
                fallbackEnabled: true,
                primary,
                primary,
                "sameRecipe"));
            Assert.IsTrue(recovery.TryScheduleFallback(
                frameCount: 20,
                fallbackEnabled: true,
                primary,
                fallback,
                "missingGround"));
            Assert.IsTrue(recovery.IsFallbackScheduled);
            Assert.AreEqual(1, recovery.FallbackAttemptCount);
            Assert.AreEqual("missingGround", recovery.FailureReason);
            Assert.AreSame(fallback, recovery.FallbackRecipe);
            Assert.IsFalse(recovery.TryActivateFallback(frameCount: 20));
            Assert.IsTrue(recovery.TryActivateFallback(frameCount: 21));
            Assert.IsTrue(recovery.IsFallbackActive);
            Assert.IsFalse(recovery.TryScheduleFallback(
                frameCount: 22,
                fallbackEnabled: true,
                primary,
                fallback,
                "secondFailure"));

            recovery.Reset();
            Assert.IsFalse(recovery.IsFallbackScheduled);
            Assert.IsFalse(recovery.IsFallbackActive);
            Assert.AreEqual(0, recovery.FallbackAttemptCount);
            Assert.IsNull(recovery.FallbackRecipe);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fallback);
            UnityEngine.Object.DestroyImmediate(primary);
        }
    }

    [Test]
    public void GenerationRecovery_TerminalProgressPreservesEvidence()
    {
        var progress = new RuntimeCityGenerationProgress(
            RuntimeCityGenerationStage.Buildings,
            seed: 26071503u,
            requestedCityCount: 2,
            generatedCityCount: 1,
            completedWorkItems: 7,
            totalWorkItems: 22,
            progress01: 0.63f);

        RuntimeCityGenerationProgress cancelled =
            RuntimeOperationMapGenerationRecoverySystemHelper.CreateTerminalProgress(
                progress,
                RuntimeCityGenerationStage.Cancelled);
        RuntimeCityGenerationProgress failed =
            RuntimeOperationMapGenerationRecoverySystemHelper.CreateTerminalProgress(
                progress,
                RuntimeCityGenerationStage.Failed);

        Assert.AreEqual(RuntimeCityGenerationStage.Cancelled, cancelled.Stage);
        Assert.AreEqual(RuntimeCityGenerationStage.Failed, failed.Stage);
        Assert.AreEqual(progress.Seed, cancelled.Seed);
        Assert.AreEqual(progress.RequestedCityCount, cancelled.RequestedCityCount);
        Assert.AreEqual(progress.GeneratedCityCount, cancelled.GeneratedCityCount);
        Assert.AreEqual(progress.CompletedWorkItems, cancelled.CompletedWorkItems);
        Assert.AreEqual(progress.TotalWorkItems, cancelled.TotalWorkItems);
        Assert.AreEqual(progress.Progress01, cancelled.Progress01);
        Assert.AreEqual(progress.Seed, failed.Seed);
        Assert.AreEqual(progress.CompletedWorkItems, failed.CompletedWorkItems);
        Assert.AreEqual(progress.TotalWorkItems, failed.TotalWorkItems);
        Assert.AreEqual(progress.Progress01, failed.Progress01);
        Assert.IsTrue(cancelled.IsTerminal);
        Assert.IsTrue(failed.IsTerminal);
    }

    [Test]
    public void VisualOnlySpawnBridge_SpawnsAndDeletesUsingExistingPresentation()
    {
        GameObject runtimeRoot = new("RuntimeCityVisualOnlyTestRoot");
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = "RuntimeCityVisualOnlyTestPrefab";
        var visualSystem = new RuntimeCityVisualPresentationSystemHelper();
        var spawnBridge = new RuntimeCitySpawnBridgeState();
        try
        {
            visualSystem.SetRuntimeRoot(runtimeRoot.transform);
            spawnBridge.ConfigureVisualOnly(visualSystem, CreateGrid(width: 64, height: 64));

            bool spawned = spawnBridge.TrySpawnCityBuilding(
                prefab,
                new Vector2Int(10, 12),
                out int buildingId,
                out Vector2Int actualOrigin,
                out Vector2Int actualFootprint,
                "Test",
                "Test visual building",
                new Vector2Int(3, 4),
                100);

            Assert.IsTrue(spawned);
            Assert.Less(buildingId, 0);
            Assert.AreEqual(new Vector2Int(10, 12), actualOrigin);
            Assert.AreEqual(new Vector2Int(3, 4), actualFootprint);
            Assert.AreEqual(1, spawnBridge.VisualSpawnCount);
            Assert.IsNotNull(runtimeRoot.transform.Find("RuntimeCityVisuals"));

            Assert.IsTrue(spawnBridge.DeleteCityBuilding(buildingId));
            Assert.AreEqual(0, spawnBridge.VisualSpawnCount);
        }
        finally
        {
            spawnBridge.Clear();
            visualSystem.Dispose();
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void PlanOnlySpawnBridge_TracksPlacementWithoutCreatingVisuals()
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = "RuntimeCityPlanOnlyTestPrefab";
        var spawnBridge = new RuntimeCitySpawnBridgeState();
        try
        {
            spawnBridge.ConfigurePlanOnly();

            bool spawned = spawnBridge.TrySpawnCityBuilding(
                prefab,
                new Vector2Int(7, 9),
                out int buildingId,
                out Vector2Int actualOrigin,
                out Vector2Int actualFootprint,
                "Test",
                "Test planned building",
                new Vector2Int(2, 3),
                100);

            Assert.IsTrue(spawned);
            Assert.Less(buildingId, 0);
            Assert.AreEqual(new Vector2Int(7, 9), actualOrigin);
            Assert.AreEqual(new Vector2Int(2, 3), actualFootprint);
            Assert.AreEqual(1, spawnBridge.PlannedBuildingCount);
            Assert.AreEqual(0, spawnBridge.VisualSpawnCount);

            Assert.IsTrue(spawnBridge.DeleteCityBuilding(buildingId));
            Assert.AreEqual(0, spawnBridge.PlannedBuildingCount);
        }
        finally
        {
            spawnBridge.Clear();
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void VisualOnlySpawnBridge_RotatesAndRecentersRectangularFootprint()
    {
        GameObject runtimeRoot = new("RuntimeCityRoadFacingTestRoot");
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = "RuntimeCityRoadFacingTestPrefab";
        var visualSystem = new RuntimeCityVisualPresentationSystemHelper();
        var spawnBridge = new RuntimeCitySpawnBridgeState();
        try
        {
            GridConfig grid = CreateGrid(width: 64, height: 64);
            visualSystem.SetRuntimeRoot(runtimeRoot.transform);
            spawnBridge.ConfigureVisualOnly(visualSystem, grid);

            bool spawned = spawnBridge.TrySpawnCityBuilding(
                prefab,
                new Vector2Int(10, 12),
                out _,
                out Vector2Int actualOrigin,
                out Vector2Int actualFootprint,
                "Test",
                "Road-facing visual building",
                new Vector2Int(2, 4),
                100,
                Quaternion.Euler(0f, 90f, 0f));

            Assert.IsTrue(spawned);
            Assert.AreEqual(new Vector2Int(9, 13), actualOrigin);
            Assert.AreEqual(new Vector2Int(4, 2), actualFootprint);
            Transform visual = runtimeRoot.transform.Find("RuntimeCityVisuals/RuntimeCityRoadFacingTestPrefab_Visual");
            Assert.IsNotNull(visual);
            Assert.That(visual.forward.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(visual.forward.z, Is.EqualTo(0f).Within(0.001f));
        }
        finally
        {
            spawnBridge.Clear();
            visualSystem.Dispose();
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void VisualOnlyPresentation_GroundsLowestRendererPoint()
    {
        GameObject runtimeRoot = new("RuntimeCityGroundingTestRoot");
        GameObject prefab = new("RuntimeCityBelowGradeTestPrefab");
        GameObject geometry = GameObject.CreatePrimitive(PrimitiveType.Cube);
        geometry.transform.SetParent(prefab.transform, false);
        geometry.transform.localPosition = new Vector3(0f, -1.5f, 0f);
        var visualSystem = new RuntimeCityVisualPresentationSystemHelper();
        try
        {
            visualSystem.SetRuntimeRoot(runtimeRoot.transform);
            GameObject visual = visualSystem.SpawnVisualOnlyPrefab(
                prefab,
                new Vector2Int(10, 12),
                new Vector2Int(3, 4),
                Quaternion.identity,
                CreateGrid(width: 64, height: 64));

            Assert.IsNotNull(visual);
            Renderer renderer = visual.GetComponentInChildren<Renderer>();
            Assert.IsNotNull(renderer);
            Assert.That(renderer.bounds.min.y, Is.EqualTo(0f).Within(0.001f));
        }
        finally
        {
            visualSystem.Dispose();
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void VisualPrototypePrefabSelection_LimitsConsecutiveRepetitionDeterministically()
    {
        GameObject prefabA = new("Repetition_A");
        GameObject prefabB = new("Repetition_B");
        var prefabs = new List<GameObject> { prefabA, prefabB };
        var first = new RuntimeCityPrefabSelectionState();
        var second = new RuntimeCityPrefabSelectionState();
        var firstRandom = new Unity.Mathematics.Random(26071501u);
        var secondRandom = new Unity.Mathematics.Random(26071501u);
        first.ConfigureConsecutiveSelectionLimit(2);
        second.ConfigureConsecutiveSelectionLimit(2);
        try
        {
            GameObject previous = null;
            int consecutive = 0;
            for (int i = 0; i < 64; i++)
            {
                GameObject firstSelection = first.GetRandomPrefab(prefabs, ref firstRandom);
                GameObject secondSelection = second.GetRandomPrefab(prefabs, ref secondRandom);
                Assert.AreSame(firstSelection, secondSelection, $"Selection drifted at index {i}.");

                consecutive = firstSelection == previous ? consecutive + 1 : 1;
                Assert.LessOrEqual(consecutive, 2, $"Prefab repeated more than twice at index {i}.");
                previous = firstSelection;
            }

            Assert.AreEqual(2, first.MaxObservedConsecutiveSelections);
            Assert.AreEqual(first.MaxObservedConsecutiveSelections, second.MaxObservedConsecutiveSelections);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefabA);
            UnityEngine.Object.DestroyImmediate(prefabB);
        }
    }

    [Test]
    public void VisualQualityClearance_PreservesLargeRockWithoutExplicitCleanup()
    {
        GameObject visual = new("VisualQualityClearanceTest");
        GameObject structure = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var quality = new RuntimeOperationMapVisualQualitySystemHelper();
        try
        {
            structure.name = "Test_SM_Bld_Structure_01";
            structure.transform.SetParent(visual.transform, false);
            structure.transform.localScale = new Vector3(4f, 4f, 4f);
            obstruction.name = "Test_SM_Env_Rock_01";
            obstruction.transform.SetParent(visual.transform, false);
            obstruction.transform.localPosition = new Vector3(1f, 0f, 0f);
            obstruction.transform.localScale = new Vector3(8f, 4f, 8f);

            quality.ApplyClearanceRules(
                visual,
                Game.Configs.RuntimeOperationMapVisualStage.DistrictModules,
                default);

            Assert.IsTrue(obstruction.activeSelf);
            Assert.AreEqual(0, quality.SuppressedObstructionCount);
        }
        finally
        {
            quality.Dispose();
            UnityEngine.Object.DestroyImmediate(visual);
        }
    }

    [Test]
    public void VisualQualityClearance_SuppressesLargeRockWithExplicitCleanup()
    {
        GameObject visual = new("VisualQualityExplicitClearanceTest");
        GameObject obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var quality = new RuntimeOperationMapVisualQualitySystemHelper();
        try
        {
            obstruction.name = "Test_SM_Env_Rock_01";
            obstruction.transform.SetParent(visual.transform, false);
            obstruction.transform.localScale = new Vector3(8f, 4f, 8f);
            var cleanup = new Game.Configs.RuntimeOperationMapVisualCleanupSettings(
                Vector2.zero,
                new Vector2(100f, 100f));

            quality.ApplyClearanceRules(
                visual,
                Game.Configs.RuntimeOperationMapVisualStage.DistrictModules,
                cleanup);

            Assert.IsFalse(obstruction.activeSelf);
            Assert.AreEqual(1, quality.SuppressedObstructionCount);
        }
        finally
        {
            quality.Dispose();
            UnityEngine.Object.DestroyImmediate(visual);
        }
    }

    [Test]
    public void VisualQualityClearance_SuppressesSmallDressingOutsideDistrictFootprint()
    {
        GameObject visual = new("VisualQualityOutlierTest");
        GameObject outlier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject structure = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var quality = new RuntimeOperationMapVisualQualitySystemHelper();
        try
        {
            visual.transform.position = new Vector3(10f, 0f, 10f);
            outlier.name = "Test_SM_Prop_Powerpole_01";
            outlier.transform.SetParent(visual.transform, false);
            outlier.transform.localPosition = new Vector3(70f, 0f, 0f);
            outlier.transform.localScale = new Vector3(1f, 8f, 1f);
            structure.name = "Test_SM_Bld_OutsideClip_01";
            structure.transform.SetParent(visual.transform, false);
            structure.transform.localPosition = new Vector3(72f, 0f, 0f);
            structure.transform.localScale = new Vector3(6f, 6f, 6f);

            var cleanup = new Game.Configs.RuntimeOperationMapVisualCleanupSettings(
                new Vector2(10f, 10f),
                new Vector2(100f, 100f));

            quality.ApplyClearanceRules(
                visual,
                Game.Configs.RuntimeOperationMapVisualStage.DistrictModules,
                cleanup);

            Assert.IsFalse(outlier.activeSelf);
            Assert.IsTrue(structure.activeSelf);
            Assert.AreEqual(1, quality.SuppressedObstructionCount);
        }
        finally
        {
            quality.Dispose();
            UnityEngine.Object.DestroyImmediate(visual);
        }
    }

    [Test]
    public void VisualQualityFoundation_BorrowedMaterialSurvivesDisposeAndColliderIsRemoved()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Assert.IsNotNull(shader);
        GameObject root = new("RuntimeVisualQualityFoundationRoot");
        Material borrowedMaterial = new(shader) { name = "BorrowedFoundationMaterial" };
        var quality = new RuntimeOperationMapVisualQualitySystemHelper();
        try
        {
            var settings = new Game.Configs.RuntimeOperationMapFoundationSettings(
                borrowedMaterial,
                new Vector3(10f, -0.2f, 12f),
                new Vector3(24f, 0.4f, 30f),
                Color.gray);

            GameObject foundation = quality.CreateFoundation(
                settings,
                root.transform,
                cloneSourceMaterial: false);

            Assert.IsNotNull(foundation);
            Assert.IsNull(foundation.GetComponent<Collider>());
            Assert.AreSame(borrowedMaterial, foundation.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.AreEqual(1, quality.FoundationVisualCount);

            quality.Dispose();

            Assert.AreEqual(0, quality.FoundationVisualCount);
            Assert.AreEqual(0, quality.SuppressedObstructionCount);
            Assert.IsTrue(borrowedMaterial != null);
        }
        finally
        {
            quality.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(borrowedMaterial);
        }
    }

    [Test]
    public void VisualQualityBoundary_IsGenerationTimeOnlyAndOwnersDisposeIt()
    {
        const string QualityPath = "Assets/Game/Scripts/Environment/RuntimeOperationMapVisualQualitySystemHelper.cs";
        const string CompositionPath = "Assets/Game/Scripts/Environment/RuntimeCityRAndDMapCompositionSystemHelper.cs";
        const string PresentationPath = "Assets/Game/Scripts/Environment/RuntimeOperationMapVisualRecipePresentationSystemHelper.cs";
        string qualitySource = File.ReadAllText(QualityPath);
        string compositionSource = File.ReadAllText(CompositionPath);
        string presentationSource = File.ReadAllText(PresentationPath);

        Assert.AreEqual(198, File.ReadAllLines(QualityPath).Length);
        Assert.AreEqual(8170, new FileInfo(QualityPath).Length);
        Assert.AreEqual(1, CountOccurrences(
            compositionSource,
            "new RuntimeOperationMapVisualQualitySystemHelper()"));
        Assert.AreEqual(1, CountOccurrences(
            presentationSource,
            "new RuntimeOperationMapVisualQualitySystemHelper()"));
        Assert.AreEqual(1, CountOccurrences(compositionSource, "_algorithmicVisualQuality.CreateFoundation("));
        Assert.AreEqual(1, CountOccurrences(presentationSource, "_quality.CreateFoundation("));
        Assert.AreEqual(1, CountOccurrences(compositionSource, "_algorithmicVisualQuality?.Dispose();"));
        Assert.AreEqual(1, CountOccurrences(presentationSource, "_quality?.Dispose();"));
        Assert.AreEqual(2, CountOccurrences(presentationSource, "_quality.ApplyClearanceRules("));
        Assert.That(qualitySource, Does.Not.Contain("Update("));
        Assert.That(qualitySource, Does.Not.Contain("IEnumerator"));
        Assert.That(qualitySource, Does.Not.Contain("StartCoroutine"));
        Assert.That(qualitySource, Does.Not.Contain("World.DefaultGameObjectInjectionWorld"));
        Assert.That(qualitySource, Does.Not.Contain("EntityManager"));
    }

    [Test]
    public void RuntimeCameraPose_PreservesStageAndClampsPresentationValues()
    {
        var pose = new Game.Configs.RuntimeOperationMapCameraPose(
            Game.Configs.RuntimeOperationMapVisualStage.Market,
            new Vector3(-112f, 48f, -68f),
            new Vector3(-48f, 3f, 10f),
            220f,
            -2f);

        Assert.AreEqual(Game.Configs.RuntimeOperationMapVisualStage.Market, pose.Stage);
        Assert.AreEqual(new Vector3(-112f, 48f, -68f), pose.Position);
        Assert.AreEqual(new Vector3(-48f, 3f, 10f), pose.Target);
        Assert.AreEqual(179f, pose.FieldOfView);
        Assert.AreEqual(0f, pose.TransitionSeconds);
        Assert.IsTrue(pose.IsConfigured);

        Assert.AreEqual(
            RuntimeOperationMapVisualStage.TerrainAndRoads,
            RuntimeCityRAndDMapCompositionSystemHelper.GetAlgorithmicVisualStage(RuntimeCityGenerationStage.Roads));
        Assert.AreEqual(
            RuntimeOperationMapVisualStage.Market,
            RuntimeCityRAndDMapCompositionSystemHelper.GetAlgorithmicVisualStage(RuntimeCityGenerationStage.Landmarks));
        Assert.AreEqual(
            RuntimeOperationMapVisualStage.DistrictModules,
            RuntimeCityRAndDMapCompositionSystemHelper.GetAlgorithmicVisualStage(RuntimeCityGenerationStage.Buildings));
        Assert.AreEqual(
            RuntimeOperationMapVisualStage.Compound,
            RuntimeCityRAndDMapCompositionSystemHelper.GetAlgorithmicVisualStage(RuntimeCityGenerationStage.Decorations));
        Assert.AreEqual(
            RuntimeOperationMapVisualStage.Compound,
            RuntimeCityRAndDMapCompositionSystemHelper.GetAlgorithmicVisualStage(RuntimeCityGenerationStage.Finalizing));
        Assert.AreEqual(
            RuntimeOperationMapVisualStage.Horizon,
            RuntimeCityRAndDMapCompositionSystemHelper.GetAlgorithmicVisualStage(RuntimeCityGenerationStage.Completed));
    }

    [Test]
    public void RuntimeDistrictModuleRecipe_SupportsExactPrefabReplayOrIndexedSlices()
    {
        GameObject prefab = new("RuntimeDistrictModuleRecipeTest");
        try
        {
            var cleanup = new Game.Configs.RuntimeOperationMapVisualCleanupSettings(
                new Vector2(10f, 20f),
                new Vector2(80f, 90f));
            var module = new Game.Configs.RuntimeOperationMapDistrictModuleRecipe(
                "TestDistrict",
                prefab,
                new Vector3(1f, 2f, 3f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one,
                true,
                cleanup,
                new List<Game.Configs.RuntimeOperationMapDistrictSliceRecipe>
                {
                    new("Building_A", new[] { 0, 2 }, new Vector3(4f, 5f, 6f), Quaternion.identity, Vector3.one, true),
                    new("Road_A", new[] { 1, 4 }, Vector3.zero, Quaternion.identity, Vector3.one, false)
                });

            Assert.IsTrue(module.IsConfigured);
            Assert.AreEqual("TestDistrict", module.Name);
            Assert.AreSame(prefab, module.Prefab);
            Assert.AreEqual(2, module.Slices.Count);
            Assert.AreEqual("Building_A", module.Slices[0].Name);
            Assert.AreEqual(2, module.Slices[0].SiblingIndices.Count);
            Assert.AreEqual(0, module.Slices[0].SiblingIndices[0]);
            Assert.AreEqual(2, module.Slices[0].SiblingIndices[1]);
            Assert.AreEqual(new Vector3(4f, 5f, 6f), module.Slices[0].Position);
            Assert.IsTrue(module.Slices[0].Active);
            Assert.IsFalse(module.Slices[1].Active);

            var completeModule = new Game.Configs.RuntimeOperationMapDistrictModuleRecipe(
                "ExactDistrict",
                prefab,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one,
                true,
                default,
                new List<Game.Configs.RuntimeOperationMapDistrictSliceRecipe>(),
                realizeAsCompletePrefab: true);
            Assert.IsTrue(completeModule.IsConfigured);
            Assert.IsTrue(completeModule.RealizeCompletePrefab);
            Assert.AreEqual(0, completeModule.Slices.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void RuntimePrototypeArchitecture_UsesPassiveViewAndSystemBaseOwner()
    {
        const string ViewPath = "Assets/Game/Scripts/Environment/RuntimeCityRAndDMapView.cs";
        const string SystemPath = "Assets/Game/Scripts/Environment/RuntimeCityRAndDMapSystem.cs";
        const string CompositionPath = "Assets/Game/Scripts/Environment/RuntimeCityRAndDMapCompositionSystemHelper.cs";
        const string PresentationPath = "Assets/Game/Scripts/Environment/RuntimeOperationMapVisualRecipePresentationSystemHelper.cs";
        const string QualityPath = "Assets/Game/Scripts/Environment/RuntimeOperationMapVisualQualitySystemHelper.cs";
        const string SurfaceGeometryPath = "Assets/Game/Scripts/Environment/RuntimeOperationMapSurfaceGeometrySystemHelper.cs";
        const string RecoveryPath = "Assets/Game/Scripts/Environment/RuntimeOperationMapGenerationRecoverySystemHelper.cs";

        string viewSource = File.ReadAllText(ViewPath);
        string systemSource = File.ReadAllText(SystemPath);
        string compositionSource = File.ReadAllText(CompositionPath);
        string presentationSource = File.ReadAllText(PresentationPath);
        string qualitySource = File.ReadAllText(QualityPath);
        string surfaceGeometrySource = File.ReadAllText(SurfaceGeometryPath);
        string recoverySource = File.ReadAllText(RecoveryPath);

        StringAssert.Contains("RuntimeCityRAndDMapView : MonoBehaviour", viewSource);
        StringAssert.Contains("public Camera PresentationCamera => presentationCamera;", viewSource);
        StringAssert.Contains("public float VisualRecipeFrameBudgetMilliseconds", viewSource);
        StringAssert.Contains("public void RequestCancel()", viewSource);
        StringAssert.Contains("_runtimeSystem?.RequestCancel();", viewSource);
        StringAssert.DoesNotContain("void Update(", viewSource);
        StringAssert.DoesNotContain("void LateUpdate(", viewSource);
        StringAssert.DoesNotContain("void FixedUpdate(", viewSource);
        StringAssert.DoesNotContain("IEnumerator", viewSource);
        StringAssert.DoesNotContain("StartCoroutine", viewSource);
        StringAssert.DoesNotContain("OnGUI", viewSource);
        StringAssert.DoesNotContain("RuntimeCityCompositionSystemHelper", viewSource);

        StringAssert.Contains("RuntimeCityRAndDMapSystem : SystemBase", systemSource);
        StringAssert.Contains("protected override void OnUpdate()", systemSource);
        StringAssert.Contains("_composition.Tick(UnityEngine.Time.frameCount);", systemSource);
        StringAssert.Contains("_composition.AdvancePresentation(UnityEngine.Time.unscaledDeltaTime);", systemSource);
        StringAssert.Contains("if (!presentationChanged)", systemSource);
        StringAssert.Contains("ResetPresentationCache", systemSource);
        StringAssert.Contains("_composition.CancelForExit();", systemSource);
        StringAssert.DoesNotContain("MonoBehaviour", systemSource);
        StringAssert.Contains("public void AdvancePresentation(float unscaledDeltaTime)", compositionSource);
        StringAssert.Contains("GetAlgorithmicStageMinimumDuration", compositionSource);
        StringAssert.Contains("_view.VisualRecipeFrameBudgetMilliseconds", compositionSource);
        StringAssert.Contains("TryActivateFallback", compositionSource);
        StringAssert.Contains("CancelGeneration(\"viewUnbound\"", compositionSource);
        StringAssert.Contains("TryScheduleFallback", recoverySource);
        StringAssert.Contains("CreateTerminalProgress", recoverySource);
        StringAssert.DoesNotContain("MonoBehaviour", compositionSource);

        string retiredRoleSuffix = "Build" + "er";
        StringAssert.DoesNotContain(retiredRoleSuffix, viewSource);
        StringAssert.DoesNotContain(retiredRoleSuffix, systemSource);
        StringAssert.DoesNotContain(retiredRoleSuffix, compositionSource);
        StringAssert.DoesNotContain(retiredRoleSuffix, presentationSource);
        StringAssert.DoesNotContain(retiredRoleSuffix, qualitySource);
        StringAssert.DoesNotContain(retiredRoleSuffix, surfaceGeometrySource);
        StringAssert.DoesNotContain(retiredRoleSuffix, recoverySource);
        StringAssert.DoesNotContain("MonoBehaviour", qualitySource);
        StringAssert.DoesNotContain("MonoBehaviour", surfaceGeometrySource);
        StringAssert.DoesNotContain("MonoBehaviour", recoverySource);
        StringAssert.DoesNotContain(".Select(", recoverySource);
        StringAssert.DoesNotContain(".Where(", recoverySource);
        Assert.IsFalse(File.Exists($"Assets/Game/Scripts/Environment/RuntimeCityRAndDMap{retiredRoleSuffix}.cs"));
        Assert.IsFalse(File.Exists($"Assets/Game/Scripts/Environment/RuntimeOperationMapVisualRecipe{retiredRoleSuffix}.cs"));
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

    private static int CountOccurrences(string source, string value)
    {
        return source.Split(new[] { value }, StringSplitOptions.None).Length - 1;
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
