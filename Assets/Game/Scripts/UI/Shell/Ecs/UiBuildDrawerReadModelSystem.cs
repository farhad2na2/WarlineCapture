using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class UiBuildDrawerReadModelSource
{
    private static readonly BuildDrawerCatalogQuerySystem CatalogQuery = new();
    private static readonly List<BuildDrawerCatalogItem> CatalogItems = new();
    private static readonly List<BuildDrawerCatalogItem> CountScratch = new();
    private static readonly List<BuildingPendingProductionUiEntry> PendingProductions = new();
    private static readonly List<BuildingPendingProductionUiEntry> ClearProductionScratch = new();

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
            catalog.Add(new UiBuildDrawerCatalogItemComponent
            {
                Visible = 1,
                Enabled = 1,
                Selected = i == selectedSlot ? (byte)1 : (byte)0,
                Category = item.Category,
                Title = ToFixed64(item.DisplayName),
                Role = ToFixed32(item.TypeLabel),
                CreditsText = ToFixed32(FormatPrice(item.Price)),
                SuppliesText = default,
                TimeText = ToFixed32(FormatDuration(item))
            });
        }
    }

    private static UiBuildDrawerDetailComponent BuildDetail(BuildDrawerCatalogItem item)
    {
        string requiredBuildingDisplayName = string.Empty;
        BuildingUiCommandFailure failure = buildingUiCommand != null
            ? buildingUiCommand.GetCampRequestFailure(item.Prefab, item.Price, out requiredBuildingDisplayName)
            : BuildingUiCommandFailure.InvalidSelection;

        bool canBuild = buildingUiCommand != null && failure == BuildingUiCommandFailure.None;
        string instruction = canBuild
            ? FormatReadyInstruction(item)
            : FormatInstructionFailureMessage(item, failure, requiredBuildingDisplayName);

        if (canBuild &&
            item.Category == BuildDrawerCategory.Buildings &&
            buildingUiCommand.HasPendingBuildingPlacement)
        {
            bool canConfirm = buildingUiCommand.CanConfirmBuildingPlacement;
            instruction = canConfirm
                ? $"Place {item.DisplayName}: drag to position, then confirm."
                : $"Cannot place here: {FormatPlacementStatus(buildingUiCommand.PlacementStatusText)}.";
        }

        return new UiBuildDrawerDetailComponent
        {
            Name = ToFixed64(item.DisplayName),
            Role = ToFixed32(item.TypeLabel),
            Description = ToFixed128(item.Description),
            FootprintText = ToFixed32(FormatFootprint(item)),
            RequirementsText = ToFixed64(FormatRequirements(item)),
            PlacementText = ToFixed64(FormatPlacement(item)),
            ProductionTimeText = ToFixed32(FormatDuration(item)),
            CreditsCostText = ToFixed32(FormatPrice(item.Price)),
            SuppliesCostText = default,
            InstructionText = ToFixed128(instruction),
            ProductionTitle = new FixedString32Bytes("PRODUCTION"),
            ProductionCountText = ToFixed32(PendingProductions.Count.ToString(CultureInfo.InvariantCulture)),
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
            Name = new FixedString64Bytes("NO ITEMS"),
            Role = ToFixed32(BuildDrawerCategoryFormatter.Format(category)),
            Description = ToFixed128(FormatEmptyCategoryInstruction(category)),
            FootprintText = default,
            RequirementsText = default,
            PlacementText = default,
            ProductionTimeText = default,
            CreditsCostText = default,
            SuppliesCostText = default,
            InstructionText = ToFixed128(FormatEmptyCategoryInstruction(category)),
            ProductionTitle = new FixedString32Bytes("PRODUCTION"),
            ProductionCountText = ToFixed32(PendingProductions.Count.ToString(CultureInfo.InvariantCulture)),
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
            : entry.Prefab != null ? entry.Prefab.name : "Production";
    }

    private static string FormatPrice(int price)
    {
        return Mathf.Max(0, price).ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatDuration(BuildDrawerCatalogItem item)
    {
        if (item.ProductionDurationSeconds <= 0f)
            return "-";

        int seconds = Mathf.CeilToInt(item.ProductionDurationSeconds);
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private static string FormatRemaining(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private static string FormatPercent(float progress01)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(progress01) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
    }

    private static string FormatFootprint(BuildDrawerCatalogItem item)
    {
        return item.Category == BuildDrawerCategory.Buildings
            ? $"{item.FootprintCells.x} x {item.FootprintCells.y}"
            : "-";
    }

    private static string FormatPlacement(BuildDrawerCatalogItem item)
    {
        return item.Category == BuildDrawerCategory.Buildings
            ? $"{item.FootprintCells.x}x{item.FootprintCells.y}"
            : "-";
    }

    private static string FormatRequirements(BuildDrawerCatalogItem item)
    {
        return item.Category switch
        {
            BuildDrawerCategory.Buildings => "Valid footprint required.",
            BuildDrawerCategory.Aircrafts => "Requires compatible air production.",
            BuildDrawerCategory.Vehicles => "Requires compatible vehicle production.",
            BuildDrawerCategory.Soldiers => "Requires compatible recruitment building.",
            _ => string.Empty
        };
    }

    private static string FormatReadyInstruction(BuildDrawerCatalogItem item)
    {
        return item.Category switch
        {
            BuildDrawerCategory.Buildings => $"PLACE: choose a location for {item.DisplayName}.",
            BuildDrawerCategory.Vehicles => $"PRODUCE: add {item.DisplayName} to the vehicle queue.",
            BuildDrawerCategory.Aircrafts => $"PRODUCE: add {item.DisplayName} to the aircraft queue.",
            BuildDrawerCategory.Soldiers => $"RECRUIT: add {item.DisplayName} to the training queue.",
            _ => $"Select {item.DisplayName}."
        };
    }

    private static string FormatInstructionFailureMessage(
        BuildDrawerCatalogItem item,
        BuildingUiCommandFailure failure,
        string requiredBuildingDisplayName)
    {
        if (buildingUiCommand == null)
            return "Build drawer is still connecting. Try again in a moment.";

        return failure switch
        {
            BuildingUiCommandFailure.NotEnoughMoney =>
                $"Need {FormatMissingCredits(item.Price)} more credits to {FormatActionVerb(item.Category).ToLowerInvariant()} {item.DisplayName}.",
            BuildingUiCommandFailure.MissingProducerBuilding when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                $"Cannot {FormatActionVerb(item.Category).ToLowerInvariant()} {item.DisplayName}: requires {requiredBuildingDisplayName}.",
            BuildingUiCommandFailure.MissingProducerBuilding =>
                $"Cannot {FormatActionVerb(item.Category).ToLowerInvariant()} {item.DisplayName}: {FormatMissingProducerFallback(item.Category)}.",
            BuildingUiCommandFailure.InvalidSelection => "Select a build drawer item first.",
            _ => $"Cannot {FormatActionVerb(item.Category).ToLowerInvariant()} {item.DisplayName}: request unavailable."
        };
    }

    private static int FormatMissingCredits(int price)
    {
        int current = buildingUiCommand != null ? buildingUiCommand.CurrentDollars : 0;
        return Mathf.Max(0, price - current);
    }

    private static string FormatActionVerb(BuildDrawerCategory category)
    {
        return category switch
        {
            BuildDrawerCategory.Buildings => "Place",
            BuildDrawerCategory.Soldiers => "Recruit",
            BuildDrawerCategory.Vehicles => "Produce",
            BuildDrawerCategory.Aircrafts => "Produce",
            _ => "Select"
        };
    }

    private static string FormatMissingProducerFallback(BuildDrawerCategory category)
    {
        return category switch
        {
            BuildDrawerCategory.Vehicles => "no compatible vehicle producer is available",
            BuildDrawerCategory.Aircrafts => "no compatible air producer is available",
            BuildDrawerCategory.Soldiers => "no compatible training building is available",
            _ => "required producer is missing"
        };
    }

    private static string FormatEmptyCategoryInstruction(BuildDrawerCategory category)
    {
        return category switch
        {
            BuildDrawerCategory.Buildings => "No requestable buildings are configured.",
            BuildDrawerCategory.Vehicles => "No requestable vehicles are configured.",
            BuildDrawerCategory.Aircrafts => "No requestable aircraft are configured.",
            BuildDrawerCategory.Soldiers => "No requestable soldiers are configured.",
            _ => "No requestable items are configured."
        };
    }

    private static string FormatPlacementStatus(string rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
            return "invalid placement";

        int separator = rawStatus.IndexOf(':');
        return separator >= 0 && separator + 1 < rawStatus.Length
            ? rawStatus.Substring(separator + 1).Trim()
            : rawStatus;
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

    private static FixedString32Bytes ToFixed32(string value)
    {
        return new FixedString32Bytes(Trim(value, 28));
    }

    private static FixedString64Bytes ToFixed64(string value)
    {
        return new FixedString64Bytes(Trim(value, 60));
    }

    private static FixedString128Bytes ToFixed128(string value)
    {
        return new FixedString128Bytes(Trim(value, 120));
    }

    private static string Trim(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct UiBuildDrawerReadModelSystem : ISystem
{
    private EntityQuery boundaryQuery;

    public void OnCreate(ref SystemState state)
    {
        boundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
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
}
