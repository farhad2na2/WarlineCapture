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
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            using var entities = sessions.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (Entity entity in entities)
            {
                var session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                if (session.IsLegacy != 0 || session.Phase != SkirmishSessionPhase.Playing)
                    continue;
                bool sessionPaused = paused;
                if (em.HasComponent<SkirmishObjectiveClockComponent>(entity) &&
                    em.GetComponentData<SkirmishObjectiveClockComponent>(entity).Paused != 0)
                    sessionPaused = true;
                if (em.HasBuffer<SkirmishReadinessRequest>(entity))
                {
                    var requests = em.GetBuffer<SkirmishReadinessRequest>(entity);
                    using var snapshot = requests.ToNativeArray(Unity.Collections.Allocator.Temp);
                    requests.Clear();
                    if (!sessionPaused)
                        foreach (var request in snapshot)
                        {
                            using var evidence = AriaCommandEvidence.Enter(request.InputReceipt);
                            if (request.CancelResearchId != 0)
                                SkirmishResearchService.TryCancel(em, entity, request.CancelResearchId, out _);
                            else
                                SkirmishResearchService.TryQueue(em, entity, SkirmishResearchKind.Readiness, 1, out _);
                        }
                }
                SkirmishResearchService.Tick(em, entity, dt, sessionPaused);
                NotifyDeadProducers(em, entity, session.SessionId);
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
