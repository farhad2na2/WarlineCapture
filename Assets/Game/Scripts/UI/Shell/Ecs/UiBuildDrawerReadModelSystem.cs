using System.Collections.Generic;
using System.Globalization;
using Game.Catalog.Contracts;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Runtime;
using static Game.UI.Shell.Ecs.UiBuildDrawerProjectionSystemHelper;

namespace Game.UI.Shell.Ecs
{
    public static class UiBuildDrawerReadModelSource
    {
        private static readonly BuildDrawerCatalogQueryUiSystemHelper CatalogQuery = new();
        private static readonly List<BuildDrawerCatalogItem> CatalogItems = new();
        private static readonly List<BuildDrawerCatalogItem> CountScratch = new();
        private static readonly List<BuildingPendingProductionUiEntry> PendingProductions = new();
        private static readonly List<BuildingPendingProductionUiEntry> ClearProductionScratch = new();
        private static readonly Dictionary<string, Sprite> SpriteByKey = new();

        private static ICatalogPrefabSource unitPrefabSource;
        private static ICatalogPrefabSource buildingPrefabSource;
        private static IBuildingUiCommand buildingUiCommand;
        private static IBuildingUiQuery buildingUiQuery;

        public static bool HasCatalogSources => unitPrefabSource != null || buildingPrefabSource != null;
        public static IBuildingUiCommand BuildingUiCommand => buildingUiCommand;

        public static void Configure(
            ICatalogPrefabSource configuredUnitPrefabSource,
            ICatalogPrefabSource configuredBuildingPrefabSource,
            IBuildingUiCommand configuredBuildingUiCommand,
            IBuildingUiQuery configuredBuildingUiQuery,
            TryResolveUiBuildingCatalogMetadata tryResolveBuildingMetadata,
            TryResolveUiUnitCatalogMetadata tryResolveUnitMetadata)
        {
            unitPrefabSource = configuredUnitPrefabSource;
            buildingPrefabSource = configuredBuildingPrefabSource;
            buildingUiCommand = configuredBuildingUiCommand;
            buildingUiQuery = configuredBuildingUiQuery;
            SpriteByKey.Clear();
            CatalogQuery.ConfigureMetadataResolvers(tryResolveBuildingMetadata, tryResolveUnitMetadata);
        }

        public static void Clear()
        {
            unitPrefabSource = null;
            buildingPrefabSource = null;
            buildingUiCommand = null;
            buildingUiQuery = null;
            CatalogItems.Clear();
            CountScratch.Clear();
            PendingProductions.Clear();
            ClearProductionScratch.Clear();
            SpriteByKey.Clear();
        }

        public static Sprite ResolveSprite(string spriteKey)
        {
            return !string.IsNullOrWhiteSpace(spriteKey) && SpriteByKey.TryGetValue(spriteKey, out Sprite sprite)
                ? sprite
                : null;
        }

        public static void ProcessPrimaryRequest(BuildDrawerCategory category, int selectedSlot)
        {
            if (buildingUiCommand == null || !TryGetCatalogItem(category, selectedSlot, out BuildDrawerCatalogItem item))
                return;

            buildingUiCommand.TryRequestCampItem(item.Prefab, item.Price, out _, false);
        }

        public static void ProcessProductionRequest(UiBuildProductionActionKind actionKind, int queueSlot)
        {
            if (buildingUiCommand == null || buildingUiQuery == null)
                return;

            RefreshPendingProductions();
            switch (actionKind)
            {
                case UiBuildProductionActionKind.CancelActive:
                    CancelPendingProductionAt(0);
                    break;
                case UiBuildProductionActionKind.CancelQueued:
                    CancelPendingProductionAt(queueSlot + 1);
                    break;
                case UiBuildProductionActionKind.Clear:
                    ClearProductionScratch.Clear();
                    ClearProductionScratch.AddRange(PendingProductions);
                    ClearProductionScratch.Sort(CompareProductionCancelOrder);
                    for (int i = 0; i < ClearProductionScratch.Count; i++)
                    {
                        BuildingPendingProductionUiEntry entry = ClearProductionScratch[i];
                        if (entry.PendingProductionIndex >= 0)
                            buildingUiCommand.CancelProduction(entry.BuildingId, entry.PendingProductionIndex);
                    }
                    ClearProductionScratch.Clear();
                    break;
            }
        }

