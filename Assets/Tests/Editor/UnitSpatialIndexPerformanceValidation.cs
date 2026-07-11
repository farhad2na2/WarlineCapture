using System;
using System.Diagnostics;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

public sealed class UnitSpatialIndexPerformanceValidation
{
    private const int GridSize = 2048;
    private const int EntryCount = 740;
    private const int TowerCount = 32;
    private const int WarmupIterations = 32;
    private const int MeasuredIterations = 128;
    private static int s_benchmarkSink;

    [TestCase(16)]
    [TestCase(32)]
    [TestCase(64)]
    public void AlternatingShadowBenchmark_PreservesResultsAndAllocatesNoManagedMemory(int bucketSize)
    {
        using var fixture = new UnitSpatialIndexEquivalenceTests.IndexFixture(
            EntryCount,
            GridSize,
            bucketSize);
        Populate(fixture);
        var origins = new int2[TowerCount];
        for (int i = 0; i < origins.Length; i++)
            origins[i] = new int2(512 + i * 11, 768 + i * 7);

        AssertEquivalent(fixture, origins, version: 1u);

        for (int i = 0; i < WarmupIterations; i++)
        {
            RunDirect(fixture, origins);
            RunIndexed(fixture, origins, (uint)i + 1u);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long directTicks = 0;
        long indexedTicks = 0;
        for (int iteration = 0; iteration < MeasuredIterations; iteration++)
        {
            if ((iteration & 1) == 0)
            {
                directTicks += MeasureDirect(fixture, origins);
                indexedTicks += MeasureIndexed(fixture, origins, (uint)iteration + 100u);
            }
            else
            {
                indexedTicks += MeasureIndexed(fixture, origins, (uint)iteration + 100u);
                directTicks += MeasureDirect(fixture, origins);
            }
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        fixture.Rebuild(1000u);
        long payloadBytes =
            (long)fixture.Entries.Length * UnsafeUtility.SizeOf<UnitSpatialIndexEntry>() +
            (long)fixture.State.BucketCount * UnsafeUtility.SizeOf<UnitSpatialIndexBucketRange>() +
            (long)fixture.State.BucketReferenceCount * UnsafeUtility.SizeOf<UnitSpatialIndexBucketEntry>();

        Assert.AreEqual(0, allocatedBytes, "The warmed shadow build/query path must not allocate managed memory.");
        Assert.LessOrEqual(payloadBytes, 256L * 1024L, "The 740-entry shadow payload exceeds the APH-704 memory ceiling.");
        Assert.AreEqual(0, fixture.State.OverflowCount);
        Assert.AreEqual(EntryCount, fixture.State.EntryCount);
        Assert.Greater(directTicks, 0);
        Assert.Greater(indexedTicks, 0);

        double tickToMs = 1000d / Stopwatch.Frequency;
        Debug.Log(
            $"[UnitSpatialIndexShadowBenchmark] bucketSize={bucketSize} entries={EntryCount} towers={TowerCount} " +
            $"warmup={WarmupIterations} measured={MeasuredIterations} " +
            $"directMs={directTicks * tickToMs:0.###} indexedBuildAndQueryMs={indexedTicks * tickToMs:0.###} " +
            $"managedBytes={allocatedBytes} payloadBytes={payloadBytes}");
    }

    private static long MeasureDirect(
        UnitSpatialIndexEquivalenceTests.IndexFixture fixture,
        int2[] origins)
    {
        long start = Stopwatch.GetTimestamp();
        RunDirect(fixture, origins);
        return Stopwatch.GetTimestamp() - start;
    }

    private static long MeasureIndexed(
        UnitSpatialIndexEquivalenceTests.IndexFixture fixture,
        int2[] origins,
        uint version)
    {
        long start = Stopwatch.GetTimestamp();
        RunIndexed(fixture, origins, version);
        return Stopwatch.GetTimestamp() - start;
    }

    private static void RunDirect(
        UnitSpatialIndexEquivalenceTests.IndexFixture fixture,
        int2[] origins)
    {
        int checksum = 0;
        for (int i = 0; i < origins.Length; i++)
        {
            var result = UnitSpatialIndexEquivalenceTests.FindNearestDirect(
                fixture.Entries.AsNativeArray(),
                origins[i],
                rangeCells: 96,
                FactionIdentity.PlayerFactionId);
            if (result.Length > 0 && result[0].SourceOrder < 0)
                throw new InvalidOperationException("Invalid direct benchmark result.");
            if (result.Length > 0)
                checksum += result[0].SourceOrder;
        }
        s_benchmarkSink = checksum;
    }

    private static void RunIndexed(
        UnitSpatialIndexEquivalenceTests.IndexFixture fixture,
        int2[] origins,
        uint version)
    {
        fixture.Rebuild(version);
        UnitSpatialIndexQuery query = fixture.CreateQuery();
        int checksum = 0;
        for (int i = 0; i < origins.Length; i++)
        {
            var indexed = UnitSpatialIndexEquivalenceTests.FindNearestIndexed(
                query,
                origins[i],
                rangeCells: 96,
                FactionIdentity.PlayerFactionId);
            if (indexed.Length > 0 && indexed[0].SourceOrder < 0)
                throw new InvalidOperationException("Invalid indexed benchmark result.");
            if (indexed.Length > 0)
                checksum += indexed[0].SourceOrder;
        }
        s_benchmarkSink = checksum;
    }

    private static void AssertEquivalent(
        UnitSpatialIndexEquivalenceTests.IndexFixture fixture,
        int2[] origins,
        uint version)
    {
        fixture.Rebuild(version);
        UnitSpatialIndexQuery query = fixture.CreateQuery();
        for (int i = 0; i < origins.Length; i++)
        {
            var direct = UnitSpatialIndexEquivalenceTests.FindNearestDirect(
                fixture.Entries.AsNativeArray(),
                origins[i],
                rangeCells: 96,
                FactionIdentity.PlayerFactionId);
            var indexed = UnitSpatialIndexEquivalenceTests.FindNearestIndexed(
                query,
                origins[i],
                rangeCells: 96,
                FactionIdentity.PlayerFactionId);
            Assert.AreEqual(direct.Length, indexed.Length);
            for (int rank = 0; rank < direct.Length; rank++)
                Assert.AreEqual(direct[rank].SourceOrder, indexed[rank].SourceOrder);
        }
    }

    private static void Populate(UnitSpatialIndexEquivalenceTests.IndexFixture fixture)
    {
        var random = new Unity.Mathematics.Random(0xA704u);
        for (int i = 0; i < EntryCount; i++)
        {
            int2 cell = random.NextInt2(int2.zero, new int2(GridSize));
            fixture.Add(new UnitSpatialIndexEntry
            {
                Entity = new Unity.Entities.Entity { Index = i + 1, Version = 1 },
                SourceOrder = i,
                Cell = cell,
                Position = new float3(cell.x + 0.5f, 0f, cell.y + 0.5f),
                SelectionPosition = new float3(cell.x + 0.5f, 0f, cell.y + 0.5f),
                HealthCurrent = 100,
                HealthMax = 100,
                FactionId = (byte)((i & 1) == 0
                    ? FactionIdentity.EnemyFactionId
                    : FactionIdentity.PlayerFactionId),
                Flags = UnitSpatialIndexFlags.HasHealth |
                        UnitSpatialIndexFlags.HasLocalTransform |
                        UnitSpatialIndexFlags.HasLocalToWorld |
                        UnitSpatialIndexFlags.Selectable
            });
        }
        fixture.Rebuild();
    }
}
