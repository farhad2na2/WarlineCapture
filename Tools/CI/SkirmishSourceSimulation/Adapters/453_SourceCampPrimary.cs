using System;
using Game.Components;
using Game.UI.Runtime;
using Game.UI.Contracts;
namespace Game.Runtime
{
    internal delegate bool SourceCampQueueRequest(int producerId, int productionIndex, float now, out SourcePendingProduction pending);
    internal sealed class SourceCampPrimaryResult
    {
        internal readonly bool Accepted;
        internal readonly string SourceReason;
        internal readonly SourcePendingProduction Pending;
        internal readonly BuildingUiCommandFailure? UiFailure;
        internal SourceCampPrimaryResult(bool accepted, string reason, SourcePendingProduction pending = null, BuildingUiCommandFailure? failure = null)
        { Accepted = accepted; SourceReason = reason; Pending = pending; UiFailure = failure; }
    }

    internal sealed partial class SourceCampAvailability
    {
        internal SourceCampPrimaryResult RequestUnitSemantic(
            SourceUnitPrefab prefab,
            int materialsFallback,
            SourceCampQueueRequest actualQueue,
            float currentSourceTime)
        {
            if (prefab == null)
                return new SourceCampPrimaryResult(false, "InvalidSelection", failure: BuildingUiCommandFailure.InvalidSelection);
            if (actualQueue == null)
                return new SourceCampPrimaryResult(false, "StillConnecting");
            if (float.IsNaN(currentSourceTime) || float.IsInfinity(currentSourceTime) || currentSourceTime < 0)
                throw new NotSupportedException("Current source production time required");
            PreflightSemanticRequest();
            RequireS003FixtureSetupIdentity();

            using var scope = new SourceCampReadScope(em, null, null, map, fixtureScope, session);
            context.ResourceMaterials = resources.CurrentMaterials;
            map.Identity(prefab);
            var adapter = new SourceUnitSemanticRequestAdapter(this, default, null, prefab, actualQueue, currentSourceTime);
            var semanticFailure = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<SourceUnitPrefab, RuntimeBuildingEntity, SourcePendingProduction>(
                adapter, prefab, materialsFallback, false, 0, currentSourceTime, out string required);
            var failure = SourceCampUiAdapter.ToCampFailure(semanticFailure);
            if (failure != BuildingUiCommandSystemHelper.CampRequestFailure.None)
                return new SourceCampPrimaryResult(false, adapter.RejectionReason ?? failure.ToString(), failure: SourceCampUiFailureMap.Map(failure));
            if (adapter.Pending == null || adapter.Pending.ReceiptId == 0 || adapter.Pending.ReceiptOwner != session)
                throw new InvalidOperationException("Accepted source queue omitted its actual paid pending receipt");
            return new SourceCampPrimaryResult(true, "OriginalQueueAccepted", adapter.Pending);
        }

        void RequireS003FixtureSetupIdentity()
        {
            if (fixtureScope?.Authority?.IsS003Fixture != true) return;
            if (!em.Exists(session) || !em.HasComponent<SkirmishProductionCatalogRecord>(session))
                throw new NotSupportedException("S003 fixture requires its current source production catalog");
            var catalog = em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;
            if (catalog == null || !fixtureScope.Authority.MatchesSetup(catalog.Setup))
                throw new NotSupportedException("Current source production catalog must retain the actual compiled T6 S003 setup object");
        }

