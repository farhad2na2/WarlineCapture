using System;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class BuildingUiCommandSystemHelper
    {
        public readonly struct Context
        {
            public readonly Func<int> GetCurrentDollars;
            public readonly Func<int> GetMaxQueuedUnitProductions;
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
            public readonly Func<int> GetActivePlacementCreditsCost;
            public readonly Func<Vector2Int> GetPlacementFootprint;

            public Context(
                Func<int> getCurrentDollars,
                Func<int> getMaxQueuedUnitProductions,
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
                Func<bool> rotateBuildingPlacement = null,
                Func<int> getActivePlacementCreditsCost = null, Func<Vector2Int> getPlacementFootprint = null)
            {
                GetCurrentDollars = getCurrentDollars;
                GetMaxQueuedUnitProductions = getMaxQueuedUnitProductions;
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
                GetActivePlacementCreditsCost = getActivePlacementCreditsCost;
                GetPlacementFootprint = getPlacementFootprint;
            }
        }
    }
}
