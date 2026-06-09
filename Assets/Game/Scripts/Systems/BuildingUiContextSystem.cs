using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingUiContextSystem
{
    public readonly struct Source
    {
        public readonly RuntimeResourceSystem RuntimeResourceSystem;
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly RuntimeBuildingSystem<RuntimeBuildingEntity> RuntimeBuildingSystem;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly BuildingProductionRequestSystem ProductionRequestSystem;
        public readonly Func<BuildingProductionRequestSystem.Context> CreateProductionRequestContext;
        public readonly Func<int?> GetActiveBuildingId;
        public readonly Func<int> GetFrameCount;
        public readonly BuildingUiQuerySystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Func<float> GetNow;
        public readonly Func<bool> HasSelectedBuilding;
        public readonly Func<bool> HasActiveBuilding;
        public readonly Func<string> GetPlacementStatusText;
        public readonly Func<string> GetSelectedBuildingLabel;
        public readonly Func<string> GetSelectedBuildingDisplayName;
        public readonly Func<string> GetSelectedBuildingDescription;
        public readonly BuildingUiQuerySystem.TryGetSelectedBuildingHealthDelegate TryGetSelectedBuildingHealth;
        public readonly BuildingUiQuerySystem.TryGetSelectedBuildingPreviewPrefabDelegate TryGetSelectedBuildingPreviewPrefab;
        public readonly Func<int, bool> IsRuntimeBuildingWall;
        public readonly Func<int, bool> IsRuntimeBuildingCityGenerated;
        public readonly BuildingUiQuerySystem.TryGetRuntimeBuildingOwnerFactionDelegate TryGetRuntimeBuildingOwnerFaction;
        public readonly Func<Camera, bool> HasVisibleSelectableBuilding;
        public readonly BuildingUiQuerySystem.TryResolveLiveUnitPreviewPrefabDelegate TryResolveLiveUnitPreviewPrefab;
        public readonly Action DeleteSelectedBuilding;
        public readonly Func<bool> ConfirmBuildingPlacement;
        public readonly Action CancelBuildingPlacement;
        public readonly Action<string> ClearSelectedBuilding;
        public readonly Action ExitBuildMode;

        public Source(
            RuntimeResourceSystem runtimeResourceSystem,
            BuildingDefinitionSystem definitionSystem,
            RuntimeBuildingSystem<RuntimeBuildingEntity> runtimeBuildingSystem,
            BuildingProductionSystem productionSystem,
            BuildingProductionRequestSystem productionRequestSystem,
            Func<BuildingProductionRequestSystem.Context> createProductionRequestContext,
            Func<int?> getActiveBuildingId,
            Func<int> getFrameCount,
            BuildingUiQuerySystem.TryGetEntityManagerDelegate tryGetEntityManager,
            Func<float> getNow,
            Func<bool> hasSelectedBuilding,
            Func<bool> hasActiveBuilding,
            Func<string> getPlacementStatusText,
            Func<string> getSelectedBuildingLabel,
            Func<string> getSelectedBuildingDisplayName,
            Func<string> getSelectedBuildingDescription,
            BuildingUiQuerySystem.TryGetSelectedBuildingHealthDelegate tryGetSelectedBuildingHealth,
            BuildingUiQuerySystem.TryGetSelectedBuildingPreviewPrefabDelegate tryGetSelectedBuildingPreviewPrefab,
            Func<int, bool> isRuntimeBuildingWall,
            Func<int, bool> isRuntimeBuildingCityGenerated,
            BuildingUiQuerySystem.TryGetRuntimeBuildingOwnerFactionDelegate tryGetRuntimeBuildingOwnerFaction,
            Func<Camera, bool> hasVisibleSelectableBuilding,
            BuildingUiQuerySystem.TryResolveLiveUnitPreviewPrefabDelegate tryResolveLiveUnitPreviewPrefab,
            Action deleteSelectedBuilding,
            Func<bool> confirmBuildingPlacement,
            Action cancelBuildingPlacement,
            Action<string> clearSelectedBuilding,
            Action exitBuildMode)
        {
            RuntimeResourceSystem = runtimeResourceSystem;
            DefinitionSystem = definitionSystem;
            RuntimeBuildingSystem = runtimeBuildingSystem;
            ProductionSystem = productionSystem;
            ProductionRequestSystem = productionRequestSystem;
            CreateProductionRequestContext = createProductionRequestContext;
            GetActiveBuildingId = getActiveBuildingId;
            GetFrameCount = getFrameCount;
            TryGetEntityManager = tryGetEntityManager;
            GetNow = getNow;
            HasSelectedBuilding = hasSelectedBuilding;
            HasActiveBuilding = hasActiveBuilding;
            GetPlacementStatusText = getPlacementStatusText;
            GetSelectedBuildingLabel = getSelectedBuildingLabel;
            GetSelectedBuildingDisplayName = getSelectedBuildingDisplayName;
            GetSelectedBuildingDescription = getSelectedBuildingDescription;
            TryGetSelectedBuildingHealth = tryGetSelectedBuildingHealth;
            TryGetSelectedBuildingPreviewPrefab = tryGetSelectedBuildingPreviewPrefab;
            IsRuntimeBuildingWall = isRuntimeBuildingWall;
            IsRuntimeBuildingCityGenerated = isRuntimeBuildingCityGenerated;
            TryGetRuntimeBuildingOwnerFaction = tryGetRuntimeBuildingOwnerFaction;
            HasVisibleSelectableBuilding = hasVisibleSelectableBuilding;
            TryResolveLiveUnitPreviewPrefab = tryResolveLiveUnitPreviewPrefab;
            DeleteSelectedBuilding = deleteSelectedBuilding;
            ConfirmBuildingPlacement = confirmBuildingPlacement;
            CancelBuildingPlacement = cancelBuildingPlacement;
            ClearSelectedBuilding = clearSelectedBuilding;
            ExitBuildMode = exitBuildMode;
        }
    }

    public Source CreateSource(
        RuntimeResourceSystem runtimeResourceSystem,
        BuildingDefinitionSystem definitionSystem,
        RuntimeBuildingSystem<RuntimeBuildingEntity> runtimeBuildingSystem,
        BuildingProductionSystem productionSystem,
        BuildingProductionRequestSystem productionRequestSystem,
        Func<BuildingProductionRequestSystem.Context> createProductionRequestContext,
        Func<int?> getActiveBuildingId,
        Func<int> getFrameCount,
        BuildingUiQuerySystem.TryGetEntityManagerDelegate tryGetEntityManager,
        Func<float> getNow,
        Func<bool> hasSelectedBuilding,
        Func<bool> hasActiveBuilding,
        Func<string> getPlacementStatusText,
        Func<string> getSelectedBuildingLabel,
        Func<string> getSelectedBuildingDisplayName,
        Func<string> getSelectedBuildingDescription,
        BuildingUiQuerySystem.TryGetSelectedBuildingHealthDelegate tryGetSelectedBuildingHealth,
        BuildingUiQuerySystem.TryGetSelectedBuildingPreviewPrefabDelegate tryGetSelectedBuildingPreviewPrefab,
        Func<int, bool> isRuntimeBuildingWall,
        Func<int, bool> isRuntimeBuildingCityGenerated,
        BuildingUiQuerySystem.TryGetRuntimeBuildingOwnerFactionDelegate tryGetRuntimeBuildingOwnerFaction,
        Func<Camera, bool> hasVisibleSelectableBuilding,
        BuildingUiQuerySystem.TryResolveLiveUnitPreviewPrefabDelegate tryResolveLiveUnitPreviewPrefab,
        Action deleteSelectedBuilding,
        Func<bool> confirmBuildingPlacement,
        Action cancelBuildingPlacement,
        Action<string> clearSelectedBuilding,
        Action exitBuildMode)
    {
        return new Source(
            runtimeResourceSystem,
            definitionSystem,
            runtimeBuildingSystem,
            productionSystem,
            productionRequestSystem,
            createProductionRequestContext,
            getActiveBuildingId,
            getFrameCount,
            tryGetEntityManager,
            getNow,
            hasSelectedBuilding,
            hasActiveBuilding,
            getPlacementStatusText,
            getSelectedBuildingLabel,
            getSelectedBuildingDisplayName,
            getSelectedBuildingDescription,
            tryGetSelectedBuildingHealth,
            tryGetSelectedBuildingPreviewPrefab,
            isRuntimeBuildingWall,
            isRuntimeBuildingCityGenerated,
            tryGetRuntimeBuildingOwnerFaction,
            hasVisibleSelectableBuilding,
            tryResolveLiveUnitPreviewPrefab,
            deleteSelectedBuilding,
            confirmBuildingPlacement,
            cancelBuildingPlacement,
            clearSelectedBuilding,
            exitBuildMode);
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
            (GameObject prefab, int price, out string requiredBuildingDisplayName) =>
                source.ProductionRequestSystem != null
                    ? source.ProductionRequestSystem.GetCampRequestFailure(
                        source.CreateProductionRequestContext != null ? source.CreateProductionRequestContext() : default,
                        prefab,
                        price,
                        out requiredBuildingDisplayName)
                    : InvalidCampRequest(out requiredBuildingDisplayName),
            (GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess) =>
                source.ProductionRequestSystem != null
                    ? source.ProductionRequestSystem.TryRequestCampItem(
                        source.CreateProductionRequestContext != null ? source.CreateProductionRequestContext() : default,
                        prefab,
                        price,
                        focusProducerOnSuccess,
                        source.GetFrameCount?.Invoke() ?? 0,
                        out requiredBuildingDisplayName)
                    : InvalidCampRequest(out requiredBuildingDisplayName),
            productionIndex => source.ProductionRequestSystem?.CreateUnitFromSelectedBuilding(
                source.CreateProductionRequestContext != null ? source.CreateProductionRequestContext() : default,
                source.GetActiveBuildingId?.Invoke(),
                productionIndex,
                source.GetFrameCount?.Invoke() ?? 0),
            (buildingId, productionIndex) => source.ProductionRequestSystem?.CreateUnitFromBuilding(
                source.CreateProductionRequestContext != null ? source.CreateProductionRequestContext() : default,
                buildingId,
                productionIndex,
                source.GetFrameCount?.Invoke() ?? 0),
            source.DeleteSelectedBuilding,
            source.ConfirmBuildingPlacement,
            source.CancelBuildingPlacement,
            () => source.ProductionRequestSystem?.FocusLastCampProductionRequest(
                source.CreateProductionRequestContext != null ? source.CreateProductionRequestContext() : default),
            () => source.ProductionRequestSystem?.ArmNextProductionFromUi(source.GetFrameCount?.Invoke() ?? 0),
            (buildingId, pendingProductionIndex) => CancelProduction(source, buildingId, pendingProductionIndex),
            source.ClearSelectedBuilding,
            source.ExitBuildMode);
    }

    private static bool CancelProduction(Source source, int buildingId, int pendingProductionIndex)
    {
        if (source.RuntimeBuildingSystem == null ||
            source.ProductionSystem == null ||
            !source.RuntimeBuildingSystem.Buildings.TryGetValue(buildingId, out RuntimeBuildingEntity building) ||
            building == null ||
            building.PendingProductions == null ||
            pendingProductionIndex < 0 ||
            pendingProductionIndex >= building.PendingProductions.Count)
        {
            return false;
        }

        if (!source.ProductionSystem.RemovePendingAt(building.PendingProductions, pendingProductionIndex))
            return false;

        source.ProductionSystem.RebuildPendingProductionTimeline(
            building.PendingProductions,
            source.GetNow?.Invoke() ?? Time.time,
            preserveActiveProgress: pendingProductionIndex > 0);
        return true;
    }

    public BuildingUiQuerySystem.Context CreateQueryContext(Source source)
    {
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings = source.RuntimeBuildingSystem.Buildings;
        return new BuildingUiQuerySystem.Context(
            runtimeBuildings,
            source.GetActiveBuildingId,
            source.TryGetEntityManager,
            source.ProductionSystem,
            source.GetNow,
            source.HasSelectedBuilding,
            source.HasActiveBuilding,
            source.GetPlacementStatusText,
            source.GetSelectedBuildingLabel,
            source.GetSelectedBuildingDisplayName,
            source.GetSelectedBuildingDescription,
            source.TryGetSelectedBuildingHealth,
            source.TryGetSelectedBuildingPreviewPrefab,
            source.ProductionRequestSystem,
            source.CreateProductionRequestContext,
            source.IsRuntimeBuildingWall,
            source.IsRuntimeBuildingCityGenerated,
            source.TryGetRuntimeBuildingOwnerFaction,
            source.HasVisibleSelectableBuilding,
            source.TryResolveLiveUnitPreviewPrefab);
    }

    private static BuildingUiCommandSystem.CampRequestFailure InvalidCampRequest(out string requiredBuildingDisplayName)
    {
        requiredBuildingDisplayName = string.Empty;
        return BuildingUiCommandSystem.CampRequestFailure.InvalidSelection;
    }
}
