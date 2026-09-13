using System;
using UnityEngine;
public static class M05EditorRegressionValidation
{
    public static void BuildAndRun() {Game.Editor.M05BreachAssaultProductionBuilder.BuildCaptioned();Run();}
    public static void Run()
    {
        int passed=0;
        try
        {
            Action[] suites={M05MovementDisplacementTests.RunFocusedValidation,M05BreachAssaultRuleTests.RunFocusedValidation,M05BreachAssaultIntegrationTests.RunFocusedValidation,
                M01FirstContactMissionRuleTests.RunFocusedValidation,M02EstablishBaseMissionRuleTests.RunFocusedValidation,
                M03RadarWarningRuleTests.RunFocusedValidation,M04AirliftRuleTests.RunFocusedValidation,
                M03SettlementTests.RunFocusedValidation,M03AttemptCleanupTests.RunFocusedValidation,
                M02EstablishBaseResourceTests.RunFocusedValidation};
            foreach(var suite in suites)
            {
                using(ValidationExit.SuppressProcessExit()) {ValidationExit.ClearLastExitCode();suite();if(ValidationExit.LastExitCode!=0)throw new InvalidOperationException(suite.Method.DeclaringType?.Name);}
                passed++;
            }
            Debug.Log("[M05Regression] result=Passed suites="+passed);ValidationExit.Passed();
        }
        catch(Exception e){Debug.LogException(e);Debug.LogError("[M05Regression] result=Failed passedSuites="+passed);ValidationExit.Failed();}
    }
}
