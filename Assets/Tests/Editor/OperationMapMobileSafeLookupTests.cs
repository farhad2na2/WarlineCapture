using System;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class OperationMapMobileSafeLookupTests
{
    [Test]
    public void ActiveMapMinimapLookup_ReusesQueriesWithoutManagedAllocations()
    {
        using var world = new World("OperationMapMobileSafeLookupTests.Minimap");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        BlobAssetReference<OperationMapBlob> metadata = CreateOperationMapBlob();
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            CreateGrid(world.EntityManager);
            CreateActiveMap(world.EntityManager, metadata, includeBounds: false);
            var adapter = new MatchHudMinimapDataSourceAdapter();

            Assert.That(adapter.TryGetGrid(out _), Is.True);
            Assert.That(adapter.TryGetGrid(out _), Is.True);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 1024; index++)
                adapter.TryGetGrid(out _);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            metadata.Dispose();
        }
    }

    [Test]
    public void ActiveMapCameraBoundsLookup_ReusesQueriesWithoutManagedAllocations()
    {
        using var world = new World("OperationMapMobileSafeLookupTests.Camera");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        GameObject cameraObject = null;
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            RtsCameraSystem cameraSystem = world.GetOrCreateSystemManaged<RtsCameraSystem>();
            RtsCameraRequestSystem requestSystem = world.GetOrCreateSystemManaged<RtsCameraRequestSystem>();
            cameraObject = new GameObject("OperationMapMobileSafeLookupCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(-50f, 10f, -50f), Quaternion.Euler(90f, 0f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 1f;

            CreateGrid(world.EntityManager);
            CreateActiveMap(world.EntityManager, default, includeBounds: true);
            requestSystem.ProcessPendingRequests(world.EntityManager, cameraSystem, camera);
            requestSystem.ProcessPendingRequests(world.EntityManager, cameraSystem, camera);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 256; index++)
                requestSystem.ProcessPendingRequests(world.EntityManager, cameraSystem, camera);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero);
        }
        finally
        {
            if (cameraObject != null)
                UnityEngine.Object.DestroyImmediate(cameraObject);
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void SurfaceAndAnchorBlobLookups_HaveZeroManagedAllocations()
    {
        BlobAssetReference<OperationMapBlob> metadata = CreateOperationMapBlob();
        BlobAssetReference<MapSurfaceBlob> surface = CreateCompactSurfaceBlob();
        try
        {
            FixedString64Bytes anchorId = new("anchor.skirmish.objective.alpha");
            OperationMapMetadataUtility.TryFindAnchor(ref metadata.Value, in anchorId, out _);
            MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.Value, new int2(1, 1), out _);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 1024; index++)
            {
                OperationMapMetadataUtility.TryFindAnchor(ref metadata.Value, in anchorId, out _);
                MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.Value, new int2(index & 1, (index >> 1) & 1), out _);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero);
        }
        finally
        {
            metadata.Dispose();
            surface.Dispose();
        }
    }

    private static void CreateGrid(EntityManager entityManager)
    {
        Entity grid = entityManager.CreateEntity(typeof(GridConfig));
        entityManager.SetComponentData(grid, new GridConfig
        {
            Origin = float3.zero,
            Width = 100,
            Height = 100,
            CellSize = 1f
        });
    }

    private static void CreateActiveMap(
        EntityManager entityManager,
        BlobAssetReference<OperationMapBlob> metadata,
        bool includeBounds)
    {
        Entity root = includeBounds
            ? entityManager.CreateEntity(typeof(ActiveOperationMapComponent), typeof(OperationMapBoundsComponent))
            : entityManager.CreateEntity(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
        entityManager.SetComponentData(root, new ActiveOperationMapComponent
        {
            OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01"),
            Generation = 1
        });

        if (includeBounds)
        {
            entityManager.SetComponentData(root, new OperationMapBoundsComponent
            {
                CameraMin = new float3(20f, 0f, 30f),
                CameraMax = new float3(80f, 100f, 90f)
            });
            return;
        }

        entityManager.SetComponentData(root, new OperationMapMetadataComponent
        {
            Blob = metadata,
            Generation = 1
        });
    }

    private static BlobAssetReference<OperationMapBlob> CreateOperationMapBlob()
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
        root.OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01");
        root.Minimap = new OperationMapMinimapBlob
        {
            Id = new FixedString64Bytes("minimap.skirmish.projection"),
            ProjectionOrigin = new float3(-100f, 0f, -50f),
            ProjectionSize = new float2(200f, 100f),
            OrientationDegrees = 0f
        };
        BlobBuilderArray<OperationMapAnchorBlob> anchors = builder.Allocate(ref root.Anchors, 1);
        anchors[0] = new OperationMapAnchorBlob
        {
            Id = new FixedString64Bytes("anchor.skirmish.objective.alpha"),
            Kind = OperationMapAnchorKind.Objective,
            Position = new float3(20f, 0f, 30f),
            Rotation = quaternion.identity,
            Radius = 5f,
            FactionId = -1,
            LaneIndex = -1
        };
        builder.Allocate(ref root.Cameras, 0);
        return builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
    }

    private static BlobAssetReference<MapSurfaceBlob> CreateCompactSurfaceBlob()
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.GridOrigin = float3.zero;
        root.CellSize = 1f;
        root.Dimensions = new int2(2, 2);
        root.RuntimeEncoding = MapSurfaceRuntimeEncoding.SingleLayerCompact;
        root.CompactMinHeight = 2f;
        root.CompactHeightStep = 0.25f;
        builder.Allocate(ref root.Cells, 0);
        builder.Allocate(ref root.Samples, 0);
        builder.Allocate(ref root.Connections, 0);
        BlobBuilderArray<MapSurfaceCompactSample> samples = builder.Allocate(ref root.CompactSamples, 4);
        for (int index = 0; index < samples.Length; index++)
        {
            samples[index] = new MapSurfaceCompactSample
            {
                PackedHeight = (ushort)index,
                LayerId = 0,
                MovementMask = MapSurfaceMovementMask.AllGroundUnits,
                NormalY = sbyte.MaxValue,
                SurfaceType = MapSurfaceType.Terrain
            };
        }
        return builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
    }
}
