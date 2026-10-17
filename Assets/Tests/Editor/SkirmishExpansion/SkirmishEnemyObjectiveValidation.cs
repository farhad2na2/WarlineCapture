#if UNITY_EDITOR
using UnityEngine;
namespace Game.Tests.Editor
{
    public static class SkirmishEnemyObjectiveValidation
    {
        public static void RunFocusedValidation()
        {
            var pure=new SkirmishEnemyObjectiveAllocationTests();
            pure.FrontlineRetainsTwoOwnedZonesAndDoesNotPileOntoThird();pure.FrontlineNeutralizationNeverPretendsZoneAlreadyOwned();
            pure.VehiclesScreenButCannotSubstituteForCaptureInfantry();pure.BreakthroughWholeSquadFortyFortyTwentyCoverageUsesPublicCorridors();
            pure.UnobservedAndStalePositionsNeverBecomeInterceptDestinations();pure.ConvoyPatrolRequiresTwoDistinctAuthoredRoutePoints();
            pure.VisibleUnrevealedContactCanBeInterceptedWithoutInventedDesignation();pure.TerminalAndWrongPerspectiveObserversNeverAllocate();
            pure.InvalidOwnFactsRejectWholeAllocationBeforeOutput();pure.StableWholeGroupsAndUnhealthyRecoveryAreOrderIndependent();
            pure.VisibleInterceptUsesSharedHysteresisAndDropsIllegalOldContact();
            var native=new SkirmishEnemyObjectiveNativeTests();
            native.NonBaScopeRequiresAllIndependentLogicalPinsAndExcludesUnsupportedStartingAir();
            native.ActualPolicyCompilerBindsObjectiveAndLogicalDigestWithoutChangingBaScope();
            native.MissingActualProviderCannotCreatePlannerOrdersOrSpendStocks();
            native.NonBaStrategyCannotFallThroughToBaCounterOrBaseAttack();
            native.MissingPublicProviderRejectsWithoutReadingOrPublishingHiddenOriginals();
            SkirmishGenericGroundEnemyTests.RunFocusedValidation();
            Debug.Log("[SkirmishEnemyObjectiveValidation] result=Passed tests=22 nativeMissionReadiness=Pending");
        }
    }
}
#endif
