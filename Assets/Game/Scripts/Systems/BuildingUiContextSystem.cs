using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingUiContextSystem
{
    public readonly struct Source
    {
        public readonly RuntimeResourceSystem RuntimeResourceSystem;
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly RuntimeBuildingSystem<RuntimeBuildingData> RuntimeBuildingSystem;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly Func<int?> GetActiveBuildingId;
        public readonly BuildingUiQuerySystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Func<float> GetNow;
        public readonly Func<bool> HasActiveBuilding;
        public readonly Func<string> GetSelectedBuildingDisplayName;
        public readonly BuildingUiQuerySystem.TryGetSelectedBuildingHealthDelegate TryGetSelectedBuildingHealth;
        public readonly BuildingUiQuerySystem.TryGetSelectedBuildingPreviewPrefabDelegate TryGetSelectedBuildingPreviewPrefab;
        public readonly Func<int, bool> IsRuntimeBuildingWall;
        public readonly Func<int, bool> IsRuntimeBuildingCityGenerated;
        public readonly BuildingUiQuerySystem.TryGetRuntimeBuildingOwnerFactionDelegate TryGetRuntimeBuildingOwnerFaction;
        public readonly Func<Camera, bool> HasVisibleSelectableBuilding;
        public readonly BuildingUiQuerySystem.TryResolveLiveUnitPreviewPrefabDelegate TryResolveLiveUnitPreviewPrefab;
        public readonly BuildingUiCommandSystem.GetCampRequestFailureDelegate GetCampRequestFailure;
        public readonly BuildingUiCommandSystem.TryRequestCampItemDelegate TryRequestCampItem;
        public readonly Action DeleteSelectedBuilding;
        public readonly Func<bool> ConfirmBuildingPlacement;
        public readonly Action CancelBuildingPlacement;
        public readonly Action FocusLastCampProductionRequest;
        public readonly Action<string> ClearSelectedBuilding;
        public readonly Action ExitBuildMode;

        public Source(
            RuntimeResourceSystem runtimeResourceSystem,
            BuildingDefinitionSystem definitionSystem,
            RuntimeBuildingSystem<RuntimeBuildingData> runtimeBuildingSystem,
            BuildingProductionSystem productionSystem,
            Func<int?> getActiveBuildingId,
            BuildingUiQuerySystem.TryGetEntityManagerDelegate tryGetEntityManager,
            Func<float> getNow,
            Func<bool> hasActiveBuilding,
            Func<string> getSelectedBuildingDisplayName,
            BuildingUiQuerySystem.TryGetSelectedBuildingHealthDelegate tryGetSelectedBuildingHealth,
            BuildingUiQuerySystem.TryGetSelectedBuildingPreviewPrefabDelegate tryGetSelectedBuildingPreviewPrefab,
            Func<int, bool> isRuntimeBuildingWall,
            Func<int, bool> isRuntimeBuildingCityGenerated,
            BuildingUiQuerySystem.TryGetRuntimeBuildingOwnerFactionDelegate tryGetRuntimeBuildingOwnerFaction,
            Func<Camera, bool> hasVisibleSelectableBuilding,
            BuildingUiQuerySystem.TryResolveLiveUnitPreviewPrefabDelegate tryResolveLiveUnitPreviewPrefab,
            BuildingUiCommandSystem.GetCampRequestFailureDelegate getCampRequestFailure,
            BuildingUiCommandSystem.TryRequestCampItemDelegate tryRequestCampItem,
            Action deleteSelectedBuilding,
            Func<bool> confirmBuildingPlacement,
            Action cancelBuildingPlacement,
            Action focusLastCampProductionRequest,
            Action<string> clearSelectedBuilding,
            Action exitBuildMode)
        {
            RuntimeResourceSystem = runtimeResourceSystem;
            DefinitionSystem = definitionSystem;
            RuntimeBuildingSystem = runtimeBuildingSystem;
            ProductionSystem = productionSystem;
            GetActiveBuildingId = getActiveBuildingId;
            TryGetEntityManager = tryGetEntityManager;
            GetNow = getNow;
            HasActiveBuilding = hasActiveBuilding;
            GetSelectedBuildingDisplayName = getSelectedBuildingDisplayName;
            TryGetSelectedBuildingHealth = tryGetSelectedBuildingHealth;
            TryGetSelectedBuildingPreviewPrefab = tryGetSelectedBuildingPreviewPrefab;
            IsRuntimeBuildingWall = isRuntimeBuildingWall;
            IsRuntimeBuildingCityGenerated = isRuntimeBuildingCityGenerated;
            TryGetRuntimeBuildingOwnerFaction = tryGetRuntimeBuildingOwnerFaction;
            HasVisibleSelectableBuilding = hasVisibleSelectableBuilding;
            TryResolveLiveUnitPreviewPrefab = tryResolveLiveUnitPreviewPrefab;
            GetCampRequestFailure = getCampRequestFailure;
            TryRequestCampItem = tryRequestCampItem;
            DeleteSelectedBuilding = deleteSelectedBuilding;
            ConfirmBuildingPlacement = confirmBuildingPlacement;
            CancelBuildingPlacement = cancelBuildingPlacement;
            FocusLastCampProductionRequest = focusLastCampProductionRequest;
            ClearSelectedBuilding = clearSelectedBuilding;
            ExitBuildMode = exitBuildMode;
        }
    }

    public BuildingUiCommandSystem.Context CreateCommandContext(Source source)
    {
        return new BuildingUiCommandSystem.Context(
            () => source.RuntimeResourceSystem.CurrentDollars,
            () => source.DefinitionSystem.ConfiguredSpawnableCount,
            source.DefinitionSystem.TryGetConfiguredSpawnable,
            () => source.DefinitionSystem.ConfiguredUnitCount,
            source.DefinitionSystem.TryGetConfiguredUnit,
            source.DefinitionSystem.IsConfiguredSpawnablePrefab,
            source.GetCampRequestFailure,
            source.TryRequestCampItem,
            source.DeleteSelectedBuilding,
            source.ConfirmBuildingPlacement,
            source.CancelBuildingPlacement,
            source.FocusLastCampProductionRequest,
            source.ClearSelectedBuilding,
            source.ExitBuildMode);
    }

    public BuildingUiQuerySystem.Context CreateQueryContext(Source source)
    {
        IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings = source.RuntimeBuildingSystem.Buildings;
        return new BuildingUiQuerySystem.Context(
            runtimeBuildings,
            source.GetActiveBuildingId,
            source.TryGetEntityManager,
            source.ProductionSystem,
            source.GetNow,
            source.HasActiveBuilding,
            source.GetSelectedBuildingDisplayName,
            source.TryGetSelectedBuildingHealth,
            source.TryGetSelectedBuildingPreviewPrefab,
            source.IsRuntimeBuildingWall,
            source.IsRuntimeBuildingCityGenerated,
            source.TryGetRuntimeBuildingOwnerFaction,
            source.HasVisibleSelectableBuilding,
            source.TryResolveLiveUnitPreviewPrefab);
    }
}
