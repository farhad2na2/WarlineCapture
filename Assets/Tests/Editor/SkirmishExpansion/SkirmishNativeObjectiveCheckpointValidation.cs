#if UNITY_EDITOR
namespace Game.Tests.Editor
{
    public static class SkirmishNativeObjectiveCheckpointValidation
    {
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishNativeObjectiveCheckpointTests();
            tests.SchemaPreservesBaseAssaultV7AndRejectsUnownedVersionCrossovers();
            tests.NativePayloadRejectsClockIdentityRosterAndReducerMutations();
            tests.NativeDescriptorChecksumCoversActualObjectivePayloadBytes();
            tests.MissingFreshBindingRejectsPreparationWithoutNativeMutation();
            tests.RealNativeCommitRollsBackSessionPartsWhenObjectiveBindingDisappears();
            tests.ObjectiveMarkerCannotSpliceOntoOrdinarySavedActorOrCrossIntoBaseAssault();
            UnityEngine.Debug.Log("[SkirmishNativeObjectiveCheckpointValidation] result=Passed tests=6 missionCertified=0 freshNativeJourney=0");
        }
    }
}
#endif
