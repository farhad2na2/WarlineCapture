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
            foreach ((RefRW<SkirmishExpandedSessionComponent> sessionRef, Entity entity) in
                     SystemAPI.Query<RefRW<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                ref SkirmishExpandedSessionComponent session = ref sessionRef.ValueRW;
                bool requested = em.HasComponent<SkirmishExpandedCleanupRequest>(entity);
                if (!requested &&
                    session.Phase != SkirmishSessionPhase.Failed &&
                    session.Phase != SkirmishSessionPhase.Finished)
                    continue;

                SkirmishScenarioSpawnSystem.DestroyAttemptOwned(em, session.SessionId);
                if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(entity))
                {
                    em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(entity).Catalog?.Dispose();
                    em.RemoveComponent<SkirmishVisualPrefabCatalogRecord>(entity);
                }
                if (em.HasComponent<SkirmishResolvedSetupRecord>(entity))
                    em.RemoveComponent<SkirmishResolvedSetupRecord>(entity);
                if (em.HasComponent<SkirmishExpandedCleanupRequest>(entity))
                    em.RemoveComponent<SkirmishExpandedCleanupRequest>(entity);
                session.Phase = SkirmishSessionPhase.Cleaning;
                session.InitializationComplete = 0;
                session.SpawnComplete = 0;
                em.SetComponentData(entity, session);
            }
        }
    }
}
