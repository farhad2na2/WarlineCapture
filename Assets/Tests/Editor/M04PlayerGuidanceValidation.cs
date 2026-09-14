using System;
using System.Reflection;
using Game.Editor;
using UnityEditor;
using UnityEngine;
public static class M04PlayerGuidanceValidation
{
    public static void RunVisualFinal()
    { Run(); Game.Editor.M04AirliftEditorProbe.RunPlayerGuidanceVisualFa(); }

    public static void RunPlayerFa() { Run(); Game.Editor.M04AirliftEditorProbe.RunPlayerGuidanceFa(); }
    public static void RunPlayerWideFa() { Run(); Game.Editor.M04AirliftEditorProbe.RunPlayerGuidanceWideFa(); }
    public static void RunPlayerEn() { Run(); Game.Editor.M04AirliftEditorProbe.RunPlayerGuidanceEn(); }

    public static void RunFinalEn()
    {
        using(ValidationExit.SuppressProcessExit())
        {
            ValidationExit.ClearLastExitCode();
            RunRegression();
            if(ValidationExit.LastExitCode is int code && code!=0) throw new Exception("Guidance regression failed");
        }
        Game.Editor.M04AirliftEditorProbe.RunPlayerGuidanceEn();
    }

    public static void RunRegression()
    {
        Run();
        MissionTutorialNextActionValidation.Run();
        HudRightColumnLayoutValidation.RunLayoutOnly();
        MissionReadinessArchitectureValidation.Run();
    }

    private static void FeedbackBindingKeepsRuntimeInstruction()
    {
        var go=new GameObject("Inactive feedback binding",typeof(RectTransform));go.SetActive(false);
        try
        {
            var text=go.AddComponent<TMPro.TextMeshProUGUI>();
            var binding=go.AddComponent<Game.UI.Runtime.V3LocalizedTextBindingView>();
            binding.Configure("authoring.example","ATTACK UNAVAILABLE");
            typeof(Game.UI.Runtime.BattleHudRuntimeFeedbackView).GetMethod("SetText",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{text,"QA selection instruction"});
            go.SetActive(true);binding.ApplyLocalization();
            NUnit.Framework.Assert.AreEqual("QA selection instruction",text.text,"Reactivation must retain the current command instruction.");
        }
        finally {UnityEngine.Object.DestroyImmediate(go);}
    }

    public static void Run()
    {
        try
        {
            typeof(M04AirliftPresentationBuilder).GetMethod("ImportCopy",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            AssetDatabase.SaveAssets();

            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode();
                MatchHudAssistantUiSystemHelperTests.RunShowMeValidation();
                if(ValidationExit.LastExitCode is int code && code!=0) throw new Exception("Show Me fixture failed");
                M04AirliftIntegrationTests.RunFocusedValidation();
                if(ValidationExit.LastExitCode!=0) throw new Exception("M4 integration failed");
            }
            FeedbackBindingKeepsRuntimeInstruction();
            new M04AirliftIntegrationTests().RescueSelectionExcludesOverlappingVehiclesOnlyDuringPassengerLessons();
            new M04AirliftIntegrationTests().LandingHoldInstructionDistinguishesCountdownContestedAndReturn();
            new M04AirliftIntegrationTests().BilingualGuidanceCopyFitsNarrationMessages();
            new M04AirliftIntegrationTests().CommandStateReportsPersistentSelectionModeAndClearsAfterExit();
            new M04AirliftIntegrationTests().TutorialTargetsFollowSelectionTransportAndDestinationForEveryActionLesson();
            Debug.Log("[M04PlayerGuidance] result=Passed selection=live partial=missing-person showMe=visibility-driven");
        }
        catch(Exception error) {Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
