public static class InitialUnitsRuntimeState
{
    public static bool PlayRequested;
    public static bool SimulationActive;
    public static UnityEngine.Camera WorldCamera;
    public static bool InitialCameraFocusRequested;
    public static UnityEngine.Vector3 InitialCameraFocusWorld;
    public static bool SelectionModeActive;
    public static bool BuildModeActive;
    public static bool FullscreenMapOpen;
    public static bool FullscreenMapIsoMode;
    public static bool ZoomInHeld;
    public static bool ZoomOutHeld;
    public static bool SuppressNextWorldClick;
    public static bool PlayerAutoModeEnabled;
    public static bool VerboseAILogs;
    public static bool TransportBoardingDiagnostics = false;
    public static bool BuildingRuntimeSliceDiagnostics = false;

    public static bool ShouldLogAI => VerboseAILogs;
}
