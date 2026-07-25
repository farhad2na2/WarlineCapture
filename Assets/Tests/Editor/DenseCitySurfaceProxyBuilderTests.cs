using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCitySurfaceProxyBuilderTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCitySurfaceProxyBuilderTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
    private const string OperationMapId = "opmap.skirmish.desert_base_01";
    private const string OutputRoot = TempRoot + "/Primary/" + OperationMapId + "/Candidate/" + Hash;
    private const string OutputFolder = OutputRoot + "/SurfaceProxies";
    private const string SourceGuid = "0123456789abcdef0123456789abcdef";
    private static readonly Rect MapSurfaceBounds = new(-10f, -10f, 100f, 100f);
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static void RunFocusedValidation()
    {
        var suite = new DenseCitySurfaceProxyBuilderTests();
        Action[] tests =
        {
            suite.Build_PartitionsRecordsAndCreatesBakeOnlyMeshes,
            suite.Build_ConsumesTerrainFoundationAndParkFactoryRecords,
            suite.Build_SeparatesRoadFromShoulderAndNaturalGroundFactoryRecords,
            suite.Build_SeparatesBridgeDeckFromApproachRampFactoryRecords,
            suite.Build_ConvertsCanalWaterExclusionWithoutPhysicsComponents,
            suite.Build_ConsumesBuildingWallAndRockBlockerFactoryRecords,
            suite.Build_ProducesRuntimeSurfaceQueriesForRepresentativeMovers,
            suite.Build_MergesOnlyAdjacentCoplanarRectangles,
            suite.Build_IsDeterministicAcrossEquivalentRecordSets,
            suite.Build_OutsideGridPresentationRemainsPresentationOnly,
            suite.Build_OutOfBoundsPolygonLeavesNoCandidateOutput,
            suite.Build_ProhibitedProxyComponentLeavesNoCandidateOutput,
            suite.Build_InvalidConcavePolygonLeavesNoCandidateOutput
        };

        for (int index = 0; index < tests.Length; index++)
        {
            suite.SetUp();
            try
            {
                tests[index]();
            }
            finally
            {
                suite.TearDown();
            }
        }

        Debug.Log($"[DenseCitySurfaceProxyValidation] result=Passed tests={tests.Length}");
    }

    [Test]
    public void Build_ProducesRuntimeSurfaceQueriesForRepresentativeMovers()
    {
        var (_, _, mapRoot) = CreateScenePair("runtime-query");
        using var records = new DenseCityGenerationRecordSet(1, 5, 1);
        uint terrainMask = (uint)(MapSurfaceMovementMask.AllGroundUnits |
            MapSurfaceMovementMask.AirGrounded |
            MapSurfaceMovementMask.BuildingPlacement);
        uint roadMask = (uint)(MapSurfaceMovementMask.AllGroundUnits |
            MapSurfaceMovementMask.AirGrounded);
        uint groundUnitMask = (uint)MapSurfaceMovementMask.AllGroundUnits;
        records.Add(CreateSurface(1, DenseCitySurfaceRecordKind.Terrain, CellRectangle(0), terrainMask, 0, Vector2Int.zero, 0f));
        records.Add(CreateSurface(2, DenseCitySurfaceRecordKind.Road, CellRectangle(1), roadMask, 0, Vector2Int.zero, 0f));
        records.Add(CreateSurface(3, DenseCitySurfaceRecordKind.Bridge, CellRectangle(2), groundUnitMask, 1, Vector2Int.zero, 0f));
        records.Add(CreateSurface(4, DenseCitySurfaceRecordKind.Ramp, CellRectangle(3), groundUnitMask, 1, Vector2Int.zero, 0f));
        records.Add(CreateSurface(5, DenseCitySurfaceRecordKind.Blocker, CellRectangle(4), 0, 0, Vector2Int.zero, 0f));
        records.Seal();

        DenseCitySurfaceProxyBuilder.Build(
            records,
            mapRoot,
            OperationMapId,
            new Rect(0f, 0f, 20f, 4f),
            OutputFolder);
        MapSurfaceMeshBakeSource[] sources =
            DenseCitySurfaceProxyBakeSourceCollector.Collect(mapRoot);
        Assert.That(sources, Has.Length.EqualTo(5));

        BlobAssetReference<MapSurfaceBlob> blob = default;
        try
        {
            Assert.That(
                new MapSurfaceBakeSystem().TryBuildSingleLayerTerrain(
                    new MapSurfaceBakeRequest(float3.zero, 4f, new int2(5, 1)),
                    sources,
                    Allocator.Persistent,
                    out blob),
                Is.True);

            ref MapSurfaceBlob surface = ref blob.Value;
            AssertSurface(ref surface, 0, MapSurfaceType.Terrain, MapSurfaceMovementMask.Infantry, true);
            AssertSurface(ref surface, 0, MapSurfaceType.Terrain, MapSurfaceMovementMask.WheeledVehicle, true);
            AssertSurface(ref surface, 0, MapSurfaceType.Terrain, MapSurfaceMovementMask.TrackedVehicle, true);
            AssertSurface(ref surface, 0, MapSurfaceType.Terrain, MapSurfaceMovementMask.BuildingPlacement, true);
            AssertSurface(ref surface, 0, MapSurfaceType.Terrain, MapSurfaceMovementMask.AirGrounded, true);
            AssertSurface(ref surface, 1, MapSurfaceType.Road, MapSurfaceMovementMask.BuildingPlacement, false);
            AssertSurface(ref surface, 1, MapSurfaceType.Road, MapSurfaceMovementMask.AirGrounded, true);
            AssertSurface(ref surface, 2, MapSurfaceType.BridgeDeck, MapSurfaceMovementMask.WheeledVehicle, true);
            AssertSurface(ref surface, 2, MapSurfaceType.BridgeDeck, MapSurfaceMovementMask.AirGrounded, false);
            AssertSurface(ref surface, 3, MapSurfaceType.Ramp, MapSurfaceMovementMask.TrackedVehicle, true);
            AssertSurface(ref surface, 4, MapSurfaceType.Blocked, MapSurfaceMovementMask.Infantry, false);
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
        }
    }

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(TempRoot);
        EnsureFolder(TempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        CloseSceneIfLoaded(EntityScenePath);
        if (SceneManager.GetSceneByPath(MapScenePath).isLoaded)
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(TempRoot);
    }

    [Test]
    public void Build_PartitionsRecordsAndCreatesBakeOnlyMeshes()
    {
        (Scene mapScene, Scene entityScene, DenseCityGeneratedRootAuthoring mapRoot) = CreateScenePair("primary");
        using DenseCityGenerationRecordSet records = CreateRepresentativeRecords();

        DenseCitySurfaceProxyBuildResult result =
            DenseCitySurfaceProxyBuilder.Build(
                records,
                mapRoot,
                OperationMapId,
                MapSurfaceBounds,
                OutputFolder);

        Assert.That(result.Records, Is.EqualTo(7));
        Assert.That(result.Partitions, Is.EqualTo(6));
        Assert.That(result.Vertices, Is.EqualTo(24));
        Assert.That(result.Triangles, Is.EqualTo(12));
        Assert.That(AssetDatabase.IsValidFolder(OutputFolder), Is.True);
        Assert.That(AssetDatabase.FindAssets("t:Mesh", new[] { OutputFolder }), Has.Length.EqualTo(6));
        Assert.That(mapRoot.GetComponentsInChildren<MeshFilter>(true), Has.Length.EqualTo(6));
        Assert.That(mapRoot.GetComponentsInChildren<MeshRenderer>(true), Is.Empty);
        Assert.That(mapRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(mapRoot.GetComponentsInChildren<Collider2D>(true), Is.Empty);
        Assert.That(mapRoot.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
        Assert.That(mapRoot.GetComponentsInChildren<Rigidbody2D>(true), Is.Empty);
        Assert.That(CountPartitions(mapRoot, "Terrain"), Is.EqualTo(2));
        Assert.That(CountPartitions(mapRoot, "Roads"), Is.EqualTo(1));
        Assert.That(CountPartitions(mapRoot, "Bridges"), Is.EqualTo(1));
        Assert.That(CountPartitions(mapRoot, "Ramps"), Is.EqualTo(1));
        Assert.That(CountPartitions(mapRoot, "Blockers"), Is.EqualTo(1));

        var metadata = new HashSet<string>(StringComparer.Ordinal);
        foreach (MeshFilter filter in mapRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            MapBakeGroupAuthoring owner = filter.GetComponent<MapBakeGroupAuthoring>();
            Assert.That(owner, Is.Not.Null);
            Assert.That(filter.transform.parent.GetComponent<MapBakeGroupAuthoring>().Role, Is.EqualTo(owner.Role));
            metadata.Add($"{owner.Role}:{owner.LayerId}:{(uint)owner.MovementMask:x8}");
            AssertMeshFacesUp(filter.sharedMesh);
        }
        Assert.That(metadata, Does.Contain("Terrain:0:00000001"));
        Assert.That(metadata, Does.Contain("Terrain:0:00000003"));
        Assert.That(metadata, Does.Contain("Road:0:00000003"));
        Assert.That(metadata, Does.Contain("Bridge:1:00000001"));
        Assert.That(metadata, Does.Contain("Ramp:1:00000001"));
        Assert.That(metadata, Does.Contain("Blocker:0:00000000"));
        MeshFilter mergedTerrain = Array.Find(
            mapRoot.GetComponentsInChildren<MeshFilter>(true),
            filter =>
            {
                MapBakeGroupAuthoring owner = filter.GetComponent<MapBakeGroupAuthoring>();
                return owner.Role == MapBakeGroupRole.Terrain && (uint)owner.MovementMask == 1;
            });
        Assert.That(mergedTerrain.sharedMesh.vertexCount, Is.EqualTo(4));
        Assert.That(
            DenseCitySemanticHierarchyBuilder.TryValidate(
                mapScene,
                entityScene,
                "dense-city:surface-proxy:primary",
                out string error),
            Is.True,
            error);
    }

    [Test]
    public void Build_ConsumesTerrainFoundationAndParkFactoryRecords()
    {
        var (_, _, mapRoot) = CreateScenePair("terrain-factories");
        using var records = new DenseCityGenerationRecordSet(1, 4, 4);
        DenseCityTerrainVisualRecordGroup terrain = CreateTerrainGroup(
            0,
            "district-terrain",
            new Vector3(0f, 0f, 0f));
        DenseCityTerrainVisualRecordGroup park = CreateTerrainGroup(
            2,
            "canal-park-terrain",
            new Vector3(20f, 0f, 0f));
        records.AddTerrainVisualGroup(terrain.Terrain, terrain.Presentations);
        records.AddTerrainVisualGroup(park.Terrain, park.Presentations);
        DenseCityBuildingRecordFactory.Add(records, DenseCityBuildingRecordFactory.Create(
            new DenseCityBuildingRecordInput(
                "dense-city-v1",
                42,
                3,
                4,
                SourceGuid,
                20,
                SourceGuid,
                21,
                new[] { SourceGuid },
                new[] { SourceGuid },
                Matrix4x4.TRS(new Vector3(40f, 0f, 0f), Quaternion.identity, Vector3.one),
                Vector2Int.zero,
                new Vector2Int(2, 2),
                new Vector2(8f, 6f),
                0f,
                new Bounds(new Vector3(40f, 2f, 0f), new Vector3(8f, 4f, 6f)),
                Vector3.forward,
                GeneratedCityBuildingRole.House,
                SourceGuid,
                0,
                100f,
                1,
                0,
                Vector2Int.zero,
                "proxy")));
        records.Seal();

        DenseCitySurfaceProxyBuildResult result = DenseCitySurfaceProxyBuilder.Build(
            records,
            mapRoot,
            OperationMapId,
            MapSurfaceBounds,
            OutputFolder);

        Assert.That(records.Surfaces, Has.Count.EqualTo(4));
        Assert.That(result.Records, Is.EqualTo(4));
        Assert.That(result.Partitions, Is.EqualTo(2));
        MeshFilter terrainProxy = Array.Find(
            mapRoot.GetComponentsInChildren<MeshFilter>(true),
            filter => filter.GetComponent<MapBakeGroupAuthoring>().Role == MapBakeGroupRole.Terrain);
        Assert.That(terrainProxy, Is.Not.Null);
        Assert.That(terrainProxy.sharedMesh.vertexCount, Is.EqualTo(12));
        Assert.That(terrainProxy.sharedMesh.triangles, Has.Length.EqualTo(18));
        Assert.That(
            terrainProxy.GetComponent<MapBakeGroupAuthoring>().MovementMask,
            Is.EqualTo((MapSurfaceMovementMask)1));
    }

    [Test]
    public void Build_SeparatesRoadFromShoulderAndNaturalGroundFactoryRecords()
    {
        var (_, _, mapRoot) = CreateScenePair("road-factories");
        using var records = new DenseCityGenerationRecordSet(1, 4, 2);
        DenseCityRoadRecordGroup road = DenseCityInfrastructureRecordFactory.CreateRoadWithShoulders(
            new DenseCityInfrastructureRecordInput(
                "dense-city-v1",
                42,
                3,
                0,
                "road",
                DenseCitySurfaceRecordKind.Road,
                SourceGuid,
                30,
                new[] { SourceGuid },
                Matrix4x4.identity,
                new Vector2(4f, 12f),
                0f,
                3,
                0,
                Vector2Int.zero,
                false,
                true,
                1),
            new[]
            {
                new DenseCityRoadShoulderRecordInput(
                    Matrix4x4.Translate(new Vector3(-3f, 0f, 0f)),
                    new Vector2(2f, 12f),
                    0f,
                    Vector2Int.zero),
                new DenseCityRoadShoulderRecordInput(
                    Matrix4x4.Translate(new Vector3(3f, 0f, 0f)),
                    new Vector2(2f, 12f),
                    0f,
                    Vector2Int.zero)
            });
        records.AddRoadGroup(road.Road, road.Presentation, road.Shoulders);
        DenseCityTerrainVisualRecordGroup naturalGround = CreateTerrainGroup(
            4,
            "natural-ground-terrain",
            new Vector3(20f, 0f, 20f),
            3);
        records.AddTerrainVisualGroup(naturalGround.Terrain, naturalGround.Presentations);
        records.Seal();

        DenseCitySurfaceProxyBuildResult result = DenseCitySurfaceProxyBuilder.Build(
            records,
            mapRoot,
            OperationMapId,
            MapSurfaceBounds,
            OutputFolder);

        Assert.That(result.Records, Is.EqualTo(4));
        Assert.That(result.Partitions, Is.EqualTo(2));
        MeshFilter[] filters = mapRoot.GetComponentsInChildren<MeshFilter>(true);
        MeshFilter roadProxy = Array.Find(
            filters,
            filter => filter.GetComponent<MapBakeGroupAuthoring>().Role == MapBakeGroupRole.Road);
        MeshFilter terrainProxy = Array.Find(
            filters,
            filter => filter.GetComponent<MapBakeGroupAuthoring>().Role == MapBakeGroupRole.Terrain);
        Assert.That(roadProxy, Is.Not.Null);
        Assert.That(terrainProxy, Is.Not.Null);
        Assert.That(roadProxy.sharedMesh.vertexCount, Is.EqualTo(4));
        Assert.That(terrainProxy.sharedMesh.vertexCount, Is.EqualTo(12));
        Assert.That(roadProxy.sharedMesh.triangles, Has.Length.EqualTo(6));
        Assert.That(terrainProxy.sharedMesh.triangles, Has.Length.EqualTo(18));
        Assert.That(roadProxy.GetComponent<MapBakeGroupAuthoring>().MovementMask,
            Is.EqualTo((MapSurfaceMovementMask)3));
        Assert.That(terrainProxy.GetComponent<MapBakeGroupAuthoring>().MovementMask,
            Is.EqualTo((MapSurfaceMovementMask)3));
    }

    [Test]
    public void Build_SeparatesBridgeDeckFromApproachRampFactoryRecords()
    {
        var (_, _, mapRoot) = CreateScenePair("bridge-factories");
        using var records = new DenseCityGenerationRecordSet(1, 3, 1);
        DenseCityBridgeRecordGroup bridge = DenseCityInfrastructureRecordFactory.CreateBridgeWithApproaches(
            new DenseCityInfrastructureRecordInput(
                "dense-city-v1",
                42,
                3,
                0,
                "bridge",
                DenseCitySurfaceRecordKind.Bridge,
                SourceGuid,
                40,
                new[] { SourceGuid },
                Matrix4x4.identity,
                new Vector2(4f, 6f),
                2f,
                3,
                2,
                Vector2Int.zero,
                true,
                true,
                2),
            new DenseCityBridgeApproachRecordInput(
                "bridge-ramp-south",
                Matrix4x4.Translate(new Vector3(0f, 0f, -5f)),
                new Vector2(4f, 4f),
                1f,
                Vector2Int.zero),
            new DenseCityBridgeApproachRecordInput(
                "bridge-ramp-north",
                Matrix4x4.Translate(new Vector3(0f, 0f, 5f)),
                new Vector2(4f, 4f),
                1f,
                Vector2Int.zero));
        records.AddBridgeGroup(
            bridge.Bridge,
            bridge.Presentation,
            bridge.FirstApproachRamp,
            bridge.SecondApproachRamp);
        records.Seal();

        DenseCitySurfaceProxyBuildResult result = DenseCitySurfaceProxyBuilder.Build(
            records,
            mapRoot,
            OperationMapId,
            MapSurfaceBounds,
            OutputFolder);

        Assert.That(result.Records, Is.EqualTo(3));
        Assert.That(result.Partitions, Is.EqualTo(2));
        MeshFilter[] filters = mapRoot.GetComponentsInChildren<MeshFilter>(true);
        MeshFilter bridgeProxy = Array.Find(
            filters,
            filter => filter.GetComponent<MapBakeGroupAuthoring>().Role == MapBakeGroupRole.Bridge);
        MeshFilter rampProxy = Array.Find(
            filters,
            filter => filter.GetComponent<MapBakeGroupAuthoring>().Role == MapBakeGroupRole.Ramp);
        Assert.That(bridgeProxy, Is.Not.Null);
        Assert.That(rampProxy, Is.Not.Null);
        Assert.That(bridgeProxy.sharedMesh.vertexCount, Is.EqualTo(4));
        Assert.That(rampProxy.sharedMesh.vertexCount, Is.EqualTo(8));
        Assert.That(bridgeProxy.sharedMesh.triangles, Has.Length.EqualTo(6));
        Assert.That(rampProxy.sharedMesh.triangles, Has.Length.EqualTo(12));
        AssertProxyMetadata(bridgeProxy, MapBakeGroupRole.Bridge, 2, 3);
        AssertProxyMetadata(rampProxy, MapBakeGroupRole.Ramp, 2, 3);
    }

    [Test]
    public void Build_ConvertsCanalWaterExclusionWithoutPhysicsComponents()
    {
        var (_, _, mapRoot) = CreateScenePair("canal-water-exclusion");
        using var records = new DenseCityGenerationRecordSet(1, 1, 2);
        Matrix4x4 bedMatrix = Matrix4x4.TRS(
            new Vector3(20f, 1.9f, 40f),
            Quaternion.identity,
            new Vector3(12f, 1f, 8f));
        Matrix4x4 waterMatrix = Matrix4x4.TRS(
            new Vector3(20f, 2f, 40f),
            Quaternion.identity,
            new Vector3(12f, 1f, 8f));
        DenseCityCanalWaterRecordGroup canal = DenseCityCanalWaterRecordFactory.Create(
            new DenseCityCanalWaterRecordInput(
                "dense-city-v1",
                42,
                3,
                0,
                SourceGuid,
                50,
                new[] { SourceGuid },
                new[] { SourceGuid },
                bedMatrix,
                waterMatrix,
                new Vector2(12f, 8f),
                2f,
                4,
                Vector2Int.zero));
        records.AddCanalWaterGroup(
            canal.Exclusion,
            canal.BedPresentation,
            canal.WaterPresentation);
        records.Seal();

        DenseCitySurfaceProxyBuildResult result = DenseCitySurfaceProxyBuilder.Build(
            records,
            mapRoot,
            OperationMapId,
            MapSurfaceBounds,
            OutputFolder);

        Assert.That(result.Records, Is.EqualTo(1));
        Assert.That(result.Partitions, Is.EqualTo(1));
        Assert.That(records.Presentations, Has.Count.EqualTo(2));
        MeshFilter[] filters = mapRoot.GetComponentsInChildren<MeshFilter>(true);
        Assert.That(filters, Has.Length.EqualTo(1));
        AssertProxyMetadata(filters[0], MapBakeGroupRole.Blocker, 4, 0);
        Assert.That(mapRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(mapRoot.GetComponentsInChildren<Rigidbody>(true), Is.Empty);

        MapSurfaceMeshBakeSource[] sources =
            DenseCitySurfaceProxyBakeSourceCollector.Collect(mapRoot);
        Assert.That(sources, Has.Length.EqualTo(1));
        BlobAssetReference<MapSurfaceBlob> blob = default;
        try
        {
            Assert.That(
                new MapSurfaceBakeSystem().TryBuildSingleLayerTerrain(
                    new MapSurfaceBakeRequest(new float3(14f, 0f, 36f), 4f, new int2(3, 2)),
                    sources,
                    Allocator.Persistent,
                    out blob),
                Is.True);
            ref MapSurfaceBlob surface = ref blob.Value;
            for (int z = 0; z < 2; z++)
            {
                for (int x = 0; x < 3; x++)
                    AssertSurface(ref surface, x, z, MapSurfaceType.Blocked, MapSurfaceMovementMask.Infantry, false);
            }
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
        }
    }

    [Test]
    public void Build_ConsumesBuildingWallAndRockBlockerFactoryRecords()
    {
        var (_, _, mapRoot) = CreateScenePair("building-wall-rock-blockers");
        using var records = new DenseCityGenerationRecordSet(1, 4, 4);
        DenseCityBuildingRecordFactory.Add(records, DenseCityBuildingRecordFactory.Create(
            new DenseCityBuildingRecordInput(
                "dense-city-v1",
                42,
                3,
                0,
                SourceGuid,
                60,
                SourceGuid,
                61,
                new[] { SourceGuid },
                new[] { SourceGuid },
                Matrix4x4.TRS(new Vector3(10f, 0f, 10f), Quaternion.identity, Vector3.one),
                Vector2Int.zero,
                new Vector2Int(2, 2),
                new Vector2(8f, 8f),
                0f,
                new Bounds(new Vector3(10f, 2f, 10f), new Vector3(8f, 4f, 8f)),
                Vector3.forward,
                GeneratedCityBuildingRole.House,
                SourceGuid,
                0,
                100f,
                1,
                3,
                Vector2Int.zero,
                "blocker-proxy")));
        DenseCityVisualBlockerRecordGroup wall = CreateVisualBlockerGroup(
            5,
            "courtyard-wall-blocker",
            new Vector3(30f, 0f, 10f),
            Quaternion.Euler(0f, 90f, 0f),
            new Vector2(2f, 8f));
        DenseCityVisualBlockerRecordGroup rock = CreateVisualBlockerGroup(
            7,
            "urban-rock-blocker",
            new Vector3(50f, 0f, 10f),
            Quaternion.Euler(0f, 30f, 0f),
            new Vector2(6f, 6f));
        records.AddVisualBlockerGroup(wall.Blocker, wall.Presentation);
        records.AddVisualBlockerGroup(rock.Blocker, rock.Presentation);
        records.Seal();

        DenseCitySurfaceProxyBuildResult result = DenseCitySurfaceProxyBuilder.Build(
            records,
            mapRoot,
            OperationMapId,
            MapSurfaceBounds,
            OutputFolder);

        Assert.That(records.Buildings, Has.Count.EqualTo(1));
        Assert.That(records.Surfaces, Has.Count.EqualTo(4));
        Assert.That(records.Presentations, Has.Count.EqualTo(4));
        Assert.That(result.Records, Is.EqualTo(4));
        Assert.That(result.Partitions, Is.EqualTo(2));
        MeshFilter blockerProxy = Array.Find(
            mapRoot.GetComponentsInChildren<MeshFilter>(true),
            filter => filter.GetComponent<MapBakeGroupAuthoring>().Role == MapBakeGroupRole.Blocker);
        Assert.That(blockerProxy, Is.Not.Null);
        Assert.That(blockerProxy.sharedMesh.vertexCount, Is.EqualTo(12));
        Assert.That(blockerProxy.sharedMesh.triangles, Has.Length.EqualTo(18));
        AssertProxyMetadata(blockerProxy, MapBakeGroupRole.Blocker, 3, 0);
        Assert.That(mapRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(mapRoot.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
    }

    [Test]
    public void Build_MergesOnlyAdjacentCoplanarRectangles()
    {
        var (_, _, mapRoot) = CreateScenePair("merge-boundary");
        using var records = new DenseCityGenerationRecordSet(1, 5, 1);
        records.Add(CreateSurface(
            1,
            DenseCitySurfaceRecordKind.Terrain,
            Rectangle(0f),
            1,
            0,
            Vector2Int.zero,
            2f));
        records.Add(CreateSurface(
            2,
            DenseCitySurfaceRecordKind.Terrain,
            Rectangle(4f),
            1,
            0,
            Vector2Int.zero,
            2f));
        records.Add(CreateSurface(
            3,
            DenseCitySurfaceRecordKind.Terrain,
            Rectangle(8f),
            1,
            0,
            Vector2Int.zero,
            3f));
        records.Add(CreateSurface(
            4,
            DenseCitySurfaceRecordKind.Road,
            Rectangle(20f),
            3,
            0,
            Vector2Int.one,
            1f));
        records.Add(CreateSurface(
            5,
            DenseCitySurfaceRecordKind.Road,
            Rectangle(24f),
            3,
            0,
            Vector2Int.one,
            1f));
        records.Seal();

        DenseCitySurfaceProxyBuildResult result =
            DenseCitySurfaceProxyBuilder.Build(
                records,
                mapRoot,
                OperationMapId,
                MapSurfaceBounds,
                OutputFolder);

        Assert.That(result.Partitions, Is.EqualTo(2));
        Assert.That(result.Records, Is.EqualTo(5));
        Assert.That(result.Vertices, Is.EqualTo(12));
        Assert.That(result.Triangles, Is.EqualTo(6));
        MeshFilter[] filters = mapRoot.GetComponentsInChildren<MeshFilter>(true);
        Assert.That(filters, Has.Length.EqualTo(2));
        MeshFilter road = Array.Find(
            filters,
            filter => filter.GetComponent<MapBakeGroupAuthoring>().Role == MapBakeGroupRole.Road);
        Assert.That(road.sharedMesh.vertexCount, Is.EqualTo(4));
    }

    [Test]
    public void Build_IsDeterministicAcrossEquivalentRecordSets()
    {
        var (_, firstEntity, firstRoot) = CreateScenePair("first");
        using DenseCityGenerationRecordSet firstRecords = CreateRepresentativeRecords();
        DenseCitySurfaceProxyBuilder.Build(
            firstRecords,
            firstRoot,
            OperationMapId,
            MapSurfaceBounds,
            TempRoot + "/First/" + OperationMapId + "/Candidate/" + Hash + "/SurfaceProxies");
        string firstSignature = CreateMeshSignature(firstRoot);
        EditorSceneManager.CloseScene(firstEntity, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        (Scene secondMap, Scene secondEntity, DenseCityGeneratedRootAuthoring secondRoot) =
            CreateScenePair("second");
        using DenseCityGenerationRecordSet secondRecords = CreateRepresentativeRecords();
        DenseCitySurfaceProxyBuilder.Build(
            secondRecords,
            secondRoot,
            OperationMapId,
            MapSurfaceBounds,
            TempRoot + "/Second/" + OperationMapId + "/Candidate/" + Hash + "/SurfaceProxies");

        Assert.That(CreateMeshSignature(secondRoot), Is.EqualTo(firstSignature));
        Assert.That(secondMap.IsValid(), Is.True);
        Assert.That(secondEntity.IsValid(), Is.True);
    }

    [Test]
    public void Build_OutsideGridPresentationRemainsPresentationOnly()
    {
        var (_, _, mapRoot) = CreateScenePair("presentation-only");
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        records.Add(CreateSurface(
            1,
            DenseCitySurfaceRecordKind.Terrain,
            Rectangle(0f),
            1,
            0,
            Vector2Int.zero));
        records.Add(new DenseCityPresentationBakeRecord(
            new DenseCityRecordIdentity(
                "dense-city-v1",
                42,
                3,
                "horizon-presentation",
                2,
                SourceGuid,
                2),
            DenseCityPresentationCategory.Horizon,
            string.Empty,
            SourceGuid,
            new[] { SourceGuid },
            Matrix4x4.Translate(new Vector3(MapSurfaceBounds.xMax + 500f, 0f, MapSurfaceBounds.yMax + 500f)),
            true,
            true,
            0));
        records.Seal();

        DenseCitySurfaceProxyBuildResult result = DenseCitySurfaceProxyBuilder.Build(
            records,
            mapRoot,
            OperationMapId,
            MapSurfaceBounds,
            OutputFolder);

        Assert.That(records.Presentations, Has.Count.EqualTo(1));
        Assert.That(records.Presentations[0].WorldMatrix.GetPosition().x, Is.GreaterThan(MapSurfaceBounds.xMax));
        Assert.That(records.Presentations[0].WorldMatrix.GetPosition().z, Is.GreaterThan(MapSurfaceBounds.yMax));
        Assert.That(result.Records, Is.EqualTo(1));
        Assert.That(result.Partitions, Is.EqualTo(1));
        Assert.That(mapRoot.GetComponentsInChildren<MeshFilter>(true), Has.Length.EqualTo(1));
        Assert.That(DenseCitySurfaceProxyBakeSourceCollector.Collect(mapRoot), Has.Length.EqualTo(1));
    }

    [Test]
    public void Build_OutOfBoundsPolygonLeavesNoCandidateOutput()
    {
        var (_, _, mapRoot) = CreateScenePair("bounds");
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        records.Add(CreateSurface(
            1,
            DenseCitySurfaceRecordKind.Terrain,
            Rectangle(88f),
            1,
            0,
            Vector2Int.zero));
        records.Seal();

        Assert.That(
            () => DenseCitySurfaceProxyBuilder.Build(
                records,
                mapRoot,
                OperationMapId,
                MapSurfaceBounds,
                OutputFolder),
            Throws.InvalidOperationException.With.Message.Contains("map bounds"));
        Assert.That(AssetDatabase.IsValidFolder(OutputFolder), Is.False);
        Assert.That(mapRoot.GetComponentsInChildren<MeshFilter>(true), Is.Empty);
    }

    [Test]
    public void Build_ProhibitedProxyComponentLeavesNoCandidateOutput()
    {
        var (_, _, mapRoot) = CreateScenePair("polluted");
        Transform terrainRoot = mapRoot.transform.Find("BakeSources/Terrain");
        terrainRoot.gameObject.AddComponent<AudioSource>();
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        records.Add(CreateSurface(
            1,
            DenseCitySurfaceRecordKind.Terrain,
            Rectangle(0f),
            1,
            0,
            Vector2Int.zero));
        records.Seal();

        Assert.That(
            () => DenseCitySurfaceProxyBuilder.Build(
                records,
                mapRoot,
                OperationMapId,
                MapSurfaceBounds,
                OutputFolder),
            Throws.InvalidOperationException.With.Message.Contains("AudioSource"));
        Assert.That(AssetDatabase.IsValidFolder(OutputFolder), Is.False);
        Assert.That(mapRoot.GetComponentsInChildren<MeshFilter>(true), Is.Empty);
    }

    [Test]
    public void Build_InvalidConcavePolygonLeavesNoCandidateOutput()
    {
        var (_, _, mapRoot) = CreateScenePair("invalid");
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        records.Add(CreateSurface(
            1,
            DenseCitySurfaceRecordKind.Terrain,
            new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 4f),
                new Vector2(2f, 2f),
                new Vector2(4f, 4f),
                new Vector2(4f, 0f)
            },
            1,
            0,
            Vector2Int.zero));
        records.Seal();

        Assert.That(
            () => DenseCitySurfaceProxyBuilder.Build(
                records,
                mapRoot,
                OperationMapId,
                MapSurfaceBounds,
                OutputFolder),
            Throws.InvalidOperationException.With.Message.Contains("convex"));
        Assert.That(AssetDatabase.IsValidFolder(OutputFolder), Is.False);
        Assert.That(mapRoot.GetComponentsInChildren<MeshFilter>(true), Is.Empty);
    }

    private static DenseCityGenerationRecordSet CreateRepresentativeRecords()
    {
        var records = new DenseCityGenerationRecordSet(1, 7, 1);
        records.Add(CreateSurface(7, DenseCitySurfaceRecordKind.Blocker, Rectangle(60f), 0, 0, new Vector2Int(2, 3)));
        records.Add(CreateSurface(2, DenseCitySurfaceRecordKind.Terrain, Rectangle(4f), 1, 0, Vector2Int.zero, 0f));
        records.Add(CreateSurface(6, DenseCitySurfaceRecordKind.Ramp, Rectangle(50f), 1, 1, new Vector2Int(2, 3)));
        records.Add(CreateSurface(1, DenseCitySurfaceRecordKind.Terrain, Rectangle(0f), 1, 0, Vector2Int.zero, 0f));
        records.Add(CreateSurface(4, DenseCitySurfaceRecordKind.Road, Rectangle(30f), 3, 0, new Vector2Int(1, 2)));
        records.Add(CreateSurface(3, DenseCitySurfaceRecordKind.Terrain, Rectangle(20f), 3, 0, Vector2Int.zero));
        records.Add(CreateSurface(5, DenseCitySurfaceRecordKind.Bridge, Rectangle(40f), 1, 1, new Vector2Int(2, 3)));
        records.Seal();
        return records;
    }

    private static DenseCityTerrainVisualRecordGroup CreateTerrainGroup(
        int sequence,
        string recordKind,
        Vector3 position,
        uint movementMask = 1) =>
        DenseCityTerrainVisualRecordFactory.Create(new DenseCityTerrainVisualRecordInput(
            "dense-city-v1",
            42,
            3,
            sequence,
            recordKind,
            Matrix4x4.TRS(position, Quaternion.identity, Vector3.one),
            new Vector2(8f, 6f),
            0f,
            movementMask,
            0,
            Vector2Int.zero,
            new[]
            {
                new DenseCityTerrainVisualPresentationInput(
                    recordKind + "-visual",
                    SourceGuid,
                    sequence + 1,
                    new[] { SourceGuid },
                    Matrix4x4.TRS(position, Quaternion.identity, Vector3.one),
                    false,
                    true,
                    1)
            }));

    private static DenseCityVisualBlockerRecordGroup CreateVisualBlockerGroup(
        int sequence,
        string recordKind,
        Vector3 position,
        Quaternion rotation,
        Vector2 blockerSize) =>
        DenseCityVisualBlockerRecordFactory.Create(new DenseCityVisualBlockerRecordInput(
            "dense-city-v1",
            42,
            3,
            sequence,
            recordKind,
            DenseCityPresentationCategory.Infrastructure,
            SourceGuid,
            sequence,
            new[] { SourceGuid },
            Matrix4x4.TRS(position, rotation, Vector3.one),
            blockerSize,
            0f,
            3,
            Vector2Int.zero,
            true,
            true,
            1));

    private static DenseCitySurfaceBakeRecord CreateSurface(
        int sequence,
        DenseCitySurfaceRecordKind kind,
        IReadOnlyList<Vector2> polygon,
        uint movementMask,
        int layer,
        Vector2Int chunk,
        float? elevation = null) =>
        new(
            new DenseCityRecordIdentity(
                "dense-city-v1",
                42,
                3,
                "surface-" + kind.ToString().ToLowerInvariant(),
                sequence,
                SourceGuid,
                sequence),
            kind,
            polygon,
            elevation ?? sequence * 0.25f,
            movementMask,
            layer,
            chunk);

    private static Vector2[] Rectangle(float x) =>
        new[]
        {
            new Vector2(x, 0f),
            new Vector2(x + 4f, 0f),
            new Vector2(x + 4f, 3f),
            new Vector2(x, 3f)
        };

    private static Vector2[] CellRectangle(int cell)
    {
        float x = cell * 4f;
        return new[]
        {
            new Vector2(x, 0f),
            new Vector2(x + 4f, 0f),
            new Vector2(x + 4f, 4f),
            new Vector2(x, 4f)
        };
    }

    private static void AssertSurface(
        ref MapSurfaceBlob blob,
        int cellX,
        MapSurfaceType expectedType,
        MapSurfaceMovementMask movement,
        bool expectedAllowed) =>
        AssertSurface(ref blob, cellX, 0, expectedType, movement, expectedAllowed);

    private static void AssertSurface(
        ref MapSurfaceBlob blob,
        int cellX,
        int cellZ,
        MapSurfaceType expectedType,
        MapSurfaceMovementMask movement,
        bool expectedAllowed)
    {
        Assert.That(
            MapSurfaceBlobAccess.TryGetPrimarySurface(
                ref blob,
                new int2(cellX, cellZ),
                out MapSurfaceSample sample),
            Is.True);
        Assert.That(sample.SurfaceType, Is.EqualTo(expectedType));
        Assert.That((sample.MovementMask & movement) != 0, Is.EqualTo(expectedAllowed));
    }

    private static void AssertProxyMetadata(
        MeshFilter filter,
        MapBakeGroupRole role,
        int layer,
        uint movementMask)
    {
        MapBakeGroupAuthoring owner = filter.GetComponent<MapBakeGroupAuthoring>();
        Assert.That(owner.Role, Is.EqualTo(role));
        Assert.That(owner.LayerId, Is.EqualTo(layer));
        Assert.That((uint)owner.MovementMask, Is.EqualTo(movementMask));
    }

    private static void AssertMeshFacesUp(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        for (int index = 0; index < triangles.Length; index += 3)
        {
            Vector3 normal = Vector3.Cross(
                vertices[triangles[index + 1]] - vertices[triangles[index]],
                vertices[triangles[index + 2]] - vertices[triangles[index]]);
            Assert.That(normal.y, Is.GreaterThan(0f));
        }
    }

    private static string CreateMeshSignature(DenseCityGeneratedRootAuthoring root)
    {
        var signature = new System.Text.StringBuilder();
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        Array.Sort(filters, (left, right) => string.CompareOrdinal(left.name, right.name));
        foreach (MeshFilter filter in filters)
        {
            MapBakeGroupAuthoring group = filter.GetComponent<MapBakeGroupAuthoring>();
            signature.Append(filter.name).Append('|').Append(group.Role).Append('|')
                .Append(group.LayerId).Append('|').Append((uint)group.MovementMask).Append('|');
            foreach (Vector3 vertex in filter.sharedMesh.vertices)
                signature.Append(vertex.x).Append(',').Append(vertex.y).Append(',').Append(vertex.z).Append(';');
            signature.Append('|').AppendJoin(',', filter.sharedMesh.triangles).AppendLine();
        }
        return signature.ToString();
    }

    private static int CountPartitions(DenseCityGeneratedRootAuthoring root, string roleName) =>
        root.transform.Find("BakeSources/" + roleName).childCount;

    private static (Scene MapScene, Scene EntityScene, DenseCityGeneratedRootAuthoring MapRoot)
        CreateScenePair(string suffix)
    {
        Scene mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(mapScene, MapScenePath), Is.True);
        Scene entityScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Assert.That(EditorSceneManager.SaveScene(entityScene, EntityScenePath), Is.True);
        var roots = DenseCitySemanticHierarchyBuilder.Create(
            mapScene,
            entityScene,
            "dense-city:surface-proxy:" + suffix,
            "dense-city-v1",
            1,
            42,
            Hash);
        return (mapScene, entityScene, roots.MapBakeSource);
    }

    private static void CloseSceneIfLoaded(string path)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        if (scene.IsValid() && scene.isLoaded)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
    }
}
