using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingUiContextCompositionSystemHelper
    {
        public Source CreateSource(
            RuntimeFactionResourceSystemHelper factionResourceSystem, BuildingDefinitionPrefabSystemHelper definitionSystem,
            RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem, BuildingProductionQueueCompositionSystemHelper productionSystem,
            BuildingProductionRequestSystemHelper productionRequestSystem, Func<BuildingProductionRequestSystemHelper.Context> createProductionRequestContext,
            Func<int?> getActiveBuildingId, Func<int> getFrameCount,
            BuildingUiQueryUiSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager, Func<float> getNow,
            Func<bool> hasSelectedBuilding, Func<bool> hasActiveBuilding,
            Func<bool> hasPendingBuildingPlacement, Func<bool> canConfirmBuildingPlacement,
            Func<string> getPlacementStatusText, Func<string> getSelectedBuildingLabel,
            Func<int> getActivePlacementCost, Func<float> getActivePlacementDurationSeconds,
            Func<string> getSelectedBuildingDisplayName, Func<string> getSelectedBuildingDescription,
            BuildingUiQueryUiSystemHelper.TryGetSelectedBuildingHealthDelegate tryGetSelectedBuildingHealth, BuildingUiQueryUiSystemHelper.TryGetSelectedBuildingPreviewPrefabDelegate tryGetSelectedBuildingPreviewPrefab,
            Func<int, bool> isRuntimeBuildingWall, Func<int, bool> isRuntimeBuildingCityGenerated,
            BuildingUiQueryUiSystemHelper.TryGetRuntimeBuildingOwnerFactionDelegate tryGetRuntimeBuildingOwnerFaction, Func<Camera, bool> hasVisibleSelectableBuilding,
            BuildingUiQueryUiSystemHelper.TryResolveLiveUnitPreviewPrefabDelegate tryResolveLiveUnitPreviewPrefab,
            Func<bool> confirmBuildingPlacement,
            Action cancelBuildingPlacement,
            Func<bool> rotateBuildingPlacement = null,
            Func<int> getPlacementCreditsCost = null, Func<Vector2Int> getPlacementFootprint = null)
        {
            return new Source(
                factionResourceSystem, definitionSystem,
                runtimeBuildingSystem, productionSystem,
                productionRequestSystem, createProductionRequestContext,
                getActiveBuildingId, getFrameCount,
                tryGetEntityManager, getNow,
                hasSelectedBuilding, hasActiveBuilding,
                hasPendingBuildingPlacement, canConfirmBuildingPlacement,
                getPlacementStatusText, getSelectedBuildingLabel,
                getActivePlacementCost, getActivePlacementDurationSeconds,
                getSelectedBuildingDisplayName, getSelectedBuildingDescription,
                tryGetSelectedBuildingHealth, tryGetSelectedBuildingPreviewPrefab,
                isRuntimeBuildingWall, isRuntimeBuildingCityGenerated,
                tryGetRuntimeBuildingOwnerFaction, hasVisibleSelectableBuilding,
                tryResolveLiveUnitPreviewPrefab,
                confirmBuildingPlacement,
                cancelBuildingPlacement,
                rotateBuildingPlacement,
                getPlacementCreditsCost, getPlacementFootprint);
        }

        public BuildingUiCommandSystemHelper.Context CreateCommandContext(Source source)
        {
            return new BuildingUiCommandSystemHelper.Context(
                () => source.RuntimeFactionResourceSystemHelper.CurrentDollars,
                () => source.CreateProductionRequestContext != null
                    ? source.CreateProductionRequestContext().MaxQueuedUnitProductions
                    : 25,
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
                    RequestCampItem(
                        source,
                        prefab,
                        price,
                        focusProducerOnSuccess,
                        out requiredBuildingDisplayName),
                source.HasPendingBuildingPlacement,
                source.CanConfirmBuildingPlacement,
                source.GetPlacementStatusText,
                source.GetActivePlacementCost,
                source.GetActivePlacementDurationSeconds,
                source.ConfirmBuildingPlacement,
                source.CancelBuildingPlacement,
                (buildingId, pendingProductionIndex) => CancelProduction(source, buildingId, pendingProductionIndex),
                source.RotateBuildingPlacement,
                source.GetActivePlacementCreditsCost, source.GetPlacementFootprint);
        }

        private static bool CancelProduction(Source source, int buildingId, int pendingProductionIndex)
        {
            if (source.ProductionRequestSystem == null ||
                !TryGetEntityManager(source, out EntityManager entityManager))
            {
                return false;
            }

            return source.ProductionRequestSystem.EnqueueAndProcessCancelProduction(
                entityManager,
                source.CreateProductionRequestContext != null ? source.CreateProductionRequestContext() : default,
                buildingId,
                pendingProductionIndex,
                source.GetNow?.Invoke() ?? UnityEngine.Time.time);
        }

        private static BuildingUiCommandSystemHelper.CampRequestFailure RequestCampItem(
            Source source,
            GameObject prefab,
            int price,
            bool focusProducerOnSuccess,
            out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            if (source.ProductionRequestSystem == null)
                return BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection;

            BuildingProductionRequestSystemHelper.Context context =
                source.CreateProductionRequestContext != null ? source.CreateProductionRequestContext() : default;
            int frameCount = source.GetFrameCount?.Invoke() ?? 0;
            if (TryGetEntityManager(source, out EntityManager entityManager))
            {
                return source.ProductionRequestSystem.EnqueueAndProcessCampItemRequest(
                    entityManager,
                    context,
                    prefab,
                    price,
                    focusProducerOnSuccess,
                    frameCount,
                    out requiredBuildingDisplayName);
            }

            return source.ProductionRequestSystem.TryRequestCampItem(
                context,
                prefab,
                price,
                focusProducerOnSuccess,
                frameCount,
                out requiredBuildingDisplayName);
        }

        private static bool TryGetEntityManager(Source source, out EntityManager entityManager)
        {
            entityManager = default;
            return source.TryGetEntityManager != null &&
                   source.TryGetEntityManager(out entityManager);
        }

        public BuildingUiQueryUiSystemHelper.Context CreateQueryContext(Source source)
        {
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings = source.RuntimeBuildingSystem.Buildings;
            return new BuildingUiQueryUiSystemHelper.Context(
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
                source.TryResolveLiveUnitPreviewPrefab,
                factionResourceEntities: null,
                tryGetFactionResourceEntity: source.RuntimeFactionResourceSystemHelper.TryGetFactionResourceEntity);
        }

        private static BuildingUiCommandSystemHelper.CampRequestFailure InvalidCampRequest(out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            return BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection;
        }
    }
}
