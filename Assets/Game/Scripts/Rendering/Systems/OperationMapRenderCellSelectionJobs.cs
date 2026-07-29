using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Rendering
{
    internal enum OperationMapRenderCellSelectionFailure : byte
    {
        None = 0,
        InvalidEnvelope = 1,
        InvalidDatabase = 2,
        CellCapacityExceeded = 3,
        InvalidCellRange = 4,
        InvalidPlacement = 5,
        InvalidVisualState = 6,
        PlacementCapacityExceeded = 7,
        InvalidPrototype = 8,
        LogicalRowCapacityExceeded = 9
    }

    internal struct OperationMapRenderLogicalRowKey
    {
        internal int PlacementIndex;
        internal int PartIndex;
        internal int PoolBucketIndex;
    }

    [BurstCompile]
    internal struct OperationMapRenderRequiredCellSelectionJob : IJob
    {
        [ReadOnly] internal BlobAssetReference<OperationMapRenderDatabaseBlob> Database;
        [ReadOnly] internal OperationMapRenderCellEnvelope RequiredEnvelope;
        [ReadOnly] internal int MaxSelectedCellCount;
        internal NativeList<int> SelectedCellIndices;
        internal NativeReference<OperationMapRenderCellSelectionFailure> Failure;

        [BurstCompile]
        public void Execute()
        {
            using var profilerScope =
                OperationMapRenderVirtualizationProfilerMarkers
                    .SelectCells.Auto();
            SelectedCellIndices.Clear();
            Failure.Value = OperationMapRenderCellSelectionFailure.None;
            if (!Database.IsCreated)
            {
                Failure.Value = OperationMapRenderCellSelectionFailure.InvalidDatabase;
                return;
            }
            if (RequiredEnvelope.Min.x > RequiredEnvelope.Max.x ||
                RequiredEnvelope.Min.y > RequiredEnvelope.Max.y)
            {
                Failure.Value = OperationMapRenderCellSelectionFailure.InvalidEnvelope;
                return;
            }

            ref OperationMapRenderDatabaseBlob blob = ref Database.Value;
            if (blob.Cells.Length <= 0 ||
                MaxSelectedCellCount < 0 ||
                MaxSelectedCellCount > SelectedCellIndices.Capacity)
            {
                Failure.Value = OperationMapRenderCellSelectionFailure.InvalidDatabase;
                return;
            }

            for (int cellIndex = 0; cellIndex < blob.Cells.Length; cellIndex++)
            {
                int2 coordinate = blob.Cells[cellIndex].Coordinate;
                if (coordinate.x < RequiredEnvelope.Min.x ||
                    coordinate.y < RequiredEnvelope.Min.y ||
                    coordinate.x > RequiredEnvelope.Max.x ||
                    coordinate.y > RequiredEnvelope.Max.y)
                {
                    continue;
                }
                if (SelectedCellIndices.Length >= MaxSelectedCellCount)
                {
                    SelectedCellIndices.Clear();
                    Failure.Value =
                        OperationMapRenderCellSelectionFailure.CellCapacityExceeded;
                    return;
                }
                SelectedCellIndices.AddNoResize(cellIndex);
            }
        }
    }

    [BurstCompile]
    internal struct OperationMapRenderPlacementGatherJob : IJob
    {
        [ReadOnly] internal BlobAssetReference<OperationMapRenderDatabaseBlob> Database;
        [ReadOnly] internal NativeList<int> SelectedCellIndices;
        [ReadOnly] internal NativeArray<OperationMapRenderVisualState> CanonicalVisualStates;
        [ReadOnly] internal int MaxSelectedPlacementCount;
        [ReadOnly] internal int MaxSelectedLogicalRowCount;
        internal NativeBitArray VisitedPlacements;
        internal NativeList<int> SelectedPlacementIndices;
        internal NativeList<OperationMapRenderLogicalRowKey> SelectedLogicalRows;
        internal NativeReference<OperationMapRenderCellSelectionFailure> Failure;

        [BurstCompile]
        public void Execute()
        {
            using var profilerScope =
                OperationMapRenderVirtualizationProfilerMarkers
                    .SelectCells.Auto();
            SelectedPlacementIndices.Clear();
            SelectedLogicalRows.Clear();
            if (Failure.Value != OperationMapRenderCellSelectionFailure.None)
                return;
            if (!Database.IsCreated)
            {
                Fail(OperationMapRenderCellSelectionFailure.InvalidDatabase);
                return;
            }

            ref OperationMapRenderDatabaseBlob blob = ref Database.Value;
            if (VisitedPlacements.Length < blob.Placements.Length ||
                MaxSelectedPlacementCount < 0 ||
                MaxSelectedPlacementCount > SelectedPlacementIndices.Capacity ||
                MaxSelectedLogicalRowCount < 0 ||
                MaxSelectedLogicalRowCount > SelectedLogicalRows.Capacity)
            {
                Fail(OperationMapRenderCellSelectionFailure.InvalidDatabase);
                return;
            }
            if (VisitedPlacements.Length > 0)
                VisitedPlacements.SetBits(0, false, VisitedPlacements.Length);

            for (int selectedCellIndex = 0;
                 selectedCellIndex < SelectedCellIndices.Length;
                 selectedCellIndex++)
            {
                int cellIndex = SelectedCellIndices[selectedCellIndex];
                if (cellIndex < 0 || cellIndex >= blob.Cells.Length)
                {
                    Fail(OperationMapRenderCellSelectionFailure.InvalidCellRange);
                    return;
                }

                OperationMapRenderCellBlob cell = blob.Cells[cellIndex];
                if (cell.FirstPlacementIndex < 0 ||
                    cell.PlacementIndexCount < 0 ||
                    cell.FirstPlacementIndex >
                        blob.CellPlacementIndices.Length -
                        cell.PlacementIndexCount)
                {
                    Fail(OperationMapRenderCellSelectionFailure.InvalidCellRange);
                    return;
                }

                int end = cell.FirstPlacementIndex + cell.PlacementIndexCount;
                for (int entryIndex = cell.FirstPlacementIndex;
                     entryIndex < end;
                     entryIndex++)
                {
                    int placementIndex = blob.CellPlacementIndices[entryIndex];
                    if (placementIndex < 0 ||
                        placementIndex >= blob.Placements.Length)
                    {
                        Fail(OperationMapRenderCellSelectionFailure.InvalidPlacement);
                        return;
                    }
                    if (VisitedPlacements.IsSet(placementIndex))
                        continue;
                    VisitedPlacements.Set(placementIndex, true);

                    OperationMapRenderPlacementBlob placement =
                        blob.Placements[placementIndex];
                    if (!TryIncludeForVisualState(placement, out bool include))
                    {
                        Fail(OperationMapRenderCellSelectionFailure.InvalidVisualState);
                        return;
                    }
                    if (!include)
                        continue;
                    if (SelectedPlacementIndices.Length >=
                        MaxSelectedPlacementCount)
                    {
                        Fail(
                            OperationMapRenderCellSelectionFailure
                                .PlacementCapacityExceeded);
                        return;
                    }
                    SelectedPlacementIndices.AddNoResize(placementIndex);
                }
            }

            SelectedPlacementIndices.AsArray().Sort();
            for (int selectedIndex = 0;
                 selectedIndex < SelectedPlacementIndices.Length;
                 selectedIndex++)
            {
                int placementIndex = SelectedPlacementIndices[selectedIndex];
                OperationMapRenderPlacementBlob placement =
                    blob.Placements[placementIndex];
                if (placement.PrototypeIndex < 0 ||
                    placement.PrototypeIndex >= blob.Prototypes.Length)
                {
                    Fail(OperationMapRenderCellSelectionFailure.InvalidPrototype);
                    return;
                }

                OperationMapRenderPrototypeBlob prototype =
                    blob.Prototypes[placement.PrototypeIndex];
                if (prototype.FirstPart < 0 ||
                    prototype.PartCount <= 0 ||
                    prototype.FirstPart > blob.Parts.Length - prototype.PartCount)
                {
                    Fail(OperationMapRenderCellSelectionFailure.InvalidPrototype);
                    return;
                }
                if (MaxSelectedLogicalRowCount - SelectedLogicalRows.Length <
                    prototype.PartCount)
                {
                    Fail(
                        OperationMapRenderCellSelectionFailure
                            .LogicalRowCapacityExceeded);
                    return;
                }

                int partEnd = prototype.FirstPart + prototype.PartCount;
                for (int partIndex = prototype.FirstPart;
                     partIndex < partEnd;
                     partIndex++)
                {
                    OperationMapRenderPrototypePartBlob part =
                        blob.Parts[partIndex];
                    if (part.PoolBucketIndex < 0 ||
                        part.PoolBucketIndex >= blob.PoolBuckets.Length)
                    {
                        Fail(OperationMapRenderCellSelectionFailure.InvalidPrototype);
                        return;
                    }
                    SelectedLogicalRows.AddNoResize(
                        new OperationMapRenderLogicalRowKey
                        {
                            PlacementIndex = placementIndex,
                            PartIndex = partIndex,
                            PoolBucketIndex = part.PoolBucketIndex
                        });
                }
            }
        }

        private bool TryIncludeForVisualState(
            in OperationMapRenderPlacementBlob placement,
            out bool include)
        {
            include = false;
            if (placement.StateOwnerIndex == -1)
            {
                if (placement.RequiredVisualState !=
                    OperationMapRenderVisualState.Any)
                {
                    return false;
                }
                include = true;
                return true;
            }
            if (placement.StateOwnerIndex < 0 ||
                placement.StateOwnerIndex >= CanonicalVisualStates.Length ||
                placement.RequiredVisualState ==
                    OperationMapRenderVisualState.Any)
            {
                return false;
            }

            OperationMapRenderVisualState canonical =
                CanonicalVisualStates[placement.StateOwnerIndex];
            if (canonical != OperationMapRenderVisualState.Intact &&
                canonical != OperationMapRenderVisualState.Destroyed)
            {
                return false;
            }
            include = canonical == placement.RequiredVisualState;
            return true;
        }

        private void Fail(OperationMapRenderCellSelectionFailure failure)
        {
            SelectedPlacementIndices.Clear();
            SelectedLogicalRows.Clear();
            Failure.Value = failure;
        }
    }
}
