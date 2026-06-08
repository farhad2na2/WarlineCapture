using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed class RuntimeCityReadinessQuerySystem
{
    private World _queryWorld;
    private EntityQuery _gridDataQuery;
    private bool _hasGridDataQuery;

    public bool TryGetGridConfig(out GridConfig grid)
    {
        bool hasGrid = TryGetGridData(out _, out GridConfig gridConfig, out _, out _);
        grid = gridConfig;
        return hasGrid;
    }

    public bool TryGetGridData(
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData)
    {
        gridEntity = Entity.Null;
        grid = default;
        roads = default;
        blockerData = default;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        EnsureGridDataQuery(em);
        if (_gridDataQuery.IsEmptyIgnoreFilter)
            return false;

        gridEntity = _gridDataQuery.GetSingletonEntity();
        grid = em.GetComponentData<GridConfig>(gridEntity);
        roads = em.GetBuffer<GridRoad>(gridEntity);
        blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        return true;
    }

    public bool HasPendingInitialUnitsSpawn(out int totalConfigs, out int initializedConfigs)
    {
        totalConfigs = 0;
        initializedConfigs = 0;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        using EntityQuery configQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        using EntityQuery initializedQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());

        totalConfigs = configQuery.CalculateEntityCount();
        initializedConfigs = initializedQuery.CalculateEntityCount();
        return totalConfigs > 0 && initializedConfigs < totalConfigs;
    }

    public List<RectInt> CollectInitialBaseExclusionRoadRects(int roadCellSizeInGridCells)
    {
        var exclusions = new List<RectInt>();
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return exclusions;

        EntityManager em = world.EntityManager;
        using EntityQuery configQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        using var entities = configQuery.ToEntityArray(Allocator.Temp);
        int roadCellSize = Mathf.Max(1, roadCellSizeInGridCells);

        for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
        {
            Entity entity = entities[entityIndex];
            if (!em.Exists(entity) ||
                !em.HasComponent<InitialUnitsSpawnConfig>(entity) ||
                !em.HasBuffer<InitialUnitsFactionSpawnEntry>(entity))
                continue;

            InitialUnitsSpawnConfig config = em.GetComponentData<InitialUnitsSpawnConfig>(entity);
            if (config.CreateFactionBases == 0)
                continue;

            int halfWidthRoadCells = Mathf.CeilToInt((config.BaseHalfWidthCells + 220) / (float)roadCellSize);
            int halfHeightRoadCells = Mathf.CeilToInt((config.BaseHalfHeightCells + 220) / (float)roadCellSize);
            DynamicBuffer<InitialUnitsFactionSpawnEntry> spawns = em.GetBuffer<InitialUnitsFactionSpawnEntry>(entity);
            for (int i = 0; i < spawns.Length; i++)
            {
                Vector2Int center = new(spawns[i].SpawnCell.x / roadCellSize, spawns[i].SpawnCell.y / roadCellSize);
                exclusions.Add(new RectInt(
                    center.x - halfWidthRoadCells,
                    center.y - halfHeightRoadCells,
                    halfWidthRoadCells * 2 + 1,
                    halfHeightRoadCells * 2 + 1));
            }
        }

        return exclusions;
    }

    private void EnsureGridDataQuery(EntityManager em)
    {
        World world = em.World;
        if (_hasGridDataQuery && _queryWorld == world && world != null && world.IsCreated)
            return;

        ClearGridDataQuery();
        _queryWorld = world;
        _gridDataQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridRoad>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>());
        _hasGridDataQuery = true;
    }

    public void Clear()
    {
        ClearGridDataQuery();
        _queryWorld = null;
    }

    private void ClearGridDataQuery()
    {
        if (_hasGridDataQuery && _queryWorld != null && _queryWorld.IsCreated)
            _gridDataQuery.Dispose();

        _gridDataQuery = default;
        _hasGridDataQuery = false;
    }
}
