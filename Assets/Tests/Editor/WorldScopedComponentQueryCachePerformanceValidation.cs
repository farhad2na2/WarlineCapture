#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class WorldScopedComponentQueryCachePerformanceValidation
{
    private const int GovernedCombinationCount = 2;
    private const int WarmupOperations = 180;
    private const int MeasuredOperations = 300;

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new WorldScopedComponentQueryCachePerformanceValidation();
            tests.GovernedCaches_ReuseAndRebindWithZeroRecurringManagedAllocation();
            Debug.Log(
                $"[WorldScopedComponentQueryCachePerformanceValidation] result=Passed tests=1 combinations={GovernedCombinationCount} phases={GovernedCombinationCount * 2}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[WorldScopedComponentQueryCachePerformanceValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void GovernedCaches_ReuseAndRebindWithZeroRecurringManagedAllocation()
    {
        Assert.GreaterOrEqual(WarmupOperations, 180);
        Assert.GreaterOrEqual(MeasuredOperations, 300);
        Assert.Greater(Stopwatch.Frequency, 0L);
        AssertGovernedConsumerMatrix();

        var resourceConsumer = new FactionResourceCompositionSystemHelper();
        WorldScopedComponentQueryCache<BuildingResourceStorageComponent> storageCache =
            GetConfiguredCache<BuildingResourceStorageComponent>(
                resourceConsumer,
                "_storageQueryCache",
                expectedReadOnly: false);
        ValidateCombination(
            nameof(FactionResourceCompositionSystemHelper),
            nameof(BuildingResourceStorageComponent),
            readOnly: false,
            storageCache);

        var haulerConsumer = new BuildingResourceHaulerBridgeCompositionSystemHelper();
        WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent> moveOrderCache =
            GetConfiguredCache<UnitMoveOrderQueueComponent>(
                haulerConsumer,
                "_moveOrderQueueQueryCache",
                expectedReadOnly: true);
        ValidateCombination(
            nameof(BuildingResourceHaulerBridgeCompositionSystemHelper),
            nameof(UnitMoveOrderQueueComponent),
            readOnly: true,
            moveOrderCache);
    }

    private static void AssertGovernedConsumerMatrix()
    {
        Type cacheTypeDefinition = typeof(WorldScopedComponentQueryCache<>);
        Type[] runtimeTypes = cacheTypeDefinition.Assembly.GetTypes();
        int governedCount = 0;
        int storageCount = 0;
        int moveOrderCount = 0;
        int unexpectedCount = 0;

        foreach (Type runtimeType in runtimeTypes)
        {
            FieldInfo[] fields = runtimeType.GetFields(
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);
            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                if (!fieldType.IsGenericType || fieldType.GetGenericTypeDefinition() != cacheTypeDefinition)
                    continue;

                governedCount++;
                Type componentType = fieldType.GetGenericArguments()[0];
                if (runtimeType == typeof(FactionResourceCompositionSystemHelper) &&
                    field.Name == "_storageQueryCache" &&
                    componentType == typeof(BuildingResourceStorageComponent))
                {
                    storageCount++;
                }
                else if (runtimeType == typeof(BuildingResourceHaulerBridgeCompositionSystemHelper) &&
                         field.Name == "_moveOrderQueueQueryCache" &&
                         componentType == typeof(UnitMoveOrderQueueComponent))
                {
                    moveOrderCount++;
                }
                else
                {
                    unexpectedCount++;
                }
            }
        }

        Assert.AreEqual(
            GovernedCombinationCount,
            governedCount,
            "Every runtime WorldScopedComponentQueryCache consumer must be explicitly covered by this validation.");
        Assert.AreEqual(1, storageCount, "The resource-storage cache consumer must exist exactly once.");
        Assert.AreEqual(1, moveOrderCount, "The move-order cache consumer must exist exactly once.");
        Assert.AreEqual(0, unexpectedCount, "An undeclared WorldScopedComponentQueryCache consumer was found.");
    }

    private static WorldScopedComponentQueryCache<T> GetConfiguredCache<T>(
        object consumer,
        string fieldName,
        bool expectedReadOnly)
        where T : unmanaged, IComponentData
    {
        Type consumerType = consumer.GetType();
        FieldInfo cacheField = consumerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(cacheField, $"{consumerType.Name}.{fieldName} must remain the governed cache field.");
        Assert.AreEqual(typeof(WorldScopedComponentQueryCache<T>), cacheField.FieldType);

        var cache = (WorldScopedComponentQueryCache<T>)cacheField.GetValue(consumer);
        Assert.IsNotNull(cache, $"{consumerType.Name}.{fieldName} must initialize its cache.");

        FieldInfo accessField = typeof(WorldScopedComponentQueryCache<T>).GetField(
            "_readOnly",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(accessField, "The cache access-mode contract must remain inspectable.");
        Assert.AreEqual(
            expectedReadOnly,
            (bool)accessField.GetValue(cache),
            $"{consumerType.Name}.{fieldName} changed its governed access mode.");
        return cache;
    }

    private static void ValidateCombination<T>(
        string consumerName,
        string componentName,
        bool readOnly,
        WorldScopedComponentQueryCache<T> cache)
        where T : unmanaged, IComponentData
    {
        using World firstWorld = new($"{nameof(WorldScopedComponentQueryCachePerformanceValidation)}_{componentName}_First");
        using World secondWorld = new($"{nameof(WorldScopedComponentQueryCachePerformanceValidation)}_{componentName}_Second");
        PopulateWorld<T>(firstWorld.EntityManager, targetCount: 1);
        PopulateWorld<T>(secondWorld.EntityManager, targetCount: 2);

        EntityQuery firstQuery = cache.Get(firstWorld.EntityManager);
        AssertQueryContract<T>(firstQuery, readOnly, expectedEntityCount: 1);
        ReuseMetrics firstMetrics = MeasureSameWorldReuse(cache, firstWorld.EntityManager, firstQuery);
        AssertMetrics(firstMetrics, consumerName, componentName, "initial-world");

        EntityQuery secondQuery = cache.Get(secondWorld.EntityManager);
        Assert.IsFalse(
            firstQuery.Equals(secondQuery),
            $"{consumerName}/{componentName} must rebuild its query when the World changes.");
        AssertQueryContract<T>(secondQuery, readOnly, expectedEntityCount: 2);
        ReuseMetrics secondMetrics = MeasureSameWorldReuse(cache, secondWorld.EntityManager, secondQuery);
        AssertMetrics(secondMetrics, consumerName, componentName, "rebound-world");

        LogMetrics(consumerName, componentName, readOnly, "initial-world", firstMetrics);
        LogMetrics(consumerName, componentName, readOnly, "rebound-world", secondMetrics);
    }

    private static void PopulateWorld<T>(EntityManager entityManager, int targetCount)
        where T : unmanaged, IComponentData
    {
        for (int i = 0; i < targetCount; i++)
            entityManager.CreateEntity(typeof(T));

        entityManager.CreateEntity(typeof(UnitGrid));
    }

    private static void AssertQueryContract<T>(EntityQuery query, bool readOnly, int expectedEntityCount)
        where T : unmanaged, IComponentData
    {
        Assert.AreEqual(expectedEntityCount, query.CalculateEntityCount());

        var expectedQuery = new EntityQueryBuilder(Allocator.Temp);
        try
        {
            if (readOnly)
                expectedQuery.WithAll<T>();
            else
                expectedQuery.WithAllRW<T>();

            Assert.IsTrue(
                query.CompareQuery(in expectedQuery),
                $"The {typeof(T).Name} query must preserve its governed access mode and component shape.");
        }
        finally
        {
            expectedQuery.Dispose();
        }
    }

    private static ReuseMetrics MeasureSameWorldReuse<T>(
        WorldScopedComponentQueryCache<T> cache,
        EntityManager entityManager,
        EntityQuery expectedQuery)
        where T : unmanaged, IComponentData
    {
        var samples = new long[MeasuredOperations];
        EntityQuery lastQuery = default;

        _ = Stopwatch.GetTimestamp();
        _ = GC.GetAllocatedBytesForCurrentThread();
        for (int operation = 0; operation < WarmupOperations; operation++)
            lastQuery = cache.Get(entityManager);

        Assert.IsTrue(expectedQuery.Equals(lastQuery), "Warmup must remain on the same cached query.");

        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int operation = 0; operation < MeasuredOperations; operation++)
        {
            long startTicks = Stopwatch.GetTimestamp();
            lastQuery = cache.Get(entityManager);
            samples[operation] = Stopwatch.GetTimestamp() - startTicks;
        }

        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
        long totalTicks = 0L;
        for (int i = 0; i < samples.Length; i++)
            totalTicks += samples[i];

        Array.Sort(samples);
        return new ReuseMetrics(
            samples.Length,
            totalTicks,
            PercentileNearestRank(samples, 95),
            PercentileNearestRank(samples, 99),
            samples[samples.Length - 1],
            allocatedBytes,
            expectedQuery,
            lastQuery);
    }

    private static long PercentileNearestRank(long[] sortedSamples, int percentile)
    {
        int rank = (sortedSamples.Length * percentile + 99) / 100;
        return sortedSamples[Math.Max(0, rank - 1)];
    }

    private static void AssertMetrics(
        ReuseMetrics metrics,
        string consumerName,
        string componentName,
        string phase)
    {
        string context = $"{consumerName}/{componentName}/{phase}";
        Assert.AreEqual(MeasuredOperations, metrics.SampleCount, $"{context} must emit every timing sample.");
        Assert.AreEqual(0L, metrics.AllocatedBytes, $"{context} must allocate exactly zero recurring managed bytes.");
        Assert.Greater(metrics.TotalTicks, 0L, $"{context} must emit a non-empty timing measurement.");
        Assert.GreaterOrEqual(metrics.P95Ticks, 0L);
        Assert.GreaterOrEqual(metrics.P99Ticks, metrics.P95Ticks);
        Assert.GreaterOrEqual(metrics.MaxTicks, metrics.P99Ticks);
        Assert.GreaterOrEqual(metrics.TotalTicks, metrics.MaxTicks);
        Assert.IsTrue(metrics.ExpectedQuery.Equals(metrics.LastQuery), $"{context} must reuse the same query instance.");
    }

    private static void LogMetrics(
        string consumerName,
        string componentName,
        bool readOnly,
        string phase,
        ReuseMetrics metrics)
    {
        double averageNanoseconds = TicksToNanoseconds((double)metrics.TotalTicks / metrics.SampleCount);
        double p95Nanoseconds = TicksToNanoseconds(metrics.P95Ticks);
        double p99Nanoseconds = TicksToNanoseconds(metrics.P99Ticks);
        double maxNanoseconds = TicksToNanoseconds(metrics.MaxTicks);
        string access = readOnly ? "ReadOnly" : "ReadWrite";
        Debug.Log(
            $"[WorldScopedComponentQueryCachePerformanceValidation] consumer={consumerName} component={componentName} access={access} phase={phase} " +
            $"warmupOperations={WarmupOperations} measuredOperations={MeasuredOperations} sampleCount={metrics.SampleCount} " +
            $"stopwatchFrequency={Stopwatch.Frequency} totalTicks={metrics.TotalTicks} " +
            $"averageNs={averageNanoseconds.ToString("0.###", CultureInfo.InvariantCulture)} " +
            $"p95Ticks={metrics.P95Ticks} p95Ns={p95Nanoseconds.ToString("0.###", CultureInfo.InvariantCulture)} " +
            $"p99Ticks={metrics.P99Ticks} p99Ns={p99Nanoseconds.ToString("0.###", CultureInfo.InvariantCulture)} " +
            $"maxTicks={metrics.MaxTicks} maxNs={maxNanoseconds.ToString("0.###", CultureInfo.InvariantCulture)} " +
            $"allocatedBytes={metrics.AllocatedBytes}");
    }

    private static double TicksToNanoseconds(double ticks)
    {
        return ticks * 1_000_000_000d / Stopwatch.Frequency;
    }

    private readonly struct ReuseMetrics
    {
        public readonly int SampleCount;
        public readonly long TotalTicks;
        public readonly long P95Ticks;
        public readonly long P99Ticks;
        public readonly long MaxTicks;
        public readonly long AllocatedBytes;
        public readonly EntityQuery ExpectedQuery;
        public readonly EntityQuery LastQuery;

        public ReuseMetrics(
            int sampleCount,
            long totalTicks,
            long p95Ticks,
            long p99Ticks,
            long maxTicks,
            long allocatedBytes,
            EntityQuery expectedQuery,
            EntityQuery lastQuery)
        {
            SampleCount = sampleCount;
            TotalTicks = totalTicks;
            P95Ticks = p95Ticks;
            P99Ticks = p99Ticks;
            MaxTicks = maxTicks;
            AllocatedBytes = allocatedBytes;
            ExpectedQuery = expectedQuery;
            LastQuery = lastQuery;
        }
    }
}
#endif
