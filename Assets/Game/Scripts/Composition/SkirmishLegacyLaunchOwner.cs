using System;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    // The legacy practice queue path shared by the native host and source host.
    // It deliberately creates only the original Queued session boundary.
    internal static class SkirmishLegacyLaunchOwner
    {
        // Shared original queued-session seed step. The native launch projection
        // forwards here so the source host exercises the same suppression and
        // singleton gates without advancing the session phase.
        internal static void ApplySeed(EntityManager em)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            using (var matches = em.CreateEntityQuery(typeof(SkirmishMatchState)))
            {
                if (matches.CalculateEntityCount() != 1) return;
                Entity session = matches.GetSingletonEntity();
                var state = em.GetComponentData<SkirmishMatchState>(session);
                if (em.HasComponent<SkirmishExpandedSessionComponent>(session) &&
                    em.GetComponentData<SkirmishExpandedSessionComponent>(session).IsLegacy == 0)
                {
                    Game.Runtime.SkirmishStartupConfigSuppression.SuppressUnselectedStartupConfigs(em);
                    return;
                }
                Game.Runtime.SkirmishStartupConfigSuppression.SuppressUnselectedStartupConfigs(em);
                using var configs = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
                if (configs.CalculateEntityCount() != 1)
                    throw new InvalidOperationException("Skirmish requires one initial spawn configuration.");
                var config = configs.GetSingleton<InitialUnitsSpawnConfig>();
                config.RandomSeed = (uint)state.Seed;
                em.SetComponentData(configs.GetSingletonEntity(), config);
            }
        }

        internal static bool TryQueueIndustrialBasin(EntityManager em, QuickGameConfig config)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            config = config.NormalizeForBaseAssault();
            if (config.ScenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex) return false;
            if (Game.Runtime.ContentAccessRuntime.Evaluate("skirmish.s073") != ContentAccessState.Allowed) return false;

            using (var campaign = em.CreateEntityQuery(typeof(CampaignMissionLaunchRequestElement)))
            using (var roots = campaign.ToEntityArray(Allocator.Temp))
                foreach (var root in roots)
                    if (em.GetBuffer<CampaignMissionLaunchRequestElement>(root).Length != 0) return false;

            using (var query = em.CreateEntityQuery(typeof(SkirmishMatchState)))
                if (!query.IsEmptyIgnoreFilter) return false;

            // Preserve the original start-boundary retry cleanup before queuing.
            using (var starts = em.CreateEntityQuery(typeof(MatchStartQueueComponent)))
            using (var boundaries = starts.ToEntityArray(Allocator.Temp))
            {
                foreach (var boundary in boundaries)
                    if (em.GetComponentData<MatchStartQueueComponent>(boundary).IsStartPending != 0) return false;
                foreach (var boundary in boundaries)
                {
                    int sequence = em.GetComponentData<MatchStartQueueComponent>(boundary).LastRequestId;
                    em.SetComponentData(boundary, new MatchStartQueueComponent { LastRequestId = sequence });
                    if (em.HasBuffer<MatchStartRequestElement>(boundary)) em.GetBuffer<MatchStartRequestElement>(boundary).Clear();
                    if (em.HasBuffer<MatchStartResultElement>(boundary)) em.GetBuffer<MatchStartResultElement>(boundary).Clear();
                    if (em.HasComponent<MatchStartProgressComponent>(boundary))
                        em.SetComponentData(boundary, default(MatchStartProgressComponent));
                }
            }

            var entity = em.CreateEntity(typeof(SkirmishMatchState));
            em.SetName(entity, "SkirmishSession");
            em.SetComponentData(entity, new SkirmishMatchState
            {
                SessionId = new FixedString64Bytes(Guid.NewGuid().ToString("N")),
                Seed = config.MapSeed,
                ScenarioIndex = config.ScenarioIndex,
                Phase = SkirmishPhase.Queued
            });
            em.AddBuffer<SkirmishActionRequest>(entity);
            em.AddBuffer<SkirmishTrackedUnit>(entity);
            em.AddComponentObject(entity, new SkirmishLaunchSnapshot { Configuration = config });
            return true;
        }
    }

    internal sealed class SkirmishLaunchSnapshot : IComponentData
    {
        public QuickGameConfig Configuration;
    }
}
