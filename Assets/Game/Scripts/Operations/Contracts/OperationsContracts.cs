using System;

namespace Game.Operations.Contracts
{
    public enum OperationsOutcomeKind : byte
    {
        None = 0,
        Victory = 1,
        Partial = 2,
        Defeat = 3,
        Withdrawn = 4,
        TechnicalFailure = 5
    }

    public enum OperationsMissionFamilyKind : byte
    {
        None = 0,
        Recon = 1,
        Patrol = 2,
        Raid = 3,
        Rescue = 4,
        Escort = 5,
        Repair = 6,
        Defense = 7,
        Interdict = 8,
        Seize = 9,
        Airlift = 10,
        Breach = 11,
        Finale = 12
    }

    public enum OperationsAbstractActionKind : byte
    {
        None = 0,
        Analyze = 1,
        Community = 2,
        Service = 3,
        Patrol = 4,
        Allocate = 5,
        Deescalate = 6
    }

    /// <summary>
    /// Ordinals match verified <c>Game.UI.Runtime.DistrictOperationActionKind</c>
    /// (Patrol=0, DroneScan=1, Aid=2, Raid=3, Repair=4). That UI enum stays in
    /// Game.UI.Runtime; this contract copy exists so Operations can map without
    /// referencing UI assemblies.
    /// </summary>
    public enum OperationsDashboardActionKind : byte
    {
        Patrol = 0,
        DroneScan = 1,
        Aid = 2,
        Raid = 3,
        Repair = 4
    }

    public enum OperationsCommandKind : byte
    {
        None = 0,
        Deploy = 1,
        AbstractAction = 2,
        EndDay = 3,
        Conclude = 4,
        Withdraw = 5,
        Practice = 6,
        Resume = 7,
        NewRun = 8,
        TechnicalFailure = 9
    }

    public enum OperationsDifficultyKind : byte
    {
        Recruit = 0,
        Regular = 1,
        Veteran = 2,
        Commander = 3
    }

    public enum OperationsRunPhaseKind : byte
    {
        None = 0,
        Dashboard = 1,
        Reserved = 2,
        Active = 3,
        PendingSettlement = 4,
        Report = 5,
        Archived = 6
    }

    public enum OperationsSiteStateKind : byte
    {
        Intact = 0,
        Damaged = 1,
        Restored = 2
    }

    public enum OperationsRouteStateKind : byte
    {
        Open = 0,
        Contested = 1,
        Blocked = 2
    }

    public enum OperationsCivilianDensityKind : byte
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum OperationsDistrictMetricKind : byte
    {
        Security = 0,
        Trust = 1,
        Infrastructure = 2,
        EnemyInfluence = 3,
        IntelConfidence = 4,
        Heat = 5,
        SupplyReadiness = 6
    }

    public enum OperationsIncidentKind : byte
    {
        None = 0,
        ServiceDisruption = 1,
        RoadBlockade = 2,
        HostilePressure = 3
    }

    public enum OperationsForcePackageKind : byte
    {
        None = 0,
        Light = 1,
        Service = 2,
        Ground = 3,
        Air = 4,
        Combined = 5
    }

    public enum OperationsEnemyPackageKind : byte
    {
        None = 0,
        Cell = 1,
        Raiders = 2,
        Mechanized = 3,
        Airfield = 4,
        Finale = 5
    }

    public enum OperationsReasonCode : byte
    {
        None = 0,
        InvalidRevision = 1,
        DuplicateCommand = 2,
        OfferUnavailable = 3,
        AttemptConflict = 4,
        InsufficientActionPoints = 5,
        InsufficientIntel = 6,
        ActionLimitReached = 7,
        PreconditionFailed = 8,
        SchemaUnknown = 9,
        Conflict = 10,
        TechnicalFailure = 11
    }