        public static void WriteReadModel(
            EntityManager entityManager,
            Entity boundary,
            ref UiBuildDrawerStateComponent drawerState,
            DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog,
            DynamicBuffer<UiBuildDrawerQueueRowComponent> queue)
        {
            if (!HasCatalogSources)
                return;

            RefreshPendingProductions();
            drawerState.BuildingsCount = CountCategory(BuildDrawerCategory.Buildings);
            drawerState.VehiclesCount = CountCategory(BuildDrawerCategory.Vehicles);
            drawerState.AircraftsCount = CountCategory(BuildDrawerCategory.Aircrafts);
            drawerState.SoldiersCount = CountCategory(BuildDrawerCategory.Soldiers);

            CatalogQuery.Collect(unitPrefabSource, buildingPrefabSource, drawerState.ActiveCategory, CatalogItems);
            int visibleCatalogCount = Mathf.Min(CatalogItems.Count, UiBuildDrawerModel.MaxCatalogItems);
            if (visibleCatalogCount == 0)
            {
                drawerState.SelectedCatalogSlot = 0;
                catalog.Clear();
                entityManager.SetComponentData(boundary, EmptyDetail(drawerState.ActiveCategory));
            }
            else
            {
                drawerState.SelectedCatalogSlot = Mathf.Clamp(drawerState.SelectedCatalogSlot, 0, visibleCatalogCount - 1);
                BuildDrawerCatalogItem selected = CatalogItems[drawerState.SelectedCatalogSlot];
                entityManager.SetComponentData(boundary, BuildDetail(selected));
                WriteCatalogBuffer(catalog, visibleCatalogCount, drawerState.SelectedCatalogSlot);
            }

            WriteQueueReadModel(entityManager, boundary, queue);
        }

        private static bool TryGetCatalogItem(
            BuildDrawerCategory category,
            int selectedSlot,
            out BuildDrawerCatalogItem item)
        {
            item = default;
            if (!HasCatalogSources)
                return false;

            CatalogQuery.Collect(unitPrefabSource, buildingPrefabSource, category, CatalogItems);
            int visibleCatalogCount = Mathf.Min(CatalogItems.Count, UiBuildDrawerModel.MaxCatalogItems);
            if (visibleCatalogCount == 0)
                return false;

            item = CatalogItems[Mathf.Clamp(selectedSlot, 0, visibleCatalogCount - 1)];
            return item.Prefab != null;
        }

        private static int CountCategory(BuildDrawerCategory category)
        {
            CatalogQuery.Collect(unitPrefabSource, buildingPrefabSource, category, CountScratch);
            int count = CountScratch.Count;
            CountScratch.Clear();
            return count;
        }

        private static void WriteCatalogBuffer(
            DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog,
            int visibleCatalogCount,
            int selectedSlot)
        {
            catalog.Clear();
            for (int i = 0; i < visibleCatalogCount; i++)
            {
                BuildDrawerCatalogItem item = CatalogItems[i];
                BuildingUiCommandFailure failure = GetCampRequestFailure(item, out _);
                catalog.Add(new UiBuildDrawerCatalogItemComponent
                {
                    Visible = 1,
                    Enabled = failure == BuildingUiCommandFailure.None ? (byte)1 : (byte)0,
                    Selected = i == selectedSlot ? (byte)1 : (byte)0,
                    DisabledReason = failure,
                    Category = item.Category,
                    ThumbnailSpriteKey = ToSpriteKey(item.CardPortrait),
                    Title = ToFixed64(item.DisplayName),
                    Role = ToFixed32(item.TypeLabel),
                    CreditsText = ToFixed32(FormatCost(item.Price)),
                    SuppliesText = ToFixed32(FormatMaterialsCost(item.MaterialsCost)),
                    TimeText = ToFixed32(FormatDuration(item))
                });
            }
        }

