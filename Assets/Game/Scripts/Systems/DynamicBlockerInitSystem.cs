using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public partial struct DynamicBlockerInitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
    }

    public void OnDestroy(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<GridConfig>())
            return;

        var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        if (!state.EntityManager.HasComponent<DynamicBlockerData>(gridEntity))
            return;

        var data = state.EntityManager.GetComponentData<DynamicBlockerData>(gridEntity);
        if (data.Counts.IsCreated) data.Counts.Dispose();
        if (data.Blocked.IsCreated) data.Blocked.Dispose();
        if (data.FriendlyPassFactionIds.IsCreated) data.FriendlyPassFactionIds.Dispose();

        if (state.EntityManager.HasComponent<PathPoolData>(gridEntity))
        {
            var pool = state.EntityManager.GetComponentData<PathPoolData>(gridEntity);
            if (pool.Cells.IsCreated) pool.Cells.Dispose();
        }

        if (state.EntityManager.HasComponent<DynamicOccupancyData>(gridEntity))
        {
            var occ = state.EntityManager.GetComponentData<DynamicOccupancyData>(gridEntity);
            if (occ.Occupied.IsCreated) occ.Occupied.Dispose();
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingleton<GridConfig>();
        int gridSize = grid.Width * grid.Height;

        var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        if (!state.EntityManager.HasComponent<DynamicBlockerData>(gridEntity))
            state.EntityManager.AddComponentData(gridEntity, default(DynamicBlockerData));
        if (!state.EntityManager.HasComponent<PathPoolData>(gridEntity))
            state.EntityManager.AddComponentData(gridEntity, new PathPoolData { Cells = new NativeList<int2>(1024, Allocator.Persistent) });
        if (!state.EntityManager.HasComponent<DynamicOccupancyData>(gridEntity))
            state.EntityManager.AddComponentData(gridEntity, default(DynamicOccupancyData));

        var dataRw = SystemAPI.GetComponentRW<DynamicBlockerData>(gridEntity);
        ref var data = ref dataRw.ValueRW;

        if (data.GridSize == gridSize && data.Counts.IsCreated && data.Blocked.IsCreated && data.FriendlyPassFactionIds.IsCreated)
            return;

        if (data.Counts.IsCreated) data.Counts.Dispose();
        if (data.Blocked.IsCreated) data.Blocked.Dispose();
        if (data.FriendlyPassFactionIds.IsCreated) data.FriendlyPassFactionIds.Dispose();

        data.GridSize = gridSize;
        data.Counts = new NativeArray<int>(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        data.Blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        data.FriendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < data.FriendlyPassFactionIds.Length; i++)
            data.FriendlyPassFactionIds[i] = byte.MaxValue;

        var occRw = SystemAPI.GetComponentRW<DynamicOccupancyData>(gridEntity);
        ref var occ = ref occRw.ValueRW;
        if (occ.Occupied.IsCreated) occ.Occupied.Dispose();
        occ.GridSize = gridSize;
        occ.Occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    }
}
