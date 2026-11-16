using System;
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

            if (context.TryGetEntityManager != null && context.TryGetEntityManager(out var catalogEm) &&
                !SkirmishCatalogPolicy.Allows(catalogEm, prefab, context.ConfiguredDefinitionsByPrefab != null && context.ConfiguredDefinitionsByPrefab.ContainsKey(prefab)))
                return CampRequestFailure.InvalidSelection;

            if (context.TryGetEntityManager != null && context.TryGetEntityManager(out var readinessWorld))
            {
                var readinessFailure = SkirmishNativeProduction.RequestFailure(readinessWorld, prefab,
                    context.ConfiguredDefinitionsByPrefab != null && context.ConfiguredDefinitionsByPrefab.ContainsKey(prefab));
                if (readinessFailure != CampRequestFailure.None)
                {
                    if (readinessFailure == CampRequestFailure.MissingProducerBuilding)
                        if (!SkirmishNativeProduction.TryRequiredProducerDisplayName(readinessWorld, prefab,
                            context.ConfiguredSpawnableDefinitions, out requiredBuildingDisplayName))
                            TryGetRequiredProducerDisplayName(context, prefab, out requiredBuildingDisplayName);
                    return readinessFailure;
                }
            }

            if (context.ConfiguredDefinitionsByPrefab != null &&
                context.ConfiguredDefinitionsByPrefab.TryGetValue(prefab, out BuildingDefinition buildingDefinition))
            {
                int buildingMaterialsCost = Mathf.Max(0, buildingDefinition?.MaterialsCost ?? 0);
                int authoredCreditsCost = 0;
                // Defense prices use both authored components to resolve their
                // Materials-only price. Dropping the legacy component here made
                // preflight disagree with the displayed and reserved amount.
                if (context.TryGetEntityManager != null && context.TryGetEntityManager(out var costWorld))
                {
                    using var resources = costWorld.CreateEntityQuery(typeof(FactionEconomy));
                    using var economies = resources.ToComponentDataArray<FactionEconomy>(Unity.Collections.Allocator.Temp);
                    foreach (var economy in economies)
                        if (FactionIdentity.IsPlayerControlled(economy.FactionId) &&
                            economy.MaterialsOnlyConstruction == Game.Tactical.Contracts.MissionConstructionCostPolicy.DefenseMaterialsV1)
                            authoredCreditsCost = Mathf.Max(0, buildingDefinition?.CreditsCost ?? 0);
                }
                if (context.EvaluateConstructionResources == null)
                    return context.ResourceMaterials < buildingMaterialsCost
                        ? CampRequestFailure.InsufficientMaterials
                        : CampRequestFailure.None;

                return BuildingCampItemCommandPolicySystemHelper.MapConstructionResourceFailure(
                    context.EvaluateConstructionResources(authoredCreditsCost, buildingMaterialsCost));
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
                    out int populationProducerId,
                    out int populationProductionIndex,
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

            if (context.TryGetEntityManager != null && context.TryGetEntityManager(out var populationEm) &&
                context.RuntimeBuildings != null && context.RuntimeBuildings.TryGetValue(populationProducerId, out var populationProducer) &&
                !SkirmishPopulationPolicy.CanQueue(populationEm, context.RuntimeBuildings, populationProducer, prefab, populationProductionIndex))
                return SkirmishNativeProduction.TrySession(populationEm, out _) ? CampRequestFailure.ArmyCapacityReached :
                    prefab.name.ToLowerInvariant().Contains("soldier") ? CampRequestFailure.InfantryLimit : CampRequestFailure.LogisticsLimit;

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
            float now,
            out string requiredBuildingDisplayName)
        {
            return FromSemanticFailure(UnitProductionSemanticRequestOwner.Execute<GameObject, RuntimeBuildingEntity, RuntimeBuildingEntity.PendingProduction>(
                new NativeUnitProductionRequestAdapter(this, context, frameCount),
                prefab, price, focusProducerOnSuccess, frameCount, now,
                out requiredBuildingDisplayName));
        }

        private sealed class NativeUnitProductionRequestAdapter : UnitProductionSemanticRequestOwner.IAdapter<GameObject, RuntimeBuildingEntity, RuntimeBuildingEntity.PendingProduction>
        {
            readonly BuildingProductionRequestSystemHelper owner;
            readonly Context context;
            readonly int frameCount;
            internal NativeUnitProductionRequestAdapter(BuildingProductionRequestSystemHelper owner, Context context, int frameCount)
            { this.owner = owner; this.context = context; this.frameCount = frameCount; }
            public UnitProductionSemanticRequestOwner.Failure Evaluate(GameObject prefab, int price, out string required) => ToSemanticFailure(owner.GetCampRequestFailure(context, prefab, price, out required));
            public bool IsConfiguredSpawnable(GameObject prefab) => context.ConfiguredDefinitionsByPrefab != null && context.ConfiguredDefinitionsByPrefab.ContainsKey(prefab);
            public bool BeginConfiguredPlacement(GameObject prefab) => context.BeginPlacementForConfiguredSpawnable != null && context.BeginPlacementForConfiguredSpawnable(prefab);
            public bool HasGlobalQueueCapacity() => BuildingProductionRequestSystemHelper.HasGlobalQueueCapacity(context);
            public bool TryResolveCosts(GameObject prefab, int fallback, out int credits, out int materials) => TryResolveUnitResourceCosts(context, prefab, fallback, out credits, out materials);
            public bool TryFindProducer(GameObject prefab, bool requireCapacity, out int id, out int index, out string display) => owner.TryFindFirstFriendlyProducerBuilding(context, prefab, requireCapacity, out id, out index, out display);
            public bool TryGetRequiredProducerDisplayName(GameObject prefab, out string display) => owner.TryGetRequiredProducerDisplayName(context, prefab, out display);
            public bool TryFindOperationMapProducer(GameObject prefab) => owner.TryFindFirstFriendlyOperationMapProducer(context, prefab, out _, out _, out _);
            public bool HasGlobalQueueCapacityAfterOperationMapFailure() => BuildingProductionRequestSystemHelper.HasGlobalQueueCapacity(context);
            public UnitProductionSemanticRequestOwner.Failure Spend(int credits, int materials) => ToSemanticFailure(TrySpendUnitProductionResources(context, credits, materials));
            public void Restore(int credits, int materials) => RestoreUnitProductionResources(context, credits, materials);
            public bool TryQueueOperationMap(GameObject prefab, float now) => owner.TryEnqueueFriendlyOperationMapProduction(context, prefab, now, out _, out _, out _);
            public bool IsMissionReceipt() => context.TryGetEntityManager != null && context.TryGetEntityManager(out var em) && SkirmishNativeProduction.TrySession(em, out _);
            public bool TryGetProducer(int id, out RuntimeBuildingEntity producer)
            {
                producer = null;
                return context.RuntimeBuildings != null && context.RuntimeBuildings.TryGetValue(id, out producer) && producer != null;
            }
            public void FocusProducer(RuntimeBuildingEntity producer, GameObject prefab) => owner.SelectBuildingForProductionRequest(context, producer, prefab);
            public UnitProductionSemanticRequestOwner.QueueOutcome TryQueueUnit(RuntimeBuildingEntity producer, int index, int ignoredFrameCount, float now, out RuntimeBuildingEntity.PendingProduction pending)
            {
                bool accepted = owner.TryCreateUnitFromBuilding(context, producer.Id, index, frameCount, frameCount, now, out byte resultCode);
                pending = accepted && producer.PendingProductions != null && producer.PendingProductions.Count > 0
                    ? producer.PendingProductions[producer.PendingProductions.Count - 1]
                    : null;
                if (accepted) return UnitProductionSemanticRequestOwner.QueueOutcome.Queued;
                return resultCode switch
                {
                    BuildingUiProductionCommandResultElement.GlobalQueueFull => UnitProductionSemanticRequestOwner.QueueOutcome.GlobalQueueFull,
                    BuildingUiProductionCommandResultElement.QueueFull => UnitProductionSemanticRequestOwner.QueueOutcome.ProducerQueueFull,
                    _ => UnitProductionSemanticRequestOwner.QueueOutcome.Rejected
                };
            }
            public void AdjustAcceptedPending(RuntimeBuildingEntity producer, RuntimeBuildingEntity.PendingProduction pending, int credits, int materials)
            {
                if (pending == null || context.TryGetEntityManager == null || !context.TryGetEntityManager(out var em)) return;
                int refundableMaterials = materials;
                using var query = em.CreateEntityQuery(typeof(FactionEconomy));
                using var economies = query.ToComponentDataArray<FactionEconomy>(Unity.Collections.Allocator.Temp);
                foreach (var economy in economies)
                    if (FactionIdentity.IsPlayerControlled(economy.FactionId) &&
                        Game.Tactical.Contracts.MissionConstructionCostPolicy.TryResolve(economy.MaterialsOnlyConstruction,
                            credits, materials, out _, out int effectiveMaterials))
                        refundableMaterials = effectiveMaterials;
                pending.RefundableMaterials = refundableMaterials;
                pending.PaidQuantity = Mathf.Max(1, pending.RemainingQuantity);
            }
            public void RecordUnitOrdered(GameObject prefab) => context.RecordUnitOrdered?.Invoke(prefab);
        }

        private static UnitProductionSemanticRequestOwner.Failure ToSemanticFailure(CampRequestFailure failure) => failure switch
        {
            CampRequestFailure.None => UnitProductionSemanticRequestOwner.Failure.None,
            CampRequestFailure.NotEnoughMoney => UnitProductionSemanticRequestOwner.Failure.NotEnoughMoney,
            CampRequestFailure.MissingProducerBuilding => UnitProductionSemanticRequestOwner.Failure.MissingProducerBuilding,
            CampRequestFailure.InvalidSelection => UnitProductionSemanticRequestOwner.Failure.InvalidSelection,
            CampRequestFailure.ProductionQueueFull => UnitProductionSemanticRequestOwner.Failure.ProducerQueueFull,
            CampRequestFailure.GlobalProductionQueueFull => UnitProductionSemanticRequestOwner.Failure.GlobalQueueFull,
            CampRequestFailure.InsufficientCredits => UnitProductionSemanticRequestOwner.Failure.InsufficientCredits,
            CampRequestFailure.InsufficientMaterials => UnitProductionSemanticRequestOwner.Failure.InsufficientMaterials,
            CampRequestFailure.InsufficientCreditsAndMaterials => UnitProductionSemanticRequestOwner.Failure.InsufficientCreditsAndMaterials,
            CampRequestFailure.InfantryLimit => UnitProductionSemanticRequestOwner.Failure.InfantryLimit,
            CampRequestFailure.LogisticsLimit => UnitProductionSemanticRequestOwner.Failure.LogisticsLimit,
            CampRequestFailure.ReadinessEstablishedRequired => UnitProductionSemanticRequestOwner.Failure.ReadinessEstablishedRequired,
            CampRequestFailure.ReadinessFullArsenalRequired => UnitProductionSemanticRequestOwner.Failure.ReadinessFullArsenalRequired,
            CampRequestFailure.ArmyCapacityReached => UnitProductionSemanticRequestOwner.Failure.ArmyCapacityReached,
            CampRequestFailure.ResearchInProgress => UnitProductionSemanticRequestOwner.Failure.ResearchInProgress,
            _ => UnitProductionSemanticRequestOwner.Failure.InvalidSelection
        };

        private static CampRequestFailure FromSemanticFailure(UnitProductionSemanticRequestOwner.Failure failure) => failure switch
        {
            UnitProductionSemanticRequestOwner.Failure.None => CampRequestFailure.None,
            UnitProductionSemanticRequestOwner.Failure.NotEnoughMoney => CampRequestFailure.NotEnoughMoney,
            UnitProductionSemanticRequestOwner.Failure.MissingProducerBuilding => CampRequestFailure.MissingProducerBuilding,
            UnitProductionSemanticRequestOwner.Failure.InvalidSelection => CampRequestFailure.InvalidSelection,
            UnitProductionSemanticRequestOwner.Failure.ProducerQueueFull => CampRequestFailure.ProductionQueueFull,
            UnitProductionSemanticRequestOwner.Failure.GlobalQueueFull => CampRequestFailure.GlobalProductionQueueFull,
            UnitProductionSemanticRequestOwner.Failure.InsufficientCredits => CampRequestFailure.InsufficientCredits,
            UnitProductionSemanticRequestOwner.Failure.InsufficientMaterials => CampRequestFailure.InsufficientMaterials,
            UnitProductionSemanticRequestOwner.Failure.InsufficientCreditsAndMaterials => CampRequestFailure.InsufficientCreditsAndMaterials,
            UnitProductionSemanticRequestOwner.Failure.InfantryLimit => CampRequestFailure.InfantryLimit,
            UnitProductionSemanticRequestOwner.Failure.LogisticsLimit => CampRequestFailure.LogisticsLimit,
            UnitProductionSemanticRequestOwner.Failure.ReadinessEstablishedRequired => CampRequestFailure.ReadinessEstablishedRequired,
            UnitProductionSemanticRequestOwner.Failure.ReadinessFullArsenalRequired => CampRequestFailure.ReadinessFullArsenalRequired,
            UnitProductionSemanticRequestOwner.Failure.ArmyCapacityReached => CampRequestFailure.ArmyCapacityReached,
            UnitProductionSemanticRequestOwner.Failure.ResearchInProgress => CampRequestFailure.ResearchInProgress,
            _ => CampRequestFailure.InvalidSelection
        };

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

