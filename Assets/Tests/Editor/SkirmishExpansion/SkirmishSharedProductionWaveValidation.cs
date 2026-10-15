#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Root integration boundary. Each suite preserves its independent marker;
    // independent suites continue after a failed group; any failure prevents the
    // aggregate pass marker. No normal mission is claimed.
    public static class SkirmishSharedProductionWaveValidation
    {
        public static void PrepareLogicalAssetsAndRunFocusedValidation()
        {
            var failures = new List<Exception>();
            RunSuite("SkirmishSharedObjectivePublicWaveValidation", SkirmishSharedObjectivePublicWaveValidation.PrepareLogicalAssetsAndRunFocusedValidation, failures);
            RunSuite("SkirmishNativeCheckpointSnapshotTests", SkirmishNativeCheckpointSnapshotTests.RunFocusedValidation, failures);
            RunSuite("SkirmishActiveRepairRecoveryValidation", SkirmishActiveRepairRecoveryValidation.RunFocusedValidation, failures);
            RunSuite("SkirmishEnemyVehicleRepairPolicyTests", SkirmishEnemyVehicleRepairPolicyTests.RunFocusedValidation, failures);
            RunSuite("SkirmishPublicObjectiveHudTests", SkirmishPublicObjectiveHudTests.RunFocusedValidation, failures);
            RunSuite("SkirmishPublicUnloadNativeTests", SkirmishPublicUnloadNativeTests.RunFocusedValidation, failures);
            RunSuite("SkirmishPublicRepairHudTests", SkirmishPublicRepairHudTests.RunFocusedValidation, failures);
            RunSuite("SkirmishCameraPosedMeshReaderTests", SkirmishCameraPosedMeshReaderTests.RunFocusedValidation, failures);
            RunSuite("SkirmishCameraFramingMeasurementTests", SkirmishCameraFramingMeasurementTests.RunFocusedValidation, failures);
            RunSuite("MatchHudNormalCameraZoomTests", MatchHudNormalCameraZoomTests.RunFocusedValidation, failures);
            RunSuite("AriaTouchInputValidation", () => { Game.Editor.AriaTouchInputValidation.Run(); }, failures);
            RunSuite("S005ObservedSquadPageReceiptTests", S005ObservedSquadPageReceiptTests.RunFocusedValidation, failures);
            RunSuite("SkirmishGroundSourceFitSurvey", Game.Editor.SkirmishGroundSourceFitSurvey.Run, failures);
            if (failures.Count != 0) throw new AggregateException("Shared production validation failed; independent suite results are retained.", failures);
            Debug.Log("[SkirmishSharedProductionWaveValidation] result=Passed objectiveWave=131 repair=55 legacySnapshot=7 enemyRepair=9 publicHud=13 unload=11 repairHud=15 posedMesh=11 framing=7 normalZoom=3 normalTouch=30 pagingReceipt=9 sourceSurvey=4 normalMissionReadiness=Pending");
        }
        private static void RunSuite(string name, Action run, List<Exception> failures)
        {
            try { run(); }
            catch (Exception exception)
            {
                failures.Add(exception);
                Debug.Log("[SkirmishSharedProductionWaveValidation] suiteFailed=" + name + " failure=" + exception);
            }
        }
    }
}
#endif
