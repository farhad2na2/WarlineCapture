using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using RequestQueue = Game.Runtime.ResourceExchangeRequestQueueSystemHelper;
using RushPolicy = Game.Runtime.ResourceExchangeRushPolicySystemHelper;

namespace Game.Runtime
{
    public partial struct ResourceExchangeRequestValidationSystem : ISystem
    {
        private EntityQuery _storageQuery;

        public void OnCreate(ref SystemState state)
        {
            _storageQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildingResourceStorageComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            float elapsedSeconds = (float)SystemAPI.Time.ElapsedTime;
            foreach (var (
                         requestQueue,
                         enabled,
                         economy,
                         materials,
                         wallet,
                         summary,
                         queue,
                         exchangeEntity)
                     in SystemAPI.Query<
                         RefRW<ResourceExchangeRequestQueueComponent>,
                         RefRO<ResourceExchangeEnabledComponent>,
                         RefRW<FactionEconomy>,
                         RefRW<FactionTacticalMaterialsComponent>,
                         RefRW<ResourceExchangeWalletComponent>,
                         RefRW<ResourceExchangeSummaryComponent>,
                         DynamicBuffer<ResourceExchangeQueueComponent>>()
                         .WithAll<ResourceExchangeRecipeComponent>()
                         .WithAll<ResourceExchangeRequestComponent>()
                         .WithAll<ResourceExchangeResultComponent>()
                         .WithAll<ResourceExchangeEconomyEventComponent>()
                         .WithEntityAccess())
            {
                DynamicBuffer<ResourceExchangeRequestComponent> requests =
                    SystemAPI.GetBuffer<ResourceExchangeRequestComponent>(exchangeEntity);
                if (requests.IsEmpty)
                    continue;

                DynamicBuffer<ResourceExchangeResultComponent> results =
                    SystemAPI.GetBuffer<ResourceExchangeResultComponent>(exchangeEntity);
                DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents =
                    SystemAPI.GetBuffer<ResourceExchangeEconomyEventComponent>(exchangeEntity);
                DynamicBuffer<ResourceExchangeRecipeComponent> recipes =
                    SystemAPI.GetBuffer<ResourceExchangeRecipeComponent>(exchangeEntity);
                bool usePhysicalStorage =
                    state.EntityManager.HasBuffer<ResourceExchangePhysicalReservationComponent>(exchangeEntity);
                DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations =
                    usePhysicalStorage
                        ? state.EntityManager.GetBuffer<ResourceExchangePhysicalReservationComponent>(exchangeEntity)
                        : default;

                ProcessRequests(
                    ref requestQueue.ValueRW,
                    enabled.ValueRO,
                    ref economy.ValueRW,
                    ref materials.ValueRW,
                    ref wallet.ValueRW,
                    ref summary.ValueRW,
                    recipes,
                    requests,
                    queue,
                    results,
                    economyEvents,
                    elapsedSeconds,
                    state.EntityManager,
                    _storageQuery,
                    physicalReservations,
                    usePhysicalStorage);

                if (!state.EntityManager.HasBuffer<ResourceExchangeToastComponent>(exchangeEntity))
                    continue;

                DynamicBuffer<ResourceExchangeToastComponent> toasts =
                    state.EntityManager.GetBuffer<ResourceExchangeToastComponent>(exchangeEntity);
                for (int i = 0; i < results.Length; i++)
                {
                    ResourceExchangeToastTextUtility.TryAppendToast(
                        toasts,
                        true,
                        results[i]);
                }
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
            return RequestQueue.Enqueue(
                em,
                exchangeEntity,
                ResourceExchangeRequestKind.Start,
                factionId,
                frameCount,
                recipeId,
                inputAmount);
        }

        public static int EnqueueCancelRequest(
            EntityManager em,
            Entity exchangeEntity,
            int queueItemId,
            byte factionId,
            int frameCount)
        {
            return RequestQueue.Enqueue(
                em,
                exchangeEntity,
                ResourceExchangeRequestKind.Cancel,
                factionId,
                frameCount,
                queueItemId: queueItemId);
        }

        public static int EnqueueRushRequest(
            EntityManager em,
            Entity exchangeEntity,
            int queueItemId,
            int rushTickets,
            byte factionId,
            int frameCount)
        {
            return RequestQueue.Enqueue(
                em,
                exchangeEntity,
                ResourceExchangeRequestKind.Rush,
                factionId,
                frameCount,
                queueItemId: queueItemId,
                rushTickets: rushTickets);
        }

        public static int EnqueueRushAllRequest(
            EntityManager em,
            Entity exchangeEntity,
            int rushTicketBudget,
            byte factionId,
            int frameCount)
        {
            return RequestQueue.Enqueue(
                em,
                exchangeEntity,
                ResourceExchangeRequestKind.RushAll,
                factionId,
                frameCount,
                rushTickets: rushTicketBudget);
        }

        public static int EnqueueClearCompletedRequest(
            EntityManager em,
            Entity exchangeEntity,
            byte factionId,
            int frameCount)
        {
            return RequestQueue.Enqueue(
                em,
                exchangeEntity,
                ResourceExchangeRequestKind.ClearCompleted,
                factionId,
                frameCount);
        }

        public static int EnqueueMissionEndRequest(
            EntityManager em,
            Entity exchangeEntity,
            byte factionId,
            int frameCount)
        {
            return RequestQueue.Enqueue(
                em,
                exchangeEntity,
                ResourceExchangeRequestKind.MissionEnd,
                factionId,
                frameCount);
        }

        public static bool TryGetResult(
            EntityManager em,
            Entity exchangeEntity,
            int requestId,
            out ResourceExchangeResultComponent result)
        {
            return RequestQueue.TryGetResult(
                em,
                exchangeEntity,
                requestId,
                out result);
        }

        public static void ProcessRequests(
            ref ResourceExchangeRequestQueueComponent requestQueue,
            in ResourceExchangeEnabledComponent enabled,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ref ResourceExchangeSummaryComponent summary,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeRequestComponent> requests,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            float elapsedSeconds)
        {
            ProcessRequests(
                ref requestQueue,
                enabled,
                ref economy,
                ref materials,
                ref wallet,
                ref summary,
                recipes,
                requests,
                queue,
                results,
                economyEvents,
                elapsedSeconds,
                default,
                default,
                default,
                false);
        }

        public static void ProcessRequests(
            ref ResourceExchangeRequestQueueComponent requestQueue,
            in ResourceExchangeEnabledComponent enabled,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ref ResourceExchangeSummaryComponent summary,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeRequestComponent> requests,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            float elapsedSeconds,
            EntityManager entityManager,
            EntityQuery storageQuery,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage)
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
                            ref economy,
                            ref materials,
                            ref wallet,
                            recipes,
                            queue,
                            economyEvents,
                            request,
                            elapsedSeconds,
                            entityManager,
                            storageQuery,
                            physicalReservations,
                            usePhysicalStorage);
                        break;
                    case ResourceExchangeRequestKind.Cancel:
                        result = ProcessCancelRequest(
                            enabled,
                            ref economy,
                            ref materials,
                            ref wallet,
                            queue,
                            economyEvents,
                            request,
                            ResourceExchangeReason.None,
                            entityManager,
                            physicalReservations,
                            usePhysicalStorage);
                        break;
                    case ResourceExchangeRequestKind.Rush:
                        result = ProcessRushRequest(
                            enabled,
                            ref economy,
                            ref materials,
                            ref wallet,
                            recipes,
                            queue,
                            results,
                            economyEvents,
                            request,
                            entityManager,
                            physicalReservations,
                            usePhysicalStorage);
                        break;
                    case ResourceExchangeRequestKind.RushAll:
                        result = ProcessRushAllRequest(
                            enabled,
                            ref economy,
                            ref materials,
                            ref wallet,
                            recipes,
                            queue,
                            results,
                            economyEvents,
                            request,
                            entityManager,
                            physicalReservations,
                            usePhysicalStorage);
                        break;
                    case ResourceExchangeRequestKind.ClearCompleted:
                        result = ProcessClearCompletedRequest(
                            enabled,
                            queue,
                            request);
                        break;
                    case ResourceExchangeRequestKind.MissionEnd:
                        result = ProcessMissionEndRequest(
                            enabled,
                            ref economy,
                            ref materials,
                            ref wallet,
                            queue,
                            economyEvents,
                            request,
                            entityManager,
                            physicalReservations,
                            usePhysicalStorage);
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
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            in ResourceExchangeRequestComponent request,
            float elapsedSeconds,
            EntityManager entityManager,
            EntityQuery storageQuery,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage)
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
            bool physicalInput = ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(
                recipe.InputResource);
            bool physicalOutput = ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(
                recipe.OutputResource);
            if ((physicalInput || physicalOutput) && !usePhysicalStorage)
                return Rejected(request, recipe, ResourceExchangeReason.StorageMissing);

