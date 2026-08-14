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

            FirstLaunchArchitectureAlignmentTests architecture = new();
            architecture.RuntimeTypeNamesUseApprovedFirstLaunchBoundaries();
            architecture.UiRuntimeAssemblyPreservesNarrativeDependencyDirection();
            architecture.MenuBootstrapUsesOnlyTheFirstLaunchCompositionBoundary();
            architecture.CompositionOwnerUsesDedicatedProfileShellAndReviewBoundaries();
            architecture.NarrativeRuntimeOwnsRoutePolicyWithoutUiCompositionOrEcsDependencies();
            architecture.SequenceProgressionStaysInPureNarrativeRuntime();
            architecture.NarrativeContractsOwnDomainDataWithoutUiDependencies();
            architecture.ProductionPolicyConsumesAuthoredNarrativeMetadata();
            architecture.PanelResidencyIsAsynchronousAndOutsideSequenceProgression();
            architecture.NarrativeViewsRemainPassiveReferenceAndIntentBoundaries();
            new ScriptArchitectureAlignmentContractTests().RuntimeTypeNamesMustNotIntroduceBroadApplicationLayerSuffixes();
            new NonEcsSystemConversionArchitectureTests().TopLevelGameplayNamingEscapesStayOnApprovedBoundaryList();

            FirstLaunchNarrativeCompositionBoundaryTests compositionBoundaries = new();
            compositionBoundaries.ProfileBoundary_ProjectsStartupDispositionWithoutUiOrEcs();
            compositionBoundaries.ProfileBoundary_PersistsProductionChoicesAndHandoffState();
            compositionBoundaries.ProfileBoundary_ReviewerChoicesDoNotMutateSavedProfile();
            compositionBoundaries.ShellBoundary_HoldsStartupForTypedMissionWithoutRouteRequest();

            FirstLaunchNarrativeRouteUtilitySystemHelperTests routes = new();
            routes.HandoffRule_SeparatesProductionReviewerAndDebriefRoutes();
            routes.SkipRule_RequiresIdentityOrConfirmationAndKeepsDebriefRoute();
            routes.ConfirmedSkipRule_SeparatesReviewerPreviewFromProductionPersistence();

            FirstLaunchNarrativeSequenceUtilitySystemHelperTests sequenceRuntime = new();
            sequenceRuntime.Configure_RejectsDuplicateUnknownAndDisconnectedStates();
            sequenceRuntime.Timeline_EmitsAuthoredLinesAndTransitionsDeterministically();
            sequenceRuntime.ActionValidation_RejectsStaleTokensAndEmitsCurrentSkipOnce();
            sequenceRuntime.NavigationPauseAndSeekRemainDeterministic();

            FirstLaunchNarrativeConfigTests config = new();
            config.SequenceConfig_HasUniqueConnectedStatesAndAllApprovedPanels();
            config.SequenceConfig_AuthorsAudioRouteAndCompletionPolicy();
            config.SequenceConfig_DoesNotDirectlyRetainPanelTextures();
            NarrativePanelAssetResidencyPresentationSystemHelperTests residency = new();
            residency.Residency_KeepsOnlyCurrentAndNextHandles();
            residency.Residency_InvalidReferencesReturnDirectFallbackWithoutHandles();
            residency.Residency_RapidSeekReleasesSupersededRequests();
            residency.Residency_ReleaseAllClearsPendingRequests();

            NarrativePanelMotionPresentationSystemHelperTests motion = new();
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
            presentation.Phase10RPresentation_UsesReadableTypeMobileTargetsAndCleanFrame();
            presentation.Dialogue_LongTextExpandsFrameWithoutEllipsis();
            presentation.Phase10RAudio_UsesIndependentSettingsAwareLayersAndCancelsCleanly();

            FirstLaunchNarrativeSequencePresentationSystemHelperTests player = new();
            player.Player_AdvancesStaticAndDialogueStatesWithoutHierarchyLookup();
            player.Player_EmitsInteractiveSkipAndTypedHandoffOnce();
            player.Player_PauseStepRestartAndCancelAreDeterministic();
            player.Player_DebriefWatchedAndSkippedRoutesShareMandatoryCluePayload();
            player.Player_AutoAdvanceHonorsAuthoredPanelAndLineTiming();

            FirstLaunchNarrativeMenuSceneInstaller.Install();
            FirstLaunchNarrativeMenuIntegrationTests integration = new();
            integration.MenuScene_HasTopLevelNarrativeLayerAndExactConfigs();
            integration.FreshProfile_SkipRequiresLiveConfirmationAndPublishesOneHandoff();
            integration.CompletedAndPendingProfiles_SelectCorrectStartupDisposition();
            integration.ReviewerMode_ProvidesNavigationWithoutMutatingCompletedProfile();
            integration.CommittedIdentity_SkipRoutesDirectlyAndPreservesSelection();

            Debug.Log("[FirstLaunchGate89Validation] result=Passed tests=56");
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
