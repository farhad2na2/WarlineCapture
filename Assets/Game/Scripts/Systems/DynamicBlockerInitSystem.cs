using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(RuntimeGridDeduplicationSystem))]
[UpdateBefore(typeof(StaticGridBlockerUpdateSystem))]
[UpdateBefore(typeof(DynamicOccupancyRebuildSystem))]
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
        if (!state.EntityManager.HasComponent<DynamicBlockerComponent>(gridEntity))
            return;

        var data = state.EntityManager.GetComponentData<DynamicBlockerComponent>(gridEntity);
        if (data.Counts.IsCreated) data.Counts.Dispose();
        if (data.Blocked.IsCreated) data.Blocked.Dispose();
        if (data.FriendlyPassFactionIds.IsCreated) data.FriendlyPassFactionIds.Dispose();

        if (state.EntityManager.HasComponent<PathPoolComponent>(gridEntity))
        {
            var pool = state.EntityManager.GetComponentData<PathPoolComponent>(gridEntity);
            if (pool.Cells.IsCreated) pool.Cells.Dispose();
        }

        if (state.EntityManager.HasComponent<DynamicOccupancyComponent>(gridEntity))
        {
            var occ = state.EntityManager.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            if (occ.Occupied.IsCreated) occ.Occupied.Dispose();
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        var grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
        int gridSize = grid.Width * grid.Height;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        bool addedMissingComponents = false;
        if (!state.EntityManager.HasComponent<DynamicBlockerComponent>(gridEntity))
        {
            ecb.AddComponent(gridEntity, default(DynamicBlockerComponent));
            addedMissingComponents = true;
        }

        if (!state.EntityManager.HasComponent<PathPoolComponent>(gridEntity))
        {
            ecb.AddComponent(gridEntity, new PathPoolComponent { Cells = new NativeList<int2>(1024, Allocator.Persistent) });
            addedMissingComponents = true;
        }

        if (!state.EntityManager.HasComponent<DynamicOccupancyComponent>(gridEntity))
        {
            ecb.AddComponent(gridEntity, default(DynamicOccupancyComponent));
            addedMissingComponents = true;
        }

        if (addedMissingComponents)
            ecb.Playback(state.EntityManager);
        ecb.Dispose();

        var dataRw = SystemAPI.GetComponentRW<DynamicBlockerComponent>(gridEntity);
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

        var occRw = SystemAPI.GetComponentRW<DynamicOccupancyComponent>(gridEntity);
        ref var occ = ref occRw.ValueRW;
        if (occ.Occupied.IsCreated) occ.Occupied.Dispose();
        occ.GridSize = gridSize;
        occ.Occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    }
}
