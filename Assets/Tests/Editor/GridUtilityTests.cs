using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public class GridUtilityTests
{
    [Test]
    public void CellToIndex_And_IndexToCell_RoundTrip()
    {
        int width = 7;
        int2 cell = new int2(3, 5);

        int index = GridUtils.CellToIndex(cell, width);

        Assert.AreEqual(cell, GridUtils.IndexToCell(index, width));
    }

    [Test]
    public void FindSpawnCellNear_AvoidsBlockedOccupiedAndReservedCells()
    {
        var grid = new GridConfig
        {
            Width = 4,
            Height = 4,
            CellSize = 1f,
            Origin = float3.zero
        };

        var walkable = new NativeArray<GridWalkable>(grid.Width * grid.Height, Allocator.Temp);
        var blocked = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        var occupied = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);

        try
        {
            for (int i = 0; i < walkable.Length; i++)
                walkable[i] = new GridWalkable { Value = 1 };

            int centerIndex = GridUtils.CellToIndex(new int2(1, 1), grid.Width);
            blocked.Set(centerIndex, true);

            int occupiedIndex = GridUtils.CellToIndex(new int2(1, 2), grid.Width);
            occupied.Set(occupiedIndex, true);

            int reservedIndex = GridUtils.CellToIndex(new int2(2, 1), grid.Width);
            reserved.Set(reservedIndex, true);

            var rng = new Random(1234);
            int2 cell = SpawnCellUtility.FindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved, new int2(1, 1), 1);

            int resultIndex = GridUtils.CellToIndex(cell, grid.Width);
            Assert.AreNotEqual(centerIndex, resultIndex);
            Assert.IsFalse(blocked.IsSet(resultIndex));
            Assert.IsFalse(occupied.IsSet(resultIndex));
        }
        finally
        {
            reserved.Dispose();
            occupied.Dispose();
            blocked.Dispose();
            walkable.Dispose();
        }
    }

    [Test]
    public void FindSpawnCellNear_PrefersExactCenter_WhenItIsFree()
    {
        var grid = new GridConfig
        {
            Width = 8,
            Height = 8,
            CellSize = 1f,
            Origin = float3.zero
        };

        var walkable = new NativeArray<GridWalkable>(grid.Width * grid.Height, Allocator.Temp);
        var blocked = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        var occupied = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);

        try
        {
            for (int i = 0; i < walkable.Length; i++)
                walkable[i] = new GridWalkable { Value = 1 };

            var rng = new Random(1);
            int2 center = new int2(4, 4);
            int2 cell = SpawnCellUtility.FindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved, center, 3);

            Assert.AreEqual(center, cell);
            Assert.IsTrue(reserved.IsSet(GridUtils.CellToIndex(center, grid.Width)));
        }
        finally
        {
            reserved.Dispose();
            occupied.Dispose();
            blocked.Dispose();
            walkable.Dispose();
        }
    }

    [Test]
    public void FindSpawnCellNear_FallsBackToAnyFreeCell_WhenRadiusIsFull()
    {
        var grid = new GridConfig
        {
            Width = 3,
            Height = 3,
            CellSize = 1f,
            Origin = float3.zero
        };

        var walkable = new NativeArray<GridWalkable>(grid.Width * grid.Height, Allocator.Temp);
        var blocked = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        var occupied = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);

        try
        {
            for (int i = 0; i < walkable.Length; i++)
                walkable[i] = new GridWalkable { Value = 1 };

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (x == 2 && y == 2)
                        continue;

                    blocked.Set(GridUtils.CellToIndex(new int2(x, y), grid.Width), true);
                }
            }

            var rng = new Random(77);
            int2 cell = SpawnCellUtility.FindSpawnCellNear(ref rng, grid, walkable, blocked, occupied, ref reserved, new int2(0, 0), 0);

            Assert.AreEqual(new int2(2, 2), cell);
        }
        finally
        {
            reserved.Dispose();
            occupied.Dispose();
            blocked.Dispose();
            walkable.Dispose();
        }
    }
}
