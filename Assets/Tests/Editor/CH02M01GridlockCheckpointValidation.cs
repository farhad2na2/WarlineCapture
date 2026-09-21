using Game.Editor;
using UnityEngine;
public static class CH02M01GridlockCheckpointValidation
{
    public static void RunArchive()
    {
        try
        {
            Run();
            var t=new CH02M01GridlockRuleTests();
            t.InterruptedWorkResumesWithoutClearingEarly();t.PauseFreezesHoldAndDepartureResetsIt();
            t.LossPrecedesArrivalAndOneWorkerIsRecoverable();t.MissingInitializationCannotWin();
            t.AllMandatoryLossesFail();t.ConnectivityAndArrivalAreIndependent();t.ReserveEntersAfterWarningWithoutChangingHealth();
            Debug.Log("[GridlockRules] result=Passed cases=9");
            CH02M01GridlockArchiveProbe.Run();
        }
        catch(System.Exception error){Debug.LogException(error);MissionEditorValidationExit.Complete(false);}
    }
    public static void RunWatch()
    {
        try
        {
            Run();
            new CH02M01GridlockRuleTests().ReserveEntersAfterWarningWithoutChangingHealth();
            Debug.Log("[GridlockReserve] result=Passed warning-release-and-linked-presentation");
            CH02M01GridlockWatchProbe.Run();
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
            MissionEditorValidationExit.Complete(false);
        }
    }

    public static void Run()
    {
        new CH02M01GridlockRuleTests().ChapterOpeningPersistsWithoutGrantingRewards();
        Debug.Log("[GridlockChapterOpening] result=Passed persistence-and-no-rewards");
        new CH02M01GridlockRuleTests().CrewSelectionUsesMissionIdentity();
        Debug.Log("[GridlockCrewSelection] result=Passed");
        MatchHudAssistantUiSystemHelperTests.RunShowMeValidation();
        Debug.Log(AriaPlayDecisionValidation.Run());
        CH02M01GridlockProductionBuilder.BuildCaptioned();
        CH02M01GridlockMediaImporter.ValidateStableArtImports();
        Debug.Log("[GridlockCheckpoint] result=Passed scope=selection-and-captioned-build acceptance=Pending");
    }
}
