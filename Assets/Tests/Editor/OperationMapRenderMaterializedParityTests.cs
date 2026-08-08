using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public static partial class OperationMapRenderMaterializedParityTests
{
    private const string CapacityContractPath =
        "Design/AgentReports/2026-07-28_dense_city_render_virtualization_capacity_budget.json";
    private const string ReportPath =
        "Design/AgentReports/2026-08-08_dense_city_render_materialized_parity.json";

    public static void RunCompileValidation()
    {
        Debug.Log(
            "[OperationMapRenderMaterializedParityCompileValidation] result=Passed");
        ValidationExit.Exit(0);
    }

    public static void RunFocusedValidation()
    {
        try
        {
            MaterializedParityReport report = RunValidation();
            string json = JsonUtility.ToJson(report, true) + Environment.NewLine;
            File.WriteAllText(
                Path.Combine(Directory.GetCurrentDirectory(), ReportPath),
                json,
                new UTF8Encoding(false));
            Debug.Log(
                "[OperationMapRenderMaterializedParityValidation] " +
                $"result=Passed envelopes={report.canonicalEnvelopeCount} states={report.stateVariantCount} " +
                $"samples={report.sampleCount} maxEnabled={report.maximumEnabledSlotCount} " +
                $"fingerprint={report.materializedFingerprint} report={ReportPath}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[OperationMapRenderMaterializedParityValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static MaterializedParityReport RunValidation()
    {
        OperationMapRenderDatabaseBakeConfig config =
            AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(
                OperationMapRenderDatabaseBuilder.ConfigPath);
        Assert.That(config, Is.Not.Null, "Current render database config is missing.");
        Assert.That(
            OperationMapRenderDatabaseBlobBuilder.TryBuild(
                config,
                out BlobAssetReference<OperationMapRenderDatabaseBlob> database,
                out string error),
            Is.True,
            error);

        try
        {
            using var fixture = new MaterializedFixture(database);
            var stateRows = new List<MaterializedParityStateRow>(2);
            var fingerprint = new StringBuilder(4 * 1024 * 1024);
            int maximumEnabled = 0;
            CanonicalEnvelopeSample[] canonicalEnvelopes =
                BuildCanonicalEnvelopes(database);
            int canonicalEnvelopeCount = canonicalEnvelopes.Length;
            Assert.That(canonicalEnvelopeCount, Is.EqualTo(14592));
            OperationMapRenderVisualState[] variants =
            {
                OperationMapRenderVisualState.Intact,
                OperationMapRenderVisualState.Destroyed
            };

            for (int stateIndex = 0; stateIndex < variants.Length; stateIndex++)
            {
                fixture.SetCanonicalState(variants[stateIndex]);
                var stateRow = new MaterializedParityStateRow
                {
                    visualState = variants[stateIndex].ToString(),
                    minimumEnabledSlotCount = int.MaxValue
                };
                int stateEnvelopeCount = 0;
                foreach (CanonicalEnvelopeSample sample in canonicalEnvelopes)
                {
                    MaterializedParitySampleResult result = fixture.RunSample(
                        sample.identity,
                        variants[stateIndex],
                        sample.envelope,
                        fingerprint);
                    stateEnvelopeCount++;
                    stateRow.sampleCount++;
                    stateRow.minimumEnabledSlotCount = math.min(
                        stateRow.minimumEnabledSlotCount,
                        result.enabledSlotCount);
                    stateRow.maximumEnabledSlotCount = math.max(
                        stateRow.maximumEnabledSlotCount,
                        result.enabledSlotCount);
                    stateRow.totalRetainedSlotCount += result.retainedSlotCount;
                    stateRow.totalReleasedSlotCount += result.releasedSlotCount;
                    stateRow.totalReboundSlotCount += result.reboundSlotCount;
                    stateRow.totalDirtySlotCount += result.dirtySlotCount;
                    if (result.enabledSlotCount > stateRow.peakEnabledSlotCount)
                    {
                        stateRow.peakEnabledSlotCount = result.enabledSlotCount;
                        stateRow.peakSampleIdentity = sample.identity;
                    }
                    maximumEnabled = math.max(
                        maximumEnabled,
                        result.enabledSlotCount);
                }
                Assert.That(
                    stateEnvelopeCount,
                    Is.EqualTo(canonicalEnvelopeCount));
                stateRows.Add(stateRow);
            }

            return new MaterializedParityReport
            {
                schema = "warline.operation-map.render-materialized-parity",
                schemaVersion = 1,
                operationMapId = config.OperationMapId,
                databaseContentHash = config.ContentHash,
                capacityContractPath = CapacityContractPath,
                result = "Passed",
                canonicalEnvelopeCount = canonicalEnvelopeCount,
                stateVariantCount = variants.Length,
                sampleCount = canonicalEnvelopeCount * variants.Length,
                placementCount = database.Value.Placements.Length,
                logicalRowCount = fixture.TotalLogicalRowCount,
                fixedSlotCount = fixture.SlotCount,
                maximumEnabledSlotCount = maximumEnabled,
                materializedFingerprint = Sha256(fingerprint.ToString()),
                states = stateRows.ToArray(),
                productionCutover = 0
            };
        }
        finally
        {
            database.Dispose();
        }
    }

    private static CanonicalEnvelopeSample[] BuildCanonicalEnvelopes(
            BlobAssetReference<OperationMapRenderDatabaseBlob> database)
    {
        ref OperationMapRenderDatabaseBlob blob = ref database.Value;
        var samples = new List<CanonicalEnvelopeSample>(
            blob.GridDimensions.x * blob.GridDimensions.y * 2);
        int2 offset = new(
            (int)math.round(blob.GridOrigin.x / blob.CellSize),
            (int)math.round(blob.GridOrigin.z / blob.CellSize));
        for (int centerZ = 0; centerZ < blob.GridDimensions.y; centerZ++)
        {
            for (int centerX = 0; centerX < blob.GridDimensions.x; centerX++)
            {
                for (int orientation = 0; orientation < 2; orientation++)
                {
                    int envelopeWidth = orientation == 0 ? 12 : 9;
                    int envelopeHeight = orientation == 0 ? 9 : 12;
                    int minimumX = math.max(
                        0,
                        centerX - (envelopeWidth - 1) / 2);
                    int maximumX = math.min(
                        blob.GridDimensions.x - 1,
                        minimumX + envelopeWidth - 1);
                    int minimumZ = math.max(
                        0,
                        centerZ - (envelopeHeight - 1) / 2);
                    int maximumZ = math.min(
                        blob.GridDimensions.y - 1,
                        minimumZ + envelopeHeight - 1);
                    samples.Add(new CanonicalEnvelopeSample
                    {
                        identity =
                            $"grid-cell:{centerX + offset.x}:{centerZ + offset.y}:" +
                            $"materialized={envelopeWidth}x{envelopeHeight}",
                        envelope = new OperationMapRenderCellEnvelope
                        {
                            Min = new int2(minimumX, minimumZ) + offset,
                            Max = new int2(maximumX, maximumZ) + offset
                        }
                    });
                }
            }
        }
        return samples.ToArray();
    }

    private static string Sha256(string value)
    {
        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        var builder = new StringBuilder(hash.Length * 2);
        for (int index = 0; index < hash.Length; index++)
            builder.Append(hash[index].ToString("x2"));
        return builder.ToString();
    }

    private sealed class MaterializedFixture : IDisposable
    {
        private readonly BlobAssetReference<OperationMapRenderDatabaseBlob> _database;
        private readonly World _world;
        private readonly EntityManager _entityManager;
        private readonly MaterializedParityLookupSystem _lookupSystem;
        private readonly EntityQuery _slotQuery;
        private readonly NativeArray<Entity> _slotEntities;
        private NativeArray<OperationMapRenderCanonicalStateComponent>
            _canonicalStates;
        private readonly NativeList<int> _selectedCells;
        private readonly NativeList<int> _selectedPlacements;
        private readonly NativeList<OperationMapRenderLogicalRowKey>
            _selectedLogicalRows;
        private readonly NativeBitArray _visitedPlacements;
        private NativeReference<OperationMapRenderCellSelectionFailure>
            _selectionFailure;
        private readonly NativeArray<int> _placementFirstLogicalRow;
        private readonly NativeArray<int> _slotToLogicalRow;
        private readonly NativeArray<int> _logicalRowToSlot;
        private readonly NativeArray<int> _slotAssignmentGenerations;
        private readonly NativeBitArray _requiredSlots;
        private readonly NativeBitArray _dirtySlots;
        private readonly NativeBitArray _activeCells;
        private readonly NativeBitArray _activePlacements;
        private readonly NativeArray<int> _nextFreeSlotByBucket;
        private readonly NativeArray<OperationMapRenderSlotCommandComponent>
            _commands;
        private readonly NativeList<int> _dirtySlotIndices;
        private NativeReference<OperationMapRenderAssignmentResult>
            _assignmentResult;
        private NativeArray<OperationMapRenderSlotApplyFailure>
            _slotFailures;
        private int _generation;
        private int _activeSlotCount;

        internal int TotalLogicalRowCount { get; }
        internal int SlotCount { get; }

        internal MaterializedFixture(
            BlobAssetReference<OperationMapRenderDatabaseBlob> database)
        {
            _database = database;
            ref OperationMapRenderDatabaseBlob blob = ref database.Value;
            int stateOwnerCount = 0;
            int logicalRowCount = 0;
            _placementFirstLogicalRow = new NativeArray<int>(
                blob.Placements.Length + 1,
                Allocator.Persistent);
            for (int placementIndex = 0;
                 placementIndex < blob.Placements.Length;
                 placementIndex++)
            {
                _placementFirstLogicalRow[placementIndex] = logicalRowCount;
                OperationMapRenderPlacementBlob placement =
                    blob.Placements[placementIndex];
                logicalRowCount +=
                    blob.Prototypes[placement.PrototypeIndex].PartCount;
                if (placement.StateOwnerIndex >= 0)
                    stateOwnerCount = math.max(
                        stateOwnerCount,
                        placement.StateOwnerIndex + 1);
            }
            _placementFirstLogicalRow[blob.Placements.Length] = logicalRowCount;
            TotalLogicalRowCount = logicalRowCount;

            int slotCount = 0;
            for (int bucketIndex = 0;
                 bucketIndex < blob.PoolBuckets.Length;
                 bucketIndex++)
            {
                OperationMapRenderPoolBucketBlob bucket =
                    blob.PoolBuckets[bucketIndex];
                Assert.That(bucket.FirstSlot, Is.EqualTo(slotCount));
                slotCount += bucket.Capacity;
            }
            SlotCount = slotCount;

            _canonicalStates = new NativeArray<
                OperationMapRenderCanonicalStateComponent>(
                stateOwnerCount,
                Allocator.Persistent);
            _selectedCells = new NativeList<int>(
                blob.Cells.Length,
                Allocator.Persistent);
            _selectedPlacements = new NativeList<int>(
                blob.Placements.Length,
                Allocator.Persistent);
            _selectedLogicalRows = new NativeList<OperationMapRenderLogicalRowKey>(
                logicalRowCount,
                Allocator.Persistent);
            _visitedPlacements = new NativeBitArray(
                blob.Placements.Length,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _selectionFailure = new NativeReference<
                OperationMapRenderCellSelectionFailure>(Allocator.Persistent);
            _slotToLogicalRow = Filled(slotCount, -1);
            _logicalRowToSlot = Filled(logicalRowCount, -1);
            _slotAssignmentGenerations = Filled(slotCount, 0);
            _requiredSlots = Bits(slotCount);
            _dirtySlots = Bits(slotCount);
            _activeCells = Bits(blob.Cells.Length);
            _activePlacements = Bits(blob.Placements.Length);
            _nextFreeSlotByBucket = Filled(blob.PoolBuckets.Length, 0);
            _commands = new NativeArray<OperationMapRenderSlotCommandComponent>(
                slotCount,
                Allocator.Persistent);
            _dirtySlotIndices = new NativeList<int>(
                slotCount,
                Allocator.Persistent);
            _assignmentResult = new NativeReference<
                OperationMapRenderAssignmentResult>(Allocator.Persistent);
            _slotFailures = new NativeArray<OperationMapRenderSlotApplyFailure>(
                slotCount,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            _world = new World("OperationMapRenderMaterializedParity");
            _entityManager = _world.EntityManager;
            _lookupSystem = _world.GetOrCreateSystemManaged<
                MaterializedParityLookupSystem>();
            EntityArchetype archetype = _entityManager.CreateArchetype(
                typeof(OperationMapRenderProxySlotComponent),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(MaterialMeshInfo),
                typeof(URPMaterialPropertyBaseColor));
            _slotEntities = _entityManager.CreateEntity(
                archetype,
                slotCount,
                Allocator.Persistent);
            int slotIndex = 0;
            for (int bucketIndex = 0;
                 bucketIndex < blob.PoolBuckets.Length;
                 bucketIndex++)
            {
                OperationMapRenderPoolBucketBlob bucket =
                    blob.PoolBuckets[bucketIndex];
                for (int bucketSlot = 0;
                     bucketSlot < bucket.Capacity;
                     bucketSlot++, slotIndex++)
                {
                    _entityManager.SetComponentData(
                        _slotEntities[slotIndex],
                        new OperationMapRenderProxySlotComponent
                        {
                            SlotIndex = slotIndex,
                            PoolBucketIndex = bucketIndex,
                            PlacementIndex = -1,
                            PartIndex = -1
                        });
                    _entityManager.SetComponentEnabled<MaterialMeshInfo>(
                        _slotEntities[slotIndex],
                        false);
                }
            }
            _slotQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<OperationMapRenderProxySlotComponent>(),
                    ComponentType.ReadWrite<LocalToWorld>(),
                    ComponentType.ReadWrite<RenderBounds>(),
                    ComponentType.ReadWrite<MaterialMeshInfo>(),
                    ComponentType.ReadWrite<URPMaterialPropertyBaseColor>()
                },
                Options = EntityQueryOptions.IgnoreComponentEnabledState
            });
        }

        internal void SetCanonicalState(OperationMapRenderVisualState state)
        {
            for (int index = 0; index < _canonicalStates.Length; index++)
            {
                _canonicalStates[index] =
                    new OperationMapRenderCanonicalStateComponent
                    {
                        VisualState = state,
                        ChangeVersion = (uint)(index + 1)
                    };
            }
        }

        internal MaterializedParitySampleResult RunSample(
            string sampleIdentity,
            OperationMapRenderVisualState visualState,
            OperationMapRenderCellEnvelope materializedEnvelope,
            StringBuilder fingerprint)
        {
            _selectionFailure.Value = OperationMapRenderCellSelectionFailure.None;
            new OperationMapRenderRequiredCellSelectionJob
            {
                Database = _database,
                RequiredEnvelope = materializedEnvelope,
                MaxSelectedCellCount = _selectedCells.Capacity,
                SelectedCellIndices = _selectedCells,
                Failure = _selectionFailure
            }.Schedule().Complete();
            new OperationMapRenderPlacementGatherJob
            {
                Database = _database,
                SelectedCellIndices = _selectedCells,
                CanonicalStates = _canonicalStates,
                MaxSelectedPlacementCount = _selectedPlacements.Capacity,
                MaxSelectedLogicalRowCount = _selectedLogicalRows.Capacity,
                VisitedPlacements = _visitedPlacements,
                SelectedPlacementIndices = _selectedPlacements,
                SelectedLogicalRows = _selectedLogicalRows,
                Failure = _selectionFailure
            }.Schedule().Complete();
            Assert.That(
                _selectionFailure.Value,
                Is.EqualTo(OperationMapRenderCellSelectionFailure.None),
                $"Selection failed for {sampleIdentity}/{visualState}.");

            _generation++;
            _assignmentResult.Value = default;
            new OperationMapRenderAssignmentJob
            {
                Database = _database,
                SelectedCellIndices = _selectedCells,
                RequiredRows = _selectedLogicalRows,
                PlacementFirstLogicalRow = _placementFirstLogicalRow,
                SelectionFailure = _selectionFailure,
                AssignmentGeneration = _generation,
                MaxDirtySlotCount = _dirtySlotIndices.Capacity,
                SlotToLogicalRow = _slotToLogicalRow,
                LogicalRowToSlot = _logicalRowToSlot,
                SlotAssignmentGenerations = _slotAssignmentGenerations,
                RequiredSlots = _requiredSlots,
                DirtySlots = _dirtySlots,
                ActiveCells = _activeCells,
                ActivePlacements = _activePlacements,
                NextFreeSlotByBucket = _nextFreeSlotByBucket,
                SlotCommands = _commands,
                DirtySlotIndices = _dirtySlotIndices,
                Result = _assignmentResult
            }.Schedule().Complete();
            OperationMapRenderAssignmentResult assignment =
                _assignmentResult.Value;
            Assert.That(
                assignment.Failure,
                Is.EqualTo(OperationMapRenderAssignmentFailure.None),
                $"Assignment failed for {sampleIdentity}/{visualState}.");
            Assert.That(
                assignment.OverflowCount,
                Is.Zero,
                $"Overflow for {sampleIdentity}/{visualState}.");
            Assert.That(
                assignment.RetainedCount + assignment.AssignedCount,
                Is.EqualTo(_selectedLogicalRows.Length),
                $"Required-row assignment mismatch for {sampleIdentity}/{visualState}.");
            int expectedActiveSlotCount =
                _activeSlotCount + assignment.AssignedCount - assignment.ReleasedCount;
            Assert.That(
                expectedActiveSlotCount,
                Is.EqualTo(_selectedLogicalRows.Length),
                $"Active-slot accounting mismatch for {sampleIdentity}/{visualState}.");

            for (int index = 0; index < _dirtySlotIndices.Length; index++)
            {
                _slotFailures[_dirtySlotIndices[index]] =
                    OperationMapRenderSlotApplyFailure.None;
            }
            var apply = new OperationMapRenderSlotApplyJob
            {
                Database = _database,
                DatabaseLookup = _lookupSystem.GetDatabaseLookup(),
                DatabaseOwner = Entity.Null,
                SlotCommands = _commands,
                ProxySlotType = _entityManager.GetComponentTypeHandle<
                    OperationMapRenderProxySlotComponent>(false),
                LocalToWorldType =
                    _entityManager.GetComponentTypeHandle<LocalToWorld>(false),
                RenderBoundsType =
                    _entityManager.GetComponentTypeHandle<RenderBounds>(false),
                MaterialMeshInfoType =
                    _entityManager.GetComponentTypeHandle<MaterialMeshInfo>(false),
                BaseColorType = _entityManager.GetComponentTypeHandle<
                    URPMaterialPropertyBaseColor>(false),
                SlotFailures = _slotFailures
            };
            apply.ScheduleParallel(_slotQuery, default).Complete();

            ref OperationMapRenderDatabaseBlob blob = ref _database.Value;
            for (int index = 0; index < _selectedLogicalRows.Length; index++)
            {
                OperationMapRenderLogicalRowKey row = _selectedLogicalRows[index];
                int logicalRow = LogicalRowIndex(row, ref blob);
                if (_logicalRowToSlot[logicalRow] < 0)
                {
                    throw new AssertionException(
                        $"Required logical row is unbound for " +
                        $"{sampleIdentity}/{visualState} row={logicalRow}.");
                }
            }

            for (int index = 0; index < _dirtySlotIndices.Length; index++)
            {
                int slotIndex = _dirtySlotIndices[index];
                if (_slotFailures[slotIndex] !=
                    OperationMapRenderSlotApplyFailure.None)
                {
                    throw new AssertionException(
                        $"Slot apply failed for {sampleIdentity}/{visualState} " +
                        $"slot={slotIndex} failure={_slotFailures[slotIndex]}.");
                }
                OperationMapRenderSlotCommandComponent command =
                    _commands[slotIndex];
                if (command.SlotIndex != slotIndex ||
                    command.AssignmentGeneration != _generation)
                {
                    throw new AssertionException(
                        $"Dirty command identity mismatch for " +
                        $"{sampleIdentity}/{visualState} slot={slotIndex}.");
                }
                bool expectedEnabled = command.Assigned == 1;
                bool enabled = _entityManager.IsComponentEnabled<MaterialMeshInfo>(
                    _slotEntities[slotIndex]);
                if (enabled != expectedEnabled)
                {
                    throw new AssertionException(
                        $"Enabled-state mismatch for {sampleIdentity}/{visualState} " +
                        $"slot={slotIndex}.");
                }
                if (expectedEnabled)
                {
                    if (command.LogicalRowIndex != _slotToLogicalRow[slotIndex] ||
                        _logicalRowToSlot[command.LogicalRowIndex] != slotIndex)
                    {
                        throw new AssertionException(
                            $"Reciprocal binding mismatch for " +
                            $"{sampleIdentity}/{visualState} slot={slotIndex}.");
                    }
                    OperationMapRenderPlacementBlob placement =
                        blob.Placements[command.PlacementIndex];
                    OperationMapRenderPrototypePartBlob part =
                        blob.Parts[command.PartIndex];
                    AssertSlotParity(
                        slotIndex,
                        command,
                        placement,
                        part,
                        sampleIdentity,
                        visualState);
                    continue;
                }

                AssertReleasedSlotParity(
                    slotIndex,
                    command,
                    sampleIdentity,
                    visualState);
            }

            _activeSlotCount = expectedActiveSlotCount;
            fingerprint
                .Append(sampleIdentity).Append('|')
                .Append((byte)visualState).Append('|')
                .Append(_selectedCells.Length).Append('|')
                .Append(_selectedPlacements.Length).Append('|')
                .Append(_selectedLogicalRows.Length).Append('|')
                .Append(assignment.RetainedCount).Append('|')
                .Append(assignment.ReleasedCount).Append('|')
                .Append(assignment.AssignedCount).Append('|')
                .Append(_dirtySlotIndices.Length).Append('\n');

            return new MaterializedParitySampleResult
            {
                enabledSlotCount = expectedActiveSlotCount,
                retainedSlotCount = assignment.RetainedCount,
                releasedSlotCount = assignment.ReleasedCount,
                reboundSlotCount = assignment.AssignedCount,
                dirtySlotCount = _dirtySlotIndices.Length
            };
        }

        private int LogicalRowIndex(
            OperationMapRenderLogicalRowKey row,
            ref OperationMapRenderDatabaseBlob blob)
        {
            OperationMapRenderPlacementBlob placement =
                blob.Placements[row.PlacementIndex];
            OperationMapRenderPrototypeBlob prototype =
                blob.Prototypes[placement.PrototypeIndex];
            return _placementFirstLogicalRow[row.PlacementIndex] +
                   row.PartIndex - prototype.FirstPart;
        }

        private void AssertSlotParity(
            int slotIndex,
            OperationMapRenderSlotCommandComponent command,
            OperationMapRenderPlacementBlob placement,
            OperationMapRenderPrototypePartBlob part,
            string sampleIdentity,
            OperationMapRenderVisualState visualState)
        {
            Entity entity = _slotEntities[slotIndex];
            OperationMapRenderProxySlotComponent binding =
                _entityManager.GetComponentData<
                    OperationMapRenderProxySlotComponent>(entity);
            float4x4 expectedMatrix = math.mul(
                placement.WorldMatrix,
                part.LocalToPlacement);
            float4x4 actualMatrix = _entityManager
                .GetComponentData<LocalToWorld>(entity).Value;
            RenderBounds bounds =
                _entityManager.GetComponentData<RenderBounds>(entity);
            MaterialMeshInfo materialMesh =
                _entityManager.GetComponentData<MaterialMeshInfo>(entity);
            float4 color = _entityManager.GetComponentData<
                URPMaterialPropertyBaseColor>(entity).Value;
            if (binding.SlotIndex != slotIndex ||
                binding.PoolBucketIndex != command.PoolBucketIndex ||
                binding.PlacementIndex != command.PlacementIndex ||
                binding.PartIndex != command.PartIndex ||
                binding.AssignmentGeneration != _generation ||
                !EqualBits(actualMatrix, expectedMatrix) ||
                !EqualBits(bounds.Value.Center, part.LocalBounds.Center) ||
                !EqualBits(bounds.Value.Extents, part.LocalBounds.Extents) ||
                MaterialMeshInfo.StaticIndexToArrayIndex(materialMesh.Material) !=
                    part.MaterialArrayIndex ||
                MaterialMeshInfo.StaticIndexToArrayIndex(materialMesh.Mesh) !=
                    part.MeshArrayIndex ||
                materialMesh.SubMesh != part.SubMeshIndex ||
                !EqualBits(color, part.LinearBaseColor))
            {
                throw new AssertionException(
                    $"Materialized field mismatch for " +
                    $"{sampleIdentity}/{visualState} slot={slotIndex} " +
                    $"logicalRow={command.LogicalRowIndex}.");
            }
        }

        private void AssertReleasedSlotParity(
            int slotIndex,
            OperationMapRenderSlotCommandComponent command,
            string sampleIdentity,
            OperationMapRenderVisualState visualState)
        {
            Entity entity = _slotEntities[slotIndex];
            OperationMapRenderProxySlotComponent binding =
                _entityManager.GetComponentData<
                    OperationMapRenderProxySlotComponent>(entity);
            RenderBounds bounds =
                _entityManager.GetComponentData<RenderBounds>(entity);
            if (command.Assigned != 0 ||
                command.LogicalRowIndex != -1 ||
                command.PlacementIndex != -1 ||
                command.PartIndex != -1 ||
                command.PoolBucketIndex != -1 ||
                _slotToLogicalRow[slotIndex] != -1 ||
                binding.PlacementIndex != -1 ||
                binding.PartIndex != -1 ||
                binding.AssignmentGeneration != _generation ||
                !EqualBits(
                    _entityManager.GetComponentData<LocalToWorld>(entity).Value,
                    float4x4.identity) ||
                !EqualBits(bounds.Value.Center, float3.zero) ||
                !EqualBits(bounds.Value.Extents, float3.zero) ||
                !EqualBits(
                    _entityManager.GetComponentData<
                        URPMaterialPropertyBaseColor>(entity).Value,
                    new float4(1f)))
            {
                throw new AssertionException(
                    $"Released field mismatch for " +
                    $"{sampleIdentity}/{visualState} slot={slotIndex}.");
            }
        }

        public void Dispose()
        {
            _entityManager.CompleteAllTrackedJobs();
            _slotFailures.Dispose();
            _assignmentResult.Dispose();
            _dirtySlotIndices.Dispose();
            _commands.Dispose();
            _nextFreeSlotByBucket.Dispose();
            _activePlacements.Dispose();
            _activeCells.Dispose();
            _dirtySlots.Dispose();
            _requiredSlots.Dispose();
            _slotAssignmentGenerations.Dispose();
            _logicalRowToSlot.Dispose();
            _slotToLogicalRow.Dispose();
            _placementFirstLogicalRow.Dispose();
            _selectionFailure.Dispose();
            _visitedPlacements.Dispose();
            _selectedLogicalRows.Dispose();
            _selectedPlacements.Dispose();
            _selectedCells.Dispose();
            _canonicalStates.Dispose();
            _slotEntities.Dispose();
            _world.Dispose();
        }

        private static NativeArray<int> Filled(int length, int value)
        {
            var array = new NativeArray<int>(length, Allocator.Persistent);
            for (int index = 0; index < length; index++)
                array[index] = value;
            return array;
        }

        private static NativeBitArray Bits(int length) =>
            new(
                length,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
    }

    private static bool EqualBits(float3 left, float3 right) =>
        math.all(math.asint(left) == math.asint(right));

    private static bool EqualBits(float4 left, float4 right) =>
        math.all(math.asint(left) == math.asint(right));

    private static bool EqualBits(float4x4 left, float4x4 right) =>
        EqualBits(left.c0, right.c0) &&
        EqualBits(left.c1, right.c1) &&
        EqualBits(left.c2, right.c2) &&
        EqualBits(left.c3, right.c3);

    private sealed class CanonicalEnvelopeSample
    {
        public string identity;
        public OperationMapRenderCellEnvelope envelope;
    }

    [Serializable]
    private sealed class MaterializedParityReport
    {
        public string schema;
        public int schemaVersion;
        public string operationMapId;
        public string databaseContentHash;
        public string capacityContractPath;
        public string result;
        public int canonicalEnvelopeCount;
        public int stateVariantCount;
        public int sampleCount;
        public int placementCount;
        public int logicalRowCount;
        public int fixedSlotCount;
        public int maximumEnabledSlotCount;
        public string materializedFingerprint;
        public MaterializedParityStateRow[] states;
        public int productionCutover;
    }

    [Serializable]
    private sealed class MaterializedParityStateRow
    {
        public string visualState;
        public int sampleCount;
        public int minimumEnabledSlotCount;
        public int maximumEnabledSlotCount;
        public int peakEnabledSlotCount;
        public string peakSampleIdentity;
        public long totalRetainedSlotCount;
        public long totalReleasedSlotCount;
        public long totalReboundSlotCount;
        public long totalDirtySlotCount;
    }

    private struct MaterializedParitySampleResult
    {
        public int enabledSlotCount;
        public int retainedSlotCount;
        public int releasedSlotCount;
        public int reboundSlotCount;
        public int dirtySlotCount;
    }

    private sealed partial class MaterializedParityLookupSystem : SystemBase
    {
        protected override void OnUpdate()
        {
        }

        internal ComponentLookup<OperationMapRenderDatabaseComponent>
            GetDatabaseLookup() =>
            GetComponentLookup<OperationMapRenderDatabaseComponent>(true);
    }
}
