using System;
using UnityEngine;
public static class MissionMobileAuditFixValidation
{
    public static void RunCorrections()
    {
        Game.Editor.MissionMobileAuditFixBuilder.Build();
        new MissionMobileAuditFixTests().PassengerDrawerOpensWithoutAnEcsDataChange();
        new MissionMobileAuditFixTests().RescuePassengerIdentityAndHealthAreReadable();
        new MissionMobileAuditFixTests().DefenseReinforcementDoesNotRequireAProductionObjective();
        new M04AirliftIntegrationTests().SelectionLessonRequiresAllFourSpecialists();
        new M03PresentationTests().AllTwelveAriaLessonsFitBothLanguagesAndTextSizes();
        new M03PresentationTests().AllTwelveM04AriaLessonsFitBothLanguagesAndTextSizes();
        new EcsBurstHotPathArchitectureTests().HotPathArraySnapshotDebtMustNotIncrease();
        Debug.Log("[MissionMobileFixCorrections] result=Passed layouts=96 selection=4 required firstOpen=visible hotPathSnapshots=0");
    }

    public static void Run()
    {
        bool failed=false;
        Action[] suites={MissionMobileAuditFixTests.RunFocusedValidation,M03PresentationTests.RunFocusedValidation,
            M03EditorRegressionValidation.Run,M04EditorRegressionValidation.Run,
            M03EditorRegressionValidation.RunInteractionRegression,M03EditorRegressionValidation.RunAcquisitionRegression,
            M03EditorRegressionValidation.RunPersistenceRegression,MissionReadinessArchitectureValidation.Run};
        foreach(var suite in suites)
        {
            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode();
                try{suite();if(ValidationExit.LastExitCode!=0)throw new InvalidOperationException("Nonzero validation result");}
                catch(Exception error){failed=true;Debug.LogError("[MissionMobileFixValidation] fail="+suite.Method.DeclaringType?.Name+"."+suite.Method.Name+" "+error);}
            }
        }
        Debug.Log("[MissionMobileFixValidation] result="+(failed?"Failed":"Passed"));ValidationExit.Exit(failed?1:0);
    }
}
