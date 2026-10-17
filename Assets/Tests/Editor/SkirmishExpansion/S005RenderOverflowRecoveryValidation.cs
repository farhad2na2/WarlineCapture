using System;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class S005RenderOverflowRecoveryValidation
    {
        public static void RunFocusedValidation()
        {
            global::OperationMapRenderOverflowGuardRecoveryTests.RunFocusedValidation();
            using (global::ValidationExit.SuppressProcessExit())
            {
                global::ValidationExit.ClearLastExitCode();
                global::OperationMapRenderVirtualizationInitializationSystemTests.RunFocusedValidation();
                if (global::ValidationExit.LastExitCode != 0)
                    throw new InvalidOperationException("Existing rendering initialization regressions failed.");
            }
            S005CameraCalibrationNativePreparation.RunFocusedValidation();
            Debug.Log("[S005RenderOverflowRecoveryValidation] result=Passed recovery=4 renderingRegression=13 cameraPreparation=Passed missionCertified=0");
        }
    }
}
