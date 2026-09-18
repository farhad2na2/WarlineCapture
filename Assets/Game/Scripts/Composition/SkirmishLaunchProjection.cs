using System;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal sealed class SkirmishLaunchSnapshot : IComponentData
    {
        public QuickGameConfig Configuration;
    }

    internal static class SkirmishLaunchProjection
    {
        public const string MissionId = "skirmish.base_assault";
        public const string ScenarioId = "scenario.skirmish.desert_base_standard";
        public const string OperationMapId = "opmap.skirmish.desert_base_01";

        public static bool TryQueue(EntityManager em, QuickGameConfig config)
        {
            using var campaign = em.CreateEntityQuery(typeof(CampaignMissionLaunchRequestElement));
            using var roots = campaign.ToEntityArray(Allocator.Temp);
            foreach (var root in roots)
                if (em.GetBuffer<CampaignMissionLaunchRequestElement>(root).Length != 0) return false;
            using var query = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (!query.IsEmptyIgnoreFilter) return false;
            // The start boundary survives scene unloads. A retry/replay must not
            // inherit the previous attempt's Failed or Started status.
            using (var starts = em.CreateEntityQuery(typeof(MatchStartQueueComponent)))
            {
                using var boundaries = starts.ToEntityArray(Allocator.Temp);
                foreach (var boundary in boundaries)
                    if (em.GetComponentData<MatchStartQueueComponent>(boundary).IsStartPending != 0) return false;
                foreach (var boundary in boundaries)
                {
                    int sequence = em.GetComponentData<MatchStartQueueComponent>(boundary).LastRequestId;
                    em.SetComponentData(boundary, new MatchStartQueueComponent { LastRequestId = sequence });
                    if (em.HasBuffer<MatchStartRequestElement>(boundary)) em.GetBuffer<MatchStartRequestElement>(boundary).Clear();
                    if (em.HasBuffer<MatchStartResultElement>(boundary)) em.GetBuffer<MatchStartResultElement>(boundary).Clear();
                    if (em.HasComponent<MatchStartProgressComponent>(boundary)) em.SetComponentData(boundary, default(MatchStartProgressComponent));
                }
            }
            var entity = em.CreateEntity(typeof(SkirmishMatchState));
            config = config.NormalizeForBaseAssault();
            em.SetName(entity, "SkirmishSession");
            em.SetComponentData(entity, new SkirmishMatchState
            {
                SessionId = new FixedString64Bytes(Guid.NewGuid().ToString("N")),
                Seed = config.MapSeed,
                Phase = SkirmishPhase.Queued
            });
            em.AddBuffer<SkirmishActionRequest>(entity);
            em.AddBuffer<SkirmishTrackedUnit>(entity);
            em.AddComponentObject(entity, new SkirmishLaunchSnapshot { Configuration = config });
            return true;
        }

        public static bool TryGet(EntityManager em, out Entity entity, out SkirmishMatchState state)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishMatchState));
            entity = Entity.Null; state = default;
            if (query.CalculateEntityCount() != 1) return false;
            entity = query.GetSingletonEntity(); state = em.GetComponentData<SkirmishMatchState>(entity);
            return true;
        }

        public static void PrepareMap(EntityManager em)
        {
            if (!TryGet(em, out var session, out var match) || match.Phase != SkirmishPhase.Queued) return;
            // The shared map's authored ownership belongs to campaign scenarios. Only
            // this preset's newly spawned force and supply buildings belong to its sides.
            using var query = em.CreateEntityQuery(typeof(OperationMapBuildingComponent), typeof(Faction));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                em.SetComponentData(entity, new Faction { Id = 0 });
                if (em.HasComponent<RuntimeBuildingCombatInfo>(entity))
                {
                    var info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity); info.OwnerFactionId = 0;
                    em.SetComponentData(entity, info);
                }
                if (em.HasComponent<BuildingResourceStorageComponent>(entity))
                {
                    var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity); storage.OwnerFactionId = 0;
                    em.SetComponentData(entity, storage);
                }
                if (em.HasComponent<AIControlledTag>(entity)) em.RemoveComponent<AIControlledTag>(entity);
            }
            match.Phase = SkirmishPhase.Preparing; em.SetComponentData(session, match);
        }

        public static void ApplySeed(EntityManager em)
        {
            if (!TryGet(em, out _, out var state)) return;
            Game.Runtime.SkirmishWorldSetup.SuppressUnselectedStartupConfigs(em);
            using var query = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
            if (query.CalculateEntityCount() != 1) throw new InvalidOperationException("Skirmish requires one initial spawn configuration.");
            var config = query.GetSingleton<InitialUnitsSpawnConfig>(); config.RandomSeed = (uint)state.Seed;
            em.SetComponentData(query.GetSingletonEntity(), config);
        }
    }
}