        private static UiBuildDrawerDetailComponent BuildDetail(BuildDrawerCatalogItem item)
        {
            BuildingUiCommandFailure failure = GetCampRequestFailure(item, out string requiredBuildingDisplayName);

            bool canBuild = buildingUiCommand != null && failure == BuildingUiCommandFailure.None;
            string instruction = canBuild
                ? FormatReadyInstruction(item)
                : FormatInstructionFailureMessage(item, failure, requiredBuildingDisplayName, buildingUiCommand);

            if (canBuild &&
                item.Category == BuildDrawerCategory.Buildings &&
                buildingUiCommand.HasPendingBuildingPlacement)
            {
                bool canConfirm = buildingUiCommand.CanConfirmBuildingPlacement;
                instruction = canConfirm
                    ? GameText.Format("build.drawer.instruction.place_pending_confirm", "Place {0}: drag to position, then confirm.", item.DisplayName)
                    : GameText.Format("build.drawer.instruction.cannot_place_here", "Cannot place here: {0}.", FormatPlacementStatus(buildingUiCommand.PlacementStatusText));
            }

            return new UiBuildDrawerDetailComponent
            {
                Name = ToFixed64(item.DisplayName),
                Role = ToFixed32(item.TypeLabel),
                PreviewSpriteKey = ToSpriteKey(item.ActionPortrait),
                Description = ToFixed128(item.Description),
                FootprintText = ToFixed32(FormatFootprint(item)),
                RequirementsText = ToFixed64(FormatRequirements(item)),
                PlacementText = ToFixed64(FormatPlacement(item)),
                ProductionTimeText = ToFixed32(FormatDuration(item)),
                CreditsCostText = ToFixed32(FormatCost(item.Price)),
                SuppliesCostText = ToFixed32(FormatMaterialsCost(item.MaterialsCost)),
                InstructionText = ToFixed128(instruction),
                ProductionTitle = ToFixed32(GameText.Get("build.drawer.production.title", "PRODUCTION")),
                ProductionCountText = ToFixed32(PendingProductions.Count.ToString(CultureInfo.InvariantCulture)),
                DisabledReason = failure,
                BuildEnabled = canBuild ? (byte)1 : (byte)0,
                RushEnabled = 0,
                ClearEnabled = buildingUiCommand != null && PendingProductions.Count > 0 ? (byte)1 : (byte)0,
                NoProductionVisible = PendingProductions.Count == 0 ? (byte)1 : (byte)0
            };
        }

        private static UiBuildDrawerDetailComponent EmptyDetail(BuildDrawerCategory category)
        {
            return new UiBuildDrawerDetailComponent
            {
                Name = ToFixed64(GameText.Get("build.drawer.empty.name", "NO ITEMS")),
                Role = ToFixed32(BuildDrawerCategoryFormatter.Format(category)),
                Description = ToFixed128(FormatEmptyCategoryInstruction(category)),
                FootprintText = default,
                RequirementsText = default,
                PlacementText = default,
                ProductionTimeText = default,
                CreditsCostText = default,
                SuppliesCostText = default,
                InstructionText = ToFixed128(FormatEmptyCategoryInstruction(category)),
                ProductionTitle = ToFixed32(GameText.Get("build.drawer.production.title", "PRODUCTION")),
                ProductionCountText = ToFixed32(PendingProductions.Count.ToString(CultureInfo.InvariantCulture)),
                DisabledReason = BuildingUiCommandFailure.InvalidSelection,
                BuildEnabled = 0,
                RushEnabled = 0,
                ClearEnabled = buildingUiCommand != null && PendingProductions.Count > 0 ? (byte)1 : (byte)0,
                NoProductionVisible = PendingProductions.Count == 0 ? (byte)1 : (byte)0
            };
        }

