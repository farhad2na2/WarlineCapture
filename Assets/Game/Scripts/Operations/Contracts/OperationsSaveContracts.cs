using System;

namespace Game.Operations.Contracts
{
    public enum OperationsSaveDispositionKind : byte
    {
        Empty = 0,
        Current = 1,
        Migrated = 2,
        ReadOnlyUnknown = 3
    }

    public sealed class OperationsSaveData
    {
        public int schemaVersion = OperationsIdentityRules.CurrentSchemaVersion;
        public int profileRevision;
        public OperationsRunSaveData activeRun;
        public OperationsRunSummarySaveData[] runSummaries = Array.Empty<OperationsRunSummarySaveData>();
        public string[] firstClearRewardIds = Array.Empty<string>();
        public string[] practiceMissionIds = Array.Empty<string>();
        public OperationsReceiptSaveData[] receipts = Array.Empty<OperationsReceiptSaveData>();
        public OperationsDayReportSaveData[] committedReports = Array.Empty<OperationsDayReportSaveData>();
        public OperationsPendingDeploymentSaveData pendingDeployment;
        public string checkpointReference = string.Empty;
        public int idSequence;
        public int operationsRewardCredits;
        public int operationsRewardCommanderXp;
        public int repeatCreditsGrantedOnDay;
        public OperationsCommandJournalSaveData[] commandJournal = Array.Empty<OperationsCommandJournalSaveData>();
        public OperationsRewardEntrySaveData[] rewardLedger = Array.Empty<OperationsRewardEntrySaveData>();
    }

    public sealed class OperationsRunSaveData
    {
        public string runId = string.Empty;
        public int revision;
        public int day = OperationsCampaignSchema.StartingDay;
        public int actionPoints = OperationsCampaignSchema.DefaultStartingActionPoints;
        public OperationsDifficultyKind difficulty = OperationsDifficultyKind.Regular;
        public int seed;
        public OperationsRunPhaseKind phase = OperationsRunPhaseKind.None;
        public int consecutiveStableDays;
        public int directorVersion = 1;
        public uint prngState;
        public bool cityCompleted;
        public int liveVictoriesToday;
        public OperationsDistrictSaveData[] districts = Array.Empty<OperationsDistrictSaveData>();
        public OperationsMilestoneSaveData[] milestones = Array.Empty<OperationsMilestoneSaveData>();
        public OperationsOfferSaveData[] offers = Array.Empty<OperationsOfferSaveData>();
        public OperationsIncidentSaveData[] incidents = Array.Empty<OperationsIncidentSaveData>();
        public OperationsSiteSaveData[] sites = Array.Empty<OperationsSiteSaveData>();
        public OperationsRouteSaveData[] routes = Array.Empty<OperationsRouteSaveData>();
        public OperationsActionUseSaveData[] actionUses = Array.Empty<OperationsActionUseSaveData>();
        public OperationsAttemptSaveData[] attempts = Array.Empty<OperationsAttemptSaveData>();
        public OperationsCooldownSaveData[] cooldowns = Array.Empty<OperationsCooldownSaveData>();
        public string[] citywidePriorityOfferIds = Array.Empty<string>();
        public string[] evidenceFlags = Array.Empty<string>();
    }

    public sealed class OperationsDistrictSaveData
    {
        public string districtId = string.Empty;
        public int security;
        public int trust;
        public int infrastructure;
        public int enemyInfluence;
        public int intelConfidence;
        public int heat;
        public int supplyReadiness;
        public OperationsCivilianDensityKind civilianDensity;
        public int changeVersion;
        public string publicHintMissionId = string.Empty;
    }

    public sealed class OperationsRunSummarySaveData
    {
        public string runId = string.Empty;
        public bool cityCompleted;
        public int endedDay;
    }

    public sealed class OperationsReceiptSaveData
    {
        public string transactionId = string.Empty;
        public string settlementKey = string.Empty;
        public string resultHash = string.Empty;
        public string commandId = string.Empty;
        public string grantKind = string.Empty;
        public int rewardCredits;
        public int rewardCommanderXp;
    }

    public sealed class OperationsDayReportSaveData
    {
        public int day;
        public string reportId = string.Empty;
        public int expiredIncidentCount;
        public bool cityStableToday;
        public int consecutiveStableDays;
        public OperationsDistrictDeltaSaveData[] districtDeltas = Array.Empty<OperationsDistrictDeltaSaveData>();
    }