        internal SourceCampPrimaryResult RequestUnitSemantic(
            BuildDrawerCatalogItem selected,
            CatalogPublicFacts facts,
            bool commandBindingKnown,
            SourceCampQueueRequest actualQueue,
            float currentSourceTime)
        {
            if (selected.Prefab == null)
                return new SourceCampPrimaryResult(false, "InvalidSelection", failure: BuildingUiCommandFailure.InvalidSelection);
            if (!commandBindingKnown)
                throw new NotSupportedException("Source primary command binding unknown");
            if (actualQueue == null)
                return new SourceCampPrimaryResult(false, "StillConnecting");
            if (float.IsNaN(currentSourceTime) || float.IsInfinity(currentSourceTime) || currentSourceTime < 0)
                throw new NotSupportedException("Current source production time required");
            if (selected.Category == BuildDrawerCategory.Buildings)
                throw new NotSupportedException("Building primary transaction belongs to shared construction owner");
            RequireS003FixtureSetupIdentity();

            using var scope = new SourceCampReadScope(em, store, facts, map, fixtureScope, session);
            context.ResourceMaterials = resources.CurrentMaterials;
            SourceUnitPrefab commandPrefab = map.Command(selected.Prefab);
            var adapter = new SourceUnitSemanticRequestAdapter(this, selected, facts, commandPrefab, actualQueue, currentSourceTime);
            var semanticFailure = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<SourceUnitPrefab, RuntimeBuildingEntity, SourcePendingProduction>(
                adapter, commandPrefab, selected.MaterialsCost, false, 0, currentSourceTime, out string required);
            var failure = SourceCampUiAdapter.ToCampFailure(semanticFailure);
            if (failure != BuildingUiCommandSystemHelper.CampRequestFailure.None)
                return new SourceCampPrimaryResult(false, adapter.RejectionReason ?? failure.ToString(), failure: SourceCampUiFailureMap.Map(failure));
            if (adapter.Pending == null || adapter.Pending.ReceiptId == 0 || adapter.Pending.ReceiptOwner != session)
                throw new InvalidOperationException("Accepted source queue omitted its actual paid pending receipt");
            return new SourceCampPrimaryResult(true, "OriginalQueueAccepted", adapter.Pending);
        }

        sealed class SourceUnitSemanticRequestAdapter : SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.IAdapter<SourceUnitPrefab, RuntimeBuildingEntity, SourcePendingProduction>
        {
            readonly SourceCampAvailability owner;
            readonly BuildDrawerCatalogItem selected;
            readonly CatalogPublicFacts facts;
            readonly SourceUnitPrefab directPrefab;
            readonly SourceCampQueueRequest actualQueue;
            readonly float now;
            internal SourcePendingProduction Pending;
            internal string RejectionReason;

            internal SourceUnitSemanticRequestAdapter(SourceCampAvailability owner, BuildDrawerCatalogItem selected,
                CatalogPublicFacts facts, SourceUnitPrefab directPrefab, SourceCampQueueRequest actualQueue, float now)
            { this.owner = owner; this.selected = selected; this.facts = facts; this.directPrefab = directPrefab; this.actualQueue = actualQueue; this.now = now; }

            public SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure Evaluate(SourceUnitPrefab prefab, int price, out string required)
            {
                if (selected.Prefab == null)
                    return SourceCampUiAdapter.ToSemanticFailure(owner.EvaluateUnitFailureInCurrentScope(directPrefab, price, out required));
                var result = owner.Read(selected, facts, true, true);
                required = result.RequiredBuilding;
                return SourceCampUiAdapter.ToSemanticFailure(result.Failure);
            }
            public bool IsConfiguredSpawnable(SourceUnitPrefab prefab) => owner.context.ConfiguredDefinitionsByPrefab != null && owner.context.ConfiguredDefinitionsByPrefab.ContainsKey(prefab);
            public bool BeginConfiguredPlacement(SourceUnitPrefab prefab) => false;
            public bool HasGlobalQueueCapacity() => SourceCampAvailabilityKernel.HasGlobalQueueCapacity(owner.context);
            public bool TryResolveCosts(SourceUnitPrefab prefab, int fallback, out int credits, out int materials) =>
                SourceCampAvailabilityKernel.TryResolveUnitResourceCosts(owner.context, prefab, fallback, out credits, out materials);
            public bool TryFindProducer(SourceUnitPrefab prefab, bool requireCapacity, out int id, out int index, out string display) =>
                owner.kernel.TryFindFirstFriendlyProducerBuilding(owner.context, prefab, requireCapacity, out id, out index, out display);
            public bool TryGetRequiredProducerDisplayName(SourceUnitPrefab prefab, out string display) =>
                owner.kernel.TryGetRequiredProducerDisplayName(owner.context, prefab, out display);
            public bool TryFindOperationMapProducer(SourceUnitPrefab prefab) =>
                owner.kernel.TryFindFirstFriendlyOperationMapProducer(owner.context, prefab, out _, out _, out _);
            public bool HasGlobalQueueCapacityAfterOperationMapFailure() => SourceCampAvailabilityKernel.HasGlobalQueueCapacity(owner.context);
            public SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure Spend(int credits, int materials) =>
                throw new NotSupportedException("Source mission queue payment belongs to actual shared reserve owner");
            public void Restore(int credits, int materials) =>
                throw new NotSupportedException("Source mission queue refund belongs to actual shared reserve owner");
            public bool TryQueueOperationMap(SourceUnitPrefab prefab, float sourceTime) => false;
            public bool IsMissionReceipt() => SourceCampNativeProduction.TrySession(owner.em, out _);
            public bool TryGetProducer(int id, out RuntimeBuildingEntity producer) => owner.buildings.TryGetValue(id, out producer) && producer != null;
            public void FocusProducer(RuntimeBuildingEntity producer, SourceUnitPrefab prefab) =>
                throw new NotSupportedException("Headless semantic command cannot claim or issue UI focus");
            public SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome TryQueueUnit(RuntimeBuildingEntity producer, int productionIndex, int frameCount, float sourceTime,
                out SourcePendingProduction pending)
            {
                bool accepted = actualQueue(producer.Id, productionIndex, now, out pending);
                Pending = pending;
                if (!accepted) RejectionReason = "OriginalQueueReturnedFalse";
                return accepted
                    ? SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.Queued
                    : SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.ProducerQueueFull;
            }
            public void AdjustAcceptedPending(RuntimeBuildingEntity producer, SourcePendingProduction pending, int credits, int materials)
            {
                if (pending != null && pending.ReceiptId == 0)
                    throw new InvalidOperationException("Source accepted pending receipt identity disappeared");
            }
            public void RecordUnitOrdered(SourceUnitPrefab prefab) { }
        }
    }