        private static void WriteQueueReadModel(
            EntityManager entityManager,
            Entity boundary,
            DynamicBuffer<UiBuildDrawerQueueRowComponent> queue)
        {
            if (PendingProductions.Count == 0)
            {
                queue.Clear();
                entityManager.SetComponentData(boundary, new UiBuildDrawerActiveProductionComponent());
                return;
            }

            BuildingPendingProductionUiEntry active = PendingProductions[0];
            entityManager.SetComponentData(boundary, new UiBuildDrawerActiveProductionComponent
            {
                Visible = 1,
                CancelEnabled = buildingUiCommand != null && active.PendingProductionIndex >= 0 ? (byte)1 : (byte)0,
                ThumbnailSpriteKey = ToSpriteKey(ResolveQueueThumbnail(active)),
                Name = ToFixed64(ResolveQueueDisplayName(active)),
                PercentText = ToFixed32(FormatPercent(active.Progress01)),
                Progress01 = Mathf.Clamp01(active.Progress01)
            });

            queue.Clear();
            int queuedCount = Mathf.Min(Mathf.Max(0, PendingProductions.Count - 1), UiBuildDrawerModel.MaxQueueRows);
            for (int i = 0; i < queuedCount; i++)
            {
                BuildingPendingProductionUiEntry entry = PendingProductions[i + 1];
                queue.Add(new UiBuildDrawerQueueRowComponent
                {
                    Visible = 1,
                    ActionEnabled = buildingUiCommand != null && entry.PendingProductionIndex >= 0 ? (byte)1 : (byte)0,
                    ThumbnailSpriteKey = ToSpriteKey(ResolveQueueThumbnail(entry)),
                    NumberText = ToFixed32((i + 2).ToString(CultureInfo.InvariantCulture)),
                    Name = ToFixed64(ResolveQueueDisplayName(entry)),
                    TimeText = ToFixed32(FormatRemaining(entry.RemainingSeconds))
                });
            }
        }

        private static void RefreshPendingProductions()
        {
            PendingProductions.Clear();
            buildingUiQuery?.GetFriendlyPendingProductionUiEntries(PendingProductions);
        }

        private static void CancelPendingProductionAt(int index)
        {
            if (index < 0 || index >= PendingProductions.Count)
                return;

            BuildingPendingProductionUiEntry entry = PendingProductions[index];
            if (entry.PendingProductionIndex >= 0)
                buildingUiCommand.CancelProduction(entry.BuildingId, entry.PendingProductionIndex);
        }

        private static string ResolveQueueDisplayName(BuildingPendingProductionUiEntry entry)
        {
            return CatalogQuery.TryResolvePrefab(unitPrefabSource, buildingPrefabSource, entry.Prefab, out BuildDrawerCatalogItem item)
                ? item.DisplayName
                : entry.Prefab != null ? entry.Prefab.name : GameText.Get("build.drawer.production.fallback_name", "Production");
        }

        private static Sprite ResolveQueueThumbnail(BuildingPendingProductionUiEntry entry)
        {
            return CatalogQuery.TryResolvePrefab(unitPrefabSource, buildingPrefabSource, entry.Prefab, out BuildDrawerCatalogItem item)
                ? item.CardPortrait
                : null;
        }

        private static BuildingUiCommandFailure GetCampRequestFailure(
            BuildDrawerCatalogItem item,
            out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            return buildingUiCommand != null
                ? buildingUiCommand.GetCampRequestFailure(item.Prefab, item.Price, out requiredBuildingDisplayName)
                : BuildingUiCommandFailure.InvalidSelection;
        }