    public readonly struct OperationsLaunchPayload : IEquatable<OperationsLaunchPayload>
    {
        public OperationsLaunchPayload(
            int schemaVersion,
            string runId,
            string districtId,
            string offerId,
            string missionId,
            string scenarioId,
            string operationMapId,
            int runRevision,
            int definitionVersion,
            string contentHash,
            OperationsDifficultyKind difficulty,
            int seed,
            string transactionId,
            string sessionId,
            int attemptOrdinal,
            string launchSnapshotHash,
            bool isPractice)
        {
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            OperationsContractText.RequireId(runId, nameof(runId), v => OperationsIdentityRules.IsValidGeneratedId(v, "run"));
            OperationsContractText.RequireId(districtId, nameof(districtId), OperationsIdentityRules.IsValidDistrictId);
            OperationsContractText.RequireId(offerId, nameof(offerId), v => OperationsIdentityRules.IsValidGeneratedId(v, "offer"));
            OperationsContractText.RequireId(missionId, nameof(missionId), OperationsIdentityRules.IsValidMissionId);
            OperationsContractText.RequireId(scenarioId, nameof(scenarioId), OperationsIdentityRules.IsValidScenarioId);
            OperationsContractText.RequireId(operationMapId, nameof(operationMapId), OperationsIdentityRules.IsValidOperationMapId);
            OperationsContractText.RequireNonNegative(runRevision, nameof(runRevision));
            if (definitionVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(definitionVersion));
            OperationsContractText.RequireToken(contentHash, nameof(contentHash));
            if (seed == 0)
                throw new ArgumentOutOfRangeException(nameof(seed));
            OperationsContractText.RequireId(transactionId, nameof(transactionId), v => OperationsIdentityRules.IsValidGeneratedId(v, "txn"));
            OperationsContractText.RequireId(sessionId, nameof(sessionId), v => OperationsIdentityRules.IsValidGeneratedId(v, "session"));
            OperationsContractText.RequireNonNegative(attemptOrdinal, nameof(attemptOrdinal));
            OperationsContractText.RequireToken(launchSnapshotHash, nameof(launchSnapshotHash));

            SchemaVersion = schemaVersion;
            RunId = runId;
            DistrictId = districtId;
            OfferId = offerId;
            MissionId = missionId;
            ScenarioId = scenarioId;
            OperationMapId = operationMapId;
            RunRevision = runRevision;
            DefinitionVersion = definitionVersion;
            ContentHash = contentHash;
            Difficulty = difficulty;
            Seed = seed;
            TransactionId = transactionId;
            SessionId = sessionId;
            AttemptOrdinal = attemptOrdinal;
            LaunchSnapshotHash = launchSnapshotHash;
            IsPractice = isPractice;
        }

        public int SchemaVersion { get; }
        public string RunId { get; }
        public string DistrictId { get; }
        public string OfferId { get; }
        public string MissionId { get; }
        public string ScenarioId { get; }
        public string OperationMapId { get; }
        public int RunRevision { get; }
        public int DefinitionVersion { get; }
        public string ContentHash { get; }
        public OperationsDifficultyKind Difficulty { get; }
        public int Seed { get; }
        public string TransactionId { get; }
        public string SessionId { get; }
        public int AttemptOrdinal { get; }
        public string LaunchSnapshotHash { get; }
        public bool IsPractice { get; }

        public bool Equals(OperationsLaunchPayload other) =>
            SchemaVersion == other.SchemaVersion &&
            RunId == other.RunId &&
            DistrictId == other.DistrictId &&
            OfferId == other.OfferId &&
            MissionId == other.MissionId &&
            ScenarioId == other.ScenarioId &&
            OperationMapId == other.OperationMapId &&
            RunRevision == other.RunRevision &&
            DefinitionVersion == other.DefinitionVersion &&
            ContentHash == other.ContentHash &&
            Difficulty == other.Difficulty &&
            Seed == other.Seed &&
            TransactionId == other.TransactionId &&
            SessionId == other.SessionId &&
            AttemptOrdinal == other.AttemptOrdinal &&
            LaunchSnapshotHash == other.LaunchSnapshotHash &&
            IsPractice == other.IsPractice;

        public override bool Equals(object obj) => obj is OperationsLaunchPayload other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(SchemaVersion);
            hash.Add(RunId);
            hash.Add(DistrictId);
            hash.Add(OfferId);
            hash.Add(MissionId);
            hash.Add(ScenarioId);
            hash.Add(OperationMapId);
            hash.Add(RunRevision);
            hash.Add(DefinitionVersion);
            hash.Add(ContentHash);
            hash.Add(Difficulty);
            hash.Add(Seed);
            hash.Add(TransactionId);
            hash.Add(SessionId);
            hash.Add(AttemptOrdinal);
            hash.Add(LaunchSnapshotHash);
            hash.Add(IsPractice);
            return hash.ToHashCode();
        }

