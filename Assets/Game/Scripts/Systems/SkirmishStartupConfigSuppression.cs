using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    // Shared original ECS startup-config suppression used by both the native
    // world setup owner and InitialUnitsSpawnSystem.
    internal static class SkirmishStartupConfigSuppression
    {
        internal static void SuppressUnselectedStartupConfigs(EntityManager em)
        {
            using var session = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (session.IsEmptyIgnoreFilter) return;
            using var expanded = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            bool authoredRoster = expanded.CalculateEntityCount() == 1 &&
                expanded.GetSingleton<SkirmishExpandedSessionComponent>().IsLegacy == 0;
            using var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<InitialUnitsSpawnConfig>() },
                None = authoredRoster ? Array.Empty<ComponentType>() :
                    new[] { ComponentType.ReadOnly<CustomGameStartupStateComponent>() }
            });
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                em.RemoveComponent<InitialUnitsSpawnConfig>(entity);
                if (em.HasComponent<InitialUsableFuelStorageSeedPending>(entity))
                    em.RemoveComponent<InitialUsableFuelStorageSeedPending>(entity);
                if (em.HasComponent<InitialUnitsBlockerChurnConfig>(entity))
                    em.RemoveComponent<InitialUnitsBlockerChurnConfig>(entity);
            }
        }
    }
}
