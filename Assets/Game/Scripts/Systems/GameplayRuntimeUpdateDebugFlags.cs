namespace Game.Runtime
{
    public static class GameplayRuntimeUpdateDebugFlags
    {
        public static bool DisableBuildingPlacementRuntime { get; set; }
        public static bool DisableSelectionRuntime { get; set; }

        public static void Reset()
        {
            DisableBuildingPlacementRuntime = false;
            DisableSelectionRuntime = false;
        }
    }
}
