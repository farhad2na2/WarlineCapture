using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishResultSettlementSystem))]
    public partial struct SkirmishExpandedSessionControlSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            bool gameplayPaused = SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) &&
                                  gameplay.SimulationActive == 0;
            float dt = SystemAPI.Time.DeltaTime;
            foreach ((RefRO<SkirmishExpandedSessionComponent> session, Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0)
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
            if (!em.HasComponent<SkirmishMatchState>(session) ||
                !em.HasComponent<SkirmishResultComponent>(session))
                return;
            var result = em.GetComponentData<SkirmishResultComponent>(session);
            if (result.Frozen == 0)
                return;
            var match = em.GetComponentData<SkirmishMatchState>(session);
            match.Phase = SkirmishPhase.Finished;
            match.Outcome = result.Outcome == SkirmishOutcomeKind.Victory
                ? SkirmishOutcome.Victory
                : result.Outcome == SkirmishOutcomeKind.Defeat
                    ? SkirmishOutcome.Defeat
                    : result.Outcome == SkirmishOutcomeKind.Draw
                        ? SkirmishOutcome.Draw
                        : SkirmishOutcome.None;
            match.Reason = result.Reason == SkirmishEndReasonKind.Surrender
                ? SkirmishEndReason.Surrender
                : result.Reason == SkirmishEndReasonKind.TimeLimit
                    ? SkirmishEndReason.TimeLimit
                    : result.Reason == SkirmishEndReasonKind.BothBasesDestroyed
                        ? SkirmishEndReason.BothBasesDestroyed
                        : result.Reason == SkirmishEndReasonKind.MainBaseDestroyed
                            ? SkirmishEndReason.MainBaseDestroyed
                            : SkirmishEndReason.None;
            if (result.SaveAcknowledged != 0)
                match.ResultSaved = 1;
            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
                match.ElapsedSeconds = em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds;
            em.SetComponentData(session, match);
        }
    }
}
