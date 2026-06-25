using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed class BuildingSpawnCompositionSystemHelper
{
    public delegate bool TryGetProductionSourceKeyDelegate(BuildingDefinition definition, int index, out FixedString64Bytes sourceKey);
    public delegate bool RuntimeBuildingMatchesIdDelegate(RuntimeBuildingEntity building, string normalizedBuildingId);
    public delegate bool TryGetRuntimeBoundaryEntityDelegate(EntityManager em, out Entity boundaryEntity);

    private const int MaxProductionSpawnRequestHistory = 256;

    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly EntityQuery LiveUnitFootprintQuery;
        public readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly BuildingSpawnPrefabSystem.Context SpawnPrefabContext;
        public readonly BuildingProductionSlotUtilitySystemHelper ProductionSlotSystem;
        public readonly TryGetProductionSourceKeyDelegate TryGetProductionSourceKey;
        public readonly RuntimeBuildingMatchesIdDelegate RuntimeBuildingMatchesId;
        public readonly TryGetRuntimeBoundaryEntityDelegate TryGetRuntimeBoundaryEntity;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            EntityQuery liveUnitFootprintQuery,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            BuildingSpawnPrefabSystem.Context spawnPrefabContext,
            BuildingProductionSlotUtilitySystemHelper productionSlotSystem,
            RuntimeBuildingMatchesIdDelegate runtimeBuildingMatchesId,
            TryGetProductionSourceKeyDelegate tryGetProductionSourceKey = null,
            TryGetRuntimeBoundaryEntityDelegate tryGetRuntimeBoundaryEntity = null)
        {
            RuntimeBuildings = runtimeBuildings;
            LiveUnitFootprintQuery = liveUnitFootprintQuery;
            ProductionSystem = productionSystem;
            SpawnPrefabSystem = spawnPrefabSystem;
            SpawnPrefabContext = spawnPrefabContext;
            ProductionSlotSystem = productionSlotSystem;
            TryGetProductionSourceKey = tryGetProductionSourceKey;
            RuntimeBuildingMatchesId = runtimeBuildingMatchesId;
            TryGetRuntimeBoundaryEntity = tryGetRuntimeBoundaryEntity;
        }
    }

    public void CleanupRecentSpawnReservations(float now)
    {
    }

    public bool TryResolveAvailableFactionHelipadSpawn(
        Context context,
        byte factionId,
        RuntimeBuildingEntity sourceBuilding,
        EntityManager em,
        Entity gridEntity,
        GridConfig grid,
        DynamicBlockerComponent blockerData,
        int2 unitFootprint,
        ref uint randomState,
        out int2 cell,
        out float3 worldPosition)
    {
        cell = default;
        worldPosition = default;

        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        try
        {
            ReserveRecentSpawnBuffers(context, em, ref reserved, grid);
            randomState = math.max(1u, randomState + 1u);
            var rng = new Unity.Mathematics.Random(randomState);
            return TryResolveHelicopterSpawnForFaction(
                context,
                factionId,
                sourceBuilding,
                em,
                ref rng,
                grid,
                walkable,
                blockerData.Blocked,
                occupied,
                ref reserved,
                unitFootprint,
                out cell,
                out worldPosition,
                out _,
                out _);
        }
        finally
        {
            if (reserved.IsCreated)
                reserved.Dispose();
        }
    }

    public bool TryGetFactionProductionSpawnPoint(
        Context context,
        byte factionId,
        string buildingId,
        int flattenedSlotIndex,
        GridConfig grid,
        out int2 cell,
        out float3 worldPosition)
    {
        return TryGetFactionProductionSpawnPointFromRuntimeBuildings(
            context,
            factionId,
            buildingId,
            flattenedSlotIndex,
            grid,
            out cell,
            out worldPosition);
    }

    public bool TryGetFactionProductionSpawnPoint(
        Context context,
        byte factionId,
        string buildingId,
        int flattenedSlotIndex,
        EntityManager em,
        GridConfig grid,
        out int2 cell,
        out float3 worldPosition)
    {
        if (TryGetFactionProductionSpawnPointFromReadModel(
                context,
                factionId,
                buildingId,
                flattenedSlotIndex,
                em,
                grid,
                out cell,
                out worldPosition))
        {
            return true;
        }

        return TryGetFactionProductionSpawnPointFromRuntimeBuildings(
            context,
            factionId,
            buildingId,
            flattenedSlotIndex,
            grid,
            out cell,
            out worldPosition);
    }

    private static bool TryGetFactionProductionSpawnPointFromReadModel(
        Context context,
        byte factionId,
        string buildingId,
        int flattenedSlotIndex,
        EntityManager em,
        GridConfig grid,
        out int2 cell,
        out float3 worldPosition)
    {
        cell = default;
        worldPosition = default;
        if (context.TryGetRuntimeBoundaryEntity == null ||
            em.World == null ||
            !em.World.IsCreated ||
            string.IsNullOrWhiteSpace(buildingId) ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity) ||
            !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
        {
            return false;
        }

        FixedString128Bytes normalizedBuildingId = new(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId));
        int remainingSlotIndex = math.max(0, flattenedSlotIndex);
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            BuildingFactionProductionSpawnPointReadModel spawnPoint = spawnPoints[i];
            if (spawnPoint.FactionId != factionId ||
                !spawnPoint.BuildingId.Equals(normalizedBuildingId))
            {
                continue;
            }

            if (remainingSlotIndex > 0)
            {
                remainingSlotIndex--;
                continue;
            }

            if (!GridUtils.InBounds(spawnPoint.Cell, grid.Width, grid.Height))
                return false;

            cell = spawnPoint.Cell;
            worldPosition = spawnPoint.WorldPosition;
            return true;
        }

        return false;
    }

    private static bool TryGetFactionProductionSpawnPointFromRuntimeBuildings(
        Context context,
        byte factionId,
        string buildingId,
        int flattenedSlotIndex,
        GridConfig grid,
        out int2 cell,
        out float3 worldPosition)
    {
        cell = default;
        worldPosition = default;
        if (context.RuntimeBuildings == null || string.IsNullOrWhiteSpace(buildingId))
            return false;

        int remainingSlotIndex = math.max(0, flattenedSlotIndex);
        string normalizedBuildingId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId);
        foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
        {
            RuntimeBuildingEntity building = entry.Value;
            if (building == null ||
                building.IsDestroyed ||
                !building.HasOwnerFaction ||
                building.OwnerFactionId != factionId ||
                building.Instance == null ||
                building.ProductionSpawnLocalPositions == null ||
                building.ProductionSpawnLocalPositions.Length == 0 ||
                context.RuntimeBuildingMatchesId == null ||
                !context.RuntimeBuildingMatchesId(building, normalizedBuildingId))
            {
                continue;
            }

            if (remainingSlotIndex >= building.ProductionSpawnLocalPositions.Length)
            {
                remainingSlotIndex -= building.ProductionSpawnLocalPositions.Length;
                continue;
            }

            Vector3 slotWorldPosition = building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[remainingSlotIndex]);
            cell = GridUtils.WorldToCell(grid, slotWorldPosition);
            worldPosition = slotWorldPosition;
            return GridUtils.InBounds(cell, grid.Width, grid.Height);
        }

        return false;
    }

    private static bool TryGetProductionSpawnPointFromReadModel(
        Context context,
        EntityManager em,
        int buildingRuntimeId,
        int slotIndex,
        GridConfig grid,
        out int2 cell,
        out float3 worldPosition)
    {
        cell = default;
        worldPosition = default;
        if (buildingRuntimeId <= 0 ||
            slotIndex < 0 ||
            context.TryGetRuntimeBoundaryEntity == null ||
            em.World == null ||
            !em.World.IsCreated ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity) ||
            !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
        {
            return false;
        }

        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            BuildingFactionProductionSpawnPointReadModel spawnPoint = spawnPoints[i];
            if (spawnPoint.BuildingRuntimeId != buildingRuntimeId ||
                spawnPoint.SlotIndex != slotIndex)
            {
                continue;
            }

            if (!GridUtils.InBounds(spawnPoint.Cell, grid.Width, grid.Height))
                return false;

            cell = spawnPoint.Cell;
            worldPosition = spawnPoint.WorldPosition;
            return true;
        }

        return false;
    }

    public bool TrySpawnPlayerUnitNearBuilding(
        Context context,
        RuntimeBuildingEntity building,
        int productionIndex,
        int reservedProductionSlotIndex,
        Vector3? overrideWorldPosition,
        int2? overrideCell,
        EntityManager em,
        Entity gridEntity,
        GridConfig grid,
        DynamicBlockerComponent blockerData,
        ref uint randomState)
    {
        if (building == null || building.Definition == null)
            return false;

        FixedString64Bytes spawnUnitSourceKey = GetProductionSourceKey(context, building.Definition, productionIndex);
        if (spawnUnitSourceKey.Length == 0)
            return false;

        if (!context.SpawnPrefabSystem.TryGetSpawnUnitPrefabEntity(context.SpawnPrefabContext, em, spawnUnitSourceKey, out Entity prefabEntity))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[BuildingSpawn] Could not resolve ECS prefab entity for source key '{spawnUnitSourceKey}' from building '{building.Definition.DisplayName}'.");
#endif
            return false;
        }

        if (!TryResolveSpawnPlacement(
                context,
                building,
                spawnUnitSourceKey,
                prefabEntity,
                reservedProductionSlotIndex,
                overrideWorldPosition,
                overrideCell,
                em,
                gridEntity,
                grid,
                blockerData,
                ref randomState,
                out int2 cell,
                out float3 pos,
                out int2 unitFootprint,
                out bool isAirUnit,
                out RuntimeBuildingEntity productionSlotBuilding,
                out int productionSlotIndex))
        {
            return false;
        }

        Entity instance = em.Instantiate(prefabEntity);
        if (!isAirUnit)
            new MapSurfaceSpawnGrounding().TryGroundCellCenter(em, grid, cell, ref pos, out _);
        em.SetComponentData(instance, new UnitGrid { Cell = cell });
        em.SetComponentData(instance, LocalTransform.FromPosition(pos));
        if (spawnUnitSourceKey.Length > 0)
            SetOrAddComponent(em, instance, new UnitSourcePrefabKey { Value = spawnUnitSourceKey });
        if (!isAirUnit)
        {
            ReserveDynamicOccupancy(em, gridEntity, grid, cell, unitFootprint);
            AddRecentSpawnReservation(context, em, cell, unitFootprint);
        }

        InitializeSpawnedUnit(em, instance, pos, cell, building, isAirUnit, ref randomState);
        bool publishedProducedUnitReadModel = PublishProducedUnitReadModel(
            context,
            em,
            building,
            productionSlotBuilding,
            productionIndex,
            productionSlotIndex,
            spawnUnitSourceKey,
            instance);
        if (!publishedProducedUnitReadModel)
        {
            building.ProducedUnits ??= new List<Entity>();
            building.ProducedUnits.Add(instance);
            if (productionSlotIndex >= 0 &&
                productionSlotBuilding?.ProducedUnitSlots != null &&
                productionSlotIndex < productionSlotBuilding.ProducedUnitSlots.Length)
            {
                productionSlotBuilding.ProducedUnitSlots[productionSlotIndex] = instance;
            }
        }

        PublishProductionSpawnRequest(
            context,
            em,
            building,
            productionIndex,
            reservedProductionSlotIndex,
            overrideWorldPosition.HasValue,
            overrideCell.HasValue,
            spawnUnitSourceKey,
            prefabEntity,
            instance,
            cell,
            pos);
        return true;
    }

    private bool TryResolveSpawnPlacement(
        Context context,
        RuntimeBuildingEntity building,
        FixedString64Bytes spawnUnitSourceKey,
        Entity prefabEntity,
        int reservedProductionSlotIndex,
        Vector3? overrideWorldPosition,
        int2? overrideCell,
        EntityManager em,
        Entity gridEntity,
        GridConfig grid,
        DynamicBlockerComponent blockerData,
        ref uint randomState,
        out int2 cell,
        out float3 pos,
        out int2 unitFootprint,
        out bool isAirUnit,
        out RuntimeBuildingEntity productionSlotBuilding,
        out int productionSlotIndex)
    {
        pos = default;
        unitFootprint = em.HasComponent<UnitFootprint>(prefabEntity)
            ? em.GetComponentData<UnitFootprint>(prefabEntity).Size
            : new int2(1, 1);
        isAirUnit = em.HasComponent<UnitAirMovement>(prefabEntity);
        bool isHelicopter = IsHelicopterSourceKey(spawnUnitSourceKey);
        bool useHelicopterSpawnResolver =
            !overrideWorldPosition.HasValue &&
            !overrideCell.HasValue &&
            isAirUnit &&
            isHelicopter &&
            building.HasOwnerFaction;
        bool useOverrideHelicopterSpawn =
            overrideWorldPosition.HasValue &&
            overrideCell.HasValue &&
            isAirUnit &&
            isHelicopter;
        productionSlotIndex = -1;
        Vector3 productionSpawnLocalPosition = Vector3.zero;
        productionSlotBuilding = building;
        bool hasProductionSpawnSlots = false;
        bool hasProductionSpawnPointFromReadModel = false;
        int2 productionSpawnCell = default;
        float3 productionSpawnWorldPosition = default;
        bool canUseProductionSpawnSlots = !useHelicopterSpawnResolver && !useOverrideHelicopterSpawn;
        if (canUseProductionSpawnSlots)
        {
            if (reservedProductionSlotIndex >= 0)
            {
                if (TryGetProductionSpawnPointFromReadModel(
                        context,
                        em,
                        building.Id,
                        reservedProductionSlotIndex,
                        grid,
                        out productionSpawnCell,
                        out productionSpawnWorldPosition))
                {
                    productionSlotIndex = reservedProductionSlotIndex;
                    hasProductionSpawnSlots = true;
                    hasProductionSpawnPointFromReadModel = true;
                }
                else if (TryGetLegacyProductionSpawnLocalPosition(building, reservedProductionSlotIndex, out productionSpawnLocalPosition))
                {
                    productionSlotIndex = reservedProductionSlotIndex;
                    hasProductionSpawnSlots = true;
                }
            }

            if (!hasProductionSpawnSlots &&
                TryGetAvailableProductionSpawnPointFromReadModel(
                    context,
                    em,
                    building,
                    grid,
                    out productionSlotIndex,
                    out productionSpawnCell,
                    out productionSpawnWorldPosition))
            {
                hasProductionSpawnSlots = true;
                hasProductionSpawnPointFromReadModel = true;
            }
            else if (!hasProductionSpawnSlots &&
                     context.ProductionSlotSystem != null &&
                     context.ProductionSlotSystem.TryGetAvailableProductionSpawnSlot(building, em, out productionSlotIndex, out productionSpawnLocalPosition))
            {
                hasProductionSpawnSlots = true;
            }
            else if (!hasProductionSpawnSlots &&
                     (HasAnyProductionSpawnPointInReadModel(context, em, building.Id) ||
                      (building.ProductionSpawnLocalPositions != null && building.ProductionSpawnLocalPositions.Length > 0)))
            {
                cell = default;
                pos = default;
                return false;
            }
        }

        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        try
        {
            randomState = math.max(1u, randomState + 1u);
            var rng = new Unity.Mathematics.Random(randomState);
            Vector2Int size = building.Definition.FootprintCells;
            ReserveBuildingBuffer(ref reserved, grid, building.OriginCell, size, 1);
            ReserveRecentSpawnBuffers(context, em, ref reserved, grid);
            int2 center = new(building.OriginCell.x + size.x / 2, building.OriginCell.y + size.y / 2);
            cell = center;
            if (overrideWorldPosition.HasValue && overrideCell.HasValue)
            {
                pos = overrideWorldPosition.Value;
                cell = overrideCell.Value;
                if (useOverrideHelicopterSpawn)
                    TryResolveProductionSlotAtCell(context, em, grid, cell, unitFootprint, out productionSlotBuilding, out productionSlotIndex);
            }
            else if (useHelicopterSpawnResolver)
            {
                if (!TryResolveHelicopterSpawnForFaction(
                        context,
                        building.OwnerFactionId,
                        building,
                        em,
                        ref rng,
                        grid,
                        walkable,
                        blockerData.Blocked,
                        occupied,
                        ref reserved,
                        unitFootprint,
                        out cell,
                        out pos,
                        out productionSlotBuilding,
                        out productionSlotIndex))
                {
                    return false;
                }
            }
            else if (hasProductionSpawnSlots)
            {
                if (hasProductionSpawnPointFromReadModel)
                {
                    cell = productionSpawnCell;
                    pos = productionSpawnWorldPosition;
                }
                else if (!TryGetProductionSpawnPointFromReadModel(
                        context,
                        em,
                        building.Id,
                        productionSlotIndex,
                        grid,
                        out cell,
                        out pos))
                {
                    pos = building.Instance != null
                        ? (float3)building.Instance.transform.TransformPoint(productionSpawnLocalPosition)
                        : (float3)productionSpawnLocalPosition;
                    cell = GridUtils.WorldToCell(grid, pos);
                }
                if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                    return false;

                if (!isAirUnit)
                {
                    bool slotCellAvailable =
                        TryReserveSpawnCandidate(grid, walkable, blockerData.Blocked, occupied, ref reserved, cell, unitFootprint) &&
                        !OverlapsRecentSpawnReservation(context, em, cell, unitFootprint) &&
                        !OverlapsExistingUnitFootprint(context, em, cell, unitFootprint);

                    if (!slotCellAvailable)
                    {
                        int radius = math.max(size.x, size.y) + math.max(unitFootprint.x, unitFootprint.y) + 6;
                        bool foundNearby = TryFindStrictSpawnCell(
                            context,
                            em,
                            ref rng,
                            grid,
                            walkable,
                            blockerData.Blocked,
                            occupied,
                            ref reserved,
                            cell,
                            radius,
                            unitFootprint,
                            out cell);
                        if (!foundNearby)
                            return false;

                        pos = GridUtils.CellToWorldCenter(grid, cell);
                    }
                    else
                    {
                        pos = GridUtils.CellToWorldCenter(grid, cell);
                    }
                }
            }
            else if (isAirUnit)
            {
                int frontX = math.clamp(building.OriginCell.x + size.x / 2, 0, grid.Width - 1);
                int frontY = math.clamp(building.OriginCell.y + size.y, 0, grid.Height - 1);
                cell = new int2(frontX, frontY);
                pos = GridUtils.CellToWorldCenter(grid, cell);
            }
            else
            {
                int radius = math.max(size.x, size.y) + 4;
                bool foundAdjacent = TryFindStrictSpawnCellAdjacentToBuilding(
                    context,
                    em,
                    ref rng,
                    grid,
                    walkable,
                    blockerData.Blocked,
                    occupied,
                    ref reserved,
                    building.OriginCell,
                    size,
                    unitFootprint,
                    out cell);
                if (!foundAdjacent &&
                    !TryFindStrictSpawnCell(context, em, ref rng, grid, walkable, blockerData.Blocked, occupied, ref reserved, center, radius + math.max(unitFootprint.x, unitFootprint.y), unitFootprint, out cell))
                    return false;

                pos = GridUtils.CellToWorldCenter(grid, cell);
            }

            return true;
        }
        finally
        {
            reserved.Dispose();
        }
    }

    private static bool TryGetLegacyProductionSpawnLocalPosition(
        RuntimeBuildingEntity building,
        int slotIndex,
        out Vector3 productionSpawnLocalPosition)
    {
        productionSpawnLocalPosition = Vector3.zero;
        if (building?.ProductionSpawnLocalPositions == null ||
            building.ProducedUnitSlots == null ||
            slotIndex < 0 ||
            slotIndex >= building.ProductionSpawnLocalPositions.Length ||
            slotIndex >= building.ProducedUnitSlots.Length)
        {
            return false;
        }

        productionSpawnLocalPosition = building.ProductionSpawnLocalPositions[slotIndex];
        return true;
    }

    private static bool TryGetAvailableProductionSpawnPointFromReadModel(
        Context context,
        EntityManager em,
        RuntimeBuildingEntity building,
        GridConfig grid,
        out int slotIndex,
        out int2 cell,
        out float3 worldPosition)
    {
        slotIndex = -1;
        cell = default;
        worldPosition = default;
        if (building == null ||
            building.Id <= 0 ||
            context.TryGetRuntimeBoundaryEntity == null ||
            context.ProductionSlotSystem == null ||
            em.World == null ||
            !em.World.IsCreated ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity) ||
            !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
        {
            return false;
        }

        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            BuildingFactionProductionSpawnPointReadModel spawnPoint = spawnPoints[i];
            if (spawnPoint.BuildingRuntimeId != building.Id ||
                spawnPoint.SlotIndex < 0 ||
                !GridUtils.InBounds(spawnPoint.Cell, grid.Width, grid.Height))
            {
                continue;
            }

            if (context.ProductionSlotSystem.IsProductionSlotReservedByPending(building, spawnPoint.SlotIndex))
                continue;
            if (IsProductionSlotOccupied(context, em, building, spawnPoint.SlotIndex))
                continue;

            slotIndex = spawnPoint.SlotIndex;
            cell = spawnPoint.Cell;
            worldPosition = spawnPoint.WorldPosition;
            return true;
        }

        return false;
    }

    private static bool HasAnyProductionSpawnPointInReadModel(
        Context context,
        EntityManager em,
        int buildingRuntimeId)
    {
        if (buildingRuntimeId <= 0 ||
            context.TryGetRuntimeBoundaryEntity == null ||
            em.World == null ||
            !em.World.IsCreated ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity) ||
            !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
        {
            return false;
        }

        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i].BuildingRuntimeId == buildingRuntimeId)
                return true;
        }

        return false;
    }

    private static bool TryResolveProductionSlotAtCell(
        Context context,
        EntityManager em,
        GridConfig grid,
        int2 targetCell,
        int2 unitFootprint,
        out RuntimeBuildingEntity slotBuilding,
        out int slotIndex)
    {
        slotBuilding = null;
        slotIndex = -1;
        if (context.RuntimeBuildings == null || context.ProductionSlotSystem == null)
            return false;

        if (TryResolveProductionSlotAtCellFromReadModel(
                context,
                em,
                grid,
                targetCell,
                unitFootprint,
                out slotBuilding,
                out slotIndex))
        {
            return true;
        }

        string helipadKey = NormalizeSpawnableKey("Building_Helipad");
        foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
        {
            RuntimeBuildingEntity building = entry.Value;
            if (building == null ||
                building.Instance == null ||
                building.ProductionSpawnLocalPositions == null ||
                building.ProducedUnitSlots == null ||
                !context.RuntimeBuildingMatchesId(building, helipadKey))
            {
                continue;
            }

            int count = math.min(building.ProductionSpawnLocalPositions.Length, building.ProducedUnitSlots.Length);
            for (int i = 0; i < count; i++)
            {
                if (context.ProductionSlotSystem.IsProductionSlotReservedByPending(building, i))
                    continue;
                if (IsProductionSlotOccupied(context, em, building, i))
                    continue;

                Vector3 candidateWorld = building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[i]);
                int2 candidateCell = GridUtils.WorldToCell(grid, candidateWorld);
                if (!FootprintsOverlap(targetCell, unitFootprint, candidateCell, new int2(1, 1)))
                    continue;

                slotBuilding = building;
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveProductionSlotAtCellFromReadModel(
        Context context,
        EntityManager em,
        GridConfig grid,
        int2 targetCell,
        int2 unitFootprint,
        out RuntimeBuildingEntity slotBuilding,
        out int slotIndex)
    {
        slotBuilding = null;
        slotIndex = -1;
        if (context.RuntimeBuildings == null ||
            context.ProductionSlotSystem == null ||
            context.TryGetRuntimeBoundaryEntity == null ||
            em.World == null ||
            !em.World.IsCreated ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity) ||
            !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
        {
            return false;
        }

        FixedString128Bytes helipadId = new(NormalizeSpawnableKey("Building_Helipad"));
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            BuildingFactionProductionSpawnPointReadModel spawnPoint = spawnPoints[i];
            if (!spawnPoint.BuildingId.Equals(helipadId) ||
                spawnPoint.BuildingRuntimeId <= 0 ||
                spawnPoint.SlotIndex < 0 ||
                !GridUtils.InBounds(spawnPoint.Cell, grid.Width, grid.Height) ||
                !FootprintsOverlap(targetCell, unitFootprint, spawnPoint.Cell, new int2(1, 1)) ||
                !context.RuntimeBuildings.TryGetValue(spawnPoint.BuildingRuntimeId, out RuntimeBuildingEntity building) ||
                building == null ||
                (building.ProducedUnitSlots != null && spawnPoint.SlotIndex >= building.ProducedUnitSlots.Length))
            {
                continue;
            }

            if (context.ProductionSlotSystem.IsProductionSlotReservedByPending(building, spawnPoint.SlotIndex))
                continue;
            if (IsProductionSlotOccupied(context, em, building, spawnPoint.SlotIndex))
                continue;

            slotBuilding = building;
            slotIndex = spawnPoint.SlotIndex;
            return true;
        }

        return false;
    }

    private static bool IsProductionSlotOccupied(
        Context context,
        EntityManager em,
        RuntimeBuildingEntity building,
        int slotIndex)
    {
        if (building == null || slotIndex < 0)
            return false;

        if (context.ProductionSlotSystem != null &&
            building.ProducedUnitSlots != null &&
            context.ProductionSlotSystem.IsProductionSlotOccupied(building, em, slotIndex))
        {
            return true;
        }

        return IsProductionSlotOccupiedByReadModel(context, em, building.Id, slotIndex);
    }

    private static bool IsProductionSlotOccupiedByReadModel(
        Context context,
        EntityManager em,
        int productionSlotBuildingRuntimeId,
        int slotIndex)
    {
        if (productionSlotBuildingRuntimeId <= 0 ||
            slotIndex < 0 ||
            context.TryGetRuntimeBoundaryEntity == null ||
            em.World == null ||
            !em.World.IsCreated ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity) ||
            !em.HasBuffer<BuildingProducedUnitReadModel>(boundaryEntity))
        {
            return false;
        }

        DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
            em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
        for (int i = 0; i < producedUnits.Length; i++)
        {
            BuildingProducedUnitReadModel producedUnit = producedUnits[i];
            int slotBuildingRuntimeId = producedUnit.ProductionSlotBuildingRuntimeId > 0
                ? producedUnit.ProductionSlotBuildingRuntimeId
                : producedUnit.BuildingRuntimeId;
            if (slotBuildingRuntimeId != productionSlotBuildingRuntimeId ||
                producedUnit.ProductionSlotIndex != slotIndex ||
                !IsProducedUnitAlive(producedUnit.Unit, em))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool IsProducedUnitAlive(Entity unit, EntityManager em)
    {
        if (unit == Entity.Null || !em.Exists(unit))
            return false;

        return !em.HasComponent<UnitHealth>(unit) ||
               em.GetComponentData<UnitHealth>(unit).Current > 0;
    }

    private static bool FootprintsOverlap(int2 aCell, int2 aSize, int2 bCell, int2 bSize)
    {
        return aCell.x < bCell.x + bSize.x &&
               aCell.x + aSize.x > bCell.x &&
               aCell.y < bCell.y + bSize.y &&
               aCell.y + aSize.y > bCell.y;
    }

    private static void InitializeSpawnedUnit(
        EntityManager em,
        Entity instance,
        float3 pos,
        int2 cell,
        RuntimeBuildingEntity building,
        bool isAirUnit,
        ref uint randomState)
    {
        if (em.HasComponent<UnitGridInitialized>(instance))
            em.RemoveComponent<UnitGridInitialized>(instance);
        if (em.HasComponent<UnitPrevWorldPos>(instance))
            em.SetComponentData(instance, new UnitPrevWorldPos { Value = pos });
        if (em.HasComponent<UnitAirComponent>(instance))
        {
            em.SetComponentData(instance, new UnitAirComponent
            {
                HomePosition = pos,
                HomeCell = cell,
                HomeInitialized = 1,
                ReturningHome = 0,
                Airborne = 0
            });
        }
        if (em.HasComponent<UnitMoveVisualComponent>(instance))
            em.SetComponentData(instance, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        if (em.HasComponent<Faction>(instance))
            em.SetComponentData(instance, new Faction { Id = ResolveProducedUnitFaction(building) });
        if (em.HasComponent<UnitRespawnPrefab>(instance))
            em.SetComponentData(instance, new UnitRespawnPrefab { Prefab = Entity.Null });
        if (em.HasComponent<UnitAttackCooldownComponent>(instance))
            em.SetComponentData(instance, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
        if (em.HasComponent<UnitIdleWanderComponent>(instance))
        {
            randomState = math.max(1u, randomState + 1u);
            em.SetComponentData(instance, new UnitIdleWanderComponent
            {
                RandomState = randomState,
                RetrySeconds = 0f,
                CurrentIdleDelaySeconds = 0f
            });
        }
        if (em.HasComponent<UnitMovementBehavior>(instance) && em.GetComponentData<UnitMovementBehavior>(instance).AllowIdleWander == 0)
        {
            if (em.HasComponent<AutoWanderMoveTag>(instance))
                em.RemoveComponent<AutoWanderMoveTag>(instance);
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
        if (em.HasComponent<SelectedUnitTag>(instance))
            em.RemoveComponent<SelectedUnitTag>(instance);
    }

    internal static byte ResolveProducedUnitFaction(RuntimeBuildingEntity building)
    {
        if (building == null || !building.HasOwnerFaction || building.OwnerFactionId == FactionIdentity.NeutralFactionId)
            return FactionIdentity.PlayerFactionId;

        return building.OwnerFactionId;
    }

    private void ReserveRecentSpawnBuffers(Context context, EntityManager em, ref NativeBitArray reserved, GridConfig grid)
    {
        float now = UnityEngine.Time.time;
        if (TryGetRecentSpawnReservationBuffer(context, em, createIfMissing: false, out DynamicBuffer<BuildingRecentSpawnReservation> boundaryReservations))
        {
            CleanupBoundaryRecentSpawnReservations(boundaryReservations, now);
            for (int i = 0; i < boundaryReservations.Length; i++)
                ReserveRecentSpawnBuffer(ref reserved, grid, boundaryReservations[i].Cell, boundaryReservations[i].Size);
        }
    }

    private static void ReserveRecentSpawnBuffer(ref NativeBitArray reserved, GridConfig grid, int2 cell, int2 footprintSize)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        for (int y = min.y; y < max.y; y++)
        {
            if ((uint)y >= (uint)grid.Height)
                continue;

            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                if ((uint)x >= (uint)grid.Width)
                    continue;

                reserved.Set(row + x, true);
            }
        }
    }

    private void AddRecentSpawnReservation(Context context, EntityManager em, int2 cell, int2 size)
    {
        int2 clampedSize = UnitFootprintUtility.ClampSize(size);
        float expiresAt = UnityEngine.Time.time + 0.5f;
        if (TryGetRecentSpawnReservationBuffer(context, em, createIfMissing: true, out DynamicBuffer<BuildingRecentSpawnReservation> boundaryReservations))
        {
            CleanupBoundaryRecentSpawnReservations(boundaryReservations, UnityEngine.Time.time);
            boundaryReservations.Add(new BuildingRecentSpawnReservation
            {
                Cell = cell,
                Size = clampedSize,
                ExpiresAt = expiresAt
            });
            return;
        }
    }

    private bool OverlapsRecentSpawnReservation(Context context, EntityManager em, int2 cell, int2 size)
    {
        float now = UnityEngine.Time.time;
        int2 clampedSize = UnitFootprintUtility.ClampSize(size);
        if (TryGetRecentSpawnReservationBuffer(context, em, createIfMissing: false, out DynamicBuffer<BuildingRecentSpawnReservation> boundaryReservations))
        {
            CleanupBoundaryRecentSpawnReservations(boundaryReservations, now);
            for (int i = 0; i < boundaryReservations.Length; i++)
            {
                BuildingRecentSpawnReservation reservation = boundaryReservations[i];
                if (UnitFootprintUtility.Overlaps(cell, clampedSize, reservation.Cell, UnitFootprintUtility.ClampSize(reservation.Size)))
                    return true;
            }

            return false;
        }

        return false;
    }

    private static bool TryGetRecentSpawnReservationBuffer(
        Context context,
        EntityManager em,
        bool createIfMissing,
        out DynamicBuffer<BuildingRecentSpawnReservation> reservations)
    {
        reservations = default;
        if (context.TryGetRuntimeBoundaryEntity == null ||
            em.World == null ||
            !em.World.IsCreated ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity))
        {
            return false;
        }

        if (!em.HasBuffer<BuildingRecentSpawnReservation>(boundaryEntity) && createIfMissing)
            em.AddBuffer<BuildingRecentSpawnReservation>(boundaryEntity);
        if (!em.HasBuffer<BuildingRecentSpawnReservation>(boundaryEntity))
            return false;

        reservations = em.GetBuffer<BuildingRecentSpawnReservation>(boundaryEntity);
        return true;
    }

    private static void CleanupBoundaryRecentSpawnReservations(DynamicBuffer<BuildingRecentSpawnReservation> reservations, float now)
    {
        for (int i = reservations.Length - 1; i >= 0; i--)
        {
            if (reservations[i].ExpiresAt > now)
                continue;

            reservations.RemoveAt(i);
        }
    }

    private bool TryResolveHelicopterSpawnForFaction(
        Context context,
        byte factionId,
        RuntimeBuildingEntity sourceBuilding,
        EntityManager em,
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        int2 unitFootprint,
        out int2 cell,
        out float3 worldPosition,
        out RuntimeBuildingEntity slotBuilding,
        out int slotIndex)
    {
        cell = default;
        worldPosition = default;
        slotBuilding = null;
        slotIndex = -1;

        bool foundHelipad = false;
        bool hasSourcePosition = sourceBuilding?.Instance != null;
        Vector3 sourcePosition = hasSourcePosition ? sourceBuilding.Instance.transform.position : Vector3.zero;
        bool hasBestHelipadSlot = false;
        float bestHelipadSlotDistanceSq = float.MaxValue;
        int2 bestHelipadSlotCell = default;
        float3 bestHelipadSlotWorldPosition = default;
        RuntimeBuildingEntity bestHelipadSlotBuilding = null;
        int bestHelipadSlotIndex = -1;
        int2 helipadSearchCenter = default;
        int helipadSearchRadius = 0;
        string helipadKey = NormalizeSpawnableKey("Building_Helipad");

        if (TryResolveHelicopterSpawnForFactionFromReadModel(
                context,
                factionId,
                sourceBuilding,
                em,
                grid,
                unitFootprint,
                out cell,
                out worldPosition,
                out slotBuilding,
                out slotIndex,
                out bool foundReadModelHelipad,
                out int2 readModelHelipadSearchCenter,
                out int readModelHelipadSearchRadius))
        {
            return true;
        }

        if (foundReadModelHelipad)
        {
            foundHelipad = true;
            helipadSearchCenter = readModelHelipadSearchCenter;
            helipadSearchRadius = readModelHelipadSearchRadius;
        }

        foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
        {
            RuntimeBuildingEntity building = entry.Value;
            if (!IsOwnedRuntimeBuildingForFaction(building, factionId) ||
                building.Instance == null ||
                building.ProductionSpawnLocalPositions == null ||
                building.ProductionSpawnLocalPositions.Length == 0 ||
                !context.RuntimeBuildingMatchesId(building, helipadKey))
                continue;

            foundHelipad = true;
            Vector2Int footprint = building.Definition != null ? building.Definition.FootprintCells : Vector2Int.one;
            int2 buildingCenter = new(building.OriginCell.x + footprint.x / 2, building.OriginCell.y + footprint.y / 2);
            if (helipadSearchRadius == 0)
                helipadSearchCenter = buildingCenter;
            helipadSearchRadius = math.max(helipadSearchRadius, math.max(footprint.x, footprint.y) + math.max(unitFootprint.x, unitFootprint.y) + 12);

            int count = building.ProducedUnitSlots != null && building.ProducedUnitSlots.Length > 0
                ? math.min(building.ProductionSpawnLocalPositions.Length, building.ProducedUnitSlots.Length)
                : building.ProductionSpawnLocalPositions.Length;
            for (int i = 0; i < count; i++)
            {
                if (context.ProductionSlotSystem.IsProductionSlotReservedByPending(building, i))
                    continue;
                if (IsProductionSlotOccupied(context, em, building, i))
                    continue;

                Vector3 candidateWorld = building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[i]);
                int2 candidateCell = GridUtils.WorldToCell(grid, candidateWorld);
                if (!GridUtils.InBounds(candidateCell, grid.Width, grid.Height))
                    continue;
                if (OverlapsRecentSpawnReservation(context, em, candidateCell, unitFootprint))
                    continue;
                if (OverlapsExistingUnitFootprint(context, em, candidateCell, unitFootprint))
                    continue;

                if (!hasSourcePosition)
                {
                    cell = candidateCell;
                    worldPosition = candidateWorld;
                    slotBuilding = building;
                    slotIndex = i;
                    return true;
                }

                float distanceSq = (candidateWorld - sourcePosition).sqrMagnitude;
                if (hasBestHelipadSlot && distanceSq >= bestHelipadSlotDistanceSq)
                    continue;

                hasBestHelipadSlot = true;
                bestHelipadSlotDistanceSq = distanceSq;
                bestHelipadSlotCell = candidateCell;
                bestHelipadSlotWorldPosition = candidateWorld;
                bestHelipadSlotBuilding = building;
                bestHelipadSlotIndex = i;
            }
        }

        if (hasBestHelipadSlot)
        {
            cell = bestHelipadSlotCell;
            worldPosition = bestHelipadSlotWorldPosition;
            slotBuilding = bestHelipadSlotBuilding;
            slotIndex = bestHelipadSlotIndex;
            return true;
        }

        if (foundHelipad)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity building = entry.Value;
                if (!IsOwnedRuntimeBuildingForFaction(building, factionId) || !context.RuntimeBuildingMatchesId(building, helipadKey))
                    continue;

                Vector2Int footprint = building.Definition != null ? building.Definition.FootprintCells : Vector2Int.one;
                int2 center = new(building.OriginCell.x + footprint.x / 2, building.OriginCell.y + footprint.y / 2);
                int radius = math.max(footprint.x, footprint.y) + math.max(unitFootprint.x, unitFootprint.y) + 10;
                if (TryFindStrictSpawnCell(context, em, ref rng, grid, walkable, blocked, occupied, ref reserved, center, radius, unitFootprint, out cell))
                {
                    worldPosition = GridUtils.CellToWorldCenter(grid, cell);
                    return true;
                }
            }

            if (TryFindStrictSpawnCell(context, em, ref rng, grid, walkable, blocked, occupied, ref reserved, helipadSearchCenter, helipadSearchRadius + 24, unitFootprint, out cell))
            {
                worldPosition = GridUtils.CellToWorldCenter(grid, cell);
                return true;
            }
        }

        if (TryGetFactionRuntimeBuildingCenter(context, factionId, sourceBuilding, out int2 baseCenter))
        {
            int baseRadius = foundHelipad ? 96 : 140;
            if (TryFindStrictSpawnCell(context, em, ref rng, grid, walkable, blocked, occupied, ref reserved, baseCenter, baseRadius, unitFootprint, out cell))
            {
                worldPosition = GridUtils.CellToWorldCenter(grid, cell);
                return true;
            }
        }

        return false;
    }

    private bool TryResolveHelicopterSpawnForFactionFromReadModel(
        Context context,
        byte factionId,
        RuntimeBuildingEntity sourceBuilding,
        EntityManager em,
        in GridConfig grid,
        int2 unitFootprint,
        out int2 cell,
        out float3 worldPosition,
        out RuntimeBuildingEntity slotBuilding,
        out int slotIndex,
        out bool foundHelipad,
        out int2 helipadSearchCenter,
        out int helipadSearchRadius)
    {
        cell = default;
        worldPosition = default;
        slotBuilding = null;
        slotIndex = -1;
        foundHelipad = false;
        helipadSearchCenter = default;
        helipadSearchRadius = 0;
        if (context.RuntimeBuildings == null ||
            context.ProductionSlotSystem == null ||
            context.TryGetRuntimeBoundaryEntity == null ||
            em.World == null ||
            !em.World.IsCreated ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity) ||
            !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
        {
            return false;
        }

        bool hasSourcePosition = TryGetRuntimeBuildingSourcePositionFromReadModel(
            context,
            em,
            sourceBuilding != null ? sourceBuilding.Id : 0,
            out float3 sourcePosition);
        bool hasBestHelipadSlot = false;
        float bestHelipadSlotDistanceSq = float.MaxValue;
        int2 bestHelipadSlotCell = default;
        float3 bestHelipadSlotWorldPosition = default;
        RuntimeBuildingEntity bestHelipadSlotBuilding = null;
        int bestHelipadSlotIndex = -1;
        FixedString128Bytes helipadId = new(NormalizeSpawnableKey("Building_Helipad"));
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            BuildingFactionProductionSpawnPointReadModel spawnPoint = spawnPoints[i];
            if (spawnPoint.FactionId != factionId ||
                !spawnPoint.BuildingId.Equals(helipadId) ||
                spawnPoint.BuildingRuntimeId <= 0 ||
                spawnPoint.SlotIndex < 0 ||
                !GridUtils.InBounds(spawnPoint.Cell, grid.Width, grid.Height) ||
                !context.RuntimeBuildings.TryGetValue(spawnPoint.BuildingRuntimeId, out RuntimeBuildingEntity building) ||
                !IsOwnedRuntimeBuildingForFaction(building, factionId) ||
                (building.ProducedUnitSlots != null && spawnPoint.SlotIndex >= building.ProducedUnitSlots.Length))
            {
                continue;
            }

            foundHelipad = true;
            Vector2Int footprint = building.Definition != null ? building.Definition.FootprintCells : Vector2Int.one;
            int2 buildingCenter = new(building.OriginCell.x + footprint.x / 2, building.OriginCell.y + footprint.y / 2);
            if (helipadSearchRadius == 0)
                helipadSearchCenter = buildingCenter;
            helipadSearchRadius = math.max(
                helipadSearchRadius,
                math.max(footprint.x, footprint.y) + math.max(unitFootprint.x, unitFootprint.y) + 12);

            if (context.ProductionSlotSystem.IsProductionSlotReservedByPending(building, spawnPoint.SlotIndex))
                continue;
            if (IsProductionSlotOccupied(context, em, building, spawnPoint.SlotIndex))
                continue;
            if (OverlapsRecentSpawnReservation(context, em, spawnPoint.Cell, unitFootprint))
                continue;
            if (OverlapsExistingUnitFootprint(context, em, spawnPoint.Cell, unitFootprint))
                continue;

            if (!hasSourcePosition)
            {
                cell = spawnPoint.Cell;
                worldPosition = spawnPoint.WorldPosition;
                slotBuilding = building;
                slotIndex = spawnPoint.SlotIndex;
                return true;
            }

            float distanceSq = math.distancesq(spawnPoint.WorldPosition, sourcePosition);
            if (hasBestHelipadSlot && distanceSq >= bestHelipadSlotDistanceSq)
                continue;

            hasBestHelipadSlot = true;
            bestHelipadSlotDistanceSq = distanceSq;
            bestHelipadSlotCell = spawnPoint.Cell;
            bestHelipadSlotWorldPosition = spawnPoint.WorldPosition;
            bestHelipadSlotBuilding = building;
            bestHelipadSlotIndex = spawnPoint.SlotIndex;
        }

        if (!hasBestHelipadSlot)
            return false;

        cell = bestHelipadSlotCell;
        worldPosition = bestHelipadSlotWorldPosition;
        slotBuilding = bestHelipadSlotBuilding;
        slotIndex = bestHelipadSlotIndex;
        return true;
    }

    private static bool TryGetRuntimeBuildingSourcePositionFromReadModel(
        Context context,
        EntityManager em,
        int buildingRuntimeId,
        out float3 sourcePosition)
    {
        sourcePosition = default;
        if (buildingRuntimeId <= 0 ||
            context.TryGetRuntimeBoundaryEntity == null ||
            em.World == null ||
            !em.World.IsCreated ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity) ||
            !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
        {
            return false;
        }

        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            BuildingFactionProductionSpawnPointReadModel spawnPoint = spawnPoints[i];
            if (spawnPoint.BuildingRuntimeId != buildingRuntimeId)
                continue;

            sourcePosition = spawnPoint.WorldPosition;
            return true;
        }

        return false;
    }

    private bool TryFindStrictSpawnCell(
        Context context,
        EntityManager em,
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        int2 center,
        int radiusCells,
        int2 footprintSize,
        out int2 result)
    {
        result = default;
        const int randomTries = 192;
        for (int i = 0; i < randomTries; i++)
        {
            int2 candidate = new(
                center.x + rng.NextInt(-radiusCells, radiusCells + 1),
                center.y + rng.NextInt(-radiusCells, radiusCells + 1));

            if (!TryReserveSpawnCandidate(grid, walkable, blocked, occupied, ref reserved, candidate, footprintSize))
                continue;
            if (OverlapsRecentSpawnReservation(context, em, candidate, footprintSize))
                continue;
            if (OverlapsExistingUnitFootprint(context, em, candidate, footprintSize))
                continue;

            result = candidate;
            return true;
        }

        int maxRadius = math.max(8, radiusCells + 32);
        for (int r = 0; r <= maxRadius; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (math.abs(dx) != r && math.abs(dy) != r)
                        continue;

                    int2 candidate = new(center.x + dx, center.y + dy);
                    if (!TryReserveSpawnCandidate(grid, walkable, blocked, occupied, ref reserved, candidate, footprintSize))
                        continue;
                    if (OverlapsRecentSpawnReservation(context, em, candidate, footprintSize))
                        continue;
                    if (OverlapsExistingUnitFootprint(context, em, candidate, footprintSize))
                        continue;

                    result = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryFindStrictSpawnCellAdjacentToBuilding(
        Context context,
        EntityManager em,
        ref Unity.Mathematics.Random rng,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        Vector2Int originCell,
        Vector2Int footprintCells,
        int2 unitFootprint,
        out int2 result)
    {
        result = default;
        int maxExtraRadius = math.max(6, math.max(unitFootprint.x, unitFootprint.y) + 2);
        for (int extraRadius = 1; extraRadius <= maxExtraRadius; extraRadius++)
        {
            var candidates = new NativeList<int2>(Allocator.Temp);
            try
            {
                int minX = originCell.x - extraRadius;
                int minY = originCell.y - extraRadius;
                int maxX = originCell.x + footprintCells.x - 1 + extraRadius;
                int maxY = originCell.y + footprintCells.y - 1 + extraRadius;

                for (int x = minX; x <= maxX; x++)
                {
                    candidates.Add(new int2(x, minY));
                    if (maxY != minY)
                        candidates.Add(new int2(x, maxY));
                }

                for (int y = minY + 1; y < maxY; y++)
                {
                    candidates.Add(new int2(minX, y));
                    if (maxX != minX)
                        candidates.Add(new int2(maxX, y));
                }

                if (candidates.Length == 0)
                    continue;

                int startIndex = rng.NextInt(candidates.Length);
                for (int offset = 0; offset < candidates.Length; offset++)
                {
                    int2 candidate = candidates[(startIndex + offset) % candidates.Length];
                    if (!TryReserveSpawnCandidate(grid, walkable, blocked, occupied, ref reserved, candidate, unitFootprint))
                        continue;
                    if (OverlapsRecentSpawnReservation(context, em, candidate, unitFootprint))
                        continue;
                    if (OverlapsExistingUnitFootprint(context, em, candidate, unitFootprint))
                        continue;

                    result = candidate;
                    return true;
                }
            }
            finally
            {
                if (candidates.IsCreated)
                    candidates.Dispose();
            }
        }

        return false;
    }

    private bool OverlapsExistingUnitFootprint(Context context, EntityManager em, int2 cell, int2 size)
    {
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<UnitGrid> unitGridType = em.GetComponentTypeHandle<UnitGrid>(true);
        ComponentTypeHandle<UnitFootprint> footprintType = em.GetComponentTypeHandle<UnitFootprint>(true);
        using NativeArray<ArchetypeChunk> chunks = context.LiveUnitFootprintQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<UnitGrid> unitGrids = chunk.GetNativeArray(ref unitGridType);
            NativeArray<UnitFootprint> footprints = chunk.GetNativeArray(ref footprintType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (em.HasComponent<Prefab>(entity) ||
                    em.HasComponent<StaticGridBlocker>(entity) ||
                    em.HasComponent<RuntimeBuildingCombatTag>(entity))
                {
                    continue;
                }

                UnitGrid otherGrid = unitGrids[i];
                UnitFootprint otherFootprint = footprints[i];
                if (UnitFootprintUtility.Overlaps(cell, size, otherGrid.Cell, otherFootprint.Size))
                    return true;
            }
        }

        return false;
    }

    private static bool TryReserveSpawnCandidate(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        ref NativeBitArray reserved,
        int2 cell,
        int2 footprintSize)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int idx = row + x;
                if (walkable[idx].Value == 0)
                    return false;
                if (blocked.IsSet(idx) || occupied.IsSet(idx) || reserved.IsSet(idx))
                    return false;
            }
        }

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
                reserved.Set(row + x, true);
        }

        return true;
    }

    private static void ReserveDynamicOccupancy(EntityManager em, Entity gridEntity, in GridConfig grid, int2 centerCell, int2 footprintSize)
    {
        if (!em.HasComponent<DynamicOccupancyComponent>(gridEntity))
            return;

        DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
        if (!occupancy.Occupied.IsCreated)
            return;

        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(centerCell, size);
        int2 max = min + size;
        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
                occupancy.Occupied.Set(row + x, true);
        }
    }

    private static void ReserveBuildingBuffer(ref NativeBitArray reserved, GridConfig grid, Vector2Int originCell, Vector2Int footprintCells, int extraRadius)
    {
        int minX = math.max(0, originCell.x - extraRadius);
        int minY = math.max(0, originCell.y - extraRadius);
        int maxX = math.min(grid.Width, originCell.x + footprintCells.x + extraRadius);
        int maxY = math.min(grid.Height, originCell.y + footprintCells.y + extraRadius);
        for (int y = minY; y < maxY; y++)
        {
            int row = y * grid.Width;
            for (int x = minX; x < maxX; x++)
                reserved.Set(row + x, true);
        }
    }

    private static bool IsOwnedRuntimeBuildingForFaction(RuntimeBuildingEntity building, byte factionId)
    {
        return building != null &&
               !building.IsDestroyed &&
               building.HasOwnerFaction &&
               building.OwnerFactionId == factionId;
    }

    private static bool TryGetFactionRuntimeBuildingCenter(Context context, byte factionId, RuntimeBuildingEntity sourceBuilding, out int2 center)
    {
        center = default;
        int2 sum = default;
        int count = 0;
        foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
        {
            RuntimeBuildingEntity building = entry.Value;
            if (!IsOwnedRuntimeBuildingForFaction(building, factionId))
                continue;

            Vector2Int footprint = building.Definition != null ? building.Definition.FootprintCells : Vector2Int.one;
            sum += new int2(building.OriginCell.x + footprint.x / 2, building.OriginCell.y + footprint.y / 2);
            count++;
        }

        if (count > 0)
        {
            center = new int2(sum.x / count, sum.y / count);
            return true;
        }

        if (sourceBuilding != null)
        {
            Vector2Int footprint = sourceBuilding.Definition != null ? sourceBuilding.Definition.FootprintCells : Vector2Int.one;
            center = new int2(sourceBuilding.OriginCell.x + footprint.x / 2, sourceBuilding.OriginCell.y + footprint.y / 2);
            return true;
        }

        return false;
    }

    private static string NormalizeSpawnableKey(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static bool IsHelicopterSourceKey(FixedString64Bytes sourceKey)
    {
        if (sourceKey.Length == 0)
            return false;

        string normalized = NormalizeSpawnableKey(sourceKey.ToString());
        return normalized.StartsWith("unit_veh_helicopter_", System.StringComparison.Ordinal);
    }

    private static FixedString64Bytes GetProductionSourceKey(
        Context context,
        BuildingDefinition definition,
        int productionIndex)
    {
        if (context.TryGetProductionSourceKey != null &&
            context.TryGetProductionSourceKey(definition, productionIndex, out FixedString64Bytes sourceKey) &&
            sourceKey.Length > 0)
        {
            return sourceKey;
        }

        return default;
    }

    private static void PublishProductionSpawnRequest(
        Context context,
        EntityManager em,
        RuntimeBuildingEntity building,
        int productionIndex,
        int reservedProductionSlotIndex,
        bool hasOverrideWorldPosition,
        bool hasOverrideCell,
        FixedString64Bytes sourceKey,
        Entity prefabEntity,
        Entity producedUnit,
        int2 spawnCell,
        float3 spawnWorldPosition)
    {
        if (context.TryGetRuntimeBoundaryEntity == null ||
            building == null ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity))
        {
            return;
        }

        if (!em.HasBuffer<BuildingProductionSpawnRequest>(boundaryEntity))
            em.AddBuffer<BuildingProductionSpawnRequest>(boundaryEntity);

        DynamicBuffer<BuildingProductionSpawnRequest> requests =
            em.GetBuffer<BuildingProductionSpawnRequest>(boundaryEntity);
        int requestId = requests.Length > 0 ? requests[requests.Length - 1].RequestId + 1 : 1;
        while (requests.Length >= MaxProductionSpawnRequestHistory)
            requests.RemoveAt(0);

        requests.Add(new BuildingProductionSpawnRequest
        {
            RequestId = requestId,
            BuildingRuntimeId = building.Id,
            ProductionIndex = productionIndex,
            ReservedProductionSlotIndex = reservedProductionSlotIndex,
            OwnerFactionId = ResolveProducedUnitFaction(building),
            HasOwnerFaction = building.HasOwnerFaction ? (byte)1 : (byte)0,
            HasOverrideWorldPosition = hasOverrideWorldPosition ? (byte)1 : (byte)0,
            HasOverrideCell = hasOverrideCell ? (byte)1 : (byte)0,
            Status = BuildingProductionSpawnRequest.Succeeded,
            UnitSourceKey = sourceKey,
            PrefabEntity = prefabEntity,
            ProducedUnit = producedUnit,
            SpawnCell = spawnCell,
            SpawnWorldPosition = spawnWorldPosition
        });
    }

    private static bool PublishProducedUnitReadModel(
        Context context,
        EntityManager em,
        RuntimeBuildingEntity building,
        RuntimeBuildingEntity productionSlotBuilding,
        int productionIndex,
        int productionSlotIndex,
        FixedString64Bytes sourceKey,
        Entity producedUnit)
    {
        if (context.TryGetRuntimeBoundaryEntity == null ||
            building == null ||
            producedUnit == Entity.Null ||
            !context.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            boundaryEntity == Entity.Null ||
            !em.Exists(boundaryEntity))
        {
            return false;
        }

        if (!em.HasBuffer<BuildingProducedUnitReadModel>(boundaryEntity))
            em.AddBuffer<BuildingProducedUnitReadModel>(boundaryEntity);

        DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
            em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity);
        producedUnits.Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = building.Id,
            ProductionSlotBuildingRuntimeId = productionSlotIndex >= 0 && productionSlotBuilding != null
                ? productionSlotBuilding.Id
                : 0,
            ProductionIndex = productionIndex,
            ProductionSlotIndex = productionSlotIndex,
            OwnerFactionId = ResolveProducedUnitFaction(building),
            HasOwnerFaction = building.HasOwnerFaction ? (byte)1 : (byte)0,
            Unit = producedUnit,
            UnitSourceKey = sourceKey
        });
        return true;
    }

    private static void SetOrAddComponent<T>(EntityManager em, Entity entity, T value)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            em.SetComponentData(entity, value);
        else
            em.AddComponentData(entity, value);
    }
}
