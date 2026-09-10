using System;
using Game.Configs;
using UnityEditor;
using UnityEngine;

public static class M04EditorRegressionValidation
{
    public static void RunCompletionArchitectureCheckpoint()
    {
        Game.Editor.MissionUiSerializedBindingsAuthoring.RepairCommittedPrefabs();
        Game.Editor.Aph700AssemblyDependencyReportGenerator.GenerateFromMenu();
        RunSuite(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
        RunSuite(ProductionSourceGrowthArchitectureTests.RunPostHardeningAuthorizationFocusedValidation);
        ValidationExit.Exit(0);
    }
    public static void RunPresentationAcceptance()
    {
        Game.Editor.M04AirliftPresentationBuilder.Build();
        Game.Editor.M04AirliftEditorProbe.RunLaunch();
    }
    // Architecture failures are reported independently; they must not prevent the live
    // mission audit from collecting its own result. Neither result implies the other passed.
    public static void RunReadinessAudit()
    {
        using(ValidationExit.SuppressProcessExit()) MissionReadinessArchitectureValidation.Run();
        RunCommittedAcceptance();
    }
    public static void RunCommittedAcceptance()
    {
        RunValidation(false);
        if(ValidationExit.LastExitCode==0)Game.Editor.M04AirliftEditorProbe.RunCommittedAcceptance();
    }
    public static void RunFinalAcceptance()
    {
        Game.Editor.M04AirliftPresentationBuilder.Build();
        Run();
        if(ValidationExit.LastExitCode==0)Game.Editor.M04AirliftEditorProbe.RunFinal();
    }
    public static void Run()=>RunValidation(true);
    private static void RunValidation(bool rebuildAssets)
    {
        string locale=GameLocalization.CurrentLocaleCode;int passed=0;
        try
        {
            Action[] suites={M04AirliftRuleTests.RunFocusedValidation,M04AirliftIntegrationTests.RunFocusedValidation,
                rebuildAssets ? MatchHudResourceTextRepairTests.RunFocusedValidation : MatchHudResourceTextRepairTests.RunDeliveredAssetsValidation,RtsCameraSystemTests.RunFocusedValidation,
                UiShellAudioRoutePopupTests.RunFocusedValidation,M03RadarWarningRuleTests.RunFocusedValidation,
                M03CameraFallbackTests.RunFocusedValidation,M03SettlementTests.RunFocusedValidation,
                M03AttemptCleanupTests.RunFocusedValidation,M01FirstContactMissionRuleTests.RunFocusedValidation,
                M02EstablishBaseMissionRuleTests.RunFocusedValidation,M02EstablishBaseResourceTests.RunFocusedValidation};
            foreach(var suite in suites){RunSuite(suite);passed++;}
            foreach(string language in new[]{"en","fa-IR"})
            {GameLocalization.SetLocale(language,false);RunSuite(TransportBoardingScenarioLabTests.RunFocusedValidation);passed++;}
            Debug.Log("[M04EditorRegression] result=Passed suites="+passed);ValidationExit.Passed();
        }
        catch(Exception e){Debug.LogException(e);Debug.LogError("[M04EditorRegression] result=Failed passedSuites="+passed);EditorApplication.Exit(1);}
        finally{GameLocalization.SetLocale(locale,false);}
    }
    private static void RunSuite(Action action)
    {
        using(ValidationExit.SuppressProcessExit())
        {ValidationExit.ClearLastExitCode();action();if(ValidationExit.LastExitCode!=0)throw new InvalidOperationException("Failed suite "+action.Method.DeclaringType?.Name);}
    }
}
