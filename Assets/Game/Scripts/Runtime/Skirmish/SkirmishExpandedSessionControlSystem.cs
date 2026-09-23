using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishResultSettlementSystem))]
    public partial struct SkirmishExpandedSessionControlSystem : ISystem
    {
        private EntityQuery sessions;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            sessions = state.GetEntityQuery(ComponentType.ReadOnly<SkirmishExpandedSessionComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            bool gameplayPaused = SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) &&
                                  gameplay.SimulationActive == 0;
            float dt = SystemAPI.Time.DeltaTime;
            using NativeArray<Entity> entities = sessions.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                if (session.IsLegacy != 0)
                    continue;
                bool paused = gameplayPaused;
                if (em.HasComponent<SkirmishExpandedPauseRequest>(entity))
                {
                    paused = em.GetComponentData<SkirmishExpandedPauseRequest>(entity).Paused != 0;
                    if (paused)
                        SkirmishExpandedSessionControlService.TryPause(em, entity, out _);
                    else
                        SkirmishExpandedSessionControlService.TryResume(em, entity);
                    em.RemoveComponent<SkirmishExpandedPauseRequest>(entity);
                }
                else if (paused && em.HasComponent<SkirmishObjectiveClockComponent>(entity) &&
                         em.GetComponentData<SkirmishObjectiveClockComponent>(entity).Paused == 0)
                {
                    SkirmishExpandedSessionControlService.TryPause(em, entity, out _);
                }

                ProjectMatchPhase(em, entity);
                SkirmishBaseAssaultHudProjection.Observe(em, entity, dt, paused);
                if (em.HasComponent<SkirmishResultComponent>(entity) &&
                    em.GetComponentData<SkirmishResultComponent>(entity).Frozen != 0 &&
                    em.GetComponentData<SkirmishResultComponent>(entity).SaveAcknowledged == 0)
                {
                    SkirmishExpandedSessionControlService.TrySettle(em, entity);
                }

                ProjectMatchResult(em, entity);
            }
        }

        private static void ProjectMatchPhase(EntityManager em, Entity session)
        {
            SkirmishExpandedSessionControlService.ProjectMatchPhase(em, session);
        }

        private static void ProjectMatchResult(EntityManager em, Entity session)
        {
            SkirmishExpandedSessionControlService.ProjectTerminalMatch(em, session);
        }
    }
}
