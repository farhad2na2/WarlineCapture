using Unity.Collections;
using Unity.Mathematics;

internal struct UnitPathCoarseWorkspaceSystem
{
    public NativeArray<int> CameFrom;
    public NativeArray<int> GScore;
    public NativeArray<int> Epoch;
    public NativeArray<int> ClosedEpoch;
    public NativeArray<int> OpenEpoch;
    public NativeArray<int> Open;
    public int Width;
    public int Height;

    private int _searchEpoch;

    public bool Ensure(int gridWidth, int gridHeight, int sectorSizeCells)
    {
        int width = (gridWidth + sectorSizeCells - 1) / sectorSizeCells;
        int height = (gridHeight + sectorSizeCells - 1) / sectorSizeCells;
        int count = width * height;
        if (count <= 0)
            return false;

        if (CameFrom.IsCreated && Width == width && Height == height)
            return true;

        Dispose();
        Width = width;
        Height = height;
        CameFrom = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        GScore = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        Epoch = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        ClosedEpoch = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        OpenEpoch = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        Open = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _searchEpoch = 1;
        return true;
    }

    public int ReserveSearchEpoch()
    {
        if (_searchEpoch <= 0 || _searchEpoch == int.MaxValue)
        {
            if (Epoch.IsCreated) Epoch.AsSpan().Clear();
            if (ClosedEpoch.IsCreated) ClosedEpoch.AsSpan().Clear();
            if (OpenEpoch.IsCreated) OpenEpoch.AsSpan().Clear();
            _searchEpoch = 1;
        }

        return _searchEpoch++;
    }

    public int Index(int2 sector) => sector.y * Width + sector.x;

    public int2 ToSector(int index) => new int2(index % Width, index / Width);

    public bool InBounds(int2 sector) =>
        (uint)sector.x < (uint)Width &&
        (uint)sector.y < (uint)Height;

    public void Dispose()
    {
        if (CameFrom.IsCreated) CameFrom.Dispose();
        if (GScore.IsCreated) GScore.Dispose();
        if (Epoch.IsCreated) Epoch.Dispose();
        if (ClosedEpoch.IsCreated) ClosedEpoch.Dispose();
        if (OpenEpoch.IsCreated) OpenEpoch.Dispose();
        if (Open.IsCreated) Open.Dispose();
        Width = 0;
        Height = 0;
        _searchEpoch = 1;
    }
}
