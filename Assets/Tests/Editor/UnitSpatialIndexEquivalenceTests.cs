using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class UnitSpatialIndexEquivalenceTests
{
    private const int GridSize = 2048;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new UnitSpatialIndexEquivalenceTests();
            tests.CompactLayout_ContainsOnlyLinkedCellIdentityAndOrderingData();
            passed++;
            tests.FixedBucketEdges_MatchDirectRectangleScan();
            passed++;
            tests.RandomizedRectangles_OneHundredSeedsMatchDirectScan();
            passed++;
            tests.LinkedTraversal_ReversedChainsPreserveDistanceThenSourceOrderRanking();
            passed++;
            tests.Query_RejectsStaleAndOverflowedSnapshots();
            passed++;
            tests.ConsumerLookup_FallsBackForStaleOverflowedAndMismatchedSnapshots();
            passed++;
            tests.Builder_RejectsLayoutsBeyondFixedHeadBudget();
            passed++;
            tests.DisabledBuilder_RefreshesOnlyOnPointOneTwoSecondCadence();
            passed++;
            tests.Source_UsesUnmanagedSinglePassBoundedDesign();
            passed++;

            Debug.Log($"[UnitSpatialIndexEquivalenceValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[UnitSpatialIndexEquivalenceValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void CompactLayout_ContainsOnlyLinkedCellIdentityAndOrderingData()
    {
        FieldInfo[] fields = typeof(UnitSpatialIndexEntry).GetFields(
            BindingFlags.Instance | BindingFlags.Public);
        Assert.AreEqual(4, fields.Length);
        Assert.AreEqual(nameof(UnitSpatialIndexEntry.Entity), fields[0].Name);
        Assert.AreEqual(nameof(UnitSpatialIndexEntry.Cell), fields[1].Name);
        Assert.AreEqual(nameof(UnitSpatialIndexEntry.SourceOrder), fields[2].Name);
        Assert.AreEqual(nameof(UnitSpatialIndexEntry.NextEntryIndex), fields[3].Name);
        Assert.AreEqual(24, UnsafeUtility.SizeOf<UnitSpatialIndexEntry>());
        Assert.AreEqual(4, UnsafeUtility.SizeOf<UnitSpatialIndexBucketHead>());
    }

    [Test]
    public void FixedBucketEdges_MatchDirectRectangleScan()
    {
        using var fixture = new IndexFixture(16, GridSize, GridSize);
        int2[] cells =
        {
            new(0, 0),
            new(127, 127),
            new(128, 0),
            new(0, 128),
            new(128, 128),
            new(255, 255),
            new(256, 256),
            new(1023, 1024),
            new(2047, 2047),
            new(-4, 3000)
        };
        for (int i = 0; i < cells.Length; i++)
            fixture.Add(CreateEntry(i, cells[i]));
        fixture.Rebuild(version: 1u, builtAtElapsedTime: 3d);

        AssertRectangleEquivalent(fixture, new int2(0, 0), new int2(127, 127));
        AssertRectangleEquivalent(fixture, new int2(127, 127), new int2(128, 128));
        AssertRectangleEquivalent(fixture, new int2(120, 120), new int2(260, 260));
        AssertRectangleEquivalent(fixture, new int2(900, 900), new int2(1100, 1100));
        AssertRectangleEquivalent(fixture, new int2(1900, 1900), new int2(2200, 2200));
    }

    [Test]
    public void RandomizedRectangles_OneHundredSeedsMatchDirectScan()
    {
        for (uint seed = 1; seed <= 100; seed++)
        {
            using var fixture = new IndexFixture(96, GridSize, GridSize);
            var random = new Unity.Mathematics.Random(seed);
            for (int i = 0; i < 96; i++)
            {
                int2 cell = random.NextInt2(new int2(-64), new int2(GridSize + 64));
                fixture.Add(CreateEntry(i, cell));
            }
            fixture.Rebuild(seed, builtAtElapsedTime: seed);

            for (int queryIndex = 0; queryIndex < 12; queryIndex++)
            {
                int2 a = random.NextInt2(new int2(-128), new int2(GridSize + 128));
                int2 b = random.NextInt2(new int2(-128), new int2(GridSize + 128));
                AssertRectangleEquivalent(fixture, a, b);
            }
        }
    }

    [Test]
    public void LinkedTraversal_ReversedChainsPreserveDistanceThenSourceOrderRanking()
    {
        using var fixture = new IndexFixture(8, GridSize, GridSize);
        fixture.Add(CreateEntry(0, new int2(66, 64)));
        fixture.Add(CreateEntry(1, new int2(62, 64)));
        fixture.Add(CreateEntry(2, new int2(64, 66)));
        fixture.Add(CreateEntry(3, new int2(64, 62)));
        fixture.Add(CreateEntry(4, new int2(70, 64)));
        fixture.Rebuild(version: 1u, builtAtElapsedTime: 1d);

        NearestFour direct = FindNearestDirect(fixture.Entries.AsNativeArray(), new int2(64), 16);
        NearestFour indexed = FindNearestIndexed(fixture.CreateQuery(), new int2(64), 16);

        AssertNearestEqual(direct, indexed);
        Assert.AreEqual(4, indexed.Count);
        Assert.AreEqual(0, indexed.Order0);
        Assert.AreEqual(1, indexed.Order1);
        Assert.AreEqual(2, indexed.Order2);
        Assert.AreEqual(3, indexed.Order3);
    }

    [Test]
    public void Query_RejectsStaleAndOverflowedSnapshots()
    {
        using var fixture = new IndexFixture(2, GridSize, GridSize);
        fixture.Add(CreateEntry(0, new int2(10)));
        fixture.Add(CreateEntry(1, new int2(20)));
        fixture.Add(CreateEntry(2, new int2(30)));
        fixture.Rebuild(version: 4u, builtAtElapsedTime: 10d);

        UnitSpatialIndexQuery query = fixture.CreateQuery();
        Assert.AreEqual(1, fixture.State.OverflowCount);
        Assert.IsFalse(query.IsReady, "Any dropped entry must force the consumer to direct fallback.");
        Assert.IsFalse(query.IsCurrent(10d));

        fixture.SetOverflowCount(0);
        query = fixture.CreateQuery();
        Assert.IsTrue(query.IsReady);
        Assert.IsTrue(query.IsCurrent(10d));
        Assert.IsFalse(query.IsCurrent(10.0001d));
    }

    [Test]
    public void ConsumerLookup_FallsBackForStaleOverflowedAndMismatchedSnapshots()
    {
        using var fixture = new IndexFixture(8, GridSize, GridSize);
        fixture.Add(CreateEntry(0, new int2(66, 64)));
        fixture.Add(CreateEntry(1, new int2(62, 64)));
        fixture.Add(CreateEntry(2, new int2(64, 66)));
        fixture.Add(CreateEntry(3, new int2(64, 62)));
        fixture.Rebuild(version: 4u, builtAtElapsedTime: 10d);

        var grid = new GridConfig
        {
            Width = GridSize,
            Height = GridSize,
            CellSize = 1f,
            Origin = float3.zero
        };
        NativeArray<UnitSpatialIndexEntry> entries = fixture.Entries.AsNativeArray();
        NearestFour direct = FindNearestDirect(entries, new int2(64), 16);

        NearestFour current = FindNearestForConsumer(
            fixture.CreateQuery(),
            entries,
            grid,
            elapsedTime: 10d,
            new int2(64),
            rangeCells: 16,
            out bool currentUsedIndex);
        Assert.IsTrue(currentUsedIndex);
        AssertNearestEqual(direct, current);

        NearestFour stale = FindNearestForConsumer(
            fixture.CreateQuery(),
            entries,
            grid,
            elapsedTime: 10.0001d,
            new int2(64),
            rangeCells: 16,
            out bool staleUsedIndex);
        Assert.IsFalse(staleUsedIndex);
        AssertNearestEqual(direct, stale);

        GridConfig mismatchedGrid = grid;
        mismatchedGrid.Width = GridSize - 1;
        NearestFour mismatched = FindNearestForConsumer(
            fixture.CreateQuery(),
            entries,
            mismatchedGrid,
            elapsedTime: 10d,
            new int2(64),
            rangeCells: 16,
            out bool mismatchedUsedIndex);
        Assert.IsFalse(mismatchedUsedIndex);
        AssertNearestEqual(direct, mismatched);

        fixture.SetOverflowCount(1);
        NearestFour overflowed = FindNearestForConsumer(
            fixture.CreateQuery(),
            entries,
            grid,
            elapsedTime: 10d,
            new int2(64),
            rangeCells: 16,
            out bool overflowedUsedIndex);
        Assert.IsFalse(overflowedUsedIndex);
        AssertNearestEqual(direct, overflowed);
    }

    [Test]
    public void Builder_RejectsLayoutsBeyondFixedHeadBudget()
    {
        Assert.IsTrue(UnitSpatialIndexBuilder.TryGetBucketLayout(
            GridSize,
            GridSize,
            out int bucketCountX,
            out int bucketCountY,
            out int bucketCount));
        Assert.AreEqual(16, bucketCountX);
        Assert.AreEqual(16, bucketCountY);
        Assert.AreEqual(UnitSpatialIndexBuildSystem.BucketHeadCount, bucketCount);

        Assert.IsFalse(UnitSpatialIndexBuilder.TryGetBucketLayout(
            GridSize + 1,
            GridSize,
            out _,
            out _,
            out int oversizedBucketCount));
        Assert.AreEqual(272, oversizedBucketCount);
    }

    [Test]
    public void DisabledBuilder_RefreshesOnlyOnPointOneTwoSecondCadence()
    {
        using World world = new(nameof(DisabledBuilder_RefreshesOnlyOnPointOneTwoSecondCadence));
        EntityManager em = world.EntityManager;
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = GridSize,
            Height = GridSize,
            CellSize = 1f,
            Origin = float3.zero
        });
        Entity unit = em.CreateEntity(typeof(UnitGrid));
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(40, 80) });
        SystemHandle buildSystem = world.CreateSystem<UnitSpatialIndexBuildSystem>();

        Update(world, buildSystem, elapsedTime: 1d);
        UnitSpatialIndexState first = GetIndexState(em);
        Assert.AreEqual(1u, first.Version);
        Assert.AreEqual(1d, first.BuiltAtElapsedTime);

        Update(world, buildSystem, elapsedTime: 1.05d);
        UnitSpatialIndexState beforeCadence = GetIndexState(em);
        Assert.AreEqual(first.Version, beforeCadence.Version);
        Assert.AreEqual(first.BuiltAtElapsedTime, beforeCadence.BuiltAtElapsedTime);

        Update(world, buildSystem, elapsedTime: 1.12d);
        UnitSpatialIndexState atCadence = GetIndexState(em);
        Assert.AreEqual(first.Version + 1u, atCadence.Version);
        Assert.AreEqual(1.12d, atCadence.BuiltAtElapsedTime);
    }

    [Test]
    public void Source_UsesUnmanagedSinglePassBoundedDesign()
    {
        string source = ReadSource("UnitSpatialIndexBuildSystem.cs");
        StringAssert.Contains("[DisableAutoCreation]", source);
        StringAssert.Contains("partial struct UnitSpatialIndexBuildSystem : ISystem", source);
        StringAssert.Contains("public const int BucketSizeCells = 128;", source);
        StringAssert.Contains("public const int BucketHeadCount = 256;", source);
        StringAssert.Contains("public const double RefreshIntervalSeconds = 0.12d;", source);
        StringAssert.Contains("UnitSpatialIndexBuilder.ClearHeads(heads);", source);
        Assert.AreEqual(1, CountOccurrences(source, "SystemAPI.Query<"));
        Assert.AreEqual(1, CountOccurrences(source, "foreach ("));
        StringAssert.DoesNotContain("ComponentLookup<", source);
        StringAssert.DoesNotContain("UnitSpatialIndexBucketRange", source);
        StringAssert.DoesNotContain("UnitSpatialIndexBucketEntry", source);
    }

    internal static NearestFour FindNearestDirect(
        NativeArray<UnitSpatialIndexEntry> entries,
        int2 origin,
        int rangeCells)
    {
        NearestFour result = default;
        int rangeSquared = rangeCells * rangeCells;
        for (int i = 0; i < entries.Length; i++)
        {
            UnitSpatialIndexEntry entry = entries[i];
            int2 delta = entry.Cell - origin;
            int distanceSquared = (int)math.lengthsq(delta);
            if (distanceSquared <= rangeSquared)
                result.Insert(entry.SourceOrder, distanceSquared);
        }

        return result;
    }

    internal static NearestFour FindNearestIndexed(
        UnitSpatialIndexQuery query,
        int2 origin,
        int rangeCells)
    {
        NearestFour result = default;
        int rangeSquared = rangeCells * rangeCells;
        UnitSpatialIndexQuery.Enumerator enumerator = query.QueryCells(
            origin - new int2(rangeCells),
            origin + new int2(rangeCells));
        NativeArray<UnitSpatialIndexEntry> entries = query.Entries;
        while (enumerator.MoveNext())
        {
            UnitSpatialIndexEntry entry = entries[enumerator.CurrentEntryIndex];
            int2 delta = entry.Cell - origin;
            int distanceSquared = (int)math.lengthsq(delta);
            if (distanceSquared <= rangeSquared)
                result.Insert(entry.SourceOrder, distanceSquared);
        }

        return result;
    }

    internal static NearestFour FindNearestForConsumer(
        UnitSpatialIndexQuery query,
        NativeArray<UnitSpatialIndexEntry> directEntries,
        in GridConfig grid,
        double elapsedTime,
        int2 origin,
        int rangeCells,
        out bool usedIndex)
    {
        usedIndex = query.IsCurrent(elapsedTime) && query.MatchesGrid(grid);
        return usedIndex
            ? FindNearestIndexed(query, origin, rangeCells)
            : FindNearestDirect(directEntries, origin, rangeCells);
    }

    internal struct NearestFour
    {
        public int Count;
        public int Order0;
        public int Order1;
        public int Order2;
        public int Order3;
        private int _distance0;
        private int _distance1;
        private int _distance2;
        private int _distance3;

        public void Insert(int sourceOrder, int distanceSquared)
        {
            if (Count == 0)
            {
                Order0 = sourceOrder;
                _distance0 = distanceSquared;
                Count = 1;
                return;
            }

            if (IsBetter(distanceSquared, sourceOrder, _distance0, Order0))
            {
                Order3 = Order2;
                _distance3 = _distance2;
                Order2 = Order1;
                _distance2 = _distance1;
                Order1 = Order0;
                _distance1 = _distance0;
                Order0 = sourceOrder;
                _distance0 = distanceSquared;
            }
            else if (Count < 2 || IsBetter(distanceSquared, sourceOrder, _distance1, Order1))
            {
                Order3 = Order2;
                _distance3 = _distance2;
                Order2 = Order1;
                _distance2 = _distance1;
                Order1 = sourceOrder;
                _distance1 = distanceSquared;
            }
            else if (Count < 3 || IsBetter(distanceSquared, sourceOrder, _distance2, Order2))
            {
                Order3 = Order2;
                _distance3 = _distance2;
                Order2 = sourceOrder;
                _distance2 = distanceSquared;
            }
            else if (Count < 4 || IsBetter(distanceSquared, sourceOrder, _distance3, Order3))
            {
                Order3 = sourceOrder;
                _distance3 = distanceSquared;
            }

            Count = math.min(4, Count + 1);
        }

        private static bool IsBetter(
            int distanceSquared,
            int sourceOrder,
            int currentDistanceSquared,
            int currentSourceOrder)
        {
            return distanceSquared < currentDistanceSquared ||
                   (distanceSquared == currentDistanceSquared && sourceOrder < currentSourceOrder);
        }
    }

    internal sealed class IndexFixture : IDisposable
    {
        private readonly World _world;
        private readonly Entity _entity;
        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly bool _layoutValid;
        private readonly int _bucketCountX;
        private readonly int _bucketCountY;
        private readonly int _bucketCount;
        private int _overflowCount;

        public IndexFixture(int capacity, int gridWidth, int gridHeight)
        {
            _world = new World(nameof(IndexFixture));
            _entity = _world.EntityManager.CreateEntity(typeof(UnitSpatialIndexState));
            _world.EntityManager.AddBuffer<UnitSpatialIndexEntry>(_entity);
            _world.EntityManager.AddBuffer<UnitSpatialIndexBucketHead>(_entity);
            DynamicBuffer<UnitSpatialIndexEntry> entries =
                _world.EntityManager.GetBuffer<UnitSpatialIndexEntry>(_entity);
            DynamicBuffer<UnitSpatialIndexBucketHead> heads =
                _world.EntityManager.GetBuffer<UnitSpatialIndexBucketHead>(_entity);
            entries.Capacity = math.max(1, capacity);
            heads.ResizeUninitialized(UnitSpatialIndexBuildSystem.BucketHeadCount);
            UnitSpatialIndexBuilder.ClearHeads(heads);
            Entries = entries;
            Heads = heads;
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _layoutValid = UnitSpatialIndexBuilder.TryGetBucketLayout(
                gridWidth,
                gridHeight,
                out _bucketCountX,
                out _bucketCountY,
                out _bucketCount);
        }

        public DynamicBuffer<UnitSpatialIndexEntry> Entries { get; }
        public DynamicBuffer<UnitSpatialIndexBucketHead> Heads { get; }
        public UnitSpatialIndexState State =>
            _world.EntityManager.GetComponentData<UnitSpatialIndexState>(_entity);

        public void Add(UnitSpatialIndexEntry entry)
        {
            if (!_layoutValid ||
                !UnitSpatialIndexBuilder.TryInsert(
                    Entries,
                    Heads,
                    entry.Entity,
                    entry.Cell,
                    entry.SourceOrder,
                    _gridWidth,
                    _gridHeight,
                    _bucketCountX))
            {
                _overflowCount++;
            }
        }

        public void Rebuild(uint version = 1u, double builtAtElapsedTime = 1d)
        {
            UnitSpatialIndexBuilder.ClearHeads(Heads);
            for (int i = 0; i < Entries.Length; i++)
            {
                if (!UnitSpatialIndexBuilder.TryLinkEntry(
                        Entries,
                        Heads,
                        i,
                        _gridWidth,
                        _gridHeight,
                        _bucketCountX))
                {
                    throw new InvalidOperationException("Failed to relink a bounded spatial-index entry.");
                }
            }

            _world.EntityManager.SetComponentData(_entity, new UnitSpatialIndexState
            {
                Version = version,
                BuiltAtElapsedTime = builtAtElapsedTime,
                EntryCount = Entries.Length,
                OverflowCount = _overflowCount,
                GridWidth = math.max(1, _gridWidth),
                GridHeight = math.max(1, _gridHeight),
                BucketCountX = _bucketCountX,
                BucketCountY = _bucketCountY,
                BucketCount = _bucketCount,
                Ready = _layoutValid ? (byte)1 : (byte)0
            });
        }

        public void SetOverflowCount(int overflowCount)
        {
            UnitSpatialIndexState state = State;
            state.OverflowCount = overflowCount;
            _world.EntityManager.SetComponentData(_entity, state);
        }

        public UnitSpatialIndexQuery CreateQuery()
        {
            return new UnitSpatialIndexQuery(
                State,
                Entries.AsNativeArray(),
                Heads.AsNativeArray());
        }

        public void Dispose()
        {
            _world.Dispose();
        }
    }

    private static UnitSpatialIndexEntry CreateEntry(int sourceOrder, int2 cell)
    {
        return new UnitSpatialIndexEntry
        {
            Entity = new Entity { Index = sourceOrder + 1, Version = 1 },
            Cell = cell,
            SourceOrder = sourceOrder,
            NextEntryIndex = UnitSpatialIndexBuilder.InvalidEntryIndex
        };
    }

    private static void AssertRectangleEquivalent(IndexFixture fixture, int2 a, int2 b)
    {
        int2 min = math.min(a, b);
        int2 max = math.max(a, b);
        var direct = new bool[fixture.Entries.Length];
        var indexed = new bool[fixture.Entries.Length];
        for (int i = 0; i < fixture.Entries.Length; i++)
        {
            UnitSpatialIndexEntry entry = fixture.Entries[i];
            if (math.all(entry.Cell >= min) && math.all(entry.Cell <= max))
                direct[entry.SourceOrder] = true;
        }

        UnitSpatialIndexQuery query = fixture.CreateQuery();
        UnitSpatialIndexQuery.Enumerator enumerator = query.QueryCells(a, b);
        NativeArray<UnitSpatialIndexEntry> entries = query.Entries;
        while (enumerator.MoveNext())
        {
            UnitSpatialIndexEntry entry = entries[enumerator.CurrentEntryIndex];
            if (math.all(entry.Cell >= min) && math.all(entry.Cell <= max))
                indexed[entry.SourceOrder] = true;
        }

        CollectionAssert.AreEqual(direct, indexed, $"Rectangle {a}..{b} diverged from direct scan.");
    }

    private static void AssertNearestEqual(NearestFour expected, NearestFour actual)
    {
        Assert.AreEqual(expected.Count, actual.Count);
        Assert.AreEqual(expected.Order0, actual.Order0);
        Assert.AreEqual(expected.Order1, actual.Order1);
        Assert.AreEqual(expected.Order2, actual.Order2);
        Assert.AreEqual(expected.Order3, actual.Order3);
    }

    private static UnitSpatialIndexState GetIndexState(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSpatialIndexState>());
        return em.GetComponentData<UnitSpatialIndexState>(query.GetSingletonEntity());
    }

    private static void Update(World world, SystemHandle system, double elapsedTime)
    {
        world.SetTime(new TimeData(elapsedTime, 0.01f));
        system.Update(world.Unmanaged);
        world.EntityManager.CompleteAllTrackedJobs();
    }

    private static string ReadSource(string fileName)
    {
        string sourcePath = Path.Combine(
            Application.dataPath,
            "Game",
            "Scripts",
            "Systems",
            fileName);
        Assert.IsTrue(File.Exists(sourcePath), $"Missing source at {sourcePath}.");
        return File.ReadAllText(sourcePath);
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
