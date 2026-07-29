using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace Game.Rendering
{
    [BurstCompile]
    internal static class OperationMapRenderVirtualizationProfilerMarkers
    {
        internal const string InitializeName =
            "OperationMapRenderVirtualization.Initialize";
        internal const string SelectCellsName =
            "OperationMapRenderVirtualization.SelectCells";
        internal const string AssignSlotsName =
            "OperationMapRenderVirtualization.AssignSlots";
        internal const string ApplySlotsName =
            "OperationMapRenderVirtualization.ApplySlots";
        internal const string SyncStateName =
            "OperationMapRenderVirtualization.SyncState";

        internal static readonly ProfilerMarker Initialize =
            new(InitializeName);
        internal static readonly ProfilerMarker SelectCells =
            new(SelectCellsName);
        internal static readonly ProfilerMarker AssignSlots =
            new(AssignSlotsName);
        internal static readonly ProfilerMarker ApplySlots =
            new(ApplySlotsName);
        internal static readonly ProfilerMarker SyncState =
            new(SyncStateName);
    }

    internal enum OperationMapRenderMetricsFailure : byte
    {
        None = 0,
        InvalidStaticCounts = 1,
        InvalidSlotCounts = 2,
        InvalidActivityCounts = 3,
        InvalidAssignmentCounts = 4,
        InvalidRebuildReason = 5
    }

    internal struct OperationMapRenderMetricsSnapshot
    {
        internal int LogicalPlacementCount;
        internal int LogicalPartCount;
        internal int ResidentExceptionCount;
        internal int Capacity;
        internal int EnabledSlotCount;
        internal int RetainedCount;
        internal int ReleasedCount;
        internal int ReboundCount;
        internal int ActiveCellCount;
        internal int ActivePlacementCount;
        internal int OverflowCount;
        internal int HighestDeficit;
        internal uint CommandVersion;
        internal OperationMapRenderRebuildReason RebuildReason;
    }

    [BurstCompile]
    internal struct OperationMapRenderVirtualizationMetricsJob : IJob
    {
        internal OperationMapRenderMetricsSnapshot Snapshot;
        internal NativeReference<OperationMapRenderVirtualizationMetricsComponent>
            Metrics;
        internal NativeReference<OperationMapRenderMetricsFailure> Failure;

        [BurstCompile]
        public void Execute()
        {
            if (!OperationMapRenderMetricsProjection.TryProject(
                    Snapshot,
                    out OperationMapRenderVirtualizationMetricsComponent metrics,
                    out OperationMapRenderMetricsFailure failure))
            {
                Metrics.Value = default;
                Failure.Value = failure;
                return;
            }
            Metrics.Value = metrics;
            Failure.Value = OperationMapRenderMetricsFailure.None;
        }
    }

    internal static class OperationMapRenderMetricsProjection
    {
        internal static bool TryProject(
            in OperationMapRenderMetricsSnapshot snapshot,
            out OperationMapRenderVirtualizationMetricsComponent metrics,
            out OperationMapRenderMetricsFailure failure)
        {
            metrics = default;
            failure = OperationMapRenderMetricsFailure.None;
            if (snapshot.LogicalPlacementCount <= 0 ||
                snapshot.LogicalPartCount <= 0 ||
                snapshot.ResidentExceptionCount < 0 ||
                snapshot.Capacity <= 0)
            {
                failure = OperationMapRenderMetricsFailure.InvalidStaticCounts;
                return false;
            }
            if (snapshot.EnabledSlotCount < 0 ||
                snapshot.EnabledSlotCount > snapshot.Capacity)
            {
                failure = OperationMapRenderMetricsFailure.InvalidSlotCounts;
                return false;
            }
            if (snapshot.ActiveCellCount < 0 ||
                snapshot.ActivePlacementCount < 0 ||
                snapshot.ActivePlacementCount >
                    snapshot.LogicalPlacementCount)
            {
                failure = OperationMapRenderMetricsFailure.InvalidActivityCounts;
                return false;
            }
            if (snapshot.RetainedCount < 0 ||
                snapshot.ReleasedCount < 0 ||
                snapshot.ReboundCount < 0 ||
                snapshot.OverflowCount < 0 ||
                snapshot.HighestDeficit < 0 ||
                (snapshot.OverflowCount == 0 &&
                 snapshot.HighestDeficit != 0) ||
                (snapshot.OverflowCount > 0 &&
                 (snapshot.HighestDeficit == 0 ||
                  snapshot.HighestDeficit > snapshot.OverflowCount)))
            {
                failure =
                    OperationMapRenderMetricsFailure.InvalidAssignmentCounts;
                return false;
            }
            if (snapshot.RebuildReason <
                    OperationMapRenderRebuildReason.None ||
                snapshot.RebuildReason >
                    OperationMapRenderRebuildReason.MapGenerationChanged)
            {
                failure = OperationMapRenderMetricsFailure.InvalidRebuildReason;
                return false;
            }

            metrics = new OperationMapRenderVirtualizationMetricsComponent
            {
                LogicalPlacementCount = snapshot.LogicalPlacementCount,
                LogicalPartCount = snapshot.LogicalPartCount,
                ResidentExceptionCount = snapshot.ResidentExceptionCount,
                Capacity = snapshot.Capacity,
                EnabledSlotCount = snapshot.EnabledSlotCount,
                DisabledSlotCount =
                    snapshot.Capacity - snapshot.EnabledSlotCount,
                RetainedCount = snapshot.RetainedCount,
                ReleasedCount = snapshot.ReleasedCount,
                ReboundCount = snapshot.ReboundCount,
                ActiveCellCount = snapshot.ActiveCellCount,
                ActivePlacementCount = snapshot.ActivePlacementCount,
                OverflowCount = snapshot.OverflowCount,
                HighestDeficit = snapshot.HighestDeficit,
                CommandVersion = snapshot.CommandVersion,
                RebuildReason = snapshot.RebuildReason
            };
            return true;
        }
    }

    internal static class OperationMapRenderMetricsFormatter
    {
        internal static bool TryFormat(
            bool diagnosticsEnabled,
            in OperationMapRenderVirtualizationMetricsComponent metrics,
            out FixedString512Bytes text)
        {
            text = default;
            if (!diagnosticsEnabled)
                return false;

            text.Append("placements=");
            text.Append(metrics.LogicalPlacementCount);
            text.Append(" parts=");
            text.Append(metrics.LogicalPartCount);
            text.Append(" resident=");
            text.Append(metrics.ResidentExceptionCount);
            text.Append(" slots=");
            text.Append(metrics.EnabledSlotCount);
            text.Append('/');
            text.Append(metrics.Capacity);
            text.Append(" activeCells=");
            text.Append(metrics.ActiveCellCount);
            text.Append(" activePlacements=");
            text.Append(metrics.ActivePlacementCount);
            text.Append(" retained=");
            text.Append(metrics.RetainedCount);
            text.Append(" released=");
            text.Append(metrics.ReleasedCount);
            text.Append(" rebound=");
            text.Append(metrics.ReboundCount);
            text.Append(" overflow=");
            text.Append(metrics.OverflowCount);
            text.Append(" deficit=");
            text.Append(metrics.HighestDeficit);
            text.Append(" reason=");
            text.Append((int)metrics.RebuildReason);
            text.Append(" commandVersion=");
            text.Append(metrics.CommandVersion);
            return true;
        }
    }
}
