using Game.Components;
using Game.Simulation;
using UnityEngine;

namespace Game.Runtime
{
    using ConfirmContext = BuildingPlacementLifecycleCompositionSystemHelper.ConfirmContext;
    using ConfirmFailureReason = BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason;
    using PlacementState = BuildingPlacementLifecycleCompositionSystemHelper.PlacementState;

    internal sealed class BuildingPlacementConstructionTransaction
    {
        private int nextTransactionId;

        public bool Confirm(
            PlacementState placement,
            ConfirmContext context,
            out ConfirmFailureReason failureReason)
        {
            return Confirm(placement, NextTransactionId(), context, out failureReason);
        }

        public bool Confirm(
            PlacementState placement,
            int transactionId,
            ConfirmContext context,
            out ConfirmFailureReason failureReason)
        {
            return Execute(placement, transactionId, context, out failureReason);
        }

        private int NextTransactionId()
        {
            if (nextTransactionId == int.MaxValue)
                nextTransactionId = 0;
            return ++nextTransactionId;
        }

        private static bool Execute(
            PlacementState placement,
            int transactionId,
            ConfirmContext context,
            out ConfirmFailureReason failureReason)
        {
            var host = new PlayerConstructionTransactionHost(placement, transactionId, context);
            ConstructionTransactionFailure result = ConstructionTransactionKernel.Execute(
                hasPlacement: placement != null,
                placementIsValid: placement != null && placement.IsValid,
                placement: placement,
                transactionId: transactionId,
                ref host);
            failureReason = ToFailureReason(result);
            return result == ConstructionTransactionFailure.None;
        }

        private struct PlayerConstructionTransactionHost : IConstructionTransactionHost<PlacementState>
        {
            private readonly PlacementState _placement;
            private readonly int _transactionId;
            private readonly ConfirmContext _context;

            public PlayerConstructionTransactionHost(PlacementState placement, int transactionId, ConfirmContext context)
            {
                _placement = placement;
                _transactionId = transactionId;
                _context = context;
            }

            public bool Validate(PlacementState placement) =>
                _context.ValidateConfirm == null || _context.ValidateConfirm(placement);

            public void GetCosts(PlacementState placement, out int creditsCost, out int materialsCost)
            {
                creditsCost = Mathf.Max(0, placement.Definition?.CreditsCost ?? 0);
                materialsCost = Mathf.Max(0, placement.Definition?.MaterialsCost ?? 0);
            }

            public ConstructionMutationResult TryReserve(int transactionId, int creditsCost, int materialsCost)
            {
                if (_context.TryReserveCost == null)
                    return ConstructionMutationResult.Rejected;

                ConstructionMutationResult result = ToKernelResult(
                    _context.TryReserveCost(transactionId, creditsCost, materialsCost));
                if (result == ConstructionMutationResult.Applied && SkirmishConstructionLedger.Observer != null)
                {
                    if (_context.TryReserveCost.Target is BuildingConstructionResourceTransactionSystemHelper owner)
                        owner.ObservePlacement(_placement, transactionId);
                    else
                        SkirmishConstructionLedger.UnsupportedPlacement();
                }

                return result;
            }

            public ConstructionCommitResult Commit(PlacementState placement)
            {
                // Commit the pose displayed and validated above, including the latest road alignment.
                placement.CommittedOriginCell = placement.OriginCell;
                BuildingPlacementCommitCompositionSystemHelper.CommitOutcome outcome =
                    _context.CommitPlacement != null ? _context.CommitPlacement(placement) : default;
                if (SkirmishConstructionLedger.Observer != null)
                {
                    if (_context.TryReserveCost?.Target is BuildingConstructionResourceTransactionSystemHelper owner)
                        owner.ObservePlacement(placement, _transactionId, outcome);
                    else
                        SkirmishConstructionLedger.UnsupportedPlacement();
                }

                return new ConstructionCommitResult(outcome.CommittedInstanceCount, outcome.ExpectedInstanceCount);
            }

            public ConstructionMutationResult TryFinalize(int transactionId) =>
                _context.FinalizeCost == null
                    ? ConstructionMutationResult.Rejected
                    : ToKernelResult(_context.FinalizeCost(transactionId));

            public ConstructionMutationResult TryRollback(int transactionId) =>
                _context.RollbackCost == null
                    ? ConstructionMutationResult.Rejected
                    : ToKernelResult(_context.RollbackCost(transactionId));
        }

        private static ConstructionMutationResult ToKernelResult(FactionConstructionResourceMutationResult result) =>
            result switch
            {
                FactionConstructionResourceMutationResult.Applied => ConstructionMutationResult.Applied,
                FactionConstructionResourceMutationResult.InsufficientCredits => ConstructionMutationResult.InsufficientCredits,
                FactionConstructionResourceMutationResult.InsufficientMaterials => ConstructionMutationResult.InsufficientMaterials,
                FactionConstructionResourceMutationResult.InsufficientCreditsAndMaterials => ConstructionMutationResult.InsufficientCreditsAndMaterials,
                FactionConstructionResourceMutationResult.DuplicateTransaction => ConstructionMutationResult.DuplicateTransaction,
                _ => ConstructionMutationResult.Rejected
            };

        private static ConfirmFailureReason ToFailureReason(ConstructionTransactionFailure result) =>
            result switch
            {
                ConstructionTransactionFailure.None => ConfirmFailureReason.None,
                ConstructionTransactionFailure.MissingPlacement => ConfirmFailureReason.MissingActivePlacement,
                ConstructionTransactionFailure.BlockedPlacement => ConfirmFailureReason.BlockedPlacement,
                ConstructionTransactionFailure.InvalidPlacement => ConfirmFailureReason.InvalidPlacement,
                ConstructionTransactionFailure.InsufficientCredits => ConfirmFailureReason.InsufficientCredits,
                ConstructionTransactionFailure.InsufficientMaterials => ConfirmFailureReason.InsufficientMaterials,
                ConstructionTransactionFailure.InsufficientCreditsAndMaterials => ConfirmFailureReason.InsufficientCreditsAndMaterials,
                ConstructionTransactionFailure.DuplicateTransaction => ConfirmFailureReason.DuplicateTransaction,
                ConstructionTransactionFailure.RegistrationFailed => ConfirmFailureReason.RegistrationFailed,
                _ => ConfirmFailureReason.TransactionRejected
            };
    }
}
