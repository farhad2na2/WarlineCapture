using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using PendingProductionUiEntry = BuildingUiQuerySystem.PendingProductionUiEntry;

public sealed class BuildingUiCommandSystem
{
    public readonly struct ConfiguredSpawnableEntry
    {
        public readonly string DisplayName;
        public readonly string Description;
        public readonly GameObject Prefab;
        public readonly bool CanRequest;
        public readonly int Price;

        public ConfiguredSpawnableEntry(string displayName, string description, GameObject prefab, bool canRequest, int price)
        {
            DisplayName = displayName;
            Description = description;
            Prefab = prefab;
            CanRequest = canRequest;
            Price = price;
        }
    }

    public readonly struct ConfiguredUnitEntry
    {
        public readonly string DisplayName;
        public readonly string Description;
        public readonly GameObject Prefab;
        public readonly bool IsVehicle;
        public readonly bool CanRequest;
        public readonly int Price;

        public ConfiguredUnitEntry(string displayName, string description, GameObject prefab, bool isVehicle, bool canRequest, int price)
        {
            DisplayName = displayName;
            Description = description;
            Prefab = prefab;
            IsVehicle = isVehicle;
            CanRequest = canRequest;
            Price = price;
        }
    }

    public enum CampRequestFailure
    {
        None = 0,
        NotEnoughMoney = 1,
        MissingProducerBuilding = 2,
        InvalidSelection = 3
    }

    public delegate bool TryGetConfiguredSpawnableDelegate(int index, out ConfiguredSpawnableEntry entry);
    public delegate bool TryGetConfiguredUnitDelegate(int index, out ConfiguredUnitEntry entry);
    public delegate CampRequestFailure GetCampRequestFailureDelegate(GameObject prefab, int price, out string requiredBuildingDisplayName);
    public delegate CampRequestFailure TryRequestCampItemDelegate(GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess);
    public delegate bool TryGetSelectedBuildingHealthDelegate(out int current, out int max);
    public delegate bool TryGetSelectedBuildingPreviewPrefabDelegate(out GameObject prefab);
    public delegate bool TryGetRuntimeBuildingOwnerFactionDelegate(int buildingId, out byte ownerFactionId);
    public delegate bool TryResolveLiveUnitPreviewPrefabDelegate(Entity unitEntity, out GameObject prefab);

