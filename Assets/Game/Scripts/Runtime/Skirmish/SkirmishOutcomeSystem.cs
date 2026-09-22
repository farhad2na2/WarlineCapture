using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct SkirmishOutcomeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            // Expanded sessions return from SkirmishRulesSystem before it can set
            // SkirmishPhase.Finished. This writer is the match-phase publisher.
            // AddComponent inside the query throws, so the result component is
            // created only after iteration. Phase and match are written first.
            var pending = new NativeList<Entity>(Allocator.Temp);
            foreach ((RefRW<SkirmishExpandedSessionComponent> sessionRef,
                      RefRO<SkirmishObjectiveStateComponent> objective,
                      Entity entity) in
                     SystemAPI.Query<RefRW<SkirmishExpandedSessionComponent>,
                         RefRO<SkirmishObjectiveStateComponent>>().WithEntityAccess())
            {
                if (sessionRef.ValueRO.IsLegacy != 0 || objective.ValueRO.Terminal == 0)
                    continue;
                if (em.HasComponent<SkirmishResultComponent>(entity) &&
                    em.GetComponentData<SkirmishResultComponent>(entity).Frozen != 0)
                {
                    if (sessionRef.ValueRO.Phase != SkirmishSessionPhase.Finished)
                        sessionRef.ValueRW.Phase = SkirmishSessionPhase.Finished;
                    SkirmishExpandedSessionControlService.ProjectTerminalMatch(em, entity);
                    continue;
                }

                sessionRef.ValueRW.Phase = SkirmishSessionPhase.Finished;
                pending.Add(entity);
            }

            bool ended = false;
            for (int i = 0; i < pending.Length; i++)
            {
                Entity entity = pending[i];
                SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(entity);
                SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                var result = new SkirmishResultComponent
                {
                    Outcome = objective.Outcome,
                    Reason = objective.Reason,
                    SetupHash = session.SetupHash,
                    SessionId = session.SessionId,
                    Frozen = 1,
                    SaveAcknowledged = 0
                };
                if (em.HasComponent<SkirmishResultComponent>(entity))
                    em.SetComponentData(entity, result);
                else
                    em.AddComponentData(entity, result);
                SkirmishExpandedSessionControlService.ProjectTerminalMatch(em, entity);
                ended = true;
            }

            pending.Dispose();
            if (!ended)
                return;
            if (SystemAPI.TryGetSingleton<RuntimeGameplayStateComponent>(out var gameplay))
            {
                gameplay.SimulationActive = 0;
                gameplay.PlayRequested = 0;
                Entity gameplayEntity = SystemAPI.GetSingletonEntity<RuntimeGameplayStateComponent>();
                em.SetComponentData(gameplayEntity, gameplay);
            }
        }
    }
}
