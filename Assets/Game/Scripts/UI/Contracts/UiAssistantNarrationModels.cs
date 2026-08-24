namespace Game.UI.Contracts
{
    public enum UiAssistantNarrationStateKind : byte
    {
        Off = 0,
        TextOnly = 1,
        Queued = 2,
        Accepted = 3,
        Presented = 4,
        Failed = 5
    }

    public readonly struct UiAssistantNarrationModel
    {
        public readonly byte State;
        public readonly byte Priority;
        public readonly string StatusText;
        public readonly string SubtitleText;
        public readonly string FailureReasonText;
        public readonly bool WaveformPulse;

        public UiAssistantNarrationModel(
            byte state,
            byte priority,
            string statusText,
            string subtitleText,
            string failureReasonText,
            bool waveformPulse)
        {
            State = state;
            Priority = priority;
            StatusText = statusText ?? string.Empty;
            SubtitleText = subtitleText ?? string.Empty;
            FailureReasonText = failureReasonText ?? string.Empty;
            WaveformPulse = waveformPulse;
        }

        public static UiAssistantNarrationModel Empty =>
            new(0, 0, string.Empty, string.Empty, string.Empty, false);
    }
}
