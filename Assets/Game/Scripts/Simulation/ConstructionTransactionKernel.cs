namespace Game.Simulation
{
    public enum ConstructionMutationResult : byte
    {
        Applied,
        InsufficientCredits,
        InsufficientMaterials,
        InsufficientCreditsAndMaterials,
        DuplicateTransaction,
        Rejected
    }

    public enum ConstructionTransactionFailure : byte
    {
        None,
        MissingPlacement,
        BlockedPlacement,
        InvalidPlacement,
        InsufficientCredits,
        InsufficientMaterials,
        InsufficientCreditsAndMaterials,
        DuplicateTransaction,
        TransactionRejected,
        RegistrationFailed
    }

    public readonly struct ConstructionCommitResult
    {
        public int CommittedCount { get; }
        public int ExpectedCount { get; }
        public bool PlacementCommitted => CommittedCount > 0;
        public bool FullyCommitted => ExpectedCount > 0 && CommittedCount == ExpectedCount;

        public ConstructionCommitResult(int committedCount, int expectedCount)
        {
            CommittedCount = committedCount;
            ExpectedCount = expectedCount;
        }
    }

    public interface IConstructionTransactionHost<TPlacement>
    {
        bool Validate(TPlacement placement);
        void GetCosts(TPlacement placement, out int creditsCost, out int materialsCost);
        ConstructionMutationResult TryReserve(int transactionId, int creditsCost, int materialsCost);
        ConstructionCommitResult Commit(TPlacement placement);
        ConstructionMutationResult TryFinalize(int transactionId);
        ConstructionMutationResult TryRollback(int transactionId);
    }

    /// <summary>Portable ordering and failure policy for resource-backed construction.</summary>
    public static class ConstructionTransactionKernel
    {
        public static ConstructionTransactionFailure Execute<TPlacement, THost>(
            bool hasPlacement,
            bool placementIsValid,
            TPlacement placement,
            int transactionId,
            ref THost host)
            where THost : IConstructionTransactionHost<TPlacement>
        {
            if (!hasPlacement)
                return ConstructionTransactionFailure.MissingPlacement;

            if (!placementIsValid)
                return ConstructionTransactionFailure.BlockedPlacement;

            if (!host.Validate(placement))
                return ConstructionTransactionFailure.InvalidPlacement;

            host.GetCosts(placement, out int creditsCost, out int materialsCost);
            ConstructionMutationResult reserveResult = host.TryReserve(
                transactionId,
                creditsCost < 0 ? 0 : creditsCost,
                materialsCost < 0 ? 0 : materialsCost);
            if (reserveResult != ConstructionMutationResult.Applied)
                return ToFailure(reserveResult);

            ConstructionCommitResult commit = host.Commit(placement);
            if (!commit.FullyCommitted)
            {
                if (commit.PlacementCommitted)
                {
                    return host.TryFinalize(transactionId) == ConstructionMutationResult.Applied
                        ? ConstructionTransactionFailure.RegistrationFailed
                        : ConstructionTransactionFailure.TransactionRejected;
                }

                return host.TryRollback(transactionId) == ConstructionMutationResult.Applied
                    ? ConstructionTransactionFailure.RegistrationFailed
                    : ConstructionTransactionFailure.TransactionRejected;
            }

            return host.TryFinalize(transactionId) == ConstructionMutationResult.Applied
                ? ConstructionTransactionFailure.None
                : ConstructionTransactionFailure.TransactionRejected;
        }

        private static ConstructionTransactionFailure ToFailure(ConstructionMutationResult result)
        {
            switch (result)
            {
                case ConstructionMutationResult.InsufficientCredits:
                    return ConstructionTransactionFailure.InsufficientCredits;
                case ConstructionMutationResult.InsufficientMaterials:
                    return ConstructionTransactionFailure.InsufficientMaterials;
                case ConstructionMutationResult.InsufficientCreditsAndMaterials:
                    return ConstructionTransactionFailure.InsufficientCreditsAndMaterials;
                case ConstructionMutationResult.DuplicateTransaction:
                    return ConstructionTransactionFailure.DuplicateTransaction;
                default:
                    return ConstructionTransactionFailure.TransactionRejected;
            }
        }
    }
}
