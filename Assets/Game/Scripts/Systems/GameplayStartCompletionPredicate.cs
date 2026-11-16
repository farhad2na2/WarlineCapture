namespace Game.Runtime
{
    /// <summary>Scalar loading-gate checks shared by native composition and source hosts.</summary>
    internal static class GameplayStartCompletionPredicate
    {
        internal static bool GameplayStartRequested(bool gameplayInitialized, bool playRequested)
        {
            if (!gameplayInitialized || !playRequested)
                return false;
            return true;
        }

        internal static bool BuildingStartupComplete(bool complete) => complete;

        internal static bool OptionalRuntimeOwnerReady(bool ownerPresent, bool ownerSpawned) =>
            !ownerPresent || ownerSpawned;

        // Shared by both original map-placement owners and source hosts. Keep the
        // null/disabled-config no-op and authoring-visual completion rules in one
        // production policy so diagnostic hosts can report the same completion
        // without constructing a parallel placement system.
        internal static bool MapPlacementOwnerComplete(
            bool configMissingOrDisabled,
            bool hideAuthoringVisualsAfterSpawn,
            bool authoringRootAssigned,
            bool ownerHiddenAuthoring,
            bool authoringRootInactive,
            bool ownerQueued)
        {
            if (configMissingOrDisabled)
                return true;

            bool authoringHidden =
                !hideAuthoringVisualsAfterSpawn ||
                !authoringRootAssigned ||
                ownerHiddenAuthoring ||
                authoringRootInactive;
            return ownerQueued && authoringHidden;
        }

        // Native city lifecycle uses the same rule: a zero-city configuration is
        // complete without a generation pass.
        internal static bool RuntimeCityOwnerSpawned(bool alreadySpawned, int cityCount) =>
            alreadySpawned || cityCount <= 0;

        internal static bool LegacyPresentationEnabled(bool entitySceneMode) =>
            !entitySceneMode;

        internal static bool RuntimeWorldAvailable(bool worldExists, bool worldIsCreated) =>
            worldExists && worldIsCreated;

        internal static bool InitialSpawnConfigsComplete(int totalConfigCount, int initializedConfigCount)
        {
            return totalConfigCount == 0 || initializedConfigCount >= totalConfigCount;
        }
    }
}
