using Game.Components;
using Game.Tactical.Contracts;

namespace Game.Runtime
{
    internal static class BuildingPlacementCommandResultMapper
    {
        public static byte ToConfirmFailureResultCode(
            BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason reason)
        {
            return reason switch
            {
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.MissingActivePlacement =>
                    BuildingUiPlacementCommandResultElement.MissingActivePlacement,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.BlockedPlacement =>
                    BuildingUiPlacementCommandResultElement.BlockedPlacement,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InvalidPlacement =>
                    BuildingUiPlacementCommandResultElement.InvalidPlacement,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientCredits =>
                    BuildingUiPlacementCommandResultElement.NotEnoughMoney,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientMaterials =>
                    BuildingUiPlacementCommandResultElement.InsufficientMaterials,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientCreditsAndMaterials =>
                    BuildingUiPlacementCommandResultElement.InsufficientCreditsAndMaterials,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.DuplicateTransaction =>
                    BuildingUiPlacementCommandResultElement.DuplicateTransaction,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.RegistrationFailed =>
                    BuildingUiPlacementCommandResultElement.RegistrationFailed,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.TransactionRejected =>
                    BuildingUiPlacementCommandResultElement.TransactionRejected,
                _ => BuildingUiPlacementCommandResultElement.Rejected
            };
        }

        public static TacticalCommandReasonCode ToReasonCode(byte resultCode)
        {
            return resultCode switch
            {
                BuildingUiPlacementCommandResultElement.Completed => TacticalCommandReasonCode.None,
                BuildingUiPlacementCommandResultElement.BlockedPlacement => TacticalCommandReasonCode.TargetBlocked,
                BuildingUiPlacementCommandResultElement.InvalidPlacement => TacticalCommandReasonCode.TargetUnreachable,
                BuildingUiPlacementCommandResultElement.NotEnoughMoney => TacticalCommandReasonCode.InsufficientResources,
                BuildingUiPlacementCommandResultElement.InsufficientMaterials => TacticalCommandReasonCode.InsufficientResources,
                BuildingUiPlacementCommandResultElement.InsufficientCreditsAndMaterials => TacticalCommandReasonCode.InsufficientResources,
                BuildingUiPlacementCommandResultElement.MissingActivePlacement => TacticalCommandReasonCode.BuildUnavailable,
                BuildingUiPlacementCommandResultElement.MissingConfig => TacticalCommandReasonCode.BuildUnavailable,
                _ => TacticalCommandReasonCode.CommandUnavailable
            };
        }
    }
}