    public sealed class OperationsPendingDeploymentSaveData
    {
        public bool reserved;
        public string offerId = string.Empty;
        public string sessionId = string.Empty;
        public string transactionId = string.Empty;
        public string snapshotHash = string.Empty;
    }

    public sealed class OperationsMilestoneSaveData
    {
        public string missionId = string.Empty;
        public bool attempted;
        public bool victory;
        public int attemptCount;
    }

    public sealed class OperationsOfferSaveData
    {
        public string offerId = string.Empty;
        public string missionId = string.Empty;
        public string districtId = string.Empty;
        public int day;
        public bool deployable;
        public bool urgent;
    }

    public sealed class OperationsIncidentSaveData
    {
        public string incidentId = string.Empty;
        public string districtId = string.Empty;
        public string missionId = string.Empty;
        public string offerId = string.Empty;
        public byte kind;
        public int createdDay;
        public int dueDay;
        public string siteId = string.Empty;
        public string routeId = string.Empty;
    }

    public sealed class OperationsSiteSaveData
    {
        public string siteId = string.Empty;
        public string districtId = string.Empty;
        public byte state;
    }

    public sealed class OperationsRouteSaveData
    {
        public string routeId = string.Empty;
        public string districtId = string.Empty;
        public byte state;
    }

    public sealed class OperationsActionUseSaveData
    {
        public string districtId = string.Empty;
        public byte actionKind;
    }

    public sealed class OperationsAttemptSaveData
    {
        public string sessionId = string.Empty;
        public string offerId = string.Empty;
        public string missionId = string.Empty;
        public string districtId = string.Empty;
        public int attemptOrdinal;
        public string transactionId = string.Empty;
        public string snapshotHash = string.Empty;
        public string resultHash = string.Empty;
        public byte phase;
        public bool practice;
        public bool apRefunded;
    }

    public sealed class OperationsCooldownSaveData
    {
        public string districtId = string.Empty;
        public byte kind;
        public int availableOnDay;
    }

    public sealed class OperationsCommandJournalSaveData
    {
        public string commandId = string.Empty;
        public bool accepted;
        public byte reasonCode;
        public int newRevision;
        public string transactionId = string.Empty;
    }

    public sealed class OperationsRewardEntrySaveData
    {
        public string transactionId = string.Empty;
        public string missionId = string.Empty;
        public int rewardCredits;
        public int rewardCommanderXp;
        public string grantKind = string.Empty;
    }

    public sealed class OperationsDistrictDeltaSaveData
    {
        public string districtId = string.Empty;
        public int requestedSecurity;
        public int requestedTrust;
        public int requestedInfrastructure;
        public int requestedEnemyInfluence;
        public int requestedIntelConfidence;
        public int requestedHeat;
        public int requestedSupplyReadiness;
        public int appliedSecurity;
        public int appliedTrust;
        public int appliedInfrastructure;
        public int appliedEnemyInfluence;
        public int appliedIntelConfidence;
        public int appliedHeat;
        public int appliedSupplyReadiness;
    }

    public readonly struct OperationsSaveMigrationResult
    {
        public OperationsSaveMigrationResult(
            OperationsSaveData data,
            OperationsSaveDispositionKind disposition,
            string reason)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Disposition = disposition;
            Reason = reason ?? string.Empty;
        }

