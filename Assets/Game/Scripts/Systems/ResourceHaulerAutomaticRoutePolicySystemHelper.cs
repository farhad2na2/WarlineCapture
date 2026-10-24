using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal static class ResourceHaulerAutomaticRoutePolicySystemHelper
    {
        internal static bool TryFindAutomaticHaulerRoute(
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context,
            EntityManager em,
            GridConfig grid,
            byte factionId,
            int2 unitCell,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            bool hasAIInput,
            in BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput aiInput,
            out RuntimeBuildingEntity source,
            out RuntimeBuildingEntity destination)
        {
            source = null;
            destination = null;
            if (context.RuntimeBuildings == null)
                return false;

            if (!TryFindNearestAutomaticSourceToCell(
                    context,
                    em,
                    grid,
                    factionId,
                    unitCell,
                    resourceKind,
                    loadAmount,
                    out source))
            {
                return false;
            }

            if (!TryFindNearestAutomaticDestination(
                context,
                em,
                source,
                factionId,
                resourceKind,
                loadAmount,
                hasAIInput,
                aiInput,
                out destination))
                return false;

            // Preserve the consumer's strategic and starvation priorities, then
            // source Oil for its supply cycle. Picking only the nearest pump to
            // the truck repeatedly crosses otherwise local supply districts.
            if (resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil &&
                TryFindOilSupplyCycleSource(context, em, grid, factionId, unitCell,
                    loadAmount, destination, out RuntimeBuildingEntity cycleSource))
                source = cycleSource;
            return true;
        }

        private static bool TryFindOilSupplyCycleSource(
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context,
            EntityManager em,
            GridConfig grid,
            byte factionId,
            int2 unitCell,
            float loadAmount,
            RuntimeBuildingEntity destination,
            out RuntimeBuildingEntity result)
        {
            result = null;
            Vector3 truckPosition = grid.Origin + new float3(
                (unitCell.x + 0.5f) * grid.CellSize, 0f, (unitCell.y + 0.5f) * grid.CellSize);
            Vector3 destinationPosition = context.ResolveBuildingFocusWorldPosition(destination);
#if UNITY_EDITOR
                S002PhysicalStartupObservation.ObserveInitialHaulerFocus(em, destination, destinationPosition, "automatic-route.focus-0");
#endif
            float bestTravel = float.MaxValue;
            foreach (var pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity candidate = pair.Value;
                if (candidate == null || candidate.IsDestroyed ||
                    !IsAutomaticSource(context, em, candidate, factionId,
                        ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil, loadAmount))
                    continue;
                Vector3 sourcePosition = context.ResolveBuildingFocusWorldPosition(candidate);
#if UNITY_EDITOR
                S002PhysicalStartupObservation.ObserveInitialHaulerFocus(em, candidate, sourcePosition, "automatic-route.focus-1");
#endif
                // Include the initial pickup and the delivery/return legs of
                // continued supply. The existing reservation checks still
                // govern assignment, including future Oil production.
                float travel = Vector3.Distance(truckPosition, sourcePosition) +
                    2f * Vector3.Distance(sourcePosition, destinationPosition);
                if (!IsBetterDistanceCandidate(candidate, result, travel, bestTravel)) continue;
                bestTravel = travel;
                result = candidate;
            }
            return result != null;
        }

        internal static FuelLogisticsBlockReasonCode ResolveAutomaticAssignmentBlockReason(
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context,
            EntityManager em,
            GridConfig grid,
            byte factionId,
            int2 unitCell,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            bool hasAIInput,
            in BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput aiInput)
        {
            if (!TryFindNearestAutomaticSourceToCell(
                    context,
                    em,
                    grid,
                    factionId,
                    unitCell,
                    resourceKind,
                    loadAmount,
                    out RuntimeBuildingEntity source))
            {
                return FuelLogisticsBlockReasonCode.SourceUnavailable;
            }

            if (!HasAutomaticDestinationCandidate(context, em, source, factionId, resourceKind))
                return FuelLogisticsBlockReasonCode.DestinationUnavailable;

            if (!TryFindNearestAutomaticDestination(
                    context,
                    em,
                    source,
                    factionId,
                    resourceKind,
                    loadAmount,
                    hasAIInput,
                    aiInput,
                    out _))
            {
                return FuelLogisticsBlockReasonCode.DestinationFull;
            }

            return FuelLogisticsBlockReasonCode.RouteUnavailable;
        }

        internal static bool IsAutomaticOilDestination(
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context,
            EntityManager em,
            RuntimeBuildingEntity candidate)
        {
            return context.ResourceHaulerUtilitySystemHelper.IsFuelBuilding(candidate) ||
                   IsEnabledMaterialFabricationInput(em, candidate);
        }

        internal static bool IsEnabledMaterialFabricationInput(
            EntityManager em,
            RuntimeBuildingEntity candidate)
        {
            return TryGetMaterialFabrication(em, candidate, out MaterialFabricationComponent fabrication) &&
                   fabrication.ProductionEnabled != 0;
        }

        internal static bool TryGetMaterialFabrication(
            EntityManager em,
            RuntimeBuildingEntity candidate,
            out MaterialFabricationComponent fabrication)
        {
            fabrication = default;
            if (candidate == null ||
                candidate.CombatEntity == Entity.Null ||
                !em.Exists(candidate.CombatEntity) ||
                !em.HasComponent<MaterialFabricationInputTag>(candidate.CombatEntity) ||
                !em.HasComponent<MaterialFabricationComponent>(candidate.CombatEntity))
            {
                return false;
            }

            fabrication = em.GetComponentData<MaterialFabricationComponent>(candidate.CombatEntity);
            return fabrication.OwnerFactionId == candidate.OwnerFactionId;
        }

        private static bool TryFindNearestAutomaticSourceToCell(
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context,
            EntityManager em,
            GridConfig grid,
            byte factionId,
            int2 originCell,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            out RuntimeBuildingEntity result)
        {
            result = null;
            if (context.RuntimeBuildings == null || context.ResolveBuildingFocusWorldPosition == null)
                return false;

            Vector3 origin = grid.Origin + new float3(
                (originCell.x + 0.5f) * grid.CellSize,
                0f,
                (originCell.y + 0.5f) * grid.CellSize);
            float bestDistanceSq = float.MaxValue;

            foreach (var pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity candidate = pair.Value;
                if (candidate == null ||
                    candidate.IsDestroyed ||
                    !IsAutomaticSource(context, em, candidate, factionId, resourceKind, loadAmount))
                {
                    continue;
                }

                Vector3 candidatePosition = context.ResolveBuildingFocusWorldPosition(candidate);
#if UNITY_EDITOR
                S002PhysicalStartupObservation.ObserveInitialHaulerFocus(em, candidate, candidatePosition, "automatic-route.focus-2");
#endif
                float distanceSq = (candidatePosition - origin).sqrMagnitude;
                if (!IsBetterDistanceCandidate(candidate, result, distanceSq, bestDistanceSq))
                    continue;

                bestDistanceSq = distanceSq;
                result = candidate;
            }

            return result != null;
        }

        private static bool TryFindNearestAutomaticDestination(
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context,
            EntityManager em,
            RuntimeBuildingEntity source,
            byte factionId,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            bool hasAIInput,
            in BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput aiInput,
            out RuntimeBuildingEntity result)
        {
            result = null;
            if (source == null || context.RuntimeBuildings == null || context.ResolveBuildingFocusWorldPosition == null)
                return false;

            Vector3 origin = context.ResolveBuildingFocusWorldPosition(source);
#if UNITY_EDITOR
                S002PhysicalStartupObservation.ObserveInitialHaulerFocus(em, source, origin, "automatic-route.focus-3");
#endif
            float bestDistanceSq = float.MaxValue;
            int bestStrategicPriority = -1;
            int bestStarvationPriority = -1;
            float bestFreeCapacityRatio = -1f;
            foreach (var pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity candidate = pair.Value;
                if (candidate == null ||
                    candidate == source ||
                    candidate.IsDestroyed ||
                    !IsAutomaticDestination(context, em, candidate, factionId, resourceKind, loadAmount))
                {
                    continue;
                }

                Vector3 candidatePosition = context.ResolveBuildingFocusWorldPosition(candidate);
#if UNITY_EDITOR
                S002PhysicalStartupObservation.ObserveInitialHaulerFocus(em, candidate, candidatePosition, "automatic-route.focus-4");
#endif
                float distanceSq = (candidatePosition - origin).sqrMagnitude;
                int strategicPriority = 0;
                int starvationPriority = 0;
                float freeCapacityRatio = 0f;
                if (resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil)
                {
                    ResolveOilDestinationDemand(
                        context,
                        em,
                        candidate,
                        loadAmount,
                        hasAIInput,
                        aiInput,
                        out strategicPriority,
                        out starvationPriority,
                        out freeCapacityRatio);
                    if (!IsBetterOilDestinationCandidate(
                            candidate,
                            result,
                            strategicPriority,
                            bestStrategicPriority,
                            starvationPriority,
                            bestStarvationPriority,
                            freeCapacityRatio,
                            bestFreeCapacityRatio,
                            distanceSq,
                            bestDistanceSq))
                    {
                        continue;
                    }
                }
                else if (!IsBetterDistanceCandidate(candidate, result, distanceSq, bestDistanceSq))
                {
                    continue;
                }

                bestDistanceSq = distanceSq;
                bestStrategicPriority = strategicPriority;
                bestStarvationPriority = starvationPriority;
                bestFreeCapacityRatio = freeCapacityRatio;
                result = candidate;
            }

            return result != null;
        }

        private static bool IsAutomaticSource(
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context,
            EntityManager em,
            RuntimeBuildingEntity candidate,
            byte factionId,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount)
        {
            if (!IsSameFactionResourceBuilding(candidate, factionId))
                return false;

            if (resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil)
                return context.ResourceHaulerUtilitySystemHelper.IsOilSourceBuilding(candidate);

            return context.ResourceHaulerUtilitySystemHelper.IsFuelStorageSourceBuilding(candidate) &&
                   context.ResourceHaulerUtilitySystemHelper.HasEnoughSourceResource(
                       em,
                       candidate,
                       resourceKind,
                       loadAmount);
        }

        private static bool IsAutomaticDestination(
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context,
            EntityManager em,
            RuntimeBuildingEntity candidate,
            byte factionId,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount)
        {
            if (!IsSameFactionResourceBuilding(candidate, factionId))
                return false;

            if (resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil)
            {
                return IsAutomaticOilDestination(context, em, candidate) &&
                       context.ResourceHaulerUtilitySystemHelper.HasReceivingCapacity(
                           em,
                           candidate,
                           resourceKind,
                           loadAmount);
            }

            return context.FactionResourceCompositionSystemHelper.IsResourceStorageBuilding(candidate) &&
                   candidate.FuelStorageCapacity > 0 &&
                   context.ResourceHaulerUtilitySystemHelper.HasReceivingCapacity(
                       em,
                       candidate,
                       resourceKind,
                       loadAmount);
        }

        private static bool HasAutomaticDestinationCandidate(
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context,
            EntityManager em,
            RuntimeBuildingEntity source,
            byte factionId,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind)
        {
            if (source == null || context.RuntimeBuildings == null)
                return false;

            foreach (var pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity candidate = pair.Value;
                if (candidate == null ||
                    candidate == source ||
                    candidate.IsDestroyed ||
                    !IsSameFactionResourceBuilding(candidate, factionId))
                {
                    continue;
                }

                if (resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil)
                {
                    if (IsAutomaticOilDestination(context, em, candidate))
                        return true;
                    continue;
                }

                if (context.FactionResourceCompositionSystemHelper.IsResourceStorageBuilding(candidate) &&
                    candidate.FuelStorageCapacity > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ResolveOilDestinationDemand(
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context,
            EntityManager em,
            RuntimeBuildingEntity candidate,
            float loadAmount,
            bool hasAIInput,
            in BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput aiInput,
            out int strategicPriority,
            out int starvationPriority,
            out float freeCapacityRatio)
        {
            float storedOil = context.ResourceHaulerUtilitySystemHelper.GetStoredResource(
                em,
                candidate,
                ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil);
            float requiredOil = loadAmount;
            if (TryGetMaterialFabrication(em, candidate, out MaterialFabricationComponent fabrication))
                requiredOil = math.max(requiredOil, fabrication.OilConsumedPerCycle);

            strategicPriority = hasAIInput
                ? ResourceHaulerAIOilAllocationPolicySystemHelper.ResolveDestinationStrategicPriority(
                    TryGetMaterialFabrication(em, candidate, out _),
                    context.ResourceHaulerUtilitySystemHelper.IsFuelBuilding(candidate),
                    aiInput)
                : 0;
            float committedOil = storedOil;
            if (TryResolveExpandedOilDemand(em, candidate, loadAmount,
                    out int expandedPriority, out float expandedRequiredOil))
            {
                strategicPriority = expandedPriority;
                requiredOil = expandedRequiredOil;
                if (em.HasComponent<BuildingResourceStorageComponent>(candidate.CombatEntity))
                {
                    var stock = em.GetComponentData<BuildingResourceStorageComponent>(candidate.CombatEntity);
                    // Use the same physically reserved batch for secondary
                    // demand as for strategic priority; a funded refinery must
                    // not attract spare trucks while other inputs are short.
                    committedOil = math.max(0, storedOil - stock.ReservedOilOutboundBarrels) +
                        math.max(0, stock.ReservedOilInboundBarrels);
                }
            }
            starvationPriority = committedOil + 0.0001f < requiredOil ? 1 : 0;
            float freeCapacity = context.ResourceHaulerUtilitySystemHelper.GetReceivingFreeCapacity(
                em,
                candidate,
                ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil);
            freeCapacityRatio = freeCapacity / math.max(1f, candidate.OilStorageCapacity);
        }

        internal static bool TryResolveExpandedOilDemand(EntityManager em, RuntimeBuildingEntity candidate,
            float loadAmount, out int priority, out float requiredOil)
        {
            priority = 0;
            requiredOil = loadAmount;
            if (candidate == null || !em.Exists(candidate.CombatEntity) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(candidate.CombatEntity)) return false;
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(candidate.CombatEntity);
            if (owner.CapacityReleased != 0 || owner.FactionId != candidate.OwnerFactionId) return false;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            using var roots = sessions.ToEntityArray(Unity.Collections.Allocator.Temp);
#if UNITY_EDITOR
            S002PhysicalStartupObservation.ContinuousRootSnapshot(em, roots);
#endif
            bool expanded = false;
            foreach (var root in roots)
            {
                var state = em.GetComponentData<SkirmishExpandedSessionComponent>(root);
                expanded |= state.IsLegacy == 0 && state.SessionId.Equals(owner.SessionId);
            }
            if (!expanded) return false;

            bool fabrication = TryGetMaterialFabrication(em, candidate, out var recipe) && recipe.ProductionEnabled != 0;
            var profile = em.HasComponent<BuildingResourceProductionProfileComponent>(candidate.CombatEntity)
                ? em.GetComponentData<BuildingResourceProductionProfileComponent>(candidate.CombatEntity) : default;
            if (!fabrication && profile.OilPerFuelBatch <= 0) return false;
            requiredOil = math.max(loadAmount, fabrication ? recipe.OilConsumedPerCycle : profile.OilPerFuelBatch);

            // Fund one batch for each enabled input. A different funded Factory
            // must not suppress this Factory's demand. Real inbound reservations
            // count so spare trucks can serve other inputs before delivery.
            if (fabrication)
            {
                float committedOil = 0;
                if (em.HasComponent<BuildingResourceStorageComponent>(candidate.CombatEntity))
                {
                    var stock = em.GetComponentData<BuildingResourceStorageComponent>(candidate.CombatEntity);
                    committedOil = math.max(0, stock.StoredOilBarrels - stock.ReservedOilOutboundBarrels) +
                        math.max(0, stock.ReservedOilInboundBarrels);
                }
                priority = committedOil + 0.0001f < recipe.OilConsumedPerCycle ? 2 : 0;
            }
            else if (em.HasComponent<BuildingResourceStorageComponent>(candidate.CombatEntity))
            {
                var stock = em.GetComponentData<BuildingResourceStorageComponent>(candidate.CombatEntity);
                float committedOil = math.max(0, stock.StoredOilBarrels - stock.ReservedOilOutboundBarrels) +
                    math.max(0, stock.ReservedOilInboundBarrels);
                bool outputRoom = stock.FuelStorageCapacity - stock.StoredFuelBarrels - stock.ReservedFuelInboundBarrels +
                    0.0001f >= profile.FuelPerBatch;
                priority = committedOil + 0.0001f < profile.OilPerFuelBatch && outputRoom ? 1 : 0;
            }
            return true;
        }

        private static bool IsBetterOilDestinationCandidate(
            RuntimeBuildingEntity candidate,
            RuntimeBuildingEntity current,
            int strategicPriority,
            int currentStrategicPriority,
            int starvationPriority,
            int currentStarvationPriority,
            float freeCapacityRatio,
            float currentFreeCapacityRatio,
            float distanceSq,
            float currentDistanceSq)
        {
            if (current == null || strategicPriority != currentStrategicPriority)
                return current == null || strategicPriority > currentStrategicPriority;
            if (current == null || starvationPriority != currentStarvationPriority)
                return current == null || starvationPriority > currentStarvationPriority;
            if (math.abs(freeCapacityRatio - currentFreeCapacityRatio) > 0.0001f)
                return freeCapacityRatio > currentFreeCapacityRatio;

            return IsBetterDistanceCandidate(candidate, current, distanceSq, currentDistanceSq);
        }

        private static bool IsBetterDistanceCandidate(
            RuntimeBuildingEntity candidate,
            RuntimeBuildingEntity current,
            float distanceSq,
            float currentDistanceSq)
        {
            if (current == null || distanceSq + 0.0001f < currentDistanceSq)
                return true;
            return math.abs(distanceSq - currentDistanceSq) <= 0.0001f && candidate.Id < current.Id;
        }

        internal static bool IsSameFactionResourceBuilding(RuntimeBuildingEntity building, byte factionId)
        {
            return building != null &&
                   !building.IsDestroyed &&
                   building.HasOwnerFaction &&
                   building.OwnerFactionId == factionId;
        }
    }
}
