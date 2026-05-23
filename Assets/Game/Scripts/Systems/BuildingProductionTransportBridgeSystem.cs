using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using RuntimeBuildingData = BuildingPlacementSystem.RuntimeBuildingData;

internal sealed class BuildingProductionTransportBridgeSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
    public delegate bool TryGetGridDataDelegate(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData);
    public delegate void EntityManagerAction(EntityManager entityManager);

    public readonly struct Context
    {
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly TryGetGridDataDelegate TryGetGridData;
        public readonly EntityManagerAction EnsureEntityQueries;
        public readonly BuildingSpawnSystem SpawnSystem;
        public readonly BuildingSpawnSystem.Context SpawnContext;

        public Context(
            TryGetEntityManagerDelegate tryGetEntityManager,
            TryGetGridDataDelegate tryGetGridData,
            EntityManagerAction ensureEntityQueries,
            BuildingSpawnSystem spawnSystem,
            BuildingSpawnSystem.Context spawnContext)
        {
            TryGetEntityManager = tryGetEntityManager;
            TryGetGridData = tryGetGridData;
            EnsureEntityQueries = ensureEntityQueries;
            SpawnSystem = spawnSystem;
            SpawnContext = spawnContext;
        }
    }

    public int2 ResolveProductionGroundGoalCell(Context context, Vector3 worldPosition)
    {
        if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
            return int2.zero;

        return GridUtils.WorldToCell(grid, worldPosition);
    }

    public void MoveNewestProducedUnitToCell(Context context, RuntimeBuildingData building, int2 goalCell)
    {
        if (building?.ProducedUnits == null || building.ProducedUnits.Count == 0)
            return;
        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
            return;

        Entity entity = building.ProducedUnits[building.ProducedUnits.Count - 1];
        if (entity == Entity.Null || !em.Exists(entity))
            return;

        bool isAirUnit = em.HasComponent<UnitAirMovement>(entity);
        bool isSpawnTransit = em.HasComponent<UnitSpawnTransitTag>(entity);
        if (isAirUnit && !isSpawnTransit)
            return;

        if (em.HasComponent<UnitTarget>(entity))
            em.SetComponentData(entity, new UnitTarget { Cell = goalCell });
        else
            em.AddComponentData(entity, new UnitTarget { Cell = goalCell });

        if (em.HasComponent<UnitPathRequest>(entity))
            em.SetComponentData(entity, new UnitPathRequest { Goal = goalCell });
        else
            em.AddComponentData(entity, new UnitPathRequest { Goal = goalCell });
    }

    public void AlignNewestProducedUnitRotation(Context context, RuntimeBuildingData building, Vector3 forward)
    {
        if (building?.ProducedUnits == null || building.ProducedUnits.Count == 0)
            return;
        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
            return;

        Entity entity = building.ProducedUnits[building.ProducedUnits.Count - 1];
        if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<LocalTransform>(entity))
            return;

        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
            return;

        forward.Normalize();
        LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
        transform.Rotation = quaternion.LookRotationSafe((float3)forward, math.up());
        em.SetComponentData(entity, transform);
    }

    public bool TrySpawnPlayerUnitNearBuilding(
        Context context,
        RuntimeBuildingData building,
        int productionIndex,
        int reservedProductionSlotIndex,
        Vector3? overrideWorldPosition,
        int2? overrideCell,
        ref uint randomState)
    {
        if (context.SpawnSystem == null)
            return false;
        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
            return false;
        if (context.TryGetGridData == null || !context.TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerData blockerData))
            return false;

        context.EnsureEntityQueries?.Invoke(em);
        return context.SpawnSystem.TrySpawnPlayerUnitNearBuilding(
            context.SpawnContext,
            building,
            productionIndex,
            reservedProductionSlotIndex,
            overrideWorldPosition,
            overrideCell,
            em,
            gridEntity,
            grid,
            blockerData,
            ref randomState);
    }
}
