using System;
using System.Globalization;
using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    internal static class UiResourceExchangeTextProjectionUtilitySystemHelper
    {
        public static string FormatTab(UiResourceExchangeTab tab)
        {
            return tab == UiResourceExchangeTab.Import ? "IMPORT" : "EXPORT";
        }

        public static string FormatResourceAmount(ResourceExchangeResourceKind resourceKind, int amount)
        {
            return $"{math.max(0, amount).ToString(CultureInfo.InvariantCulture)} {FormatResource(resourceKind)}";
        }

        public static string FormatRate(in ResourceExchangeRecipeComponent recipe)
        {
            float effectiveRate = math.max(0f, recipe.OutputPerInput) * (1f - math.clamp(recipe.FeePercent, 0f, 0.95f));
            return $"1 {FormatResource(recipe.InputResource)} -> {effectiveRate.ToString("0.##", CultureInfo.InvariantCulture)} {FormatResource(recipe.OutputResource)}";
        }

        public static string FormatDuration(float seconds)
        {
            int totalSeconds = math.max(0, (int)math.ceil(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        public static string FormatPercent(float progress01)
        {
            int percent = math.clamp((int)math.round(math.saturate(progress01) * 100f), 0, 100);
            return percent.ToString(CultureInfo.InvariantCulture) + "%";
        }

        public static string FormatRequirements(in ResourceExchangeRecipeComponent recipe)
        {
            if (recipe.RequiresStorage != 0)
                return "Storage capacity required.";

            return recipe.RequiresTransportPlane != 0
                ? "Logistics transport required."
                : "No special requirements.";
        }

        public static string FormatState(ResourceExchangeQueueState state, ResourceExchangeReason reason)
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

        public static string FormatReason(ResourceExchangeReason reason)
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

        public static FixedString32Bytes ToFixed32(string value)
        {
            return new FixedString32Bytes(Trim(value, 28));
        }

        public static FixedString64Bytes ToFixed64(string value)
        {
            return new FixedString64Bytes(Trim(value, 60));
        }

        public static FixedString64Bytes ToFixed64(FixedString128Bytes value)
        {
            FixedString64Bytes result = default;
            result.Append(value);
            return result;
        }

        public static FixedString128Bytes ToFixed128(string value)
        {
            return new FixedString128Bytes(Trim(value, 120));
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

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