        public OperationsSaveData Data { get; }
        public OperationsSaveDispositionKind Disposition { get; }
        public string Reason { get; }
        public bool CanWrite => Disposition != OperationsSaveDispositionKind.ReadOnlyUnknown;
    }

    public static class OperationsSaveMigration
    {
        public const int CurrentSchemaVersion = OperationsIdentityRules.CurrentSchemaVersion;

        public static OperationsSaveData CreateEmpty()
        {
            return new OperationsSaveData
            {
                schemaVersion = CurrentSchemaVersion,
                profileRevision = 0,
                activeRun = null,
                runSummaries = Array.Empty<OperationsRunSummarySaveData>(),
                firstClearRewardIds = Array.Empty<string>(),
                practiceMissionIds = Array.Empty<string>(),
                receipts = Array.Empty<OperationsReceiptSaveData>(),
                committedReports = Array.Empty<OperationsDayReportSaveData>(),
                pendingDeployment = null,
                checkpointReference = string.Empty,
                idSequence = 0,
                operationsRewardCredits = 0,
                operationsRewardCommanderXp = 0,
                repeatCreditsGrantedOnDay = 0,
                commandJournal = Array.Empty<OperationsCommandJournalSaveData>(),
                rewardLedger = Array.Empty<OperationsRewardEntrySaveData>()
            };
        }

        public static OperationsSaveMigrationResult Migrate(OperationsSaveData source)
        {
            if (source == null)
            {
                return new OperationsSaveMigrationResult(
                    CreateEmpty(),
                    OperationsSaveDispositionKind.Empty,
                    "Missing Operations envelope initialized without touching Campaign or quick-game fields.");
            }

            if (source.schemaVersion > CurrentSchemaVersion)
            {
                return new OperationsSaveMigrationResult(
                    CloneShallow(source),
                    OperationsSaveDispositionKind.ReadOnlyUnknown,
                    "Unknown newer Operations schema is read-only and must not be overwritten.");
            }

            if (source.schemaVersion < 1)
            {
                OperationsSaveData migrated = CreateEmpty();
                migrated.profileRevision = source.profileRevision;
                migrated.firstClearRewardIds = source.firstClearRewardIds ?? Array.Empty<string>();
                migrated.practiceMissionIds = source.practiceMissionIds ?? Array.Empty<string>();
                return new OperationsSaveMigrationResult(
                    migrated,
                    OperationsSaveDispositionKind.Migrated,
                    "Legacy or zero-version envelope migrated to an empty current Operations run.");
            }

            OperationsSaveData current = CloneShallow(source);
            current.schemaVersion = CurrentSchemaVersion;
            current.runSummaries ??= Array.Empty<OperationsRunSummarySaveData>();
            current.firstClearRewardIds ??= Array.Empty<string>();
            current.practiceMissionIds ??= Array.Empty<string>();
            current.receipts ??= Array.Empty<OperationsReceiptSaveData>();
            current.committedReports ??= Array.Empty<OperationsDayReportSaveData>();
            current.checkpointReference ??= string.Empty;
            current.commandJournal ??= Array.Empty<OperationsCommandJournalSaveData>();
            current.rewardLedger ??= Array.Empty<OperationsRewardEntrySaveData>();
            if (current.activeRun != null)
                CoalesceRun(current.activeRun);
            return new OperationsSaveMigrationResult(
                current,
                OperationsSaveDispositionKind.Current,
                "Current Operations schema accepted.");
        }

        public static bool HasActiveRun(OperationsSaveData data) =>
            data?.activeRun != null &&
            !string.IsNullOrWhiteSpace(data.activeRun.runId) &&
            data.activeRun.phase != OperationsRunPhaseKind.None &&
            data.activeRun.phase != OperationsRunPhaseKind.Archived;

        private static OperationsSaveData CloneShallow(OperationsSaveData source)
        {
            return new OperationsSaveData
            {
                schemaVersion = source.schemaVersion,
                profileRevision = source.profileRevision,
                activeRun = source.activeRun,
                runSummaries = source.runSummaries,
                firstClearRewardIds = source.firstClearRewardIds,
                practiceMissionIds = source.practiceMissionIds,
                receipts = source.receipts,
                committedReports = source.committedReports,
                pendingDeployment = source.pendingDeployment,
                checkpointReference = source.checkpointReference,
                idSequence = source.idSequence,
                operationsRewardCredits = source.operationsRewardCredits,
                operationsRewardCommanderXp = source.operationsRewardCommanderXp,
                repeatCreditsGrantedOnDay = source.repeatCreditsGrantedOnDay,
                commandJournal = source.commandJournal,
                rewardLedger = source.rewardLedger
            };
        }

        private static void CoalesceRun(OperationsRunSaveData run)
        {
            run.districts ??= Array.Empty<OperationsDistrictSaveData>();
            run.milestones ??= Array.Empty<OperationsMilestoneSaveData>();
            run.offers ??= Array.Empty<OperationsOfferSaveData>();
            run.incidents ??= Array.Empty<OperationsIncidentSaveData>();
            run.sites ??= Array.Empty<OperationsSiteSaveData>();
            run.routes ??= Array.Empty<OperationsRouteSaveData>();
            run.actionUses ??= Array.Empty<OperationsActionUseSaveData>();
            run.attempts ??= Array.Empty<OperationsAttemptSaveData>();
            run.cooldowns ??= Array.Empty<OperationsCooldownSaveData>();
            run.citywidePriorityOfferIds ??= Array.Empty<string>();
            run.evidenceFlags ??= Array.Empty<string>();
            if (run.directorVersion < 1)
                run.directorVersion = 1;
        }
    }
}
