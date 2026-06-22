using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed class BuildingProductionTransportBridgeSystem
{
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
        ref uint randomState,
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
                ref randomState,
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
        FixedString64Bytes sourceKey = GetUnitPrefabSourceKey(spawnUnitPrefab);
        if (context.SpawnContext.SpawnPrefabSystem.TryGetSpawnUnitPrefabEntity(
                context.SpawnContext.SpawnPrefabContext,
                em,
                sourceKey,
                out Entity prefabEntity) &&
            prefabEntity != Entity.Null &&
            em.HasComponent<UnitFootprint>(prefabEntity))
        {
            return em.GetComponentData<UnitFootprint>(prefabEntity).Size;
        }

        return new int2(1, 1);
    }

    private static FixedString64Bytes GetUnitPrefabSourceKey(GameObject unitPrefab)
    {
        string sourceKey = BuildingDefinitionSystem.GetSpawnableLookupKey(unitPrefab);
        return string.IsNullOrWhiteSpace(sourceKey) ? default : new FixedString64Bytes(sourceKey);
    }

    public void MoveNewestProducedUnitToCell(Context context, RuntimeBuildingEntity building, int2 goalCell)
    {
        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
            return;

        if (!TryGetNewestProducedUnit(context, building, em, out Entity entity))
            return;

        bool isAirUnit = em.HasComponent<UnitAirMovement>(entity);
        bool isSpawnTransit = em.HasComponent<UnitSpawnTransitTag>(entity);
        if (isAirUnit && !isSpawnTransit)
            return;

        UnitMoveOrderRequestSystem.EnqueueAndProcessTargetPathMoveOrder(em, entity, goalCell);
    }

    public void AlignNewestProducedUnitRotation(Context context, RuntimeBuildingEntity building, Vector3 forward)
    {
        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
            return;

        if (!TryGetNewestProducedUnit(context, building, em, out Entity entity) ||
            !em.HasComponent<LocalTransform>(entity))
        {
            return;
        }

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
            !IsPlayerProductionFocusAllowed(building))
        {
            return false;
        }

        if (!TryGetNewestProducedUnit(context, building, em, out Entity newest) ||
            !em.HasComponent<LocalTransform>(newest))
        {
            return false;
        }

        LocalTransform transform = em.GetComponentData<LocalTransform>(newest);
        context.SmoothMoveCameraGroundCenterTo(transform.Position);
        return true;
    }

    internal static bool TryGetNewestProducedUnit(Context context, RuntimeBuildingEntity building, EntityManager em, out Entity newest)
    {
        newest = Entity.Null;
        if (building == null || em.World == null || !em.World.IsCreated)
            return false;

        if (TryGetNewestProducedUnitFromReadModel(context, building.Id, em, out newest))
            return true;

        if (building.ProducedUnits == null || building.ProducedUnits.Count == 0)
            return false;

        for (int i = building.ProducedUnits.Count - 1; i >= 0; i--)
        {
            Entity candidate = building.ProducedUnits[i];
            if (candidate == Entity.Null || !em.Exists(candidate))
                continue;

            newest = candidate;
            return true;
        }

        return false;
    }

    private static bool TryGetNewestProducedUnitFromReadModel(
        Context context,
        int buildingRuntimeId,
        EntityManager em,
        out Entity newest)
    {
        newest = Entity.Null;
        if (buildingRuntimeId <= 0 ||
            context.SpawnContext.TryGetRuntimeBoundaryEntity == null ||
            !context.SpawnContext.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity) ||
            !em.HasBuffer<BuildingProducedUnitReadModel>(boundaryEntity))
        {
            return false;
        }

        DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
            em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
        for (int i = producedUnits.Length - 1; i >= 0; i--)
        {
            BuildingProducedUnitReadModel producedUnit = producedUnits[i];
            if (producedUnit.BuildingRuntimeId != buildingRuntimeId ||
                producedUnit.Unit == Entity.Null ||
                !em.Exists(producedUnit.Unit))
            {
                continue;
            }

            newest = producedUnit.Unit;
            return true;
        }

        return false;
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
