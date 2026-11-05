#if UNITY_EDITOR
using System;
using Game.Configs;
using Game.Editor;
using UnityEngine;
public static class SharedMissionResultRegressionTests
{
    public static void BuildAndRunAndJourney()
    {
        try{SharedMissionResultBuilder.Build();RunAndJourney();}
        catch(Exception e){Debug.LogException(e);MissionEditorValidationExit.Complete(false);}
    }
    public static void RunAndJourney()=>SharedMissionResultValidation.RunThen(RunFocusedAndJourney);
    static void RunFocusedAndJourney()
    {
        try
        {
            GameLocalization.SetLocale("en",false);
            Check(ModeResultStarRulesTests.RunFocusedValidation);
            Check(M01FirstContactHudResultTests.RunFocusedValidation);
            Check(M02EstablishBaseHudResultTests.RunFocusedValidation);
            Check(M02EstablishBaseResultSettlementTests.RunFocusedValidation);
            Check(Game.Tests.Editor.SkirmishTerminalPresentationObservationTests.RunFocusedValidation);
            new Game.Tests.Editor.SkirmishLibraryJourneyTests().ResultCardShowsTheFirstMedalAndBattlefieldBack();
            Debug.Log("[SharedMissionResultRegressions] result=Passed suites=6 fixtures=true missionReadiness=NotClaimed");
            CampaignTutorialEntryEditorProbe.RunApprovedAtlasM01Journey();
        }
        catch(Exception e){Debug.LogException(e);MissionEditorValidationExit.Complete(false);}
    }
    public static void RunSkirmishJourney()
    {
        SkirmishS002AriaRunHarness.RunS002ResultUiEn();
    }
    static void Check(Action test)
    {
        ValidationExit.ClearLastExitCode();
        using(ValidationExit.SuppressProcessExit())test();
        if(ValidationExit.LastExitCode is int code&&code!=0)throw new InvalidOperationException(test.Method.Name+" failed");
    }
}
#endif
