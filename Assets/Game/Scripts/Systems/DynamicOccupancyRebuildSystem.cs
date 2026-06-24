using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

[BurstCompile]
[UpdateBefore(typeof(UnitEngagedMovementSystem))]
[UpdateBefore(typeof(UnitGridMovementSystem))]
public partial struct DynamicOccupancyRebuildSystem : ISystem
{
    private struct OccupancyRecord
    {
        public int2 Cell;
        public int2 Size;
    }

    private EntityQuery _gridQuery;
    private EntityQuery _trackedUnitsQuery;
    private EntityQuery _changedGridUnitsQuery;
    private NativeParallelHashMap<Entity, OccupancyRecord> _occupancyRecords;
    private NativeArray<int> _occupancyCounts;
    private int _cachedGridSize;
    private int _lastTrackedUnitCount;
    private EntityTypeHandle _trackedEntityType;

    private static void GetClampedFootprintBounds(in GridConfig grid, int2 centerCell, int2 size, out int2 clampedMin, out int2 clampedMax)
    {
        int2 clampedSize = UnitFootprintUtility.ClampSize(size);
        int2 min = UnitFootprintUtility.GetMinCell(centerCell, clampedSize);
        int2 max = min + clampedSize;
        clampedMin = new int2(math.clamp(min.x, 0, grid.Width), math.clamp(min.y, 0, grid.Height));
        clampedMax = new int2(math.clamp(max.x, 0, grid.Width), math.clamp(max.y, 0, grid.Height));
    }

    private static void AddOccupancy(in GridConfig grid, ref DynamicOccupancyComponent occ, NativeArray<int> counts, int2 centerCell, int2 size)
    {
        GetClampedFootprintBounds(grid, centerCell, size, out int2 clampedMin, out int2 clampedMax);
        AddOccupancyRect(grid, ref occ, counts, clampedMin, clampedMax);
    }

    private static void AddOccupancyRect(in GridConfig grid, ref DynamicOccupancyComponent occ, NativeArray<int> counts, int2 clampedMin, int2 clampedMax)
    {
        for (int y = clampedMin.y; y < clampedMax.y; y++)
        {
            int row = y * grid.Width;
            for (int x = clampedMin.x; x < clampedMax.x; x++)
            {
                int index = row + x;
                int nextCount = counts[index] + 1;
                counts[index] = nextCount;
                if (nextCount == 1)
                    occ.Occupied.Set(index, true);
            }
        }
    }

    private static void RemoveOccupancy(in GridConfig grid, ref DynamicOccupancyComponent occ, NativeArray<int> counts, int2 centerCell, int2 size)
    {
        GetClampedFootprintBounds(grid, centerCell, size, out int2 clampedMin, out int2 clampedMax);
        RemoveOccupancyRect(grid, ref occ, counts, clampedMin, clampedMax);
    }

    private static void RemoveOccupancyRect(in GridConfig grid, ref DynamicOccupancyComponent occ, NativeArray<int> counts, int2 clampedMin, int2 clampedMax)
    {
        for (int y = clampedMin.y; y < clampedMax.y; y++)
        {
            int row = y * grid.Width;
            for (int x = clampedMin.x; x < clampedMax.x; x++)
            {
                int index = row + x;
                int nextCount = math.max(0, counts[index] - 1);
                counts[index] = nextCount;
                if (nextCount == 0)
                    occ.Occupied.Set(index, false);
            }
        }
    }

    private static void UpdateOccupancyDelta(
        in GridConfig grid,
        ref DynamicOccupancyComponent occ,
        NativeArray<int> counts,
        int2 previousCell,
        int2 previousSize,
        int2 currentCell,
        int2 currentSize)
    {
        GetClampedFootprintBounds(grid, previousCell, previousSize, out int2 previousMin, out int2 previousMax);
        GetClampedFootprintBounds(grid, currentCell, currentSize, out int2 currentMin, out int2 currentMax);

        if (previousMin.Equals(currentMin) && previousMax.Equals(currentMax))
            return;

        int2 overlapMin = new int2(math.max(previousMin.x, currentMin.x), math.max(previousMin.y, currentMin.y));
        int2 overlapMax = new int2(math.min(previousMax.x, currentMax.x), math.min(previousMax.y, currentMax.y));
        bool hasOverlap = overlapMin.x < overlapMax.x && overlapMin.y < overlapMax.y;
        if (!hasOverlap)
        {
            RemoveOccupancyRect(grid, ref occ, counts, previousMin, previousMax);
            AddOccupancyRect(grid, ref occ, counts, currentMin, currentMax);
            return;
        }

        if (previousMin.x < overlapMin.x)
            RemoveOccupancyRect(grid, ref occ, counts, new int2(previousMin.x, previousMin.y), new int2(overlapMin.x, previousMax.y));
        if (overlapMax.x < previousMax.x)
            RemoveOccupancyRect(grid, ref occ, counts, new int2(overlapMax.x, previousMin.y), new int2(previousMax.x, previousMax.y));
        if (previousMin.y < overlapMin.y)
            RemoveOccupancyRect(grid, ref occ, counts, new int2(overlapMin.x, previousMin.y), new int2(overlapMax.x, overlapMin.y));
        if (overlapMax.y < previousMax.y)
            RemoveOccupancyRect(grid, ref occ, counts, new int2(overlapMin.x, overlapMax.y), new int2(overlapMax.x, previousMax.y));

        if (currentMin.x < overlapMin.x)
            AddOccupancyRect(grid, ref occ, counts, new int2(currentMin.x, currentMin.y), new int2(overlapMin.x, currentMax.y));
        if (overlapMax.x < currentMax.x)
            AddOccupancyRect(grid, ref occ, counts, new int2(overlapMax.x, currentMin.y), new int2(currentMax.x, currentMax.y));
        if (currentMin.y < overlapMin.y)
            AddOccupancyRect(grid, ref occ, counts, new int2(overlapMin.x, currentMin.y), new int2(overlapMax.x, overlapMin.y));
        if (overlapMax.y < currentMax.y)
            AddOccupancyRect(grid, ref occ, counts, new int2(overlapMin.x, overlapMax.y), new int2(overlapMax.x, currentMax.y));
    }

