using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Composition;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class MapSurfaceRuntimeBootstrapSceneSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new MapSurfaceRuntimeBootstrapSceneSystemHelperTests();
            tests.EnsureReplacesStaleSubsceneSurfaceWithAuthoredRuntimeAsset();
            tests.EnsureMatchingActiveMapPublishesSurface();
            tests.EnsureMismatchedActiveMapPreservesExistingSurface();
            tests.DisposeRuntimeSurfaceAfterWorldDisposeDoesNotThrow();
            tests.RuntimeBlobHashChangesWhenSurfacePayloadChanges();
            tests.MapSurfaceAuthoringBakerUsesContentHashDeduplication();
            Debug.Log("[MapSurfaceRuntimeBootstrapValidation] result=Passed tests=6");
        }
        catch (Exception exception)
        {
            Debug.LogError("[MapSurfaceRuntimeBootstrapValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void EnsureMatchingActiveMapPublishesSurface()
    {
        using World world = new("MapSurfaceRuntimeBootstrapSceneSystemHelperTests.ActiveMap");
        BlobAssetReference<MapSurfaceBlob> sourceBlob = default;
        BlobAssetReference<OperationMapBlob> operationMapBlob = default;
        MapSurfaceDataAsset asset = ScriptableObject.CreateInstance<MapSurfaceDataAsset>();
        MapSurfaceRuntimeBootstrapSceneSystemHelper bootstrap = new(world);
        try
        {
            sourceBlob = CreateSingleCellSurface(2.5f);
            asset.ConfigureBakedSurface(Vector3.zero, 1f, Vector2Int.one, sourceBlob, false);
            operationMapBlob = CreateActiveOperationMap(world.EntityManager, asset);

            Assert.That(bootstrap.Ensure(asset, out string error), Is.True, error);
            using EntityQuery query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            Assert.That(query.CalculateEntityCount(), Is.EqualTo(1));
        }
        finally
        {
            bootstrap.DisposeRuntimeSurface();
            if (operationMapBlob.IsCreated)
                operationMapBlob.Dispose();
            if (sourceBlob.IsCreated)
                sourceBlob.Dispose();
            UnityEngine.Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void EnsureMismatchedActiveMapPreservesExistingSurface()
    {
        using World world = new("MapSurfaceRuntimeBootstrapSceneSystemHelperTests.Mismatch");
        BlobAssetReference<MapSurfaceBlob> existingBlob = default;
        BlobAssetReference<MapSurfaceBlob> sourceBlob = default;
        BlobAssetReference<OperationMapBlob> operationMapBlob = default;
        MapSurfaceDataAsset asset = ScriptableObject.CreateInstance<MapSurfaceDataAsset>();
        try
        {
            existingBlob = CreateSingleCellSurface(7f);
            sourceBlob = CreateSingleCellSurface(2.5f);
            asset.ConfigureBakedSurface(Vector3.zero, 1f, Vector2Int.one, sourceBlob, false);
            operationMapBlob = CreateActiveOperationMap(
                world.EntityManager,
                asset,
                new FixedString64Bytes("00000000000000000000000000000000"));

            Entity existing = world.EntityManager.CreateEntity(typeof(MapSurfaceComponent));
            world.EntityManager.SetComponentData(existing, CreateSurfaceComponent(existingBlob));
            MapSurfaceRuntimeBootstrapSceneSystemHelper bootstrap = new(world);

            Assert.That(bootstrap.Ensure(asset, out string error), Is.False);
            Assert.That(error, Does.Contain("does not match"));
            Assert.That(world.EntityManager.Exists(existing), Is.True);
            MapSurfaceComponent retained = world.EntityManager.GetComponentData<MapSurfaceComponent>(existing);
            Assert.That(retained.SurfaceBlob, Is.EqualTo(existingBlob));
        }
        finally
        {
            if (operationMapBlob.IsCreated)
                operationMapBlob.Dispose();
            if (existingBlob.IsCreated)
                existingBlob.Dispose();
            if (sourceBlob.IsCreated)
                sourceBlob.Dispose();
            UnityEngine.Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void EnsureReplacesStaleSubsceneSurfaceWithAuthoredRuntimeAsset()
    {
        World world = new("MapSurfaceRuntimeBootstrapSceneSystemHelperTests");
        BlobAssetReference<MapSurfaceBlob> staleBlob = default;
        BlobAssetReference<MapSurfaceBlob> authoredSourceBlob = default;
        MapSurfaceDataAsset asset = ScriptableObject.CreateInstance<MapSurfaceDataAsset>();
        try
        {
            staleBlob = CreateSingleCellSurface(7f);
            authoredSourceBlob = CreateSingleCellSurface(0.125f);
            asset.ConfigureBakedSurface(
                Vector3.zero,
                1f,
                Vector2Int.one,
                authoredSourceBlob,
                generatedFlatEquivalent: false);

            EntityManager em = world.EntityManager;
            Entity staleSubsceneSurface = em.CreateEntity(typeof(MapSurfaceComponent));
            em.SetComponentData(staleSubsceneSurface, CreateSurfaceComponent(staleBlob));

            MapSurfaceRuntimeBootstrapSceneSystemHelper bootstrap = new(world);
            MethodInfo ensure = bootstrap.GetType().GetMethod(
                "Ensure",
                new[] { typeof(MapSurfaceDataAsset) });
            Assert.IsNotNull(ensure);

            bool ensured = (bool)ensure.Invoke(bootstrap, new object[] { asset });

            Assert.IsTrue(ensured);
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            using NativeArray<Entity> surfaces = query.ToEntityArray(Allocator.Temp);
            Assert.AreEqual(1, surfaces.Length);
            Assert.AreEqual(staleSubsceneSurface, surfaces[0], "Runtime may reuse the stale subscene entity, but it must replace its surface data.");
            Assert.IsTrue(em.HasComponent<MapSurfaceRuntimeBakedBlobTag>(staleSubsceneSurface));

            MapSurfaceComponent runtimeSurface = em.GetComponentData<MapSurfaceComponent>(staleSubsceneSurface);
            ref MapSurfaceBlob runtimeBlob = ref runtimeSurface.SurfaceBlob.Value;
            Assert.IsTrue(MapSurfaceBlobAccess.TryGetPrimarySurface(ref runtimeBlob, int2.zero, out MapSurfaceSample runtimeSample));
            Assert.AreEqual(0.125f, runtimeSample.Height, 0.0001f);
            Assert.AreEqual(new float3(0f, 0f, 0f), runtimeBlob.GridOrigin);
            Assert.AreEqual(new int2(1, 1), runtimeBlob.Dimensions);
        }
        finally
        {
            if (world.IsCreated)
                new MapSurfaceRuntimeBootstrapSceneSystemHelper(world).DisposeRuntimeSurface();
            if (staleBlob.IsCreated)
                staleBlob.Dispose();
            if (authoredSourceBlob.IsCreated)
                authoredSourceBlob.Dispose();
            UnityEngine.Object.DestroyImmediate(asset);
            world.Dispose();
        }
    }

    [Test]
    public void DisposeRuntimeSurfaceAfterWorldDisposeDoesNotThrow()
    {
        World world = new("MapSurfaceRuntimeBootstrapSystemDisposeTests");
        MapSurfaceRuntimeBootstrapSceneSystemHelper bootstrap = new(world);

        world.Dispose();

        Assert.DoesNotThrow(() => bootstrap.DisposeRuntimeSurface());
    }

    [Test]
    public void RuntimeBlobHashChangesWhenSurfacePayloadChanges()
    {
        BlobAssetReference<MapSurfaceBlob> firstBlob = default;
        BlobAssetReference<MapSurfaceBlob> secondBlob = default;
        MapSurfaceDataAsset asset = ScriptableObject.CreateInstance<MapSurfaceDataAsset>();
        try
        {
            firstBlob = CreateSingleCellSurface(0.125f);
            asset.ConfigureBakedSurface(
                Vector3.zero,
                1f,
                Vector2Int.one,
                firstBlob,
                generatedFlatEquivalent: false);
            Unity.Entities.Hash128 firstHash = asset.ComputeRuntimeBlobHash();

            secondBlob = CreateSingleCellSurface(9f);
            asset.ConfigureBakedSurface(
                Vector3.zero,
                1f,
                Vector2Int.one,
                secondBlob,
                generatedFlatEquivalent: false);
            Unity.Entities.Hash128 secondHash = asset.ComputeRuntimeBlobHash();

            Assert.AreNotEqual(firstHash, secondHash);
        }
        finally
        {
            if (firstBlob.IsCreated)
                firstBlob.Dispose();
            if (secondBlob.IsCreated)
                secondBlob.Dispose();
            UnityEngine.Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void MapSurfaceAuthoringBakerUsesContentHashDeduplication()
    {
        string source = File.ReadAllText("Assets/Game/Scripts/Authorings/MapSurfaceAuthoring.cs");

        StringAssert.Contains("DependsOn(surfaceData)", source);
        StringAssert.Contains("surfaceData.ComputeRuntimeBlobHash()", source);
        StringAssert.Contains("TryGetBlobAssetReference(surfaceHash", source);
        StringAssert.Contains("AddBlobAssetWithCustomHash(ref surfaceBlob, surfaceHash)", source);
    }

    private static MapSurfaceComponent CreateSurfaceComponent(BlobAssetReference<MapSurfaceBlob> blob)
    {
        return new MapSurfaceComponent
        {
            SurfaceBlob = blob,
            GridOrigin = float3.zero,
            CellSize = 1f,
            Dimensions = new int2(1, 1),
            HasSurfaceData = 1,
            HasLayeredCells = 0,
            HasRoadSurfaces = 0,
            HasBridgeSurfaces = 0
        };
    }

    private static BlobAssetReference<MapSurfaceBlob> CreateSingleCellSurface(float height)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.GridOrigin = float3.zero;
        root.CellSize = 1f;
        root.Dimensions = new int2(1, 1);
        root.RuntimeEncoding = MapSurfaceRuntimeEncoding.Full;

        BlobBuilderArray<MapSurfaceCell> cells = builder.Allocate(ref root.Cells, 1);
        cells[0] = new MapSurfaceCell
        {
            FirstSurfaceIndex = 0,
            SurfaceCount = 1,
            InlineSurfaceIndex = 0
        };

        BlobBuilderArray<MapSurfaceSample> samples = builder.Allocate(ref root.Samples, 1);
        samples[0] = new MapSurfaceSample
        {
            Cell = int2.zero,
            SurfaceId = 0,
            LayerId = 0,
            Height = height,
            Normal = new float3(0f, 1f, 0f),
            SlopeDegrees = 0f,
            SurfaceType = MapSurfaceType.Terrain,
            MovementMask = MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.BuildingPlacement,
            Flags = MapSurfaceFlags.None,
            FirstConnectionIndex = 0,
            ConnectionCount = 0
        };

        builder.Allocate(ref root.Connections, 0);
        builder.Allocate(ref root.CompactSamples, 0);
        BlobAssetReference<MapSurfaceBlob> blob = builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
        builder.Dispose();
        return blob;
    }

    private static BlobAssetReference<OperationMapBlob> CreateActiveOperationMap(
        EntityManager entityManager,
        MapSurfaceDataAsset surface,
        FixedString64Bytes runtimeHash = default)
    {
        if (runtimeHash.IsEmpty)
            runtimeHash = new FixedString64Bytes(surface.ComputeRuntimeBlobHash().ToString());

        using BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
        FixedString64Bytes operationMapId = new("opmap.skirmish.desert_base_01");
        root.OperationMapId = operationMapId;
        root.Grid = new OperationMapGridBlob
        {
            Origin = surface.GridOrigin,
            Dimensions = new int2(surface.Dimensions.x, surface.Dimensions.y),
            CellSize = surface.CellSize
        };
        root.Surface = new OperationMapSurfaceMetadataBlob
        {
            RuntimeBlobHash = runtimeHash,
            SurfaceCount = surface.SurfaceCount,
            PayloadVersion = surface.PayloadVersion,
            PayloadEncoding = surface.PayloadEncoding,
            MinimumHeight = 0f,
            MaximumHeight = 10f
        };
        builder.Allocate(ref root.Anchors, 0);
        builder.Allocate(ref root.Cameras, 0);
        BlobAssetReference<OperationMapBlob> metadata =
            builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);

        Entity mapRoot = entityManager.CreateEntity(
            typeof(OperationMapRootComponent),
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent));
        entityManager.SetComponentData(mapRoot, new ActiveOperationMapComponent
        {
            OperationMapId = operationMapId,
            Generation = 1
        });
        entityManager.SetComponentData(mapRoot, new OperationMapMetadataComponent
        {
            Blob = metadata,
            Generation = 1
        });
        return metadata;
    }
}
#endif
