using Game.Components;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public sealed class OperationMapRenderCellSelectionJobsTests
{
    private BlobAssetReference<OperationMapRenderDatabaseBlob> _database;

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new OperationMapRenderCellSelectionJobsTests();
            RunCase(tests, nameof(RequiredCells_SelectOnlyInclusiveEnvelope),
                test => test.RequiredCells_SelectOnlyInclusiveEnvelope());
            RunCase(tests, nameof(Gather_DeduplicatesFiltersAndExpandsLogicalRows),
                test => test.Gather_DeduplicatesFiltersAndExpandsLogicalRows());
            RunCase(tests, nameof(Gather_IsDeterministicAcrossCellTraversalOrder),
                test => test.Gather_IsDeterministicAcrossCellTraversalOrder());
            RunCase(tests, nameof(DestroyedState_SelectsDestroyedPlacement),
                test => test.DestroyedState_SelectsDestroyedPlacement());
            RunCase(tests, nameof(MissingCanonicalState_FailsClosed),
                test => test.MissingCanonicalState_FailsClosed());
            RunCase(tests, nameof(BoundedCapacity_FailsWithoutGrowing),
                test => test.BoundedCapacity_FailsWithoutGrowing());
            RunCase(tests, nameof(CorruptCellRange_FailsClosed),
                test => test.CorruptCellRange_FailsClosed());
            Debug.Log(
                "[OperationMapRenderCellSelectionValidation] result=Passed tests=7");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[OperationMapRenderCellSelectionValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(
        OperationMapRenderCellSelectionJobsTests tests,
        string name,
        System.Action<OperationMapRenderCellSelectionJobsTests> action)
    {
        tests.SetUp();
        try
        {
            action(tests);
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "[OperationMapRenderCellSelectionValidation] " +
                $"result=Failed test={name} error={exception}");
            throw;
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _database = CreateDatabase();
    }

    [TearDown]
    public void TearDown()
    {
        if (_database.IsCreated)
            _database.Dispose();
    }

    [Test]
    public void RequiredCells_SelectOnlyInclusiveEnvelope()
    {
        using var cells = new NativeList<int>(2, Allocator.TempJob);
        using var failure =
            new NativeReference<OperationMapRenderCellSelectionFailure>(
                Allocator.TempJob);
        var job = new OperationMapRenderRequiredCellSelectionJob
        {
            Database = _database,
            RequiredEnvelope = Envelope(0, 0, 1, 0),
            MaxSelectedCellCount = 2,
            SelectedCellIndices = cells,
            Failure = failure
        };

        job.Schedule().Complete();

        Assert.That(failure.Value, Is.EqualTo(
            OperationMapRenderCellSelectionFailure.None));
        Assert.That(cells.AsArray().ToArray(), Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void Gather_DeduplicatesFiltersAndExpandsLogicalRows()
    {
        using SelectionFixture fixture = CreateSelectionFixture(
            OperationMapRenderVisualState.Intact);
        fixture.Run(new[] { 0, 1 });

        Assert.That(fixture.Failure.Value, Is.EqualTo(
            OperationMapRenderCellSelectionFailure.None));
        Assert.That(
            fixture.Placements.AsArray().ToArray(),
            Is.EqualTo(new[] { 0, 1 }));
        AssertRows(
            fixture.Rows,
            (0, 0, 0),
            (1, 1, 0),
            (1, 2, 1));
    }

    [Test]
    public void Gather_IsDeterministicAcrossCellTraversalOrder()
    {
        using SelectionFixture forward = CreateSelectionFixture(
            OperationMapRenderVisualState.Intact);
        using SelectionFixture reverse = CreateSelectionFixture(
            OperationMapRenderVisualState.Intact);

        forward.Run(new[] { 0, 1 });
        reverse.Run(new[] { 1, 0 });

        Assert.That(
            reverse.Placements.AsArray().ToArray(),
            Is.EqualTo(forward.Placements.AsArray().ToArray()));
        AssertRows(reverse.Rows, (0, 0, 0), (1, 1, 0), (1, 2, 1));
    }

    [Test]
    public void DestroyedState_SelectsDestroyedPlacement()
    {
        using SelectionFixture fixture = CreateSelectionFixture(
            OperationMapRenderVisualState.Destroyed);
        fixture.Run(new[] { 0, 1 });

        Assert.That(
            fixture.Placements.AsArray().ToArray(),
            Is.EqualTo(new[] { 0, 2 }));
        AssertRows(fixture.Rows, (0, 0, 0), (2, 0, 0));
    }

    [Test]
    public void MissingCanonicalState_FailsClosed()
    {
        using SelectionFixture fixture = CreateSelectionFixture(
            OperationMapRenderVisualState.Any);
        fixture.Run(new[] { 0, 1 });

        Assert.That(
            fixture.Failure.Value,
            Is.EqualTo(OperationMapRenderCellSelectionFailure.InvalidVisualState));
        Assert.That(fixture.Placements.Length, Is.Zero);
        Assert.That(fixture.Rows.Length, Is.Zero);
    }

    [Test]
    public void BoundedCapacity_FailsWithoutGrowing()
    {
        using SelectionFixture fixture = CreateSelectionFixture(
            OperationMapRenderVisualState.Intact,
            placementCapacity: 1);
        fixture.Run(new[] { 0, 1 });

        Assert.That(
            fixture.Failure.Value,
            Is.EqualTo(
                OperationMapRenderCellSelectionFailure.PlacementCapacityExceeded));
        Assert.That(fixture.Placements.Length, Is.Zero);
        Assert.That(fixture.Rows.Length, Is.Zero);
    }

    [Test]
    public void CorruptCellRange_FailsClosed()
    {
        BlobAssetReference<OperationMapRenderDatabaseBlob> corrupt =
            CreateDatabase(corruptCellRange: true);
        _database.Dispose();
        _database = corrupt;
        using SelectionFixture fixture = CreateSelectionFixture(
            OperationMapRenderVisualState.Intact);
        fixture.Run(new[] { 0 });

        Assert.That(
            fixture.Failure.Value,
            Is.EqualTo(OperationMapRenderCellSelectionFailure.InvalidCellRange));
        Assert.That(fixture.Placements.Length, Is.Zero);
        Assert.That(fixture.Rows.Length, Is.Zero);
    }

    private SelectionFixture CreateSelectionFixture(
        OperationMapRenderVisualState visualState,
        int placementCapacity = 3)
    {
        return new SelectionFixture(
            _database,
            visualState,
            placementCapacity);
    }

    private static void AssertRows(
        NativeList<OperationMapRenderLogicalRowKey> rows,
        params (int placement, int part, int bucket)[] expected)
    {
        Assert.That(rows.Length, Is.EqualTo(expected.Length));
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(rows[index].PlacementIndex,
                Is.EqualTo(expected[index].placement));
            Assert.That(rows[index].PartIndex,
                Is.EqualTo(expected[index].part));
            Assert.That(rows[index].PoolBucketIndex,
                Is.EqualTo(expected[index].bucket));
        }
    }

    private static OperationMapRenderCellEnvelope Envelope(
        int minX,
        int minY,
        int maxX,
        int maxY)
    {
        return new OperationMapRenderCellEnvelope
        {
            Min = new int2(minX, minY),
            Max = new int2(maxX, maxY)
        };
    }

    private static BlobAssetReference<OperationMapRenderDatabaseBlob>
        CreateDatabase(bool corruptCellRange = false)
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref OperationMapRenderDatabaseBlob root =
            ref builder.ConstructRoot<OperationMapRenderDatabaseBlob>();
        root.SchemaVersion = 1;
        root.CellSize = 32f;
        root.GridDimensions = new int2(3, 1);

        BlobBuilderArray<OperationMapRenderPrototypeBlob> prototypes =
            builder.Allocate(ref root.Prototypes, 2);
        prototypes[0] = new OperationMapRenderPrototypeBlob
        {
            FirstPart = 0,
            PartCount = 1
        };
        prototypes[1] = new OperationMapRenderPrototypeBlob
        {
            FirstPart = 1,
            PartCount = 2
        };

        BlobBuilderArray<OperationMapRenderPrototypePartBlob> parts =
            builder.Allocate(ref root.Parts, 3);
        parts[0] = new OperationMapRenderPrototypePartBlob
        {
            PoolBucketIndex = 0
        };
        parts[1] = new OperationMapRenderPrototypePartBlob
        {
            PoolBucketIndex = 0
        };
        parts[2] = new OperationMapRenderPrototypePartBlob
        {
            PoolBucketIndex = 1
        };

        BlobBuilderArray<OperationMapRenderPlacementBlob> placements =
            builder.Allocate(ref root.Placements, 3);
        placements[0] = new OperationMapRenderPlacementBlob
        {
            PrototypeIndex = 0,
            StateOwnerIndex = -1,
            RequiredVisualState = OperationMapRenderVisualState.Any
        };
        placements[1] = new OperationMapRenderPlacementBlob
        {
            PrototypeIndex = 1,
            StateOwnerIndex = 0,
            RequiredVisualState = OperationMapRenderVisualState.Intact
        };
        placements[2] = new OperationMapRenderPlacementBlob
        {
            PrototypeIndex = 0,
            StateOwnerIndex = 0,
            RequiredVisualState = OperationMapRenderVisualState.Destroyed
        };

        BlobBuilderArray<OperationMapRenderCellBlob> cells =
            builder.Allocate(ref root.Cells, 3);
        cells[0] = new OperationMapRenderCellBlob
        {
            Coordinate = new int2(0, 0),
            FirstPlacementIndex = corruptCellRange ? 6 : 0,
            PlacementIndexCount = 2
        };
        cells[1] = new OperationMapRenderCellBlob
        {
            Coordinate = new int2(1, 0),
            FirstPlacementIndex = 2,
            PlacementIndexCount = 2
        };
        cells[2] = new OperationMapRenderCellBlob
        {
            Coordinate = new int2(2, 0),
            FirstPlacementIndex = 4,
            PlacementIndexCount = 1
        };
        BlobBuilderArray<int> cellPlacements =
            builder.Allocate(ref root.CellPlacementIndices, 5);
        cellPlacements[0] = 0;
        cellPlacements[1] = 1;
        cellPlacements[2] = 1;
        cellPlacements[3] = 2;
        cellPlacements[4] = 0;

        BlobBuilderArray<OperationMapRenderPoolBucketBlob> buckets =
            builder.Allocate(ref root.PoolBuckets, 2);
        buckets[0] = new OperationMapRenderPoolBucketBlob { Capacity = 4 };
        buckets[1] = new OperationMapRenderPoolBucketBlob { Capacity = 2 };

        return builder.CreateBlobAssetReference<OperationMapRenderDatabaseBlob>(
            Allocator.Persistent);
    }

    private sealed class SelectionFixture : System.IDisposable
    {
        internal NativeArray<OperationMapRenderVisualState> States;
        internal NativeBitArray Visited;
        internal NativeList<int> Cells;
        internal NativeList<int> Placements;
        internal NativeList<OperationMapRenderLogicalRowKey> Rows;
        internal NativeReference<
            OperationMapRenderCellSelectionFailure> Failure;
        private readonly BlobAssetReference<OperationMapRenderDatabaseBlob>
            _database;
        private readonly int _maxPlacementCount;

        internal SelectionFixture(
            BlobAssetReference<OperationMapRenderDatabaseBlob> database,
            OperationMapRenderVisualState visualState,
            int placementCapacity)
        {
            _database = database;
            _maxPlacementCount = placementCapacity;
            States = new NativeArray<OperationMapRenderVisualState>(
                1,
                Allocator.TempJob);
            States[0] = visualState;
            Visited = new NativeBitArray(
                3,
                Allocator.TempJob,
                NativeArrayOptions.ClearMemory);
            Cells = new NativeList<int>(2, Allocator.TempJob);
            Placements = new NativeList<int>(3, Allocator.TempJob);
            Rows = new NativeList<OperationMapRenderLogicalRowKey>(
                4,
                Allocator.TempJob);
            Failure =
                new NativeReference<OperationMapRenderCellSelectionFailure>(
                    Allocator.TempJob);
        }

        internal void Run(int[] cellIndices)
        {
            Cells.Clear();
            for (int index = 0; index < cellIndices.Length; index++)
                Cells.AddNoResize(cellIndices[index]);
            Failure.Value = OperationMapRenderCellSelectionFailure.None;
            var job = new OperationMapRenderPlacementGatherJob
            {
                Database = _database,
                SelectedCellIndices = Cells,
                CanonicalVisualStates = States,
                MaxSelectedPlacementCount = _maxPlacementCount,
                MaxSelectedLogicalRowCount = 4,
                VisitedPlacements = Visited,
                SelectedPlacementIndices = Placements,
                SelectedLogicalRows = Rows,
                Failure = Failure
            };
            JobHandle handle = job.Schedule();
            handle.Complete();
        }

        public void Dispose()
        {
            Failure.Dispose();
            Rows.Dispose();
            Placements.Dispose();
            Cells.Dispose();
            Visited.Dispose();
            States.Dispose();
        }
    }
}
