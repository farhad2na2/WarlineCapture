using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

internal sealed class RuntimeGridBootstrapSystem
{
    private const string FixMarker = "RuntimeGridBootstrap_NoTacticalMapRuntimeLoader_2026-05-26";

    public bool Ensure(World world, int width, int height, float cellSize, Vector3 origin)
    {
        if (world == null || !world.IsCreated)
        {
            Debug.LogWarning("[RuntimeGridBootstrap] missingWorld");
            return false;
        }

        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        cellSize = Mathf.Max(0.01f, cellSize);

        EntityManager entityManager = world.EntityManager;
        Entity gridEntity = ResolveGridEntity(entityManager);
        entityManager.SetComponentData(gridEntity, new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = cellSize,
            Origin = (float3)origin
        });

        int gridSize = width * height;
        EnsureBufferExists<GridWalkable>(entityManager, gridEntity);
        EnsureBufferExists<GridRoad>(entityManager, gridEntity);
        EnsureBufferExists<GridRoadSidewalk>(entityManager, gridEntity);
        EnsureBufferExists<GridRoadDirt>(entityManager, gridEntity);
        if (!entityManager.HasComponent<DynamicBlockerData>(gridEntity))
            entityManager.AddComponentData(gridEntity, default(DynamicBlockerData));
        if (!entityManager.HasComponent<PathPoolData>(gridEntity))
            entityManager.AddComponentData(gridEntity, new PathPoolData { Cells = new NativeList<int2>(1024, Allocator.Persistent) });
        if (!entityManager.HasComponent<DynamicOccupancyData>(gridEntity))
            entityManager.AddComponentData(gridEntity, default(DynamicOccupancyData));

        DynamicBuffer<GridWalkable> walkable = ResizeBuffer<GridWalkable>(entityManager, gridEntity, gridSize);
        DynamicBuffer<GridRoad> roads = ResizeBuffer<GridRoad>(entityManager, gridEntity, gridSize);
        DynamicBuffer<GridRoadSidewalk> sidewalks = ResizeBuffer<GridRoadSidewalk>(entityManager, gridEntity, gridSize);
        DynamicBuffer<GridRoadDirt> dirtRoads = ResizeBuffer<GridRoadDirt>(entityManager, gridEntity, gridSize);
        EnsureDynamicGridStorage(entityManager, gridEntity, gridSize);

        for (int i = 0; i < gridSize; i++)
        {
            walkable[i] = new GridWalkable { Value = 1 };
            roads[i] = new GridRoad { Value = 0 };
            sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            dirtRoads[i] = new GridRoadDirt { Value = 0 };
        }

        Debug.Log($"[RuntimeGridBootstrap] {FixMarker} ready entity={gridEntity.Index} size={width}x{height} cellSize={cellSize:0.###}");
        return true;
    }

    private static Entity ResolveGridEntity(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        using NativeArray<Entity> gridEntities = query.ToEntityArray(Allocator.Temp);
        if (gridEntities.Length > 0)
        {
            for (int i = 0; i < gridEntities.Length; i++)
            {
                Entity candidate = gridEntities[i];
                if (!entityManager.HasComponent<RuntimeGridBootstrapGridTag>(candidate))
                    return candidate;
            }

            return gridEntities[0];
        }

        Entity entity = entityManager.CreateEntity(typeof(GridConfig), typeof(RuntimeGridBootstrapGridTag));
        entityManager.SetName(entity, "RuntimeGameplayGrid");
        return entity;
    }

    private static void EnsureBufferExists<T>(EntityManager entityManager, Entity entity)
        where T : unmanaged, IBufferElementData
    {
        if (!entityManager.HasBuffer<T>(entity))
            entityManager.AddBuffer<T>(entity);
    }

    private static DynamicBuffer<T> ResizeBuffer<T>(EntityManager entityManager, Entity entity, int size)
        where T : unmanaged, IBufferElementData
    {
        DynamicBuffer<T> buffer = entityManager.GetBuffer<T>(entity);
        buffer.ResizeUninitialized(size);
        return buffer;
    }

    private static void EnsureDynamicGridStorage(EntityManager entityManager, Entity entity, int gridSize)
    {
        DynamicBlockerData blockerData = entityManager.GetComponentData<DynamicBlockerData>(entity);
        if (blockerData.GridSize != gridSize ||
            !blockerData.Counts.IsCreated ||
            !blockerData.Blocked.IsCreated ||
            !blockerData.FriendlyPassFactionIds.IsCreated)
        {
            if (blockerData.Counts.IsCreated)
                blockerData.Counts.Dispose();
            if (blockerData.Blocked.IsCreated)
                blockerData.Blocked.Dispose();
            if (blockerData.FriendlyPassFactionIds.IsCreated)
                blockerData.FriendlyPassFactionIds.Dispose();

            blockerData.GridSize = gridSize;
            blockerData.Counts = new NativeArray<int>(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            blockerData.Blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            blockerData.FriendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < blockerData.FriendlyPassFactionIds.Length; i++)
                blockerData.FriendlyPassFactionIds[i] = byte.MaxValue;

            entityManager.SetComponentData(entity, blockerData);
        }

        DynamicOccupancyData occupancyData = entityManager.GetComponentData<DynamicOccupancyData>(entity);
        if (occupancyData.GridSize == gridSize && occupancyData.Occupied.IsCreated)
            return;

        if (occupancyData.Occupied.IsCreated)
            occupancyData.Occupied.Dispose();

        occupancyData.GridSize = gridSize;
        occupancyData.Occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        entityManager.SetComponentData(entity, occupancyData);
    }
}
