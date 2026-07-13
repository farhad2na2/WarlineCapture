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
    private const double MaxAcquisitionMilliseconds = 1.025d;
    private const double MinimumRelativeImprovementPercent = 10d;
    private const long MaxPayloadBytes = 256L * 1024L;
    private static int s_benchmarkSink;

    [Test]
    public void BoundedLinkedCellBenchmark_BeatsDirectGeometryScanWithoutManagedAllocation()
    {
        using var fixture = new UnitSpatialIndexEquivalenceTests.IndexFixture(
            EntryCount,
            GridSize,
            GridSize);
        Populate(fixture);
        var origins = new int2[TowerCount];
        for (int i = 0; i < origins.Length; i++)
            origins[i] = new int2(512 + i * 11, 768 + i * 7);

        AssertEquivalent(fixture, origins, version: 1u);
        for (int i = 0; i < WarmupIterations; i++)
        {
            RunDirect(fixture, origins);
            RunIndexed(fixture, origins, (uint)i + 2u);
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

        fixture.Rebuild(version: 1000u, builtAtElapsedTime: 1000d);
        long payloadBytes =
            (long)fixture.Entries.Length * UnsafeUtility.SizeOf<UnitSpatialIndexEntry>() +
            (long)fixture.Heads.Length * UnsafeUtility.SizeOf<UnitSpatialIndexBucketHead>();
        double tickToMs = 1000d / Stopwatch.Frequency;
        double directAcquisitionMs = directTicks * tickToMs / MeasuredIterations;
        double indexedAcquisitionMs = indexedTicks * tickToMs / MeasuredIterations;
        double relativeImprovementPercent = (directTicks - indexedTicks) * 100d / directTicks;

        Assert.AreEqual(0, allocatedBytes, "The warmed fixed-head build/query path must not allocate managed memory.");
        Assert.AreEqual(18_784L, payloadBytes, "The 740-entry payload must remain 24-byte entries plus 256 integer heads.");
        Assert.LessOrEqual(payloadBytes, MaxPayloadBytes, "The candidate exceeds the APH-704 payload ceiling.");
        Assert.AreEqual(0, fixture.State.OverflowCount);
        Assert.AreEqual(EntryCount, fixture.State.EntryCount);
        Assert.AreEqual(UnitSpatialIndexBuildSystem.BucketHeadCount, fixture.State.BucketCount);
        Assert.Greater(directTicks, 0);
        Assert.Greater(indexedTicks, 0);
        Assert.Less(indexedAcquisitionMs, MaxAcquisitionMilliseconds,
            "The complete build-plus-query acquisition must remain below the fixed APH-704 gate.");
        Assert.GreaterOrEqual(relativeImprovementPercent, MinimumRelativeImprovementPercent,
            "The complete build-plus-query acquisition must improve the direct baseline by at least ten percent.");

        Debug.Log(
            $"[UnitSpatialIndexBoundedBenchmark] bucketSize={UnitSpatialIndexBuildSystem.BucketSizeCells} " +
            $"heads={UnitSpatialIndexBuildSystem.BucketHeadCount} entries={EntryCount} towers={TowerCount} " +
            $"warmup={WarmupIterations} measured={MeasuredIterations} " +
            $"directAcquisitionMs={directAcquisitionMs:0.###} " +
            $"indexedBuildAndQueryAcquisitionMs={indexedAcquisitionMs:0.###} " +
            $"relativeImprovementPercent={relativeImprovementPercent:0.##} " +
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
            UnitSpatialIndexEquivalenceTests.NearestFour result =
                UnitSpatialIndexEquivalenceTests.FindNearestDirect(
                    fixture.Entries.AsNativeArray(),
                    origins[i],
                    rangeCells: 96);
            checksum += result.Count > 0 ? result.Order0 : 0;
        }

        s_benchmarkSink = checksum;
    }

    private static void RunIndexed(
        UnitSpatialIndexEquivalenceTests.IndexFixture fixture,
        int2[] origins,
        uint version)
    {
        fixture.Rebuild(version, builtAtElapsedTime: version);
        UnitSpatialIndexQuery query = fixture.CreateQuery();
        int checksum = 0;
        for (int i = 0; i < origins.Length; i++)
        {
            UnitSpatialIndexEquivalenceTests.NearestFour result =
                UnitSpatialIndexEquivalenceTests.FindNearestIndexed(
                    query,
                    origins[i],
                    rangeCells: 96);
            checksum += result.Count > 0 ? result.Order0 : 0;
        }

        s_benchmarkSink = checksum;
    }

    private static void AssertEquivalent(
        UnitSpatialIndexEquivalenceTests.IndexFixture fixture,
        int2[] origins,
        uint version)
    {
        fixture.Rebuild(version, builtAtElapsedTime: version);
        UnitSpatialIndexQuery query = fixture.CreateQuery();
        for (int i = 0; i < origins.Length; i++)
        {
            UnitSpatialIndexEquivalenceTests.NearestFour direct =
                UnitSpatialIndexEquivalenceTests.FindNearestDirect(
                    fixture.Entries.AsNativeArray(),
                    origins[i],
                    rangeCells: 96);
            UnitSpatialIndexEquivalenceTests.NearestFour indexed =
                UnitSpatialIndexEquivalenceTests.FindNearestIndexed(
                    query,
                    origins[i],
                    rangeCells: 96);
            Assert.AreEqual(direct.Count, indexed.Count);
            Assert.AreEqual(direct.Order0, indexed.Order0);
            Assert.AreEqual(direct.Order1, indexed.Order1);
            Assert.AreEqual(direct.Order2, indexed.Order2);
            Assert.AreEqual(direct.Order3, indexed.Order3);
        }
    }

    private static void Populate(UnitSpatialIndexEquivalenceTests.IndexFixture fixture)
    {
        var random = new Unity.Mathematics.Random(0xA704u);
        for (int i = 0; i < EntryCount; i++)
        {
            fixture.Add(new UnitSpatialIndexEntry
            {
                Entity = new Unity.Entities.Entity { Index = i + 1, Version = 1 },
                Cell = random.NextInt2(int2.zero, new int2(GridSize)),
                SourceOrder = i,
                NextEntryIndex = UnitSpatialIndexBuilder.InvalidEntryIndex
            });
        }

        fixture.Rebuild();
    }
}
