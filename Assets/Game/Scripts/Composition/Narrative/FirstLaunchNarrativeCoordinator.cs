using System;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEngine;

namespace Game.Composition
{
    internal enum FirstLaunchNarrativeStartupDisposition
    {
        EnterMenu = 0,
        Playing = 1,
        ResumeHandoff = 2
    }

    internal sealed class FirstLaunchNarrativeCoordinator
    {
        private readonly FirstLaunchNarrativePlayer player = new();
        private SaveService saveService;
        private PlayerProfileSaveData profile;
        private NarrativeSequenceView view;
        private bool initialized;
        private bool handoffRequestPending;
        private bool handoffRequestPublished;
        private bool skipConfirmationPending;
        private bool reviewerMode;
        private int lastReviewerStateIndex = -2;
        private bool lastReviewerPaused;
        private bool lastReviewerReducedMotion;
        private bool lastReviewerSubtitles;
        private bool reviewerSafeArea;
        private NarrativeCompletionPayload lastReviewerCompletion;
        private UiShellStartupDisposition startupDisposition = UiShellStartupDisposition.Pending;
        private bool shellRoutePending;

        public event Action MatchHandoffRequested;
        public event Action<bool> SkipConfirmationVisibilityChanged;

        public bool IsPlaying => initialized && player.IsRunning;
        public bool IsSkipConfirmationPending => skipConfirmationPending;
        public string CurrentStateId => player.CurrentStateId;
        public NarrativeCompletionPayload LastReviewerCompletion => lastReviewerCompletion;

        public FirstLaunchNarrativeStartupDisposition Initialize(
            NarrativeSequenceConfig config,
            NarrativeSpeakerCatalog speakers,
            NarrativePunctuationProfile punctuation,
            NarrativeSequenceView sequenceView,
            IGameTextResolver textResolver,
            SaveService persistence,
            bool bypassForDiagnostics,
            bool startInReviewerMode = false)
        {
            if (initialized)
                return ResolveCurrentDisposition();

            initialized = true;
            saveService = persistence ?? SaveService.CreateDefault();
            profile = saveService.LoadProfile();
            view = sequenceView;
            reviewerMode = startInReviewerMode;
            if (view?.SkipConfirmationView != null)
            {
                view.SkipConfirmationView.Bind(ConfirmSkip, CancelSkip);
                view.SkipConfirmationView.SetVisible(false);
            }

            if (!reviewerMode && (bypassForDiagnostics || profile.firstLaunchStatus == FirstLaunchProfileState.Completed))
            {
                view?.SetVisible(false);
                return FirstLaunchNarrativeStartupDisposition.EnterMenu;
            }

            if (!reviewerMode && profile.firstLaunchStatus == FirstLaunchProfileState.HandoffPending)
            {
                view?.SetVisible(false);
                handoffRequestPending = true;
                return FirstLaunchNarrativeStartupDisposition.ResumeHandoff;
            }

            if (!player.Initialize(config, speakers, punctuation, view, textResolver, SettingsService.Load()))
            {
                view?.SetVisible(false);
                return FirstLaunchNarrativeStartupDisposition.EnterMenu;
            }

            player.InteractiveStateRequested += HandleInteractiveState;
            player.CommanderIdentityCommitted += HandleCommanderIdentityCommitted;
            player.GuidanceCommitted += HandleGuidanceCommitted;
            player.HandoffRequested += HandleWatchedHandoff;
            player.SkipRequested += HandleSkipRequested;
            if (view?.ReviewerControlsView != null)
            {
                view.ReviewerControlsView.Bind(HandleReviewerAction);
                view.ReviewerControlsView.SetDevelopmentVisibility(reviewerMode);
            }
            if (!reviewerMode)
            {
                profile.firstLaunchStatus = FirstLaunchProfileState.InProgress;
                saveService.SaveProfile(profile);
            }
            player.Start();
            RefreshReviewerSurface(true);
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
            startupDisposition = disposition == FirstLaunchNarrativeStartupDisposition.EnterMenu
                ? UiShellStartupDisposition.EnterMenu
                : UiShellStartupDisposition.FirstLaunch;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!initialized)
                return;
            player.Tick(unscaledDeltaTime);
            RefreshReviewerSurface();
            if (handoffRequestPending && !handoffRequestPublished)
            {
                handoffRequestPublished = true;
                shellRoutePending = true;
                MatchHandoffRequested?.Invoke();
            }
        }

