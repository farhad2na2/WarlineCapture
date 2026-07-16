using Game.Components;
using Game.Composition;
using Game.UI.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class MatchHudMinimapDataSourceAdapterTests
{
    [Test]
    public void ActiveMapProjectionOverridesLegacyGridExtents()
    {
        using var world = new World("MinimapActiveMapProjectionTests");
        World previous = World.DefaultGameObjectInjectionWorld;
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(new float3(11f, 3f, 17f), new float2(321.5f, 123.25f));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            CreateGrid(world.EntityManager);
            Entity root = world.EntityManager.CreateEntity(
                typeof(ActiveOperationMapComponent),
                typeof(OperationMapMetadataComponent));
            world.EntityManager.SetComponentData(root, new ActiveOperationMapComponent { Generation = 4 });
            world.EntityManager.SetComponentData(root, new OperationMapMetadataComponent
            {
                Blob = blob,
                Generation = 4
            });

            var adapter = new MatchHudMinimapDataSourceAdapter();
            Assert.That(adapter.TryGetGrid(out MatchHudMinimapGridModel model), Is.True);
            Assert.That(model.Origin, Is.EqualTo(new UnityEngine.Vector3(11f, 3f, 17f)));
            Assert.That(model.WorldWidth, Is.EqualTo(321.5f).Within(0.001f));
            Assert.That(model.WorldHeight, Is.EqualTo(123.25f).Within(0.001f));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            blob.Dispose();
        }
    }

    [Test]
    public void MissingActiveMapPreservesLegacyGridProjection()
    {
        using var world = new World("MinimapLegacyGridProjectionTests");
        World previous = World.DefaultGameObjectInjectionWorld;
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            CreateGrid(world.EntityManager);

            var adapter = new MatchHudMinimapDataSourceAdapter();
            Assert.That(adapter.TryGetGrid(out MatchHudMinimapGridModel model), Is.True);
            Assert.That(model.Origin, Is.EqualTo(new UnityEngine.Vector3(5f, 2f, 7f)));
            Assert.That(model.WorldWidth, Is.EqualTo(200f).Within(0.001f));
            Assert.That(model.WorldHeight, Is.EqualTo(100f).Within(0.001f));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
        }
    }

    [Test]
    public void MismatchedGenerationPreservesLegacyGridProjection()
    {
        using var world = new World("MinimapStaleProjectionTests");
        World previous = World.DefaultGameObjectInjectionWorld;
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(float3.zero, new float2(500f, 250f));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            CreateGrid(world.EntityManager);
            Entity root = world.EntityManager.CreateEntity(
                typeof(ActiveOperationMapComponent),
                typeof(OperationMapMetadataComponent));
            world.EntityManager.SetComponentData(root, new ActiveOperationMapComponent { Generation = 2 });
            world.EntityManager.SetComponentData(root, new OperationMapMetadataComponent
            {
                Blob = blob,
                Generation = 1
            });

            var adapter = new MatchHudMinimapDataSourceAdapter();
            Assert.That(adapter.TryGetGrid(out MatchHudMinimapGridModel model), Is.True);
            Assert.That(model.WorldWidth, Is.EqualTo(200f).Within(0.001f));
            Assert.That(model.WorldHeight, Is.EqualTo(100f).Within(0.001f));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            blob.Dispose();
        }
    }

    [Test]
    public void RotatedProjectionPreservesLegacyGridUntilRasterSupportsRotation()
    {
        using var world = new World("MinimapRotatedProjectionTests");
        World previous = World.DefaultGameObjectInjectionWorld;
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(float3.zero, new float2(500f, 250f), 15f);
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            CreateGrid(world.EntityManager);
            Entity root = world.EntityManager.CreateEntity(
                typeof(ActiveOperationMapComponent),
                typeof(OperationMapMetadataComponent));
            world.EntityManager.SetComponentData(root, new ActiveOperationMapComponent { Generation = 3 });
            world.EntityManager.SetComponentData(root, new OperationMapMetadataComponent
            {
                Blob = blob,
                Generation = 3
            });

            var adapter = new MatchHudMinimapDataSourceAdapter();
            Assert.That(adapter.TryGetGrid(out MatchHudMinimapGridModel model), Is.True);
            Assert.That(model.WorldWidth, Is.EqualTo(200f).Within(0.001f));
            Assert.That(model.WorldHeight, Is.EqualTo(100f).Within(0.001f));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            blob.Dispose();
        }
    }

    [Test]
    public void MultipleActiveMapsPreserveLegacyGridProjection()
    {
        using var world = new World("MinimapMultipleActiveMapTests");
        World previous = World.DefaultGameObjectInjectionWorld;
        BlobAssetReference<OperationMapBlob> firstBlob = CreateBlob(float3.zero, new float2(500f, 250f));
        BlobAssetReference<OperationMapBlob> secondBlob = CreateBlob(float3.zero, new float2(700f, 350f));
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            CreateGrid(world.EntityManager);
            CreateActiveMap(world.EntityManager, firstBlob, 1);
            CreateActiveMap(world.EntityManager, secondBlob, 2);

            var adapter = new MatchHudMinimapDataSourceAdapter();
            Assert.That(adapter.TryGetGrid(out MatchHudMinimapGridModel model), Is.True);
            Assert.That(model.WorldWidth, Is.EqualTo(200f).Within(0.001f));
            Assert.That(model.WorldHeight, Is.EqualTo(100f).Within(0.001f));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            firstBlob.Dispose();
            secondBlob.Dispose();
        }
    }

    private static void CreateGrid(EntityManager entityManager)
    {
        Entity grid = entityManager.CreateEntity(typeof(GridConfig));
        entityManager.SetComponentData(grid, new GridConfig
        {
            Origin = new float3(5f, 2f, 7f),
            Width = 20,
            Height = 10,
            CellSize = 10f
        });
    }

    private static void CreateActiveMap(
        EntityManager entityManager,
        BlobAssetReference<OperationMapBlob> blob,
        int generation)
    {
        Entity root = entityManager.CreateEntity(
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent));
        entityManager.SetComponentData(root, new ActiveOperationMapComponent { Generation = generation });
        entityManager.SetComponentData(root, new OperationMapMetadataComponent
        {
            Blob = blob,
            Generation = generation
        });
    }

    private static BlobAssetReference<OperationMapBlob> CreateBlob(
        float3 origin,
        float2 size,
        float orientationDegrees = 0f)
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
        root.Minimap = new OperationMapMinimapBlob
        {
            ProjectionOrigin = origin,
            ProjectionSize = size,
            OrientationDegrees = orientationDegrees
        };
        builder.Allocate(ref root.Anchors, 0);
        builder.Allocate(ref root.Cameras, 0);
        return builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
    }
}
