using System;
using System.Collections.Generic;
using Game.Components;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class AndroidPerformanceRecorder
    {
        private const string RenderVirtualizationMetricsCommandLineArgument =
            "-warlineVrp053Metrics";
        private const double RenderVirtualizationMetricsIntervalSeconds = 1d;

        private bool _renderVirtualizationMetricsEnabled;
        private bool _renderVirtualizationMetricsObserved;
        private uint _lastRenderVirtualizationCommandVersion;
        private double _nextRenderVirtualizationMetricsSeconds;

        internal bool ShouldSampleRenderVirtualizationMetrics =>
            _renderVirtualizationMetricsEnabled &&
            Time.realtimeSinceStartupAsDouble >=
            _nextRenderVirtualizationMetricsSeconds;

        private void InitializeRenderVirtualizationMetrics(
            IReadOnlyList<string> commandLineArguments)
        {
            _renderVirtualizationMetricsEnabled = ContainsExactArgument(
                commandLineArguments,
                RenderVirtualizationMetricsCommandLineArgument);
            _renderVirtualizationMetricsObserved = false;
            _lastRenderVirtualizationCommandVersion = 0u;
            _nextRenderVirtualizationMetricsSeconds = 0d;
        }

        internal void RecordRenderVirtualizationMetrics(
            in OperationMapRenderVirtualizationMetricsComponent metrics)
        {
            if (!_renderVirtualizationMetricsEnabled)
                return;

            _nextRenderVirtualizationMetricsSeconds =
                Time.realtimeSinceStartupAsDouble +
                RenderVirtualizationMetricsIntervalSeconds;
            if (_renderVirtualizationMetricsObserved &&
                metrics.CommandVersion ==
                _lastRenderVirtualizationCommandVersion)
            {
                return;
            }

            _renderVirtualizationMetricsObserved = true;
            _lastRenderVirtualizationCommandVersion =
                metrics.CommandVersion;
            LogNoStackTrace(
                "[VRP-053 AndroidMetrics] " +
                $"placements={metrics.LogicalPlacementCount} " +
                $"parts={metrics.LogicalPartCount} " +
                $"resident={metrics.ResidentExceptionCount} " +
                $"slots={metrics.EnabledSlotCount}/{metrics.Capacity} " +
                $"activeCells={metrics.ActiveCellCount} " +
                $"activePlacements={metrics.ActivePlacementCount} " +
                $"retained={metrics.RetainedCount} " +
                $"released={metrics.ReleasedCount} " +
                $"rebound={metrics.ReboundCount} " +
                $"overflow={metrics.OverflowCount} " +
                $"deficit={metrics.HighestDeficit} " +
                $"reason={(int)metrics.RebuildReason} " +
                $"commandVersion={metrics.CommandVersion}");
        }

        private static bool ContainsExactArgument(
            IReadOnlyList<string> arguments,
            string argumentName)
        {
            if (arguments == null)
                return false;

            for (int i = 0; i < arguments.Count; i++)
            {
                if (string.Equals(
                        arguments[i],
                        argumentName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
