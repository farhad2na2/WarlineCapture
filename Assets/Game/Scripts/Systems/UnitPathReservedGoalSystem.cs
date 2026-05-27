using Unity.Collections;
using Unity.Mathematics;

internal struct UnitPathReservedGoalSystem
{
    public NativeArray<int> Epochs;
    public int Generation;

    private int _gridSize;

    public void Prepare(int gridSize)
    {
        if (!Epochs.IsCreated || _gridSize != gridSize)
        {
            Dispose();
            Epochs = new NativeArray<int>(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _gridSize = gridSize;
            Generation = 1;
            return;
        }

        if (Generation == int.MaxValue)
        {
            Epochs.AsSpan().Clear();
            Generation = 1;
            return;
        }

        Generation++;
    }

    public void Dispose()
    {
        if (Epochs.IsCreated)
            Epochs.Dispose();
        _gridSize = 0;
        Generation = 0;
    }

    public static void ReserveGoalFootprint(
        GridConfig grid,
        NativeArray<int> reservedGoalEpochs,
        int reservedGoalGeneration,
        int2 cell,
        int2 footprintSize)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
                reservedGoalEpochs[row + x] = reservedGoalGeneration;
        }
    }
}
