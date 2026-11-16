using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Game.Components;

namespace Game.Runtime
{
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
        private bool _ownsTrackedUnitsQuery;
        private NativeParallelHashMap<Entity, OccupancyRecord> _occupancyRecords;
        private NativeArray<int> _occupancyCounts;
        private int _cachedGridSize;
        private int _lastTrackedUnitCount;
        private int _lastTrackedComponentOrderVersion;
        private Entity _cachedGridEntity;
        private EntityTypeHandle _trackedEntityType;

        private struct OccupancyStorage : Navigation.IGridOccupancyStorage
        {
            public NativeArray<int> Counts;
            public NativeBitArray Occupied;
            public int GetCount(int index) => Counts[index];
            public void SetCount(int index, int count) => Counts[index] = count;
            public void SetOccupied(int index, bool occupied) => Occupied.Set(index, occupied);
        }
        private static Navigation.OccupancyBounds Bounds(in GridConfig grid, int2 cell, int2 size) =>
            Navigation.GridOccupancyKernel.Bounds(grid.Width, grid.Height, cell.x, cell.y, size.x, size.y);
        private static void AddOccupancy(in GridConfig grid, ref DynamicOccupancyComponent occ, NativeArray<int> counts, int2 cell, int2 size)
        {
            var storage = new OccupancyStorage { Counts = counts, Occupied = occ.Occupied };
            Navigation.GridOccupancyKernel.Add(grid.Width, ref storage, Bounds(grid, cell, size));
        }
        private static void RemoveOccupancy(in GridConfig grid, ref DynamicOccupancyComponent occ, NativeArray<int> counts, int2 cell, int2 size)
        {
            var storage = new OccupancyStorage { Counts = counts, Occupied = occ.Occupied };
            Navigation.GridOccupancyKernel.Remove(grid.Width, ref storage, Bounds(grid, cell, size));
        }
        private static void UpdateOccupancyDelta(in GridConfig grid, ref DynamicOccupancyComponent occ, NativeArray<int> counts,
            int2 previousCell, int2 previousSize, int2 currentCell, int2 currentSize)
        {
            var storage = new OccupancyStorage { Counts = counts, Occupied = occ.Occupied };
            Navigation.GridOccupancyKernel.Update(grid.Width, ref storage,
                Bounds(grid, previousCell, previousSize), Bounds(grid, currentCell, currentSize));
        }

        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            state.RequireForUpdate(_gridQuery);
            state.RequireForUpdate<DynamicOccupancyComponent>();
            state.RequireForUpdate<UnitGrid>();

            // SystemState caches identical queries. Keep full membership independently
            // owned so a movement change filter cannot hide stationary live units.
            _trackedUnitsQuery = state.EntityManager.CreateEntityQuery(new EntityQueryDesc
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
                    ComponentType.ReadOnly<UnitDeathAnimationComponent>(),
                }
            });
            _ownsTrackedUnitsQuery = true;

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
                    ComponentType.ReadOnly<UnitDeathAnimationComponent>(),
                }
            });
            _changedGridUnitsQuery.SetChangedVersionFilter(ComponentType.ReadOnly<UnitGrid>());

            _occupancyRecords = new NativeParallelHashMap<Entity, OccupancyRecord>(128, Allocator.Persistent);
            _occupancyCounts = default;
            _cachedGridSize = 0;
            _lastTrackedUnitCount = -1;
            _lastTrackedComponentOrderVersion = -1;
            _cachedGridEntity = Entity.Null;
            _trackedEntityType = state.GetEntityTypeHandle();
#if UNITY_EDITOR
            ObserveInitialWorldCreate(ref state);
#endif
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_ownsTrackedUnitsQuery)
            {
                _trackedUnitsQuery.Dispose();
                _ownsTrackedUnitsQuery = false;
            }
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

            occ.Occupied.Clear();

            _cachedGridSize = gridSize;
            _cachedGridEntity = gridEntity;
            _occupancyRecords.Clear();

            var unitGridLookup = SystemAPI.GetComponentLookup<UnitGrid>(true);
            var footprintLookup = SystemAPI.GetComponentLookup<UnitFootprint>(true);
            using NativeArray<Entity> entities = _trackedUnitsQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                OccupancyRecord record = new()
                {
                    Cell = unitGridLookup[entity].Cell,
                    Size = footprintLookup[entity].Size
                };

                AddOccupancy(grid, ref occ, _occupancyCounts, record.Cell, record.Size);
                _occupancyRecords[entity] = record;
            }

            _lastTrackedUnitCount = trackedUnitCount;
            _lastTrackedComponentOrderVersion = state.EntityManager.GetComponentOrderVersion<UnitGrid>();
        }

        private void SyncTrackedEntityMembership(
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
#if UNITY_EDITOR
            ObserveInitialWorldUpdateEntry(ref state);
#endif
            int trackedUnitCount = _trackedUnitsQuery.CalculateEntityCount();
            int componentOrderVersion = state.EntityManager.GetComponentOrderVersion<UnitGrid>();
            bool membershipChanged = trackedUnitCount != _lastTrackedUnitCount ||
                                     componentOrderVersion != _lastTrackedComponentOrderVersion;
            Entity gridEntity = _gridQuery.GetSingletonEntity();
            var grid = SystemAPI.GetComponent<GridConfig>(gridEntity);

            var occRw = SystemAPI.GetComponentRW<DynamicOccupancyComponent>(gridEntity);
            ref var occ = ref occRw.ValueRW;

            int gridSize = grid.Width * grid.Height;
            if (occ.GridSize != gridSize || !occ.Occupied.IsCreated)
                return;

            bool gridChanged = !_changedGridUnitsQuery.IsEmpty;
            if (!membershipChanged &&
                _cachedGridEntity == gridEntity &&
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
            bool gridEntityChanged = _cachedGridEntity != gridEntity;
            bool missingRecords = !_occupancyRecords.IsCreated;
            if (needsInitialRebuild ||
                missingCounts ||
                gridSizeChanged ||
                gridEntityChanged ||
                missingRecords)
            {
                RebuildOccupancy(ref state, gridEntity, grid, ref occ, trackedUnitCount);
                return;
            }

            if (membershipChanged)
                SyncTrackedEntityMembership(ref state, grid, ref occ, trackedUnitCount);

            var unitGridLookup = SystemAPI.GetComponentLookup<UnitGrid>(true);
            var footprintLookup = SystemAPI.GetComponentLookup<UnitFootprint>(true);

            foreach (var (_, _, entity) in SystemAPI
                         .Query<RefRO<UnitGrid>, RefRO<UnitFootprint>>()
                         .WithNone<StaticGridBlocker, RuntimeBuildingCombatTag, UnitDeathAnimationComponent>()
                         .WithChangeFilter<UnitGrid>()
                         .WithEntityAccess())
            {
                ApplyChangedEntity(entity, grid, ref occ, unitGridLookup, footprintLookup);
            }

            _lastTrackedUnitCount = trackedUnitCount;
            _lastTrackedComponentOrderVersion = componentOrderVersion;
        }
    }
}
