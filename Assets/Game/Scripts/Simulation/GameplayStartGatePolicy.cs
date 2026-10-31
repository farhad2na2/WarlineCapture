namespace Game.Simulation
{
    // Pure readiness predicates extracted for cross-runtime use. Callers must
    // observe and supply these facts from the existing runtime owners.
    public static class GameplayStartGatePolicy
    {
        public const int RuntimeCityFailOpenFrames = 1800;

        public static bool HasLaunchRequest(bool gameplayInitialized, bool playRequested) =>
            gameplayInitialized && playRequested;

        public static bool IsBuildingStartupComplete(bool buildingStartupComplete) => buildingStartupComplete;

        public static bool IsOptionalRuntimeOwnerComplete(bool ownerPresent, bool ownerComplete) =>
            !ownerPresent || ownerComplete;

        public static bool IsWorldAvailable(bool worldCreated) => worldCreated;

        public static bool AreInitialSpawnCountsComplete(int configCount, int initializedCount) =>
            configCount == 0 || initializedCount >= configCount;

        public static bool IsRuntimeCityFailOpenWindow(
            bool gameplayInitialized, bool playRequested,
            int loadingGateStartedFrame, int currentFrame) =>
            loadingGateStartedFrame >= 0 && gameplayInitialized && playRequested &&
            currentFrame - loadingGateStartedFrame >= RuntimeCityFailOpenFrames;

        public static bool HasRuntimeCity(bool runtimeCityPresent) => runtimeCityPresent;

        public static bool IsRuntimeCityNotSpawned(bool runtimeCitySpawned) => !runtimeCitySpawned;

        public static bool IsRuntimeCityNotGenerating(bool runtimeCityGenerating) => !runtimeCityGenerating;
    }
}
