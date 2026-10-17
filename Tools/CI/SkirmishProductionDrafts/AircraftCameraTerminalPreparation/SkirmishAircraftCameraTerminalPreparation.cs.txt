#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Editor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // One focused engineering preparation. It does not start Play or certify a mission.
    public static class SkirmishAircraftCameraTerminalPreparation
    {
        public static void RunFocusedValidation()
        {
            var failures = new List<Exception>();
            try
            {
                SkirmishAircraftRotorGeometryAuthoring.CreateMeasuredAdditionalAircraftMetadata();
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishAircraftCameraTerminalPreparation] suiteFailed=metadata");
                Debug.LogException(error);
                failures.Add(new InvalidOperationException("Measured aircraft metadata preparation failed", error));
            }
            try
            {
                SkirmishMeasuredRotorAircraftFramingTests.RunFocusedValidation();
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishAircraftCameraTerminalPreparation] suiteFailed=framing55");
                Debug.LogException(error);
                failures.Add(new InvalidOperationException("Measured aircraft framing controls failed", error));
            }
            try
            {
                SkirmishTerminalPresentationObservationTests.RunFocusedValidation();
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishAircraftCameraTerminalPreparation] suiteFailed=terminal6");
                Debug.LogException(error);
                failures.Add(new InvalidOperationException("Terminal presentation controls failed", error));
            }
            if (failures.Count != 0)
                throw new AggregateException("Aircraft camera and terminal preparation failed; retain every suite failure", failures);
            Debug.Log("[SkirmishAircraftCameraTerminalPreparation] result=Passed metadataAdditionalRoles=6 rotorRoles=4 framingControls=55 terminalMethods=6 nativeCameraAndMissionReadiness=Pending");
        }
    }
}
#endif
