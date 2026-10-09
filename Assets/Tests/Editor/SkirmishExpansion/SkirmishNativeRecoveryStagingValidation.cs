using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishNativeRecoveryStagingValidation
    {
        public static void RunFocusedValidation()
        {
            SkirmishExpandedCheckpointTests.RunFocusedValidation();
            SkirmishNativeCheckpointPartsTests.RunFocusedValidation();
            SkirmishNativeProductionCheckpointTests.RunFocusedValidation();
            SkirmishNativeCheckpointSnapshotTests.RunFocusedValidation();
            SkirmishNativeCheckpointCodecTests.RunFocusedValidation();
            SkirmishNativeCheckpointRecoveryPreparationTests.RunFocusedValidation();
            SkirmishNativeCheckpointActorSpawnTests.RunFocusedValidation();
            SkirmishNativeCheckpointStructureReconstructionTests.RunFocusedValidation();
            SkirmishNativeCheckpointRecoveryCommitTests.RunFocusedValidation();
            SkirmishNativeCheckpointRecoveryRunnerTests.RunFocusedValidation();
            SkirmishNativeCheckpointPrefabRegistryTests.RunFocusedValidation();
            Debug.Log("[SkirmishNativeRecoveryStagingValidation] result=Passed tests=51 nativeRecoveredSessions=0");
        }
    }
}
