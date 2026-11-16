#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SharedSpawnInitializationParityTests
    {
        public static void RunFocusedValidation()
        {
            int kernelTests = SkirmishActorInitializationKernelTests.RunChecks();
            kernelTests += SpawnCellSearchKernelTests.RunChecks();
            kernelTests += SharedStaticBlockerKernelTests.RunChecks();
            GridBlockerStorageEpochTests.RunFocusedValidation();
            SkirmishTypedSpawnConstraintTests.RunFocusedValidation();
            SkirmishGenericGroundRosterTests.RunFocusedValidation();
            SkirmishNativeStartupRosterTests.RunFocusedValidation();
            SkirmishActorInitializationKernelTests.EmitNativeTrace();
            SpawnCellSearchKernelTests.EmitNativeTrace();
            SharedStaticBlockerKernelTests.EmitNativeTrace();
            // A fresh source-pinned map snapshot is required after shared runtime code changes.
            SkirmishOfflineDataExportTests.RunFocusedValidation();
            Debug.Log("[SharedSpawnInitializationParity] result=Passed kernel_tests="+kernelTests+
                " blocker_epoch_controls=12 typed_spawn_controls=5 roster_tests=8 startup_cases=6 export_checks=9 liveMissionReady=0");
        }
    }
}
#endif
