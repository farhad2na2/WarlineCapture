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
        public OperationsDistrictSaveData[] districts = Array.Empty<OperationsDistrictSaveData>();
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
    }

    public sealed class OperationsDayReportSaveData
    {
        public int day;
        public string reportId = string.Empty;
    }

    public sealed class OperationsPendingDeploymentSaveData
    {
        public bool reserved;
        public string offerId = string.Empty;
        public string sessionId = string.Empty;
        public string transactionId = string.Empty;
        public string snapshotHash = string.Empty;
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
                checkpointReference = string.Empty
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
                checkpointReference = source.checkpointReference
            };
        }
    }
}
