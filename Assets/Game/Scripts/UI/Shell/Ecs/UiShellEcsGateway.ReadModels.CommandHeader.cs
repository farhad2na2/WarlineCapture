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
            string fuelText = null;
            bool showOil = false;
            bool oilVisibilityResolved = false;
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
                showOil = usableOilVisible;
                if (!showOil)
                    showOil = TryHasPlayerOilResourceSummary(entityManager, boundary);

                oilVisibilityResolved = true;
                if (TryReadCachedMatchHudHeader(
                        entityManager.World,
                        boundary,
                        component,
                        1,
                        usableFuelVersion,
                        usableOil,
                        usableFuel,
                        showOil,
                        out header))
                {
                    return true;
                }

                cacheHeader = true;
                resourceSource = 1;
                resourceVersion = usableFuelVersion;
                resourceOil = usableOil;
                resourceFuel = usableFuel;
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

            if (!oilVisibilityResolved && !showOil)
                showOil = TryHasPlayerOilResourceSummary(entityManager, boundary);

            if (cacheHeader)
            {
                bool canReuseCachedProjection = CanReuseCachedMatchHudHeaderProjection(
                    entityManager.World,
                    boundary,
                    resourceSource);
                header = new UiMatchHudHeaderModel(
                    ResolveMatchHudHeaderText(
                        component.OrderText,
                        cachedMatchHudHeaderComponent.OrderText,
                        cachedMatchHudHeader.OrderText,
                        canReuseCachedProjection),
                    ResolveMatchHudHeaderText(
                        component.SquadText,
                        cachedMatchHudHeaderComponent.SquadText,
                        cachedMatchHudHeader.SquadText,
                        canReuseCachedProjection),
                    ResolveMatchHudHeaderText(
                        component.CreditsText,
                        cachedMatchHudHeaderComponent.CreditsText,
                        cachedMatchHudHeader.CreditsText,
                        canReuseCachedProjection),
                    ResolveCompactResourceText(
                        resourceFuel,
                        cachedMatchHudHeaderFuel,
                        cachedMatchHudHeader.FuelText,
                        canReuseCachedProjection),
                    ResolveMatchHudHeaderText(
                        component.SupplyText,
                        cachedMatchHudHeaderComponent.SupplyText,
                        cachedMatchHudHeader.SupplyText,
                        canReuseCachedProjection),
                    ResolveMatchHudHeaderText(
                        component.CivilianRiskText,
                        cachedMatchHudHeaderComponent.CivilianRiskText,
                        cachedMatchHudHeader.CivilianRiskText,
                        canReuseCachedProjection),
                    ResolveCompactResourceText(
                        resourceOil,
                        cachedMatchHudHeaderOil,
                        cachedMatchHudHeader.OilText,
                        canReuseCachedProjection),
                    showOil);
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
            else
            {
                if (fuelText == null)
                    fuelText = component.FuelText.ToString();

                header = new UiMatchHudHeaderModel(
                    component.OrderText.ToString(),
                    component.SquadText.ToString(),
                    component.CreditsText.ToString(),
                    fuelText,
                    component.SupplyText.ToString(),
                    component.CivilianRiskText.ToString(),
                    oilText,
                    showOil);
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
                cachedMatchHudHeaderOil == oil &&
                cachedMatchHudHeaderFuel == fuel &&
                cachedMatchHudHeaderShowOil == showOil &&
                MatchHudHeaderComponentProjectionEquals(cachedMatchHudHeaderComponent, component))
            {
                cachedMatchHudHeaderResourceVersion = resourceVersion;
                cachedMatchHudHeaderComponent = component;
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

        private static bool CanReuseCachedMatchHudHeaderProjection(
            World world,
            Entity boundary,
            byte resourceSource)
        {
            return hasCachedMatchHudHeader &&
                   cachedMatchHudHeaderWorld == world &&
                   cachedMatchHudHeaderBoundary == boundary &&
                   cachedMatchHudHeaderResourceSource == resourceSource;
        }

        private static string ResolveMatchHudHeaderText(
            FixedString32Bytes value,
            FixedString32Bytes cachedValue,
            string cachedText,
            bool canReuseCachedProjection)
        {
            return canReuseCachedProjection &&
                   cachedText != null &&
                   value.Equals(cachedValue)
                ? cachedText
                : value.ToString();
        }

        private static string ResolveCompactResourceText(
            int value,
            int cachedValue,
            string cachedText,
            bool canReuseCachedProjection)
        {
            return canReuseCachedProjection &&
                   cachedText != null &&
                   value == cachedValue
                ? cachedText
                : FormatCompact(value);
        }

        private static bool MatchHudHeaderComponentProjectionEquals(
            in UiMatchHudHeaderComponent left,
            in UiMatchHudHeaderComponent right)
        {
            return left.OrderText.Equals(right.OrderText) &&
                   left.SquadText.Equals(right.SquadText) &&
                   left.CreditsText.Equals(right.CreditsText) &&
                   left.SupplyText.Equals(right.SupplyText) &&
                   left.CivilianRiskText.Equals(right.CivilianRiskText);
        }

        }
    }
}
