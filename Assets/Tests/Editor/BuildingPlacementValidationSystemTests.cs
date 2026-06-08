#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingPlacementValidationSystemTests
{
    private World _world;

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
        _world = null;
    }

    [Test]
    public void PlacementRectValidation_RejectsRoadCellsAndRuntimeBuildingOverlap()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        roads[GridUtils.CellToIndex(new Unity.Mathematics.int2(1, 1), grid.Width)] = new GridRoad { Value = 1 };
        DynamicBlockerComponent blockerData = default;

        Assert.IsFalse(BuildingPlacementValidationSystem.IsPlacementRectValid(
            new RectInt(1, 1, 1, 1),
            grid,
            roads,
            blockerData,
            false,
            null,
            0,
            0,
            null,
            null,
            null));

        Assert.IsFalse(BuildingPlacementValidationSystem.IsPlacementRectValid(
            new RectInt(2, 2, 1, 1),
            grid,
            roads,
            blockerData,
            false,
            null,
            0,
            0,
            null,
            null,
            rect => rect.position == new Vector2Int(2, 2)));
    }

    [Test]
    public void PlacementRectValidation_AllowsRuntimeBlockerCellsButRejectsStaticBlockers()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        int blockedIndex = GridUtils.CellToIndex(new Unity.Mathematics.int2(1, 2), grid.Width);
        var blocked = new NativeBitArray(16, Allocator.Persistent);
        blocked.Set(blockedIndex, true);
        DynamicBlockerComponent blockerData = new() { GridSize = 16, Blocked = blocked };

        try
        {
            Assert.IsFalse(BuildingPlacementValidationSystem.IsPlacementRectValid(
                new RectInt(1, 2, 1, 1),
                grid,
                roads,
                blockerData,
                false,
                null,
                0,
                0,
                null,
                null,
                null));

            Assert.IsTrue(BuildingPlacementValidationSystem.IsPlacementRectValid(
                new RectInt(1, 2, 1, 1),
                grid,
                roads,
                blockerData,
                false,
                null,
                0,
                0,
                (x, y, _, _) => x == 1 && y == 2,
                null,
                null));
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
        }
    }

    [Test]
    public void InvalidPrefix_DetectsRoadFootprintMaskAndOutOfBounds()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        bool[] roadFootprintMask = new bool[16];
        roadFootprintMask[GridUtils.CellToIndex(new Unity.Mathematics.int2(2, 1), grid.Width)] = true;
        int[] prefix = null;

        BuildingPlacementValidationSystem.RebuildInvalidPrefix(
            grid,
            roads,
            default,
            roadFootprintMask,
            null,
            ref prefix,
            out int prefixWidth,
            out int prefixHeight,
            out bool hasPrefix);

        Assert.IsTrue(hasPrefix);
        Assert.IsFalse(BuildingPlacementValidationSystem.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(0, 0), new Vector2Int(1, 1)));
        Assert.IsTrue(BuildingPlacementValidationSystem.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(2, 1), new Vector2Int(1, 1)));
        Assert.IsTrue(BuildingPlacementValidationSystem.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(4, 4), new Vector2Int(1, 1)));
    }

    [Test]
    public void WallSegmentConflict_OnlyRejectsOverlappingSameAxisSegments()
    {
        Assert.IsTrue(BuildingPlacementValidationSystem.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(1, 3),
            new Vector2Int(1, 4),
            true));

        Assert.IsFalse(BuildingPlacementValidationSystem.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(1, 3),
            new Vector2Int(4, 1),
            false));

        Assert.IsFalse(BuildingPlacementValidationSystem.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(3, 1),
            new Vector2Int(1, 4),
            true));
    }

    private void CreateRoadBuffer(int width, int height, out GridConfig grid, out DynamicBuffer<GridRoad> roads)
    {
        _world ??= new World("BuildingPlacementValidationSystemTests");
        Entity entity = _world.EntityManager.CreateEntity();
        roads = _world.EntityManager.AddBuffer<GridRoad>(entity);
        roads.ResizeUninitialized(width * height);
        for (int i = 0; i < roads.Length; i++)
            roads[i] = default;

        grid = new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = default
        };
    }
}
#endif
