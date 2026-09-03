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
        ResumeHandoff = 2,
        AwaitingLanguage = 3
    }

    internal sealed class FirstLaunchNarrativeCompositionSystemHelper
    {
        private readonly FirstLaunchNarrativeSequencePresentationSystemHelper sequencePresentation = new();
        private readonly FirstLaunchNarrativeProfileCompositionSystemHelper profileComposition = new();
        private readonly FirstLaunchNarrativeShellCompositionSystemHelper shellComposition = new();
        private readonly FirstLaunchNarrativeReviewPresentationSystemHelper reviewPresentation = new();
        private NarrativeSequenceView view;
        private FirstLaunchLanguageChoiceView languageChoiceView;
        private NarrativeSequenceConfig sequenceConfig;
        private NarrativeSpeakerCatalog speakerCatalog;
        private NarrativePunctuationConfig punctuationProfile;
        private NarrativeLocaleConfig persianLocale;
        private IGameTextResolver baseTextResolver;
        private bool initialized;
        private bool reviewerMode;
        private bool awaitingLanguage;
        private bool sequenceEventsBound;
        private bool skipConfirmationPending;
        private Game.Missions.Contracts.MissionLaunchPayload missionHandoff;
        private bool missionHandoffActive, missionHandoffPublished;
        private byte missionHandoffRejections;
        private string skipConfirmationReviewerStateId = string.Empty;
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
            bool startInReviewerMode = false,
            FirstLaunchLanguageChoiceView configuredLanguageChoiceView = null,
            NarrativeLocaleConfig configuredPersianLocale = null)
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
            SynchronizeUiLocale(profileComposition.Language);
            view = sequenceView;
            languageChoiceView = configuredLanguageChoiceView;
            sequenceConfig = config;
            speakerCatalog = speakers;
            punctuationProfile = punctuation;
            persianLocale = configuredPersianLocale;
            baseTextResolver = textResolver ?? FallbackGameTextResolver.Instance;
            reviewerMode = startInReviewerMode;
            languageChoiceView?.Bind(HandleLanguageSelected);
            languageChoiceView?.SetVisible(false);
            if (view?.SkipConfirmationView != null)
            {
                view.SkipConfirmationView.Bind(ConfirmSkip, CancelSkip);
                view.SkipConfirmationView.SetVisible(false);
            }

            if (profileComposition.ShouldEnterMenu(bypassForDiagnostics, startInReviewerMode))
            {
                view?.SetVisible(false);
                languageChoiceView?.SetVisible(false);
                LogStartupDisposition(FirstLaunchNarrativeStartupDisposition.EnterMenu);
                return FirstLaunchNarrativeStartupDisposition.EnterMenu;
            }

            if (profileComposition.ShouldResumeHandoff(startInReviewerMode))
            {
                // A resumed handoff has no current narrative panel to render. Keep the
                // cleared narrative layer hidden and let the shell loading curtain own
                // this transition; otherwise the panel Image falls back to solid white.
                view?.SetVisible(false);
                languageChoiceView?.SetVisible(false);
                BeginMissionHandoff(0);
                LogStartupDisposition(FirstLaunchNarrativeStartupDisposition.ResumeHandoff);
                return FirstLaunchNarrativeStartupDisposition.ResumeHandoff;
            }

            if (!startInReviewerMode && profileComposition.RequiresLanguageSelection && languageChoiceView != null)
            {
                view?.SetVisible(false);
                awaitingLanguage = true;
                languageChoiceView.SetVisible(true);
                LogStartupDisposition(FirstLaunchNarrativeStartupDisposition.AwaitingLanguage);
                return FirstLaunchNarrativeStartupDisposition.AwaitingLanguage;
            }

            FirstLaunchNarrativeLanguage profileLanguage = profileComposition.Language;
            FirstLaunchNarrativeLanguage language = startInReviewerMode
                ? FirstLaunchNarrativeLanguage.English
                : ResolveNarrativeLanguage(profileLanguage);
            if (language == FirstLaunchNarrativeLanguage.Unselected)
            {
                language = FirstLaunchNarrativeLanguage.English;
                profileComposition.CommitLanguage(language, true);
            }
            else if (!startInReviewerMode && language != profileLanguage)
            {
                // Repair profiles created while the old settings enum and the shared locale
                // preference disagreed. From this point on subtitles, voices, and persisted
                // first-launch state all describe the same selected language.
                profileComposition.CommitLanguage(language, true);
            }

            FirstLaunchNarrativeStartupDisposition disposition = StartNarrative(language)
                ? FirstLaunchNarrativeStartupDisposition.Playing
                : FirstLaunchNarrativeStartupDisposition.EnterMenu;
            LogStartupDisposition(disposition);
            return disposition;
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
                startInReviewerMode,
                menuView.FirstLaunchLanguageChoiceView,
                menuView.FirstLaunchPersianLocale);
            shellComposition.SetStartupDisposition(disposition);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!initialized)
                return;
            sequencePresentation.Tick(unscaledDeltaTime);
            reviewPresentation.Tick();
        }

        public void ApplyShellState(EntityManager entityManager, Entity boundary)
        {
            shellComposition.Apply(entityManager, boundary);
            if (!missionHandoffActive) return;
            FirstLaunchMissionHandoffState state = FirstLaunchMissionHandoffOperation.Advance(
                entityManager, missionHandoff, ref missionHandoffPublished, ref missionHandoffRejections);
            if (state == FirstLaunchMissionHandoffState.Accepted && profileComposition.MarkMissionAccepted(missionHandoff))
            { missionHandoffActive = false; view?.SetVisible(false); }
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

            if (decision.Action == FirstLaunchNarrativeRouteAction.CompleteSkippedAndRequestMenu)
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
            BeginMissionHandoff(0);
        }

        public void CancelSkip()
        {
            if (!skipConfirmationPending)
                return;
            skipConfirmationPending = false;
            skipConfirmationReviewerStateId = string.Empty;
            view?.SkipConfirmationView?.SetVisible(false);
            view?.SetSkipState(true, true, sequencePresentation.SkipLabel);
            SkipConfirmationVisibilityChanged?.Invoke(false);
            sequencePresentation.Resume();
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
            languageChoiceView?.Unbind();
            languageChoiceView?.SetVisible(false);
            view?.SkipConfirmationView?.Unbind();
            view?.SkipConfirmationView?.SetVisible(false);
            reviewPresentation.Shutdown();
            initialized = false;
            skipConfirmationPending = false;
            skipConfirmationReviewerStateId = string.Empty;
            shellComposition.Reset();
            view = null;
            languageChoiceView = null;
            sequenceConfig = null;
            speakerCatalog = null;
            punctuationProfile = null;
            persianLocale = null;
            baseTextResolver = null;
            reviewerMode = false;
            awaitingLanguage = false;
            sequenceEventsBound = false;
            missionHandoff = default; missionHandoffActive = false; missionHandoffPublished = false; missionHandoffRejections = 0;
            profileComposition.Reset();
        }

        private void HandleLanguageSelected(FirstLaunchNarrativeLanguage language)
        {
            if (!awaitingLanguage ||
                language != FirstLaunchNarrativeLanguage.English &&
                language != FirstLaunchNarrativeLanguage.Persian)
            {
                return;
            }

            if (language == FirstLaunchNarrativeLanguage.Persian && persianLocale == null)
            {
                UnityEngine.Debug.LogError(
                    "[FirstLaunchNarrative] Persian was selected, but no Persian locale is configured.");
                languageChoiceView?.SetVisible(true);
                return;
            }

            profileComposition.CommitLanguage(language, true);
            UISettingsModel settings = SettingsService.Load();
            settings.Localization.Language = language == FirstLaunchNarrativeLanguage.Persian
                ? UILanguage.Persian
                : UILanguage.English;
            settings.Localization.LocaleCode = language == FirstLaunchNarrativeLanguage.Persian
                ? GameLocalization.PersianLocaleCode
                : GameLocalization.EnglishLocaleCode;
            SettingsService.Save(settings);
            SettingsService.ApplyRuntime(settings);
            awaitingLanguage = false;
            languageChoiceView?.SetVisible(false);
            if (StartNarrative(language))
                return;

            shellComposition.SetStartupDisposition(FirstLaunchNarrativeStartupDisposition.EnterMenu);
        }

        private static void SynchronizeUiLocale(FirstLaunchNarrativeLanguage language)
        {
            if (language == FirstLaunchNarrativeLanguage.Unselected ||
                UnityEngine.PlayerPrefs.HasKey(GameLocalization.LocalePreferenceKey))
                return;

            UISettingsModel settings = SettingsService.Load();
            UILanguage uiLanguage = language == FirstLaunchNarrativeLanguage.Persian
                ? UILanguage.Persian
                : UILanguage.English;
            if (settings.Localization.Language != uiLanguage)
            {
                settings.Localization.Language = uiLanguage;
            }
            settings.Localization.LocaleCode = uiLanguage == UILanguage.Persian
                ? GameLocalization.PersianLocaleCode
                : GameLocalization.EnglishLocaleCode;
            SettingsService.Save(settings);
            GameLocalization.SetLocale(
                uiLanguage == UILanguage.Persian
                    ? GameLocalization.PersianLocaleCode
                    : GameLocalization.EnglishLocaleCode,
                persist: true);
        }

        private bool StartNarrative(FirstLaunchNarrativeLanguage language)
        {
            if (!reviewerMode)
                language = ResolveNarrativeLanguage(language);

            if (language == FirstLaunchNarrativeLanguage.Persian && persianLocale == null)
            {
                UnityEngine.Debug.LogError(
                    "[FirstLaunchNarrative] Cannot start Persian narrative without a Persian locale.");
                return false;
            }

            NarrativeLocaleConfig locale = language == FirstLaunchNarrativeLanguage.Persian
                ? persianLocale
                : null;
            IGameTextResolver legacyResolver = locale != null
                ? new FirstLaunchNarrativeLocaleTextCompositionSystemHelper(baseTextResolver, locale)
                : baseTextResolver;
            IGameTextResolver resolver =
                new SharedLocalizationTextCompositionSystemHelper(legacyResolver);
            if (!sequencePresentation.Initialize(
                    sequenceConfig,
                    speakerCatalog,
                    punctuationProfile,
                    view,
                    resolver,
                    SettingsService.Load(),
                    locale))
            {
                view?.SetVisible(false);
                return false;
            }

            BindSequenceEvents();
            reviewPresentation.Initialize(reviewerMode, view, sequencePresentation);
            profileComposition.MarkInProgress(reviewerMode);
            sequencePresentation.Start();
            reviewPresentation.Refresh(true);
            return true;
        }

        internal static FirstLaunchNarrativeLanguage ResolveNarrativeLanguage(
            FirstLaunchNarrativeLanguage profileLanguage)
        {
            // The shared localization locale is the live player choice used by subtitles and
            // every V3 screen. Voice selection must use that same source of truth, even when an
            // older first-launch profile still contains a different language value.
            string localeCode = GameLocalization.CurrentLocaleCode;
            if (string.Equals(
                    localeCode,
                    GameLocalization.PersianLocaleCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                return FirstLaunchNarrativeLanguage.Persian;
            }

            if (string.Equals(
                    localeCode,
                    GameLocalization.EnglishLocaleCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                return FirstLaunchNarrativeLanguage.English;
            }

            return profileLanguage;
        }

        private void LogStartupDisposition(FirstLaunchNarrativeStartupDisposition disposition)
        {
            UnityEngine.Debug.Log(
                $"[FirstLaunchStartup] fix=LanguageChoiceAwakeOrder_2026-07-17 disposition={disposition} " +
                $"languageView={(languageChoiceView != null ? 1 : 0)} " +
                $"languageVisible={(languageChoiceView != null && languageChoiceView.IsVisible ? 1 : 0)} " +
                $"narrativeView={(view != null ? 1 : 0)}");
        }

        private void BindSequenceEvents()
        {
            if (sequenceEventsBound)
                return;

            sequencePresentation.InteractiveStateRequested += HandleInteractiveState;
            sequencePresentation.CommanderIdentityCommitted += HandleCommanderIdentityCommitted;
            sequencePresentation.GuidanceCommitted += HandleGuidanceCommitted;
            sequencePresentation.HandoffRequested += HandleWatchedHandoff;
            sequencePresentation.SkipRequested += HandleSkipRequested;
            sequenceEventsBound = true;
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
                case FirstLaunchNarrativeRouteAction.CompleteWatchedAndRequestMenu:
                    profileComposition.MarkWatchedHandoff(result);
                    BeginMissionHandoff(result.TransitionToken);
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
                case FirstLaunchNarrativeRouteAction.CompleteSkippedAndRequestMenu:
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
            view?.SetSkipState(false, false, sequencePresentation.SkipLabel);
            view?.SkipConfirmationView?.SetVisible(true);
            SkipConfirmationVisibilityChanged?.Invoke(true);
        }

        private void BeginMissionHandoff(ulong transitionToken)
        {
            missionHandoff = profileComposition.PrepareMissionHandoff(transitionToken);
            missionHandoffActive = true; missionHandoffPublished = false; missionHandoffRejections = 0;
            shellComposition.RequestHandoff();
            // Do not re-show NarrativeSequenceView here. A production skip clears its
            // panel before requesting this handoff, so making it visible produces a
            // full-screen white Image above the real loading presentation.
        }

        private FirstLaunchNarrativeStartupDisposition ResolveCurrentDisposition()
        {
            if (awaitingLanguage)
                return FirstLaunchNarrativeStartupDisposition.AwaitingLanguage;
            if (shellComposition.IsHandoffPending)
                return FirstLaunchNarrativeStartupDisposition.ResumeHandoff;
            return sequencePresentation.IsRunning ? FirstLaunchNarrativeStartupDisposition.Playing : FirstLaunchNarrativeStartupDisposition.EnterMenu;
        }

    }
}
