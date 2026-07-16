using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class OperationMapMetadataUtilityTests
{
    [Test]
    public void AnchorAndCameraLookup_ReturnExactTypedRecords()
    {
        BlobAssetReference<OperationMapBlob> blob = CreateMetadataBlob();
        try
        {
            ref OperationMapBlob metadata = ref blob.Value;
            FixedString64Bytes anchorId = new("anchor.skirmish.objective.alpha");
            FixedString64Bytes cameraId = new("camera.skirmish.battle");

            Assert.That(OperationMapMetadataUtility.TryFindAnchor(ref metadata, in anchorId, out OperationMapAnchorBlob anchor), Is.True);
            Assert.That(anchor.Kind, Is.EqualTo(OperationMapAnchorKind.Objective));
            Assert.That(anchor.Position, Is.EqualTo(new float3(20f, 0f, 30f)));
            Assert.That(OperationMapMetadataUtility.TryFindCamera(ref metadata, in cameraId, out OperationMapCameraBlob camera), Is.True);
            Assert.That(camera.IsOrthographic, Is.EqualTo(1));

            FixedString64Bytes missing = new("anchor.skirmish.objective.missing");
            Assert.That(OperationMapMetadataUtility.TryFindAnchor(ref metadata, in missing, out _), Is.False);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void BoundsQueriesAndClamp_AreInclusiveAndDeterministic()
    {
        OperationMapBoundsComponent bounds = CreateBounds();

        Assert.That(OperationMapMetadataUtility.IsInsideWorldBounds(in bounds, new float3(-100f, -10f, -100f)), Is.True);
        Assert.That(OperationMapMetadataUtility.IsInsidePlayableBounds(in bounds, new float3(95f, 0f, 0f)), Is.False);
        Assert.That(OperationMapMetadataUtility.IsInsideCameraBounds(in bounds, new float3(0f, 45f, 0f)), Is.False);
        Assert.That(
            OperationMapMetadataUtility.ClampToCameraBounds(in bounds, new float3(120f, 50f, -120f)),
            Is.EqualTo(new float3(80f, 40f, -80f)));
    }

    [Test]
    public void ActiveGridConfig_ResolvesExactMetadataAndRejectsGenerationMismatch()
    {
        using World world = new("OperationMapMetadataUtilityTests.ActiveGrid");
        EntityManager entityManager = world.EntityManager;
        BlobAssetReference<OperationMapBlob> blob = CreateMetadataBlob();
        Entity root = entityManager.CreateEntity(
            typeof(OperationMapRootComponent),
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent));
        entityManager.SetComponentData(root, new ActiveOperationMapComponent
        {
            OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01"),
            Generation = 4
        });
        entityManager.SetComponentData(root, new OperationMapMetadataComponent
        {
            Blob = blob,
            Generation = 4
        });

        Assert.That(OperationMapMetadataUtility.TryResolveActiveGridConfig(
            entityManager,
            out GridConfig grid,
            out bool hasActiveMap,
            out string error), Is.True, error);
        Assert.That(hasActiveMap, Is.True);
        Assert.That(grid.Width, Is.EqualTo(320));
        Assert.That(grid.Height, Is.EqualTo(180));
        Assert.That(grid.CellSize, Is.EqualTo(2f));
        Assert.That(grid.Origin, Is.EqualTo(new float3(-10f, 0f, -20f)));

        OperationMapMetadataComponent stale = entityManager.GetComponentData<OperationMapMetadataComponent>(root);
        stale.Generation = 3;
        entityManager.SetComponentData(root, stale);
        Assert.That(OperationMapMetadataUtility.TryResolveActiveGridConfig(
            entityManager,
            out _,
            out hasActiveMap,
            out error), Is.False);
        Assert.That(hasActiveMap, Is.True);
        Assert.That(error, Does.Contain("different generation"));
        blob.Dispose();
    }

    [Test]
    public void ActiveGridConfig_NoRootPermitsCompatibilityFallback()
    {
        using World world = new("OperationMapMetadataUtilityTests.NoActiveGrid");

        Assert.That(OperationMapMetadataUtility.TryResolveActiveGridConfig(
            world.EntityManager,
            out _,
            out bool hasActiveMap,
            out string error), Is.False);
        Assert.That(hasActiveMap, Is.False);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void ActiveGridConfig_DuplicateRootsFailClosed()
    {
        using World world = new("OperationMapMetadataUtilityTests.DuplicateGrid");
        world.EntityManager.CreateEntity(typeof(OperationMapRootComponent));
        world.EntityManager.CreateEntity(typeof(OperationMapRootComponent));

        Assert.That(OperationMapMetadataUtility.TryResolveActiveGridConfig(
            world.EntityManager,
            out _,
            out bool hasActiveMap,
            out string error), Is.False);
        Assert.That(hasActiveMap, Is.True);
        Assert.That(error, Does.Contain("exactly one"));
    }

    [Test]
    public void ActiveGridConfig_InvalidGridFailsClosed()
    {
        using World world = new("OperationMapMetadataUtilityTests.InvalidGrid");
        EntityManager entityManager = world.EntityManager;
        BlobAssetReference<OperationMapBlob> blob = CreateMetadataBlob(width: 0);
        Entity root = entityManager.CreateEntity(
            typeof(OperationMapRootComponent),
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent));
        entityManager.SetComponentData(root, new ActiveOperationMapComponent
        {
            OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01"),
            Generation = 1
        });
        entityManager.SetComponentData(root, new OperationMapMetadataComponent
        {
            Blob = blob,
            Generation = 1
        });

        Assert.That(OperationMapMetadataUtility.TryResolveActiveGridConfig(
            entityManager,
            out _,
            out bool hasActiveMap,
            out string error), Is.False);
        Assert.That(hasActiveMap, Is.True);
        Assert.That(error, Does.Contain("grid metadata is invalid"));
        blob.Dispose();
    }

    [Test]
    public void ActiveSurfaceMetadata_ResolvesTypedSurfaceAndGrid()
    {
        using World world = new("OperationMapMetadataUtilityTests.ActiveSurface");
        EntityManager entityManager = world.EntityManager;
        BlobAssetReference<OperationMapBlob> blob = CreateMetadataBlob();
        Entity root = entityManager.CreateEntity(
            typeof(OperationMapRootComponent),
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent));
        entityManager.SetComponentData(root, new ActiveOperationMapComponent
        {
            OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01"),
            Generation = 2
        });
        entityManager.SetComponentData(root, new OperationMapMetadataComponent
        {
            Blob = blob,
            Generation = 2
        });

        Assert.That(OperationMapMetadataUtility.TryResolveActiveSurfaceMetadata(
            entityManager,
            out OperationMapSurfaceMetadataBlob surface,
            out OperationMapGridBlob grid,
            out bool hasActiveMap,
            out string error), Is.True, error);
        Assert.That(hasActiveMap, Is.True);
        Assert.That(surface.SurfaceCount, Is.EqualTo(57_600));
        Assert.That(surface.RuntimeBlobHash.ToString(), Is.EqualTo("0123456789abcdef0123456789abcdef"));
        Assert.That(grid.Dimensions, Is.EqualTo(new int2(320, 180)));
        blob.Dispose();
    }

    [Test]
    public void ActiveInfrastructureAnchor_ResolvesExactTypedRecord()
    {
        using World world = new("OperationMapMetadataUtilityTests.Infrastructure");
        BlobAssetReference<OperationMapBlob> blob = CreateInfrastructureMetadataBlob(
            new InfrastructureAnchor(OperationMapAnchorKind.Runway, 1, 2, new float3(20f, 1f, 30f), 24f));
        CreateActiveRoot(world.EntityManager, blob);

        Assert.That(OperationMapMetadataUtility.TryResolveActiveInfrastructureAnchor(
            world.EntityManager,
            OperationMapAnchorKind.Runway,
            1,
            2,
            out OperationMapAnchorBlob anchor,
            out bool hasActiveMap,
            out bool hasMatchingAnchor,
            out string error), Is.True, error);
        Assert.That(hasActiveMap, Is.True);
        Assert.That(hasMatchingAnchor, Is.True);
        Assert.That(anchor.Position, Is.EqualTo(new float3(20f, 1f, 30f)));
        Assert.That(anchor.Radius, Is.EqualTo(24f));
        blob.Dispose();
    }

    [Test]
    public void ActiveInfrastructureAnchor_MissingRecordPermitsCompatibilityFallback()
    {
        using World world = new("OperationMapMetadataUtilityTests.InfrastructureFallback");
        BlobAssetReference<OperationMapBlob> blob = CreateInfrastructureMetadataBlob(
            new InfrastructureAnchor(OperationMapAnchorKind.Runway, 1, 2, new float3(20f, 1f, 30f), 24f));
        CreateActiveRoot(world.EntityManager, blob);

        Assert.That(OperationMapMetadataUtility.TryResolveActiveInfrastructureAnchor(
            world.EntityManager,
            OperationMapAnchorKind.Helipad,
            1,
            2,
            out _,
            out bool hasActiveMap,
            out bool hasMatchingAnchor,
            out string error), Is.False);
        Assert.That(hasActiveMap, Is.True);
        Assert.That(hasMatchingAnchor, Is.False);
        Assert.That(error, Is.Null);
        blob.Dispose();
    }

    [Test]
    public void ActiveInfrastructureAnchor_DuplicateRecordFailsClosed()
    {
        using World world = new("OperationMapMetadataUtilityTests.InfrastructureDuplicate");
        InfrastructureAnchor runway = new(OperationMapAnchorKind.Runway, 1, 2, new float3(20f, 1f, 30f), 24f);
        BlobAssetReference<OperationMapBlob> blob = CreateInfrastructureMetadataBlob(runway, runway);
        CreateActiveRoot(world.EntityManager, blob);

        Assert.That(OperationMapMetadataUtility.TryResolveActiveInfrastructureAnchor(
            world.EntityManager,
            OperationMapAnchorKind.Runway,
            1,
            2,
            out _,
            out bool hasActiveMap,
            out bool hasMatchingAnchor,
            out string error), Is.False);
        Assert.That(hasActiveMap, Is.True);
        Assert.That(hasMatchingAnchor, Is.True);
        Assert.That(error, Does.Contain("ambiguous"));
        blob.Dispose();
    }

    [TestCase(0f, 20f, 30f, 0f, "invalid")]
    [TestCase(1000f, 20f, 30f, 10f, "outside")]
    public void ActiveInfrastructureAnchor_InvalidGeometryFailsClosed(
        float x,
        float y,
        float z,
        float radius,
        string expectedError)
    {
        using World world = new("OperationMapMetadataUtilityTests.InfrastructureInvalid");
        BlobAssetReference<OperationMapBlob> blob = CreateInfrastructureMetadataBlob(
            new InfrastructureAnchor(OperationMapAnchorKind.Helipad, 2, 0, new float3(x, y, z), radius));
        CreateActiveRoot(world.EntityManager, blob);

        Assert.That(OperationMapMetadataUtility.TryResolveActiveInfrastructureAnchor(
            world.EntityManager,
            OperationMapAnchorKind.Helipad,
            2,
            0,
            out _,
            out bool hasActiveMap,
            out bool hasMatchingAnchor,
            out string error), Is.False);
        Assert.That(hasActiveMap, Is.True);
        Assert.That(hasMatchingAnchor, Is.True);
        Assert.That(error, Does.Contain(expectedError));
        blob.Dispose();
    }

    [Test]
    public void MinimapProjection_ZeroRotationMatchesCurrentLowerLeftXZContract()
    {
        OperationMapMinimapBlob projection = new()
        {
            ProjectionOrigin = new float3(-100f, 3f, -50f),
            ProjectionSize = new float2(200f, 100f),
            OrientationDegrees = 0f
        };

        Assert.That(OperationMapMetadataUtility.TryWorldToMinimapNormalized(
            in projection,
            new float3(0f, 20f, 0f),
            out float2 normalized), Is.True);
        Assert.That(normalized, Is.EqualTo(new float2(0.5f, 0.5f)));
        Assert.That(OperationMapMetadataUtility.IsInsideNormalizedProjection(normalized), Is.True);

        Assert.That(OperationMapMetadataUtility.TryMinimapNormalizedToWorldClamped(
            in projection,
            new float2(1.5f, -0.5f),
            7f,
            out float3 world), Is.True);
        Assert.That(world, Is.EqualTo(new float3(100f, 7f, -50f)));
    }

    [Test]
    public void MinimapProjection_RotatedRoundTripPreservesWorldXZ()
    {
        OperationMapMinimapBlob projection = new()
        {
            ProjectionOrigin = new float3(10f, 0f, 20f),
            ProjectionSize = new float2(80f, 40f),
            OrientationDegrees = 90f
        };
        float2 expectedNormalized = new(0.25f, 0.75f);

        Assert.That(OperationMapMetadataUtility.TryMinimapNormalizedToWorldClamped(
            in projection,
            expectedNormalized,
            5f,
            out float3 world), Is.True);
        Assert.That(OperationMapMetadataUtility.TryWorldToMinimapNormalized(
            in projection,
            world,
            out float2 actualNormalized), Is.True);
        Assert.That(math.distance(actualNormalized, expectedNormalized), Is.LessThan(0.0001f));
    }

    [Test]
    public void InvalidProjection_FailsClosed()
    {
        OperationMapMinimapBlob zeroWidth = new()
        {
            ProjectionSize = new float2(0f, 100f)
        };
        Assert.That(OperationMapMetadataUtility.TryWorldToMinimapNormalized(
            in zeroWidth,
            float3.zero,
            out _), Is.False);

        OperationMapMinimapBlob nonFinite = new()
        {
            ProjectionSize = new float2(100f, 100f),
            OrientationDegrees = float.NaN
        };
        Assert.That(OperationMapMetadataUtility.TryMinimapNormalizedToWorldClamped(
            in nonFinite,
            float2.zero,
            0f,
            out _), Is.False);
    }

    [Test]
    public void LookupBoundsAndProjection_HaveZeroManagedAllocationsAfterWarmup()
    {
        BlobAssetReference<OperationMapBlob> blob = CreateMetadataBlob();
        try
        {
            ref OperationMapBlob metadata = ref blob.Value;
            FixedString64Bytes anchorId = new("anchor.skirmish.objective.alpha");
            OperationMapBoundsComponent bounds = CreateBounds();
            OperationMapMinimapBlob projection = metadata.Minimap;
            OperationMapMetadataUtility.TryFindAnchor(ref metadata, in anchorId, out _);
            OperationMapMetadataUtility.TryWorldToMinimapNormalized(in projection, float3.zero, out _);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 1024; index++)
            {
                OperationMapMetadataUtility.TryFindAnchor(ref metadata, in anchorId, out _);
                OperationMapMetadataUtility.ClampToCameraBounds(in bounds, new float3(index, 20f, -index));
                OperationMapMetadataUtility.TryWorldToMinimapNormalized(in projection, float3.zero, out _);
            }
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(after - before, Is.Zero);
        }
        finally
        {
            blob.Dispose();
        }
    }

    private static OperationMapBoundsComponent CreateBounds() => new()
    {
        WorldMin = new float3(-100f, -10f, -100f),
        WorldMax = new float3(100f, 50f, 100f),
        PlayableMin = new float3(-90f, -5f, -90f),
        PlayableMax = new float3(90f, 40f, 90f),
        CameraMin = new float3(-80f, 10f, -80f),
        CameraMax = new float3(80f, 40f, 80f)
    };

    private readonly struct InfrastructureAnchor
    {
        public readonly OperationMapAnchorKind Kind;
        public readonly int FactionId;
        public readonly int LaneIndex;
        public readonly float3 Position;
        public readonly float Radius;

        public InfrastructureAnchor(
            OperationMapAnchorKind kind,
            int factionId,
            int laneIndex,
            float3 position,
            float radius)
        {
            Kind = kind;
            FactionId = factionId;
            LaneIndex = laneIndex;
            Position = position;
            Radius = radius;
        }
    }

    private static Entity CreateActiveRoot(
        EntityManager entityManager,
        BlobAssetReference<OperationMapBlob> blob)
    {
        Entity root = entityManager.CreateEntity(
            typeof(OperationMapRootComponent),
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent));
        entityManager.SetComponentData(root, new ActiveOperationMapComponent
        {
            OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01"),
            Generation = 1
        });
        entityManager.SetComponentData(root, new OperationMapMetadataComponent
        {
            Blob = blob,
            Generation = 1
        });
        return root;
    }

    private static BlobAssetReference<OperationMapBlob> CreateInfrastructureMetadataBlob(
        params InfrastructureAnchor[] sourceAnchors)
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
        BlobBuilderArray<OperationMapAnchorBlob> anchors = builder.Allocate(ref root.Anchors, sourceAnchors.Length);
        for (int index = 0; index < sourceAnchors.Length; index++)
        {
            InfrastructureAnchor source = sourceAnchors[index];
            anchors[index] = new OperationMapAnchorBlob
            {
                Id = new FixedString64Bytes($"anchor.infrastructure.{index}"),
                Kind = source.Kind,
                Position = source.Position,
                Rotation = quaternion.identity,
                Radius = source.Radius,
                FactionId = source.FactionId,
                LaneIndex = source.LaneIndex
            };
        }

        builder.Allocate(ref root.Cameras, 0);
        return builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
    }

    private static BlobAssetReference<OperationMapBlob> CreateMetadataBlob(
        int width = 320,
        int height = 180,
        float cellSize = 2f)
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
        root.OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01");
        root.Grid = new OperationMapGridBlob
        {
            Origin = new float3(-10f, 0f, -20f),
            Dimensions = new int2(width, height),
            CellSize = cellSize,
            AuthoredBlockedCellCount = 0
        };
        root.Surface = new OperationMapSurfaceMetadataBlob
        {
            RuntimeBlobHash = new FixedString64Bytes("0123456789abcdef0123456789abcdef"),
            SurfaceCount = math.max(1, width * height),
            PayloadVersion = 3,
            PayloadEncoding = 1,
            MinimumHeight = -5f,
            MaximumHeight = 35f
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
        BlobBuilderArray<OperationMapCameraBlob> cameras = builder.Allocate(ref root.Cameras, 1);
        cameras[0] = new OperationMapCameraBlob
        {
            Id = new FixedString64Bytes("camera.skirmish.battle"),
            Position = new float3(0f, 30f, 0f),
            Rotation = quaternion.identity,
            OrthographicSize = 20f,
            IsOrthographic = 1,
            ClampToCameraBounds = 1
        };
        root.Minimap = new OperationMapMinimapBlob
        {
            Id = new FixedString64Bytes("minimap.skirmish.projection"),
            ProjectionOrigin = new float3(-100f, 0f, -50f),
            ProjectionSize = new float2(200f, 100f),
            OrientationDegrees = 0f
        };
        return builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
    }
}
