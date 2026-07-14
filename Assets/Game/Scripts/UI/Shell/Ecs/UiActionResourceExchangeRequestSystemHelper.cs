using Unity.Entities;
using Unity.Mathematics;
using Game.Components;
using Game.Runtime;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    internal static class UiActionResourceExchangeRequestSystemHelper
    {
        internal static void AdjustAmount(
            EntityManager entityManager,
            Entity exchangeEntity,
            bool hasExchangeEntity,
            ref UiResourceExchangeStateComponent state,
            bool hasState,
            int direction)
        {
            if (!hasState ||
                !hasExchangeEntity ||
                !TryResolveSelectedRecipe(entityManager, exchangeEntity, state, out ResourceExchangeRecipeComponent recipe))
            {
                return;
            }

            int current = NormalizeInputAmount(recipe, state.SelectedInputAmount);
            int step = math.max(1, recipe.InputStep);
            int next = current + (direction >= 0 ? step : -step);
            state.SelectedInputAmount = NormalizeInputAmount(recipe, next);
            state.Version++;
        }

        internal static void EnqueueConfirm(
            EntityManager entityManager,
            Entity exchangeEntity,
            in ResourceExchangeEnabledComponent enabled,
            bool hasExchangeEntity,
            in UiResourceExchangeStateComponent state,
            bool hasState,
            int frame)
        {
            if (!hasState ||
                !hasExchangeEntity ||
                !TryResolveSelectedRecipe(entityManager, exchangeEntity, state, out ResourceExchangeRecipeComponent recipe))
            {
                return;
            }

            int amount = NormalizeInputAmount(recipe, state.SelectedInputAmount);
            ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
                entityManager,
                exchangeEntity,
                recipe.RecipeId,
                amount,
                enabled.FactionId,
                frame);
        }

        private static bool TryResolveSelectedRecipe(
            EntityManager entityManager,
            Entity exchangeEntity,
            in UiResourceExchangeStateComponent state,
            out ResourceExchangeRecipeComponent recipe)
        {
            recipe = default;
            if (exchangeEntity == Entity.Null ||
                !entityManager.HasBuffer<ResourceExchangeRecipeComponent>(exchangeEntity))
            {
                return false;
            }

            ResourceExchangeRouteType routeType = ToRouteType(state.ActiveTab);
            int selectedSlot = math.max(0, state.SelectedRecipeSlot);
            int visibleIndex = 0;
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes =
                entityManager.GetBuffer<ResourceExchangeRecipeComponent>(exchangeEntity, true);
            for (int i = 0; i < recipes.Length; i++)
            {
                ResourceExchangeRecipeComponent candidate = recipes[i];
                if (candidate.RouteType != routeType)
                    continue;

                if (visibleIndex == selectedSlot)
                {
                    recipe = candidate;
                    return true;
                }

                visibleIndex++;
            }

            return false;
        }

        private static int NormalizeInputAmount(in ResourceExchangeRecipeComponent recipe, int inputAmount)
        {
            int min = math.max(1, recipe.InputAmountMin);
            int max = math.max(min, recipe.InputAmountMax);
            int step = math.max(1, recipe.InputStep);
            int amount = inputAmount > 0 ? inputAmount : min;
            amount = math.clamp(amount, min, max);
            int completedSteps = (amount - min) / step;
            return math.clamp(min + completedSteps * step, min, max);
        }

        private static ResourceExchangeRouteType ToRouteType(UiResourceExchangeTab tab)
        {
            return tab == UiResourceExchangeTab.Import
                ? ResourceExchangeRouteType.Import
                : ResourceExchangeRouteType.Export;
        }
    }
}
