using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(SkirmishOutcomeSystem))]
    public partial struct SkirmishSessionCleanupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            // DestroyEntity is illegal while this session query is still iterating.
            using NativeArray<Entity> entities = state
                .GetEntityQuery(ComponentType.ReadOnly<SkirmishExpandedSessionComponent>())
                .ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity) || !em.HasComponent<SkirmishExpandedSessionComponent>(entity))
                    continue;
                SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                bool requested = em.HasComponent<SkirmishExpandedCleanupRequest>(entity);
                if (!requested &&
                    session.Phase != SkirmishSessionPhase.Failed &&
                    session.Phase != SkirmishSessionPhase.Finished)
                    continue;

                FixedString64Bytes sessionId = session.SessionId;
                SkirmishScenarioSpawnSystem.DestroyAttemptOwned(em, sessionId);
                if (!em.Exists(entity))
                    continue;
                if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(entity))
                {
                    em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(entity).Catalog?.Dispose();
                    em.RemoveComponent<SkirmishVisualPrefabCatalogRecord>(entity);
                }
                if (em.HasComponent<SkirmishResolvedSetupRecord>(entity))
                    em.RemoveComponent<SkirmishResolvedSetupRecord>(entity);
                if (em.HasComponent<SkirmishExpandedCleanupRequest>(entity))
                    em.RemoveComponent<SkirmishExpandedCleanupRequest>(entity);
                session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                session.Phase = SkirmishSessionPhase.Cleaning;
                session.InitializationComplete = 0;
                session.SpawnComplete = 0;
                em.SetComponentData(entity, session);
            }
        }
    }
}
