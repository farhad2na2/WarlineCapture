using System;

namespace Game.Operations.Loop
{
    public enum OperationsMatchMode : byte
    {
        None = 0,
        Campaign = 1,
        Skirmish = 2,
        Operations = 3
    }

    public enum OperationsLoopPhase : byte
    {
        Dashboard = 0,
        Reserved = 1,
        LaunchDispatched = 2,
        Active = 3,
        PendingResult = 4,
        Settled = 5
    }

    public enum OperationsRecoveryChoice : byte
    {
        ResumeReservation = 1,
        ResumeCheckpoint = 2,
        RestartAttempt = 3,
        Withdraw = 4,
        RefundTechnicalFailure = 5,
        SettlePendingResult = 6,
        ReturnToDashboard = 7
    }

    public readonly struct OperationsLoopStep
    {
        public OperationsLoopStep(bool accepted, string reason)
        {
            Accepted = accepted;
            Reason = reason ?? string.Empty;
        }

        public bool Accepted { get; }
        public string Reason { get; }

        public static OperationsLoopStep Ok(string reason = "") => new(true, reason);
        public static OperationsLoopStep Reject(string reason) => new(false, reason);
    }

    public readonly struct OperationsLoopOrder
    {
        public OperationsLoopOrder(byte kind, string a, string b, int n)
        {
            Kind = kind;
            A = a ?? string.Empty;
            B = b ?? string.Empty;
            N = n;
        }

        public byte Kind { get; }
        public string A { get; }
        public string B { get; }
        public int N { get; }
    }

    public readonly struct OperationsModeLaunchRequest
    {
        public OperationsModeLaunchRequest(
            OperationsMatchMode mode,
            string mapId,
            string scenarioId,
            string sessionId,
            string returnRoute,
            bool exclusive,
            bool invokesSharedSceneView,
            string dispatchOwner)
        {
            Mode = mode;
            MapId = mapId ?? string.Empty;
            ScenarioId = scenarioId ?? string.Empty;
            SessionId = sessionId ?? string.Empty;
            ReturnRoute = returnRoute ?? string.Empty;
            Exclusive = exclusive;
            InvokesSharedSceneView = invokesSharedSceneView;
            DispatchOwner = dispatchOwner ?? string.Empty;
        }

        public OperationsMatchMode Mode { get; }
        public string MapId { get; }
        public string ScenarioId { get; }
        public string SessionId { get; }
        public string ReturnRoute { get; }
        public bool Exclusive { get; }
        public bool InvokesSharedSceneView { get; }
        public string DispatchOwner { get; }
    }

    public sealed class OperationsLoopDocument
    {
        public OperationsLoopPhase Phase;
        public OperationsMatchMode Slot = OperationsMatchMode.None;
        public string RunId = string.Empty;
        public string SessionId = string.Empty;
        public string OfferId = string.Empty;
        public string MissionId = string.Empty;
        public string DistrictId = string.Empty;
        public string MapId = string.Empty;
        public string ScenarioId = string.Empty;
        public string TransactionId = string.Empty;
        public string SnapshotHash = string.Empty;
        public string ContentHash = string.Empty;
        public int Seed;
        public int AttemptOrdinal;
        public int RunRevision;
        public int DefinitionVersion = 1;
        public int RestartCount;
        public int CheckpointSequence;
        public bool Practice;
        public bool ReturnAcknowledged;
        public bool RefundEligible;
        public string PublishedCheckpointId = string.Empty;
        public string ResultText = string.Empty;
        public string ResultHash = string.Empty;
        public string BeforeMetrics = string.Empty;
        public string History = OperationsShellNames.Operations;
        public int CreditsAtLaunch;
        public int XpAtLaunch;
        public int ActionPointsAtLaunch;

        public OperationsLoopDocument Copy()
        {
            return new OperationsLoopDocument
            {
                Phase = Phase,
                Slot = Slot,
                RunId = RunId,
                SessionId = SessionId,
                OfferId = OfferId,
                MissionId = MissionId,
                DistrictId = DistrictId,
                MapId = MapId,
                ScenarioId = ScenarioId,
                TransactionId = TransactionId,
                SnapshotHash = SnapshotHash,
                ContentHash = ContentHash,
                Seed = Seed,
                AttemptOrdinal = AttemptOrdinal,
                RunRevision = RunRevision,
                DefinitionVersion = DefinitionVersion,
                RestartCount = RestartCount,
                CheckpointSequence = CheckpointSequence,
                Practice = Practice,
                ReturnAcknowledged = ReturnAcknowledged,
                RefundEligible = RefundEligible,
                PublishedCheckpointId = PublishedCheckpointId,
                ResultText = ResultText,
                ResultHash = ResultHash,
                BeforeMetrics = BeforeMetrics,
                History = History,
                CreditsAtLaunch = CreditsAtLaunch,
                XpAtLaunch = XpAtLaunch,
                ActionPointsAtLaunch = ActionPointsAtLaunch
            };
        }
    }

    public static class OperationsShellNames
    {
        public const string Operations = "Operations";
        public const string DistrictDetail = "DistrictDetail";
        public const string MissionBriefing = "MissionBriefing";
        public const string Match = "Match";
        public const string MissionResult = "MissionResult";
        public const string EndOfDayReport = "EndOfDayReport";
        public const string DispatchOwner = "Game.Operations.Loop";
    }
}
