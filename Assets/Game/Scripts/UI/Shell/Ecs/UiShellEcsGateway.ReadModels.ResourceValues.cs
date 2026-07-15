using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        public static bool TryReadMatchHudResourceValues(out UiMatchHudResourceValuesModel values) =>
            UiShellReadModelAdapter.TryReadMatchHudResourceValues(out values);

        private static partial class UiShellReadModelAdapter
        {
            public static bool TryReadMatchHudResourceValues(out UiMatchHudResourceValuesModel values)
            {
                values = UiMatchHudResourceValuesModel.Invalid;
                if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                    return false;

                EnsureMatchHudHeaderState(entityManager, boundary);
                bool hasUsableFuelSummaryBuffer =
                    entityManager.HasBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
                if (TryReadPlayerUsableFuelSummary(
                        entityManager,
                        boundary,
                        out int usableOil,
                        out int usableFuel,
                        out bool usableOilVisible,
                        out _))
                {
                    bool showOil = usableOilVisible ||
                                   TryHasPlayerOilResourceSummary(entityManager, boundary);
                    values = UiMatchHudResourceValuesModel.FromValues(usableOil, usableFuel, showOil);
                    return true;
                }

                if (TryReadLivePlayerResourceStorage(
                        entityManager,
                        out int liveOil,
                        out int liveFuel,
                        out bool liveOilVisible))
                {
                    bool showOil = liveOilVisible ||
                                   TryHasPlayerOilResourceSummary(entityManager, boundary);
                    values = UiMatchHudResourceValuesModel.FromValues(liveOil, liveFuel, showOil);
                    return true;
                }

                if (hasUsableFuelSummaryBuffer)
                {
                    values = UiMatchHudResourceValuesModel.FromValues(
                        0,
                        0,
                        TryHasPlayerOilResourceSummary(entityManager, boundary));
                    return true;
                }

                if (TryReadPlayerResourceSummaryValues(
                        entityManager,
                        boundary,
                        out int summaryOil,
                        out int summaryFuel,
                        out bool summaryOilVisible))
                {
                    values = UiMatchHudResourceValuesModel.FromValues(
                        summaryOil,
                        summaryFuel,
                        summaryOilVisible);
                    return true;
                }

                values = UiMatchHudResourceValuesModel.TextFallback(
                    TryHasPlayerOilResourceSummary(entityManager, boundary));
                return true;
            }

            private static bool TryReadPlayerUsableFuelSummary(
                EntityManager entityManager,
                Entity boundary,
                out int oil,
                out int fuel,
                out bool showOil,
                out uint version)
            {
                oil = 0;
                fuel = 0;
                showOil = false;
                version = 0u;
                if (!entityManager.HasBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary))
                    return false;

                DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                    entityManager.GetBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary, true);
                for (int i = 0; i < summaries.Length; i++)
                {
                    BuildingRuntimeFactionUsableFuelSummary summary = summaries[i];
                    if (!FactionIdentity.IsPlayerControlled(summary.FactionId))
                        continue;

                    oil = Mathf.Max(0, Mathf.RoundToInt(summary.StoredOilBarrels));
                    fuel = Mathf.Max(0, Mathf.RoundToInt(summary.StoredFuelBarrels));
                    showOil = summary.OilStorageCapacity > 0 || summary.StoredOilBarrels > 0.001f;
                    version = summary.Version;
                    return true;
                }

                return false;
            }

            private static bool TryFormatLivePlayerResourceStorage(
                EntityManager entityManager,
                out string oilText,
                out string fuelText,
                out bool showOil)
            {
                oilText = string.Empty;
                fuelText = string.Empty;
                if (!TryReadLivePlayerResourceStorage(
                        entityManager,
                        out int oil,
                        out int fuel,
                        out showOil))
                {
                    return false;
                }

                oilText = FormatCompact(oil);
                fuelText = FormatCompact(fuel);
                return true;
            }

            private static bool TryReadLivePlayerResourceStorage(
                EntityManager entityManager,
                out int oil,
                out int fuel,
                out bool showOil)
            {
                oil = 0;
                fuel = 0;
                showOil = false;
                EnsureResourceStorageQuery(entityManager);
                if (resourceStorageQuery.IsEmptyIgnoreFilter)
                    return false;

                float accumulatedOil = 0f;
                float accumulatedFuel = 0f;
                bool foundPlayerStorage = false;
                using NativeArray<BuildingResourceStorageComponent> storages =
                    resourceStorageQuery.ToComponentDataArray<BuildingResourceStorageComponent>(Allocator.Temp);
                using NativeArray<Faction> factions =
                    resourceStorageQuery.ToComponentDataArray<Faction>(Allocator.Temp);
                int count = math.min(storages.Length, factions.Length);
                for (int i = 0; i < count; i++)
                {
                    if (!FactionIdentity.IsPlayerControlled(factions[i].Id))
                        continue;

                    BuildingResourceStorageComponent storage = storages[i];
                    if (!IsUsableHeaderResourceStorage(storage))
                        continue;

                    foundPlayerStorage = true;
                    accumulatedOil += Mathf.Max(0f, storage.StoredOilBarrels);
                    accumulatedFuel += Mathf.Max(0f, storage.StoredFuelBarrels);
                    showOil |= storage.OilStorageCapacity > 0 || storage.StoredOilBarrels > 0.001f;
                }

                if (!foundPlayerStorage)
                    return false;

                oil = Mathf.Max(0, Mathf.RoundToInt(accumulatedOil));
                fuel = Mathf.Max(0, Mathf.RoundToInt(accumulatedFuel));
                return true;
            }

            private static bool IsUsableHeaderResourceStorage(in BuildingResourceStorageComponent storage)
            {
                bool hasStorage = storage.OilStorageCapacity > 0 || storage.FuelStorageCapacity > 0;
                bool producesResource = storage.OilBarrelsPerDay > 0f || storage.FuelBarrelsPerDay > 0f;
                return hasStorage && !producesResource;
            }

            private static bool TryFormatPlayerResourceSummary(
                EntityManager entityManager,
                Entity boundary,
                out string oilText,
                out string fuelText,
                out bool showOil)
            {
                oilText = string.Empty;
                fuelText = string.Empty;
                if (!TryReadPlayerResourceSummaryValues(
                        entityManager,
                        boundary,
                        out int oil,
                        out int fuel,
                        out showOil))
                {
                    return false;
                }

                oilText = FormatCompact(oil);
                fuelText = FormatCompact(fuel);
                return true;
            }

            private static bool TryReadPlayerResourceSummaryValues(
                EntityManager entityManager,
                Entity boundary,
                out int oil,
                out int fuel,
                out bool showOil)
            {
                oil = 0;
                fuel = 0;
                showOil = false;
                if (!entityManager.HasBuffer<BuildingRuntimeFactionSummary>(boundary))
                    return false;

                DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
                    entityManager.GetBuffer<BuildingRuntimeFactionSummary>(boundary, true);
                for (int i = 0; i < summaries.Length; i++)
                {
                    BuildingRuntimeFactionSummary summary = summaries[i];
                    if (!FactionIdentity.IsPlayerControlled(summary.FactionId))
                        continue;

                    oil = Mathf.Max(0, Mathf.RoundToInt(summary.StoredOilBarrels));
                    fuel = Mathf.Max(0, Mathf.RoundToInt(summary.StoredFuelBarrels));
                    showOil = oil > 0 || summary.OilBarrelsPerDay > 0f;
                    return true;
                }

                return false;
            }

            private static bool TryHasPlayerOilResourceSummary(EntityManager entityManager, Entity boundary)
            {
                if (!entityManager.HasBuffer<BuildingRuntimeFactionSummary>(boundary))
                    return false;

                DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
                    entityManager.GetBuffer<BuildingRuntimeFactionSummary>(boundary, true);
                for (int i = 0; i < summaries.Length; i++)
                {
                    BuildingRuntimeFactionSummary summary = summaries[i];
                    if (!FactionIdentity.IsPlayerControlled(summary.FactionId))
                        continue;

                    return summary.StoredOilBarrels > 0.001f || summary.OilBarrelsPerDay > 0f;
                }

                return false;
            }

            private static void EnsureResourceStorageQuery(EntityManager entityManager)
            {
                if (hasResourceStorageQuery && cachedWorld == entityManager.World)
                    return;

                resourceStorageQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<BuildingResourceStorageComponent>(),
                    ComponentType.ReadOnly<Faction>());
                hasResourceStorageQuery = true;
            }

            private static string FormatCompact(int value)
            {
                if (value >= 1000000)
                    return $"{value / 1000000f:0.#}M";
                if (value >= 10000)
                    return $"{value / 1000f:0.#}K";
                return value.ToString();
            }
        }
    }
}
