#if UNITY_EDITOR
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishNativePublicGoalPresentationValidation
    {
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishNativePublicGoalPresentationTests();
            tests.ActualProviderBoundFrontlinePublishesThreePublicZones();
            tests.HiddenEnemyHealthAndPositionNeverEnterFrontlineView();
            tests.PublicTerminalTechnicalFailureIsPreserved();
            tests.MismatchedPublicObjectiveKindRejects();
            tests.MissingNativeAreasRejectInsteadOfFallingBackToBaseAssault();
            tests.MissingSessionRejectsWithoutInventingGoals();
            Debug.Log("[SkirmishNativePublicGoalPresentationValidation] result=Passed focusedTests=6 nativeHudAcceptance=Pending");
        }
    }
}
#endif
