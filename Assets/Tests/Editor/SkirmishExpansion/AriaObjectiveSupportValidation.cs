namespace Game.Tests.Editor
{
    public static class AriaObjectiveSupportValidation
    {
        public static void Run()
        {
            AriaObjectiveSupportTests.RunFocusedValidation();
            var aircraft=new AriaAircraftServiceTests();
            aircraft.ExactActorReceiptContactAndHomeAreAllRequired();aircraft.ReplacementAircraftCannotCompleteAnOldReturn();
            aircraft.WrongReturnButtonContactCannotCertifyService();aircraft.PublicContactRejectsWrongFrameAndScreenWithoutInventingAcceptance();
            aircraft.MultipleAircraftDoNotForceAggregateReturnOrSuppressObjectiveOrder();
            aircraft.ActualNormalReturnPublishesOnlyExactOwnedActor(false);aircraft.ActualNormalReturnPublishesOnlyExactOwnedActor(true);
            var scan=new AriaPublicScanTests();scan.ScanUsesNormalPresentedWorldContactAndRequiresNativeReceipt();
            scan.ExactAcceptedScanWaitsForActualCompletionThenReturnsToObjective();scan.WrongGoalOrContactCannotCertifyAcceptedScan();
            scan.ActorReplacementAndPublicGoalChangeAbortWithoutSuccess();scan.StopAndRiskPreserveAlreadyAcceptedNativeOrders();scan.InterruptedScanRequiresFreshTargetAndNewRequestOnReentry();
            var native=new AriaPublicScanNativeTests();
            for(int i=0;i<4;i++)native.ActualNativeDurationCompletionIsDistinctFromDeathReplacementAndDisappearance(i);
            UnityEngine.Debug.Log("[AriaObjectiveSupportValidation] result=Passed controls=32 board=Pending normalMissionAcceptance=Pending");
        }
    }
}
