using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class OperationMapHelipadReadModelUtilityTests
{
    [Test]
    public void ActiveHelipads_BindExactCentersAndKeepOnlyOwnedRuntimeBuildings()
    {
        using World world = new("OperationMapHelipadReadModelUtilityTests.Active");
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(
            new HelipadRecord("anchor.helipad.faction_1.lane_0", 1, 0, new float3(20f, 1f, 30f), 6f));
        try
        {
            CreateActiveRoot(world.EntityManager, blob, 7);
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> buffer = CreateBuffer(world.EntityManager);
            GridConfig grid = CreateGrid();
            buffer.Add(CreateRow(1, "Building_Helipad", 11, 0, new float3(20.1f, 1f, 30f), in grid));
            buffer.Add(CreateRow(1, "Building_Helipad", 12, 0, new float3(50f, 1f, 30f), in grid));
            buffer.Add(CreateRow(0, "Building_Helipad", 13, 0, new float3(50f, 1f, 40f), in grid));
            buffer.Add(CreateRow(1, "Building_Airport", 14, 0, new float3(60f, 1f, 40f), in grid));

            Assert.That(OperationMapHelipadReadModelUtility.TryBind(
                world.EntityManager,
                buffer,
                in grid,
                out bool hasActiveMap,
                out string error), Is.True, error);

            Assert.That(hasActiveMap, Is.True);
            Assert.That(buffer.Length, Is.EqualTo(3));
            Assert.That(buffer[0].BuildingRuntimeId, Is.EqualTo(11));
            Assert.That(buffer[0].WorldPosition, Is.EqualTo(new float3(20f, 1f, 30f)));
            Assert.That(buffer[0].Cell, Is.EqualTo(GridUtils.WorldToCell(in grid, buffer[0].WorldPosition)));
            Assert.That(buffer[1].BuildingRuntimeId, Is.EqualTo(13));
            Assert.That(buffer[2].BuildingRuntimeId, Is.EqualTo(14));
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void NoActiveMap_PreservesCompatibilityRows()
    {
        using World world = new("OperationMapHelipadReadModelUtilityTests.Compatibility");
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> buffer = CreateBuffer(world.EntityManager);
        GridConfig grid = CreateGrid();
        buffer.Add(CreateRow(1, "Building_Helipad", 21, 0, new float3(20f, 1f, 30f), in grid));

        Assert.That(OperationMapHelipadReadModelUtility.TryBind(
            world.EntityManager,
            buffer,
            in grid,
            out bool hasActiveMap,
            out string error), Is.True, error);

        Assert.That(hasActiveMap, Is.False);
        Assert.That(buffer.Length, Is.EqualTo(1));
        Assert.That(buffer[0].BuildingRuntimeId, Is.EqualTo(21));
    }

    [Test]
    public void MissingRuntimeOwner_FailsWithoutChangingRows()
    {
        using World world = new("OperationMapHelipadReadModelUtilityTests.Missing");
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(
            new HelipadRecord("anchor.helipad.missing", 1, 0, new float3(20f, 1f, 30f), 6f));
        try
        {
            CreateActiveRoot(world.EntityManager, blob, 2);
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> buffer = CreateBuffer(world.EntityManager);
            GridConfig grid = CreateGrid();
            buffer.Add(CreateRow(1, "Building_Helipad", 31, 0, new float3(50f, 1f, 30f), in grid));

            Assert.That(OperationMapHelipadReadModelUtility.TryBind(
                world.EntityManager,
                buffer,
                in grid,
                out bool hasActiveMap,
                out string error), Is.False);

            Assert.That(hasActiveMap, Is.True);
            Assert.That(error, Does.Contain("no unique runtime production-slot owner"));
            Assert.That(buffer.Length, Is.EqualTo(1));
            Assert.That(buffer[0].BuildingRuntimeId, Is.EqualTo(31));
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void DuplicateFactionLane_FailsWithoutPublishingPartialChanges()
    {
        using World world = new("OperationMapHelipadReadModelUtilityTests.Duplicate");
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(
            new HelipadRecord("anchor.helipad.first", 1, 0, new float3(20f, 1f, 30f), 6f),
            new HelipadRecord("anchor.helipad.duplicate", 1, 0, new float3(40f, 1f, 30f), 6f));
        try
        {
            CreateActiveRoot(world.EntityManager, blob, 3);
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> buffer = CreateBuffer(world.EntityManager);
            GridConfig grid = CreateGrid();
            buffer.Add(CreateRow(1, "Building_Helipad", 41, 0, new float3(20f, 1f, 30f), in grid));
            buffer.Add(CreateRow(1, "Building_Helipad", 42, 0, new float3(40f, 1f, 30f), in grid));

            Assert.That(OperationMapHelipadReadModelUtility.TryBind(
                world.EntityManager,
                buffer,
                in grid,
                out _,
                out string error), Is.False);

            Assert.That(error, Does.Contain("duplicate helipad anchors"));
            Assert.That(buffer.Length, Is.EqualTo(2));
            Assert.That(buffer[0].BuildingRuntimeId, Is.EqualTo(41));
            Assert.That(buffer[1].BuildingRuntimeId, Is.EqualTo(42));
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void TwoAnchorsResolvingToOneBuilding_FailClosed()
    {
        using World world = new("OperationMapHelipadReadModelUtilityTests.AmbiguousBuilding");
        BlobAssetReference<OperationMapBlob> blob = CreateBlob(
            new HelipadRecord("anchor.helipad.first", 1, 0, new float3(20f, 1f, 30f), 6f),
            new HelipadRecord("anchor.helipad.second", 1, 1, new float3(20f, 1f, 30f), 6f));
        try
        {
            CreateActiveRoot(world.EntityManager, blob, 4);
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> buffer = CreateBuffer(world.EntityManager);
            GridConfig grid = CreateGrid();
            buffer.Add(CreateRow(1, "Building_Helipad", 51, 0, new float3(20f, 1f, 30f), in grid));

            Assert.That(OperationMapHelipadReadModelUtility.TryBind(
                world.EntityManager,
                buffer,
                in grid,
                out _,
                out string error), Is.False);

            Assert.That(error, Does.Contain("already-bound runtime building"));
            Assert.That(buffer.Length, Is.EqualTo(1));
            Assert.That(buffer[0].BuildingRuntimeId, Is.EqualTo(51));
        }
        finally
        {
            blob.Dispose();
        }
    }

    private readonly struct HelipadRecord
    {
        public readonly FixedString64Bytes Id;
        public readonly int FactionId;
        public readonly int LaneIndex;
        public readonly float3 Position;
        public readonly float Radius;

        public HelipadRecord(string id, int factionId, int laneIndex, float3 position, float radius)
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

    private static BuildingFactionProductionSpawnPointReadModel CreateRow(
        byte factionId,
        string buildingId,
        int runtimeId,
        int slotIndex,
        float3 position,
        in GridConfig grid) => new()
    {
        FactionId = factionId,
        BuildingId = new FixedString128Bytes(
            BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId)),
        BuildingRuntimeId = runtimeId,
        SlotIndex = slotIndex,
        Cell = GridUtils.WorldToCell(in grid, position),
        WorldPosition = position
    };

    private static DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> CreateBuffer(EntityManager entityManager)
    {
        Entity entity = entityManager.CreateEntity();
        return entityManager.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(entity);
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

    private static BlobAssetReference<OperationMapBlob> CreateBlob(params HelipadRecord[] source)
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
            HelipadRecord helipad = source[index];
            anchors[index] = new OperationMapAnchorBlob
            {
                Id = helipad.Id,
                Kind = OperationMapAnchorKind.Helipad,
                Position = helipad.Position,
                Rotation = quaternion.identity,
                Radius = helipad.Radius,
                FactionId = helipad.FactionId,
                LaneIndex = helipad.LaneIndex
            };
        }

        builder.Allocate(ref root.Cameras, 0);
        return builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
    }
}
