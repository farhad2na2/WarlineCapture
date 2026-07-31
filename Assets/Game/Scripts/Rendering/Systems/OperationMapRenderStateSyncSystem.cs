using System;
using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Rendering
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RuntimeCameraReferenceSystem))]
    [UpdateBefore(typeof(OperationMapRenderVirtualizationInitializationSystem))]
    public partial struct OperationMapRenderStateSyncSystem : ISystem
    {
        private EntityQuery _activeMapQuery;
        private EntityQuery _databaseQuery;
        private EntityQuery _buildingQuery;
        private NativeBitArray _affectedStateOwners;
        private NativeBitArray _dirtyPlacements;
        private NativeBitArray _dirtyCells;
        private Entity _initializedDatabaseEntity;
        private int _initializedMapGeneration;
        private ComponentLookup<OperationMapRenderStateSyncStateComponent>
            _syncStateLookup;
        private ComponentLookup<OperationMapRenderVirtualizationStateComponent>
            _virtualizationStateLookup;

        internal bool IsPersistentStateCreated => _dirtyPlacements.IsCreated;

        public void OnCreate(ref SystemState state)
        {
            _activeMapQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ActiveOperationMapComponent>());
            _databaseQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>(),
                ComponentType.ReadWrite<OperationMapRenderStateSyncStateComponent>(),
                ComponentType.ReadOnly<
                    OperationMapRenderStateChangeSequenceComponent>(),
                ComponentType.ReadWrite<OperationMapRenderStateChangeComponent>(),
                ComponentType.ReadWrite<
                    OperationMapRenderCanonicalStateComponent>(),
                ComponentType.ReadWrite<
                    OperationMapRenderVirtualizationStateComponent>());
            _buildingQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<
                        OperationMapVirtualizedBuildingPresentationComponent>(),
                    ComponentType.ReadOnly<OperationMapBuildingDestroyedComponent>()
                },
                Options = EntityQueryOptions.IgnoreComponentEnabledState
            });
            _syncStateLookup = state.GetComponentLookup<
                OperationMapRenderStateSyncStateComponent>(false);
            _virtualizationStateLookup = state.GetComponentLookup<
                OperationMapRenderVirtualizationStateComponent>(false);
            _initializedDatabaseEntity = Entity.Null;
        }

        public void OnDestroy(ref SystemState state)
        {
            CompleteAndDispose(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            int databaseCount = _databaseQuery.CalculateEntityCount();
            if (databaseCount == 0)
            {
                CompleteAndDispose(ref state);
                return;
            }
            if (databaseCount != 1)
            {
                throw new InvalidOperationException(
                    $"Render state synchronization requires exactly one owner, found {databaseCount}.");
            }
            int activeMapCount = _activeMapQuery.CalculateEntityCount();
            if (activeMapCount == 0)
            {
                CompleteAndDispose(ref state);
                return;
            }
            if (activeMapCount != 1)
            {
                throw new InvalidOperationException(
                    "Render state synchronization requires exactly one active operation map.");
            }

            Entity owner = _databaseQuery.GetSingletonEntity();
            OperationMapRenderDatabaseComponent database =
                _databaseQuery.GetSingleton<OperationMapRenderDatabaseComponent>();
            ActiveOperationMapComponent activeMap =
                _activeMapQuery.GetSingleton<ActiveOperationMapComponent>();
            ValidateDatabase(database, activeMap);
            ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
            OperationMapRenderStateSyncStateComponent syncState =
                state.EntityManager.GetComponentData<
                    OperationMapRenderStateSyncStateComponent>(owner);
            bool initialize = !_dirtyPlacements.IsCreated ||
                              _initializedDatabaseEntity != owner ||
                              _initializedMapGeneration != activeMap.Generation ||
                              syncState.Initialized == 0;
            int stateOwnerCount = initialize
                ? CalculateRequiredStateOwnerCount(ref blob)
                : syncState.StateOwnerCount;
            if (stateOwnerCount < 0)
            {
                throw new InvalidOperationException(
                    "Render canonical-state owner count is invalid.");
            }

            DynamicBuffer<OperationMapRenderCanonicalStateComponent>
                canonicalStates = state.EntityManager.GetBuffer<
                    OperationMapRenderCanonicalStateComponent>(owner);
            JobHandle dependency = state.Dependency;
            if (initialize)
            {
                state.Dependency.Complete();
                CompleteAndDispose(ref state);
                dependency = default;
                ValidateStateOwnerMapping(
                    state.EntityManager,
                    stateOwnerCount);
                canonicalStates.ResizeUninitialized(stateOwnerCount);
                state.EntityManager.GetBuffer<
                    OperationMapRenderStateChangeComponent>(owner).Clear();
                state.EntityManager.SetComponentData(
                    owner,
                    new OperationMapRenderStateChangeSequenceComponent());
                EnsurePersistentCapacity(
                    stateOwnerCount,
                    blob.Placements.Length,
                    blob.Cells.Length);

                using NativeArray<Entity> buildingEntities =
                    _buildingQuery.ToEntityArray(Allocator.Temp);
                var initialStates = new NativeArray<
                    OperationMapRenderVisualState>(
                    stateOwnerCount,
                    Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory);
                var stateOwnerIndices = new NativeArray<int>(
                    stateOwnerCount,
                    Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory);
                for (int index = 0; index < buildingEntities.Length; index++)
                {
                    Entity building = buildingEntities[index];
                    stateOwnerIndices[index] = state.EntityManager.GetComponentData<
                        OperationMapVirtualizedBuildingPresentationComponent>(
                        building).StateOwnerIndex;
                    initialStates[index] = state.EntityManager.IsComponentEnabled<
                        OperationMapBuildingDestroyedComponent>(building)
                        ? OperationMapRenderVisualState.Destroyed
                        : OperationMapRenderVisualState.Intact;
                }

                dependency = new OperationMapRenderCanonicalStateInitializeJob
                {
                    InitialStates = initialStates,
                    StateOwnerIndices = stateOwnerIndices,
                    CanonicalStates = canonicalStates.AsNativeArray()
                }.Schedule(dependency);
                dependency = initialStates.Dispose(dependency);
                dependency = stateOwnerIndices.Dispose(dependency);

                syncState = new OperationMapRenderStateSyncStateComponent
                {
                    Initialized = 1,
                    Revision = NextNonzero(syncState.Revision),
                    StateOwnerCount = stateOwnerCount
                };
                state.EntityManager.SetComponentData(owner, syncState);
                _initializedDatabaseEntity = owner;
                _initializedMapGeneration = activeMap.Generation;
            }
            else if (canonicalStates.Length != stateOwnerCount ||
                     syncState.StateOwnerCount != stateOwnerCount)
            {
                throw new InvalidOperationException(
                    "Render canonical-state capacity changed after initialization.");
            }

            DynamicBuffer<OperationMapRenderStateChangeComponent> changes =
                state.EntityManager.GetBuffer<
                    OperationMapRenderStateChangeComponent>(owner);
            OperationMapRenderStateChangeSequenceComponent sequence =
                state.EntityManager.GetComponentData<
                    OperationMapRenderStateChangeSequenceComponent>(owner);
            ValidatePendingChanges(
                changes,
                sequence,
                syncState.LastAppliedChangeVersion,
                stateOwnerCount);
            if (changes.Length == 0)
            {
                state.Dependency = dependency;
                return;
            }
            uint nextRevision = NextNonzero(syncState.Revision);
            _syncStateLookup.Update(ref state);
            _virtualizationStateLookup.Update(ref state);
            dependency = new OperationMapRenderStateSyncJob
            {
                Database = database.Blob,
                Owner = owner,
                Changes = changes.AsNativeArray(),
                CanonicalStates = canonicalStates.AsNativeArray(),
                AffectedStateOwners = _affectedStateOwners,
                DirtyPlacements = _dirtyPlacements,
                DirtyCells = _dirtyCells,
                NextRevision = nextRevision,
                SyncStateLookup = _syncStateLookup,
                VirtualizationStateLookup = _virtualizationStateLookup
            }.Schedule(dependency);
            state.Dependency = new OperationMapRenderStateChangeClearJob
            {
                Changes = changes
            }.Schedule(dependency);
        }

        internal static int CalculateRequiredStateOwnerCount(
            ref OperationMapRenderDatabaseBlob blob)
        {
            int maximum = -1;
            for (int index = 0; index < blob.Placements.Length; index++)
            {
                OperationMapRenderPlacementBlob placement = blob.Placements[index];
                if (placement.StateOwnerIndex == -1)
                {
                    if (placement.RequiredVisualState !=
                        OperationMapRenderVisualState.Any)
                    {
                        throw new InvalidOperationException(
                            "A render-only placement has a state requirement.");
                    }
                    continue;
                }
                if (placement.StateOwnerIndex < 0 ||
                    placement.RequiredVisualState ==
                    OperationMapRenderVisualState.Any)
                {
                    throw new InvalidOperationException(
                        "A stateful placement has an invalid owner or visual state.");
                }
                maximum = Math.Max(maximum, placement.StateOwnerIndex);
            }
            if (maximum < 0)
            {
                ValidateCellMemberships(ref blob);
                return 0;
            }

            var seen = new NativeArray<byte>(
                maximum + 1,
                Allocator.Temp,
                NativeArrayOptions.ClearMemory);
            try
            {
                for (int index = 0; index < blob.Placements.Length; index++)
                {
                    int stateOwnerIndex = blob.Placements[index].StateOwnerIndex;
                    if (stateOwnerIndex >= 0)
                        seen[stateOwnerIndex] = 1;
                }
                for (int index = 0; index < seen.Length; index++)
                {
                    if (seen[index] == 0)
                    {
                        throw new InvalidOperationException(
                            "Render state-owner indices are not contiguous.");
                    }
                }
                ValidateCellMemberships(ref blob);
                return seen.Length;
            }
            finally
            {
                seen.Dispose();
            }
        }

        private void ValidateStateOwnerMapping(
            EntityManager entityManager,
            int requiredStateOwnerCount)
        {
            int buildingCount = _buildingQuery.CalculateEntityCount();
            if (buildingCount != requiredStateOwnerCount)
            {
                throw new InvalidOperationException(
                    "Virtualized building count does not match the canonical state array.");
            }
            var seen = new NativeArray<byte>(
                Math.Max(1, requiredStateOwnerCount),
                Allocator.Temp,
                NativeArrayOptions.ClearMemory);
            try
            {
                using NativeArray<Entity> buildings =
                    _buildingQuery.ToEntityArray(Allocator.Temp);
                for (int index = 0; index < buildings.Length; index++)
                {
                    int stateOwnerIndex = entityManager.GetComponentData<
                        OperationMapVirtualizedBuildingPresentationComponent>(
                        buildings[index]).StateOwnerIndex;
                    if (stateOwnerIndex < 0 ||
                        stateOwnerIndex >= requiredStateOwnerCount ||
                        seen[stateOwnerIndex] != 0)
                    {
                        throw new InvalidOperationException(
                            "Virtualized building state-owner indices must be unique and contiguous.");
                    }
                    seen[stateOwnerIndex] = 1;
                }
            }
            finally
            {
                seen.Dispose();
            }
        }

        private static void ValidatePendingChanges(
            DynamicBuffer<OperationMapRenderStateChangeComponent> changes,
            OperationMapRenderStateChangeSequenceComponent sequence,
            uint lastAppliedVersion,
            int stateOwnerCount)
        {
            if (changes.Length > stateOwnerCount)
            {
                throw new InvalidOperationException(
                    "Render state-change buffer exceeded its map-owned bound.");
            }
            if (changes.Length == 0)
            {
                if (sequence.LastPublishedVersion != lastAppliedVersion)
                {
                    throw new InvalidOperationException(
                        "Render state-change sequence advanced without a pending event.");
                }
                return;
            }

            var seenStateOwners = new NativeArray<byte>(
                Math.Max(1, stateOwnerCount),
                Allocator.Temp,
                NativeArrayOptions.ClearMemory);
            try
            {
                uint expected = lastAppliedVersion;
                for (int index = 0; index < changes.Length; index++)
                {
                    OperationMapRenderStateChangeComponent change = changes[index];
                    if (expected == uint.MaxValue)
                    {
                        throw new InvalidOperationException(
                            "Render state-change sequence overflowed.");
                    }
                    expected++;
                    if (change.ChangeVersion != expected ||
                        change.StateOwnerIndex < 0 ||
                        change.StateOwnerIndex >= stateOwnerCount ||
                        seenStateOwners[change.StateOwnerIndex] != 0 ||
                        (change.VisualState != OperationMapRenderVisualState.Intact &&
                         change.VisualState != OperationMapRenderVisualState.Destroyed))
                    {
                        throw new InvalidOperationException(
                            "Render state-change records are invalid or out of sequence.");
                    }
                    seenStateOwners[change.StateOwnerIndex] = 1;
                }
                if (sequence.LastPublishedVersion != expected)
                {
                    throw new InvalidOperationException(
                        "Render state-change producer and consumer versions diverged.");
                }
            }
            finally
            {
                seenStateOwners.Dispose();
            }
        }

        private static void ValidateCellMemberships(
            ref OperationMapRenderDatabaseBlob blob)
        {
            for (int cellIndex = 0; cellIndex < blob.Cells.Length; cellIndex++)
            {
                OperationMapRenderCellBlob cell = blob.Cells[cellIndex];
                if (cell.FirstPlacementIndex < 0 ||
                    cell.PlacementIndexCount < 0 ||
                    cell.FirstPlacementIndex >
                        blob.CellPlacementIndices.Length -
                        cell.PlacementIndexCount)
                {
                    throw new InvalidOperationException(
                        "Render state synchronization found an invalid cell range.");
                }
                int end = cell.FirstPlacementIndex + cell.PlacementIndexCount;
                for (int index = cell.FirstPlacementIndex; index < end; index++)
                {
                    int placementIndex = blob.CellPlacementIndices[index];
                    if (placementIndex < 0 ||
                        placementIndex >= blob.Placements.Length)
                    {
                        throw new InvalidOperationException(
                            "Render state synchronization found an invalid cell membership.");
                    }
                }
            }
        }

        private static void ValidateDatabase(
            OperationMapRenderDatabaseComponent database,
            ActiveOperationMapComponent activeMap)
        {
            if (!database.Blob.IsCreated ||
                activeMap.Generation <= 0 ||
                !database.Blob.Value.OperationMapId.Equals(activeMap.OperationMapId))
            {
                throw new InvalidOperationException(
                    "Render state synchronization received an invalid map database.");
            }
        }

        private void EnsurePersistentCapacity(
            int stateOwnerCount,
            int placementCount,
            int cellCount)
        {
            _affectedStateOwners = new NativeBitArray(
                Math.Max(1, stateOwnerCount),
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _dirtyPlacements = new NativeBitArray(
                Math.Max(1, placementCount),
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _dirtyCells = new NativeBitArray(
                Math.Max(1, cellCount),
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
        }

        private void CompleteAndDispose(ref SystemState state)
        {
            if (_dirtyPlacements.IsCreated)
                state.Dependency.Complete();
            if (_dirtyCells.IsCreated)
                _dirtyCells.Dispose();
            if (_dirtyPlacements.IsCreated)
                _dirtyPlacements.Dispose();
            if (_affectedStateOwners.IsCreated)
                _affectedStateOwners.Dispose();
            _initializedDatabaseEntity = Entity.Null;
            _initializedMapGeneration = 0;
        }

        private static uint NextNonzero(uint value)
        {
            uint next = value + 1u;
            return next == 0u ? 1u : next;
        }
    }

    [BurstCompile]
    internal struct OperationMapRenderCanonicalStateInitializeJob : IJob
    {
        [ReadOnly] internal NativeArray<OperationMapRenderVisualState> InitialStates;
        [ReadOnly] internal NativeArray<int> StateOwnerIndices;
        internal NativeArray<OperationMapRenderCanonicalStateComponent>
            CanonicalStates;

        [BurstCompile]
        public void Execute()
        {
            for (int index = 0; index < StateOwnerIndices.Length; index++)
            {
                CanonicalStates[StateOwnerIndices[index]] =
                    new OperationMapRenderCanonicalStateComponent
                    {
                        VisualState = InitialStates[index],
                        ChangeVersion = 0
                    };
            }
        }
    }

    [BurstCompile]
    internal struct OperationMapRenderStateSyncJob : IJob
    {
        [ReadOnly] internal BlobAssetReference<OperationMapRenderDatabaseBlob> Database;
        [ReadOnly] internal Entity Owner;
        [ReadOnly] internal NativeArray<OperationMapRenderStateChangeComponent> Changes;
        internal NativeArray<OperationMapRenderCanonicalStateComponent>
            CanonicalStates;
        internal NativeBitArray AffectedStateOwners;
        internal NativeBitArray DirtyPlacements;
        internal NativeBitArray DirtyCells;
        [ReadOnly] internal uint NextRevision;
        internal ComponentLookup<OperationMapRenderStateSyncStateComponent>
            SyncStateLookup;
        internal ComponentLookup<OperationMapRenderVirtualizationStateComponent>
            VirtualizationStateLookup;

        [BurstCompile]
        public void Execute()
        {
            using var profilerScope =
                OperationMapRenderVirtualizationProfilerMarkers.SyncState.Auto();
            AffectedStateOwners.SetBits(
                0, false, AffectedStateOwners.Length);
            DirtyPlacements.SetBits(0, false, DirtyPlacements.Length);
            DirtyCells.SetBits(0, false, DirtyCells.Length);

            uint lastVersion = 0;
            for (int index = 0; index < Changes.Length; index++)
            {
                OperationMapRenderStateChangeComponent change = Changes[index];
                CanonicalStates[change.StateOwnerIndex] =
                    new OperationMapRenderCanonicalStateComponent
                    {
                        VisualState = change.VisualState,
                        ChangeVersion = change.ChangeVersion
                    };
                AffectedStateOwners.Set(change.StateOwnerIndex, true);
                lastVersion = change.ChangeVersion;
            }

            ref OperationMapRenderDatabaseBlob blob = ref Database.Value;
            int dirtyPlacementCount = 0;
            for (int placementIndex = 0;
                 placementIndex < blob.Placements.Length;
                 placementIndex++)
            {
                int stateOwnerIndex =
                    blob.Placements[placementIndex].StateOwnerIndex;
                if (stateOwnerIndex < 0 ||
                    !AffectedStateOwners.IsSet(stateOwnerIndex))
                {
                    continue;
                }
                DirtyPlacements.Set(placementIndex, true);
                dirtyPlacementCount++;
            }

            int dirtyCellCount = 0;
            for (int cellIndex = 0; cellIndex < blob.Cells.Length; cellIndex++)
            {
                OperationMapRenderCellBlob cell = blob.Cells[cellIndex];
                int end = cell.FirstPlacementIndex + cell.PlacementIndexCount;
                for (int entryIndex = cell.FirstPlacementIndex;
                     entryIndex < end;
                     entryIndex++)
                {
                    if (!DirtyPlacements.IsSet(
                            blob.CellPlacementIndices[entryIndex]))
                    {
                        continue;
                    }
                    DirtyCells.Set(cellIndex, true);
                    dirtyCellCount++;
                    break;
                }
            }

            OperationMapRenderStateSyncStateComponent sync =
                SyncStateLookup[Owner];
            sync.Revision = NextRevision;
            sync.LastAppliedChangeVersion = lastVersion;
            sync.DirtyPlacementCount = dirtyPlacementCount;
            sync.DirtyCellCount = dirtyCellCount;
            SyncStateLookup[Owner] = sync;

            OperationMapRenderVirtualizationStateComponent runtime =
                VirtualizationStateLookup[Owner];
            runtime.DirtyPlacementCount = dirtyPlacementCount;
            VirtualizationStateLookup[Owner] = runtime;
        }
    }

    [BurstCompile]
    internal struct OperationMapRenderStateChangeClearJob : IJob
    {
        internal DynamicBuffer<OperationMapRenderStateChangeComponent> Changes;

        [BurstCompile]
        public void Execute()
        {
            Changes.Clear();
        }
    }
}
