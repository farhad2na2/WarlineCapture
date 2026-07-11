using System;
using System.IO;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class UnitSpatialIndexEquivalenceTests
{
    private const int GridSize = 2048;
    private static readonly int[] BucketSizes = { 16, 32, 64 };

    [Test]
    public void FixedEdges_PreserveInclusiveBoundsAndGlobalSourceOrder()
    {
        using var fixture = new IndexFixture(8, GridSize, 16);
        fixture.Add(CreateEntry(0, new int2(0, 0), FactionIdentity.EnemyFactionId));
        fixture.Add(CreateEntry(1, new int2(15, 15), FactionIdentity.EnemyFactionId));
        fixture.Add(CreateEntry(2, new int2(16, 16), FactionIdentity.EnemyFactionId));
        fixture.Add(CreateEntry(3, new int2(GridSize - 1), FactionIdentity.EnemyFactionId));
        fixture.Rebuild();

        var query = fixture.CreateQuery();
        FixedList64Bytes<int> firstBucket = CollectSortedSourceOrders(query, new int2(-50), new int2(15, 15));
        CollectionAssert.AreEqual(new[] { 0, 1 }, ToArray(firstBucket));
        FixedList64Bytes<int> secondBucket = CollectSortedSourceOrders(query, new int2(16), new int2(16));
        CollectionAssert.AreEqual(new[] { 2 }, ToArray(secondBucket));
        FixedList64Bytes<int> clampedLast = CollectSortedSourceOrders(
            query,
            new int2(GridSize - 1),
            new int2(GridSize + 100));
        CollectionAssert.AreEqual(new[] { 3 }, ToArray(clampedLast));

        NativeArray<UnitSpatialIndexEntry> entries = query.Entries;
        for (int i = 0; i < entries.Length; i++)
            Assert.AreEqual(i, entries[i].SourceOrder);
    }

    [Test]
    public void EqualDistanceAndEqualScore_KeepFirstGlobalSourceWinner()
    {
        using var fixture = new IndexFixture(8, 128, 16);
        fixture.Add(CreateEntry(0, new int2(40, 32), FactionIdentity.EnemyFactionId, health: 100));
        fixture.Add(CreateEntry(1, new int2(24, 32), FactionIdentity.EnemyFactionId, health: 100));
        fixture.Add(CreateEntry(2, new int2(32, 40), FactionIdentity.EnemyFactionId, health: 100));
        fixture.Add(CreateEntry(3, new int2(32, 24), FactionIdentity.EnemyFactionId, health: 100));
        fixture.Rebuild();

        FixedList64Bytes<RankedCandidate> direct = FindNearestDirect(
            fixture.Entries.AsNativeArray(),
            new int2(32),
            rangeCells: 16,
            FactionIdentity.PlayerFactionId);
        FixedList64Bytes<RankedCandidate> indexed = FindNearestIndexed(
            fixture.CreateQuery(),
            new int2(32),
            rangeCells: 16,
            FactionIdentity.PlayerFactionId);
        AssertRankedEqual(direct, indexed);
        Assert.AreEqual(0, indexed[0].SourceOrder);

        int directAi = FindAiBestDirect(fixture.Entries.AsNativeArray(), new int2(32), FactionIdentity.PlayerFactionId);
        int indexedAi = FindAiBestIndexed(fixture.CreateQuery(), new int2(32), FactionIdentity.PlayerFactionId);
        Assert.AreEqual(0, directAi);
        Assert.AreEqual(directAi, indexedAi);
    }

    [Test]
    public void EmptyRebuild_ClearsPreviouslyPopulatedBuckets()
    {
        using var fixture = new IndexFixture(4, 128, 16);
        fixture.Add(CreateEntry(0, new int2(10), FactionIdentity.EnemyFactionId));
        fixture.Rebuild();
        Assert.IsTrue(fixture.CreateQuery().QueryCells(int2.zero, new int2(127)).MoveNext());

        fixture.Entries.Clear();
        fixture.Rebuild();
        UnitSpatialIndexQuery empty = fixture.CreateQuery();
        Assert.AreEqual(0, fixture.State.EntryCount);
        Assert.IsFalse(empty.QueryCells(int2.zero, new int2(127)).MoveNext());
    }

    [Test]
    public void SeededFixtures_MatchDirectBuildingAiThreatAndSelectionResults()
    {
        const int seedCount = 100;
        const int entryCount = 740;
        const int towerCount = 12;

        for (int bucketIndex = 0; bucketIndex < BucketSizes.Length; bucketIndex++)
        {
            int bucketSize = BucketSizes[bucketIndex];
            using var fixture = new IndexFixture(entryCount, GridSize, bucketSize);
            using var selectionMembership = new NativeArray<byte>(entryCount, Allocator.Temp);
            for (uint seed = 1; seed <= seedCount; seed++)
            {
                fixture.Entries.Clear();
                var random = new Unity.Mathematics.Random(seed * 747796405u + 2891336453u);
                for (int i = 0; i < entryCount; i++)
                {
                    int2 cell = random.NextInt2(int2.zero, new int2(GridSize));
                    byte faction = (byte)(random.NextBool()
                        ? FactionIdentity.PlayerFactionId
                        : FactionIdentity.EnemyFactionId);
                    int health = random.NextInt(0, 201);
                    UnitSpatialIndexFlags flags = UnitSpatialIndexFlags.HasHealth |
                                                  UnitSpatialIndexFlags.HasLocalTransform |
                                                  UnitSpatialIndexFlags.HasLocalToWorld |
                                                  UnitSpatialIndexFlags.Selectable;
                    if ((i & 3) == 0)
                        flags |= UnitSpatialIndexFlags.GroundVehicle | UnitSpatialIndexFlags.SelectionVehicle;
                    if ((i & 15) == 0)
                        flags |= UnitSpatialIndexFlags.Air;
                    if ((i & 31) == 0)
                        flags |= UnitSpatialIndexFlags.RuntimeBuilding;
                    fixture.Add(CreateEntry(i, cell, faction, health, flags));
                }

                fixture.Rebuild();
                UnitSpatialIndexQuery query = fixture.CreateQuery();
                Assert.AreEqual(entryCount, query.Entries.Length);
                for (int i = 0; i < query.Entries.Length; i++)
                    Assert.AreEqual(i, query.Entries[i].SourceOrder);

                for (int tower = 0; tower < towerCount; tower++)
                {
                    int2 origin = random.NextInt2(int2.zero, new int2(GridSize));
                    int range = random.NextInt(8, 97);
                    FixedList64Bytes<RankedCandidate> direct = FindNearestDirect(
                        fixture.Entries.AsNativeArray(),
                        origin,
                        range,
                        FactionIdentity.PlayerFactionId);
                    FixedList64Bytes<RankedCandidate> indexed = FindNearestIndexed(
                        query,
                        origin,
                        range,
                        FactionIdentity.PlayerFactionId);
                    AssertRankedEqual(direct, indexed);
                }

                int2 aiOrigin = random.NextInt2(int2.zero, new int2(GridSize));
                Assert.AreEqual(
                    FindAiBestDirect(fixture.Entries.AsNativeArray(), aiOrigin, FactionIdentity.PlayerFactionId),
                    FindAiBestIndexed(query, aiOrigin, FactionIdentity.PlayerFactionId));

                int2 sensor = random.NextInt2(int2.zero, new int2(GridSize));
                int sensorRadius = random.NextInt(12, 65);
                Assert.AreEqual(
                    SummarizeThreatsDirect(fixture.Entries.AsNativeArray(), sensor, sensorRadius),
                    SummarizeThreatsIndexed(query, sensor, sensorRadius));

                int2 selectionStart = random.NextInt2(int2.zero, new int2(GridSize));
                int2 selectionExtent = random.NextInt2(new int2(8), new int2(160));
                int2 selectionEnd = math.min(selectionStart + selectionExtent, new int2(GridSize - 1));
                Assert.AreEqual(
                    SummarizeSelectionDirect(
                        fixture.Entries.AsNativeArray(),
                        selectionStart,
                        selectionEnd),
                    SummarizeSelectionIndexed(
                        query,
                        selectionStart,
                        selectionEnd,
                        selectionMembership));
            }
        }
    }

    [Test]
    public void BuildSystemSource_UsesUnmanagedISystemAndKeepsMeasuredUpdateFreeOfForbiddenBoundaries()
    {
        string source = File.ReadAllText(Path.Combine(
            Application.dataPath,
            "Game",
            "Scripts",
            "Systems",
            "UnitSpatialIndexBuildSystem.cs"));
        StringAssert.Contains("partial struct UnitSpatialIndexBuildSystem : ISystem", source);
        StringAssert.Contains("[BurstCompile]", source);
        StringAssert.DoesNotContain("SystemBase", source);
        StringAssert.DoesNotContain("World.DefaultGameObjectInjectionWorld", source);
        StringAssert.DoesNotContain(".Complete(", source);
        StringAssert.DoesNotContain("ToEntityArray(", source);
        StringAssert.DoesNotContain("ToComponentDataArray(", source);

        string update = ExtractMethod(source, "public void OnUpdate(ref SystemState state)", "private void UpdateLookups");
        StringAssert.DoesNotContain("CreateEntity(", update);
        StringAssert.DoesNotContain("DestroyEntity(", update);
        StringAssert.DoesNotContain("AddBuffer<", update);
        StringAssert.DoesNotContain("Allocator.", update);
        StringAssert.DoesNotContain("new Native", update);
        StringAssert.Contains("entries.Clear();", update);
        StringAssert.Contains("SourceOrder = sourceOrder", source);
    }

    private static UnitSpatialIndexEntry CreateEntry(
        int sourceOrder,
        int2 cell,
        byte faction,
        int health = 100,
        UnitSpatialIndexFlags flags = UnitSpatialIndexFlags.HasHealth |
                                      UnitSpatialIndexFlags.HasLocalTransform |
                                      UnitSpatialIndexFlags.HasLocalToWorld |
                                      UnitSpatialIndexFlags.Selectable)
    {
        return new UnitSpatialIndexEntry
        {
            Entity = new Entity { Index = sourceOrder + 1, Version = 1 },
            SourceOrder = sourceOrder,
            Cell = cell,
            Position = new float3(cell.x + 0.5f, 0f, cell.y + 0.5f),
            SelectionPosition = new float3(cell.x + 0.5f, 0f, cell.y + 0.5f),
            HealthCurrent = health,
            HealthMax = math.max(health, 100),
            FactionId = faction,
            Flags = flags
        };
    }

    internal static FixedList64Bytes<RankedCandidate> FindNearestDirect(
        NativeArray<UnitSpatialIndexEntry> entries,
        int2 origin,
        int rangeCells,
        byte sourceFaction)
    {
        FixedList64Bytes<RankedCandidate> result = default;
        int rangeSq = rangeCells * rangeCells;
        for (int i = 0; i < entries.Length; i++)
            TryRankBuildingCandidate(entries[i], origin, rangeSq, sourceFaction, ref result);
        return result;
    }

    internal static FixedList64Bytes<RankedCandidate> FindNearestIndexed(
        UnitSpatialIndexQuery query,
        int2 origin,
        int rangeCells,
        byte sourceFaction)
    {
        FixedList64Bytes<RankedCandidate> result = default;
        int rangeSq = rangeCells * rangeCells;
        UnitSpatialIndexQuery.Enumerator enumerator = query.QueryCells(
            origin - new int2(rangeCells),
            origin + new int2(rangeCells));
        NativeArray<UnitSpatialIndexEntry> entries = query.Entries;
        while (enumerator.MoveNext())
        {
            int index = enumerator.CurrentEntryIndex;
            if ((uint)index < (uint)entries.Length)
                TryRankBuildingCandidate(entries[index], origin, rangeSq, sourceFaction, ref result);
        }
        return result;
    }

    private static void TryRankBuildingCandidate(
        in UnitSpatialIndexEntry entry,
        int2 origin,
        int rangeSq,
        byte sourceFaction,
        ref FixedList64Bytes<RankedCandidate> ranked)
    {
        if (!entry.Has(UnitSpatialIndexFlags.HasHealth) ||
            entry.HealthCurrent <= 0 ||
            entry.FactionId == sourceFaction ||
            entry.FactionId == FactionIdentity.NeutralFactionId ||
            (entry.Flags & (UnitSpatialIndexFlags.Air | UnitSpatialIndexFlags.DebugTarget)) != 0)
        {
            return;
        }

        int2 delta = entry.Cell - origin;
        int distanceSq = delta.x * delta.x + delta.y * delta.y;
        if (distanceSq > rangeSq)
            return;

        InsertRanked(ref ranked, new RankedCandidate(entry.SourceOrder, distanceSq));
    }

    private static void InsertRanked(
        ref FixedList64Bytes<RankedCandidate> ranked,
        RankedCandidate candidate)
    {
        int insertAt = ranked.Length;
        for (int i = 0; i < ranked.Length; i++)
        {
            RankedCandidate current = ranked[i];
            if (candidate.DistanceSq < current.DistanceSq ||
                (candidate.DistanceSq == current.DistanceSq && candidate.SourceOrder < current.SourceOrder))
            {
                insertAt = i;
                break;
            }
        }

        if (insertAt >= 4)
            return;
        if (ranked.Length < 4)
            ranked.Add(default);
        for (int i = ranked.Length - 1; i > insertAt; i--)
            ranked[i] = ranked[i - 1];
        ranked[insertAt] = candidate;
    }

    private static int FindAiBestDirect(NativeArray<UnitSpatialIndexEntry> entries, int2 origin, byte sourceFaction)
    {
        int bestOrder = -1;
        int bestScore = int.MinValue;
        for (int i = 0; i < entries.Length; i++)
            ScoreAi(entries[i], origin, sourceFaction, ref bestOrder, ref bestScore);
        return bestOrder;
    }

    private static int FindAiBestIndexed(UnitSpatialIndexQuery query, int2 origin, byte sourceFaction)
    {
        int bestOrder = -1;
        int bestScore = int.MinValue;
        UnitSpatialIndexQuery.Enumerator enumerator = query.QueryCells(int2.zero, new int2(GridSize - 1));
        NativeArray<UnitSpatialIndexEntry> entries = query.Entries;
        while (enumerator.MoveNext())
            ScoreAi(entries[enumerator.CurrentEntryIndex], origin, sourceFaction, ref bestOrder, ref bestScore);
        return bestOrder;
    }

    private static void ScoreAi(
        in UnitSpatialIndexEntry entry,
        int2 origin,
        byte sourceFaction,
        ref int bestOrder,
        ref int bestScore)
    {
        if (!entry.Has(UnitSpatialIndexFlags.HasHealth) ||
            entry.HealthCurrent <= 0 ||
            entry.FactionId == sourceFaction ||
            entry.FactionId == FactionIdentity.NeutralFactionId)
        {
            return;
        }

        int distance = math.abs(entry.Cell.x - origin.x) + math.abs(entry.Cell.y - origin.y);
        int score = 100 - math.min(distance, 100) + math.clamp(entry.HealthMax / 10, 0, 30);
        if (entry.Has(UnitSpatialIndexFlags.RuntimeBuilding))
            score += 35;
        else if ((entry.Flags & (UnitSpatialIndexFlags.CanAttack | UnitSpatialIndexFlags.HasCombat)) != 0)
            score += 45;
        else
            score += 10;
        if (entry.Has(UnitSpatialIndexFlags.ResourceHauler))
            score += 20;

        if (score > bestScore || (score == bestScore && (bestOrder < 0 || entry.SourceOrder < bestOrder)))
        {
            bestScore = score;
            bestOrder = entry.SourceOrder;
        }
    }

    private static ThreatSummary SummarizeThreatsDirect(
        NativeArray<UnitSpatialIndexEntry> entries,
        int2 sensor,
        int radius)
    {
        ThreatSummary summary = default;
        summary.MinDistance = int.MaxValue;
        for (int i = 0; i < entries.Length; i++)
            AddThreat(entries[i], sensor, radius, ref summary);
        return summary;
    }

    private static ThreatSummary SummarizeThreatsIndexed(UnitSpatialIndexQuery query, int2 sensor, int radius)
    {
        ThreatSummary summary = default;
        summary.MinDistance = int.MaxValue;
        UnitSpatialIndexQuery.Enumerator enumerator = query.QueryCells(
            sensor - new int2(radius),
            sensor + new int2(radius));
        NativeArray<UnitSpatialIndexEntry> entries = query.Entries;
        while (enumerator.MoveNext())
            AddThreat(entries[enumerator.CurrentEntryIndex], sensor, radius, ref summary);
        return summary;
    }

    private static void AddThreat(
        in UnitSpatialIndexEntry entry,
        int2 sensor,
        int radius,
        ref ThreatSummary summary)
    {
        if (!entry.Has(UnitSpatialIndexFlags.HasHealth) ||
            entry.HealthCurrent <= 0 ||
            entry.FactionId != FactionIdentity.EnemyFactionId ||
            entry.Has(UnitSpatialIndexFlags.RuntimeBuilding))
        {
            return;
        }

        bool validKind = entry.Has(UnitSpatialIndexFlags.Air) || entry.Has(UnitSpatialIndexFlags.GroundVehicle);
        int distance = math.cmax(math.abs(entry.Cell - sensor));
        if (!validKind || distance > radius)
            return;

        summary.Count++;
        summary.MinDistance = math.min(summary.MinDistance, distance);
        summary.SourceOrderSum += entry.SourceOrder;
        summary.SourceOrderXor ^= entry.SourceOrder;
    }

    private static SelectionSummary SummarizeSelectionDirect(
        NativeArray<UnitSpatialIndexEntry> entries,
        int2 min,
        int2 max)
    {
        SelectionSummary summary = default;
        for (int i = 0; i < entries.Length; i++)
        {
            UnitSpatialIndexEntry entry = entries[i];
            if (IsSelectionCandidate(entry, min, max))
                AddSelection(entry, ref summary);
        }
        return summary;
    }

    private static SelectionSummary SummarizeSelectionIndexed(
        UnitSpatialIndexQuery query,
        int2 min,
        int2 max,
        NativeArray<byte> membership)
    {
        for (int i = 0; i < membership.Length; i++)
            membership[i] = 0;

        UnitSpatialIndexQuery.Enumerator enumerator = query.QueryCells(min, max);
        NativeArray<UnitSpatialIndexEntry> entries = query.Entries;
        while (enumerator.MoveNext())
        {
            int entryIndex = enumerator.CurrentEntryIndex;
            if ((uint)entryIndex < (uint)entries.Length && IsSelectionCandidate(entries[entryIndex], min, max))
                membership[entryIndex] = 1;
        }

        SelectionSummary summary = default;
        for (int i = 0; i < entries.Length; i++)
        {
            if (membership[i] != 0)
                AddSelection(entries[i], ref summary);
        }
        return summary;
    }

    private static bool IsSelectionCandidate(in UnitSpatialIndexEntry entry, int2 min, int2 max)
    {
        return entry.FactionId == FactionIdentity.PlayerFactionId &&
               entry.Has(UnitSpatialIndexFlags.Selectable) &&
               math.all(entry.Cell >= min) &&
               math.all(entry.Cell <= max);
    }

    private static void AddSelection(in UnitSpatialIndexEntry entry, ref SelectionSummary summary)
    {
        summary.Count++;
        if (entry.Has(UnitSpatialIndexFlags.SelectionVehicle))
            summary.VehicleCount++;
        summary.OrderHash = unchecked(summary.OrderHash * 16777619u ^ (uint)entry.SourceOrder);
        summary.PositionHash = unchecked(summary.PositionHash * 16777619u ^ math.hash(entry.SelectionPosition));
    }

    private static FixedList64Bytes<int> CollectSortedSourceOrders(
        UnitSpatialIndexQuery query,
        int2 min,
        int2 max)
    {
        FixedList64Bytes<int> orders = default;
        UnitSpatialIndexQuery.Enumerator enumerator = query.QueryCells(min, max);
        NativeArray<UnitSpatialIndexEntry> entries = query.Entries;
        while (enumerator.MoveNext())
        {
            UnitSpatialIndexEntry entry = entries[enumerator.CurrentEntryIndex];
            if (math.all(entry.Cell >= math.min(min, max)) && math.all(entry.Cell <= math.max(min, max)))
                orders.Add(entry.SourceOrder);
        }
        for (int i = 1; i < orders.Length; i++)
        {
            int value = orders[i];
            int j = i - 1;
            while (j >= 0 && orders[j] > value)
            {
                orders[j + 1] = orders[j];
                j--;
            }
            orders[j + 1] = value;
        }
        return orders;
    }

    private static int[] ToArray(FixedList64Bytes<int> values)
    {
        var result = new int[values.Length];
        for (int i = 0; i < values.Length; i++)
            result[i] = values[i];
        return result;
    }

    private static void AssertRankedEqual(
        FixedList64Bytes<RankedCandidate> expected,
        FixedList64Bytes<RankedCandidate> actual)
    {
        Assert.AreEqual(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i].SourceOrder, actual[i].SourceOrder, $"Rank {i} source order differs.");
            Assert.AreEqual(expected[i].DistanceSq, actual[i].DistanceSq, $"Rank {i} distance differs.");
        }
    }

    private static string ExtractMethod(string source, string startToken, string endToken)
    {
        int start = source.IndexOf(startToken, StringComparison.Ordinal);
        int end = source.IndexOf(endToken, start, StringComparison.Ordinal);
        Assert.GreaterOrEqual(start, 0);
        Assert.Greater(end, start);
        return source.Substring(start, end - start);
    }

    internal readonly struct RankedCandidate
    {
        public RankedCandidate(int sourceOrder, int distanceSq)
        {
            SourceOrder = sourceOrder;
            DistanceSq = distanceSq;
        }

        public int SourceOrder { get; }
        public int DistanceSq { get; }
    }

    private struct ThreatSummary : IEquatable<ThreatSummary>
    {
        public int Count;
        public int MinDistance;
        public int SourceOrderSum;
        public int SourceOrderXor;

        public bool Equals(ThreatSummary other)
        {
            return Count == other.Count &&
                   MinDistance == other.MinDistance &&
                   SourceOrderSum == other.SourceOrderSum &&
                   SourceOrderXor == other.SourceOrderXor;
        }

        public override bool Equals(object obj)
        {
            return obj is ThreatSummary other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (((Count * 397) ^ MinDistance) * 397 ^ SourceOrderSum) * 397 ^ SourceOrderXor;
        }
    }

    private struct SelectionSummary : IEquatable<SelectionSummary>
    {
        public int Count;
        public int VehicleCount;
        public uint OrderHash;
        public uint PositionHash;

        public bool Equals(SelectionSummary other)
        {
            return Count == other.Count &&
                   VehicleCount == other.VehicleCount &&
                   OrderHash == other.OrderHash &&
                   PositionHash == other.PositionHash;
        }

        public override bool Equals(object obj)
        {
            return obj is SelectionSummary other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (((Count * 397) ^ VehicleCount) * 397 ^ (int)OrderHash) * 397 ^ (int)PositionHash;
        }
    }

    internal sealed class IndexFixture : IDisposable
    {
        private readonly World _world;
        private readonly Entity _entity;
        private readonly int _gridSize;
        private readonly int _bucketSize;

        public IndexFixture(int entryCapacity, int gridSize, int bucketSize)
        {
            _world = new World(nameof(IndexFixture));
            _entity = _world.EntityManager.CreateEntity(typeof(UnitSpatialIndexState));
            _world.EntityManager.AddBuffer<UnitSpatialIndexEntry>(_entity);
            _world.EntityManager.AddBuffer<UnitSpatialIndexBucketRange>(_entity);
            _world.EntityManager.AddBuffer<UnitSpatialIndexBucketEntry>(_entity);
            DynamicBuffer<UnitSpatialIndexEntry> entries =
                _world.EntityManager.GetBuffer<UnitSpatialIndexEntry>(_entity);
            DynamicBuffer<UnitSpatialIndexBucketRange> ranges =
                _world.EntityManager.GetBuffer<UnitSpatialIndexBucketRange>(_entity);
            DynamicBuffer<UnitSpatialIndexBucketEntry> bucketEntries =
                _world.EntityManager.GetBuffer<UnitSpatialIndexBucketEntry>(_entity);
            int bucketCount = ((gridSize + bucketSize - 1) / bucketSize);
            bucketCount *= bucketCount;
            entries.Capacity = entryCapacity;
            ranges.Capacity = bucketCount;
            bucketEntries.Capacity = entryCapacity;
            ranges.ResizeUninitialized(bucketCount);
            bucketEntries.ResizeUninitialized(entryCapacity);
            UnitSpatialIndexBuilder.ClearRanges(ranges, bucketCount);
            Entries = entries;
            Ranges = ranges;
            BucketEntries = bucketEntries;
            _gridSize = gridSize;
            _bucketSize = bucketSize;
        }

        public DynamicBuffer<UnitSpatialIndexEntry> Entries { get; }
        public DynamicBuffer<UnitSpatialIndexBucketRange> Ranges { get; }
        public DynamicBuffer<UnitSpatialIndexBucketEntry> BucketEntries { get; }
        public UnitSpatialIndexState State => _world.EntityManager.GetComponentData<UnitSpatialIndexState>(_entity);

        public void Add(UnitSpatialIndexEntry entry)
        {
            Entries.Add(entry);
        }

        public void Rebuild(uint version = 1)
        {
            UnitSpatialIndexBuilder.BuildBuckets(
                Entries,
                Ranges,
                BucketEntries,
                _gridSize,
                _gridSize,
                _bucketSize,
                overflowCount: 0,
                version,
                out UnitSpatialIndexState state);
            _world.EntityManager.SetComponentData(_entity, state);
        }

        public UnitSpatialIndexQuery CreateQuery()
        {
            return new UnitSpatialIndexQuery(
                State,
                Entries.AsNativeArray(),
                Ranges.AsNativeArray(),
                BucketEntries.AsNativeArray());
        }

        public void Dispose()
        {
            _world.Dispose();
        }
    }
}
