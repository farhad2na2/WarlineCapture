using Game.Components;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public sealed class OperationMapRenderAssignmentJobTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(nameof(AssignsLowestMatchingSlotsDeterministically),
                AssignsLowestMatchingSlotsDeterministically);
            RunCase(nameof(RetainsAndReleasesWithoutTouchingUnchangedSlots),
                RetainsAndReleasesWithoutTouchingUnchangedSlots);
            RunCase(nameof(ReleasedSlotIsReusedWithOneFinalCommand),
                ReleasedSlotIsReusedWithOneFinalCommand);
            RunCase(nameof(OverflowIsBoundedAndReported),
                OverflowIsBoundedAndReported);
            RunCase(nameof(UnsortedSelectionFailsBeforeMutation),
                UnsortedSelectionFailsBeforeMutation);
            RunCase(nameof(CorruptReciprocalBindingFailsBeforeMutation),
                CorruptReciprocalBindingFailsBeforeMutation);
            Debug.Log(
                "[OperationMapRenderAssignmentValidation] result=Passed tests=6");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[OperationMapRenderAssignmentValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(string name, System.Action action)
    {
        try
        {
            action();
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "[OperationMapRenderAssignmentValidation] " +
                $"result=Failed test={name} error={exception}");
            throw;
        }
    }

    [Test]
    public static void AssignsLowestMatchingSlotsDeterministically()
    {
        using var fixture = new AssignmentFixture();
        fixture.SetRows(Row(0, 0, 0), Row(1, 1, 0), Row(1, 2, 1));
        fixture.Run(7);

        Assert.That(fixture.Result.Value.Failure, Is.EqualTo(
            OperationMapRenderAssignmentFailure.None));
        Assert.That(fixture.Result.Value.AssignedCount, Is.EqualTo(3));
        Assert.That(fixture.SlotToRow.ToArray(), Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(fixture.RowToSlot.ToArray(), Is.EqualTo(new[] { 0, 1, 2, -1 }));
        Assert.That(fixture.Dirty.AsArray().ToArray(),
            Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(fixture.Commands[2].PoolBucketIndex, Is.EqualTo(1));
        Assert.That(fixture.ActiveCells.IsSet(0), Is.True);
        Assert.That(fixture.ActivePlacements.IsSet(1), Is.True);
    }

    [Test]
    public static void RetainsAndReleasesWithoutTouchingUnchangedSlots()
    {
        using var fixture = new AssignmentFixture();
        fixture.SetRows(Row(0, 0, 0), Row(1, 1, 0), Row(1, 2, 1));
        fixture.Run(7);
        fixture.SetRows(Row(0, 0, 0), Row(1, 2, 1));
        fixture.Run(8);

        Assert.That(fixture.Result.Value.RetainedCount, Is.EqualTo(2));
        Assert.That(fixture.Result.Value.ReleasedCount, Is.EqualTo(1));
        Assert.That(fixture.Result.Value.AssignedCount, Is.Zero);
        Assert.That(fixture.Dirty.AsArray().ToArray(), Is.EqualTo(new[] { 1 }));
        Assert.That(fixture.SlotToRow.ToArray(), Is.EqualTo(new[] { 0, -1, 2 }));
        Assert.That(fixture.Generations.ToArray(), Is.EqualTo(new[] { 7, 8, 7 }));
    }

    [Test]
    public static void ReleasedSlotIsReusedWithOneFinalCommand()
    {
        using var fixture = new AssignmentFixture();
        fixture.SetRows(Row(0, 0, 0), Row(1, 1, 0));
        fixture.Run(3);
        fixture.SetRows(Row(0, 0, 0), Row(2, 0, 0));
        fixture.Run(4);

        Assert.That(fixture.Result.Value.ReleasedCount, Is.EqualTo(1));
        Assert.That(fixture.Result.Value.AssignedCount, Is.EqualTo(1));
        Assert.That(fixture.Dirty.AsArray().ToArray(), Is.EqualTo(new[] { 1 }));
        Assert.That(fixture.Commands[1].Assigned, Is.EqualTo(1));
        Assert.That(fixture.Commands[1].PlacementIndex, Is.EqualTo(2));
        Assert.That(fixture.Commands[1].AssignmentGeneration, Is.EqualTo(4));
    }

    [Test]
    public static void OverflowIsBoundedAndReported()
    {
        using var fixture = new AssignmentFixture();
        fixture.SetRows(Row(0, 0, 0), Row(1, 1, 0), Row(2, 0, 0));
        fixture.Run(1);

        Assert.That(fixture.Result.Value.Failure, Is.EqualTo(
            OperationMapRenderAssignmentFailure.None));
        Assert.That(fixture.Result.Value.AssignedCount, Is.EqualTo(2));
        Assert.That(fixture.Result.Value.OverflowCount, Is.EqualTo(1));
        Assert.That(fixture.Dirty.Length, Is.EqualTo(2));
        Assert.That(fixture.RowToSlot[3], Is.EqualTo(-1));
    }

    [Test]
    public static void UnsortedSelectionFailsBeforeMutation()
    {
        using var fixture = new AssignmentFixture();
        fixture.SetRows(Row(1, 1, 0), Row(0, 0, 0));
        fixture.Run(1);

        Assert.That(fixture.Result.Value.Failure, Is.EqualTo(
            OperationMapRenderAssignmentFailure.InvalidSelection));
        Assert.That(fixture.Dirty.Length, Is.Zero);
        Assert.That(fixture.SlotToRow.ToArray(), Is.EqualTo(new[] { -1, -1, -1 }));
    }

    [Test]
    public static void CorruptReciprocalBindingFailsBeforeMutation()
    {
        using var fixture = new AssignmentFixture();
        fixture.SlotToRow[0] = 0;
        fixture.RowToSlot[0] = 1;
        fixture.SetRows(Row(0, 0, 0));
        fixture.Run(1);

        Assert.That(fixture.Result.Value.Failure, Is.EqualTo(
            OperationMapRenderAssignmentFailure.InvalidBinding));
        Assert.That(fixture.Dirty.Length, Is.Zero);
        Assert.That(fixture.SlotToRow[0], Is.EqualTo(0));
        Assert.That(fixture.RowToSlot[0], Is.EqualTo(1));
    }

    private static OperationMapRenderLogicalRowKey Row(
        int placement,
        int part,
        int bucket) =>
        new OperationMapRenderLogicalRowKey
        {
            PlacementIndex = placement,
            PartIndex = part,
            PoolBucketIndex = bucket
        };

    private sealed class AssignmentFixture : System.IDisposable
    {
        private readonly BlobAssetReference<OperationMapRenderDatabaseBlob> _database;
        internal NativeList<int> Cells;
        internal NativeList<OperationMapRenderLogicalRowKey> Rows;
        internal NativeArray<int> Prefix;
        internal NativeArray<int> SlotToRow;
        internal NativeArray<int> RowToSlot;
        internal NativeArray<int> Generations;
        internal NativeBitArray RequiredSlots;
        internal NativeBitArray DirtySlots;
        internal NativeBitArray ActiveCells;
        internal NativeBitArray ActivePlacements;
        internal NativeArray<int> NextFree;
        internal NativeArray<OperationMapRenderSlotCommandComponent> Commands;
        internal NativeList<int> Dirty;
        internal NativeReference<OperationMapRenderAssignmentResult> Result;

        internal AssignmentFixture()
        {
            _database = CreateDatabase();
            Cells = new NativeList<int>(1, Allocator.TempJob);
            Cells.AddNoResize(0);
            Rows = new NativeList<OperationMapRenderLogicalRowKey>(
                4, Allocator.TempJob);
            Prefix = new NativeArray<int>(
                new[] { 0, 1, 3, 4 }, Allocator.TempJob);
            SlotToRow = Filled(3, -1);
            RowToSlot = Filled(4, -1);
            Generations = Filled(3, 0);
            RequiredSlots = Bits(3);
            DirtySlots = Bits(3);
            ActiveCells = Bits(1);
            ActivePlacements = Bits(3);
            NextFree = Filled(2, 0);
            Commands = new NativeArray<OperationMapRenderSlotCommandComponent>(
                3, Allocator.TempJob);
            Dirty = new NativeList<int>(3, Allocator.TempJob);
            Result = new NativeReference<OperationMapRenderAssignmentResult>(
                Allocator.TempJob);
        }

        internal void SetRows(params OperationMapRenderLogicalRowKey[] rows)
        {
            Rows.Clear();
            for (int index = 0; index < rows.Length; index++)
                Rows.AddNoResize(rows[index]);
        }

        internal void Run(int generation)
        {
            new OperationMapRenderAssignmentJob
            {
                Database = _database,
                SelectedCellIndices = Cells,
                RequiredRows = Rows,
                PlacementFirstLogicalRow = Prefix,
                AssignmentGeneration = generation,
                MaxDirtySlotCount = 3,
                SlotToLogicalRow = SlotToRow,
                LogicalRowToSlot = RowToSlot,
                SlotAssignmentGenerations = Generations,
                RequiredSlots = RequiredSlots,
                DirtySlots = DirtySlots,
                ActiveCells = ActiveCells,
                ActivePlacements = ActivePlacements,
                NextFreeSlotByBucket = NextFree,
                SlotCommands = Commands,
                DirtySlotIndices = Dirty,
                Result = Result
            }.Schedule().Complete();
        }

        public void Dispose()
        {
            Result.Dispose();
            Dirty.Dispose();
            Commands.Dispose();
            NextFree.Dispose();
            ActivePlacements.Dispose();
            ActiveCells.Dispose();
            DirtySlots.Dispose();
            RequiredSlots.Dispose();
            Generations.Dispose();
            RowToSlot.Dispose();
            SlotToRow.Dispose();
            Prefix.Dispose();
            Rows.Dispose();
            Cells.Dispose();
            _database.Dispose();
        }

        private static NativeArray<int> Filled(int length, int value)
        {
            var array = new NativeArray<int>(length, Allocator.TempJob);
            for (int index = 0; index < length; index++)
                array[index] = value;
            return array;
        }

        private static NativeBitArray Bits(int length) =>
            new NativeBitArray(
                length, Allocator.TempJob, NativeArrayOptions.ClearMemory);
    }

    private static BlobAssetReference<OperationMapRenderDatabaseBlob>
        CreateDatabase()
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref OperationMapRenderDatabaseBlob root =
            ref builder.ConstructRoot<OperationMapRenderDatabaseBlob>();
        BlobBuilderArray<OperationMapRenderPrototypeBlob> prototypes =
            builder.Allocate(ref root.Prototypes, 2);
        prototypes[0] = new OperationMapRenderPrototypeBlob
            { FirstPart = 0, PartCount = 1 };
        prototypes[1] = new OperationMapRenderPrototypeBlob
            { FirstPart = 1, PartCount = 2 };
        BlobBuilderArray<OperationMapRenderPrototypePartBlob> parts =
            builder.Allocate(ref root.Parts, 3);
        parts[0] = new OperationMapRenderPrototypePartBlob { PoolBucketIndex = 0 };
        parts[1] = new OperationMapRenderPrototypePartBlob { PoolBucketIndex = 0 };
        parts[2] = new OperationMapRenderPrototypePartBlob { PoolBucketIndex = 1 };
        BlobBuilderArray<OperationMapRenderPlacementBlob> placements =
            builder.Allocate(ref root.Placements, 3);
        placements[0] = new OperationMapRenderPlacementBlob { PrototypeIndex = 0 };
        placements[1] = new OperationMapRenderPlacementBlob { PrototypeIndex = 1 };
        placements[2] = new OperationMapRenderPlacementBlob { PrototypeIndex = 0 };
        BlobBuilderArray<OperationMapRenderCellBlob> cells =
            builder.Allocate(ref root.Cells, 1);
        cells[0] = new OperationMapRenderCellBlob();
        BlobBuilderArray<OperationMapRenderPoolBucketBlob> buckets =
            builder.Allocate(ref root.PoolBuckets, 2);
        buckets[0] = new OperationMapRenderPoolBucketBlob
            { FirstSlot = 0, Capacity = 2 };
        buckets[1] = new OperationMapRenderPoolBucketBlob
            { FirstSlot = 2, Capacity = 1 };
        builder.Allocate(ref root.CellPlacementIndices, 0);
        return builder.CreateBlobAssetReference<OperationMapRenderDatabaseBlob>(
            Allocator.Persistent);
    }
}
