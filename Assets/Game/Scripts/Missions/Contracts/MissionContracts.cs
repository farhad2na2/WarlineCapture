using System;
using Game.Narrative.Contracts;

namespace Game.Missions.Contracts
{
    public enum MissionLaunchOriginKind : byte
    {
        None = 0,
        FirstLaunch = 1,
        CampaignOperations = 2
    }

    public enum MissionRunKind : byte
    {
        None = 0,
        FirstClear = 1,
        Retry = 2,
        Replay = 3
    }

    public enum MissionPhaseKind : byte
    {
        None = 0,
        Preparing = 1,
        InteractiveBrief = 2,
        FindSquad = 3,
        MoveToCover = 4,
        ConfirmThreat = 5,
        Engage = 6,
        SecureCorridor = 7,
        Result = 8,
        DebriefFirstClear = 9,
        ReturnReplay = 10
    }

    public enum MissionOutcomeKind : byte
    {
        None = 0,
        Victory = 1,
        Defeat = 2
    }

    public enum MissionActionKind : byte
    {
        None = 0,
        Deploy = 1,
        Retry = 2,
        Continue = 3,
        Exit = 4,
        SetReplayTutorial = 5
    }

    public enum MissionReturnDestinationKind : byte
    {
        None = 0,
        CommandBase = 1,
        CampaignOperations = 2
    }

    public enum MissionObjectiveRuleKind : byte
    {
        None = 0,
        DestroyMissionRole = 1,
        ProtectMissionRole = 2
    }

    public enum MissionStarRuleKind : byte
    {
        None = 0,
        CompleteMission = 1,
        NoSquadLoss = 2,
        CompleteUnderMilliseconds = 3
    }

    public enum MissionRewardKind : byte
    {
        None = 0,
        Credits = 1,
        Materials = 2,
        Fuel = 3,
        Intel = 4
    }

    public readonly struct MissionLaunchPayload : IEquatable<MissionLaunchPayload>
    {
        public MissionLaunchPayload(
            int schemaVersion,
            string missionId,
            string scenarioId,
            string operationMapId,
            MissionLaunchOriginKind launchOrigin,
            MissionRunKind runKind,
            NarrativeGuidanceMode guidance,
            bool replayTutorialEnabled,
            ulong transitionToken,
            string sessionToken,
            int attemptOrdinal,
            int deterministicSeed)
        {
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            MissionContractText.Require(missionId, nameof(missionId));
            MissionContractText.Require(scenarioId, nameof(scenarioId));
            MissionContractText.Require(operationMapId, nameof(operationMapId));
            MissionContractText.Require(sessionToken, nameof(sessionToken));
            if (launchOrigin == MissionLaunchOriginKind.None)
                throw new ArgumentOutOfRangeException(nameof(launchOrigin));
            if (runKind == MissionRunKind.None)
                throw new ArgumentOutOfRangeException(nameof(runKind));
            if (attemptOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            if (deterministicSeed == 0)
                throw new ArgumentOutOfRangeException(nameof(deterministicSeed));

            SchemaVersion = schemaVersion;
            MissionId = missionId;
            ScenarioId = scenarioId;
            OperationMapId = operationMapId;
            LaunchOrigin = launchOrigin;
            RunKind = runKind;
            Guidance = guidance;
            ReplayTutorialEnabled = replayTutorialEnabled;
            TransitionToken = transitionToken;
            SessionToken = sessionToken;
            AttemptOrdinal = attemptOrdinal;
            DeterministicSeed = deterministicSeed;
        }

        public int SchemaVersion { get; }
        public string MissionId { get; }
        public string ScenarioId { get; }
        public string OperationMapId { get; }
        public MissionLaunchOriginKind LaunchOrigin { get; }
        public MissionRunKind RunKind { get; }
        public NarrativeGuidanceMode Guidance { get; }
        public bool ReplayTutorialEnabled { get; }
        public ulong TransitionToken { get; }
        public string SessionToken { get; }
        public int AttemptOrdinal { get; }
        public int DeterministicSeed { get; }

        public bool Equals(MissionLaunchPayload other) =>
            SchemaVersion == other.SchemaVersion && MissionId == other.MissionId &&
            ScenarioId == other.ScenarioId && OperationMapId == other.OperationMapId &&
            LaunchOrigin == other.LaunchOrigin && RunKind == other.RunKind && Guidance == other.Guidance &&
            ReplayTutorialEnabled == other.ReplayTutorialEnabled && TransitionToken == other.TransitionToken &&
            SessionToken == other.SessionToken && AttemptOrdinal == other.AttemptOrdinal &&
            DeterministicSeed == other.DeterministicSeed;

        public override bool Equals(object obj) => obj is MissionLaunchPayload other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(SchemaVersion); hash.Add(MissionId); hash.Add(ScenarioId); hash.Add(OperationMapId);
            hash.Add(LaunchOrigin); hash.Add(RunKind); hash.Add(Guidance); hash.Add(ReplayTutorialEnabled);
            hash.Add(TransitionToken); hash.Add(SessionToken); hash.Add(AttemptOrdinal); hash.Add(DeterministicSeed);
            return hash.ToHashCode();
        }

        public static bool operator ==(MissionLaunchPayload left, MissionLaunchPayload right) => left.Equals(right);
        public static bool operator !=(MissionLaunchPayload left, MissionLaunchPayload right) => !left.Equals(right);
    }

