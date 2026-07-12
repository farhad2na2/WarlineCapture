using System;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.Narrative.Runtime;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;

namespace Game.Composition
{
    internal enum FirstLaunchNarrativeStartupDisposition
    {
        EnterMenu = 0,
        Playing = 1,
        ResumeHandoff = 2
    }

    internal sealed class FirstLaunchNarrativeCompositionSystemHelper
    {
        private readonly FirstLaunchNarrativeSequencePresentationSystemHelper sequencePresentation = new();
        private readonly FirstLaunchNarrativeProfileCompositionSystemHelper profileComposition = new();
        private readonly FirstLaunchNarrativeShellCompositionSystemHelper shellComposition = new();
        private readonly FirstLaunchNarrativeReviewPresentationSystemHelper reviewPresentation = new();
        private NarrativeSequenceView view;
        private bool initialized;
        private bool skipConfirmationPending;
        private string skipConfirmationReviewerStateId = string.Empty;
        public event Action MatchHandoffRequested;
        public event Action<bool> SkipConfirmationVisibilityChanged;

        public bool IsPlaying => initialized && sequencePresentation.IsRunning;
        public bool IsSkipConfirmationPending => skipConfirmationPending;
        public string CurrentStateId => sequencePresentation.CurrentStateId;
        public NarrativeCompletionPayload LastReviewerCompletion => reviewPresentation.LastCompletion;

        public FirstLaunchNarrativeStartupDisposition Initialize(
            NarrativeSequenceConfig config,
            NarrativeSpeakerCatalog speakers,
            NarrativePunctuationConfig punctuation,
            NarrativeSequenceView sequenceView,
            IGameTextResolver textResolver,
            SaveService persistence,
            bool bypassForDiagnostics,
            bool startInReviewerMode = false)
        {
            if (initialized)
                return ResolveCurrentDisposition();

            initialized = true;
            profileComposition.Initialize(
                persistence,
                FirstLaunchNarrativeModelUtilitySystemHelper.FindStateId(
                    config,
                    NarrativeRouteRole.CommanderIdentity),
                FirstLaunchNarrativeModelUtilitySystemHelper.FindStateId(
                    config,
                    NarrativeRouteRole.GuidanceChoice));
            view = sequenceView;
            if (view?.SkipConfirmationView != null)
            {
                view.SkipConfirmationView.Bind(ConfirmSkip, CancelSkip);
                view.SkipConfirmationView.SetVisible(false);
            }

            if (profileComposition.ShouldEnterMenu(bypassForDiagnostics, startInReviewerMode))
            {
                view?.SetVisible(false);
                return FirstLaunchNarrativeStartupDisposition.EnterMenu;
            }

            if (profileComposition.ShouldResumeHandoff(startInReviewerMode))
            {
                view?.SetVisible(false);
                shellComposition.RequestHandoff();
                return FirstLaunchNarrativeStartupDisposition.ResumeHandoff;
            }

            if (!sequencePresentation.Initialize(config, speakers, punctuation, view, textResolver, SettingsService.Load()))
            {
                view?.SetVisible(false);
                return FirstLaunchNarrativeStartupDisposition.EnterMenu;
            }

            sequencePresentation.InteractiveStateRequested += HandleInteractiveState;
            sequencePresentation.CommanderIdentityCommitted += HandleCommanderIdentityCommitted;
            sequencePresentation.GuidanceCommitted += HandleGuidanceCommitted;
            sequencePresentation.HandoffRequested += HandleWatchedHandoff;
            sequencePresentation.SkipRequested += HandleSkipRequested;
            reviewPresentation.Initialize(startInReviewerMode, view, sequencePresentation);
            profileComposition.MarkInProgress(startInReviewerMode);
            sequencePresentation.Start();
            reviewPresentation.Refresh(true);
            return FirstLaunchNarrativeStartupDisposition.Playing;
        }

        public void InitializeShell(
            MenuBootstrapView menuView,
            IGameTextResolver textResolver,
            bool bypassForDiagnostics,
            bool startInReviewerMode)
        {
            FirstLaunchNarrativeStartupDisposition disposition = Initialize(
                menuView.FirstLaunchNarrativeConfig,
                menuView.FirstLaunchSpeakerCatalog,
                menuView.FirstLaunchPunctuationProfile,
                menuView.FirstLaunchNarrativeView,
                textResolver,
                SaveService.CreateDefault(),
                bypassForDiagnostics,
                startInReviewerMode);
            shellComposition.SetStartupDisposition(disposition);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!initialized)
                return;
            sequencePresentation.Tick(unscaledDeltaTime);
            reviewPresentation.Tick();
            if (shellComposition.TryPublishHandoff())
                MatchHandoffRequested?.Invoke();
        }

        public void ApplyShellState(EntityManager entityManager, Entity boundary)
        {
            shellComposition.Apply(entityManager, boundary);
        }

        public static void ResetShellState(EntityManager entityManager, Entity boundary)
        {
            FirstLaunchNarrativeShellCompositionSystemHelper.ResetBoundary(entityManager, boundary);
        }

        public void ConfirmSkip()
        {
            if (!skipConfirmationPending || !profileComposition.IsInitialized)
                return;

            FirstLaunchNarrativeRouteDecision decision =
                FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateConfirmedSkip(
                    reviewPresentation.IsEnabled,
                    skipConfirmationReviewerStateId);
            if (decision.Action == FirstLaunchNarrativeRouteAction.ContinueReviewerAfterConfirmedSkip)
            {
                skipConfirmationPending = false;
                skipConfirmationReviewerStateId = string.Empty;
                view?.SkipConfirmationView?.SetVisible(false);
                SkipConfirmationVisibilityChanged?.Invoke(false);
                sequencePresentation.StartAt(decision.NextStateId);
                reviewPresentation.Refresh(true);
                return;
            }

            if (decision.Action == FirstLaunchNarrativeRouteAction.CompleteSkippedAndRequestMatch)
                CompleteProductionSkip();
        }

