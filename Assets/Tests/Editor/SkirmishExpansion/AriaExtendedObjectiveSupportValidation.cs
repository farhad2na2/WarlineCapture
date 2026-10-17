namespace Game.Tests.Editor
{
    public static class AriaExtendedObjectiveSupportValidation
    {
        public static void Run()
        {
            AriaCompleteObjectiveSupportValidation.Run();
            var t=new AriaOffensiveAirTests();
            t.NormalAttackModeAndExactPublicGroundContactAreRequired();t.GestureCompletionAndStaleReceiptCannotCountAsAirPressure();
            t.NewExactActorOrderRequiresNativeGoalInsideDisplayedObjective();t.ReplacementOrReturningAircraftYieldsWithoutCancellingNativeOrders();
            t.ExistingCombatAssignmentAndPublicRiskPreemptNewPressure();t.StopClearsOnlyPolicyAndFreshAttemptCannotReuseOldContact();
            t.RecruitmentChoosesLegalAirCounterOrAvailableGroundPressureRole();t.PendingAndExistingPressureRespectCapsWithoutRoleSubstitution();
            t.TransportRecruitmentIsUsefulOnlyForEvacuationAndReconRemainsNonCombat();
            t.RecoveredNativeScanWithoutEphemeralReceiptIsPreservedWithoutInventedCompletion();t.PeriodicReturnCannotInterruptAnExistingRecoveredNativeScan();
            t.RequestedFighterCannotBeCertifiedByAnotherRolesPendingQueue();
            t.ReadinessRequiresAUsefulLegallyQuotedFutureRoleAndNormalResearchEligibility();
            var native=new AriaOffensiveAirNativeTests();for(int i=0;i<6;i++)native.NativeAcceptedOrderReadoutRejectsForeignUnarmedChangedAndDuplicateSelection(i);
            new SkirmishNativeCheckpointPartsTests().RestoredClocksPreserveScanDropAndHaulProgressAcrossSeparateTimeOrigins();
            var footprint=new AriaBoardingFootprintTests();
            footprint.ActualOddAndEvenFootprintsRejectShiftedExitContainment(3,3);
            footprint.ActualOddAndEvenFootprintsRejectShiftedExitContainment(4,2);
            footprint.NativeOriginAndCellScaleAreAppliedToAllCorners();
            footprint.MissingAndMalformedNativeFootprintsRemainUnknown();
            footprint.NativeDestinationRequiresAllOddAndEvenFootprintCornersInside(3,3);
            footprint.NativeDestinationRequiresAllOddAndEvenFootprintCornersInside(4,2);
            UnityEngine.Debug.Log("[AriaExtendedObjectiveSupport] result=Passed controls=70 normalMissionAcceptance=Pending");
        }
    }
}
