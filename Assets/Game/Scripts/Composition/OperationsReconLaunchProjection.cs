using System;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal sealed class OperationsReconLaunchReference : IComponentData
    {
        public OperationsReconMissionConfig Definition;
        public string StartupFailure;
    }

    internal static class OperationsReconLaunchProjection
    {
        internal static bool TryGet(EntityManager em, out Entity entity, out OperationsReconMissionComponent mission)
        {
            using var query = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
            entity = Entity.Null; mission = default;
            if (query.CalculateEntityCount() != 1) return false;
            entity = query.GetSingletonEntity();
            mission = em.GetComponentData<OperationsReconMissionComponent>(entity);
            return true;
        }

        internal static bool TryQueue(EntityManager em, OperationsReconMissionConfig definition,
            string sessionId, out string error)
        {
            error = string.Empty;
            if (definition == null || !definition.TryValidate(out error)) return false;
            if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 60)
            { error = "Missing persisted Operations session identity."; return false; }
            using var operations = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
            using var skirmish = em.CreateEntityQuery(typeof(SkirmishMatchState));
            using var campaign = em.CreateEntityQuery(typeof(CampaignMissionLaunchRequestElement));
            if (!operations.IsEmptyIgnoreFilter || !skirmish.IsEmptyIgnoreFilter)
            { error = "Another match is already queued."; return false; }
            using (var roots = campaign.ToEntityArray(Allocator.Temp))
                foreach (var root in roots)
                    if (em.GetBuffer<CampaignMissionLaunchRequestElement>(root).Length != 0)
                    { error = "A Campaign mission is already queued."; return false; }
            using (var starts = em.CreateEntityQuery(typeof(MatchStartQueueComponent)))
            {
                using var boundaries = starts.ToEntityArray(Allocator.Temp);
                foreach (var boundary in boundaries)
                    if (em.GetComponentData<MatchStartQueueComponent>(boundary).IsStartPending != 0)
                    { error = "Another match is starting."; return false; }
                foreach (var boundary in boundaries)
                {
                    int sequence = em.GetComponentData<MatchStartQueueComponent>(boundary).LastRequestId;
                    em.SetComponentData(boundary, new MatchStartQueueComponent { LastRequestId = sequence });
                    if (em.HasBuffer<MatchStartRequestElement>(boundary)) em.GetBuffer<MatchStartRequestElement>(boundary).Clear();
                    if (em.HasBuffer<MatchStartResultElement>(boundary)) em.GetBuffer<MatchStartResultElement>(boundary).Clear();
                    if (em.HasComponent<MatchStartProgressComponent>(boundary)) em.SetComponentData(boundary, default(MatchStartProgressComponent));
                }
            }
            Entity session = em.CreateEntity(typeof(OperationsReconMissionComponent), typeof(OperationsReconEvidenceComponent));
            em.SetName(session, "Operations Street Signals");
            em.SetComponentData(session, new OperationsReconMissionComponent
            {
                SessionId = new FixedString64Bytes(sessionId), Phase = OperationsReconPhase.Preparing,
                DeadlineSeconds = definition.deadlineSeconds, ScanSeconds = definition.scanSeconds,
                EvidenceSeconds = definition.evidenceSeconds, ExitPosition = definition.exitPosition, ExitRadius = 8f
            });
            em.SetComponentData(session, new OperationsReconEvidenceComponent { Position = definition.evidencePosition });
            var sites = em.AddBuffer<OperationsReconSiteElement>(session);
            for (int i = 0; i < definition.scanPositions.Length; i++) sites.Add(new OperationsReconSiteElement
            { RoleId = new FixedString64Bytes("signal_" + (char)('a' + i)), Position = definition.scanPositions[i], Radius = 8f });
            em.AddBuffer<OperationsReconRosterElement>(session);
            em.AddBuffer<OperationsReconActionElement>(session);
            em.AddComponentObject(session, new OperationsReconLaunchReference { Definition = definition });
            return true;
        }

        internal static void PrepareMap(EntityManager em)
        {
            if (!TryGet(em, out _, out var mission) || mission.Phase != OperationsReconPhase.Preparing) return;
            using var query = em.CreateEntityQuery(typeof(OperationMapBuildingComponent), typeof(Faction));
            using var buildings = query.ToEntityArray(Allocator.Temp);
            foreach (var building in buildings)
            {
                em.SetComponentData(building, new Faction { Id = 0 });
                if (em.HasComponent<RuntimeBuildingCombatInfo>(building))
                {
                    var info = em.GetComponentData<RuntimeBuildingCombatInfo>(building); info.OwnerFactionId = 0;
                    em.SetComponentData(building, info);
                }
                if (em.HasComponent<BuildingResourceStorageComponent>(building))
                {
                    var storage = em.GetComponentData<BuildingResourceStorageComponent>(building); storage.OwnerFactionId = 0;
                    em.SetComponentData(building, storage);
                }
                if (em.HasComponent<AIControlledTag>(building)) em.RemoveComponent<AIControlledTag>(building);
            }
            // The physical city also contains baked vehicles. They are scenery for
            // this infantry mission and must not add another faction's combat force.
            using var vehicleQuery = em.CreateEntityQuery(typeof(OperationMapAuthoredVehiclePresentation), typeof(Faction));
            using var vehicles = vehicleQuery.ToEntityArray(Allocator.Temp);
            foreach (var vehicle in vehicles)
            {
                em.SetComponentData(vehicle, new Faction { Id = 0 });
                var presentation = em.GetComponentData<OperationMapAuthoredVehiclePresentation>(vehicle);
                presentation.FactionId = 0; em.SetComponentData(vehicle, presentation);
                if (em.HasComponent<UnitCombat>(vehicle))
                {
                    var combat = em.GetComponentData<UnitCombat>(vehicle); combat.CanAttack = 0; combat.AutoEngage = 0;
                    em.SetComponentData(vehicle, combat);
                }
                if (em.HasComponent<EngageTarget>(vehicle)) em.SetComponentData(vehicle, default(EngageTarget));
                if (em.HasComponent<UnitRespawnPrefab>(vehicle)) em.RemoveComponent<UnitRespawnPrefab>(vehicle);
                if (em.HasComponent<AIControlledTag>(vehicle)) em.RemoveComponent<AIControlledTag>(vehicle);
            }
        }
    }
}
