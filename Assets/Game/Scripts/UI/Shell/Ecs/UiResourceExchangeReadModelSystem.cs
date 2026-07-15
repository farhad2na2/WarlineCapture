using System.Globalization;
using Game.Components;
using Game.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Game.UI.Shell.Ecs.UiResourceExchangeProjectionSystemHelper;
using Hash = Game.UI.Shell.Ecs.UiResourceExchangeProjectionFingerprintUtilitySystemHelper;
using Text = Game.UI.Shell.Ecs.UiResourceExchangeTextProjectionUtilitySystemHelper;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct UiResourceExchangeReadModelSystem : ISystem
    {
        private const int MaxRecipeCards = 7;
        private const int MaxQueueRows = 4;

        private EntityQuery boundaryQuery;
        private EntityQuery physicalResourceQuery;
        private Entity lastBoundary;
        private ulong lastHash;
        private byte hasHash;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiShellActivePopupComponent>(),
                ComponentType.ReadWrite<UiResourceExchangeStateComponent>(),
                ComponentType.ReadWrite<UiResourceExchangeDetailComponent>(),
                ComponentType.ReadWrite<UiResourceExchangeRecipeCardComponent>(),
                ComponentType.ReadWrite<UiResourceExchangeQueueRowComponent>());
            state.RequireForUpdate(boundaryQuery);
            physicalResourceQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                ComponentType.ReadOnly<BuildingRuntimeFactionUsableFuelSummary>());
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            if (boundary != lastBoundary)
            {
                lastBoundary = boundary;
                hasHash = 0;
            }
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
            BuildingRuntimeFactionUsableFuelSummary physicalResources = default;
            if (!physicalResourceQuery.IsEmptyIgnoreFilter)
            {
                Entity physicalResourceBoundary = physicalResourceQuery.GetSingletonEntity();
                DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                    state.EntityManager.GetBuffer<BuildingRuntimeFactionUsableFuelSummary>(
                        physicalResourceBoundary,
                        true);
                TryReadPhysicalResources(summaries, out physicalResources);
            }

            int playerExchangeCount = 0;
            ulong projectionFingerprint = 0;
            bool projectionChanged = false;
            foreach (var (
                         enabled,
                         economy,
                         materials,
                         wallet,
                         summary,
                         recipes,
                         queue)
                     in SystemAPI.Query<
                         RefRO<ResourceExchangeEnabledComponent>,
                         RefRO<FactionEconomy>,
                         RefRO<FactionTacticalMaterialsComponent>,
                         RefRO<ResourceExchangeWalletComponent>,
                         RefRO<ResourceExchangeSummaryComponent>,
                         DynamicBuffer<ResourceExchangeRecipeComponent>,
                         DynamicBuffer<ResourceExchangeQueueComponent>>())
            {
                byte factionId = enabled.ValueRO.FactionId;
                if (factionId != FactionIdentity.PlayerFactionId ||
                    economy.ValueRO.FactionId != factionId ||
                    materials.ValueRO.FactionId != factionId ||
                    wallet.ValueRO.FactionId != factionId)
                {
                    continue;
                }

                playerExchangeCount++;
                if (playerExchangeCount > 1)
                    break;

                projectionFingerprint = Hash.Calculate(
                    enabled.ValueRO, economy.ValueRO, materials.ValueRO, wallet.ValueRO,
                    physicalResources, summary.ValueRO, uiState, recipes, queue);
                if (hasHash == 0 || projectionFingerprint != lastHash)
                {
                    WriteReadModel(
                        enabled.ValueRO,
                        economy.ValueRO,
                        materials.ValueRO,
                        wallet.ValueRO,
                        physicalResources,
                        summary.ValueRO,
                        recipes,
                        queue,
                        ref uiState,
                        ref detail,
                        cards,
                        queueRows);
                    projectionFingerprint = Hash.Calculate(
                        enabled.ValueRO, economy.ValueRO, materials.ValueRO, wallet.ValueRO,
                        physicalResources, summary.ValueRO, uiState, recipes, queue);
                    projectionChanged = true;
                }
            }

            if (playerExchangeCount != 1)
            {
                projectionFingerprint = Hash.CalculateUnavailable(uiState);
                if (hasHash == 0 || projectionFingerprint != lastHash)
                {
                    WriteUnavailable(ref uiState, ref detail, cards, queueRows);
                    projectionChanged = true;
                }
            }

            lastHash = projectionFingerprint;
            hasHash = 1;
            if (!projectionChanged)
                return;

            state.EntityManager.SetComponentData(boundary, uiState);
            state.EntityManager.SetComponentData(boundary, detail);
        }

        public static void WriteReadModel(
            in ResourceExchangeEnabledComponent enabled,
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials,
            in ResourceExchangeWalletComponent wallet,
            in BuildingRuntimeFactionUsableFuelSummary physicalResources,
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
            uiState.QueueCapacityText = Text.ToFixed32(
                $"{summary.ActiveCount}/{math.max(0, enabled.MaxQueueItems)}");
            uiState.CreditsText = Text.ToFixed32(economy.Money.ToString(CultureInfo.InvariantCulture));
            uiState.MaterialsText = Text.ToFixed32(materials.Current.ToString(CultureInfo.InvariantCulture));
            uiState.OilText = Text.ToFixed32(ResourceExchangeResourceUtilitySystemHelper.GetAmount(
                economy, materials, wallet, physicalResources,
                ResourceExchangeResourceKind.Oil).ToString(CultureInfo.InvariantCulture));
            uiState.FuelText = Text.ToFixed32(ResourceExchangeResourceUtilitySystemHelper.GetAmount(
                economy, materials, wallet, physicalResources,
                ResourceExchangeResourceKind.Fuel).ToString(CultureInfo.InvariantCulture));
            uiState.RushTicketsText = Text.ToFixed32(wallet.RushTickets.ToString(CultureInfo.InvariantCulture));
            uiState.RushAllEnabled = HasRushableQueueItem(wallet, recipes, queue) ? (byte)1 : (byte)0;
            uiState.ClearCompletedEnabled = HasCompletedQueueItem(queue) ? (byte)1 : (byte)0;
            uiState.Version = math.max(uiState.Version + 1u, summary.Version);

            WriteRecipeCards(
                enabled, economy, materials, wallet, physicalResources,
                recipes, queue, ref uiState, ref detail,
                cards);
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
                RouteText = Text.ToFixed32(Text.FormatTab(uiState.ActiveTab)),
                RequirementsText = new FixedString64Bytes("Exchange unavailable."),
                InstructionText = new FixedString128Bytes("Resource Exchange is not enabled for this scenario.")
            };
            cards.Clear();
            queueRows.Clear();
        }

        private static void WriteRecipeCards(
            in ResourceExchangeEnabledComponent enabled,
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials,
            in ResourceExchangeWalletComponent wallet,
            in BuildingRuntimeFactionUsableFuelSummary physicalResources,
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
                    Title = Text.ToFixed64(recipe.DisplayName),
                    InputText = Text.ToFixed32(
                        Text.FormatResourceAmount(recipe.InputResource, amount)),
                    OutputText = Text.ToFixed32(
                        Text.FormatResourceAmount(recipe.OutputResource, outputAmount)),
                    DurationText = Text.ToFixed32(
                        Text.FormatDuration(CalculateDuration(recipe, amount))),
                    ReasonText = Text.ToFixed64(Text.FormatReason(availability))
                });
                visibleIndex++;
            }

            if (visibleIndex == 0)
            {
                uiState.SelectedRecipeSlot = 0;
                uiState.SelectedInputAmount = 0;
                detail = EmptyDetail(uiState.ActiveTab);
                return;
            }

            uiState.SelectedRecipeSlot = math.clamp(uiState.SelectedRecipeSlot, 0, visibleIndex - 1);
            if (selectedRecipeIndex < 0)
                selectedRecipeIndex = FindRecipeIndexByVisibleSlot(recipes, activeRoute, uiState.SelectedRecipeSlot);

            if (selectedRecipeIndex >= 0)
            {
                int selectedAmount = NormalizeInputAmount(recipes[selectedRecipeIndex], uiState.SelectedInputAmount);
                uiState.SelectedInputAmount = selectedAmount;
                detail = BuildDetail(
                    enabled,
                    economy,
                    materials,
                    wallet,
                    physicalResources,
                    recipes[selectedRecipeIndex],
                    queue,
                    selectedAmount);
            }
            else
            {
                uiState.SelectedInputAmount = 0;
                detail = EmptyDetail(uiState.ActiveTab);
            }
        }

        private static UiResourceExchangeDetailComponent BuildDetail(
            in ResourceExchangeEnabledComponent enabled,
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials,
            in ResourceExchangeWalletComponent wallet,
            in BuildingRuntimeFactionUsableFuelSummary physicalResources,
            in ResourceExchangeRecipeComponent recipe,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            int amount)
        {
            int outputAmount = CalculateOutputAmount(recipe, amount);
            float duration = CalculateDuration(recipe, amount);
            ResourceExchangeReason reason = ValidateConfirm(
                enabled,
                economy,
                materials,
                wallet,
                physicalResources,
                recipe,
                amount,
                outputAmount,
                CountActiveQueueItems(queue, enabled.FactionId));
            return new UiResourceExchangeDetailComponent
            {
                RecipeId = recipe.RecipeId,
                Name = Text.ToFixed64(recipe.DisplayName),
                RouteText = Text.ToFixed32(
                    recipe.RouteType == ResourceExchangeRouteType.Export ? "EXPORT" : "IMPORT"),
                RateText = Text.ToFixed64(Text.FormatRate(recipe)),
                AmountText = Text.ToFixed32(amount.ToString(CultureInfo.InvariantCulture)),
                InputCostText = Text.ToFixed32(
                    Text.FormatResourceAmount(recipe.InputResource, amount)),
                OutputPreviewText = Text.ToFixed32(
                    Text.FormatResourceAmount(recipe.OutputResource, outputAmount)),
                DurationText = Text.ToFixed32(Text.FormatDuration(duration)),
                RequirementsText = Text.ToFixed64(Text.FormatRequirements(recipe)),
                InstructionText = Text.ToFixed128(reason == ResourceExchangeReason.None
                    ? "Confirm to start a timed logistics exchange."
                    : Text.FormatReason(reason)),
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
                    NumberText = Text.ToFixed32((i + 1).ToString(CultureInfo.InvariantCulture)),
                    Name = recipe.DisplayName.Length > 0
                        ? Text.ToFixed64(recipe.DisplayName)
                        : Text.ToFixed64(item.RecipeId),
                    InputText = Text.ToFixed32(
                        Text.FormatResourceAmount(item.InputResource, item.InputAmount)),
                    OutputText = Text.ToFixed32(
                        Text.FormatResourceAmount(item.OutputResource, item.OutputAmount)),
                    TimeText = Text.ToFixed32(Text.FormatDuration(item.RemainingSeconds)),
                    PercentText = Text.ToFixed32(Text.FormatPercent(progress)),
                    StateText = Text.ToFixed64(
                        Text.FormatState(item.State, item.StateReason)),
                    Progress01 = progress
                });
            }
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

        private static bool TryReadPhysicalResources(
            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries,
            out BuildingRuntimeFactionUsableFuelSummary physicalResources)
        {
            for (int i = 0; i < summaries.Length; i++)
            {
                if (!FactionIdentity.IsPlayerControlled(summaries[i].FactionId))
                    continue;

                physicalResources = summaries[i];
                return true;
            }

            physicalResources = default;
            return false;
        }

    }
}