            ResourceExchangeReason storageReason = physicalOutput
                ? ResourceExchangeReason.None
                : ValidateOutputStorage(economy, materials, wallet, recipe, outputAmount);
            if (storageReason != ResourceExchangeReason.None)
                return Rejected(request, recipe, storageReason);

            int queueItemId = math.max(requestQueue.LastQueueItemId, MaxQueueItemId(queue)) + 1;
            if ((physicalInput || physicalOutput) &&
                !ResourceExchangePhysicalStorageUtilitySystemHelper.TryReserveForQueue(
                    entityManager,
                    storageQuery,
                    physicalReservations,
                    queueItemId,
                    factionId,
                    recipe.InputResource,
                    request.InputAmount,
                    recipe.OutputResource,
                    outputAmount,
                    out ResourceExchangeReason physicalStorageReason))
            {
                return Rejected(request, recipe, physicalStorageReason);
            }

            if (!physicalInput &&
                !TrySpendInput(
                    ref economy,
                    ref materials,
                    ref wallet,
                    recipe.InputResource,
                    request.InputAmount,
                    out ResourceExchangeReason spendReason))
            {
                if (physicalOutput)
                {
                    ResourceExchangePhysicalStorageUtilitySystemHelper.CancelQueueItem(
                        entityManager,
                        physicalReservations,
                        queueItemId,
                        true);
                }

                return Rejected(request, recipe, spendReason);
            }

