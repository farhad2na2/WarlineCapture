using Game.Operations.Contracts;

namespace Game.Operations.Loop
{
    public readonly struct OperationsShellFrame
    {
        public OperationsShellFrame(
            string rootRoute,
            string top,
            string[] history,
            bool backEnabled,
            string backReason,
            bool rootEnabled,
            string rootBlockReason,
            int day,
            OperationsLoopPhase phase)
        {
            RootRoute = rootRoute ?? string.Empty;
            Top = top ?? string.Empty;
            History = history ?? System.Array.Empty<string>();
            BackEnabled = backEnabled;
            BackReason = backReason ?? string.Empty;
            RootEnabled = rootEnabled;
            RootBlockReason = rootBlockReason ?? string.Empty;
            Day = day;
            Phase = phase;
        }

        public string RootRoute { get; }
        public string Top { get; }
        public string[] History { get; }
        public bool BackEnabled { get; }
        public string BackReason { get; }
        public bool RootEnabled { get; }
        public string RootBlockReason { get; }
        public int Day { get; }
        public OperationsLoopPhase Phase { get; }
    }

    public readonly struct OperationsBriefingFrame
    {
        public OperationsBriefingFrame(
            string missionId,
            string districtId,
            string mapId,
            string scenarioId,
            int liveApCost,
            int practiceApCost,
            string[] approaches,
            string nameKey,
            string briefKey,
            string debriefKey,
            string contentHash,
            OperationsMissionFamilyKind family)
        {
            MissionId = missionId ?? string.Empty;
            DistrictId = districtId ?? string.Empty;
            MapId = mapId ?? string.Empty;
            ScenarioId = scenarioId ?? string.Empty;
            LiveApCost = liveApCost;
            PracticeApCost = practiceApCost;
            Approaches = approaches ?? System.Array.Empty<string>();
            NameKey = nameKey ?? string.Empty;
            BriefKey = briefKey ?? string.Empty;
            DebriefKey = debriefKey ?? string.Empty;
            ContentHash = contentHash ?? string.Empty;
            Family = family;
        }

        public string MissionId { get; }
        public string DistrictId { get; }
        public string MapId { get; }
        public string ScenarioId { get; }
        public int LiveApCost { get; }
        public int PracticeApCost { get; }
        public string[] Approaches { get; }
        public string NameKey { get; }
        public string BriefKey { get; }
        public string DebriefKey { get; }
        public string ContentHash { get; }
        public OperationsMissionFamilyKind Family { get; }
    }

    public readonly struct OperationsHudObjective
    {
        public OperationsHudObjective(string nodeId, bool optional, bool complete, bool failed, int progress)
        {
            NodeId = nodeId ?? string.Empty;
            Optional = optional;
            Complete = complete;
            Failed = failed;
            Progress = progress;
        }

        public string NodeId { get; }
        public bool Optional { get; }
        public bool Complete { get; }
        public bool Failed { get; }
        public int Progress { get; }
    }

    public readonly struct OperationsHudFrame
    {
        public OperationsHudFrame(
            OperationsHudObjective[] required,
            OperationsHudObjective[] optional,
            bool timerVisible,
            int timerRemaining,
            string escortTarget,
            int protectedSites,
            int civilianDeaths,
            bool withdrawAvailable,
            bool pauseAvailable,
            string cameraFocus,
            bool paused)
        {
            Required = required ?? System.Array.Empty<OperationsHudObjective>();
            Optional = optional ?? System.Array.Empty<OperationsHudObjective>();
            TimerVisible = timerVisible;
            TimerRemaining = timerRemaining;
            EscortTarget = escortTarget ?? string.Empty;
            ProtectedSites = protectedSites;
            CivilianDeaths = civilianDeaths;
            WithdrawAvailable = withdrawAvailable;
            PauseAvailable = pauseAvailable;
            CameraFocus = cameraFocus ?? string.Empty;
            Paused = paused;
        }

        public OperationsHudObjective[] Required { get; }
        public OperationsHudObjective[] Optional { get; }
        public bool TimerVisible { get; }
        public int TimerRemaining { get; }
        public string EscortTarget { get; }
        public int ProtectedSites { get; }
        public int CivilianDeaths { get; }
        public bool WithdrawAvailable { get; }
        public bool PauseAvailable { get; }
        public string CameraFocus { get; }
        public bool Paused { get; }
    }

    public readonly struct OperationsResultFrame
    {
        public OperationsResultFrame(
            OperationsOutcomeKind outcome,
            string terminalReason,
            string districtId,
            int[] before,
            int[] after,
            int civilianDeaths,
            int taskForceLosses,
            int receivedCredits,
            int receivedXp,
            bool rewardsApplied,
            string[] nextOfferMissionIds,
            string resultHash,
            int campaignStarsAwarded,
            OperationsObjectiveFact[] achieved,
            OperationsObjectiveFact[] missed)
        {
            Outcome = outcome;
            TerminalReason = terminalReason ?? string.Empty;
            DistrictId = districtId ?? string.Empty;
            Before = before ?? System.Array.Empty<int>();
            After = after ?? System.Array.Empty<int>();
            CivilianDeaths = civilianDeaths;
            TaskForceLosses = taskForceLosses;
            ReceivedCredits = receivedCredits;
            ReceivedXp = receivedXp;
            RewardsApplied = rewardsApplied;
            NextOfferMissionIds = nextOfferMissionIds ?? System.Array.Empty<string>();
            ResultHash = resultHash ?? string.Empty;
            CampaignStarsAwarded = campaignStarsAwarded;
            Achieved = achieved ?? System.Array.Empty<OperationsObjectiveFact>();
            Missed = missed ?? System.Array.Empty<OperationsObjectiveFact>();
        }

        public OperationsOutcomeKind Outcome { get; }
        public string TerminalReason { get; }
        public string DistrictId { get; }
        public int[] Before { get; }
        public int[] After { get; }
        public int CivilianDeaths { get; }
        public int TaskForceLosses { get; }
        public int ReceivedCredits { get; }
        public int ReceivedXp { get; }
        public bool RewardsApplied { get; }
        public string[] NextOfferMissionIds { get; }
        public string ResultHash { get; }
        public int CampaignStarsAwarded { get; }
        public OperationsObjectiveFact[] Achieved { get; }
        public OperationsObjectiveFact[] Missed { get; }
    }
}
