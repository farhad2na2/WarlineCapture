using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;

namespace Game.Narrative.Runtime
{
    public sealed class FirstLaunchNarrativeSequenceUtilitySystemHelper
    {
        private const float TimingEpsilonSeconds = 0.001f;
        private readonly Dictionary<string, FirstLaunchNarrativeSequenceStateDefinition> states =
            new(StringComparer.Ordinal);
        private readonly List<string> stateOrder = new();
        private string entryStateId = string.Empty;
        private FirstLaunchNarrativeSequenceStateDefinition currentState;
        private int currentStateIndex = -1;
        private int currentLineIndex;
        private float stateElapsed;
        private bool autoAdvancePending;

        public event Action<FirstLaunchNarrativeSequenceOutput> Output;

        public bool IsConfigured { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public string CurrentStateId => currentStateIndex >= 0 ? currentState.StateId : string.Empty;
        public NarrativeStateKind CurrentStateKind => currentState.Kind;
        public int CurrentStateIndex => currentStateIndex;
        public int CurrentLineIndex => currentLineIndex;
        public int StateCount => stateOrder.Count;
        public ulong TransitionToken { get; private set; }

        public bool Configure(
            string entryId,
            IReadOnlyList<FirstLaunchNarrativeSequenceStateDefinition> definitions)
        {
            ResetPlayback();
            IsConfigured = false;
            states.Clear();
            stateOrder.Clear();
            entryStateId = entryId ?? string.Empty;
            if (definitions == null || definitions.Count == 0)
                return false;

            for (int i = 0; i < definitions.Count; i++)
            {
                FirstLaunchNarrativeSequenceStateDefinition definition = definitions[i];
                if (string.IsNullOrWhiteSpace(definition.StateId) ||
                    !states.TryAdd(definition.StateId, definition) ||
                    !HasValidLineTiming(definition.LineStartSeconds))
                {
                    states.Clear();
                    stateOrder.Clear();
                    return false;
                }
                stateOrder.Add(definition.StateId);
            }

            if (!states.ContainsKey(entryStateId) || !HasValidLinks() || !IsFullyReachable())
            {
                states.Clear();
                stateOrder.Clear();
                return false;
            }

            IsConfigured = true;
            return true;
        }

        public bool Apply(in FirstLaunchNarrativeSequenceIntent intent)
        {
            switch (intent.Kind)
            {
                case FirstLaunchNarrativeSequenceIntentKind.Start:
                case FirstLaunchNarrativeSequenceIntentKind.Restart:
                    return StartAt(entryStateId);
                case FirstLaunchNarrativeSequenceIntentKind.StartAt:
                    return StartAt(intent.StateId);
                case FirstLaunchNarrativeSequenceIntentKind.Tick:
                    return Tick(intent.Value, intent.Enabled);
                case FirstLaunchNarrativeSequenceIntentKind.Pause:
                    return SetPaused(true);
                case FirstLaunchNarrativeSequenceIntentKind.Resume:
                    return SetPaused(false);
                case FirstLaunchNarrativeSequenceIntentKind.PreviousState:
                    return Navigate(-1);
                case FirstLaunchNarrativeSequenceIntentKind.NextState:
                    return Navigate(1);
                case FirstLaunchNarrativeSequenceIntentKind.Seek:
                    return Seek(intent.Value);
                case FirstLaunchNarrativeSequenceIntentKind.Cancel:
                    return Stop();
            }

            if (!Accepts(intent.StateId, intent.TransitionToken))
                return false;
            switch (intent.Kind)
            {
                case FirstLaunchNarrativeSequenceIntentKind.CompleteText:
                    Emit(FirstLaunchNarrativeSequenceOutputKind.CompleteTextRequested);
                    return true;
                case FirstLaunchNarrativeSequenceIntentKind.Continue:
                    AdvanceLineOrState(false);
                    return true;
                case FirstLaunchNarrativeSequenceIntentKind.ContinueState:
                case FirstLaunchNarrativeSequenceIntentKind.CommitInteractive:
                    ContinueTo(currentState.ContinueStateId);
                    return true;
                case FirstLaunchNarrativeSequenceIntentKind.Skip:
                    Emit(
                        FirstLaunchNarrativeSequenceOutputKind.SkipRequested,
                        destinationStateId: currentState.SkipStateId);
                    return true;
                case FirstLaunchNarrativeSequenceIntentKind.DialogueAutoAdvance:
                    autoAdvancePending = true;
                    AdvanceLineOrState(true);
                    return true;
                default:
                    return false;
            }
        }

        public bool Accepts(string stateId, ulong transitionToken)
        {
            return IsRunning && currentStateIndex >= 0 && transitionToken == TransitionToken &&
                   string.Equals(CurrentStateId, stateId, StringComparison.Ordinal);
        }

        private bool StartAt(string stateId)
        {
            if (!IsConfigured || !states.TryGetValue(stateId ?? string.Empty, out FirstLaunchNarrativeSequenceStateDefinition state))
                return false;
            IsRunning = true;
            IsPaused = false;
            TransitionToken++;
            EnterState(state);
            return true;
        }

        private bool Tick(float deltaTime, bool autoAdvanceEnabled)
        {
            if (!IsRunning || IsPaused || currentStateIndex < 0)
                return false;
            stateElapsed += Math.Max(0f, deltaTime);
            if (currentState.Kind != NarrativeStateKind.PanelDialogue)
                return true;

            float[] lineStarts = currentState.LineStartSeconds;
            if (lineStarts.Length == 0)
            {
                if (autoAdvanceEnabled && Reached(Math.Max(0.1f, currentState.DurationSeconds)))
                    ContinueTo(currentState.ContinueStateId);
                return true;
            }

            if (currentLineIndex < 0 && Reached(lineStarts[0]))
                StartLine(0);
            if (autoAdvancePending)
                AdvanceLineOrState(true);
            return true;
        }

        private bool SetPaused(bool paused)
        {
            if (!IsRunning || IsPaused == paused)
                return false;
            IsPaused = paused;
            return true;
        }

        private bool Navigate(int offset)
        {
            if (!IsConfigured || currentStateIndex < 0)
                return false;
            int targetIndex = currentStateIndex + offset;
            return targetIndex >= 0 && targetIndex < stateOrder.Count && StartAt(stateOrder[targetIndex]);
        }

        private bool Seek(float normalizedPosition)
        {
            if (!IsConfigured || stateOrder.Count == 0)
                return false;
            float position = Math.Max(0f, Math.Min(1f, normalizedPosition));
            int targetIndex = (int)Math.Round(position * (stateOrder.Count - 1), MidpointRounding.AwayFromZero);
            return StartAt(stateOrder[targetIndex]);
        }

        private bool Stop()
        {
            bool changed = IsRunning || currentStateIndex >= 0;
            ResetPlayback();
            if (changed)
                Emit(FirstLaunchNarrativeSequenceOutputKind.Stopped);
            return changed;
        }

        private void EnterState(in FirstLaunchNarrativeSequenceStateDefinition state)
        {
            currentState = state;
            currentStateIndex = stateOrder.IndexOf(state.StateId);
            currentLineIndex = -1;
            stateElapsed = 0f;
            autoAdvancePending = false;
            Emit(FirstLaunchNarrativeSequenceOutputKind.StateEntered);

            if (state.Kind == NarrativeStateKind.PanelDialogue &&
                state.LineStartSeconds.Length > 0 && state.LineStartSeconds[0] <= 0f)
            {
                StartLine(0);
            }
            else if (state.Kind == NarrativeStateKind.RouteHandoff || state.Kind == NarrativeStateKind.RouteArrival)
            {
                IsRunning = false;
                Emit(FirstLaunchNarrativeSequenceOutputKind.RouteReached);
            }
        }

        private void StartLine(int index)
        {
            currentLineIndex = index;
            autoAdvancePending = false;
            Emit(FirstLaunchNarrativeSequenceOutputKind.LineStarted, index);
        }

        private void AdvanceLineOrState(bool respectAuthoredTiming)
        {
            float[] lineStarts = currentState.LineStartSeconds;
            if (currentLineIndex + 1 < lineStarts.Length)
            {
                if (!respectAuthoredTiming || Reached(lineStarts[currentLineIndex + 1]))
                    StartLine(currentLineIndex + 1);
                return;
            }
            if (respectAuthoredTiming && !Reached(Math.Max(0.1f, currentState.DurationSeconds)))
                return;
            autoAdvancePending = false;
            ContinueTo(currentState.ContinueStateId);
        }

        private void ContinueTo(string stateId)
        {
            if (!states.TryGetValue(stateId ?? string.Empty, out FirstLaunchNarrativeSequenceStateDefinition next))
            {
                Stop();
                return;
            }
            TransitionToken++;
            EnterState(next);
        }

        private bool HasValidLinks()
        {
            foreach (FirstLaunchNarrativeSequenceStateDefinition state in states.Values)
            {
                if (!IsKnownOptionalLink(state.ContinueStateId) || !IsKnownOptionalLink(state.SkipStateId))
                    return false;
            }
            return true;
        }

        private bool IsFullyReachable()
        {
            HashSet<string> visited = new(StringComparer.Ordinal);
            Stack<string> pending = new();
            pending.Push(entryStateId);
            while (pending.Count > 0)
            {
                string stateId = pending.Pop();
                if (!visited.Add(stateId))
                    continue;
                FirstLaunchNarrativeSequenceStateDefinition state = states[stateId];
                if (!string.IsNullOrEmpty(state.ContinueStateId))
                    pending.Push(state.ContinueStateId);
                if (!string.IsNullOrEmpty(state.SkipStateId))
                    pending.Push(state.SkipStateId);
            }
            return visited.Count == states.Count;
        }

        private bool IsKnownOptionalLink(string stateId) => string.IsNullOrEmpty(stateId) || states.ContainsKey(stateId);

        private static bool HasValidLineTiming(float[] starts)
        {
            if (starts == null)
                return false;
            float previous = 0f;
            for (int i = 0; i < starts.Length; i++)
            {
                if (starts[i] < 0f || (i > 0 && starts[i] < previous))
                    return false;
                previous = starts[i];
            }
            return true;
        }

        private bool Reached(float target) => stateElapsed + TimingEpsilonSeconds >= target;

        private void Emit(
            FirstLaunchNarrativeSequenceOutputKind kind,
            int lineIndex = -1,
            string destinationStateId = null)
        {
            Output?.Invoke(new FirstLaunchNarrativeSequenceOutput(
                kind,
                CurrentStateId,
                TransitionToken,
                lineIndex,
                destinationStateId));
        }

        private void ResetPlayback()
        {
            IsRunning = false;
            IsPaused = false;
            currentState = default;
            currentStateIndex = -1;
            currentLineIndex = 0;
            stateElapsed = 0f;
            autoAdvancePending = false;
        }
    }
}
