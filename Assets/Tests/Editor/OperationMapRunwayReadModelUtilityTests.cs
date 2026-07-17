using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class OperationMapRunwayReadModelUtilityTests
{
    [Test]
    public void ActiveRunway_ProjectsExactReadModelAndOwnedFaction()
    {
        using World world = new("OperationMapRunwayReadModelUtilityTests.Active");
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(
            new RunwayRecord("anchor.runway.faction_1.lane_0", 1, 0, new float3(20f, 1f, 30f), 24f));
        try
        {
            CreateActiveRoot(world.EntityManager, blob, 7);
            DynamicBuffer<BuildingFactionRunwayReadModel> buffer = CreateBuffer(world.EntityManager);
            GridConfig grid = CreateGrid();

            Assert.That(OperationMapRunwayReadModelUtility.TryAppendRunways(
                world.EntityManager,
                buffer,
                in grid,
                out FixedList512Bytes<byte> factions,
                out bool hasActiveMap,
                out string error), Is.True, error);

            Assert.That(hasActiveMap, Is.True);
            Assert.That(buffer.Length, Is.EqualTo(1));
            Assert.That(factions.Length, Is.EqualTo(1));
            Assert.That(factions[0], Is.EqualTo(1));
            BuildingFactionRunwayReadModel runway = buffer[0];
            Assert.That(runway.FactionId, Is.EqualTo(1));
            Assert.That(runway.BuildingId.ToString(), Is.EqualTo("anchor.runway.faction_1.lane_0"));
            Assert.That(runway.BuildingRuntimeId, Is.Zero);
            Assert.That(runway.Center, Is.EqualTo(new float3(20f, 1f, 30f)));
            Assert.That(runway.Direction, Is.EqualTo(new float3(0f, 0f, 1f)));
            Assert.That(runway.TakeoffPosition, Is.EqualTo(new float3(20f, 1f, 6f)));
            Assert.That(runway.LandingPosition, Is.EqualTo(new float3(20f, 1f, 54f)));
            Assert.That(runway.TakeoffCell, Is.EqualTo(GridUtils.WorldToCell(in grid, runway.TakeoffPosition)));
            Assert.That(runway.LandingCell, Is.EqualTo(GridUtils.WorldToCell(in grid, runway.LandingPosition)));
            Assert.That(runway.HalfExtents, Is.EqualTo(new float2(1f, 24f)));
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void NoActiveMap_PreservesCompatibilityDestination()
    {
        using World world = new("OperationMapRunwayReadModelUtilityTests.Compatibility");
        DynamicBuffer<BuildingFactionRunwayReadModel> buffer = CreateBuffer(world.EntityManager);
        buffer.Add(new BuildingFactionRunwayReadModel { FactionId = 3, BuildingRuntimeId = 42 });
        GridConfig grid = CreateGrid();

        Assert.That(OperationMapRunwayReadModelUtility.TryAppendRunways(
            world.EntityManager,
            buffer,
            in grid,
            out FixedList512Bytes<byte> factions,
            out bool hasActiveMap,
            out string error), Is.True, error);

        Assert.That(hasActiveMap, Is.False);
        Assert.That(factions.Length, Is.Zero);
        Assert.That(buffer.Length, Is.EqualTo(1));
        Assert.That(buffer[0].BuildingRuntimeId, Is.EqualTo(42));
        Assert.That(OperationMapRunwayReadModelUtility.ResolveGenerationSignature(world.EntityManager), Is.Zero);
    }

    [Test]
    public void MapOwnedFaction_RemovesOnlyItsBuildingFallbacks()
    {
        using World world = new("OperationMapRunwayReadModelUtilityTests.Fallbacks");
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(
            new RunwayRecord("anchor.runway.faction_1.lane_0", 1, 0, new float3(20f, 1f, 30f), 24f));
        try
        {
            CreateActiveRoot(world.EntityManager, blob, 2);
            DynamicBuffer<BuildingFactionRunwayReadModel> buffer = CreateBuffer(world.EntityManager);
            GridConfig grid = CreateGrid();
            Assert.That(OperationMapRunwayReadModelUtility.TryAppendRunways(
                world.EntityManager,
                buffer,
                in grid,
                out FixedList512Bytes<byte> factions,
                out _,
                out string error), Is.True, error);

            int mapCount = buffer.Length;
            buffer.Add(new BuildingFactionRunwayReadModel { FactionId = 1, BuildingRuntimeId = 11 });
            buffer.Add(new BuildingFactionRunwayReadModel { FactionId = 0, BuildingRuntimeId = 12 });
            OperationMapRunwayReadModelUtility.RemoveBuildingFallbacks(buffer, mapCount, in factions);

            Assert.That(buffer.Length, Is.EqualTo(2));
            Assert.That(buffer[0].BuildingRuntimeId, Is.Zero);
            Assert.That(buffer[1].FactionId, Is.Zero);
            Assert.That(buffer[1].BuildingRuntimeId, Is.EqualTo(12));
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void DuplicateRunway_FailsClosedWithoutPublishingPartialData()
    {
        using World world = new("OperationMapRunwayReadModelUtilityTests.Duplicate");
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(
            new RunwayRecord("anchor.runway.first", 1, 0, new float3(20f, 1f, 30f), 24f),
            new RunwayRecord("anchor.runway.duplicate", 1, 0, new float3(40f, 1f, 30f), 24f));
        try
        {
            CreateActiveRoot(world.EntityManager, blob, 3);
            DynamicBuffer<BuildingFactionRunwayReadModel> buffer = CreateBuffer(world.EntityManager);
            buffer.Add(new BuildingFactionRunwayReadModel { FactionId = 9, BuildingRuntimeId = 99 });
            GridConfig grid = CreateGrid();

            Assert.That(OperationMapRunwayReadModelUtility.TryAppendRunways(
                world.EntityManager,
                buffer,
                in grid,
                out FixedList512Bytes<byte> factions,
                out bool hasActiveMap,
                out string error), Is.False);

            Assert.That(hasActiveMap, Is.True);
            Assert.That(error, Does.Contain("duplicate runway anchors"));
            Assert.That(factions.Length, Is.Zero);
            Assert.That(buffer.Length, Is.EqualTo(1));
            Assert.That(buffer[0].BuildingRuntimeId, Is.EqualTo(99));
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void GenerationSignature_ChangesAcrossActivationAndTeardown()
    {
        using World world = new("OperationMapRunwayReadModelUtilityTests.Generation");
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(
            new RunwayRecord("anchor.runway.faction_1.lane_0", 1, 0, new float3(20f, 1f, 30f), 24f));
        try
        {
            Assert.That(OperationMapRunwayReadModelUtility.ResolveGenerationSignature(world.EntityManager), Is.Zero);
            Entity root = CreateActiveRoot(world.EntityManager, blob, 4);
            int activeSignature = OperationMapRunwayReadModelUtility.ResolveGenerationSignature(world.EntityManager);
            Assert.That(activeSignature, Is.Not.Zero);

            ActiveOperationMapComponent active = world.EntityManager.GetComponentData<ActiveOperationMapComponent>(root);
            OperationMapMetadataComponent metadata = world.EntityManager.GetComponentData<OperationMapMetadataComponent>(root);
            active.Generation = 5;
            metadata.Generation = 5;
            world.EntityManager.SetComponentData(root, active);
            world.EntityManager.SetComponentData(root, metadata);
            Assert.That(OperationMapRunwayReadModelUtility.ResolveGenerationSignature(world.EntityManager), Is.Not.EqualTo(activeSignature));

            world.EntityManager.DestroyEntity(root);
            Assert.That(OperationMapRunwayReadModelUtility.ResolveGenerationSignature(world.EntityManager), Is.Zero);
        }
        finally
        {
            blob.Dispose();
        }
    }

    private readonly struct RunwayRecord
    {
        public readonly FixedString64Bytes Id;
        public readonly int FactionId;
        public readonly int LaneIndex;
        public readonly float3 Position;
        public readonly float Radius;

        public RunwayRecord(string id, int factionId, int laneIndex, float3 position, float radius)
        {
            Id = new FixedString64Bytes(id);
            FactionId = factionId;
            LaneIndex = laneIndex;
            Position = position;
            Radius = radius;
        }
    }

    private static GridConfig CreateGrid() => new()
    {
        Width = 100,
        Height = 100,
        CellSize = 2f,
        Origin = new float3(-100f, 0f, -100f)
    };

    private static DynamicBuffer<BuildingFactionRunwayReadModel> CreateBuffer(EntityManager entityManager)
    {
        Entity entity = entityManager.CreateEntity();
        return entityManager.AddBuffer<BuildingFactionRunwayReadModel>(entity);
    }

    private static Entity CreateActiveRoot(
        EntityManager entityManager,
        BlobAssetReference<OperationMapBlob> blob,
        int generation)
    {
        Entity root = entityManager.CreateEntity(
            typeof(OperationMapRootComponent),
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent));
        entityManager.SetComponentData(root, new ActiveOperationMapComponent
        {
            OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01"),
            Generation = generation
        });
        entityManager.SetComponentData(root, new OperationMapMetadataComponent
        {
            Blob = blob,
            Generation = generation
        });
        return root;
    }

    private static BlobAssetReference<OperationMapBlob> CreateBlob(params RunwayRecord[] source)
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
        root.OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01");
        root.Grid = new OperationMapGridBlob
        {
            Origin = new float3(-100f, 0f, -100f),
            Dimensions = new int2(100, 100),
            CellSize = 2f
        };
        BlobBuilderArray<OperationMapAnchorBlob> anchors = builder.Allocate(ref root.Anchors, source.Length);
        for (int index = 0; index < source.Length; index++)
        {
            RunwayRecord runway = source[index];
            anchors[index] = new OperationMapAnchorBlob
            {
                Id = runway.Id,
                Kind = OperationMapAnchorKind.Runway,
                Position = runway.Position,
                Rotation = quaternion.identity,
                Radius = runway.Radius,
                FactionId = runway.FactionId,
                LaneIndex = runway.LaneIndex
            };
        }

        builder.Allocate(ref root.Cameras, 0);
        return builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
    }
}
