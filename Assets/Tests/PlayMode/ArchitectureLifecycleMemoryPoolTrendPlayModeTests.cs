using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class ArchitectureLifecycleMemoryPoolTrendPlayModeTests
{
    private const int WarmupCycleCount = 1;
    private const int MeasuredCycleCount = 5;
    private const long OneMebibyte = 1024L * 1024L;
    private const long MonoMedianDeltaLimitBytes = OneMebibyte;
    private const long AllocatedMedianDeltaLimitBytes = 4L * OneMebibyte;
    private const long ReservedMedianDeltaLimitBytes = 8L * OneMebibyte;
    private const long MonoSlopeLimitBytesPerCycle = 64L * 1024L;
    private const long AllocatedSlopeLimitBytesPerCycle = 256L * 1024L;

    [UnityTest]
    [Timeout(240000)]
    public IEnumerator FiveMeasuredCycles_PreserveStructuralAndPoolPlateausAndReportMemoryTrend()
    {
        var context = new Aph805MenuMatchMenuLifecyclePlayModeTests.TransitionContext();
        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.PrepareStableMenu(context);

        using var collector = new ArchitectureMenuMatchLifecycleSnapshotCollector(
            context.World,
            context.Menu.ContentSystem);
        for (int warmup = 0; warmup < WarmupCycleCount; warmup++)
        {
            yield return Aph805MenuMatchMenuLifecyclePlayModeTests.EnterStableMatch(context);
            yield return Aph805MenuMatchMenuLifecyclePlayModeTests.ReturnToStableMenu(context);
        }

        var menuSnapshots = new List<ArchitectureMenuMatchLifecycleSnapshot>(MeasuredCycleCount);
        var matchSnapshots = new List<ArchitectureMenuMatchLifecycleSnapshot>(MeasuredCycleCount);
        for (int cycle = 1; cycle <= MeasuredCycleCount; cycle++)
        {
            yield return Aph805MenuMatchMenuLifecyclePlayModeTests.EnterStableMatch(context);
            ArchitectureMenuMatchLifecycleSnapshot match = collector.Capture(
                cycle,
                ArchitectureLifecycleCheckpointPhase.Match);
            AssertRequiredCounters(match);
            matchSnapshots.Add(match);

            yield return Aph805MenuMatchMenuLifecyclePlayModeTests.ReturnToStableMenu(context);
            ArchitectureMenuMatchLifecycleSnapshot menu = collector.Capture(
                cycle,
                ArchitectureLifecycleCheckpointPhase.Menu);
            AssertRequiredCounters(menu);
            menuSnapshots.Add(menu);
        }

        AssertStructuralPlateau(matchSnapshots);
        AssertStructuralPlateau(menuSnapshots);
        ReportMemoryTrend("Match", matchSnapshots);
        ReportMemoryTrend("Menu", menuSnapshots);

        TestContext.Out.WriteLine($"Match baseline: {matchSnapshots[0].ToCompactString()}");
        TestContext.Out.WriteLine($"Match final: {matchSnapshots[^1].ToCompactString()}");
        TestContext.Out.WriteLine($"Menu baseline: {menuSnapshots[0].ToCompactString()}");
        TestContext.Out.WriteLine($"Menu final: {menuSnapshots[^1].ToCompactString()}");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.EnsureMatchIsUnloaded();
    }

    private static void AssertRequiredCounters(ArchitectureMenuMatchLifecycleSnapshot snapshot)
    {
        Assert.That(snapshot.AudioRuntimeViewCount, Is.EqualTo(1), snapshot.ToCompactString());
        Assert.That(snapshot.AudioPoolSize, Is.GreaterThan(0), snapshot.ToCompactString());
        Assert.That(snapshot.TotalAllocatedMemoryBytes, Is.GreaterThan(0), snapshot.ToCompactString());
        Assert.That(snapshot.TotalReservedMemoryBytes, Is.GreaterThan(0), snapshot.ToCompactString());
        Assert.That(snapshot.MonoUsedMemoryBytes, Is.GreaterThan(0), snapshot.ToCompactString());
        Assert.That(snapshot.MonoHeapMemoryBytes, Is.GreaterThan(0), snapshot.ToCompactString());
        if (snapshot.Phase == ArchitectureLifecycleCheckpointPhase.Match)
        {
            Assert.That(snapshot.PathPoolOwnerCount, Is.EqualTo(1), snapshot.ToCompactString());
            Assert.That(snapshot.PathPoolCapacity, Is.GreaterThanOrEqualTo(snapshot.PathPoolLength), snapshot.ToCompactString());
        }
        else
        {
            Assert.That(snapshot.PathPoolOwnerCount, Is.Zero, snapshot.ToCompactString());
        }
    }

    private static void AssertStructuralPlateau(
        IReadOnlyList<ArchitectureMenuMatchLifecycleSnapshot> snapshots)
    {
        ArchitectureMenuMatchLifecycleSnapshot baseline = snapshots[0];
        for (int index = 1; index < snapshots.Count; index++)
        {
            ArchitectureMenuMatchLifecycleSnapshot actual = snapshots[index];
            string message = $"Expected [{baseline.ToCompactString()}] but observed [{actual.ToCompactString()}].";
            Assert.That(actual.Phase, Is.EqualTo(baseline.Phase), message);
            Assert.That(actual.WorldSequence, Is.EqualTo(baseline.WorldSequence), message);
            Assert.That(actual.LoadedSceneCount, Is.EqualTo(baseline.LoadedSceneCount), message);
            Assert.That(actual.SceneRootCount, Is.EqualTo(baseline.SceneRootCount), message);
            Assert.That(actual.TotalEntityCount, Is.EqualTo(baseline.TotalEntityCount), message);
            Assert.That(actual.ManagedSystemCount, Is.EqualTo(baseline.ManagedSystemCount), message);
            Assert.That(actual.ShellFlowSystemHandle, Is.EqualTo(baseline.ShellFlowSystemHandle), message);
            Assert.That(actual.ActionRequestSystemHandle, Is.EqualTo(baseline.ActionRequestSystemHandle), message);
            Assert.That(actual.LifecycleRootCount, Is.EqualTo(baseline.LifecycleRootCount), message);
            Assert.That(actual.OperationMapRootCount, Is.EqualTo(baseline.OperationMapRootCount), message);
            Assert.That(actual.MenuViewCount, Is.EqualTo(baseline.MenuViewCount), message);
            Assert.That(actual.MatchViewCount, Is.EqualTo(baseline.MatchViewCount), message);
            Assert.That(actual.MatchHudCount, Is.EqualTo(baseline.MatchHudCount), message);
            Assert.That(actual.EnabledAudioListenerCount, Is.EqualTo(baseline.EnabledAudioListenerCount), message);
            Assert.That(actual.MissileTrailRootCount, Is.EqualTo(baseline.MissileTrailRootCount), message);
            Assert.That(actual.AudioRuntimeViewCount, Is.EqualTo(baseline.AudioRuntimeViewCount), message);
            Assert.That(actual.AudioPoolSize, Is.EqualTo(baseline.AudioPoolSize), message);
            Assert.That(actual.ActiveAudioSourceCount, Is.EqualTo(baseline.ActiveAudioSourceCount), message);
            Assert.That(actual.PathPoolOwnerCount, Is.EqualTo(baseline.PathPoolOwnerCount), message);
            Assert.That(actual.PathPoolLength, Is.EqualTo(baseline.PathPoolLength), message);
            Assert.That(actual.PathPoolCapacity, Is.EqualTo(baseline.PathPoolCapacity), message);
            Assert.That(actual.MissileTrailCreatedCount, Is.EqualTo(baseline.MissileTrailCreatedCount), message);
            Assert.That(actual.MissileTrailActiveCount, Is.EqualTo(baseline.MissileTrailActiveCount), message);
            Assert.That(actual.ImpactVfxCreatedCount, Is.EqualTo(baseline.ImpactVfxCreatedCount), message);
            Assert.That(actual.ImpactVfxActiveCount, Is.EqualTo(baseline.ImpactVfxActiveCount), message);
        }
    }

    private static void ReportMemoryTrend(
        string phase,
        IReadOnlyList<ArchitectureMenuMatchLifecycleSnapshot> snapshots)
    {
        long[] allocated = Select(snapshots, snapshot => snapshot.TotalAllocatedMemoryBytes);
        long[] reserved = Select(snapshots, snapshot => snapshot.TotalReservedMemoryBytes);
        long[] monoUsed = Select(snapshots, snapshot => snapshot.MonoUsedMemoryBytes);

        ReportMedianDelta(phase, "Mono used", monoUsed, MonoMedianDeltaLimitBytes);
        ReportMedianDelta(phase, "total allocated", allocated, AllocatedMedianDeltaLimitBytes);
        ReportMedianDelta(phase, "total reserved", reserved, ReservedMedianDeltaLimitBytes);
        ReportSlope(phase, "Mono used", monoUsed, MonoSlopeLimitBytesPerCycle);
        ReportSlope(phase, "total allocated", allocated, AllocatedSlopeLimitBytesPerCycle);
    }

    private static void ReportMedianDelta(string phase, string counter, long[] samples, long limitBytes)
    {
        double firstMedian = ArchitectureLifecycleMemoryTrendUtilitySystemHelper.CalculateMedian(
            new[] { samples[0], samples[1] });
        double finalMedian = ArchitectureLifecycleMemoryTrendUtilitySystemHelper.CalculateMedian(
            new[] { samples[^2], samples[^1] });
        double delta = finalMedian - firstMedian;
        string status = delta <= limitBytes ? "within" : "exceeded";
        TestContext.Out.WriteLine(
            $"{phase} {counter} medianDeltaBytes={delta:F0} ceilingBytes={limitBytes} status={status}");
    }

    private static void ReportSlope(string phase, string counter, long[] samples, long limitBytesPerCycle)
    {
        double slope = ArchitectureLifecycleMemoryTrendUtilitySystemHelper
            .CalculateTheilSenSlopePerCycle(samples);
        string status = slope <= limitBytesPerCycle ? "within" : "exceeded";
        TestContext.Out.WriteLine(
            $"{phase} {counter} slopeBytesPerCycle={slope:F0} ceilingBytesPerCycle={limitBytesPerCycle} status={status}");
    }

    private static long[] Select(
        IReadOnlyList<ArchitectureMenuMatchLifecycleSnapshot> snapshots,
        System.Func<ArchitectureMenuMatchLifecycleSnapshot, long> selector)
    {
        var values = new long[snapshots.Count];
        for (int index = 0; index < snapshots.Count; index++)
            values[index] = selector(snapshots[index]);
        return values;
    }
}
