using Game.Components;
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
                    ResourceExchangeReason blockedReason =
                        ResourceExchangeQueueCompletionSystemHelper.ValidateOutputStorage(
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
                    ResourceExchangeQueueCompletionSystemHelper.CompleteQueueItem(
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
                    ResourceExchangeQueueCompletionSystemHelper.CompleteQueueItem(
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
            return ResourceExchangeQueueCompletionSystemHelper.TryCompleteQueueItem(
                ref economy,
                ref materials,
                ref wallet,
                source,
                results,
                economyEvents,
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
            return ResourceExchangeQueueCompletionSystemHelper.TryCompleteQueueItem(
                ref economy,
                ref materials,
                ref wallet,
                source,
                results,
                economyEvents,
                deltaFlyouts,
                emitDeltaFlyouts,
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
            return ResourceExchangeQueueCompletionSystemHelper.TryCompleteQueueItem(
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
            return ResourceExchangeQueueCompletionSystemHelper.TryCompleteQueueItem(
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
            return ResourceExchangeQueueCompletionSystemHelper.TryCompleteQueueItem(
                ref economy,
                ref materials,
                ref wallet,
                source,
                results,
                economyEvents,
                entityManager,
                physicalReservations,
                usePhysicalStorage,
                out completed);
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