    public readonly struct MissionResultSummary : IEquatable<MissionResultSummary>
    {
        public MissionResultSummary(
            int schemaVersion,
            string missionId,
            string sessionToken,
            int attemptOrdinal,
            MissionOutcomeKind outcome,
            byte stars,
            int elapsedMilliseconds,
            int squadLossCount,
            MissionReturnDestinationKind returnDestination)
        {
            if (schemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            MissionContractText.Require(missionId, nameof(missionId));
            MissionContractText.Require(sessionToken, nameof(sessionToken));
            if (attemptOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            if (outcome == MissionOutcomeKind.None) throw new ArgumentOutOfRangeException(nameof(outcome));
            if (stars > 3) throw new ArgumentOutOfRangeException(nameof(stars));
            if (elapsedMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds));
            if (squadLossCount < 0) throw new ArgumentOutOfRangeException(nameof(squadLossCount));
            if (returnDestination == MissionReturnDestinationKind.None)
                throw new ArgumentOutOfRangeException(nameof(returnDestination));

            SchemaVersion = schemaVersion; MissionId = missionId; SessionToken = sessionToken;
            AttemptOrdinal = attemptOrdinal; Outcome = outcome; Stars = stars;
            ElapsedMilliseconds = elapsedMilliseconds; SquadLossCount = squadLossCount;
            ReturnDestination = returnDestination;
        }

        public int SchemaVersion { get; }
        public string MissionId { get; }
        public string SessionToken { get; }
        public int AttemptOrdinal { get; }
        public MissionOutcomeKind Outcome { get; }
        public byte Stars { get; }
        public int ElapsedMilliseconds { get; }
        public int SquadLossCount { get; }
        public MissionReturnDestinationKind ReturnDestination { get; }

        public bool Equals(MissionResultSummary other) =>
            SchemaVersion == other.SchemaVersion && MissionId == other.MissionId &&
            SessionToken == other.SessionToken && AttemptOrdinal == other.AttemptOrdinal &&
            Outcome == other.Outcome && Stars == other.Stars && ElapsedMilliseconds == other.ElapsedMilliseconds &&
            SquadLossCount == other.SquadLossCount && ReturnDestination == other.ReturnDestination;
        public override bool Equals(object obj) => obj is MissionResultSummary other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            SchemaVersion, MissionId, SessionToken, AttemptOrdinal, Outcome, Stars,
            HashCode.Combine(ElapsedMilliseconds, SquadLossCount, ReturnDestination));
        public static bool operator ==(MissionResultSummary left, MissionResultSummary right) => left.Equals(right);
        public static bool operator !=(MissionResultSummary left, MissionResultSummary right) => !left.Equals(right);
    }

    public readonly struct MissionActionResult : IEquatable<MissionActionResult>
    {
        public MissionActionResult(
            MissionActionKind action,
            bool accepted,
            ulong transitionToken,
            string sessionToken,
            int attemptOrdinal,
            string reasonCode)
        {
            if (action == MissionActionKind.None) throw new ArgumentOutOfRangeException(nameof(action));
            MissionContractText.Require(sessionToken, nameof(sessionToken));
            if (attemptOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            if (!accepted) MissionContractText.Require(reasonCode, nameof(reasonCode));
            Action = action; Accepted = accepted; TransitionToken = transitionToken;
            SessionToken = sessionToken; AttemptOrdinal = attemptOrdinal; ReasonCode = reasonCode ?? string.Empty;
        }

        public MissionActionKind Action { get; }
        public bool Accepted { get; }
        public ulong TransitionToken { get; }
        public string SessionToken { get; }
        public int AttemptOrdinal { get; }
        public string ReasonCode { get; }
        public bool Equals(MissionActionResult other) => Action == other.Action && Accepted == other.Accepted &&
            TransitionToken == other.TransitionToken && SessionToken == other.SessionToken &&
            AttemptOrdinal == other.AttemptOrdinal && ReasonCode == other.ReasonCode;
        public override bool Equals(object obj) => obj is MissionActionResult other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            Action, Accepted, TransitionToken, SessionToken, AttemptOrdinal, ReasonCode);
        public static bool operator ==(MissionActionResult left, MissionActionResult right) => left.Equals(right);
        public static bool operator !=(MissionActionResult left, MissionActionResult right) => !left.Equals(right);
    }

    internal static class MissionContractText
    {
        public static void Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 96)
                throw new ArgumentException("A non-blank value of at most 96 characters is required.", parameterName);
        }
    }
}
