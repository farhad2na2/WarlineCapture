using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionAttemptFactProjectionSystem
    {
        internal static bool TryResolveForwardPost(
            in CampaignMissionCatalogComponent catalog,
            in CampaignMissionRuntimeComponent runtime,
            out FixedString64Bytes missionRoleId,
            out FixedString64Bytes anchorId)
        {
            missionRoleId = default;
            anchorId = default;
            if (runtime.Version == 0 || runtime.SourceVersion == 0 ||
                runtime.SourceVersion != catalog.SourceVersion || runtime.SessionToken.IsEmpty ||
                runtime.AttemptOrdinal < 0 ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return false;

            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            if (definition.MissionRuntimeEnabled == 0 || definition.BaseMissionRoleId.IsEmpty ||
                definition.BaseAnchorId.IsEmpty ||
                !definition.BaseMissionRoleId.Equals(definition.DelayedWaveTargetMissionRoleId))
                return false;

            int matchCount = 0;
            for (int index = 0; index < definition.Objectives.Length; index++)
            {
                ref CampaignMissionObjectiveBlob objective = ref definition.Objectives[index];
                if (objective.Rule != MissionObjectiveRuleKind.DefendMissionRole)
                    continue;

                matchCount++;
                missionRoleId = objective.MissionRoleId;
            }

            anchorId = definition.BaseAnchorId;
            return matchCount == 1 && missionRoleId.Equals(definition.BaseMissionRoleId);
        }

        internal static bool TryFindAuthoritativeForwardPost(
            EntityManager entityManager,
            EntityQuery metadataQuery,
            EntityQuery candidateQuery,
            in CampaignMissionRuntimeComponent runtime,
            in FixedString64Bytes anchorId,
            in FixedString64Bytes missionRoleId,
            out Entity forwardPost)
        {
            forwardPost = Entity.Null;
            if (anchorId.IsEmpty || missionRoleId.IsEmpty || metadataQuery.CalculateEntityCount() != 1)
                return false;

            OperationMapMetadataComponent metadata = metadataQuery.GetSingleton<OperationMapMetadataComponent>();
            if (!metadata.Blob.IsCreated || metadata.PhysicalSourceValidated == 0)
                return false;

            ref OperationMapBlob map = ref metadata.Blob.Value;
            if (!map.OperationMapId.Equals(runtime.OperationMapId) || map.SourceOperationMapId.IsEmpty ||
                !TryFindUniqueBaseAnchor(ref map, in anchorId, out OperationMapAnchorBlob anchor))
                return false;

            int2 anchorCell = new(
                (int)math.floor((anchor.Position.x - map.Grid.Origin.x) / map.Grid.CellSize),
                (int)math.floor((anchor.Position.z - map.Grid.Origin.z) / map.Grid.CellSize));
            FixedString128Bytes sourceMapId = new(map.SourceOperationMapId);
            int matches = 0;
            using NativeArray<Entity> candidates = candidateQuery.ToEntityArray(Allocator.Temp);
            for (int index = 0; index < candidates.Length; index++)
            {
                Entity candidate = candidates[index];
                RuntimeBuildingCombatInfo info =
                    entityManager.GetComponentData<RuntimeBuildingCombatInfo>(candidate);
                OperationMapBuildingComponent building =
                    entityManager.GetComponentData<OperationMapBuildingComponent>(candidate);
                Faction faction = entityManager.GetComponentData<Faction>(candidate);
                UnitHealth health = entityManager.GetComponentData<UnitHealth>(candidate);
                if (faction.Id != FactionIdentity.PlayerFactionId ||
                    info.OwnerFactionId != FactionIdentity.PlayerFactionId || health.Max <= 0 ||
                    !building.OperationMapId.Equals(sourceMapId) ||
                    !entityManager.HasComponent<OperationMapBuildingDestroyedComponent>(candidate) ||
                    !ContainsCell(in info, anchorCell))
                    continue;

                if (entityManager.HasComponent<CampaignMissionUnitRoleComponent>(candidate) &&
                    !entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(candidate)
                        .MissionRoleId.Equals(missionRoleId))
                    continue;

                forwardPost = candidate;
                matches++;
            }

            return matches == 1;
        }

        private static bool TryFindUniqueBaseAnchor(
            ref OperationMapBlob map,
            in FixedString64Bytes anchorId,
            out OperationMapAnchorBlob anchor)
        {
            anchor = default;
            int matches = 0;
            for (int index = 0; index < map.Anchors.Length; index++)
            {
                ref OperationMapAnchorBlob candidate = ref map.Anchors[index];
                if (!candidate.Id.Equals(anchorId))
                    continue;

                anchor = candidate;
                matches++;
            }

            return matches == 1 && anchor.Kind == OperationMapAnchorKind.Base &&
                   anchor.FactionId == FactionIdentity.PlayerFactionId && map.Grid.CellSize > 0f;
        }

        private static bool ContainsCell(in RuntimeBuildingCombatInfo building, int2 cell) =>
            cell.x >= building.OriginCell.x && cell.y >= building.OriginCell.y &&
            cell.x < building.OriginCell.x + building.FootprintCells.x &&
            cell.y < building.OriginCell.y + building.FootprintCells.y;

        private static void BindForwardPostRole(
            EntityManager entityManager,
            Entity forwardPost,
            in FixedString64Bytes sessionToken,
            in FixedString64Bytes missionRoleId)
        {
            CampaignMissionUnitRoleComponent role = new()
            {
                MissionRoleId = missionRoleId,
                SessionToken = sessionToken
            };
            if (entityManager.HasComponent<CampaignMissionUnitRoleComponent>(forwardPost))
                entityManager.SetComponentData(forwardPost, role);
            else
                entityManager.AddComponentData(forwardPost, role);
        }
    }
}