            requestQueue.LastQueueItemId = queueItemId;
            ResourceExchangeQueueComponent queueItem = CreateQueueItem(
                queueItemId,
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
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            in ResourceExchangeRequestComponent request,
            ResourceExchangeReason stateReason,
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage)
        {
            byte factionId = request.FactionId != 0 ? request.FactionId : enabled.FactionId;
            if (enabled.FactionId != 0 && factionId != enabled.FactionId)
                return Rejected(request, default, ResourceExchangeReason.ExchangeUnavailable);

            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.QueueItemId != request.QueueItemId || item.FactionId != factionId)
                    continue;

                if (!RequestQueue.CanCancel(item))
                    return Rejected(request, default, ResourceExchangeReason.CancelUnavailable);

                int refundAmount = RequestQueue.CalculateRefundAmount(item);
                bool physicalInput = usePhysicalStorage &&
                                     ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(
                                         item.InputResource);
                bool physicalOutput = usePhysicalStorage &&
                                      ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(
                                          item.OutputResource);
                if (physicalInput || physicalOutput)
                {
                    ResourceExchangePhysicalStorageUtilitySystemHelper.CancelQueueItem(
                        entityManager,
                        physicalReservations,
                        item.QueueItemId,
                        refundAmount > 0);
                }

                if (refundAmount > 0 && !physicalInput)
                {
                    ResourceExchangeResourceUtilitySystemHelper.TryRefundReservedInput(
                        ref economy,
                        ref materials,
                        ref wallet,
                        item.InputResource,
                        refundAmount);
                }

                economyEvents.Add(new ResourceExchangeEconomyEventComponent
                {
                    QueueItemId = item.QueueItemId,
                    FactionId = item.FactionId,
                    ResultKind = ResourceExchangeResultKind.QueueCancelled,
                    ResourceKind = item.InputResource,
                    Amount = refundAmount,
                    RecipeId = item.RecipeId
                });

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

        private static ResourceExchangeResultComponent ProcessRushRequest(
            in ResourceExchangeEnabledComponent enabled,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            in ResourceExchangeRequestComponent request,
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage)
        {
            ResourceExchangeReason gateReason =
                RushPolicy.ValidateGate(enabled, request, out byte factionId);
            if (gateReason != ResourceExchangeReason.None)
                return RushPolicy.Rejected(request, default, gateReason);

            if (request.RushTickets <= 0)
                return RushPolicy.Rejected(
                    request,
                    default,
                    ResourceExchangeReason.RushUnavailable);

            int queueIndex =
                RushPolicy.FindQueueItemIndex(
                    queue,
                    request.QueueItemId,
                    factionId);
            if (queueIndex < 0)
                return RushPolicy.Rejected(
                    request,
                    default,
                    ResourceExchangeReason.RushUnavailable);

            ResourceExchangeQueueComponent item = queue[queueIndex];
            ResourceExchangeReason itemReason = RushPolicy.ValidateItem(item);
            if (itemReason != ResourceExchangeReason.None)
                return RushPolicy.Rejected(request, item, itemReason);

            if (!TryFindRecipe(recipes, item.RecipeId, out ResourceExchangeRecipeComponent recipe))
                return RushPolicy.Rejected(
                    request,
                    item,
                    ResourceExchangeReason.RushUnavailable);

            ResourceExchangeReason recipeReason = RushPolicy.ValidateRecipe(recipe);
            if (recipeReason != ResourceExchangeReason.None)
                return RushPolicy.Rejected(request, item, recipeReason);

            int maxSpend = RushPolicy.CalculateTicketCapacity(item, recipe);
            if (request.RushTickets > maxSpend)
                return RushPolicy.Rejected(
                    request,
                    item,
                    ResourceExchangeReason.RushUnavailable);

            if (!TrySpendInput(
                    ref wallet,
                    ResourceExchangeResourceKind.RushTickets,
                    request.RushTickets,
                    out ResourceExchangeReason spendReason))
            {
                return RushPolicy.Rejected(request, item, spendReason);
            }

            RushPolicy.ApplyTickets(
                ref economy,
                ref materials,
                ref wallet,
                queue,
                results,
                economyEvents,
                queueIndex,
                item,
                recipe,
                request.RushTickets,
                entityManager,
                physicalReservations,
                usePhysicalStorage);

            item = queue[queueIndex];
            return RushPolicy.Accepted(request, item, request.RushTickets, 1);
        }

