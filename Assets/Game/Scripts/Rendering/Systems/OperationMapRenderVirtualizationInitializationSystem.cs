using System;
using Game.Components;
using Game.Configs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Rendering
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RuntimeCameraReferenceSystem))]
    public partial struct OperationMapRenderVirtualizationInitializationSystem : ISystem
    {
        private EntityQuery _activeMapQuery;
        private EntityQuery _databaseQuery;
        private EntityQuery _stateOwnerQuery;
        private EntityQuery _slotQuery;
        private EntityQuery _cameraQuery;
        private OperationMapRenderVirtualizationNativeState _nativeState;
        private Entity _initializedDatabaseEntity;
        private int _initializedMapGeneration;
        private OperationMapRenderCellEnvelope _guardEnvelope;
        private uint _scheduledCameraVersion;
        private uint _commandVersion;
        private int _assignmentGeneration;
        private byte _hasGuardEnvelope;
        private ComponentLookup<OperationMapRenderVirtualizationStateComponent>
            _virtualizationStateLookup;
        private ComponentLookup<OperationMapRenderVirtualizationMetricsComponent>
            _metricsLookup;
        private ComponentLookup<OperationMapRenderSlotCommandStateComponent>
            _commandStateLookup;

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
            _cameraQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<RuntimeCameraSnapshotComponent>());
            _virtualizationStateLookup = state.GetComponentLookup<
                OperationMapRenderVirtualizationStateComponent>(false);
            _metricsLookup = state.GetComponentLookup<
                OperationMapRenderVirtualizationMetricsComponent>(false);
            _commandStateLookup = state.GetComponentLookup<
                OperationMapRenderSlotCommandStateComponent>(false);
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
                ScheduleCameraRebuildIfRequired(
                    ref state,
                    databaseEntity,
                    entityManager.GetComponentData<
                        OperationMapRenderDatabaseComponent>(databaseEntity));
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
            ScheduleCameraRebuildIfRequired(
                ref state,
                databaseEntity,
                database);
        }

        private void ScheduleCameraRebuildIfRequired(
            ref SystemState state,
            Entity databaseEntity,
            OperationMapRenderDatabaseComponent database)
        {
            if (_cameraQuery.CalculateEntityCount() != 1)
                return;
            RuntimeCameraSnapshotComponent camera =
                _cameraQuery.GetSingleton<RuntimeCameraSnapshotComponent>();
            if (camera.IsValid == 0 ||
                camera.PublicationVersion == _scheduledCameraVersion)
            {
                return;
            }
            if (!TryBuildCameraEnvelopes(
                    database.Blob,
                    camera.Position,
                    out OperationMapRenderCellEnvelope requiredEnvelope,
                    out OperationMapRenderCellEnvelope materializedEnvelope))
            {
                throw new InvalidOperationException(
                    "Render virtualization camera envelope is invalid.");
            }
            if (_hasGuardEnvelope != 0 &&
                OperationMapRenderGuardEnvelopeDecision.Contains(
                    _guardEnvelope,
                    requiredEnvelope))
            {
                _scheduledCameraVersion = camera.PublicationVersion;
                return;
            }
            if (!state.EntityManager.HasBuffer<
                    OperationMapRenderSlotCommandComponent>(databaseEntity) ||
                !state.EntityManager.HasComponent<
                    OperationMapRenderSlotCommandStateComponent>(databaseEntity))
            {
                throw new InvalidOperationException(
                    "Render virtualization runtime owner is missing its fixed command contract.");
            }
            DynamicBuffer<OperationMapRenderSlotCommandComponent> commands =
                state.EntityManager.GetBuffer<
                    OperationMapRenderSlotCommandComponent>(databaseEntity);
            if (commands.Length != _nativeState.SlotCapacity)
            {
                throw new InvalidOperationException(
                    "Render virtualization command capacity does not match fixed slots.");
            }

            _assignmentGeneration = NextPositive(_assignmentGeneration);
            _commandVersion = NextNonzero(_commandVersion);
            _virtualizationStateLookup.Update(ref state);
            _metricsLookup.Update(ref state);
            _commandStateLookup.Update(ref state);
            state.Dependency = _nativeState.Schedule(
                database.Blob,
                materializedEnvelope,
                _assignmentGeneration,
                _commandVersion,
                commands.AsNativeArray(),
                databaseEntity,
                _virtualizationStateLookup,
                _metricsLookup,
                _commandStateLookup,
                state.Dependency);
            _guardEnvelope = materializedEnvelope;
            _hasGuardEnvelope = 1;
            _scheduledCameraVersion = camera.PublicationVersion;
        }

        internal static bool TryBuildCameraEnvelopes(
            BlobAssetReference<OperationMapRenderDatabaseBlob> database,
            float3 cameraPosition,
            out OperationMapRenderCellEnvelope requiredEnvelope,
            out OperationMapRenderCellEnvelope materializedEnvelope)
        {
            requiredEnvelope = default;
            materializedEnvelope = default;
            if (!database.IsCreated ||
                !math.all(math.isfinite(cameraPosition)))
                return false;
            ref OperationMapRenderDatabaseBlob blob = ref database.Value;
            if (!math.isfinite(blob.CellSize) ||
                blob.CellSize <= 0f ||
                blob.GridDimensions.x <= 0 ||
                blob.GridDimensions.y <= 0)
                return false;

            int2 coordinateOffset = new(
                (int)math.round(blob.GridOrigin.x / blob.CellSize),
                (int)math.round(blob.GridOrigin.z / blob.CellSize));
            int2 maximum = coordinateOffset + blob.GridDimensions - 1;
            int2 center = new(
                (int)math.floor(cameraPosition.x / blob.CellSize),
                (int)math.floor(cameraPosition.z / blob.CellSize));
            center = math.clamp(center, coordinateOffset, maximum);
            requiredEnvelope = new OperationMapRenderCellEnvelope
            {
                Min = math.max(center - 1, coordinateOffset),
                Max = math.min(center + 1, maximum)
            };
            materializedEnvelope = new OperationMapRenderCellEnvelope
            {
                Min = math.max(center - 2, coordinateOffset),
                Max = math.min(center + 2, maximum)
            };
            return true;
        }

        private static int NextPositive(int value) =>
            value == int.MaxValue ? 1 : value + 1;

        private static uint NextNonzero(uint value)
        {
            uint next = value + 1u;
            return next == 0u ? 1u : next;
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
            _guardEnvelope = default;
            _scheduledCameraVersion = 0;
            _commandVersion = 0;
            _assignmentGeneration = 0;
            _hasGuardEnvelope = 0;
        }
    }

    internal struct OperationMapRenderVirtualizationNativeState : IDisposable
    {
        private NativeArray<int> _slotToLogicalRow;
        private NativeArray<int> _logicalRowToSlot;
        private NativeArray<int> _placementFirstLogicalRow;
        private NativeArray<int> _slotAssignmentGenerations;
        private NativeBitArray _requiredSlots;
        private NativeBitArray _dirtySlots;
        private NativeBitArray _activeCells;
        private NativeBitArray _activePlacements;
        private NativeBitArray _visitedPlacements;
        private NativeArray<int> _nextFreeSlotByBucket;
        private NativeArray<OperationMapRenderVisualState> _canonicalVisualStates;
        private NativeList<int> _selectedCells;
        private NativeList<int> _selectedPlacements;
        private NativeList<OperationMapRenderLogicalRowKey> _selectedLogicalRows;
        private NativeList<int> _dirtySlotIndices;
        private NativeReference<OperationMapRenderCellSelectionFailure>
            _selectionFailure;
        private NativeReference<OperationMapRenderAssignmentResult>
            _assignmentResult;

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
            _slotAssignmentGenerations = new NativeArray<int>(
                expectedSlotCount,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _requiredSlots = new NativeBitArray(
                expectedSlotCount,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _dirtySlots = new NativeBitArray(
                expectedSlotCount,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _activeCells = new NativeBitArray(
                blob.Cells.Length,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _activePlacements = new NativeBitArray(
                blob.Placements.Length,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _visitedPlacements = new NativeBitArray(
                blob.Placements.Length,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _nextFreeSlotByBucket = new NativeArray<int>(
                blob.PoolBuckets.Length,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _canonicalVisualStates =
                new NativeArray<OperationMapRenderVisualState>(
                    0,
                    Allocator.Persistent);
            _selectedCells = new NativeList<int>(
                blob.Cells.Length,
                Allocator.Persistent);
            _selectedPlacements = new NativeList<int>(
                blob.Placements.Length,
                Allocator.Persistent);
            _selectedLogicalRows =
                new NativeList<OperationMapRenderLogicalRowKey>(
                    logicalRowCount,
                    Allocator.Persistent);
            _dirtySlotIndices = new NativeList<int>(
                expectedSlotCount,
                Allocator.Persistent);
            _selectionFailure =
                new NativeReference<OperationMapRenderCellSelectionFailure>(
                    Allocator.Persistent);
            _assignmentResult =
                new NativeReference<OperationMapRenderAssignmentResult>(
                    Allocator.Persistent);

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

        internal JobHandle Schedule(
            BlobAssetReference<OperationMapRenderDatabaseBlob> database,
            OperationMapRenderCellEnvelope materializedEnvelope,
            int assignmentGeneration,
            uint commandVersion,
            NativeArray<OperationMapRenderSlotCommandComponent> commands,
            Entity owner,
            ComponentLookup<OperationMapRenderVirtualizationStateComponent>
                stateLookup,
            ComponentLookup<OperationMapRenderVirtualizationMetricsComponent>
                metricsLookup,
            ComponentLookup<OperationMapRenderSlotCommandStateComponent>
                commandStateLookup,
            JobHandle dependency)
        {
            var selection = new OperationMapRenderRequiredCellSelectionJob
            {
                Database = database,
                RequiredEnvelope = materializedEnvelope,
                MaxSelectedCellCount = _selectedCells.Capacity,
                SelectedCellIndices = _selectedCells,
                Failure = _selectionFailure
            };
            JobHandle selectionHandle = selection.Schedule(dependency);
            var gather = new OperationMapRenderPlacementGatherJob
            {
                Database = database,
                SelectedCellIndices = _selectedCells,
                CanonicalVisualStates = _canonicalVisualStates,
                MaxSelectedPlacementCount = _selectedPlacements.Capacity,
                MaxSelectedLogicalRowCount = _selectedLogicalRows.Capacity,
                VisitedPlacements = _visitedPlacements,
                SelectedPlacementIndices = _selectedPlacements,
                SelectedLogicalRows = _selectedLogicalRows,
                Failure = _selectionFailure
            };
            JobHandle gatherHandle = gather.Schedule(selectionHandle);
            var assignment = new OperationMapRenderAssignmentJob
            {
                Database = database,
                SelectedCellIndices = _selectedCells,
                RequiredRows = _selectedLogicalRows,
                PlacementFirstLogicalRow = _placementFirstLogicalRow,
                SelectionFailure = _selectionFailure,
                AssignmentGeneration = assignmentGeneration,
                MaxDirtySlotCount = _dirtySlotIndices.Capacity,
                SlotToLogicalRow = _slotToLogicalRow,
                LogicalRowToSlot = _logicalRowToSlot,
                SlotAssignmentGenerations = _slotAssignmentGenerations,
                RequiredSlots = _requiredSlots,
                DirtySlots = _dirtySlots,
                ActiveCells = _activeCells,
                ActivePlacements = _activePlacements,
                NextFreeSlotByBucket = _nextFreeSlotByBucket,
                SlotCommands = commands,
                DirtySlotIndices = _dirtySlotIndices,
                Result = _assignmentResult
            };
            JobHandle assignmentHandle = assignment.Schedule(gatherHandle);
            return new OperationMapRenderAssignmentPublishJob
            {
                Owner = owner,
                MaterializedEnvelope = materializedEnvelope,
                CommandVersion = commandVersion,
                SelectionFailure = _selectionFailure,
                AssignmentResult = _assignmentResult,
                SelectedCellIndices = _selectedCells,
                SelectedPlacementIndices = _selectedPlacements,
                StateLookup = stateLookup,
                MetricsLookup = metricsLookup,
                CommandStateLookup = commandStateLookup
            }.Schedule(assignmentHandle);
        }

        public void Dispose()
        {
            if (_assignmentResult.IsCreated)
                _assignmentResult.Dispose();
            if (_selectionFailure.IsCreated)
                _selectionFailure.Dispose();
            if (_dirtySlotIndices.IsCreated)
                _dirtySlotIndices.Dispose();
            if (_selectedLogicalRows.IsCreated)
                _selectedLogicalRows.Dispose();
            if (_selectedPlacements.IsCreated)
                _selectedPlacements.Dispose();
            if (_selectedCells.IsCreated)
                _selectedCells.Dispose();
            if (_nextFreeSlotByBucket.IsCreated)
                _nextFreeSlotByBucket.Dispose();
            if (_canonicalVisualStates.IsCreated)
                _canonicalVisualStates.Dispose();
            if (_visitedPlacements.IsCreated)
                _visitedPlacements.Dispose();
            if (_activePlacements.IsCreated)
                _activePlacements.Dispose();
            if (_activeCells.IsCreated)
                _activeCells.Dispose();
            if (_dirtySlots.IsCreated)
                _dirtySlots.Dispose();
            if (_requiredSlots.IsCreated)
                _requiredSlots.Dispose();
            if (_slotAssignmentGenerations.IsCreated)
                _slotAssignmentGenerations.Dispose();
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

    [BurstCompile]
    internal struct OperationMapRenderAssignmentPublishJob : IJob
    {
        [ReadOnly] internal Entity Owner;
        [ReadOnly] internal OperationMapRenderCellEnvelope MaterializedEnvelope;
        [ReadOnly] internal uint CommandVersion;
        [ReadOnly] internal NativeReference<
            OperationMapRenderCellSelectionFailure> SelectionFailure;
        [ReadOnly] internal NativeReference<
            OperationMapRenderAssignmentResult> AssignmentResult;
        [ReadOnly] internal NativeList<int> SelectedCellIndices;
        [ReadOnly] internal NativeList<int> SelectedPlacementIndices;
        internal ComponentLookup<OperationMapRenderVirtualizationStateComponent>
            StateLookup;
        internal ComponentLookup<OperationMapRenderVirtualizationMetricsComponent>
            MetricsLookup;
        internal ComponentLookup<OperationMapRenderSlotCommandStateComponent>
            CommandStateLookup;

        [BurstCompile]
        public void Execute()
        {
            OperationMapRenderVirtualizationStateComponent runtime =
                StateLookup[Owner];
            OperationMapRenderVirtualizationMetricsComponent metrics =
                MetricsLookup[Owner];
            OperationMapRenderAssignmentResult assignment =
                AssignmentResult.Value;
            if (SelectionFailure.Value !=
                    OperationMapRenderCellSelectionFailure.None ||
                assignment.Failure != OperationMapRenderAssignmentFailure.None)
            {
                runtime.OverflowCount = math.max(runtime.OverflowCount, 1);
                metrics.OverflowCount = runtime.OverflowCount;
                metrics.HighestDeficit = math.max(metrics.HighestDeficit, 1);
                StateLookup[Owner] = runtime;
                MetricsLookup[Owner] = metrics;
                return;
            }

            runtime.ActiveEnvelopeMin = MaterializedEnvelope.Min;
            runtime.ActiveEnvelopeMax = MaterializedEnvelope.Max;
            runtime.ActiveSlotCount = assignment.RetainedCount +
                                      assignment.AssignedCount;
            runtime.OverflowCount = assignment.OverflowCount;
            runtime.RebuildCount++;
            metrics.EnabledSlotCount = runtime.ActiveSlotCount;
            metrics.DisabledSlotCount =
                math.max(0, metrics.Capacity - runtime.ActiveSlotCount);
            metrics.RetainedCount = assignment.RetainedCount;
            metrics.ReleasedCount = assignment.ReleasedCount;
            metrics.ReboundCount = assignment.AssignedCount;
            metrics.ActiveCellCount = SelectedCellIndices.Length;
            metrics.ActivePlacementCount = SelectedPlacementIndices.Length;
            metrics.OverflowCount = assignment.OverflowCount;
            metrics.HighestDeficit =
                math.max(metrics.HighestDeficit, assignment.OverflowCount);
            metrics.CommandVersion = CommandVersion;
            metrics.RebuildReason = runtime.RebuildCount == 1
                ? OperationMapRenderRebuildReason.InitialView
                : OperationMapRenderRebuildReason.CameraEnvelopeChanged;
            CommandStateLookup[Owner] =
                new OperationMapRenderSlotCommandStateComponent
                {
                    Version = CommandVersion
                };
            StateLookup[Owner] = runtime;
            MetricsLookup[Owner] = metrics;
        }
    }
}