namespace Game.Runtime
{
// Shared ordered unit-request executor. Concrete adapters only read and mutate
// their actual world/context owners; legality ordering, spend/refund sequencing,
// focus timing, and queue-result mapping stay here.
internal static class UnitProductionSemanticRequestOwner
{
    internal enum Failure
    {
        None,
        NotEnoughMoney,
        MissingProducerBuilding,
        InvalidSelection,
        ProducerQueueFull,
        GlobalQueueFull,
        InsufficientCredits,
        InsufficientMaterials,
        InsufficientCreditsAndMaterials,
        InfantryLimit,
        LogisticsLimit,
        ReadinessEstablishedRequired,
        ReadinessFullArsenalRequired,
        ArmyCapacityReached,
        ResearchInProgress
    }

    internal enum QueueOutcome { Queued, ProducerQueueFull, GlobalQueueFull, Rejected }

    internal interface IAdapter<TPrefab, TProducer, TPending> where TProducer : class where TPending : class
    {
        Failure Evaluate(TPrefab prefab, int price, out string requiredBuildingDisplayName);
        bool IsConfiguredSpawnable(TPrefab prefab);
        bool BeginConfiguredPlacement(TPrefab prefab);
        bool HasGlobalQueueCapacity();
        bool TryResolveCosts(TPrefab prefab, int fallbackMaterialsCost, out int creditsCost, out int materialsCost);
        bool TryFindProducer(TPrefab prefab, bool requireQueueCapacity, out int buildingId, out int productionIndex, out string displayName);
        bool TryGetRequiredProducerDisplayName(TPrefab prefab, out string displayName);
        bool TryFindOperationMapProducer(TPrefab prefab);
        bool HasGlobalQueueCapacityAfterOperationMapFailure();
        Failure Spend(int creditsCost, int materialsCost);
        void Restore(int creditsCost, int materialsCost);
        bool TryQueueOperationMap(TPrefab prefab, float now);
        bool IsMissionReceipt();
        bool TryGetProducer(int buildingId, out TProducer producer);
        void FocusProducer(TProducer producer, TPrefab prefab);
        QueueOutcome TryQueueUnit(TProducer producer, int productionIndex, int frameCount, float now, out TPending pending);
        void AdjustAcceptedPending(TProducer producer, TPending pending, int creditsCost, int materialsCost);
        void RecordUnitOrdered(TPrefab prefab);
    }

