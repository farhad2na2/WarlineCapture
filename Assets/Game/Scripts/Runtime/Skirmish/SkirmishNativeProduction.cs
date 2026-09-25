using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    // The shared building pipeline calls this adapter at its transaction boundaries.
    // With no expanded catalog, existing campaign and legacy skirmish behavior is unchanged.
    internal static class SkirmishNativeProduction
    {
        internal static bool TrySession(EntityManager em, out Entity session)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishProductionCatalogRecord));
            session = query.CalculateEntityCount() == 1 ? query.GetSingletonEntity() : Entity.Null;
            return session != Entity.Null;
        }

        internal static Game.Skirmish.Contracts.SkirmishReadinessStage Readiness(EntityManager em, Entity session)
        {
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            return em.HasComponent<SkirmishResearchStateComponent>(session)
                ? em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness : setup.Readiness;
        }

        internal static BuildingUiCommandSystemHelper.CampRequestFailure RequestFailure(EntityManager em, GameObject prefab, bool building)
        {
            if (!TrySession(em, out var session)) return BuildingUiCommandSystemHelper.CampRequestFailure.None;
            var catalog = em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;
            if (prefab == null || catalog == null || !catalog.Allows(prefab, building))
                return BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection;
            var required = Game.Skirmish.Contracts.SkirmishReadinessStage.Field;
            if (building)
            {
                var producer = SkirmishStructureIds.ProducerFor(SkirmishStructureIds.FromVisualKey(prefab.name));
                if (producer == Game.Skirmish.Contracts.SkirmishProducerKind.Airport)
                    required = Game.Skirmish.Contracts.SkirmishReadinessStage.FullArsenal;
                else if (producer == Game.Skirmish.Contracts.SkirmishProducerKind.Helipad ||
                    producer == Game.Skirmish.Contracts.SkirmishProducerKind.IntelStation)
                    required = Game.Skirmish.Contracts.SkirmishReadinessStage.Established;
            }
            else if (catalog.TryRole(prefab.name, out var role))
                required = SkirmishProductionEligibility.RequiredReadiness(role.RoleKind);
            if (Readiness(em, session) < required)
                return required == Game.Skirmish.Contracts.SkirmishReadinessStage.FullArsenal
                    ? BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessFullArsenalRequired
                    : BuildingUiCommandSystemHelper.CampRequestFailure.ReadinessEstablishedRequired;
            if (!building && catalog.TryRole(prefab.name, out var researchRole) && ResearchBlocks(em, session, researchRole.Producer, 1))
                return BuildingUiCommandSystemHelper.CampRequestFailure.ResearchInProgress;
            return BuildingUiCommandSystemHelper.CampRequestFailure.None;
        }

        private static bool ResearchBlocks(EntityManager em, Entity session, SkirmishProducerKind producer, byte faction)
        {
            if (em.HasBuffer<SkirmishResearchQueueItem>(session))
                foreach (var research in em.GetBuffer<SkirmishResearchQueueItem>(session))
                    if (research.FactionId == faction && research.Producer == producer && research.Phase == SkirmishResearchPhase.Researching)
                        return true;
            return false;
        }

        internal static bool CanQueue(EntityManager em, Entity session, RuntimeBuildingEntity producer, GameObject prefab)
        {
            if (producer == null || producer.IsDestroyed || !producer.HasOwnerFaction || prefab == null ||
                !em.Exists(producer.CombatEntity) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(producer.CombatEntity)) return false;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(producer.CombatEntity);
            if (state.Phase != Game.Skirmish.Contracts.SkirmishSessionPhase.Playing || state.SpawnComplete == 0 ||
                !owner.SessionId.Equals(state.SessionId) || owner.FactionId != producer.OwnerFactionId) return false;
            var catalog = em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;
            return catalog != null && catalog.TryRole(prefab.name, out var role) &&
                !ResearchBlocks(em, session, role.Producer, producer.OwnerFactionId) &&
                SkirmishProductionService.EvaluateQueue(em, session, role.RoleId, 1,
                    SkirmishArmyProfileConfig.ResolveCached(catalog.Setup.ArmyProfileId), producer.OwnerFactionId).Accepted;
        }

        // 0 waits for the shared request processor, 1 means queued, -1 is rejection.
        internal static int RequestEnemyCounter(EntityManager em, Entity session, string prefabKey)
        {
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingFactionUnitProductionRequest));
            if (query.CalculateEntityCount() != 1) return 0;
            var requests = em.GetBuffer<BuildingFactionUnitProductionRequest>(query.GetSingletonEntity());
            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                if (request.RequestOwner != session) continue;
                if (!request.RequestAttempt.Equals(attempt)) { requests.RemoveAt(i--); continue; }
                if (request.Status == BuildingFactionUnitProductionRequest.Pending) return 0;
                requests.RemoveAt(i);
                return request.Status == BuildingFactionUnitProductionRequest.Succeeded ? 1 : -1;
            }
            requests.Add(new BuildingFactionUnitProductionRequest {
                RequestOwner = session, RequestAttempt = attempt, RequestId = -1, FactionId = 2,
                UnitId = new Unity.Collections.FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefabKey)),
                Status = BuildingFactionUnitProductionRequest.Pending
            });
            return 0;
        }

        internal static bool RequestAttemptActive(EntityManager em, in BuildingFactionUnitProductionRequest request)
        {
            if (request.RequestAttempt.IsEmpty) return request.RequestOwner == Entity.Null;
            if (!em.Exists(request.RequestOwner) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(request.RequestOwner)) return false;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(request.RequestOwner);
            return state.SessionId.Equals(request.RequestAttempt) && state.Phase == SkirmishSessionPhase.Playing;
        }

        internal static bool TryReserve(EntityManager em, RuntimeBuildingEntity producer, GameObject prefab,
            out Entity owner, out uint id)
        {
            id = 0;
            if (!TrySession(em, out owner)) return true;
            if (!CanQueue(em, owner, producer, prefab)) return false;
            var catalog = em.GetComponentObject<SkirmishProductionCatalogRecord>(owner).Catalog;
            catalog.TryRole(prefab.name, out var role);
            if (!SkirmishProductionService.TryQueue(em, owner, role.RoleId, 1,
                SkirmishArmyProfileConfig.ResolveCached(catalog.Setup.ArmyProfileId), producer.OwnerFactionId, out var decision)) return false;
            id = decision.ReservationId;
            if (SkirmishProductionService.BindNativeQueue(em, owner, id, producer.Id)) return true;
            SkirmishProductionService.TryRefundFailedDispatch(em, owner, id, out _);
            id = 0;
            return false;
        }
    }
}
