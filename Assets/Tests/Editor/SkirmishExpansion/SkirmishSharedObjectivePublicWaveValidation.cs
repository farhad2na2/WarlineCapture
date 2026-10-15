#if UNITY_EDITOR
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishSharedObjectivePublicWaveValidation
    {
        // Explicit root-owned logical asset authoring precedes the isolated
        // provider/collector controls. No Campaign geometry or match is authored.
        public static void PrepareLogicalAssetsAndRunFocusedValidation()
        {
            Game.Editor.SkirmishConvoyLogicalAssetsAuthoringEntry.EnsurePinnedLogicalAssets();
            SkirmishMissionLogicalSubsystemValidation.RunFocusedValidation();
            SkirmishEnemyObjectiveValidation.RunFocusedValidation();
            SkirmishConvoyCommandDomainTests.RunFocusedValidation();
            SkirmishMissionLogicalBindingSubjectFixtureTests.RunFocusedValidation();
            SkirmishNativePublicGoalPresentationValidation.RunFocusedValidation();
            SkirmishNativePublicSubjectPresentationTests.RunFocusedValidation();
            Debug.Log("[SkirmishSharedObjectivePublicWaveValidation] result=Passed logical=80 enemy=22 convoyCommands=5 subjects=2 publicFC=6 publicSubjects=16 missionReadiness=Pending");
        }
    }
}
#endif
