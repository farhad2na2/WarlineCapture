using System;

namespace Game.Runtime
{
    internal sealed partial class SelectionGameplayStartupSystemHelper
    {
        private static Action CreateDisposeAction(
            RtsSelectionRuntimeCameraSystemHelper runtimeCamera,
            SelectionUiCameraSystemHelper selectionCamera,
            TacticalFollowCameraModeSystemHelper tacticalFollowCamera,
            SelectionOrderMarkerPresentationSystemHelper orderMarkers)
        {
            return () =>
            {
                runtimeCamera.Dispose();
                selectionCamera.Dispose();
                tacticalFollowCamera.Dispose();
                orderMarkers.Dispose();
            };
        }

        private static RtsSelectionRuntimeCameraSystemHelper ResolveRtsSelectionRuntimeCameraSystemHelper()
        {
            return new RtsSelectionRuntimeCameraSystemHelper();
        }
    }
}
