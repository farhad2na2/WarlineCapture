namespace Game.Runtime
{
    public static class GameplayRuntimeUpdateDebugFlags
    {
        public static bool DisableBuildingPlacementRuntime { get; set; }
        public static bool DisableSelectionRuntime { get; set; }
        public static bool DisableUnitMotionAudioRuntime { get; set; }
        public static bool DisableBuildingBoundaryRuntime { get; set; }
        public static bool DisableBuildingProductionRuntime { get; set; }
        public static bool DisableBuildingResourceRuntime { get; set; }
        public static bool DisableBuildingResourceHaulerRuntime { get; set; }
        public static bool DisableBuildingVisualRuntime { get; set; }
        public static bool DisableBuildingInputRuntime { get; set; }
        public static bool DisableBuildingReservationCleanupRuntime { get; set; }
        public static bool DisableBuildingDestroyedRuntime { get; set; }
        public static bool DisableBuildingDoorRuntime { get; set; }
        public static bool DisableBuildingMarkerRuntime { get; set; }

        public static void Reset()
        {
            DisableBuildingPlacementRuntime = false;
            DisableSelectionRuntime = false;
            DisableUnitMotionAudioRuntime = false;
            DisableBuildingBoundaryRuntime = false;
            DisableBuildingProductionRuntime = false;
            DisableBuildingResourceRuntime = false;
            DisableBuildingResourceHaulerRuntime = false;
            DisableBuildingVisualRuntime = false;
            DisableBuildingInputRuntime = false;
            DisableBuildingReservationCleanupRuntime = false;
            DisableBuildingDestroyedRuntime = false;
            DisableBuildingDoorRuntime = false;
            DisableBuildingMarkerRuntime = false;
        }
    }
}
