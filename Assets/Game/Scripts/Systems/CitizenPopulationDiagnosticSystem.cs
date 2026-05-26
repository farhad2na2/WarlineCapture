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

    public FrameTimings BeginFrame()
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
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

    public void MarkBuildings(ref FrameTimings timings) => timings.AfterBuildings = Time.realtimeSinceStartupAsDouble;
    public void MarkResolve(ref FrameTimings timings) => timings.AfterResolve = Time.realtimeSinceStartupAsDouble;
    public void MarkDanger(ref FrameTimings timings) => timings.AfterDanger = Time.realtimeSinceStartupAsDouble;
    public void MarkLogical(ref FrameTimings timings) => timings.AfterLogical = Time.realtimeSinceStartupAsDouble;
    public void MarkVisible(ref FrameTimings timings) => timings.AfterVisible = Time.realtimeSinceStartupAsDouble;
    public void MarkTotals(ref FrameTimings timings) => timings.AfterTotals = Time.realtimeSinceStartupAsDouble;

    public void MarkSkippedForPathfinding(ref FrameTimings timings)
    {
        timings.SkippedForPathfinding = true;
    }

    public void EndFrame(ref FrameTimings timings, CitizenPopulationStateSystem state)
    {
        double elapsed = Time.realtimeSinceStartupAsDouble - timings.StartTime;
        if (!EnableCitizenPopulationDiagnostics || elapsed < FreezeLogThresholdSeconds)
            return;

        if (timings.AfterBuildings < timings.StartTime) timings.AfterBuildings = timings.StartTime;
        if (timings.AfterResolve < timings.AfterBuildings) timings.AfterResolve = timings.AfterBuildings;
        if (timings.AfterDanger < timings.AfterResolve) timings.AfterDanger = timings.AfterResolve;
        if (timings.AfterLogical < timings.AfterDanger) timings.AfterLogical = timings.AfterDanger;
        if (timings.AfterVisible < timings.AfterLogical) timings.AfterVisible = timings.AfterLogical;
        if (timings.AfterTotals < timings.AfterVisible) timings.AfterTotals = timings.AfterVisible;

        Debug.Log(
            $"[CitizenPopulationDiag] frame={Time.frameCount} total={elapsed * 1000d:F1}ms " +
            $"buildings={(timings.AfterBuildings - timings.StartTime) * 1000d:F1}ms " +
            $"resolve={(timings.AfterResolve - timings.AfterBuildings) * 1000d:F1}ms " +
            $"danger={(timings.AfterDanger - timings.AfterResolve) * 1000d:F1}ms " +
            $"logical={(timings.AfterLogical - timings.AfterDanger) * 1000d:F1}ms " +
            $"visible={(timings.AfterVisible - timings.AfterLogical) * 1000d:F1}ms " +
            $"totals={(timings.AfterTotals - timings.AfterVisible) * 1000d:F1}ms " +
            $"citizens={state.CitizensById.Count} visible={state.VisibleCitizensById.Count} skippedPath={timings.SkippedForPathfinding}");
    }
}
