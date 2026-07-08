using System.Globalization;
using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct UiResourceExchangeReadModelSystem : ISystem
    {
        private const int MaxRecipeCards = 7;
        private const int MaxQueueRows = 4;

        private EntityQuery boundaryQuery;
        private EntityQuery exchangeQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiShellActivePopupComponent>(),
                ComponentType.ReadWrite<UiResourceExchangeStateComponent>(),
                ComponentType.ReadWrite<UiResourceExchangeDetailComponent>(),
                ComponentType.ReadWrite<UiResourceExchangeRecipeCardComponent>(),
                ComponentType.ReadWrite<UiResourceExchangeQueueRowComponent>());
            exchangeQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ResourceExchangeEnabledComponent>(),
                ComponentType.ReadOnly<ResourceExchangeWalletComponent>(),
                ComponentType.ReadOnly<ResourceExchangeSummaryComponent>(),
                ComponentType.ReadOnly<ResourceExchangeRecipeComponent>(),
                ComponentType.ReadOnly<ResourceExchangeQueueComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            UiShellActivePopupComponent activePopup =
                state.EntityManager.GetComponentData<UiShellActivePopupComponent>(boundary);
            if (activePopup.Visible == 0 || activePopup.PopupKind != Game.UI.Contracts.UiShellPopupKind.ResourceExchange)
                return;

            UiResourceExchangeStateComponent uiState =
                state.EntityManager.GetComponentData<UiResourceExchangeStateComponent>(boundary);
            UiResourceExchangeDetailComponent detail =
                state.EntityManager.GetComponentData<UiResourceExchangeDetailComponent>(boundary);
            DynamicBuffer<UiResourceExchangeRecipeCardComponent> cards =
                state.EntityManager.GetBuffer<UiResourceExchangeRecipeCardComponent>(boundary);
            DynamicBuffer<UiResourceExchangeQueueRowComponent> queueRows =
                state.EntityManager.GetBuffer<UiResourceExchangeQueueRowComponent>(boundary);

            if (exchangeQuery.IsEmptyIgnoreFilter)
            {
                WriteUnavailable(ref uiState, ref detail, cards, queueRows);
            }
            else
            {
                Entity exchange = ResolveFirstEntity(exchangeQuery);
                WriteReadModel(
                    state.EntityManager.GetComponentData<ResourceExchangeEnabledComponent>(exchange),
                    state.EntityManager.GetComponentData<ResourceExchangeWalletComponent>(exchange),
                    state.EntityManager.GetComponentData<ResourceExchangeSummaryComponent>(exchange),
                    state.EntityManager.GetBuffer<ResourceExchangeRecipeComponent>(exchange, true),
                    state.EntityManager.GetBuffer<ResourceExchangeQueueComponent>(exchange, true),
                    ref uiState,
                    ref detail,
                    cards,
                    queueRows);
            }

            state.EntityManager.SetComponentData(boundary, uiState);
            state.EntityManager.SetComponentData(boundary, detail);
        }

        public static void WriteReadModel(
            in ResourceExchangeEnabledComponent enabled,
            in ResourceExchangeWalletComponent wallet,
            in ResourceExchangeSummaryComponent summary,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            ref UiResourceExchangeStateComponent uiState,
            ref UiResourceExchangeDetailComponent detail,
            DynamicBuffer<UiResourceExchangeRecipeCardComponent> cards,
            DynamicBuffer<UiResourceExchangeQueueRowComponent> queueRows)
        {
            uiState.ExchangeEnabled = enabled.Enabled;
            uiState.ExportRecipeCount = CountRecipes(recipes, ResourceExchangeRouteType.Export);
            uiState.ImportRecipeCount = CountRecipes(recipes, ResourceExchangeRouteType.Import);
            uiState.QueueCount = queue.Length;
            uiState.ActiveCount = summary.ActiveCount;
            uiState.CompletedCount = summary.CompletedCount;
            uiState.MaxQueueItems = enabled.MaxQueueItems;
            uiState.QueueCapacityText = ToFixed32($"{summary.ActiveCount}/{math.max(0, enabled.MaxQueueItems)}");
            uiState.CreditsText = ToFixed32(wallet.Credits.ToString(CultureInfo.InvariantCulture));
            uiState.MaterialsText = ToFixed32(wallet.Materials.ToString(CultureInfo.InvariantCulture));
            uiState.OilText = ToFixed32(wallet.Oil.ToString(CultureInfo.InvariantCulture));
            uiState.FuelText = ToFixed32(wallet.Fuel.ToString(CultureInfo.InvariantCulture));
            uiState.RushTicketsText = ToFixed32(wallet.RushTickets.ToString(CultureInfo.InvariantCulture));
            uiState.RushAllEnabled = HasRushableQueueItem(wallet, recipes, queue) ? (byte)1 : (byte)0;
            uiState.ClearCompletedEnabled = HasCompletedQueueItem(queue) ? (byte)1 : (byte)0;
            uiState.Version = math.max(uiState.Version + 1u, summary.Version);

            WriteRecipeCards(enabled, wallet, recipes, queue, ref uiState, ref detail, cards);
            WriteQueueRows(wallet, recipes, queue, queueRows);
        }

        private static void WriteUnavailable(
            ref UiResourceExchangeStateComponent uiState,
            ref UiResourceExchangeDetailComponent detail,
            DynamicBuffer<UiResourceExchangeRecipeCardComponent> cards,
            DynamicBuffer<UiResourceExchangeQueueRowComponent> queueRows)
        {
            uiState.ExchangeEnabled = 0;
            uiState.ExportRecipeCount = 0;
            uiState.ImportRecipeCount = 0;
            uiState.QueueCount = 0;
            uiState.ActiveCount = 0;
            uiState.CompletedCount = 0;
            uiState.MaxQueueItems = 0;
            uiState.QueueCapacityText = new FixedString32Bytes("0/0");
            uiState.RushAllEnabled = 0;
            uiState.ClearCompletedEnabled = 0;
            uiState.Version++;
            detail = new UiResourceExchangeDetailComponent
            {
                Name = new FixedString64Bytes("RESOURCE EXCHANGE"),
                RouteText = ToFixed32(FormatTab(uiState.ActiveTab)),
                RequirementsText = new FixedString64Bytes("Exchange unavailable."),
                InstructionText = new FixedString128Bytes("Resource Exchange is not enabled for this scenario.")
            };
            cards.Clear();
            queueRows.Clear();
        }

        private static void WriteRecipeCards(
            in ResourceExchangeEnabledComponent enabled,
            in ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            ref UiResourceExchangeStateComponent uiState,
            ref UiResourceExchangeDetailComponent detail,
            DynamicBuffer<UiResourceExchangeRecipeCardComponent> cards)
        {
            cards.Clear();
            ResourceExchangeRouteType activeRoute = ToRouteType(uiState.ActiveTab);
            int selectedRecipeIndex = -1;
            int visibleIndex = 0;
            for (int i = 0; i < recipes.Length && visibleIndex < MaxRecipeCards; i++)
            {
                ResourceExchangeRecipeComponent recipe = recipes[i];
                if (recipe.RouteType != activeRoute)
                    continue;

                if (visibleIndex == uiState.SelectedRecipeSlot)
                    selectedRecipeIndex = i;

                ResourceExchangeReason availability = ValidateRecipe(enabled, recipe);
                int amount = math.max(1, recipe.InputAmountMin);
                int outputAmount = CalculateOutputAmount(recipe, amount);
                cards.Add(new UiResourceExchangeRecipeCardComponent
                {
                    Visible = 1,
                    Enabled = availability == ResourceExchangeReason.None ? (byte)1 : (byte)0,
                    Selected = visibleIndex == uiState.SelectedRecipeSlot ? (byte)1 : (byte)0,
                    Locked = availability == ResourceExchangeReason.None ? (byte)0 : (byte)1,
                    WarningVisible = availability == ResourceExchangeReason.None ? (byte)0 : (byte)1,
                    Tab = uiState.ActiveTab,
                    RecipeId = recipe.RecipeId,
                    Title = ToFixed64(recipe.DisplayName),
                    InputText = ToFixed32(FormatResourceAmount(recipe.InputResource, amount)),
                    OutputText = ToFixed32(FormatResourceAmount(recipe.OutputResource, outputAmount)),
                    DurationText = ToFixed32(FormatDuration(CalculateDuration(recipe, amount))),
                    ReasonText = ToFixed64(FormatReason(availability))
                });
                visibleIndex++;
            }

            if (visibleIndex == 0)
            {
                uiState.SelectedRecipeSlot = 0;
                detail = EmptyDetail(uiState.ActiveTab);
                return;
            }

            uiState.SelectedRecipeSlot = math.clamp(uiState.SelectedRecipeSlot, 0, visibleIndex - 1);
            if (selectedRecipeIndex < 0)
                selectedRecipeIndex = FindRecipeIndexByVisibleSlot(recipes, activeRoute, uiState.SelectedRecipeSlot);

            detail = selectedRecipeIndex >= 0
                ? BuildDetail(enabled, wallet, recipes[selectedRecipeIndex], queue)
                : EmptyDetail(uiState.ActiveTab);
        }

        private static UiResourceExchangeDetailComponent BuildDetail(
            in ResourceExchangeEnabledComponent enabled,
            in ResourceExchangeWalletComponent wallet,
            in ResourceExchangeRecipeComponent recipe,
            DynamicBuffer<ResourceExchangeQueueComponent> queue)
        {
            int amount = math.max(1, recipe.InputAmountMin);
            int outputAmount = CalculateOutputAmount(recipe, amount);
            float duration = CalculateDuration(recipe, amount);
            ResourceExchangeReason reason = ValidateConfirm(enabled, wallet, recipe, outputAmount, CountActiveQueueItems(queue, enabled.FactionId));
            return new UiResourceExchangeDetailComponent
            {
                RecipeId = recipe.RecipeId,
                Name = ToFixed64(recipe.DisplayName),
                RouteText = ToFixed32(recipe.RouteType == ResourceExchangeRouteType.Export ? "EXPORT" : "IMPORT"),
                RateText = ToFixed64(FormatRate(recipe)),
                AmountText = ToFixed32(amount.ToString(CultureInfo.InvariantCulture)),
                InputCostText = ToFixed32(FormatResourceAmount(recipe.InputResource, amount)),
                OutputPreviewText = ToFixed32(FormatResourceAmount(recipe.OutputResource, outputAmount)),
                DurationText = ToFixed32(FormatDuration(duration)),
                RequirementsText = ToFixed64(FormatRequirements(recipe)),
                InstructionText = ToFixed128(reason == ResourceExchangeReason.None
                    ? "Confirm to start a timed logistics exchange."
                    : FormatReason(reason)),
                ConfirmEnabled = reason == ResourceExchangeReason.None ? (byte)1 : (byte)0,
                WarningVisible = reason == ResourceExchangeReason.None ? (byte)0 : (byte)1
            };
        }

        private static void WriteQueueRows(
            in ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<UiResourceExchangeQueueRowComponent> queueRows)
        {
            queueRows.Clear();
            int count = math.min(queue.Length, MaxQueueRows);
            for (int i = 0; i < count; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                TryFindRecipe(recipes, item.RecipeId, out ResourceExchangeRecipeComponent recipe);
                float progress = item.DurationSeconds <= 0f
                    ? 1f
                    : math.saturate(1f - item.RemainingSeconds / item.DurationSeconds);
                bool active = item.State == ResourceExchangeQueueState.Pending ||
                              item.State == ResourceExchangeQueueState.InProgress ||
                              item.State == ResourceExchangeQueueState.Blocked;
                bool rushable = item.State == ResourceExchangeQueueState.InProgress &&
                                item.RemainingSeconds > 0f &&
                                wallet.RushTickets > 0 &&
                                recipe.MaxRushTickets > item.RushTicketsSpent &&
                                recipe.RushTicketSecondsPerTicket > 0;
                queueRows.Add(new UiResourceExchangeQueueRowComponent
                {
                    Visible = 1,
                    RushEnabled = rushable ? (byte)1 : (byte)0,
                    CancelEnabled = active && item.OutputApplied == 0 ? (byte)1 : (byte)0,
                    CompletedVisible = item.State == ResourceExchangeQueueState.Completed ? (byte)1 : (byte)0,
                    QueueItemId = item.QueueItemId,
                    State = ToUiQueueState(item.State),
                    NumberText = ToFixed32((i + 1).ToString(CultureInfo.InvariantCulture)),
                    Name = recipe.DisplayName.Length > 0 ? ToFixed64(recipe.DisplayName) : ToFixed64(item.RecipeId),
                    InputText = ToFixed32(FormatResourceAmount(item.InputResource, item.InputAmount)),
                    OutputText = ToFixed32(FormatResourceAmount(item.OutputResource, item.OutputAmount)),
                    TimeText = ToFixed32(FormatDuration(item.RemainingSeconds)),
                    PercentText = ToFixed32(FormatPercent(progress)),
                    StateText = ToFixed64(FormatState(item.State, item.StateReason)),
                    Progress01 = progress
                });
            }
        }

        private static Entity ResolveFirstEntity(EntityQuery query)
        {
            if (query.CalculateEntityCount() == 1)
                return query.GetSingletonEntity();

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            return entities.Length > 0 ? entities[0] : Entity.Null;
        }

        private static int CountRecipes(DynamicBuffer<ResourceExchangeRecipeComponent> recipes, ResourceExchangeRouteType routeType)
        {
            int count = 0;
            for (int i = 0; i < recipes.Length; i++)
                if (recipes[i].RouteType == routeType)
                    count++;
            return count;
        }

        private static int CountActiveQueueItems(DynamicBuffer<ResourceExchangeQueueComponent> queue, byte factionId)
        {
            int count = 0;
            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (factionId != 0 && item.FactionId != factionId)
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

        private static bool HasCompletedQueueItem(DynamicBuffer<ResourceExchangeQueueComponent> queue)
        {
            for (int i = 0; i < queue.Length; i++)
                if (queue[i].State == ResourceExchangeQueueState.Completed)
                    return true;
            return false;
        }

        private static bool HasRushableQueueItem(
            in ResourceExchangeWalletComponent wallet,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeQueueComponent> queue)
        {
            if (wallet.RushTickets <= 0)
                return false;

            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                if (item.State != ResourceExchangeQueueState.InProgress || item.RemainingSeconds <= 0f)
                    continue;

                if (TryFindRecipe(recipes, item.RecipeId, out ResourceExchangeRecipeComponent recipe) &&
                    recipe.RushTicketSecondsPerTicket > 0 &&
                    recipe.MaxRushTickets > item.RushTicketsSpent)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindRecipeIndexByVisibleSlot(
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            ResourceExchangeRouteType routeType,
            int visibleSlot)
        {
            int visibleIndex = 0;
            for (int i = 0; i < recipes.Length; i++)
            {
                if (recipes[i].RouteType != routeType)
                    continue;

                if (visibleIndex == visibleSlot)
                    return i;
                visibleIndex++;
            }

            return -1;
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

        private static ResourceExchangeReason ValidateRecipe(
            in ResourceExchangeEnabledComponent enabled,
            in ResourceExchangeRecipeComponent recipe)
        {
            if (enabled.Enabled == 0)
                return ResourceExchangeReason.ExchangeUnavailable;

            if (recipe.Enabled == 0)
                return recipe.DisabledReason != ResourceExchangeReason.None
                    ? recipe.DisabledReason
                    : ResourceExchangeReason.RecipeLocked;

            if (recipe.MissionTag.Length > 0 && !recipe.MissionTag.Equals(enabled.ScenarioTag))
                return ResourceExchangeReason.RecipeLocked;

            return ResourceExchangeReason.None;
        }

        private static ResourceExchangeReason ValidateConfirm(
            in ResourceExchangeEnabledComponent enabled,
            in ResourceExchangeWalletComponent wallet,
            in ResourceExchangeRecipeComponent recipe,
            int outputAmount,
            int activeQueueCount)
        {
            ResourceExchangeReason recipeReason = ValidateRecipe(enabled, recipe);
            if (recipeReason != ResourceExchangeReason.None)
                return recipeReason;

            int maxQueueItems = math.max(0, enabled.MaxQueueItems);
            if (maxQueueItems <= 0 || activeQueueCount >= maxQueueItems)
                return ResourceExchangeReason.QueueFull;

            int inputAmount = math.max(1, recipe.InputAmountMin);
            if (GetResourceAmount(wallet, recipe.InputResource) < inputAmount)
                return InsufficientReason(recipe.InputResource);

            if (recipe.RequiresStorage != 0 && recipe.OutputResource != ResourceExchangeResourceKind.Credits)
            {
                int capacity = GetCapacity(wallet, recipe.OutputResource);
                if (capacity <= 0)
                    return ResourceExchangeReason.StorageMissing;

                if (GetResourceAmount(wallet, recipe.OutputResource) + outputAmount > capacity)
                    return ResourceExchangeReason.StorageFull;
            }

            return ResourceExchangeReason.None;
        }

        private static int CalculateOutputAmount(in ResourceExchangeRecipeComponent recipe, int inputAmount)
        {
            float output = inputAmount * math.max(0f, recipe.OutputPerInput) * (1f - math.clamp(recipe.FeePercent, 0f, 0.95f));
            return math.max(0, (int)math.floor(output));
        }

        private static float CalculateDuration(in ResourceExchangeRecipeComponent recipe, int inputAmount)
        {
            int steps = math.max(0, (inputAmount - recipe.InputAmountMin) / math.max(1, recipe.InputStep));
            return math.max(0f, recipe.DurationSecondsBase + steps * recipe.DurationSecondsPerStep);
        }

        private static int GetResourceAmount(in ResourceExchangeWalletComponent wallet, ResourceExchangeResourceKind resourceKind)
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

        private static int GetCapacity(in ResourceExchangeWalletComponent wallet, ResourceExchangeResourceKind resourceKind)
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

        private static ResourceExchangeRouteType ToRouteType(UiResourceExchangeTab tab)
        {
            return tab == UiResourceExchangeTab.Import
                ? ResourceExchangeRouteType.Import
                : ResourceExchangeRouteType.Export;
        }

        private static UiResourceExchangeQueueState ToUiQueueState(ResourceExchangeQueueState state)
        {
            switch (state)
            {
                case ResourceExchangeQueueState.Pending:
                    return UiResourceExchangeQueueState.Pending;
                case ResourceExchangeQueueState.InProgress:
                case ResourceExchangeQueueState.Completing:
                    return UiResourceExchangeQueueState.InProgress;
                case ResourceExchangeQueueState.Completed:
                    return UiResourceExchangeQueueState.Completed;
                case ResourceExchangeQueueState.Cancelled:
                    return UiResourceExchangeQueueState.Cancelled;
                case ResourceExchangeQueueState.Blocked:
                    return UiResourceExchangeQueueState.Blocked;
                default:
                    return UiResourceExchangeQueueState.None;
            }
        }

        private static UiResourceExchangeDetailComponent EmptyDetail(UiResourceExchangeTab tab)
        {
            return new UiResourceExchangeDetailComponent
            {
                Name = new FixedString64Bytes("NO ROUTES"),
                RouteText = ToFixed32(FormatTab(tab)),
                RateText = new FixedString64Bytes("No routes available."),
                RequirementsText = new FixedString64Bytes("Scenario locked."),
                InstructionText = new FixedString128Bytes("No resource exchange routes are available."),
                ConfirmEnabled = 0,
                WarningVisible = 1
            };
        }

        private static string FormatTab(UiResourceExchangeTab tab)
        {
            return tab == UiResourceExchangeTab.Import ? "IMPORT" : "EXPORT";
        }

        private static string FormatResourceAmount(ResourceExchangeResourceKind resourceKind, int amount)
        {
            return $"{math.max(0, amount).ToString(CultureInfo.InvariantCulture)} {FormatResource(resourceKind)}";
        }

        private static string FormatResource(ResourceExchangeResourceKind resourceKind)
        {
            switch (resourceKind)
            {
                case ResourceExchangeResourceKind.Credits:
                    return "CREDITS";
                case ResourceExchangeResourceKind.Materials:
                    return "MATERIALS";
                case ResourceExchangeResourceKind.Oil:
                    return "OIL";
                case ResourceExchangeResourceKind.Fuel:
                    return "FUEL";
                case ResourceExchangeResourceKind.RushTickets:
                    return "RUSH";
                default:
                    return "RESOURCE";
            }
        }

        private static string FormatRate(in ResourceExchangeRecipeComponent recipe)
        {
            float effectiveRate = math.max(0f, recipe.OutputPerInput) * (1f - math.clamp(recipe.FeePercent, 0f, 0.95f));
            return $"1 {FormatResource(recipe.InputResource)} -> {effectiveRate.ToString("0.##", CultureInfo.InvariantCulture)} {FormatResource(recipe.OutputResource)}";
        }

        private static string FormatDuration(float seconds)
        {
            int totalSeconds = math.max(0, (int)math.ceil(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static string FormatPercent(float progress01)
        {
            int percent = math.clamp((int)math.round(math.saturate(progress01) * 100f), 0, 100);
            return percent.ToString(CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatRequirements(in ResourceExchangeRecipeComponent recipe)
        {
            if (recipe.RequiresStorage != 0)
                return "Storage capacity required.";

            return recipe.RequiresTransportPlane != 0
                ? "Logistics transport required."
                : "No special requirements.";
        }

        private static string FormatState(ResourceExchangeQueueState state, ResourceExchangeReason reason)
        {
            if (state == ResourceExchangeQueueState.Blocked)
                return "BLOCKED: " + FormatReason(reason);

            switch (state)
            {
                case ResourceExchangeQueueState.Pending:
                    return "QUEUED";
                case ResourceExchangeQueueState.InProgress:
                    return "IN PROGRESS";
                case ResourceExchangeQueueState.Completing:
                    return "COMPLETING";
                case ResourceExchangeQueueState.Completed:
                    return "COMPLETE";
                case ResourceExchangeQueueState.Cancelled:
                    return "CANCELLED";
                default:
                    return "IDLE";
            }
        }

        private static string FormatReason(ResourceExchangeReason reason)
        {
            switch (reason)
            {
                case ResourceExchangeReason.None:
                    return string.Empty;
                case ResourceExchangeReason.ExchangeUnavailable:
                    return "Exchange unavailable";
                case ResourceExchangeReason.RecipeLocked:
                    return "Route locked";
                case ResourceExchangeReason.InsufficientCredits:
                    return "Insufficient Credits";
                case ResourceExchangeReason.InsufficientMaterials:
                    return "Insufficient Materials";
                case ResourceExchangeReason.InsufficientOil:
                    return "Insufficient Oil";
                case ResourceExchangeReason.InsufficientFuel:
                    return "Insufficient Fuel";
                case ResourceExchangeReason.QueueFull:
                    return "Queue full";
                case ResourceExchangeReason.StorageFull:
                    return "Storage full";
                case ResourceExchangeReason.StorageMissing:
                    return "Storage missing";
                case ResourceExchangeReason.RushUnavailable:
                    return "Rush unavailable";
                case ResourceExchangeReason.InsufficientRushTickets:
                    return "Insufficient Rush Tickets";
                case ResourceExchangeReason.CancelUnavailable:
                    return "Cancel unavailable";
                case ResourceExchangeReason.MissionEnding:
                    return "Mission ending";
                default:
                    return "Unavailable";
            }
        }

        private static FixedString32Bytes ToFixed32(string value)
        {
            return new FixedString32Bytes(Trim(value, 28));
        }

        private static FixedString64Bytes ToFixed64(string value)
        {
            return new FixedString64Bytes(Trim(value, 60));
        }

        private static FixedString64Bytes ToFixed64(FixedString128Bytes value)
        {
            return new FixedString64Bytes(Trim(value.ToString(), 60));
        }

        private static FixedString128Bytes ToFixed128(string value)
        {
            return new FixedString128Bytes(Trim(value, 120));
        }

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
