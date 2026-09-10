using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
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
