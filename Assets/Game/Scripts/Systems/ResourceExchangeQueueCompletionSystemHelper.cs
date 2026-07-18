using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    internal static class ResourceExchangeQueueCompletionSystemHelper
    {
        internal static bool TryCompleteQueueItem(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            in ResourceExchangeQueueComponent source,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            out ResourceExchangeQueueComponent completed)
        {
            return TryCompleteQueueItem(
                ref economy,
                ref materials,
                ref wallet,
                source,
                results,
                economyEvents,
                default,
                false,
                default,
                false,
                default,
                false,
                default,
                default,
                false,
                out completed);
        }

        internal static bool TryCompleteQueueItem(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            in ResourceExchangeQueueComponent source,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts,
            bool emitDeltaFlyouts,
            out ResourceExchangeQueueComponent completed)
        {
            return TryCompleteQueueItem(
                ref economy,
                ref materials,
                ref wallet,
                source,
                results,
                economyEvents,
                deltaFlyouts,
                emitDeltaFlyouts,
                default,
                false,
                default,
                false,
                default,
                default,
                false,
                out completed);
        }

        internal static bool TryCompleteQueueItem(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            in ResourceExchangeQueueComponent source,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts,
            bool emitDeltaFlyouts,
            DynamicBuffer<ResourceExchangeToastComponent> toasts,
            bool emitToasts,
            out ResourceExchangeQueueComponent completed)
        {
            return TryCompleteQueueItem(
                ref economy,
                ref materials,
                ref wallet,
                source,
                results,
                economyEvents,
                deltaFlyouts,
                emitDeltaFlyouts,
                toasts,
                emitToasts,
                default,
                false,
                default,
                default,
                false,
                out completed);
        }

        internal static bool TryCompleteQueueItem(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            in ResourceExchangeQueueComponent source,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts,
            bool emitDeltaFlyouts,
            DynamicBuffer<ResourceExchangeToastComponent> toasts,
            bool emitToasts,
            DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> ariaAnnouncements,
            bool emitAriaAnnouncements,
            out ResourceExchangeQueueComponent completed)
        {
            return TryCompleteQueueItem(
                ref economy,
                ref materials,
                ref wallet,
                source,
                results,
                economyEvents,
                deltaFlyouts,
                emitDeltaFlyouts,
                toasts,
                emitToasts,
                ariaAnnouncements,
                emitAriaAnnouncements,
                default,
                default,
                false,
                out completed);
        }

        internal static bool TryCompleteQueueItem(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            in ResourceExchangeQueueComponent source,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage,
            out ResourceExchangeQueueComponent completed)
        {
            return TryCompleteQueueItem(
                ref economy,
                ref materials,
                ref wallet,
                source,
                results,
                economyEvents,
                default,
                false,
                default,
                false,
                default,
                false,
                entityManager,
                physicalReservations,
                usePhysicalStorage,
                out completed);
        }

        private static bool TryCompleteQueueItem(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            in ResourceExchangeQueueComponent source,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts,
            bool emitDeltaFlyouts,
            DynamicBuffer<ResourceExchangeToastComponent> toasts,
            bool emitToasts,
            DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> ariaAnnouncements,
            bool emitAriaAnnouncements,
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage,
            out ResourceExchangeQueueComponent completed)
        {
            bool stateChanged = false;
            CompleteQueueItem(
                ref economy,
                ref materials,
                ref wallet,
                source,
                ref stateChanged,
                results,
                economyEvents,
                deltaFlyouts,
                emitDeltaFlyouts,
                toasts,
                emitToasts,
                ariaAnnouncements,
                emitAriaAnnouncements,
                entityManager,
                physicalReservations,
                usePhysicalStorage,
                out completed);
            return completed.State == ResourceExchangeQueueState.Completed;
        }

        internal static void CompleteQueueItem(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            in ResourceExchangeQueueComponent source,
            ref bool stateChanged,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts,
            bool emitDeltaFlyouts,
            DynamicBuffer<ResourceExchangeToastComponent> toasts,
            bool emitToasts,
            DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> ariaAnnouncements,
            bool emitAriaAnnouncements,
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage,
            out ResourceExchangeQueueComponent completed)
        {
            completed = source;
            ResourceExchangeReason storageReason = ValidateOutputStorage(
                economy,
                materials,
                wallet,
                completed,
                entityManager,
                physicalReservations,
                usePhysicalStorage);
            if (storageReason != ResourceExchangeReason.None)
            {
                completed.State = ResourceExchangeQueueState.Blocked;
                completed.StateReason = storageReason;
                completed.Version++;
                stateChanged = true;
                ResourceExchangeResultComponent blockedResult =
                    CreateResult(completed, ResourceExchangeResultKind.QueueBlocked, 0, storageReason);
                results.Add(blockedResult);
                economyEvents.Add(new ResourceExchangeEconomyEventComponent
                {
                    QueueItemId = completed.QueueItemId,
                    FactionId = completed.FactionId,
                    ResultKind = ResourceExchangeResultKind.QueueBlocked,
                    ResourceKind = completed.OutputResource,
                    Amount = 0,
                    RecipeId = completed.RecipeId
                });
                ResourceExchangeToastTextUtility.TryAppendToast(toasts, emitToasts, blockedResult);
                ResourceExchangeAriaTextUtility.TryAppendAnnouncement(
                    ariaAnnouncements,
                    emitAriaAnnouncements,
                    blockedResult);
                return;
            }

            bool physicalResource = usePhysicalStorage &&
                                    (ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(
                                         completed.InputResource) ||
                                     ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(
                                         completed.OutputResource));
            if (physicalResource &&
                !ResourceExchangePhysicalStorageUtilitySystemHelper.TryCompleteQueueItem(
                    entityManager,
                    physicalReservations,
                    completed,
                    out ResourceExchangeReason physicalReason))
            {
                completed.State = ResourceExchangeQueueState.Blocked;
                completed.StateReason = physicalReason;
                completed.Version++;
                stateChanged = true;
                return;
            }

            if (!usePhysicalStorage ||
                !ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(completed.OutputResource))
            {
                ResourceExchangeResourceUtilitySystemHelper.TryGrantImport(
                    ref economy,
                    ref materials,
                    ref wallet,
                    completed.OutputResource,
                    completed.OutputAmount);
            }
            completed.OutputApplied = 1;
            completed.ReservedInputAmount = 0;
            completed.RemainingSeconds = 0f;
            completed.State = ResourceExchangeQueueState.Completed;
            completed.StateReason = ResourceExchangeReason.None;
            completed.Version++;
            stateChanged = true;

            economyEvents.Add(new ResourceExchangeEconomyEventComponent
            {
                QueueItemId = completed.QueueItemId,
                FactionId = completed.FactionId,
                ResultKind = ResourceExchangeResultKind.QueueCompleted,
                ResourceKind = completed.OutputResource,
                Amount = completed.OutputAmount,
                RecipeId = completed.RecipeId
            });
            EmitDeltaFlyout(
                deltaFlyouts,
                emitDeltaFlyouts,
                completed.QueueItemId,
                completed.FactionId,
                ResourceExchangeDeltaFlyoutKind.OutputGranted,
                ResourceExchangeResultKind.QueueCompleted,
                completed.OutputResource,
                completed.OutputAmount,
                completed.RecipeId);
            ResourceExchangeResultComponent completedResult =
                CreateResult(completed, ResourceExchangeResultKind.QueueCompleted, 1, ResourceExchangeReason.None);
            results.Add(completedResult);
            ResourceExchangeToastTextUtility.TryAppendToast(toasts, emitToasts, completedResult);
            ResourceExchangeAriaTextUtility.TryAppendAnnouncement(
                ariaAnnouncements,
                emitAriaAnnouncements,
                completedResult);
        }

        internal static ResourceExchangeReason ValidateOutputStorage(
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials,
            in ResourceExchangeWalletComponent wallet,
            in ResourceExchangeQueueComponent item,
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage)
        {
            bool physicalResource =
                ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(item.InputResource) ||
                ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(item.OutputResource);
            if (physicalResource)
            {
                if (!usePhysicalStorage)
                    return ResourceExchangeReason.StorageMissing;

                return ResourceExchangePhysicalStorageUtilitySystemHelper.ValidateCompletion(
                    entityManager,
                    physicalReservations,
                    item);
            }

            int capacity = ResourceExchangeResourceUtilitySystemHelper.GetCapacity(
                materials,
                wallet,
                item.OutputResource);
            if (capacity <= 0)
                return ResourceExchangeReason.StorageMissing;

            int current = ResourceExchangeResourceUtilitySystemHelper.GetAmount(
                economy,
                materials,
                wallet,
                item.OutputResource);
            return current >= 0 && item.OutputAmount >= 0 && item.OutputAmount <= capacity - current
                ? ResourceExchangeReason.None
                : ResourceExchangeReason.StorageFull;
        }

        private static void EmitDeltaFlyout(
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts,
            bool emitDeltaFlyouts,
            int queueItemId,
            byte factionId,
            ResourceExchangeDeltaFlyoutKind flyoutKind,
            ResourceExchangeResultKind resultKind,
            ResourceExchangeResourceKind resourceKind,
            int amount,
            FixedString128Bytes recipeId)
        {
            if (!emitDeltaFlyouts || amount == 0)
                return;

            deltaFlyouts.Add(new ResourceExchangeDeltaFlyoutComponent
            {
                SequenceId = deltaFlyouts.Length + 1,
                QueueItemId = queueItemId,
                FactionId = factionId,
                FlyoutKind = flyoutKind,
                ResultKind = resultKind,
                ResourceKind = resourceKind,
                Amount = amount,
                RecipeId = recipeId
            });
        }

        private static ResourceExchangeResultComponent CreateResult(
            in ResourceExchangeQueueComponent item,
            ResourceExchangeResultKind resultKind,
            byte accepted,
            ResourceExchangeReason reason)
        {
            return new ResourceExchangeResultComponent
            {
                QueueItemId = item.QueueItemId,
                FactionId = item.FactionId,
                ResultKind = resultKind,
                Accepted = accepted,
                Reason = reason,
                RecipeId = item.RecipeId,
                InputResource = item.InputResource,
                OutputResource = item.OutputResource,
                InputAmount = item.InputAmount,
                OutputAmount = item.OutputAmount
            };
        }
    }
}
