using System;
using Game.Operations.Contracts;
using Game.Operations.Strategic;

namespace Game.Runtime
{
    /// <summary>
    /// Command boundary between the pure strategic reducer and the shared disk profile.
    /// A domain acceptance is published only after its profile replacement succeeds.
    /// Retry with the same command identity after a lost acknowledgment.
    /// </summary>
    public sealed class OperationsProfileCommandService
    {
        private readonly SaveService store;

        public OperationsProfileCommandService(SaveService store) =>
            this.store = store ?? throw new ArgumentNullException(nameof(store));

        public OperationsSaveData Read() => store.LoadProfile().operations;

        public bool TrySubmit(OperationsCommand command, out OperationsCommandResult result, out string error) =>
            TryCommit(session => session.Submit(command), command.Kind == OperationsCommandKind.Deploy ? string.Empty : null, out result, out error);

        public bool TryNewRun(OperationsCommand command, int seed, OperationsDifficultyKind difficulty,
            out OperationsCommandResult result, out string error) =>
            TryCommit(session => session.SubmitNewRun(command, seed, difficulty), string.Empty, out result, out error);

        public bool TrySettle(OperationsCommand command, OperationsMissionResult missionResult, string terminalAttemptJson,
            out OperationsCommandResult result, out string error) =>
            TryCommit(session => session.SubmitResult(command, missionResult), terminalAttemptJson, out result, out error, missionResult);

        private bool TryCommit(Func<OperationsStrategicSession, OperationsCommandResult> reduce,
            string attemptJson, out OperationsCommandResult result, out string error, OperationsMissionResult? medalResult = null)
        {
            result = default;
            error = string.Empty;
            PlayerProfileSaveData profile = store.LoadProfile();
            OperationsSaveMigrationResult migration = OperationsSaveMigration.Migrate(profile.operations);
            if (migration.Data.schemaVersion != OperationsSaveMigration.CurrentSchemaVersion)
            {
                error = "newer_operations_schema";
                return false;
            }

            int previousRevision = profile.operations.profileRevision;
            OperationsStrategicSession session = OperationsStrategicSession.FromMigration(migration);
            OperationsCommandResult candidate = reduce(session);
            if (session.Revision == previousRevision)
            {
                // Duplicate or rejected commands that did not mutate the reducer need no write.
                result = candidate;
                return true;
            }

            OperationsMissionMedalSaveData medal = null;
            if (candidate.Accepted && medalResult.HasValue && medalResult.Value.Outcome == OperationsOutcomeKind.Victory &&
                profile.operations.activeRun != null)
            {
                var attempt = Array.Find(profile.operations.activeRun.attempts ?? Array.Empty<OperationsAttemptSaveData>(),
                    item => item.sessionId == medalResult.Value.SessionId && item.missionId == medalResult.Value.MissionId);
                byte tier = (byte)profile.operations.activeRun.difficulty;
                if (attempt != null && !attempt.practice && tier < 4)
                    medal = new OperationsMissionMedalSaveData { missionId=medalResult.Value.MissionId, difficulty=tier, sessionId=medalResult.Value.SessionId };
            }
            if (!store.TryCommitOperations(profile.profileCommitRevision, previousRevision, session.Save,
                attemptJson ?? profile.operationsAttemptJson, out _, out error, medal))
                return false;
            result = candidate;
            return true;
        }
    }
}
