using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingUiContextCompositionSystemHelper
    {
        public readonly struct Source
        {
            public readonly RuntimeFactionResourceSystemHelper RuntimeFactionResourceSystemHelper;
            public readonly BuildingDefinitionPrefabSystemHelper DefinitionSystem;
            public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
            public readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;
            public readonly BuildingProductionRequestSystemHelper ProductionRequestSystem;
            public readonly Func<BuildingProductionRequestSystemHelper.Context> CreateProductionRequestContext;
            public readonly Func<int?> GetActiveBuildingId;
            public readonly Func<int> GetFrameCount;
            public readonly BuildingUiQueryUiSystemHelper.TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly Func<float> GetNow;
            public readonly Func<bool> HasSelectedBuilding;
            public readonly Func<bool> HasActiveBuilding;
            public readonly Func<bool> HasPendingBuildingPlacement;
            public readonly Func<bool> CanConfirmBuildingPlacement;
            public readonly Func<string> GetPlacementStatusText;
            public readonly Func<string> GetSelectedBuildingLabel;
            public readonly Func<int> GetActivePlacementCost;
            public readonly Func<float> GetActivePlacementDurationSeconds;
            public readonly Func<string> GetSelectedBuildingDisplayName;
            public readonly Func<string> GetSelectedBuildingDescription;
            public readonly BuildingUiQueryUiSystemHelper.TryGetSelectedBuildingHealthDelegate TryGetSelectedBuildingHealth;
            public readonly BuildingUiQueryUiSystemHelper.TryGetSelectedBuildingPreviewPrefabDelegate TryGetSelectedBuildingPreviewPrefab;
            public readonly Func<int, bool> IsRuntimeBuildingWall;
            public readonly Func<int, bool> IsRuntimeBuildingCityGenerated;
            public readonly BuildingUiQueryUiSystemHelper.TryGetRuntimeBuildingOwnerFactionDelegate TryGetRuntimeBuildingOwnerFaction;
            public readonly Func<Camera, bool> HasVisibleSelectableBuilding;
            public readonly BuildingUiQueryUiSystemHelper.TryResolveLiveUnitPreviewPrefabDelegate TryResolveLiveUnitPreviewPrefab;
            public readonly Func<bool> ConfirmBuildingPlacement;
            public readonly Action CancelBuildingPlacement;
            public readonly Func<bool> RotateBuildingPlacement;
            public readonly Func<int> GetActivePlacementCreditsCost;
            public readonly Func<Vector2Int> GetPlacementFootprint;

            public Source(
                RuntimeFactionResourceSystemHelper factionResourceSystem,
                BuildingDefinitionPrefabSystemHelper definitionSystem,
                RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
                BuildingProductionQueueCompositionSystemHelper productionSystem,
                BuildingProductionRequestSystemHelper productionRequestSystem,
                Func<BuildingProductionRequestSystemHelper.Context> createProductionRequestContext,
                Func<int?> getActiveBuildingId,
                Func<int> getFrameCount,
                BuildingUiQueryUiSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
                Func<float> getNow,
                Func<bool> hasSelectedBuilding,
                Func<bool> hasActiveBuilding,
                Func<bool> hasPendingBuildingPlacement,
                Func<bool> canConfirmBuildingPlacement,
                Func<string> getPlacementStatusText,
                Func<string> getSelectedBuildingLabel,
                Func<int> getActivePlacementCost,
                Func<float> getActivePlacementDurationSeconds,
                Func<string> getSelectedBuildingDisplayName,
                Func<string> getSelectedBuildingDescription,
                BuildingUiQueryUiSystemHelper.TryGetSelectedBuildingHealthDelegate tryGetSelectedBuildingHealth,
                BuildingUiQueryUiSystemHelper.TryGetSelectedBuildingPreviewPrefabDelegate tryGetSelectedBuildingPreviewPrefab,
                Func<int, bool> isRuntimeBuildingWall,
                Func<int, bool> isRuntimeBuildingCityGenerated,
                BuildingUiQueryUiSystemHelper.TryGetRuntimeBuildingOwnerFactionDelegate tryGetRuntimeBuildingOwnerFaction,
                Func<Camera, bool> hasVisibleSelectableBuilding,
                BuildingUiQueryUiSystemHelper.TryResolveLiveUnitPreviewPrefabDelegate tryResolveLiveUnitPreviewPrefab,
                Func<bool> confirmBuildingPlacement,
                Action cancelBuildingPlacement,
                Func<bool> rotateBuildingPlacement = null,
                Func<int> getPlacementCreditsCost = null, Func<Vector2Int> getPlacementFootprint = null)
            {
                RuntimeFactionResourceSystemHelper = factionResourceSystem;
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
                HasPendingBuildingPlacement = hasPendingBuildingPlacement;
                CanConfirmBuildingPlacement = canConfirmBuildingPlacement;
                GetPlacementStatusText = getPlacementStatusText;
                GetSelectedBuildingLabel = getSelectedBuildingLabel;
                GetActivePlacementCost = getActivePlacementCost;
                GetActivePlacementDurationSeconds = getActivePlacementDurationSeconds;
                GetSelectedBuildingDisplayName = getSelectedBuildingDisplayName;
                GetSelectedBuildingDescription = getSelectedBuildingDescription;
                TryGetSelectedBuildingHealth = tryGetSelectedBuildingHealth;
                TryGetSelectedBuildingPreviewPrefab = tryGetSelectedBuildingPreviewPrefab;
                IsRuntimeBuildingWall = isRuntimeBuildingWall;
                IsRuntimeBuildingCityGenerated = isRuntimeBuildingCityGenerated;
                TryGetRuntimeBuildingOwnerFaction = tryGetRuntimeBuildingOwnerFaction;
                HasVisibleSelectableBuilding = hasVisibleSelectableBuilding;
                TryResolveLiveUnitPreviewPrefab = tryResolveLiveUnitPreviewPrefab;
                ConfirmBuildingPlacement = confirmBuildingPlacement;
                CancelBuildingPlacement = cancelBuildingPlacement;
                RotateBuildingPlacement = rotateBuildingPlacement;
                GetActivePlacementCreditsCost = getPlacementCreditsCost;
                GetPlacementFootprint = getPlacementFootprint;
            }
        }
    }
}
