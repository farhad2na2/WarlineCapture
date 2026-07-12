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
        public static bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap)
        {
            minimap = UiMatchHudMinimapModel.Default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureMatchHudMinimapState(entityManager, boundary);
            UiMatchHudMinimapComponent component = entityManager.GetComponentData<UiMatchHudMinimapComponent>(boundary);
            UiMatchHudMinimapMarkerModel friendlyA = default;
            UiMatchHudMinimapMarkerModel friendlyB = default;
            UiMatchHudMinimapMarkerModel hostileA = default;
            UiMatchHudMinimapMarkerModel neutral = default;
            bool hasRuntimeMarkers = TryReadRuntimeMinimapMarkers(
                out friendlyA,
                out friendlyB,
                out hostileA,
                out neutral);

            minimap = new UiMatchHudMinimapModel(
                component.ViewportLeftPercent,
                component.ViewportTopPercent,
                component.ViewportWidthPercent,
                component.ViewportHeightPercent,
                component.ZoomInEnabled != 0,
                component.ZoomOutEnabled != 0,
                component.FocusEnabled != 0,
                hasRuntimeMarkers
                    ? friendlyA
                    : new UiMatchHudMinimapMarkerModel(false, component.FriendlyALeftPercent, component.FriendlyATopPercent),
                hasRuntimeMarkers
                    ? friendlyB
                    : new UiMatchHudMinimapMarkerModel(false, component.FriendlyBLeftPercent, component.FriendlyBTopPercent),
                hasRuntimeMarkers
                    ? hostileA
                    : new UiMatchHudMinimapMarkerModel(false, component.HostileALeftPercent, component.HostileATopPercent),
                hasRuntimeMarkers
                    ? neutral
                    : new UiMatchHudMinimapMarkerModel(false, component.CivilianLeftPercent, component.CivilianTopPercent));
            return true;
        }

        private static bool TryReadRuntimeMinimapMarkers(
            out UiMatchHudMinimapMarkerModel friendlyA,
            out UiMatchHudMinimapMarkerModel friendlyB,
            out UiMatchHudMinimapMarkerModel hostileA,
            out UiMatchHudMinimapMarkerModel neutral)
        {
            friendlyA = default;
            friendlyB = default;
            hostileA = default;
            neutral = default;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            EnsureMinimapMarkerQuery(entityManager);
            EnsureGridConfigQuery(entityManager);
            if (minimapMarkerQuery.IsEmptyIgnoreFilter || gridConfigQuery.IsEmptyIgnoreFilter)
                return false;

            Entity markerEntity = minimapMarkerQuery.GetSingletonEntity();
            DynamicBuffer<MatchHudMinimapMarkerElement> markers =
                entityManager.GetBuffer<MatchHudMinimapMarkerElement>(markerEntity, true);
            if (markers.Length == 0)
                return false;

            Entity gridEntity = gridConfigQuery.GetSingletonEntity();
            GridConfig grid = entityManager.GetComponentData<GridConfig>(gridEntity);
            bool hasFriendlyA = false;
            bool hasFriendlyB = false;
            bool hasHostileA = false;
            bool hasNeutral = false;
            for (int i = 0; i < markers.Length; i++)
            {
                MatchHudMinimapMarkerElement marker = markers[i];
                UiMatchHudMinimapMarkerModel model = ToMinimapMarkerModel(marker.Position, grid);
                if (FactionIdentity.IsPlayerControlled(marker.FactionId))
                {
                    if (!hasFriendlyA)
                    {
                        friendlyA = model;
                        hasFriendlyA = true;
                    }
                    else if (!hasFriendlyB)
                    {
                        friendlyB = model;
                        hasFriendlyB = true;
                    }
                }
                else if (FactionIdentity.IsHostileToPlayer(marker.FactionId))
                {
                    if (!hasHostileA)
                    {
                        hostileA = model;
                        hasHostileA = true;
                    }
                }
                else if (!hasNeutral)
                {
                    neutral = model;
                    hasNeutral = true;
                }

                if (hasFriendlyA && hasFriendlyB && hasHostileA && hasNeutral)
                    break;
            }

            return hasFriendlyA || hasFriendlyB || hasHostileA || hasNeutral;
        }

        private static void EnsureMinimapMarkerQuery(EntityManager entityManager)
        {
            if (hasMinimapMarkerQuery && cachedWorld == entityManager.World)
                return;

            minimapMarkerQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MatchHudMinimapMarkerStateComponent>(),
                ComponentType.ReadOnly<MatchHudMinimapMarkerElement>());
            hasMinimapMarkerQuery = true;
        }

        private static void EnsureGridConfigQuery(EntityManager entityManager)
        {
            if (hasGridConfigQuery && cachedWorld == entityManager.World)
                return;

            gridConfigQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            hasGridConfigQuery = true;
        }

        private static UiMatchHudMinimapMarkerModel ToMinimapMarkerModel(float3 worldPosition, GridConfig grid)
        {
            float width = math.max(1f, grid.Width * grid.CellSize);
            float height = math.max(1f, grid.Height * grid.CellSize);
            float left = math.saturate((worldPosition.x - grid.Origin.x) / width) * 100f;
            float top = (1f - math.saturate((worldPosition.z - grid.Origin.z) / height)) * 100f;
            return new UiMatchHudMinimapMarkerModel(true, left, top);
        }

        public static bool TryReadBuildDrawer(out UiBuildDrawerModel drawer)
        {
            drawer = UiBuildDrawerModel.Empty;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureBuildDrawerState(entityManager, boundary);
            UiBuildDrawerStateComponent drawerState =
                entityManager.GetComponentData<UiBuildDrawerStateComponent>(boundary);
            UiBuildDrawerDetailComponent detail = entityManager.GetComponentData<UiBuildDrawerDetailComponent>(boundary);
            UiBuildDrawerActiveProductionComponent active =
                entityManager.GetComponentData<UiBuildDrawerActiveProductionComponent>(boundary);
            DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog =
                entityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary, true);
            DynamicBuffer<UiBuildDrawerQueueRowComponent> queue =
                entityManager.GetBuffer<UiBuildDrawerQueueRowComponent>(boundary, true);

            UiBuildDrawerCatalogItemModel catalog0 = default;
            UiBuildDrawerCatalogItemModel catalog1 = default;
            UiBuildDrawerCatalogItemModel catalog2 = default;
            UiBuildDrawerCatalogItemModel catalog3 = default;
            UiBuildDrawerCatalogItemModel catalog4 = default;
            UiBuildDrawerCatalogItemModel catalog5 = default;
            UiBuildDrawerCatalogItemModel catalog6 = default;
            int catalogCount = Mathf.Min(catalog.Length, UiBuildDrawerModel.MaxCatalogItems);
            for (int i = 0; i < catalogCount; i++)
            {
                UiBuildDrawerCatalogItemModel item = ToBuildDrawerCatalogItem(catalog[i]);
                switch (i)
                {
                    case 0:
                        catalog0 = item;
                        break;
                    case 1:
                        catalog1 = item;
                        break;
                    case 2:
                        catalog2 = item;
                        break;
                    case 3:
                        catalog3 = item;
                        break;
                    case 4:
                        catalog4 = item;
                        break;
                    case 5:
                        catalog5 = item;
                        break;
                    case 6:
                        catalog6 = item;
                        break;
                }
            }

            UiBuildDrawerQueueRowModel queue0 = default;
            UiBuildDrawerQueueRowModel queue1 = default;
            int queueCount = Mathf.Min(queue.Length, UiBuildDrawerModel.MaxQueueRows);
            for (int i = 0; i < queueCount; i++)
            {
                UiBuildDrawerQueueRowModel row = ToBuildDrawerQueueRow(queue[i]);
                if (i == 0)
                    queue0 = row;
                else if (i == 1)
                    queue1 = row;
            }

            drawer = new UiBuildDrawerModel(
                detail.Name.ToString(),
                detail.Role.ToString(),
                detail.Description.ToString(),
                detail.FootprintText.ToString(),
                detail.RequirementsText.ToString(),
                detail.PlacementText.ToString(),
                detail.ProductionTimeText.ToString(),
                detail.CreditsCostText.ToString(),
                detail.SuppliesCostText.ToString(),
                detail.InstructionText.ToString(),
                detail.ProductionTitle.ToString(),
                detail.ProductionCountText.ToString(),
                detail.BuildEnabled != 0,
                detail.RushEnabled != 0,
                detail.ClearEnabled != 0,
                detail.NoProductionVisible != 0,
                new UiBuildDrawerActiveProductionModel(
                    active.Visible != 0,
                    active.CancelEnabled != 0,
                    ResolveBuildDrawerSprite(active.ThumbnailSpriteKey),
                    active.Name.ToString(),
                    active.PercentText.ToString(),
                    active.Progress01),
                ResolveBuildDrawerSprite(detail.PreviewSpriteKey),
                drawerState.ActiveCategory,
                drawerState.BuildingsCount,
                drawerState.VehiclesCount,
                drawerState.AircraftsCount,
                drawerState.SoldiersCount,
                drawerState.SelectedCatalogSlot,
                catalogCount,
                catalog0,
                catalog1,
                catalog2,
                catalog3,
                catalog4,
                catalog5,
                catalog6,
                queueCount,
                queue0,
                queue1);
            return true;
        }

        public static bool TryReadResourceExchange(out UiResourceExchangeModel exchange)
        {
            exchange = UiResourceExchangeModel.Empty;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureResourceExchangeUiState(entityManager, boundary);
            UiResourceExchangeStateComponent state =
                entityManager.GetComponentData<UiResourceExchangeStateComponent>(boundary);
            UiResourceExchangeDetailComponent detail =
                entityManager.GetComponentData<UiResourceExchangeDetailComponent>(boundary);
            DynamicBuffer<UiResourceExchangeRecipeCardComponent> cards =
                entityManager.GetBuffer<UiResourceExchangeRecipeCardComponent>(boundary, true);
            DynamicBuffer<UiResourceExchangeQueueRowComponent> queue =
                entityManager.GetBuffer<UiResourceExchangeQueueRowComponent>(boundary, true);

            UiResourceExchangeRecipeCardModel card0 = default;
            UiResourceExchangeRecipeCardModel card1 = default;
            UiResourceExchangeRecipeCardModel card2 = default;
            UiResourceExchangeRecipeCardModel card3 = default;
            UiResourceExchangeRecipeCardModel card4 = default;
            UiResourceExchangeRecipeCardModel card5 = default;
            UiResourceExchangeRecipeCardModel card6 = default;
            int cardCount = Mathf.Min(cards.Length, UiResourceExchangeModel.MaxRecipeCards);
            for (int i = 0; i < cardCount; i++)
            {
                UiResourceExchangeRecipeCardModel card = ToResourceExchangeRecipeCard(cards[i], i);
                switch (i)
                {
                    case 0:
                        card0 = card;
                        break;
                    case 1:
                        card1 = card;
                        break;
                    case 2:
                        card2 = card;
                        break;
                    case 3:
                        card3 = card;
                        break;
                    case 4:
                        card4 = card;
                        break;
                    case 5:
                        card5 = card;
                        break;
                    case 6:
                        card6 = card;
                        break;
                }
            }

            UiResourceExchangeQueueRowModel row0 = default;
            UiResourceExchangeQueueRowModel row1 = default;
            UiResourceExchangeQueueRowModel row2 = default;
            UiResourceExchangeQueueRowModel row3 = default;
            int rowCount = Mathf.Min(queue.Length, UiResourceExchangeModel.MaxQueueRows);
            for (int i = 0; i < rowCount; i++)
            {
                UiResourceExchangeQueueRowModel row = ToResourceExchangeQueueRow(queue[i], i);
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
                    case 3:
                        row3 = row;
                        break;
                }
            }

            exchange = new UiResourceExchangeModel(
                state.Version,
                state.ActiveTab == UiResourceExchangeTab.Import
                    ? UiResourceExchangeTabKind.Import
                    : UiResourceExchangeTabKind.Export,
                state.SelectedRecipeSlot,
                state.ExportRecipeCount,
                state.ImportRecipeCount,
                state.QueueCount,
                state.ActiveCount,
                state.CompletedCount,
                state.MaxQueueItems,
                state.QueueCapacityText.ToString(),
                state.CreditsText.ToString(),
                state.MaterialsText.ToString(),
                state.OilText.ToString(),
                state.FuelText.ToString(),
                state.RushTicketsText.ToString(),
                state.ExchangeEnabled != 0,
                state.RushAllEnabled != 0,
                state.ClearCompletedEnabled != 0,
                new UiResourceExchangeDetailModel(
                    detail.RecipeId.ToString(),
                    detail.Name.ToString(),
                    detail.RouteText.ToString(),
                    detail.RateText.ToString(),
                    detail.AmountText.ToString(),
                    detail.InputCostText.ToString(),
                    detail.OutputPreviewText.ToString(),
                    detail.DurationText.ToString(),
                    detail.RequirementsText.ToString(),
                    detail.InstructionText.ToString(),
                    detail.ConfirmEnabled != 0,
                    detail.WarningVisible != 0),
                cardCount,
                card0,
                card1,
                card2,
                card3,
                card4,
                card5,
                card6,
                rowCount,
                row0,
                row1,
                row2,
                row3);
            return true;
        }


        }
    }
}
