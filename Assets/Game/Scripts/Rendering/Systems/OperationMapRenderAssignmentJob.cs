using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Rendering
{
    internal enum OperationMapRenderAssignmentFailure : byte
    {
        None = 0,
        InvalidDatabase = 1,
        InvalidCapacity = 2,
        InvalidSelection = 3,
        InvalidBinding = 4
    }

    internal struct OperationMapRenderAssignmentResult
    {
        internal OperationMapRenderAssignmentFailure Failure;
        internal int RetainedCount;
        internal int ReleasedCount;
        internal int AssignedCount;
        internal int OverflowCount;
    }

    [BurstCompile]
    internal struct OperationMapRenderAssignmentJob : IJob
    {
        [ReadOnly] internal BlobAssetReference<OperationMapRenderDatabaseBlob> Database;
        [ReadOnly] internal NativeList<int> SelectedCellIndices;
        [ReadOnly] internal NativeList<OperationMapRenderLogicalRowKey> RequiredRows;
        [ReadOnly] internal NativeArray<int> PlacementFirstLogicalRow;
        [ReadOnly] internal int AssignmentGeneration;
        [ReadOnly] internal int MaxDirtySlotCount;

        internal NativeArray<int> SlotToLogicalRow;
        internal NativeArray<int> LogicalRowToSlot;
        internal NativeArray<int> SlotAssignmentGenerations;
        internal NativeBitArray RequiredSlots;
        internal NativeBitArray DirtySlots;
        internal NativeBitArray ActiveCells;
        internal NativeBitArray ActivePlacements;
        internal NativeArray<int> NextFreeSlotByBucket;
        internal NativeArray<OperationMapRenderSlotCommandComponent> SlotCommands;
        internal NativeList<int> DirtySlotIndices;
        internal NativeReference<OperationMapRenderAssignmentResult> Result;

        [BurstCompile]
        public void Execute()
        {
            DirtySlotIndices.Clear();
            var result = new OperationMapRenderAssignmentResult();
            if (!Validate(ref result))
            {
                Result.Value = result;
                return;
            }

            ClearBits(RequiredSlots);
            ClearBits(DirtySlots);
            ClearBits(ActiveCells);
            ClearBits(ActivePlacements);

            for (int index = 0; index < SelectedCellIndices.Length; index++)
                ActiveCells.Set(SelectedCellIndices[index], true);

            for (int index = 0; index < RequiredRows.Length; index++)
            {
                OperationMapRenderLogicalRowKey row = RequiredRows[index];
                ActivePlacements.Set(row.PlacementIndex, true);
                int logicalRow = GetLogicalRow(row);
                int slotIndex = LogicalRowToSlot[logicalRow];
                if (slotIndex < 0)
                    continue;
                RequiredSlots.Set(slotIndex, true);
                result.RetainedCount++;
            }

            for (int slotIndex = 0;
                 slotIndex < SlotToLogicalRow.Length;
                 slotIndex++)
            {
                int logicalRow = SlotToLogicalRow[slotIndex];
                if (logicalRow < 0 || RequiredSlots.IsSet(slotIndex))
                    continue;
                LogicalRowToSlot[logicalRow] = -1;
                SlotToLogicalRow[slotIndex] = -1;
                SlotAssignmentGenerations[slotIndex] = AssignmentGeneration;
                WriteReleasedCommand(slotIndex);
                MarkDirty(slotIndex);
                result.ReleasedCount++;
            }

            ref OperationMapRenderDatabaseBlob blob = ref Database.Value;
            for (int bucketIndex = 0;
                 bucketIndex < blob.PoolBuckets.Length;
                 bucketIndex++)
            {
                NextFreeSlotByBucket[bucketIndex] =
                    blob.PoolBuckets[bucketIndex].FirstSlot;
            }

            for (int index = 0; index < RequiredRows.Length; index++)
            {
                OperationMapRenderLogicalRowKey row = RequiredRows[index];
                int logicalRow = GetLogicalRow(row);
                if (LogicalRowToSlot[logicalRow] >= 0)
                    continue;

                OperationMapRenderPoolBucketBlob bucket =
                    blob.PoolBuckets[row.PoolBucketIndex];
                int slotEnd = bucket.FirstSlot + bucket.Capacity;
                int slotIndex = NextFreeSlotByBucket[row.PoolBucketIndex];
                while (slotIndex < slotEnd &&
                       SlotToLogicalRow[slotIndex] >= 0)
                {
                    slotIndex++;
                }
                NextFreeSlotByBucket[row.PoolBucketIndex] = slotIndex + 1;
                if (slotIndex >= slotEnd)
                {
                    result.OverflowCount++;
                    continue;
                }

                SlotToLogicalRow[slotIndex] = logicalRow;
                LogicalRowToSlot[logicalRow] = slotIndex;
                SlotAssignmentGenerations[slotIndex] = AssignmentGeneration;
                SlotCommands[slotIndex] = new OperationMapRenderSlotCommandComponent
                {
                    SlotIndex = slotIndex,
                    LogicalRowIndex = logicalRow,
                    PlacementIndex = row.PlacementIndex,
                    PartIndex = row.PartIndex,
                    PoolBucketIndex = row.PoolBucketIndex,
                    AssignmentGeneration = AssignmentGeneration,
                    Assigned = 1
                };
                MarkDirty(slotIndex);
                result.AssignedCount++;
            }

            Result.Value = result;
        }

        private bool Validate(ref OperationMapRenderAssignmentResult result)
        {
            if (!Database.IsCreated || AssignmentGeneration <= 0)
                return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidDatabase);

            ref OperationMapRenderDatabaseBlob blob = ref Database.Value;
            int slotCount = 0;
            for (int bucketIndex = 0;
                 bucketIndex < blob.PoolBuckets.Length;
                 bucketIndex++)
            {
                OperationMapRenderPoolBucketBlob bucket =
                    blob.PoolBuckets[bucketIndex];
                if (bucket.FirstSlot != slotCount || bucket.Capacity <= 0)
                    return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidDatabase);
                slotCount += bucket.Capacity;
            }

            if (slotCount <= 0 ||
                SlotToLogicalRow.Length != slotCount ||
                SlotAssignmentGenerations.Length != slotCount ||
                SlotCommands.Length != slotCount ||
                RequiredSlots.Length < slotCount ||
                DirtySlots.Length < slotCount ||
                ActiveCells.Length < blob.Cells.Length ||
                ActivePlacements.Length < blob.Placements.Length ||
                NextFreeSlotByBucket.Length != blob.PoolBuckets.Length ||
                PlacementFirstLogicalRow.Length != blob.Placements.Length + 1 ||
                MaxDirtySlotCount < slotCount ||
                MaxDirtySlotCount > DirtySlotIndices.Capacity)
            {
                return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidCapacity);
            }

            int expectedLogicalRow = 0;
            for (int placementIndex = 0;
                 placementIndex < blob.Placements.Length;
                 placementIndex++)
            {
                OperationMapRenderPlacementBlob placement =
                    blob.Placements[placementIndex];
                if (placement.PrototypeIndex < 0 ||
                    placement.PrototypeIndex >= blob.Prototypes.Length ||
                    PlacementFirstLogicalRow[placementIndex] != expectedLogicalRow)
                {
                    return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidDatabase);
                }
                OperationMapRenderPrototypeBlob prototype =
                    blob.Prototypes[placement.PrototypeIndex];
                if (prototype.FirstPart < 0 || prototype.PartCount <= 0 ||
                    prototype.FirstPart > blob.Parts.Length - prototype.PartCount)
                {
                    return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidDatabase);
                }
                expectedLogicalRow += prototype.PartCount;
            }
            if (PlacementFirstLogicalRow[blob.Placements.Length] != expectedLogicalRow ||
                LogicalRowToSlot.Length != expectedLogicalRow)
            {
                return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidCapacity);
            }

            int previousCell = -1;
            for (int index = 0; index < SelectedCellIndices.Length; index++)
            {
                int cellIndex = SelectedCellIndices[index];
                if (cellIndex <= previousCell || cellIndex >= blob.Cells.Length)
                    return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidSelection);
                previousCell = cellIndex;
            }

            int previousLogicalRow = -1;
            for (int index = 0; index < RequiredRows.Length; index++)
            {
                OperationMapRenderLogicalRowKey row = RequiredRows[index];
                if (!TryValidateRow(row, out int logicalRow) ||
                    logicalRow <= previousLogicalRow)
                {
                    return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidSelection);
                }
                previousLogicalRow = logicalRow;
            }

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                int logicalRow = SlotToLogicalRow[slotIndex];
                if (logicalRow < -1 || logicalRow >= expectedLogicalRow ||
                    (logicalRow >= 0 && LogicalRowToSlot[logicalRow] != slotIndex))
                {
                    return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidBinding);
                }
                if (logicalRow >= 0 &&
                    !SlotMatchesLogicalRowBucket(slotIndex, logicalRow))
                {
                    return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidBinding);
                }
            }
            for (int logicalRow = 0;
                 logicalRow < expectedLogicalRow;
                 logicalRow++)
            {
                int slotIndex = LogicalRowToSlot[logicalRow];
                if (slotIndex < -1 || slotIndex >= slotCount ||
                    (slotIndex >= 0 && SlotToLogicalRow[slotIndex] != logicalRow))
                {
                    return Fail(ref result, OperationMapRenderAssignmentFailure.InvalidBinding);
                }
            }
            return true;
        }

        private bool TryValidateRow(
            OperationMapRenderLogicalRowKey row,
            out int logicalRow)
        {
            logicalRow = -1;
            ref OperationMapRenderDatabaseBlob blob = ref Database.Value;
            if (row.PlacementIndex < 0 ||
                row.PlacementIndex >= blob.Placements.Length)
                return false;
            OperationMapRenderPlacementBlob placement =
                blob.Placements[row.PlacementIndex];
            OperationMapRenderPrototypeBlob prototype =
                blob.Prototypes[placement.PrototypeIndex];
            if (row.PartIndex < prototype.FirstPart ||
                row.PartIndex >= prototype.FirstPart + prototype.PartCount)
                return false;
            OperationMapRenderPrototypePartBlob part = blob.Parts[row.PartIndex];
            if (row.PoolBucketIndex != part.PoolBucketIndex ||
                row.PoolBucketIndex < 0 ||
                row.PoolBucketIndex >= blob.PoolBuckets.Length)
                return false;
            logicalRow = GetLogicalRow(row);
            return true;
        }

        private bool SlotMatchesLogicalRowBucket(int slotIndex, int logicalRow)
        {
            ref OperationMapRenderDatabaseBlob blob = ref Database.Value;
            int placementIndex = 0;
            while (PlacementFirstLogicalRow[placementIndex + 1] <= logicalRow)
                placementIndex++;
            OperationMapRenderPlacementBlob placement =
                blob.Placements[placementIndex];
            OperationMapRenderPrototypeBlob prototype =
                blob.Prototypes[placement.PrototypeIndex];
            int partIndex = prototype.FirstPart +
                logicalRow - PlacementFirstLogicalRow[placementIndex];
            int bucketIndex = blob.Parts[partIndex].PoolBucketIndex;
            OperationMapRenderPoolBucketBlob bucket =
                blob.PoolBuckets[bucketIndex];
            return slotIndex >= bucket.FirstSlot &&
                   slotIndex < bucket.FirstSlot + bucket.Capacity;
        }

        private int GetLogicalRow(OperationMapRenderLogicalRowKey row)
        {
            ref OperationMapRenderDatabaseBlob blob = ref Database.Value;
            OperationMapRenderPlacementBlob placement =
                blob.Placements[row.PlacementIndex];
            OperationMapRenderPrototypeBlob prototype =
                blob.Prototypes[placement.PrototypeIndex];
            return PlacementFirstLogicalRow[row.PlacementIndex] +
                   row.PartIndex - prototype.FirstPart;
        }

        private void WriteReleasedCommand(int slotIndex)
        {
            SlotCommands[slotIndex] = new OperationMapRenderSlotCommandComponent
            {
                SlotIndex = slotIndex,
                LogicalRowIndex = -1,
                PlacementIndex = -1,
                PartIndex = -1,
                PoolBucketIndex = -1,
                AssignmentGeneration = AssignmentGeneration,
                Assigned = 0
            };
        }

        private void MarkDirty(int slotIndex)
        {
            if (DirtySlots.IsSet(slotIndex))
                return;
            DirtySlots.Set(slotIndex, true);
            DirtySlotIndices.AddNoResize(slotIndex);
        }

        private static bool Fail(
            ref OperationMapRenderAssignmentResult result,
            OperationMapRenderAssignmentFailure failure)
        {
            result.Failure = failure;
            return false;
        }

        private static void ClearBits(NativeBitArray bits)
        {
            if (bits.Length > 0)
                bits.SetBits(0, false, bits.Length);
        }
    }
}
