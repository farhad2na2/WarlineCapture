using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.Narrative.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEngine;
namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativeSequencePresentationSystemHelper
    {
        private readonly Dictionary<string, NarrativeStateRecord> states = new(StringComparer.Ordinal);
        private readonly Dictionary<NarrativeSpeakerId, NarrativeSpeakerRecord> speakers = new();
        private readonly FirstLaunchNarrativeSequenceUtilitySystemHelper sequenceRuntime = new();
        private NarrativeSequenceConfig config;
        private NarrativePunctuationConfig punctuation;
        private NarrativeSequenceView view;
        private NarrativeDialoguePresentationSystemHelper presentation;
        private NarrativePanelMotionPresentationSystemHelper panelMotion;
        private FirstLaunchNarrativeInteractivePresentationSystemHelper interactive;
        private readonly FirstLaunchNarrativePanelPresentationSystemHelper panels = new();
        private readonly FirstLaunchNarrativePortraitVoiceSelectionPresentationSystemHelper portraitVoice = new();
        private IGameTextResolver textResolver = FallbackGameTextResolver.Instance;
        private UISettingsModel settings;
        private NarrativeCompletionPayload completionOverride;
        private bool hasCompletionOverride;
        public event Action<NarrativeStateRecord> InteractiveStateRequested;
        public event Action<NarrativeCommanderIdentityData, int> CommanderIdentityCommitted;
        public event Action<NarrativeGuidanceMode> GuidanceCommitted;
        public event Action<NarrativeHandoffResult> HandoffRequested;
        public event Action<NarrativeRouteRequest> SkipRequested;
        public bool IsRunning => sequenceRuntime.IsRunning;
        public bool IsPaused => sequenceRuntime.IsPaused;
        public string CurrentStateId => sequenceRuntime.CurrentStateId;
        public int CurrentLineIndex => sequenceRuntime.CurrentLineIndex;
        public int StateCount => sequenceRuntime.StateCount;
        public int CurrentStateIndex => sequenceRuntime.CurrentStateIndex;
        public bool ReducedMotionEnabled => settings.Accessibility.ReducedMotion;
        public bool SubtitlesEnabled => settings.Narrative.SubtitlesEnabled;
        internal int ResidentPanelAssetCount => panels.ResidentAssetCount;
        internal string CurrentPanelAssetKey => panels.CurrentKey;
        internal string NextPanelAssetKey => panels.NextKey;
        public bool Initialize(
            NarrativeSequenceConfig sequenceConfig,
            NarrativeSpeakerCatalog speakerCatalog,
            NarrativePunctuationConfig punctuationProfile,
            NarrativeSequenceView sequenceView,
            IGameTextResolver resolver,
            in UISettingsModel runtimeSettings)
        {
            Cancel();
            config = sequenceConfig;
            punctuation = punctuationProfile;
            view = sequenceView;
            textResolver = resolver ?? FallbackGameTextResolver.Instance;
            settings = runtimeSettings;
            portraitVoice.Reset(view?.CommanderIdentityView);
            states.Clear();
            speakers.Clear();
            sequenceRuntime.Output -= HandleSequenceOutput;

            if (config == null || speakerCatalog == null || punctuation == null || view == null)
                return false;

            List<FirstLaunchNarrativeSequenceStateDefinition> definitions = new(config.States.Count);
            for (int i = 0; i < config.States.Count; i++)
            {
                NarrativeStateRecord state = config.States[i];
                if (state == null || string.IsNullOrWhiteSpace(state.StateId) || !states.TryAdd(state.StateId, state))
                    return false;
                float[] lineStartSeconds = new float[state.Lines.Count];
                for (int lineIndex = 0; lineIndex < state.Lines.Count; lineIndex++)
                    lineStartSeconds[lineIndex] = state.Lines[lineIndex].StartSeconds;
                definitions.Add(new FirstLaunchNarrativeSequenceStateDefinition(
                    state.StateId,
                    state.Kind,
                    state.ContinueStateId,
                    state.SkipStateId,
                    state.DurationSeconds,
                    lineStartSeconds));
            }
            for (int i = 0; i < speakerCatalog.Speakers.Count; i++)
            {
                NarrativeSpeakerRecord speaker = speakerCatalog.Speakers[i];
                if (speaker == null || !speakers.TryAdd(speaker.SpeakerId, speaker))
                    return false;
            }
            if (!sequenceRuntime.Configure(config.EntryStateId, definitions))
                return false;

            presentation = new NarrativeDialoguePresentationSystemHelper(view);
            panelMotion = new NarrativePanelMotionPresentationSystemHelper(view.PanelMotionRoot);
            interactive = new FirstLaunchNarrativeInteractivePresentationSystemHelper(
                view.CommanderIdentityView,
                view.GuidanceChoiceView);
            panels.Initialize(view, states);
            sequenceRuntime.Output += HandleSequenceOutput;
            return true;
        }

        public bool Start()
        {
            hasCompletionOverride = false;
            BindViewIntents();
            return sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                FirstLaunchNarrativeSequenceIntentKind.Start));
        }

        public bool StartAt(string stateId)
        {
            hasCompletionOverride = false;
            return StartAtInternal(stateId);
        }

        public bool StartAt(string stateId, in NarrativeCompletionPayload completionOverride)
        {
            this.completionOverride = completionOverride;
            hasCompletionOverride = true;
            return StartAtInternal(stateId);
        }

        private bool StartAtInternal(string stateId)
        {
            BindViewIntents();
            bool started = sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                FirstLaunchNarrativeSequenceIntentKind.StartAt,
                stateId));
            if (!started)
            {
                hasCompletionOverride = false;
                return false;
            }
            return true;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!sequenceRuntime.IsRunning || sequenceRuntime.IsPaused)
                return;

            FirstLaunchNarrativeAudioPresentationSystemHelper.ApplyVolumes(view, settings);
            panelMotion?.Tick(unscaledDeltaTime);
            if (sequenceRuntime.CurrentStateKind == NarrativeStateKind.PanelDialogue &&
                sequenceRuntime.CurrentLineIndex >= 0)
            {
                presentation.Tick(unscaledDeltaTime);
            }

            bool dialogueAutoAdvance = presentation.ConsumeAutoAdvanceRequest();
            FirstLaunchNarrativeSequenceIntent autoAdvanceIntent = CurrentIntent(
                FirstLaunchNarrativeSequenceIntentKind.DialogueAutoAdvance);
            sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                FirstLaunchNarrativeSequenceIntentKind.Tick,
                value: unscaledDeltaTime,
                enabled: settings.Narrative.AutoAdvance));
            if (dialogueAutoAdvance)
                sequenceRuntime.Apply(autoAdvanceIntent);
        }

        public void Pause()
        {
            if (!sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                    FirstLaunchNarrativeSequenceIntentKind.Pause)))
                return;
            presentation?.Pause();
            view.SequenceAudioView?.Pause();
        }

        public void Resume()
        {
            if (!sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                    FirstLaunchNarrativeSequenceIntentKind.Resume)))
                return;
            presentation?.Resume();
            view.SequenceAudioView?.Resume();
        }

        public bool Restart()
        {
            hasCompletionOverride = false;
            return sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                FirstLaunchNarrativeSequenceIntentKind.Restart));
        }

        public bool PreviousState()
        {
            hasCompletionOverride = false;
            return sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                FirstLaunchNarrativeSequenceIntentKind.PreviousState));
        }

        public bool NextState()
        {
            hasCompletionOverride = false;
            return sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                FirstLaunchNarrativeSequenceIntentKind.NextState));
        }

        public bool SeekNormalized(float normalizedPosition)
        {
            hasCompletionOverride = false;
            return sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                FirstLaunchNarrativeSequenceIntentKind.Seek,
                value: normalizedPosition));
        }

        public void SetReducedMotion(bool enabled)
        {
            settings.Accessibility.ReducedMotion = enabled;
            panelMotion?.SetReducedMotion(enabled);
        }

        public void SetSubtitlesEnabled(bool enabled)
        {
            settings.Narrative.SubtitlesEnabled = enabled;
            view?.DialogueView?.SetSubtitlesVisible(enabled);
        }

        public void CommitInteractiveState(string expectedStateId)
        {
            sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                FirstLaunchNarrativeSequenceIntentKind.CommitInteractive,
                expectedStateId,
                sequenceRuntime.TransitionToken));
        }

        public void Cancel()
        {
            sequenceRuntime.Apply(new FirstLaunchNarrativeSequenceIntent(
                FirstLaunchNarrativeSequenceIntentKind.Cancel));
            ClearPresentation();
            hasCompletionOverride = false;
        }

        private void ClearPresentation()
        {
            if (view != null)
            {
                view.DialogueView?.UnbindInput();
                view.PlaybackControlsView?.UnbindSkip();
                view.SetInteractiveState(NarrativeInteractiveStateKind.None);
                view.ClearPanel();
                view.SetVisible(false);
                view.SequenceAudioView?.StopAll();
            }
            presentation?.Cancel();
            panelMotion?.Cancel();
            interactive?.Unbind();
            panels.Clear();
        }

        private void PresentState(NarrativeStateRecord state, ulong transitionToken)
        {
            presentation?.Cancel();
            panelMotion?.Start(state.MotionPreset, state.DurationSeconds, settings.Accessibility.ReducedMotion);
            view.SetSkipState(!string.IsNullOrEmpty(state.SkipStateId), true, "SKIP");
            view.SetInteractiveState(NarrativeInteractiveStateKind.None);
            view.ApplyLocation(FirstLaunchNarrativeModelUtilitySystemHelper.CreateLocation(state, textResolver));
            FirstLaunchNarrativeAudioPresentationSystemHelper.EnterState(view, settings, state);
            panels.Present(state, transitionToken);

            if (state.Kind == NarrativeStateKind.PanelDialogue)
            {
                if (state.Lines.Count == 0 || state.Lines[0].StartSeconds > 0f)
                    view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
                return;
            }

            view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
            if (state.Kind == NarrativeStateKind.InteractiveIdentity || state.Kind == NarrativeStateKind.InteractiveGuidance)
            {
                view.SetInteractiveState(state.Kind == NarrativeStateKind.InteractiveIdentity
                    ? NarrativeInteractiveStateKind.CommanderIdentity
                    : NarrativeInteractiveStateKind.GuidanceChoice);
                interactive?.Enter(config.SequenceId, state.StateId, transitionToken);
                InteractiveStateRequested?.Invoke(state);
            }
        }

        private void PresentLine(NarrativeStateRecord state, int index, ulong transitionToken)
        {
            NarrativeDialogueLineRecord line = state.Lines[index];
            if (!speakers.TryGetValue(line.Speaker, out NarrativeSpeakerRecord speaker))
            {
                sequenceRuntime.Apply(CurrentIntent(
                    FirstLaunchNarrativeSequenceIntentKind.ContinueState));
                return;
            }

            string resolvedText = textResolver.Get(line.TextKey, line.EnglishFallback);
            NarrativeSpeakerPresentationModel speakerModel = new()
            {
                SpeakerId = speaker.SpeakerId,
                DisplayName = textResolver.Get(speaker.NameKey, speaker.NameFallback),
                Role = textResolver.Get(speaker.RoleKey, speaker.RoleFallback),
                AccessibleLabel = textResolver.Get(speaker.AccessibleLabelKey, speaker.AccessibleLabelFallback),
                IdentitySprite = portraitVoice.ResolvePortrait(line, speaker),
                AccentColor = speaker.AccentColor,
                Treatment = speaker.Treatment
            };
            presentation.StartDialogue(
                resolvedText,
                speakerModel,
                portraitVoice.ResolveVoiceClip(line),
                Mathf.Max(0.1f, line.DeadlineSeconds - line.StartSeconds),
                NarrativePunctuationUtilitySystemHelper.From(punctuation),
                settings);
        }

        private void HandleDialogueInput(NarrativeDialoguePhase phase)
        {
            sequenceRuntime.Apply(CurrentIntent(phase == NarrativeDialoguePhase.Revealing
                ? FirstLaunchNarrativeSequenceIntentKind.CompleteText
                : FirstLaunchNarrativeSequenceIntentKind.Continue));
        }

        private void HandleSkip()
        {
            sequenceRuntime.Apply(CurrentIntent(FirstLaunchNarrativeSequenceIntentKind.Skip));
        }

        private void HandleInteractiveUiAction(NarrativeUiAction action)
        {
            if (!sequenceRuntime.Accepts(action.StateId, action.TransitionToken))
                return;
            if (action.Kind == NarrativeUiActionKind.CommitCommanderIdentity && view.CommanderIdentityView != null)
            {
                portraitVoice.Capture(interactive.SelectedPortraitIndex);
                CommanderIdentityCommitted?.Invoke(
                    interactive.SelectedIdentity,
                    interactive.SelectedPortraitIndex);
                sequenceRuntime.Apply(CurrentIntent(
                    FirstLaunchNarrativeSequenceIntentKind.CommitInteractive));
            }
            else if (action.Kind == NarrativeUiActionKind.CommitGuidance && view.GuidanceChoiceView != null)
            {
                GuidanceCommitted?.Invoke(interactive.SelectedGuidance);
                sequenceRuntime.Apply(CurrentIntent(
                    FirstLaunchNarrativeSequenceIntentKind.CommitInteractive));
            }
        }

        public void ApplyCommanderIdentity(in NarrativeCommanderIdentityData identity, int portraitIndex) =>
            portraitVoice.Apply(interactive, identity, portraitIndex);

        public void ApplyGuidance(NarrativeGuidanceMode guidance) =>
            interactive?.ApplyGuidance(guidance);

        private void BindViewIntents()
        {
            view?.DialogueView?.BindInput(HandleDialogueInput);
            view?.PlaybackControlsView?.BindSkip(HandleSkip);
            interactive?.Bind(HandleInteractiveUiAction);
        }

        private void HandleSequenceOutput(FirstLaunchNarrativeSequenceOutput output)
        {
            if (output.Kind == FirstLaunchNarrativeSequenceOutputKind.Stopped)
            {
                ClearPresentation();
                return;
            }
            if (!states.TryGetValue(output.StateId, out NarrativeStateRecord state))
                return;
            switch (output.Kind)
            {
                case FirstLaunchNarrativeSequenceOutputKind.StateEntered:
                    if (state.Kind != NarrativeStateKind.RouteHandoff &&
                        state.Kind != NarrativeStateKind.RouteArrival)
                    {
                        hasCompletionOverride = false;
                    }
                    PresentState(state, output.TransitionToken);
                    view.SetVisible(true);
                    break;
                case FirstLaunchNarrativeSequenceOutputKind.LineStarted:
                    PresentLine(state, output.LineIndex, output.TransitionToken);
                    break;
                case FirstLaunchNarrativeSequenceOutputKind.CompleteTextRequested:
                    presentation.CompleteText();
                    break;
                case FirstLaunchNarrativeSequenceOutputKind.SkipRequested:
                    SkipRequested?.Invoke(
                        FirstLaunchNarrativeModelUtilitySystemHelper.CreateRouteRequest(
                            states,
                            output.DestinationStateId));
                    break;
                case FirstLaunchNarrativeSequenceOutputKind.RouteReached:
                    EmitHandoff(state, output.TransitionToken);
                    break;
            }
        }

        private void EmitHandoff(NarrativeStateRecord state, ulong transitionToken)
        {
            NarrativeCompletionPayload completion = hasCompletionOverride
                ? completionOverride
                : CreateRouteCompletion(state, false);
            hasCompletionOverride = false;
            HandoffRequested?.Invoke(new NarrativeHandoffResult
            {
                DestinationId = state.StateId,
                RouteRole = state.RouteRole,
                ReviewerContinueStateId = state.ContinueStateId,
                Guidance = NarrativeGuidanceMode.Full,
                Completion = completion,
                TransitionToken = transitionToken
            });
        }

        internal static NarrativeCompletionPayload CreateRouteCompletion(NarrativeStateRecord state, bool skipped)
        {
            return FirstLaunchNarrativeModelUtilitySystemHelper.CreateRouteCompletion(state, skipped);
        }

        internal NarrativeCompletionPayload CreateCompletion(string stateId, bool skipped)
        {
            return states.TryGetValue(stateId ?? string.Empty, out NarrativeStateRecord state)
                ? CreateRouteCompletion(state, skipped)
                : default;
        }

        internal string FindStateId(NarrativeRouteRole role)
        {
            return FirstLaunchNarrativeModelUtilitySystemHelper.FindStateId(config, role);
        }

        private FirstLaunchNarrativeSequenceIntent CurrentIntent(
            FirstLaunchNarrativeSequenceIntentKind kind)
        {
            return new FirstLaunchNarrativeSequenceIntent(
                kind,
                sequenceRuntime.CurrentStateId,
                sequenceRuntime.TransitionToken);
        }

    }
}
