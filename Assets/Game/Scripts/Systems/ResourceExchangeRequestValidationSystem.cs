using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct ResourceExchangeRequestValidationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float elapsedSeconds = (float)SystemAPI.Time.ElapsedTime;
            foreach (var (
                         requestQueue,
                         enabled,
                         wallet,
                         summary,
                         recipes,
                         requests,
                         queue,
                         results,
                         economyEvents)
                     in SystemAPI.Query<
                         RefRW<ResourceExchangeRequestQueueComponent>,
                         RefRO<ResourceExchangeEnabledComponent>,
                         RefRW<ResourceExchangeWalletComponent>,
                         RefRW<ResourceExchangeSummaryComponent>,
                         DynamicBuffer<ResourceExchangeRecipeComponent>,
                         DynamicBuffer<ResourceExchangeRequestComponent>,
                         DynamicBuffer<ResourceExchangeQueueComponent>,
                         DynamicBuffer<ResourceExchangeResultComponent>,
                         DynamicBuffer<ResourceExchangeEconomyEventComponent>>())
            {
                ProcessRequests(
                    ref requestQueue.ValueRW,
                    enabled.ValueRO,
                    ref wallet.ValueRW,
                    ref summary.ValueRW,
                    recipes,
                    requests,
                    queue,
                    results,
                    economyEvents,
                    elapsedSeconds);
            }
        }

        public static int EnqueueStartRequest(
            EntityManager em,
            Entity exchangeEntity,
            FixedString128Bytes recipeId,
            int inputAmount,
            byte factionId,
            int frameCount)
        {
            ResourceExchangeRequestQueueComponent requestQueue =
                em.GetComponentData<ResourceExchangeRequestQueueComponent>(exchangeEntity);
            requestQueue.LastRequestId++;
            em.SetComponentData(exchangeEntity, requestQueue);
            em.GetBuffer<ResourceExchangeRequestComponent>(exchangeEntity).Add(new ResourceExchangeRequestComponent
            {
                RequestId = requestQueue.LastRequestId,
                RequestKind = ResourceExchangeRequestKind.Start,
                FactionId = factionId,
                RecipeId = recipeId,
                InputAmount = inputAmount,
                FrameCount = frameCount
            });

            return requestQueue.LastRequestId;
        }

        public static int EnqueueCancelRequest(
            EntityManager em,
            Entity exchangeEntity,
            int queueItemId,
            byte factionId,
            int frameCount)
        {
            ResourceExchangeRequestQueueComponent requestQueue =
                em.GetComponentData<ResourceExchangeRequestQueueComponent>(exchangeEntity);
            requestQueue.LastRequestId++;
            em.SetComponentData(exchangeEntity, requestQueue);
            em.GetBuffer<ResourceExchangeRequestComponent>(exchangeEntity).Add(new ResourceExchangeRequestComponent
            {
                RequestId = requestQueue.LastRequestId,
                RequestKind = ResourceExchangeRequestKind.Cancel,
                FactionId = factionId,
                QueueItemId = queueItemId,
                FrameCount = frameCount
            });

            return requestQueue.LastRequestId;
        }

        public static int EnqueueMissionEndRequest(
            EntityManager em,
            Entity exchangeEntity,
            byte factionId,
            int frameCount)
        {
            ResourceExchangeRequestQueueComponent requestQueue =
                em.GetComponentData<ResourceExchangeRequestQueueComponent>(exchangeEntity);
            requestQueue.LastRequestId++;
            em.SetComponentData(exchangeEntity, requestQueue);
            em.GetBuffer<ResourceExchangeRequestComponent>(exchangeEntity).Add(new ResourceExchangeRequestComponent
            {
                RequestId = requestQueue.LastRequestId,
                RequestKind = ResourceExchangeRequestKind.MissionEnd,
                FactionId = factionId,
                FrameCount = frameCount
            });

            return requestQueue.LastRequestId;
        }

        public static bool TryGetResult(
            EntityManager em,
            Entity exchangeEntity,
            int requestId,
            out ResourceExchangeResultComponent result)
        {
            result = default;
            DynamicBuffer<ResourceExchangeResultComponent> results =
                em.GetBuffer<ResourceExchangeResultComponent>(exchangeEntity, true);
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].RequestId == requestId)
                {
                    result = results[i];
                    return true;
                }
            }

            return false;
        }

        public static void ProcessRequests(
            ref ResourceExchangeRequestQueueComponent requestQueue,
            in ResourceExchangeEnabledComponent enabled,
            ref ResourceExchangeWalletComponent wallet,
            ref ResourceExchangeSummaryComponent summary,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeRequestComponent> requests,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            float elapsedSeconds)
        {
            if (requests.Length == 0)
                return;

            results.Clear();
            for (int i = 0; i < requests.Length; i++)
            {
                ResourceExchangeRequestComponent request = requests[i];
                ResourceExchangeResultComponent result;
                switch (request.RequestKind)
                {
                    case ResourceExchangeRequestKind.Start:
                        result = ProcessStartRequest(
                            ref requestQueue,
                            enabled,
                            ref wallet,
                            recipes,
                            queue,
                            economyEvents,
                            request,
                            elapsedSeconds);
                        break;
                    case ResourceExchangeRequestKind.Cancel:
                        result = ProcessCancelRequest(
                            enabled,
                            ref wallet,
                            queue,
                            economyEvents,
                            request,
                            ResourceExchangeReason.None);
                        break;
                    case ResourceExchangeRequestKind.MissionEnd:
                        result = ProcessMissionEndRequest(
                            enabled,
                            ref wallet,
                            queue,
                            economyEvents,
                            request);
                        break;
                    default:
                        result = Rejected(request, default, ResourceExchangeReason.InvalidRecipe);
                        break;
                }

                results.Add(result);
                ApplySummary(ref summary, enabled, queue, result);
            }

            requests.Clear();
        }

        private static ResourceExchangeResultComponent ProcessStartRequest(
            ref ResourceExchangeRequestQueueComponent requestQueue,
            in ResourceExchangeEnabledComponent enabled,
            ref ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            in ResourceExchangeRequestComponent request,
            float elapsedSeconds)
        {
            if (request.RequestKind != ResourceExchangeRequestKind.Start)
                return Rejected(request, default, ResourceExchangeReason.InvalidRecipe);

            if (enabled.Enabled == 0)
                return Rejected(request, default, ResourceExchangeReason.ExchangeUnavailable);

            byte factionId = request.FactionId != 0 ? request.FactionId : enabled.FactionId;
            if (enabled.FactionId != 0 && factionId != enabled.FactionId)
                return Rejected(request, default, ResourceExchangeReason.ExchangeUnavailable);

            if (!TryFindRecipe(recipes, request.RecipeId, out ResourceExchangeRecipeComponent recipe))
                return Rejected(request, default, ResourceExchangeReason.RecipeLocked);

            ResourceExchangeReason recipeReason = ValidateRecipeAvailability(enabled, recipe);
            if (recipeReason != ResourceExchangeReason.None)
                return Rejected(request, recipe, recipeReason);

            ResourceExchangeReason amountReason = ValidateAmount(recipe, request.InputAmount);
            if (amountReason != ResourceExchangeReason.None)
                return Rejected(request, recipe, amountReason);

            int activeQueueCount = CountActiveQueueItems(queue, factionId);
            int maxQueueItems = math.max(0, enabled.MaxQueueItems);
            if (maxQueueItems <= 0 || activeQueueCount >= maxQueueItems)
                return Rejected(request, recipe, ResourceExchangeReason.QueueFull);

            int outputAmount = CalculateOutputAmount(recipe, request.InputAmount);
            ResourceExchangeReason storageReason = ValidateOutputStorage(wallet, recipe, outputAmount);
            if (storageReason != ResourceExchangeReason.None)
                return Rejected(request, recipe, storageReason);

            if (!TrySpendInput(ref wallet, recipe.InputResource, request.InputAmount, out ResourceExchangeReason spendReason))
                return Rejected(request, recipe, spendReason);

            requestQueue.LastQueueItemId = math.max(requestQueue.LastQueueItemId, MaxQueueItemId(queue)) + 1;
            ResourceExchangeQueueComponent queueItem = CreateQueueItem(
                requestQueue.LastQueueItemId,
                factionId,
                recipe,
                request.InputAmount,
                outputAmount,
                elapsedSeconds);
            queue.Add(queueItem);
            economyEvents.Add(new ResourceExchangeEconomyEventComponent
            {
                QueueItemId = queueItem.QueueItemId,
                FactionId = factionId,
                ResultKind = ResourceExchangeResultKind.QueueStarted,
                ResourceKind = recipe.InputResource,
                Amount = -request.InputAmount,
                RecipeId = recipe.RecipeId
            });

            return new ResourceExchangeResultComponent
            {
                RequestId = request.RequestId,
                QueueItemId = queueItem.QueueItemId,
                FactionId = factionId,
                ResultKind = ResourceExchangeResultKind.RequestAccepted,
                Accepted = 1,
                Reason = ResourceExchangeReason.None,
                RecipeId = recipe.RecipeId,
                InputResource = recipe.InputResource,
                OutputResource = recipe.OutputResource,
                InputAmount = request.InputAmount,
                OutputAmount = outputAmount
            };
        }

        private static ResourceExchangeResultComponent ProcessCancelRequest(
            in ResourceExchangeEnabledComponent enabled,
            ref ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            in ResourceExchangeRequestComponent request,
            ResourceExchangeReason stateReason)
        {
            byte factionId = request.FactionId != 0 ? request.FactionId : enabled.FactionId;
            if (enabled.FactionId != 0 && factionId != enabled.FactionId)
                return Rejected(request, default, ResourceExchangeReason.ExchangeUnavailable);

            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.QueueItemId != request.QueueItemId || item.FactionId != factionId)
                    continue;

                if (!CanCancel(item))
                    return Rejected(request, default, ResourceExchangeReason.CancelUnavailable);

                int refundAmount = CalculateRefundAmount(item);
                if (refundAmount > 0)
                {
                    AddResourceAmount(ref wallet, item.InputResource, refundAmount);
                    economyEvents.Add(new ResourceExchangeEconomyEventComponent
                    {
                        QueueItemId = item.QueueItemId,
                        FactionId = item.FactionId,
                        ResultKind = ResourceExchangeResultKind.QueueCancelled,
                        ResourceKind = item.InputResource,
                        Amount = refundAmount,
                        RecipeId = item.RecipeId
                    });
                }

                item.State = ResourceExchangeQueueState.Cancelled;
                item.StateReason = stateReason;
                item.RemainingSeconds = 0f;
                item.ReservedInputAmount = 0;
                item.Version++;
                queue[i] = item;

                return new ResourceExchangeResultComponent
                {
                    RequestId = request.RequestId,
                    QueueItemId = item.QueueItemId,
                    FactionId = factionId,
                    ResultKind = ResourceExchangeResultKind.QueueCancelled,
                    Accepted = 1,
                    Reason = stateReason,
                    RecipeId = item.RecipeId,
                    InputResource = item.InputResource,
                    OutputResource = item.OutputResource,
                    InputAmount = refundAmount,
                    OutputAmount = item.OutputAmount
                };
            }

            return Rejected(request, default, ResourceExchangeReason.CancelUnavailable);
        }

        private static ResourceExchangeResultComponent ProcessMissionEndRequest(
            in ResourceExchangeEnabledComponent enabled,
            ref ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            in ResourceExchangeRequestComponent request)
        {
            byte factionId = request.FactionId != 0 ? request.FactionId : enabled.FactionId;
            if (enabled.FactionId != 0 && factionId != enabled.FactionId)
                return Rejected(request, default, ResourceExchangeReason.ExchangeUnavailable);

            int cancelledCount = 0;
            int totalRefund = 0;
            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.FactionId != factionId || !CanCancel(item))
                    continue;

                int refundAmount = CalculateRefundAmount(item);
                if (refundAmount > 0)
                {
                    AddResourceAmount(ref wallet, item.InputResource, refundAmount);
                    economyEvents.Add(new ResourceExchangeEconomyEventComponent
                    {
                        QueueItemId = item.QueueItemId,
                        FactionId = item.FactionId,
                        ResultKind = ResourceExchangeResultKind.QueueCancelled,
                        ResourceKind = item.InputResource,
                        Amount = refundAmount,
                        RecipeId = item.RecipeId
                    });
                    totalRefund += refundAmount;
                }

                item.State = ResourceExchangeQueueState.Cancelled;
                item.StateReason = ResourceExchangeReason.MissionEnding;
                item.RemainingSeconds = 0f;
                item.ReservedInputAmount = 0;
                item.Version++;
                queue[i] = item;
                cancelledCount++;
            }

            return new ResourceExchangeResultComponent
            {
                RequestId = request.RequestId,
                FactionId = factionId,
                ResultKind = cancelledCount > 0
                    ? ResourceExchangeResultKind.QueueCancelled
                    : ResourceExchangeResultKind.RequestAccepted,
                Accepted = 1,
                Reason = ResourceExchangeReason.MissionEnding,
                InputAmount = totalRefund,
                OutputAmount = cancelledCount
            };
        }

        private static bool TryFindRecipe(
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            FixedString128Bytes recipeId,
            out ResourceExchangeRecipeComponent recipe)
        {
            for (int i = 0; i < recipes.Length; i++)
            {
                if (recipes[i].RecipeId.Equals(recipeId))
                {
                    recipe = recipes[i];
                    return true;
                }
            }

            recipe = default;
            return false;
        }

        private static ResourceExchangeReason ValidateRecipeAvailability(
            in ResourceExchangeEnabledComponent enabled,
            in ResourceExchangeRecipeComponent recipe)
        {
            if (recipe.Enabled == 0)
                return recipe.DisabledReason != ResourceExchangeReason.None
                    ? recipe.DisabledReason
                    : ResourceExchangeReason.RecipeLocked;

            if (recipe.MissionTag.Length > 0 && !recipe.MissionTag.Equals(enabled.ScenarioTag))
                return ResourceExchangeReason.RecipeLocked;

            return ResourceExchangeReason.None;
        }

        private static ResourceExchangeReason ValidateAmount(
            in ResourceExchangeRecipeComponent recipe,
            int inputAmount)
        {
            if (inputAmount < recipe.InputAmountMin)
                return ResourceExchangeReason.InputBelowMinimum;

            if (inputAmount > recipe.InputAmountMax)
                return ResourceExchangeReason.InputAboveMaximum;

            int step = math.max(1, recipe.InputStep);
            return ((inputAmount - recipe.InputAmountMin) % step) == 0
                ? ResourceExchangeReason.None
                : ResourceExchangeReason.InputStepInvalid;
        }

        private static int CountActiveQueueItems(
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            byte factionId)
        {
            int count = 0;
            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.FactionId != factionId)
                    continue;

                if (item.State == ResourceExchangeQueueState.Pending ||
                    item.State == ResourceExchangeQueueState.InProgress ||
                    item.State == ResourceExchangeQueueState.Completing ||
                    item.State == ResourceExchangeQueueState.Blocked)
                {
                    count++;
                }
            }

            return count;
        }

        private static int MaxQueueItemId(DynamicBuffer<ResourceExchangeQueueComponent> queue)
        {
            int maxId = 0;
            for (int i = 0; i < queue.Length; i++)
                maxId = math.max(maxId, queue[i].QueueItemId);
            return maxId;
        }

        private static int CalculateOutputAmount(
            in ResourceExchangeRecipeComponent recipe,
            int inputAmount)
        {
            float output = inputAmount * math.max(0f, recipe.OutputPerInput) * (1f - math.clamp(recipe.FeePercent, 0f, 0.95f));
            return math.max(0, (int)math.floor(output));
        }

        private static ResourceExchangeReason ValidateOutputStorage(
            in ResourceExchangeWalletComponent wallet,
            in ResourceExchangeRecipeComponent recipe,
            int outputAmount)
        {
            if (recipe.RequiresStorage == 0 || recipe.OutputResource == ResourceExchangeResourceKind.Credits)
                return ResourceExchangeReason.None;

            int capacity = GetCapacity(wallet, recipe.OutputResource);
            if (capacity <= 0)
                return ResourceExchangeReason.StorageMissing;

            int current = GetResourceAmount(wallet, recipe.OutputResource);
            return current + outputAmount <= capacity
                ? ResourceExchangeReason.None
                : ResourceExchangeReason.StorageFull;
        }

        private static bool TrySpendInput(
            ref ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind,
            int amount,
            out ResourceExchangeReason reason)
        {
            int current = GetResourceAmount(wallet, resourceKind);
            if (current < amount)
            {
                reason = InsufficientReason(resourceKind);
                return false;
            }

            SetResourceAmount(ref wallet, resourceKind, current - amount);
            wallet.Version++;
            reason = ResourceExchangeReason.None;
            return true;
        }

        private static ResourceExchangeQueueComponent CreateQueueItem(
            int queueItemId,
            byte factionId,
            in ResourceExchangeRecipeComponent recipe,
            int inputAmount,
            int outputAmount,
            float elapsedSeconds)
        {
            int completedSteps = math.max(0, (inputAmount - recipe.InputAmountMin) / math.max(1, recipe.InputStep));
            float duration = math.max(0f, recipe.DurationSecondsBase + completedSteps * recipe.DurationSecondsPerStep);
            return new ResourceExchangeQueueComponent
            {
                QueueItemId = queueItemId,
                FactionId = factionId,
                RecipeId = recipe.RecipeId,
                RouteType = recipe.RouteType,
                InputResource = recipe.InputResource,
                OutputResource = recipe.OutputResource,
                InputAmount = inputAmount,
                ReservedInputAmount = inputAmount,
                OutputAmount = outputAmount,
                State = ResourceExchangeQueueState.InProgress,
                StateReason = ResourceExchangeReason.None,
                StartTimeSeconds = elapsedSeconds,
                DurationSeconds = duration,
                RemainingSeconds = duration,
                Version = 1
            };
        }

        private static ResourceExchangeResultComponent Rejected(
            in ResourceExchangeRequestComponent request,
            in ResourceExchangeRecipeComponent recipe,
            ResourceExchangeReason reason)
        {
            return new ResourceExchangeResultComponent
            {
                RequestId = request.RequestId,
                QueueItemId = request.QueueItemId,
                FactionId = request.FactionId,
                ResultKind = ResourceExchangeResultKind.RequestRejected,
                Accepted = 0,
                Reason = reason,
                RecipeId = recipe.RecipeId,
                InputResource = recipe.InputResource,
                OutputResource = recipe.OutputResource,
                InputAmount = request.InputAmount
            };
        }

        private static void ApplySummary(
            ref ResourceExchangeSummaryComponent summary,
            in ResourceExchangeEnabledComponent enabled,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            in ResourceExchangeResultComponent result)
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
            summary.LastReason = result.Reason;
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

        private static ResourceExchangeReason InsufficientReason(ResourceExchangeResourceKind resourceKind)
        {
            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Credits:
                    return ResourceExchangeReason.InsufficientCredits;
                case ResourceExchangeResourceKind.Materials:
                    return ResourceExchangeReason.InsufficientMaterials;
                case ResourceExchangeResourceKind.Oil:
                    return ResourceExchangeReason.InsufficientOil;
                case ResourceExchangeResourceKind.Fuel:
                    return ResourceExchangeReason.InsufficientFuel;
                case ResourceExchangeResourceKind.RushTickets:
                    return ResourceExchangeReason.InsufficientRushTickets;
                default:
                    return ResourceExchangeReason.InvalidResource;
            }
        }

        private static bool CanCancel(in ResourceExchangeQueueComponent item)
        {
            if (item.OutputApplied != 0)
                return false;

            return item.State == ResourceExchangeQueueState.Pending ||
                   item.State == ResourceExchangeQueueState.InProgress ||
                   item.State == ResourceExchangeQueueState.Blocked;
        }

        private static int CalculateRefundAmount(in ResourceExchangeQueueComponent item)
        {
            return item.PresentationStarted == 0
                ? math.max(0, item.ReservedInputAmount)
                : 0;
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
