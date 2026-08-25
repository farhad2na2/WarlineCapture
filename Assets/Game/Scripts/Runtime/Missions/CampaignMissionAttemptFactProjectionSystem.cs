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

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _missionRootQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>(),
                ComponentType.ReadOnly<CampaignMissionCatalogComponent>(),
                ComponentType.ReadOnly<CampaignMissionRuntimeComponent>(),
                ComponentType.ReadWrite<CampaignMissionAttemptFactsComponent>(),
                ComponentType.ReadWrite<CampaignMissionAttemptFactProjectionStateComponent>());
            _buildingBoundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                ComponentType.ReadOnly<BuildingRuntimeSpawnRequest>());
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
            if (!TryResolveRequiredBuilding(
                    in catalog,
                    in runtime,
                    out FixedString128Bytes requiredBuildingId,
                    out int requiredCount))
                return;

            Entity buildingBoundary = _buildingBoundaryQuery.GetSingletonEntity();
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                entityManager.GetBuffer<BuildingRuntimeSpawnRequest>(buildingBoundary, true);
            int currentMaximumRequestId = FindMaximumRequestId(requests);
            if (!IsCurrentAttempt(in projectionState, in runtime, catalog.SourceVersion))
            {
                entityManager.SetComponentData(root, CreateAttemptState(
                    in runtime, catalog.SourceVersion, currentMaximumRequestId));
                return;
            }

            int observedCount = 0;
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

                observedCount++;
                if (observedCount >= requiredCount)
                    break;
            }

            int completedCount = math.min(requiredCount, observedCount);
            CampaignMissionAttemptFactsComponent facts =
                entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            int nextPlacedCount = math.max(facts.RequiredBuildingPlacedCount, completedCount);
            int nextCompletedCount = math.max(facts.RequiredBuildingCompletedCount, completedCount);
            if (nextPlacedCount == facts.RequiredBuildingPlacedCount &&
                nextCompletedCount == facts.RequiredBuildingCompletedCount)
                return;

            facts.RequiredBuildingPlacedCount = nextPlacedCount;
            facts.RequiredBuildingCompletedCount = nextCompletedCount;
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

        private static CampaignMissionAttemptFactProjectionStateComponent CreateAttemptState(
            in CampaignMissionRuntimeComponent runtime,
            uint sourceVersion,
            int buildingRequestBaselineId) =>
            new()
            {
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal,
                BuildingRequestBaselineId = buildingRequestBaselineId,
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
    }
}
