using System;
using System.Reflection;
using Game.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class S005CameraCalibrationNativePreparation
    {
        public static void RunFocusedValidation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stopped Play is required for native preparation.");
            SkirmishCameraFramingMeasurementTests.RunFocusedValidation();
            SkirmishMissionViewProfileTests.RunFocusedValidation();
            SkirmishProductionContinuationValidation.RunFocusedValidation();
            var entry = typeof(SkirmishS002AriaRunHarness).GetMethod(
                "RunS005NormalCameraCalibration", BindingFlags.Public | BindingFlags.Static);
            if (entry == null || entry.GetParameters().Length != 0 || entry.ReturnType != typeof(void))
                throw new InvalidOperationException("Normal camera calibration entry signature differs.");
            SkirmishProductionNativeIdentityCensus.ExportS005RegularStandard155926Fa();
            S005SceneArtifactWarmup.Run();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Preparation unexpectedly started Play.");
            Debug.Log("[S005CameraCalibrationNativePreparation] result=Passed gameplayStarted=0 cameraAccepted=0 snapshotAfterWrapperExit=1");
        }
    }
}
