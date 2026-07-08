using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct ResourceExchangeQueueTickSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float deltaSeconds = (float)SystemAPI.Time.DeltaTime;
            foreach (var (
                         enabled,
                         wallet,
                         summary,
                         queue,
                         results,
                         economyEvents)
                     in SystemAPI.Query<
                         RefRO<ResourceExchangeEnabledComponent>,
                         RefRW<ResourceExchangeWalletComponent>,
                         RefRW<ResourceExchangeSummaryComponent>,
                         DynamicBuffer<ResourceExchangeQueueComponent>,
                         DynamicBuffer<ResourceExchangeResultComponent>,
                         DynamicBuffer<ResourceExchangeEconomyEventComponent>>())
            {
                TickQueue(
                    enabled.ValueRO,
                    ref wallet.ValueRW,
                    ref summary.ValueRW,
                    queue,
                    results,
                    economyEvents,
                    deltaSeconds);
            }
        }

        public static void TickQueue(
            in ResourceExchangeEnabledComponent enabled,
            ref ResourceExchangeWalletComponent wallet,
            ref ResourceExchangeSummaryComponent summary,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            float deltaSeconds)
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
                    ResourceExchangeReason blockedReason = ValidateOutputStorage(wallet, item);
                    if (blockedReason != ResourceExchangeReason.None)
                        continue;

                    item.State = ResourceExchangeQueueState.InProgress;
                    item.StateReason = ResourceExchangeReason.None;
                    item.Version++;
                    stateChanged = true;
                }

                if (item.State == ResourceExchangeQueueState.Completing)
                {
                    CompleteQueueItem(ref wallet, item, ref stateChanged, results, economyEvents, out item);
                    queue[i] = item;
                    continue;
                }

                if (item.State != ResourceExchangeQueueState.InProgress)
                {
                    queue[i] = item;
                    continue;
                }

                ResourceExchangeReason storageReason = ValidateOutputStorage(wallet, item);
                if (storageReason != ResourceExchangeReason.None)
                {
                    item.State = ResourceExchangeQueueState.Blocked;
                    item.StateReason = storageReason;
                    item.Version++;
                    queue[i] = item;
                    stateChanged = true;
                    results.Add(CreateResult(item, ResourceExchangeResultKind.QueueBlocked, 0, storageReason));
                    continue;
                }

                item.RemainingSeconds = math.max(0f, item.RemainingSeconds - safeDeltaSeconds);
                if (item.RemainingSeconds <= 0f)
                    CompleteQueueItem(ref wallet, item, ref stateChanged, results, economyEvents, out item);

                queue[i] = item;
            }

            if (stateChanged)
                ApplySummary(ref summary, enabled, queue);
        }

        private static void CompleteQueueItem(
            ref ResourceExchangeWalletComponent wallet,
            in ResourceExchangeQueueComponent source,
            ref bool stateChanged,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            out ResourceExchangeQueueComponent completed)
        {
            completed = source;
            ResourceExchangeReason storageReason = ValidateOutputStorage(wallet, completed);
            if (storageReason != ResourceExchangeReason.None)
            {
                completed.State = ResourceExchangeQueueState.Blocked;
                completed.StateReason = storageReason;
                completed.Version++;
                stateChanged = true;
                results.Add(CreateResult(completed, ResourceExchangeResultKind.QueueBlocked, 0, storageReason));
                return;
            }

            AddResourceAmount(ref wallet, completed.OutputResource, completed.OutputAmount);
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
            results.Add(CreateResult(completed, ResourceExchangeResultKind.QueueCompleted, 1, ResourceExchangeReason.None));
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
            in ResourceExchangeWalletComponent wallet,
            in ResourceExchangeQueueComponent item)
        {
            if (item.OutputResource == ResourceExchangeResourceKind.Credits)
                return ResourceExchangeReason.None;

            int capacity = GetCapacity(wallet, item.OutputResource);
            if (capacity <= 0)
                return ResourceExchangeReason.StorageMissing;

            int current = GetResourceAmount(wallet, item.OutputResource);
            return current + item.OutputAmount <= capacity
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
            summary.QueueCount = queue.Length;
            summary.ActiveCount = activeCount;
            summary.CompletedCount = completedCount;
            summary.MaxQueueItems = enabled.MaxQueueItems;
            summary.LastReason = ResourceExchangeReason.None;
            summary.Version++;
        }

        private static int GetResourceAmount(
            in ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind)
        {
            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Credits:
                    return wallet.Credits;
                case ResourceExchangeResourceKind.Materials:
                    return wallet.Materials;
                case ResourceExchangeResourceKind.Oil:
                    return wallet.Oil;
                case ResourceExchangeResourceKind.Fuel:
                    return wallet.Fuel;
                case ResourceExchangeResourceKind.RushTickets:
                    return wallet.RushTickets;
                default:
                    return 0;
            }
        }

        private static void SetResourceAmount(
            ref ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind,
            int amount)
        {
            amount = math.max(0, amount);
            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Credits:
                    wallet.Credits = amount;
                    break;
                case ResourceExchangeResourceKind.Materials:
                    wallet.Materials = amount;
                    break;
                case ResourceExchangeResourceKind.Oil:
                    wallet.Oil = amount;
                    break;
                case ResourceExchangeResourceKind.Fuel:
                    wallet.Fuel = amount;
                    break;
                case ResourceExchangeResourceKind.RushTickets:
                    wallet.RushTickets = amount;
                    break;
            }
        }

        private static int GetCapacity(
            in ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind)
        {
            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Materials:
                    return wallet.MaterialsCapacity;
                case ResourceExchangeResourceKind.Oil:
                    return wallet.OilCapacity;
                case ResourceExchangeResourceKind.Fuel:
                    return wallet.FuelCapacity;
                default:
                    return int.MaxValue;
            }
        }

        private static void AddResourceAmount(
            ref ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind,
            int amount)
        {
            if (amount <= 0)
                return;

            SetResourceAmount(ref wallet, resourceKind, GetResourceAmount(wallet, resourceKind) + amount);
            wallet.Version++;
        }
    }
}
