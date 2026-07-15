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

            return TryFindNearestAutomaticDestination(
                context,
                em,
                source,
                factionId,
                resourceKind,
                loadAmount,
                hasAIInput,
                aiInput,
                out destination);
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
            starvationPriority = storedOil + 0.0001f < requiredOil ? 1 : 0;
            float freeCapacity = context.ResourceHaulerUtilitySystemHelper.GetReceivingFreeCapacity(
                em,
                candidate,
                ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil);
            freeCapacityRatio = freeCapacity / math.max(1f, candidate.OilStorageCapacity);
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
