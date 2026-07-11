using System;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Editor;
using UnityEngine;

public static class FirstLaunchGate89Validation
{
    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchNarrativeMenuSceneInstaller.Install();

            FirstLaunchNarrativeConfigTests config = new();
            config.SequenceConfig_HasUniqueConnectedStatesAndAllApprovedPanels();
            config.SequenceConfig_DoesNotDirectlyRetainPanelTextures();
            new NarrativePanelAssetResidencyTests().Residency_KeepsOnlyCurrentAndNextHandles();

            NarrativePanelMotionTests motion = new();
            foreach (NarrativeMotionPreset preset in new[]
                     {
                         NarrativeMotionPreset.PushIn,
                         NarrativeMotionPreset.PullBack,
                         NarrativeMotionPreset.DriftLeft,
                         NarrativeMotionPreset.DriftRight,
                         NarrativeMotionPreset.StaticImpact
                     })
            {
                motion.MotionPresets_RemainFullBleedAndBounded(preset);
            }
            motion.ReducedMotion_IsStaticWithoutChangingTimeline();

            NarrativeReviewerControlsViewTests reviewer = new();
            reviewer.Bind_EmitsEveryReviewerActionOnce();
            reviewer.RepeatedBind_ReplacesDelegateWithoutDuplicatingListeners();
            reviewer.Unbind_StopsActionEmission();
            reviewer.Setters_ProjectStateWithoutEmittingActions();
            reviewer.StateAndProgressSetters_ClampInvalidInput();

            FirstLaunchNarrativePresentationTests presentation = new();
            presentation.PresentationPrefab_HasBoundViewsSkipAndDedicatedVoiceSource();
            presentation.PresentationHelper_RespectsAutoAdvancePauseAndCancel();

            FirstLaunchNarrativePlayerTests player = new();
            player.Player_AdvancesStaticAndDialogueStatesWithoutHierarchyLookup();
            player.Player_EmitsInteractiveSkipAndTypedHandoffOnce();
            player.Player_PauseStepRestartAndCancelAreDeterministic();
            player.Player_DebriefWatchedAndSkippedRoutesShareMandatoryCluePayload();
            player.Player_AutoAdvanceHonorsAuthoredPanelAndLineTiming();

            FirstLaunchNarrativeMenuSceneInstaller.Install();
            FirstLaunchNarrativeMenuIntegrationTests integration = new();
            integration.MenuScene_HasHiddenTopLevelNarrativeLayerAndExactConfigs();
            integration.FreshProfile_SkipRequiresLiveConfirmationAndPublishesOneHandoff();
            integration.CompletedAndPendingProfiles_SelectCorrectStartupDisposition();
            integration.ReviewerMode_ProvidesNavigationWithoutMutatingCompletedProfile();
            integration.CommittedIdentity_SkipRoutesDirectlyAndPreservesSelection();

            Debug.Log("[FirstLaunchGate89Validation] result=Passed tests=26");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[FirstLaunchGate89Validation] result=Failed");
            ValidationExit.Failed();
        }
    }
}
