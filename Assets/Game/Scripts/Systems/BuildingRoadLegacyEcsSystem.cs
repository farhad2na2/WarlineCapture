using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed class BuildingRoadLegacyEcsSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
    public delegate bool TryGetGridDataDelegate(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData);
    public delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);

    public readonly struct Context
    {
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly TryGetGridDataDelegate TryGetGridData;
        public readonly GetFootprintCenterDelegate GetFootprintCenter;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly uint BuildingSpawnRandomState;

        public Context(
            TryGetEntityManagerDelegate tryGetEntityManager,
            TryGetGridDataDelegate tryGetGridData,
            GetFootprintCenterDelegate getFootprintCenter,
            BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
            BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
            uint buildingSpawnRandomState)
        {
            TryGetEntityManager = tryGetEntityManager;
            TryGetGridData = tryGetGridData;
            GetFootprintCenter = getFootprintCenter;
            BuildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
            BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
            BuildingSpawnRandomState = buildingSpawnRandomState;
        }
    }

    public readonly struct SpawnResult
    {
        public readonly bool Spawned;
        public readonly uint BuildingSpawnRandomState;

        public SpawnResult(bool spawned, uint buildingSpawnRandomState)
        {
            Spawned = spawned;
            BuildingSpawnRandomState = buildingSpawnRandomState;
        }
    }

    public Entity CreateBlockerEntity(Context context, Vector2Int originCell, Vector2Int footprintCells)
    {
        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
            return Entity.Null;

        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new UnitGrid { Cell = new int2(originCell.x, originCell.y) });
        em.AddComponentData(entity, new GridBlockerSize
        {
            Size = new int2(Mathf.Max(1, footprintCells.x), Mathf.Max(1, footprintCells.y))
        });
        em.AddComponent<StaticGridBlocker>(entity);
        return entity;
    }

    public Entity CreateBuildingCombatEntity(Context context, Vector2Int originCell, BuildingDefinition definition)
    {
        if (definition == null)
            return Entity.Null;
        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
            return Entity.Null;
        if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
            return Entity.Null;
        if (context.GetFootprintCenter == null)
            return Entity.Null;

        Entity entity = em.CreateEntity();
        float3 center = context.GetFootprintCenter(originCell, definition.FootprintCells, grid);

        em.AddComponentData(entity, new LocalTransform
        {
            Position = center,
            Rotation = quaternion.identity,
            Scale = 1f
        });
        em.AddComponentData(entity, new LocalToWorld());
        em.AddComponentData(entity, new UnitGrid
        {
            Cell = new int2(originCell.x, originCell.y)
        });
        em.AddComponentData(entity, new UnitGridInitialized());
        em.AddComponentData(entity, new Faction { Id = 0 });
        em.AddComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
        em.AddComponentData(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
        em.AddComponentData(entity, new UnitPrevWorldPos { Value = center });
        em.AddComponentData(entity, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
        return entity;
    }

    public void AttachRuntimeLink(Context context, RuntimeBuildingData building)
    {
        if (context.BuildingPlacementInteractionSystem == null || building?.Instance == null)
            return;

        RuntimeBuildingEntityLink link = building.Instance.GetComponent<RuntimeBuildingEntityLink>();
        if (link == null)
            link = building.Instance.AddComponent<RuntimeBuildingEntityLink>();

        link.Configure(
            context.BuildingPlacementInteractionSystem,
            context.BuildingPlacementInteractionContext,
            building.Id,
            building.CombatEntity,
            building.BlockerEntity);
    }

    public SpawnResult TrySpawnPlayerUnitNearBuilding(Context context, RuntimeBuildingData building)
    {
        uint randomState = context.BuildingSpawnRandomState;
        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
            return new SpawnResult(false, randomState);
        if (context.TryGetGridData == null || !context.TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerData blockerData))
            return new SpawnResult(false, randomState);
        if (!TryGetPlayerUnitPrefabEntity(em, out Entity prefabEntity))
            return new SpawnResult(false, randomState);

        var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        try
        {
            randomState = math.max(1u, randomState + 1u);
            var rng = new Unity.Mathematics.Random(randomState);
            Vector2Int size = building.Definition.FootprintCells;
            int2 center = new(building.OriginCell.x + size.x / 2, building.OriginCell.y + size.y / 2);
            int radius = Mathf.Max(size.x, size.y) + 4;
            int2 cell = SpawnCellUtility.FindSpawnCellNear(ref rng, grid, walkable, blockerData.Blocked, occupied, ref reserved, center, radius);

            Entity instance = em.Instantiate(prefabEntity);
            float3 pos = GridUtils.CellToWorldCenter(grid, cell);
            em.SetComponentData(instance, new UnitGrid { Cell = cell });
            em.SetComponentData(instance, LocalTransform.FromPosition(pos));
            if (em.HasComponent<UnitPrevWorldPos>(instance))
                em.SetComponentData(instance, new UnitPrevWorldPos { Value = pos });
            if (em.HasComponent<UnitMoveVisualState>(instance))
                em.SetComponentData(instance, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
            if (em.HasComponent<Faction>(instance))
                em.SetComponentData(instance, new Faction { Id = 0 });
            if (em.HasComponent<UnitRespawnPrefab>(instance))
                em.SetComponentData(instance, new UnitRespawnPrefab { Prefab = prefabEntity });
            if (em.HasComponent<UnitIdleWanderState>(instance))
            {
                randomState = math.max(1u, randomState + 1u);
                em.SetComponentData(instance, new UnitIdleWanderState
                {
                    RandomState = randomState,
                    RetrySeconds = 0f,
                    CurrentIdleDelaySeconds = 0f
                });
            }
            if (em.HasComponent<UnitPathFollow>(instance))
                em.RemoveComponent<UnitPathFollow>(instance);
            if (em.HasComponent<UnitPathRange>(instance))
                em.RemoveComponent<UnitPathRange>(instance);
            if (em.HasComponent<EngageTarget>(instance))
                em.RemoveComponent<EngageTarget>(instance);
            if (em.HasComponent<UnitPathRequest>(instance))
                em.RemoveComponent<UnitPathRequest>(instance);
            if (em.HasComponent<UnitTarget>(instance))
                em.RemoveComponent<UnitTarget>(instance);
            if (em.HasComponent<AutoWanderMoveTag>(instance))
                em.RemoveComponent<AutoWanderMoveTag>(instance);

            return new SpawnResult(true, randomState);
        }
        finally
        {
            reserved.Dispose();
        }
    }

    private static bool TryGetPlayerUnitPrefabEntity(EntityManager em, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        using var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitRespawnPrefab>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.HasComponent<StaticGridBlocker>(entity))
                continue;
            if (em.GetComponentData<Faction>(entity).Id != 0)
                continue;

            Entity candidate = em.GetComponentData<UnitRespawnPrefab>(entity).Prefab;
            if (candidate == Entity.Null)
                continue;

            prefabEntity = candidate;
            return true;
        }

        return false;
    }
}