        private void CompleteProductionSkip()
        {
            profileComposition.MarkSkipped(sequencePresentation.CurrentStateId);
            skipConfirmationPending = false;
            skipConfirmationReviewerStateId = string.Empty;
            view?.SkipConfirmationView?.SetVisible(false);
            SkipConfirmationVisibilityChanged?.Invoke(false);
            sequencePresentation.Cancel();
            shellComposition.RequestHandoff();
        }

        public void CancelSkip()
        {
            if (!skipConfirmationPending)
                return;
            skipConfirmationPending = false;
            skipConfirmationReviewerStateId = string.Empty;
            view?.SkipConfirmationView?.SetVisible(false);
            view?.SetSkipState(true, true, "SKIP");
            SkipConfirmationVisibilityChanged?.Invoke(false);
            sequencePresentation.Resume();
        }

        public void MarkMatchHudReady()
        {
            if (reviewPresentation.IsEnabled)
                return;
            profileComposition.MarkMatchHudReady();
        }

        public void OnMatchRouteAccepted()
        {
            if (reviewPresentation.IsEnabled)
                return;
            if (shellComposition.IsHandoffPublished)
                view?.SetVisible(false);
        }

        public void Shutdown()
        {
            if (!initialized)
                return;
            sequencePresentation.InteractiveStateRequested -= HandleInteractiveState;
            sequencePresentation.CommanderIdentityCommitted -= HandleCommanderIdentityCommitted;
            sequencePresentation.GuidanceCommitted -= HandleGuidanceCommitted;
            sequencePresentation.HandoffRequested -= HandleWatchedHandoff;
            sequencePresentation.SkipRequested -= HandleSkipRequested;
            sequencePresentation.Cancel();
            view?.SkipConfirmationView?.Unbind();
            view?.SkipConfirmationView?.SetVisible(false);
            reviewPresentation.Shutdown();
            initialized = false;
            skipConfirmationPending = false;
            skipConfirmationReviewerStateId = string.Empty;
            shellComposition.Reset();
            view = null;
            profileComposition.Reset();
        }

        private void HandleInteractiveState(NarrativeStateRecord state)
        {
            if (state.Kind == NarrativeStateKind.InteractiveIdentity)
            {
                sequencePresentation.ApplyCommanderIdentity(
                    profileComposition.CommanderIdentity,
                    profileComposition.CommanderPortraitIndex);
            }
            else if (state.Kind == NarrativeStateKind.InteractiveGuidance)
            {
                sequencePresentation.ApplyGuidance(profileComposition.Guidance);
            }
        }

        private void HandleCommanderIdentityCommitted(NarrativeCommanderIdentityData identity, int portraitIndex)
        {
            profileComposition.CommitCommanderIdentity(identity, portraitIndex, !reviewPresentation.IsEnabled);
        }

        private void HandleGuidanceCommitted(NarrativeGuidanceMode guidance)
        {
            profileComposition.CommitGuidance(guidance, !reviewPresentation.IsEnabled);
        }

        private void HandleWatchedHandoff(NarrativeHandoffResult result)
        {
            FirstLaunchNarrativeRouteDecision decision =
                FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateHandoff(
                    result,
                    reviewPresentation.IsEnabled);
            switch (decision.Action)
            {
                case FirstLaunchNarrativeRouteAction.RecordReviewerHandoff:
                    reviewPresentation.RecordHandoff(result, decision.NextStateId);
                    break;
                case FirstLaunchNarrativeRouteAction.CompleteWatchedAndRequestMatch:
                    profileComposition.MarkWatchedHandoff(result);
                    shellComposition.RequestHandoff();
                    break;
            }
        }

        private void HandleSkipRequested(NarrativeRouteRequest request)
        {
            FirstLaunchNarrativeRouteDecision decision =
                FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateSkipRequest(
                    request,
                    reviewPresentation.IsEnabled,
                    profileComposition.HasCommittedCommanderIdentity(),
                    skipConfirmationPending);
            switch (decision.Action)
            {
                case FirstLaunchNarrativeRouteAction.StartSkippedDebrief:
                    sequencePresentation.StartAt(
                        decision.NextStateId,
                        sequencePresentation.CreateCompletion(decision.NextStateId, true));
                    reviewPresentation.Refresh(true);
                    break;
                case FirstLaunchNarrativeRouteAction.CompleteSkippedAndRequestMatch:
                    CompleteProductionSkip();
                    break;
                case FirstLaunchNarrativeRouteAction.RequestSkipConfirmation:
                    ShowSkipConfirmation(decision.NextStateId);
                    break;
            }
        }

        private void ShowSkipConfirmation(string reviewerContinueStateId)
        {
            skipConfirmationPending = true;
            skipConfirmationReviewerStateId = reviewerContinueStateId ?? string.Empty;
            sequencePresentation.Pause();
            view?.SetSkipState(false, false, "SKIP");
            view?.SkipConfirmationView?.SetVisible(true);
            SkipConfirmationVisibilityChanged?.Invoke(true);
        }

        private FirstLaunchNarrativeStartupDisposition ResolveCurrentDisposition()
        {
            if (shellComposition.IsHandoffPending)
                return FirstLaunchNarrativeStartupDisposition.ResumeHandoff;
            return sequencePresentation.IsRunning ? FirstLaunchNarrativeStartupDisposition.Playing : FirstLaunchNarrativeStartupDisposition.EnterMenu;
        }

    }
}