    internal static class SourceCampUiAdapter
    {
        internal static SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure ToSemanticFailure(
            BuildingUiCommandSystemHelper.CampRequestFailure failure) => failure switch
        {
            BuildingUiCommandSystemHelper.CampRequestFailure.None => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.None,
            BuildingUiCommandSystemHelper.CampRequestFailure.NotEnoughMoney => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.NotEnoughMoney,
            BuildingUiCommandSystemHelper.CampRequestFailure.MissingProducerBuilding => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.MissingProducerBuilding,
            BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InvalidSelection,
            BuildingUiCommandSystemHelper.CampRequestFailure.ProductionQueueFull => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ProducerQueueFull,
            BuildingUiCommandSystemHelper.CampRequestFailure.GlobalProductionQueueFull => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.GlobalQueueFull,
            BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientCredits => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InsufficientCredits,
            BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientMaterials => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InsufficientMaterials,
            BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientCreditsAndMaterials => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InsufficientCreditsAndMaterials,
            BuildingUiCommandSystemHelper.CampRequestFailure.InfantryLimit => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InfantryLimit,
            BuildingUiCommandSystemHelper.CampRequestFailure.LogisticsLimit => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.LogisticsLimit,
            BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessEstablishedRequired => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ReadinessEstablishedRequired,
            BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessFullArsenalRequired => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ReadinessFullArsenalRequired,
            BuildingUiCommandSystemHelper.CampRequestFailure.ArmyCapacityReached => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ArmyCapacityReached,
            BuildingUiCommandSystemHelper.CampRequestFailure.ResearchInProgress => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ResearchInProgress,
            _ => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InvalidSelection
        };

        internal static BuildingUiCommandSystemHelper.CampRequestFailure ToCampFailure(
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure failure) => failure switch
        {
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.None => BuildingUiCommandSystemHelper.CampRequestFailure.None,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.NotEnoughMoney => BuildingUiCommandSystemHelper.CampRequestFailure.NotEnoughMoney,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.MissingProducerBuilding => BuildingUiCommandSystemHelper.CampRequestFailure.MissingProducerBuilding,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InvalidSelection => BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ProducerQueueFull => BuildingUiCommandSystemHelper.CampRequestFailure.ProductionQueueFull,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.GlobalQueueFull => BuildingUiCommandSystemHelper.CampRequestFailure.GlobalProductionQueueFull,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InsufficientCredits => BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientCredits,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InsufficientMaterials => BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientMaterials,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InsufficientCreditsAndMaterials => BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientCreditsAndMaterials,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InfantryLimit => BuildingUiCommandSystemHelper.CampRequestFailure.InfantryLimit,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.LogisticsLimit => BuildingUiCommandSystemHelper.CampRequestFailure.LogisticsLimit,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ReadinessEstablishedRequired => BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessEstablishedRequired,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ReadinessFullArsenalRequired => BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessFullArsenalRequired,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ArmyCapacityReached => BuildingUiCommandSystemHelper.CampRequestFailure.ArmyCapacityReached,
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ResearchInProgress => BuildingUiCommandSystemHelper.CampRequestFailure.ResearchInProgress,
            _ => BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection
        };
    }
}
