using System;
using System.Collections.Generic;
using UnityEngine;
using CampRequestFailure = BuildingPlacementSystem.CampRequestFailure;
using ConfiguredSpawnableEntry = BuildingPlacementSystem.ConfiguredSpawnableEntry;
using ConfiguredUnitEntry = BuildingPlacementSystem.ConfiguredUnitEntry;
using PendingProductionUiEntry = BuildingPlacementSystem.PendingProductionUiEntry;

public sealed class BuildingUiCommandSystem
{
    public delegate bool TryGetConfiguredSpawnableDelegate(int index, out ConfiguredSpawnableEntry entry);
    public delegate bool TryGetConfiguredUnitDelegate(int index, out ConfiguredUnitEntry entry);
    public delegate CampRequestFailure GetCampRequestFailureDelegate(GameObject prefab, int price, out string requiredBuildingDisplayName);
    public delegate CampRequestFailure TryRequestCampItemDelegate(GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess);

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

        public Context(
            Func<int> getCurrentDollars,
            Func<int> getConfiguredSpawnableCount,
            TryGetConfiguredSpawnableDelegate tryGetConfiguredSpawnable,
            Func<int> getConfiguredUnitCount,
            TryGetConfiguredUnitDelegate tryGetConfiguredUnit,
            Func<GameObject, bool> isConfiguredSpawnablePrefab,
            GetCampRequestFailureDelegate getCampRequestFailure,
            TryRequestCampItemDelegate tryRequestCampItem,
            Action<List<PendingProductionUiEntry>> getFriendlyPendingProductionUiEntries)
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
}