        private static ResourceExchangeResultComponent ProcessRushAllRequest(
            in ResourceExchangeEnabledComponent enabled,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            in ResourceExchangeRequestComponent request,
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage)
        {
            ResourceExchangeReason gateReason =
                RushPolicy.ValidateGate(enabled, request, out byte factionId);
            if (gateReason != ResourceExchangeReason.None)
                return RushPolicy.Rejected(request, default, gateReason);

            int requestedBudget = request.RushTickets > 0 ? request.RushTickets : wallet.RushTickets;
            if (requestedBudget <= 0)
                return RushPolicy.Rejected(
                    request,
                    default,
                    ResourceExchangeReason.InsufficientRushTickets);

            if (wallet.RushTickets < requestedBudget)
                return RushPolicy.Rejected(
                    request,
                    default,
                    ResourceExchangeReason.InsufficientRushTickets);

            int remainingBudget = requestedBudget;
            int totalSpent = 0;
            int affectedCount = 0;
            ResourceExchangeQueueComponent lastAffectedItem = default;
            for (int i = 0; i < queue.Length && remainingBudget > 0; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.FactionId != factionId ||
                    RushPolicy.ValidateItem(item) != ResourceExchangeReason.None)
                    continue;

                if (!TryFindRecipe(recipes, item.RecipeId, out ResourceExchangeRecipeComponent recipe) ||
                    RushPolicy.ValidateRecipe(recipe) != ResourceExchangeReason.None)
                {
                    continue;
                }

                int ticketCapacity =
                    RushPolicy.CalculateTicketCapacity(item, recipe);
                int ticketsNeeded =
                    RushPolicy.CalculateTicketsNeeded(item, recipe);
                int ticketsToSpend = math.min(remainingBudget, math.min(ticketCapacity, ticketsNeeded));
                if (ticketsToSpend <= 0)
                    continue;

                if (!TrySpendInput(
                        ref wallet,
                        ResourceExchangeResourceKind.RushTickets,
                        ticketsToSpend,
                        out _))
                {
                    break;
                }

                RushPolicy.ApplyTickets(
                    ref economy,
                    ref materials,
                    ref wallet,
                    queue,
                    results,
                    economyEvents,
                    i,
                    item,
                    recipe,
                    ticketsToSpend,
                    entityManager,
                    physicalReservations,
                    usePhysicalStorage);
                lastAffectedItem = queue[i];
                remainingBudget -= ticketsToSpend;
                totalSpent += ticketsToSpend;
                affectedCount++;
            }

