using System;

namespace Game.Operations.Contracts
{
    public readonly struct OperationsObjectiveFact : IEquatable<OperationsObjectiveFact>
    {
        public OperationsObjectiveFact(string nodeId, bool completed, bool failed, int count)
        {
            OperationsContractText.RequireToken(nodeId, nameof(nodeId));
            OperationsContractText.RequireNonNegative(count, nameof(count));
            NodeId = nodeId;
            Completed = completed;
            Failed = failed;
            Count = count;
        }

        public string NodeId { get; }
        public bool Completed { get; }
        public bool Failed { get; }
        public int Count { get; }

        public bool Equals(OperationsObjectiveFact other) =>
            NodeId == other.NodeId && Completed == other.Completed && Failed == other.Failed && Count == other.Count;

        public override bool Equals(object obj) => obj is OperationsObjectiveFact other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(NodeId, Completed, Failed, Count);
    }

    public readonly struct OperationsMissionResult : IEquatable<OperationsMissionResult>
    {
        public OperationsMissionResult(
            int schemaVersion,
            string runId,
            string offerId,
            string missionId,
            string sessionId,
            int attemptOrdinal,
            int definitionVersion,
            OperationsOutcomeKind outcome,
            string terminalReason,
            int elapsedTicks,
            OperationsObjectiveFact[] mandatoryObjectiveFacts,
            OperationsObjectiveFact[] optionalObjectiveFacts,
            int civilianDeaths,
            OperationsObjectiveFact[] protectedSiteFacts,
            OperationsObjectiveFact[] deliveredCargoFacts,
            string[] extractedEvidenceIds,
            int taskForceLosses,
            int initialTaskForceCount,
            string resultHash)
        {
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            OperationsContractText.RequireId(runId, nameof(runId), v => OperationsIdentityRules.IsValidGeneratedId(v, "run"));
            OperationsContractText.RequireId(offerId, nameof(offerId), v => OperationsIdentityRules.IsValidGeneratedId(v, "offer"));
            OperationsContractText.RequireId(missionId, nameof(missionId), OperationsIdentityRules.IsValidMissionId);
            OperationsContractText.RequireId(sessionId, nameof(sessionId), v => OperationsIdentityRules.IsValidGeneratedId(v, "session"));
            OperationsContractText.RequireNonNegative(attemptOrdinal, nameof(attemptOrdinal));
            if (definitionVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(definitionVersion));
            if (outcome == OperationsOutcomeKind.None)
                throw new ArgumentOutOfRangeException(nameof(outcome));
            OperationsContractText.RequireToken(terminalReason, nameof(terminalReason));
            OperationsContractText.RequireNonNegative(elapsedTicks, nameof(elapsedTicks));
            OperationsContractText.RequireNonNegative(civilianDeaths, nameof(civilianDeaths));
            OperationsContractText.RequireNonNegative(taskForceLosses, nameof(taskForceLosses));
            OperationsContractText.RequireNonNegative(initialTaskForceCount, nameof(initialTaskForceCount));
            if (taskForceLosses > initialTaskForceCount)
                throw new ArgumentOutOfRangeException(nameof(taskForceLosses));
            OperationsContractText.RequireToken(resultHash, nameof(resultHash));

            SchemaVersion = schemaVersion;
            RunId = runId;
            OfferId = offerId;
            MissionId = missionId;
            SessionId = sessionId;
            AttemptOrdinal = attemptOrdinal;
            DefinitionVersion = definitionVersion;
            Outcome = outcome;
            TerminalReason = terminalReason;
            ElapsedTicks = elapsedTicks;
            MandatoryObjectiveFacts = mandatoryObjectiveFacts ?? Array.Empty<OperationsObjectiveFact>();
            OptionalObjectiveFacts = optionalObjectiveFacts ?? Array.Empty<OperationsObjectiveFact>();
            CivilianDeaths = civilianDeaths;
            ProtectedSiteFacts = protectedSiteFacts ?? Array.Empty<OperationsObjectiveFact>();
            DeliveredCargoFacts = deliveredCargoFacts ?? Array.Empty<OperationsObjectiveFact>();
            ExtractedEvidenceIds = extractedEvidenceIds ?? Array.Empty<string>();
            TaskForceLosses = taskForceLosses;
            InitialTaskForceCount = initialTaskForceCount;
            ResultHash = resultHash;
        }

        public int SchemaVersion { get; }
        public string RunId { get; }
        public string OfferId { get; }
        public string MissionId { get; }
        public string SessionId { get; }
        public int AttemptOrdinal { get; }
        public int DefinitionVersion { get; }
        public OperationsOutcomeKind Outcome { get; }
        public string TerminalReason { get; }
        public int ElapsedTicks { get; }
        public OperationsObjectiveFact[] MandatoryObjectiveFacts { get; }
        public OperationsObjectiveFact[] OptionalObjectiveFacts { get; }
        public int CivilianDeaths { get; }
        public OperationsObjectiveFact[] ProtectedSiteFacts { get; }
        public OperationsObjectiveFact[] DeliveredCargoFacts { get; }
        public string[] ExtractedEvidenceIds { get; }
        public int TaskForceLosses { get; }
        public int InitialTaskForceCount { get; }
        public string ResultHash { get; }

        public bool Equals(OperationsMissionResult other)
        {
            if (SchemaVersion != other.SchemaVersion ||
                RunId != other.RunId ||
                OfferId != other.OfferId ||
                MissionId != other.MissionId ||
                SessionId != other.SessionId ||
                AttemptOrdinal != other.AttemptOrdinal ||
                DefinitionVersion != other.DefinitionVersion ||
                Outcome != other.Outcome ||
                TerminalReason != other.TerminalReason ||
                ElapsedTicks != other.ElapsedTicks ||
                CivilianDeaths != other.CivilianDeaths ||
                TaskForceLosses != other.TaskForceLosses ||
                InitialTaskForceCount != other.InitialTaskForceCount ||
                ResultHash != other.ResultHash)
                return false;
            return EqualFacts(MandatoryObjectiveFacts, other.MandatoryObjectiveFacts) &&
                   EqualFacts(OptionalObjectiveFacts, other.OptionalObjectiveFacts) &&
                   EqualFacts(ProtectedSiteFacts, other.ProtectedSiteFacts) &&
                   EqualFacts(DeliveredCargoFacts, other.DeliveredCargoFacts) &&
                   EqualStrings(ExtractedEvidenceIds, other.ExtractedEvidenceIds);
        }

        public override bool Equals(object obj) => obj is OperationsMissionResult other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(RunId, OfferId, MissionId, SessionId, AttemptOrdinal, Outcome, ResultHash);

        public static bool operator ==(OperationsMissionResult left, OperationsMissionResult right) => left.Equals(right);
        public static bool operator !=(OperationsMissionResult left, OperationsMissionResult right) => !left.Equals(right);

        private static bool EqualFacts(OperationsObjectiveFact[] left, OperationsObjectiveFact[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int index = 0; index < left.Length; index++)
                if (!left[index].Equals(right[index]))
                    return false;
            return true;
        }

        private static bool EqualStrings(string[] left, string[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int index = 0; index < left.Length; index++)
                if (left[index] != right[index])
                    return false;
            return true;
        }
    }
}
