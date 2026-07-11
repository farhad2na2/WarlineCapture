using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativePlayer
    {
        private const float TimingEpsilonSeconds = 0.001f;
        private readonly Dictionary<string, NarrativeStateRecord> states = new(StringComparer.Ordinal);
        private readonly Dictionary<NarrativeSpeakerId, NarrativeSpeakerRecord> speakers = new();
        private NarrativeSequenceConfig config;
        private NarrativePunctuationProfile punctuation;
        private NarrativeSequenceView view;
        private NarrativeSequencePresenter presentation;
        private NarrativePanelMotion panelMotion;
        private readonly NarrativePanelAssetResidency panelResidency = new();
        private IGameTextResolver textResolver = FallbackGameTextResolver.Instance;
        private UISettingsModel settings;
        private NarrativeStateRecord currentState;
        private int currentLineIndex;
        private float stateElapsed;
        private bool autoAdvancePending;
        private bool running;
        private bool paused;
        private ulong transitionToken;
        private NarrativeCompletionPayload routeCompletionOverride;
        private bool hasRouteCompletionOverride;

        public event Action<NarrativeStateRecord> InteractiveStateRequested;
        public event Action<NarrativeCommanderIdentityData, int> CommanderIdentityCommitted;
        public event Action<NarrativeGuidanceMode> GuidanceCommitted;
        public event Action<NarrativeHandoffResult> HandoffRequested;
        public event Action<string> SkipRequested;

        public bool IsRunning => running;
        public bool IsPaused => paused;
        public string CurrentStateId => currentState?.StateId ?? string.Empty;
        public int CurrentLineIndex => currentLineIndex;
        public int StateCount => config != null ? config.States.Count : 0;
        public int CurrentStateIndex => config != null && currentState != null ? IndexOfState(currentState.StateId) : -1;
        public bool ReducedMotionEnabled => settings.Accessibility.ReducedMotion;
        public bool SubtitlesEnabled => settings.Narrative.SubtitlesEnabled;
        internal int ResidentPanelAssetCount => panelResidency.ResidentAssetCount;
        internal string CurrentPanelAssetKey => panelResidency.CurrentKey;
        internal string NextPanelAssetKey => panelResidency.NextKey;

        public bool Initialize(
            NarrativeSequenceConfig sequenceConfig,
            NarrativeSpeakerCatalog speakerCatalog,
            NarrativePunctuationProfile punctuationProfile,
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
            states.Clear();
            speakers.Clear();

            if (config == null || speakerCatalog == null || punctuation == null || view == null)
                return false;

            for (int i = 0; i < config.States.Count; i++)
            {
                NarrativeStateRecord state = config.States[i];
                if (state == null || string.IsNullOrWhiteSpace(state.StateId) || !states.TryAdd(state.StateId, state))
                    return false;
            }
            for (int i = 0; i < speakerCatalog.Speakers.Count; i++)
            {
                NarrativeSpeakerRecord speaker = speakerCatalog.Speakers[i];
                if (speaker == null || !speakers.TryAdd(speaker.SpeakerId, speaker))
                    return false;
            }

            presentation = new NarrativeSequencePresenter(view);
            panelMotion = new NarrativePanelMotion(view.PanelMotionRoot);
            view.BindActions(HandleUiAction);
            return true;
        }

        public bool Start()
        {
            return StartAt(config != null ? config.EntryStateId : string.Empty);
        }

        public bool StartAt(string stateId)
        {
            hasRouteCompletionOverride = false;
            return StartAtInternal(stateId);
        }

        public bool StartAt(string stateId, in NarrativeCompletionPayload completionOverride)
        {
            routeCompletionOverride = completionOverride;
            hasRouteCompletionOverride = true;
            return StartAtInternal(stateId);
        }

        private bool StartAtInternal(string stateId)
        {
            if (!states.TryGetValue(stateId ?? string.Empty, out NarrativeStateRecord state))
            {
                hasRouteCompletionOverride = false;
                return false;
            }
            running = true;
            paused = false;
            transitionToken++;
            EnterState(state);
            view.SetVisible(true);
            return true;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!running || paused || currentState == null)
                return;

            stateElapsed += Mathf.Max(0f, unscaledDeltaTime);
            panelMotion?.Tick(unscaledDeltaTime);
            if (currentState.Kind != NarrativeStateKind.PanelDialogue)
                return;

            if (currentState.Lines.Count == 0)
            {
                if (settings.Narrative.AutoAdvance && Reached(stateElapsed, Mathf.Max(0.1f, currentState.DurationSeconds)))
                    ContinueTo(currentState.ContinueStateId);
                return;
            }

            if (currentLineIndex < 0)
            {
                if (Reached(stateElapsed, currentState.Lines[0].StartSeconds))
                    StartLine(0);
                return;
            }

            presentation.Tick(unscaledDeltaTime);
            if (presentation.ConsumeAutoAdvanceRequest())
                autoAdvancePending = true;
            if (autoAdvancePending)
                AdvanceLineOrState(true);
        }

        public void Pause()
        {
            if (!running || paused)
                return;
            paused = true;
            presentation?.Pause();
        }

        public void Resume()
        {
            if (!running || !paused)
                return;
            paused = false;
            presentation?.Resume();
        }

        public bool Restart()
        {
            return Start();
        }

        public bool PreviousState()
        {
            if (config == null || currentState == null)
                return false;
            int index = IndexOfState(currentState.StateId);
            return index > 0 && StartAt(config.States[index - 1].StateId);
        }

        public bool NextState()
        {
            if (config == null || currentState == null)
                return false;
            int index = IndexOfState(currentState.StateId);
            return index >= 0 && index + 1 < config.States.Count && StartAt(config.States[index + 1].StateId);
        }

        public bool SeekNormalized(float normalizedPosition)
        {
            if (config == null || config.States.Count == 0)
                return false;
            int index = Mathf.RoundToInt(Mathf.Clamp01(normalizedPosition) * (config.States.Count - 1));
            return StartAt(config.States[index].StateId);
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
            if (!running || currentState == null || !string.Equals(currentState.StateId, expectedStateId, StringComparison.Ordinal))
                return;
            ContinueTo(currentState.ContinueStateId);
        }

        public void Cancel()
        {
            if (view != null)
            {
                view.UnbindActions();
                view.CommanderIdentityView?.UnbindActions();
                view.GuidanceChoiceView?.UnbindActions();
                view.SetInteractiveState(NarrativeInteractiveStateKind.None);
                view.ClearPanel();
                view.SetVisible(false);
            }
            presentation?.Cancel();
            panelMotion?.Cancel();
            panelResidency.ReleaseAll();
            running = false;
            paused = false;
            currentState = null;
            currentLineIndex = 0;
            stateElapsed = 0f;
            autoAdvancePending = false;
            hasRouteCompletionOverride = false;
        }

        private void EnterState(NarrativeStateRecord state)
        {
            presentation?.Cancel();
            panelMotion?.Start(state.MotionPreset, state.DurationSeconds, settings.Accessibility.ReducedMotion);
            currentState = state;
            currentLineIndex = -1;
            stateElapsed = 0f;
            autoAdvancePending = false;
            view.SetActionContext(config.SequenceId, state.StateId, string.Empty, transitionToken);
            view.SetSkipState(!string.IsNullOrEmpty(state.SkipStateId), true, "SKIP");
            view.SetInteractiveState(NarrativeInteractiveStateKind.None);
            view.CommanderIdentityView?.UnbindActions();
            view.GuidanceChoiceView?.UnbindActions();

            AssetReferenceSprite panelReference = ResolvePanelReference(state);
            NarrativeStateRecord nextPanelState = FindNextPanelState(state);
            AssetReferenceSprite nextPanelReference = ResolvePanelReference(nextPanelState);
            Sprite panel = !IsReferenceValid(panelReference)
                ? panelResidency.KeepCurrentAndPrepareNext(nextPanelReference)
                : panelResidency.LoadCurrentAndPrepareNext(panelReference, nextPanelReference, ResolveDirectPanel(state));
            if (panel == null)
                panel = ResolveDirectPanel(state);
            if (panel != null)
            {
                view.ApplyPanel(new NarrativePanelPresentationModel
                {
                    StateId = state.StateId,
                    PanelSprite = panel,
                    Tint = Color.white
                });
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else if (state.Kind == NarrativeStateKind.PanelDialogue || state.Kind == NarrativeStateKind.InteractiveIdentity)
            {
                Debug.LogError($"[FirstLaunchNarrativePlayer] Missing panel for state '{state.StateId}'. Playback will continue with the empty static fallback.");
            }
#endif

            if (state.Kind == NarrativeStateKind.PanelDialogue)
            {
                if (state.Lines.Count > 0 && state.Lines[0].StartSeconds <= 0f)
                    StartLine(0);
                else
                    view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
                return;
            }

            view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
            if (state.Kind == NarrativeStateKind.InteractiveIdentity || state.Kind == NarrativeStateKind.InteractiveGuidance)
            {
                view.SetInteractiveState(state.Kind == NarrativeStateKind.InteractiveIdentity
                    ? NarrativeInteractiveStateKind.CommanderIdentity
                    : NarrativeInteractiveStateKind.GuidanceChoice);
                if (state.Kind == NarrativeStateKind.InteractiveIdentity && view.CommanderIdentityView != null)
                {
                    view.CommanderIdentityView.SetActionContext(config.SequenceId, state.StateId, string.Empty, transitionToken);
                    view.CommanderIdentityView.BindActions(HandleInteractiveUiAction);
                }
                else if (state.Kind == NarrativeStateKind.InteractiveGuidance && view.GuidanceChoiceView != null)
                {
                    view.GuidanceChoiceView.SetActionContext(config.SequenceId, state.StateId, string.Empty, transitionToken);
                    view.GuidanceChoiceView.BindActions(HandleInteractiveUiAction);
                }
                InteractiveStateRequested?.Invoke(state);
                return;
            }

            if (state.Kind == NarrativeStateKind.RouteHandoff || state.Kind == NarrativeStateKind.RouteArrival)
            {
                running = false;
                NarrativeCompletionPayload completion = hasRouteCompletionOverride
                    ? routeCompletionOverride
                    : CreateRouteCompletion(state, false);
                hasRouteCompletionOverride = false;
                HandoffRequested?.Invoke(new NarrativeHandoffResult
                {
                    DestinationId = state.StateId,
                    Guidance = NarrativeGuidanceMode.Full,
                    Completion = completion,
                    TransitionToken = transitionToken
                });
            }
        }

        private void StartLine(int index)
        {
            currentLineIndex = index;
            autoAdvancePending = false;
            NarrativeDialogueLineRecord line = currentState.Lines[index];
            if (!speakers.TryGetValue(line.Speaker, out NarrativeSpeakerRecord speaker))
            {
                ContinueTo(currentState.ContinueStateId);
                return;
            }

            string resolvedText = textResolver.Get(line.TextKey, line.EnglishFallback);
            NarrativeSpeakerPresentationModel speakerModel = new()
            {
                SpeakerId = speaker.SpeakerId,
                DisplayName = textResolver.Get(speaker.NameKey, speaker.NameFallback),
                Role = textResolver.Get(speaker.RoleKey, speaker.RoleFallback),
                AccessibleLabel = textResolver.Get(speaker.AccessibleLabelKey, speaker.AccessibleLabelFallback),
                IdentitySprite = speaker.IdentitySprite,
                AccentColor = speaker.AccentColor,
                Treatment = speaker.Treatment
            };
            view.SetActionContext(config.SequenceId, currentState.StateId, line.LineId, transitionToken);
            presentation.StartDialogue(
                resolvedText,
                speakerModel,
                line.VoiceClip,
                Mathf.Max(0.1f, line.DeadlineSeconds - line.StartSeconds),
                NarrativePunctuationAdapter.From(punctuation),
                settings);
        }

        private void HandleUiAction(NarrativeUiAction action)
        {
            if (!running || action.TransitionToken != transitionToken || currentState == null || action.StateId != currentState.StateId)
                return;
            switch (action.Kind)
            {
                case NarrativeUiActionKind.CompleteText:
                    presentation.CompleteText();
                    break;
                case NarrativeUiActionKind.Continue:
                    AdvanceLineOrState(false);
                    break;
                case NarrativeUiActionKind.Skip:
                    SkipRequested?.Invoke(currentState.SkipStateId);
                    break;
            }
        }

        private void HandleInteractiveUiAction(NarrativeUiAction action)
        {
            if (!running || currentState == null || action.TransitionToken != transitionToken || action.StateId != currentState.StateId)
                return;
            if (action.Kind == NarrativeUiActionKind.CommitCommanderIdentity && view.CommanderIdentityView != null)
            {
                CommanderIdentityCommitted?.Invoke(
                    view.CommanderIdentityView.SelectedIdentity,
                    view.CommanderIdentityView.SelectedPortraitIndex);
                CommitInteractiveState(currentState.StateId);
            }
            else if (action.Kind == NarrativeUiActionKind.CommitGuidance && view.GuidanceChoiceView != null)
            {
                GuidanceCommitted?.Invoke(view.GuidanceChoiceView.SelectedGuidance);
                CommitInteractiveState(currentState.StateId);
            }
        }

        private void AdvanceLineOrState(bool respectAuthoredTiming)
        {
            if (currentState == null)
                return;
            if (currentLineIndex + 1 < currentState.Lines.Count)
            {
                if (respectAuthoredTiming && !Reached(stateElapsed, currentState.Lines[currentLineIndex + 1].StartSeconds))
                    return;
                StartLine(currentLineIndex + 1);
                return;
            }
            if (respectAuthoredTiming && !Reached(stateElapsed, Mathf.Max(0.1f, currentState.DurationSeconds)))
                return;
            autoAdvancePending = false;
            ContinueTo(currentState.ContinueStateId);
        }

        private static bool Reached(float elapsed, float target) => elapsed + TimingEpsilonSeconds >= target;

        private void ContinueTo(string stateId)
        {
            hasRouteCompletionOverride = false;
            if (!states.TryGetValue(stateId ?? string.Empty, out NarrativeStateRecord next))
            {
                Cancel();
                return;
            }
            transitionToken++;
            EnterState(next);
        }

        internal static NarrativeCompletionPayload CreateRouteCompletion(NarrativeStateRecord state, bool skipped)
        {
            bool isDebriefArrival = state != null && state.StateId == "first_launch.command_base_reveal";
            if (isDebriefArrival)
                return CreateDebriefCompletion(skipped);
            return new NarrativeCompletionPayload
            {
                PayloadId = "first_launch.m01_handoff_completion",
                Watched = !skipped,
                Skipped = skipped,
                LastCompletedStateId = state?.StateId ?? string.Empty,
                EvidenceIds = Array.Empty<string>(),
                MissionContextFlags = Array.Empty<string>()
            };
        }

        internal static NarrativeCompletionPayload CreateDebriefCompletion(bool skipped)
        {
            return new NarrativeCompletionPayload
            {
                PayloadId = "first_launch.m01_debrief_completion",
                Watched = !skipped,
                Skipped = skipped,
                LastCompletedStateId = "first_launch.command_base_reveal",
                EvidenceIds = new[] { "evidence.aria.revoked_credential_fragment" },
                MissionContextFlags = new[] { "story.m01.corridor_secured", "story.aria.revoked_credential_clue_found" }
            };
        }

        private int IndexOfState(string stateId)
        {
            for (int i = 0; i < config.States.Count; i++)
            {
                if (string.Equals(config.States[i].StateId, stateId, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        private NarrativeStateRecord FindNextPanelState(NarrativeStateRecord state)
        {
            string nextId = state?.ContinueStateId;
            for (int i = 0; i < states.Count && !string.IsNullOrEmpty(nextId); i++)
            {
                if (!states.TryGetValue(nextId, out NarrativeStateRecord candidate))
                    return null;
                if (IsReferenceValid(ResolvePanelReference(candidate)) || ResolveDirectPanel(candidate) != null)
                    return candidate;
                nextId = candidate.ContinueStateId;
            }
            return null;
        }

        private static AssetReferenceSprite ResolvePanelReference(NarrativeStateRecord state)
        {
            if (state == null)
                return null;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            if (aspect >= 2f && IsReferenceValid(state.Panel20x9Reference))
                return state.Panel20x9Reference;
            return IsReferenceValid(state.Panel16x9Reference)
                ? state.Panel16x9Reference
                : state.Panel20x9Reference;
        }

        private static bool IsReferenceValid(AssetReferenceSprite reference) => reference != null && reference.RuntimeKeyIsValid();

        private static Sprite ResolveDirectPanel(NarrativeStateRecord state)
        {
            if (state == null)
                return null;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            return aspect >= 2f && state.Panel20x9 != null ? state.Panel20x9 : state.Panel16x9;
        }
    }
}
