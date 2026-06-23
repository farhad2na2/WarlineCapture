using UnityEngine;

internal sealed class RuntimeCityDiagnosticsSystemHelper
{
    private bool _enableStateDiagnostics;

    public void LogLifecycleStart(int frameCount, int cityCount, bool generateBuildings, int generationYieldInterval)
    {
        if (!_enableStateDiagnostics)
            return;

        Debug.Log($"[RuntimeCityState] frame={frameCount} reason=start cityCount={cityCount} generateBuildings={(generateBuildings ? 1 : 0)} yieldInterval={generationYieldInterval}");
    }

    public void LogLifecycleGenerating(
        int frameCount,
        int generationStartedFrame,
        int generationMoveNextCount,
        int cityCount,
        bool generateBuildings,
        int generationYieldInterval)
    {
        if (!_enableStateDiagnostics)
            return;

        Debug.Log($"[RuntimeCityState] frame={frameCount} reason=generating ageFrames={frameCount - generationStartedFrame} steps={generationMoveNextCount} cityCount={cityCount} generateBuildings={(generateBuildings ? 1 : 0)} yieldInterval={generationYieldInterval}");
    }

    public void LogLifecycleEnded(int frameCount, int generationStartedFrame, int generationMoveNextCount, bool spawned)
    {
        if (!_enableStateDiagnostics)
            return;

        Debug.Log($"[RuntimeCityState] frame={frameCount} reason=ended spawned={(spawned ? 1 : 0)} ageFrames={frameCount - generationStartedFrame} steps={generationMoveNextCount}");
    }

    public void LogLifecycleCompleted(int frameCount, int generationStartedFrame, int generationMoveNextCount, int generatedCityCount)
    {
        if (!_enableStateDiagnostics)
            return;

        Debug.Log($"[RuntimeCityState] frame={frameCount} reason=completed cities={generatedCityCount} ageFrames={frameCount - generationStartedFrame} steps={generationMoveNextCount}");
    }

    public void LogInitialSpawnWait(int frameCount, int initialSpawnConfigs, int initializedInitialSpawnConfigs)
    {
        if (!_enableStateDiagnostics)
            return;

        Debug.Log($"[RuntimeCityState] frame={frameCount} reason=waiting-initial-units configs={initialSpawnConfigs} initialized={initializedInitialSpawnConfigs}");
    }

    public void LogCityPlanningFailed(int cityNumber, int generatedCityCount)
    {
        Debug.LogWarning($"[RuntimeCity] Failed to plan city {cityNumber}. Stopping city chain at {generatedCityCount} city/cities.");
    }

    public void LogSourceExitRoadFailed(int cityNumber, int pathLength)
    {
        Debug.LogWarning($"[RuntimeCity] Failed to create source exit road for city {cityNumber}. pathLength={pathLength}.");
    }

    public void LogAutobahnFailed(int cityNumber, int pathLength, Vector2Int direction)
    {
        Debug.LogWarning($"[RuntimeCity] Failed to create autobahn for city {cityNumber}. pathLength={pathLength}, direction={direction}.");
    }

    public void LogHallPlacementFailed(Vector2Int centerRoadCell)
    {
        Debug.LogWarning($"[RuntimeCity] Hall could not be placed for city at {centerRoadCell}.");
    }
}
