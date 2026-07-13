using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static partial class UiShellReadModelAdapter
        {
        public static bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState)
        {
            commandState = default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            TacticalCommandMode activeCommandMode = TacticalCommandMode.None;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                if (!hasSelectionInputQuery)
                {
                    selectionInputQuery =
                        world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RtsSelectionInputStateComponent>());
                    hasSelectionInputQuery = true;
                }

                if (!selectionInputQuery.IsEmptyIgnoreFilter)
                {
                    RtsSelectionInputStateComponent inputState =
                        selectionInputQuery.GetSingleton<RtsSelectionInputStateComponent>();
                    activeCommandMode = (TacticalCommandMode)inputState.ActiveCommandMode;
                }
            }

            bool buildDrawerVisible = false;
            if (entityManager.HasComponent<UiShellActivePopupComponent>(boundary))
            {
                UiShellActivePopupComponent activePopup =
                    entityManager.GetComponentData<UiShellActivePopupComponent>(boundary);
                buildDrawerVisible =
                    activePopup.Visible != 0 &&
                    activePopup.PopupKind == UiShellPopupKind.BuildDrawer;
            }

            commandState = new UiMatchHudCommandStateModel(activeCommandMode, buildDrawerVisible);
            return true;
        }

        public static bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer)
        {
            passengerDrawer = UiMatchHudPassengerDrawerModel.Hidden;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            if (!hasFocusedSelectionQuery)
            {
                focusedSelectionQuery =
                    world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<FocusedUnitUiReadModelComponent>());
                hasFocusedSelectionQuery = true;
            }

            if (focusedSelectionQuery.IsEmptyIgnoreFilter)
                return true;

            Entity focusedEntity = focusedSelectionQuery.GetSingletonEntity();
            FocusedUnitUiReadModelComponent selection =
                world.EntityManager.GetComponentData<FocusedUnitUiReadModelComponent>(focusedEntity);
            if (selection.HasFocusedUnit == 0 || selection.TransportPassengerCapacity <= 0)
                return true;

            bool drawerVisible = false;
            if (entityManager.HasComponent<UiMatchHudPassengerDrawerStateComponent>(boundary))
            {
                UiMatchHudPassengerDrawerStateComponent drawerState =
                    entityManager.GetComponentData<UiMatchHudPassengerDrawerStateComponent>(boundary);
                drawerVisible = drawerState.Visible != 0;
            }

            int passengerCount = Mathf.Max(0, selection.PassengerCount);
            int capacity = Mathf.Max(0, selection.TransportPassengerCapacity);
            UiMatchHudPassengerRowModel row0 = default;
            UiMatchHudPassengerRowModel row1 = default;
            UiMatchHudPassengerRowModel row2 = default;
            int rowCount = 0;

            if (world.EntityManager.HasBuffer<FocusedUnitPassengerUiReadModelElement>(focusedEntity))
            {
                DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers =
                    world.EntityManager.GetBuffer<FocusedUnitPassengerUiReadModelElement>(focusedEntity, true);
                int limit = Mathf.Min(passengers.Length, UiMatchHudPassengerDrawerModel.MaxRows);
                for (int i = 0; i < limit; i++)
                {
                    UiMatchHudPassengerRowModel row = ToPassengerRow(passengers[i]);
                    switch (i)
                    {
                        case 0:
                            row0 = row;
                            break;
                        case 1:
                            row1 = row;
                            break;
                        case 2:
                            row2 = row;
                            break;
                    }
                }

                rowCount = limit;
            }

            passengerDrawer = new UiMatchHudPassengerDrawerModel(
                true,
                drawerVisible,
                passengerCount,
                capacity,
                rowCount,
                row0,
                row1,
                row2);
            return true;
        }

        public static bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray)
        {
            squadTray = UiMatchHudSquadTrayModel.Default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            MatchHudSquadTraySlot selectedSlot = MatchHudSquadTraySlot.None;
            if (entityManager.HasComponent<UiMatchHudSquadTrayStateComponent>(boundary))
            {
                UiMatchHudSquadTrayStateComponent state =
                    entityManager.GetComponentData<UiMatchHudSquadTrayStateComponent>(boundary);
                selectedSlot = state.SelectedSlot;
            }

            UiMatchHudSquadTrayModel defaults = UiMatchHudSquadTrayModel.Default;
            squadTray = new UiMatchHudSquadTrayModel(
                selectedSlot,
                defaults.Card0,
                defaults.Card1,
                defaults.Card2,
                defaults.Card3,
                defaults.Card4);
            return true;
        }

        public static bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header)
        {
            header = UiMatchHudHeaderModel.Default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureMatchHudHeaderState(entityManager, boundary);
            UiMatchHudHeaderComponent component = entityManager.GetComponentData<UiMatchHudHeaderComponent>(boundary);
            string oilText = "0";
            string fuelText = component.FuelText.ToString();
            bool showOil = false;
            bool cacheHeader = false;
            byte resourceSource = 0;
            uint resourceVersion = 0u;
            int resourceOil = 0;
            int resourceFuel = 0;
            bool hasUsableFuelSummaryBuffer =
                entityManager.HasBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            if (TryReadPlayerUsableFuelSummary(
                    entityManager,
                    boundary,
                    out int usableOil,
                    out int usableFuel,
                    out bool usableOilVisible,
                    out uint usableFuelVersion))
            {
                if (TryReadCachedMatchHudHeader(
                        entityManager.World,
                        boundary,
                        component,
                        1,
                        usableFuelVersion,
                        usableOil,
                        usableFuel,
                        usableOilVisible,
                        out header))
                {
                    return true;
                }

                cacheHeader = true;
                resourceSource = 1;
                resourceVersion = usableFuelVersion;
                resourceOil = usableOil;
                resourceFuel = usableFuel;
                oilText = FormatCompact(usableOil);
                fuelText = FormatCompact(usableFuel);
                showOil = usableOilVisible;
            }
            else if (TryFormatLivePlayerResourceStorage(
                         entityManager,
                         out string liveOilText,
                         out string liveFuelText,
                         out bool liveOilVisible))
            {
                oilText = liveOilText;
                fuelText = liveFuelText;
                showOil = liveOilVisible;
            }
            else if (hasUsableFuelSummaryBuffer)
            {
                fuelText = "0";
            }
            else if (TryFormatPlayerResourceSummary(
                         entityManager,
                         boundary,
                         out string resourceOilText,
                         out string resourceFuelText,
                         out bool resourceOilVisible))
            {
                oilText = resourceOilText;
                fuelText = resourceFuelText;
                showOil = resourceOilVisible;
            }

            if (!showOil)
                showOil = TryHasPlayerOilResourceSummary(entityManager, boundary);

            header = new UiMatchHudHeaderModel(
                component.OrderText.ToString(),
                component.SquadText.ToString(),
                component.CreditsText.ToString(),
                fuelText,
                component.SupplyText.ToString(),
                component.CivilianRiskText.ToString(),
                oilText,
                showOil);
            if (cacheHeader)
            {
                CacheMatchHudHeader(
                    entityManager.World,
                    boundary,
                    component,
                    resourceSource,
                    resourceVersion,
                    resourceOil,
                    resourceFuel,
                    showOil,
                    header);
            }

            return true;
        }

        private static bool TryReadCachedMatchHudHeader(
            World world,
            Entity boundary,
            in UiMatchHudHeaderComponent component,
            byte resourceSource,
            uint resourceVersion,
            int oil,
            int fuel,
            bool showOil,
            out UiMatchHudHeaderModel header)
        {
            if (hasCachedMatchHudHeader &&
                cachedMatchHudHeaderWorld == world &&
                cachedMatchHudHeaderBoundary == boundary &&
                cachedMatchHudHeaderResourceSource == resourceSource &&
                cachedMatchHudHeaderResourceVersion == resourceVersion &&
                cachedMatchHudHeaderOil == oil &&
                cachedMatchHudHeaderFuel == fuel &&
                cachedMatchHudHeaderShowOil == showOil &&
                MatchHudHeaderComponentEquals(cachedMatchHudHeaderComponent, component))
            {
                header = cachedMatchHudHeader;
                return true;
            }

            header = default;
            return false;
        }

        private static void CacheMatchHudHeader(
            World world,
            Entity boundary,
            in UiMatchHudHeaderComponent component,
            byte resourceSource,
            uint resourceVersion,
            int oil,
            int fuel,
            bool showOil,
            in UiMatchHudHeaderModel header)
        {
            hasCachedMatchHudHeader = true;
            cachedMatchHudHeaderWorld = world;
            cachedMatchHudHeaderBoundary = boundary;
            cachedMatchHudHeaderComponent = component;
            cachedMatchHudHeaderResourceSource = resourceSource;
            cachedMatchHudHeaderResourceVersion = resourceVersion;
            cachedMatchHudHeaderOil = oil;
            cachedMatchHudHeaderFuel = fuel;
            cachedMatchHudHeaderShowOil = showOil;
            cachedMatchHudHeader = header;
        }

        private static bool MatchHudHeaderComponentEquals(
            in UiMatchHudHeaderComponent left,
            in UiMatchHudHeaderComponent right)
        {
            return left.ResourceVersion == right.ResourceVersion &&
                   left.OrderText.Equals(right.OrderText) &&
                   left.SquadText.Equals(right.SquadText) &&
                   left.CreditsText.Equals(right.CreditsText) &&
                   left.FuelText.Equals(right.FuelText) &&
                   left.SupplyText.Equals(right.SupplyText) &&
                   left.CivilianRiskText.Equals(right.CivilianRiskText);
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
            showOil = false;
            EnsureResourceStorageQuery(entityManager);
            if (resourceStorageQuery.IsEmptyIgnoreFilter)
                return false;

            float oil = 0f;
            float fuel = 0f;
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
                oil += Mathf.Max(0f, storage.StoredOilBarrels);
                fuel += Mathf.Max(0f, storage.StoredFuelBarrels);
                showOil |= storage.OilStorageCapacity > 0 || storage.StoredOilBarrels > 0.001f;
            }

            if (!foundPlayerStorage)
                return false;

            oilText = FormatCompact(Mathf.Max(0, Mathf.RoundToInt(oil)));
            fuelText = FormatCompact(Mathf.Max(0, Mathf.RoundToInt(fuel)));
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

                int oil = Mathf.Max(0, Mathf.RoundToInt(summary.StoredOilBarrels));
                int fuel = Mathf.Max(0, Mathf.RoundToInt(summary.StoredFuelBarrels));
                oilText = FormatCompact(oil);
                fuelText = FormatCompact(fuel);
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
