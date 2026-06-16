using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed partial class BuildingProductionTransportBridgeSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
    public delegate bool TryGetGridDataDelegate(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData);
    public delegate void EntityManagerAction(EntityManager entityManager);
    public delegate bool BooleanQuery();
    public delegate void CameraFocusAction(Vector3 worldPosition);

    public readonly struct Context
    {
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly TryGetGridDataDelegate TryGetGridData;
        public readonly EntityManagerAction EnsureEntityQueries;
        public readonly BuildingSpawnSystem SpawnSystem;
        public readonly BuildingSpawnSystem.Context SpawnContext;
        public readonly BooleanQuery IsBuildDrawerOpen;
        public readonly CameraFocusAction SmoothMoveCameraGroundCenterTo;

        public Context(
            TryGetEntityManagerDelegate tryGetEntityManager,
            TryGetGridDataDelegate tryGetGridData,
            EntityManagerAction ensureEntityQueries,
            BuildingSpawnSystem spawnSystem,
            BuildingSpawnSystem.Context spawnContext,
            BooleanQuery isBuildDrawerOpen,
            CameraFocusAction smoothMoveCameraGroundCenterTo)
        {
            TryGetEntityManager = tryGetEntityManager;
            TryGetGridData = tryGetGridData;
            EnsureEntityQueries = ensureEntityQueries;
            SpawnSystem = spawnSystem;
            SpawnContext = spawnContext;
            IsBuildDrawerOpen = isBuildDrawerOpen;
            SmoothMoveCameraGroundCenterTo = smoothMoveCameraGroundCenterTo;
        }
    }

    public int2 ResolveProductionGroundGoalCell(Context context, Vector3 worldPosition)
    {
        if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
            return int2.zero;

        return GridUtils.WorldToCell(grid, worldPosition);
    }

    public bool TryResolveAvailableFactionHelipadSpawn(
        Context context,
        byte factionId,
        RuntimeBuildingEntity sourceBuilding,
        GameObject spawnUnitPrefab,
        out int2 cell,
        out Vector3 worldPosition)
    {
        cell = default;
        worldPosition = default;
        if (context.SpawnSystem == null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em) ||
            context.TryGetGridData == null ||
            !context.TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerComponent blockerData))
        {
            return false;
        }

        int2 unitFootprint = ResolveUnitFootprint(context, em, spawnUnitPrefab);
        if (!context.SpawnSystem.TryResolveAvailableFactionHelipadSpawn(
                context.SpawnContext,
                factionId,
                sourceBuilding,
                em,
                gridEntity,
                grid,
                blockerData,
                unitFootprint,
                out cell,
                out float3 position))
        {
            return false;
        }

        worldPosition = position;
        return true;
    }

    private static int2 ResolveUnitFootprint(Context context, EntityManager em, GameObject spawnUnitPrefab)
    {
        if (context.SpawnContext.SpawnPrefabSystem != null &&
            context.SpawnContext.SpawnPrefabSystem.TryGetSpawnUnitPrefabEntity(
                context.SpawnContext.SpawnPrefabContext,
                em,
                spawnUnitPrefab,
                out Entity prefabEntity) &&
            prefabEntity != Entity.Null &&
            em.HasComponent<UnitFootprint>(prefabEntity))
        {
            return em.GetComponentData<UnitFootprint>(prefabEntity).Size;
        }

        return new int2(1, 1);
    }

    public void MoveNewestProducedUnitToCell(Context context, RuntimeBuildingEntity building, int2 goalCell)
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

        UnitMoveOrderRequestSystem.EnqueueAndProcessTargetPathMoveOrder(em, entity, goalCell);
    }

    public void AlignNewestProducedUnitRotation(Context context, RuntimeBuildingEntity building, Vector3 forward)
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
        RuntimeBuildingEntity building,
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
        if (context.TryGetGridData == null || !context.TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerComponent blockerData))
            return false;

        context.EnsureEntityQueries?.Invoke(em);
        bool spawned = context.SpawnSystem.TrySpawnPlayerUnitNearBuilding(
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
        if (spawned)
            FocusNewestPlayerProducedUnit(context, building, em);

        return spawned;
    }

    internal static bool FocusNewestPlayerProducedUnit(Context context, RuntimeBuildingEntity building, EntityManager em)
    {
        if (context.SmoothMoveCameraGroundCenterTo == null ||
            context.IsBuildDrawerOpen == null ||
            !context.IsBuildDrawerOpen() ||
            building == null ||
            !IsPlayerProductionFocusAllowed(building) ||
            building.ProducedUnits == null ||
            building.ProducedUnits.Count == 0)
        {
            return false;
        }

        Entity newest = building.ProducedUnits[building.ProducedUnits.Count - 1];
        if (newest == Entity.Null || !em.Exists(newest) || !em.HasComponent<LocalTransform>(newest))
            return false;

        LocalTransform transform = em.GetComponentData<LocalTransform>(newest);
        context.SmoothMoveCameraGroundCenterTo(transform.Position);
        return true;
    }

    private static bool IsPlayerProductionFocusAllowed(RuntimeBuildingEntity building)
    {
        if (building == null)
            return false;

        return !building.HasOwnerFaction ||
               building.OwnerFactionId == FactionIdentity.NeutralFactionId ||
               building.OwnerFactionId == FactionIdentity.PlayerFactionId;
    }
}
