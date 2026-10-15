#if UNITY_EDITOR
using UnityEngine;

namespace Game.Tests.Editor
{
    // Keep each focused suite's independent marker; any failure aborts the
    // aggregate. The final marker is never a normal-play or readiness claim.
    public static class SkirmishSharedProductionNativeValidation
    {
        public static void RunFocusedValidation()
        {
            MatchHudNormalCameraZoomTests.RunFocusedValidation();
            SkirmishS005OpeningViewPolicyTests.RunFocusedValidation();
            SkirmishMissionLogicalSubsystemValidation.RunFocusedValidation();
            SkirmishManagedLogicalBindingDefaultsTests.RunFocusedValidation();
            SkirmishNativePublicGoalPresentationValidation.RunFocusedValidation();
            // Explicit local logical camera preparation; normal trial source freeze begins after wrapper exit.
            Game.Editor.SkirmishS005OpeningViewPolicyAuthoring.EnableExplicitOpeningRiflePolicy();
            S005CameraCalibrationNativePreparation.RunFocusedValidation();
            Debug.Log("[SkirmishSharedProductionNativeValidation] result=Passed shared=80 publicGoals=6 managedDefaults=1 openingPolicy=5 normalZoom=3 cameraPreparation=Passed normalMissionReadiness=Pending");
        }
    }
}
#endif
