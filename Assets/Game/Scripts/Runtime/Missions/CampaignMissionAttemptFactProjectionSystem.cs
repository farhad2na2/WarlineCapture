using Game.Components;
using Game.Missions.Contracts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CampaignMissionAttemptResourceInitializationSystem))]
    [UpdateBefore(typeof(CampaignMissionRuntimeSystem))]
    public partial struct CampaignMissionAttemptFactProjectionSystem : ISystem
    {
        private EntityQuery _missionRootQuery;
        private EntityQuery _buildingBoundaryQuery;
        private EntityQuery _operationMapMetadataQuery;
        private EntityQuery _forwardPostCandidateQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _missionRootQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<
                    CampaignMissionRootComponent,
                    CampaignMissionCatalogComponent,
                    CampaignMissionRuntimeComponent>()
                .WithAllRW<CampaignMissionAttemptFactsComponent>()
                .WithAllRW<CampaignMissionAttemptFactProjectionStateComponent>()
                .Build(ref state);
            _buildingBoundaryQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<
                    BuildingRuntimeStateTag,
                    BuildingRuntimeSpawnRequest,
                    BuildingRuntimeDeleteRequest,
                    BuildingRuntimeOwnedBuildingSummary,
                    BuildingProducedUnitReadModel>()
                .Build(ref state);
            _operationMapMetadataQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<OperationMapMetadataComponent>()
                .Build(ref state);
            _forwardPostCandidateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<
                    RuntimeBuildingCombatTag,
                    RuntimeBuildingCombatInfo,
                    OperationMapBuildingComponent,
                    Faction,
                    UnitHealth>()
                .Build(ref state);
            state.RequireForUpdate(_missionRootQuery);
            state.RequireForUpdate(_buildingBoundaryQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_missionRootQuery.CalculateEntityCount() != 1 ||
                _buildingBoundaryQuery.CalculateEntityCount() != 1)
                return;

            EntityManager entityManager = state.EntityManager;
            Entity root = _missionRootQuery.GetSingletonEntity();
            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            CampaignMissionAttemptFactProjectionStateComponent projectionState =
                entityManager.GetComponentData<CampaignMissionAttemptFactProjectionStateComponent>(root);
            bool hasRequiredBuilding = TryResolveRequiredBuilding(
                    in catalog,
                    in runtime,
                    out FixedString128Bytes requiredBuildingId,
                    out int requiredBuildingCount);
            bool hasRequiredUnit = TryResolveRequiredUnit(
                    in catalog,
                    in runtime,
                    out FixedString128Bytes requiredUnitId,
                    out int requiredUnitCount);
            bool hasForwardPost = TryResolveForwardPost(
                in catalog,
                in runtime,
                out FixedString64Bytes forwardPostRoleId,
                out FixedString64Bytes forwardPostAnchorId);
            if (!hasRequiredBuilding && !hasRequiredUnit && !hasForwardPost)
                return;

            Entity buildingBoundary = _buildingBoundaryQuery.GetSingletonEntity();
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                entityManager.GetBuffer<BuildingRuntimeSpawnRequest>(buildingBoundary, true);
            DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> ownedBuildings =
                entityManager.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(buildingBoundary, true);
            DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
                entityManager.GetBuffer<BuildingProducedUnitReadModel>(buildingBoundary, true);
            bool deliveryInProgress = false;
            if (entityManager.HasComponent<BuildingProductionDeliveryReadModel>(buildingBoundary))
            {
                BuildingProductionDeliveryReadModel delivery =
                    entityManager.GetComponentData<BuildingProductionDeliveryReadModel>(buildingBoundary);
                deliveryInProgress = delivery.ActiveCanonicalDeliveryCount > 0 ||
                                     delivery.ActiveManagedDeliveryCount > 0;
            }
            int currentMaximumRequestId = FindMaximumRequestId(requests);
            int currentRequiredBuildingOwnedCount = hasRequiredBuilding
                ? CountMatchingOwnedBuildings(ownedBuildings, requiredBuildingId)
                : 0;
            if (!IsCurrentAttempt(in projectionState, in runtime, catalog.SourceVersion))
            {
                entityManager.SetComponentData(root, CreateAttemptState(
                    in runtime,
                    catalog.SourceVersion,
                    currentMaximumRequestId,
                    currentRequiredBuildingOwnedCount,
                    producedUnits.Length));
                return;
            }

            CampaignMissionAttemptFactsComponent facts =
                entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            bool changed = false;
            if (hasRequiredBuilding)
            {
                int observedBuildingCount = 0;
                foreach ((RefRO<RuntimeBuildingCombatInfo> info,
                          RefRO<Faction> faction,
                          RefRO<UnitHealth> health)
                         in SystemAPI.Query<RefRO<RuntimeBuildingCombatInfo>, RefRO<Faction>, RefRO<UnitHealth>>()
                             .WithAll<RuntimeBuildingCombatTag>())
                {
                    if (faction.ValueRO.Id != FactionIdentity.PlayerFactionId || health.ValueRO.Max <= 0 ||
                        !HasMatchingCompletedRequest(
                            requests,
                            projectionState.BuildingRequestBaselineId,
                            requiredBuildingId,
                            in info.ValueRO))
                        continue;

                    observedBuildingCount++;
                    if (observedBuildingCount >= requiredBuildingCount)
                        break;
                }

                int ownedBuildingCountSinceAttempt = math.max(
                    0,
                    currentRequiredBuildingOwnedCount - projectionState.RequiredBuildingOwnedCountBaseline);
                int completedBuildingCount = math.min(
                    requiredBuildingCount,
                    math.max(observedBuildingCount, ownedBuildingCountSinceAttempt));
                int nextPlacedCount = math.max(facts.RequiredBuildingPlacedCount, completedBuildingCount);
                int nextCompletedCount = math.max(facts.RequiredBuildingCompletedCount, completedBuildingCount);
                changed |= nextPlacedCount != facts.RequiredBuildingPlacedCount ||
                           nextCompletedCount != facts.RequiredBuildingCompletedCount;
                facts.RequiredBuildingPlacedCount = nextPlacedCount;
                facts.RequiredBuildingCompletedCount = nextCompletedCount;
            }

            if (hasRequiredUnit && !deliveryInProgress)
            {
                int observedProducedCount = CountMatchingProducedUnits(
                    entityManager,
                    producedUnits,
                    projectionState.ProducedUnitReadModelBaselineCount,
                    requiredUnitId,
                    requiredUnitCount);
                int nextProducedCount = math.max(
                    facts.RequiredUnitProducedCount,
                    math.min(requiredUnitCount, observedProducedCount));
                changed |= nextProducedCount != facts.RequiredUnitProducedCount;
                facts.RequiredUnitProducedCount = nextProducedCount;
            }

            if (hasForwardPost && TryFindAuthoritativeForwardPost(
                    entityManager,
                    _operationMapMetadataQuery,
                    _forwardPostCandidateQuery,
                    in runtime,
                    in forwardPostAnchorId,
                    in forwardPostRoleId,
                    out Entity forwardPost))
            {
                BindForwardPostRole(
                    entityManager,
                    forwardPost,
                    in runtime.SessionToken,
                    in forwardPostRoleId);
                UnitHealth health = entityManager.GetComponentData<UnitHealth>(forwardPost);
                bool destroyed = health.Current <= 0 ||
                    entityManager.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(forwardPost);
                byte nextBound = 1;
                byte nextDamaged = health.Current < health.Max ? (byte)1 : facts.ForwardPostDamaged;
                byte nextDestroyed = destroyed ? (byte)1 : facts.ForwardPostDestroyed;
                changed |= facts.ForwardPostBound != nextBound ||
                           facts.ForwardPostDamaged != nextDamaged ||
                           facts.ForwardPostDestroyed != nextDestroyed;
                facts.ForwardPostBound = nextBound;
                facts.ForwardPostDamaged = nextDamaged;
                facts.ForwardPostDestroyed = nextDestroyed;
            }

            if (!changed)
                return;

            entityManager.SetComponentData(root, facts);
        }

        internal static bool TryResolveRequiredBuilding(
            in CampaignMissionCatalogComponent catalog,
            in CampaignMissionRuntimeComponent runtime,
            out FixedString128Bytes requiredBuildingId,
            out int requiredCount)
        {
            requiredBuildingId = default;
            requiredCount = 0;
            if (runtime.Version == 0 || runtime.SourceVersion == 0 ||
                runtime.SourceVersion != catalog.SourceVersion || runtime.SessionToken.IsEmpty ||
                runtime.AttemptOrdinal < 0 ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return false;

            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            if (definition.MissionRuntimeEnabled == 0)
                return false;

            int matchCount = 0;
            for (int index = 0; index < definition.Objectives.Length; index++)
            {
                ref CampaignMissionObjectiveBlob objective = ref definition.Objectives[index];
                if (objective.Rule != MissionObjectiveRuleKind.BuildStructure)
                    continue;

                matchCount++;
                requiredBuildingId = objective.TargetConfigId;
                requiredCount = objective.RequiredCount;
            }

            return matchCount == 1 && !requiredBuildingId.IsEmpty && requiredCount > 0;
        }

        internal static bool TryResolveRequiredUnit(
            in CampaignMissionCatalogComponent catalog,
            in CampaignMissionRuntimeComponent runtime,
            out FixedString128Bytes requiredUnitId,
            out int requiredCount)
        {
            requiredUnitId = default;
            requiredCount = 0;
            if (runtime.Version == 0 || runtime.SourceVersion == 0 ||
                runtime.SourceVersion != catalog.SourceVersion || runtime.SessionToken.IsEmpty ||
                runtime.AttemptOrdinal < 0 ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return false;

            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            if (definition.MissionRuntimeEnabled == 0)
                return false;

            int matchCount = 0;
            for (int index = 0; index < definition.Objectives.Length; index++)
            {
                ref CampaignMissionObjectiveBlob objective = ref definition.Objectives[index];
                if (objective.Rule != MissionObjectiveRuleKind.ProduceUnit)
                    continue;

                matchCount++;
                requiredUnitId = objective.TargetConfigId;
                requiredCount = objective.RequiredCount;
            }

            return matchCount == 1 && !requiredUnitId.IsEmpty && requiredCount > 0;
        }

        internal static bool HasMatchingCompletedRequest(
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests,
            int baselineRequestId,
            in FixedString128Bytes requiredBuildingId,
            in RuntimeBuildingCombatInfo building)
        {
            for (int index = 0; index < requests.Length; index++)
            {
                BuildingRuntimeSpawnRequest request = requests[index];
                if (request.RequestId <= baselineRequestId ||
                    request.RequestKind != BuildingRuntimeSpawnRequest.KindBuilding ||
                    request.Status != BuildingRuntimeSpawnRequest.Succeeded ||
                    request.HasOwnerFaction == 0 ||
                    request.FactionId != FactionIdentity.PlayerFactionId ||
                    !request.BuildingId.Equals(requiredBuildingId) ||
                    request.BuildingRuntimeId != building.RuntimeBuildingId ||
                    !request.ActualOrigin.Equals(building.OriginCell) ||
                    !request.ActualFootprint.Equals(building.FootprintCells) ||
                    building.OwnerFactionId != FactionIdentity.PlayerFactionId)
                    continue;

                return true;
            }

            return false;
        }

        internal static int CountMatchingOwnedBuildings(
            DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> summaries,
            in FixedString128Bytes requiredBuildingId)
        {
            int count = 0;
            for (int index = 0; index < summaries.Length; index++)
            {
                BuildingRuntimeOwnedBuildingSummary summary = summaries[index];
                if (summary.FactionId != FactionIdentity.PlayerFactionId || summary.Count <= 0 ||
                    !FixedStringsEqualIgnoreCase(in summary.BuildingId, in requiredBuildingId))
                    continue;

                count = math.max(count, summary.Count);
            }

            return count;
        }

        internal static int CountMatchingProducedUnits(
            EntityManager entityManager,
            DynamicBuffer<BuildingProducedUnitReadModel> producedUnits,
            int baselineCount,
            in FixedString128Bytes requiredUnitId,
            int requiredCount)
        {
            if (baselineCount < 0 || baselineCount > producedUnits.Length ||
                requiredUnitId.IsEmpty || requiredCount <= 0)
                return 0;

            int observedCount = 0;
            for (int index = baselineCount; index < producedUnits.Length; index++)
            {
                BuildingProducedUnitReadModel produced = producedUnits[index];
                if (HasEarlierProducedUnit(producedUnits, baselineCount, index, produced.Unit) ||
                    !IsMatchingProducedUnit(entityManager, in produced, in requiredUnitId))
                    continue;

                observedCount++;
                if (observedCount >= requiredCount)
                    break;
            }

            return observedCount;
        }

        private static bool IsMatchingProducedUnit(
            EntityManager entityManager,
            in BuildingProducedUnitReadModel produced,
            in FixedString128Bytes requiredUnitId)
        {
            if (produced.HasOwnerFaction == 0 || produced.OwnerFactionId != FactionIdentity.PlayerFactionId ||
                produced.Unit == Entity.Null || !entityManager.Exists(produced.Unit) ||
                entityManager.HasComponent<Prefab>(produced.Unit) ||
                !entityManager.HasComponent<Faction>(produced.Unit) ||
                !entityManager.HasComponent<UnitHealth>(produced.Unit) ||
                !entityManager.HasComponent<UnitSourcePrefabKey>(produced.Unit) ||
                !FixedStringsEqualIgnoreCase(in produced.UnitSourceKey, in requiredUnitId))
                return false;

            Faction faction = entityManager.GetComponentData<Faction>(produced.Unit);
            UnitHealth health = entityManager.GetComponentData<UnitHealth>(produced.Unit);
            UnitSourcePrefabKey source = entityManager.GetComponentData<UnitSourcePrefabKey>(produced.Unit);
            return faction.Id == FactionIdentity.PlayerFactionId && health.Max > 0 && health.Current > 0 &&
                   FixedStringsEqualIgnoreCase(in source.Value, in produced.UnitSourceKey);
        }

        private static bool HasEarlierProducedUnit(
            DynamicBuffer<BuildingProducedUnitReadModel> producedUnits,
            int baselineCount,
            int index,
            Entity unit)
        {
            if (unit == Entity.Null)
                return false;

            for (int prior = baselineCount; prior < index; prior++)
            {
                if (producedUnits[prior].Unit == unit)
                    return true;
            }

            return false;
        }

        private static bool FixedStringsEqualIgnoreCase(
            in FixedString64Bytes left,
            in FixedString128Bytes right)
        {
            if (left.Length != right.Length)
                return false;

            for (int index = 0; index < left.Length; index++)
            {
                if (ToAsciiLower(left[index]) != ToAsciiLower(right[index]))
                    return false;
            }

            return true;
        }

        private static bool FixedStringsEqualIgnoreCase(
            in FixedString64Bytes left,
            in FixedString64Bytes right)
        {
            if (left.Length != right.Length)
                return false;

            for (int index = 0; index < left.Length; index++)
            {
                if (ToAsciiLower(left[index]) != ToAsciiLower(right[index]))
                    return false;
            }

            return true;
        }

        private static CampaignMissionAttemptFactProjectionStateComponent CreateAttemptState(
            in CampaignMissionRuntimeComponent runtime,
            uint sourceVersion,
            int buildingRequestBaselineId,
            int requiredBuildingOwnedCountBaseline,
            int producedUnitReadModelBaselineCount) =>
            new()
            {
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal,
                BuildingRequestBaselineId = buildingRequestBaselineId,
                RequiredBuildingOwnedCountBaseline = requiredBuildingOwnedCountBaseline,
                ProducedUnitReadModelBaselineCount = producedUnitReadModelBaselineCount,
                SourceVersion = sourceVersion,
                Initialized = 1
            };

        private static bool IsCurrentAttempt(
            in CampaignMissionAttemptFactProjectionStateComponent projectionState,
            in CampaignMissionRuntimeComponent runtime,
            uint sourceVersion) =>
            projectionState.Initialized != 0 && projectionState.SourceVersion == sourceVersion &&
            projectionState.SessionToken.Equals(runtime.SessionToken) &&
            projectionState.AttemptOrdinal == runtime.AttemptOrdinal;

        private static int FindMaximumRequestId(DynamicBuffer<BuildingRuntimeSpawnRequest> requests)
        {
            int maximum = 0;
            for (int index = 0; index < requests.Length; index++)
                maximum = math.max(maximum, requests[index].RequestId);
            return maximum;
        }

        private static bool FixedStringsEqualIgnoreCase(
            in FixedString128Bytes left,
            in FixedString128Bytes right)
        {
            if (left.Length != right.Length)
                return false;

            for (int index = 0; index < left.Length; index++)
            {
                byte leftValue = ToAsciiLower(left[index]);
                byte rightValue = ToAsciiLower(right[index]);
                if (leftValue != rightValue)
                    return false;
            }

            return true;
        }

        private static byte ToAsciiLower(byte value) =>
            value >= (byte)'A' && value <= (byte)'Z'
                ? (byte)(value + ('a' - 'A'))
                : value;
    }
}
