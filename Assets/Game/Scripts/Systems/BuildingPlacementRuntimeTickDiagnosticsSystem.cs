using System;
using UnityEngine;

internal sealed class BuildingPlacementRuntimeTickDiagnosticsSystem
{
    private const double SlowLogThresholdSeconds = 0.01d;
    private const double SlowLogCooldownSeconds = 1d;
    private double _nextSlowLogAt;

    public readonly struct Context
    {
        public readonly Func<bool> ShouldLogDiagnostics;
        public readonly Func<int> GetRuntimeBuildingCount;
        public readonly Action<string> Log;

        public Context(Func<bool> shouldLogDiagnostics, Func<int> getRuntimeBuildingCount, Action<string> log)
        {
            ShouldLogDiagnostics = shouldLogDiagnostics;
            GetRuntimeBuildingCount = getRuntimeBuildingCount;
            Log = log;
        }
    }

    public readonly struct Timing
    {
        public readonly double Start;
        public readonly double AfterMapPlacements;
        public readonly double AfterBoundary;
        public readonly double AfterProductions;
        public readonly double AfterResources;
        public readonly double AfterHaulers;
        public readonly double AfterResourceVisuals;
        public readonly double AfterReservations;
        public readonly double AfterDestroyed;
        public readonly double AfterDoors;
        public readonly double AfterMarkers;
        public readonly double AfterInputOutline;
        public readonly double AfterInputMouse;
        public readonly double AfterInputUi;
        public readonly double AfterInputBuildingClick;
        public readonly double AfterInput;

        public Timing(
            double start,
            double afterMapPlacements,
            double afterBoundary,
            double afterProductions,
            double afterResources,
            double afterHaulers,
            double afterResourceVisuals,
            double afterReservations,
            double afterDestroyed,
            double afterDoors,
            double afterMarkers,
            double afterInputOutline,
            double afterInputMouse,
            double afterInputUi,
            double afterInputBuildingClick,
            double afterInput)
        {
            Start = start;
            AfterMapPlacements = afterMapPlacements;
            AfterBoundary = afterBoundary;
            AfterProductions = afterProductions;
            AfterResources = afterResources;
            AfterHaulers = afterHaulers;
            AfterResourceVisuals = afterResourceVisuals;
            AfterReservations = afterReservations;
            AfterDestroyed = afterDestroyed;
            AfterDoors = afterDoors;
            AfterMarkers = afterMarkers;
            AfterInputOutline = afterInputOutline;
            AfterInputMouse = afterInputMouse;
            AfterInputUi = afterInputUi;
            AfterInputBuildingClick = afterInputBuildingClick;
            AfterInput = afterInput;
        }
    }

    public static Context CreateContext(Func<bool> shouldLogDiagnostics, Func<int> getRuntimeBuildingCount, Action<string> log)
    {
        return new Context(shouldLogDiagnostics, getRuntimeBuildingCount, log);
    }

    public void LogIfSlow(Context context, Timing timing)
    {
        double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
        double elapsed = now - timing.Start;
        if (context.ShouldLogDiagnostics == null ||
            !context.ShouldLogDiagnostics() ||
            elapsed < SlowLogThresholdSeconds ||
            now < _nextSlowLogAt ||
            !Application.isFocused)
            return;

        _nextSlowLogAt = now + SlowLogCooldownSeconds;
        double afterMapPlacements = Math.Max(timing.AfterMapPlacements, timing.Start);
        double afterBoundary = Math.Max(timing.AfterBoundary, afterMapPlacements);
        double afterProductions = Math.Max(timing.AfterProductions, afterBoundary);
        double afterResources = Math.Max(timing.AfterResources, afterProductions);
        double afterHaulers = Math.Max(timing.AfterHaulers, afterResources);
        double afterResourceVisuals = Math.Max(timing.AfterResourceVisuals, afterHaulers);
        double afterReservations = Math.Max(timing.AfterReservations, afterResourceVisuals);
        double afterDestroyed = Math.Max(timing.AfterDestroyed, afterReservations);
        double afterDoors = Math.Max(timing.AfterDoors, afterDestroyed);
        double afterMarkers = Math.Max(timing.AfterMarkers, afterDoors);
        double afterInputOutline = Math.Max(timing.AfterInputOutline, afterMarkers);
        double afterInputMouse = Math.Max(timing.AfterInputMouse, afterInputOutline);
        double afterInputUi = Math.Max(timing.AfterInputUi, afterInputMouse);
        double afterInputBuildingClick = Math.Max(timing.AfterInputBuildingClick, afterInputUi);
        double afterInput = Math.Max(timing.AfterInput, afterInputBuildingClick);

        context.Log?.Invoke(
            $"[BuildingRuntimeSliceDiag] frame={UnityEngine.Time.frameCount} total={elapsed * 1000d:F1}ms " +
            $"mapPlacement={(afterMapPlacements - timing.Start) * 1000d:F1}ms " +
            $"boundary={(afterBoundary - afterMapPlacements) * 1000d:F1}ms " +
            $"productions={(afterProductions - afterBoundary) * 1000d:F1}ms " +
            $"resources={(afterResources - afterProductions) * 1000d:F1}ms " +
            $"haulers={(afterHaulers - afterResources) * 1000d:F1}ms " +
            $"resourceVisuals={(afterResourceVisuals - afterHaulers) * 1000d:F1}ms " +
            $"reservations={(afterReservations - afterResourceVisuals) * 1000d:F1}ms " +
            $"destroyed={(afterDestroyed - afterReservations) * 1000d:F1}ms " +
            $"doors={(afterDoors - afterDestroyed) * 1000d:F1}ms " +
            $"markers={(afterMarkers - afterDoors) * 1000d:F1}ms " +
            $"input={(afterInput - afterMarkers) * 1000d:F1}ms " +
            $"inputOutline={(afterInputOutline - afterMarkers) * 1000d:F1}ms " +
            $"inputMouse={(afterInputMouse - afterInputOutline) * 1000d:F1}ms " +
            $"inputUi={(afterInputUi - afterInputMouse) * 1000d:F1}ms " +
            $"inputBuilding={(afterInputBuildingClick - afterInputUi) * 1000d:F1}ms " +
            $"buildings={context.GetRuntimeBuildingCount?.Invoke() ?? 0}");
    }
}
