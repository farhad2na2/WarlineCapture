#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

public sealed class MapSurfaceLayeredGridFocusedTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new MapSurfaceLayeredGridFocusedTests();
            tests.FlatEquivalentSurfaceSamplesPrimaryHeightAndNormal();
            tests.SlopeSamplingAndClassificationUseBakedSampleData();
            tests.BuildingFootprintValidationRejectsHeightDelta();
            tests.LayeredBridgeAndLowerHighwayRemainIndependentlyWalkable();
            tests.LayeredCellsDoNotPermitSurfaceJumpWithoutExplicitConnection();
            tests.RuntimeValidationProbeCoversSlopeTankAndBridgeSeparation();
            tests.PerformanceValidationProbeKeepsSurfaceSamplingAllocationBounded();
            tests.PathingValidationUsesTraversableLayerWhenFirstSampleIsBlocked();
            tests.PathingValidationAllowsRoadsRegardlessOfSlope();
            tests.MapSurfaceBakeUsesGroundHeightWhenBlockerOverlapsTerrain();
            tests.MapSurfaceBakeIgnoresBlockerBuriedBelowGround();
            tests.MapSurfaceBakeKeepsRoadWalkableWhenBlockerMeshOverlaps();
            tests.MapSurfaceBakePrefersRoadHeightOverHigherTerrain();
            tests.MapSurfaceBakeUsesLowestNonRoadGroundWhenAccidentalHigherGroundingMeshOverlaps();
            tests.MovePreviewResolverUsesSelectedVehicleFootprint();
            Debug.Log("[MapSurfaceLayeredGridFocusedValidation] result=Passed tests=15");
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[MapSurfaceLayeredGridFocusedValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void FlatEquivalentSurfaceSamplesPrimaryHeightAndNormal()
    {
        using SurfaceBlobScope scope = CreateSurface(
            new int2(2, 2),
            FlatCells(2, 2),
            new[]
            {
                Sample(new int2(0, 0), 1, 0, 0f),
                Sample(new int2(1, 0), 2, 0, 0f),
                Sample(new int2(0, 1), 3, 0, 0f),
                Sample(new int2(1, 1), 4, 0, 0f)
            },
            Array.Empty<MapSurfaceConnection>());

        var querySystem = new MapSurfaceQuerySystem();
        var context = new MapSurfaceQuerySystem.Context(scope.Surface);

        Assert.IsTrue(querySystem.TrySampleHeight(context, new int2(1, 1), out float height));
        Assert.AreEqual(0f, height);
        Assert.IsTrue(querySystem.TrySampleNormal(context, new int2(1, 1), out float3 normal));
        Assert.That(math.distance(new float3(0f, 1f, 0f), normal), Is.LessThan(0.0001f));
    }

    [Test]
    public void SlopeSamplingAndClassificationUseBakedSampleData()
    {
        MapSurfaceSample slopeSample = Sample(new int2(0, 0), 10, 0, 3f, 12f);
        using SurfaceBlobScope scope = CreateSurface(
            new int2(1, 1),
            FlatCells(1, 1),
            new[] { slopeSample },
            Array.Empty<MapSurfaceConnection>());

        var querySystem = new MapSurfaceQuerySystem();
        var context = new MapSurfaceQuerySystem.Context(scope.Surface);
        var classificationSystem = new MapSurfaceSlopeClassifier();

        Assert.IsTrue(querySystem.TrySampleHeight(context, new int2(0, 0), out float height));
        Assert.AreEqual(3f, height);
        Assert.IsTrue(querySystem.TrySampleSlope(context, new int2(0, 0), out float slope));
        Assert.AreEqual(12f, slope);
        Assert.AreEqual(MapSurfaceSlopeClass.Gentle, classificationSystem.Classify(slopeSample));
    }

    [Test]
    public void BuildingFootprintValidationRejectsHeightDelta()
    {
        using SurfaceBlobScope scope = CreateSurface(
            new int2(2, 1),
            FlatCells(2, 1),
            new[]
            {
                Sample(new int2(0, 0), 1, 0, 0f),
                Sample(new int2(1, 0), 1, 0, 0.5f)
            },
            Array.Empty<MapSurfaceConnection>());

        object placementSystem = Activator.CreateInstance(typeof(BuildingSurfacePlacementSystem), true);
        Type resultType = placementSystem.GetType().GetNestedType("Result", BindingFlags.Public);
        MethodInfo method = placementSystem.GetType().GetMethod(
            "TryEvaluateFootprint",
            new[]
            {
                typeof(MapSurfaceComponent),
                typeof(Vector2Int),
                typeof(Vector2Int),
                typeof(float),
                typeof(float),
                resultType.MakeByRefType()
            });

        object[] args =
        {
            scope.Surface,
            new Vector2Int(0, 0),
            new Vector2Int(2, 1),
            0.2f,
            5f,
            null
        };

        Assert.IsTrue((bool)method.Invoke(placementSystem, args));
        object result = args[5];
        Assert.IsFalse((bool)resultType.GetField("IsValid").GetValue(result));
        Assert.AreEqual(0.5f, (float)resultType.GetField("MaxFootprintHeightDelta").GetValue(result));
    }

    [Test]
    public void LayeredBridgeAndLowerHighwayRemainIndependentlyWalkable()
    {
        MapSurfaceSample bridge = Sample(new int2(0, 0), 100, 1, 6f, surfaceType: MapSurfaceType.BridgeDeck, flags: MapSurfaceFlags.Road | MapSurfaceFlags.Bridge | MapSurfaceFlags.Layered);
        MapSurfaceSample highway = Sample(new int2(0, 0), 101, 0, 0f, surfaceType: MapSurfaceType.Highway, flags: MapSurfaceFlags.Road | MapSurfaceFlags.Highway | MapSurfaceFlags.Layered);
        using SurfaceBlobScope scope = CreateSurface(
            new int2(1, 1),
            new[]
            {
                new MapSurfaceCell { FirstSurfaceIndex = 0, SurfaceCount = 2, InlineSurfaceIndex = 0 }
            },
            new[] { bridge, highway },
            Array.Empty<MapSurfaceConnection>());

        var layeredCellSystem = new MapSurfaceLayeredCellSystem();
        var slopeSystem = new MapSurfaceSlopeClassifier();

        Assert.IsTrue(layeredCellSystem.TryGetSurfaceRange(scope.Surface, new int2(0, 0), out MapSurfaceCellSurfaceRange range));
        Assert.AreEqual(2, range.SurfaceCount);
        Assert.IsTrue(layeredCellSystem.TryGetSurface(scope.Surface, range, 0, out MapSurfaceSample bridgeSample));
        Assert.IsTrue(layeredCellSystem.TryGetSurface(scope.Surface, range, 1, out MapSurfaceSample highwaySample));
        Assert.AreEqual(MapSurfaceType.BridgeDeck, bridgeSample.SurfaceType);
        Assert.AreEqual(MapSurfaceType.Highway, highwaySample.SurfaceType);
        Assert.IsTrue(slopeSystem.AllowsMovement(bridgeSample, MapSurfaceMovementMask.Infantry));
        Assert.IsTrue(slopeSystem.AllowsMovement(highwaySample, MapSurfaceMovementMask.Infantry));
    }

    [Test]
    public void LayeredCellsDoNotPermitSurfaceJumpWithoutExplicitConnection()
    {
        MapSurfaceSample bridge = Sample(new int2(0, 0), 100, 1, 6f, connectionCount: 0, surfaceType: MapSurfaceType.BridgeDeck, flags: MapSurfaceFlags.Road | MapSurfaceFlags.Bridge | MapSurfaceFlags.Layered);
        MapSurfaceSample highway = Sample(new int2(0, 0), 101, 0, 0f, surfaceType: MapSurfaceType.Highway, flags: MapSurfaceFlags.Road | MapSurfaceFlags.Highway | MapSurfaceFlags.Layered);
        using SurfaceBlobScope scope = CreateSurface(
            new int2(1, 1),
            new[]
            {
                new MapSurfaceCell { FirstSurfaceIndex = 0, SurfaceCount = 2, InlineSurfaceIndex = 0 }
            },
            new[] { bridge, highway },
            Array.Empty<MapSurfaceConnection>());

        var connectionSystem = new MapSurfaceConnectionSystem();
        var context = new MapSurfaceConnectionSystem.Context(scope.Surface);

        Assert.IsFalse(connectionSystem.TryFindConnection(
            context,
            bridge,
            highway.SurfaceId,
            int2.zero,
            MapSurfaceMovementMask.Infantry,
            out _));
    }

    [Test]
    public void PathingValidationUsesTraversableLayerWhenFirstSampleIsBlocked()
    {
        MapSurfaceSample blocker = Sample(new int2(0, 0), 100, 1, 2f, surfaceType: MapSurfaceType.Blocked);
        MapSurfaceSample ground = Sample(new int2(0, 0), 101, 0, 0f, surfaceType: MapSurfaceType.Terrain);
        using SurfaceBlobScope scope = CreateSurface(
            new int2(1, 1),
            new[]
            {
                new MapSurfaceCell { FirstSurfaceIndex = 0, SurfaceCount = 2, InlineSurfaceIndex = 0 }
            },
            new[] { blocker, ground },
            Array.Empty<MapSurfaceConnection>());

        var validationSystem = new MapSurfacePathingValidationSystem();
        var grid = new GridConfig
        {
            Width = 1,
            Height = 1,
            CellSize = 1f,
            Origin = float3.zero
        };

        Assert.IsTrue(
            validationSystem.CanTraverse(scope.Surface, scope.Surface.HasSurfaceData, int2.zero, MapSurfaceMovementMask.TrackedVehicle),
            "Pathing must use the traversable ground layer when a blocked mesh sample is stored first.");
        Assert.IsTrue(
            validationSystem.CanTraverseFootprint(scope.Surface, scope.Surface.HasSurfaceData, grid, int2.zero, new int2(1, 1), true),
            "Vehicle footprint validation must agree with single-cell pathing over layered blocked/ground cells.");
    }

    [Test]
    public void PathingValidationAllowsRoadsRegardlessOfSlope()
    {
        int width = 3;
        int height = 3;
        MapSurfaceCell[] cells = FlatCells(width, height);
        var samples = new MapSurfaceSample[width * height];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Sample(
                new int2(i % width, i / width),
                i + 1,
                0,
                1.36f,
                slopeDegrees: 24.88f,
                surfaceType: MapSurfaceType.Road,
                flags: MapSurfaceFlags.Road,
                movementMask: MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.AirGrounded);
        }

        using SurfaceBlobScope roadScope = CreateSurface(
            new int2(width, height),
            cells,
            samples,
            Array.Empty<MapSurfaceConnection>());

        var validationSystem = new MapSurfacePathingValidationSystem();
        var grid = new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = float3.zero
        };

        Assert.IsTrue(
            validationSystem.CanTraverse(roadScope.Surface, roadScope.Surface.HasSurfaceData, new int2(1, 1), MapSurfaceMovementMask.TrackedVehicle),
            "Road cells must stay vehicle-walkable even when their baked slope exceeds the normal vehicle slope limit.");
        Assert.IsTrue(
            validationSystem.CanTraverseFootprint(roadScope.Surface, roadScope.Surface.HasSurfaceData, grid, new int2(1, 1), new int2(3, 3), true),
            "A full tank footprint on road cells must not fail only because the road surface is sloped.");

        MapSurfaceSample steepTerrain = Sample(
            int2.zero,
            100,
            0,
            1.36f,
            slopeDegrees: 24.88f,
            surfaceType: MapSurfaceType.Terrain,
            movementMask: MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.AirGrounded);
        using SurfaceBlobScope terrainScope = CreateSurface(
            new int2(1, 1),
            FlatCells(1, 1),
            new[] { steepTerrain },
            Array.Empty<MapSurfaceConnection>());

        Assert.IsFalse(
            validationSystem.CanTraverse(terrainScope.Surface, terrainScope.Surface.HasSurfaceData, int2.zero, MapSurfaceMovementMask.TrackedVehicle),
            "Non-road terrain should still enforce the normal vehicle slope limit.");
    }

    [Test]
    public void RuntimeValidationProbeCoversSlopeTankAndBridgeSeparation()
    {
        MapSurfaceSample slope = Sample(
            new int2(0, 0),
            10,
            0,
            2f,
            slopeDegrees: 12f,
            normal: math.normalize(new float3(0f, 0.96f, 0.28f)));
        MapSurfaceSample bridge = Sample(new int2(1, 0), 100, 1, 6f, surfaceType: MapSurfaceType.BridgeDeck, flags: MapSurfaceFlags.Road | MapSurfaceFlags.Bridge | MapSurfaceFlags.Layered);
        MapSurfaceSample highway = Sample(new int2(1, 0), 101, 0, 0f, surfaceType: MapSurfaceType.Highway, flags: MapSurfaceFlags.Road | MapSurfaceFlags.Highway | MapSurfaceFlags.Layered);
        using SurfaceBlobScope scope = CreateSurface(
            new int2(2, 1),
            new[]
            {
                new MapSurfaceCell { FirstSurfaceIndex = 0, SurfaceCount = 1, InlineSurfaceIndex = 0 },
                new MapSurfaceCell { FirstSurfaceIndex = 1, SurfaceCount = 2, InlineSurfaceIndex = 1 }
            },
            new[] { slope, bridge, highway },
            Array.Empty<MapSurfaceConnection>());

        var probe = new RuntimeValidationProbe();

        Assert.IsTrue(probe.RunProbe(scope.Surface, new int2(0, 0), new int2(1, 0), out RuntimeValidationProbe.Result result));
        Assert.IsTrue(result.UnitMoveOverSlopeGrounded);
        Assert.IsTrue(result.TankVisualPitchRollResolved);
        Assert.IsTrue(result.BridgeAndHighwaySeparated);
        Assert.AreEqual(2f, result.SlopeHeight);
        Assert.AreEqual(100, result.BridgeSurfaceId);
        Assert.AreEqual(101, result.HighwaySurfaceId);
    }

    [Test]
    public void PerformanceValidationProbeKeepsSurfaceSamplingAllocationBounded()
    {
        int width = 8;
        int height = 8;
        MapSurfaceCell[] cells = FlatCells(width, height);
        var samples = new MapSurfaceSample[width * height];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = Sample(new int2(i % width, i / width), i + 1, 0, i % 3);

        using SurfaceBlobScope scope = CreateSurface(
            new int2(width, height),
            cells,
            samples,
            Array.Empty<MapSurfaceConnection>());

        var performanceProbe = new PerformanceValidationProbe();
        PerformanceValidationProbe.Result result = performanceProbe.RunSamplingProbe(scope.Surface, 256);

        Assert.AreEqual(256, result.SampleIterations);
        Assert.AreEqual(256, result.HeightSamples);
        Assert.AreEqual(256, result.NormalSamples);
        Assert.AreEqual(256, result.PathingChecks);
        Assert.Greater(result.EstimatedSurfaceBytes, 0);
        Assert.IsTrue(result.StayedWithinFrameBudget);
        Assert.LessOrEqual(result.AllocatedBytes, PerformanceValidationProbe.MaxSamplingAllocationBytes);
    }

    [Test]
    public void MapSurfaceBakeUsesGroundHeightWhenBlockerOverlapsTerrain()
    {
        Mesh terrain = CreatePlaneMesh(0f);
        Mesh blocker = CreatePlaneMesh(3f);
        BlobAssetReference<MapSurfaceBlob> blob = default;
        try
        {
            var bakeSystem = new MapSurfaceBakeSystem();
            bool baked = bakeSystem.TryBuildSingleLayerTerrain(
                new MapSurfaceBakeRequest(float3.zero, 1f, new int2(1, 1)),
                new[]
                {
                    new MapSurfaceMeshBakeSource(
                        terrain,
                        Matrix4x4.identity,
                        MapSurfaceType.Terrain,
                        MapSurfaceFlags.None,
                        MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.BuildingPlacement,
                        0),
                    new MapSurfaceMeshBakeSource(
                        blocker,
                        Matrix4x4.identity,
                        MapSurfaceType.Blocked,
                        MapSurfaceFlags.None,
                        MapSurfaceMovementMask.None,
                        0)
                },
                Allocator.Persistent,
                out blob);

            Assert.IsTrue(baked);
            ref MapSurfaceBlob surface = ref blob.Value;
            MapSurfaceSample sample = surface.Samples[0];
            Assert.AreEqual(0f, sample.Height, 0.0001f);
            Assert.AreEqual(MapSurfaceType.Blocked, sample.SurfaceType);
            Assert.AreEqual(MapSurfaceMovementMask.None, sample.MovementMask);
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
            UnityEngine.Object.DestroyImmediate(terrain);
            UnityEngine.Object.DestroyImmediate(blocker);
        }
    }

    [Test]
    public void MapSurfaceBakeIgnoresBlockerBuriedBelowGround()
    {
        Mesh terrain = CreatePlaneMesh(0f);
        Mesh blocker = CreatePlaneMesh(-40f);
        BlobAssetReference<MapSurfaceBlob> blob = default;
        try
        {
            var bakeSystem = new MapSurfaceBakeSystem();
            bool baked = bakeSystem.TryBuildSingleLayerTerrain(
                new MapSurfaceBakeRequest(float3.zero, 1f, new int2(1, 1)),
                new[]
                {
                    new MapSurfaceMeshBakeSource(
                        terrain,
                        Matrix4x4.identity,
                        MapSurfaceType.Terrain,
                        MapSurfaceFlags.None,
                        MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.BuildingPlacement,
                        0),
                    new MapSurfaceMeshBakeSource(
                        blocker,
                        Matrix4x4.identity,
                        MapSurfaceType.Blocked,
                        MapSurfaceFlags.None,
                        MapSurfaceMovementMask.None,
                        0)
                },
                Allocator.Persistent,
                out blob);

            Assert.IsTrue(baked);
            ref MapSurfaceBlob surface = ref blob.Value;
            MapSurfaceSample sample = surface.Samples[0];
            Assert.AreEqual(0f, sample.Height, 0.0001f);
            Assert.AreEqual(MapSurfaceType.Terrain, sample.SurfaceType);
            Assert.IsTrue((sample.MovementMask & MapSurfaceMovementMask.TrackedVehicle) != 0);
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
            UnityEngine.Object.DestroyImmediate(terrain);
            UnityEngine.Object.DestroyImmediate(blocker);
        }
    }

    [Test]
    public void MapSurfaceBakeKeepsRoadWalkableWhenBlockerMeshOverlaps()
    {
        Mesh road = CreatePlaneMesh(0f);
        Mesh blocker = CreatePlaneMesh(3f);
        BlobAssetReference<MapSurfaceBlob> blob = default;
        try
        {
            var bakeSystem = new MapSurfaceBakeSystem();
            bool baked = bakeSystem.TryBuildSingleLayerTerrain(
                new MapSurfaceBakeRequest(float3.zero, 1f, new int2(1, 1)),
                new[]
                {
                    new MapSurfaceMeshBakeSource(
                        road,
                        Matrix4x4.identity,
                        MapSurfaceType.Road,
                        MapSurfaceFlags.Road,
                        MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.AirGrounded,
                        0),
                    new MapSurfaceMeshBakeSource(
                        blocker,
                        Matrix4x4.identity,
                        MapSurfaceType.Blocked,
                        MapSurfaceFlags.None,
                        MapSurfaceMovementMask.None,
                        0)
                },
                Allocator.Persistent,
                out blob);

            Assert.IsTrue(baked);
            ref MapSurfaceBlob surface = ref blob.Value;
            MapSurfaceSample sample = surface.Samples[0];
            Assert.AreEqual(0f, sample.Height, 0.0001f);
            Assert.AreEqual(MapSurfaceType.Road, sample.SurfaceType);
            Assert.IsTrue((sample.MovementMask & MapSurfaceMovementMask.Infantry) != 0);
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
            UnityEngine.Object.DestroyImmediate(road);
            UnityEngine.Object.DestroyImmediate(blocker);
        }
    }

    [Test]
    public void MapSurfaceBakePrefersRoadHeightOverHigherTerrain()
    {
        Mesh road = CreatePlaneMesh(0f);
        Mesh terrain = CreatePlaneMesh(3f);
        BlobAssetReference<MapSurfaceBlob> blob = default;
        try
        {
            var bakeSystem = new MapSurfaceBakeSystem();
            bool baked = bakeSystem.TryBuildSingleLayerTerrain(
                new MapSurfaceBakeRequest(float3.zero, 1f, new int2(1, 1)),
                new[]
                {
                    new MapSurfaceMeshBakeSource(
                        terrain,
                        Matrix4x4.identity,
                        MapSurfaceType.Terrain,
                        MapSurfaceFlags.None,
                        MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.BuildingPlacement,
                        0),
                    new MapSurfaceMeshBakeSource(
                        road,
                        Matrix4x4.identity,
                        MapSurfaceType.Road,
                        MapSurfaceFlags.Road,
                        MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.AirGrounded,
                        0)
                },
                Allocator.Persistent,
                out blob);

            Assert.IsTrue(baked);
            ref MapSurfaceBlob surface = ref blob.Value;
            MapSurfaceSample sample = surface.Samples[0];
            Assert.AreEqual(0f, sample.Height, 0.0001f);
            Assert.AreEqual(MapSurfaceType.Road, sample.SurfaceType);
            Assert.IsTrue((sample.MovementMask & MapSurfaceMovementMask.Infantry) != 0);
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
            UnityEngine.Object.DestroyImmediate(road);
            UnityEngine.Object.DestroyImmediate(terrain);
        }
    }

    [Test]
    public void MapSurfaceBakeUsesLowestNonRoadGroundWhenAccidentalHigherGroundingMeshOverlaps()
    {
        Mesh ground = CreatePlaneMesh(0f);
        Mesh propLikeGroundingMesh = CreatePlaneMesh(0.8f);
        BlobAssetReference<MapSurfaceBlob> blob = default;
        try
        {
            var bakeSystem = new MapSurfaceBakeSystem();
            bool baked = bakeSystem.TryBuildSingleLayerTerrain(
                new MapSurfaceBakeRequest(float3.zero, 1f, new int2(1, 1)),
                new[]
                {
                    new MapSurfaceMeshBakeSource(
                        ground,
                        Matrix4x4.identity,
                        MapSurfaceType.Terrain,
                        MapSurfaceFlags.None,
                        MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.BuildingPlacement,
                        0),
                    new MapSurfaceMeshBakeSource(
                        propLikeGroundingMesh,
                        Matrix4x4.identity,
                        MapSurfaceType.Terrain,
                        MapSurfaceFlags.None,
                        MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.BuildingPlacement,
                        0)
                },
                Allocator.Persistent,
                out blob);

            Assert.IsTrue(baked);
            ref MapSurfaceBlob surface = ref blob.Value;
            MapSurfaceSample sample = surface.Samples[0];
            Assert.AreEqual(0f, sample.Height, 0.0001f);
            Assert.AreEqual(MapSurfaceType.Terrain, sample.SurfaceType);
            Assert.IsTrue((sample.MovementMask & MapSurfaceMovementMask.Infantry) != 0);
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
            UnityEngine.Object.DestroyImmediate(ground);
            UnityEngine.Object.DestroyImmediate(propLikeGroundingMesh);
        }
    }

    [Test]
    public void MovePreviewResolverUsesSelectedVehicleFootprint()
    {
        const int width = 8;
        const int height = 8;
        int2 desiredGoal = new(3, 3);
        GridConfig grid = new()
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = float3.zero
        };

        MapSurfaceCell[] cells = FlatCells(width, height);
        var samples = new MapSurfaceSample[width * height];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = Sample(new int2(i % width, i / width), i + 1, 0, 0f);

        samples[GridUtils.CellToIndex(desiredGoal, width)] = Sample(
            desiredGoal,
            100,
            0,
            0f,
            surfaceType: MapSurfaceType.Blocked,
            movementMask: MapSurfaceMovementMask.None);

        using SurfaceBlobScope scope = CreateSurface(
            new int2(width, height),
            cells,
            samples,
            Array.Empty<MapSurfaceConnection>());

        World world = new("MovePreviewResolverUsesSelectedVehicleFootprint");
        NativeArray<int> blockerCounts = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreatePreviewGrid(
                em,
                grid,
                out blockerCounts,
                out friendlyPassFactionIds,
                out blocked,
                out occupied);

            Entity surfaceEntity = em.CreateEntity(typeof(MapSurfaceComponent));
            em.SetComponentData(surfaceEntity, scope.Surface);
            using EntityQuery surfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());

            Entity vehicle = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitMove),
                typeof(UnitFootprint),
                typeof(UnitMovementBehavior),
                typeof(SelectedUnitTag));
            em.SetComponentData(vehicle, new Faction { Id = 1 });
            em.SetComponentData(vehicle, new UnitGrid { Cell = new int2(1, 1) });
            em.SetComponentData(vehicle, new UnitMove { Speed = 1f });
            em.SetComponentData(vehicle, new UnitFootprint { Size = new int2(3, 3) });
            em.SetComponentData(vehicle, new UnitMovementBehavior { UsesVehicleMotion = 1 });

            SelectionStateSystem selectionState = new();
            selectionState.SetFocusedUnit(vehicle);
            selectionState.CacheSelectedMoveEntity(em, vehicle);

            var pointerTargetCommandSystem = new RtsSelectionPointerTargetCommandSystem();
            Assert.IsTrue(pointerTargetCommandSystem.TryResolveSelectedMoveFootprintTarget(
                em,
                surfaceQuery,
                gridEntity,
                grid,
                selectionState,
                desiredGoal,
                out int2 resolvedCell,
                out RtsSelectionPointerTargetCommandSystem.MapSurfaceCommandTargetResult result));

            var validationSystem = new MapSurfacePathingValidationSystem();
            Assert.AreNotEqual(desiredGoal, resolvedCell);
            Assert.AreEqual(resolvedCell, result.Cell);
            Assert.IsFalse(validationSystem.CanTraverseFootprint(scope.Surface, 1, grid, desiredGoal, new int2(3, 3), true));
            Assert.IsTrue(validationSystem.CanTraverseFootprint(scope.Surface, 1, grid, resolvedCell, new int2(3, 3), true));
        }
        finally
        {
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
            world.Dispose();
        }
    }


    private static MapSurfaceCell[] FlatCells(int width, int height)
    {
        var cells = new MapSurfaceCell[width * height];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = new MapSurfaceCell { FirstSurfaceIndex = i, SurfaceCount = 1, InlineSurfaceIndex = (ushort)i };
        return cells;
    }

    private static MapSurfaceSample Sample(
        int2 cell,
        int surfaceId,
        int layerId,
        float height,
        float slopeDegrees = 0f,
        ushort connectionCount = 0,
        MapSurfaceType surfaceType = MapSurfaceType.Terrain,
        MapSurfaceFlags flags = MapSurfaceFlags.None,
        MapSurfaceMovementMask movementMask = MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.BuildingPlacement,
        float3? normal = null)
    {
        return new MapSurfaceSample
        {
            Cell = cell,
            SurfaceId = surfaceId,
            LayerId = layerId,
            Height = height,
            Normal = normal ?? new float3(0f, 1f, 0f),
            SlopeDegrees = slopeDegrees,
            SurfaceType = surfaceType,
            MovementMask = movementMask,
            Flags = flags,
            FirstConnectionIndex = 0,
            ConnectionCount = connectionCount
        };
    }

    private static Entity CreatePreviewGrid(
        EntityManager em,
        GridConfig grid,
        out NativeArray<int> blockerCounts,
        out NativeArray<byte> friendlyPassFactionIds,
        out NativeBitArray blocked,
        out NativeBitArray occupied)
    {
        int gridSize = grid.Width * grid.Height;
        blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        for (int i = 0; i < friendlyPassFactionIds.Length; i++)
            friendlyPassFactionIds[i] = byte.MaxValue;

        Entity gridEntity = em.CreateEntity(
            typeof(GridConfig),
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(GridWalkable));
        em.SetComponentData(gridEntity, grid);
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = blockerCounts,
            Blocked = blocked,
            FriendlyPassFactionIds = friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = occupied
        });

        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
            walkable[i] = new GridWalkable { Value = 1 };

        return gridEntity;
    }

    private static SurfaceBlobScope CreateSurface(
        int2 dimensions,
        MapSurfaceCell[] cells,
        MapSurfaceSample[] samples,
        MapSurfaceConnection[] connections)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.GridOrigin = float3.zero;
        root.CellSize = 1f;
        root.Dimensions = dimensions;

        BlobBuilderArray<MapSurfaceCell> cellArray = builder.Allocate(ref root.Cells, cells.Length);
        for (int i = 0; i < cells.Length; i++)
            cellArray[i] = cells[i];

        BlobBuilderArray<MapSurfaceSample> sampleArray = builder.Allocate(ref root.Samples, samples.Length);
        for (int i = 0; i < samples.Length; i++)
            sampleArray[i] = samples[i];

        BlobBuilderArray<MapSurfaceConnection> connectionArray = builder.Allocate(ref root.Connections, connections.Length);
        for (int i = 0; i < connections.Length; i++)
            connectionArray[i] = connections[i];

        BlobAssetReference<MapSurfaceBlob> blob = builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
        builder.Dispose();
        return new SurfaceBlobScope(blob, dimensions);
    }

    private static Mesh CreatePlaneMesh(float y)
    {
        return new Mesh
        {
            vertices = new[]
            {
                new Vector3(0f, y, 0f),
                new Vector3(1f, y, 0f),
                new Vector3(0f, y, 1f),
                new Vector3(1f, y, 1f)
            },
            triangles = new[] { 0, 2, 1, 1, 2, 3 }
        };
    }

    private sealed class RuntimeValidationProbe
    {
        private const float MaxVehiclePitchRollDegrees = 20f;
        private readonly MapSurfaceQuerySystem _querySystem = new();
        private readonly MapSurfaceLayeredCellSystem _layeredCellSystem = new();
        private readonly MapSurfaceConnectionSystem _connectionSystem = new();
        private readonly MapSurfaceSlopeClassifier _slopeClassificationSystem = new();

        public readonly struct Result
        {
            public readonly bool UnitMoveOverSlopeGrounded;
            public readonly bool TankVisualPitchRollResolved;
            public readonly bool BridgeAndHighwaySeparated;
            public readonly float SlopeHeight;
            public readonly float TankPitchDegrees;
            public readonly int BridgeSurfaceId;
            public readonly int HighwaySurfaceId;

            public Result(
                bool unitMoveOverSlopeGrounded,
                bool tankVisualPitchRollResolved,
                bool bridgeAndHighwaySeparated,
                float slopeHeight,
                float tankPitchDegrees,
                int bridgeSurfaceId,
                int highwaySurfaceId)
            {
                UnitMoveOverSlopeGrounded = unitMoveOverSlopeGrounded;
                TankVisualPitchRollResolved = tankVisualPitchRollResolved;
                BridgeAndHighwaySeparated = bridgeAndHighwaySeparated;
                SlopeHeight = slopeHeight;
                TankPitchDegrees = tankPitchDegrees;
                BridgeSurfaceId = bridgeSurfaceId;
                HighwaySurfaceId = highwaySurfaceId;
            }
        }

        public bool RunProbe(MapSurfaceComponent surface, int2 slopeCell, int2 layeredBridgeCell, out Result result)
        {
            result = default;
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
                return false;

            MapSurfaceQuerySystem.Context queryContext = new(surface);
            bool slopeResolved = _querySystem.TryGetPrimarySurface(queryContext, slopeCell, out MapSurfaceSample slopeSample);
            float slopeHeight = 0f;
            bool unitGrounded = slopeResolved &&
                _querySystem.TrySampleHeight(queryContext, slopeCell, out slopeHeight) &&
                _slopeClassificationSystem.AllowsMovement(slopeSample, MapSurfaceMovementMask.Infantry);
            float tankPitch = slopeResolved ? ResolveVehiclePitchDegrees(slopeSample.Normal) : 0f;
            bool tankAligned = math.abs(tankPitch) > 0.1f;
            bool separated = TryProbeBridgeHighwaySeparation(surface, layeredBridgeCell, out int bridgeSurfaceId, out int highwaySurfaceId);

            result = new Result(
                unitGrounded,
                tankAligned,
                separated,
                slopeResolved ? slopeHeight : 0f,
                tankPitch,
                bridgeSurfaceId,
                highwaySurfaceId);
            return unitGrounded && tankAligned && separated;
        }

        private bool TryProbeBridgeHighwaySeparation(
            MapSurfaceComponent surface,
            int2 layeredBridgeCell,
            out int bridgeSurfaceId,
            out int highwaySurfaceId)
        {
            bridgeSurfaceId = -1;
            highwaySurfaceId = -1;
            if (!_layeredCellSystem.TryGetSurfaceRange(surface, layeredBridgeCell, out MapSurfaceCellSurfaceRange range) ||
                range.SurfaceCount < 2)
            {
                return false;
            }

            MapSurfaceSample bridge = default;
            MapSurfaceSample highway = default;
            bool hasBridge = false;
            bool hasHighway = false;
            for (int i = 0; i < range.SurfaceCount; i++)
            {
                if (!_layeredCellSystem.TryGetSurface(surface, range, i, out MapSurfaceSample sample))
                    continue;

                if (sample.SurfaceType == MapSurfaceType.BridgeDeck)
                {
                    bridge = sample;
                    bridgeSurfaceId = sample.SurfaceId;
                    hasBridge = true;
                }
                else if (sample.SurfaceType == MapSurfaceType.Highway)
                {
                    highway = sample;
                    highwaySurfaceId = sample.SurfaceId;
                    hasHighway = true;
                }
            }

            if (!hasBridge || !hasHighway)
                return false;

            MapSurfaceConnectionSystem.Context context = new(surface);
            return !_connectionSystem.TryFindConnection(
                context,
                bridge,
                highway.SurfaceId,
                int2.zero,
                MapSurfaceMovementMask.Infantry,
                out _);
        }

        private static float ResolveVehiclePitchDegrees(float3 normal)
        {
            float3 resolvedNormal = math.normalizesafe(normal, math.up());
            return math.clamp(
                math.degrees(math.atan2(resolvedNormal.z, resolvedNormal.y)),
                -MaxVehiclePitchRollDegrees,
                MaxVehiclePitchRollDegrees);
        }
    }

    private sealed class PerformanceValidationProbe
    {
        public const double BaselineFrameBudgetMilliseconds = 16.67d;
        public const long MaxSamplingAllocationBytes = 128;

        private readonly MapSurfaceQuerySystem _querySystem = new();
        private readonly MapSurfacePathingValidationSystem _pathingValidationSystem = new();

        public readonly struct Result
        {
            public readonly int SampleIterations;
            public readonly int HeightSamples;
            public readonly int NormalSamples;
            public readonly int PathingChecks;
            public readonly long AllocatedBytes;
            public readonly long ElapsedTicks;
            public readonly int EstimatedSurfaceBytes;
            public readonly bool StayedWithinFrameBudget;
            public readonly bool StayedWithinAllocationBudget;

            public Result(
                int sampleIterations,
                int heightSamples,
                int normalSamples,
                int pathingChecks,
                long allocatedBytes,
                long elapsedTicks,
                int estimatedSurfaceBytes,
                bool stayedWithinFrameBudget,
                bool stayedWithinAllocationBudget)
            {
                SampleIterations = sampleIterations;
                HeightSamples = heightSamples;
                NormalSamples = normalSamples;
                PathingChecks = pathingChecks;
                AllocatedBytes = allocatedBytes;
                ElapsedTicks = elapsedTicks;
                EstimatedSurfaceBytes = estimatedSurfaceBytes;
                StayedWithinFrameBudget = stayedWithinFrameBudget;
                StayedWithinAllocationBudget = stayedWithinAllocationBudget;
            }
        }

        public Result RunSamplingProbe(MapSurfaceComponent surface, int sampleIterations)
        {
            int iterations = math.max(1, sampleIterations);
            MapSurfaceQuerySystem.Context context = new(surface);
            RunWarmup(surface, context);

            var stopwatch = new Stopwatch();
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            stopwatch.Start();

            int heightSamples = 0;
            int normalSamples = 0;
            int pathingChecks = 0;
            int2 dimensions = math.max(surface.Dimensions, new int2(1, 1));
            for (int i = 0; i < iterations; i++)
            {
                int2 cell = new(i % dimensions.x, (i / dimensions.x) % dimensions.y);
                if (_querySystem.TrySampleHeight(context, cell, out _))
                    heightSamples++;
                if (_querySystem.TrySampleNormal(context, cell, out _))
                    normalSamples++;
                if (_pathingValidationSystem.CanTraverse(surface, surface.HasSurfaceData, cell, MapSurfaceMovementMask.Infantry))
                    pathingChecks++;
            }

            stopwatch.Stop();
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            double elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            bool stayedWithinFrameBudget = elapsedMilliseconds <= BaselineFrameBudgetMilliseconds;
            bool stayedWithinAllocationBudget = allocatedBytes <= MaxSamplingAllocationBytes;
            return new Result(
                iterations,
                heightSamples,
                normalSamples,
                pathingChecks,
                allocatedBytes,
                stopwatch.ElapsedTicks,
                EstimateSurfaceMemoryBytes(surface),
                stayedWithinFrameBudget,
                stayedWithinAllocationBudget);
        }

        public int EstimateSurfaceMemoryBytes(MapSurfaceComponent surface)
        {
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
                return 0;

            ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
            const int estimatedCellBytes = 8;
            const int estimatedSampleBytes = 64;
            const int estimatedConnectionBytes = 24;
            return blob.Cells.Length * estimatedCellBytes +
                   blob.Samples.Length * estimatedSampleBytes +
                   blob.Connections.Length * estimatedConnectionBytes;
        }

        private void RunWarmup(MapSurfaceComponent surface, MapSurfaceQuerySystem.Context context)
        {
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
                return;

            int2 cell = int2.zero;
            _querySystem.TrySampleHeight(context, cell, out _);
            _querySystem.TrySampleNormal(context, cell, out _);
            _pathingValidationSystem.CanTraverse(surface, surface.HasSurfaceData, cell, MapSurfaceMovementMask.Infantry);
        }
    }

    private readonly struct SurfaceBlobScope : IDisposable
    {
        private readonly BlobAssetReference<MapSurfaceBlob> _blob;
        public readonly MapSurfaceComponent Surface;

        public SurfaceBlobScope(BlobAssetReference<MapSurfaceBlob> blob, int2 dimensions)
        {
            _blob = blob;
            Surface = new MapSurfaceComponent
            {
                SurfaceBlob = blob,
                GridOrigin = float3.zero,
                CellSize = 1f,
                Dimensions = dimensions,
                HasSurfaceData = 1,
                HasLayeredCells = (byte)(dimensions.x == 1 && dimensions.y == 1 ? 1 : 0),
                HasRoadSurfaces = 1,
                HasBridgeSurfaces = 1
            };
        }

        public void Dispose()
        {
            if (_blob.IsCreated)
                _blob.Dispose();
        }
    }
}
#endif