        public void ApplyShellState(EntityManager entityManager, Entity boundary)
        {
            if (entityManager.HasComponent<UiShellStartupDispositionComponent>(boundary))
            {
                UiShellStartupDispositionComponent current =
                    entityManager.GetComponentData<UiShellStartupDispositionComponent>(boundary);
                if (current.Value != startupDisposition)
                {
                    current.Value = startupDisposition;
                    entityManager.SetComponentData(boundary, current);
                }
            }
            if (!shellRoutePending || !entityManager.HasBuffer<UiShellRouteRequestComponent>(boundary))
                return;
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary).Add(new UiShellRouteRequestComponent
            {
                Intent = UiShellRouteIntent.EnterMatch,
                Route = UIRoute.Match,
                PushHistory = 0
            });
            shellRoutePending = false;
        }

        public static void ResetShellState(EntityManager entityManager, Entity boundary)
        {
            UiShellStartupDispositionComponent state = new() { Value = UiShellStartupDisposition.Pending };
            if (entityManager.HasComponent<UiShellStartupDispositionComponent>(boundary))
                entityManager.SetComponentData(boundary, state);
            else
                entityManager.AddComponentData(boundary, state);
        }

        public void ConfirmSkip()
        {
            if (!skipConfirmationPending || profile == null)
                return;
            if (reviewerMode)
            {
                skipConfirmationPending = false;
                view?.SkipConfirmationView?.SetVisible(false);
                SkipConfirmationVisibilityChanged?.Invoke(false);
                player.StartAt("first_launch.gameplay_placeholder");
                RefreshReviewerSurface(true);
                return;
            }
            CompleteProductionSkip();
        }

        private void CompleteProductionSkip()
        {
            EnsureValidDefaults();
            profile.firstLaunchStatus = FirstLaunchProfileState.HandoffPending;
            profile.firstLaunchSkipped = true;
            profile.firstLaunchWatched = false;
            profile.firstLaunchLastCompletedStateId = player.CurrentStateId;
            saveService.SaveProfile(profile);
            skipConfirmationPending = false;
            view?.SkipConfirmationView?.SetVisible(false);
            SkipConfirmationVisibilityChanged?.Invoke(false);
            player.Cancel();
            handoffRequestPending = true;
            handoffRequestPublished = false;
        }

        public void CancelSkip()
        {
            if (!skipConfirmationPending)
                return;
            skipConfirmationPending = false;
            view?.SkipConfirmationView?.SetVisible(false);
            view?.SetSkipState(true, true, "SKIP");
            SkipConfirmationVisibilityChanged?.Invoke(false);
            player.Resume();
        }

        public void MarkMatchHudReady()
        {
            if (reviewerMode)
                return;
            if (profile == null || profile.firstLaunchStatus != FirstLaunchProfileState.HandoffPending)
                return;
            profile.firstLaunchStatus = FirstLaunchProfileState.Completed;
            saveService.SaveProfile(profile);
        }

        public void OnMatchRouteAccepted()
        {
            if (reviewerMode)
                return;
            if (handoffRequestPublished)
                view?.SetVisible(false);
        }

        public void Shutdown()
        {
            if (!initialized)
                return;
            player.InteractiveStateRequested -= HandleInteractiveState;
            player.CommanderIdentityCommitted -= HandleCommanderIdentityCommitted;
            player.GuidanceCommitted -= HandleGuidanceCommitted;
            player.HandoffRequested -= HandleWatchedHandoff;
            player.SkipRequested -= HandleSkipRequested;
            player.Cancel();
            view?.SkipConfirmationView?.Unbind();
            view?.SkipConfirmationView?.SetVisible(false);
            view?.ReviewerControlsView?.Unbind();
            view?.ReviewerControlsView?.SetDevelopmentVisibility(false);
            view?.SetSafeAreaPreview(false);
            initialized = false;
            handoffRequestPending = false;
            handoffRequestPublished = false;
            skipConfirmationPending = false;
            reviewerMode = false;
            lastReviewerStateIndex = -2;
            lastReviewerCompletion = default;
            reviewerSafeArea = false;
            startupDisposition = UiShellStartupDisposition.Pending;
            shellRoutePending = false;
            view = null;
            profile = null;
            saveService = null;
        }

        private void HandleInteractiveState(NarrativeStateRecord state)
        {
            if (state.Kind == NarrativeStateKind.InteractiveIdentity && view?.CommanderIdentityView != null)
            {
                view.CommanderIdentityView.ApplyIdentity(new NarrativeCommanderIdentityData
                {
                    Callsign = profile.firstLaunchCommanderCallsign,
                    DisplayName = profile.firstLaunchCommanderDisplayName
                }, profile.firstLaunchCommanderPortraitIndex);
            }
            else if (state.Kind == NarrativeStateKind.InteractiveGuidance && view?.GuidanceChoiceView != null)
            {
                view.GuidanceChoiceView.SetSelectedGuidance(ParseGuidance(profile.firstLaunchGuidance));
            }
        }

        private void HandleCommanderIdentityCommitted(NarrativeCommanderIdentityData identity, int portraitIndex)
        {
            profile.firstLaunchCommanderCallsign = identity.Callsign;
            profile.firstLaunchCommanderDisplayName = identity.DisplayName;
            profile.firstLaunchCommanderPortraitIndex = Math.Max(0, portraitIndex);
            profile.firstLaunchLastCompletedStateId = "first_launch.commander_identity";
            if (!reviewerMode)
                saveService.SaveProfile(profile);
        }

        private void HandleGuidanceCommitted(NarrativeGuidanceMode guidance)
        {
            profile.firstLaunchGuidance = guidance.ToString();
            profile.firstLaunchLastCompletedStateId = "first_launch.guidance_choice";
            if (!reviewerMode)
                saveService.SaveProfile(profile);
        }

        private void HandleWatchedHandoff(NarrativeHandoffResult result)
        {
            if (reviewerMode)
            {
                lastReviewerCompletion = result.Completion;
                if (result.DestinationId == "first_launch.m01_handoff")
                    player.StartAt("first_launch.gameplay_placeholder");
                RefreshReviewerSurface(true);
                return;
            }
            if (result.DestinationId == "first_launch.command_base_reveal")
                return;
            EnsureValidDefaults();
            profile.firstLaunchStatus = FirstLaunchProfileState.HandoffPending;
            profile.firstLaunchWatched = true;
            profile.firstLaunchSkipped = false;
            profile.firstLaunchLastCompletedStateId = result.Completion.LastCompletedStateId;
            saveService.SaveProfile(profile);
            handoffRequestPending = true;
            handoffRequestPublished = false;
        }

        private void HandleSkipRequested(string destination)
        {
            if (destination == "first_launch.command_base_reveal")
            {
                player.StartAt(destination, FirstLaunchNarrativePlayer.CreateDebriefCompletion(true));
                RefreshReviewerSurface(true);
                return;
            }
            if (skipConfirmationPending || destination != "first_launch.m01_handoff")
                return;
            if (!reviewerMode && HasCommittedCommanderIdentity())
            {
                CompleteProductionSkip();
                return;
            }
            skipConfirmationPending = true;
            player.Pause();
            view?.SetSkipState(false, false, "SKIP");
            view?.SkipConfirmationView?.SetVisible(true);
            SkipConfirmationVisibilityChanged?.Invoke(true);
        }

        private bool HasCommittedCommanderIdentity()
        {
            return profile != null &&
                   (profile.firstLaunchLastCompletedStateId == "first_launch.commander_identity" ||
                    profile.firstLaunchLastCompletedStateId == "first_launch.guidance_choice");
        }

        private void EnsureValidDefaults()
        {
            if (string.IsNullOrWhiteSpace(profile.firstLaunchCommanderCallsign))
                profile.firstLaunchCommanderCallsign = "COMMANDER";
            if (string.IsNullOrWhiteSpace(profile.firstLaunchCommanderDisplayName))
                profile.firstLaunchCommanderDisplayName = "Commander";
            if (string.IsNullOrWhiteSpace(profile.firstLaunchGuidance))
                profile.firstLaunchGuidance = NarrativeGuidanceMode.Full.ToString();
        }

        private void HandleReviewerAction(NarrativeReviewerAction action)
        {
            if (!reviewerMode)
                return;

            switch (action.Kind)
            {
                case NarrativeReviewerActionKind.TogglePlayPause:
                    if (!player.IsRunning)
                        player.Restart();
                    else if (player.IsPaused)
                        player.Resume();
                    else
                        player.Pause();
                    break;
                case NarrativeReviewerActionKind.Restart:
                    player.Restart();
                    break;
                case NarrativeReviewerActionKind.Previous:
                    player.PreviousState();
                    break;
                case NarrativeReviewerActionKind.Next:
                    player.NextState();
                    break;
                case NarrativeReviewerActionKind.Seek:
                    player.SeekNormalized(action.Position);
                    break;
                case NarrativeReviewerActionKind.SkipToGame:
                    player.StartAt("first_launch.gameplay_placeholder");
                    break;
                case NarrativeReviewerActionKind.JumpToDebrief:
                    player.StartAt("FL-P19");
                    break;
                case NarrativeReviewerActionKind.SetReducedMotion:
                    player.SetReducedMotion(action.ReducedMotion);
                    break;
                case NarrativeReviewerActionKind.SetSubtitles:
                    player.SetSubtitlesEnabled(action.Enabled);
                    break;
                case NarrativeReviewerActionKind.SetSafeArea:
                    reviewerSafeArea = action.Enabled;
                    view?.SetSafeAreaPreview(reviewerSafeArea);
                    break;
                case NarrativeReviewerActionKind.Capture:
                    Debug.Log($"[FirstLaunchNarrativeReviewer] Capture requested at {player.CurrentStateId}.");
                    break;
            }
            RefreshReviewerSurface(true);
        }

        private void RefreshReviewerSurface(bool force = false)
        {
            if (!reviewerMode || view?.ReviewerControlsView == null)
                return;

            int index = player.CurrentStateIndex;
            bool paused = player.IsPaused;
            bool reducedMotion = player.ReducedMotionEnabled;
            bool subtitles = player.SubtitlesEnabled;
            if (!force && index == lastReviewerStateIndex && paused == lastReviewerPaused && reducedMotion == lastReviewerReducedMotion && subtitles == lastReviewerSubtitles)
                return;

            int total = player.StateCount;
            view.ReviewerControlsView.SetPlayingState(player.IsRunning && !paused);
            view.ReviewerControlsView.SetState(player.CurrentStateId, index >= 0 ? index + 1 : 0, total);
            view.ReviewerControlsView.SetProgress(index >= 0 && total > 1 ? (float)index / (total - 1) : 0f);
            view.ReviewerControlsView.SetReducedMotion(reducedMotion);
            view.ReviewerControlsView.SetSubtitles(subtitles);
            view.ReviewerControlsView.SetSafeArea(reviewerSafeArea);
            view.ReviewerControlsView.SetNavigationState(index > 0, index >= 0 && index + 1 < total);
            lastReviewerStateIndex = index;
            lastReviewerPaused = paused;
            lastReviewerReducedMotion = reducedMotion;
            lastReviewerSubtitles = subtitles;
        }

        private FirstLaunchNarrativeStartupDisposition ResolveCurrentDisposition()
        {
            if (handoffRequestPending)
                return FirstLaunchNarrativeStartupDisposition.ResumeHandoff;
            return player.IsRunning ? FirstLaunchNarrativeStartupDisposition.Playing : FirstLaunchNarrativeStartupDisposition.EnterMenu;
        }

        private static NarrativeGuidanceMode ParseGuidance(string value)
        {
            return Enum.TryParse(value, true, out NarrativeGuidanceMode guidance)
                ? guidance
                : NarrativeGuidanceMode.Full;
        }
    }
}
