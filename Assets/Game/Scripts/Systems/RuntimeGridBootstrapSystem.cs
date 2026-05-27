using Unity.Entities;
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

        DynamicBuffer<GridWalkable> walkable = ResizeBuffer<GridWalkable>(entityManager, gridEntity, gridSize);
        DynamicBuffer<GridRoad> roads = ResizeBuffer<GridRoad>(entityManager, gridEntity, gridSize);
        DynamicBuffer<GridRoadSidewalk> sidewalks = ResizeBuffer<GridRoadSidewalk>(entityManager, gridEntity, gridSize);
        DynamicBuffer<GridRoadDirt> dirtRoads = ResizeBuffer<GridRoadDirt>(entityManager, gridEntity, gridSize);

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
        if (!query.IsEmptyIgnoreFilter)
            return query.GetSingletonEntity();

        Entity entity = entityManager.CreateEntity(typeof(GridConfig));
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
}
