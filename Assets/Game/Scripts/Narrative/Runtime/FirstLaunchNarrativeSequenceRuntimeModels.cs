using System;
using Game.Catalog.Contracts;

namespace Game.Narrative.Runtime
{
    public enum FirstLaunchNarrativeSequenceIntentKind
    {
        Start = 0,
        StartAt = 1,
        Tick = 2,
        Pause = 3,
        Resume = 4,
        Restart = 5,
        PreviousState = 6,
        NextState = 7,
        Seek = 8,
        CompleteText = 9,
        Continue = 10,
        ContinueState = 11,
        CommitInteractive = 12,
        Skip = 13,
        DialogueAutoAdvance = 14,
        Cancel = 15,
    }

    public enum FirstLaunchNarrativeSequenceOutputKind
    {
        StateEntered = 0,
        LineStarted = 1,
        CompleteTextRequested = 2,
        SkipRequested = 3,
        RouteReached = 4,
        Stopped = 5,
    }

    public readonly struct FirstLaunchNarrativeSequenceIntent
    {
        public FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind kind,
            string stateId = null,
            ulong transitionToken = 0,
            float value = 0f,
            bool enabled = false)
        {
            Kind = kind;
            StateId = stateId;
            TransitionToken = transitionToken;
            Value = value;
            Enabled = enabled;
        }

        public FirstLaunchNarrativeSequenceIntentKind Kind { get; }
        public string StateId { get; }
        public ulong TransitionToken { get; }
        public float Value { get; }
        public bool Enabled { get; }
    }

    public readonly struct FirstLaunchNarrativeSequenceOutput
    {
        public FirstLaunchNarrativeSequenceOutput(
            FirstLaunchNarrativeSequenceOutputKind kind,
            string stateId,
            ulong transitionToken,
            int lineIndex = -1,
            string destinationStateId = null)
        {
            Kind = kind;
            StateId = stateId;
            TransitionToken = transitionToken;
            LineIndex = lineIndex;
            DestinationStateId = destinationStateId;
        }

        public FirstLaunchNarrativeSequenceOutputKind Kind { get; }
        public string StateId { get; }
        public ulong TransitionToken { get; }
        public int LineIndex { get; }
        public string DestinationStateId { get; }
    }

    public readonly struct FirstLaunchNarrativeSequenceStateDefinition
    {
        public FirstLaunchNarrativeSequenceStateDefinition(
            string stateId,
            NarrativeStateKind kind,
            string continueStateId,
            string skipStateId,
            float durationSeconds,
            float[] lineStartSeconds)
        {
            StateId = stateId;
            Kind = kind;
            ContinueStateId = continueStateId;
            SkipStateId = skipStateId;
            DurationSeconds = durationSeconds;
            LineStartSeconds = lineStartSeconds ?? Array.Empty<float>();
        }

        public string StateId { get; }
        public NarrativeStateKind Kind { get; }
        public string ContinueStateId { get; }
        public string SkipStateId { get; }
        public float DurationSeconds { get; }
        public float[] LineStartSeconds { get; }
    }
}
