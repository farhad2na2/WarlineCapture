using System;
using System.IO;
using Game.Operations.Contracts;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class SaveService
    {
        /// <summary>
        /// Commits a verified strategic transaction and its attempt journal in the shipping
        /// profile. Called by the Operations persistence edge after the domain validates the
        /// command/result. Cumulative reward differences are credited in this same replacement.
        /// </summary>
        public bool TryCommitOperations(
            int expectedProfileRevision, int expectedOperationsRevision,
            OperationsSaveData operations, string attemptJson,
            out PlayerProfileSaveData committed, out string reason)
        {
            committed = null;
            reason = string.Empty;
            if (operations == null || operations.schemaVersion != OperationsSaveMigration.CurrentSchemaVersion ||
                expectedOperationsRevision < 0 || expectedOperationsRevision == int.MaxValue ||
                operations.profileRevision != expectedOperationsRevision + 1)
            {
                reason = "invalid_operations_revision";
                return false;
            }

            using IDisposable lease = _repository.AcquireWriteLease(ProfileFileName);
            PlayerProfileSaveData current = LoadProfileForCommit();
            RequireWritableProfile(current);
            OperationsSaveData nextOperations = JsonUtility.FromJson<OperationsSaveData>(JsonUtility.ToJson(operations));
            nextOperations = OperationsSaveMigration.Migrate(nextOperations).Data;
            attemptJson ??= string.Empty;

            // An identical retry after a lost acknowledgment returns the committed receipt,
            // even if another mode subsequently changed unrelated profile fields.
            if (current.operations.profileRevision == nextOperations.profileRevision &&
                current.operationsAttemptJson == attemptJson &&
                JsonUtility.ToJson(current.operations) == JsonUtility.ToJson(nextOperations))
            {
                committed = current;
                return true;
            }

            if (current.profileCommitRevision != expectedProfileRevision ||
                current.operations.profileRevision != expectedOperationsRevision)
            {
                reason = "stale_profile";
                return false;
            }

            long creditDelta = (long)nextOperations.operationsRewardCredits - current.operations.operationsRewardCredits;
            long xpDelta = (long)nextOperations.operationsRewardCommanderXp - current.operations.operationsRewardCommanderXp;
            if (creditDelta < 0 || xpDelta < 0 || creditDelta + current.credits > int.MaxValue ||
                xpDelta + current.commanderXp > int.MaxValue)
            {
                reason = "invalid_reward_ledger";
                return false;
            }

            current.operations = nextOperations;
            current.operationsAttemptJson = attemptJson;
            current.credits += (int)creditDelta;
            current.commanderXp += (int)xpDelta;
            current.profileCommitRevision = checked(current.profileCommitRevision + 1);
            _repository.SaveAtomic(ProfileFileName, current);
            committed = current;
            return true;
        }

        private PlayerProfileSaveData LoadProfileForCommit()
        {
            if (!_repository.Exists(ProfileFileName))
                return NormalizeProfile(new PlayerProfileSaveData(), false);
            string raw = _repository.ReadRaw(ProfileFileName);
            if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith("{", StringComparison.Ordinal))
                throw new InvalidDataException("The saved profile is unreadable; preserve it for recovery.");
            PlayerProfileSaveData profile;
            try { profile = JsonUtility.FromJson<PlayerProfileSaveData>(raw); }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("The saved profile is unreadable; preserve it for recovery.", exception);
            }
            if (profile == null)
                throw new InvalidDataException("The saved profile is missing; preserve it for recovery.");
            RequireWritableProfile(profile);
            return NormalizeProfile(profile,
                raw.IndexOf("\"firstLaunchStatus\"", StringComparison.Ordinal) < 0);
        }

        private static void RequireWritableProfile(PlayerProfileSaveData profile)
        {
            if (profile.profileSchemaVersion > FirstLaunchProfileState.CurrentSchemaVersion ||
                profile.operations?.schemaVersion > OperationsSaveMigration.CurrentSchemaVersion)
                throw new InvalidOperationException("This profile uses a newer schema and is read-only.");
            if (profile.profileCommitRevision < 0 || profile.operations?.profileRevision < 0)
                throw new InvalidDataException("The saved profile revision is invalid; preserve it for recovery.");
        }
    }
}