        private static int CompareProductionCancelOrder(
            BuildingPendingProductionUiEntry left,
            BuildingPendingProductionUiEntry right)
        {
            int buildingComparison = left.BuildingId.CompareTo(right.BuildingId);
            return buildingComparison != 0
                ? buildingComparison
                : right.PendingProductionIndex.CompareTo(left.PendingProductionIndex);
        }

        private static FixedString64Bytes ToSpriteKey(Sprite sprite)
        {
            if (sprite == null)
                return default;

            string key = sprite.GetEntityId().ToString();
            SpriteByKey[key] = sprite;
            return new FixedString64Bytes(key);
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct UiBuildDrawerReadModelSystem : ISystem
    {
        private EntityQuery boundaryQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadWrite<UiBuildDrawerStateComponent>(),
                ComponentType.ReadWrite<UiBuildDrawerDetailComponent>(),
                ComponentType.ReadWrite<UiBuildDrawerActiveProductionComponent>(),
                ComponentType.ReadWrite<UiBuildDrawerCatalogItemComponent>(),
                ComponentType.ReadWrite<UiBuildDrawerQueueRowComponent>(),
                ComponentType.ReadWrite<UiBuildCatalogRequestComponent>(),
                ComponentType.ReadWrite<UiBuildProductionRequestComponent>(),
                ComponentType.ReadWrite<UiBuildPrimaryRequestComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            UiBuildDrawerStateComponent drawerState =
                state.EntityManager.GetComponentData<UiBuildDrawerStateComponent>(boundary);
            DynamicBuffer<UiBuildCatalogRequestComponent> catalogRequests =
                state.EntityManager.GetBuffer<UiBuildCatalogRequestComponent>(boundary);
            DynamicBuffer<UiBuildProductionRequestComponent> productionRequests =
                state.EntityManager.GetBuffer<UiBuildProductionRequestComponent>(boundary);
            DynamicBuffer<UiBuildPrimaryRequestComponent> primaryRequests =
                state.EntityManager.GetBuffer<UiBuildPrimaryRequestComponent>(boundary);
            bool hasDrawerRequests =
                catalogRequests.Length > 0 ||
                productionRequests.Length > 0 ||
                primaryRequests.Length > 0;
            if (!hasDrawerRequests && !IsBuildDrawerVisible(state.EntityManager, boundary))
                return;

            for (int i = 0; i < catalogRequests.Length; i++)
                drawerState.SelectedCatalogSlot = catalogRequests[i].CatalogSlot;
            catalogRequests.Clear();

            for (int i = 0; i < productionRequests.Length; i++)
            {
                UiBuildProductionRequestComponent request = productionRequests[i];
                UiBuildDrawerReadModelSource.ProcessProductionRequest(request.ActionKind, request.QueueSlot);
            }
            productionRequests.Clear();

            for (int i = 0; i < primaryRequests.Length; i++)
                UiBuildDrawerReadModelSource.ProcessPrimaryRequest(drawerState.ActiveCategory, drawerState.SelectedCatalogSlot);
            primaryRequests.Clear();

            DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog =
                state.EntityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
            DynamicBuffer<UiBuildDrawerQueueRowComponent> queue =
                state.EntityManager.GetBuffer<UiBuildDrawerQueueRowComponent>(boundary);
            UiBuildDrawerReadModelSource.WriteReadModel(
                state.EntityManager,
                boundary,
                ref drawerState,
                catalog,
                queue);
            state.EntityManager.SetComponentData(boundary, drawerState);
        }

        private static bool IsBuildDrawerVisible(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasComponent<UiShellActivePopupComponent>(boundary))
                return false;

            UiShellActivePopupComponent activePopup =
                entityManager.GetComponentData<UiShellActivePopupComponent>(boundary);
            return activePopup.Visible != 0 && activePopup.PopupKind == UiShellPopupKind.BuildDrawer;
        }
    }
}
