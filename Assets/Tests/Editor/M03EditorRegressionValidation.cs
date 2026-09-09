using System;
using System.Linq;
using UnityEngine;

public static class M03EditorRegressionValidation
{
    public static void Run()
    {
        int passed=0;
        try
        {
            RunSuite(M03RadarWarningRuleTests.RunFocusedValidation); passed++;
            RunSuite(M03RadarPingTests.RunFocusedValidation); passed++;
            RunSuite(M03WarningInteractionTests.RunFocusedValidation); passed++;
            RunSuite(UiShellAudioRoutePopupTests.RunFocusedValidation); passed++;
            RunSuite(M03NarrativeFlowTests.RunFocusedValidation); passed++;
            RunSuite(M03SettlementTests.RunFocusedValidation); passed++;
            RunSuite(M01FirstContactMissionRuleTests.RunFocusedValidation); passed++;
            RunSuite(M02EstablishBaseMissionRuleTests.RunFocusedValidation); passed++;
            RunSuite(M02EstablishBaseResourceTests.RunFocusedValidation); passed++;
            RunSuite(M02EstablishBaseNarrativeTests.RunFocusedValidation); passed++;
            RunSuite(ProductionSourceGrowthArchitectureTests.RunFocusedValidation); passed++;
            Debug.Log($"[M03EditorRegressionValidation] result=Passed suites={passed}"); ValidationExit.Passed();
        }
        catch(Exception exception)
        {Debug.LogException(exception); Debug.LogError($"[M03EditorRegressionValidation] result=Failed passedSuites={passed}"); ValidationExit.Failed();}
    }
    private static void RunSuite(Action run)
    {
        using(ValidationExit.SuppressProcessExit())
        {
            ValidationExit.ClearLastExitCode(); run();
            if(ValidationExit.LastExitCode!=0) throw new InvalidOperationException("Failed suite: "+run.Method.DeclaringType?.Name);
        }
    }
    public static void RunAcquisitionRegression()
    {
        try
        {
            var tests=new AutomaticCombatFactionTargetingTests();
            tests.UnitEngagementSystem_IgnoresNeutralCitizenTargets();
            tests.UnitEngagementSystem_HoldAcquiresHostileAndHonorsSuppressionWithRealEcbSingleton();
            tests.BuildingDefenseAttackSystem_IgnoresNeutralCitizenTargetsAndAttacksHostileTargets();
            new SelectionCommandRequestResultContractTests().UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea();
            Debug.Log("[M03AcquisitionRegression] result=Passed tests=4"); ValidationExit.Passed();
        }
        catch(Exception error) {Debug.LogException(error); Debug.LogError("[M03AcquisitionRegression] result=Failed"); ValidationExit.Failed();}
    }
    public static void RunPersistenceRegression()
    {
        int passed=0;
        try
        {
            foreach(var method in typeof(SaveServiceTests).GetMethods().Where(m=>m.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute),true).Length>0))
            {
                var test=new SaveServiceTests(); test.SetUp();
                try {method.Invoke(test,null); passed++;}
                finally {test.TearDown();}
            }
            Debug.Log($"[M03PersistenceRegression] result=Passed tests={passed}"); ValidationExit.Passed();
        }
        catch(Exception error) {Debug.LogException(error); Debug.LogError("[M03PersistenceRegression] result=Failed"); ValidationExit.Failed();}
    }
    public static void RunArchitectureAudit()
    {
        int passed=0,failed=0;
        var tests=new ProductionSourceGrowthArchitectureTests();
        foreach(var method in typeof(ProductionSourceGrowthArchitectureTests).GetMethods()
            .Where(m=>m.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute),true).Length>0))
        {
            try {method.Invoke(tests,null); passed++; Debug.Log("[M03ArchitectureAudit] pass="+method.Name);}
            catch(Exception error) {failed++; Debug.LogError("[M03ArchitectureAudit] fail="+method.Name+" "+(error.InnerException??error));}
        }
        Debug.Log($"[M03ArchitectureAudit] result={(failed==0 ? "Passed" : "Failed")} passed={passed} failed={failed}");
        ValidationExit.Exit(failed==0 ? 0 : 1);
    }
    public static void RunInteractionRegression()
    {
        try
        {
            Game.Editor.M03RadarWarningUiBuilder.Build();
            RunSuite(M03WarningInteractionTests.RunFocusedValidation);
            RunSuite(M03CameraFallbackTests.RunFocusedValidation);
            RunSuite(M03PresentationTests.RunFocusedValidation);
            RunSuite(AssistantCommandIntentGatewayTests.RunFocusedValidation);
            RunSuite(AssistantCommandIntentSystemTests.RunFocusedValidation);
            // This legacy runner throws on failure instead of recording ValidationExit.
            FocusedUnitCommandSystemTests.RunFocusedValidation();
            RunSuite(AriaTutorialPresentationV3PrefabTests.RunFocusedValidation);
            Debug.Log("[M03InteractionRegression] result=Passed suites=7"); ValidationExit.Passed();
        }
        catch(Exception exception)
        {Debug.LogException(exception); Debug.LogError("[M03InteractionRegression] result=Failed"); ValidationExit.Failed();}
    }
    public static void RunCampaignRegression()
    {
        string originalLocale=Game.Configs.GameLocalization.CurrentLocaleCode;
        try
        {
            Game.Configs.GameLocalization.SetLocale("en",false);
            RunSuite(M03CampaignEntryTests.RunFocusedValidation);
            RunSuite(M01FirstContactCampaignUiTests.RunFocusedValidation);
            RunSuite(M02EstablishBaseCampaignUiTests.RunFocusedValidation);
            RunSuite(M01FirstContactMissionBriefingTests.RunFocusedValidation);
            Debug.Log("[M03CampaignRegression] result=Passed suites=4"); ValidationExit.Passed();
        }
        catch(Exception exception)
        {Debug.LogException(exception); Debug.LogError("[M03CampaignRegression] result=Failed"); ValidationExit.Failed();}
        finally {Game.Configs.GameLocalization.SetLocale(originalLocale,false);}
    }
    public static void RunCleanupRegression()
    {
        try
        {
            RunSuite(M03AttemptCleanupTests.RunFocusedValidation);
            RunSuite(M02EstablishBaseLifecycleTests.RunFocusedValidation);
            Debug.Log("[M03CleanupRegression] result=Passed suites=2"); ValidationExit.Passed();
        }
        catch(Exception exception)
        {Debug.LogException(exception); Debug.LogError("[M03CleanupRegression] result=Failed"); ValidationExit.Failed();}
    }
    public static void RunEditorBehaviorAudit()
    {
        string originalLocale=Game.Configs.GameLocalization.CurrentLocaleCode;
        int passed=0,failed=0;
        try
        {
            Game.Editor.M03RadarWarningUiBuilder.Build();
            Game.Configs.GameLocalization.SetLocale("en",false);
            Action[] suites={RunAcquisitionRegression,RunPersistenceRegression,M03RadarWarningRuleTests.RunFocusedValidation,M03RadarPingTests.RunFocusedValidation,
                M03WarningInteractionTests.RunFocusedValidation,UiShellAudioRoutePopupTests.RunFocusedValidation,
                M03NarrativeFlowTests.RunFocusedValidation,M03SettlementTests.RunFocusedValidation,M03ActiveClassCommandTests.RunFocusedValidation,M03MapIsolationTests.RunFocusedValidation,
                M01FirstContactMissionRuleTests.RunFocusedValidation,M01FirstContactHudRestrictionTests.RunFocusedValidation,
                M02EstablishBaseMissionRuleTests.RunFocusedValidation,
                M02EstablishBaseResourceTests.RunFocusedValidation,M02EstablishBaseNarrativeTests.RunFocusedValidation,
                M03CameraFallbackTests.RunFocusedValidation,M03PresentationTests.RunFocusedValidation,
                AssistantCommandIntentGatewayTests.RunFocusedValidation,AssistantCommandIntentSystemTests.RunFocusedValidation,
                ()=>{FocusedUnitCommandSystemTests.RunFocusedValidation(); ValidationExit.Passed();},
                AriaTutorialPresentationV3PrefabTests.RunFocusedValidation,M03CampaignEntryTests.RunFocusedValidation,
                M01FirstContactCampaignUiTests.RunFocusedValidation,M02EstablishBaseCampaignUiTests.RunFocusedValidation,
                M01FirstContactMissionBriefingTests.RunFocusedValidation,M03AttemptCleanupTests.RunFocusedValidation,
                M02EstablishBaseLifecycleTests.RunFocusedValidation,M02EstablishBaseLaunchTests.RunFocusedValidation,
                MatchHudCommandFeedbackPanelTests.RunFocusedValidation,RtsSelectionInputSystemTests.RunFocusedValidation,
                FirstLaunchNarrativeMenuIntegrationTests.RunFocusedValidation};
            foreach(var suite in suites)
            {
                try {RunSuite(suite); passed++;}
                catch(Exception error) {failed++; Debug.LogError("[M03EditorBehaviorAudit] suite="+suite.Method.DeclaringType?.Name+" "+error);}
            }
        }
        catch(Exception exception) {failed++; Debug.LogException(exception);}
        finally {Game.Configs.GameLocalization.SetLocale(originalLocale,false);}
        Debug.Log($"[M03EditorBehaviorAudit] result={(failed==0 ? "Passed" : "Failed")} passed={passed} failed={failed}");
        ValidationExit.Exit(failed==0 ? 0 : 1);
    }
}
