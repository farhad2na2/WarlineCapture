using Game.Components;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public sealed class OperationMapRenderVirtualizationInstrumentationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            MarkerNames_AreExactAndComplete();
            MetricsJob_ProjectsEveryBoundedCounter();
            InvalidMetrics_FailClosedAndClearOutput();
            DisabledFormatting_ReturnsBeforeAllocationOrConstruction();
            EnabledFormatting_IsFixedAndComplete();
            Projection_IsAllocationFree();
            Debug.Log(
                "[OperationMapRenderVirtualizationInstrumentationValidation] " +
                "result=Passed tests=6");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[OperationMapRenderVirtualizationInstrumentationValidation] " +
                "result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public static void MarkerNames_AreExactAndComplete()
    {
        Assert.That(
            OperationMapRenderVirtualizationProfilerMarkers.InitializeName,
            Is.EqualTo("OperationMapRenderVirtualization.Initialize"));
        Assert.That(
            OperationMapRenderVirtualizationProfilerMarkers.SelectCellsName,
            Is.EqualTo("OperationMapRenderVirtualization.SelectCells"));
        Assert.That(
            OperationMapRenderVirtualizationProfilerMarkers.AssignSlotsName,
            Is.EqualTo("OperationMapRenderVirtualization.AssignSlots"));
        Assert.That(
            OperationMapRenderVirtualizationProfilerMarkers.ApplySlotsName,
            Is.EqualTo("OperationMapRenderVirtualization.ApplySlots"));
        Assert.That(
            OperationMapRenderVirtualizationProfilerMarkers.SyncStateName,
            Is.EqualTo("OperationMapRenderVirtualization.SyncState"));
    }

    [Test]
    public static void MetricsJob_ProjectsEveryBoundedCounter()
    {
        using var metrics =
            new NativeReference<OperationMapRenderVirtualizationMetricsComponent>(
                Allocator.TempJob);
        using var failure =
            new NativeReference<OperationMapRenderMetricsFailure>(
                Allocator.TempJob);
        var job = new OperationMapRenderVirtualizationMetricsJob
        {
            Snapshot = ValidSnapshot(),
            Metrics = metrics,
            Failure = failure
        };

        job.Schedule().Complete();

        Assert.That(failure.Value, Is.EqualTo(
            OperationMapRenderMetricsFailure.None));
        OperationMapRenderVirtualizationMetricsComponent value = metrics.Value;
        Assert.That(value.LogicalPlacementCount, Is.EqualTo(9721));
        Assert.That(value.LogicalPartCount, Is.EqualTo(11299));
        Assert.That(value.ResidentExceptionCount, Is.EqualTo(70710));
        Assert.That(value.Capacity, Is.EqualTo(704));
        Assert.That(value.EnabledSlotCount, Is.EqualTo(311));
        Assert.That(value.DisabledSlotCount, Is.EqualTo(393));
        Assert.That(value.RetainedCount, Is.EqualTo(280));
        Assert.That(value.ReleasedCount, Is.EqualTo(12));
        Assert.That(value.ReboundCount, Is.EqualTo(31));
        Assert.That(value.ActiveCellCount, Is.EqualTo(48));
        Assert.That(value.ActivePlacementCount, Is.EqualTo(299));
        Assert.That(value.OverflowCount, Is.EqualTo(3));
        Assert.That(value.HighestDeficit, Is.EqualTo(2));
        Assert.That(value.CommandVersion, Is.EqualTo(19));
        Assert.That(value.RebuildReason, Is.EqualTo(
            OperationMapRenderRebuildReason.CameraEnvelopeChanged));
    }

    [Test]
    public static void InvalidMetrics_FailClosedAndClearOutput()
    {
        OperationMapRenderMetricsSnapshot snapshot = ValidSnapshot();
        snapshot.EnabledSlotCount = snapshot.Capacity + 1;

        Assert.That(
            OperationMapRenderMetricsProjection.TryProject(
                snapshot,
                out OperationMapRenderVirtualizationMetricsComponent metrics,
                out OperationMapRenderMetricsFailure failure),
            Is.False);
        Assert.That(failure, Is.EqualTo(
            OperationMapRenderMetricsFailure.InvalidSlotCounts));
        Assert.That(metrics.Capacity, Is.Zero);
        Assert.That(metrics.EnabledSlotCount, Is.Zero);
    }

    [Test]
    public static void
        DisabledFormatting_ReturnsBeforeAllocationOrConstruction()
    {
        OperationMapRenderMetricsProjection.TryProject(
            ValidSnapshot(),
            out OperationMapRenderVirtualizationMetricsComponent metrics,
            out _);
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        bool formatted = false;
        FixedString512Bytes text = default;
        for (int index = 0; index < 10000; index++)
        {
            formatted |= OperationMapRenderMetricsFormatter.TryFormat(
                false,
                metrics,
                out text);
        }
        long after = System.GC.GetAllocatedBytesForCurrentThread();

        Assert.That(formatted, Is.False);
        Assert.That(text.Length, Is.Zero);
        Assert.That(after - before, Is.Zero);
    }

    [Test]
    public static void EnabledFormatting_IsFixedAndComplete()
    {
        OperationMapRenderMetricsProjection.TryProject(
            ValidSnapshot(),
            out OperationMapRenderVirtualizationMetricsComponent metrics,
            out _);

        Assert.That(
            OperationMapRenderMetricsFormatter.TryFormat(
                true,
                metrics,
                out FixedString512Bytes text),
            Is.True);

        string formatted = text.ToString();
        Assert.That(formatted, Does.Contain("placements=9721"));
        Assert.That(formatted, Does.Contain("parts=11299"));
        Assert.That(formatted, Does.Contain("resident=70710"));
        Assert.That(formatted, Does.Contain("slots=311/704"));
        Assert.That(formatted, Does.Contain("overflow=3"));
        Assert.That(formatted, Does.Contain("deficit=2"));
        Assert.That(formatted, Does.Contain("reason=2"));
        Assert.That(formatted, Does.Contain("commandVersion=19"));
        Assert.That(text.Length, Is.LessThanOrEqualTo(512));
    }

    [Test]
    public static void Projection_IsAllocationFree()
    {
        OperationMapRenderMetricsSnapshot snapshot = ValidSnapshot();
        OperationMapRenderMetricsProjection.TryProject(
            snapshot, out _, out _);
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        bool valid = true;
        for (int index = 0; index < 10000; index++)
        {
            valid &= OperationMapRenderMetricsProjection.TryProject(
                snapshot, out _, out _);
        }
        long after = System.GC.GetAllocatedBytesForCurrentThread();

        Assert.That(valid, Is.True);
        Assert.That(after - before, Is.Zero);
    }

    private static OperationMapRenderMetricsSnapshot ValidSnapshot() =>
        new OperationMapRenderMetricsSnapshot
        {
            LogicalPlacementCount = 9721,
            LogicalPartCount = 11299,
            ResidentExceptionCount = 70710,
            Capacity = 704,
            EnabledSlotCount = 311,
            RetainedCount = 280,
            ReleasedCount = 12,
            ReboundCount = 31,
            ActiveCellCount = 48,
            ActivePlacementCount = 299,
            OverflowCount = 3,
            HighestDeficit = 2,
            CommandVersion = 19,
            RebuildReason =
                OperationMapRenderRebuildReason.CameraEnvelopeChanged
        };
}
