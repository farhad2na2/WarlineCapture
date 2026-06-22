using UnityEngine;

internal sealed class CitizenPopulationDiagnosticSystem
{
    private static readonly bool EnableCitizenPopulationDiagnostics = false;
    private const double FreezeLogThresholdSeconds = 0.05d;

    public struct FrameTimings
    {
        public double StartTime;
        public double AfterBuildings;
        public double AfterResolve;
        public double AfterDanger;
        public double AfterLogical;
        public double AfterVisible;
        public double AfterTotals;
        public bool SkippedForPathfinding;
    }

    public static FrameTimings BeginFrame(CitizenPopulationDiagnosticSystem system)
    {
        return system != null
            ? system.BeginFrame()
            : BeginFrameState();
    }

    public FrameTimings BeginFrame()
    {
        return BeginFrameState();
    }

    public static void MarkBuildings(CitizenPopulationDiagnosticSystem system, ref FrameTimings timings)
    {
        if (system != null)
            system.MarkBuildings(ref timings);
        else
            MarkBuildingsState(ref timings);
    }

    public static void MarkResolve(CitizenPopulationDiagnosticSystem system, ref FrameTimings timings)
    {
        if (system != null)
            system.MarkResolve(ref timings);
        else
            MarkResolveState(ref timings);
    }

    public static void MarkDanger(CitizenPopulationDiagnosticSystem system, ref FrameTimings timings)
    {
        if (system != null)
            system.MarkDanger(ref timings);
        else
            MarkDangerState(ref timings);
    }

    public static void MarkLogical(CitizenPopulationDiagnosticSystem system, ref FrameTimings timings)
    {
        if (system != null)
            system.MarkLogical(ref timings);
        else
            MarkLogicalState(ref timings);
    }

    public static void MarkVisible(CitizenPopulationDiagnosticSystem system, ref FrameTimings timings)
    {
        if (system != null)
            system.MarkVisible(ref timings);
        else
            MarkVisibleState(ref timings);
    }

    public static void MarkTotals(CitizenPopulationDiagnosticSystem system, ref FrameTimings timings)
    {
        if (system != null)
            system.MarkTotals(ref timings);
        else
            MarkTotalsState(ref timings);
    }

    public static void MarkSkippedForPathfinding(CitizenPopulationDiagnosticSystem system, ref FrameTimings timings)
    {
        if (system != null)
            system.MarkSkippedForPathfinding(ref timings);
        else
            MarkSkippedForPathfindingState(ref timings);
    }

    public static void EndFrame(CitizenPopulationDiagnosticSystem system, ref FrameTimings timings, CitizenPopulationStateSystem state)
    {
        if (system != null)
        {
            system.EndFrame(ref timings, state);
            return;
        }

        EndFrameState(ref timings, state);
    }

    public void MarkBuildings(ref FrameTimings timings) => MarkBuildingsState(ref timings);
    public void MarkResolve(ref FrameTimings timings) => MarkResolveState(ref timings);
    public void MarkDanger(ref FrameTimings timings) => MarkDangerState(ref timings);
    public void MarkLogical(ref FrameTimings timings) => MarkLogicalState(ref timings);
    public void MarkVisible(ref FrameTimings timings) => MarkVisibleState(ref timings);
    public void MarkTotals(ref FrameTimings timings) => MarkTotalsState(ref timings);

    public void MarkSkippedForPathfinding(ref FrameTimings timings)
    {
        MarkSkippedForPathfindingState(ref timings);
    }

    public void EndFrame(ref FrameTimings timings, CitizenPopulationStateSystem state)
    {
        EndFrameState(ref timings, state);
    }

    private static FrameTimings BeginFrameState()
    {
        double startTime = UnityEngine.Time.realtimeSinceStartupAsDouble;
        return new FrameTimings
        {
            StartTime = startTime,
            AfterBuildings = startTime,
            AfterResolve = startTime,
            AfterDanger = startTime,
            AfterLogical = startTime,
            AfterVisible = startTime,
            AfterTotals = startTime,
            SkippedForPathfinding = false
        };
    }

    private static void MarkBuildingsState(ref FrameTimings timings) => timings.AfterBuildings = UnityEngine.Time.realtimeSinceStartupAsDouble;
    private static void MarkResolveState(ref FrameTimings timings) => timings.AfterResolve = UnityEngine.Time.realtimeSinceStartupAsDouble;
    private static void MarkDangerState(ref FrameTimings timings) => timings.AfterDanger = UnityEngine.Time.realtimeSinceStartupAsDouble;
    private static void MarkLogicalState(ref FrameTimings timings) => timings.AfterLogical = UnityEngine.Time.realtimeSinceStartupAsDouble;
    private static void MarkVisibleState(ref FrameTimings timings) => timings.AfterVisible = UnityEngine.Time.realtimeSinceStartupAsDouble;
    private static void MarkTotalsState(ref FrameTimings timings) => timings.AfterTotals = UnityEngine.Time.realtimeSinceStartupAsDouble;

    private static void MarkSkippedForPathfindingState(ref FrameTimings timings)
    {
        timings.SkippedForPathfinding = true;
    }

    private static void EndFrameState(ref FrameTimings timings, CitizenPopulationStateSystem state)
    {
        double elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - timings.StartTime;
        if (!EnableCitizenPopulationDiagnostics || elapsed < FreezeLogThresholdSeconds)
            return;

        if (timings.AfterBuildings < timings.StartTime) timings.AfterBuildings = timings.StartTime;
        if (timings.AfterResolve < timings.AfterBuildings) timings.AfterResolve = timings.AfterBuildings;
        if (timings.AfterDanger < timings.AfterResolve) timings.AfterDanger = timings.AfterResolve;
        if (timings.AfterLogical < timings.AfterDanger) timings.AfterLogical = timings.AfterDanger;
        if (timings.AfterVisible < timings.AfterLogical) timings.AfterVisible = timings.AfterLogical;
        if (timings.AfterTotals < timings.AfterVisible) timings.AfterTotals = timings.AfterVisible;

        Debug.Log(
            $"[CitizenPopulationDiag] frame={UnityEngine.Time.frameCount} total={elapsed * 1000d:F1}ms " +
            $"buildings={(timings.AfterBuildings - timings.StartTime) * 1000d:F1}ms " +
            $"resolve={(timings.AfterResolve - timings.AfterBuildings) * 1000d:F1}ms " +
            $"danger={(timings.AfterDanger - timings.AfterResolve) * 1000d:F1}ms " +
            $"logical={(timings.AfterLogical - timings.AfterDanger) * 1000d:F1}ms " +
            $"visible={(timings.AfterVisible - timings.AfterLogical) * 1000d:F1}ms " +
            $"totals={(timings.AfterTotals - timings.AfterVisible) * 1000d:F1}ms " +
            $"citizens={state.CitizensById.Count} visible={state.VisibleCitizensById.Count} skippedPath={timings.SkippedForPathfinding}");
    }
}
