using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed partial class BuildingSpawnSystem : SystemBase
{
    public delegate GameObject GetProductionPrefabDelegate(BuildingDefinition definition, int index);
    public delegate bool RuntimeBuildingMatchesIdDelegate(RuntimeBuildingEntity building, string normalizedBuildingId);

    private sealed class RecentSpawnReservation
    {
        public int2 Cell;
        public int2 Size;
        public float ExpiresAt;
    }

    private readonly List<RecentSpawnReservation> _recentSpawnReservations = new();
    private readonly MapSurfaceSpawnGrounding _spawnGroundingSystem = new();
    private uint _buildingSpawnRandomState = 0x12345678u;

    internal uint BuildingSpawnRandomState
    {
        get => _buildingSpawnRandomState;
        set => _buildingSpawnRandomState = value;
    }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly EntityQuery LiveUnitFootprintQuery;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly BuildingSpawnPrefabSystem SpawnPrefabSystem;
        public readonly BuildingSpawnPrefabSystem.Context SpawnPrefabContext;
        public readonly BuildingProductionSlotSystem ProductionSlotSystem;
        public readonly GetProductionPrefabDelegate GetProductionPrefab;
        public readonly RuntimeBuildingMatchesIdDelegate RuntimeBuildingMatchesId;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            EntityQuery liveUnitFootprintQuery,
            BuildingProductionSystem productionSystem,
            BuildingSpawnPrefabSystem spawnPrefabSystem,
            BuildingSpawnPrefabSystem.Context spawnPrefabContext,
            BuildingProductionSlotSystem productionSlotSystem,
            GetProductionPrefabDelegate getProductionPrefab,
            RuntimeBuildingMatchesIdDelegate runtimeBuildingMatchesId)
        {
            RuntimeBuildings = runtimeBuildings;
            LiveUnitFootprintQuery = liveUnitFootprintQuery;
            ProductionSystem = productionSystem;
            SpawnPrefabSystem = spawnPrefabSystem;
            SpawnPrefabContext = spawnPrefabContext;
            ProductionSlotSystem = productionSlotSystem;
            GetProductionPrefab = getProductionPrefab;
            RuntimeBuildingMatchesId = runtimeBuildingMatchesId;
        }
    }

    public void CleanupRecentSpawnReservations(float now)
    {
        if (_recentSpawnReservations.Count == 0)
            return;

        for (int i = _recentSpawnReservations.Count - 1; i >= 0; i--)
        {
            if (_recentSpawnReservations[i].ExpiresAt > now)
                continue;

            _recentSpawnReservations.RemoveAt(i);
        }
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
            ReserveRecentSpawnBuffers(ref reserved, grid);
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

    public bool TryResolveAvailableFactionHelipadSpawn(
        Context context,
        byte factionId,
        RuntimeBuildingEntity sourceBuilding,
        EntityManager em,
        Entity gridEntity,
        GridConfig grid,
        DynamicBlockerComponent blockerData,
        int2 unitFootprint,
        out int2 cell,
        out float3 worldPosition)
    {
        uint randomState = _buildingSpawnRandomState;
        bool resolved = TryResolveAvailableFactionHelipadSpawn(
            context,
            factionId,
            sourceBuilding,
            em,
            gridEntity,
            grid,
            blockerData,
            unitFootprint,
            ref randomState,
            out cell,
            out worldPosition);
        _buildingSpawnRandomState = randomState;
        return resolved;
    }

    public bool TryResolveAvailableFactionHelipadSpawn(
        Context context,
        byte factionId,
        EntityManager em,
        Entity gridEntity,
        GridConfig grid,
        DynamicBlockerComponent blockerData,
        int2 unitFootprint,
        out int2 cell,
        out float3 worldPosition)
    {
        uint randomState = _buildingSpawnRandomState;
        bool resolved = TryResolveAvailableFactionHelipadSpawn(
            context,
            factionId,
            null,
            em,
            gridEntity,
            grid,
            blockerData,
            unitFootprint,
            ref randomState,
            out cell,
            out worldPosition);
        _buildingSpawnRandomState = randomState;
        return resolved;
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
        cell = default;
        worldPosition = default;
        if (context.RuntimeBuildings == null || string.IsNullOrWhiteSpace(buildingId))
            return false;

        int remainingSlotIndex = math.max(0, flattenedSlotIndex);
        string normalizedBuildingId = BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId);
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
        if (building == null || building.Definition == null || context.GetProductionPrefab == null)
            return false;

        GameObject spawnUnitPrefab = context.GetProductionPrefab(building.Definition, productionIndex);
        FixedString64Bytes spawnUnitSourceKey = GetUnitPrefabSourceKey(spawnUnitPrefab);
        if (!context.SpawnPrefabSystem.TryGetSpawnUnitPrefabEntity(context.SpawnPrefabContext, em, spawnUnitSourceKey, out Entity prefabEntity))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[BuildingSpawn] Could not resolve ECS prefab entity for spawn prefab '{(spawnUnitPrefab != null ? spawnUnitPrefab.name : "<null>")}' from building '{building.Definition.DisplayName}'.");
#endif
            return false;
        }

        if (!TryResolveSpawnPlacement(
                context,
                building,
                spawnUnitPrefab,
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
            _spawnGroundingSystem.TryGroundCellCenter(em, grid, cell, ref pos, out _);
        em.SetComponentData(instance, new UnitGrid { Cell = cell });
        em.SetComponentData(instance, LocalTransform.FromPosition(pos));
        if (spawnUnitSourceKey.Length > 0)
            SetOrAddComponent(em, instance, new UnitSourcePrefabKey { Value = spawnUnitSourceKey });
        building.ProducedUnits ??= new List<Entity>();
        building.ProducedUnitPrefabs ??= new Dictionary<Entity, GameObject>();
        building.ProducedUnitSourceKeys ??= new Dictionary<Entity, FixedString64Bytes>();
        building.ProducedUnits.Add(instance);
        building.ProducedUnitPrefabs[instance] = spawnUnitPrefab;
        building.ProducedUnitSourceKeys[instance] = spawnUnitSourceKey;
        if (!isAirUnit)
        {
            ReserveDynamicOccupancy(em, gridEntity, grid, cell, unitFootprint);
            AddRecentSpawnReservation(cell, unitFootprint);
        }

        if (productionSlotIndex >= 0 &&
            productionSlotBuilding?.ProducedUnitSlots != null &&
            productionSlotIndex < productionSlotBuilding.ProducedUnitSlots.Length)
        {
            productionSlotBuilding.ProducedUnitSlots[productionSlotIndex] = instance;
        }

        InitializeSpawnedUnit(em, instance, pos, cell, building, isAirUnit, ref randomState);
        return true;
    }

    private bool TryResolveSpawnPlacement(
        Context context,
        RuntimeBuildingEntity building,
        GameObject spawnUnitPrefab,
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
        bool useHelicopterSpawnResolver =
            !overrideWorldPosition.HasValue &&
            !overrideCell.HasValue &&
            isAirUnit &&
            context.ProductionSystem.IsHelicopterUnitPrefab(spawnUnitPrefab) &&
            building.HasOwnerFaction;
        bool useOverrideHelicopterSpawn =
            overrideWorldPosition.HasValue &&
            overrideCell.HasValue &&
            isAirUnit &&
            context.ProductionSystem.IsHelicopterUnitPrefab(spawnUnitPrefab);
        productionSlotIndex = -1;
        Vector3 productionSpawnLocalPosition = Vector3.zero;
        productionSlotBuilding = building;
        bool hasProductionSpawnSlots = building.ProductionSpawnLocalPositions != null &&
                                       building.ProducedUnitSlots != null &&
                                       building.ProductionSpawnLocalPositions.Length > 0;
        if (hasProductionSpawnSlots && !useHelicopterSpawnResolver && !useOverrideHelicopterSpawn)
        {
            if (reservedProductionSlotIndex >= 0 &&
                reservedProductionSlotIndex < building.ProductionSpawnLocalPositions.Length &&
                reservedProductionSlotIndex < building.ProducedUnitSlots.Length)
            {
                productionSlotIndex = reservedProductionSlotIndex;
                productionSpawnLocalPosition = building.ProductionSpawnLocalPositions[reservedProductionSlotIndex];
            }
            else if (context.ProductionSlotSystem == null ||
                     !context.ProductionSlotSystem.TryGetAvailableProductionSpawnSlot(building, em, out productionSlotIndex, out productionSpawnLocalPosition))
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
            ReserveRecentSpawnBuffers(ref reserved, grid);
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
                pos = building.Instance != null
                    ? (float3)building.Instance.transform.TransformPoint(productionSpawnLocalPosition)
                    : (float3)productionSpawnLocalPosition;
                cell = GridUtils.WorldToCell(grid, pos);
                if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                    return false;

                if (!isAirUnit)
                {
                    bool slotCellAvailable =
                        TryReserveSpawnCandidate(grid, walkable, blockerData.Blocked, occupied, ref reserved, cell, unitFootprint) &&
                        !OverlapsRecentSpawnReservation(cell, unitFootprint) &&
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
                if (context.ProductionSlotSystem.IsProductionSlotOccupied(building, em, i))
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

    private void ReserveRecentSpawnBuffers(ref NativeBitArray reserved, GridConfig grid)
    {
        if (_recentSpawnReservations.Count == 0)
            return;

        float now = UnityEngine.Time.time;
        for (int i = 0; i < _recentSpawnReservations.Count; i++)
        {
            RecentSpawnReservation reservation = _recentSpawnReservations[i];
            if (reservation == null || reservation.ExpiresAt <= now)
                continue;

            int2 size = UnitFootprintUtility.ClampSize(reservation.Size);
            int2 min = UnitFootprintUtility.GetMinCell(reservation.Cell, size);
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
    }

    private void AddRecentSpawnReservation(int2 cell, int2 size)
    {
        _recentSpawnReservations.Add(new RecentSpawnReservation
        {
            Cell = cell,
            Size = UnitFootprintUtility.ClampSize(size),
            ExpiresAt = UnityEngine.Time.time + 0.5f
        });
    }

    private bool OverlapsRecentSpawnReservation(int2 cell, int2 size)
    {
        if (_recentSpawnReservations.Count == 0)
            return false;

        float now = UnityEngine.Time.time;
        int2 clampedSize = UnitFootprintUtility.ClampSize(size);
        for (int i = 0; i < _recentSpawnReservations.Count; i++)
        {
            RecentSpawnReservation reservation = _recentSpawnReservations[i];
            if (reservation == null || reservation.ExpiresAt <= now)
                continue;

            if (UnitFootprintUtility.Overlaps(cell, clampedSize, reservation.Cell, reservation.Size))
                return true;
        }

        return false;
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
                if (context.ProductionSlotSystem.IsProductionSlotOccupied(building, em, i))
                    continue;

                Vector3 candidateWorld = building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[i]);
                int2 candidateCell = GridUtils.WorldToCell(grid, candidateWorld);
                if (!GridUtils.InBounds(candidateCell, grid.Width, grid.Height))
                    continue;
                if (OverlapsRecentSpawnReservation(candidateCell, unitFootprint))
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
            if (OverlapsRecentSpawnReservation(candidate, footprintSize))
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
                    if (OverlapsRecentSpawnReservation(candidate, footprintSize))
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
                    if (OverlapsRecentSpawnReservation(candidate, unitFootprint))
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

    private static FixedString64Bytes GetUnitPrefabSourceKey(GameObject unitPrefab)
    {
        string sourceKey = BuildingDefinitionSystem.GetSpawnableLookupKey(unitPrefab);
        return string.IsNullOrWhiteSpace(sourceKey) ? default : new FixedString64Bytes(sourceKey);
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
