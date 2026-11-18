using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingPlacementOriginSearchTests
{
    [Test]
    public void HugeBlockedMapUsesBoundedUniqueCandidates()
    {
        var visited = new HashSet<Vector2Int>();
        var preferred = new Vector2Int(1026, 340);
        Assert.IsFalse(BuildingPlacementOriginSearch.TryFind(new RectInt(0, 0, 4096, 4096), preferred,
            p => { Assert.IsTrue(visited.Add(p), "A candidate must not be scanned twice."); return false; }, out var resolved));
        Assert.AreEqual(preferred, resolved);
        Assert.AreEqual(65 * 65, visited.Count, "A blocked map must not cause an exhaustive world scan.");
    }

    [TestCase(0, 0)]
    [TestCase(2, 4)]
    [TestCase(1, 2)]
    [TestCase(-20, 50)]
    public void GridEdgesAreVisitedOnceWithoutClampedDuplicates(int x, int y)
    {
        var bounds = new RectInt(0, 0, 3, 5);
        var visited = new HashSet<Vector2Int>();
        Assert.IsFalse(BuildingPlacementOriginSearch.TryFind(bounds, new Vector2Int(x, y), p =>
        { Assert.IsTrue(bounds.Contains(p)); Assert.IsTrue(visited.Add(p)); return false; }, out var resolved));
        Assert.IsTrue(bounds.Contains(resolved));
        Assert.AreEqual(15, visited.Count);
    }

    [Test]
    public void PreferredAndNearbyValidCellsReturnWithoutScanningFurther()
    {
        var bounds = new RectInt(0, 0, 100, 100);
        var preferred = new Vector2Int(20, 20);
        int calls = 0;
        Assert.IsTrue(BuildingPlacementOriginSearch.TryFind(bounds, preferred, _ => { calls++; return true; }, out var resolved));
        Assert.AreEqual(preferred, resolved); Assert.AreEqual(1, calls);
        var nearby = new Vector2Int(19, 19); calls = 0;
        Assert.IsTrue(BuildingPlacementOriginSearch.TryFind(bounds, preferred, p => { calls++; return p == nearby; }, out resolved));
        Assert.AreEqual(nearby, resolved); Assert.AreEqual(2, calls);
    }

    [Test]
    public void MissionLotWithOneLegalOriginChecksOnlyThatOrigin()
    {
        int checks = 0;
        var origin = new Vector2Int(1006, 330);
        var footprint = new Vector2Int(40, 20);
        var context = Context(footprint, (_, candidate, _, _, _, _, _) =>
        { checks++; Assert.AreEqual(origin, candidate); return false; });
        var search = new BuildingRuntimeSpawnCompositionSystemHelper();
        Assert.IsFalse(search.TryResolveInitialPlacementOrigin(context, new BuildingDefinition(), Vector2Int.zero,
            out var resolved, new RectInt(origin, Vector2Int.one)));
        Assert.AreEqual(origin, resolved); Assert.AreEqual(1, checks);
    }

    [Test]
    public void OversizedFootprintDoesNotSearchOrClaimAValidPlacement()
    {
        var context = Context(new Vector2Int(5000, 20), (_, _, _, _, _, _, _) =>
        { Assert.Fail("Oversized footprints must be rejected before checking candidate cells."); return true; });
        Assert.IsFalse(new BuildingRuntimeSpawnCompositionSystemHelper().TryResolveInitialPlacementOrigin(
            context, new BuildingDefinition(), Vector2Int.zero, out _));
    }

    [Test]
    public void EmptyMissionBoundsDoNotFallBackToWorldSearch()
    {
        var context = Context(new Vector2Int(40, 20), (_, _, _, _, _, _, _) =>
        { Assert.Fail("No world scan is allowed outside the mission's legal bounds."); return true; });
        Assert.IsFalse(new BuildingRuntimeSpawnCompositionSystemHelper().TryResolveInitialPlacementOrigin(
            context, new BuildingDefinition(), Vector2Int.zero, out _, new RectInt(1006, 330, 0, 0)));
    }

    [Test]
    public void CompactRuntimeBuildingStillFindsClearLandBeyondOccupiedCompound()
    {
        var preferred = new Vector2Int(100,100);
        var clear = preferred + new Vector2Int(60,0);
        var context = Context(new Vector2Int(10,10), (_, origin, _, _, _, _, _) => origin == clear);
        var search = new BuildingRuntimeSpawnCompositionSystemHelper();
        Assert.IsTrue(search.TryFindValidInitialBuildingOrigin(context, new BuildingDefinition(), preferred, false,
            new GridConfig { Width=2048, Height=1024, CellSize=1 }, default, default, out var actual));
        Assert.AreEqual(clear, actual);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void S004EnemyRefinerySearchRespectsBakedBuildingPlacement(bool blockRunway)
    {
        using var world = new World("S004 refinery placement");
        var previous = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            var grid = new GridConfig { Origin = new float3(870, 0, 520), Width = 96, Height = 80, CellSize = 1 };
            // Crop the baked grid around the east runway end. Keep its world coordinates.
            var surfaceOrigin = new float3(880, 0, 527);
            var dimensions = new int2(64, 64);
            var runway = new RectInt(10, 23, 30, 34);
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var blobRoot = ref builder.ConstructRoot<MapSurfaceBlob>();
            blobRoot.GridOrigin = surfaceOrigin;
            blobRoot.CellSize = grid.CellSize;
            blobRoot.Dimensions = dimensions;
            blobRoot.RuntimeEncoding = MapSurfaceRuntimeEncoding.SingleLayerCompact;
            builder.Allocate(ref blobRoot.Cells, 0);
            builder.Allocate(ref blobRoot.Samples, 0);
            builder.Allocate(ref blobRoot.Connections, 0);
            var samples = builder.Allocate(ref blobRoot.CompactSamples, dimensions.x * dimensions.y);
            for (int z = 0; z < dimensions.y; z++)
            {
                for (int x = 0; x < dimensions.x; x++)
                {
                    bool onRunway = runway.Contains(new Vector2Int(x + 10, z + 7));
                    var movement = MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.AirGrounded;
                    if (!blockRunway || !onRunway)
                        movement |= MapSurfaceMovementMask.BuildingPlacement;
                    samples[x + z * dimensions.x] = new MapSurfaceCompactSample
                    {
                        SurfaceType = onRunway ? MapSurfaceType.Plaza : MapSurfaceType.Terrain,
                        NormalY = sbyte.MaxValue,
                        MovementMask = movement
                    };
                }
            }

            using var blob = builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
            var em = world.EntityManager;
            em.SetComponentData(em.CreateEntity(typeof(MapSurfaceComponent)), new MapSurfaceComponent
            {
                SurfaceBlob = blob,
                HasSurfaceData = 1,
                GridOrigin = surfaceOrigin,
                Dimensions = dimensions,
                CellSize = grid.CellSize
            });
            var source = new BuildingGameplaySourceCompositionSystemHelper();
            var roads = em.AddBuffer<GridRoad>(em.CreateEntity());
            roads.ResizeUninitialized(grid.Width * grid.Height);
            for (int i = 0; i < roads.Length; i++)
                roads[i] = default;

            var refinery = new BuildingDefinition { DisplayName = "Oil Refinery", FootprintCells = new Vector2Int(25, 15) };
            var preferred = new Vector2Int(24, 42); // World x=894, z=562.
            var context = Context(refinery.FootprintCells, (definition, origin, footprint, rotated, g, r, blockers) =>
                source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(
                    source, definition, origin, footprint, rotated, g, r, blockers,
                    (_, building, cell, _, _) => new RectInt(cell, building.FootprintCells),
                    (_, _) => false));

            var search = new BuildingRuntimeSpawnCompositionSystemHelper();
            Assert.That(new RectInt(preferred, refinery.FootprintCells).Overlaps(runway), Is.True);
            Assert.That(search.TryFindValidInitialBuildingOrigin(context, refinery, preferred, false,
                grid, roads, default, out _, requirePreferredOrigin: true), Is.EqualTo(!blockRunway));
            Assert.That(search.TryFindValidInitialBuildingOrigin(context, refinery, preferred, false,
                grid, roads, default, out var actual), Is.True);
            if (blockRunway)
                Assert.That(new RectInt(actual, refinery.FootprintCells).Overlaps(runway), Is.False);
            else
                Assert.That(actual, Is.EqualTo(preferred));
            Assert.That(search.TryFindValidInitialBuildingOrigin(context, refinery, new Vector2Int(71, 42), false,
                grid, roads, default, out _, requirePreferredOrigin: true), Is.True,
                "Cells beyond the baked surface use the existing grid checks.");
        }
        finally { World.DefaultGameObjectInjectionWorld = previous; }
    }

    [Test]
    public void PlacedAirportOverlapUsesRotatedRunwayAndStoredModelDimensions()
    {
        var instance = new GameObject("Placed airport");
        try
        {
            instance.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var source = new BuildingGameplaySourceCompositionSystemHelper();
            var query = source.BuildingRuntimeQueryCompositionSystemHelper;
            source.RuntimeBuildingSystem.AddBuilding(1, new RuntimeBuildingEntity
            {
                Definition = new BuildingDefinition
                {
                    FootprintCells = new Vector2Int(8, 4), HasRunway = true,
                    RunwayLocalPosition = new Vector3(0, 0, 12),
                    RunwayLocalRotation = Quaternion.identity, RunwayHalfExtents = new Vector3(1, .5f, 4)
                },
                OriginCell = new Vector2Int(20, 20),
                Instance = instance
            });

            bool ReadGrid(BuildingGameplaySourceCompositionSystemHelper _, out Entity entity, out GridConfig grid,
                out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockers)
            {
                entity = Entity.Null;
                grid = new GridConfig { Width = 64, Height = 64, CellSize = 1 };
                roads = default;
                blockers = default;
                return true;
            }

            bool Overlaps(int x, int z) => query.OverlapsAnyRuntimeBuilding(
                source, new RectInt(x, z, 1, 1), ReadGrid, query.GetEffectivePlacementRect);
            Assert.That(Overlaps(39, 22), Is.True, "The far end of the rotated runway must block placement.");
            Assert.That(Overlaps(24, 34), Is.False, "The old runway projection must leave this cell free.");
        }
        finally { Object.DestroyImmediate(instance); }
    }

    private static BuildingRuntimeSpawnCompositionSystemHelper.Context Context(Vector2Int footprint,
        BuildingRuntimeSpawnCompositionSystemHelper.IsPlacementValidDelegate validate)
    {
        bool Grid(out Entity entity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blocked)
        { entity = Entity.Null; grid = new GridConfig { Width = 2048, Height = 1024, CellSize = 1 }; roads = default; blocked = default; return true; }
        return new BuildingRuntimeSpawnCompositionSystemHelper.Context(null, null, null, null, default,
            Grid, (_, _) => footprint, (_, origin, _, _) => new RectInt(origin, footprint), validate,
            null, null, null, null, null);
    }
}
