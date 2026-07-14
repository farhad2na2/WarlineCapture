using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct ResourceExchangeQueueTickSystem : ISystem
    {
        private EntityQuery _storageQuery;

        public void OnCreate(ref SystemState state)
        {
            _storageQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildingResourceStorageComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaSeconds = (float)SystemAPI.Time.DeltaTime;
            foreach (var (
                         enabled,
                         economy,
                         materials,
                         wallet,
                         summary,
                         queue,
                         results,
                         exchangeEntity)
                     in SystemAPI.Query<
                         RefRO<ResourceExchangeEnabledComponent>,
                         RefRW<FactionEconomy>,
                         RefRW<FactionTacticalMaterialsComponent>,
                         RefRW<ResourceExchangeWalletComponent>,
                         RefRW<ResourceExchangeSummaryComponent>,
                         DynamicBuffer<ResourceExchangeQueueComponent>,
                         DynamicBuffer<ResourceExchangeResultComponent>>()
                         .WithAll<ResourceExchangeEconomyEventComponent>()
                         .WithEntityAccess())
            {
                bool hasDeltaFlyouts =
                    state.EntityManager.HasBuffer<ResourceExchangeDeltaFlyoutComponent>(exchangeEntity);
                DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts = hasDeltaFlyouts
                    ? state.EntityManager.GetBuffer<ResourceExchangeDeltaFlyoutComponent>(exchangeEntity)
                    : default;
                bool hasToasts =
                    state.EntityManager.HasBuffer<ResourceExchangeToastComponent>(exchangeEntity);
                DynamicBuffer<ResourceExchangeToastComponent> toasts = hasToasts
                    ? state.EntityManager.GetBuffer<ResourceExchangeToastComponent>(exchangeEntity)
                    : default;
                bool hasAriaAnnouncements =
                    state.EntityManager.HasBuffer<ResourceExchangeAriaAnnouncementComponent>(exchangeEntity);
                DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> ariaAnnouncements = hasAriaAnnouncements
                    ? state.EntityManager.GetBuffer<ResourceExchangeAriaAnnouncementComponent>(exchangeEntity)
                    : default;
                DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents =
                    SystemAPI.GetBuffer<ResourceExchangeEconomyEventComponent>(exchangeEntity);
                bool usePhysicalStorage =
                    state.EntityManager.HasBuffer<ResourceExchangePhysicalReservationComponent>(exchangeEntity);
                DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations =
                    usePhysicalStorage
                        ? state.EntityManager.GetBuffer<ResourceExchangePhysicalReservationComponent>(exchangeEntity)
                        : default;
                TickQueue(
                    enabled.ValueRO,
                    ref economy.ValueRW,
                    ref materials.ValueRW,
                    ref wallet.ValueRW,
                    ref summary.ValueRW,
                    queue,
                    results,
                    economyEvents,
                    deltaFlyouts,
                    hasDeltaFlyouts,
                    toasts,
                    hasToasts,
                    ariaAnnouncements,
                    hasAriaAnnouncements,
                    deltaSeconds,
                    state.EntityManager,
                    physicalReservations,
                    usePhysicalStorage);
            }
        }

        public static void TickQueue(
            in ResourceExchangeEnabledComponent enabled,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ref ResourceExchangeSummaryComponent summary,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            float deltaSeconds)
        {
            TickQueue(
                enabled,
                ref economy,
                ref materials,
                ref wallet,
                ref summary,
                queue,
                results,
                economyEvents,
                default,
                false,
                default,
                false,
                default,
                false,
                deltaSeconds);
        }

        public static void TickQueue(
            in ResourceExchangeEnabledComponent enabled,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ref ResourceExchangeSummaryComponent summary,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts,
            bool emitDeltaFlyouts,
            float deltaSeconds)
        {
            TickQueue(
                enabled,
                ref economy,
                ref materials,
                ref wallet,
                ref summary,
                queue,
                results,
                economyEvents,
                deltaFlyouts,
                emitDeltaFlyouts,
                default,
                false,
                default,
                false,
                deltaSeconds);
        }

        public static void TickQueue(
            in ResourceExchangeEnabledComponent enabled,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ref ResourceExchangeSummaryComponent summary,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts,
            bool emitDeltaFlyouts,
            DynamicBuffer<ResourceExchangeToastComponent> toasts,
            bool emitToasts,
            float deltaSeconds)
        {
            TickQueue(
                enabled,
                ref economy,
                ref materials,
                ref wallet,
                ref summary,
                queue,
                results,
                economyEvents,
                deltaFlyouts,
                emitDeltaFlyouts,
                toasts,
                emitToasts,
                default,
                false,
                deltaSeconds);
        }

        public static void TickQueue(
            in ResourceExchangeEnabledComponent enabled,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ref ResourceExchangeSummaryComponent summary,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts,
            bool emitDeltaFlyouts,
            DynamicBuffer<ResourceExchangeToastComponent> toasts,
            bool emitToasts,
            DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> ariaAnnouncements,
            bool emitAriaAnnouncements,
            float deltaSeconds)
        {
            TickQueue(
                enabled,
                ref economy,
                ref materials,
                ref wallet,
                ref summary,
                queue,
                results,
                economyEvents,
                deltaFlyouts,
                emitDeltaFlyouts,
                toasts,
                emitToasts,
                ariaAnnouncements,
                emitAriaAnnouncements,
                deltaSeconds,
                default,
                default,
                false);
        }

        public static void TickQueue(
            in ResourceExchangeEnabledComponent enabled,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ref ResourceExchangeSummaryComponent summary,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            DynamicBuffer<ResourceExchangeDeltaFlyoutComponent> deltaFlyouts,
            bool emitDeltaFlyouts,
            DynamicBuffer<ResourceExchangeToastComponent> toasts,
            bool emitToasts,
            DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> ariaAnnouncements,
            bool emitAriaAnnouncements,
            float deltaSeconds,
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage)
        {
            if (queue.Length == 0)
                return;

            bool stateChanged = false;
            float safeDeltaSeconds = math.max(0f, deltaSeconds);
            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.OutputApplied != 0 || item.State == ResourceExchangeQueueState.Completed)
                    continue;

                if (item.State == ResourceExchangeQueueState.Blocked)
                {
                    ResourceExchangeReason blockedReason = ValidateOutputStorage(
                        economy,
                        materials,
                        wallet,
                        item,
                        entityManager,
                        physicalReservations,
                        usePhysicalStorage);
                    if (blockedReason != ResourceExchangeReason.None)
                        continue;

                    item.State = ResourceExchangeQueueState.InProgress;
                    item.StateReason = ResourceExchangeReason.None;
                    item.Version++;
                    stateChanged = true;
                }

                if (item.State == ResourceExchangeQueueState.Completing)
                {
                    CompleteQueueItem(
                        ref economy,
                        ref materials,
                        ref wallet,
                        item,
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
                        out item);
                    queue[i] = item;
                    continue;
                }

                if (item.State != ResourceExchangeQueueState.InProgress)
                {
                    queue[i] = item;
                    continue;
                }

                item.RemainingSeconds = math.max(0f, item.RemainingSeconds - safeDeltaSeconds);
                if (item.RemainingSeconds <= 0f)
                {
                    CompleteQueueItem(
                        ref economy,
                        ref materials,
                        ref wallet,
                        item,
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
                        out item);
                }

                queue[i] = item;
            }

            if (stateChanged)
                ApplySummary(ref summary, enabled, queue);
        }

        public static bool TryCompleteQueueItem(
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
                out completed);
        }

        public static bool TryCompleteQueueItem(
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
                out completed);
        }

        public static bool TryCompleteQueueItem(
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
                out completed);
        }

        public static bool TryCompleteQueueItem(
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

        public static bool TryCompleteQueueItem(
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

        private static void CompleteQueueItem(
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

        private static ResourceExchangeReason ValidateOutputStorage(
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

            if (item.OutputResource == ResourceExchangeResourceKind.Credits)
                return ResourceExchangeReason.None;

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

        private static void ApplySummary(
            ref ResourceExchangeSummaryComponent summary,
            in ResourceExchangeEnabledComponent enabled,
            DynamicBuffer<ResourceExchangeQueueComponent> queue)
        {
            int activeCount = 0;
            int completedCount = 0;
            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.State == ResourceExchangeQueueState.Completed)
                    completedCount++;
                else if (item.State == ResourceExchangeQueueState.Pending ||
                         item.State == ResourceExchangeQueueState.InProgress ||
                         item.State == ResourceExchangeQueueState.Completing ||
                         item.State == ResourceExchangeQueueState.Blocked)
                    activeCount++;
            }

            summary.FactionId = enabled.FactionId;
            summary.Enabled = enabled.Enabled;
            summary.AllowRush = enabled.AllowRush;
            summary.AllowWorldPresentation = enabled.AllowWorldPresentation;
            summary.AllowAiExchange = enabled.AllowAiExchange;
            summary.QueueCount = queue.Length;
            summary.ActiveCount = activeCount;
            summary.CompletedCount = completedCount;
            summary.MaxQueueItems = enabled.MaxQueueItems;
            summary.LastReason = ResourceExchangeReason.None;
            summary.Version++;
        }

    }
}
