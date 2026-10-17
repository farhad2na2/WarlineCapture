#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Explicit engineering suites only; ordinary missions and native acceptance remain separate.
    public static class SkirmishSharedNativeProductionPreparation
    {
        public static void Run()
        {
            var failures = new List<Exception>();
            try
            {
                SkirmishAircraftCameraTerminalPreparation.RunFocusedValidation();
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishSharedNativeProductionPreparation] suiteFailed=cameraTerminal");
                Debug.LogException(error);
                failures.Add(new InvalidOperationException("Aircraft camera and terminal preparation failed", error));
            }
            try
            {
                SkirmishLogicalRefinementPreparationValidation.Run();
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishSharedNativeProductionPreparation] suiteFailed=logicalRefinement");
                Debug.LogException(error);
                failures.Add(new InvalidOperationException("Logical refinement preparation failed", error));
            }
            try
            {
                AriaExtendedObjectiveSupportValidation.Run();
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishSharedNativeProductionPreparation] suiteFailed=aria70");
                Debug.LogException(error);
                failures.Add(new InvalidOperationException("ARIA objective support validation failed", error));
            }
            if (failures.Count != 0)
                throw new AggregateException("Shared native production preparation failed; retain every independent suite failure", failures);
            Debug.Log("[SkirmishSharedNativeProductionPreparation] result=Passed metadataAdditionalRoles=6 rotorRoles=4 framingControls=55 terminalMethods=6 enemy=5 approach=3 view=6 typedSpawn=5 aria=70 cameraAndMissionAcceptance=Pending");
        }
    }
}
#endif