        public static bool operator ==(OperationsLaunchPayload left, OperationsLaunchPayload right) => left.Equals(right);
        public static bool operator !=(OperationsLaunchPayload left, OperationsLaunchPayload right) => !left.Equals(right);
    }

    public readonly struct OperationsCommand : IEquatable<OperationsCommand>
    {
        public OperationsCommand(
            string commandId,
            int expectedRunRevision,
            OperationsCommandKind kind,
            string districtId,
            string offerId,
            string actionId)
        {
            OperationsContractText.RequireId(commandId, nameof(commandId), v => OperationsIdentityRules.IsValidGeneratedId(v, "cmd"));
            OperationsContractText.RequireNonNegative(expectedRunRevision, nameof(expectedRunRevision));
            if (kind == OperationsCommandKind.None)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!string.IsNullOrEmpty(districtId))
                OperationsContractText.RequireId(districtId, nameof(districtId), OperationsIdentityRules.IsValidDistrictId);
            if (!string.IsNullOrEmpty(offerId))
                OperationsContractText.RequireId(offerId, nameof(offerId), v => OperationsIdentityRules.IsValidGeneratedId(v, "offer"));
            if (!string.IsNullOrEmpty(actionId))
                OperationsContractText.RequireId(actionId, nameof(actionId), OperationsIdentityRules.IsValidActionId);

            CommandId = commandId;
            ExpectedRunRevision = expectedRunRevision;
            Kind = kind;
            DistrictId = districtId ?? string.Empty;
            OfferId = offerId ?? string.Empty;
            ActionId = actionId ?? string.Empty;
        }

        public string CommandId { get; }
        public int ExpectedRunRevision { get; }
        public OperationsCommandKind Kind { get; }
        public string DistrictId { get; }
        public string OfferId { get; }
        public string ActionId { get; }

        public bool Equals(OperationsCommand other) =>
            CommandId == other.CommandId &&
            ExpectedRunRevision == other.ExpectedRunRevision &&
            Kind == other.Kind &&
            DistrictId == other.DistrictId &&
            OfferId == other.OfferId &&
            ActionId == other.ActionId;

        public override bool Equals(object obj) => obj is OperationsCommand other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(CommandId, ExpectedRunRevision, Kind, DistrictId, OfferId, ActionId);
        public static bool operator ==(OperationsCommand left, OperationsCommand right) => left.Equals(right);
        public static bool operator !=(OperationsCommand left, OperationsCommand right) => !left.Equals(right);
    }

    public readonly struct OperationsCommandResult : IEquatable<OperationsCommandResult>
    {
        public OperationsCommandResult(
            string commandId,
            bool accepted,
            OperationsReasonCode reasonCode,
            int newRevision,
            string transactionId)
        {
            OperationsContractText.RequireId(commandId, nameof(commandId), v => OperationsIdentityRules.IsValidGeneratedId(v, "cmd"));
            if (!accepted && reasonCode == OperationsReasonCode.None)
                throw new ArgumentOutOfRangeException(nameof(reasonCode));
            OperationsContractText.RequireNonNegative(newRevision, nameof(newRevision));
            if (accepted)
                OperationsContractText.RequireId(transactionId, nameof(transactionId), v => OperationsIdentityRules.IsValidGeneratedId(v, "txn"));
            else if (!string.IsNullOrEmpty(transactionId))
                OperationsContractText.RequireId(transactionId, nameof(transactionId), v => OperationsIdentityRules.IsValidGeneratedId(v, "txn"));

            CommandId = commandId;
            Accepted = accepted;
            ReasonCode = reasonCode;
            NewRevision = newRevision;
            TransactionId = transactionId ?? string.Empty;
        }

        public string CommandId { get; }
        public bool Accepted { get; }
        public OperationsReasonCode ReasonCode { get; }
        public int NewRevision { get; }
        public string TransactionId { get; }

        public bool Equals(OperationsCommandResult other) =>
            CommandId == other.CommandId &&
            Accepted == other.Accepted &&
            ReasonCode == other.ReasonCode &&
            NewRevision == other.NewRevision &&
            TransactionId == other.TransactionId;

        public override bool Equals(object obj) => obj is OperationsCommandResult other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(CommandId, Accepted, ReasonCode, NewRevision, TransactionId);
        public static bool operator ==(OperationsCommandResult left, OperationsCommandResult right) => left.Equals(right);
        public static bool operator !=(OperationsCommandResult left, OperationsCommandResult right) => !left.Equals(right);
    }

    public static class OperationsDashboardActionMap
    {
        public static bool TryMapAbstract(
            OperationsDashboardActionKind dashboardAction,
            out OperationsAbstractActionKind abstractAction)
        {
            switch (dashboardAction)
            {
                case OperationsDashboardActionKind.Patrol:
                    abstractAction = OperationsAbstractActionKind.Patrol;
                    return true;
                case OperationsDashboardActionKind.DroneScan:
                    abstractAction = OperationsAbstractActionKind.Analyze;
                    return true;
                case OperationsDashboardActionKind.Aid:
                    abstractAction = OperationsAbstractActionKind.Community;
                    return true;
                case OperationsDashboardActionKind.Repair:
                    abstractAction = OperationsAbstractActionKind.Service;
                    return true;
                default:
                    abstractAction = OperationsAbstractActionKind.None;
                    return false;
            }
        }

        public static bool IsTacticalOfferAction(OperationsDashboardActionKind dashboardAction) =>
            dashboardAction == OperationsDashboardActionKind.Raid;

        public static string ActionId(OperationsAbstractActionKind kind) => kind switch
        {
            OperationsAbstractActionKind.Analyze => "action.operations.analyze",
            OperationsAbstractActionKind.Community => "action.operations.community",
            OperationsAbstractActionKind.Service => "action.operations.service",
            OperationsAbstractActionKind.Patrol => "action.operations.patrol",
            OperationsAbstractActionKind.Allocate => "action.operations.allocate",
            OperationsAbstractActionKind.Deescalate => "action.operations.deescalate",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public static class OperationsFamilyIds
    {
        public static string ToCatalogToken(OperationsMissionFamilyKind family) => family switch
        {
            OperationsMissionFamilyKind.Recon => "RECON",
            OperationsMissionFamilyKind.Patrol => "PATROL",
            OperationsMissionFamilyKind.Raid => "RAID",
            OperationsMissionFamilyKind.Rescue => "RESCUE",
            OperationsMissionFamilyKind.Escort => "ESCORT",
            OperationsMissionFamilyKind.Repair => "REPAIR",
            OperationsMissionFamilyKind.Defense => "DEFENSE",
            OperationsMissionFamilyKind.Interdict => "INTERDICT",
            OperationsMissionFamilyKind.Seize => "SEIZE",
            OperationsMissionFamilyKind.Airlift => "AIRLIFT",
            OperationsMissionFamilyKind.Breach => "BREACH",
            OperationsMissionFamilyKind.Finale => "FINALE",
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };

        public static bool TryParseCatalogToken(string token, out OperationsMissionFamilyKind family)
        {
            switch (token)
            {
                case "RECON": family = OperationsMissionFamilyKind.Recon; return true;
                case "PATROL": family = OperationsMissionFamilyKind.Patrol; return true;
                case "RAID": family = OperationsMissionFamilyKind.Raid; return true;
                case "RESCUE": family = OperationsMissionFamilyKind.Rescue; return true;
                case "ESCORT": family = OperationsMissionFamilyKind.Escort; return true;
                case "REPAIR": family = OperationsMissionFamilyKind.Repair; return true;
                case "DEFENSE": family = OperationsMissionFamilyKind.Defense; return true;
                case "INTERDICT": family = OperationsMissionFamilyKind.Interdict; return true;
                case "SEIZE": family = OperationsMissionFamilyKind.Seize; return true;
                case "AIRLIFT": family = OperationsMissionFamilyKind.Airlift; return true;
                case "BREACH": family = OperationsMissionFamilyKind.Breach; return true;
                case "FINALE": family = OperationsMissionFamilyKind.Finale; return true;
                default:
                    family = OperationsMissionFamilyKind.None;
                    return false;
            }
        }
    }

    public static class OperationsForcePackageIds
    {
        public static string ToCatalogToken(OperationsForcePackageKind kind) => kind switch
        {
            OperationsForcePackageKind.Light => "FP_LIGHT",
            OperationsForcePackageKind.Service => "FP_SERVICE",
            OperationsForcePackageKind.Ground => "FP_GROUND",
            OperationsForcePackageKind.Air => "FP_AIR",
            OperationsForcePackageKind.Combined => "FP_COMBINED",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        public static bool TryParseCatalogToken(string token, out OperationsForcePackageKind kind)
        {
            switch (token)
            {
                case "FP_LIGHT": kind = OperationsForcePackageKind.Light; return true;
                case "FP_SERVICE": kind = OperationsForcePackageKind.Service; return true;
                case "FP_GROUND": kind = OperationsForcePackageKind.Ground; return true;
                case "FP_AIR": kind = OperationsForcePackageKind.Air; return true;
                case "FP_COMBINED": kind = OperationsForcePackageKind.Combined; return true;
                default:
                    kind = OperationsForcePackageKind.None;
                    return false;
            }
        }

        public static bool TryParseEnemyToken(string token, out OperationsEnemyPackageKind kind)
        {
            switch (token)
            {
                case "EP_CELL": kind = OperationsEnemyPackageKind.Cell; return true;
                case "EP_RAIDERS": kind = OperationsEnemyPackageKind.Raiders; return true;
                case "EP_MECHANIZED": kind = OperationsEnemyPackageKind.Mechanized; return true;
                case "EP_AIRFIELD": kind = OperationsEnemyPackageKind.Airfield; return true;
                case "EP_FINALE": kind = OperationsEnemyPackageKind.Finale; return true;
                default:
                    kind = OperationsEnemyPackageKind.None;
                    return false;
            }
        }
    }
}
