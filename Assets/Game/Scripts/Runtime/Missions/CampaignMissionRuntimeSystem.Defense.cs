using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private bool TryAdvanceDefense(ref SystemState state, Entity root, in CampaignMissionRuntimeComponent runtime)
        {
            if (!SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int index)) return false;
            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[index];
            if (definition.Defense.Enabled == 0) return false;
            if (runtime.Outcome != MissionOutcomeKind.None) return true;
            EntityManager em = state.EntityManager;
            CampaignMissionAttemptFactsComponent facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            bool controlsReady = false;
            if (em.HasComponent<CampaignMissionDefenseStateComponent>(root))
            {
                CampaignMissionDefenseStateComponent defense = em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
                if (!defense.SessionToken.Equals(runtime.SessionToken) || defense.AttemptOrdinal != runtime.AttemptOrdinal ||
                    defense.SourceVersion != runtime.SourceVersion) return true;
                TryPrepareDefenseProducer(ref state, root, ref defense, ref definition, ref facts);
                if (em.HasComponent<CampaignMissionOpeningPresentationComponent>(root))
                {
                    CampaignMissionOpeningPresentationComponent opening = em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root);
                    if (opening.SessionToken.Equals(runtime.SessionToken) && opening.Stage >= 6) defense.OpeningComplete = 1;
                }
                controlsReady = defense.OpeningComplete != 0 && defense.InitialProducerReady != 0;
                if (facts.CommandSquadSpawned != 0)
                    ProjectDefenseRoster(em, root, in runtime, in defense, ref definition, ref facts);
                if (controlsReady && runtime.Phase == MissionPhaseKind.Engage)
                    facts.ElapsedMilliseconds = SaturatingAddMilliseconds(facts.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
                em.SetComponentData(root, defense);
            }
            em.SetComponentData(root, facts);
            if (CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, controlsReady, out CampaignMissionRuntimeComponent next))
                em.SetComponentData(root, next);
            return true;
        }

        private static void ProjectDefenseRoster(EntityManager em, Entity root,
            in CampaignMissionRuntimeComponent runtime, in CampaignMissionDefenseStateComponent defense,
            ref CampaignMissionDefinitionBlob definition, ref CampaignMissionAttemptFactsComponent facts)
        {
            DynamicBuffer<CampaignMissionDefenseMember> members = em.GetBuffer<CampaignMissionDefenseMember>(root);
            int defeated = 0, civiliansLost = 0, squadLost = 0, aliveSquad = 0;
            float radiusSquared = definition.Defense.InnerCoreRadius * definition.Defense.InnerCoreRadius;
            for (int i = 0; i < members.Length; i++)
            {
                CampaignMissionDefenseMember member = members[i];
                bool exists = em.Exists(member.Entity) && em.HasComponent<UnitHealth>(member.Entity);
                if (member.Defeated == 0 && !exists)
                {
                    facts.HostileRosterIntegrityFault = 1;
                    continue;
                }
                UnitHealth health = exists ? em.GetComponentData<UnitHealth>(member.Entity) : default;
                if (health.Max > 0) member.HealthInitialized = 1;
                if (member.HealthInitialized != 0 && health.Current <= 0) member.Defeated = 1;
                members[i] = member;
                if (member.ElementIndex >= 0)
                {
                    defeated += member.Defeated;
                    if (runtime.Phase == MissionPhaseKind.Engage && member.Defeated == 0 &&
                        member.HealthInitialized != 0 && !em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity) &&
                        em.HasComponent<LocalTransform>(member.Entity) &&
                        math.distancesq(em.GetComponentData<LocalTransform>(member.Entity).Position.xz,
                            defense.InnerCoreCenter.xz) <= radiusSquared) facts.CoreBreached = 1;
                }
                else if (member.FactionId == FactionIdentity.NeutralFactionId) civiliansLost += member.Defeated;
                else if (member.IsSensor == 0)
                {
                    squadLost += member.Defeated;
                    aliveSquad += member.Defeated == 0 ? 1 : 0;
                }
            }
            facts.HostileDefeatedCount = math.max(facts.HostileDefeatedCount, defeated);
            facts.CivilianLossCount = math.max(facts.CivilianLossCount, civiliansLost);
            facts.SquadLossCount = math.max(facts.SquadLossCount, squadLost);
            facts.CommandSquadAlive = aliveSquad > 0 ? (byte)1 : (byte)0;
        }

        private void TryPrepareDefenseProducer(ref SystemState state, Entity root,
            ref CampaignMissionDefenseStateComponent defense, ref CampaignMissionDefinitionBlob definition,
            ref CampaignMissionAttemptFactsComponent facts)
        {
            if (defense.InitialProducerReady != 0 ||
                !SystemAPI.TryGetSingletonEntity<BuildingRuntimeStateTag>(out Entity boundary) ||
                !state.EntityManager.HasBuffer<BuildingRuntimeSpawnRequest>(boundary) ||
                !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) || !metadata.Blob.IsCreated) return;
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests = state.EntityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            if (defense.InitialProducerRequestId == 0)
            {
                if (!CampaignMissionSpawnSystem.TryFindAnchor(ref metadata.Blob.Value,
                        definition.Defense.InitialProducerAnchorId, out OperationMapAnchorBlob anchor)) return;
                int id = 1;
                for (int i = 0; i < requests.Length; i++) id = math.max(id, requests[i].RequestId + 1);
                requests.Add(new BuildingRuntimeSpawnRequest
                {
                    RequestId = id, RequestKind = BuildingRuntimeSpawnRequest.KindBuilding,
                    FactionId = FactionIdentity.PlayerFactionId, HasOwnerFaction = 1,
                    BuildingId = definition.Defense.InitialProducerConfigId,
                    PreferredOrigin = CampaignMissionSpawnSystem.ToGridCell(anchor.Position, metadata.Blob.Value.Grid)
                });
                defense.InitialProducerRequestId = id;
                return;
            }
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i].RequestId != defense.InitialProducerRequestId) continue;
                if (requests[i].Status == BuildingRuntimeSpawnRequest.Succeeded) defense.InitialProducerReady = 1;
                if (requests[i].Status == BuildingRuntimeSpawnRequest.Failed) facts.HostileRosterIntegrityFault = 1;
                return;
            }
        }
    }
}
