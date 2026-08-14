namespace Game.UI.Contracts
{
    public enum UiMissionResultOutcome : byte
    {
        Victory,
        Loss
    }

    public enum UiMissionResultActionKind : byte
    {
        None = 0,
        Retry = 1,
        Continue = 2
    }

    public readonly struct UiMissionResultPopupModel
    {
        public readonly uint Version;
        public readonly string MissionId;
        public readonly UiMissionResultOutcome Outcome;
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string SummaryBody;
        public readonly bool ReplayEnabled;
        public readonly byte Stars;
        public readonly string ElapsedText;
        public readonly string SquadLossText;
        public readonly string EnemiesDefeatedText;
        public readonly string RewardsText;
        public readonly string PrimaryActionLabel;
        public readonly bool PrimaryActionEnabled;
        public readonly bool RetryVisible;

        public UiMissionResultPopupModel(
            UiMissionResultOutcome outcome,
            string title,
            string subtitle,
            string summaryBody,
            bool replayEnabled)
        {
            Version = 0;
            MissionId = string.Empty;
            Outcome = outcome;
            Title = title;
            Subtitle = subtitle;
            SummaryBody = summaryBody;
            ReplayEnabled = replayEnabled;
            Stars = 0;
            ElapsedText = string.Empty;
            SquadLossText = string.Empty;
            EnemiesDefeatedText = string.Empty;
            RewardsText = string.Empty;
            PrimaryActionLabel = string.Empty;
            PrimaryActionEnabled = false;
            RetryVisible = replayEnabled;
        }

        public UiMissionResultPopupModel(
            uint version, string missionId, UiMissionResultOutcome outcome, string title,
            string subtitle, string summaryBody, byte stars, string elapsedText,
            string squadLossText, string enemiesDefeatedText, string rewardsText,
            string primaryActionLabel, bool primaryActionEnabled, bool retryVisible)
        {
            Version = version;
            MissionId = missionId ?? string.Empty;
            Outcome = outcome;
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            SummaryBody = summaryBody ?? string.Empty;
            ReplayEnabled = retryVisible;
            Stars = stars > 3 ? (byte)3 : stars;
            ElapsedText = elapsedText ?? string.Empty;
            SquadLossText = squadLossText ?? string.Empty;
            EnemiesDefeatedText = enemiesDefeatedText ?? string.Empty;
            RewardsText = rewardsText ?? string.Empty;
            PrimaryActionLabel = primaryActionLabel ?? string.Empty;
            PrimaryActionEnabled = primaryActionEnabled;
            RetryVisible = retryVisible;
        }

        public static UiMissionResultPopupModel VictoryDefault =>
            new(
                UiMissionResultOutcome.Victory,
                "VICTORY",
                "Sector secured. Command net restored.",
                "Primary objectives completed with acceptable losses. Civilian risk stabilized and remaining hostile cells are retreating.",
                true);

        public static UiMissionResultPopupModel LossDefault =>
            new(
                UiMissionResultOutcome.Loss,
                "MISSION FAILED",
                "Command net disrupted. Extraction required.",
                "Primary objectives were not completed. Regroup, resupply, and redeploy when command authorizes a new operation.",
                true);
    }
}
