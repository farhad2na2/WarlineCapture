namespace Game.Runtime
{
    public static class InitialUnitsRuntimeState
    {
        public static bool VerboseAILogs;
        public static bool TransportBoardingDiagnostics = false;
        public static bool BuildingRuntimeSliceDiagnostics = false;

        public static bool ShouldLogAI => VerboseAILogs;
    }
}