    public void OnCreate(ref SystemState state)
    {
        _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
        state.RequireForUpdate(_gridQuery);
        state.RequireForUpdate<DynamicOccupancyComponent>();
        state.RequireForUpdate<UnitGrid>();

        _trackedUnitsQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
            }
        });

        _changedGridUnitsQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
            }
        });
        _changedGridUnitsQuery.SetChangedVersionFilter(ComponentType.ReadOnly<UnitGrid>());

        _occupancyRecords = new NativeParallelHashMap<Entity, OccupancyRecord>(128, Allocator.Persistent);
        _occupancyCounts = default;
        _cachedGridSize = 0;
        _lastTrackedUnitCount = -1;
        _trackedEntityType = state.GetEntityTypeHandle();
    }

    public void OnDestroy(ref SystemState state)
    {
        if (_occupancyRecords.IsCreated)
            _occupancyRecords.Dispose();
        if (_occupancyCounts.IsCreated)
            _occupancyCounts.Dispose();
    }

    private void EnsureStorage(int gridSize, int trackedUnitCount)
    {
        int requiredCapacity = math.max(128, trackedUnitCount * 2);
        if (!_occupancyRecords.IsCreated)
        {
            _occupancyRecords = new NativeParallelHashMap<Entity, OccupancyRecord>(requiredCapacity, Allocator.Persistent);
        }
        else
        {
            _occupancyRecords.Clear();
            if (_occupancyRecords.Capacity < requiredCapacity)
                _occupancyRecords.Capacity = requiredCapacity;
        }
    }

    private void RebuildOccupancy(ref SystemState state, Entity gridEntity, in GridConfig grid, ref DynamicOccupancyComponent occ, int trackedUnitCount)
    {
        EnsureStorage(grid.Width * grid.Height, trackedUnitCount);

        int gridSize = grid.Width * grid.Height;
        if (_occupancyCounts.IsCreated)
            _occupancyCounts.Dispose();
        _occupancyCounts = new NativeArray<int>(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        if (occ.Occupied.IsCreated)
            occ.Occupied.Dispose();
        occ.GridSize = gridSize;
        occ.Occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        _cachedGridSize = gridSize;
        _occupancyRecords.Clear();

        foreach (var (unitGrid, footprint, entity) in SystemAPI.Query<RefRO<UnitGrid>, RefRO<UnitFootprint>>().WithNone<StaticGridBlocker, RuntimeBuildingCombatTag>().WithEntityAccess())
        {
            OccupancyRecord record = new()
            {
                Cell = unitGrid.ValueRO.Cell,
                Size = footprint.ValueRO.Size
            };

            AddOccupancy(grid, ref occ, _occupancyCounts, record.Cell, record.Size);
            _occupancyRecords[entity] = record;
        }

        _lastTrackedUnitCount = trackedUnitCount;
    }

    private void SyncTrackedEntitiesForCountChange(
        ref SystemState state,
        in GridConfig grid,
        ref DynamicOccupancyComponent occ,
        int trackedUnitCount)
    {
        _trackedEntityType.Update(ref state);
        using NativeArray<ArchetypeChunk> chunks = _trackedUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
        using var currentEntities = new NativeList<Entity>(_trackedUnitsQuery.CalculateEntityCount(), Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(_trackedEntityType);
            currentEntities.AddRange(entities);
        }

        using var currentEntitySet = new NativeHashSet<Entity>(math.max(1, currentEntities.Length), Allocator.Temp);
        for (int i = 0; i < currentEntities.Length; i++)
            currentEntitySet.Add(currentEntities[i]);

        using var previousEntities = _occupancyRecords.GetKeyArray(Allocator.Temp);
        for (int i = 0; i < previousEntities.Length; i++)
        {
            Entity entity = previousEntities[i];
            if (currentEntitySet.Contains(entity))
                continue;

            if (_occupancyRecords.TryGetValue(entity, out OccupancyRecord previous))
            {
                RemoveOccupancy(grid, ref occ, _occupancyCounts, previous.Cell, previous.Size);
                _occupancyRecords.Remove(entity);
            }
        }

        var unitGridLookup = SystemAPI.GetComponentLookup<UnitGrid>(true);
        var footprintLookup = SystemAPI.GetComponentLookup<UnitFootprint>(true);
        for (int i = 0; i < currentEntities.Length; i++)
        {
            Entity entity = currentEntities[i];
            if (_occupancyRecords.ContainsKey(entity))
                continue;
            if (!unitGridLookup.HasComponent(entity) || !footprintLookup.HasComponent(entity))
                continue;

            OccupancyRecord current = new()
            {
                Cell = unitGridLookup[entity].Cell,
                Size = footprintLookup[entity].Size
            };

            AddOccupancy(grid, ref occ, _occupancyCounts, current.Cell, current.Size);
            _occupancyRecords[entity] = current;
        }

        _lastTrackedUnitCount = trackedUnitCount;
    }

    private void ApplyChangedEntity(
        Entity entity,
        in GridConfig grid,
        ref DynamicOccupancyComponent occ,
        ComponentLookup<UnitGrid> unitGridLookup,
        ComponentLookup<UnitFootprint> footprintLookup)
    {
        bool hadPrevious = _occupancyRecords.TryGetValue(entity, out OccupancyRecord previous);
        if (hadPrevious)
            _occupancyRecords.Remove(entity);

        if (!unitGridLookup.HasComponent(entity) || !footprintLookup.HasComponent(entity))
        {
            if (hadPrevious)
                RemoveOccupancy(grid, ref occ, _occupancyCounts, previous.Cell, previous.Size);
            return;
        }

        OccupancyRecord current = new()
        {
            Cell = unitGridLookup[entity].Cell,
            Size = footprintLookup[entity].Size
        };

        if (!hadPrevious)
        {
            AddOccupancy(grid, ref occ, _occupancyCounts, current.Cell, current.Size);
        }
        else if (!previous.Cell.Equals(current.Cell) || !previous.Size.Equals(current.Size))
        {
            UpdateOccupancyDelta(grid, ref occ, _occupancyCounts, previous.Cell, previous.Size, current.Cell, current.Size);
        }

        _occupancyRecords[entity] = current;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int trackedUnitCount = _trackedUnitsQuery.CalculateEntityCount();
        Entity gridEntity = _gridQuery.GetSingletonEntity();
        var grid = SystemAPI.GetComponent<GridConfig>(gridEntity);

        var occRw = SystemAPI.GetComponentRW<DynamicOccupancyComponent>(gridEntity);
        ref var occ = ref occRw.ValueRW;

        int gridSize = grid.Width * grid.Height;
        if (occ.GridSize != gridSize || !occ.Occupied.IsCreated)
            return;

        bool gridChanged = !_changedGridUnitsQuery.IsEmptyIgnoreFilter;
        if (trackedUnitCount == _lastTrackedUnitCount &&
            _cachedGridSize == gridSize &&
            _occupancyCounts.IsCreated &&
            _occupancyRecords.IsCreated &&
            !gridChanged)
        {
            return;
        }

        bool needsInitialRebuild = _lastTrackedUnitCount < 0;
        bool missingCounts = !_occupancyCounts.IsCreated;
        bool gridSizeChanged = _cachedGridSize != gridSize;
        bool missingRecords = !_occupancyRecords.IsCreated;
        if (needsInitialRebuild ||
            missingCounts ||
            gridSizeChanged ||
            missingRecords)
        {
            RebuildOccupancy(ref state, gridEntity, grid, ref occ, trackedUnitCount);
            return;
        }

        if (trackedUnitCount != _lastTrackedUnitCount)
            SyncTrackedEntitiesForCountChange(ref state, grid, ref occ, trackedUnitCount);

        var unitGridLookup = SystemAPI.GetComponentLookup<UnitGrid>(true);
        var footprintLookup = SystemAPI.GetComponentLookup<UnitFootprint>(true);

        foreach (var (_, _, entity) in SystemAPI
                     .Query<RefRO<UnitGrid>, RefRO<UnitFootprint>>()
                     .WithNone<StaticGridBlocker, RuntimeBuildingCombatTag>()
                     .WithChangeFilter<UnitGrid>()
                     .WithEntityAccess())
        {
            ApplyChangedEntity(entity, grid, ref occ, unitGridLookup, footprintLookup);
        }

        _lastTrackedUnitCount = trackedUnitCount;
    }
}