    internal static Failure Execute<TPrefab, TProducer, TPending>(
        IAdapter<TPrefab, TProducer, TPending> adapter,
        TPrefab prefab,
        int price,
        bool focusProducerOnSuccess,
        int frameCount,
        float now,
        out string requiredBuildingDisplayName)
        where TProducer : class
        where TPending : class
    {
        if (adapter == null) throw new ArgumentNullException(nameof(adapter));
        requiredBuildingDisplayName = string.Empty;
        var failure = adapter.Evaluate(prefab, price, out requiredBuildingDisplayName);
        if (failure != Failure.None) return failure;

        if (adapter.IsConfiguredSpawnable(prefab))
            return adapter.BeginConfiguredPlacement(prefab)
                ? Failure.None
                : Failure.InvalidSelection;

        if (!adapter.HasGlobalQueueCapacity())
            return Failure.GlobalQueueFull;

        if (!adapter.TryResolveCosts(prefab, price, out int creditsCost, out int materialsCost))
            return Failure.InvalidSelection;

        if (!adapter.TryFindProducer(prefab, true, out int producerBuildingId, out int productionIndex, out _))
        {
            if (adapter.TryFindProducer(prefab, false, out _, out _, out string fullProducerDisplayName))
            {
                requiredBuildingDisplayName = fullProducerDisplayName;
                return Failure.ProducerQueueFull;
            }

            if (adapter.TryFindOperationMapProducer(prefab))
            {
                failure = adapter.Spend(creditsCost, materialsCost);
                if (failure != Failure.None) return failure;
                if (!adapter.TryQueueOperationMap(prefab, now))
                {
                    adapter.Restore(creditsCost, materialsCost);
                    return adapter.HasGlobalQueueCapacityAfterOperationMapFailure()
                        ? Failure.InvalidSelection
                        : Failure.GlobalQueueFull;
                }

                adapter.RecordUnitOrdered(prefab);
                return Failure.None;
            }

            adapter.TryGetRequiredProducerDisplayName(prefab, out requiredBuildingDisplayName);
            return Failure.MissingProducerBuilding;
        }

        bool missionReceipt = adapter.IsMissionReceipt();
        if (!missionReceipt)
        {
            failure = adapter.Spend(creditsCost, materialsCost);
            if (failure != Failure.None) return failure;
        }

        if (!adapter.TryGetProducer(producerBuildingId, out TProducer producer) || producer == null)
        {
            if (!missionReceipt) adapter.Restore(creditsCost, materialsCost);
            return Failure.InvalidSelection;
        }

        if (focusProducerOnSuccess) adapter.FocusProducer(producer, prefab);
        QueueOutcome queueOutcome = adapter.TryQueueUnit(producer, productionIndex, frameCount, now, out TPending pending);
        if (queueOutcome != QueueOutcome.Queued)
        {
            if (!missionReceipt) adapter.Restore(creditsCost, materialsCost);
            return queueOutcome switch
            {
                QueueOutcome.GlobalQueueFull => Failure.GlobalQueueFull,
                QueueOutcome.ProducerQueueFull => Failure.ProducerQueueFull,
                _ => Failure.InvalidSelection
            };
        }

        if (!missionReceipt) adapter.AdjustAcceptedPending(producer, pending, creditsCost, materialsCost);
        adapter.RecordUnitOrdered(prefab);
        return Failure.None;
    }
}
}
