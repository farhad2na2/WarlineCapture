using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishCapacityLifecycleSystem))]
    public partial struct SkirmishResearchSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            bool paused = SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) &&
                          gameplay.SimulationActive == 0;
            float dt = SystemAPI.Time.DeltaTime;
            foreach ((RefRO<SkirmishExpandedSessionComponent> session, Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0 || session.ValueRO.Phase != SkirmishSessionPhase.Playing)
                    continue;
                if (em.HasComponent<SkirmishObjectiveClockComponent>(entity) &&
                    em.GetComponentData<SkirmishObjectiveClockComponent>(entity).Paused != 0)
                    paused = true;
                SkirmishResearchService.Tick(em, entity, dt, paused);
                NotifyDeadProducers(em, entity, session.ValueRO.SessionId);
            }
        }

        private static void NotifyDeadProducers(
            EntityManager em,
            Entity session,
            Unity.Collections.FixedString64Bytes sessionId)
        {
            using var query = em.CreateEntityQuery(
                typeof(SkirmishStructureIdentityComponent),
                typeof(SkirmishAttemptOwnedComponent),
                typeof(UnitHealth));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity structure = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(structure);
                if (!owned.SessionId.Equals(sessionId) || em.GetComponentData<UnitHealth>(structure).Current > 0)
                    continue;
                SkirmishProducerKind producer = em.GetComponentData<SkirmishStructureIdentityComponent>(structure).Producer;
                if (producer == SkirmishProducerKind.None)
                    continue;
                if (SkirmishProductionService.HasLivingProducer(em, session, producer, owned.FactionId))
                    continue;
                SkirmishResearchService.NotifyProducerDestroyed(em, session, producer, owned.FactionId);
                SkirmishProductionService.NotifyProducerDestroyed(em, session, producer, owned.FactionId);
            }
        }
    }
}
