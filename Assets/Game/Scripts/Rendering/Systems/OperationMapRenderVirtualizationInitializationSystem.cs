using System;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Rendering
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct OperationMapRenderVirtualizationInitializationSystem : ISystem
    {
        private EntityQuery _activeMapQuery;
        private EntityQuery _databaseQuery;
        private EntityQuery _stateOwnerQuery;
        private EntityQuery _slotQuery;
        private OperationMapRenderVirtualizationNativeState _nativeState;
        private Entity _initializedDatabaseEntity;
        private int _initializedMapGeneration;

        internal bool IsPersistentStateCreated => _nativeState.IsCreated;
        internal int PersistentSlotCapacity => _nativeState.SlotCapacity;

        public void OnCreate(ref SystemState state)
        {
            _activeMapQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ActiveOperationMapComponent>());
            _databaseQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<OperationMapRenderDatabaseComponent>());
            _stateOwnerQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<OperationMapRenderVirtualizationStateComponent>());
            _slotQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<OperationMapRenderProxySlotComponent>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities |
                          EntityQueryOptions.IgnoreComponentEnabledState
            });
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

            int stateOwnerCount = _stateOwnerQuery.CalculateEntityCount();
            int activeMapCount = _activeMapQuery.CalculateEntityCount();
            Entity databaseEntity = databaseCount == 1
                ? _databaseQuery.GetSingletonEntity()
                : Entity.Null;
            Entity stateOwnerEntity = stateOwnerCount == 1
                ? _stateOwnerQuery.GetSingletonEntity()
                : Entity.Null;
            ValidateOwnership(
                databaseCount,
                stateOwnerCount,
                activeMapCount,
                databaseEntity,
                stateOwnerEntity);

            EntityManager entityManager = state.EntityManager;
            if (!entityManager.HasComponent<OperationMapRenderPackedReadinessComponent>(
                    databaseEntity) ||
                !entityManager.HasComponent<OperationMapRenderVirtualizationMetricsComponent>(
                    databaseEntity) ||
                !entityManager.HasBuffer<OperationMapRenderStateChangeComponent>(
                    databaseEntity))
            {
                throw new InvalidOperationException(
                    "The render database is missing its packed readiness, metrics, or state-change owner.");
            }

            ActiveOperationMapComponent activeMap =
                _activeMapQuery.GetSingleton<ActiveOperationMapComponent>();
            if (activeMap.Generation <= 0)
            {
                throw new InvalidOperationException(
                    "Render virtualization requires a positive active map generation.");
            }

            if (_nativeState.IsCreated &&
                _initializedDatabaseEntity == databaseEntity &&
                _initializedMapGeneration == activeMap.Generation)
            {
                return;
            }

            bool generationChanged = _nativeState.IsCreated;
            using var profilerScope =
                OperationMapRenderVirtualizationProfilerMarkers
                    .Initialize.Auto();
            CompleteAndDispose(ref state);

            OperationMapRenderDatabaseComponent database =
                entityManager.GetComponentData<OperationMapRenderDatabaseComponent>(
                    databaseEntity);
            OperationMapRenderPackedReadinessComponent readiness =
                entityManager.GetComponentData<OperationMapRenderPackedReadinessComponent>(
                    databaseEntity);
            ValidateDatabaseIdentity(database, readiness, activeMap);

            using NativeArray<OperationMapRenderProxySlotComponent> slots =
                _slotQuery.ToComponentDataArray<OperationMapRenderProxySlotComponent>(
                    Allocator.Temp);
            _nativeState.Initialize(database.Blob, readiness, slots);

            database.MapGeneration = activeMap.Generation;
            entityManager.SetComponentData(databaseEntity, database);
            entityManager.SetComponentData(
                databaseEntity,
                new OperationMapRenderVirtualizationStateComponent
                {
                    Initialized = 1
                });
            entityManager.SetComponentData(
                databaseEntity,
                new OperationMapRenderVirtualizationMetricsComponent
                {
                    LogicalPlacementCount = database.Blob.Value.Placements.Length,
                    LogicalPartCount = _nativeState.LogicalRowCapacity,
                    ResidentExceptionCount = readiness.ResidentSourceRowCount,
                    Capacity = _nativeState.SlotCapacity,
                    DisabledSlotCount = _nativeState.SlotCapacity,
                    RebuildReason = generationChanged
                        ? OperationMapRenderRebuildReason.MapGenerationChanged
                        : OperationMapRenderRebuildReason.InitialView
                });
            entityManager.GetBuffer<OperationMapRenderStateChangeComponent>(
                databaseEntity).Clear();

            _initializedDatabaseEntity = databaseEntity;
            _initializedMapGeneration = activeMap.Generation;
        }

        internal static void ValidateOwnership(
            int databaseCount,
            int stateOwnerCount,
            int activeMapCount,
            Entity databaseEntity,
            Entity stateOwnerEntity)
        {
            if (databaseCount != 1)
            {
                throw new InvalidOperationException(
                    $"Render virtualization requires exactly one database, found {databaseCount}.");
            }
            if (stateOwnerCount != 1)
            {
                throw new InvalidOperationException(
                    "Render virtualization requires exactly one runtime state owner.");
            }
            if (activeMapCount != 1)
            {
                throw new InvalidOperationException(
                    "Render virtualization requires exactly one active operation map.");
            }
            if (databaseEntity != stateOwnerEntity)
            {
                throw new InvalidOperationException(
                    "The render database entity must own the virtualization runtime state.");
            }
        }

        internal static void ValidateDatabaseIdentity(
            OperationMapRenderDatabaseComponent database,
            OperationMapRenderPackedReadinessComponent readiness,
            ActiveOperationMapComponent activeMap)
        {
            if (!database.Blob.IsCreated)
            {
                throw new InvalidOperationException(
                    "Render virtualization database blob is not created.");
            }

            ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
            if (database.SchemaVersion <= 0 ||
                database.SchemaVersion != blob.SchemaVersion ||
                database.ContentHash.Length == 0 ||
                !database.ContentHash.Equals(blob.ContentHash) ||
                !blob.OperationMapId.Equals(activeMap.OperationMapId))
            {
                throw new InvalidOperationException(
                    "Render virtualization database map, schema, or content identity is invalid.");
            }
            if (readiness.ResidencyMode !=
                (byte)OperationMapRenderResidencyMode.VirtualizedProxyPool)
            {
                throw new InvalidOperationException(
                    "Render virtualization initialization requires VirtualizedProxyPool residency.");
            }
        }

        private void CompleteAndDispose(ref SystemState state)
        {
            if (_nativeState.IsCreated)
            {
                state.Dependency.Complete();
                _nativeState.Dispose();
            }
            _initializedDatabaseEntity = Entity.Null;
            _initializedMapGeneration = 0;
        }
    }

    internal struct OperationMapRenderVirtualizationNativeState : IDisposable
    {
        private NativeArray<int> _slotToLogicalRow;
        private NativeArray<int> _logicalRowToSlot;
        private NativeArray<int> _placementFirstLogicalRow;

        internal bool IsCreated => _slotToLogicalRow.IsCreated;
        internal int SlotCapacity => _slotToLogicalRow.IsCreated
            ? _slotToLogicalRow.Length
            : 0;
        internal int LogicalRowCapacity => _logicalRowToSlot.IsCreated
            ? _logicalRowToSlot.Length
            : 0;
        internal int PlacementCapacity => _placementFirstLogicalRow.IsCreated
            ? _placementFirstLogicalRow.Length - 1
            : 0;

        internal int GetSlotBinding(int slotIndex) =>
            _slotToLogicalRow[slotIndex];

        internal int GetLogicalRowBinding(int logicalRowIndex) =>
            _logicalRowToSlot[logicalRowIndex];

        internal int GetPlacementFirstLogicalRow(int placementIndex) =>
            _placementFirstLogicalRow[placementIndex];

        internal void Initialize(
            BlobAssetReference<OperationMapRenderDatabaseBlob> database,
            OperationMapRenderPackedReadinessComponent readiness,
            NativeArray<OperationMapRenderProxySlotComponent> slots)
        {
            if (IsCreated)
                throw new InvalidOperationException(
                    "Render virtualization native state is already initialized.");

            ref OperationMapRenderDatabaseBlob blob = ref database.Value;
            int expectedSlotCount = ValidatePoolBuckets(ref blob);
            if (expectedSlotCount != readiness.ProxySlotCount ||
                slots.Length != expectedSlotCount)
            {
                throw new InvalidOperationException(
                    "Proxy-slot count does not match the packed database contract.");
            }
            ValidateSlots(ref blob, slots, expectedSlotCount);

            int logicalRowCount = CalculateLogicalRowCount(ref blob);
            _slotToLogicalRow = new NativeArray<int>(
                expectedSlotCount,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            _logicalRowToSlot = new NativeArray<int>(
                logicalRowCount,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            _placementFirstLogicalRow = new NativeArray<int>(
                blob.Placements.Length + 1,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            for (int index = 0; index < _slotToLogicalRow.Length; index++)
                _slotToLogicalRow[index] = -1;
            for (int index = 0; index < _logicalRowToSlot.Length; index++)
                _logicalRowToSlot[index] = -1;

            int logicalRow = 0;
            for (int placementIndex = 0;
                 placementIndex < blob.Placements.Length;
                 placementIndex++)
            {
                _placementFirstLogicalRow[placementIndex] = logicalRow;
                OperationMapRenderPlacementBlob placement =
                    blob.Placements[placementIndex];
                logicalRow = checked(
                    logicalRow + blob.Prototypes[placement.PrototypeIndex].PartCount);
            }
            _placementFirstLogicalRow[blob.Placements.Length] = logicalRow;
        }

        public void Dispose()
        {
            if (_placementFirstLogicalRow.IsCreated)
                _placementFirstLogicalRow.Dispose();
            if (_logicalRowToSlot.IsCreated)
                _logicalRowToSlot.Dispose();
            if (_slotToLogicalRow.IsCreated)
                _slotToLogicalRow.Dispose();
        }

        private static int ValidatePoolBuckets(
            ref OperationMapRenderDatabaseBlob blob)
        {
            if (blob.Prototypes.Length <= 0 ||
                blob.Parts.Length <= 0 ||
                blob.Placements.Length <= 0 ||
                blob.Cells.Length <= 0 ||
                blob.PoolBuckets.Length <= 0)
            {
                throw new InvalidOperationException(
                    "Render virtualization database contains an empty required array.");
            }

            int expectedSlotCount = 0;
            for (int bucketIndex = 0;
                 bucketIndex < blob.PoolBuckets.Length;
                 bucketIndex++)
            {
                OperationMapRenderPoolBucketBlob bucket =
                    blob.PoolBuckets[bucketIndex];
                if (bucket.FirstSlot != expectedSlotCount ||
                    bucket.Capacity <= 0 ||
                    bucket.PeakRequiredCount <= 0 ||
                    bucket.HeadroomCount < 0)
                {
                    throw new InvalidOperationException(
                        "Render virtualization pool buckets are not positive contiguous ranges.");
                }
                expectedSlotCount = checked(expectedSlotCount + bucket.Capacity);
            }
            return expectedSlotCount;
        }

        private static void ValidateSlots(
            ref OperationMapRenderDatabaseBlob blob,
            NativeArray<OperationMapRenderProxySlotComponent> slots,
            int expectedSlotCount)
        {
            using var seenSlots = new NativeBitArray(
                expectedSlotCount,
                Allocator.Temp,
                NativeArrayOptions.ClearMemory);
            for (int index = 0; index < slots.Length; index++)
            {
                OperationMapRenderProxySlotComponent slot = slots[index];
                if (slot.SlotIndex < 0 ||
                    slot.SlotIndex >= expectedSlotCount ||
                    seenSlots.IsSet(slot.SlotIndex) ||
                    slot.PoolBucketIndex < 0 ||
                    slot.PoolBucketIndex >= blob.PoolBuckets.Length)
                {
                    throw new InvalidOperationException(
                        "Proxy slots contain an invalid or duplicate stable identity.");
                }
                OperationMapRenderPoolBucketBlob bucket =
                    blob.PoolBuckets[slot.PoolBucketIndex];
                if (slot.SlotIndex < bucket.FirstSlot ||
                    slot.SlotIndex >= bucket.FirstSlot + bucket.Capacity)
                {
                    throw new InvalidOperationException(
                        "A proxy slot is outside its immutable pool-bucket range.");
                }
                seenSlots.Set(slot.SlotIndex, true);
            }
        }

        private static int CalculateLogicalRowCount(
            ref OperationMapRenderDatabaseBlob blob)
        {
            int logicalRowCount = 0;
            for (int placementIndex = 0;
                 placementIndex < blob.Placements.Length;
                 placementIndex++)
            {
                OperationMapRenderPlacementBlob placement =
                    blob.Placements[placementIndex];
                if (placement.PrototypeIndex < 0 ||
                    placement.PrototypeIndex >= blob.Prototypes.Length)
                {
                    throw new InvalidOperationException(
                        "A render placement references an invalid prototype.");
                }
                OperationMapRenderPrototypeBlob prototype =
                    blob.Prototypes[placement.PrototypeIndex];
                if (prototype.FirstPart < 0 ||
                    prototype.PartCount <= 0 ||
                    prototype.FirstPart > blob.Parts.Length - prototype.PartCount)
                {
                    throw new InvalidOperationException(
                        "A render prototype contains an invalid part range.");
                }
                logicalRowCount = checked(logicalRowCount + prototype.PartCount);
            }
            return logicalRowCount;
        }
    }
}
