using System.Globalization;
using Game.Components;
using Game.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    internal static class UiResourceExchangeProjectionSystemHelper
    {
        internal static ResourceExchangeReason ValidateRecipe(
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

        internal static ResourceExchangeReason ValidateConfirm(
            in ResourceExchangeEnabledComponent enabled,
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials,
            in ResourceExchangeWalletComponent wallet,
            in BuildingRuntimeFactionUsableFuelSummary physicalResources,
            in ResourceExchangeRecipeComponent recipe,
            int inputAmount,
            int outputAmount,
            int activeQueueCount)
        {
            ResourceExchangeReason recipeReason = ValidateRecipe(enabled, recipe);
            if (recipeReason != ResourceExchangeReason.None)
                return recipeReason;

            int maxQueueItems = math.max(0, enabled.MaxQueueItems);
            if (maxQueueItems <= 0 || activeQueueCount >= maxQueueItems)
                return ResourceExchangeReason.QueueFull;

            if (ResourceExchangeResourceUtilitySystemHelper.GetAmount(
                    economy,
                    materials,
                    wallet,
                    physicalResources,
                    recipe.InputResource) < inputAmount)
                return InsufficientReason(recipe.InputResource);

            if (recipe.RequiresStorage != 0)
            {
                int capacity = ResourceExchangeResourceUtilitySystemHelper.GetCapacity(
                    materials,
                    wallet,
                    physicalResources,
                    recipe.OutputResource);
                if (capacity <= 0)
                    return ResourceExchangeReason.StorageMissing;

                int currentOutput = ResourceExchangeResourceUtilitySystemHelper.GetAmount(
                    economy,
                    materials,
                    wallet,
                    physicalResources,
                    recipe.OutputResource);
                if (currentOutput < 0 || outputAmount < 0 || outputAmount > capacity - currentOutput)
                    return ResourceExchangeReason.StorageFull;
            }

            return ResourceExchangeReason.None;
        }

        internal static int CalculateOutputAmount(in ResourceExchangeRecipeComponent recipe, int inputAmount)
        {
            float output = inputAmount * math.max(0f, recipe.OutputPerInput) *
                           (1f - math.clamp(recipe.FeePercent, 0f, 0.95f));
            return math.max(0, (int)math.floor(output));
        }

        internal static float CalculateDuration(in ResourceExchangeRecipeComponent recipe, int inputAmount)
        {
            int steps = math.max(0, (inputAmount - recipe.InputAmountMin) / math.max(1, recipe.InputStep));
            return math.max(0f, recipe.DurationSecondsBase + steps * recipe.DurationSecondsPerStep);
        }

        internal static int NormalizeInputAmount(in ResourceExchangeRecipeComponent recipe, int inputAmount)
        {
            int min = math.max(1, recipe.InputAmountMin);
            int max = math.max(min, recipe.InputAmountMax);
            int step = math.max(1, recipe.InputStep);
            int amount = inputAmount > 0 ? inputAmount : min;
            amount = math.clamp(amount, min, max);
            int completedSteps = (amount - min) / step;
            return math.clamp(min + completedSteps * step, min, max);
        }

        internal static ResourceExchangeRouteType ToRouteType(UiResourceExchangeTab tab)
        {
            return tab == UiResourceExchangeTab.Import
                ? ResourceExchangeRouteType.Import
                : ResourceExchangeRouteType.Export;
        }

        internal static UiResourceExchangeQueueState ToUiQueueState(ResourceExchangeQueueState state)
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

        internal static UiResourceExchangeDetailComponent EmptyDetail(UiResourceExchangeTab tab)
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

        internal static string FormatTab(UiResourceExchangeTab tab)
        {
            return tab == UiResourceExchangeTab.Import ? "IMPORT" : "EXPORT";
        }

        internal static string FormatResourceAmount(ResourceExchangeResourceKind resourceKind, int amount)
        {
            return $"{math.max(0, amount).ToString(CultureInfo.InvariantCulture)} {FormatResource(resourceKind)}";
        }

        internal static string FormatRate(in ResourceExchangeRecipeComponent recipe)
        {
            float effectiveRate = math.max(0f, recipe.OutputPerInput) *
                                  (1f - math.clamp(recipe.FeePercent, 0f, 0.95f));
            return $"1 {FormatResource(recipe.InputResource)} -> " +
                   $"{effectiveRate.ToString("0.##", CultureInfo.InvariantCulture)} {FormatResource(recipe.OutputResource)}";
        }

        internal static string FormatDuration(float seconds)
        {
            int totalSeconds = math.max(0, (int)math.ceil(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        internal static string FormatPercent(float progress01)
        {
            int percent = math.clamp((int)math.round(math.saturate(progress01) * 100f), 0, 100);
            return percent.ToString(CultureInfo.InvariantCulture) + "%";
        }

        internal static string FormatRequirements(in ResourceExchangeRecipeComponent recipe)
        {
            if (recipe.RequiresStorage != 0)
                return "Storage capacity required.";

            return recipe.RequiresTransportPlane != 0
                ? "Logistics transport required."
                : "No special requirements.";
        }

        internal static string FormatState(ResourceExchangeQueueState state, ResourceExchangeReason reason)
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

        internal static string FormatReason(ResourceExchangeReason reason)
        {
            switch (reason)
            {
                case ResourceExchangeReason.None:
                    return string.Empty;
                case ResourceExchangeReason.ExchangeUnavailable:
                    return "Exchange unavailable";
                case ResourceExchangeReason.RecipeLocked:
                    return "Route locked";
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

        internal static FixedString32Bytes ToFixed32(string value)
        {
            return new FixedString32Bytes(Trim(value, 28));
        }

        internal static FixedString64Bytes ToFixed64(string value)
        {
            return new FixedString64Bytes(Trim(value, 60));
        }

        internal static FixedString64Bytes ToFixed64(FixedString128Bytes value)
        {
            FixedString64Bytes result = default;
            result.Append(value);
            return result;
        }

        internal static FixedString128Bytes ToFixed128(string value)
        {
            return new FixedString128Bytes(Trim(value, 120));
        }

        private static string FormatResource(ResourceExchangeResourceKind resourceKind)
        {
            switch (resourceKind)
            {
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

        private static ResourceExchangeReason InsufficientReason(ResourceExchangeResourceKind resourceKind)
        {
            switch (resourceKind)
            {
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

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
