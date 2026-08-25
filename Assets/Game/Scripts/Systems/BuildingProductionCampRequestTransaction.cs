using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    using CampRequestFailure = BuildingUiCommandSystemHelper.CampRequestFailure;

    internal sealed partial class BuildingProductionRequestSystemHelper
    {
        public CampRequestFailure GetCampRequestFailure(
            Context context,
            GameObject prefab,
            int price,
            out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            if (prefab == null)
                return CampRequestFailure.InvalidSelection;

            if (context.ConfiguredDefinitionsByPrefab != null &&
                context.ConfiguredDefinitionsByPrefab.TryGetValue(prefab, out BuildingDefinition buildingDefinition))
            {
                int buildingMaterialsCost = Mathf.Max(0, buildingDefinition?.MaterialsCost ?? 0);
                if (context.EvaluateConstructionResources == null)
                    return context.ResourceMaterials < buildingMaterialsCost
                        ? CampRequestFailure.InsufficientMaterials
                        : CampRequestFailure.None;

                return BuildingCampItemCommandPolicySystemHelper.MapConstructionResourceFailure(
                    context.EvaluateConstructionResources(0, buildingMaterialsCost));
            }

            if (!TryResolveUnitResourceCosts(
                    context,
                    prefab,
                    price,
                    out int creditsCost,
                    out int materialsCost))
                return CampRequestFailure.InvalidSelection;

            if (context.TryResolveUnitResourceCosts != null)
            {
                if (context.EvaluateConstructionResources == null)
                    return CampRequestFailure.InvalidSelection;

                CampRequestFailure resourceFailure =
                    BuildingCampItemCommandPolicySystemHelper.MapConstructionResourceFailure(
                        context.EvaluateConstructionResources(creditsCost, materialsCost));
                if (resourceFailure != CampRequestFailure.None)
                    return resourceFailure;
            }
            else if (context.ResourceMaterials < materialsCost)
            {
                return CampRequestFailure.InsufficientMaterials;
            }

            if (!TryFindFirstFriendlyProducerBuilding(
                    context,
                    prefab,
                    requireQueueCapacity: false,
                    out _,
                    out _,
                    out string producerDisplayName))
            {
                if (TryFindFirstFriendlyOperationMapProducer(
                        context,
                        prefab,
                        out Entity operationMapProducer,
                        out _,
                        out _) &&
                    context.TryGetEntityManager != null &&
                    context.TryGetEntityManager(out EntityManager em) &&
                    em.HasComponent<OperationMapBuildingProductionQueueComponent>(operationMapProducer) &&
                    em.HasBuffer<OperationMapBuildingUnitProductionRequest>(operationMapProducer))
                {
                    return HasGlobalQueueCapacity(context)
                        ? CampRequestFailure.None
                        : CampRequestFailure.GlobalProductionQueueFull;
                }

                TryGetRequiredProducerDisplayName(context, prefab, out requiredBuildingDisplayName);
                return CampRequestFailure.MissingProducerBuilding;
            }

            if (!HasGlobalQueueCapacity(context))
                return CampRequestFailure.GlobalProductionQueueFull;

            if (TryFindFirstFriendlyProducerBuilding(
                    context,
                    prefab,
                    requireQueueCapacity: true,
                    out _,
                    out _,
                    out _))
                return CampRequestFailure.None;

            requiredBuildingDisplayName = producerDisplayName;
            return CampRequestFailure.ProductionQueueFull;
        }

        public CampRequestFailure TryRequestCampItem(
            Context context,
            GameObject prefab,
            int price,
            bool focusProducerOnSuccess,
            int frameCount,
            out string requiredBuildingDisplayName)
        {
            CampRequestFailure failure = GetCampRequestFailure(context, prefab, price, out requiredBuildingDisplayName);
            if (failure != CampRequestFailure.None)
                return failure;

            if (context.ConfiguredDefinitionsByPrefab != null && context.ConfiguredDefinitionsByPrefab.ContainsKey(prefab))
            {
                if (context.BeginPlacementForConfiguredSpawnable == null ||
                    !context.BeginPlacementForConfiguredSpawnable(prefab))
                    return CampRequestFailure.InvalidSelection;

                return CampRequestFailure.None;
            }

            if (!HasGlobalQueueCapacity(context))
                return CampRequestFailure.GlobalProductionQueueFull;

            if (!TryResolveUnitResourceCosts(
                    context,
                    prefab,
                    price,
                    out int creditsCost,
                    out int materialsCost))
                return CampRequestFailure.InvalidSelection;

            if (!TryFindFirstFriendlyProducerBuilding(
                    context,
                    prefab,
                    requireQueueCapacity: true,
                    out int producerBuildingId,
                    out int productionIndex,
                    out _))
            {
                if (TryFindFirstFriendlyProducerBuilding(
                        context,
                        prefab,
                        requireQueueCapacity: false,
                        out _,
                        out _,
                        out string fullProducerDisplayName))
                {
                    requiredBuildingDisplayName = fullProducerDisplayName;
                    return CampRequestFailure.ProductionQueueFull;
                }

                if (TryFindFirstFriendlyOperationMapProducer(context, prefab, out _, out _, out _))
                {
                    CampRequestFailure spendFailure = TrySpendUnitProductionResources(
                        context,
                        creditsCost,
                        materialsCost);
                    if (spendFailure != CampRequestFailure.None)
                        return spendFailure;

                    if (!TryEnqueueFriendlyOperationMapProduction(
                            context,
                            prefab,
                            Time.time,
                            out _,
                            out _,
                            out _))
                    {
                        RestoreUnitProductionResources(context, creditsCost, materialsCost);
                        return HasGlobalQueueCapacity(context)
                            ? CampRequestFailure.InvalidSelection
                            : CampRequestFailure.GlobalProductionQueueFull;
                    }

                    context.RecordUnitOrdered?.Invoke(prefab);
                    return CampRequestFailure.None;
                }

                TryGetRequiredProducerDisplayName(context, prefab, out requiredBuildingDisplayName);
                return CampRequestFailure.MissingProducerBuilding;
            }

            CampRequestFailure buildingSpendFailure = TrySpendUnitProductionResources(
                context,
                creditsCost,
                materialsCost);
            if (buildingSpendFailure != CampRequestFailure.None)
                return buildingSpendFailure;

            if (context.RuntimeBuildings == null ||
                !context.RuntimeBuildings.TryGetValue(producerBuildingId, out RuntimeBuildingEntity producerBuilding) ||
                producerBuilding == null)
            {
                RestoreUnitProductionResources(context, creditsCost, materialsCost);
                return CampRequestFailure.InvalidSelection;
            }

            if (focusProducerOnSuccess)
                SelectBuildingForProductionRequest(context, producerBuilding, prefab);

            if (!TryCreateUnitFromBuilding(
                    context,
                    producerBuildingId,
                    productionIndex,
                    frameCount,
                    frameCount,
                    out byte resultCode))
            {
                RestoreUnitProductionResources(context, creditsCost, materialsCost);
                return resultCode switch
                {
                    BuildingUiProductionCommandResultElement.GlobalQueueFull => CampRequestFailure.GlobalProductionQueueFull,
                    BuildingUiProductionCommandResultElement.QueueFull => CampRequestFailure.ProductionQueueFull,
                    _ => CampRequestFailure.InvalidSelection
                };
            }

            context.RecordUnitOrdered?.Invoke(prefab);
            return CampRequestFailure.None;
        }

        private static bool TryResolveUnitResourceCosts(
            Context context,
            GameObject prefab,
            int fallbackMaterialsCost,
            out int creditsCost,
            out int materialsCost)
        {
            creditsCost = 0;
            materialsCost = Mathf.Max(0, fallbackMaterialsCost);
            if (context.TryResolveUnitResourceCosts == null)
                return true;

            if (!context.TryResolveUnitResourceCosts(
                    prefab,
                    materialsCost,
                    out creditsCost,
                    out materialsCost))
                return false;

            creditsCost = Mathf.Max(0, creditsCost);
            materialsCost = Mathf.Max(0, materialsCost);
            return true;
        }

        private static CampRequestFailure TrySpendUnitProductionResources(
            Context context,
            int creditsCost,
            int materialsCost)
        {
            if (context.TrySpendConstructionResources != null)
            {
                if (context.TryRestoreConstructionResources == null)
                    return CampRequestFailure.InvalidSelection;

                return BuildingCampItemCommandPolicySystemHelper.MapConstructionResourceFailure(
                    context.TrySpendConstructionResources(creditsCost, materialsCost));
            }

            return context.TrySpendMaterials != null && context.TrySpendMaterials(materialsCost)
                ? CampRequestFailure.None
                : CampRequestFailure.InsufficientMaterials;
        }

        private static void RestoreUnitProductionResources(
            Context context,
            int creditsCost,
            int materialsCost)
        {
            if (context.TryRestoreConstructionResources != null)
            {
                FactionConstructionResourceMutationResult restoreResult =
                    context.TryRestoreConstructionResources(creditsCost, materialsCost);
                if (restoreResult != FactionConstructionResourceMutationResult.Applied)
                    context.LogWarning?.Invoke(
                        $"Unable to restore rejected unit production resources: credits={creditsCost} materials={materialsCost} result={restoreResult}.");
                return;
            }

            context.RefundMaterials?.Invoke(materialsCost);
        }
    }
}
