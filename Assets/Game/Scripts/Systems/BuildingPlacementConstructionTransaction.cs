using Game.Components;
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
            if (placement == null)
            {
                failureReason = ConfirmFailureReason.MissingActivePlacement;
                return false;
            }

            if (!placement.IsValid)
            {
                failureReason = ConfirmFailureReason.BlockedPlacement;
                return false;
            }

            if (context.ValidateConfirm != null && !context.ValidateConfirm(placement))
            {
                failureReason = ConfirmFailureReason.InvalidPlacement;
                return false;
            }

            int materialsCost = Mathf.Max(0, placement.Definition?.MaterialsCost ?? 0);
            if (context.TryReserveCost == null)
            {
                failureReason = ConfirmFailureReason.TransactionRejected;
                return false;
            }

            FactionConstructionResourceMutationResult reserveResult =
                context.TryReserveCost(transactionId, 0, materialsCost);
            if (reserveResult != FactionConstructionResourceMutationResult.Applied)
            {
                failureReason = ToFailureReason(reserveResult);
                return false;
            }

            placement.OriginCell = placement.CommittedOriginCell;
            BuildingPlacementCommitCompositionSystemHelper.CommitOutcome outcome =
                context.CommitPlacement != null ? context.CommitPlacement(placement) : default;
            if (!outcome.FullyCommitted)
            {
                if (outcome.PlacementCommitted)
                {
                    failureReason = TrySettle(context.FinalizeCost, transactionId)
                        ? ConfirmFailureReason.RegistrationFailed
                        : ConfirmFailureReason.TransactionRejected;
                    return false;
                }

                failureReason = TrySettle(context.RollbackCost, transactionId)
                    ? ConfirmFailureReason.RegistrationFailed
                    : ConfirmFailureReason.TransactionRejected;
                return false;
            }

            if (!TrySettle(context.FinalizeCost, transactionId))
            {
                failureReason = ConfirmFailureReason.TransactionRejected;
                return false;
            }

            failureReason = ConfirmFailureReason.None;
            return true;
        }

        private static bool TrySettle(
            BuildingPlacementLifecycleCompositionSystemHelper.SettleCostDelegate settle,
            int transactionId)
        {
            return settle != null &&
                   settle(transactionId) == FactionConstructionResourceMutationResult.Applied;
        }

        private static ConfirmFailureReason ToFailureReason(
            FactionConstructionResourceMutationResult result)
        {
            return result switch
            {
                FactionConstructionResourceMutationResult.InsufficientCredits =>
                    ConfirmFailureReason.InsufficientCredits,
                FactionConstructionResourceMutationResult.InsufficientMaterials =>
                    ConfirmFailureReason.InsufficientMaterials,
                FactionConstructionResourceMutationResult.InsufficientCreditsAndMaterials =>
                    ConfirmFailureReason.InsufficientCreditsAndMaterials,
                FactionConstructionResourceMutationResult.DuplicateTransaction =>
                    ConfirmFailureReason.DuplicateTransaction,
                _ => ConfirmFailureReason.TransactionRejected
            };
        }
    }
}