    public readonly struct Context
    {
        public readonly Func<int> GetCurrentDollars;
        public readonly Func<int> GetConfiguredSpawnableCount;
        public readonly TryGetConfiguredSpawnableDelegate TryGetConfiguredSpawnable;
        public readonly Func<int> GetConfiguredUnitCount;
        public readonly TryGetConfiguredUnitDelegate TryGetConfiguredUnit;
        public readonly Func<GameObject, bool> IsConfiguredSpawnablePrefab;
        public readonly GetCampRequestFailureDelegate GetCampRequestFailure;
        public readonly TryRequestCampItemDelegate TryRequestCampItem;
        public readonly Action<List<PendingProductionUiEntry>> GetFriendlyPendingProductionUiEntries;
        public readonly Func<bool> HasActiveBuilding;
        public readonly Func<string> GetSelectedBuildingDisplayName;
        public readonly Action DeleteSelectedBuilding;
        public readonly Func<bool> ConfirmBuildingPlacement;
        public readonly TryGetSelectedBuildingHealthDelegate TryGetSelectedBuildingHealth;
        public readonly TryGetSelectedBuildingPreviewPrefabDelegate TryGetSelectedBuildingPreviewPrefab;
        public readonly Func<int, bool> IsRuntimeBuildingWall;
        public readonly Func<int, bool> IsRuntimeBuildingCityGenerated;
        public readonly TryGetRuntimeBuildingOwnerFactionDelegate TryGetRuntimeBuildingOwnerFaction;
        public readonly Func<Camera, bool> HasVisibleSelectableBuilding;
        public readonly TryResolveLiveUnitPreviewPrefabDelegate TryResolveLiveUnitPreviewPrefab;
        public readonly Action CancelBuildingPlacement;
        public readonly Action FocusLastCampProductionRequest;
        public readonly Action<string> ClearSelectedBuilding;
        public readonly Action ExitBuildMode;

        public Context(
            Func<int> getCurrentDollars,
            Func<int> getConfiguredSpawnableCount,
            TryGetConfiguredSpawnableDelegate tryGetConfiguredSpawnable,
            Func<int> getConfiguredUnitCount,
            TryGetConfiguredUnitDelegate tryGetConfiguredUnit,
            Func<GameObject, bool> isConfiguredSpawnablePrefab,
            GetCampRequestFailureDelegate getCampRequestFailure,
            TryRequestCampItemDelegate tryRequestCampItem,
            Action<List<PendingProductionUiEntry>> getFriendlyPendingProductionUiEntries,
            Func<bool> hasActiveBuilding,
            Func<string> getSelectedBuildingDisplayName,
            Action deleteSelectedBuilding,
            Func<bool> confirmBuildingPlacement,
            TryGetSelectedBuildingHealthDelegate tryGetSelectedBuildingHealth,
            TryGetSelectedBuildingPreviewPrefabDelegate tryGetSelectedBuildingPreviewPrefab,
            Func<int, bool> isRuntimeBuildingWall,
            Func<int, bool> isRuntimeBuildingCityGenerated,
            TryGetRuntimeBuildingOwnerFactionDelegate tryGetRuntimeBuildingOwnerFaction,
            Func<Camera, bool> hasVisibleSelectableBuilding,
            TryResolveLiveUnitPreviewPrefabDelegate tryResolveLiveUnitPreviewPrefab,
            Action cancelBuildingPlacement,
            Action focusLastCampProductionRequest,
            Action<string> clearSelectedBuilding,
            Action exitBuildMode)
        {
            GetCurrentDollars = getCurrentDollars;
            GetConfiguredSpawnableCount = getConfiguredSpawnableCount;
            TryGetConfiguredSpawnable = tryGetConfiguredSpawnable;
            GetConfiguredUnitCount = getConfiguredUnitCount;
            TryGetConfiguredUnit = tryGetConfiguredUnit;
            IsConfiguredSpawnablePrefab = isConfiguredSpawnablePrefab;
            GetCampRequestFailure = getCampRequestFailure;
            TryRequestCampItem = tryRequestCampItem;
            GetFriendlyPendingProductionUiEntries = getFriendlyPendingProductionUiEntries;
            HasActiveBuilding = hasActiveBuilding;
            GetSelectedBuildingDisplayName = getSelectedBuildingDisplayName;
            DeleteSelectedBuilding = deleteSelectedBuilding;
            ConfirmBuildingPlacement = confirmBuildingPlacement;
            TryGetSelectedBuildingHealth = tryGetSelectedBuildingHealth;
            TryGetSelectedBuildingPreviewPrefab = tryGetSelectedBuildingPreviewPrefab;
            IsRuntimeBuildingWall = isRuntimeBuildingWall;
            IsRuntimeBuildingCityGenerated = isRuntimeBuildingCityGenerated;
            TryGetRuntimeBuildingOwnerFaction = tryGetRuntimeBuildingOwnerFaction;
            HasVisibleSelectableBuilding = hasVisibleSelectableBuilding;
            TryResolveLiveUnitPreviewPrefab = tryResolveLiveUnitPreviewPrefab;
            CancelBuildingPlacement = cancelBuildingPlacement;
            FocusLastCampProductionRequest = focusLastCampProductionRequest;
            ClearSelectedBuilding = clearSelectedBuilding;
            ExitBuildMode = exitBuildMode;
        }
    }

    public int CurrentDollars(Context context)
    {
        return context.GetCurrentDollars?.Invoke() ?? 0;
    }

    public int ConfiguredSpawnableCount(Context context)
    {
        return context.GetConfiguredSpawnableCount?.Invoke() ?? 0;
    }

    public bool TryGetConfiguredSpawnable(Context context, int index, out ConfiguredSpawnableEntry entry)
    {
        entry = default;
        return context.TryGetConfiguredSpawnable != null &&
               context.TryGetConfiguredSpawnable(index, out entry);
    }

    public int ConfiguredUnitCount(Context context)
    {
        return context.GetConfiguredUnitCount?.Invoke() ?? 0;
    }

    public bool TryGetConfiguredUnit(Context context, int index, out ConfiguredUnitEntry entry)
    {
        entry = default;
        return context.TryGetConfiguredUnit != null &&
               context.TryGetConfiguredUnit(index, out entry);
    }

