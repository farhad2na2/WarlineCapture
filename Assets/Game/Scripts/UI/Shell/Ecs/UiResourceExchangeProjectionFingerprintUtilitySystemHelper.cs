using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    public static class UiResourceExchangeProjectionFingerprintUtilitySystemHelper
    {
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public static ulong Calculate(
            in ResourceExchangeEnabledComponent enabled,
            in FactionEconomy economy, in FactionTacticalMaterialsComponent materials,
            in ResourceExchangeWalletComponent wallet, in BuildingRuntimeFactionUsableFuelSummary physical,
            in ResourceExchangeSummaryComponent summary,
            in UiResourceExchangeStateComponent uiState,
            DynamicBuffer<ResourceExchangeRecipeComponent> recipes,
            DynamicBuffer<ResourceExchangeQueueComponent> queue)
        {
            ulong hash = FnvOffset;
            Mix(ref hash, enabled.Enabled);
            Mix(ref hash, enabled.FactionId);
            Mix(ref hash, enabled.AllowRush);
            Mix(ref hash, enabled.MaxQueueItems);
            Mix(ref hash, enabled.ScenarioTag);
            Mix(ref hash, enabled.Version);

            Mix(ref hash, economy.Money);
            Mix(ref hash, materials.Current);
            Mix(ref hash, materials.Capacity);
            Mix(ref hash, Project(physical.StoredOilBarrels));
            Mix(ref hash, Project(physical.StoredFuelBarrels));
            Mix(ref hash, math.max(0, physical.OilStorageCapacity));
            Mix(ref hash, math.max(0, physical.FuelStorageCapacity));
            Mix(ref hash, wallet.RushTickets);
            Mix(ref hash, wallet.Version);

            Mix(ref hash, summary.ActiveCount);
            Mix(ref hash, summary.CompletedCount);
            Mix(ref hash, summary.Version);
            Mix(ref hash, (byte)uiState.ActiveTab);
            Mix(ref hash, uiState.SelectedRecipeSlot);
            Mix(ref hash, uiState.SelectedInputAmount);

            Mix(ref hash, recipes.Length);
            for (int i = 0; i < recipes.Length; i++)
                MixRecipe(ref hash, recipes[i]);

            Mix(ref hash, queue.Length);
            for (int i = 0; i < queue.Length; i++)
                MixQueueItem(ref hash, queue[i]);

            return hash;
        }

        public static ulong CalculateUnavailable(in UiResourceExchangeStateComponent uiState)
        {
            ulong hash = FnvOffset;
            Mix(ref hash, uint.MaxValue);
            Mix(ref hash, (byte)uiState.ActiveTab);
            return hash;
        }

        private static void MixRecipe(ref ulong hash, in ResourceExchangeRecipeComponent r)
        {
            Mix(ref hash, r.RecipeId);
            Mix(ref hash, r.DisplayName);
            Mix(ref hash, (byte)r.RouteType);
            Mix(ref hash, (byte)r.InputResource);
            Mix(ref hash, (byte)r.OutputResource);
            Mix(ref hash, r.InputAmountMin);
            Mix(ref hash, r.InputAmountMax);
            Mix(ref hash, r.InputStep);
            Mix(ref hash, r.OutputPerInput);
            Mix(ref hash, r.FeePercent);
            Mix(ref hash, r.DurationSecondsBase);
            Mix(ref hash, r.DurationSecondsPerStep);
            Mix(ref hash, r.RushTicketSecondsPerTicket);
            Mix(ref hash, r.MaxRushTickets);
            Mix(ref hash, r.RequiresStorage);
            Mix(ref hash, r.RequiresTransportPlane);
            Mix(ref hash, r.Enabled);
            Mix(ref hash, r.MissionTag);
            Mix(ref hash, (byte)r.DisabledReason);
            Mix(ref hash, r.SortOrder);
        }

        private static void MixQueueItem(ref ulong hash, in ResourceExchangeQueueComponent q)
        {
            Mix(ref hash, q.QueueItemId);
            Mix(ref hash, q.FactionId);
            Mix(ref hash, q.RecipeId);
            Mix(ref hash, (byte)q.InputResource);
            Mix(ref hash, (byte)q.OutputResource);
            Mix(ref hash, q.InputAmount);
            Mix(ref hash, q.OutputAmount);
            Mix(ref hash, (byte)q.State);
            Mix(ref hash, (byte)q.StateReason);
            Mix(ref hash, q.DurationSeconds);
            Mix(ref hash, math.max(0, (int)math.ceil(q.RemainingSeconds)));
            float progress = q.DurationSeconds <= 0f ? 1f : math.saturate(1f - q.RemainingSeconds / q.DurationSeconds);
            Mix(ref hash, math.clamp((int)math.round(progress * 100f), 0, 100));
            Mix(ref hash, q.RushTicketsSpent);
            Mix(ref hash, q.OutputApplied);
            Mix(ref hash, q.Version);
        }

        private static void Mix(ref ulong hash, FixedString64Bytes value)
        {
            Mix(ref hash, value.Length);
            for (int i = 0; i < value.Length; i++)
                Mix(ref hash, value[i]);
        }

        private static void Mix(ref ulong hash, FixedString128Bytes value)
        {
            Mix(ref hash, value.Length);
            for (int i = 0; i < value.Length; i++)
                Mix(ref hash, value[i]);
        }

        private static void Mix(ref ulong hash, float value) => Mix(ref hash, math.asuint(value));

        private static int Project(float value) => math.max(0, (int)math.floor(value));

        private static void Mix(ref ulong hash, int value) => Mix(ref hash, unchecked((uint)value));

        private static void Mix(ref ulong hash, byte value) => Mix(ref hash, (uint)value);

        private static void Mix(ref ulong hash, uint value)
        {
            unchecked
            {
                hash ^= value;
                hash *= Prime;
            }
        }
    }
}
