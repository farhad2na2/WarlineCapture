using System;
using UnityEngine;
public static class MissionTutorialNextActionValidation
{
    public static void RunPlacementAcceptance()
    {
        using(ValidationExit.SuppressProcessExit())
        {
            ValidationExit.ClearLastExitCode();
            MissionReadinessArchitectureValidation.Run();
            if(ValidationExit.LastExitCode != 0) {UnityEditor.EditorApplication.Exit(1);return;}
        }
        RunBuildAcceptance();
    }

    public static void RunBuildAcceptance()
    {
        Run();
        Game.Editor.M03RadarWarningEditorLaunchProbe.RunTutorialBuildClicks();
    }

    public static void Run()
    {
        bool failed=false;
        Game.Editor.V3UiLocalizationCatalogBuilder.ApplyConfiguredUiStrings();
        Action[] suites={
            () => new BuildingPlacementPointerTests().CrowdedMissionCenterFindsLegalOuterPlotWithinFixedBudget(),
            () => new BuildingPlacementPointerTests().TapAndDragReachNewPlotAndReleaseUsesFinalPosition(),
            () => new BuildingPlacementPointerTests().PlacementUiPressDoesNotMovePreview(),
            BuildingPlacementConstructionTransactionTests.RunFocusedValidation,
            () => new M03PlayerBuiltDefenseGuidanceTests().ExistingMapDefensesDoNotCompleteLessonButPlayerPlacementDoes(),
            () => new TutorialCueOverlayTests().NextClickFrameIsAbovePopupAndNeverBlocksItsButton(),
            () => new M04AirliftIntegrationTests().TutorialTargetsFollowSelectionTransportAndDestinationForEveryActionLesson(),
            () => new ProductionSourceGrowthArchitectureTests().JenkinsEditModeGateFailsClosed(),
            () => new ProductionSourceGrowthArchitectureTests().JenkinsCheckoutProvidesGuardInputsAndHistory(),
            () => new M02EstablishBaseGuidanceTests().DefenseBuildCueSkipsSelectedTabAndFollowsRealItemThenPlaceClicks(),
            () => new M02EstablishBaseGuidanceTests().RifleDoItClicksExactlyOneControlAndNeverRetriesPastIt(true,false),
            () => new M02EstablishBaseGuidanceTests().RifleDoItClicksExactlyOneControlAndNeverRetriesPastIt(false,false),
            () => new M02EstablishBaseGuidanceTests().RifleDoItClicksExactlyOneControlAndNeverRetriesPastIt(true,true),
            M01FirstContactGuidanceTests.RunFocusedValidation,M02EstablishBaseGuidanceTests.RunFocusedValidation,
            MatchHudAssistantUiSystemHelperTests.RunFocusedValidation,M03RadarWarningRuleTests.RunFocusedValidation,
            M04AirliftIntegrationTests.RunFocusedValidation};
        foreach(var suite in suites)
        {
            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode();
                try {suite(); if(ValidationExit.LastExitCode is int code && code!=0) throw new Exception("Validation reported failure");}
                catch(Exception error) {failed=true; Debug.LogError("[TutorialNextAction] fail="+suite.Method.Name+" "+error);}
            }
        }
        Debug.Log("[TutorialNextAction] result="+(failed?"Failed":"Passed"));
        if(failed) UnityEditor.EditorApplication.Exit(1);
    }
}
