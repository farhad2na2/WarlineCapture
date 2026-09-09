namespace Game.UI.Contracts
{
    public readonly struct UiMissionDefenseResultDetails
    {
        public readonly bool Applicable,PostDamaged,PostDestroyed,CoreBreached,IntegrityFault,ReviewTutorial;
        public readonly int CivilianLosses;
        public UiMissionDefenseResultDetails(int civilianLosses,bool postDamaged,bool postDestroyed,bool coreBreached,bool integrityFault,bool reviewTutorial=false)
        {Applicable=true; CivilianLosses=civilianLosses; PostDamaged=postDamaged; PostDestroyed=postDestroyed; CoreBreached=coreBreached; IntegrityFault=integrityFault; ReviewTutorial=reviewTutorial;}
    }
    public readonly struct UiMissionExtractionResultDetails
    {
        public readonly bool Applicable,CarrierLost,AircraftLost,TimedOut;public readonly int Delivered,Losses,CarrierLeg;
        public UiMissionExtractionResultDetails(int delivered,int losses,int carrierLeg,bool carrierLost,bool aircraftLost,bool timedOut)
        {Applicable=true;Delivered=delivered;Losses=losses;CarrierLeg=carrierLeg;CarrierLost=carrierLost;AircraftLost=aircraftLost;TimedOut=timedOut;}
    }
    public enum UiMissionResultOutcome : byte
    {
        Victory,
        Loss
    }

    public enum UiMissionResultActionKind : byte
    {
        None = 0,
        Retry = 1,
        Continue = 2,
        RetrySave = 3
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
        public readonly bool FirstClear;
        public readonly bool DebriefRequired;
        public readonly bool SettlementFailed;
        public readonly UiMissionDefenseResultDetails Defense;
        public readonly UiMissionExtractionResultDetails Extraction;

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
            FirstClear = false;
            DebriefRequired = false;
            SettlementFailed = false;
            Defense = default;
            Extraction = default;
        }

        public UiMissionResultPopupModel(
            uint version, string missionId, UiMissionResultOutcome outcome, string title,
            string subtitle, string summaryBody, byte stars, string elapsedText,
            string squadLossText, string enemiesDefeatedText, string rewardsText,
            string primaryActionLabel, bool primaryActionEnabled, bool retryVisible,
            bool firstClear = false, bool debriefRequired = false, UiMissionDefenseResultDetails defense = default, bool settlementFailed = false, UiMissionExtractionResultDetails extraction = default)
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
            FirstClear = firstClear;
            DebriefRequired = debriefRequired;
            SettlementFailed = settlementFailed;
            Defense = defense;
            Extraction = extraction;
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