            return totalSpent > 0
                ? RushPolicy.Accepted(
                    request,
                    lastAffectedItem,
                    totalSpent,
                    affectedCount)
                : RushPolicy.Rejected(
                    request,
                    default,
                    ResourceExchangeReason.RushUnavailable);
        }

        private static ResourceExchangeResultComponent ProcessClearCompletedRequest(
            in ResourceExchangeEnabledComponent enabled,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            in ResourceExchangeRequestComponent request)
        {
            byte factionId = request.FactionId != 0 ? request.FactionId : enabled.FactionId;
            if (enabled.Enabled == 0 ||
                (enabled.FactionId != 0 && factionId != enabled.FactionId))
            {
                return Rejected(request, default, ResourceExchangeReason.ExchangeUnavailable);
            }

            int removedCount = 0;
            for (int i = queue.Length - 1; i >= 0; i--)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.FactionId != factionId || item.State != ResourceExchangeQueueState.Completed)
                    continue;

                queue.RemoveAt(i);
                removedCount++;
            }

            return new ResourceExchangeResultComponent
            {
                RequestId = request.RequestId,
                FactionId = factionId,
                ResultKind = ResourceExchangeResultKind.RequestAccepted,
                Accepted = 1,
                Reason = ResourceExchangeReason.None,
                InputAmount = removedCount
            };
        }

        private static ResourceExchangeResultComponent ProcessMissionEndRequest(
            in ResourceExchangeEnabledComponent enabled,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> economyEvents,
            in ResourceExchangeRequestComponent request,
            EntityManager entityManager,
            DynamicBuffer<ResourceExchangePhysicalReservationComponent> physicalReservations,
            bool usePhysicalStorage)
        {
            byte factionId = request.FactionId != 0 ? request.FactionId : enabled.FactionId;
            if (enabled.FactionId != 0 && factionId != enabled.FactionId)
                return Rejected(request, default, ResourceExchangeReason.ExchangeUnavailable);

            int cancelledCount = 0;
            int totalRefund = 0;
            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.FactionId != factionId ||
                    !RequestQueue.CanCancel(item))
                    continue;

                int refundAmount = RequestQueue.CalculateRefundAmount(item);
                bool physicalInput = usePhysicalStorage &&
                                     ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(
                                         item.InputResource);
                bool physicalOutput = usePhysicalStorage &&
                                      ResourceExchangePhysicalStorageUtilitySystemHelper.IsPhysicalResource(
                                          item.OutputResource);
                if (physicalInput || physicalOutput)
                {
                    ResourceExchangePhysicalStorageUtilitySystemHelper.CancelQueueItem(
                        entityManager,
                        physicalReservations,
                        item.QueueItemId,
                        refundAmount > 0);
                }

                if (refundAmount > 0)
                {
                    if (!physicalInput)
                    {
                        ResourceExchangeResourceUtilitySystemHelper.TryRefundReservedInput(
                            ref economy,
                            ref materials,
                            ref wallet,
                            item.InputResource,
                            refundAmount);
                    }
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
                else
                {
                    economyEvents.Add(new ResourceExchangeEconomyEventComponent
                    {
                        QueueItemId = item.QueueItemId,
                        FactionId = item.FactionId,
                        ResultKind = ResourceExchangeResultKind.QueueCancelled,
                        ResourceKind = item.InputResource,
                        Amount = 0,
                        RecipeId = item.RecipeId
                    });
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
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials,
            in ResourceExchangeWalletComponent wallet,
            in ResourceExchangeRecipeComponent recipe,
            int outputAmount)
        {
            if (recipe.RequiresStorage == 0)
                return ResourceExchangeReason.None;

            int capacity = ResourceExchangeResourceUtilitySystemHelper.GetCapacity(
                materials,
                wallet,
                recipe.OutputResource);
            if (capacity <= 0)
                return ResourceExchangeReason.StorageMissing;

            int current = ResourceExchangeResourceUtilitySystemHelper.GetAmount(
                economy,
                materials,
                wallet,
                recipe.OutputResource);
            return current >= 0 && outputAmount >= 0 && outputAmount <= capacity - current
                ? ResourceExchangeReason.None
                : ResourceExchangeReason.StorageFull;
        }

        private static bool TrySpendInput(
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            ref ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind,
            int amount,
            out ResourceExchangeReason reason)
        {
            int current = ResourceExchangeResourceUtilitySystemHelper.GetAmount(
                economy,
                materials,
                wallet,
                resourceKind);
            if (current < amount)
            {
                reason = RequestQueue.InsufficientReason(resourceKind);
                return false;
            }

            if (!ResourceExchangeResourceUtilitySystemHelper.TrySpend(
                    ref economy,
                    ref materials,
                    ref wallet,
                    resourceKind,
                    amount))
            {
                reason = RequestQueue.InsufficientReason(resourceKind);
                return false;
            }

            reason = ResourceExchangeReason.None;
            return true;
        }

        private static bool TrySpendInput(
            ref ResourceExchangeWalletComponent wallet,
            ResourceExchangeResourceKind resourceKind,
            int amount,
            out ResourceExchangeReason reason)
        {
            if (resourceKind != ResourceExchangeResourceKind.RushTickets ||
                amount <= 0 ||
                wallet.RushTickets < amount)
            {
                reason = RequestQueue.InsufficientReason(resourceKind);
                return false;
            }

            wallet.RushTickets -= amount;
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
            summary.AllowAiExchange = enabled.AllowAiExchange;
            summary.QueueCount = queue.Length;
            summary.ActiveCount = activeCount;
            summary.CompletedCount = completedCount;
            summary.MaxQueueItems = enabled.MaxQueueItems;
            summary.LastReason = result.Reason;
            summary.Version++;
        }

    }
}
