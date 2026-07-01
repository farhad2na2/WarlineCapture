using System;
using UnityEngine;

public sealed class BuildingUiCommandSystemHelper
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
        InvalidSelection = 3,
        ProductionQueueFull = 4
    }

    public delegate bool TryGetConfiguredSpawnableDelegate(int index, out ConfiguredSpawnableEntry entry);
    public delegate bool TryGetConfiguredUnitDelegate(int index, out ConfiguredUnitEntry entry);
    public delegate CampRequestFailure GetCampRequestFailureDelegate(GameObject prefab, int price, out string requiredBuildingDisplayName);
    public delegate CampRequestFailure TryRequestCampItemDelegate(GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess);
    public delegate bool CancelProductionDelegate(int buildingId, int pendingProductionIndex);

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
        public readonly Func<bool> HasPendingBuildingPlacement;
        public readonly Func<bool> CanConfirmBuildingPlacement;
        public readonly Func<string> GetPlacementStatusText;
        public readonly Func<int> GetActivePlacementCost;
        public readonly Func<float> GetActivePlacementDurationSeconds;
        public readonly Func<bool> ConfirmBuildingPlacement;
        public readonly Action CancelBuildingPlacement;
        public readonly CancelProductionDelegate CancelProduction;
        public readonly Func<bool> RotateBuildingPlacement;

        public Context(
            Func<int> getCurrentDollars,
            Func<int> getConfiguredSpawnableCount,
            TryGetConfiguredSpawnableDelegate tryGetConfiguredSpawnable,
            Func<int> getConfiguredUnitCount,
            TryGetConfiguredUnitDelegate tryGetConfiguredUnit,
            Func<GameObject, bool> isConfiguredSpawnablePrefab,
            GetCampRequestFailureDelegate getCampRequestFailure,
            TryRequestCampItemDelegate tryRequestCampItem,
            Func<bool> hasPendingBuildingPlacement,
            Func<bool> canConfirmBuildingPlacement,
            Func<string> getPlacementStatusText,
            Func<int> getActivePlacementCost,
            Func<float> getActivePlacementDurationSeconds,
            Func<bool> confirmBuildingPlacement,
            Action cancelBuildingPlacement,
            CancelProductionDelegate cancelProduction,
            Func<bool> rotateBuildingPlacement = null)
        {
            GetCurrentDollars = getCurrentDollars;
            GetConfiguredSpawnableCount = getConfiguredSpawnableCount;
            TryGetConfiguredSpawnable = tryGetConfiguredSpawnable;
            GetConfiguredUnitCount = getConfiguredUnitCount;
            TryGetConfiguredUnit = tryGetConfiguredUnit;
            IsConfiguredSpawnablePrefab = isConfiguredSpawnablePrefab;
            GetCampRequestFailure = getCampRequestFailure;
            TryRequestCampItem = tryRequestCampItem;
            HasPendingBuildingPlacement = hasPendingBuildingPlacement;
            CanConfirmBuildingPlacement = canConfirmBuildingPlacement;
            GetPlacementStatusText = getPlacementStatusText;
            GetActivePlacementCost = getActivePlacementCost;
            GetActivePlacementDurationSeconds = getActivePlacementDurationSeconds;
            ConfirmBuildingPlacement = confirmBuildingPlacement;
            CancelBuildingPlacement = cancelBuildingPlacement;
            CancelProduction = cancelProduction;
            RotateBuildingPlacement = rotateBuildingPlacement;
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

    public bool HasPendingBuildingPlacement(Context context)
    {
        return context.HasPendingBuildingPlacement != null &&
               context.HasPendingBuildingPlacement();
    }

    public bool CanConfirmBuildingPlacement(Context context)
    {
        return context.CanConfirmBuildingPlacement != null &&
               context.CanConfirmBuildingPlacement();
    }

    public string PlacementStatusText(Context context)
    {
        return context.GetPlacementStatusText?.Invoke() ?? string.Empty;
    }

    public int ActivePlacementCost(Context context)
    {
        return Mathf.Max(0, context.GetActivePlacementCost?.Invoke() ?? 0);
    }

    public float ActivePlacementDurationSeconds(Context context)
    {
        return Mathf.Max(0f, context.GetActivePlacementDurationSeconds?.Invoke() ?? 0f);
    }

    public bool CancelProduction(Context context, int buildingId, int pendingProductionIndex)
    {
        return context.CancelProduction != null &&
               context.CancelProduction(buildingId, pendingProductionIndex);
    }

    public bool ConfirmBuildingPlacement(Context context)
    {
        return context.ConfirmBuildingPlacement != null &&
               context.ConfirmBuildingPlacement();
    }

    public void CancelBuildingPlacement(Context context)
    {
        context.CancelBuildingPlacement?.Invoke();
    }

    public bool RotateBuildingPlacement(Context context)
    {
        return context.RotateBuildingPlacement != null &&
               context.RotateBuildingPlacement();
    }
}
