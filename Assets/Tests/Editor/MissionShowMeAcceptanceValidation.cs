using System;
using UnityEngine;
public static class MissionShowMeAcceptanceValidation
{
    public static void RunFinalChecks()
    {
        try
        {
            MatchHudAssistantUiSystemHelperTests.RunShowMeValidation();
            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode(); MatchHudAssistantUiSystemHelperTests.RunFocusedValidation();
                if(ValidationExit.LastExitCode!=0) throw new Exception("ARIA regression failed.");
            }
            new ProductionSourceGrowthArchitectureTests().AllBaselinedPathsRespectRatchetedLineAndByteCeilings();
            Debug.Log("[MissionShowMeFinalChecks] result=Passed popup,embedded,source-growth");
        }
        catch(Exception error) {Debug.LogException(error);UnityEditor.EditorApplication.Exit(1);}
    }

    public static void Run()
    {
        try
        {
            MatchHudAssistantUiSystemHelperTests.RunShowMeValidation();
            MissionTutorialNextActionValidation.Run();
            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode(); MissionReadinessArchitectureValidation.Run();
                if(ValidationExit.LastExitCode!=0) throw new Exception("Architecture failed.");
            }
            Debug.Log("[MissionShowMeAcceptance] result=Passed");
            ValidationExit.Exit(0);
        }
        catch(Exception error) {Debug.LogException(error);UnityEditor.EditorApplication.Exit(1);}
    }
}