    public bool IsConfiguredSpawnablePrefab(Context context, GameObject prefab)
    {
        return context.IsConfiguredSpawnablePrefab != null &&
               context.IsConfiguredSpawnablePrefab(prefab);
    }

    public CampRequestFailure GetCampRequestFailure(Context context, GameObject prefab, int price, out string requiredBuildingDisplayName)
    {
        requiredBuildingDisplayName = string.Empty;
        if (context.GetCampRequestFailure == null)
            return CampRequestFailure.InvalidSelection;

        return context.GetCampRequestFailure(prefab, price, out requiredBuildingDisplayName);
    }

    public CampRequestFailure TryRequestCampItem(
        Context context,
        GameObject prefab,
        int price,
        out string requiredBuildingDisplayName,
        bool focusProducerOnSuccess)
    {
        requiredBuildingDisplayName = string.Empty;
        if (context.TryRequestCampItem == null)
            return CampRequestFailure.InvalidSelection;

        return context.TryRequestCampItem(prefab, price, out requiredBuildingDisplayName, focusProducerOnSuccess);
    }

    public void GetFriendlyPendingProductionUiEntries(Context context, List<PendingProductionUiEntry> entries)
    {
        if (entries == null)
            return;

        entries.Clear();
        context.GetFriendlyPendingProductionUiEntries?.Invoke(entries);
    }

    public bool HasActiveBuilding(Context context)
    {
        return context.HasActiveBuilding != null &&
               context.HasActiveBuilding();
    }

    public string SelectedBuildingDisplayName(Context context)
    {
        return context.GetSelectedBuildingDisplayName?.Invoke() ?? string.Empty;
    }

    public void DeleteSelectedBuilding(Context context)
    {
        context.DeleteSelectedBuilding?.Invoke();
    }

    public bool ConfirmBuildingPlacement(Context context)
    {
        return context.ConfirmBuildingPlacement != null &&
               context.ConfirmBuildingPlacement();
    }

    public bool TryGetSelectedBuildingHealth(Context context, out int current, out int max)
    {
        current = 0;
        max = 0;
        return context.TryGetSelectedBuildingHealth != null &&
               context.TryGetSelectedBuildingHealth(out current, out max);
    }

    public bool TryGetSelectedBuildingPreviewPrefab(Context context, out GameObject prefab)
    {
        prefab = null;
        return context.TryGetSelectedBuildingPreviewPrefab != null &&
               context.TryGetSelectedBuildingPreviewPrefab(out prefab);
    }

    public bool IsRuntimeBuildingWall(Context context, int buildingId)
    {
        return context.IsRuntimeBuildingWall != null &&
               context.IsRuntimeBuildingWall(buildingId);
    }

    public bool IsRuntimeBuildingCityGenerated(Context context, int buildingId)
    {
        return context.IsRuntimeBuildingCityGenerated != null &&
               context.IsRuntimeBuildingCityGenerated(buildingId);
    }

    public bool TryGetRuntimeBuildingOwnerFaction(Context context, int buildingId, out byte ownerFactionId)
    {
        ownerFactionId = 0;
        return context.TryGetRuntimeBuildingOwnerFaction != null &&
               context.TryGetRuntimeBuildingOwnerFaction(buildingId, out ownerFactionId);
    }

    public bool HasVisibleSelectableBuilding(Context context, Camera camera)
    {
        return context.HasVisibleSelectableBuilding != null &&
               context.HasVisibleSelectableBuilding(camera);
    }

    public bool TryResolveLiveUnitPreviewPrefab(Context context, Entity unitEntity, out GameObject prefab)
    {
        prefab = null;
        return context.TryResolveLiveUnitPreviewPrefab != null &&
               context.TryResolveLiveUnitPreviewPrefab(unitEntity, out prefab);
    }

    public void CancelBuildingPlacement(Context context)
    {
        context.CancelBuildingPlacement?.Invoke();
    }

    public void FocusLastCampProductionRequest(Context context)
    {
        context.FocusLastCampProductionRequest?.Invoke();
    }

    public void ClearSelectedBuilding(Context context, string reason)
    {
        context.ClearSelectedBuilding?.Invoke(reason);
    }

    public void ExitBuildMode(Context context)
    {
        context.ExitBuildMode?.Invoke();
    }
}
