namespace Game.Tests.Editor
{
    public static class AriaCompleteObjectiveSupportValidation
    {
        public static void Run()
        {
            AriaObjectiveSupportValidation.Run();
            var board=new AriaPublicBoardingTests();board.BoardUsesNormalCarrierTouchAndCannotCompleteFromGesture();
            board.FerryRequiresFullMountAndAcceptedNormalMoveBeforeExitUnload();board.PartialBoardingCannotDispatchCarrierMove();
            board.CarrierReplacementBlocksWithoutRebindingOrUnload();board.FreshMountedRosterPreservesExistingCarrierMoveWithoutOldReceipt();
            board.PublicBoardContactRejectsStaleScreenAndUnpresentedNativeResult();board.StopPreservesAcceptedOrdersAndTimeoutCannotUnloadOutsideExit();
            var native=new AriaPublicBoardingNativeTests();
            for(int i=0;i<5;i++)native.ActualPublicOriginalReadoutRejectsPartialForeignAndBrokenReciprocalEdges(i);
            UnityEngine.Debug.Log("[AriaCompleteObjectiveSupport] result=Passed controls=44 normalMissionAcceptance=Pending");
        }
    }
}
