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
            TryCommit(session => session.SubmitResult(command, missionResult), terminalAttemptJson, out result, out error);

        private bool TryCommit(Func<OperationsStrategicSession, OperationsCommandResult> reduce,
            string attemptJson, out OperationsCommandResult result, out string error)
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

            if (!store.TryCommitOperations(profile.profileCommitRevision, previousRevision, session.Save,
                attemptJson ?? profile.operationsAttemptJson, out _, out error))
                return false;
            result = candidate;
            return true;
        }
    }
}
