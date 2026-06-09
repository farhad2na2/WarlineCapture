using System;
using UnityEngine;

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
    public delegate void CreateSelectedBuildingUnitDelegate(int productionIndex);
    public delegate void CreateBuildingUnitDelegate(int buildingId, int productionIndex);
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
        public readonly CreateSelectedBuildingUnitDelegate CreateSelectedBuildingUnit;
        public readonly CreateBuildingUnitDelegate CreateBuildingUnit;
        public readonly Action DeleteSelectedBuilding;
        public readonly Func<bool> ConfirmBuildingPlacement;
        public readonly Action CancelBuildingPlacement;
        public readonly Action FocusLastCampProductionRequest;
        public readonly Action ArmNextProductionFromUi;
        public readonly CancelProductionDelegate CancelProduction;
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
            CreateSelectedBuildingUnitDelegate createSelectedBuildingUnit,
            CreateBuildingUnitDelegate createBuildingUnit,
            Action deleteSelectedBuilding,
            Func<bool> confirmBuildingPlacement,
            Action cancelBuildingPlacement,
            Action focusLastCampProductionRequest,
            Action armNextProductionFromUi,
            CancelProductionDelegate cancelProduction,
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
            CreateSelectedBuildingUnit = createSelectedBuildingUnit;
            CreateBuildingUnit = createBuildingUnit;
            DeleteSelectedBuilding = deleteSelectedBuilding;
            ConfirmBuildingPlacement = confirmBuildingPlacement;
            CancelBuildingPlacement = cancelBuildingPlacement;
            FocusLastCampProductionRequest = focusLastCampProductionRequest;
            ArmNextProductionFromUi = armNextProductionFromUi;
            CancelProduction = cancelProduction;
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

    public void DeleteSelectedBuilding(Context context)
    {
        context.DeleteSelectedBuilding?.Invoke();
    }

    public void CreateUnitFromSelectedBuilding(Context context)
    {
        CreateUnitFromSelectedBuilding(context, 0);
    }

    public void CreateUnitFromSelectedBuilding(Context context, int productionIndex)
    {
        context.CreateSelectedBuildingUnit?.Invoke(productionIndex);
    }

    public void CreateUnitFromBuilding(Context context, int buildingId)
    {
        CreateUnitFromBuilding(context, buildingId, 0);
    }

    public void CreateUnitFromBuilding(Context context, int buildingId, int productionIndex)
    {
        context.CreateBuildingUnit?.Invoke(buildingId, productionIndex);
    }

    public void CreateSecondaryUnitFromSelectedBuilding(Context context)
    {
        CreateUnitFromSelectedBuilding(context, 1);
    }

    public void CreateSecondaryUnitFromBuilding(Context context, int buildingId)
    {
        CreateUnitFromBuilding(context, buildingId, 1);
    }

    public void CreateTertiaryUnitFromSelectedBuilding(Context context)
    {
        CreateUnitFromSelectedBuilding(context, 2);
    }

    public void CreateTertiaryUnitFromBuilding(Context context, int buildingId)
    {
        CreateUnitFromBuilding(context, buildingId, 2);
    }

    public void CreateQuaternaryUnitFromSelectedBuilding(Context context)
    {
        CreateUnitFromSelectedBuilding(context, 3);
    }

    public void CreateQuaternaryUnitFromBuilding(Context context, int buildingId)
    {
        CreateUnitFromBuilding(context, buildingId, 3);
    }

    public void CreateSoldierFromSelectedBuilding(Context context)
    {
        CreateUnitFromSelectedBuilding(context);
    }

    public void ArmNextProductionFromUi(Context context)
    {
        context.ArmNextProductionFromUi?.Invoke();
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
