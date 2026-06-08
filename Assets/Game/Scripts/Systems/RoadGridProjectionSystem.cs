using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using FootprintKind = RoadFootprintQuerySystem.FootprintKind;
using RoadTileData = RoadNetworkSystem.RoadTileData;

public sealed class RoadGridProjectionSystem
{
    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<Vector2Int, RoadTileData> RoadTiles;
        public readonly RoadFootprintQuerySystem FootprintQuerySystem;
        public readonly RoadFootprintQuerySystem.Context FootprintContext;
        public readonly float RoadGridSize;

        public Context(
            IReadOnlyDictionary<Vector2Int, RoadTileData> roadTiles,
            RoadFootprintQuerySystem footprintQuerySystem,
            RoadFootprintQuerySystem.Context footprintContext,
            float roadGridSize)
        {
            RoadTiles = roadTiles;
            FootprintQuerySystem = footprintQuerySystem;
            FootprintContext = footprintContext;
            RoadGridSize = roadGridSize;
        }
    }

    private struct RoadBuffersData
    {
        public DynamicBuffer<GridRoad> Roads;
        public DynamicBuffer<GridRoadSidewalk> Sidewalks;
        public DynamicBuffer<GridRoadDirt> DirtRoads;
        public GridConfig Grid;

        public RoadBuffersData(
            DynamicBuffer<GridRoad> roads,
            DynamicBuffer<GridRoadSidewalk> sidewalks,
            DynamicBuffer<GridRoadDirt> dirtRoads,
            GridConfig grid)
        {
            Roads = roads;
            Sidewalks = sidewalks;
            DirtRoads = dirtRoads;
            Grid = grid;
        }
    }

    private World _queryWorld;
    private EntityQuery _gridDataQuery;
    private EntityQuery _roadBufferQuery;
    private EntityQuery _roadBuffersQuery;
    private int _deferRoadEcsSyncDepth;
    private bool _pendingRoadEcsSync;

    public void BeginDeferredRoadEcsSync()
    {
        _deferRoadEcsSyncDepth++;
    }

    public void EndDeferredRoadEcsSync(Context context)
    {
        if (_deferRoadEcsSyncDepth <= 0)
            return;

        _deferRoadEcsSyncDepth--;
        if (_deferRoadEcsSyncDepth == 0 && _pendingRoadEcsSync)
        {
            SyncRoadCellsToEcs(context);
            _pendingRoadEcsSync = false;
        }
    }

    public void RequestRoadEcsSync(Context context)
    {
        if (_deferRoadEcsSyncDepth > 0)
        {
            _pendingRoadEcsSync = true;
            return;
        }

        SyncRoadCellsToEcs(context);
    }

    public void SyncRoadCellsToEcs(Context context)
    {
        if (!TryGetRoadBuffers(out var roadBuffers))
            return;

        ClearRoadBuffers(roadBuffers);

        GridConfig grid = roadBuffers.Grid;
        if (context.RoadGridSize <= 0f || grid.CellSize <= 0f || context.RoadTiles == null || context.FootprintQuerySystem == null)
            return;

        foreach (var entry in context.RoadTiles)
        {
            Vector2Int roadCell = entry.Key;
            context.FootprintQuerySystem.ForEachRoadWorldFootprintKind(context.FootprintContext, roadCell, entry.Value, (worldMin, worldMax, kind) =>
            {
                GetGridBounds(grid, worldMin, worldMax, out int minX, out int minY, out int maxX, out int maxY);

                for (int y = minY; y < maxY; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        if (!RoadFootprintQuerySystem.IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                            continue;

                        int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                        roadBuffers.Roads[index] = new GridRoad { Value = 1 };
                        if (kind == FootprintKind.Sidewalk)
                            roadBuffers.Sidewalks[index] = new GridRoadSidewalk { Value = 1 };
                        else
                            roadBuffers.DirtRoads[index] = new GridRoadDirt { Value = 1 };
                    }
                }

                return true;
            });
        }
    }

    public void ClearRoadDataInEcs()
    {
        if (!TryGetRoadBuffers(out var roadBuffers))
            return;

        ClearRoadBuffers(roadBuffers);
    }

    public void RemoveRuntimeBlockersUnderRoads(Context context, RuntimeGridBlockerSystem runtimeGridBlockers)
    {
        if (runtimeGridBlockers == null || !TryGetRoadBuffer(out _, out var grid) || context.RoadTiles == null || context.FootprintQuerySystem == null)
            return;

        foreach (var entry in context.RoadTiles)
        {
            Vector2Int roadCell = entry.Key;
            context.FootprintQuerySystem.ForEachRoadWorldFootprint(context.FootprintContext, roadCell, entry.Value, (worldMin, worldMax) =>
            {
                GetGridBounds(grid, worldMin, worldMax, out int minX, out int minY, out int maxX, out int maxY);

                int overlapMinX = int.MaxValue;
                int overlapMinY = int.MaxValue;
                int overlapMaxX = int.MinValue;
                int overlapMaxY = int.MinValue;

                for (int y = minY; y < maxY; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        if (!RoadFootprintQuerySystem.IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                            continue;

                        overlapMinX = Mathf.Min(overlapMinX, x);
                        overlapMinY = Mathf.Min(overlapMinY, y);
                        overlapMaxX = Mathf.Max(overlapMaxX, x + 1);
                        overlapMaxY = Mathf.Max(overlapMaxY, y + 1);
                    }
                }

                if (overlapMaxX > overlapMinX && overlapMaxY > overlapMinY)
                {
                    runtimeGridBlockers.RemoveBlockersOverlappingFootprint(
                        new Vector2Int(overlapMinX, overlapMinY),
                        new Vector2Int(overlapMaxX - overlapMinX, overlapMaxY - overlapMinY));
                }

                return true;
            });
        }
    }

    public bool TryGetGridData(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData)
    {
        gridEntity = Entity.Null;
        grid = default;
        roads = default;
        blockerData = default;

        if (!TryGetEntityManager(out EntityManager em))
            return false;

        EnsureEntityQueries(em);
        if (_gridDataQuery.IsEmptyIgnoreFilter)
            return false;

        gridEntity = _gridDataQuery.GetSingletonEntity();
        grid = em.GetComponentData<GridConfig>(gridEntity);
        roads = em.GetBuffer<GridRoad>(gridEntity);
        blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        return true;
    }

    public bool TryGetGridConfig(out GridConfig grid)
    {
        grid = default;

        if (!TryGetEntityManager(out EntityManager entityManager))
            return false;

        EnsureEntityQueries(entityManager);
        if (_gridDataQuery.IsEmptyIgnoreFilter)
            return false;

        grid = entityManager.GetComponentData<GridConfig>(_gridDataQuery.GetSingletonEntity());
        return true;
    }

    private bool TryGetRoadBuffer(out DynamicBuffer<GridRoad> roads, out GridConfig grid)
    {
        roads = default;
        grid = default;

        if (!TryGetEntityManager(out EntityManager entityManager))
            return false;

        EnsureEntityQueries(entityManager);
        if (_roadBufferQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = _roadBufferQuery.GetSingletonEntity();
        grid = entityManager.GetComponentData<GridConfig>(gridEntity);
        roads = entityManager.GetBuffer<GridRoad>(gridEntity);
        return true;
    }

    private bool TryGetRoadBuffers(out RoadBuffersData roadBuffers)
    {
        roadBuffers = default;

        if (!TryGetEntityManager(out EntityManager entityManager))
            return false;

        EnsureEntityQueries(entityManager);
        if (_roadBuffersQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = _roadBuffersQuery.GetSingletonEntity();
        roadBuffers = new RoadBuffersData(
            entityManager.GetBuffer<GridRoad>(gridEntity),
            entityManager.GetBuffer<GridRoadSidewalk>(gridEntity),
            entityManager.GetBuffer<GridRoadDirt>(gridEntity),
            entityManager.GetComponentData<GridConfig>(gridEntity));
        return true;
    }

    private void EnsureEntityQueries(EntityManager entityManager)
    {
        World world = entityManager.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _gridDataQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridRoad>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>());
        _roadBufferQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadWrite<GridRoad>());
        _roadBuffersQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadWrite<GridRoad>(),
            ComponentType.ReadWrite<GridRoadSidewalk>(),
            ComponentType.ReadWrite<GridRoadDirt>());
    }

    private static void ClearRoadBuffers(RoadBuffersData roadBuffers)
    {
        for (int i = 0; i < roadBuffers.Roads.Length; i++)
        {
            roadBuffers.Roads[i] = new GridRoad { Value = 0 };
            roadBuffers.Sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            roadBuffers.DirtRoads[i] = new GridRoadDirt { Value = 0 };
        }
    }

    private static void GetGridBounds(GridConfig grid, Vector3 worldMin, Vector3 worldMax, out int minX, out int minY, out int maxX, out int maxY)
    {
        float3 localMin = (float3)(worldMin - (Vector3)grid.Origin);
        float3 localMax = (float3)(worldMax - (Vector3)grid.Origin);

        minX = Mathf.Clamp(Mathf.FloorToInt(localMin.x / grid.CellSize), 0, grid.Width);
        minY = Mathf.Clamp(Mathf.FloorToInt(localMin.z / grid.CellSize), 0, grid.Height);
        maxX = Mathf.Clamp(Mathf.CeilToInt(localMax.x / grid.CellSize), 0, grid.Width);
        maxY = Mathf.Clamp(Mathf.CeilToInt(localMax.z / grid.CellSize), 0, grid.Height);
    }

    private static bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        return true;
    }
}
